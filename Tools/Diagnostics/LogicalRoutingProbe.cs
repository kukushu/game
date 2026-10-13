using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using HarborCity;
public static class LogicalRoutingProbe
{
    public static void Main(string[] args)
    {
        var json=new JavaScriptSerializer{MaxJsonLength=int.MaxValue};var city=json.Deserialize<CitySaveData>(File.ReadAllText(args[0])).ToCity();
        var corridor=new CityRoads{nextStroke=2};
        for(int i=0;i<600;i++)corridor.nodes.Add(new RoadNode{id=i==0?CityRoads.Entrance:corridor.nextNode++,x=-150+i*.5f,z=(float)Math.Sin(i*.1)*10,y=1});
        for(int i=1;i<600;i++)corridor.edges.Add(new RoadEdge{id=corridor.nextEdge++,a=corridor.nodes[i-1].id,b=corridor.nodes[i].id,stroke=1});
        corridor.Changed();if(!corridor.Valid())throw new Exception("Corridor invalid");
        var output=args[1];Directory.CreateDirectory(output);var signatures=new List<string>();var rows=new List<string>{"fixture,queries,mean_ms,p95_ms,p99_ms,mean_gc_bytes,logical_fallbacks,nodes,edges,roads"};
        foreach(var fixture in new[]{("fixed_city",city.roads),("600_sample_corridor",corridor)})
        {
            var roads=fixture.Item2;var topology=roads.GetLogicalTopology();var nodes=roads.nodes.Where(n=>roads.Active(n.id)).Select(n=>n.id).ToArray();var random=new Random(371);
            var pairs=new List<(List<int> from,List<int> to)>();for(int i=0;i<256;i++)pairs.Add((new List<int>{nodes[random.Next(nodes.Length)]},new List<int>{nodes[random.Next(nodes.Length)]}));
            var route=(Func<List<int>,List<int>,bool,List<int>>)Delegate.CreateDelegate(typeof(Func<List<int>,List<int>,bool,List<int>>),roads,typeof(CityRoads).GetMethod("CalculatePath",BindingFlags.Instance|BindingFlags.NonPublic));
            var logical=(Func<CityRoads,List<int>,List<int>,bool,List<int>>)Delegate.CreateDelegate(typeof(Func<CityRoads,List<int>,List<int>,bool,List<int>>),typeof(CityRoads).Assembly.GetType("HarborCity.CityLogicalRoadRouter").GetMethod("Search",BindingFlags.Static|BindingFlags.NonPublic));
            for(int i=0;i<30;i++)route(pairs[i].from,pairs[i].to,false);
            var times=new List<double>();var allocations=new List<long>();var timer=new Stopwatch();int fallbacks=0;
            for(int i=0;i<pairs.Count;i++)
            {
                long bytes=GC.GetAllocatedBytesForCurrentThread();timer.Restart();var path=route(pairs[i].from,pairs[i].to,false);timer.Stop();long allocated=GC.GetAllocatedBytesForCurrentThread()-bytes;
                times.Add(timer.Elapsed.TotalMilliseconds);allocations.Add(allocated);
                if(logical(roads,pairs[i].from,pairs[i].to,false)==null)fallbacks++;
                using(var sha=SHA256.Create())signatures.Add(fixture.Item1+","+i+","+BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(string.Join(",",path)))).Replace("-",""));
            }
            times.Sort();string N(double v)=>v.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
            rows.Add(fixture.Item1+","+pairs.Count+","+N(times.Average())+","+N(times[243])+","+N(times[253])+","+N(allocations.Average())+","+fallbacks+","+roads.nodes.Count+","+roads.edges.Count+","+topology.Roads.Count);
        }
        File.WriteAllLines(Path.Combine(output,"routes.csv"),rows);File.WriteAllLines(Path.Combine(output,"signatures.csv"),signatures);
        File.WriteAllText(Path.Combine(output,"configuration.txt"),"Pure Mono cold route algorithm diagnostic, no singlePaths cache, snapshot prewarmed; 30 warmup, 256 measured same seeded endpoint pairs. Timing excludes endpoint generation, signatures and fallback census; GC excludes timer construction. Corridor is a synthetic graph, not a city FPS forecast.");
        Console.WriteLine(string.Join("\n",rows));
    }
}
