using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using HarborCity;
public static class RoadKeyDistribution
{
    public static void Main(string[] args)
    {
        var city=new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Deserialize<CitySaveData>(File.ReadAllText(args[0])).ToCity();
        var keys=new HashSet<long>();foreach(var e in city.roads.edges){keys.Add(((long)e.a<<32)|(uint)e.b);keys.Add(((long)e.b<<32)|(uint)e.a);}
        Report("fixed-city",keys);
        foreach(int n in new[]{1000,10000})
        {keys.Clear();for(int a=1;a<=n;a++){keys.Add(((long)a<<32)|(uint)(a+1));keys.Add(((long)(a+1)<<32)|(uint)a);}Report("consecutive-"+n,keys);}
    }
    static void Report(string label,HashSet<long> keys)
    {
        foreach(bool mixed in new[]{false,true})
        {
            var groups=keys.GroupBy(k=>(mixed?CityRoadKeyComparer.Instance.GetHashCode(k):k.GetHashCode())&int.MaxValue).Select(g=>g.Count()).ToArray();
            // Equality probes from identical hashes alone; modulo-bucket collisions can only add work.
            double comparisons=groups.Sum(n=>n*(n+1.0)/2)/keys.Count;
            Console.WriteLine(label+","+(mixed?"mixed":"default")+",keys="+keys.Count+",distinct_hashes="+groups.Length+",max_same_hash="+groups.Max()+",mean_same_hash_success_probes="+comparisons.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
