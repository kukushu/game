import json
import unittest
from unittest.mock import patch

from landlab.model import LandCity
from landlab.session import LandSession


class AssessmentTests(unittest.TestCase):
    def test_complete_costs_outcomes_and_roundtrip(self):
        c = LandCity(7)
        c.zones = dict.fromkeys(range(35), 'open')
        for _ in range(16):
            c.step()
        audit = c.commercial_audit
        self.assertEqual(len(audit.rounds), 16)
        self.assertGreater(len(audit.visits), 12*8)
        decisions = set()
        for v in audit.visits:
            for p in v['plans']:
                costs = p['costs']
                self.assertAlmostEqual(costs['revenue']-costs['total'], p['profit'])
                self.assertEqual(p['capital'], p['working_capital']+p['setup_fee'])
                self.assertEqual(p['affordable'], v['fund_cash'] >= p['capital'])
                decisions.add(p['decision'])
        self.assertIn('selected', decisions)
        self.assertIn('rejected', decisions)
        self.assertIn('other_site_selected', decisions)
        restored = LandCity.restore(c.snapshot())
        self.assertEqual(c.snapshot(), restored.snapshot())
        session = LandSession(c, restored)
        loaded = LandSession.load(json.loads(json.dumps(session.dump())))
        self.assertEqual(loaded.player.commercial_audit.dump(), c.commercial_audit.dump())
        c.step(); restored.step()
        self.assertEqual(c.snapshot(), restored.snapshot())

    def test_blocked_sites_and_inspection_are_recorded_honestly(self):
        c = LandCity(2)
        c.zones = dict.fromkeys(range(35), 'reserve')
        c.step()
        self.assertEqual(len(c.commercial_audit.visits), 8)
        for v in c.commercial_audit.visits:
            self.assertIn('no_commercial_permission', v['blockers'])
            self.assertEqual(v['plans'], [])
        s = LandSession(c, LandCity.restore(c.snapshot()))
        before = c.snapshot()
        s.inspect('node', 8)
        self.assertEqual(c.snapshot(), before)
        frozen = c.commercial_audit.dump()
        c.step()
        self.assertEqual(frozen['visits'], c.commercial_audit.visits[:8])

    def test_instrumentation_does_not_change_economy(self):
        a, b = LandCity(5), LandCity(5)
        with patch.object(b.commercial_audit, 'begin'), patch.object(b.commercial_audit, 'finish'):
            for _ in range(14):
                a.step(); b.step()
        left, right = a.snapshot(), b.snapshot()
        left['_land'].pop('commercial_audit'); right['_land'].pop('commercial_audit')
        self.assertEqual(left, right)

    def test_legacy_import_keeps_only_real_numbers(self):
        c = LandCity(4)
        for _ in range(3): c.step()
        old = c.snapshot(); old['_land'].pop('commercial_audit')
        restored = LandCity.restore(old)
        self.assertEqual(restored.commercial_audit.full_since, c.month)
        for v in restored.commercial_audit.visits:
            self.assertTrue(v['legacy_partial'])
            self.assertNotIn('fund_cash', v)
            for p in v['plans']: self.assertNotIn('costs', p)
        restored.step()
        self.assertTrue(any(not v['legacy_partial'] for v in restored.commercial_audit.visits))


if __name__ == '__main__': unittest.main()
