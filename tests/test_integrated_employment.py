import copy
import json
import unittest
from landlab.game_session import GameSession, GamePlanningSession
from landlab.session import LandSession
from playweb.server import Application


class IntegratedEmploymentTests(unittest.TestCase):
    def test_old_save_uses_market_without_rewriting_history_or_accounts(self):
        old=LandSession.new(1).dump();frozen=copy.deepcopy(old)
        session=GameSession.load(old)
        for name in ('player','reference'):
            city=getattr(session,name)
            expected=copy.deepcopy(old[name]);expected['_land']['employment']['mode']='market'
            self.assertEqual(city.snapshot(),expected)
            self.assertFalse(city.employers)
        self.assertEqual(old,frozen)
        session.advance(1)
        self.assertTrue(session.player.labor_result['choices'])
        self.assertEqual(session.player.snapshot(),session.reference.snapshot())
        replay=GameSession.load(json.loads(json.dumps(session.dump())))
        session.advance(1);replay.advance(1)
        self.assertEqual(session.dump(),replay.dump())

    def test_blank_blueprint_launches_market_through_original_application(self):
        app=Application(session=GameSession.new(1,'blank'),session_type=GameSession)
        self.assertIsInstance(app.session,GamePlanningSession)
        for action,node in [('road',{'id':0,'neighbor':1}),('road',{'id':1,'neighbor':2}),
                            ('road',{'id':2,'neighbor':3}),('zone',{'id':0,'zone':'home'}),
                            ('zone',{'id':1,'zone':'home'}),('zone',{'id':2,'zone':'factory'}),
                            ('zone',{'id':3,'zone':'factory'})]:
            app.mutate('action',{'revision':app.revision,'action':action,'node':node})
        app.mutate('launch',{'revision':app.revision})
        self.assertEqual(app.session.player.labor_mode,'market')
        self.assertEqual(app.session.reference.labor_mode,'market')
        self.assertEqual(app.session.player.month,0)
        app.mutate('advance',{'revision':app.revision,'count':1})
        self.assertTrue(app.session.player.current['labor_choice'])

    def test_normal_new_city_uses_market_and_research_default_remains_pool(self):
        self.assertEqual(GameSession.new(1).player.labor_mode,'market')
        self.assertEqual(LandSession.new(1).player.labor_mode,'pool')
