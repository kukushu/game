"""Analyze existing zoning edit captures only; never launches a simulation."""
from pathlib import Path
import csv, hashlib, json
ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'Temp/ZoningOptimization-20261018'
RUNS=('paired-a1','paired-b1','paired-b2','paired-a2')
def read(path):
    with path.open(encoding='utf-8-sig',newline='') as stream:return list(csv.DictReader(stream))
def write(name,rows):
    with (BASE/name).open('w',encoding='utf-8',newline='') as stream:
        writer=csv.DictWriter(stream,fieldnames=list(rows[0]));writer.writeheader();writer.writerows(rows)
rows=[];states={}
for run in RUNS:
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
            frame_peak_including_failure_validation_ms=peak,
            final_sha256=states[run]))
if rows:write('paired-summary.csv',rows)
if len(states)>1:assert len(set(states.values()))==1,'Native final city state differs'
pixel_results={}
if len(states)==4:
    from PIL import Image,ImageChops
    reference=Image.open(BASE/'paired-a1/final.png').convert('RGB')
    for run in RUNS:
        candidate=Image.open(BASE/run/'final.png').convert('RGB')
        assert candidate.size==reference.size,'Screenshot size differs'
        diff=ImageChops.difference(reference,candidate)
        changed=sum(pixel!=(0,0,0) for pixel in diff.get_flattened_data())
        pixel_results[run]=dict(changed_pixels=changed,width=reference.width,height=reference.height)
        if changed:diff.save(BASE/(run+'-pixel-diff.png'))
    (BASE/'pixel-check.json').write_text(json.dumps(pixel_results,indent=2),encoding='utf-8')
(BASE/'state-check.json').write_text(json.dumps(dict(states=states,all_equal=len(set(states.values()))==1,complete=len(states)==4),indent=2),encoding='utf-8')
print(json.dumps(dict(available=list(states),summary=rows,pixels=pixel_results),indent=2))
