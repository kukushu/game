import copy
import unittest
from unittest.mock import patch

from commerce.model import City
from landlab.model import LandCity

DIVIDENDS = {'retail_dividend', 'factory_dividend', 'household_dividend', 'fund_dividend'}


class DividendPauseTests(unittest.TestCase):
    def test_same_state_changes_only_distributions_in_first_period(self):
        paused = LandCity(1)
        legacy = copy.deepcopy(paused)
        with patch.object(LandCity, 'distribute_dividends', City.distribute_dividends):
            legacy.step()
        paused.step()
        paid = legacy.current['payments']
        distributions = [p for p in paid if p['purpose'] in DIVIDENDS]
        self.assertTrue(distributions)
        self.assertEqual(paused.current['payments'], [p for p in paid if p['purpose'] not in DIVIDENDS])
        expected = legacy.accounts.copy()
        for p in distributions:
            expected[p['source']] += p['cents']
            expected[p['target']] -= p['cents']
        self.assertEqual(paused.accounts, expected)
        self.assertEqual([s.last_profit for s in paused.stores], [s.last_profit for s in legacy.stores])
        self.assertEqual([p.production for p in paused.producers], [p.production for p in legacy.producers])
        self.assertEqual([(h.food,h.goods) for h in paused.residents], [(h.food,h.goods) for h in legacy.residents])

    def test_history_survives_restore_and_future_distributions_stop(self):
        city = LandCity(7)
        with patch.object(LandCity, 'distribute_dividends', City.distribute_dividends):
            city.step()
        history = copy.deepcopy(city.traces)
        restored = LandCity.restore(city.snapshot())
        self.assertEqual(restored.traces, history)
        self.assertTrue(any(p['purpose'] in DIVIDENDS for p in restored.current['payments']))
        restored.income_tax_rate = 10
        for _ in range(12):
            row = restored.step()
            payments = restored.current['payments']
            self.assertFalse(any(p['purpose'] in DIVIDENDS for p in payments))
            self.assertTrue(any(p['purpose']=='manufacturing' for p in payments))
            self.assertEqual(row['cash_error'],0)
            self.assertEqual(row['stock_error_food'],0)
            self.assertEqual(row['stock_error_goods'],0)
        self.assertFalse(restored.fiscal_summary()['dividends_enabled'])

    def test_pause_hook_does_not_withdraw_any_account_balance(self):
        city = LandCity(1)
        accounts = city.accounts.copy()
        city.distribute_dividends()
        self.assertEqual(city.accounts,accounts)
