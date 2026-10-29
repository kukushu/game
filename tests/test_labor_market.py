import copy
import json
import unittest

from commerce.model import Settings
from landlab.model import LandCity
from landlab.session import LandSession
from landlab.labor import build_posts, match, wage_bill
from unittest.mock import patch
from landlab import labor


def arena(wages=(100,150), workers=2):
    c=LandCity(1,Settings(households=8,entry=False,channel='retail',congestion=0,spoilage=False))
    c.labor_mode='market';c.mode='fixed'
    c.zones=dict.fromkeys(range(35),'open')
    c.producers[0].node=0;c.producers[1].node=2
    for p in c.producers:p.expected=27.5 if p.id<2 else 0
    for h in c.residents:h.node=1;h.value_time=100;h.work=0 if h.id<workers else 8
    c.factory_wages.update({'0':wages[0],'1':wages[1]})
    c.current={'month':0}
    return c


def next_recruitment(c,workers=2):
    c.reset_period();c.jobs={};c.commute={};c.commute_visits=set();c.current={'month':c.month}
    for h in c.residents:h.work=0 if h.id<workers else 8
    for p in c.producers:p.stock=0;p.expected=27.5 if p.id<2 else 0
    c.production()


class LaborMarketTests(unittest.TestCase):
    def test_A_equal_distance_high_wage_wins_real_scarce_labor(self):
        c=arena();c.production()
        self.assertEqual(c.producers[0].production,0)
        self.assertEqual(c.producers[1].production,32)
        self.assertEqual(set(c.employers.values()),{1})
        self.assertEqual(c.labor_result['firms']['0']['unfilled_hours'],8)
        self.assertEqual(sum(p['cents'] for p in c.current['payments']),1200)

    def test_B_different_commutes_and_road_can_change_choice(self):
        c=arena((100,125));c.residents[0].node=1;c.residents[1].node=5
        c.producers[1].node=6;c.producers[0].expected=13
        c.production()
        self.assertEqual(c.employers,{'0':0,'1':1})
        control=copy.deepcopy(c)
        for a in range(1,6):c.roads[(a,a+1)]=2
        next_recruitment(c);next_recruitment(control)
        self.assertEqual(c.employers['0'],1)
        self.assertEqual(control.employers['0'],0)
        high=next(o for o in control.labor_result['choices']['0']['candidates'] if o['producer']==1)
        self.assertLess(c.labor_result['choices']['0']['contract']['trip'],high['trip'])
        self.assertGreater(c.labor_result['choices']['0']['contract']['trip'],control.labor_result['choices']['0']['contract']['trip'])

    def test_C_cash_caps_posts_payments_and_actual_output(self):
        c=arena((150,100));c.producers[1].expected=0
        old=c.accounts['p0'];c.accounts['p0']=123;c.accounts['property']+=old-123
        c.production();f=c.labor_result['firms']['0']
        self.assertEqual(c.producers[0].production,3)
        self.assertEqual(f['funded_units'],3)
        self.assertEqual(f['wages_paid'],wage_bill(3,150))
        self.assertEqual(c.accounts['p0'],10)
        self.assertEqual(sum(c.accounts.values()),c.initial_money)
        self.assertIn('cash_limit',f['reasons'])

    def test_D_stable_on_small_fluctuation_can_switch_on_large_gain(self):
        c=arena((100,100),1);c.production();old=c.employers['0'];other=1-old
        c.factory_wages[str(other)]=105
        next_recruitment(c,1)
        self.assertEqual(c.employers['0'],old)
        c.factory_wages[str(other)]=200
        next_recruitment(c,1)
        self.assertEqual(c.employers['0'],other)

    def test_E_net_wages_are_real_and_tax_reconciles(self):
        c=arena();c.income_tax_rate=30
        before=c.accounts.copy();c.production()
        contracts=c.current['labor_contract'];self.assertTrue(contracts)
        for x in contracts:
            h=f'h{x["resident"]}';self.assertEqual(c.accounts[h]-before[h],x['net'])
            self.assertEqual(x['gross']-x['gross']*30//100,x['net'])
        self.assertEqual(c.accounts['civic']-before['civic'],sum(x['gross']-x['net'] for x in contracts))
        self.assertEqual(c.labor['manufacturing']*4,sum(p.production for p in c.producers))

    def test_low_wage_time_exhaustion_and_disconnection_can_all_prevent_hiring(self):
        for reason in ('not_worthwhile','time_limit','unreachable'):
            c=arena((10,10),0 if reason=='time_limit' else 2)
            if reason=='unreachable':c.roads={};c.paths={}
            c.production()
            self.assertFalse(c.employers)
            self.assertEqual(sum(p.production for p in c.producers),0)
            self.assertTrue(any(x['status']==reason for x in c.labor_result['choices']['0']['candidates']))

    def test_no_cash_does_not_post_free_jobs(self):
        c=arena();c.accounts['property']+=c.accounts['p0'];c.accounts['p0']=0
        c.production();self.assertEqual(c.producers[0].production,0)
        self.assertEqual(c.labor_result['firms']['0']['posted_shifts'],0)

    def test_no_employer_or_proposal_iteration_privilege(self):
        c=arena(workers=8)
        posts,_=build_posts(c,{0:32,1:32,2:0,3:0})
        a,choices=match(c,posts)
        for postings,workers in [(list(reversed(posts)),list(range(8))),(posts,list(reversed(range(8)))),(list(reversed(posts)),list(reversed(range(8))))]:
            b,other=match(c,postings,worker_order=workers)
            self.assertEqual(a,b);self.assertEqual(choices,other)
        c.producers=list(reversed(c.producers))
        reverse_posts,_=build_posts(c,{0:32,1:32,2:0,3:0})
        self.assertEqual(posts,reverse_posts)

    def test_full_city_resume_profile_and_remaining_casual_hours(self):
        c=LandCity(7);c.labor_mode='market';c.factory_wages={'0':100,'1':150,'2':125,'3':100};c.income_tax_rate=10
        for _ in range(6):c.step()
        d=LandCity.restore(json.loads(json.dumps(c.snapshot())))
        for _ in range(4):
            self.assertEqual(c.step(),d.step())
            self.assertEqual(c.snapshot(),d.snapshot())
            self.assertTrue(all(h.work+c.commute.get(h.id,0)<=8+1e-6 for h in c.residents))
            self.assertEqual(c.metrics[-1]['cash_error'],0)
            self.assertEqual(c.metrics[-1]['stock_error_food'],0)
            self.assertEqual(c.metrics[-1]['stock_error_goods'],0)
        session=LandSession(c,d)
        for h in c.residents:
            profile=session.inspect('resident',h.id)['profile']
            self.assertAlmostEqual(sum(j['hours'] for j in profile['jobs']),h.work)
            self.assertEqual(sum(j['gross_wage'] for j in profile['jobs']),profile['income']['wages'])
        session=LandSession.load(json.loads(json.dumps(session.dump())))
        self.assertEqual(session.player.employers,c.employers)
        self.assertEqual(session.inspect('producer',0)['recruitment'],c.labor_result['firms']['0'])

    def test_old_save_remains_pool_and_does_not_invent_old_jobs(self):
        c=LandCity(1);c.step();snapshot=c.snapshot();snapshot['_land'].pop('employment')
        restored=LandCity.restore(snapshot)
        self.assertEqual(restored.labor_mode,'pool');self.assertFalse(restored.employers)
        self.assertEqual(restored.current,c.current)
        for _ in range(2):self.assertEqual(c.step(),restored.step())

    def test_full_market_reversing_post_and_worker_iteration_preserves_every_record(self):
        c=LandCity(1);c.income_tax_rate=10
        for _ in range(6):c.step()
        c.labor_mode='market';d=copy.deepcopy(c)
        original=match
        def reverse(city,posts):
            return original(city,list(reversed(posts)),worker_order=[h.id for h in reversed(city.residents)])
        for _ in range(6):
            c.step()
            with patch.object(labor,'match',side_effect=reverse):d.step()
            self.assertEqual(c.snapshot(),d.snapshot())

    def test_experimental_preset_is_explicit_and_survives_web_restore(self):
        session=LandSession.new(1,'labor')
        self.assertEqual(session.player.labor_mode,'market')
        self.assertEqual(session.player.snapshot(),session.reference.snapshot())
        restored=LandSession.load(json.loads(json.dumps(session.dump())))
        session.advance(1);restored.advance(1)
        self.assertEqual(session.dump(),restored.dump())

    def test_factory_site_cost_uses_its_own_production_wage(self):
        c=LandCity(1);c.labor_mode='market';p=c.producers[0]
        low=c.factory_score(p,p.node)
        c.factory_wages['0']=150
        high=c.factory_score(p,p.node)
        self.assertAlmostEqual(low['profit']-high['profit'],low['volume']*12.5)

    def test_invalid_labor_configuration_rejected(self):
        c=LandCity(1)
        for value in (True,0,-1,float('inf')):
            s=c.snapshot();s['_land']['employment']['wages']['0']=value
            with self.assertRaises(ValueError):LandCity.restore(s)
