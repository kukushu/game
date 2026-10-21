using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using HarborCity;
using HarborCity.RoadCore;

// Reuses frozen real query inputs from the independent lane experiment, not its
// implementation. All route legality is checked by authoritative Network rules.
static class CityDepotJunctionExperiment
{
    sealed class Query {internal int depot;internal bool back;internal RoadAccess anchor,point;}
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static string Hash(byte[] b){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(b)).Replace("-","");}
    static CityCoreIndustrySession Load(byte[] b){var city=CityCoreSave.Read(b);city.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);return city;}
    static void Step(CityCoreIndustrySession s){Check(s.Simulation.Advance(.05,1,double.PositiveInfinity)==1,"Lost fixed step");}
    static string Business(CityCoreIndustrySession s){var w=s.Simulation.Waste;return string.Join(",",s.City.population,s.City.money,s.Transport.ActiveCount,w.Vehicles,w.Generated,w.Collected,w.InTransit,w.Stored,w.Processed,w.BalanceError);}
    static double Length(Network n,List<long> r,double start,double end){double sum=0;for(int i=0;i<r.Count;i++){var d=n.Get(r[i]);sum+=(i==r.Count-1?end:d.EndDistance)-(i==0?start:d.StartDistance);}return sum;}
    static void Legal(Network n,Query q,List<long> r,double start,double end)
    {
        if(r==null)return;n.ValidateRoute(r);
        Check(q.back?r[0]==q.point.lane&&start==q.point.distance:AccessRouting.Matches(n,q.anchor,r[0],start,true),"Wrong route start");
        Check(AccessRouting.Matches(n,q.back?q.anchor:q.point,r[r.Count-1],end,true),"Wrong route end");
        var a=n.Get(r[0]);var b=n.Get(r[r.Count-1]);Check(start>=a.StartDistance&&start<=a.EndDistance&&end>=b.StartDistance&&end<=b.EndDistance,"Bad partial distance");
        Check(r.Count>1||start<=end,"Reversed same-lane path");
        // A quotient tree may revisit a junction through a different turn state,
        // but cannot cycle through the same state. One endpoint exception is
        // necessary even in original Dijkstra: target behind source on the SAME
        // directed lane requires a legal circuit back to that lane. Check it
        // explicitly, rather than accepting arbitrary repeated interior lanes.
        var lanes=r.Where(x=>x>0).ToArray();int unique=lanes.Distinct().Count();
        Check(unique==lanes.Length||(unique==lanes.Length-1&&lanes[0]==lanes[lanes.Length-1]&&start>end&&lanes.Take(lanes.Length-1).Distinct().Count()==lanes.Length-1),"Repeated internal directed lane / gratuitous cycle depot="+q.depot+" back="+q.back+" start="+start+" end="+end+" path="+string.Join(";",r));
    }
    static List<long> Original(Network n,Query q,out double start,out double end)
    {if(q.back){start=q.point.distance;return AccessRouting.FromLane(n,q.point.lane,q.point.distance,q.anchor,true,out end);}return AccessRouting.Find(n,q.anchor,q.point,true,out start,out end);}
    static List<long> Cached(Network n,DepotJunctionRouting cache,Query q,out double start,out double end)
    {bool ok;if(q.back){start=q.point.distance;ok=cache.Return(n,q.depot,q.anchor,q.point.lane,q.point.distance,out var r,out end);Check(ok,"Unexpected fallback");return r;}ok=cache.Outbound(n,q.depot,q.anchor,q.point,out var path,out start,out end);Check(ok,"Unexpected fallback");return path;}
    static List<Query> ReadQueries(string path)=>File.ReadLines(path).Skip(1).Select(line=>{var x=line.Split(',');return new Query{depot=int.Parse(x[1]),back=bool.Parse(x[2]),anchor=new RoadAccess{endpoint=int.Parse(x[1]),lane=long.Parse(x[3]),distance=double.Parse(x[4],CultureInfo.InvariantCulture)},point=new RoadAccess{endpoint=int.Parse(x[5]),lane=long.Parse(x[6]),distance=double.Parse(x[7],CultureInfo.InvariantCulture)}};}).ToList();
    static void Replay(string prior,string dir)
    {
        var s=Load(File.ReadAllBytes(Path.Combine(prior,"baseline.hcity")));var n=s.Transport.Network;var queries=ReadQueries(Path.Combine(prior,"baseline-service-sequence.csv"));
        var cache=new DepotJunctionRouting{Trace=true};var summary=new List<string>{"mode,queries,ms,gc_bytes,dijkstra_calls,expanded,nodes,lanes,entries,tree_bytes,graph_payload_estimate,hits,misses,builds,local_checks,fallbacks,evictions,heap_delta"};
        var ratios=new List<string>{"ordinal,depot,returning,old_length,new_length,length_ratio,old_seconds,new_seconds,time_ratio,old_segments,new_segments"};
        var paths=new List<List<long>>();var starts=new List<double>();var ends=new List<double>();
        DepotJunctionRouting.CountSearches=true;
        for(int mode=0;mode<4;mode++)
        {
            if(mode==3){int a=s.Transport.AddJunction(new Point(3000,2,3000)),b=s.Transport.AddJunction(new Point(3025,2,3000));s.Transport.AddRoad(a,b,new[]{s.Transport.JunctionPosition(a),s.Transport.JunctionPosition(b)});n=s.Transport.Network;}
            GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();long heap=GC.GetTotalMemory(true),bytes=GC.GetAllocatedBytesForCurrentThread();
            DepotJunctionRouting.SearchCalls=DepotJunctionRouting.SearchExpanded=0;var watch=Stopwatch.StartNew();
            var results=new List<List<long>>();var ss=new List<double>();var ee=new List<double>();
            foreach(var q in queries){double start,end;var r=mode==0?Original(n,q,out start,out end):Cached(n,cache,q,out start,out end);results.Add(r);ss.Add(start);ee.Add(end);}
            watch.Stop();long allocated=GC.GetAllocatedBytesForCurrentThread()-bytes;long calls=DepotJunctionRouting.SearchCalls,expanded=DepotJunctionRouting.SearchExpanded;
            summary.Add(string.Join(",",new[]{"original","junction-cold","junction-hot","junction-after-edit"}[mode],queries.Count,watch.Elapsed.TotalMilliseconds,allocated,calls,expanded,cache.Nodes,cache.Lanes,cache.Count,cache.TreeBytes,cache.GraphPayloadEstimate,cache.Hits,cache.Misses,cache.Builds,cache.LocalChecks,cache.Fallbacks,cache.Evictions,GC.GetTotalMemory(true)-heap));
            for(int i=0;i<queries.Count;i++)
            {
                var r=results[i];Legal(n,queries[i],r,ss[i],ee[i]);
                if(mode==0){paths.Add(r);starts.Add(ss[i]);ends.Add(ee[i]);continue;}
                Check((paths[i]==null)==(r==null),"Replay reachability differs");
                if(mode==1&&r!=null){double ol=Length(n,paths[i],starts[i],ends[i]),nl=Length(n,r,ss[i],ee[i]),ot=new DispatchBlueprint(n,paths[i],starts[i],ends[i]).seconds,nt=new DispatchBlueprint(n,r,ss[i],ee[i]).seconds;ratios.Add(string.Join(",",i,queries[i].depot,queries[i].back,ol,nl,ol==0?1:nl/ol,ot,nt,ot==0?1:nt/ot,paths[i].Count,r.Count));}
            }
            File.WriteAllText(Path.Combine(dir,"replay-"+mode+"-trace.csv"),cache.TraceCsv());cache.Samples.Clear();
        }
        File.WriteAllLines(Path.Combine(dir,"replay-summary.csv"),summary);File.WriteAllLines(Path.Combine(dir,"route-ratios.csv"),ratios);
        var stress=new DepotJunctionRouting{Trace=true};var bases=s.City.buildings.OfType<LandfillBuilding>().Select(x=>s.Transport.GetAccess(x.id)).Where(x=>x!=null&&x.valid).ToArray();
        var target=queries.First(x=>!x.back).point;var stressRows=new List<string>{"pass,depots,ms,bytes,entries,tree_bytes,builds,hits,misses,evictions,fallbacks"};
        for(int pass=0;pass<2;pass++){long a=GC.GetAllocatedBytesForCurrentThread();var clock=Stopwatch.StartNew();foreach(var depot in bases){var q=new Query{anchor=depot,point=target,depot=depot.endpoint};Cached(n,stress,q,out _,out _);q.back=true;Cached(n,stress,q,out _,out _);}clock.Stop();stressRows.Add(string.Join(",",pass,bases.Length,clock.Elapsed.TotalMilliseconds,GC.GetAllocatedBytesForCurrentThread()-a,stress.Count,stress.TreeBytes,stress.Builds,stress.Hits,stress.Misses,stress.Evictions,stress.Fallbacks));}
        File.WriteAllLines(Path.Combine(dir,"all-depots.csv"),stressRows);File.WriteAllText(Path.Combine(dir,"all-depots-trace.csv"),stress.TraceCsv());
        DepotJunctionRouting.CountSearches=false;Console.WriteLine("PASS exact query replay "+queries.Count+"; quotient "+cache.Nodes+" / "+cache.Lanes+" lanes; depots="+bases.Length);
    }
    static void Simulate(string prior,string dir,int steps)
    {
        byte[] input=File.ReadAllBytes(Path.Combine(prior,"warm.hcity"));var rows=new List<string>{"mode,steps,ms,gc_bytes,dijkstra_calls,search_nodes,population,money,active,waste_vehicles,generated,collected,in_transit,stored,processed,balance_error,sha256"};byte[] expected=null;
        foreach(bool enabled in new[]{false,true})
        {
            var s=Load(input);s.Transport.DepotJunctionRoutingEnabled=enabled;DepotJunctionRouting.CountSearches=true;DepotJunctionRouting.SearchCalls=DepotJunctionRouting.SearchExpanded=0;
            long bytes=GC.GetAllocatedBytesForCurrentThread();var watch=Stopwatch.StartNew();for(int i=0;i<steps;i++)Step(s);watch.Stop();long allocated=GC.GetAllocatedBytesForCurrentThread()-bytes;
            s.Validate();byte[] state=CityCoreSave.Write(s);File.WriteAllBytes(Path.Combine(dir,enabled?"junction.hcity":"original.hcity"),state);
            rows.Add(string.Join(",",enabled?"junction":"original",steps,watch.Elapsed.TotalMilliseconds,allocated,DepotJunctionRouting.SearchCalls,DepotJunctionRouting.SearchExpanded,Business(s),Hash(state)));
            if(enabled)expected=state;
        }
        DepotJunctionRouting.CountSearches=false;File.WriteAllLines(Path.Combine(dir,"simulation.csv"),rows);
        // Safety and serialization assertions deliberately outside throughput sample.
        var checkedCity=Load(input);checkedCity.Transport.DepotJunctionRoutingEnabled=true;
        for(int i=0;i<steps;i++){Step(checkedCity);checkedCity.Transport.AssertSafety();Check(checkedCity.Simulation.Waste.BalanceError==0,"Waste imbalance");if(i%200==199)checkedCity.Validate();}
        Check(expected.SequenceEqual(CityCoreSave.Write(checkedCity)),"Observed replay changed state");
        var cold=Load(expected);cold.Transport.DepotJunctionRoutingEnabled=true;
        for(int i=0;i<200;i++){Step(checkedCity);Step(cold);checkedCity.Transport.AssertSafety();cold.Transport.AssertSafety();Check(checkedCity.Simulation.Waste.BalanceError==0&&cold.Simulation.Waste.BalanceError==0,"Restore waste imbalance");}
        checkedCity.Validate();cold.Validate();Check(CityCoreSave.Write(checkedCity).SequenceEqual(CityCoreSave.Write(cold)),"Warm/cold continuation differs");
        File.WriteAllText(Path.Combine(dir,"correctness.txt"),"PASS "+steps+" per-step safety/conservation + periodic Validate; observed/unobserved final state equal; 200 hot/cold complete steps byte-identical. Different routes versus original are permitted, not asserted equal.\n");
    }
    static void Main(string[] args)
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
        Check(!Directory.Exists(args[1]),"Choose fresh results");Directory.CreateDirectory(args[1]);
        Replay(args[0],args[1]);Simulate(args[0],args[1],args.Length>2?int.Parse(args[2]):4000);
        Console.WriteLine("PASS real-city simulation, per-step safety/waste conservation and cold continuation");
    }
}
