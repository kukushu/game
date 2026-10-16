using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarborCity;
using HarborCity.RoadCore;

// Diagnostic assembly only. Hooks observe the actual existing movement branches.
// No second simulator, forced arrival, clock edits or inventory writes.
static class LogisticsTrace
{
    internal static bool Enabled,Capture;
    internal static readonly HashSet<int> Watch=new HashSet<int>();
    internal static readonly Dictionary<int,double> Travelled=new Dictionary<int,double>();
    internal static readonly Dictionary<int,double> StepMoved=new Dictionary<int,double>();
    internal static readonly Dictionary<int,List<Edge>> Edges=new Dictionary<int,List<Edge>>();
    static readonly Dictionary<long,int> Entries=new Dictionary<long,int>(CityRoadKeyComparer.Instance);
    static readonly FieldInfo Claims=typeof(JunctionReservations).GetField("claims",BindingFlags.NonPublic|BindingFlags.Instance);
    static object reservations;
    internal sealed class Edge {public int blocker;public string reason;}
    internal static void Begin(World world,object claims)
    {if(!Enabled)return;reservations=claims;Edges.Clear();Entries.Clear();StepMoved.Clear();}
    internal static void Block(Vehicle v,int blocker,string reason)
    {
        if(!Enabled||!Capture||v.id==blocker)return;
        if(!Edges.TryGetValue(v.id,out var list))Edges.Add(v.id,list=new List<Edge>());
        if(!list.Any(e=>e.blocker==blocker&&e.reason==reason))list.Add(new Edge{blocker=blocker,reason=reason});
    }
    internal static void Moved(Vehicle v,double distance)
    {
        if(!Enabled)return;
        if(Watch.Contains(v.Task.id)){Travelled.TryGetValue(v.Task.id,out double n);Travelled[v.Task.id]=n+distance;}
        if(Capture){StepMoved.TryGetValue(v.id,out double n);StepMoved[v.id]=n+distance;}
    }
    internal static void Entered(Vehicle v,long lane){if(Enabled&&Capture)Entries[lane]=v.id;}
    internal static void Entry(Vehicle v,long lane,string reason){if(Enabled&&Capture&&Entries.TryGetValue(lane,out int id))Block(v,id,reason);}
    internal static void Shadow(World world,Vehicle v,List<LaneOccupant> occupants)
    {
        if(!Enabled||!Capture)return;var lane=world.Network.Get(v.Current);var target=world.Network.Get(v.changeTarget);
        double projected=lane.Geometry.MapDistance(target.Geometry,v.distance);
        LaneOccupant previous=null;foreach(var actor in occupants)if(actor.distance>projected)previous=actor;else break;
        if(previous!=null&&previous.distance-World.MinimumSpacing<projected+target.Speed*World.StepSeconds)Block(v,previous.vehicle.id,"shadow-following");
    }
    internal static void Reservation(World world,Vehicle v,int junction,long movement)
    {
        if(!Enabled||!Capture)return;
        // Read actual private claim store only after the real CanReserve rejected.
        var claims=(IDictionary)Claims.GetValue(reservations);if(!claims.Contains(junction))throw new Exception("Rejected reservation without claims");
        foreach(var claim in (IEnumerable)claims[junction])
        {
            var type=claim.GetType();int owner=(int)type.GetField("owner").GetValue(claim);long path=(long)type.GetField("movement").GetValue(claim);
            if(owner!=v.id&&(movement==0||path==0||world.Network.MovementsConflict(movement,path)))Block(v,owner,"junction-reservation");
        }
    }
    internal static object Graph(World world,IEnumerable<int> watched)
    {
        var vehicles=world.Vehicles.ToDictionary(v=>v.id);var starts=watched.Select(world.FindTaskVehicle).Where(v=>v!=null).Select(v=>v.id).ToArray();
        bool Stopped(int id)=>!StepMoved.TryGetValue(id,out double n)||n<1e-8;
        var reachable=new HashSet<int>();var pending=new Stack<int>(starts);
        while(pending.Count>0){int id=pending.Pop();if(!reachable.Add(id)||!Stopped(id)||!Edges.TryGetValue(id,out var edges))continue;foreach(var e in edges)pending.Push(e.blocker);}
        var color=new Dictionary<int,int>();var path=new List<int>();var cycles=new List<int[]>();
        // Iterative DFS: no recursion proportional to a congested city's size.
        foreach(int start in starts)
        {
            if(color.ContainsKey(start))continue;var frames=new Stack<(int id,int next)>();frames.Push((start,0));
            while(frames.Count>0)
            {
                var f=frames.Pop();if(f.next==0){if(color.TryGetValue(f.id,out int c)&&c!=0)continue;color[f.id]=1;path.Add(f.id);}
                List<Edge> edges=null;if(!Stopped(f.id))edges=null;else Edges.TryGetValue(f.id,out edges);
                if(edges!=null&&f.next<edges.Count)
                {
                    frames.Push((f.id,f.next+1));int child=edges[f.next].blocker;if(!Stopped(child))continue;
                    color.TryGetValue(child,out int state);if(state==1){int at=path.IndexOf(child);cycles.Add(path.Skip(at).Concat(new[]{child}).ToArray());}else if(state==0)frames.Push((child,0));
                }
                else{color[f.id]=2;if(path.Count==0||path[path.Count-1]!=f.id)throw new Exception("Diagnostic DFS path mismatch");path.RemoveAt(path.Count-1);}
            }
        }
        return new{tick=world.Clock/World.StepSeconds,reachableActors=reachable.Count,cycles=cycles.ToArray(),edges=reachable.Where(id=>Stopped(id)&&Edges.ContainsKey(id)).SelectMany(id=>Edges[id].Select(e=>new{vehicle=id,task=vehicles.TryGetValue(id,out var v)?v.Task.id:0,e.blocker,e.reason,blockerMoved=StepMoved.TryGetValue(e.blocker,out double n)?n:0})).ToArray()};
    }
}

static class CityLogisticsTrace
{
    static readonly CityScenarioJson Json=new CityScenarioJson();
    static readonly HashSet<int> WatchedShops=new HashSet<int>();
    static readonly System.Globalization.CultureInfo Inv=System.Globalization.CultureInfo.InvariantCulture;
    static void Write(string path,object data)=>File.WriteAllText(path,Json.Serialize(data));
    static (double metres,double seconds) Remaining(World w,Vehicle v,int cursor,double station)
    {
        double metres=0,seconds=0;
        for(int i=cursor;i<v.Route.Count;i++){var d=w.Network.Get(v.Route[i]);double end=i==v.Route.Count-1&&v.Plan.EndDistance>=0?v.Plan.EndDistance:d.EndDistance;double length=Math.Max(0,end-(i==cursor?station:d.StartDistance));metres+=length;seconds+=length/d.Speed;}
        return(metres,seconds);
    }
    static object Snapshot(CityCoreIndustrySession s,int task)
    {
        var v=s.Transport.FindTaskVehicle(task);if(v==null)return new{task,retired=true};
        var remaining=Remaining(s.Transport,v,v.cursor,v.distance);var origin=s.Transport.GetAccess(v.Task.origin);var first=s.Transport.Network.Get(v.Route[0]);
        var total=Remaining(s.Transport,v,0,origin!=null&&origin.lane==first.Id?origin.distance:first.StartDistance);
        return new{task,vehicle=v.id,retired=false,kind=v.Task.Shipment?.kind.ToString(),v.Task.origin,v.Task.destination,v.Task.Cargo,age=s.Simulation.Seconds-v.Task.departedAt,v.cursor,segments=v.Route.Count,lane=v.Current,station=v.distance,position=s.Transport.Position(v),speedLimit=s.Transport.Network.Get(v.Current).Speed,remainingMetres=remaining.metres,freeFlowRemainingSeconds=remaining.seconds,planMetres=total.metres,planFreeFlowSeconds=total.seconds,planStartMatchesOrigin=origin!=null&&origin.lane==first.Id,measuredTravelled=LogisticsTrace.Travelled.TryGetValue(task,out double moved)?moved:0};
    }
    static object Retail(CityCoreIndustrySession s)
    {
        var workers=new Dictionary<int,int>();foreach(var m in s.Entities.Members)if(m.Person.atWork){int id=s.City.Workplace(m.Person);workers.TryGetValue(id,out int n);workers[id]=n+1;}
        var open=typeof(CityCoreConsumption).GetMethod("Open",BindingFlags.NonPublic|BindingFlags.Instance);
        return new{tick=s.Simulation.Tick,minute=s.City.ResidentMinute,day=s.City.day,shoppingDue=s.City.consumption.NextTick,shoppingStart=s.Simulation.Design.Retail.ShoppingStartMinute,stocked=s.Entities.Shops.Where(b=>b.Stock>0||WatchedShops.Contains(b.id)).Select(b=>new{b.id,b.Stock,cash=b.Account.Cash,wholesaleFunded=b.Account.WholesaleFunded,wages=b.Account.WageCosts,unpaidWages=b.Account.UnpaidWages,workersAtWork=workers.TryGetValue(b.id,out int n)?n:0,paidStaffed=s.Simulation.Payroll.WorkersAt(b.id),open=(bool)open.Invoke(s.Simulation.Consumption,new object[]{b}),b.retailSold}).ToArray(),eligibleFamilies=s.City.society.families.Count(h=>h.resident&&h.shoppingDay!=s.City.day&&h.savings>=s.Simulation.Design.Retail.UnitPrice&&h.people.Any(p=>!p.dead&&!p.sick&&p.age>=18&&!p.atWork&&p.tripId==0&&p.location==h.home&&p.carriedGoods==0&&s.Simulation.Seconds>=p.retryAt)),shoppingTasks=s.Transport.Tasks.Count(t=>t.purpose==TransportPurpose.Shopping),consumed=s.City.consumption.ConsumedGoods};
    }
    static void Main(string[] args)
    {
        int steps=int.Parse(args[2]);if(steps<0||steps>2401||IntPtr.Size!=8)throw new Exception("Bounded 64-bit probe only");
        string output=args[1];var s=CityCoreSave.Read(File.ReadAllBytes(args[0]));s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);s.Validate();
        var watch=s.Transport.Tasks.Where(t=>t.purpose==TransportPurpose.Freight&&t.Shipment!=null).Select(t=>t.id).ToArray();foreach(int id in watch)LogisticsTrace.Watch.Add(id);
        foreach(int id in watch){var t=s.Transport.FindTask(id);if(t.Shipment.kind==CargoKind.Goods&&s.Entities.Building(t.destination) is CommercialBuilding)WatchedShops.Add(t.destination);}
        var watchedActors=watch.ToDictionary(id=>id,id=>s.Transport.FindTaskVehicle(id));var retired=new HashSet<int>();
        LogisticsTrace.Enabled=args[3]=="trace";
        Write(Path.Combine(output,"initial.json"),new{tick=s.Simulation.Tick,watch,vehicles=watch.Select(id=>Snapshot(s,id)).ToArray(),retail=Retail(s)});
        var timing=new List<double>();var allocation=new List<long>();int g0=GC.CollectionCount(0),g1=GC.CollectionCount(1),g2=GC.CollectionCount(2);
        using(var samples=new StreamWriter(Path.Combine(output,"trajectories.jsonl")))
        using(var graph=new StreamWriter(Path.Combine(output,"blockers.jsonl")))
        using(var retail=new StreamWriter(Path.Combine(output,"retail.jsonl")))
        using(var events=new StreamWriter(Path.Combine(output,"arrivals.jsonl")))
        using(var perf=new StreamWriter(Path.Combine(output,"steps.csv")))
        {
            perf.WriteLine("tick,ms,allocated,active,routes");
            for(int i=0;i<steps;i++)
            {
                LogisticsTrace.Capture=i%100==0;long at=Stopwatch.GetTimestamp(),bytes=GC.GetAllocatedBytesForCurrentThread(),routes=s.Transport.RoutePreparations;
                if(s.Advance(World.StepSeconds)!=1)throw new Exception("Fixed step count mismatch");
                double ms=(Stopwatch.GetTimestamp()-at)*1000.0/Stopwatch.Frequency;long allocated=GC.GetAllocatedBytesForCurrentThread()-bytes;timing.Add(ms);allocation.Add(allocated);
                foreach(var pair in watchedActors)if(pair.Value.status==VehicleStatus.Delivered&&retired.Add(pair.Key))events.WriteLine(Json.Serialize(new{tick=s.Simulation.Tick,seconds=s.Simulation.Seconds,task=pair.Key,vehicle=pair.Value.id,state=pair.Value.Task.state.ToString(),kind=pair.Value.Task.Shipment.kind.ToString(),elapsed=s.Simulation.Seconds-pair.Value.Task.departedAt,pair.Value.Task.destination,remainingCargo=pair.Value.Task.Cargo,travelledDuringProbe=LogisticsTrace.Travelled.TryGetValue(pair.Key,out double n)?n:0,consumed=s.City.consumption.ConsumedGoods}));
                perf.WriteLine(s.Simulation.Tick+","+ms.ToString("R",Inv)+","+allocated+","+s.Transport.ActiveCount+","+(s.Transport.RoutePreparations-routes));
                if(i%20==0||i==steps-1){samples.WriteLine(Json.Serialize(new{tick=s.Simulation.Tick,vehicles=watch.Select(id=>Snapshot(s,id)).ToArray()}));retail.WriteLine(Json.Serialize(Retail(s)));}
                if(LogisticsTrace.Enabled&&LogisticsTrace.Capture)graph.WriteLine(Json.Serialize(LogisticsTrace.Graph(s.Transport,watch)));
                if(i%100==0){samples.Flush();retail.Flush();perf.Flush();graph.Flush();Console.WriteLine("Progress tick "+s.Simulation.Tick);}
            }
        }
        s.Validate();byte[] final=CityCoreSave.Write(s);File.WriteAllBytes(Path.Combine(output,"final.hcity"),final);
        timing.Sort();Write(Path.Combine(output,"summary.json"),new{mode=LogisticsTrace.Enabled?"instrumented actual branch trace; not baseline performance":"identical hooks disabled; model performance with diagnostic host",steps,finalTick=s.Simulation.Tick,mean=timing.Count==0?0:timing.Average(),p95=timing.Count==0?0:timing[(int)Math.Ceiling(timing.Count*.95)-1],p99=timing.Count==0?0:timing[(int)Math.Ceiling(timing.Count*.99)-1],max=timing.Count==0?0:timing.Last(),meanAllocated=allocation.Count==0?0:allocation.Average(),gc0=GC.CollectionCount(0)-g0,gc1=GC.CollectionCount(1)-g1,gc2=GC.CollectionCount(2)-g2,consumed=s.City.consumption.ConsumedGoods,retiredWatched=watch.Count(id=>s.Transport.FindTask(id)==null),finalHash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(final)).ToLowerInvariant()});
        Console.WriteLine("PASS actual city invariants; watched retired "+watch.Count(id=>s.Transport.FindTask(id)==null));
    }
}
