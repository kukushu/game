"""Export small summaries from actual Unity Hierarchy, never commit binary captures.
Usage: python Tools/SummarizeCityCoreProfile.py input_directory output.csv
Inclusive scopes overlap; self scopes and GC rows must be read separately.
"""
import csv, statistics, sys
from pathlib import Path
source, target = map(Path, sys.argv[1:3])
rows=list(csv.DictReader((source/'hierarchy.csv').open(encoding='utf-8-sig')))
samples={}
for row in rows:
    name=row['path'].rsplit('/',1)[-1]
    if name.startswith('City.Attribution.') or name=='GC.Collect':
        samples.setdefault(name,[]).append(row)
step_count=len(samples.get('City.Attribution.Step',[]))
if step_count!=200: raise ValueError(f'Expected 200 real simulation steps; found {step_count}')
target.parent.mkdir(parents=True,exist_ok=True)
with target.open('w',newline='',encoding='utf-8') as stream:
    writer=csv.writer(stream)
    writer.writerow(['module','steps','sampled_frames','mean_per_step_ms','self_per_step_ms','calls_per_step','gc_bytes_per_step','sample_p95_ms','sample_p99_ms'])
    for name,group in samples.items():
        values=sorted(float(row['total_ms']) for row in group)
        sums=[sum(float(row[column]) for row in group)/step_count for column in ['total_ms','self_ms','calls','gc_bytes']]
        writer.writerow([name,step_count,len(group),*(round(value,6) for value in sums),values[int((len(values)-1)*.95)],values[int((len(values)-1)*.99)]])
print(f'Exported {len(samples)} actual Unity module summaries from {step_count} steps to {target}')
