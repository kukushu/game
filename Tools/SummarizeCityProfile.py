"""Summarize measured fixed steps and Unity hierarchy; do not infer missing allocation."""
import csv,json,statistics,argparse
from pathlib import Path
parser=argparse.ArgumentParser();parser.add_argument('source',type=Path);parser.add_argument('output',type=Path);args=parser.parse_args();args.output.mkdir(parents=True,exist_ok=True)
rows=list(csv.DictReader((args.source/'steps.csv').open(encoding='utf-8-sig')))
assert rows and len({r['tick'] for r in rows})==len(rows)
assert all(int(r['Step_calls'])==1 for r in rows)
parts=[k[:-3] for k in rows[0] if k.endswith('_ms') and not k.endswith('_self_ms')]
hierarchy=list(csv.DictReader((args.source/'hierarchy.csv').open(encoding='utf-8-sig')))
def pct(values,p):return sorted(values)[int((len(values)-1)*p)]
summary=[]
for part in parts:
    values=[float(r[part+'_ms']) for r in rows];selfs=[float(r[part+'_self_ms']) for r in rows]
    samples=[r for r in hierarchy if r['path'].split('/')[-1]=='City.Attribution.'+part and 'City.Attribution.Step' in r['path'].split('/')]
    calls=sum(float(r['calls']) for r in samples);gc=sum(float(r['gc_bytes']) for r in samples)
    assert int(calls)==sum(int(r[part+'_calls']) for r in rows),part
    summary.append(dict(module=part,mean_ms=statistics.mean(values),self_mean_ms=statistics.mean(selfs),p95_ms=pct(values,.95),p99_ms=pct(values,.99),max_ms=max(values),calls=int(calls),unity_gc_total_bytes=int(gc),unity_gc_bytes_per_fixed_step=gc/len(rows)))
with (args.output/'modules.csv').open('w',newline='',encoding='utf-8') as f:
    w=csv.DictWriter(f,fieldnames=list(summary[0]));w.writeheader();w.writerows(summary)
spikes=sorted(rows,key=lambda r:float(r['Step_ms']),reverse=True)[:12]
keys=['tick','active','Step_ms','Step_bytes','gc0','gc1','gc2']+[p+'_self_ms' for p in parts if p!='Step']
with (args.output/'spikes.csv').open('w',newline='',encoding='utf-8') as f:
    w=csv.DictWriter(f,fieldnames=keys,extrasaction='ignore');w.writeheader();w.writerows(spikes)
meta=dict(steps=len(rows),active_peak=max(int(r['active']) for r in rows),tick_first=int(rows[0]['tick']),tick_last=int(rows[-1]['tick']),allocation_counter_supported=all(r.get('allocation_counter_supported')=='1' for r in rows),gc_note='Unity/Boehm generation counters may alias; do not sum gc0+gc1+gc2. Hierarchy GC allocations are inclusive, not additive between parent/child modules.',scope_note='Instrumented fixed-step times, not normal FPS; CSV writing/rendering outside Step. Raw scope bytes are unavailable when probe fails; use Unity hierarchy.')
(args.output/'metadata.json').write_text(json.dumps(meta,indent=2)+'\n',encoding='utf-8');print(json.dumps(meta))
