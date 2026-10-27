from __future__ import annotations

import copy
import math

from playweb.engine import Session, validate_snapshot
from .model import LandCity, ZONES, RIGHTS

MODES={'adaptive':'自主选址','fixed':'固定位置','blind':'只看许可随机选址'}


def validate(data):
    if not isinstance(data,dict) or '_land' not in data: raise ValueError('请导入土地实验存档；旧商业快照请在旧界面打开')
    # Reuse base shape/numeric/account validation, then validate the new account and spatial data.
    core=copy.deepcopy(data)
    civic=core['accounts'].pop('civic')
    if not isinstance(civic,int) or isinstance(civic,bool) or not 0<=civic<=10**12: raise ValueError('公共预算无效')
    core['initial_money']-=civic
    core.pop('_land')
    validate_snapshot(core)
    def finite(value,depth=0):
        if depth>24: raise ValueError('空间存档嵌套过深')
        if isinstance(value,float) and not math.isfinite(value): raise ValueError('空间存档包含非有限数值')
        if isinstance(value,dict):
            for x in value.values(): finite(x,depth+1)
        elif isinstance(value,list):
            for x in value: finite(x,depth+1)
    finite(data['_land'])
    model=LandCity.restore(data)
    if len(model.spatial_history)!=model.month: raise ValueError('空间历史长度无效')
    return model


class LandSession(Session):
    @classmethod
    def new(cls,seed=1,preset='adaptive'):
        if not isinstance(seed,int) or isinstance(seed,bool) or not 0<=seed<2**31 or preset not in MODES:
            raise ValueError('种子或选址机制无效')
        city=LandCity(seed)
        city.mode=preset
        for _ in range(6): city.step()
        return cls(city,LandCity.restore(city.snapshot()),preset=preset)

    def dump(self):
        return {'app':'CityEconomyLab Land','version':1,'player':self.player.snapshot(),'reference':self.reference.snapshot(),
                'actions':copy.deepcopy(self.actions),'fork_month':self.fork_month,'preset':self.preset}

    @classmethod
    def load(cls,data):
        if data.get('app')!='CityEconomyLab Land' or data.get('version')!=1: raise ValueError('这不是土地实验存档')
        a,b=validate(data['player']),validate(data['reference'])
        if a.seed!=b.seed or a.month!=b.month or a.mode!=b.mode: raise ValueError('对照的种子、时间与选址机制必须相同')
        if [(h.id,h.value_time) for h in a.residents]!=[(h.id,h.value_time) for h in b.residents]: raise ValueError('对照家庭身份不一致')
        actions,fork=data.get('actions'),data.get('fork_month')
        if not isinstance(actions,list) or len(actions)>500 or not isinstance(fork,int) or not 0<=fork<=a.month: raise ValueError('规划记录无效')
        if any(not isinstance(x,dict) or x.get('action') not in ('zone','road') or not isinstance(x.get('month'),int) or not 0<=x['month']<=a.month for x in actions): raise ValueError('规划记录无效')
        return cls(a,b,actions=copy.deepcopy(actions),fork_month=fork,preset=a.mode)

    def anchor(self):
        self.reference=LandCity.restore(self.player.snapshot());self.fork_month=self.player.month

    def intervene(self,action,node=None,note=''):
        if action not in ('zone','road') or not isinstance(node,dict): raise ValueError('仅支持规划用途或修建道路；不能直接放置主体')
        city=self.player
        identity=node.get('id')
        if not isinstance(identity,int) or isinstance(identity,bool) or not 0<=identity<35: raise ValueError('区域无效')
        if action=='zone':
            zone=node.get('zone')
            if zone not in ZONES: raise ValueError('用途无效')
            if city.zones[identity]==zone: raise ValueError('用途没有变化，未扣款')
            cost=250
            name=f'区域 {identity} 规划为{ZONES[zone]}'
        else:
            neighbor=node.get('neighbor')
            if not isinstance(neighbor,int) or isinstance(neighbor,bool) or not 0<=neighbor<35 or city.manhattan(identity,neighbor)!=1:
                raise ValueError('请选择一个相邻区域')
            edge=tuple(sorted((identity,neighbor)))
            level=city.roads.get(edge,0)
            if level==2: raise ValueError('道路已经改良，未扣款')
            cost=1200 if level==0 else 900
            name=f'{"新建连接" if level==0 else "改良道路"} {edge[0]} ↔ {edge[1]}'
        if city.accounts['civic']<cost: raise ValueError('公共预算不足；原城市未改变')
        city.pay('civic','property',cost,'planning_admin' if action=='zone' else 'road_build')
        if action=='zone': city.zones[identity]=zone
        else: city.roads[edge]=level+1
        city.paths={}
        self.actions.append({'month':city.month,'action':action,'node':copy.deepcopy(node),'name':name,'cost':cost,'note':str(note)[:240]})

    def view(self):
        result=super().view()
        result['mode']=self.player.mode
        for name,city in [('player',self.player),('reference',self.reference)]:
            world=result['worlds'][name]
            world['roads']=[[a,b,v] for (a,b),v in city.roads.items()]
            world['space']=copy.deepcopy(city.spatial_history[-36:])
            world['spatial_events']=copy.deepcopy(city.spatial_events)
            world['spatial_log']=copy.deepcopy(city.spatial_log[-12:])
            world['civic']=city.accounts['civic']/100
            world['maintenance_ratio']=city.maintenance_ratio
            world['reviews']={'home':len(city.home_reviews),'factory':len(city.factory_reviews)}
            for h in world['residents']:
                h['commute']=city.commute.get(h['id'],0)
            for n in world['nodes']:
                identity=n['id'];n.update(city.usage(identity))
                n.update({'zone':city.zones[identity],'free':city.free_land(identity),'exposure':city.exposure(identity),
                          'home_rent':city.home_rent(identity)/100})
            world['home_reviews']=copy.deepcopy(city.home_reviews)
            world['factory_reviews']=copy.deepcopy(city.factory_reviews)
        window=result['comparison_window']
        result['spatial_comparisons']={}
        for key in ('exposure','commute','occupied_home_nodes','moves','factory_moves'):
            result['spatial_comparisons'][key]={name:sum(x[key] for x in city.spatial_history[-window:])/window for name,city in [('player',self.player),('reference',self.reference)]} if window else None
        return result

    def inspect(self,kind,identity,world='player'):
        result=super().inspect(kind,identity,world)
        city=self.world(world)
        if kind=='node':
            result.update({'zone':city.zones[identity],'usage':city.usage(identity),'free':city.free_land(identity),
                           'exposure':city.exposure(identity),'home_rent':city.home_rent(identity),
                           'home_appraisals':sorted([{'resident':h.id,'origin':h.node,**city.home_score(h,identity)} for h in city.residents if h.node!=identity],key=lambda x:x['total'])[:3],
                           'home_eligible':'R' in RIGHTS[city.zones[identity]] and city.free_land(identity)>=(1 if city.usage(identity)['homes']%4==0 else 0),
                           'factory_eligible':'I' in RIGHTS[city.zones[identity]] and city.free_land(identity)>=4,
                           'factory_appraisals':[{'producer':p.id,**city.factory_score(p,identity),'current_profit':city.factory_score(p,p.node)['profit']} for p in city.producers],
                           'near_roads':[{'neighbor':n,'level':city.roads.get(tuple(sorted((identity,n))),0)} for n in range(35) if city.manhattan(identity,n)==1]})
        elif kind=='resident':
            result['commute']=city.commute.get(identity,0)
            result['appraisal']=city.home_score(city.residents[identity],city.residents[identity].node)
            result['reviews']=[r for r in city.home_review_history if r['resident']==identity][-3:]
            result['routes'] += [{'mode':'commute','edges':r['edges']} for r in city.current.get('commute_trip',[]) if r['resident']==identity]
        elif kind=='producer':
            result['reviews']=[r for r in city.factory_review_history if r['producer']==identity][-3:]
            result['appraisal']=city.factory_score(city.producers[identity],city.producers[identity].node)
        return result
