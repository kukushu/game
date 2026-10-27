from __future__ import annotations

import copy
import math
from dataclasses import asdict

from commerce.model import City, Settings, GOODS

MAX_MONTH = 240
PRESETS = {
    'neighborhood': {'zoning': 'small_only'},
    'sparse': {'zoning': 'small_only', 'households': 32, 'residential': 'dispersed'},
    'easy_access': {'zoning': 'small_only', 'customer_edge_hours': .02},
}
ACTION_COSTS = {'permit_large': 2, 'permit_small': 1, 'freight': 5, 'access': 5, 'assortment': 3}
ACTION_NAMES = {'permit_large': '允许综合大店', 'permit_small': '仅许可新小店',
                'freight': '试行货运优先走廊', 'access': '改善居民出行', 'assortment': '放宽新小店品类'}


class PlayCity(City):
    """Read-only traces + explicit parcel permits. Unchanged rules without permits."""

    def __init__(self, seed=1, settings=None, trace=False):
        self.permits = {}
        self.traces = []
        self.current = {}
        super().__init__(seed, settings, trace)

    def legal_areas(self, node):
        if node in self.permits:
            return [1, 4] if self.permits[node] else [1]
        return super().legal_areas(node)

    def observe(self, event, **details):
        if not self.current:
            return
        record = copy.deepcopy(details)
        if 'path' in record:
            record['path'] = [list(edge) for edge in record['path']]
        if event == 'shopping_plan':
            record['cash_before'] = self.accounts[f"h{record['resident']}"]
            record['road_edges'] = [list(edge) for a,b in record['path'] for edge in self.route(a,b)[1]]
        if event == 'shopping_blocked':
            h = self.residents[record['resident']]
            trips = [2*self.route(h.node,n)[0]+.06 for account,n,prices in self.offers()
                     if any(self.seller_stock(account,g) for g in prices) and self.seller_service(account)>=.08]
            record['shortest_available_roundtrip'] = min(trips) if trips else None
        self.current.setdefault(event, []).append(record)

    def pay(self, source, target, amount, purpose):
        super().pay(source, target, amount, purpose)
        if amount and self.current:
            self.current.setdefault('payments', []).append({'source':source,'target':target,'cents':amount,'purpose':purpose})

    def production(self):
        before = {p.id:{'stock_before':p.stock,'previous_sales':p.sold,'previous_expected':p.expected} for p in self.producers}
        super().production()
        for p in self.producers:
            self.observe('production', producer=p.id, **before[p.id], expected=p.expected,
                         produced=p.production, stock_after=p.stock, counter_hours=p.service)

    def step(self):
        self.current = {'month':self.month}
        old_events = len(self.events)
        row = super().step()
        self.current['residents'] = [asdict(h) for h in self.residents]
        self.current['new_events'] = copy.deepcopy(self.events[old_events:])
        self.traces.append(self.current)
        self.traces = self.traces[-12:]
        return row

    def snapshot(self):
        snapshot = super().snapshot()
        snapshot['_play'] = copy.deepcopy({'version':1,'permits':self.permits,'traces':self.traces})
        return snapshot

    @classmethod
    def restore(cls, data, trace=False):
        model = super().restore(data, trace)
        extra = data.get('_play', {})
        if extra.get('version',1) != 1:
            raise ValueError('不支持的网页观测版本')
        model.permits = {int(k):v for k,v in extra.get('permits',{}).items()}
        if any(not 0<=k<35 or not isinstance(v,bool) for k,v in model.permits.items()):
            raise ValueError('地块许可无效')
        model.traces = copy.deepcopy(extra.get('traces',[]))[-12:]
        model.current = model.traces[-1] if model.traces else {}
        return model


def validate_snapshot(data):
    if not isinstance(data,dict) or data.get('schema') != 1:
        raise ValueError('请选择商业模型的有效快照')
    n = data.get('settings',{}).get('households')
    month = data.get('month')
    seed = data.get('seed')
    if not isinstance(seed,int) or isinstance(seed,bool) or not 0<=seed<2**31:
        raise ValueError('快照种子无效')
    if not isinstance(n,int) or isinstance(n,bool) or not 8<=n<=240:
        raise ValueError('网页快照需包含 8–240 个家庭')
    if not isinstance(month,int) or isinstance(month,bool) or not 0<=month<=MAX_MONTH:
        raise ValueError(f'网页原型支持 0–{MAX_MONTH} 期')
    for key,maximum in [('residents',240),('producers',4),('stores',500),('metrics',MAX_MONTH),('observations',20000)]:
        if not isinstance(data.get(key),list) or len(data[key])>maximum:
            raise ValueError(f'快照字段 {key} 过大或无效')
    if len(data['residents']) != n or len(data['producers']) != 4 or len(data['metrics']) != month:
        raise ValueError('快照的主体数或历史长度不一致')
    for i,h in enumerate(data['residents']):
        if h.get('id')!=i or not isinstance(h.get('node'),int) or not 0<=h['node']<35:
            raise ValueError('家庭编号或位置无效')
    for i,p in enumerate(data['producers']):
        if p.get('id')!=i or p.get('good') not in GOODS or not isinstance(p.get('node'),int) or not 0<=p['node']<35:
            raise ValueError('工厂编号、位置或商品无效')
        if not isinstance(p.get('price'),int) or p['price']<=0 or not isinstance(p.get('stock'),int) or p['stock']<0:
            raise ValueError('工厂价格或库存无效')
    for i,s in enumerate(data['stores']):
        if s.get('id')!=i or s.get('area') not in (1,4) or not isinstance(s.get('node'),int) or not 0<=s['node']<35:
            raise ValueError('商店编号、面积或位置无效')
        if not s.get('assortment') or len(set(s['assortment']))!=len(s['assortment']) or not set(s['assortment'])<=set(GOODS):
            raise ValueError('商店品类无效')
        for good in s['assortment']:
            if not isinstance(s.get('prices',{}).get(good),int) or s['prices'][good]<=0:
                raise ValueError('商店报价无效')
    accounts = data.get('accounts',{})
    required = {'property','fund'} | {f'h{i}' for i in range(n)} | {f'p{i}' for i in range(4)} | {f'r{i}' for i in range(len(data['stores']))}
    if set(accounts)!=required or any(not isinstance(v,int) or isinstance(v,bool) or not 0<=v<=10**12 for v in accounts.values()):
        raise ValueError('快照账户无效')
    def finite(value, depth=0):
        if depth>24:
            raise ValueError('快照嵌套过深')
        if isinstance(value,float) and not math.isfinite(value):
            raise ValueError('快照包含非有限数值')
        if isinstance(value,dict):
            for item in value.values(): finite(item,depth+1)
        elif isinstance(value,list):
            for item in value: finite(item,depth+1)
    finite(data)
    return PlayCity.restore(data)


class Session:
    def __init__(self, player, reference=None, points=8, actions=None, fork_month=None, preset='imported'):
        self.player = player
        self.reference = reference or PlayCity.restore(player.snapshot())
        self.points = points
        self.actions = actions or []
        self.fork_month = player.month if fork_month is None else fork_month
        self.preset = preset

    @classmethod
    def new(cls, seed=1, preset='neighborhood'):
        if not isinstance(seed,int) or isinstance(seed,bool) or not 0<=seed<2**31 or preset not in PRESETS:
            raise ValueError('种子或城市条件无效')
        player = PlayCity(seed, Settings(**PRESETS[preset]))
        for _ in range(6): player.step()
        return cls(player,preset=preset)

    def dump(self):
        return {'app':'CityEconomyLab Play','version':1,'player':self.player.snapshot(),'reference':self.reference.snapshot(),
                'points':self.points,'actions':copy.deepcopy(self.actions),'fork_month':self.fork_month,'preset':self.preset}

    @classmethod
    def load(cls, data):
        if data.get('schema')==1:
            return cls(validate_snapshot(data))
        if data.get('app')!='CityEconomyLab Play' or data.get('version')!=1:
            raise ValueError('这不是本原型的存档，也不是商业实验快照')
        player,reference = validate_snapshot(data['player']),validate_snapshot(data['reference'])
        if player.month != reference.month or player.seed!=reference.seed:
            raise ValueError('对照的种子和时间必须一致')
        if [(h.id,h.node,h.value_time) for h in player.residents] != [(h.id,h.node,h.value_time) for h in reference.residents]:
            raise ValueError('对照必须保留相同家庭，才能比较家庭变化')
        points,actions,fork = data.get('points'),data.get('actions'),data.get('fork_month')
        if not isinstance(points,int) or isinstance(points,bool) or not 0<=points<=8:
            raise ValueError('行动额度无效')
        if not isinstance(actions,list) or len(actions)>100 or not isinstance(fork,int) or not 0<=fork<=player.month:
            raise ValueError('行动记录或对照起点无效')
        for action in actions:
            if not isinstance(action,dict) or action.get('action') not in ACTION_COSTS or not isinstance(action.get('month'),int) or not 0<=action['month']<=player.month:
                raise ValueError('行动记录无效')
        return cls(player,reference,points,copy.deepcopy(actions),fork,str(data.get('preset','imported'))[:40])

    def intervene(self, action, node=None, note=''):
        if action not in ACTION_COSTS:
            raise ValueError('不支持这项规划；不能直接写入经济指标')
        cost = ACTION_COSTS[action]
        if self.points<cost:
            raise ValueError('行动额度不足；可以先观察，或导出存档后另开实验')
        city = self.player
        if action.startswith('permit_'):
            if not isinstance(node,int) or isinstance(node,bool) or not 0<=node<35:
                raise ValueError('请先选择地图上的一个地块')
            target = action=='permit_large'
            if (4 in city.legal_areas(node))==target:
                raise ValueError('该地块已经是这个许可状态，没有扣除额度')
            city.permits[node] = target
        elif action=='freight':
            if city.settings.corridor: raise ValueError('货运优先走廊已经启用')
            city.settings.corridor=True
        elif action=='access':
            if any(a['action']=='access' for a in self.actions): raise ValueError('本次实验已改善居民出行')
            city.settings.customer_edge_hours *= .65
        elif action=='assortment':
            if city.settings.small_multigood: raise ValueError('新小店已经允许兼售两类商品')
            city.settings.small_multigood=True
        city.paths = {}
        self.points-=cost
        self.actions.append({'month':city.month,'action':action,'name':ACTION_NAMES[action],
                             'node':node if action.startswith('permit_') else None,'cost':cost,'note':str(note)[:240]})

    def advance(self, count):
        if not isinstance(count,int) or isinstance(count,bool) or not 1<=count<=12 or self.player.month+count>MAX_MONTH:
            raise ValueError(f'每次推进 1–12 期，实验最多到第 {MAX_MONTH} 期')
        for _ in range(count):
            self.player.step()
            self.reference.step()

    def anchor(self):
        self.reference=PlayCity.restore(self.player.snapshot())
        self.fork_month=self.player.month

    def world(self,name):
        if name not in ('player','reference'): raise ValueError('城市视图无效')
        return self.player if name=='player' else self.reference

    @staticmethod
    def metrics(city):
        return city.metrics[-1] if city.metrics else None

    def view(self):
        def world(city):
            nodes=[]
            for node in range(35):
                residents=[h for h in city.residents if h.node==node]
                stores=[s for s in city.stores if s.active and s.node==node]
                nodes.append({'id':node,'residents':len(residents),'large_allowed':4 in city.legal_areas(node),
                              'stores':[{'id':s.id,'area':s.area} for s in stores],
                              'producers':[{'id':p.id,'good':p.good} for p in city.producers if p.node==node],
                              'shopping':sum(h.shopping for h in residents)/max(1,len(residents)),
                              'missing':sum(4-h.food-h.goods for h in residents)})
            return {'nodes':nodes,'settings':asdict(city.settings),'metrics':self.metrics(city),
                    'history':city.metrics[-36:],'traffic':[[a,b,v] for (a,b),v in city.previous_traffic.items()],
                    'residents':[{'id':h.id,'node':h.node,'shopping':h.shopping,'food':h.food,'goods':h.goods} for h in city.residents],
                    'stores':[{'id':s.id,'node':s.node,'area':s.area,'cash':city.accounts[f'r{s.id}'],
                               'profit':s.last_profit,'sales':s.sales,'age':s.age,'active':s.active} for s in city.stores],
                    'events':city.events[-12:]}
        elapsed=self.player.month-self.fork_month
        window=min(6,elapsed)
        comparisons={}
        for key in ('food_coverage','goods_coverage','paid_price','shopping_hours','hours_per_unit','retail_share'):
            a=self.player.metrics[-window:] if window else []
            b=self.reference.metrics[-window:] if window else []
            comparisons[key]={'player':sum(r[key] for r in a)/len(a),'reference':sum(r[key] for r in b)/len(b)} if window else None
        examples=[]
        for h,ref in zip(self.player.residents,self.reference.residents):
            score=abs(h.shopping-ref.shopping)+abs(h.food-ref.food)+abs(h.goods-ref.goods)
            if score>1e-8:
                examples.append({'id':h.id,'node':h.node,'score':score,'player':asdict(h),'reference':asdict(ref)})
        examples.sort(key=lambda h:-h['score'])
        hot=[h for h in self.player.residents if h.food<2 or h.goods<2]
        hot.sort(key=lambda h:(h.food+h.goods,-h.shopping))
        return {'month':self.player.month,'seed':self.player.seed,'preset':self.preset,'points':self.points,
                'fork_month':self.fork_month,'comparison_window':window,'comparisons':comparisons,
                'examples':examples[:4],'households_needing_attention':[asdict(h) for h in hot[:4]],
                'actions':self.actions,'worlds':{'player':world(self.player),'reference':world(self.reference)}}

    def inspect(self, kind, identity, world='player'):
        city=self.world(world)
        if not isinstance(identity,int) or identity<0: raise ValueError('主体编号无效')
        current=city.current
        if kind=='node':
            if identity>=35: raise ValueError('地块编号无效')
            return {'kind':kind,'id':identity,'large_allowed':4 in city.legal_areas(identity),
                'rent_small':city.rent(identity,1),'rent_large':city.rent(identity,4),
                'residents':[asdict(h) for h in city.residents if h.node==identity],
                'stores':[asdict(s) for s in city.stores if s.node==identity and s.active],
                'producers':[asdict(p) for p in city.producers if p.node==identity],
                'estimates':[e for e in current.get('site_estimate',[]) if e['node']==identity],
                'routes':[]}
        if kind=='resident':
            if identity>=len(city.residents): raise ValueError('家庭编号无效')
            h=city.residents[identity]
            plans=[p for p in current.get('shopping_plan',[]) if p['resident']==identity]
            blocked=[p for p in current.get('shopping_blocked',[]) if p['resident']==identity]
            purchases=[p for p in current.get('purchase',[]) if p['resident']==identity]
            history=[]
            for trace in city.traces[-6:]:
                if identity<len(trace.get('residents',[])):
                    history.append({'month':trace['month'],**trace['residents'][identity]})
            other=self.reference if world=='player' else self.player
            other_plans=[p for p in other.current.get('shopping_plan',[]) if p['resident']==identity]
            return {'kind':kind,'entity':asdict(h),'cash':city.accounts[f'h{identity}'],'plans':plans,'blocked':blocked,
                'purchases':purchases,'payments':[p for p in current.get('payments',[]) if p['source']==f'h{identity}' or p['target']==f'h{identity}'],
                'history':history,'routes':[{'mode':'customer','edges':p['road_edges']} for p in plans],
                'other':{'entity':asdict(other.residents[identity]),'purchases':[p for p in other.current.get('purchase',[]) if p['resident']==identity],
                         'routes':[{'mode':'reference','edges':p['road_edges']} for p in other_plans]}}
        if kind=='store':
            if identity>=len(city.stores): raise ValueError('商店编号无效')
            s=city.stores[identity]
            shipments=[p for p in current.get('shipment',[]) if p['store']==identity]
            return {'kind':kind,'entity':asdict(s),'cash':city.accounts[f'r{identity}'],'rent':city.rent(s.node,s.area)+100,
                'staff_hours':city.staff_hours(s.area),'capacity':s.capacity,
                'history':[x for x in city.observations if x['store']==identity][-8:],
                'events':[e for e in city.events if e['store']==identity],
                'shipments':shipments,'purchases':[p for p in current.get('purchase',[]) if p['seller']==f'r{identity}'],
                'payments':[p for p in current.get('payments',[]) if p['source']==f'r{identity}' or p['target']==f'r{identity}'],
                'routes':[{'mode':'truck','edges':p['path']} for p in shipments]}
        if kind=='producer':
            if identity>=len(city.producers): raise ValueError('工厂编号无效')
            p=city.producers[identity]
            shipments=[s for s in current.get('shipment',[]) if s['supplier']==identity]
            return {'kind':kind,'entity':asdict(p),'cash':city.accounts[f'p{identity}'],
                'production':[x for x in current.get('production',[]) if x['producer']==identity],
                'shipments':shipments,'purchases':[x for x in current.get('purchase',[]) if x['seller']==f'p{identity}'],
                'payments':[x for x in current.get('payments',[]) if x['source']==f'p{identity}' or x['target']==f'p{identity}'],
                'routes':[{'mode':'truck','edges':s['path']} for s in shipments]}
        raise ValueError('不支持的检查对象')
