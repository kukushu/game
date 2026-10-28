"""Compare relocation rules from identical cities, without changing accounts or metrics."""
import argparse
import copy
import json
import sys
from pathlib import Path
from statistics import mean

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from landlab.session import LandSession


def compare(snapshot, name, months):
    rows = []
    for policy in ('legacy', 'stable'):
        city = LandSession.load(copy.deepcopy(snapshot)).player
        city.home_move_policy = policy
        start = city.month
        seen = {h.id: [h.node] for h in city.residents}
        moves, returns = [], 0
        for _ in range(months):
            city.step()
            for event in city.spatial_events:
                if event['type'] != 'home_move':
                    continue
                visits = seen[event['resident']]
                returns += event['to'] in visits
                visits.append(event['to'])
                moves.append(event)
        rows.append({
            'scenario': name, 'policy': policy, 'start_month': start, 'months': months,
            'moves': len(moves), 'return_moves_within_window': returns,
            'distinct_movers': len({e['resident'] for e in moves}),
            'food_coverage_last6': mean(m['food_coverage'] for m in city.metrics[-6:]),
            'goods_coverage_last6': mean(m['goods_coverage'] for m in city.metrics[-6:]),
            'commute_last6': mean(m['commute'] for m in city.spatial_history[-6:]),
            'cash_error': city.metrics[-1]['cash_error'],
        })
        assert all(m['cash_error'] == m['stock_error_food'] == m['stock_error_goods'] == 0
                   for m in city.metrics)
    return rows


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--snapshot', type=Path, help='Optional Land session to continue, read only')
    parser.add_argument('--months', type=int, default=24, choices=range(6, 121))
    parser.add_argument('--output', type=Path, default=Path('results/home-stability/runs.json'))
    args = parser.parse_args()
    rows = []
    scenarios = {}
    if args.snapshot:
        scenarios['saved-city'] = json.loads(args.snapshot.read_text(encoding='utf-8'))
    for seed in (1, 2, 3):
        session = LandSession.new(seed)
        for node in range(35):
            if session.player.zones[node] != 'open':
                session.intervene('zone', {'id': node, 'zone': 'open'})
        scenarios[f'open-seed{seed}'] = session.dump()
    for name, snapshot in scenarios.items():
        result = compare(snapshot, name, args.months)
        rows.extend(result)
        for row in result:
            print(json.dumps(row), flush=True)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(rows, ensure_ascii=False, indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()
