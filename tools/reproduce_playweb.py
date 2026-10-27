"""Reproduce actual player interventions without the HTTP/UI layer. Stdlib only."""
from __future__ import annotations

import csv
import json
import sys
from pathlib import Path
from statistics import mean

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from playweb.engine import Session

STRATEGIES = {
    'observe': [],
    'access': [('access', None)],
    'freight': [('freight', None)],
    'central_permits': [('permit_large', 8), ('permit_large', 16), ('permit_large', 22)],
    'edge_permits': [('permit_large', 0), ('permit_large', 14), ('permit_large', 28)],
    'assortment': [('assortment', None)],
}


def main():
    out = Path('results/playweb')
    out.mkdir(parents=True, exist_ok=True)
    rows = []
    example = None
    for preset in ('neighborhood', 'sparse', 'easy_access'):
        for seed in range(1, 5):
            start = Session.new(seed, preset).dump()
            for strategy, actions in STRATEGIES.items():
                session = Session.load(start)
                for action, node in actions:
                    session.intervene(action, node, f'reproducible experiment: {strategy}')
                session.advance(12)
                session.advance(12)
                row = {'preset': preset, 'seed': seed, 'strategy': strategy, 'period': session.player.month}
                for name, city in [('player', session.player), ('reference', session.reference)]:
                    for key in ('food_coverage','goods_coverage','paid_price','shopping_hours','retail_share','small','large','hours_per_unit'):
                        row[f'{name}_{key}'] = mean(r[key] for r in city.metrics[-6:])
                    row[f'{name}_shopping_hours'] /= len(city.residents)
                for key in ('food_coverage','goods_coverage','paid_price','shopping_hours','retail_share','small','large','hours_per_unit'):
                    row[f'delta_{key}'] = row[f'player_{key}'] - row[f'reference_{key}']
                if strategy == 'observe':
                    assert session.player.snapshot() == session.reference.snapshot()
                assert all(r['cash_error']==r['stock_error_food']==r['stock_error_goods']==0 for c in (session.player,session.reference) for r in c.metrics)
                rows.append(row)
                if preset == 'neighborhood' and seed == 1 and strategy == 'central_permits':
                    example = session.dump()
            print(f'{preset} seed {seed}: done', flush=True)
    with (out/'runs.csv').open('w', newline='', encoding='utf-8') as f:
        writer = csv.DictWriter(f, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)
    summary = []
    for preset in ('neighborhood','sparse','easy_access'):
        for strategy in STRATEGIES:
            group = [r for r in rows if r['preset']==preset and r['strategy']==strategy]
            summary.append({'preset':preset,'strategy':strategy,'seeds':len(group),
                            **{k:mean(r[k] for r in group) for k in group[0] if k.startswith('delta_')},
                            'shopping_time_decreased_seeds':sum(r['delta_shopping_hours']<0 for r in group),
                            'large_store_present_seeds':sum(r['player_large']>0 for r in group)})
    (out/'summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
    (out/'central-permits-s1.json').write_text(json.dumps(example,ensure_ascii=False),encoding='utf-8')
    print(json.dumps(summary,indent=2))


if __name__ == '__main__':
    main()
