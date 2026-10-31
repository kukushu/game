"""Observe existing waste decisions only in a disposable Temp Unity project."""
from pathlib import Path
import sys
root,project=(Path(v).resolve() for v in sys.argv[1:3])
assert project.is_relative_to(root/'Temp')
def once(s,a,b):
    assert s.count(a)==1, (a[:100],s.count(a))
    return s.replace(a,b)
(project/'Assets/Scripts/Diagnostics/Runtime/WasteRoutingProbe.cs').write_bytes((root/'Tools/Diagnostics/WasteRoutingProbe.cs').read_bytes())
p=project/'Assets/Scripts/Runtime/Simulation/Services/CityCoreWaste.cs';s=p.read_text(encoding='utf-8-sig')
s=once(s,'var admission=world.PrepareTransport(plant.id,target.id,0,0,TransportPurpose.Service,0,out var rejected);','using var wasteAttempt=WasteRoutingProbe.Outbound(plant.id,target.id);var admission=world.PrepareTransport(plant.id,target.id,0,0,TransportPurpose.Service,0,out var rejected);wasteAttempt.Prepared(admission!=null,rejected);')
s=once(s,'world.CheckAdmission(admission);var v=world.CommitAdmission(admission);var mission=', 'world.CheckAdmission(admission);var v=world.CommitAdmission(admission);wasteAttempt.Commit(v.id,v.Task.id);var mission=')
s=once(s,'var next=world.PrepareServiceReturn(arrival,m.station,amount);', 'using var wasteReturn=WasteRoutingProbe.Return(m.station,m.pickup,m.vehicle,m.task);var next=world.PrepareServiceReturn(arrival,m.station,amount);wasteReturn.Prepared(next!=null,next==null?"ContinuationUnavailable":"None");')
s=once(s,'m.phase=WastePhase.Return;m.task=next.task.id;', 'm.phase=WastePhase.Return;m.task=next.task.id;wasteReturn.Commit(m.vehicle,m.task);')
p.write_text(s,encoding='utf-8')
p=project/'Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs';s=p.read_text(encoding='utf-8-sig');a=s.index('        DispatchBlueprint ServiceBlueprint(');b=s.index('        readonly LostOccupancy',a);body=s[a:b]
# The two common-path results have different types. Preserve both original evaluations.
needle='var result=dispatchRoutes.Get(Network,depot,destination,source,target,true,business as IJunctionEndpointProvider);if(began!=0)JunctionRoutes.FallbackCost(began);return result;'
body=once(body,needle,needle.replace('return result;','HarborCity.WasteRoutingProbe.RouteResult(result.route!=null&&result.route.Length>0,result.route?.Length??0,result.route!=null&&result.route.Length>0?result.route[0]:0,result.start);return result;'))
needle='var result=AccessRouting.FromLane(Network,lane,distance,target,true,out end);if(began!=0)JunctionRoutes.FallbackCost(began);return result;'
body=once(body,needle,needle.replace('return result;','HarborCity.WasteRoutingProbe.RouteResult(result!=null&&result.Count>0,result?.Count??0,result!=null&&result.Count>0?result[0]:0,distance);return result;'))
s=s[:a]+body+s[b:]
s=once(s,'            bool second=!hasOther||SpawnBlocked(opposite,otherDistance);','            bool second=!hasOther||SpawnBlocked(opposite,otherDistance);HarborCity.WasteRoutingProbe.SourceGuard(first,hasOther,second,source.lane,opposite);')
p.write_text(s,encoding='utf-8')
p=project/'Assets/Scripts/Diagnostics/Runtime/HarborCityScenarioWindowProbe.cs';s=p.read_text(encoding='utf-8-sig')
s=once(s,'            host.ScenarioRate(sharedWarmup?0:rate);','            WasteRoutingProbe.Warm(host.ScenarioSession);host.ScenarioRate(sharedWarmup?0:rate);')
s=once(s,'            double start=Time.realtimeSinceStartupAsDouble,last=start;', '            WasteRoutingProbe.Begin(output);double start=Time.realtimeSinceStartupAsDouble,last=start;')
s=once(s,'            int endGen0=GC.CollectionCount(0)', '            WasteRoutingProbe.Write(output);int endGen0=GC.CollectionCount(0)')
s=once(s,'File.WriteAllBytes(Path.Combine(output,"fixed-start.hcity"),CityCoreSave.Write(city));','File.WriteAllBytes(Path.Combine(output,"fixed-start.hcity"),CityCoreSave.Write(city));WasteRoutingProbe.Begin(output);')
s=once(s,'long measuredEnd=city.Simulation.Tick;IndexOracle(city);','WasteRoutingProbe.Write(output);long measuredEnd=city.Simulation.Tick;IndexOracle(city);')
s=once(s,'"PASS Unity fixed-work complete state and cold20; scripted one step/frame only");Application.Quit(0);','"PASS Unity fixed-work complete state and cold20; scripted one step/frame only");yield return new WaitForEndOfFrame();var screenshot=ScreenCapture.CaptureScreenshotAsTexture();try{File.WriteAllBytes(Path.Combine(output,"fixed-window.png"),screenshot.EncodeToPNG());}finally{UnityEngine.Object.Destroy(screenshot);}yield return null;Application.Quit(0);')
p.write_text(s,encoding='utf-8')
print('Prepared Temp-only waste attempt/query/result observation',project)
