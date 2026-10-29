"""An empty blueprint, before any economic subjects are created.

Opening initializes the existing fixed cohort once; this is not a population-growth model.
"""
import copy
import heapq
import math

from .model import LandCity, RIGHTS, ZONES
from .layers import planning_layers


def route(roads,start,end):
    queue,best,parents=[(0.,start)],{start:0.},{}
    while queue:
        cost,node=heapq.heappop(queue)
        if cost!=best[node]: continue
        if node==end: break
        for (a,b),level in roads.items():
            other=b if a==node else a if b==node else None
            if other is None: continue
            value=cost+.18*(.55 if level==2 else 1)
            if value<best.get(other,math.inf):
                best[other]=value;parents[other]=(node,(a,b));heapq.heappush(queue,(value,other))
    if end not in best:return None,[]
    edges=[];node=end
    while node!=start:
        node,edge=parents[node];edges.append(list(edge))
    return best[end],list(reversed(edges))


class PlanningSession:
    def __init__(self,seed=1):
        self.seed=seed
        self.zones={n:'reserve' for n in range(35)}
        self.roads={}
        self.budget=30000
        self.actions=[]

    def dump(self):
        return {'app':'CityEconomyLab Blueprint','version':1,'seed':self.seed,'zones':copy.deepcopy(self.zones),
                'roads':[[a,b,v] for (a,b),v in self.roads.items()],'budget':self.budget,'actions':copy.deepcopy(self.actions)}

    @classmethod
    def load(cls,data):
        if data.get('app')!='CityEconomyLab Blueprint' or data.get('version')!=1 or not isinstance(data.get('seed'),int) or isinstance(data['seed'],bool) or not 0<=data['seed']<2**31:
            raise ValueError('规划存档无效')
        result=cls(data['seed'])
        result.zones={int(n):v for n,v in data['zones'].items()}
        if set(result.zones)!=set(range(35)) or any(v not in ZONES for v in result.zones.values()):raise ValueError('规划用途无效')
        for a,b,v in data['roads']:
            if not 0<=a<b<35 or LandCity.manhattan(a,b)!=1 or v not in (1,2):raise ValueError('规划道路无效')
            result.roads[a,b]=v
        result.budget=data['budget'];result.actions=copy.deepcopy(data['actions'])
        if not isinstance(result.budget,int) or not 0<=result.budget<=30000 or len(result.actions)>500:raise ValueError('规划预算无效')
        # Replay the paid blueprint so an imported plan cannot hide unpaid roads or permits.
        replay=cls(result.seed)
        for item in result.actions: replay.intervene(item['action'],item['node'],item.get('note',''))
        if replay.dump()!=result.dump():raise ValueError('规划记录与预算或地图不一致')
        return result

    def intervene(self,action,node=None,note=''):
        if not isinstance(node,dict):raise ValueError('请选择地图上的位置')
        identity=node.get('id')
        if not isinstance(identity,int) or isinstance(identity,bool) or not 0<=identity<35:raise ValueError('地块无效')
        if action=='zone':
            zone=node.get('zone')
            if zone not in ZONES:raise ValueError('用途无效')
            if self.zones[identity]==zone:raise ValueError('许可没有变化，未扣款')
            cost=250;name=f'区域 {identity} 规划为{ZONES[zone]}'
        elif action=='road':
            neighbor=node.get('neighbor')
            if not isinstance(neighbor,int) or isinstance(neighbor,bool) or not 0<=neighbor<35 or LandCity.manhattan(identity,neighbor)!=1:
                raise ValueError('只能连接相邻街区')
            edge=tuple(sorted((identity,neighbor)));level=self.roads.get(edge,0)
            if level==2:raise ValueError('道路已经改良，未扣款')
            cost=1200 if not level else 900;name=f'{"新建连接" if not level else "改良道路"} {edge[0]} ↔ {edge[1]}'
        else:raise ValueError('规划阶段只支持道路和用地许可')
        if self.budget<cost:raise ValueError('规划预算不足')
        self.budget-=cost
        if action=='zone':self.zones[identity]=zone
        else:self.roads[edge]=level+1
        self.actions.append({'month':0,'action':action,'node':copy.deepcopy(node),'name':name,'cost':cost,'note':str(note)[:240]})

    def layout(self):
        pending={n for edge in self.roads for n in edge};components=[]
        while pending:
            start=min(pending);component={start};stack=[start]
            while stack:
                n=stack.pop()
                for a,b in self.roads:
                    other=b if a==n else a if b==n else None
                    if other is not None and other not in component:component.add(other);stack.append(other)
            pending-=component;components.append(component)
        for component in sorted(components,key=lambda c:-len(c)):
            homes=[n for n in component if 'R' in RIGHTS[self.zones[n]]]
            used=dict.fromkeys(component,0);factories=[];factory_assessments=[]
            for _ in range(4):
                candidates=[]
                for node in component:
                    if 'I' not in RIGHTS[self.zones[node]] or used[node]+4>8:continue
                    capacity=sum((8-used[n]-(4 if n==node else 0))*4 for n in homes)
                    if capacity<64:continue
                    distance=sum(route(self.roads,node,n)[0] for n in homes)/max(1,len(homes))
                    candidates.append((distance,node))
                if not candidates:break
                distance,node=min(candidates)
                factory_assessments.append({'producer':len(factories),'node':node,'distance':distance,
                    'alternatives':[{'node':n,'distance':d} for d,n in sorted(candidates)]})
                used[node]+=4;factories.append(node)
            if len(factories)==4:return {'ready':True,'component':sorted(component),'factories':factories,'homes':homes,
                'factory_assessments':factory_assessments,'reason':'道路与用地已能容纳首批 64 户和 4 家工厂。'}
        if not self.roads:reason='先在地图上修路，再沿道路规划住宅和工业用地。'
        else:reason='同一片连通道路内，需要容纳 64 户的住宅土地和 4 家工厂的工业土地；可先连起两块住宅地、两块工业地。'
        return {'ready':False,'reason':reason}

    def launch(self):
        from .session import LandSession
        layout=self.layout()
        if not layout['ready']:raise ValueError(layout['reason'])
        city=LandCity(self.seed)
        city.greenfield=True;city.gateway=min(layout['component'])
        city.zones=self.zones.copy();city.roads=self.roads.copy();city.paths={}
        for h in city.residents:h.node=-1
        for p,node in zip(city.producers,layout['factories']):p.node=node
        city.initial_locations['factories']=copy.deepcopy(layout['factory_assessments'])
        for h in city.residents:
            candidates=[n for n in layout['homes'] if city.free_land(n)>=(1 if city.usage(n)['homes']%4==0 else 0)]
            scored=sorted([{'node':n,**city.home_score(h,n)} for n in candidates],key=lambda a:(a['total'],a['node']))
            h.node=scored[0]['node']
            city.initial_locations['homes'].append({'resident':h.id,'node':h.node,'alternatives':scored})
        spent=30000-self.budget
        city.accounts['civic']=self.budget;city.accounts['property']+=spent
        city.check()
        assert all(city.free_land(n)>=0 for n in range(35))
        return LandSession(city,LandCity.restore(city.snapshot()),actions=copy.deepcopy(self.actions),preset='adaptive')

    def view(self):
        def world(zones,roads,budget):
            return {'nodes':[{'id':n,'zone':zones[n],'residents':0,'homes':0,'housing_land':0,'commercial_land':0,'industrial_land':0,
                              'free':8,'stores':[],'producers':[],'large_allowed':False,'exposure':0,'home_rent':None,
                              'layer_data':planning_layers()} for n in range(35)],
                    'roads':[[a,b,v] for (a,b),v in roads.items()],'space':[],'residents':[],'producers':[],'stores':[],
                    'civic':budget/100,'metrics':None,'traffic':[],'history':[],'events':[],
                    'changes':{'shops':[],'moves':[]},'maintenance_ratio':1}
        return {'stage':'planning','month':0,'seed':self.seed,'mode':'blank','preset':'blank','fork_month':0,
                'actions':copy.deepcopy(self.actions),'readiness':self.layout(),'comparison_window':0,'comparisons':{},'spatial_comparisons':{},
                'player_feedback':{'periods':0,'commute_less':0,'commute_more':0,'new_road_users':0,'examples':[]},
                'worlds':{'player':world(self.zones,self.roads,self.budget),
                          'reference':world({n:'reserve' for n in range(35)},{},30000)}}

    def inspect(self,kind,identity,world='player'):
        if world!='player':raise ValueError('规划阶段没有经济对照，开放城市后开始对照')
        if kind=='road':
            a,b=divmod(identity,35)
            if not 0<=a<b<35 or LandCity.manhattan(a,b)!=1:raise ValueError('请选择相邻地块间的连接')
            level=self.roads.get((a,b),0);duration,path=route(self.roads,a,b)
            direct=.18*(.55 if level else 1)
            return {'kind':'road','id':identity,'a':a,'b':b,'level':level,'cost':1200 if not level else 900 if level==1 else None,
                    'duration_before':duration,'duration_with_connection':min(duration,direct) if duration is not None else direct,
                    'upkeep_after':8 if level else 4,'routes':[{'mode':'detour','edges':path}]}
        if kind!='node' or not 0<=identity<35:raise ValueError('空白场地还没有居民或企业')
        return {'kind':'node','id':identity,'zone':self.zones[identity],'free':8,
                'residents':[],'stores':[],'producers':[],'exposure':0,'home_rent':None,
                'usage':{'housing_land':0,'commercial_land':0,'industrial_land':0},'recent_estimates':[],'routes':[]}

    def advance(self,count):raise ValueError('先完成规划，再点“开放城市”；规划阶段没有经济主体运行')
    def anchor(self):raise ValueError('开放城市时会自动建立同状态对照')
