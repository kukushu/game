using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using UnityEngine;
using Unity.Profiling;
using HarborCity.RoadCore;
namespace HarborCity
{
    // Diagnostic-only clock and profiler labels, injected only into Temp copies.
    public static class DemolitionFrameCounters
    {
        public static bool Enabled;
        static readonly Dictionary<string,ProfilerMarker> markers=new Dictionary<string,ProfilerMarker>();
        static readonly Stack<Scope> stack=new Stack<Scope>();
        sealed class Row {public int frame,calls;public string path;public double total,self;public long bytes;}
        static readonly Dictionary<(int,string),Row> rows=new Dictionary<(int,string),Row>();
        static readonly Dictionary<string,long> counts=new Dictionary<string,long>();
        static readonly bool allocations=Probe();static bool Probe(){long a=GC.GetAllocatedBytesForCurrentThread();GC.KeepAlive(new byte[128]);return GC.GetAllocatedBytesForCurrentThread()>a;}
        public static void Count(string key,long value){if(Enabled){counts.TryGetValue(key,out long old);counts[key]=old+value;}}
        sealed class Scope:IDisposable
        {
            readonly long start,bytes;readonly int frame;internal readonly string path;readonly ProfilerMarker marker;internal double children;
            internal Scope(string name){frame=Time.frameCount;path=stack.Count==0?name:stack.Peek().path+"/"+name;if(!markers.TryGetValue(name,out marker)){marker=new ProfilerMarker("City.Diag."+name);markers.Add(name,marker);}marker.Begin();stack.Push(this);bytes=allocations?GC.GetAllocatedBytesForCurrentThread():0;start=Stopwatch.GetTimestamp();}
            public void Dispose(){double ms=(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;long allocated=allocations?GC.GetAllocatedBytesForCurrentThread()-bytes:-1;stack.Pop();if(stack.Count>0)stack.Peek().children+=ms;if(!rows.TryGetValue((frame,path),out var row)){row=new Row{frame=frame,path=path};rows.Add((frame,path),row);}row.calls++;row.total+=ms;row.self+=ms-children;row.bytes=allocations?row.bytes+allocated:-1;marker.End();}
        }
        public sealed class Phases:IDisposable{IDisposable current;public void Next(string name){current?.Dispose();current=Measure(name);}public void Dispose(){current?.Dispose();}}
        public static IDisposable Measure(string name)=>Enabled?new Scope(name):null;
        static string N(double v)=>v.ToString("R",CultureInfo.InvariantCulture);
        public static void Write(string output){File.WriteAllLines(Path.Combine(output,"detail-scopes.csv"),new[]{"unity_frame,path,calls,inclusive_ms,self_ms,thread_gc_bytes"}.Concat(rows.Values.Select(r=>string.Join(",",r.frame,r.path,r.calls,N(r.total),N(r.self),r.bytes))));File.WriteAllLines(Path.Combine(output,"counts.csv"),new[]{"name,count"}.Concat(counts.Select(p=>p.Key+","+p.Value)));}
    }
    public sealed class HarborCityDemolitionFrameProbe:MonoBehaviour
    {
        HarborCityRoadCoreGame host;string output;
        public void Configure(HarborCityRoadCoreGame h,string path,string result,float rate){host=h;output=result;host.ScenarioMeasuring=true;StartCoroutine(Run());}
        struct Row{internal int frame;internal double interval,delta,main,render,gcMs;internal long bytes;}
        static string N(double x)=>x.ToString("R",CultureInfo.InvariantCulture);
        IEnumerator Run()
        {
            Directory.CreateDirectory(output);QualitySettings.vSyncCount=0;Application.targetFrameRate=60;host.ScenarioRate(0);yield return null;double warm=Time.realtimeSinceStartupAsDouble;while(Time.realtimeSinceStartupAsDouble-warm<8)yield return null;
            var s=host.ScenarioSession;var w=s.Transport;w.DepotLaneRoutingEnabled=true;File.WriteAllBytes(Path.Combine(output,"before.hcity"),CityCoreSave.Write(s));
            bool raw=Environment.GetCommandLineArgs().Contains("-harbor-demolition-raw");if(raw){UnityEngine.Profiling.Profiler.logFile=Path.Combine(output,"window.raw");UnityEngine.Profiling.Profiler.maxUsedMemory=128*1024*1024;UnityEngine.Profiling.Profiler.enableBinaryLog=true;UnityEngine.Profiling.Profiler.enabled=true;}
            var rows=new List<Row>();using(var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame"))using(var main=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread"))using(var render=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Render Thread"))using(var collect=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC.Collect"))
            {
                DemolitionFrameCounters.Enabled=true;for(int i=0;i<3;i++)yield return null;
                int editFrame=Time.frameCount;double start=Time.realtimeSinceStartupAsDouble,last=start;var timer=Stopwatch.StartNew();
                using(DemolitionFrameCounters.Measure("Operation"))
                {
                    using(DemolitionFrameCounters.Measure("HistoryAndConstruction"))new RoadEditHistory(w).Execute(()=>s.Construction.RemoveRoad(87));
                    using(DemolitionFrameCounters.Measure("Bindings"))new CityCoreRoadBindings(s.City,w).RefreshIfChanged();
                    using(DemolitionFrameCounters.Measure("ExplicitRoadViews"))host.ScenarioRefreshRoads();
                }
                timer.Stop();double operation=timer.Elapsed.TotalMilliseconds;
                for(int i=0;i<12;i++){yield return null;double now=Time.realtimeSinceStartupAsDouble;rows.Add(new Row{frame=Time.frameCount,interval=(now-last)*1000,delta=Time.unscaledDeltaTime*1000,main=main.Valid?main.LastValue/1e6:-1,render=render.Valid?render.LastValue/1e6:-1,gcMs=collect.Valid?collect.LastValue/1e6:-1,bytes=gc.Valid?gc.LastValue:-1});last=now;}
                DemolitionFrameCounters.Enabled=false;if(raw){UnityEngine.Profiling.Profiler.enabled=false;UnityEngine.Profiling.Profiler.enableBinaryLog=false;}
                File.WriteAllText(Path.Combine(output,"summary.csv"),"edit_unity_frame,operation_ms,first_coroutine_interval_ms,raw\n"+string.Join(",",editFrame,N(operation),N(rows[0].interval),raw));
            }
            File.WriteAllLines(Path.Combine(output,"frames.csv"),new[]{"observed_unity_frame,completed_unity_frame,coroutine_interval_ms,unscaled_delta_ms,previous_main_ms,previous_render_ms,previous_gc_collect_ms,previous_gc_bytes"}.Concat(rows.Select(r=>string.Join(",",r.frame,r.frame-1,N(r.interval),N(r.delta),N(r.main),N(r.render),N(r.gcMs),r.bytes))));DemolitionFrameCounters.Write(output);
            yield return new WaitForEndOfFrame();var screenshot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,"after.png"),screenshot.EncodeToPNG());Destroy(screenshot);
            if(Environment.GetCommandLineArgs().Contains("-harbor-demolition-view-check"))
            {
                var type=typeof(HarborCityRoadCoreGame).Assembly.GetType("HarborCityZoningViewDeltaChecks",true);
                var field=typeof(HarborCityRoadCoreGame).GetField("zoningViews",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);var view=field.GetValue(host);
                var palette=(Material[])view.GetType().GetField("materials",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(view);
                yield return (IEnumerator)type.GetMethod("CompareCity").Invoke(null,new object[]{view,s.Simulation.Growth.Zoning.Cells(),(Func<float,float,float>)host.ScenarioSurface,palette,output});
            }
            s.Validate();var boundary=CityCoreSave.Write(s);File.WriteAllBytes(Path.Combine(output,"pending.hcity"),boundary);var cold=CityCoreSave.Read(boundary);cold.Transport.DepotLaneRoutingEnabled=true;cold.Simulation.Growth.Zoning.SetSurface(host.ScenarioSurface);
            for(int i=0;i<80;i++){s.Simulation.Advance(.05,1,double.PositiveInfinity);cold.Simulation.Advance(.05,1,double.PositiveInfinity);}s.Validate();cold.Validate();if(!CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(cold)))throw new Exception("Diagnostic instrumentation changed cold continuation");File.WriteAllBytes(Path.Combine(output,"final.hcity"),CityCoreSave.Write(s));File.WriteAllText(Path.Combine(output,"Result.txt"),"PASS diagnostic-only, exact saved edit, full validation, 80 fixed-step hot/cold state identical; CPU and coroutine clocks recorded separately.\n");Application.Quit(0);
        }
    }
}
