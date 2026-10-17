import csv,json
from pathlib import Path
root=Path('Temp/RoadBuildDiagnosis-20261017/10000-deep-raw')
rows=list(csv.DictReader((root/'hierarchy.csv').open()))
summary=[]
for f in ['0','1','2']:
 r=[x for x in rows if x['frame']==f and x['thread']=='Main Thread']
 def total(s):return sum(float(x['total_ms']) for x in r if x['path'].endswith(s))
 def alloc(s):return sum(float(x['gc_bytes']) for x in r if x['path'].endswith(s))
 summary.append(dict(frame=int(f),preview_ms=total('City.RoadBuild.ActionPreview'),network_ms=total('City.RoadBuild.NetworkCtor'),conflicts_ms=total('City.RoadBuild.ConnectionConflicts'),pairs_ms=total('City.RoadBuild.ConflictPairs'),gc_collect_ms=total('/GC.Collect'),preview_gc_bytes=alloc('City.RoadBuild.ActionPreview'),note='GC marker total, not proven complete pause total; GPU unknown'))
(root/'raw-summary.json').write_text(json.dumps(summary,indent=2))
print(json.dumps(summary,indent=2))
