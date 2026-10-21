using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using HarborCity;
using HarborCity.RoadCore;
static class CityDepotJunctionRoutingChecks
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
        Check(!double.IsNaN(b)&&!double.IsInfinity(b)&&b>=0, "Nonfinite route cost");
        if (!old.SequenceEqual(path) || os != s || oe != e)
            different++;
    }
    static bool Find(World w,int id,RoadAccess anchor,RoadAccess point,bool back,out List<long> path,out double start,out double end)
    {
        bool handled;
        if(back){start=point.distance;handled=w.JunctionRoutes.Return(w.Network,id,anchor,point.lane,point.distance,out path,out end);}
        else handled=w.JunctionRoutes.Outbound(w.Network,id,anchor,point,out path,out start,out end);
        if(path!=null){var first=w.Network.Get(path[0]);var last=w.Network.Get(path[path.Count-1]);Check(start>=first.StartDistance&&start<=first.EndDistance&&end>=last.StartDistance&&end<=last.EndDistance,"Invalid partial lane");Check(path.Count>1||end>=start,"Same-lane backwards");Check(back?path[0]==point.lane&&start==point.distance:AccessRouting.Matches(w.Network,anchor,path[0],start,true),"Wrong source");Check(AccessRouting.Matches(w.Network,back?anchor:point,path[path.Count-1],end,true),"Wrong destination");}
        return handled;
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
            Check(Find(w, a.endpoint, a, p, false, out var r, out double s, out double e), "Edit tree missing");
            Compare(w.Network, old, r, os, oe, s, e);
            old = AccessRouting.FromLane(w.Network, p.lane, p.distance, a, true, out oe);
            Find(w, a.endpoint, a, p, true, out r, out s, out e);
            Compare(w.Network, old, r, p.distance, oe, s, e);
        }
    }
    static void Main()
    {
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
        foreach (var depot in accesses.Take(12))
        foreach (var target in accesses)
        {
            var old = AccessRouting.Find(w.Network, depot, target, true, out double os, out double oe);
            Check(Find(w, depot.endpoint, depot, target, false, out var path, out double s, out double e), "No tree");
            Compare(w.Network, old, path, os, oe, s, e);
            var prior = path?.ToArray();
            Find(w, depot.endpoint, depot, target, false, out path, out s, out e);
            Check(prior == null ? path == null : prior.SequenceEqual(path), "Warm changed route");
            old = AccessRouting.FromLane(w.Network, target.lane, target.distance, depot, true, out oe);
            Check(Find(w, depot.endpoint, depot, target, true, out path, out s, out e), "No reverse tree");
            Compare(w.Network, old, path, target.distance, oe, s, e);
                Check(path==null||path[0]==target.lane&&s==target.distance,"Return moved actual source lane or position");
                Check(path==null||AccessRouting.Matches(w.Network,depot,path[path.Count-1],e,true),"Return missed depot access");
        }
        long invalidations = w.JunctionRoutes.Invalidations;
        w.SplitRoadAt(2, 12);
        Recheck(w);
        Check(w.JunctionRoutes.Invalidations > invalidations, "Split retained stale tree");
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
        for (int i = 1000; i < 1134; i++)
        {
            w.RegisterAccess(i, anchor.lane, anchor.distance);
            Find(w, i, w.GetAccess(i), anchor, false, out _, out _, out _);
        }
        Check(w.JunctionRoutes.Count <= w.JunctionRoutes.MaximumDepots && w.JunctionRoutes.TreeBytes <= w.JunctionRoutes.MaximumTreeBytes && w.JunctionRoutes.Evictions > 0, "LRU bound not enforced");
        w.JunctionRoutes.Clear();
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
                Find(w, id, a, p, false, out var r, out double s, out double e);
                Compare(w.Network, old, r, os, oe, s, e);
                old = AccessRouting.FromLane(w.Network, p.lane, p.distance, a, true, out oe);
                Find(w, id, a, p, true, out r, out s, out e);
                Compare(w.Network, old, r, p.distance, oe, s, e);
            }
        }
        int ja = w.AddJunction(new Point(400, 2, 400)), jb = w.AddJunction(new Point(425, 2, 400));
        int island = w.AddRoad(ja, jb, new[] { w.JunctionPosition(ja), w.JunctionPosition(jb) }, oneWay: true);
        var isolated = w.Network.Get(Network.LaneId(island, false));
        var unreachable = new RoadAccess { lane = isolated.Id, distance = (isolated.StartDistance + isolated.EndDistance) / 2 };
        var home = w.GetAccess(4000);
        Check(Find(w, home.endpoint, home, unreachable, false, out var noPath, out _, out _) && noPath == null, "Disconnected component was reachable");
        byte[] unchanged = SaveCodec.Write(w);
        Find(w, home.endpoint, home, unreachable, true, out noPath, out _, out _);
        Check(noPath == null && unchanged.SequenceEqual(SaveCodec.Write(w)), "Derived routing mutated world facts");
        Check(!Find(w, home.endpoint, home, new RoadAccess { lane = home.lane, distance = double.NaN }, true, out _, out _, out _), "Invalid point did not fall back");
        w.JunctionRoutes.SetDepots(new[]{home.endpoint});
        Check(w.JunctionRoutes.Count<=1,"Deleted depot association retained");
        Check(!Find(w,1000,w.GetAccess(1000),home,false,out _,out _,out _),"Deleted depot should use legacy fallback");
        w.RegisterAccess(5000,home.lane,home.distance);w.JunctionRoutes.SetDepots(new[]{home.endpoint,5000});
        Check(Find(w,5000,w.GetAccess(5000),home,false,out _,out _,out _),"New depot not admitted");
        w.JunctionRoutes.SetDepots(new[]{home.endpoint});Check(w.JunctionRoutes.Count<=1,"Removed new depot not pruned");
        Console.WriteLine("PASS " + checks + " junction quotient checks; legal nonshortest/different route choices=" + different + "; trees=" + w.JunctionRoutes.Count + " bytes=" + w.JunctionRoutes.TreeBytes);
    }
}
