"""Paired location/land experiments. No fit to a preferred urban outcome."""
from __future__ import annotations
import csv
import json
import sys
from pathlib import Path
from statistics import mean
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from landlab.session import LandSession

CASES=('hold','open','separated','compact','compact_links','compact_cash_control','open_fixed','open_blind','open_no_environment')


def apply(session,case):
    c=session.player
    if case=='hold': return
    if case.startswith('open'):
        plan={n:'open' for n in range(35)}
    elif case=='separated':
        plan={n:('home' if n%7<=2 else 'shop' if n%7<=4 else 'factory') for n in range(35)}
    else:
        plan={8:'mixed',9:'mixed',15:'mixed',16:'mixed',17:'mixed',21:'mixed',22:'mixed',23:'mixed',12:'factory',26:'factory'}
    for n,zone in plan.items():
        if c.zones[n]!=zone: session.intervene('zone',{'id':n,'zone':zone},f'predeclared {case}')
    if case=='compact_links':
        for a,b in ((9,16),(16,23),(8,15)):
            session.intervene('road',{'id':a,'neighbor':b},'three interior links')
    if case=='compact_cash_control':
        for _ in range(3): c.pay('civic','property',1200,'road_build')
        c.extra_maintenance=12


def main():
    out=Path('results/land');out.mkdir(parents=True,exist_ok=True)
    rows=[]
    for seed in range(1,5):
        baseline=LandSession.new(seed)
        baseline.player.home_move_policy=baseline.reference.home_move_policy='legacy'
        initial=baseline.dump()  # Preserve the original land study's relocation rule.
        for case in CASES:
            s=LandSession.load(initial)
            mode='fixed' if case=='open_fixed' else 'blind' if case=='open_blind' else 'adaptive'
            s.player.mode=s.reference.mode=mode;s.preset=mode
            if case=='open_no_environment': s.player.environment_weight=s.reference.environment_weight=0
            apply(s,case)
            spent=sum(a['cost'] for a in s.actions)/100+(36 if case=='compact_cash_control' else 0)
            s.advance(12);s.advance(12)
            row={'seed':seed,'case':case,'mode':mode,'period':s.player.month,'public_planning_spend':spent}
            for name,c in [('player',s.player),('reference',s.reference)]:
                for key in ('food_coverage','goods_coverage','shopping_hours','retail_share','paid_price','stores','hours_per_unit','nearest_store_distance'):
                    row[f'{name}_{key}']=mean(r[key] for r in c.metrics[-6:])
                row[f'{name}_shopping_hours']/=len(c.residents)
                for key in ('commute','exposure','occupied_home_nodes','built_land','civic','production','home_factory_distance','home_factory_overlap'):
                    row[f'{name}_{key}']=mean(r[key] for r in c.spatial_history[-6:])
                row[f'{name}_home_moves']=sum(r['moves'] for r in c.spatial_history[6:])
                row[f'{name}_factory_moves']=sum(r['factory_moves'] for r in c.spatial_history[6:])
                row[f'{name}_max_parcel_use']=max(8-c.free_land(n) for n in range(35))
                assert all(r['cash_error']==r['stock_error_food']==r['stock_error_goods']==0 for r in c.metrics)
            for key in list(row):
                if key.startswith('player_'): row['delta_'+key[7:]]=row[key]-row['reference_'+key[7:]]
            if case=='hold': assert s.player.snapshot()==s.reference.snapshot()
            rows.append(row)
            if seed==1 and case in ('open','compact_links'):
                (out/f'{case}-seed1.json').write_text(json.dumps(s.dump(),ensure_ascii=False),encoding='utf-8')
                (out/f'{case}-seed1-reasons.json').write_text(json.dumps({'homes':s.player.home_reviews,'factories':s.player.factory_reviews,
                     'locations':{'homes':[[h.id,h.node] for h in s.player.residents],'factories':[[p.id,p.node] for p in s.player.producers]}},ensure_ascii=False,indent=2),encoding='utf-8')
            print(f'seed {seed} {case}: done',flush=True)
    with (out/'runs.csv').open('w',encoding='utf-8',newline='') as f:
        w=csv.DictWriter(f,fieldnames=list(rows[0]));w.writeheader();w.writerows(rows)
    summary=[]
    for case in CASES:
        group=[r for r in rows if r['case']==case]
        summary.append({'case':case,'seeds':len(group),**{k:mean(r[k] for r in group) for k in group[0] if k.startswith(('delta_','player_'))},
                        'shorter_shopping_seeds':sum(r['delta_shopping_hours']<0 for r in group),
                        'lower_exposure_seeds':sum(r['delta_exposure']<0 for r in group)})
    (out/'summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
    print('Saved results/land/runs.csv and summary.json')


if __name__=='__main__': main()
