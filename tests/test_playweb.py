import copy
import json
import tempfile
import threading
import unittest
import urllib.error
import urllib.request
from pathlib import Path

from commerce.model import City, Settings
from playweb.engine import PlayCity, Session
from playweb.server import Application, Server


class PlayerEngineTests(unittest.TestCase):
    def test_observation_hooks_do_not_change_existing_economy(self):
        a=City(7,Settings(households=24))
        b=PlayCity(7,Settings(households=24))
        for _ in range(18):
            self.assertEqual(a.step(),b.step())
        self.assertEqual(a.snapshot(),City.snapshot(b))
        self.assertTrue(b.current.get('purchase'))

    def test_no_intervention_control_is_exact(self):
        s=Session.new(7,'sparse')
        s.advance(12)
        self.assertEqual(s.player.snapshot(),s.reference.snapshot())

    def test_policy_changes_conditions_not_results_or_money(self):
        s=Session.new(1)
        metrics=copy.deepcopy(s.player.metrics)
        accounts=s.player.accounts.copy()
        s.intervene('permit_large',8,'希望少跑腿')
        self.assertEqual(metrics,s.player.metrics)
        self.assertEqual(accounts,s.player.accounts)
        self.assertEqual(s.points,6)
        self.assertIn(4,s.player.legal_areas(8))
        self.assertNotIn(4,s.reference.legal_areas(8))
        s.intervene('access')
        s.advance(3)
        self.assertNotEqual(s.player.metrics,s.reference.metrics)
        self.assertEqual(s.player.initial_money,sum(s.player.accounts.values()))

    def test_budget_duplicates_and_invalid_actions_do_not_apply(self):
        s=Session.new(1,'sparse')
        s.intervene('freight')
        saved=s.dump()
        for action in ('freight','access','set_gdp'):
            with self.assertRaises(ValueError): s.intervene(action)
            self.assertEqual(saved,s.dump())

    def test_snapshot_json_roundtrip_preserves_both_worlds_and_future(self):
        a=Session.new(2,'sparse')
        a.intervene('assortment')
        a.advance(4)
        b=Session.load(json.loads(json.dumps(a.dump())))
        a.advance(4);b.advance(4)
        self.assertEqual(a.dump(),b.dump())

    def test_purchase_trace_matches_real_consumption_and_payments(self):
        city=PlayCity(4,Settings(households=24))
        for _ in range(8): city.step()
        purchases=city.current['purchase']
        self.assertEqual(sum(p['quantity'] for p in purchases if p['good']=='food'),sum(h.food for h in city.residents))
        self.assertEqual(sum(p['cents'] for p in purchases),city.consumer_spend)
        self.assertEqual(sum(p['cents'] for p in city.current['payments'] if p['purpose'] in ('retail_sale','direct_sale')),city.consumer_spend)

    def test_anchor_does_not_refund_or_rewrite_history(self):
        s=Session.new(4,'sparse');s.intervene('freight');s.advance(2)
        s.anchor()
        self.assertEqual(s.points,3)
        self.assertEqual(s.player.snapshot(),s.reference.snapshot())
        self.assertEqual(s.fork_month,s.player.month)

    def test_load_legacy_commerce_and_reject_invalid_comparison(self):
        legacy=City(1,Settings(households=24));legacy.step()
        s=Session.load(legacy.snapshot())
        self.assertEqual(City.snapshot(s.player),legacy.snapshot())
        bad=s.dump();bad['reference']['residents'][0]['node']=(bad['reference']['residents'][0]['node']+1)%35
        with self.assertRaises(ValueError): Session.load(bad)


class ServerTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.directory=tempfile.TemporaryDirectory()
        cls.app=Application(Path(cls.directory.name)/'save.json',Session.new(9,'sparse'))
        cls.server=Server(('127.0.0.1',0),cls.app)
        cls.thread=threading.Thread(target=cls.server.serve_forever,daemon=True);cls.thread.start()
        cls.url=f'http://127.0.0.1:{cls.server.server_port}'

    @classmethod
    def tearDownClass(cls):
        cls.server.shutdown();cls.server.server_close();cls.thread.join();cls.directory.cleanup()

    def api(self,path,body=None,origin=None):
        headers={'Content-Type':'application/json'}
        if origin is not None: headers['Origin']=origin
        request=urllib.request.Request(self.url+path,data=json.dumps(body).encode() if body is not None else None,headers=headers)
        try:
            with urllib.request.urlopen(request) as response:
                return response.status,response.read()
        except urllib.error.HTTPError as error:
            return error.code,error.read()

    def test_real_http_step_and_save_restore(self):
        _,body=self.api('/api/state');before=json.loads(body)
        code,body=self.api('/api/advance',{'revision':before['revision'],'count':1})
        self.assertEqual(code,200)
        after=json.loads(body);self.assertEqual(after['month'],before['month']+1)
        self.assertTrue((Path(self.directory.name)/'save.json').exists())
        _,saved=self.api('/api/save');copy_session=Session.load(json.loads(saved))
        self.assertEqual(copy_session.player.month,after['month'])

    def test_reject_direct_metric_writes_stale_updates_and_cross_origin(self):
        _,body=self.api('/api/state');state=json.loads(body)
        code,_=self.api('/api/action',{'revision':state['revision'],'action':'access','food_coverage':1})
        self.assertEqual(code,400)
        code,_=self.api('/api/advance',{'revision':-1,'count':1});self.assertEqual(code,409)
        code,_=self.api('/api/advance',{'revision':state['revision'],'count':1},'https://example.org');self.assertEqual(code,403)
        _,body=self.api('/api/state');self.assertEqual(json.loads(body)['revision'],state['revision'])

    def test_invalid_save_does_not_overwrite_current_city(self):
        _,body=self.api('/api/state');state=json.loads(body)
        code,_=self.api('/api/load',{'revision':state['revision'],'save':{'schema':1}})
        self.assertEqual(code,400)
        _,body=self.api('/api/state');self.assertEqual(json.loads(body),state)

    def test_static_and_no_arbitrary_workspace_access(self):
        code,body=self.api('/');self.assertEqual(code,200);self.assertIn('规划试验桌'.encode(),body)
        code,_=self.api('/../README.md');self.assertEqual(code,404)
        code,_=self.api('/api/inspect?kind=resident&id=0');self.assertEqual(code,200)


if __name__=='__main__': unittest.main()
