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
        GC.KeepAlive(CityCoreProfile.AllocationAvailable);
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
}
