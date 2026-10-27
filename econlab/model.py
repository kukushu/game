from __future__ import annotations

import hashlib
import copy
import json
import math
import random
from dataclasses import asdict, dataclass, fields
from pathlib import Path

SECTORS = ("food", "goods")


@dataclass
class Config:
    households: int = 240
    firms: int = 16
    initial_household_cash: float = 20
    initial_firm_cash: float = 120
    initial_treasury: float = 2000
    food_need: float = 1.0
    goods_need: float = 0.55
    food_productivity: float = 3.0
    goods_productivity: float = 1.5
    food_price: float = 3.5
    goods_price: float = 8.0
    food_wage: float = 8.0
    goods_wage: float = 11.0
    income_spend: float = 0.85
    wealth_spend: float = 0.06
    demand_memory: float = 0.35
    inventory_buffer: float = 0.25
    price_adjustment: float = 0.025
    wage_adjustment: float = 0.02
    food_spoilage: float = 0.30
    goods_spoilage: float = 0.025
    max_workers: int = 28
    max_commute: float = 1.35
    max_shopping: float = 2.0
    river_penalty: float = 1.35
    job_search_rate: float = 0.15
    commute_disutility: float = 1.4
    shopping_disutility: float = 0.6
    dividend_fraction: float = 0.25
    reserve_payrolls: float = 2.0
    skill_premium: float = 0.25
    enable_price_adaptation: bool = True
    enable_demand_adaptation: bool = True
    enable_entry: bool = True
    search_all_sellers: bool = False
    shuffle_buyers: bool = True

    def validate(self):
        for field in fields(self):
            value = getattr(self, field.name)
            if isinstance(value, (int, float)) and not math.isfinite(value):
                raise ValueError(f"{field.name} must be finite")
        for key in ("households", "firms", "max_workers"):
            if not isinstance(getattr(self, key), int):
                raise ValueError(f"{key} must be an integer")
        if self.households < 16 or self.firms < 8 or self.firms > self.households:
            raise ValueError("Require households >= 16 and 8 <= firms <= households")
        for key in ("initial_household_cash", "initial_firm_cash", "initial_treasury",
                    "food_need", "goods_need", "food_productivity", "goods_productivity",
                    "food_price", "goods_price", "food_wage", "goods_wage", "max_commute", "max_shopping"):
            if not math.isfinite(getattr(self, key)) or getattr(self, key) <= 0:
                raise ValueError(f"{key} must be positive and finite")
        for key in ("income_spend", "wealth_spend", "demand_memory", "inventory_buffer",
                    "price_adjustment", "wage_adjustment", "food_spoilage", "goods_spoilage",
                    "job_search_rate", "dividend_fraction"):
            if not 0 <= getattr(self, key) <= 1:
                raise ValueError(f"{key} must be in [0,1]")
        if self.max_workers < 1 or self.reserve_payrolls < 0 or self.skill_premium < 0:
            raise ValueError("Invalid labor / reserve / skill settings")
        if min(self.river_penalty, self.commute_disutility, self.shopping_disutility) < 0:
            raise ValueError("Distance costs must be nonnegative")


@dataclass
class Household:
    id: int
    district: int
    cash: int
    skill: float
    job: int | None = None
    income: int = 0
    food: float = 0
    goods: float = 0
    training: int = 0


@dataclass
class Firm:
    id: int
    district: int
    sector: str
    owner: int
    cash: int
    price: int
    wage: int
    productivity: float
    expected: float
    inventory: float = 0
    sales: float = 0
    unmet: float = 0
    revenue: int = 0
    payroll: int = 0
    production: float = 0
    wanted: int = 0
    vacancies: int = 0
    dry_months: int = 0
    closed: int = 0


@dataclass
class Policy:
    income_tax: float = 0.12
    benefit: float = 1.8
    training_seats: int = 0
    training_months: int = 4
    training_stipend: float = 5
    bridge_target: float = 0
    bridge_units_per_month: float = 10


def cents(value: float) -> int:
    return int(round(value * 100))


def gini(values):
    values = sorted(values)
    total = sum(values)
    return (2 * sum((i + 1) * x for i, x in enumerate(values)) / (len(values) * total)
            - (len(values) + 1) / len(values)) if total else 0.0


class Economy:
    """One tick = one stylized month, one household = one worker/consumption unit.

    Cash is integer cents; all money moves through transfer(). There is no debt,
    creation of money, aggregate growth equation, or automatic rescue transfer.
    """

    def __init__(self, seed=1, config=None, trace=False):
        self.seed = seed
        self.config = config or Config()
        self.config.validate()
        self.policy = Policy()
        self.month = 0
        self.treasury = cents(self.config.initial_treasury)
        self.bridge_progress = 0.0
        self.bridge_open = False
        self.food_shock = 1.0
        self.trace = trace
        self.ledger = []
        self.events = []
        self.metrics = []
        self.transaction_count = 0
        self.flow = {}
        self.reasons = {}
        self.last_decisions = []
        r = self.rng("init")
        self.households = [Household(i, i % 4, cents(self.config.initial_household_cash * r.uniform(.6, 1.4)),
                                     1.15 if r.random() < (.20 if i % 4 < 2 else .45) else .65)
                           for i in range(self.config.households)]
        owners = r.sample(range(self.config.households), self.config.firms)
        self.firms = []
        for i in range(self.config.firms):
            sector = SECTORS[i % 2]
            # Goods firms cluster east; residences do not move in this prototype.
            district = (i // 2) % 4 if sector == "food" else 2 + (i // 2) % 2
            expected = self.config.households * getattr(self.config, f"{sector}_need") / (self.config.firms / 2)
            self.firms.append(Firm(i, district, sector, owners[i], cents(self.config.initial_firm_cash),
                                   cents(getattr(self.config, f"{sector}_price") * r.uniform(.9, 1.1)),
                                   cents(getattr(self.config, f"{sector}_wage") * r.uniform(.95, 1.05)),
                                   getattr(self.config, f"{sector}_productivity") * r.uniform(.9, 1.1), expected))
        self.initial_money = self.total_money()
        self.check()

    def rng(self, stream, actor=0):
        # Stable per mechanism / time / actor. Policy branches do not shift other streams.
        digest = hashlib.blake2b(f"{self.seed}:{self.month}:{stream}:{actor}".encode(), digest_size=8).digest()
        return random.Random(int.from_bytes(digest, "big"))

    def total_money(self):
        return self.treasury + sum(h.cash for h in self.households) + sum(f.cash for f in self.firms)

    def balance(self, account):
        if account == "government":
            return self.treasury
        return (self.households if account[0] == "h" else self.firms)[int(account[1:])].cash

    def _credit(self, account, amount):
        if account == "government":
            self.treasury += amount
        else:
            (self.households if account[0] == "h" else self.firms)[int(account[1:])].cash += amount

    def transfer(self, source, target, amount, kind):
        if not isinstance(amount, int) or amount < 0 or amount > self.balance(source):
            raise AssertionError(f"Invalid payment: {source}->{target}, {amount}")
        if not amount:
            return
        self._credit(source, -amount)
        self._credit(target, amount)
        self.transaction_count += 1
        self.flow[kind] = self.flow.get(kind, 0) + amount
        if self.trace:
            self.ledger.append({"month": self.month, "source": source, "target": target, "cents": amount, "kind": kind})

    def distance(self, a, b):
        river = (a < 2) != (b < 2)
        return abs(a - b) * .45 + (self.config.river_penalty * (.12 if self.bridge_open else 1) if river else 0)

    def effective_productivity(self, f):
        workers = [h for h in self.households if h.job == f.id]
        skill = sum(h.skill for h in workers) / len(workers) if workers else 1.0
        return f.productivity * (1 + self.config.skill_premium * (skill - .65)) * (self.food_shock if f.sector == "food" else 1)

    def intervene(self, action, value):
        policy_types = {f.name: f.type for f in fields(Policy)}
        if action in policy_types:
            current = getattr(self.policy, action)
            if isinstance(current, int) and not isinstance(value, int):
                raise ValueError(f"{action} requires an integer")
            if not isinstance(value, (float, int)) or not math.isfinite(value) or value < 0:
                raise ValueError(f"Invalid policy value: {action}")
            if action == "income_tax" and value > .8:
                raise ValueError("income_tax must be <= .8")
            if action == "training_months" and value < 1:
                raise ValueError("training_months must be >= 1")
            setattr(self.policy, action, value)
        elif action == "food_shock":
            if not 0 < value <= 2:
                raise ValueError("food_shock must be in (0,2]")
            self.food_shock = value
        else:
            raise ValueError(f"Unknown intervention: {action}")
        self.events.append({"month": self.month, "action": action, "value": value})

    def _train(self):
        p = self.policy
        candidates = [h for h in self.households if h.training or (h.job is None and h.skill < 1)]
        candidates.sort(key=lambda h: (not bool(h.training), h.cash, h.id))
        used = 0
        for h in candidates[:p.training_seats]:
            cost = cents(p.training_stipend)
            if self.treasury < cost:
                self.reasons["training_unfunded"] += 1
                continue
            h.job = None
            self.transfer("government", f"h{h.id}", cost, "training_stipend")
            h.income += cost
            h.training += 1
            used += 1
            if h.training >= p.training_months:
                h.skill = 1.15
                h.training = 0
                self.reasons["graduates"] += 1
        # A slot consumes labor during this month's study, even on graduation.
        self.studying = {h.id for h in candidates[:used]}

    def _plan_firms(self):
        c = self.config
        # All potential entrants see the same last-month observations, regardless of ID order.
        peer_signals = [(f.sector, f.district, f.closed, f.sales, f.unmet) for f in self.firms]
        for f in self.firms:
            if f.closed:
                f.closed -= 1
                if f.closed or not c.enable_entry or self.households[f.owner].cash < cents(45):
                    f.closed = max(1, f.closed)
                    continue
                # Owner compares only nearby firms' last unmet/sales; no economy-wide oracle.
                scores = {}
                for sector in SECTORS:
                    peers = [(sales, unmet) for s, d, closed, sales, unmet in peer_signals
                             if not closed and s == sector and abs(d - f.district) <= 1]
                    scores[sector] = sum(unmet / max(1, sales) for sales, unmet in peers) / max(1, len(peers))
                entry_rng = self.rng("entry", f.id)
                tie_breaks = {s: entry_rng.random() for s in SECTORS}
                f.sector = max(SECTORS, key=lambda s: (scores[s], tie_breaks[s]))
                f.price = cents(getattr(c, f"{f.sector}_price"))
                f.wage = cents(getattr(c, f"{f.sector}_wage"))
                f.productivity = getattr(c, f"{f.sector}_productivity")
                f.expected = 4
                f.dry_months = 0
                self.transfer(f"h{f.owner}", f"f{f.id}", cents(45), "owner_investment")
                self.reasons["entries"] += 1
            spoiled = f.inventory * getattr(c, f"{f.sector}_spoilage")
            f.inventory -= spoiled
            self.spoiled[f.sector] += spoiled
            if self.month:
                if c.enable_price_adaptation:
                    change = c.price_adjustment if f.unmet > max(.2, .05 * f.sales) else (
                        -c.price_adjustment if f.inventory > max(1, f.sales * .6) else 0)
                    f.price = max(10, round(f.price * (1 + change)))
                if f.vacancies and f.revenue > f.payroll:
                    f.wage = round(f.wage * (1 + c.wage_adjustment))
                elif f.inventory > max(2, f.sales):
                    f.wage = max(250, round(f.wage * (1 - c.wage_adjustment)))
                if c.enable_demand_adaptation:
                    f.expected = (1 - c.demand_memory) * f.expected + c.demand_memory * (f.sales + f.unmet)
            productivity = max(.05, self.effective_productivity(f))
            production_target = max(0, f.expected * (1 + c.inventory_buffer) - f.inventory)
            desired = min(c.max_workers, math.ceil(production_target / productivity))
            affordable = f.cash // max(1, f.wage)
            f.wanted = min(desired, affordable)
            if affordable < desired:
                self.reasons["cash_constrained_firms"] += 1
            if self.trace:
                self.last_decisions.append({"month": self.month, "firm": f.id, "sector": f.sector,
                    "expected": f.expected, "stock": f.inventory, "price_cents": f.price,
                    "wage_cents": f.wage, "desired_workers": desired, "affordable_workers": affordable,
                    "posted_jobs": f.wanted})
            f.sales = f.unmet = f.production = 0
            f.revenue = f.payroll = 0

    def _labor(self):
        c = self.config
        for f in self.firms:
            workers = [h for h in self.households if h.job == f.id]
            workers.sort(key=lambda h: (-h.skill, h.id))
            for h in workers:
                if f.closed or self.distance(h.district, f.district) > c.max_commute or h.id in self.studying:
                    h.job = None
            keep = [h for h in workers if h.job == f.id]
            for h in keep[f.wanted:]:
                h.job = None
        counts = {f.id: sum(h.job == f.id for h in self.households) for f in self.firms}
        searchers = list(self.households)
        self.rng("labor_order").shuffle(searchers)
        for h in searchers:
            if h.id in self.studying:
                continue
            if h.job is not None and self.rng("search", h.id).random() >= c.job_search_rate:
                continue
            options = [f for f in self.firms if not f.closed and counts[f.id] < f.wanted]
            qualified = [f for f in options if f.sector == "food" or h.skill >= 1.0]
            reachable = [f for f in qualified if self.distance(h.district, f.district) <= c.max_commute]
            if h.job is None:
                if options and not qualified:
                    self.reasons["skill_blocked"] += 1
                elif qualified and not reachable:
                    self.reasons["commute_blocked"] += 1
            def score(f):
                return f.wage * (1 - self.policy.income_tax) - cents(c.commute_disutility * self.distance(h.district, f.district))
            if not reachable:
                continue
            choice = max(reachable, key=lambda f: (score(f), -f.id))
            previous = self.firms[h.job] if h.job is not None else None
            if score(choice) > (score(previous) * 1.03 if previous else 200):
                if previous:
                    counts[previous.id] -= 1
                h.job = choice.id
                counts[choice.id] += 1
        for f in self.firms:
            f.vacancies = max(0, f.wanted - counts[f.id]) if not f.closed else 0
        for h in self.households:
            if h.job is not None:
                f = self.firms[h.job]
                tax = int(f.wage * self.policy.income_tax)
                self.transfer(f"f{f.id}", f"h{h.id}", f.wage - tax, "net_wages")
                self.transfer(f"f{f.id}", "government", tax, "income_tax")
                h.income += f.wage - tax
                f.payroll += f.wage
        for f in self.firms:
            if not f.closed:
                f.production = counts[f.id] * self.effective_productivity(f)
                f.inventory += f.production

    def _benefits(self):
        eligible = [h for h in self.households if h.job is None and h.id not in self.studying]
        requested = cents(self.policy.benefit) * len(eligible)
        ratio = min(1, self.treasury / requested) if requested else 1
        self.reasons["benefit_funding_ratio"] = ratio
        for h in eligible:
            amount = int(cents(self.policy.benefit) * ratio)
            self.transfer("government", f"h{h.id}", amount, "benefit")
            h.income += amount

    def _buy(self, account, sector, quantity, budget, district, stream):
        c = self.config
        sellers = [f for f in self.firms if not f.closed and f.sector == sector
                   and self.distance(district, f.district) <= c.max_shopping]
        # Bounded information: sample at most three sellers before price comparison.
        if not c.search_all_sellers and len(sellers) > 3:
            sellers = self.rng("sellers_" + stream, account).sample(sellers, 3)
        sellers.sort(key=lambda f: (f.price + cents(c.shopping_disutility * self.distance(district, f.district)), f.id))
        available_budget = min(budget, self.balance(account))
        bought = spent = 0
        for f in sellers:
            qty = min(quantity - bought, f.inventory, available_budget / f.price)
            payment = min(available_budget, max(0, int(qty * f.price + 1e-8)))
            actual = payment / f.price
            if payment:
                self.transfer(account, f"f{f.id}", payment, "public_procurement" if account == "government" else sector + "_purchase")
                f.inventory -= actual
                f.sales += actual
                f.revenue += payment
                bought += actual
                spent += payment
                available_budget -= payment
        remaining = quantity - bought
        affordable_sellers = [f for f in sellers if available_budget >= f.price * .01]
        # Count only final unfilled affordable orders (not failed visits later filled elsewhere).
        if remaining > .01 and affordable_sellers:
            first = affordable_sellers[0]
            first.unmet += min(remaining, available_budget / first.price)
        if remaining > .01 and account != "government":
            if not sellers:
                reason = sector + "_no_access"
            elif available_budget < min(f.price for f in sellers) * remaining:
                reason = sector + "_budget_shortfall"
            else:
                reason = sector + "_stock_shortfall"
            self.reasons[reason] = self.reasons.get(reason, 0) + 1
        return bought, spent

    def _public_project(self):
        if self.policy.bridge_target > self.bridge_progress and not self.bridge_open:
            need = min(self.policy.bridge_units_per_month, self.policy.bridge_target - self.bridge_progress)
            units, _ = self._buy("government", "goods", need, self.treasury, 1, "bridge")
            self.public_units += units
            self.bridge_progress += units
            if self.bridge_progress > 0 and self.bridge_progress >= self.policy.bridge_target - .02:
                self.bridge_open = True
                self.events.append({"month": self.month, "action": "bridge_completed", "value": self.bridge_progress})

    def step(self):
        opening_stocks = {s: sum(f.inventory for f in self.firms if f.sector == s) for s in SECTORS}
        self.spoiled = dict.fromkeys(SECTORS, 0.0)
        scrapped = dict.fromkeys(SECTORS, 0.0)
        self.public_units = 0.0
        self.flow = {}
        self.last_decisions = []
        keys = ("graduates", "entries", "closures", "cash_constrained_firms", "skill_blocked", "commute_blocked",
                "training_unfunded", "food_no_access", "goods_no_access", "food_budget_shortfall",
                "goods_budget_shortfall", "food_stock_shortfall", "goods_stock_shortfall")
        self.reasons = dict.fromkeys(keys, 0)
        for h in self.households:
            h.income = 0
            h.food = h.goods = 0
        for f in self.firms:
            f.production = 0
        self._train()
        self._plan_firms()
        self._labor()
        self._benefits()
        # Alternate public/private priority to expose and reduce fixed order artifacts.
        if self.month % 2 == 0:
            self._public_project()
        buyers = list(self.households)
        if self.config.shuffle_buyers:
            self.rng("buyer_order").shuffle(buyers)
        for h in buyers:
            budget = min(h.cash, int(self.config.income_spend * h.income + self.config.wealth_spend * h.cash))
            h.food, spent = self._buy(f"h{h.id}", "food", self.config.food_need, budget, h.district, "food")
            h.goods, _ = self._buy(f"h{h.id}", "goods", self.config.goods_need, budget - spent, h.district, "goods")
        if self.month % 2:
            self._public_project()
        for f in self.firms:
            if f.closed:
                continue
            reserve = int(max(f.wage, f.payroll) * self.config.reserve_payrolls)
            dividend = int(max(0, f.cash - reserve) * self.config.dividend_fraction)
            self.transfer(f"f{f.id}", f"h{f.owner}", dividend, "dividend")
            f.dry_months = f.dry_months + 1 if f.cash < 250 and f.sales < .1 else 0
            if f.dry_months >= 3:
                self.transfer(f"f{f.id}", f"h{f.owner}", f.cash, "liquidation")
                scrapped[f.sector] += f.inventory
                f.inventory = 0
                f.closed = 4
                f.wanted = f.vacancies = 0
                for h in self.households:
                    if h.job == f.id:
                        h.job = None
                self.reasons["closures"] += 1
        self.check()
        result = self.measure()
        for s in SECTORS:
            produced = sum(f.production for f in self.firms if f.sector == s)
            closing = sum(f.inventory for f in self.firms if f.sector == s)
            consumed = sum(getattr(h, s) for h in self.households)
            public = self.public_units if s == "goods" else 0.0
            error = opening_stocks[s] + produced - self.spoiled[s] - scrapped[s] - consumed - public - closing
            assert abs(error) < 1e-6, f"Physical conservation failed: {s} {error}"
            result[f"physical_error_{s}"] = error
            result[f"spoiled_{s}"] = self.spoiled[s]
        result["public_goods_units"] = self.public_units
        self.metrics.append(result)
        self.month += 1
        return result

    def measure(self):
        c = self.config
        active = [f for f in self.firms if not f.closed]
        prices = {s: sum(f.price for f in active if f.sector == s) / max(1, sum(f.sector == s for f in active)) / 100
                  for s in SECTORS}
        food = sum(h.food for h in self.households)
        goods = sum(h.goods for h in self.households)
        employed = [h for h in self.households if h.job is not None]
        row = {"month": self.month, "employment": len(employed) / c.households,
               "food_coverage": food / (c.households * c.food_need),
               "goods_coverage": goods / (c.households * c.goods_need),
               "real_consumption": food * c.food_price + goods * c.goods_price,
               "production_at_base_prices": sum(f.production * getattr(c, f"{f.sector}_price") for f in self.firms),
               "food_price": prices["food"], "goods_price": prices["goods"],
               "basket_price": prices["food"] * c.food_need + prices["goods"] * c.goods_need,
               "mean_wage": sum(self.firms[h.job].wage for h in employed) / max(1, len(employed)) / 100,
               "treasury": self.treasury / 100, "household_cash_gini": gini([h.cash for h in self.households]),
               "skilled_share": sum(h.skill >= 1 for h in self.households) / c.households,
               "bridge_progress": self.bridge_progress, "bridge_open": int(self.bridge_open),
               "food_firms": sum(f.sector == "food" for f in active), "goods_firms": sum(f.sector == "goods" for f in active),
               "vacancies": sum(f.vacancies for f in active), "money_error_cents": self.total_money() - self.initial_money,
               "private_trade": (self.flow.get("food_purchase", 0) + self.flow.get("goods_purchase", 0)) / 100}
        for d in range(4):
            group = [h for h in self.households if h.district == d]
            row[f"employment_d{d}"] = sum(h.job is not None for h in group) / len(group)
            row[f"food_d{d}"] = sum(h.food for h in group) / (len(group) * c.food_need)
        row.update(self.reasons)
        row.update({"flow_" + k: self.flow.get(k, 0) / 100 for k in (
            "net_wages", "income_tax", "benefit", "training_stipend", "public_procurement", "dividend")})
        return row

    def check(self):
        assert self.total_money() == self.initial_money, "Cash conservation failed"
        assert self.treasury >= 0
        for h in self.households:
            assert isinstance(h.cash, int) and h.cash >= 0
            if h.job is not None:
                f = self.firms[h.job]
                assert not f.closed
                assert f.sector == "food" or h.skill >= 1
        for f in self.firms:
            assert isinstance(f.cash, int) and f.cash >= 0
            assert math.isfinite(f.inventory) and f.inventory >= -1e-7
            assert f.price > 0 and f.wage > 0
            assert sum(h.job == f.id for h in self.households) <= f.wanted

    def snapshot(self):
        return {"schema": 1, "seed": self.seed, "config": asdict(self.config), "policy": asdict(self.policy),
                "month": self.month, "treasury": self.treasury, "bridge_progress": self.bridge_progress,
                "bridge_open": self.bridge_open, "food_shock": self.food_shock, "initial_money": self.initial_money,
                "households": [asdict(h) for h in self.households], "firms": [asdict(f) for f in self.firms],
                "events": self.events, "metrics": self.metrics, "transaction_count": self.transaction_count}

    @classmethod
    def restore(cls, snapshot, trace=False):
        snapshot = copy.deepcopy(snapshot)
        if snapshot.get("schema") != 1:
            raise ValueError("Unsupported snapshot schema")
        model = cls(snapshot["seed"], Config(**snapshot["config"]), trace)
        model.policy = Policy(**snapshot["policy"])
        for key in ("month", "treasury", "bridge_progress", "bridge_open", "food_shock", "initial_money",
                    "events", "metrics", "transaction_count"):
            setattr(model, key, snapshot[key])
        model.households = [Household(**h) for h in snapshot["households"]]
        model.firms = [Firm(**f) for f in snapshot["firms"]]
        model.check()
        return model

    def save(self, path):
        Path(path).write_text(json.dumps(self.snapshot(), ensure_ascii=False, indent=2), encoding="utf-8")
