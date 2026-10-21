"""Independently verify real-window CSV accounting, not a performance threshold."""
import argparse
import csv
import json
import math
import hashlib
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('results', type=Path)
args = parser.parse_args()
metadata = json.loads((args.results / 'environment.json').read_text(encoding='utf-8-sig'))
for number in metadata.get('rates', [1, 3]):
    rate = f'{number}x'
    folder = args.results / rate
    rows = list(csv.DictReader((folder / 'frames.csv').read_text(encoding='utf-8-sig').splitlines()))
    result = next(csv.DictReader((folder / 'summary.csv').read_text(encoding='utf-8-sig').splitlines()))
    assert rows and (folder / 'Result.txt').read_text().startswith('PASS '), 'Missing successful model validation'
    resources = json.loads((args.results / f'{rate}-resources.json').read_text(encoding='utf-8-sig'))
    assert resources.get('exitCode', 0) == 0, 'Player did not exit successfully'
    lifecycle = args.results / rate / 'process-exit.json'
    if lifecycle.exists():
        assert json.loads(lifecycle.read_text(encoding='utf-8-sig'))['normalExit'], 'Abnormal/interrupted Player run; do not report PASS'
    assert not resources.get('terminatedBy'), 'Measurement may exist but Player did not exit normally'
    assert (folder / 'window.png').is_file() and (folder / 'final.hcity').stat().st_size > 0
    assert result['input_sha256'].lower() == metadata['inputHash'].lower(), 'Different city input'
    previous = int(result['start_tick'])
    for row in rows:
        if 'actual_rate' in row:
            assert float(row['actual_rate']) == number, 'User input changed benchmark speed'
        tick = int(row['tick'])
        assert tick - previous == int(row['fixed_steps']), 'Tick change differs from actual host steps'
        assert int(row['active_vehicles']) == int(row['vehicle_views']), 'Vehicles missing from normal views'
        previous = tick
    assert previous == int(result['end_tick'])
    close = lambda a,b: math.isclose(float(a), float(b), rel_tol=1e-8, abs_tol=1e-7)
    elapsed = float(result['real_seconds'])
    if number == 0:
        assert previous == int(result['start_tick']) and all(float(r.get('requested_seconds',0)) == 0 for r in rows), 'Paused-labelled run advanced the city'
        expected_hash = ((folder / 'warm-state.sha256').read_text().strip()
                         if metadata.get('warmupSimulationSteps', 0) else metadata['inputHash'])
        assert hashlib.sha256((folder / 'final.hcity').read_bytes()).hexdigest().lower() == expected_hash.lower(), 'Paused observation changed complete saved facts'
    elif all('requested_seconds' in r for r in rows):
        requested = sum(float(r['requested_seconds']) for r in rows)
        edge = max(float(r['frame_ms']) for r in rows) / 1000 * number + .01
        assert abs(requested - elapsed * number) <= edge, 'Requested time does not match benchmark rate'
    times = sorted(float(r['frame_ms']) for r in rows)
    assert close(sum(times) / 1000, elapsed) and close(rows[-1]['real_seconds'], elapsed)
    assert close(len(rows)/elapsed, result['fps'])
    assert close((previous-int(result['start_tick']))*.05/elapsed, result['sim_ratio'])
    assert close(times[math.ceil(len(times)*.95)-1], result['p95_ms'])
    assert close(times[math.ceil(len(times)*.99)-1], result['p99_ms'])
    valid = [int(r['gc_bytes']) for r in rows if int(r['gc_bytes']) >= 0]
    assert len(valid) == int(result['gc_samples'])
    expected = sum(valid)/len(valid) if len(valid) == len(rows) else -1
    assert close(expected, result['gc_bytes_per_frame']), 'Unavailable GC counter misreported'
    if (folder / 'clock-balance.json').is_file():
        clock = json.loads((folder / 'clock-balance.json').read_text())
        initial = float(clock['initial_pending_seconds'])
        pending = initial
        requested = 0.0
        for row in rows:
            requested += float(row['requested_seconds'])
            pending += float(row['requested_seconds']) - int(row['fixed_steps']) * .05
            assert close(pending, row['pending_seconds']), 'Time debt lost within a rendered frame'
        assert close(requested, clock['requested_seconds']) and close(pending, clock['final_pending_seconds'])
        assert int(clock['completed_steps']) == previous - int(result['start_tick'])
        assert abs(float(clock['balance_error_seconds'])) < 1e-6
    print(f'PASS {rate}: {len(rows)} actual frames, {previous-int(result["start_tick"])} steps, every vehicle view present; frame/GC/multiplier summary matches raw CSV')
print('No mouse acceptance or economic-health/performance-threshold claim.')
if metadata.get('hostSource'):
    print('Diagnostic presentation source override: this is an ablation/control, not full normal rendering performance.')
