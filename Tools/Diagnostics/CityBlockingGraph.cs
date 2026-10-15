using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using HarborCity;
using HarborCity.RoadCore;
// Read-only reconstruction of the currently implemented blocking predicates.
// This is evidence, never a second arbiter or authoritative traffic state.
static class CityBlockingGraph
{
    sealed class Edge {public int from,to;public string reason;}
    sealed class Claim {internal int owner;internal long movement;}
    static void Main(string[] args)
    {
        var s=CityCoreSave.Read(File.ReadAllBytes(args[0]));s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);s.Validate();var w=s.Transport;var n=w.Network;
        var lanes=new Dictionary<long,List<Vehicle>>(CityRoadKeyComparer.Instance);var claims=new Dictionary<int,List<Claim>>();var edges=new List<Edge>();
        foreach(var v in w.Vehicles.Where(v=>v.status==VehicleStatus.Active))
        {
            if(v.changeTarget!=0)throw new Exception("This cold graph probe requires no active lane changes; do not silently omit shadow occupancy");
            if(!lanes.TryGetValue(v.Current,out var list))lanes.Add(v.Current,list=new List<Vehicle>());list.Add(v);
            var d=n.Get(v.Current);int junction=d.JunctionId;
            if(junction==0&&v.distance<d.StartDistance+Math.Min(n.Clearance(d),World.MinimumSpacing)-1e-9)junction=d.From;
            if(!d.IsConnection&&v.distance>Math.Max(d.StartDistance,d.EndDistance-Math.Min(n.Clearance(d),World.MinimumSpacing))+1e-9)junction=d.To;
            if(!n.IsConflictJunction(junction))continue;
            long movement=d.IsConnection?d.Id:junction==d.To&&v.cursor+1<v.Route.Count?v.Route[v.cursor+1]:junction==d.From&&v.cursor>0&&n.Get(v.Route[v.cursor-1]).IsConnection?v.Route[v.cursor-1]:0;
            if(!claims.TryGetValue(junction,out var bucket))claims.Add(junction,bucket=new List<Claim>());bucket.Add(new Claim{owner=v.id,movement=movement});
        }
        foreach(var bucket in lanes.Values)bucket.Sort((a,b)=>{int c=b.distance.CompareTo(a.distance);return c!=0?c:a.id.CompareTo(b.id);});
        foreach(var pair in lanes)
        {
            var d=n.Get(pair.Key);var bucket=pair.Value;
            for(int i=0;i<bucket.Count;i++)
            {
                var v=bucket[i];
                if(i>0&&bucket[i-1].distance-v.distance<=World.MinimumSpacing+1e-8){edges.Add(new Edge{from=v.id,to=bucket[i-1].id,reason="following"});continue;}
                double stop=Math.Max(d.StartDistance,d.EndDistance-Math.Min(n.Clearance(d),World.MinimumSpacing));
                if(d.IsConnection||!n.IsConflictJunction(d.To)||v.distance<stop-1e-8||v.cursor+1>=v.Route.Count)continue;
                long movement=v.Route[v.cursor+1];bool conflict=false;
                if(claims.TryGetValue(d.To,out var owners))foreach(var claim in owners)if(claim.owner!=v.id&&(claim.movement==0||movement==0||n.MovementsConflict(movement,claim.movement))){edges.Add(new Edge{from=v.id,to=claim.owner,reason="junction-claim"});conflict=true;}
                if(conflict||v.cursor+2>=v.Route.Count)continue;
                var exit=n.Get(v.Route[v.cursor+2]);
                if(lanes.TryGetValue(exit.Id,out var receiving)&&receiving.Count>0&&receiving[receiving.Count-1].distance<exit.StartDistance+World.MinimumSpacing+n.Clearance(exit))edges.Add(new Edge{from=v.id,to=receiving[receiving.Count-1].id,reason="receiving-space"});
            }
        }
        File.WriteAllText(args[1],new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Serialize(new{tick=s.Simulation.Tick,active=w.ActiveCount,claims=claims.Sum(p=>p.Value.Count),edges,nodes=w.Vehicles.Where(v=>v.status==VehicleStatus.Active).Select(v=>new{id=v.id,purpose=v.Task.purpose.ToString(),lane=v.Current,distance=v.distance,connection=n.Get(v.Current).IsConnection}).ToArray(),note="Cold predicate reconstruction; edges identify necessary blockers at this snapshot, not proven root causes or a replacement scheduler"}));
        Console.WriteLine("Blocking graph: "+edges.Count+" edges, "+claims.Sum(p=>p.Value.Count)+" physical junction claims");
    }
}
