"""Summarize preserved native/steady measurements, without launching any tests."""
import csv, hashlib, json, statistics
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / 'Temp/RoadPreviewOptimization-20261018'

def rows(path):
    with path.open(encoding='utf-8-sig', newline='') as stream:
        return list(csv.DictReader(stream))

def write(name, data):
    with (BASE / name).open('w', encoding='utf-8', newline='') as stream:
        writer = csv.DictWriter(stream, fieldnames=list(data[0]))
        writer.writeheader()
        writer.writerows(data)

native = []
for population in ('3000', '10000', '30000'):
    scopes = rows(BASE / 'native-final' / population / 'scopes.csv')
    def scope(phase, name):
        return sum(float(r['ms']) for r in scopes if r['phase'] == phase and r['name'] == name)
    native.append(dict(scenario_population=int(population),
        first_action_preview_ms=scope('preview0', 'ActionPreview'),
        warm_action_preview_median_ms=statistics.median(scope(p, 'ActionPreview') for p in ('preview1', 'preview2')),
        warm_preview_median_ms=statistics.median(scope(p, 'Preview') for p in ('preview1', 'preview2')),
        click_plan_ms=scope('click', 'ActionPlan'), click_check_ms=scope('click', 'ActionCheck'),
        commit_ms=scope('click', 'ActionCommit'), road_views_ms=scope('click', 'ActionRoadViews'),
        zone_views_ms=scope('nextFrame', 'ZoneViews'), first_post_edit_step_ms=scope('postEditStep0', 'ActionFullStep')))
write('native-final-summary.csv', native)
steady = []
for label in ('a1', 'b1', 'b2', 'a2'):
    folder = BASE / ('steady-final-' + label)
    samples = rows(folder / 'steps.csv')
    times = sorted(float(r['ms']) for r in samples)
    steady.append(dict(run=label, variant='old' if label.startswith('a') else 'new', steps=len(samples),
        mean_ms=statistics.mean(times), p99_ms=times[int(len(times)*.99)-1],
        allocated_per_step_bytes=statistics.mean(float(r['allocated']) for r in samples),
        final_sha256=hashlib.sha256((folder / 'final.hcity').read_bytes()).hexdigest()))
assert len({r['final_sha256'] for r in steady}) == 1, 'Steady final state differs'
write('steady-final-summary.csv', steady)
print(json.dumps(dict(native=native, steady=steady), indent=2))
