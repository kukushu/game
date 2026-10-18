"""Summarize retained captures only; never runs a benchmark or edits evidence."""
import csv
import hashlib
import json
from collections import defaultdict
from pathlib import Path
import sys

base = Path(sys.argv[1] if len(sys.argv) > 1 else 'Temp/ConstructionCopies-20261018')

def read(path):
    with path.open(encoding='utf-8-sig', newline='') as stream:
        return list(csv.DictReader(stream))

def write(name, rows, columns):
    with (base / name).open('w', encoding='utf-8', newline='') as stream:
        writer = csv.DictWriter(stream, fieldnames=columns)
        writer.writeheader()
        writer.writerows(rows)

measurements, stages, allocations = [], [], []
for directory in sorted(base.iterdir()):
    if not (directory / 'Result.txt').exists() or not (directory / 'scopes.csv').exists():
        continue
    scopes, frames = read(directory / 'scopes.csv'), read(directory / 'frames.csv')
    environment = json.loads((directory / 'environment.json').read_text(encoding='utf-8-sig'))
    mode = 'running' if environment.get('running') or any(f['operation'] == 'normal' for f in frames) else 'paused_attribution'
    if directory.name == 'running-a1':
        mode = 'excluded_concurrent_build'
    for operation in read(directory / 'operations.csv'):
        name = operation['operation']
        times = [float(s['ms']) for s in scopes if s['operation'] == name and s['name'] == 'Construction']
        selected = [f for f in frames if f['operation'] == name]
        normal = [float(f['frame_ms']) for f in frames if f['operation'] == 'normal']
        measurements.append(dict(run=directory.name, mode=mode, operation=name, status=operation['status'],
            fine=int(bool(environment.get('fine'))), raw=int(bool(environment.get('raw'))),
            transaction_peak_ms=max(times, default=-1), transaction_sum_ms=sum(times),
            player_frame_peak_ms=max((float(f['frame_ms']) for f in selected), default=-1),
            normal_frame_peak_ms=max(normal, default=-1), click_to_input_ms=-1,
            tick_min=min((int(f.get('tick', -1)) for f in selected), default=-1),
            tick_max=max((int(f.get('tick', -1)) for f in selected), default=-1),
            active_min=min((int(f.get('active', -1)) for f in selected), default=-1)))
    totals = defaultdict(lambda: [0., 0])
    for row in scopes:
        key = row['operation'], row['name']
        totals[key][0] += float(row['ms'])
        totals[key][1] += 1
    stages.extend(dict(run=directory.name, operation=op, stage=stage, inclusive_ms=ms, calls=calls)
        for (op, stage), (ms, calls) in sorted(totals.items()))
    if (directory / 'hierarchy.csv').exists():
        totals = defaultdict(lambda: [0., 0, 0])
        for row in read(directory / 'hierarchy.csv'):
            path = row['path']
            # Only construction descendants; exclude next-frame simulation and
            # zoning. Inclusive parents must not be summed into a grand total.
            if 'City.ZoningEdit.Construction' not in path or not path.split('/')[-1].startswith('City.ZoningEdit.'):
                continue
            key = path.split('/')[-1].removeprefix('City.ZoningEdit.')
            totals[key][0] += float(row['total_ms'])
            totals[key][1] += int(row['gc_bytes'])
            totals[key][2] += int(row['calls'])
        allocations.extend(dict(run=directory.name, stage=stage, inclusive_ms=ms, allocated_bytes=allocated, calls=calls)
            for stage, (ms, allocated, calls) in sorted(totals.items()))

write('measurements.csv', measurements, ['run','mode','operation','status','fine','raw','transaction_peak_ms','transaction_sum_ms','player_frame_peak_ms','normal_frame_peak_ms','click_to_input_ms','tick_min','tick_max','active_min'])
write('stage-summary.csv', stages, ['run','operation','stage','inclusive_ms','calls'])
write('gc-summary.csv', allocations, ['run','stage','inclusive_ms','allocated_bytes','calls'])
pairs = {}
for run in ('baseline-complete','owned-stages','geometry-stages','spatial-stages','draft-stages'):
    path = base / run / 'final.hcity'
    if path.exists():
        pairs[run] = hashlib.sha256(path.read_bytes()).hexdigest()
assert len(set(pairs.values())) <= 1, 'Paused native complete state differs'
replays = {}
for run in ('replay-before','replay-after','replay-geometry','replay-spatial'):
    path = base / run / 'replay.csv'
    if path.exists():
        replays[run] = read(path)
assert all(rows == next(iter(replays.values())) for rows in replays.values()), 'Deterministic full replay differs'
expanded = {}
for run in ('expanded-old','expanded-utility','expanded-draft','expanded-release'):
    if (base / run / 'Result.txt').exists():
        expanded[run] = dict(states=read(base/run/'replay.csv'), outcomes=read(base/run/'expanded-operations.csv'))
assert all(rows == next(iter(expanded.values())) for rows in expanded.values()), 'Expanded frozen/current whole-state or outcome differs'
scale_replays = {}
for run in ('scale-replay-old','scale-replay-new'):
    if (base / run / 'Result.txt').exists():
        scale_replays[run] = dict(states=read(base/run/'replay.csv'), outcomes=read(base/run/'expanded-operations.csv'))
assert all(rows == next(iter(scale_replays.values())) for rows in scale_replays.values()), '50000 frozen/current whole-state or outcome differs'
(base / 'state-check.json').write_text(json.dumps(dict(paused_native=pairs, deterministic_replay=replays,
    expanded=expanded, scale_replays=scale_replays,
    note='Realtime running captures occur at different wall-clock ticks and are not full-save equivalence pairs. Assertions/save IO excluded from running construction frames; click-to-input unknown=-1.'), indent=2), encoding='utf-8')
print(f'Summarized {len(measurements)} operations; exact paired state checks passed')
