import copy
import json
import unittest

from landlab.model import LandCity
from landlab.session import LandSession
from playweb.server import Application


class LandTests(unittest.TestCase):
    def test_zoning_grants_right_not_population_or_buildings(self):
        s=LandSession.new(1)
        homes=[h.node for h in s.player.residents];factories=[p.node for p in s.player.producers]
        metrics=copy.deepcopy(s.player.metrics);shops=len(s.player.stores);cash=sum(s.player.accounts.values())
        budget=s.player.accounts['civic']
        s.intervene('zone',{'id':11,'zone':'open'})
        self.assertEqual(homes,[h.node for h in s.player.residents]);self.assertEqual(factories,[p.node for p in s.player.producers])
        self.assertEqual(shops,len(s.player.stores));self.assertEqual(metrics,s.player.metrics)
        self.assertEqual(sum(s.player.accounts.values()),cash);self.assertEqual(s.player.accounts['civic'],budget-250)

    def test_shared_land_restricts_commerce_and_existing_uses_are_grandfathered(self):
        c=LandCity(1)
        c.zones[8]='shop'
        for h in c.residents[:32]: h.node=8
        for h in c.residents[32:]:
            if h.node==8:h.node=15
        self.assertEqual(c.free_land(8),0);self.assertEqual(c.legal_areas(8),[])
        self.assertEqual(sum(h.node==8 for h in c.residents),32)

    def test_road_changes_actual_path_and_costs_real_money(self):
        s=LandSession.new(2);c=s.player
        old,path=c.route(9,16);budget=c.accounts['civic'];accounts=sum(c.accounts.values())
        self.assertNotEqual(path,((9,16),))
        s.intervene('road',{'id':9,'neighbor':16})
        new,path=c.route(9,16)
        self.assertLess(new,old);self.assertEqual(path,((9,16),))
        self.assertEqual(c.accounts['civic'],budget-1200);self.assertEqual(sum(c.accounts.values()),accounts)
        self.assertNotIn((9,16),s.reference.roads)

    def test_fixed_mechanism_does_not_move_subjects_but_runs_real_economy(self):
        s=LandSession.new(3,'fixed');homes=[h.node for h in s.player.residents];factories=[p.node for p in s.player.producers]
        for n in range(35):
            if s.player.zones[n]!='open':s.intervene('zone',{'id':n,'zone':'open'})
        s.advance(6)
        self.assertEqual(homes,[h.node for h in s.player.residents]);self.assertEqual(factories,[p.node for p in s.player.producers])
        self.assertTrue(s.player.metrics[-1]['units']>0)

    def test_pollution_enters_home_decision_only_when_enabled(self):
        c=LandCity(1);h=c.residents[0]
        c.producers[0].production=160
        near=c.producers[0].node;far=0
        a=c.home_score(h,near);b=c.home_score(h,far)
        self.assertGreater(a['environment'],b['environment'])
        c.environment_weight=0
        self.assertEqual(c.home_score(h,near)['environment'],0)

    def test_household_and_factory_decisions_pay_and_respect_capacity(self):
        s=LandSession.new(1)
        for n in range(35):
            if s.player.zones[n]!='open':s.intervene('zone',{'id':n,'zone':'open'})
        s.advance(6)
        c=s.player
        self.assertGreater(sum(x['moves'] for x in c.spatial_history),0)
        self.assertGreater(sum(x['factory_moves'] for x in c.spatial_history),0)
        self.assertTrue(all(c.free_land(n)>=0 for n in range(35)))
        for r in c.home_reviews:
            if r['reason']=='搬迁':self.assertGreater(r['current']['total']-r['candidate']['total'],50)
        self.assertEqual(sum(c.accounts.values()),c.initial_money)

    def test_commuting_uses_finite_labor_time_and_records_real_routes(self):
        s=LandSession.new(2)
        self.assertTrue(s.player.current.get('commute_trip'))
        self.assertTrue(all(h.work+s.player.commute.get(h.id,0)<=8+1e-6 for h in s.player.residents))
        for r in s.player.current['commute_trip']:
            self.assertTrue(all(tuple(e) in s.player.roads for e in r['edges']))

    def test_snapshot_resumes_migration_roads_traces_and_comparison(self):
        a=LandSession.new(4);a.intervene('zone',{'id':9,'zone':'open'});a.intervene('road',{'id':9,'neighbor':16});a.advance(3)
        b=LandSession.load(json.loads(json.dumps(a.dump())))
        self.assertEqual(a.inspect('node',9)['home_appraisals'],b.inspect('node',9)['home_appraisals'])
        a.advance(3);b.advance(3)
        self.assertEqual(a.dump(),b.dump())

    def test_invalid_road_and_unfunded_action_leave_application_unchanged(self):
        app=Application(session=LandSession.new(1),session_type=LandSession,default_preset='adaptive')
        before=app.dump()
        with self.assertRaises(ValueError):app.mutate('action',{'revision':app.revision,'action':'road','node':{'id':0,'neighbor':34}})
        self.assertEqual(before,app.dump())
        app.session.player.accounts['property']+=app.session.player.accounts['civic'];app.session.player.accounts['civic']=0
        before=app.dump()
        with self.assertRaises(ValueError):app.mutate('action',{'revision':app.revision,'action':'zone','node':{'id':0,'zone':'home'}})
        self.assertEqual(before,app.dump())

    def test_no_intervention_reference_remains_exact(self):
        s=LandSession.new(5);s.advance(4)
        self.assertEqual(s.player.snapshot(),s.reference.snapshot())

    def test_corrupt_spatial_save_rejected_before_overwrite(self):
        s=LandSession.new(2);bad=s.dump();bad['player']['_land']['roads']=[]
        with self.assertRaises(ValueError):LandSession.load(bad)

    def test_unfunded_maintenance_slows_routes_without_creating_money(self):
        c=LandCity(1);c.settings.congestion=0
        before=c.route(0,1)[0]
        c.accounts['property']+=c.accounts['civic'];c.accounts['civic']=0
        c.step()
        self.assertEqual(c.maintenance_ratio,0)
        self.assertAlmostEqual(c.route(0,1)[0],2*before)
        self.assertEqual(sum(c.accounts.values()),c.initial_money)


if __name__=='__main__':unittest.main()
