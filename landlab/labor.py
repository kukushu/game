"""Opt-in manufacturing market. One household supplies one primary shift plus residual casual work.

Five probes: equal-distance wages; wage/commute and a road counterfactual;
cash-capped vacancies; incumbent stability versus a large improvement; real
pay/tax/output/consumption. No employment target or macro correction is used.
"""
from collections import deque
import copy
import hashlib
import math

VERSION = 1
SHIFT_UNITS = 16  # one unit = .25 hours and one produced item; max shift = 4 hours
LEISURE_WEIGHT = .35
SWITCH_FLOOR = 20  # cents of utility per period, not a payment
SWITCH_FRACTION = .10


def rank(seed, *parts):
    return int.from_bytes(hashlib.blake2b(':'.join(map(str,(seed,*parts))).encode(),digest_size=8).digest(),'big')


def wage_bill(units, wage):
    return (units*wage+2)//4  # nearest cent, integer arithmetic


def build_posts(city, targets):
    posts = []
    firms = {}
    for p in sorted(city.producers,key=lambda p:p.id):
        wage = city.factory_wages[str(p.id)]
        remaining = targets[p.id]
        budget = city.accounts[f'p{p.id}']
        firms[str(p.id)] = {'producer':p.id,'target_units':remaining,'desired_hours':remaining/4,
            'wage':wage,'cash_at_posting':budget,'funded_units':0,'posted_shifts':0}
        while remaining:
            units = min(SHIFT_UNITS,remaining)
            while units and wage_bill(units,wage)>budget:units-=1
            if not units:break
            cost = wage_bill(units,wage)
            posts.append({'key':f'{p.id}:{firms[str(p.id)]["posted_shifts"]}',
                          'producer':p.id,'node':p.node,'units':units,'hours':units/4,
                          'wage':wage,'gross':cost,'net':cost-cost*city.income_tax_rate//100})
            remaining-=units;budget-=cost
            firms[str(p.id)]['funded_units']+=units
            firms[str(p.id)]['posted_shifts']+=1
    return posts,firms


def match(city,posts,worker_order=None):
    """Worker-proposing deferred acceptance, one held offer per worker and per shift.

    Every worker sees the same posted board. Firms prefer feasible incumbents,
    then a seeded neutral priority. Strict seeded ties remove array-order privilege.
    The priority is an allocation abstraction, not a productivity/skill assessment.
    """
    by_id={h.id:h for h in city.residents}
    preferences={};evaluations={};attempted={h.id:set() for h in city.residents}
    by_post={p['key']:p for p in posts}
    for h in city.residents:
        options=[]
        for post in posts:
            visit=(h.id,post['node'])
            trip=0. if visit in city.commute_visits else 2*city.route(h.node,post['node'])[0]
            feasible=math.isfinite(trip) and h.work+city.commute.get(h.id,0)+trip+post['hours']<=8+1e-8
            utility=post['net']-h.value_time*(LEISURE_WEIGHT*post['hours']+trip) if math.isfinite(trip) else None
            reason='unreachable' if not math.isfinite(trip) else 'time_limit' if not feasible else 'not_worthwhile' if utility<=0 else 'available'
            options.append({**post,'trip':trip if math.isfinite(trip) else None,'utility':utility,'reason':reason})
        incumbent=city.employers.get(str(h.id))
        old=max((o['utility'] for o in options if o['producer']==incumbent and o['reason']=='available'),default=None)
        penalty=max(SWITCH_FLOOR,SWITCH_FRACTION*old) if old is not None else 0
        for o in options:
            o['switch_penalty']=penalty if o['producer']!=incumbent else 0
            o['score']=o['utility']-o['switch_penalty'] if o['utility'] is not None else None
        evaluations[h.id]=options
        preferences[h.id]=sorted([o for o in options if o['reason']=='available'],
            key=lambda o:(-o['score'],0 if o['producer']==incumbent else 1,rank(city.seed,'job_choice',h.id,o['producer'],o['key'])))
    order=list(by_id) if worker_order is None else list(worker_order)
    assert set(order)==set(by_id) and len(order)==len(by_id)
    queue=deque(order);cursor=dict.fromkeys(by_id,0);holders={}
    def employer_priority(post,hid):
        return (0 if city.employers.get(str(hid))==post['producer'] else 1,
                rank(city.seed,'recruitment',post['key'],hid))
    while queue:
        hid=queue.popleft();prefs=preferences[hid]
        if cursor[hid]>=len(prefs):continue
        offer=prefs[cursor[hid]];cursor[hid]+=1;key=offer['key'];attempted[hid].add(key)
        held=holders.get(key)
        if held is None or employer_priority(by_post[key],hid)<employer_priority(by_post[key],held):
            holders[key]=hid
            if held is not None:queue.append(held)
        else:queue.append(hid)
    assignments={hid:next(o for o in evaluations[hid] if o['key']==key) for key,hid in holders.items()}
    choices={}
    for hid,options in evaluations.items():
        selected=assignments.get(hid);candidates=[]
        for p in sorted(city.producers,key=lambda p:p.id):
            firm=[o for o in options if o['producer']==p.id]
            considered=[o for o in firm if o['key'] in attempted[hid]]
            chosen=selected if selected and selected['producer']==p.id else None
            feasible=[o for o in firm if o['reason']=='available']
            best=chosen or (max(feasible,key=lambda o:(o['score'],-rank(city.seed,'job_choice',hid,o['producer'],o['key']))) if feasible else min(firm,key=lambda o:({'not_worthwhile':0,'time_limit':1,'unreachable':2}.get(o['reason'],3),-(o['utility'] or 0),rank(city.seed,'job_choice',hid,o['producer'],o['key']))) if firm else None)
            status='accepted' if chosen else 'capacity_rejected' if considered else 'preferred_other' if feasible and selected else 'not_selected' if feasible else best['reason'] if best else 'no_post'
            candidates.append({**copy.deepcopy(best or {'producer':p.id,'wage':city.factory_wages[str(p.id)]}),
                               'status':status,'applications':len(considered)})
        choices[str(hid)]={'resident':hid,'month':city.month,'previous_employer':city.employers.get(str(hid)),
            'employer':selected['producer'] if selected else None,'candidates':candidates,
            'contract':copy.deepcopy(selected),'reason':'accepted' if selected else 'no_acceptable_or_available_shift'}
    return assignments,choices


def recruit(city,targets):
    posts,firms=build_posts(city,targets)
    assignments,choices=match(city,posts)
    for hid,o in sorted(assignments.items()):
        h=city.residents[hid];account=f'p{o["producer"]}';visit=(hid,o['node'])
        if visit not in city.commute_visits:
            if o['trip']:
                city.observe('commute_trip',resident=hid,workplace=o['node'],duration=o['trip'],
                             edges=[list(e) for e in city.route(h.node,o['node'])[1]])
                city.commute[hid]=city.commute.get(hid,0)+city.trip(h.node,o['node'],count=2)
            city.commute_visits.add(visit)
        city.pay(account,f'h{hid}',o['gross'],'manufacturing')
        h.work+=o['hours'];city.labor['manufacturing']+=o['hours']
        jobs=city.jobs.setdefault(str(hid),{})
        jobs[account]=jobs.get(account,0)+round(o['hours']*100)  # time weights, not wages
        city.observe('labor_contract',resident=hid,producer=o['producer'],hours=o['hours'],
                     wage=o['wage'],gross=o['gross'],net=o['net'],commute=o['trip'])
    for key,f in firms.items():
        accepted=[o for o in assignments.values() if o['producer']==f['producer']]
        filled=sum(o['units'] for o in accepted)
        statuses=[next(c['status'] for c in d['candidates'] if c['producer']==f['producer']) for d in choices.values()]
        f.update({'filled_units':filled,'actual_hours':filled/4,'hired_households':len(accepted),
                  'wages_paid':sum(o['gross'] for o in accepted),
                  'household_responses':{status:statuses.count(status) for status in sorted(set(statuses))},
                  'cash_limited_units':f['target_units']-f['funded_units'],
                  'unfilled_hours':(f['funded_units']-filled)/4,
                  'lost_output':f['target_units']-filled,
                  'reasons':(['cash_limit'] if f['funded_units']<f['target_units'] else [])+
                            (['unaccepted_shifts'] if filled<f['funded_units'] else [])+
                            (['no_production_demand'] if not f['target_units'] else [])})
        city.observe('labor_recruitment',**f)
    for decision in choices.values():city.observe('labor_choice',**decision)
    city.employers={str(hid):o['producer'] for hid,o in assignments.items()}
    city.labor_result={'month':city.month,'choices':choices,'firms':firms}
    return {int(k):f['filled_units']/4 for k,f in firms.items()}


def production(city,pooled=False):
    """Plan all manufacturing before matching; only afterwards hire casual counters."""
    targets={};before={}
    for p in sorted(city.producers,key=lambda p:p.id):
        before[p.id]={'stock_before':p.stock,'previous_sales':p.sold,'previous_expected':p.expected}
        p.stock-=city.decay(p.stock,p.good,f'p{p.id}')
        if city.month:
            p.expected=.6*p.expected+.4*(p.sold+p.unmet)
            if city.settings.factory_prices_adapt:
                delta=.01 if p.unmet>3 else (-.01 if p.stock>p.sold else 0)
                p.price=max(40,round(p.price*(1+delta)))
        targets[p.id]=min(160,max(0,math.ceil(p.expected*1.15-p.stock)))
    if pooled:  # diagnostic staging-only control; old hire and old wages
        hours={p.id:city.hire(f'p{p.id}',targets[p.id]*.25,'manufacturing') for p in sorted(city.producers,key=lambda p:p.id)}
    else:hours=recruit(city,targets)
    for p in sorted(city.producers,key=lambda p:p.id):
        p.production=int(hours[p.id]/.25+1e-8);p.stock+=p.production
        city.produced[p.good]+=p.production;p.sold=p.unmet=0
    for p in sorted(city.producers,key=lambda p:p.id):
        counter_units=p.stock if city.month==0 else max(12,p.direct_sold*1.2)
        counter_hours=(1+counter_units*.05) if city.settings.channel!='retail' else 0
        p.service=city.hire(f'p{p.id}',counter_hours*city.settings.service_multiplier,'factory_service')
        p.direct_sold=0
        city.observe('production',producer=p.id,**before[p.id],expected=p.expected,
                     produced=p.production,stock_after=p.stock,counter_hours=p.service,
                     target_units=targets[p.id],labor_mode=city.labor_mode)


def restore(city,data):
    if data is None:return  # old saves keep pooled labor; do not infer historical employers
    if not isinstance(data,dict) or data.get('version')!=VERSION:raise ValueError('就业实验存档版本无效')
    mode=data.get('mode')
    wages=data.get('wages');employers=data.get('employers')
    if mode not in ('pool','market','batch_pool'):raise ValueError('就业模式无效')
    if not isinstance(wages,dict) or set(wages)!={str(p.id) for p in city.producers}:raise ValueError('工厂工资报价无效')
    if any(not isinstance(w,int) or isinstance(w,bool) or not 1<=w<=100000 for w in wages.values()):raise ValueError('工厂工资报价无效')
    if not isinstance(employers,dict) or any(k not in {str(h.id) for h in city.residents} or type(v)!=int or v not in {p.id for p in city.producers} for k,v in employers.items()):raise ValueError('雇佣关系无效')
    result=data.get('result',{})
    if not isinstance(result,dict):raise ValueError('就业记录无效')
    city.labor_mode=mode;city.factory_wages=copy.deepcopy(wages)
    city.employers=copy.deepcopy(employers);city.labor_result=copy.deepcopy(result)
