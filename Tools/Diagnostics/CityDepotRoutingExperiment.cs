using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Cryptography;
using HarborCity;
using HarborCity.RoadCore;
static class CityDepotRoutingExperiment
{
    sealed class Query
    {
        internal int depot; internal RoadAccess a, p; internal bool back;
    }
    static RoadAccess Copy(RoadAccess a) => a == null ? null : new RoadAccess { endpoint = a.endpoint, lane = a.lane, distance = a.distance, use = a.use, valid = a.valid };
    static string Hash(byte[] b)
    {
        using (var h = SHA256.Create())
            return BitConverter.ToString(h.ComputeHash(b)).Replace("-", "");
    }
    static CityCoreIndustrySession Load(byte[] b)
    {
        var s = CityCoreSave.Read(b);
        s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
        return s;
    }
    static string Business(CityCoreIndustrySession s)
    {
        var w = s.Simulation.Waste;
        return string.Join(",", s.City.population, s.City.money, s.City.buildings.Count, s.Transport.ActiveCount, w.Generated, w.Collected, w.InTransit, w.Stored, w.Processed, w.BalanceError);
    }
    static void Sequence(string file, List<Query> q)
    {
        File.WriteAllLines(file, new[] { "ordinal,depot,returning,anchor_lane,anchor_distance,point_endpoint,point_lane,point_distance" }.Concat(q.Select((x, i) => string.Join(",", i, x.depot, x.back, x.a.lane, x.a.distance, x.p.endpoint, x.p.lane, x.p.distance))));
    }
    static void Main(string[] args)
    {
        if(args.Length==4&&args[0]=="--captured"){ReplayCaptured(args[1],args[2],args[3]);return;}
        GC.KeepAlive(CityCoreProfile.AllocationAvailable);
        if(Directory.Exists(args[1]))throw new IOException("Choose a fresh experiment result directory");
        Directory.CreateDirectory(args[1]);
        string dir = args[1];
        int steps = int.Parse(args[2]);
        var warm = Load(File.ReadAllBytes(args[0]));
        for (int i = 0; i < 200; i++)
            warm.Simulation.Advance(.05, 1, double.PositiveInfinity);
        byte[] input = CityCoreSave.Write(warm);
        File.WriteAllBytes(Path.Combine(dir, "warm.hcity"), input);
        var queries = new List<Query>();
        var before = Load(input);
        var after = Load(input);
        var network = before.Transport.Network;
        DepotRoutingTrace.QueryObserver = (d, a, p, r) => queries.Add(new Query { depot = d, a = Copy(a), p = Copy(p), back = r });
        DepotRoutingTrace.Reset();
        DepotRoutingTrace.Enabled = true;
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < steps; i++)
            before.Simulation.Advance(.05, 1, double.PositiveInfinity);
        watch.Stop();
        double oldMs = watch.Elapsed.TotalMilliseconds;
        DepotRoutingTrace.Enabled = false;
        DepotRoutingTrace.QueryObserver = null;
        DepotRoutingTrace.Write(Path.Combine(dir, "baseline-routing.csv"));
        before.Validate();
        byte[] oldState = CityCoreSave.Write(before);
        File.WriteAllBytes(Path.Combine(dir, "baseline.hcity"), oldState);
        var newQueries = new List<Query>();
        DepotRoutingTrace.QueryObserver = (d, a, p, r) => newQueries.Add(new Query { depot = d, a = Copy(a), p = Copy(p), back = r });
        after.Transport.DepotLaneRoutingEnabled = true;
        DepotRoutingTrace.Reset();
        DepotRoutingTrace.Enabled = true;
        watch.Restart();
        for (int i = 0; i < steps; i++)
            after.Simulation.Advance(.05, 1, double.PositiveInfinity);
        watch.Stop();
        double newMs = watch.Elapsed.TotalMilliseconds;
        DepotRoutingTrace.QueryObserver = null;
        DepotRoutingTrace.Enabled = false;
        DepotRoutingTrace.Write(Path.Combine(dir, "tree-routing.csv"));
        after.Validate();
        byte[] newState = CityCoreSave.Write(after);
        File.WriteAllBytes(Path.Combine(dir, "tree.hcity"), newState);
        Sequence(Path.Combine(dir, "baseline-service-sequence.csv"), queries);
        Sequence(Path.Combine(dir, "tree-service-sequence.csv"), newQueries);
        File.WriteAllLines(Path.Combine(dir, "business-state.csv"), new[] { "mode,population,money,buildings,active,generated,collected,in_transit,stored,processed,balance_error", "baseline," + Business(before), "tree," + Business(after) });
        var cold = Load(newState);
        cold.Transport.DepotLaneRoutingEnabled = true;
        for (int i = 0; i < 200; i++)
        {
            after.Simulation.Advance(.05, 1, double.PositiveInfinity);
            cold.Simulation.Advance(.05, 1, double.PositiveInfinity);
        }
        after.Validate();
        cold.Validate();
        bool restore = CityCoreSave.Write(after).SequenceEqual(CityCoreSave.Write(cold));
        if (!restore)
            throw new Exception("Tree warm/cold continuation differs");
        File.WriteAllText(Path.Combine(dir, "simulation.csv"), "steps,baseline_ms,tree_ms,full_state_equal,baseline_sha256,tree_sha256,warm_cold_200_steps_equal,queries,depots,tree_bytes,graph_bytes,evictions,invalidations\n" + string.Join(",", steps, oldMs, newMs, oldState.SequenceEqual(newState), Hash(oldState), Hash(newState), restore, queries.Count, queries.Select(q => q.depot).Distinct().Count(), after.Transport.DepotRoutes.ReservedTreeBytes, after.Transport.DepotRoutes.ReservedGraphBytes, after.Transport.DepotRoutes.Evictions, after.Transport.DepotRoutes.Invalidations));
        // Replay the exact baseline service requests, including original OD cache hits.
        if (!ReferenceEquals(network, before.Transport.Network))
            throw new Exception("Replay requires unchanged topology");
        var paths = new List<DispatchBlueprint>();
        before.Transport.ResetDepotExperimentCaches();
        before.Transport.DepotLaneRoutingEnabled = false;
        DepotRoutingTrace.Reset();
        DepotRoutingTrace.Enabled = true;
        foreach (var q in queries)
            paths.Add(before.Transport.ReplayDepotQuery(q.depot, q.a, q.p, q.back));
        DepotRoutingTrace.Enabled = false;
        DepotRoutingTrace.Write(Path.Combine(dir, "replay-baseline.csv"));
        before.Transport.ResetDepotExperimentCaches();
        before.Transport.DepotLaneRoutingEnabled = true;
        DepotRoutingTrace.Reset();
        long heapBefore = GC.GetTotalMemory(true);
        DepotRoutingTrace.Enabled = true;
        var choiceRows = new List<string> { "query,depot,returning,old_cost,new_cost,delta,old_source,new_source,old_goal,new_goal" };
        double maxDelta = 0;
        int changed = 0;
        for (int i = 0; i < queries.Count; i++)
        {
            var q = queries[i];
            var r = before.Transport.ReplayDepotQuery(q.depot, q.a, q.p, q.back);
            var p = paths[i];
            if (!double.IsPositiveInfinity(p.seconds))
                maxDelta = Math.Max(maxDelta, Math.Abs(r.seconds - p.seconds));
            if ((r.route == null) != (p.route == null) || !double.IsPositiveInfinity(p.seconds) && Math.Abs(r.seconds - p.seconds) > 1e-9 * Math.Max(1, p.seconds))
                throw new Exception("Replay shortest cost/reachability mismatch " + i);
            if (r.route != null)
            {
                before.Transport.Network.ValidateRoute(r.route.ToList());
                if (!r.route.SequenceEqual(p.route) || r.start != p.start || r.end != p.end)
                {
                    changed++;
                    choiceRows.Add(string.Join(",", i, q.depot, q.back, p.seconds.ToString("R"), r.seconds.ToString("R"), (r.seconds - p.seconds).ToString("R"), p.route.First(), r.route.First(), p.route.Last(), r.route.Last()));
                }
            }
        }
        DepotRoutingTrace.Enabled = false;
        DepotRoutingTrace.Write(Path.Combine(dir, "replay-tree.csv"));
        File.WriteAllLines(Path.Combine(dir, "route-choices.csv"), choiceRows);
        File.WriteAllText(Path.Combine(dir, "replay-heap.txt"), "Managed retained heap delta after forced collections (trees, graph and trace; not exclusive): " + (GC.GetTotalMemory(true) - heapBefore));
        DepotRoutingTrace.Reset();
        DepotRoutingTrace.Enabled = true;
        foreach (var q in queries)
            before.Transport.ReplayDepotQuery(q.depot, q.a, q.p, q.back);
        DepotRoutingTrace.Enabled = false;
        DepotRoutingTrace.Write(Path.Combine(dir, "replay-tree-hot.csv"));
        File.WriteAllText(Path.Combine(dir, "replay-result.txt"), "PASS exact baseline service queries: " + queries.Count + "; equal-cost route differences=" + changed + "; max_cost_delta_seconds=" + maxDelta + "; capacity=" + before.Transport.DepotRoutes.Count + "; reservedBytes=" + before.Transport.DepotRoutes.ReservedTreeBytes + "; evictions=" + before.Transport.DepotRoutes.Evictions + "\n");
        // A real accepted isolated-road edit changes Network identity without changing any old route costs.
        int ja = before.Transport.AddJunction(new Point(760, 2, 760)), jb = before.Transport.AddJunction(new Point(780, 2, 760));
        before.Transport.AddRoad(ja, jb, new[] { before.Transport.JunctionPosition(ja), before.Transport.JunctionPosition(jb) });
        DepotRoutingTrace.Reset();
        DepotRoutingTrace.Enabled = true;
        for (int i = 0; i < queries.Count; i++)
        {
            var q = queries[i];
            var r = before.Transport.ReplayDepotQuery(q.depot, q.a, q.p, q.back);
            var p = paths[i];
            if ((r.route == null) != (p.route == null) || !double.IsPositiveInfinity(p.seconds) && Math.Abs(r.seconds - p.seconds) > 1e-9 * Math.Max(1, p.seconds))
                throw new Exception("Post-edit cost mismatch");
        }
        DepotRoutingTrace.Enabled = false;
        DepotRoutingTrace.Write(Path.Combine(dir, "replay-tree-after-edit.csv"));
        var point = queries.First().p;
        foreach (int pass in new[] { 1, 2 })
        {
            DepotRoutingTrace.Reset();
            long misses = before.Transport.DepotRoutes.Misses, evictions = before.Transport.DepotRoutes.Evictions;
            DepotRoutingTrace.Enabled = true;
            foreach (var plant in before.City.buildings.OfType<LandfillBuilding>())
            {
                var access = before.Transport.GetAccess(plant.id);
                if (access == null || !access.valid)
                    continue;
                before.Transport.ReplayDepotQuery(plant.id, access, point, false);
                before.Transport.ReplayDepotQuery(plant.id, access, point, true);
            }
            DepotRoutingTrace.Enabled = false;
            DepotRoutingTrace.Write(Path.Combine(dir, "all-depots-pass" + pass + ".csv"));
            File.WriteAllText(Path.Combine(dir, "all-depots-pass" + pass + "-cache.csv"), "misses,evictions,entries,reserved_bytes\n" + string.Join(",", before.Transport.DepotRoutes.Misses - misses, before.Transport.DepotRoutes.Evictions - evictions, before.Transport.DepotRoutes.Count, before.Transport.DepotRoutes.ReservedTreeBytes));
        }
        before.Transport.ResetDepotExperimentCaches();
        before.Transport.DepotLaneRoutingEnabled = false;
        DepotRoutingTrace.Reset();
        DepotRoutingTrace.Enabled = true;
        foreach (var plant in before.City.buildings.OfType<LandfillBuilding>())
        {
            var access = before.Transport.GetAccess(plant.id);
            if (access == null || !access.valid)
                continue;
            before.Transport.ReplayDepotQuery(plant.id, access, point, false);
            before.Transport.ReplayDepotQuery(plant.id, access, point, true);
        }
        DepotRoutingTrace.Enabled = false;
        DepotRoutingTrace.Write(Path.Combine(dir, "all-depots-baseline.csv"));
        var differences = before.Transport.Vehicles.Join(Load(newState).Transport.Vehicles, v => v.id, v => v.id, (a, b) => new { a, b }).Where(x => !x.a.Plan.segments.SequenceEqual(x.b.Plan.segments) || x.a.distance != x.b.distance || x.a.Current != x.b.Current).Take(5).Select(x => "vehicle=" + x.a.id + " purpose=" + x.a.Task.purpose + " oldLane=" + x.a.Current + " newLane=" + x.b.Current + " oldDistance=" + x.a.distance + " newDistance=" + x.b.distance + " oldRoute=" + string.Join("/", x.a.Plan.segments) + " newRoute=" + string.Join("/", x.b.Plan.segments));
        File.WriteAllLines(Path.Combine(dir, "state-differences.txt"), differences);
        Console.WriteLine(File.ReadAllText(Path.Combine(dir, "simulation.csv")));
        Console.WriteLine(File.ReadAllText(Path.Combine(dir, "replay-result.txt")));
    }
    // Read-only algorithm study: replay actual Player requests with the EXISTING
    // tree implementation. A private cache has no declared live-set filter; this
    // deliberately does NOT change the production world's 200>128 policy.
    static void ReplayCaptured(string save,string capture,string output)
    {
        System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.InvariantCulture;
        if(Directory.Exists(output))throw new IOException("Fresh captured replay output required");
        Directory.CreateDirectory(output);var city=Load(File.ReadAllBytes(save));var world=city.Transport;
        string before=Hash(CityCoreSave.Write(city));var lines=File.ReadAllLines(capture);var header=lines[0].Split(',');
        string Field(string[] row,string name){int i=Array.IndexOf(header,name);if(i<0)throw new Exception("Missing captured field "+name);return row[i];}
        var queries=new List<Query>();
        foreach(string line in lines.Skip(1))
        {
            var r=line.Split(',');var anchor=world.GetAccess(int.Parse(Field(r,"depot"))).Copy();
            bool back=bool.Parse(Field(r,"returning"));int endpoint=int.Parse(Field(r,"point_endpoint"));
            var point=back?new RoadAccess():world.GetAccess(endpoint).Copy();point.endpoint=endpoint;
            long al=long.Parse(Field(r,"anchor_lane")),pl=long.Parse(Field(r,"point_lane"));double ad=double.Parse(Field(r,"anchor_distance")),pd=double.Parse(Field(r,"point_distance"));
            if(anchor.lane!=al||BitConverter.DoubleToInt64Bits(anchor.distance)!=BitConverter.DoubleToInt64Bits(ad)||!back&&(point.lane!=pl||BitConverter.DoubleToInt64Bits(point.distance)!=BitConverter.DoubleToInt64Bits(pd)))throw new Exception("Captured access differs from frozen network");
            point.lane=pl;point.distance=pd;queries.Add(new Query{depot=anchor.endpoint,a=anchor,p=point,back=back});
        }
        if(queries.Count==0||queries.Count>10000)throw new Exception("Bounded captured query set required");
        world.ResetDepotExperimentCaches();world.DepotLaneRoutingEnabled=false;DepotRoutingTrace.Enabled=false;
        var reference=new List<DispatchBlueprint>();var timer=Stopwatch.StartNew();
        foreach(var q in queries)reference.Add(world.ReplayDepotQuery(q.depot,q.a,q.p,q.back));
        timer.Stop();double baselineMs=timer.Elapsed.TotalMilliseconds;
        var cache=new DepotLaneRouting();var coldPaths=new List<DispatchBlueprint>();var costs=new List<string>{"query,depot,returning,baseline_seconds,tree_seconds,delta_seconds,route_equal,start_equal,end_equal,baseline_first_lane,tree_first_lane"};
        long bytes=GC.GetAllocatedBytesForCurrentThread();timer.Restart();
        foreach(var q in queries){if(!cache.TryFind(world,q.depot,q.a,q.p,q.back,out var route,out double start,out double end))throw new Exception("Isolated tree replay could not serve valid input");coldPaths.Add(new DispatchBlueprint(world.Network,route,start,end));}
        timer.Stop();double coldMs=timer.Elapsed.TotalMilliseconds;long coldBytes=GC.GetAllocatedBytesForCurrentThread()-bytes;long coldBuilds=cache.TreeBuilds;
        int routesChanged=0,curbsChanged=0;double maxDelta=0;
        for(int i=0;i<queries.Count;i++)
        {
            var q=queries[i];var a=reference[i];var b=coldPaths[i];bool reachable=a.route!=null;
            if(reachable!=(b.route!=null))throw new Exception("Captured replay reachability differs "+i);
            if(reachable){world.Network.ValidateRoute(b.route.ToList());if(!AccessRouting.Matches(world.Network,q.a,q.back?b.route.Last():b.route.First(),q.back?b.end:b.start,true)||!AccessRouting.Matches(world.Network,q.p,q.back?b.route.First():b.route.Last(),q.back?b.start:b.end,!q.back))throw new Exception("Captured replay endpoint mismatch "+i);}
            // Same tolerance as the existing lane-tree research checks, not a state-equivalence assertion.
            double delta=reachable?b.seconds-a.seconds:0;if(Math.Abs(delta)>1e-9*Math.Max(1,Math.Abs(a.seconds)))throw new Exception("Captured replay shortest cost mismatch "+i);
            bool sameRoute=!reachable||a.route.SequenceEqual(b.route),sameStart=a.start==b.start,sameEnd=a.end==b.end;
            if(!sameRoute||!sameStart||!sameEnd)routesChanged++;if(reachable&&(a.route.First()!=b.route.First()||a.route.Last()!=b.route.Last()||!sameStart||!sameEnd))curbsChanged++;maxDelta=Math.Max(maxDelta,Math.Abs(delta));
            costs.Add(string.Join(",",i,q.depot,q.back,a.seconds.ToString("R"),b.seconds.ToString("R"),delta.ToString("R"),sameRoute,sameStart,sameEnd,reachable?a.route.First():0,reachable?b.route.First():0));
        }
        bytes=GC.GetAllocatedBytesForCurrentThread();timer.Restart();
        for(int i=0;i<queries.Count;i++){var q=queries[i];if(!cache.TryFind(world,q.depot,q.a,q.p,q.back,out var route,out double start,out double end))throw new Exception("Hot replay rejected input");var old=coldPaths[i];if((route==null)!=(old.route==null)||route!=null&&!route.SequenceEqual(old.route)||start!=old.start||end!=old.end)throw new Exception("Cold/hot static route differs "+i);}
        timer.Stop();double hotMs=timer.Elapsed.TotalMilliseconds;long hotBytes=GC.GetAllocatedBytesForCurrentThread()-bytes;
        if(before!=Hash(CityCoreSave.Write(city)))throw new Exception("Read-only replay modified city facts");
        File.WriteAllLines(Path.Combine(output,"captured-choices.csv"),costs);
        File.WriteAllText(Path.Combine(output,"captured-summary.csv"),"queries,depots,point_replay_ms,tree_cold_ms,tree_hot_ms,cold_tree_builds,hot_tree_builds,entries,evictions,logical_tree_bytes,logical_graph_bytes,cold_allocated_bytes,hot_allocated_bytes,route_differences,curb_differences,max_cost_delta_seconds,city_facts_unchanged,note\n"+string.Join(",",queries.Count,queries.Select(q=>q.depot).Distinct().Count(),baselineMs,coldMs,hotMs,coldBuilds,cache.TreeBuilds-coldBuilds,cache.Count,cache.Evictions,cache.ReservedTreeBytes,cache.ReservedGraphBytes,coldBytes,hotBytes,routesChanged,curbsChanged,maxDelta,true,"isolated dotnet static requests; no traffic admission or Unity FPS; no production live-set policy changed"));
        Console.WriteLine(File.ReadAllText(Path.Combine(output,"captured-summary.csv")));
    }
}
