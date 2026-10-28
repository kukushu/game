import copy
import json
import tempfile
import unittest
from pathlib import Path

from landlab.model import LandCity
from landlab.session import LandSession
from playweb.server import Application


class TaxationTests(unittest.TestCase):
    def test_income_is_withheld_from_actual_recipient_without_changing_gross_pay(self):
        city = LandCity(1)
        city.income_tax_rate = 10
        city.current = {'month': 0}
        before = city.accounts.copy()
        city.pay('p0', 'h0', 123, 'manufacturing')
        self.assertEqual(city.accounts['p0'], before['p0'] - 123)
        self.assertEqual(city.accounts['h0'], before['h0'] + 111)
        self.assertEqual(city.accounts['civic'], before['civic'] + 12)
        self.assertEqual(sum(city.accounts.values()), city.initial_money)
        self.assertEqual([x['purpose'] for x in city.current['payments']],
                         ['manufacturing', 'income_tax'])
        self.assertEqual(city.fiscal_summary()['gross_income'], 123)
        self.assertEqual(city.fiscal_summary()['net_income'], 111)

    def test_dividends_taxed_but_existing_cash_rent_and_business_transfers_are_not(self):
        city = LandCity(1)
        city.income_tax_rate = 30
        city.current = {'month': 0}
        public = city.accounts['civic']
        city.pay('h0', 'property', 100, 'housing_rent')
        city.pay('fund', 'p0', 100, 'equity_investment')
        self.assertEqual(city.accounts['civic'], public)
        city.pay('property', 'h0', 100, 'household_dividend')
        self.assertEqual(city.accounts['civic'], public + 30)

    def test_policy_change_is_free_has_no_immediate_income_and_does_not_change_control(self):
        session = LandSession.new(1)
        accounts = session.player.accounts.copy()
        reference = session.reference.snapshot()
        session.intervene('tax', {'rate': 2})
        self.assertEqual(session.player.accounts, accounts)
        self.assertEqual(session.reference.snapshot(), reference)
        self.assertEqual(session.actions[-1]['cost'], 0)

    def test_invalid_rates_leave_application_unchanged_and_valid_policy_persists(self):
        with tempfile.TemporaryDirectory() as folder:
            app = Application(Path(folder) / 'city.json', LandSession.new(2), session_type=LandSession)
            before = copy.deepcopy(app.dump())
            for rate in (-1, 31, True, 1.5, '2'):
                with self.assertRaises(ValueError):
                    app.mutate('action', {'revision': app.revision, 'action': 'tax', 'node': {'rate': rate}})
                self.assertEqual(before, app.dump())
            app.mutate('action', {'revision': app.revision, 'action': 'tax', 'node': {'rate': 2}})
            restored = LandSession.load(json.loads((Path(folder) / 'city.json').read_text(encoding='utf-8')))
            self.assertEqual(restored.player.income_tax_rate, 2)
            app.session.advance(4)
            restored.advance(4)
            self.assertEqual(app.session.dump(), restored.dump())

    def test_old_save_defaults_to_zero_and_recovery_uses_previous_period_tax_receipts(self):
        original = LandSession.new(3).dump()
        for name in ('player', 'reference'):
            original[name]['_land'].pop('income_tax_rate')
        session = LandSession.load(original)
        self.assertEqual(session.player.income_tax_rate, 0)
        city = session.player
        city.accounts['property'] += city.accounts['civic']
        city.accounts['civic'] = 0
        session.intervene('tax', {'rate': 2})
        city.step()
        self.assertEqual(city.maintenance_ratio, 0)
        self.assertGreater(city.accounts['civic'], 0)
        city.step()
        self.assertEqual(city.maintenance_ratio, 1)
        self.assertEqual(city.metrics[-1]['cash_error'], 0)

    def test_actual_tax_ledger_and_period_history_match_and_zero_rate_stays_identical(self):
        session = LandSession.new(1)
        session.advance(3)
        self.assertEqual(session.player.snapshot(), session.reference.snapshot())
        session.intervene('tax', {'rate': 10})
        session.advance(3)
        city = session.player
        taxes = sum(x['cents'] for x in city.current['payments'] if x['purpose'] == 'income_tax')
        self.assertEqual(taxes, city.spatial_history[-1]['tax_income'])
        self.assertGreater(taxes, 0)
        self.assertEqual(city.metrics[-1]['cash_error'], 0)
        self.assertEqual(city.metrics[-1]['stock_error_food'], 0)


if __name__ == '__main__':
    unittest.main()
