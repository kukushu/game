"""Paired tax experiments: fixed starting city, actual income withholding, no grants."""
import argparse
import copy
import json
import sys
from pathlib import Path
from statistics import mean

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from landlab.session import LandSession


def run(snapshot, name, rate, months):
    session = LandSession.load(copy.deepcopy(snapshot))
    city = session.player
    if city.income_tax_rate != rate:
        session.intervene('tax', {'rate': rate})
    start = city.month
    for _ in range(months):
        city.step()
    periods = city.spatial_history[-months:]
    row = {
        'scenario': name, 'tax_rate': rate, 'start_month': start, 'months': months,
        'tax_collected': sum(x['tax_income'] for x in periods) / 100,
        'road_maintenance_paid': sum(x['upkeep_paid'] for x in periods) / 100,
        'public_cash': city.accounts['civic'] / 100,
        'household_cash': sum(v for k, v in city.accounts.items() if k.startswith('h')) / 100,
        'last_tax': city.fiscal_summary()['tax_income'] / 100,
        'maintenance_ratio': city.maintenance_ratio,
        'food_coverage_last6': mean(x['food_coverage'] for x in city.metrics[-6:]),
        'goods_coverage_last6': mean(x['goods_coverage'] for x in city.metrics[-6:]),
        'commute_last6': mean(x['commute'] for x in city.spatial_history[-6:]),
        'stores': sum(s.active for s in city.stores),
        'cash_error': city.metrics[-1]['cash_error'],
    }
    assert all(x['cash_error'] == x['stock_error_food'] == x['stock_error_goods'] == 0
               for x in city.metrics)
    return row


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--snapshot', type=Path)
    parser.add_argument('--months', type=int, default=24, choices=range(6, 121))
    parser.add_argument('--output', type=Path, default=Path('results/taxation/runs.json'))
    args = parser.parse_args()
    scenarios = {}
    if args.snapshot:
        scenarios['saved-city'] = json.loads(args.snapshot.read_text(encoding='utf-8'))
    for seed in (1, 2, 3):
        scenarios[f'city-seed{seed}'] = LandSession.new(seed).dump()
    rows = []
    for name, snapshot in scenarios.items():
        for rate in (0, 1, 2, 10, 30):
            row = run(snapshot, name, rate, args.months)
            rows.append(row)
            print(json.dumps(row), flush=True)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(rows, ensure_ascii=False, indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()
