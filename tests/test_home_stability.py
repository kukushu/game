import copy
import json
import unittest
from unittest.mock import patch

from landlab.model import LandCity
from landlab.session import LandSession


class HomeStabilityTests(unittest.TestCase):
    def appraise(self, city, saving):
        city.month = 10
        city.zones = dict.fromkeys(range(35), 'open')

        def score(household, node, previous_jobs=None):
            return {'rent': 100, 'shopping': 500, 'commute': 400, 'environment': 0,
                    'total': 1000 if node == household.node else 1000 - saving}

        with patch.object(city, 'home_score', side_effect=score):
            city.review_homes({})

    def test_small_gain_stays_large_gain_still_relocates_and_pays(self):
        city = LandCity(1)
        homes = [h.node for h in city.residents]
        accounts = copy.deepcopy(city.accounts)
        self.appraise(city, 80)
        self.assertEqual(homes, [h.node for h in city.residents])
        self.assertEqual(accounts, city.accounts)

        self.appraise(city, 200)
        moves = [e for e in city.spatial_events if e['type'] == 'home_move']
        self.assertTrue(moves, 'Large improvements must remain actionable')
        for event in moves:
            self.assertEqual(city.accounts[f'h{event["resident"]}'],
                             accounts[f'h{event["resident"]}'] - 120)
        self.assertEqual(sum(city.accounts.values()), city.initial_money)
        self.assertTrue(all(city.free_land(n) >= 0 for n in range(35)))

    def test_legacy_and_blind_research_controls_keep_old_behavior(self):
        for mode, policy in [('adaptive', 'legacy'), ('blind', 'stable')]:
            city = LandCity(1)
            city.mode, city.home_move_policy = mode, policy
            self.appraise(city, 80)
            self.assertTrue(city.spatial_events)

    def test_move_cooldown_still_prevents_immediate_repeat(self):
        city = LandCity(1)
        city.moved_home = dict.fromkeys(range(64), 9)
        self.appraise(city, 200)
        self.assertFalse(city.spatial_events)

    def test_policy_survives_save_and_old_save_adopts_rule_without_reset(self):
        session = LandSession.new(2)
        session.advance(3)
        snapshot = session.dump()
        for world in ('player', 'reference'):
            snapshot[world]['_land'].pop('home_move_policy')
        restored = LandSession.load(snapshot)
        self.assertEqual(restored.player.home_move_policy, 'stable')
        self.assertEqual(restored.player.accounts, session.player.accounts)
        self.assertEqual(restored.player.metrics, session.player.metrics)
        self.assertEqual([h.node for h in restored.player.residents],
                         [h.node for h in session.player.residents])
        resumed = LandSession.load(json.loads(json.dumps(restored.dump())))
        restored.advance(3)
        resumed.advance(3)
        self.assertEqual(restored.dump(), resumed.dump())

        restored.player.home_move_policy = 'legacy'
        self.assertEqual(LandSession.load(restored.dump()).player.home_move_policy, 'legacy')


if __name__ == '__main__':
    unittest.main()
