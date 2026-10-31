using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using HarborCity;
using HarborCity.RoadCore;
static class CityDepotLaneRoutingChecks
{
    static int checks, different; static void Check(bool x, string m)
    {
        checks++;
        if (!x)
            throw new Exception(m);
    }
    static void Compare(Network n, List<long> old, List<long> path, double os, double oe, double s, double e)
    {
        Check((old == null) == (path == null), "Reachability mismatch");
        if (old == null)
            return;
        n.ValidateRoute(path);
        double a = new DispatchBlueprint(n, old, os, oe).seconds, b = new DispatchBlueprint(n, path, s, e).seconds;
        Check(Math.Abs(a - b) <= 1e-9 * Math.Max(1, Math.Abs(a)), "Shortest cost mismatch " + a + " / " + b);
        if (!old.SequenceEqual(path) || os != s || oe != e)
            different++;
    }
    static void Recheck(World w)
    {
        var a = w.GetAccess(20);
        if (a == null || !a.valid)
            a = w.Snapshot().accesses.First(x => x.valid);
        var points = w.Snapshot().accesses.Where(x => x.valid).Take(20).ToArray();
        foreach (var p in points)
        {
            var old = AccessRouting.Find(w.Network, a, p, true, out double os, out double oe);
            Check(w.DepotRoutes.TryFind(w, a.endpoint, a, p, false, out var r, out double s, out double e), "Edit tree missing");
            Compare(w.Network, old, r, os, oe, s, e);
            old = AccessRouting.FromLane(w.Network, p.lane, p.distance, a, true, out oe);
            w.DepotRoutes.TryFind(w, a.endpoint, a, p, true, out r, out s, out e);
            Compare(w.Network, old, r, p.distance, oe, s, e);
        }
    }
    static void Main()
    {
        Check(DepotLaneRouting.ReservationForLanes(9508)==609536,"Four-tree array reservation changed");
        Check(DepotLaneRouting.Capacity*DepotLaneRouting.ReservationForLanes(9508)>64L*1024*1024,"Count-cap regression fixture must exceed the former byte cap");
        var w = new World();
        var ids = new int[4, 4];
        for (int z = 0; z < 4; z++)
        for (int x = 0; x < 4; x++)
            ids[x, z] = w.AddJunction(new Point(x * 25, 2, z * 25));
        for (int z = 0; z < 4; z++)
        for (int x = 0; x < 4; x++)
        {
            if (x < 3)
                w.AddRoad(ids[x, z], ids[x + 1, z], new[] { w.JunctionPosition(ids[x, z]), w.JunctionPosition(ids[x + 1, z]) }, lanes: 2);
            if (z < 3)
                w.AddRoad(ids[x, z], ids[x, z + 1], new[] { w.JunctionPosition(ids[x, z]), w.JunctionPosition(ids[x, z + 1]) }, oneWay: x % 2 == 1, lanes: 2);
        }
        var lanes = w.Network.Drivables.Where(d => !d.IsConnection).ToArray();
        var accesses = lanes.Select((d, i) => new RoadAccess { endpoint = i + 1, lane = d.Id, distance = d.StartDistance + d.TravelLength * (.15 + (i % 7) * .1) }).ToArray();
        foreach (var a in accesses)
            w.RegisterAccess(a.endpoint, a.lane, a.distance);
        var baseAccess=accesses[0];var destinationAccess=accesses[8];
        byte[] routingFacts=SaveCodec.Write(w);
        w.DepotLaneRoutingEnabled=true;
        var lanePlan=w.ReplayDepotQuery(baseAccess.endpoint,baseAccess,destinationAccess,false);
        Check(w.DepotRoutes.TreeBuilds>0&&w.JunctionRoutes.Builds==0,"Lane selection accidentally ran junction routing");
        long laneBuilds=w.DepotRoutes.TreeBuilds;
        bool rejected=false;try{w.DepotJunctionRoutingEnabled=true;}catch(InvalidOperationException){rejected=true;}
        Check(rejected&&w.DepotLaneRoutingEnabled&&!w.DepotJunctionRoutingEnabled,"Conflicting algorithm switch was not atomic");
        w.DepotLaneRoutingEnabled=false;w.DepotJunctionRoutingEnabled=true;
        w.JunctionRoutes.SetDepots(new[]{baseAccess.endpoint},w.GetAccess,()=>w.AccessRevision);
        var junctionPlan=w.ReplayDepotQuery(baseAccess.endpoint,baseAccess,destinationAccess,false);
        Check(w.JunctionRoutes.Builds>0&&w.DepotRoutes.TreeBuilds==laneBuilds,"Junction selection accidentally ran lane routing");
        rejected=false;try{w.DepotLaneRoutingEnabled=true;}catch(InvalidOperationException){rejected=true;}
        Check(rejected&&!w.DepotLaneRoutingEnabled&&w.DepotJunctionRoutingEnabled,"Reverse conflicting switch was not atomic");
        w.DepotJunctionRoutingEnabled=false;w.DepotLaneRoutingEnabled=true;
        var warmLane=w.ReplayDepotQuery(baseAccess.endpoint,baseAccess,destinationAccess,false);
        Check(lanePlan.route.SequenceEqual(warmLane.route)&&w.DepotRoutes.TreeBuilds==laneBuilds,"Switch back lost deterministic warm lane cache");
        Check(routingFacts.SequenceEqual(SaveCodec.Write(w)),"Routing selection changed authoritative world facts");
        foreach (var depot in accesses.Take(12))
        foreach (var target in accesses)
        {
            var old = AccessRouting.Find(w.Network, depot, target, true, out double os, out double oe);
            Check(w.DepotRoutes.TryFind(w, depot.endpoint, depot, target, false, out var path, out double s, out double e), "No tree");
            Compare(w.Network, old, path, os, oe, s, e);
            var prior = path?.ToArray();
            w.DepotRoutes.TryFind(w, depot.endpoint, depot, target, false, out path, out s, out e);
            Check(prior == null ? path == null : prior.SequenceEqual(path), "Warm changed route");
            old = AccessRouting.FromLane(w.Network, target.lane, target.distance, depot, true, out oe);
            Check(w.DepotRoutes.TryFind(w, depot.endpoint, depot, target, true, out path, out s, out e), "No reverse tree");
            Compare(w.Network, old, path, target.distance, oe, s, e);
                Check(path==null||path[0]==target.lane&&s==target.distance,"Return moved actual source lane or position");
                Check(path==null||AccessRouting.Matches(w.Network,depot,path[path.Count-1],e,true),"Return missed depot access");
        }
        long invalidations = w.DepotRoutes.Invalidations;
        w.SplitRoadAt(2, 12);
        Recheck(w);
        Check(w.DepotRoutes.Invalidations > invalidations, "Split retained stale tree");
        Check(w.PlanUpgradeRoad(3, 3, 17, RoadDirection.Both).TryCommit(), "Upgrade rejected");
        Recheck(w);
        Check(w.PlanUpgradeRoad(3, 2, 9, RoadDirection.Backward).TryCommit(), "Direction change rejected");
        Recheck(w);
        w.RemoveRoad(1);
        Recheck(w);
        var anchor = w.GetAccess(20);
        var lane = w.Network.Get(anchor.lane);
        anchor.distance = (lane.StartDistance + lane.EndDistance) / 2;
        Recheck(w);
        for (int i = 1000; i < 1000+DepotLaneRouting.Capacity+6; i++)
        {
            w.RegisterAccess(i, anchor.lane, anchor.distance);
            w.DepotRoutes.TryFind(w, i, w.GetAccess(i), anchor, false, out _, out _, out _);
        }
        Check(w.DepotRoutes.Count <= DepotLaneRouting.Capacity && w.DepotRoutes.Evictions > 0, "Depot-count LRU bound not enforced");
        w.DepotRoutes.Clear();
        Recheck(w);
        var testLane = w.Network.Drivables.First(d => !d.IsConnection);
        int sequence = 4000;
        foreach (double fraction in new[] { 0.0, .3, 1.0 })
        {
            int id = sequence++;
            double at = testLane.StartDistance + fraction * testLane.TravelLength;
            w.RegisterAccess(id, testLane.Id, at);
            var a = w.GetAccess(id);
            foreach (double targetFraction in new[] { 0.0, .2, .8, 1.0 })
            {
                var p = new RoadAccess { lane = testLane.Id, distance = testLane.StartDistance + targetFraction * testLane.TravelLength };
                var old = AccessRouting.Find(w.Network, a, p, true, out double os, out double oe);
                w.DepotRoutes.TryFind(w, id, a, p, false, out var r, out double s, out double e);
                Compare(w.Network, old, r, os, oe, s, e);
                old = AccessRouting.FromLane(w.Network, p.lane, p.distance, a, true, out oe);
                w.DepotRoutes.TryFind(w, id, a, p, true, out r, out s, out e);
                Compare(w.Network, old, r, p.distance, oe, s, e);
            }
        }
        int ja = w.AddJunction(new Point(400, 2, 400)), jb = w.AddJunction(new Point(425, 2, 400));
        int island = w.AddRoad(ja, jb, new[] { w.JunctionPosition(ja), w.JunctionPosition(jb) }, oneWay: true);
        var isolated = w.Network.Get(Network.LaneId(island, false));
        var unreachable = new RoadAccess { lane = isolated.Id, distance = (isolated.StartDistance + isolated.EndDistance) / 2 };
        var home = w.GetAccess(4000);
        Check(w.DepotRoutes.TryFind(w, home.endpoint, home, unreachable, false, out var noPath, out _, out _) && noPath == null, "Disconnected component was reachable");
        byte[] unchanged = SaveCodec.Write(w);
        w.DepotRoutes.TryFind(w, home.endpoint, home, unreachable, true, out noPath, out _, out _);
        Check(noPath == null && unchanged.SequenceEqual(SaveCodec.Write(w)), "Derived routing mutated world facts");
        Check(!w.DepotRoutes.TryFind(w, home.endpoint, home, new RoadAccess { lane = home.lane, distance = double.NaN }, true, out _, out _, out _), "Invalid point did not fall back");
        w.DepotRoutes.RetainDepots(100,new[]{home.endpoint});
        Check(w.DepotRoutes.Count<=1,"Deleted depot association retained");
        Check(!w.DepotRoutes.TryFind(w,1000,w.GetAccess(1000),home,false,out _,out _,out _),"Deleted depot should use legacy fallback");
        w.RegisterAccess(5000,home.lane,home.distance);w.DepotRoutes.RetainDepots(101,new[]{home.endpoint,5000});
        Check(w.DepotRoutes.TryFind(w,5000,w.GetAccess(5000),home,false,out _,out _,out _),"New depot not admitted");
        w.DepotRoutes.RetainDepots(102,new[]{home.endpoint});Check(w.DepotRoutes.Count<=1,"Removed new depot not pruned");
        // Oversized declared live sets use the common point router. This is a
        // world-derived policy, not a cache-heat dependent change of algorithm.
        var beforePolicy=SaveCodec.Write(w);
        w.DepotRoutes.Clear();w.DepotRoutes.RetainDepots(103,Enumerable.Range(1,199).Concat(new[]{home.endpoint}));
        Check(w.DepotRoutes.CanServeLiveSet,"200 declared depots should fit the enlarged cache");long at200=w.DepotRoutes.TreeBuilds;
        w.ReplayDepotQuery(home.endpoint,home,w.GetAccess(5000),false);
        Check(w.DepotRoutes.TreeBuilds>at200&&w.DepotRoutes.Count==1,"200-depot policy still fell back or eagerly created unqueried depots");
        w.DepotRoutes.RetainDepots(104,Enumerable.Range(1,DepotLaneRouting.Capacity-1).Concat(new[]{home.endpoint}));
        Check(w.DepotRoutes.CanServeLiveSet,"Exact capacity should remain eligible");
        w.DepotRoutes.RetainDepots(105,Enumerable.Range(1,DepotLaneRouting.Capacity).Concat(new[]{home.endpoint}));
        Check(!w.DepotRoutes.CanServeLiveSet&&w.DepotRoutes.Count==0,"Oversized tree set not released");long builds=w.DepotRoutes.TreeBuilds;var common=w.ReplayDepotQuery(home.endpoint,home,w.GetAccess(5000),false);
        Check(w.DepotRoutes.TreeBuilds==builds&&common.route!=null,"Oversized set rebuilt full trees instead of common routing");w.Network.ValidateRoute(common.route.ToList());
        Check(beforePolicy.SequenceEqual(SaveCodec.Write(w)),"Routing policy changed authoritative facts");w.DepotRoutes.RetainDepots(106,new[]{home.endpoint});
        Check(w.DepotRoutes.CanServeLiveSet,"Shrinking live set did not restore bounded tree eligibility");
        Console.WriteLine("PASS " + checks + " lane-tree checks; equal-cost different route choices=" + different + "; trees=" + w.DepotRoutes.Count + " bytes=" + w.DepotRoutes.ReservedTreeBytes);
    }
}
