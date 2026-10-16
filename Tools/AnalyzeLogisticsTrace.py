"""Summarize bounded actual-city observations; no simulation or game edits."""
import json, sys, hashlib
from pathlib import Path

root=Path(__file__).resolve().parents[1]
base=root/'Temp/CityScenarios'
first=base/'logistics-attribution-trace'
second=base/'logistics-attribution-arrivals'
origin=base/'logistics-attribution-origin-final'
out=Path(sys.argv[1]);out.mkdir(parents=True,exist_ok=True)
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def lines(p):return [json.loads(x) for x in p.read_text(encoding='utf-8-sig').splitlines() if x.strip()]
def sha(p):
    h=hashlib.sha256()
    with p.open('rb') as f:
        for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
    return h.hexdigest()
initial=read(first/'initial.json');endpoints={v['task']:v for v in read(origin/'health.json')['rawDetails']}
events={r['task']:r for r in lines(second/'arrivals.jsonl')}
part1=lines(first/'trajectories.jsonl');part2=lines(second/'trajectories.jsonl')
rows=[]
for v in initial['vehicles']:
    task=v['task'];observed=[(initial['tick'],v)]+[(r['tick'],x) for r in part1 for x in r['vehicles'] if x['task']==task]
    retired=next(((t,x) for t,x in observed if x['retired']),None)
    live=[(t,x) for t,x in observed if not x['retired']]
    moved1=live[-1][1]['measuredTravelled'];duration1=(live[-1][0]-initial['tick'])*.05
    stationary=sum(b['measuredTravelled']-a['measuredTravelled']<1e-8 for (_,a),(_,b) in zip(live,live[1:]))
    arrival=events.get(task);duration=None;distance=None
    if arrival:
        duration=arrival['seconds']-initial['tick']*.05;distance=moved1+arrival['travelledDuringProbe']
    rows.append(dict(task=task,vehicle=v['vehicle'],kind=v['kind'],ageAtFailureSeconds=v['age'],remainingWorldUnits=v['remainingMetres'],freeFlowRemainingSeconds=v['freeFlowRemainingSeconds'],
        endpointRouteWorldUnits=endpoints[task].get('endpointRouteWorldUnits',endpoints[task].get('endpointRouteMetres')),endpointFreeFlowSeconds=endpoints[task]['endpointFreeFlowSeconds'],
        completedByFinal=True if arrival or retired else False,arrivalTick=arrival['tick'] if arrival else None,
        arrivalTickInterval=[live[-1][0]+1,retired[0]] if retired and not arrival else None,
        totalTripSecondsInterval=[v['age']+(live[-1][0]+1-initial['tick'])*.05,v['age']+(retired[0]-initial['tick'])*.05] if retired and not arrival else None,
        totalTripSeconds=arrival['elapsed'] if arrival else None,
        measuredTravelledWorldUnits=distance if distance is not None else moved1,
        observedSeconds=duration if duration is not None else duration1,
        measuredMeanWorldUnitsPerSecond=distance/duration if duration else moved1/duration1,
        meanNote='exact longitudinal motion across both probes until arrival' if arrival else 'exact mean in observed partial window; final sub-second arrival interval excluded, recorded distance is a lower bound only',
        stationarySampleIntervals=stationary,sampleIntervals=len(live)-1,stationaryNote='first probe only; roughly one-second snapshots, not per-step stop duration'))
graphs=lines(first/'blockers.jsonl')+lines(second/'blockers.jsonl')
retail=lines(first/'retail.jsonl')+lines(second/'retail.jsonl')
stocked=[r for r in retail if any(b['Stock']>0 for b in r['stocked'])]
shopping=[r for r in stocked if r['minute']>=r['shoppingStart']]
matches=[dict(label='midpoint tick 7201',trace=sha(first/'final.hcity'),reference=sha(base/'challenge100000-continuation/inflight.hcity')),
         dict(label='final tick 9600',trace=sha(second/'final.hcity'),reference=sha(base/'challenge100000-continuation/final.hcity'))]
assert all(m['trace']==m['reference'] for m in matches),'Diagnostic observation changed complete city state'
assert all(r['completedByFinal'] for r in rows),'Do not claim all original freight arrived'
report=dict(units='Distances are world units, NOT calibrated metres. Early raw trace field names containing Metres are historical naming only.',
    source='Original failed city tick4800; actual normal CitySimulation, 2401+2399 fixed steps; no stock/money/clock/road edits',
    completeSaveMatches=matches,originalFreight=rows,
    blockerGraphs=dict(captures=len(graphs),reachableCycles=sum(len(g['cycles']) for g in graphs),maxReachableActors=max(g['reachableActors'] for g in graphs),
        caveats='Instantaneous dependency graphs sampled every100 steps. Not proof of absence of transient cycles or unrelated city deadlock. First two traces conservatively include source-following candidate during shadow changes; final anchor-check refines this predicate. All original freight actually arrived.'),
    retail=dict(firstSampleWithGoods=stocked[0] if stocked else None,shoppingTimeStockedSamples=len(shopping),anyOpenInThoseSamples=any(b['open'] for r in shopping for b in r['stocked'] if b['Stock']>0),eligibleFamiliesRange=[min(r['eligibleFamilies'] for r in shopping),max(r['eligibleFamilies'] for r in shopping)] if shopping else None,consumed=retail[-1]['consumed']),
    performance=dict(originalContinuation=read(base/'challenge100000-continuation/report.json'),firstInstrumented=read(first/'summary.json'),secondInstrumented=read(second/'summary.json'),caveat='No product optimization this batch. Trace timings include observation hooks; original continuation overlaps visible viewer. Not an isolated speedup comparison. GC pause unknown.'),
    resources=[read(first/'resources.json'),read(second/'resources.json')])
(out/'attribution.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
table=['|任务 / 车辆|货物|失败时剩余距离|剩余自由流秒|端点路线距离 / 自由流秒|交付 tick|总旅程秒|续跑观测均速|',
       '|---|---|---:|---:|---|---|---:|---:|']
for r in rows:
    tick=str(r['arrivalTick']) if r['arrivalTick'] else str(r['arrivalTickInterval'])
    age=f"{r['totalTripSeconds']:.2f}" if r['totalTripSeconds'] is not None else '–'.join(f'{n:.2f}' for n in r['totalTripSecondsInterval'])
    table.append(f"|{r['task']} / {r['vehicle']}|{r['kind']}|{r['remainingWorldUnits']:.2f}|{r['freeFlowRemainingSeconds']:.2f}|{r['endpointRouteWorldUnits']:.2f} / {r['endpointFreeFlowSeconds']:.2f}|{tick}|{age}|{r['measuredMeanWorldUnitsPerSecond']:.3f}|")
(out/'freight-table.md').write_text('\n'.join(table)+'\n',encoding='utf-8')
print(json.dumps(dict(matches=matches,freight=rows,retail=report['retail'],graphs=report['blockerGraphs']),ensure_ascii=False))
