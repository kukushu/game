using System;
using System.IO;
using System.Diagnostics;
using System.Web.Script.Serialization;
using HarborCity;

// Read-only diagnostic: load a detached city, never write to the supplied save.
public static class PerformanceProbe
{
    static readonly Func<long> Allocated=(Func<long>)Delegate.CreateDelegate(typeof(Func<long>),typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread"));
    static void Measure(string name,int count,Action action)
    {
        action(); // Warm JIT/derived caches before recording steady work.
        long bytes=Allocated(),start=Stopwatch.GetTimestamp();double maximum=0;
        for(int i=0;i<count;i++)
        {
            long tick=Stopwatch.GetTimestamp();action();
            maximum=Math.Max(maximum,(Stopwatch.GetTimestamp()-tick)*1000.0/Stopwatch.Frequency);
        }
        double average=(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency/count;
        Console.WriteLine(name+": average="+average.ToString("F3")+"ms max="+maximum.ToString("F3")+"ms allocated="+((Allocated()-bytes)/count)+" bytes/call");
    }
    public static void Main(string[] args)
    {
        if(args.Length<1)throw new ArgumentException("Supply a city save path; optional second path receives the detached final state.");
        var json=new JavaScriptSerializer{MaxJsonLength=int.MaxValue};
        var city=json.Deserialize<CitySaveData>(File.ReadAllText(args[0])).ToCity();
        if(city==null || !city.Valid())throw new InvalidOperationException("Invalid city save");
        Console.WriteLine("Loaded detached save: day="+city.day+", buildings="+city.buildings.Count+", roads="+city.roads.edges.Count+", trips="+city.traffic.trips.Count);
        var traffic=new CityTraffic(city);city.analysis=new CityDailyAnalysis(city);
        Measure("Analysis snapshot",20,()=>city.analysis.Refresh(true));
        Measure("Building problems",20,()=>city.BuildingProblems());
        Measure("Vehicle geometry, position and heading",120,()=>
        {
            foreach(var t in city.traffic.trips)
            {
                CityVehicleGeometry.Position(city.roads,t,t.segment,t.progress);
                CityVehicleGeometry.Position(city.roads,t,t.segment,Math.Min(1,t.progress+.02f));
            }
        });
        Measure("Traffic fixed step (50ms simulated)",240,()=>traffic.Advance(.05f));
        if(!city.Valid())throw new InvalidOperationException("Simulation invalid after measurement");
        if(args.Length>1)
        {
            if(Path.GetFullPath(args[0])==Path.GetFullPath(args[1]))throw new ArgumentException("Output must not overwrite source save");
            File.WriteAllText(args[1],json.Serialize(city.ToSaveData()));
        }
        Console.WriteLine("PASS: detached simulation remains valid. This excludes Unity rendering and editor overhead; it is not an FPS measurement.");
    }
}
