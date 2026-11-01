"""Observe frozen departure decisions only in a disposable Temp project."""
from pathlib import Path
import sys
root, project = (Path(v).resolve() for v in sys.argv[1:3])
assert project.is_relative_to(root / 'Temp')
def once(text, old, new):
    assert text.count(old) == 1, (old[:80], text.count(old))
    return text.replace(old, new)
p = project / 'Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs'
s = p.read_text(encoding='utf-8-sig')
signature = '        bool PrepareDeparture(int origin,int destination,TransportPurpose purpose,int actor,out DispatchBlueprint blueprint,out TransportRejection rejection)'
wrapper = '''        bool PrepareDeparture(int origin,int destination,TransportPurpose purpose,int actor,out DispatchBlueprint blueprint,out TransportRejection rejection)
        {
            if(!ResidentDepartureProbe.Enabled||timeline.Tick+1!=ResidentDepartureProbe.TargetTick)return PrepareDepartureObservedCore(origin,destination,purpose,actor,out blueprint,out rejection);
            long searches=Network.PointSearches,began=System.Diagnostics.Stopwatch.GetTimestamp();
            bool accepted=PrepareDepartureObservedCore(origin,destination,purpose,actor,out blueprint,out rejection);
            long elapsed=System.Diagnostics.Stopwatch.GetTimestamp()-began;bool blocked=false;
            // AFTER the unchanged decision, before admission can commit. Do not
            // time this shadow query as part of the original prepare work.
            if(IsResident(purpose)&&origin!=destination&&HasResidentAccess(origin,destination))blocked=ServiceSourceBlocked(GetAccess(origin),GetAccess(destination));
            ResidentDepartureProbe.Record(timeline.Tick+1,origin,destination,purpose,rejection,accepted,blocked,Network.PointSearches-searches,elapsed);
            return accepted;
        }
'''
s=once(s,signature,wrapper+signature.replace('PrepareDeparture(', 'PrepareDepartureObservedCore('))
p.write_text(s,encoding='utf-8')
target=project/'Assets/Scripts/Diagnostics/Runtime/ResidentDepartureProbe.cs'
target.parent.mkdir(parents=True,exist_ok=True)
target.write_bytes((root/'Tools/Diagnostics/ResidentDepartureProbe.cs').read_bytes())
p=project/'Assets/Scripts/Diagnostics/Runtime/HarborCityScenarioWindowProbe.cs'
if p.exists():
    s=p.read_text(encoding='utf-8-sig')
    s=once(s,'File.WriteAllBytes(Path.Combine(output,"fixed-start.hcity"),CityCoreSave.Write(city));','File.WriteAllBytes(Path.Combine(output,"fixed-start.hcity"),CityCoreSave.Write(city));HarborCity.RoadCore.ResidentDepartureProbe.Enabled=true;')
    s=once(s,'long measuredEnd=city.Simulation.Tick;IndexOracle(city);','HarborCity.RoadCore.ResidentDepartureProbe.Enabled=false;HarborCity.RoadCore.ResidentDepartureProbe.Write(output);long measuredEnd=city.Simulation.Tick;IndexOracle(city);')
    p.write_text(s,encoding='utf-8')
print('Prepared Temp-only departure observer',project)
