"""Analyze existing zoning edit captures only; never launches a simulation."""
from pathlib import Path
import csv, hashlib, json
from collections import defaultdict
ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'Temp/ZoningOptimization-20261018'
RUNS=('paired-a1','paired-b1','paired-b2','paired-a2')
def read(path):
    with path.open(encoding='utf-8-sig',newline='') as stream:return list(csv.DictReader(stream))
def write(name,rows):
    with (BASE/name).open('w',encoding='utf-8',newline='') as stream:
        writer=csv.DictWriter(stream,fieldnames=list(rows[0]));writer.writeheader();writer.writerows(rows)
def summarize(runs,prefix,required):
    rows=[];states={}
    for run in runs:
        folder=BASE/run
        if not (folder/'Result.txt').exists():continue
        scopes=read(folder/'scopes.csv');frames=read(folder/'frames.csv');operations=read(folder/'operations.csv')
        states[run]=hashlib.sha256((folder/'final.hcity').read_bytes()).hexdigest()
        for operation in operations:
            name=operation['operation'];selected=[r for r in scopes if r['operation']==name]
            def total(scope):return sum(float(r['ms']) for r in selected if r['name']==scope)
            zones=[float(r['ms']) for r in selected if r['name']=='ZoneViews']
            peak=max(float(r['frame_ms']) for r in frames if r['operation']==name)
            rows.append(dict(run=run,variant='old' if '-a' in run else 'new',operation=name,status=operation['status'],
                zoning_updates=len(zones),zoning_cells_ms=total('ZoningCells'),zone_views_ms=sum(zones),zone_views_peak_ms=max(zones,default=0),
                mesh_geometry_ms=total('MeshGeometry'),mesh_upload_ms=total('MeshUpload'),gameobjects_ms=total('GameObjects'),chunk_membership_ms=total('ChunkMembership'),
                construction_ms=total('Construction'),road_views_ms=total('RoadViews'),
                scoped_operation_total_ms=total('Construction')+total('RoadViews')+sum(zones),
                accepted_frame_peak_ms=peak if operation['status']=='accepted' else -1,
                observed_frame_peak_ms=peak,final_sha256=states[run]))
    if rows:write(prefix+'summary.csv',rows)
    if len(states)>1:assert len(set(states.values()))==1,'Native final city state differs: '+prefix
    pixels={}
    if all(run in states for run in required):
        from PIL import Image,ImageChops
        reference=Image.open(BASE/next(iter(states))/'final.png').convert('RGB')
        for run in states:
            candidate=Image.open(BASE/run/'final.png').convert('RGB');assert candidate.size==reference.size,'Screenshot size differs'
            diff=ImageChops.difference(reference,candidate);changed=sum(pixel!=(0,0,0) for pixel in diff.get_flattened_data())
            pixels[run]=dict(changed_pixels=changed,width=reference.width,height=reference.height)
            if changed:diff.save(BASE/(run+'-pixel-diff.png'))
        (BASE/(prefix+'pixel-check.json')).write_text(json.dumps(pixels,indent=2),encoding='utf-8')
    (BASE/(prefix+'state-check.json')).write_text(json.dumps(dict(states=states,all_equal=len(set(states.values()))==1,complete=all(run in states for run in required)),indent=2),encoding='utf-8')
    return dict(available=list(states),summary=rows,pixels=pixels)

results={'30000':summarize(RUNS+('final-a','final-b'),'paired-',RUNS),'50000':summarize(('scale-a','scale-b'),'scale-',('scale-a','scale-b'))}
gc_rows=[]
for run in ('fullraw-a','fullraw-b'):
    for operation in ('add','connect_split','widen','delete','continuous'):
        path=BASE/run/('raw-'+operation)/'hierarchy.csv'
        if not path.exists():continue
        groups=defaultdict(list)
        for row in read(path):
            scope=row['path'].rsplit('/',1)[-1]
            if scope.startswith('City.ZoningEdit.'):groups[scope].append(row)
        for scope,samples in sorted(groups.items()):
            gc_rows.append(dict(run=run,variant='old' if run.endswith('-a') else 'new',operation=operation,scope=scope,
                total_ms=sum(float(r['total_ms']) for r in samples),self_ms=sum(float(r['self_ms']) for r in samples),
                gc_bytes=sum(float(r['gc_bytes']) for r in samples),calls=sum(float(r['calls']) for r in samples)))
if gc_rows:write('gc-summary.csv',gc_rows)
print(json.dumps(results,indent=2))
