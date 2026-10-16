using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using HarborCity;
using HarborCity.RoadCore;
// Resumes a real saved city. Creates no people, vehicles, tasks, money or goods.
static class CityTrafficThresholdCapture
{
    static void Main(string[] args)
    {
        int target=int.Parse(args[3]),limit=int.Parse(args[4]);if(target<1||target>100000||limit<1||limit>24000)throw new ArgumentOutOfRangeException();
        var s=CityCoreSave.Read(File.ReadAllBytes(args[0]));s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);s.Validate();long start=s.Simulation.Tick;var watch=System.Diagnostics.Stopwatch.StartNew();double logAt=0;int peak=s.Transport.ActiveCount,steps=0;
        while(s.Transport.ActiveCount<target&&steps<limit)
        {
            if(s.Advance(.05)!=1)throw new Exception("Skipped capture step");steps++;peak=Math.Max(peak,s.Transport.ActiveCount);
            if(watch.Elapsed.TotalSeconds-logAt>=10){Console.WriteLine("Capture tick "+s.Simulation.Tick+" actual active "+s.Transport.ActiveCount);logAt=watch.Elapsed.TotalSeconds;}
        }
        s.Validate();bool reached=s.Transport.ActiveCount>=target;int captured=s.Transport.ActiveCount;long tick=s.Simulation.Tick;
        File.WriteAllBytes(reached?args[2]:args[2]+".not-reached",CityCoreSave.Write(s));
        var purposes=s.Transport.Tasks.GroupBy(t=>t.purpose).ToDictionary(g=>g.Key.ToString(),g=>g.Count());
        var positions=s.Transport.Vehicles.Where(v=>v.status==VehicleStatus.Active).ToDictionary(v=>v.id,v=>s.Transport.Position(v));
        if(s.Advance(.05)!=1)throw new Exception("Skipped movement diagnostic step");s.Validate();int moved=0,retired=0;
        foreach(var pair in positions){var v=s.Transport.FindVehicle(pair.Key);if(v==null||v.status!=VehicleStatus.Active){retired++;continue;}if(Point.Distance(pair.Value,s.Transport.Position(v))>1e-6)moved++;}
        File.WriteAllText(args[1],new JavaScriptSerializer().Serialize(new{status=reached?"actual-traffic-threshold-captured":"threshold-not-reached",target,startTick=start,capturedTick=tick,stepsUntilCapture=steps,peak,activeAtCapture=captured,population=s.Entities.Members.Count,buildings=s.Entities.Buildings.Count,purposes,followingNormalStepMoved=moved,followingNormalStepRetired=retired,additionalDiagnosticSteps=1,note="Normal CitySimulation from the supplied saved facts; no insertion/culling or rule changes. Active includes queues; moving count is separate. Snapshot saved before the extra movement check; not a performance or economic-health pass."}));
        Console.WriteLine((reached?"CAPTURED ":"NOT REACHED ")+captured+" actual active; next normal step moved="+moved+" retired="+retired);
    }
}
