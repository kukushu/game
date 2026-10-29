import json
import unittest
from unittest.mock import patch

from landlab.model import LandCity
from landlab.session import LandSession


class AssessmentTests(unittest.TestCase):
    def test_whole_round_ranking_is_historical_and_matches_actual_winner(self):
        c = LandCity(7)
        c.zones = dict.fromkeys(range(35), 'open')
        c.step()
        round_ = c.commercial_audit.rounds[-1]
        proposals = round_['proposals']
        eligible = [p for p in proposals if p['profit'] > 0 and p['affordable']]
        expected = max(eligible, key=lambda p:p['score'])
        self.assertEqual(expected['decision'], 'selected')
        self.assertEqual(expected['rank'], 1)
        self.assertEqual(round_['winner']['node'], expected['node'])
        store_report=c.commercial_audit.for_store(round_['winner']['store'])
        self.assertEqual(store_report['proposals'],round_['proposals'])
        self.assertEqual(store_report['selected_plan']['profit'],expected['profit'])
        for p in proposals:
            self.assertAlmostEqual(p['score'], p['profit']/p['capital'])
            if p['blockers']: self.assertIsNone(p['rank'])
        saved = json.dumps(round_, sort_keys=True)
        c.step()
        self.assertEqual(saved, json.dumps(c.commercial_audit.rounds[0], sort_keys=True))
        visit = c.commercial_audit.for_node(expected['node'])['visits'][0]
        self.assertEqual(visit['comparison'], round_)

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

    def test_ineligible_sites_do_not_consume_assessments(self):
        c = LandCity(2)
        c.zones = dict.fromkeys(range(35), 'reserve')
        c.step()
        self.assertEqual(c.commercial_audit.visits, [])
        self.assertEqual(c.commercial_audit.rounds[-1]['eligible_nodes'], [])
        s = LandSession(c, LandCity.restore(c.snapshot()))
        before = c.snapshot()
        s.inspect('node', 8)
        self.assertEqual(c.snapshot(), before)
        frozen = c.commercial_audit.dump()
        c.step()
        self.assertEqual(frozen['visits'], [])
        self.assertEqual(c.commercial_audit.visits, [])

    def test_few_eligible_sites_are_all_assessed_without_forced_investment(self):
        c = LandCity(2)
        c.zones = dict.fromkeys(range(35), 'reserve')
        c.zones.update({10:'shop', 17:'mixed'})
        c.accounts['property'] += c.accounts['fund']; c.accounts['fund'] = 0
        c.step()
        self.assertEqual({v['node'] for v in c.commercial_audit.visits}, {10,17})
        self.assertTrue(all(v['plans'] for v in c.commercial_audit.visits))
        self.assertFalse(c.stores)
        self.assertTrue(all('insufficient_capital' in p['blockers'] for v in c.commercial_audit.visits for p in v['plans']))

    def test_rotation_covers_all_eligible_sites_and_survives_save(self):
        c = LandCity(7)
        c.zones = dict.fromkeys(range(35), 'open')
        c.accounts['property'] += c.accounts['fund']; c.accounts['fund'] = 0
        eligible = {n for n in range(35) if c.commercial_areas(n)}
        for _ in range(5): c.step()
        visited = {v['node'] for v in c.commercial_audit.visits}
        self.assertTrue(eligible <= visited)
        self.assertTrue(all(len(r['selected_nodes']) <= 8 for r in c.commercial_audit.rounds))
        restored = LandCity.restore(json.loads(json.dumps(c.snapshot())))
        c.step(); restored.step()
        self.assertEqual(c.snapshot(), restored.snapshot())

    def test_disconnected_full_and_occupied_sites_are_excluded(self):
        c = LandCity(7)
        c.zones = dict.fromkeys(range(35), 'shop')
        c.greenfield = True; c.gateway = 0
        # Empty border plot 1 is isolated; existing actors remain connected.
        c.roads.pop((0,1)); c.roads.pop((1,2)); c.paths = {}
        self.assertFalse(c.commercial_areas(1))
        for h in c.residents: h.node = 0 if h.id < 32 else 7
        full = 0
        self.assertEqual(c.free_land(full), 0)
        c.step()
        selected = c.commercial_audit.rounds[-1]['selected_nodes']
        self.assertNotIn(1, selected)
        self.assertNotIn(full, selected)
        occupied = {s.node for s in c.stores if s.active}
        c.step()
        self.assertTrue(occupied.isdisjoint(c.commercial_audit.rounds[-1]['selected_nodes']))

    def test_newly_zoned_unseen_candidate_is_reviewed_next_period(self):
        c = LandCity(7)
        c.zones = dict.fromkeys(range(35), 'open')
        c.zones[2] = 'reserve'
        c.accounts['property'] += c.accounts['fund']; c.accounts['fund'] = 0
        for _ in range(5): c.step()
        self.assertNotIn(2, c.commercial_last_review)
        c.zones[2] = 'shop'
        c.step()
        self.assertEqual(c.commercial_audit.rounds[-1]['selected_nodes'][0], 2)

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
