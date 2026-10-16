"""Fixed same-city import experiment, explicit cohorts and right-censoring."""
from pathlib import Path
import json,csv,math,sys
root=Path(__file__).resolve().parents[1];base=root/'Temp/CityScenarios'
out=Path(sys.argv[1]);out.mkdir(parents=True,exist_ok=True)
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def lines(p):return [json.loads(x) for x in p.read_text(encoding='utf-8-sig').splitlines() if x.strip()]
def dist(a):
    a=sorted(a)
    return None if not a else dict(count=len(a),mean=sum(a)/len(a),p50=a[math.ceil(len(a)*.5)-1],p95=a[math.ceil(len(a)*.95)-1],max=a[-1])
construction=read(base/'imports-100k-common/construction.json');budget=read(base/'imports-100k-budget/budget.json')
results=[];cohorts={};initial_facts=read(base/'imports-facts-initial/facts.json')
for n in (1,4,12):
    directory=base/f'imports-100k-run-{n}';r=read(directory/'report.json');events=lines(directory/'trips.jsonl')
    facts=read(base/f'imports-facts-{n}/facts.json')
    assert facts['rawStock']-initial_facts['rawStock']==r['rawDelivered']-(facts['rawConsumed']-initial_facts['rawConsumed']),'Independent raw stock/use balance'
    departures={e['task']:e for e in events if e['eventType']=='depart' and e['kind']=='RawMaterial'}
    completed=[e for e in events if e['eventType']=='arrive' and e['kind']=='RawMaterial' and not e['carry']]
    rawarrivals=[e for e in events if e['eventType']=='arrive' and e['kind']=='RawMaterial']
    carry=[e for e in rawarrivals if e['carry']];ports=[p for p in r['observation']['ports'] if p['active']]
    assert sum(e['cargo'] for e in rawarrivals)==r['rawDelivered']
    assert sum(e['cargo'] for e in departures.values())==r['rawDispatched']
    complete_ids={e['task'] for e in completed};incomplete=[e for k,e in departures.items() if k not in complete_ids]
    expected=sum(x['estimatedRawPerGameDayIfFull'] for x in budget['rows'] if x['id'] in {p['id'] for p in ports})
    demands=lines(directory/'demand.jsonl');retail=lines(directory/'retail.jsonl')
    with (directory/'factories.csv').open(encoding='utf-8-sig',newline='') as f:factories=list(csv.DictReader(f))
    waits=[float(f['observed_wait_seconds']) for f in factories if float(f['observed_wait_seconds'])>0]
    first={}
    for e in completed:
        if e['destination'] not in first:first[e['destination']]=e
    cohorts[n]=dict(first=first,carry={e['task']:e for e in carry})
    with (out/f'ports-{n}.csv').open('w',newline='',encoding='utf-8') as w:
        writer=csv.writer(w);writer.writerow(['port','x','z','active','departed_trucks','departed_raw','arrived_trucks','arrived_raw','new_arrived_trucks','carry_arrived_trucks','slot_occupancy','new_completed_mean_seconds','incomplete_trucks'])
        for p in r['observation']['ports']:writer.writerow([p['id'],p['x'],p['z'],p['active'],p['departed'],p['departedCargo'],p['arrived'],p['arrivedCargo'],p['newArrived'],p['carryArrived'],p['slotOccupancy'],p['newCompletedSeconds']['mean'] if p['newCompletedSeconds'] else None,len(p['incomplete'])])
    results.append(dict(imports=n,report=r,resources=read(directory/'resources.json'),preRunFreeFlowBudget=expected,coldFacts=facts,rawConsumedDelta=facts['rawConsumed']-initial_facts['rawConsumed'],
        observedAttendanceMinutes=facts['attendanceMinutes']-initial_facts['attendanceMinutes'],attendancePotentialRaw=(facts['attendanceMinutes']-initial_facts['attendanceMinutes'])/facts['requiredLabourPerItem'],potentialCaveat='Only labour potential at rate1, NOT actual paid productive input or sales demand. Cash/stock constraints ignored.',
        rawArrivalCargoIncludingCarryIn=sum(e['cargo'] for e in rawarrivals),carryInArrivalCargo=sum(e['cargo'] for e in carry),
        newlyDispatched=len(departures),newCompleted=len(completed),newIncomplete=len(incomplete),
        newArrivalCargo=sum(e['cargo'] for e in completed),meanNewPayload=sum(e['cargo'] for e in departures.values())/len(departures) if departures else None,
        departedRouteWorldUnits=dist([e['routeWorldUnits'] for e in departures.values()]),departedFreeFlowSeconds=dist([e['freeFlowSeconds'] for e in departures.values()]),
        completedNewSeconds=dist([e['duration'] for e in completed]),carryInSeconds=dist([e['duration'] for e in carry]),
        incompleteAges=dist([r['finalTick']*.05-e['departed'] for e in incomplete]),
        completedInflation=dist([e['duration']/e['freeFlowSeconds'] for e in completed]),
        meanSlotOccupancy=sum(p['slotOccupancy'] for p in ports)/len(ports),
        factoryObservedWaiting=dist(waits),demandStart=demands[0],demandEnd=demands[-1],
        paidStaffedNoRaw=dist([d['paidStaffedNoRaw'] for d in demands]),
        stockedShopSamples=sum(any(s['Stock']>0 for s in row['shops']) for row in retail),
        stockedAndOpenSamples=sum(any(s['Stock']>0 and s['open'] for s in row['shops']) for row in retail),
        finalRetail=retail[-1],limitations='One game day from same already-distressed city. Newly completed cohort only is completion-biased; all departures and right-censored trips are reported. No nearest-port policy. Not long-run capacity or FPS.'))
matched=[]
for n in (4,12):
    a=cohorts[1]['first'];b=cohorts[n]['first'];ids=sorted(set(a)&set(b))
    matched.append(dict(imports=n,sharedNewlyServedFactories=len(ids),rows=[dict(factory=i,controlOrigin=a[i]['origin'],variantOrigin=b[i]['origin'],controlDuration=a[i]['duration'],variantDuration=b[i]['duration'],controlFreeFlow=a[i]['freeFlowSeconds'],variantFreeFlow=b[i]['freeFlowSeconds']) for i in ids],
        inheritedSameTasks=[dict(task=i,controlDuration=e['duration'],variantDuration=cohorts[n]['carry'][i]['duration']) for i,e in cohorts[1]['carry'].items() if i in cohorts[n]['carry']]))
summary=dict(construction=construction,budget=budget,initialColdFacts=initial_facts,results=results,matched=matched,productRuleChanges=False,
    comparison='1/4/12 all share the same 11 added paid stubs, registry IDs and public/private funds; only import activation differs. Original city unchanged. Not an algorithm optimization.')
(out/'comparison.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
table=['|进口端点|实际到货原料/游戏日|其中新发货到货|新发/完成/未完车|新完成旅程均秒|全部新发自由流均秒|生产增量|成品到店|消费增量|', '|---|---:|---:|---|---:|---:|---:|---:|---:|']
for x in results:
    r=x['report'];duration=x['completedNewSeconds']['mean'] if x['completedNewSeconds'] else None
    table.append(f"|{x['imports']}|{r['rawPerGameDay']:.1f}|{x['newArrivalCargo']}|{x['newlyDispatched']}/{x['newCompleted']}/{x['newIncomplete']}|{duration:.2f}|{x['departedFreeFlowSeconds']['mean']:.2f}|{r['produced']}|{r['observation']['goodsArrived']}|{r['consumed']}|")
(out/'comparison-table.md').write_text('\n'.join(table)+'\n',encoding='utf-8')
print('\n'.join(table));print('Matched factories:',[(m['imports'],m['sharedNewlyServedFactories']) for m in matched])
