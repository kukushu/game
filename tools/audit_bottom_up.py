"""Read-only architecture audit probes; no production rules are edited.

Run: python tools/audit_bottom_up.py
Counterfactual replacements live only on disposable in-memory city copies.
"""
import argparse
import copy
import hashlib
import json
import statistics
import subprocess
import sys
from pathlib import Path
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
from commerce.model import Settings
from landlab.model import LandCity
from landlab.session import LandSession
from landlab.layers import city_layers

SOURCES = ('commerce/model.py','landlab/model.py','landlab/planning.py',
           'landlab/session.py','playweb/engine.py','econlab/model.py')


def hashes():
    return {name:hashlib.sha256((ROOT/name).read_bytes()).hexdigest() for name in SOURCES}


def new_city(seed, entry=False, all_open=False):
    city = LandCity(seed, Settings(households=64,zoning='flexible',entry=entry))
    city.mode = 'fixed'  # Isolate the tested rule from endogenous relocations.
    if all_open:city.zones = dict.fromkeys(range(35),'open')
    return city


def summary(city, start=0):
    rows = city.metrics[start:]
    receipts = city.current.get('payments',[])
    wages = sum(p['cents'] for p in receipts if p['target'].startswith('h') and
                p['purpose'] in ('manufacturing','factory_service','retail_staff','freight'))
    dividends = sum(p['cents'] for p in receipts if p['target'].startswith('h') and
                    p['purpose'] in ('household_dividend','fund_dividend'))
    cash = [city.accounts[f'h{h.id}']/100 for h in city.residents]
    return {'month':city.month,'population':len(city.residents),'factory_count':len(city.producers),
            'food_coverage_mean':statistics.mean(r['food_coverage'] for r in rows),
            'goods_coverage_mean':statistics.mean(r['goods_coverage'] for r in rows),
            'consumer_spend_mean':statistics.mean(r['consumer_spend'] for r in rows),
            'production_mean':statistics.mean(r['production'] for r in city.spatial_history[start:]),
            'active_stores_end':sum(s.active for s in city.stores),
            'active_stores_max':max(r['stores'] for r in rows),
            'entry_cap_rounds':sum(r['status']=='store_limit' for r in city.commercial_audit.rounds),
            'entries':sum(e['event']=='entry' for e in city.events),
            'exits':sum(e['event']=='exit' for e in city.events),
            'household_cash':sum(cash),'household_cash_median':statistics.median(cash),
            'civic_cash':city.accounts['civic']/100,'fund_cash':city.accounts['fund']/100,
            'tax_collected':sum(r['tax_income'] for r in city.spatial_history[start:])/100,
            'cash_error':sum(city.accounts.values())-city.initial_money,
            'goods_error_max':max(abs(r[k]) for r in rows for k in ('stock_error_food','stock_error_goods')),
            'last_period_wages':wages/100,'last_period_dividends':dividends/100,
            'last_period_zero_work_households':sum(h.work==0 for h in city.residents),
            'last_period_reported_total_hours':rows[-1]['total_hours'],
            'last_period_commute_hours':sum(city.commute.values()),
            'production_by_factory':[p.production for p in city.producers]}


def physical_snapshot(city):
    data = city.snapshot()
    data.pop('metrics')
    return data


def run(seeds, months):
    before = hashes()
    output = {'revision':subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),
              'months':months,'seeds':seeds,'source_hashes_before':before,
              'observation_probes':[],'labor_probes':[],'dividend_probes':[],
              'tax_probes':[],'search_probes':[],'liquidity_probes':[]}
    for seed in seeds:
        c = new_city(seed)
        for _ in range(6):c.step()
        frozen = c.snapshot()
        session = LandSession(c,LandCity.restore(frozen))
        city_layers(c); session.view(); session.inspect('resident',0)
        readonly = frozen == c.snapshot()
        a,b = LandCity.restore(frozen),LandCity.restore(frozen)
        measure = b.measure
        with patch.object(b,'measure',side_effect=lambda:{**measure(),'audit_only_probe':10**9}):
            for _ in range(3):a.step();b.step()
        statistics_independent = physical_snapshot(a)==physical_snapshot(b)
        output['observation_probes'].append({'seed':seed,'inspect_and_layers_leave_state_unchanged':readonly,
            'extra_reported_statistic_leaves_physical_state_unchanged':statistics_independent})
        assert readonly and statistics_independent

        # At the first period, time preference changes shopping, but cannot affect
        # the labor allocation that precedes shopping and lacks any utility comparison.
        a = new_city(seed); b = LandCity.restore(a.snapshot())
        for h in b.residents:h.value_time *= 100
        a.step(); b.step()
        output['labor_probes'].append({'seed':seed,'time_value_multiplier':100,
            'jobs_identical':a.jobs==b.jobs,
            'work_hours_identical':[h.work for h in a.residents]==[h.work for h in b.residents],
            'shopping_hours_baseline':sum(h.shopping for h in a.residents),
            'shopping_hours_high_time_value':sum(h.shopping for h in b.residents)})

        a=new_city(seed,all_open=True); b=LandCity.restore(a.snapshot())
        for h in b.residents:
            b.pay(f'h{h.id}','civic',b.accounts[f'h{h.id}'],'audit_liquidity_transfer')
        forecast_a=a.estimate_site(7,4,['food','goods'])
        forecast_b=b.estimate_site(7,4,['food','goods'])
        a.step(); b.step()
        output['liquidity_probes'].append({'seed':seed,
            'treatment':'all initial household cash transferred to treasury, no new money',
            'forecasts_identical':forecast_a==forecast_b,
            'forecast_profit':forecast_a[0]/100,
            'actual_consumer_spend_baseline':a.metrics[-1]['consumer_spend'],
            'actual_consumer_spend_cash_transferred':b.metrics[-1]['consumer_spend'],
            'actual_units_baseline':a.metrics[-1]['units'],
            'actual_units_cash_transferred':b.metrics[-1]['units']})

        initial = new_city(seed).snapshot()
        for label in ('equal_dividends','withhold_household_dividends'):
            c = LandCity.restore(initial)
            pay = c.pay
            def payment(source,target,amount,purpose):
                if purpose not in ('household_dividend','fund_dividend'):
                    pay(source,target,amount,purpose)
            if label=='withhold_household_dividends':
                with patch.object(c,'pay',side_effect=payment):
                    for _ in range(months):c.step()
            else:
                for _ in range(months):c.step()
            output['dividend_probes'].append({'seed':seed,'case':label,**summary(c)})

        for rate in (0,1,30):
            c = LandCity.restore(initial); c.income_tax_rate = rate
            for _ in range(months):c.step()
            output['tax_probes'].append({'seed':seed,'tax_rate':rate,**summary(c)})

        initial = new_city(seed,entry=True,all_open=True).snapshot()
        for label in ('up_to_8_eligible','all_eligible'):
            c = LandCity.restore(initial)
            def all_candidates():
                occupied={s.node for s in c.stores if s.active}
                eligible=[n for n in range(35) if n not in occupied and c.commercial_areas(n)]
                for node in eligible:c.commercial_last_review[node]=c.month
                c.commercial_audit.schedule(eligible,eligible)
                return eligible
            if label=='all_eligible':
                with patch.object(c,'site_candidates',side_effect=all_candidates):
                    for _ in range(months):c.step()
            else:
                for _ in range(months):c.step()
            output['search_probes'].append({'seed':seed,'case':label,**summary(c)})

    after = hashes()
    output['source_hashes_after']=after
    output['production_sources_unchanged']=before==after
    assert output['production_sources_unchanged']
    for key in ('dividend_probes','tax_probes','search_probes'):
        assert all(r['cash_error']==r['goods_error_max']==0 for r in output[key])
    return output


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--seeds',nargs='+',type=int,default=[1,7,23])
    parser.add_argument('--months',type=int,default=24)
    parser.add_argument('--output',type=Path,default=ROOT/'outputs/bottom-up-audit/evidence.json')
    args=parser.parse_args()
    if args.months<1:parser.error('--months must be positive')
    result=run(args.seeds,args.months)
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({'output':str(args.output),'revision':result['revision'],
                     'production_sources_unchanged':result['production_sources_unchanged'],
                     'observations':result['observation_probes'],'labor':result['labor_probes']},ensure_ascii=False))


if __name__=='__main__':main()
