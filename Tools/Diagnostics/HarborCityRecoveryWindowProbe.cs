using System;using System.IO;using System.Linq;using System.Collections;using System.Collections.Generic;using System.Diagnostics;using System.Globalization;using UnityEngine;using Unity.Profiling;using HarborCity.RoadCore;
namespace HarborCity {
 // Copied only into an isolated test Player; never part of the shipped host.
 public static class RecoveryCounters {
  public static bool Enabled;static readonly Dictionary<string,(int calls,double ms,long bytes)> rows=new Dictionary<string,(int,double,long)>();
  public static readonly bool Allocations=Probe();static bool Probe(){long a=GC.GetAllocatedBytesForCurrentThread();GC.KeepAlive(new byte[128]);return GC.GetAllocatedBytesForCurrentThread()>a;}
  sealed class Scope:IDisposable {readonly string key;readonly long started,bytes;public Scope(string key){this.key=key;started=Stopwatch.GetTimestamp();bytes=Allocations?GC.GetAllocatedBytesForCurrentThread():0;}public void Dispose(){double ms=(Stopwatch.GetTimestamp()-started)*1000.0/Stopwatch.Frequency;long b=Allocations?GC.GetAllocatedBytesForCurrentThread()-bytes:0;rows.TryGetValue(key,out var r);rows[key]=(r.calls+1,r.ms+ms,r.bytes+b);}}
  public static IDisposable Measure(string key)=>Enabled?new Scope(key):null;
  public static void Write(string path){File.WriteAllLines(path,new[]{"name,calls,inclusive_ms,allocated_bytes"}.Concat(rows.Select(p=>string.Join(",",p.Key,p.Value.calls,p.Value.ms.ToString("R",CultureInfo.InvariantCulture),Allocations?p.Value.bytes:-1))));}
 }
 public sealed class HarborCityRecoveryWindowProbe:MonoBehaviour {
  HarborCityRoadCoreGame host;string input,output;public void Configure(HarborCityRoadCoreGame h,string path,string result,float rate){host=h;input=path;output=result;host.ScenarioMeasuring=true;StartCoroutine(Run());}
  IEnumerator Run(){Directory.CreateDirectory(output);QualitySettings.vSyncCount=0;Application.targetFrameRate=60;host.ScenarioRate(0);yield return null;double warm=Time.realtimeSinceStartupAsDouble;while(Time.realtimeSinceStartupAsDouble-warm<8)yield return null;
   var s=host.ScenarioSession;var w=s.Transport;w.DepotLaneRoutingEnabled=true;File.WriteAllBytes(Path.Combine(output,"before.hcity"),CityCoreSave.Write(s));
   int road=87;var args=Environment.GetCommandLineArgs();int ix=Array.IndexOf(args,"-harbor-recovery-road");if(ix>=0)road=int.Parse(args[ix+1]);
   // Measured frame contains only production commands + their view refresh.
   // Validation and serialization are postponed until all frame samples finish.
   var frames=new List<string>{"frame_ms,gc_bytes"};using(var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame")){
    yield return null;double last=Time.realtimeSinceStartupAsDouble;var timer=Stopwatch.StartNew();s.Construction.RemoveRoad(road);timer.Stop();double removeMs=timer.Elapsed.TotalMilliseconds;
    var frozen=w.Vehicles.Where(v=>v.status==VehicleStatus.Stranded).Select(v=>v.id).ToArray();var positions=w.Vehicles.ToDictionary(v=>v.id,v=>w.Position(v));timer.Restart();host.ScenarioRefreshRoads();timer.Stop();double viewMs=timer.Elapsed.TotalMilliseconds;
    RecoveryCounters.Enabled=true;timer.Restart();new CityCoreRoadBindings(s.City,w).RefreshIfChanged();timer.Stop();RecoveryCounters.Enabled=false;double recoveryMs=timer.Elapsed.TotalMilliseconds;
    var resumed=frozen.Where(id=>w.FindVehicle(id).status==VehicleStatus.Active).ToArray();
    for(int i=0;i<12;i++){yield return null;double now=Time.realtimeSinceStartupAsDouble;frames.Add(((now-last)*1000).ToString("R",CultureInfo.InvariantCulture)+","+(gc.Valid?gc.LastValue:-1));last=now;}
    File.WriteAllLines(Path.Combine(output,"frames.csv"),frames);File.WriteAllText(Path.Combine(output,"summary.csv"),"tick,road,attempted,resumed,remaining,delete_ms,views_ms,recovery_ms\n"+string.Join(",",s.Simulation.Tick,road,frozen.Length,resumed.Length,frozen.Length-resumed.Length,removeMs.ToString("R",CultureInfo.InvariantCulture),viewMs.ToString("R",CultureInfo.InvariantCulture),recoveryMs.ToString("R",CultureInfo.InvariantCulture)));File.WriteAllLines(Path.Combine(output,"resumed.csv"),new[]{"vehicle"}.Concat(resumed.Select(id=>id.ToString())));RecoveryCounters.Write(Path.Combine(output,"scopes.csv"));
    foreach(int id in resumed)if(Point.Distance(positions[id],w.Position(w.FindVehicle(id)))>1e-7)throw new Exception("Recovery changed position");
   }
   s.Validate();var checkpoint=CityCoreSave.Write(s);File.WriteAllBytes(Path.Combine(output,"recovered.hcity"),checkpoint);var cold=CityCoreSave.Read(checkpoint);cold.Simulation.Growth.Zoning.SetSurface(host.ScenarioSurface);cold.Transport.DepotLaneRoutingEnabled=true;
   for(int i=0;i<80;i++){s.Simulation.Advance(.05,1,double.PositiveInfinity);cold.Simulation.Advance(.05,1,double.PositiveInfinity);}s.Validate();cold.Validate();if(!CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(cold)))throw new Exception("Cold recovery continuation differs");File.WriteAllBytes(Path.Combine(output,"after80.hcity"),CityCoreSave.Write(s));File.WriteAllText(Path.Combine(output,"Result.txt"),"PASS native exact edit tick, production demolition/bindings/views; positions and 80 fixed-step warm/cold full state identical. Scripted paused edit window, not manual mouse acceptance or normal-game FPS.\n");Application.Quit(0);
  }
 }
}
