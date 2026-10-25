using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using HarborCity.RoadCore;
namespace HarborCity
{
    internal static class UnifiedRoutingChecks
    {
        internal static long Queries,Checks,RouteChanges,CostBitChanges;internal static double MaxCostDelta;
        static void Check(bool ok,string why){Checks++;if(!ok)throw new Exception(why);}
        static double Cost(Network n,List<long> path,double start,double end)
        {if(path==null)return double.PositiveInfinity;double cost=0;for(int i=0;i<path.Count;i++){var d=n.Get(path[i]);cost+=((i==path.Count-1?end:d.EndDistance)-(i==0?start:d.StartDistance))/d.Speed;}return cost;}
        static RoadAccess Other(Network n,RoadAccess a)
        {var d=n.Get(a.lane);long id=Network.LaneId(d.RoadId,!d.Reverse,d.LaneIndex);return n.Contains(id)?new RoadAccess{lane=id,distance=d.Geometry.MapReverseDistance(n.Get(id).Geometry,a.distance),valid=a.valid}:null;}
        internal static void Pair(Network n,long lane,double distance,RoadAccess goal,bool both)
        {
            Queries++;var source=new RoadAccess{lane=lane,distance=distance};var other=both?Other(n,goal):null;
            var old=n.FrozenRouteToEither(source,goal,other,out int os);var actual=n.RouteToEither(source,goal,other,out int chosen);
            Check((old==null)==(actual==null),"Reachability changed");
            n.RouteSharingEnabled=false;var fresh=n.RouteToEither(source,goal,other,out int fs);n.RouteSharingEnabled=true;
            Check(fs==chosen&&(actual==null?fresh==null:actual.SequenceEqual(fresh)),"Cache changed deterministic route");
            if(actual==null)return;n.ValidateRoute(actual);Check(actual[0]==lane&&actual[actual.Count-1]==(chosen==0?goal:other).lane,"Wrong physical endpoints");
            double a=Cost(n,old,distance,(os==0?goal:other).distance),b=Cost(n,actual,distance,(chosen==0?goal:other).distance),delta=Math.Abs(a-b);MaxCostDelta=Math.Max(MaxCostDelta,delta);
            if(BitConverter.DoubleToInt64Bits(a)!=BitConverter.DoubleToInt64Bits(b))CostBitChanges++;
            // Interior normalization changes floating addition order. Record every
            // bit difference; the bound is a cost-accuracy check, NOT state equivalence.
            Check(delta<=1e-10*Math.Max(1,Math.Abs(a)),"Non-shortest route "+a+" / "+b);
            if(os!=chosen||!old.SequenceEqual(actual))RouteChanges++;
            if(Queries%19==0)
            {
                var path=AccessRouting.Find(n,source,goal,both,out double start,out double end);double quote=AccessRouting.EstimateSeconds(n,source,goal,both);
                Check(BitConverter.DoubleToInt64Bits(Cost(n,path,start,end))==BitConverter.DoubleToInt64Bits(quote),"Quote/materialization bits differ");
            }
            if(Queries%503==0){actual[0]=long.MaxValue;var again=n.RouteToEither(source,goal,other,out int side);Check(again[0]==lane&&side==chosen,"Actor mutated shared cache");}
            Check(n.SharedRouteCount<=4096&&n.SharedRouteBytes<=8*1024*1024,"Lane cache unbounded");
        }
        internal static void Junctions(Network n)
        {
            var ids=n.RoadIds.SelectMany(id=>new[]{n.Settings(id).from,n.Settings(id).to}).Distinct().ToArray();
            foreach(int a in ids)foreach(int b in ids)
            {
                var old=n.FrozenJunctionRoute(a,b);var next=n.Route(a,b);Check((old==null)==(next==null),"Junction reachability changed");if(next==null||a==b)continue;n.ValidateRoute(next);
                Check(n.Get(next[0]).From==a&&n.Get(next[next.Count-1]).To==b,"Junction endpoint wrong");
                double x=Cost(n,old,n.Get(old[0]).StartDistance,n.Get(old[old.Count-1]).EndDistance),y=Cost(n,next,n.Get(next[0]).StartDistance,n.Get(next[next.Count-1]).EndDistance);Check(Math.Abs(x-y)<=1e-10*Math.Max(1,Math.Abs(x)),"Junction non-shortest");
            }
        }
        internal static void Replay(string save,string queries,string output)
        {
            var city=CityCoreSave.Read(File.ReadAllBytes(save));var n=city.Transport.Network;
            var q=File.ReadAllLines(queries).Skip(1).Select(line=>line.Split(',')).Select(v=>new[]{double.Parse(v[2],CultureInfo.InvariantCulture),double.Parse(v[3],CultureInfo.InvariantCulture),double.Parse(v[5],CultureInfo.InvariantCulture),double.Parse(v[6],CultureInfo.InvariantCulture),bool.Parse(v[7])?1:0}).ToArray();
            foreach(var v in q)Pair(n,(long)v[0],v[1],new RoadAccess{lane=(long)v[2],distance=v[3]},v[4]!=0);
            var rows=new List<string>{"round,variant,queries,ms,gc_bytes,searches,expanded,hits,entries,logical_cache_bytes"};
            foreach(string mode in new[]{"dijkstra-no-cache","astar-no-cache","astar-cache"})
            {
                n.RouteHeuristicEnabled=mode!="dijkstra-no-cache";n.RouteSharingEnabled=mode=="astar-cache";
                for(int round=-1;round<12;round++)
                {
                    long searches=n.PointSearches,expanded=n.PointExpanded,hits=n.SharedRouteHits,bytes=GC.GetAllocatedBytesForCurrentThread();var timer=Stopwatch.StartNew();
                    foreach(var v in q)AccessRouting.FromLane(n,(long)v[0],v[1],new RoadAccess{lane=(long)v[2],distance=v[3]},v[4]!=0,out _);
                    timer.Stop();bytes=GC.GetAllocatedBytesForCurrentThread()-bytes;
                    rows.Add(string.Join(",",round,mode,q.Length,timer.Elapsed.TotalMilliseconds.ToString("R",CultureInfo.InvariantCulture),bytes,n.PointSearches-searches,n.PointExpanded-expanded,n.SharedRouteHits-hits,n.SharedRouteCount,n.SharedRouteBytes));
                }
            }
            File.WriteAllLines(output,rows);
        }
        internal static string Summary()=>"queries="+Queries+" checks="+Checks+" changed_routes="+RouteChanges+" cost_bit_changes="+CostBitChanges+" max_cost_delta="+MaxCostDelta.ToString("R",CultureInfo.InvariantCulture);
        internal static void LargeCoordinateMetric()
        {
            // Finite individual road lengths, but squared point-to-goal distance
            // overflows. A nonfinite heuristic must not prune the faster detour.
            var data=new NetworkData();int next=1;
            int Node(double x,double z){int id=next++;data.junctions.Add(new Junction{id=id,position=new Point(x,0,z)});return id;}
            void Road(int id,int a,int b,double speed){data.roads.Add(new PhysicalRoad{id=id,from=a,to=b,speed=speed,direction=RoadDirection.Forward,points=new[]{data.junctions[a-1].position,data.junctions[b-1].position}});}
            int origin=Node(0,0),start=Node(1e153,0),goal=Node(20e153,0),end=Node(21e153,0);Road(1,origin,start,1);Road(2,goal,end,1);
            int prev=start;for(int i=2;i<=20;i++){int id=i==20?goal:Node(i*1e153,0);Road(10+i,prev,id,1);prev=id;}
            prev=Node(1e153,1e152);Road(100,start,prev,2);for(int i=2;i<=20;i++){int id=Node(i*1e153,1e152);Road(100+i,prev,id,2);prev=id;}Road(121,prev,goal,2);data.nextJunction=next;data.nextRoad=122;
            var n=new Network(data);var source=n.Get(Network.LaneId(1,false));var target=n.Get(Network.LaneId(2,false));Pair(n,source.Id,source.EndDistance,new RoadAccess{lane=target.Id,distance=target.StartDistance},false);
        }
        static byte[] Business(CityCoreIndustrySession city)
        {using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream)){typeof(CityCoreSave).GetMethod("Put",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{writer,typeof(CityCoreBusinessData),city.SnapshotBusiness()});return stream.ToArray();}}
        static byte[] Actor(World world,Vehicle v)
        {
            using(var stream=new MemoryStream())using(var w=new BinaryWriter(stream))
            {
                var t=v.Task;w.Write(v.id);w.Write((int)v.status);w.Write(v.Current);w.Write(v.distance);w.Write(v.Position.IsFrozen);var p=world.Position(v);w.Write(p.x);w.Write(p.y);w.Write(p.z);w.Write(v.cursor);w.Write(v.changeTarget);w.Write(v.changeElapsed);
                w.Write(t.id);w.Write(t.origin);w.Write(t.destination);w.Write((int)t.purpose);w.Write(t.citizenId);w.Write(t.departedAt);w.Write((int)t.state);w.Write(t.Shipment!=null);if(t.Shipment!=null){w.Write((int)t.Shipment.kind);w.Write(t.Shipment.remaining);w.Write(t.Shipment.unitPrice);}return stream.ToArray();
            }
        }
        internal static void Boundary(string oldFile,string newFile,string output)
        {
            var old=CityCoreSave.Read(File.ReadAllBytes(oldFile));var next=CityCoreSave.Read(File.ReadAllBytes(newFile));old.Validate();next.Validate();Check(Business(old).SequenceEqual(Business(next)),"Boundary business/clock/economy/waste/households differ");
            var actors=old.Transport.Vehicles.ToDictionary(v=>v.id);Check(actors.Count==next.Transport.Vehicles.Count(),"Boundary vehicle set changed");int different=0;
            foreach(var v in next.Transport.Vehicles){Check(actors.TryGetValue(v.id,out var prior)&&Actor(old.Transport,prior).SequenceEqual(Actor(next.Transport,v)),"Boundary pose/task/cargo/status changed vehicle "+v.id);if(!prior.Route.SequenceEqual(v.Route)||prior.Plan.EndDistance!=v.Plan.EndDistance)different++;}
            File.WriteAllText(output,"PASS complete business facts, identical vehicle IDs/status/physical poses/navigation cursors/tasks/cargo; changed legal future plans="+different+" / "+actors.Count);Console.WriteLine(File.ReadAllText(output));
        }
        internal static void Pressure(string output)
        {
            var data=new NetworkData();for(int i=0;i<=420;i++)data.junctions.Add(new Junction{id=i+1,position=new Point(i*30,0,0)});
            for(int i=0;i<420;i++)data.roads.Add(new PhysicalRoad{id=i+1,from=i+1,to=i+2,lanes=1,speed=12,points=new[]{data.junctions[i].position,data.junctions[i+1].position}});
            data.nextRoad=421;data.nextJunction=422;var n=new Network(data);n.RouteSharingEnabled=false;
            Action fill=()=>{for(int i=1;i<=12;i++)for(int j=200;j<=420;j++){var a=n.Get(Network.LaneId(i,false,0));var b=n.Get(Network.LaneId(j,false,0));n.Route(new RoadAccess{lane=a.Id,distance=a.StartDistance},new RoadAccess{lane=b.Id,distance=b.EndDistance});}};
            fill();long before=GC.GetTotalMemory(true);n.RouteSharingEnabled=true;fill();long after=GC.GetTotalMemory(true);
            Check(n.SharedRouteCount<=4096&&n.SharedRouteBytes<=8*1024*1024&&n.SharedRouteEvictions>0,"Memory pressure did not bound/evict cache");
            var first=n.Get(Network.LaneId(1,false,0));var last=n.Get(Network.LaneId(420,false,0));var source=new RoadAccess{lane=first.Id,distance=first.StartDistance};var goal=new RoadAccess{lane=last.Id,distance=last.EndDistance};var expected=n.Route(source,goal);fill();Check(expected.SequenceEqual(n.Route(source,goal)),"Eviction changed path");
            File.WriteAllText(output,"entries,logical_bytes,managed_heap_delta,evictions\n"+string.Join(",",n.SharedRouteCount,n.SharedRouteBytes,after-before,n.SharedRouteEvictions));Console.WriteLine("PASS cache pressure/eviction: "+n.SharedRouteCount+" entries / "+n.SharedRouteBytes+" logical bytes; heap delta "+(after-before));
        }
        internal static void Continue(string save,string directory,int steps)
        {
            Directory.CreateDirectory(directory);var bytes=File.ReadAllBytes(save);var hot=CityCoreSave.Read(bytes);var control=CityCoreSave.Read(bytes);
            hot.Transport.DepotLaneRoutingEnabled=true;control.Transport.DepotLaneRoutingEnabled=true;hot.Simulation.Growth.Zoning.SetSurface((x,z)=>2);control.Simulation.Growth.Zoning.SetSurface((x,z)=>2);
            var timer=Stopwatch.StartNew();for(int i=0;i<steps;i++)
            {
                control.Transport.Network.RouteSharingEnabled=false;hot.Simulation.Advance(.05,1,double.PositiveInfinity);control.Simulation.Advance(.05,1,double.PositiveInfinity);
                if(i%40==39){hot.Validate();control.Validate();Check(CityCoreSave.Write(hot).SequenceEqual(CityCoreSave.Write(control)),"Shared/uncached full city diverged at step "+i);}if(i%400==399)Console.WriteLine("Validated paired city steps "+(i+1));
            }
            timer.Stop();hot.Validate();control.Validate();var final=CityCoreSave.Write(hot);File.WriteAllBytes(Path.Combine(directory,"final.hcity"),final);Check(final.SequenceEqual(CityCoreSave.Write(control)),"Final cache state divergence");
            var cold=CityCoreSave.Read(final);cold.Transport.DepotLaneRoutingEnabled=true;cold.Simulation.Growth.Zoning.SetSurface((x,z)=>2);
            for(int i=0;i<80;i++){hot.Simulation.Advance(.05,1,double.PositiveInfinity);cold.Simulation.Advance(.05,1,double.PositiveInfinity);if(i%10==9)Check(CityCoreSave.Write(hot).SequenceEqual(CityCoreSave.Write(cold)),"Cold route cache changed complete city");}hot.Validate();cold.Validate();
            File.WriteAllText(Path.Combine(directory,"Result.txt"),"PASS "+steps+" paired complete fixed steps, shared vs uncached A*, safety/economy/waste validation every40; 80 hot/cold steps exact. Paired wall ms="+timer.Elapsed.TotalMilliseconds.ToString("R",CultureInfo.InvariantCulture));Console.WriteLine(File.ReadAllText(Path.Combine(directory,"Result.txt")));
        }
    }
}
