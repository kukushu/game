import copy
import json
import unittest
from dataclasses import asdict

from econlab.experiments import run_until
from econlab.model import Config, Economy, cents


class AccountingAndMechanismTests(unittest.TestCase):
    def model(self, **kwargs):
        return Economy(17, Config(households=48, firms=8, **kwargs), trace=True)

    def test_cash_and_physical_conservation_under_multiple_policies(self):
        for seed in range(4):
            m = Economy(seed, Config(households=48, firms=8), trace=True)
            for month in range(60):
                if month == 8:
                    m.intervene("bridge_target", 12)
                    m.intervene("training_seats", 3)
                if month == 24:
                    m.intervene("food_shock", .5)
                if month == 32:
                    m.intervene("food_shock", 1)
                m.step()
                self.assertEqual(m.total_money(), m.initial_money)
                self.assertLess(abs(m.metrics[-1]["physical_error_food"]), 1e-6)
                self.assertLess(abs(m.metrics[-1]["physical_error_goods"]), 1e-6)

    def test_full_ledger_reconstructs_each_account(self):
        m = self.model()
        accounts = {"government": m.treasury}
        accounts.update({f"h{h.id}": h.cash for h in m.households})
        accounts.update({f"f{f.id}": f.cash for f in m.firms})
        run_until(m, 30)
        for t in m.ledger:
            accounts[t["source"]] -= t["cents"]
            accounts[t["target"]] += t["cents"]
            self.assertGreaterEqual(accounts[t["source"]], 0)
        for account, balance in accounts.items():
            self.assertEqual(balance, m.balance(account))

    def test_snapshot_resume_is_exact_and_forks_do_not_alias(self):
        m = self.model()
        run_until(m, 10)
        snapshot = json.loads(json.dumps(m.snapshot()))
        frozen = copy.deepcopy(snapshot)
        branch = Economy.restore(snapshot, True)
        run_until(m, 20)
        run_until(branch, 20)
        self.assertEqual(m.snapshot(), branch.snapshot())
        branch.intervene("income_tax", .2)
        self.assertEqual(snapshot, frozen)
        self.assertNotEqual(m.policy.income_tax, branch.policy.income_tax)

    def test_no_jobs_when_employers_have_no_cash(self):
        m = self.model()
        for f in m.firms:
            m.transfer(f"f{f.id}", f"h{f.owner}", f.cash, "test_liquidation")
        m.step()
        self.assertEqual(m.metrics[-1]["employment"], 0)
        self.assertEqual(sum(f.production for f in m.firms), 0)

    def test_no_stock_no_magic_goods_and_no_payment(self):
        m = self.model()
        before = m.households[0].cash
        qty, paid = m._buy("h0", "food", 1, before, 0, "test")
        self.assertEqual((qty, paid), (0, 0))
        self.assertEqual(before, m.households[0].cash)

    def test_production_without_any_labor_is_zero(self):
        m = self.model()
        for h in m.households:
            h.skill = .65
        for f in m.firms:
            if f.sector == "food":
                m.transfer(f"f{f.id}", f"h{f.owner}", f.cash, "test_drain")
        m.step()
        self.assertEqual(sum(f.production for f in m.firms), 0)

    def test_bridge_requires_actual_goods_and_payment(self):
        m = self.model()
        m.intervene("bridge_target", 10)
        m.public_units = 0
        m._public_project()
        self.assertEqual(m.bridge_progress, 0)
        self.assertFalse(m.bridge_open)
        f = next(f for f in m.firms if f.sector == "goods")
        f.district = 1
        f.inventory = 10
        before = m.treasury
        m._public_project()
        self.assertTrue(m.bridge_open)
        self.assertAlmostEqual(m.bridge_progress, 10)
        self.assertEqual(before - m.treasury, cents(10 * f.price / 100))

    def test_no_unfunded_education_and_completion_takes_time(self):
        m = self.model()
        m.intervene("training_seats", 2)
        m.intervene("training_months", 3)
        before = sum(h.skill >= 1 for h in m.households)
        m.step()
        self.assertEqual(before, sum(h.skill >= 1 for h in m.households))
        for _ in range(2):
            m.step()
        self.assertGreater(sum(h.skill >= 1 for h in m.households), before)
        m.transfer("government", "h0", m.treasury, "test_drain")
        m.flow = {}
        m.reasons = {"training_unfunded": 0, "graduates": 0}
        m._train()
        self.assertEqual(m.flow.get("training_stipend", 0), 0)

    def test_geographic_barrier_changes_reach_not_output_directly(self):
        m = self.model()
        old = m.distance(1, 2)
        self.assertGreater(old, m.config.max_commute)
        productivity = [f.productivity for f in m.firms]
        m.bridge_open = True
        self.assertLess(m.distance(1, 2), m.config.max_commute)
        self.assertEqual(productivity, [f.productivity for f in m.firms])

    def test_reject_invalid_inputs(self):
        with self.assertRaises(ValueError):
            Economy(config=Config(food_productivity=0))
        with self.assertRaises(ValueError):
            Config(wealth_spend=2).validate()
        m = self.model()
        for action, value in [("income_tax", 2), ("food_shock", 0), ("training_months", 0), ("training_seats", 1.2)]:
            with self.assertRaises(ValueError):
                m.intervene(action, value)
        with self.assertRaises(ValueError):
            run_until(m, 10, [{"month": -1, "action": "benefit", "value": 2}])

    def test_same_random_seed_and_policy_reproduces_all_states(self):
        a, b = self.model(), self.model()
        schedule = [{"month": 4, "action": "income_tax", "value": .2}]
        run_until(a, 24, schedule)
        run_until(b, 24, schedule)
        self.assertEqual(a.snapshot(), b.snapshot())
        self.assertEqual(a.ledger, b.ledger)

    def test_noop_policy_does_not_change_economic_outcomes(self):
        a, b = self.model(), self.model()
        run_until(a, 24)
        run_until(b, 24, [{"month": 8, "action": "income_tax", "value": .12}])
        self.assertEqual(a.metrics, b.metrics)
        self.assertEqual([asdict(h) for h in a.households], [asdict(h) for h in b.households])

    def test_exit_and_reentry_use_real_owner_money(self):
        m = self.model()
        f = m.firms[0]
        m.transfer(f"f{f.id}", f"h{f.owner}", f.cash, "test_withdrawal")
        m.transfer("government", f"h{f.owner}", cents(100), "test_capital")
        run_until(m, 9)
        self.assertGreaterEqual(sum(r["closures"] for r in m.metrics), 1)
        self.assertTrue(any(t["kind"] == "owner_investment" for t in m.ledger))
        self.assertEqual(m.total_money(), m.initial_money)

    def test_long_runs_and_extreme_policies_keep_accounts_valid(self):
        for seed in (0, 7, 99):
            m = Economy(seed, Config(households=48, firms=8))
            run_until(m, 600, [
                {"month": 5, "action": "income_tax", "value": .7},
                {"month": 30, "action": "benefit", "value": 20},
                {"month": 40, "action": "training_seats", "value": 100},
                {"month": 80, "action": "bridge_target", "value": 500},
                {"month": 120, "action": "food_shock", "value": .1},
                {"month": 160, "action": "food_shock", "value": 1},
            ])
            m.check()


if __name__ == "__main__":
    unittest.main()
