using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using HarborCity;
using HarborCity.RoadCore;
static class RebindMeters
{
    internal static bool Enabled;internal static string Phase;internal static int Run;
    internal sealed class Row{public int run,calls;public string phase,name;public double ms;public long bytes;}
    internal static readonly Dictionary<string,Row> Rows=new Dictionary<string,Row>();
    sealed class Scope:IDisposable
    {
        readonly string key;readonly long start,bytes;
        internal Scope(string name){key=Run+"/"+Phase+"/"+name;if(!Rows.ContainsKey(key))Rows.Add(key,new Row{run=Run,phase=Phase,name=name});start=Stopwatch.GetTimestamp();bytes=GC.GetAllocatedBytesForCurrentThread();}
        public void Dispose(){long allocation=GC.GetAllocatedBytesForCurrentThread()-bytes;var r=Rows[key];r.calls++;r.ms+=(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;r.bytes+=allocation;}
    }
    internal static IDisposable Measure(string name)=>Enabled?new Scope(name):null;
}
static class CityAccessRebindMeasure
{
    static void Main(string[] args)
    {
        string output=args[1];Directory.CreateDirectory(output);var summary=new List<string>{"run,phase,ms,gc_bytes"};
        for(int run=-1;run<3;run++)
        {
            var s=CityCoreSave.Read(File.ReadAllBytes(args[0]));s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);s.Transport.DepotLaneRoutingEnabled=true;
            RebindMeters.Run=run;RebindMeters.Enabled=run>=0;RebindMeters.Phase="delete";long memory=GC.GetAllocatedBytesForCurrentThread();var timer=Stopwatch.StartNew();new RoadEditHistory(s.Transport).Execute(()=>s.Construction.RemoveRoad(87));timer.Stop();if(run>=0)summary.Add(run+",delete,"+timer.Elapsed.TotalMilliseconds.ToString("R",CultureInfo.InvariantCulture)+","+(GC.GetAllocatedBytesForCurrentThread()-memory));
            RebindMeters.Phase="bindings";memory=GC.GetAllocatedBytesForCurrentThread();timer.Restart();new CityCoreRoadBindings(s.City,s.Transport).RefreshIfChanged();timer.Stop();if(run>=0)summary.Add(run+",bindings,"+timer.Elapsed.TotalMilliseconds.ToString("R",CultureInfo.InvariantCulture)+","+(GC.GetAllocatedBytesForCurrentThread()-memory));RebindMeters.Enabled=false;
            s.Validate();if(run>=0)File.WriteAllBytes(Path.Combine(output,"run"+run+"-boundary.hcity"),CityCoreSave.Write(s));
            for(int i=0;i<80;i++)s.Simulation.Advance(.05,1,double.PositiveInfinity);s.Validate();if(run>=0)File.WriteAllBytes(Path.Combine(output,"run"+run+"-after80.hcity"),CityCoreSave.Write(s));
        }
        File.WriteAllLines(Path.Combine(output,"summary.csv"),summary);File.WriteAllLines(Path.Combine(output,"scopes.csv"),new[]{"run,phase,name,calls,inclusive_ms,gc_bytes"}.Concat(RebindMeters.Rows.Values.Select(r=>string.Join(",",r.run,r.phase,r.name,r.calls,r.ms.ToString("R",CultureInfo.InvariantCulture),r.bytes))));Console.WriteLine("PASS exact input x3, full validation and 80-step continuation; warmup/validation/save excluded from timings");
    }
}
