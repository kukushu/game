"""Reproduce labor-market probes; writes only isolated experiment artifacts."""
import argparse
import copy
import csv
import hashlib
import json
import math
from pathlib import Path
import sys
from unittest.mock import patch

sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from landlab.model import LandCity
from landlab.session import LandSession
from landlab import labor

WAGES={'manufacturing','factory_service','retail_staff','freight'}


def period(city,previous_employers):
    payments=city.current.get('payments',[])
    incomes=[sum(p['cents'] for p in payments if p['target']==f'h{h.id}' and p['purpose'] in WAGES) for h in city.residents]
    contracts=city.current.get('labor_contract',[])
    desired=sum(min(160,max(0,math.ceil(p['expected']*1.15-(p['stock_after']-p['produced']))))/4 for p in city.current['production'])
    vacancies=sum(f['unfilled_hours'] for f in city.labor_result.get('firms',{}).values()) if city.labor_mode=='market' else None
    wage_sum=sum(p['cents'] for p in payments if p['purpose']=='manufacturing')
    work=city.labor['manufacturing']
    return {'month':city.month,'manufacturing_hours':work,'total_work_hours':sum(h.work for h in city.residents),
        'desired_manufacturing_hours':desired,'desired_unfilled_hours':max(0,desired-work),
        'funded_vacant_hours':vacancies,'primary_households':len(contracts),
        'manufacturing_households':len({p['target'] for p in payments if p['purpose']=='manufacturing'}),
        'zero_work_households':sum(h.work==0 for h in city.residents),
        'production':sum(p.production for p in city.producers),
        'manufacturing_gross':wage_sum,'manufacturing_hourly_mean':wage_sum/work if work else None,
        'wage_min':min(incomes),'wage_median':sorted(incomes)[len(incomes)//2],'wage_max':max(incomes),
        'household_wages':sum(incomes),'tax':city.fiscal_summary()['tax_income'],
        'shopping_spend':city.consumer_spend,'shopping_units':sum(h.food+h.goods for h in city.residents),
        'household_cash':sum(city.accounts[f'h{h.id}'] for h in city.residents),
        'commute':sum(city.commute.values())/len(city.residents),
        'home_moves':city.spatial_history[-1]['moves'],
        'primary_switches':sum(k in previous_employers and previous_employers[k]!=v for k,v in city.employers.items()),
        'primary_exits':sum(k not in city.employers for k in previous_employers),
        'primary_entries':sum(k not in previous_employers for k in city.employers),
        'cash_error':city.metrics[-1]['cash_error'],
        'stock_error':sum(abs(city.metrics[-1]['stock_error_'+g]) for g in ('food','goods')),
        'dividend_payments':sum(p['cents'] for p in payments if 'dividend' in p['purpose'])}


def run(city,count):
    out=[]
    for _ in range(count):
        previous=city.employers.copy();city.step();out.append(period(city,previous))
    return out


def summary(rows):
    keys=('manufacturing_hours','total_work_hours','desired_unfilled_hours','production','household_wages',
          'shopping_units','shopping_spend','commute','wage_min','wage_median','wage_max','manufacturing_hourly_mean','tax','zero_work_households','manufacturing_households','primary_households')
    return {**{k:sum(r[k] or 0 for r in rows)/len(rows) for k in keys},
        'home_moves':sum(r['home_moves'] for r in rows),'primary_switches':sum(r['primary_switches'] for r in rows),
        'primary_exits':sum(r['primary_exits'] for r in rows),'primary_entries':sum(r['primary_entries'] for r in rows),
        'ending_household_cash':rows[-1]['household_cash'],
        'funded_vacant_hours':sum(r['funded_vacant_hours'] or 0 for r in rows)/len(rows) if any(r['funded_vacant_hours'] is not None for r in rows) else None,
        'manufacturing_hourly_mean':sum(r['manufacturing_gross'] for r in rows)/sum(r['manufacturing_hours'] for r in rows) if sum(r['manufacturing_hours'] for r in rows) else None,
        'cash_error':max(abs(r['cash_error']) for r in rows),'stock_error':max(r['stock_error'] for r in rows),
        'dividend_payments':sum(r['dividend_payments'] for r in rows)}


def experiment(output,count=24):
    output=Path(output);output.mkdir(parents=True,exist_ok=True)
    results={'version':1,'months':count,'rules':{'dividends':'paused in every arm','tax_rate':10,
        'start':'same pooled state after 6 months','currency':'integer internal cents; 100 cents = 1K in UI',
        'wages':'pool,batch_pool,market all 100; market_quotes separately changes 1 and 3 to 150'},'runs':[], 'roads':[], 'order':[]}
    for seed in (1,7,23):
        base=LandCity(seed);base.income_tax_rate=10
        for _ in range(6):base.step()
        snapshot=base.snapshot()
        digest=hashlib.sha256(json.dumps(snapshot['accounts'],sort_keys=True).encode()).hexdigest()
        for mode in ('pool','batch_pool','market','market_quotes'):
            city=LandCity.restore(snapshot);city.labor_mode='market' if mode=='market_quotes' else mode
            if mode=='market_quotes':city.factory_wages.update({'1':150,'3':150})
            rows=run(city,count)
            results['runs'].append({'seed':seed,'mode':mode,'initial_accounts_sha256':digest,'initial_household_cash':sum(snapshot['accounts'][f'h{h.id}'] for h in base.residents),'summary':summary(rows),'periods':rows})
        city=LandCity.restore(snapshot);city.labor_mode='market'
        ordinary=copy.deepcopy(city);shuffled=copy.deepcopy(city)
        real_match=labor.match
        def reversed_match(c,posts):return real_match(c,list(reversed(posts)),worker_order=[h.id for h in reversed(c.residents)])
        run(ordinary,12)
        with patch.object(labor,'match',side_effect=reversed_match):run(shuffled,12)
        results['order'].append({'seed':seed,'market_reverse_posts_workers_exact':ordinary.snapshot()==shuffled.snapshot()})
        for action in ('none','road','cash_only_placebo'):
            c=LandCity.restore(snapshot);c.labor_mode='market'
            session=LandSession(c,LandCity.restore(c.snapshot()))
            if action=='road':session.intervene('road',{'id':9,'neighbor':16},'employment road probe')
            if action=='cash_only_placebo':c.pay('civic','property',1200,'audit_cash_only_placebo')
            results['roads'].append({'seed':seed,'action':action,'summary':summary(run(c,12))})
    assert all(r['summary']['cash_error']==r['summary']['stock_error']==r['summary']['dividend_payments']==0 for r in results['runs'])
    (output/'results.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')
    flat=[{'seed':r['seed'],'mode':r['mode'],**r['summary']} for r in results['runs']]
    with (output/'summary.csv').open('w',encoding='utf-8-sig',newline='') as f:
        w=csv.DictWriter(f,fieldnames=list(flat[0]));w.writeheader();w.writerows(flat)
    print(json.dumps({'runs':flat,'order':results['order'],'roads':results['roads']},ensure_ascii=False))
    return results


if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--output',default='outputs/labor-market-v1');parser.add_argument('--months',type=int,default=24)
    args=parser.parse_args();experiment(args.output,args.months)
