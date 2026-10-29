from __future__ import annotations

import copy
import hashlib
import heapq
import math
import random
from dataclasses import asdict, dataclass

GOODS = ("food", "goods")


@dataclass
class Settings:
    households: int = 96
    channel: str = "mixed"  # direct, retail, mixed
    residential: str = "clustered"
    industry: str = "far"
    zoning: str = "flexible"  # small_only, large_edge, large_central, flexible
    customer_edge_hours: float = .18
    truck_edge_hours: float = .045
    shopping_hours: float = 4.0
    time_value: float = 100.0  # cents per hour; heterogeneous by household
    chain_trips: bool = True
    congestion: float = .12
    corridor: bool = False
    freight_multiplier: float = 1.0
    truck_load: int = 32
    spoilage: bool = True
    entry: bool = True
    factory_prices_adapt: bool = False
    rent_multiplier: float = 1.0
    initial_capital: int = 180000
    service_multiplier: float = 1.0
    small_multigood: bool = False

    def validate(self):
        if self.channel not in ("direct", "retail", "mixed"):
            raise ValueError("channel must be direct/retail/mixed")
        if self.residential not in ("clustered", "dispersed") or self.industry not in ("far", "near"):
            raise ValueError("Unknown city layout")
        if self.zoning not in ("small_only", "large_edge", "large_central", "flexible", "matched_edge"):
            raise ValueError("Unknown zoning")
        if not isinstance(self.households, int) or self.households < 8:
            raise ValueError("households must be integer >= 8")
        if not isinstance(self.truck_load, int) or self.truck_load < 1:
            raise ValueError("truck_load must be a positive integer")
        for key in ("customer_edge_hours", "truck_edge_hours", "shopping_hours", "freight_multiplier", "rent_multiplier", "service_multiplier"):
            if not math.isfinite(getattr(self, key)) or getattr(self, key) <= 0:
                raise ValueError(f"{key} must be positive and finite")
        if self.time_value < 0 or self.congestion < 0 or self.initial_capital < 0:
            raise ValueError("Invalid cost or capital")


@dataclass
class Resident:
    id: int
    node: int
    value_time: float
    work: float = 0
    shopping: float = 0
    food: int = 0
    goods: int = 0


@dataclass
class Producer:
    id: int
    node: int
    good: str
    price: int
    stock: int = 0
    expected: float = 96
    sold: int = 0
    unmet: int = 0
    production: int = 0
    service: float = 0
    direct_sold: int = 0


@dataclass
class Store:
    id: int
    node: int
    area: int
    assortment: list[str]
    prices: dict
    stock: dict
    expected: dict
    unit_cost: dict
    age: int = 0
    sales: int = 0
    customers: int = 0
    service: float = 0
    losses: int = 0
    cash_failures: int = 0
    last_profit: int = 0
    active: bool = True
    freight_paid: int = 0

    @property
    def capacity(self):
        return 48 * self.area


class City:
    """One period: abstract month; four units desired per resident (2 of each).

    Controlled distribution module: equal household equity, pooled paid labor,
    no credit, migration, taste bonus for stores, or retail wholesale discount.
    """

    width, height = 7, 5

    def observe(self, event, **details):
        """Optional observation hook. The baseline does not change any decisions."""
        pass

    def __init__(self, seed=1, settings=None, trace=False):
        self.seed, self.settings, self.trace = seed, settings or Settings(), trace
        self.settings.validate()
        self.month = 0
        self.accounts = {"property": 0, "fund": self.settings.initial_capital}
        self.residents = []
        r = self.rng("residents")
        centers = [(1, 1), (1, 3), (2, 2)]
        for i in range(self.settings.households):
            if self.settings.residential == "clustered":
                x, y = centers[r.randrange(len(centers))]
                if r.random() < .25:
                    x = max(0, min(3, x + r.choice([-1, 1])))
            else:
                x, y = r.randrange(5), r.randrange(5)
            self.residents.append(Resident(i, self.node(x, y), self.settings.time_value * r.uniform(.65, 1.35)))
            self.accounts[f"h{i}"] = 4000
        layout = [(6, 0), (6, 4), (5, 0), (5, 4)] if self.settings.industry == "far" else [(2, 1), (2, 3), (4, 1), (4, 3)]
        self.producers = [Producer(i, self.node(*xy), GOODS[i % 2], 100 if i % 2 == 0 else 140,
                                   expected=self.settings.households) for i, xy in enumerate(layout)]
        for p in self.producers:
            self.accounts[f"p{p.id}"] = 20000
        self.stores = []
        self.initial_money = sum(self.accounts.values())
        self.metrics, self.events, self.ledger, self.transactions = [], [], [], 0
        self.previous_traffic = {}
        self.paths = {}
        self.observations = []
        self.reset_period()

    @classmethod
    def node(cls, x, y):
        return y * cls.width + x

    @classmethod
    def xy(cls, node):
        return node % cls.width, node // cls.width

    @classmethod
    def manhattan(cls, a, b):
        x, y = cls.xy(a)
        u, v = cls.xy(b)
        return abs(x - u) + abs(y - v)

    def rng(self, stream, actor=0):
        key = hashlib.blake2b(f"{self.seed}:{self.month}:{stream}:{actor}".encode(), digest_size=8).digest()
        return random.Random(int.from_bytes(key, "big"))

    def reset_period(self):
        self.flow = {}
        self.traffic = {}
        self.paths = {}
        self.labor = {"manufacturing": 0., "factory_service": 0., "retail_staff": 0., "freight": 0.}
        self.direct_units = self.retail_units = self.truck_trips = self.customer_visits = 0
        self.wasted_visits = self.lost_service = self.stock_shortfall = self.budget_shortfall = 0
        self.produced = dict.fromkeys(GOODS, 0)
        self.wasted = dict.fromkeys(GOODS, 0)
        self.consumer_spend = self.freight_spend = 0
        self.operating_profit = 0
        self.employee_order = list(range(len(self.residents)))
        self.rng("workers").shuffle(self.employee_order)
        self.worker_cursor = 0
        self.sales_by_location = {}
        self.opening_accounts = self.accounts.copy()
        for h in self.residents:
            h.work = h.shopping = 0
            h.food = h.goods = 0

    def pay(self, source, target, amount, purpose):
        assert isinstance(amount, int) and 0 <= amount <= self.accounts[source], (source, target, amount)
        if not amount:
            return
        self.accounts[source] -= amount
        self.accounts[target] = self.accounts.get(target, 0) + amount
        self.transactions += 1
        self.flow[purpose] = self.flow.get(purpose, 0) + amount
        if self.trace:
            self.ledger.append({"month": self.month, "source": source, "target": target, "cents": amount, "purpose": purpose})

    def hire(self, account, hours, purpose):
        """Same $1/hour wage and same finite labor pool for every channel."""
        wanted = min(round(hours * 100), self.accounts[account])
        remaining = wanted
        visited = 0
        while remaining and visited < len(self.residents):
            h = self.residents[self.employee_order[self.worker_cursor]]
            available = max(0, round((8 - h.work) * 100))
            amount = min(remaining, available)
            self.pay(account, f"h{h.id}", amount, purpose)
            h.work += amount / 100
            remaining -= amount
            if amount == available:
                self.worker_cursor = (self.worker_cursor + 1) % len(self.residents)
                visited += 1
        actual = (wanted - remaining) / 100
        self.labor[purpose] += actual
        return actual

    def route(self, start, end, mode="customer"):
        key = start, end, mode
        if key in self.paths:
            return self.paths[key]
        base = self.settings.customer_edge_hours if mode == "customer" else self.settings.truck_edge_hours
        queue, best, parents = [(0., start)], {start: 0.}, {}
        while queue:
            cost, current = heapq.heappop(queue)
            if cost != best[current]:
                continue
            if current == end:
                break
            x, y = self.xy(current)
            for u, v in ((x-1,y), (x+1,y), (x,y-1), (x,y+1)):
                if not (0 <= u < self.width and 0 <= v < self.height):
                    continue
                neighbor = self.node(u, v)
                edge = tuple(sorted((current, neighbor)))
                multiplier = 1 + self.settings.congestion * self.previous_traffic.get(edge, 0) / 12
                if mode == "truck" and self.settings.corridor and y == v == 2:
                    multiplier *= .35
                duration = base * multiplier
                if cost + duration < best.get(neighbor, math.inf) - 1e-12:
                    best[neighbor] = cost + duration
                    parents[neighbor] = (current, edge)
                    heapq.heappush(queue, (cost + duration, neighbor))
        edges, current = [], end
        while current != start:
            current, edge = parents[current]
            edges.append(edge)
        answer = best[end], tuple(reversed(edges))
        self.paths[key] = answer
        return answer

    def trip(self, start, end, mode="customer", count=1):
        duration, path = self.route(start, end, mode)
        for edge in path:
            self.traffic[edge] = self.traffic.get(edge, 0) + count
        return duration * count

    def rent(self, node, area):
        nearby = sum(self.manhattan(h.node, node) <= 1 for h in self.residents)
        return round((160 + nearby * 1.8) * area * self.settings.rent_multiplier)

    def staff_hours(self, area):
        return (2 + 3 * area) * self.settings.service_multiplier

    def legal_areas(self, node):
        zone = self.settings.zoning
        if zone == "small_only":
            return [1]
        x, y = self.xy(node)
        large = (zone == "flexible" or (zone == "large_edge" and x >= 4)
                 or (zone == "large_central" and 1 <= x <= 2 and 1 <= y <= 3)
                 or (zone == "matched_edge" and 5 <= x <= 6 and 1 <= y <= 3))
        return [1, 4] if large else [1]

    def offers(self, include_empty=True):
        result = []
        if self.settings.channel != "retail":
            for p in self.producers:
                if include_empty or p.stock:
                    result.append((f"p{p.id}", p.node, {p.good: p.price}))
        if self.settings.channel != "direct":
            for s in self.stores:
                if s.active and (include_empty or sum(s.stock.values())):
                    result.append((f"r{s.id}", s.node, s.prices))
        return result

    def procurement_quotes(self, good, node, quantity):
        """Joint price/freight quotes for one batch, without reserving supply.

        Retail uses the same ordering before clipping orders to stock and cash.
        Stock and hiring feasibility remain separate from the posted quote.
        """
        quantity = max(1, math.ceil(quantity))
        quotes = []
        for p in self.producers:
            if p.good != good or not math.isfinite(self.route(p.node,node,'truck')[0]):
                continue
            freight, hours, trips = self.delivery_cost(p.node,node,quantity)
            total = p.price*quantity+freight
            quotes.append({'supplier':p.id, 'source':p.node, 'base_price':p.price,
                           'batch_quantity':quantity, 'batch_goods_cost':p.price*quantity,
                           'freight':freight, 'freight_hours':hours, 'trips':trips,
                           'landed_cost':total, 'landed_unit_cost':total/quantity,
                           'supplier_stock':p.stock})
        return sorted(quotes,key=lambda q:q['landed_unit_cost'])

    def forecast_procurement(self, node, demand, prices):
        """Expected sales cost with a coherent, integer-batch transport quote.

        Expected sales may be fractional. Goods expense is expected units times
        the selected supplier's price; freight charges the whole rounded batch.
        This conservatively expenses all batch freight, without predicting stock,
        production, cash competition or transport hiring before they occur.
        """
        products = []
        revenue = purchases = freight = 0.
        for good, quantity in demand.items():
            quote = self.procurement_quotes(good,node,quantity)[0]
            shipping = quote['freight'] if quantity > 0 else 0
            goods_cost = quantity*quote['base_price']
            revenue += quantity*prices[good]
            purchases += goods_cost
            freight += shipping
            products.append({**quote, 'good':good, 'quantity':quantity, 'price':prices[good],
                             'expected_goods_cost':goods_cost, 'freight':shipping,
                             'freight_hours':quote['freight_hours'] if quantity > 0 else 0,
                             'trips':quote['trips'] if quantity > 0 else 0,
                             'batch_quantity':quote['batch_quantity'] if quantity > 0 else 0,
                             'batch_goods_cost':quote['batch_goods_cost'] if quantity > 0 else 0,
                             'landed_cost':quote['landed_cost'] if quantity > 0 else 0,
                             'quote_rule':'joint_price_freight', 'supply_guaranteed':False})
        return {'revenue':revenue, 'purchases':purchases, 'freight':freight, 'products':products}

    def estimate_site(self, node, area, assortment):
        """Myopic entrant forecast: known residents/posted prices, not future sales.

        Ignores linked multi-stop tours and stockouts, so entrants can be wrong.
        There is no attraction from neighboring stores or imposed density bonus.
        """
        overhead = self.rent(node, area) + round(self.staff_hours(area) * 100) + 100
        capacity = 48 * area
        planned = capacity * .7
        prices = {g: round(min(p.price for p in self.producers if p.good == g) + 8 + overhead / planned + 10) for g in assortment}
        demand = dict.fromkeys(assortment, 0.)
        current = self.offers()
        for h in self.residents:
            duration = self.route(h.node, node)[0] * 2 + .06
            if duration > self.settings.shopping_hours:
                continue
            for g in assortment:
                cost = prices[g] + h.value_time * duration / (2 * len(assortment))
                alternatives = [pr[g] + h.value_time * (2 * self.route(h.node, n)[0] + .06) / (2 * len(pr))
                                for _, n, pr in current if g in pr]
                if alternatives:
                    share = max(0., min(1., .5 + (min(alternatives) - cost) / 100))
                else:
                    share = 1.
                demand[g] += 2 * share
        total = sum(demand.values())
        scale = min(1., capacity / max(1., total))
        demand = {g: q * scale for g, q in demand.items()}
        procurement = self.forecast_procurement(node,demand,prices)
        return procurement['revenue']-procurement['purchases']-overhead-procurement['freight'], demand, prices

    def site_candidates(self):
        """Legacy commerce baseline; spatial experiments may override scheduling."""
        occupied = {s.node for s in self.stores if s.active}
        nodes = [n for n in range(self.width * self.height) if n not in occupied]
        return self.rng("sites").sample(nodes, min(8, len(nodes)))

    def enter(self):
        if not self.settings.entry or self.settings.channel == "direct" or len([s for s in self.stores if s.active]) >= 18:
            return
        candidates = self.site_candidates()
        proposals = []
        for node in candidates:
            for area in self.legal_areas(node):
                assortments = [[g] for g in GOODS] if area == 1 and not self.settings.small_multigood else [list(GOODS)]
                for assortment in assortments:
                    profit, demand, prices = self.estimate_site(node, area, assortment)
                    working = round(48 * area * 130 + 2 * (self.rent(node, area) + self.staff_hours(area)*100))
                    fee = 800 + 600 * area
                    self.observe('site_estimate', node=node, area=area, assortment=assortment,
                                 profit=profit, demand=demand, prices=prices, capital=working+fee,
                                 affordable=self.accounts['fund'] >= working+fee)
                    if profit > 0 and self.accounts["fund"] >= working + fee:
                        proposals.append((profit / (working + fee), node, area, assortment, demand, prices, working, fee, profit))
        if not proposals:
            return
        _, node, area, assortment, demand, prices, working, fee, forecast = max(proposals, key=lambda x: x[0])
        i = len(self.stores)
        self.accounts[f"r{i}"] = 0
        self.pay("fund", f"r{i}", working, "equity_investment")
        self.pay("fund", "property", fee, "lease_setup_fee")
        cost = {q['good']:q['landed_unit_cost'] for q in self.forecast_procurement(node,demand,prices)['products']}
        self.stores.append(Store(i, node, area, assortment, prices, dict.fromkeys(assortment, 0), demand, cost))
        self.events.append({"month": self.month, "event": "entry", "store": i, "node": node, "area": area,
                            "forecast_profit": forecast, "capital": working, "fee": fee})

    def decay(self, quantity, good, actor):
        if not self.settings.spoilage:
            return 0
        expected = quantity * (.12 if good == "food" else .015)
        loss = math.floor(expected) + (self.rng("spoil_" + good, actor).random() < expected % 1)
        self.wasted[good] += loss
        return loss

    def production(self):
        for p in self.producers:
            p.stock -= self.decay(p.stock, p.good, f"p{p.id}")
            if self.month:
                p.expected = .6 * p.expected + .4 * (p.sold + p.unmet)
                if self.settings.factory_prices_adapt:
                    delta = .01 if p.unmet > 3 else (-.01 if p.stock > p.sold else 0)
                    p.price = max(40, round(p.price * (1 + delta)))
            target = min(160, max(0, math.ceil(p.expected * 1.15 - p.stock)))
            actual_hours = self.hire(f"p{p.id}", target * .25, "manufacturing")
            p.production = int(actual_hours / .25 + 1e-8)
            p.stock += p.production
            self.produced[p.good] += p.production
            p.sold = p.unmet = 0
            # A factory is allowed to staff its counter for all offered stock;
            # it is not artificially restricted to make intermediaries necessary.
            counter_units = p.stock if self.month == 0 else max(12, p.direct_sold*1.2)
            counter_hours = (1 + counter_units*.05) if self.settings.channel != "retail" else 0
            p.service = self.hire(f"p{p.id}", counter_hours * self.settings.service_multiplier, "factory_service")
            p.direct_sold = 0

    def delivery_cost(self, source, target, units):
        if units <= 0:
            return 0, 0., 0
        trips = math.ceil(units / self.settings.truck_load)
        hours = (2 * self.route(source, target, "truck")[0] * trips + .15 * trips + .012 * units) * self.settings.freight_multiplier
        return round(hours * 100) + trips * 60, hours, trips

    def operate_stores(self):
        stores = [s for s in self.stores if s.active]
        self.rng("restock_order").shuffle(stores)
        for s in stores:
            s.age += 1
            s.sales = s.customers = s.freight_paid = 0
            account = f"r{s.id}"
            s.opening_cash = self.accounts[account]
            overhead = self.rent(s.node, s.area) + 100
            if self.accounts[account] < overhead + round(self.staff_hours(s.area) * 100):
                s.cash_failures += 1
                s.service = 0
                if s.cash_failures >= 2:
                    self.close(s, "cash")
                continue
            s.cash_failures = 0
            self.pay(account, "property", overhead, "rent_utilities")
            s.service = self.hire(account, self.staff_hours(s.area), "retail_staff")
            for good in s.assortment:
                s.stock[good] -= self.decay(s.stock[good], good, account)
            volume = max(4, sum(s.expected.values()))
            fixed_per_unit = (overhead + self.staff_hours(s.area)*100) / volume
            for good in s.assortment:
                target_price = round(s.unit_cost[good] + fixed_per_unit + 10)
                # Gradual cost recovery, not an exogenous price advantage.
                s.prices[good] = max(10, round(.8 * s.prices[good] + .2 * target_price))
                target_stock = math.ceil(s.expected[good] * 1.1)
                space = s.capacity // len(s.assortment) - s.stock[good]
                needed = max(0, min(space, target_stock - s.stock[good]))
                suppliers = [self.producers[q['supplier']] for q in self.procurement_quotes(good,s.node,needed)]
                ordered = needed
                for p in suppliers:
                    quantity = min(needed, p.stock)
                    while quantity > 0:
                        delivery, hours, trips = self.delivery_cost(p.node, s.node, quantity)
                        if p.price * quantity + delivery <= self.accounts[account]:
                            break
                        quantity -= 1
                    if quantity <= 0:
                        continue
                    # Reserve freight payment first; if labor cannot be bought the shipment is not moved.
                    hired = self.hire(account, hours, "freight")
                    if hired + .011 < hours:
                        self.lost_service += 1
                        continue
                    self.pay(account, "property", trips * 60, "truck_lease")
                    cost = quantity * p.price
                    self.pay(account, f"p{p.id}", cost, "wholesale")
                    old_value = s.stock[good] * s.unit_cost[good]
                    s.unit_cost[good] = (old_value + cost + delivery) / (s.stock[good] + quantity)
                    s.stock[good] += quantity
                    p.stock -= quantity
                    p.sold += quantity
                    self.trip(p.node, s.node, "truck", 2 * trips)
                    self.truck_trips += trips
                    s.freight_paid += delivery
                    self.freight_spend += delivery
                    self.observe('shipment', store=s.id, supplier=p.id, good=good, quantity=quantity,
                                 wholesale_cents=cost, freight_cents=delivery, hours=hired, trips=trips,
                                 source=p.node, destination=s.node,
                                 path=self.route(p.node, s.node, 'truck')[1])
                    needed -= quantity
                # Unfinanced intentions are not upstream orders.
                if needed and suppliers and self.accounts[account] >= suppliers[0].price:
                    suppliers[0].unmet += min(needed, self.accounts[account] // suppliers[0].price)
                s.expected[good] = max(2., s.expected[good])

    def seller_stock(self, account, good):
        if account.startswith("p"):
            p = self.producers[int(account[1:])]
            return p.stock if p.good == good else 0
        return self.stores[int(account[1:])].stock.get(good, 0)

    def seller_service(self, account):
        seller = self.producers[int(account[1:])] if account[0] == 'p' else self.stores[int(account[1:])]
        return seller.service

    def stock_total(self, good):
        return sum(p.stock for p in self.producers if p.good == good) + sum(s.stock.get(good, 0) for s in self.stores)

    def itinerary(self, home, nodes):
        if len(nodes) == 2 and self.settings.chain_trips:
            a, b = nodes
            route = [(home, a), (a, b), (b, home)]
        else:
            route = [(a, b) for node in nodes for a, b in ((home, node), (node, home))]
        return sum(self.route(a, b)[0] for a, b in route) + len(nodes)*.06, route

    def consume(self):
        offers = self.offers()
        households = list(self.residents)
        self.rng("buyers").shuffle(households)
        for h in households:
            remaining = dict.fromkeys(GOODS, 2)
            cash = self.accounts[f"h{h.id}"]
            # At most three local alternatives per product; no global optimal-tour solver.
            choices = {}
            for good in GOODS:
                candidates = [o for o in offers if good in o[2] and self.seller_stock(o[0], good)
                              and self.seller_service(o[0]) >= .08-1e-8]
                candidates.sort(key=lambda o: 2*o[2][good] + h.value_time*(2*self.route(h.node,o[1])[0]+.06)/len(o[2]))
                choices[good] = candidates[:3] + [None]
            plans = []
            for a in choices["food"]:
                for b in choices["goods"]:
                    selected = [o for o in (a, b) if o is not None]
                    unique = list({o[0]: o for o in selected}.values())
                    if not unique:
                        continue
                    duration, path = self.itinerary(h.node, [o[1] for o in unique])
                    if duration > self.settings.shopping_hours:
                        continue
                    budget, utility, orders = cash, -h.value_time*duration, []
                    for good, offer in zip(GOODS, (a,b)):
                        if offer:
                            price = offer[2][good]
                            quantity = min(2, self.seller_stock(offer[0],good), budget//price)
                            budget -= quantity*price
                            utility += quantity*((650 if good == "food" else 500)-price)
                            orders.append((good,offer,quantity))
                    service_needed = {}
                    for _, offer, quantity in orders:
                        if quantity:
                            service_needed[offer[0]] = service_needed.get(offer[0], .06) + .02*quantity
                    feasible = all(self.seller_service(account)+1e-8 >= service for account,service in service_needed.items())
                    if feasible and sum(q for _,_,q in orders):
                        plans.append((utility, duration, path, orders))
            if not plans:
                self.observe('shopping_blocked', resident=h.id, cash=cash,
                             available={g:len([o for o in choices[g] if o]) for g in GOODS})
                if cash < 100:
                    self.budget_shortfall += 1
                continue
            utility, duration, path, orders = max(plans, key=lambda x: x[0])
            self.observe('shopping_plan', resident=h.id, utility=utility, duration=duration, path=path,
                         orders=[{'good':g,'seller':o[0],'node':o[1],'quantity':q,'price_cents':o[2][g]}
                                 for g,o,q in orders],
                         alternatives=[{'utility':u,'duration':d,'sellers':list(dict.fromkeys(o[0] for _,o,q in orders_ if q))}
                                       for u,d,_,orders_ in sorted(plans,key=lambda p:-p[0])[:3]])
            if utility <= 0:
                continue
            h.shopping += duration
            for a,b in path:
                self.trip(a,b)
            visited = set()
            for good, offer, desired in orders:
                account, node, prices = offer
                seller = self.producers[int(account[1:])] if account[0] == "p" else self.stores[int(account[1:])]
                service = (0.06 if account not in visited else 0) + .02*desired
                if seller.service + 1e-8 < service:
                    self.lost_service += 1
                    continue
                quantity = min(desired, self.seller_stock(account,good))
                if quantity:
                    seller.service -= service
                    amount = quantity*prices[good]
                    self.pay(f"h{h.id}", account, amount, "direct_sale" if account[0]=="p" else "retail_sale")
                    self.consumer_spend += amount
                    self.observe('purchase', resident=h.id, seller=account, good=good, quantity=quantity,
                                 price_cents=prices[good], cents=amount, node=node)
                    setattr(h,good,getattr(h,good)+quantity)
                    remaining[good] -= quantity
                    self.sales_by_location[node] = self.sales_by_location.get(node,0)+quantity
                    if account[0] == "p":
                        seller.stock -= quantity
                        seller.sold += quantity
                        seller.direct_sold += quantity
                        self.direct_units += quantity
                    else:
                        seller.stock[good] -= quantity
                        seller.period_sales[good] += quantity
                        seller.sales += quantity
                        self.retail_units += quantity
                    if account not in visited:
                        self.customer_visits += 1
                        if account[0] == "r":
                            seller.customers += 1
                        visited.add(account)
            if not visited:
                self.wasted_visits += 1
            if any(remaining.values()) and cash < 300:
                self.budget_shortfall += 1
            for good in GOODS:
                if remaining[good] and not choices[good][:-1]:
                    self.stock_shortfall += 1

    def close(self, store, reason):
        writeoff = round(sum(q*store.unit_cost[g] for g,q in store.stock.items()))
        self.operating_profit -= writeoff
        store.last_profit -= writeoff
        for good, stock in store.stock.items():
            self.wasted[good] += stock
            store.stock[good] = 0
        store.active = False
        self.pay(f"r{store.id}", "fund", self.accounts[f"r{store.id}"], "liquidation")
        self.events.append({"month":self.month,"event":"exit","store":store.id,"node":store.node,
                            "area":store.area,"age":store.age,"reason":reason})

    def settle(self):
        for s in self.stores:
            if not s.active:
                continue
            # Change in cash + inventory at cost = operating profit (before equity withdrawals).
            closing_asset = sum(s.stock[g]*s.unit_cost[g] for g in s.assortment)
            s.last_profit = round(self.accounts[f"r{s.id}"]-s.opening_cash + closing_asset-s.opening_inventory)
            self.operating_profit += s.last_profit
            s.losses = s.losses+1 if s.last_profit < 0 else 0
            self.observations.append({"month":self.month,"store":s.id,"node":s.node,"area":s.area,"age":s.age,
                "sales":s.sales,"customers":s.customers,"profit_cents":s.last_profit,"cash":self.accounts[f"r{s.id}"],
                "freight_cents":s.freight_paid,"stock":sum(s.stock.values())})
            for good in s.assortment:
                # Market-specific sales: no same-value assignment to both categories.
                sold = s.period_sales.get(good,0)
                s.expected[good] = .65*s.expected[good]+.35*sold
            if s.age >= 6 and s.losses >= 4:
                self.close(s,"losses")
        self.distribute_dividends()

    def distribute_dividends(self):
        """Legacy shared-ownership baseline; scenarios may suspend distributions."""
        for s in self.stores:
            if s.active:
                reserve = round(48*s.area*130 + self.rent(s.node,s.area)+self.staff_hours(s.area)*100)
                self.pay(f"r{s.id}","fund",max(0,self.accounts[f"r{s.id}"]-reserve)//2,"retail_dividend")
        for p in self.producers:
            extra = max(0,self.accounts[f"p{p.id}"]-8000)
            self.pay(f"p{p.id}","property",extra//2,"factory_dividend")
        # Land services and productive businesses have the same household equity ownership.
        payout = self.accounts["property"]//len(self.residents)
        for h in self.residents:
            self.pay("property",f"h{h.id}",payout,"household_dividend")
        # Retail profits also return to households, but initial investment pool stays reserved.
        extra = max(0,self.accounts["fund"]-self.settings.initial_capital)
        payout = extra//len(self.residents)
        for h in self.residents:
            self.pay("fund",f"h{h.id}",payout,"fund_dividend")

    def step(self):
        opening = {g:self.stock_total(g) for g in GOODS}
        self.reset_period()
        for s in self.stores:
            s.period_sales = dict.fromkeys(s.assortment,0)
        self.enter()
        for s in self.stores:
            s.period_sales = dict.fromkeys(s.assortment,0)
            s.opening_inventory = sum(s.stock[g]*s.unit_cost[g] for g in s.assortment)
        self.production()
        self.operate_stores()
        self.consume()
        self.settle()
        self.check()
        row = self.measure()
        for good in GOODS:
            consumed = sum(getattr(h,good) for h in self.residents)
            error = opening[good]+self.produced[good]-consumed-self.wasted[good]-self.stock_total(good)
            assert error == 0, (good,error)
            row[f"stock_error_{good}"] = error
        self.metrics.append(row)
        self.previous_traffic = self.traffic.copy()
        self.month += 1
        return row

    def check(self):
        assert all(isinstance(v,int) and v>=0 for v in self.accounts.values())
        assert sum(self.accounts.values()) == self.initial_money
        for p in self.producers:
            assert p.stock>=0 and p.price>0
        for s in self.stores:
            assert all(isinstance(q,int) and q>=0 for q in s.stock.values())
            assert sum(s.stock.values())<=s.capacity
        for h in self.residents:
            assert h.work<=8+1e-6 and h.shopping<=self.settings.shopping_hours+1e-8

    def measure(self):
        n=len(self.residents)
        units=sum(h.food+h.goods for h in self.residents)
        active=[s for s in self.stores if s.active]
        nearest = sum(min((self.manhattan(s.node,t.node) for t in active if t.id!=s.id),default=0) for s in active)/max(1,len(active))
        production_exposure = sum(p.production/(1+self.manhattan(h.node,p.node))**2 for h in self.residents for p in self.producers)/n
        commercial_exposure = sum(q/(1+self.manhattan(h.node,node))**2 for h in self.residents for node,q in self.sales_by_location.items())/n
        return {"month":self.month,"food_coverage":sum(h.food for h in self.residents)/(2*n),
                "goods_coverage":sum(h.goods for h in self.residents)/(2*n),"units":units,
                "retail_share":self.retail_units/max(1,units),"shopping_hours":sum(h.shopping for h in self.residents),
                "labor_hours":sum(self.labor.values()),"manufacturing_hours":self.labor['manufacturing'],
                "retail_staff_hours":self.labor['retail_staff'],"factory_service_hours":self.labor['factory_service'],
                "freight_hours":self.labor['freight'],"total_hours":sum(self.labor.values())+sum(h.shopping for h in self.residents),
                "hours_per_unit":(sum(self.labor.values())+sum(h.shopping for h in self.residents))/max(1,units),
                "consumer_spend":self.consumer_spend/100,"paid_price":self.consumer_spend/100/max(1,units),
                "stores":len(active),"small":sum(s.area==1 for s in active),"large":sum(s.area==4 for s in active),
                "retail_land":sum(s.area for s in active),"nearest_store_distance":nearest,
                "truck_trips":self.truck_trips,"road_load":sum(self.traffic.values()),"freight_spend":self.freight_spend/100,
                "waste":sum(self.wasted.values()),"production_exposure":production_exposure,"commercial_exposure":commercial_exposure,
                "service_rejections":self.lost_service,"budget_shortfall":self.budget_shortfall,"stock_shortfall":self.stock_shortfall,
                "retail_profit":self.operating_profit/100,"fund_cash":self.accounts['fund']/100,
                "cash_error":sum(self.accounts.values())-self.initial_money}

    def snapshot(self):
        return copy.deepcopy({"schema":1,"seed":self.seed,"settings":asdict(self.settings),"month":self.month,
            "accounts":self.accounts,"initial_money":self.initial_money,"residents":[asdict(h) for h in self.residents],
            "producers":[asdict(p) for p in self.producers],"stores":[asdict(s) for s in self.stores],
            "previous_traffic":[[a,b,v] for (a,b),v in self.previous_traffic.items()],
            "metrics":self.metrics,"events":self.events,"observations":self.observations,"transactions":self.transactions})

    @classmethod
    def restore(cls,data,trace=False):
        if data.get("schema")!=1:
            raise ValueError("Unsupported commerce snapshot")
        data=copy.deepcopy(data)
        model=cls(data['seed'],Settings(**data['settings']),trace)
        for key in ('month','accounts','initial_money','metrics','events','observations','transactions'):
            setattr(model,key,data[key])
        model.residents=[Resident(**x) for x in data['residents']]
        model.producers=[Producer(**x) for x in data['producers']]
        model.stores=[Store(**x) for x in data['stores']]
        model.previous_traffic={(a,b):v for a,b,v in data['previous_traffic']}
        model.check()
        return model
