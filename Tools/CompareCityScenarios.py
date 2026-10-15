"""Strict paired baseline for unchanged behavior; never uses reduced populations as a faster result."""
from pathlib import Path
import argparse
import json

parser=argparse.ArgumentParser()
parser.add_argument('before',type=Path)
parser.add_argument('after',type=Path)
args=parser.parse_args()
read=lambda folder,name:json.loads((folder/name).read_text(encoding='utf-8-sig'))
a,b=(read(p,'report.json') for p in [args.before,args.after])
for key in ['status','steps','initialHash','finalHash','replayHash']:
    if a[key]!=b[key]:
        raise SystemExit(f'NOT an equivalent pair: {key} differs. Review actual business and safety differences; do not label this an equivalent speedup.')
if a['status']!='passed-invariants' or not all(r['replayChecked'] for r in [a,b]):
    raise SystemExit('Correctness/replay not verified')
print('PASS identical initial, final and in-flight continuation state; same simulated work')
for key in ['allStepMs','stableNonDayBoundaryMs','postRestoreStepMs','dayBoundaryMs','stepAllocatedBytes']:
    print(key)
    if a[key] is None or b[key] is None:
        print('  unavailable counter');continue
    for metric in ['mean','p95','p99','max']:
        old,new=a[key][metric],b[key][metric]
        change=100*(new/old-1) if old else float('nan')
        print(f'  {metric}: {old:.6f} -> {new:.6f} ({change:+.2f}%)')
if a['profile']!=b['profile']:
    print('NOTE: instrumented vs normal host; these differences measure diagnostic overhead, NOT an optimization.')
print('Headless only. No FPS or real-window 1x/3x claim.')
