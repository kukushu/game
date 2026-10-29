from __future__ import annotations

import copy
import heapq
import math

from commerce.model import Settings
from playweb.engine import PlayCity
from .assessments import CommercialAudit

ZONES = {'reserve':'保留地', 'home':'住宅', 'shop':'商业', 'factory':'工业',
         'mixed':'住商混合', 'open':'自由混合'}
TAXABLE_INCOME = frozenset(('manufacturing','factory_service','retail_staff','freight',
                            'household_dividend','fund_dividend'))

RIGHTS = {'reserve':'', 'home':'R', 'shop':'C', 'factory':'I', 'mixed':'RC', 'open':'RCI'}


class LandCity(PlayCity):
    """Finite shared parcels; no forced eviction, demographic growth, or free buildings."""

    def __init__(self, seed=1, settings=None, trace=False):
        super().__init__(seed, settings or Settings(households=64, zoning='flexible'), trace)
        self.mode = 'adaptive'
        self.home_move_policy = 'stable'
        self.income_tax_rate = 0
        self.commercial_audit = CommercialAudit()
        self.commercial_last_review = {}
        self.initial_locations = {'homes': [], 'factories': []}
        self.greenfield, self.gateway = False, None
        self.environment_weight = 1.0
        self.zones = {n:'reserve' for n in range(35)}
        for h in self.residents: self.zones[h.node] = 'home'
        for n in (8,16,22): self.zones[n] = 'mixed'
        for n in (9,15,21): self.zones[n] = 'home'
        for n in (10,17,24): self.zones[n] = 'shop'
        for p in self.producers: self.zones[p.node] = 'factory'
        self.roads = {tuple(sorted((n,n+1))):1 for n in range(35) if n%7<6}
        self.roads.update({(n,n+7):1 for n in range(28) if n%7 in (0,6)})
        self.accounts['civic'] = 30000
        self.accounts['fund'] -= 30000
        self.jobs, self.commute, self.commute_visits = {}, {}, set()
        self.moved_home, self.moved_factory = {}, {}
        self.spatial_events, self.home_reviews, self.factory_reviews = [], [], []
        self.spatial_history = []
        self.spatial_log, self.home_review_history, self.factory_review_history = [], [], []
        self.maintenance_ratio = 1.0
        self.extra_maintenance = 0  # Offline cash-flow placebo; never exposed as a player action.
        self.preferences = {h.id:self.rng('environment_preference',h.id).uniform(100,350) for h in self.residents}

    def observe(self, event, **details):
        super().observe(event, **details)
        if event == 'site_estimate' and hasattr(self, 'commercial_audit'):
            self.commercial_audit.estimate(self, details)

    def pay(self, source, target, amount, purpose):
        super().pay(source, target, amount, purpose)
        rate=getattr(self,'income_tax_rate',0)
        if amount and rate and target.startswith('h') and purpose in TAXABLE_INCOME:
            # Gross employer/owner payment is unchanged; a real second transfer withholds tax.
            tax=amount*rate//100
            if tax:super().pay(target,'civic',tax,'income_tax')

    def distribute_dividends(self):
        """Temporarily suspend all four distributions; keep cash with its owner.

        Profit, inventory, expectations and exits are still settled by the base class.
        Historical payments are preserved when restoring earlier snapshots.
        """
        return None

    def fiscal_summary(self):
        payments=self.current.get('payments',[])
        gross=sum(p['cents'] for p in payments if p['target'].startswith('h') and p['purpose'] in TAXABLE_INCOME)
        tax=sum(p['cents'] for p in payments if p['purpose']=='income_tax')
        paid=sum(p['cents'] for p in payments if p['purpose']=='road_maintenance')
        return {'dividends_enabled':False,'tax_rate':self.income_tax_rate,'tax_income':tax,'gross_income':gross,
                'net_income':gross-tax,'upkeep_paid':paid,
                'next_upkeep_due':4*sum(self.roads.values())+self.extra_maintenance,
                'household_cash':sum(v for a,v in self.accounts.items() if a.startswith('h')),
                'maintenance_ratio':self.maintenance_ratio,'period':self.month}

    def usage(self, node):
        homes = sum(h.node==node for h in self.residents)
        return {'homes':homes,'housing_land':math.ceil(homes/4),
                'commercial_land':sum(s.area for s in self.stores if s.active and s.node==node),
                'industrial_land':4*sum(p.node==node for p in self.producers)}

    def free_land(self,node):
        u=self.usage(node)
        return 8-u['housing_land']-u['commercial_land']-u['industrial_land']

    def legal_areas(self,node):
        if not hasattr(self,'zones'): return super().legal_areas(node)
        areas=self.commercial_areas(node)
        if self.commercial_audit.active:self.commercial_audit.visit(self,node,areas)
        return areas

    def commercial_areas(self,node):
        """Pure eligibility check: screening a plot is not a site assessment."""
        return [a for a in (1,4) if 'C' in RIGHTS[self.zones[node]] and
                self.location_accessible(node) and self.free_land(node)>=a]

    def site_candidates(self):
        occupied={s.node for s in self.stores if s.active}
        eligible=[n for n in range(35) if n not in occupied and self.commercial_areas(n)]
        # Least recently assessed first. New/unseen candidates precede reviewed ones;
        # node ID breaks ties deterministically, independently of audit retention.
        selected=sorted(eligible,key=lambda n:(self.commercial_last_review.get(n,-1),n))[:8]
        for node in selected:self.commercial_last_review[node]=self.month
        self.commercial_audit.schedule(eligible,selected)
        return selected

    def location_accessible(self,node):
        return not self.greenfield or math.isfinite(self.route(self.gateway,node)[0])

    def route(self,start,end,mode='customer'):
        if not hasattr(self,'roads'): return super().route(start,end,mode)
        key=start,end,mode
        if key in self.paths: return self.paths[key]
        base=self.settings.customer_edge_hours if mode=='customer' else self.settings.truck_edge_hours
        queue,best,parents=[(0.,start)],{start:0.},{}
        while queue:
            cost,n=heapq.heappop(queue)
            if cost!=best[n]: continue
            if n==end: break
            for neighbor in (n-1,n+1,n-7,n+7):
                if not 0<=neighbor<35 or self.manhattan(n,neighbor)!=1: continue
                edge=tuple(sorted((n,neighbor)))
                if edge not in self.roads: continue
                duration=base*(.55 if self.roads[edge]==2 else 1)*(2-self.maintenance_ratio)*(1+self.settings.congestion*self.previous_traffic.get(edge,0)/12)
                if cost+duration<best.get(neighbor,math.inf)-1e-12:
                    best[neighbor]=cost+duration;parents[neighbor]=(n,edge)
                    heapq.heappush(queue,(cost+duration,neighbor))
        if end not in best: return math.inf,()
        edges,n=[],end
        while n!=start:
            n,edge=parents[n];edges.append(edge)
        self.paths[key]=best[end],tuple(reversed(edges))
        return self.paths[key]

    def home_rent(self,node):
        # A posted scarcity tariff, not a developer or bidding market.
        return 55+3*self.usage(node)['homes']+5*(8-self.free_land(node))

    def exposure(self,node):
        return sum(p.production/100/(1+self.manhattan(node,p.node))**2 for p in self.producers)

    def home_score(self,h,node,previous_jobs=None):
        offers=self.offers()
        shopping=0.
        for g in ('food','goods'):
            shopping+=min((2*prices[g]+h.value_time*(2*self.route(node,n)[0]+.06)
                           for _,n,prices in offers if g in prices),default=1000)
        jobs=(self.jobs if previous_jobs is None else previous_jobs).get(str(h.id),{})
        total=sum(jobs.values())
        commute=sum(self.route(node,self.employer_node(a))[0]*2*w for a,w in jobs.items())/max(1,total)
        rent=self.home_rent(node)
        environment=self.environment_weight*self.preferences[h.id]*self.exposure(node)
        return {'rent':rent,'shopping':shopping,'commute':h.value_time*commute,
                'environment':environment,'total':rent+shopping+h.value_time*commute+environment}

    def employer_node(self,account):
        return self.producers[int(account[1:])].node if account.startswith('p') else self.stores[int(account[1:])].node

    def hire(self,account,hours,purpose):
        if not hasattr(self,'roads'): return super().hire(account,hours,purpose)
        wanted=min(round(hours*100),self.accounts[account]);remaining=wanted;visited=0
        node=self.employer_node(account)
        while remaining and visited<len(self.residents):
            h=self.residents[self.employee_order[self.worker_cursor]]
            visit=(h.id,node)
            trip=0. if visit in self.commute_visits else 2*self.route(h.node,node)[0]
            available=max(0,math.floor((8-h.work-self.commute.get(h.id,0)-trip)*100+1e-7))
            amount=min(remaining,available)
            if amount:
                if trip:
                    self.observe('commute_trip',resident=h.id,workplace=node,duration=trip,
                                 edges=[list(e) for e in self.route(h.node,node)[1]])
                    self.commute[h.id]=self.commute.get(h.id,0)+self.trip(h.node,node,count=2)
                self.commute_visits.add(visit)
                self.pay(account,f'h{h.id}',amount,purpose)
                h.work+=amount/100;remaining-=amount
                jobs=self.jobs.setdefault(str(h.id),{})
                jobs[account]=jobs.get(account,0)+amount
            if amount==available:
                self.worker_cursor=(self.worker_cursor+1)%len(self.residents);visited+=1
        actual=(wanted-remaining)/100
        self.labor[purpose]+=actual
        return actual

    def review_homes(self,previous_jobs):
        if self.mode=='fixed' or self.month<6: return
        candidates=list(self.residents)
        self.rng('home_review').shuffle(candidates)
        for h in candidates[:max(1,len(candidates)//6)]:
            if self.month-self.moved_home.get(h.id,-10)<4: continue
            nodes=[n for n in range(35) if n==h.node or ('R' in RIGHTS[self.zones[n]] and self.location_accessible(n) and
                   self.free_land(n)>=(1 if self.usage(n)['homes']%4==0 else 0))]
            current=self.home_score(h,h.node,previous_jobs)
            scored=[(self.home_score(h,n,previous_jobs),n) for n in nodes]
            best,node=min(scored,key=lambda x:(x[0]['total'],x[1]))
            if self.mode=='blind':
                node=self.rng('blind_home',h.id).choice(nodes);best=self.home_score(h,node,previous_jobs)
            enough=self.accounts[f'h{h.id}']>=120
            gain=current['total']-best['total']
            required_gain=max(120/6+30,.10*current['total']) if self.home_move_policy=='stable' else 120/6+30
            willing=self.mode=='blind' or gain>required_gain
            reason='同地居住' if node==h.node else '现金不足' if not enough else '节约不足以补偿搬迁' if not willing else '搬迁'
            record={'resident':h.id,'from':h.node,'to':node,'current':current,'candidate':best,
                    'reason':reason,'month':self.month,'gain':gain,'required_gain':required_gain,
                    'decision_mode':self.mode,'cooldown_months':4,
                    'move_policy':self.home_move_policy,'cash_at_review':self.accounts[f'h{h.id}'],
                    'move_cost':120,'candidate_count':len(scored),
                    'alternatives':sorted([{'node':n,**s} for s,n in scored],key=lambda x:x['total'])}
            self.home_reviews.append(record)
            if node!=h.node and enough and willing:
                self.pay(f'h{h.id}','property',120,'home_move')
                h.node=node;self.moved_home[h.id]=self.month
                self.spatial_events.append({'type':'home_move',**record})
                self.paths={}

    def factory_score(self,p,node):
        others=[q for q in self.producers if q.good==p.good and q.id!=p.id]
        direct=0.
        for h in self.residents:
            own=p.price+h.value_time*(2*self.route(h.node,node)[0]+.06)/2
            alternatives=[q.price+h.value_time*(2*self.route(h.node,q.node)[0]+.06)/2 for q in others]
            alternatives += [s.prices[p.good]+h.value_time*(2*self.route(h.node,s.node)[0]+.06)/(2*len(s.assortment))
                             for s in self.stores if s.active and p.good in s.assortment]
            if 2*self.route(h.node,node)[0]+.06<=self.settings.shopping_hours:
                direct+=2*max(0,min(1,.5+(min(alternatives,default=own)-own)/100))
        wholesale=0.
        for s in self.stores:
            if not s.active or p.good not in s.assortment: continue
            own=self.route(node,s.node,'truck')[0]
            other=min((self.route(q.node,s.node,'truck')[0] for q in others),default=own)
            wholesale+=s.expected[p.good]*max(0,min(1,.5+(other-own)))
        labor=sum(max(0,8-2*self.route(h.node,node)[0]) for h in self.residents)/len(self.producers)
        volume=min(160,direct+wholesale,labor/.25)
        rent=self.rent(node,4)+100
        # Same output price and production wage as actual trading, approximate counter labor.
        profit=volume*(p.price-25)-100*(1+.05*min(direct,volume))-rent
        return {'direct':direct,'wholesale':wholesale,'volume':volume,'rent':rent,'profit':profit}

    def review_factory(self):
        if self.mode=='fixed' or self.month<6: return
        p=self.producers[self.month%len(self.producers)]
        if self.month-self.moved_factory.get(p.id,-10)<8: return
        nodes=[n for n in range(35) if n==p.node or ('I' in RIGHTS[self.zones[n]] and self.location_accessible(n) and self.free_land(n)>=4)]
        current=self.factory_score(p,p.node)
        scored=[(self.factory_score(p,n),n) for n in nodes]
        best,node=max(scored,key=lambda x:(x[0]['profit'],-x[1]))
        if self.mode=='blind': node=self.rng('blind_factory',p.id).choice(nodes);best=self.factory_score(p,node)
        enough=self.accounts[f'p{p.id}']>=1200
        willing=self.mode=='blind' or best['profit']-current['profit']>1200/6+200
        reason='原址更合适' if node==p.node else '现金不足' if not enough else '预期改善不足以补偿搬迁' if not willing else '搬迁'
        record={'producer':p.id,'from':p.node,'to':node,'current':current,'candidate':best,'reason':reason,'month':self.month,
                'decision_mode':self.mode,'cooldown_months':8,
                'cash_at_review':self.accounts[f'p{p.id}'],'move_cost':1200,'required_gain':400,
                'candidate_count':len(scored),'alternatives':sorted([{'node':n,**s} for s,n in scored],key=lambda x:-x['profit'])}
        self.factory_reviews.append(record)
        if node!=p.node and enough and willing:
            self.pay(f'p{p.id}','property',1200,'factory_move')
            p.node=node;self.moved_factory[p.id]=self.month
            self.spatial_events.append({'type':'factory_move',**record});self.paths={}

    def enter(self):
        previous_jobs=copy.deepcopy(self.jobs)
        self.jobs,self.commute,self.commute_visits={},{},set()
        self.spatial_events,self.home_reviews,self.factory_reviews=[],[],[]
        upkeep=4*sum(self.roads.values())+self.extra_maintenance
        paid=min(upkeep,self.accounts['civic'])
        self.maintenance_ratio=paid/max(1,upkeep)
        self.pay('civic','property',paid,'road_maintenance')
        for h in self.residents:
            self.pay(f'h{h.id}','property',min(self.home_rent(h.node),self.accounts[f'h{h.id}']),'housing_rent')
        self.review_homes(previous_jobs)
        self.review_factory()
        for p in self.producers:
            self.pay(f'p{p.id}','property',min(self.rent(p.node,4)+100,self.accounts[f'p{p.id}']),'industrial_rent')
        self.commercial_audit.begin(self)
        try:
            super().enter()
        finally:
            self.commercial_audit.finish(self)

    def step(self):
        row=super().step()
        assert all(self.free_land(n)>=0 for n in range(35))
        assert all(h.work+self.commute.get(h.id,0)<=8+1e-6 for h in self.residents)
        item={'month':self.month-1,'moves':len([e for e in self.spatial_events if e['type']=='home_move']),
              'factory_moves':len([e for e in self.spatial_events if e['type']=='factory_move']),
              'commute':sum(self.commute.values())/len(self.residents),
              'exposure':sum(self.exposure(h.node) for h in self.residents)/len(self.residents),
              'occupied_home_nodes':len({h.node for h in self.residents}),
              'production':sum(p.production for p in self.producers),
              'home_factory_distance':sum(min(self.manhattan(h.node,p.node) for p in self.producers) for h in self.residents)/len(self.residents),
              'home_factory_overlap':sum(any(h.node==p.node for p in self.producers) for h in self.residents)/len(self.residents),
              'civic':self.accounts['civic']/100,'built_land':sum(8-self.free_land(n) for n in range(35))}
        fiscal=self.fiscal_summary()
        item.update({'tax_income':fiscal['tax_income'],'upkeep_paid':fiscal['upkeep_paid'],
                     'upkeep_due':fiscal['next_upkeep_due'],'income_tax_rate':self.income_tax_rate})
        self.spatial_history.append(item)
        self.spatial_log=(self.spatial_log+copy.deepcopy(self.spatial_events))[-200:]
        self.home_review_history.extend(copy.deepcopy(self.home_reviews))
        self.factory_review_history.extend(copy.deepcopy(self.factory_reviews))
        # Inspections quote the next period's known traffic, consistently before/after a save.
        # Actual previous-period routes are preserved in the observation records.
        self.paths={}
        return row

    def snapshot(self):
        data=super().snapshot()
        data['_land']=copy.deepcopy({'version':1,'mode':self.mode,'environment_weight':self.environment_weight,
              'zones':self.zones,'roads':[[a,b,v] for (a,b),v in self.roads.items()],
              'jobs':self.jobs,'commute':self.commute,'moved_home':self.moved_home,'moved_factory':self.moved_factory,
              'spatial_events':self.spatial_events,'home_reviews':self.home_reviews,'factory_reviews':self.factory_reviews,
              'spatial_history':self.spatial_history})
        data['_land']['commercial_audit']=self.commercial_audit.dump()
        data['_land']['commercial_last_review']=copy.deepcopy(self.commercial_last_review)
        data['_land']['initial_locations']=copy.deepcopy(self.initial_locations)
        data['_land']['income_tax_rate']=self.income_tax_rate
        data['_land']['home_move_policy']=self.home_move_policy
        data['_land']['maintenance_ratio']=self.maintenance_ratio
        data['_land']['extra_maintenance']=self.extra_maintenance
        if self.greenfield:data['_land']['greenfield']={'gateway':self.gateway}
        for key in ('spatial_log','home_review_history','factory_review_history'):
            data['_land'][key]=copy.deepcopy(getattr(self,key))
        return data

    @classmethod
    def restore(cls,data,trace=False):
        model=super().restore(data,trace)
        extra=data['_land']
        if extra['version']!=1: raise ValueError('土地实验存档版本无效')
        # Old saves keep their history and adopt the new rule for future decisions.
        model.income_tax_rate=extra.get('income_tax_rate',0)
        if not isinstance(model.income_tax_rate,int) or isinstance(model.income_tax_rate,bool) or not 0<=model.income_tax_rate<=30:raise ValueError('收入税率须为 0 到 30 的整数百分比')
        model.home_move_policy=extra.get('home_move_policy','stable')
        if model.home_move_policy not in ('stable','legacy'):raise ValueError('迁居规则无效')
        for k in ('mode','environment_weight','jobs','spatial_events','home_reviews','factory_reviews','spatial_history'):
            setattr(model,k,copy.deepcopy(extra[k]))
        for k in ('zones','commute','moved_home','moved_factory'):
            setattr(model,k,{int(n):v for n,v in extra[k].items()})
        model.roads={(a,b):v for a,b,v in extra['roads']}
        model.greenfield='greenfield' in extra
        model.gateway=extra.get('greenfield',{}).get('gateway')
        if model.greenfield and (not isinstance(model.gateway,int) or not 0<=model.gateway<35):raise ValueError('城市道路起点无效')
        model.maintenance_ratio=float(extra['maintenance_ratio'])
        model.extra_maintenance=extra.get('extra_maintenance',0)
        if not isinstance(model.extra_maintenance,int) or not 0<=model.extra_maintenance<=10000: raise ValueError('维护参数无效')
        for key in ('spatial_log','home_review_history','factory_review_history'):
            setattr(model,key,copy.deepcopy(extra.get(key,[])))
        if not 0<=model.maintenance_ratio<=1: raise ValueError('道路维护状态无效')
        if set(model.zones)!=set(range(35)) or any(v not in ZONES for v in model.zones.values()): raise ValueError('土地用途无效')
        if model.mode not in ('adaptive','fixed','blind') or not 0<=model.environment_weight<=2: raise ValueError('选址机制无效')
        if any(not 0<=a<b<35 or model.manhattan(a,b)!=1 or v not in (1,2) for (a,b),v in model.roads.items()): raise ValueError('道路无效')
        model.paths={}
        required=({h.node for h in model.residents}|{p.node for p in model.producers}|{s.node for s in model.stores if s.active}) if model.greenfield else set(range(35))
        origin=model.gateway if model.greenfield else 0
        if any(not math.isfinite(model.route(origin,n)[0]) for n in required): raise ValueError('已有主体的道路网络必须连通')
        if any(model.free_land(n)<0 for n in range(35)): raise ValueError('地块超过土地容量')
        model.commute_visits=set()
        model.commercial_audit=CommercialAudit.restore(extra.get('commercial_audit'),model)
        model.initial_locations=copy.deepcopy(extra.get('initial_locations',{'homes':[],'factories':[]}))
        if (not isinstance(model.initial_locations,dict) or
            any(not isinstance(model.initial_locations.get(k),list) for k in ('homes','factories'))):
            raise ValueError('初始选址记录无效')
        if 'commercial_last_review' in extra:
            if not isinstance(extra['commercial_last_review'],dict):raise ValueError('商业轮查记录无效')
            model.commercial_last_review={int(n):v for n,v in extra['commercial_last_review'].items()}
            if any(not 0<=n<35 or not isinstance(v,int) or isinstance(v,bool) or not 0<=v<=model.month
                   for n,v in model.commercial_last_review.items()):raise ValueError('商业轮查记录无效')
        else:
            # Only surviving actual estimates can seed old-save scheduling. Missing
            # history is unknown; do not reconstruct it from the current economy.
            model.commercial_last_review={}
            for visit in model.commercial_audit.visits:
                if visit['plans']:
                    n=visit['node']
                    model.commercial_last_review[n]=max(visit['month'],model.commercial_last_review.get(n,-1))
        return model
