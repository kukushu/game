import copy
import json
import unittest

from landlab.planning import PlanningSession
from landlab.session import LandSession
from playweb.server import Application


def prepared():
    s=LandSession.new(1,'blank')
    for a,b in ((8,9),(9,10),(10,11)):s.intervene('road',{'id':a,'neighbor':b})
    for node,zone in ((8,'home'),(9,'home'),(10,'factory'),(11,'factory')):s.intervene('zone',{'id':node,'zone':zone})
    return s


class BlankStartTests(unittest.TestCase):
    def test_no_hidden_population_factories_roads_or_economic_time(self):
        s=LandSession.new(1,'blank');view=s.view();city=view['worlds']['player']
        self.assertEqual(view['stage'],'planning');self.assertEqual(view['month'],0)
        self.assertEqual(city['residents'],[]);self.assertEqual(city['producers'],[])
        self.assertEqual(city['stores'],[]);self.assertEqual(city['roads'],[])
        self.assertTrue(all(n['zone']=='reserve' and n['free']==8 for n in city['nodes']))
        self.assertNotIn('player',s.dump())
        saved=copy.deepcopy(s.dump())
        with self.assertRaises(ValueError):s.advance(3)
        self.assertEqual(saved,s.dump())

    def test_plans_pay_but_do_not_create_subjects(self):
        s=prepared()
        self.assertEqual(s.budget,25400)
        self.assertTrue(s.layout()['ready'])
        self.assertFalse(s.view()['worlds']['player']['residents'])
        self.assertEqual(s.dump(),LandSession.load(json.loads(json.dumps(s.dump()))).dump())

    def test_open_only_after_valid_connected_capacity_then_continue_economy(self):
        plan=prepared();s=plan.launch();c=s.player
        self.assertEqual(c.month,0);self.assertEqual(len(c.residents),64);self.assertEqual(len(c.producers),4)
        self.assertEqual(c.accounts['civic'],25400);self.assertEqual(sum(c.accounts.values()),c.initial_money)
        self.assertEqual(sum(p.production for p in c.producers),0)
        self.assertTrue(all(c.zones[h.node]=='home' for h in c.residents))
        self.assertTrue(all(c.zones[p.node]=='factory' for p in c.producers))
        self.assertEqual(len(c.initial_locations['homes']),64)
        self.assertEqual(len(c.initial_locations['factories']),4)
        for record in c.initial_locations['homes']:
            winner=min(record['alternatives'],key=lambda a:(a['total'],a['node']))
            self.assertEqual(record['node'],winner['node'])
        for record in c.initial_locations['factories']:
            winner=min(record['alternatives'],key=lambda a:(a['distance'],a['node']))
            self.assertEqual(record['node'],winner['node'])
        restored=LandSession.load(json.loads(json.dumps(s.dump())))
        self.assertEqual(restored.player.initial_locations,c.initial_locations)
        self.assertEqual(len(c.roads),3)
        s.advance(3)
        self.assertGreater(c.metrics[-1]['units'],0)
        loaded=LandSession.load(json.loads(json.dumps(s.dump())))
        loaded.advance(3);s.advance(3)
        self.assertEqual(loaded.dump(),s.dump())

    def test_disconnected_zones_do_not_make_start_ready(self):
        s=prepared();s.roads.pop((9,10))
        self.assertFalse(s.layout()['ready'])
        with self.assertRaises(ValueError):s.launch()

    def test_unconnected_future_land_can_be_inspected_but_cannot_host_entrants(self):
        s=prepared().launch();s.intervene('zone',{'id':34,'zone':'open'})
        self.assertEqual(s.player.legal_areas(34),[])
        self.assertEqual(s.inspect('node',34)['home_appraisals'],[])
        self.assertIsNone(s.inspect('road',27*35+34)['duration_before'])
        s.advance(12)
        self.assertFalse(any(h.node==34 for h in s.player.residents))
        self.assertFalse(any(p.node==34 for p in s.player.producers))
        self.assertFalse(any(x.active and x.node==34 for x in s.player.stores))

    def test_application_defaults_to_blank_and_launch_failure_is_atomic(self):
        app=Application(session_type=LandSession,default_preset='blank')
        self.assertIsInstance(app.session,PlanningSession)
        before=app.dump()
        with self.assertRaises(ValueError):app.mutate('launch',{'revision':app.revision})
        self.assertEqual(before,app.dump())
        app.session=prepared()
        app.mutate('launch',{'revision':app.revision})
        self.assertEqual(app.state()['stage'],'running')


if __name__=='__main__':unittest.main()
