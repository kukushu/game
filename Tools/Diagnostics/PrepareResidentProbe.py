"""Patch only a disposable Temp Unity project, never the source Assets."""
from pathlib import Path
import sys, subprocess
root, project, variant = Path(sys.argv[1]).resolve(), Path(sys.argv[2]).resolve(), sys.argv[3]
assert project.is_relative_to(root / 'Temp')
assert variant in ('stock', 'detail', 'memo', 'legacy', 'retry', 'occupancy', 'occupancy-census', 'occupancy-trim', 'occupancy-active', 'latest', 'growth')
for name in ('ResidentRequestProbe.cs', 'HarborCityResidentWindowProbe.cs', 'ResidentDispatchTrace.cs', 'OccupancyMovementProbe.cs'):
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
if variant=='latest':
    assert 'int workplace=job?.buildingId??-1;' in body
    body=body.replace('var p=member.Person;', 'using var personDiagnostic=ResidentRequestProbe.Person();var p=member.Person;')
    gate='if(!h.resident||p.dead||p.tripId!=0||transport.Clock<p.retryAt||transport.Clock<p.visitUntil)continue;'
    assert gate in body;body=body.replace(gate,'if(!ResidentRequestProbe.Gate(h,p,transport))continue;ResidentRequestProbe.Next(ResidentRequestProbe.Phase.Eligible);')
    for token,phase_name in [('float shift=','Shift'),('bool returning=','Return'),('double estimate=','Estimate'),('if(p.observedHome','Observed'),('bool leaving=','Leaving')]:
        body=body.replace(token,'ResidentRequestProbe.Next(ResidentRequestProbe.Phase.'+phase_name+');'+token)
    body=body.replace('if(!returning&&!leaving)continue;', 'ResidentRequestProbe.Results(returning,eligible,leaving);ResidentDispatchTrace.Qualification(city,transport,p,h,workplace,minute,shift,end,estimate,eligible,returning,leaving);if(!returning&&!leaving)continue;')
    body=body.replace('p.retryAt=(float)', 'ResidentRequestProbe.Next(ResidentRequestProbe.Phase.Dispatch);p.retryAt=(float)')
    for old_read,new_read in [('city.ResidentJob(p)','ResidentRequestProbe.Job(city,p)'),('city.GetBuilding(','ResidentRequestProbe.Building(city,'),('entities.Employment.ShiftStart(','ResidentRequestProbe.Shift(entities.Employment,'),('entities.Employment.ShiftMinutes','ResidentRequestProbe.Minutes(entities.Employment)'),('city.CanAttendWork(p)','ResidentRequestProbe.Attend(city,p)'),('city.JobFunded(','ResidentRequestProbe.Funded(city,')]:body=body.replace(old_read,new_read)
    jobs=project/'Assets/Scripts/Runtime/Simulation/Residents/CityJobs.cs';j=jobs.read_text(encoding='utf-8-sig');i=j.index('{',j.index('public CityJob Job(int id)'))+1;j=j[:i]+'\n            ResidentRequestProbe.Lookup(id);'+j[i:];jobs.write_text(j,encoding='utf-8')
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
if variant in ('occupancy','occupancy-census','occupancy-trim','occupancy-active'):
    from PrepareOccupancyProbe import instrument
    instrument(project, counting=variant=='occupancy-census', trim=variant in ('occupancy-trim','occupancy-active'), active_sort=variant=='occupancy-active')
print('Prepared Temp-only',variant,project)

if variant=='legacy':
    partial=project/'Assets/Scripts/Runtime/Traffic/CityRoadCoreOccupancy.cs'
    w=partial.read_text(encoding='utf-8-sig');needle='            IncrementalIndexUpdates++;'
    assert needle in w
    partial.write_text(w.replace(needle,'using var deltaAttribution=CityCoreProfile.Measure(CityCoreProfile.Part.Occupancy);'+needle),encoding='utf-8')

if variant=='growth':
    (project/'Assets/Scripts/Diagnostics/Runtime/GrowthBoundaryProbe.cs').write_text((root/'Tools/Diagnostics/GrowthBoundaryProbe.cs').read_text(encoding='utf-8'),encoding='utf-8')
    g=project/'Assets/Scripts/Runtime/Simulation/Growth/CityCoreGrowth.cs';v=g.read_text(encoding='utf-8-sig')
    needle='void AdvanceGrowth(TimeContext time)\n        {';assert needle in v;v=v.replace(needle,needle+'using var growthDetail=GrowthBoundaryProbe.Begin(time.Tick);')
    v=v.replace('            RefreshStatistics();','            using(GrowthBoundaryProbe.Measure(GrowthBoundaryProbe.Part.Statistics)){RefreshStatistics();}')
    v=v.replace('city.DevelopmentDemand();var candidates=Zoning.Cells();','using(GrowthBoundaryProbe.Measure(GrowthBoundaryProbe.Part.Demand)){city.DevelopmentDemand();}var cellDetail=GrowthBoundaryProbe.Measure(GrowthBoundaryProbe.Part.Cells);var candidates=Zoning.Cells();cellDetail.Dispose();using var searchDetail=GrowthBoundaryProbe.Measure(GrowthBoundaryProbe.Part.Search);')
    v=v.replace('var size=CityModel.GrowthFootprints[','GrowthBoundaryProbe.Size();var size=CityModel.GrowthFootprints[').replace('candidates.Where(c=>c.row==0&&c.available&&c.use==use)','candidates.Where(c=>GrowthBoundaryProbe.Candidate(c,use))')
    g.write_text(v,encoding='utf-8')
    for file,methods in [('CityCoreZoningGeometry.cs', [('internal ZoneCell[] GrowthFootprint(', 'Footprint'),('internal bool OverlapsOtherPlot(', 'Overlap')]),('CityCoreZoning.cs',[('internal bool CanPlace(', 'CanPlace'),('internal SpatialHit? Entrance(', 'Entrance')])]:
        g=project/'Assets/Scripts/Runtime/Infrastructure/Zoning'/file;v=g.read_text(encoding='utf-8-sig')
        for signature,part in methods:
            a=v.index('{',v.index(signature))+1;v=v[:a]+'using var growthDetail=GrowthBoundaryProbe.Measure(GrowthBoundaryProbe.Part.'+part+');'+v[a:]
        g.write_text(v,encoding='utf-8')
    driver=project/'Assets/Scripts/Diagnostics/Runtime/HarborCityResidentWindowProbe.cs';v=driver.read_text(encoding='utf-8');v=v.replace('var boundary=new double[measuredSteps,8];','GrowthBoundaryProbe.Reset(s.Simulation.Tick,measuredSteps);var boundary=new double[measuredSteps,8];').replace('   File.WriteAllText(Path.Combine(output,"index-work.csv")','   GrowthBoundaryProbe.Write(output);File.WriteAllText(Path.Combine(output,"index-work.csv")');driver.write_text(v,encoding='utf-8')
