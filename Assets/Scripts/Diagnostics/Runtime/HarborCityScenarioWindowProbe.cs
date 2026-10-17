using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Unity.Profiling;
using UnityEngine;
namespace HarborCity
{
    // Observes the normal host clock and views. Never advances or repairs the city.
    public sealed class HarborCityScenarioWindowProbe:MonoBehaviour
    {
        struct Sample {public double seconds,ms,advance,vehicles,structure,pending,gpu,requested,rate;public long bytes,tick,mainNs,renderNs,gcNs,uiNs,batches,drawCalls,setPass,triangles;public int steps,active,views;}
        readonly FrameTiming[] timing=new FrameTiming[1];
        HarborCityRoadCoreGame host;string savePath,output;float rate;
        public void Configure(HarborCityRoadCoreGame game,string input,string result,float speed)
        {host=game;savePath=input;output=result;rate=speed;host.ScenarioMeasuring=true;StartCoroutine(Measure());}
        IEnumerator Measure()
        {
            Directory.CreateDirectory(output);QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
            yield return null;yield return null;
            host.ScenarioRate(rate);double warm=Time.realtimeSinceStartupAsDouble;
            while(Time.realtimeSinceStartupAsDouble-warm<8)yield return null;
            var city=host.ScenarioSession;long startTick=city.Simulation.Tick;
            double startPending=city.Simulation.PendingSeconds;
            var frames=new List<double>(5000);var rows=new List<Sample>(5000);
            var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
            var mainThread=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1);
            var renderThread=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Render Thread",1);
            var collect=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC.Collect",1);
            var ui=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"City.Views.UI",1);
            var batches=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count",1);var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1);var passes=ProfilerRecorder.StartNew(ProfilerCategory.Render,"SetPass Calls Count",1);var triangles=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count",1);
            bool raw=Array.IndexOf(Environment.GetCommandLineArgs(),"-harbor-window-raw")>=0;
            double duration=raw?15:60;
            var arguments=Environment.GetCommandLineArgs();int durationFlag=Array.IndexOf(arguments,"-harbor-window-duration");
            if(raw&&durationFlag>=0){duration=double.Parse(arguments[durationFlag+1],CultureInfo.InvariantCulture);if(duration<15||duration>150)throw new ArgumentOutOfRangeException("Raw capture duration");}
            int frameLimit=60;long triggerTick=0;int option=Array.IndexOf(arguments,"-harbor-raw-frame-limit");if(option>=0)frameLimit=int.Parse(arguments[option+1],CultureInfo.InvariantCulture);
            option=Array.IndexOf(arguments,"-harbor-raw-start-tick");if(option>=0)triggerTick=long.Parse(arguments[option+1],CultureInfo.InvariantCulture);
            if(frameLimit<5||frameLimit>120||triggerTick<0)throw new ArgumentOutOfRangeException("Bounded raw frame capture");
            bool recording=false,recorded=false;int capturedFrames=0;long captureStartTick=0,captureEndTick=0;
            double start=Time.realtimeSinceStartupAsDouble,last=start;long allocated=0;int gcSamples=0,peak=0,maxSteps=0;
            int gen0=GC.CollectionCount(0),gen1=GC.CollectionCount(1),gen2=GC.CollectionCount(2);
            while(Time.realtimeSinceStartupAsDouble-start<duration)
            {
                if(raw&&!recorded&&!recording&&city.Simulation.Tick>=triggerTick)
                {UnityEngine.Profiling.Profiler.logFile=Path.Combine(output,"window.raw");UnityEngine.Profiling.Profiler.maxUsedMemory=128*1024*1024;UnityEngine.Profiling.Profiler.enableBinaryLog=true;UnityEngine.Profiling.Profiler.enabled=true;recording=true;captureStartTick=city.Simulation.Tick;}
                yield return null;double now=Time.realtimeSinceStartupAsDouble,ms=(now-last)*1000;last=now;frames.Add(ms);
                if(host.ScenarioCurrentRate!=rate){File.WriteAllText(Path.Combine(output,"InputFailure.txt"),"Requested rate "+rate+" changed to "+host.ScenarioCurrentRate+"; discard this measurement.");Application.Quit(1);yield break;}
                long bytes=gc.Valid&&gc.Count>0?gc.LastValue:-1;if(bytes>=0){allocated+=bytes;gcSamples++;}
                peak=Math.Max(peak,city.Transport.ActiveCount);maxSteps=Math.Max(maxSteps,host.ScenarioSteps);
                FrameTimingManager.CaptureFrameTimings();uint count=FrameTimingManager.GetLatestTimings(1,timing);
                rows.Add(new Sample{seconds=now-start,ms=ms,rate=host.ScenarioCurrentRate,bytes=bytes,tick=city.Simulation.Tick,steps=host.ScenarioSteps,active=city.Transport.ActiveCount,views=host.ScenarioVehicleViews,advance=host.ScenarioAdvanceMs,vehicles=host.ScenarioVehicleMs,structure=host.ScenarioStructureMs,pending=city.Simulation.PendingSeconds,batches=batches.Valid&&batches.Count>0&&batches.LastValue>0?batches.LastValue:-1,drawCalls=draws.Valid&&draws.Count>0&&draws.LastValue>0?draws.LastValue:-1,setPass=passes.Valid&&passes.Count>0&&passes.LastValue>0?passes.LastValue:-1,triangles=triangles.Valid&&triangles.Count>0&&triangles.LastValue>0?triangles.LastValue:-1,requested=host.ScenarioRequestedSeconds,mainNs=mainThread.Valid&&mainThread.Count>0&&mainThread.LastValue>0?mainThread.LastValue:-1,renderNs=renderThread.Valid&&renderThread.Count>0&&renderThread.LastValue>0?renderThread.LastValue:-1,gcNs=collect.Valid&&collect.Count>0?collect.LastValue:-1,uiNs=ui.Valid&&ui.Count>0?ui.LastValue:-1,gpu=count>0&&timing[0].gpuFrameTime>0?timing[0].gpuFrameTime:-1});
                if(recording&&++capturedFrames>=frameLimit){captureEndTick=city.Simulation.Tick;UnityEngine.Profiling.Profiler.enabled=false;UnityEngine.Profiling.Profiler.enableBinaryLog=false;recording=false;recorded=true;}
            }
            int endGen0=GC.CollectionCount(0),endGen1=GC.CollectionCount(1),endGen2=GC.CollectionCount(2);
            double elapsed=last-start;long endTick=city.Simulation.Tick;host.ScenarioRate(0);gc.Dispose();mainThread.Dispose();renderThread.Dispose();collect.Dispose();ui.Dispose();batches.Dispose();draws.Dispose();passes.Dispose();triangles.Dispose();
            if(raw){UnityEngine.Profiling.Profiler.enabled=false;UnityEngine.Profiling.Profiler.enableBinaryLog=false;}
            if(raw){if(recording){captureEndTick=city.Simulation.Tick;recorded=true;}File.WriteAllText(Path.Combine(output,"capture.json"),"{\"started\":"+(recorded?"true":"false")+",\"trigger_tick\":"+triggerTick+",\"start_tick\":"+captureStartTick+",\"end_tick\":"+captureEndTick+",\"observed_frames\":"+capturedFrames+",\"limit\":"+frameLimit+",\"note\":\"Partial diagnostic instrumentation; whole-window FPS is not normal uninstrumented performance.\"}");if(!recorded){File.WriteAllText(Path.Combine(output,"CaptureFailure.txt"),"Requested tick was not reached within bounded window");Application.Quit(1);yield break;}}
            double endPending=city.Simulation.PendingSeconds;
            double balanceError=startPending+rows.Sum(r=>r.requested)-(endTick-startTick)*WorldStepSeconds-endPending;
            if(rows.Sum(r=>r.steps)!=endTick-startTick||Math.Abs(balanceError)>1e-6)throw new InvalidOperationException("Requested time/fixed steps/debt do not balance");
            frames.Sort();double p95=frames[(int)Math.Ceiling(frames.Count*.95)-1],p99=frames[(int)Math.Ceiling(frames.Count*.99)-1];
            string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(savePath))).Replace("-","").ToLowerInvariant();
            File.WriteAllLines(Path.Combine(output,"frames.csv"),new[]{"real_seconds,frame_ms,gc_bytes,tick,fixed_steps,active_vehicles,vehicle_views,advance_ms,vehicle_sync_ms,structure_sync_ms,pending_seconds,main_thread_ns,render_thread_ns,gc_collect_ns,gpu_ms,ui_ns,requested_seconds,batches,draw_calls,setpass_calls,triangles,actual_rate"}.Concat(rows.Select(r=>string.Join(",",r.seconds.ToString("R",CultureInfo.InvariantCulture),r.ms.ToString("R",CultureInfo.InvariantCulture),r.bytes,r.tick,r.steps,r.active,r.views,r.advance,r.vehicles,r.structure,r.pending,r.mainNs,r.renderNs,r.gcNs,r.gpu,r.uiNs,r.requested,r.batches,r.drawCalls,r.setPass,r.triangles,r.rate))));
            File.WriteAllText(Path.Combine(output,"clock-balance.json"),"{\"initial_pending_seconds\":"+startPending.ToString("R",CultureInfo.InvariantCulture)+",\"requested_seconds\":"+rows.Sum(r=>r.requested).ToString("R",CultureInfo.InvariantCulture)+",\"completed_steps\":"+(endTick-startTick)+",\"final_pending_seconds\":"+endPending.ToString("R",CultureInfo.InvariantCulture)+",\"balance_error_seconds\":"+balanceError.ToString("R",CultureInfo.InvariantCulture)+"}");
            File.WriteAllText(Path.Combine(output,"attribution.json"),"{\"advance_mean_ms\":"+rows.Average(r=>r.advance).ToString("R",CultureInfo.InvariantCulture)+",\"vehicle_sync_mean_ms\":"+rows.Average(r=>r.vehicles).ToString("R",CultureInfo.InvariantCulture)+",\"structure_sync_mean_ms\":"+rows.Average(r=>r.structure).ToString("R",CultureInfo.InvariantCulture)+",\"max_pending_seconds\":"+rows.Max(r=>r.pending).ToString("R",CultureInfo.InvariantCulture)+",\"note\":\"Stopwatch host scopes are current Update. ProfilerRecorder and GPU timings may lag; unavailable is -1. Main Thread can include waits; GC.Collect marker is not a proven complete pause total. This probe does not budget or discard steps.\"}");
            File.WriteAllText(Path.Combine(output,"summary.csv"),"input_sha256,rate,real_seconds,start_tick,end_tick,sim_ratio,fps,p95_ms,p99_ms,max_ms,gc_bytes_per_frame,gc_samples,gen0,gen1,gen2,peak_vehicles,max_steps_frame,building_views,population,zone_plots,zone_renderers,road_views,utility_views\n"+string.Join(",",hash,rate,elapsed,startTick,endTick,(endTick-startTick)*.05/elapsed,frames.Count/elapsed,p95,p99,frames.Last(),gcSamples==frames.Count?(double)allocated/gcSamples:-1,gcSamples,endGen0-gen0,endGen1-gen1,endGen2-gen2,peak,maxSteps,host.ScenarioBuildingViews,city.City.population,host.ScenarioZonePlots,host.ScenarioZoneRenderers,host.ScenarioRoadViews,host.ScenarioUtilityViews));
            // Validation and disk output are outside the measured window.
            yield return null;
            if(city.Simulation.Tick!=endTick||city.Simulation.PendingSeconds!=endPending)throw new InvalidOperationException("Paused host silently drains saved debt");
            try
            {
                city.Validate();byte[] final=CityCoreSave.Write(city);File.WriteAllBytes(Path.Combine(output,"final.hcity"),final);
                if(Array.IndexOf(Environment.GetCommandLineArgs(),"-harbor-window-restore")>=0)
                {
                    var restored=CityCoreSave.Read(final);restored.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
                    for(int i=0;i<20;i++){double request=city.Simulation.PendingSeconds>=.05?0:.05;if(city.Simulation.Advance(request,1,double.PositiveInfinity)!=1||restored.Simulation.Advance(request,1,double.PositiveInfinity)!=1)throw new InvalidOperationException("Restore check lost a full step");}
                    city.Validate();restored.Validate();if(!CityCoreSave.Write(city).SequenceEqual(CityCoreSave.Write(restored)))throw new InvalidOperationException("Warm/cold whole-city debt continuation differs");
                    File.WriteAllText(Path.Combine(output,"RestoreResult.txt"),"PASS native Unity warm/cold 20 complete steps with saved pending debt, all city/transport/money/goods/waste state byte-identical. Outside timed window; final.hcity is original measured endpoint.\n");
                }
                File.WriteAllText(Path.Combine(output,"Result.txt"),"PASS normal host visible Player, loaded captured flat mature city; not mouse construction acceptance. GC pauses not measured.\n");
            }
            catch(Exception e){File.WriteAllText(Path.Combine(output,"Result.txt"),e.ToString());Application.Quit(1);yield break;}
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"window.png"));yield return new WaitForEndOfFrame();yield return null;
            Debug.Log("Harbor window probe: validation/output complete; requesting Player shutdown");Application.Quit(0);
        }
        const double WorldStepSeconds=HarborCity.RoadCore.World.StepSeconds;
    }
}
