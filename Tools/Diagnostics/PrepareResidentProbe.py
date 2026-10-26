"""Patch only a disposable Temp Unity project, never the source Assets."""
from pathlib import Path
import sys, subprocess
root, project, variant = Path(sys.argv[1]).resolve(), Path(sys.argv[2]).resolve(), sys.argv[3]
assert project.is_relative_to(root / 'Temp')
assert variant in ('stock', 'detail', 'memo', 'legacy', 'retry')
for name in ('ResidentRequestProbe.cs', 'HarborCityResidentWindowProbe.cs', 'ResidentDispatchTrace.cs'):
    (project/'Assets/Scripts/Diagnostics/Runtime'/name).write_text((root/'Tools/Diagnostics'/name).read_text(encoding='utf-8-sig'), encoding='utf-8')
host=project/'Assets/Scripts/Runtime/Presentation/Game/HarborCityRoadCoreGame.cs'
text=host.read_text(encoding='utf-8-sig')
assert 'AddComponent<HarborCityScenarioWindowProbe>()' in text
host.write_text(text.replace('AddComponent<HarborCityScenarioWindowProbe>()','AddComponent<HarborCityResidentWindowProbe>()'),encoding='utf-8')
path=project/'Assets/Scripts/Runtime/Simulation/Residents/CityCoreResidents.cs'
if len(sys.argv)>4 and sys.argv[4]:
    frozen=subprocess.check_output(['git','-C',str(root),'show',sys.argv[4]+':Assets/Scripts/Runtime/Simulation/Residents/CityCoreResidents.cs'])
    path.write_bytes(frozen)
text=path.read_text(encoding='utf-8-sig');a=text.index('        public void AdvanceRequests()');b=text.index('        public ResidentActivity Observe(',a)
body=text[a:b].replace('            if(!Enabled)return;','            using var residentDiagnostic=ResidentRequestProbe.Advance();\n            if(!Enabled)return;')
body=body.replace('RequestCommute(p.id,','using var dispatchDiagnostic=ResidentRequestProbe.Dispatch();RequestCommute(p.id,')
if variant=='memo':
    assert 'continue;float shift=' in body, 'Use an unoptimized SourceReference for the historical memo ablation'
    # Frozen diagnostic ablation of redundant pure reads. Not shipped as an optimization.
    body=body.replace('continue;float shift=', 'continue;var diagnosticJob=city.ResidentJob(p);int diagnosticWork=diagnosticJob?.buildingId??-1;float shift=')
    body=body.replace('city.Workplace(p)','diagnosticWork').replace('city.ResidentJob(p))','diagnosticJob)')
if variant=='detail':
    assert 'continue;float shift=' in body, 'Historical fine-grained detail requires the unoptimized SourceReference'
    body=body.replace('var p=member.Person;', 'using var personDiagnostic=ResidentRequestProbe.Person();var p=member.Person;')
    body=body.replace('if(!h.resident||p.dead||p.tripId!=0||transport.Clock<p.retryAt||transport.Clock<p.visitUntil)continue;float shift=', 'if(!ResidentRequestProbe.Gate(h,p,transport))continue;ResidentRequestProbe.Next(ResidentRequestProbe.Phase.Shift);float shift=')
    body=body.replace('bool returning=', 'ResidentRequestProbe.Next(ResidentRequestProbe.Phase.Return);bool returning=')
    body=body.replace('bool eligible=', 'ResidentRequestProbe.Next(ResidentRequestProbe.Phase.Eligible);bool eligible=')
    body=body.replace(';double estimate=', ';ResidentRequestProbe.Next(ResidentRequestProbe.Phase.Estimate);double estimate=')
    body=body.replace('if(p.observedHome', 'ResidentRequestProbe.Next(ResidentRequestProbe.Phase.Observed);if(p.observedHome')
    body=body.replace('bool leaving=', 'ResidentRequestProbe.Next(ResidentRequestProbe.Phase.Leaving);bool leaving=')
    body=body.replace('if(!returning&&!leaving)', 'ResidentRequestProbe.Results(returning,eligible,leaving);if(!returning&&!leaving)')
    body=body.replace('p.retryAt=(float)', 'ResidentRequestProbe.Next(ResidentRequestProbe.Phase.Dispatch);p.retryAt=(float)')
    for old,new in [('city.Workplace(p)','ResidentRequestProbe.Workplace(city,p)'),('city.ResidentJob(p)','ResidentRequestProbe.Job(city,p)'),('city.GetBuilding(','ResidentRequestProbe.Building(city,'),('entities.Employment.ShiftStart(','ResidentRequestProbe.Shift(entities.Employment,'),('entities.Employment.ShiftMinutes','ResidentRequestProbe.Minutes(entities.Employment)'),('city.CanAttendWork(p)','ResidentRequestProbe.Attend(city,p)'),('city.JobFunded(','ResidentRequestProbe.Funded(city,')]:
        body=body.replace(old,new)
    jobs=project/'Assets/Scripts/Runtime/Simulation/Residents/CityJobs.cs'
    j=jobs.read_text(encoding='utf-8-sig');needle='public CityJob Job(int id)';i=j.index('{',j.index(needle))+1;j=j[:i]+'\n            ResidentRequestProbe.Lookup(id);'+j[i:];jobs.write_text(j,encoding='utf-8')
if variant=='retry':
    assert 'int workplace=job?.buildingId??-1;' in body
    body=body.replace('if(!returning&&!leaving)continue;', 'ResidentDispatchTrace.Qualification(city,transport,p,h,workplace,minute,shift,end,estimate,eligible,returning,leaving);if(!returning&&!leaving)continue;')
    simulation=project/'Assets/Scripts/Runtime/Simulation/Core/CitySimulation.cs'
    sim=simulation.read_text(encoding='utf-8-sig')
    old='var admission=transport.PrepareTransport(origin,destination,cargo,price,purpose,citizen);if(admission==null)return null;transport.CheckAdmission(admission);if(admission.vehicle.Task.Shipment!=null)admission.vehicle.Task.Shipment.kind=kind;if(!business.Depart(admission.vehicle.Task))return null;return transport.CommitAdmission(admission);'
    new='long traceStarted=ResidentDispatchTrace.Start(purpose);var admission=transport.PrepareTransport(origin,destination,cargo,price,purpose,citizen,out var rejection);if(admission==null){ResidentDispatchTrace.Record(city,transport,citizen,purpose,origin,destination,rejection,false,false,traceStarted);return null;}transport.CheckAdmission(admission);if(admission.vehicle.Task.Shipment!=null)admission.vehicle.Task.Shipment.kind=kind;if(!business.Depart(admission.vehicle.Task)){ResidentDispatchTrace.Record(city,transport,citizen,purpose,origin,destination,TransportRejection.None,false,true,traceStarted);return null;}var result=transport.CommitAdmission(admission);ResidentDispatchTrace.Record(city,transport,citizen,purpose,origin,destination,TransportRejection.None,true,false,traceStarted);return result;'
    assert old in sim
    simulation.write_text(sim.replace(old,new),encoding='utf-8')
    world=project/'Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs'
    w=world.read_text(encoding='utf-8-sig');needle='        void Reindex()\n        {';assert needle in w
    world.write_text(w.replace(needle,needle+'\n            using var indexTrace=ResidentDispatchTrace.Index();'),encoding='utf-8')
path.write_text(text[:a]+body+text[b:],encoding='utf-8')
print('Prepared Temp-only',variant,project)
