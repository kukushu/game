using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using HarborCity;
using HarborCity.RoadCore;

// Cold diagnosis only: observes a bounded normal continuation, not a timing benchmark.
static class CityProgressProbe
{
    sealed class Tracked {internal int id,task;internal Point point;internal bool moved,retired;internal string purpose;}
    static void Main(string[] args)
    {
        var s=CityCoreSave.Read(File.ReadAllBytes(args[0]));s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);s.Validate();
        var tracked=s.Transport.Vehicles.Where(v=>v.status==VehicleStatus.Active).Select(v=>new Tracked{id=v.id,task=v.Task.id,point=s.Transport.Position(v),purpose=v.Task.purpose.ToString()}).ToArray();
        long start=s.Simulation.Tick;long generated=s.Simulation.Waste.Generated,processed=s.Simulation.Waste.Processed;int consumed=s.City.consumption.ConsumedGoods;
        for(int i=0;i<1200;i++)
        {
            if(s.Advance(.05)!=1)throw new Exception("Skipped step in progress probe");
            foreach(var t in tracked){if(t.retired)continue;var v=s.Transport.FindVehicle(t.id);if(v==null||v.Task.id!=t.task||v.status!=VehicleStatus.Active){t.retired=true;continue;}var p=s.Transport.Position(v);if(Point.Distance(p,t.point)>1e-6)t.moved=true;t.point=p;}
        }
        s.Validate();
        var stopped=tracked.Where(t=>!t.moved&&!t.retired).ToArray();int connections=0,nearExit=0;
        foreach(var t in stopped){var v=s.Transport.FindVehicle(t.id);var lane=s.Transport.Network.Get(v.Current);if(lane.IsConnection)connections++;if(lane.EndDistance-v.distance<1.50001)nearExit++;}
        var report=new{startTick=start,endTick=s.Simulation.Tick,seconds=60,initialActive=tracked.Length,finalActive=s.Transport.ActiveCount,everMoved=tracked.Count(t=>t.moved),retiredOrTaskChanged=tracked.Count(t=>t.retired),stationaryAllSteps=stopped.Length,stationaryByPurpose=stopped.GroupBy(t=>t.purpose).ToDictionary(g=>g.Key,g=>g.Count()),stationaryOnConnection=connections,stationaryNearDrivableExit=nearExit,wasteGenerated=s.Simulation.Waste.Generated-generated,wasteProcessed=s.Simulation.Waste.Processed-processed,consumed=s.City.consumption.ConsumedGoods-consumed,note="1200 actual normal steps, every tracked physical position checked each step; stationary does not alone identify blocking cause. Not a performance measurement or rule change."};
        File.WriteAllText(args[1],new JavaScriptSerializer().Serialize(report));File.WriteAllBytes(args[2],CityCoreSave.Write(s));Console.WriteLine("PASS bounded continuation invariants; "+stopped.Length+" / "+tracked.Length+" initial vehicles never moved in 60 simulation seconds");
    }
}
