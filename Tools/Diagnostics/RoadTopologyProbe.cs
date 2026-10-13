using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using HarborCity;
public static class RoadTopologyProbe
{
    static double sink;
    static string N(double v)=>v.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
    public static void Main(string[] args)
    {
        var json=new JavaScriptSerializer{MaxJsonLength=int.MaxValue};var source=File.ReadAllText(args[0]);var city=json.Deserialize<CitySaveData>(source).ToCity();
        string output=args[1];Directory.CreateDirectory(output);string before=json.Serialize(city.ToSaveData());
        long allocationBefore=GC.GetAllocatedBytesForCurrentThread();var allocationControl=new byte[4096];GC.KeepAlive(allocationControl);bool supported=GC.GetAllocatedBytesForCurrentThread()>allocationBefore;
        for(int i=0;i<5;i++)city.roads.Copy().GetLogicalTopology();
        var milliseconds=new List<double>();var allocations=new List<long>();CityRoadTopology topology=null;
        for(int i=0;i<40;i++)
        {
            var graph=city.roads.Copy();long bytes=GC.GetAllocatedBytesForCurrentThread();var watch=Stopwatch.StartNew();topology=graph.GetLogicalTopology();watch.Stop();
            milliseconds.Add(watch.Elapsed.TotalMilliseconds);allocations.Add(supported?GC.GetAllocatedBytesForCurrentThread()-bytes:-1);sink+=topology.Roads.Count;
        }
        var memoryGraph=city.roads.Copy();GC.Collect();long memoryBefore=GC.GetTotalMemory(true);var retained=memoryGraph.GetLogicalTopology();long memoryAfter=GC.GetTotalMemory(true);GC.KeepAlive(memoryGraph);GC.KeepAlive(retained);
        var live=city.roads.GetLogicalTopology();var edge=city.roads.edges[0];int count=200000;var timer=new Stopwatch();long queryBytes=GC.GetAllocatedBytesForCurrentThread();timer.Start();
        for(int i=0;i<count;i++){if(!ReferenceEquals(live,city.roads.GetLogicalTopology()) || !live.TryMapLegacy(edge.a,edge.b,.37f,out var p) || !live.TryToLegacy(p,out int a,out int b,out float progress) || a!=edge.a || b!=edge.b || Math.Abs(progress-.37f)>.000001f)throw new Exception("Hot mapping failed");sink+=p.Distance;}
        timer.Stop();queryBytes=supported?GC.GetAllocatedBytesForCurrentThread()-queryBytes:-1;
        milliseconds.Sort();File.WriteAllText(Path.Combine(output,"topology.csv"),"legacy_nodes,legacy_edges,logical_roads,mean_build_ms,p95_build_ms,mean_build_gc_bytes,retained_snapshot_bytes_approx,hot_queries,hot_total_ms,hot_gc_bytes\n"+city.roads.nodes.Count+","+city.roads.edges.Count+","+live.Roads.Count+","+N(milliseconds.Average())+","+N(milliseconds[37])+","+N(allocations.Average())+","+(memoryAfter-memoryBefore)+","+count+","+N(timer.Elapsed.TotalMilliseconds)+","+queryBytes+"\n");
        var preview=new List<string>{"kind,cache_warmed,repeat,ms,gc_bytes,valid,cost,edges,splits"};var plans=new Dictionary<(bool,int),string>();
        var signatures=new List<string>();
        for(int repeat=0;repeat<12;repeat++)foreach(bool warmed in new[]{false,true})foreach(bool curve in new[]{false,true})
        {
            var c=json.Deserialize<CitySaveData>(source).ToCity();c.money=10000000;if(warmed)c.roads.GetLogicalTopology();
            string original=json.Serialize(c.ToSaveData());
            var a=new RoadNode{x=-130,z=curve?-115:-130,y=1};var b=new RoadNode{x=curve?-90:-100,z=a.z,y=1};
            long start=GC.GetAllocatedBytesForCurrentThread();timer.Restart();var plan=curve?c.roads.PlanCurve(c,a,new RoadNode{x=-110,z=-95,y=1},b,(x,z)=>1):c.roads.Plan(c,a,b,(x,z)=>1);timer.Stop();
            if(!plan.Valid)throw new Exception("Preview fixture invalid: "+plan.error);
            preview.Add((curve?"curve":"straight")+","+(warmed?1:0)+","+repeat+","+N(timer.Elapsed.TotalMilliseconds)+","+(supported?GC.GetAllocatedBytesForCurrentThread()-start:-1)+",1,"+plan.cost+","+plan.network.edges.Count+","+plan.splits.Count);
            // Compare the full constructed graph outside timed/allocation regions.
            if(original!=json.Serialize(c.ToSaveData()))throw new Exception("Preview mutated the live city");
            var key=(curve,repeat);string graphJson=json.Serialize(plan.network);
            string complete=json.Serialize(new{plan.length,plan.grade,plan.cost,plan.stroke,plan.revision,plan.error,plan.points,plan.splits,plan.network});
            using(var sha=System.Security.Cryptography.SHA256.Create())signatures.Add((curve?"curve":"straight")+","+warmed+","+repeat+","+BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(complete))).Replace("-",""));
            if(!warmed)plans[key]=graphJson;else if(plans[key]!=graphJson)throw new Exception("Warmed derived cache changed construction result");
            sink+=plan.network.edges.Count;
        }
        File.WriteAllLines(Path.Combine(output,"previews.csv"),preview);
        File.WriteAllLines(Path.Combine(output,"plan-signatures.csv"),signatures);
        if(before!=json.Serialize(city.ToSaveData()))throw new Exception("Deriving snapshot changed saved city");
        File.WriteAllText(Path.Combine(output,"configuration.txt"),"Pure Mono algorithm/construction diagnostic; not Unity frame/FPS or native GC.\nsource="+args[0]+"\nallocation_control_passed="+supported+"\nJIT_warmup=5\nbuild_repeats=40\npreview_repeats=12 paired (fresh identical city each)\nMemory uses GC.GetTotalMemory delta, approximate; not a native heap capture.\n");
        File.WriteAllText(Path.Combine(output,"PASS.txt"),"Read-only conversion, hot roundtrips and paired valid construction previews; city serialization unchanged.");Console.WriteLine(File.ReadAllText(Path.Combine(output,"topology.csv")));GC.KeepAlive(sink);
    }
}
