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
    // Observes the normal host clock and views; optional fixed warmup is outside measurement.
    public sealed class HarborCityScenarioWindowProbe:MonoBehaviour
    {
        struct Sample {public double seconds,ms,advance,vehicles,structure,pending,gpu,requested,rate;public long bytes,tick,mainNs,renderNs,gcNs,uiNs,batches,drawCalls,setPass,triangles;public int steps,active,views;}
        readonly FrameTiming[] timing=new FrameTiming[1];
        HarborCityRoadCoreGame host;string savePath,output;float rate;
        public void Configure(HarborCityRoadCoreGame game,string input,string result,float speed)
        {
            var experimentArgs=Environment.GetCommandLineArgs();
            HarborCity.RoadCore.DepotRoutingTrace.Enabled=Array.IndexOf(experimentArgs,"-harbor-depot-routing-trace")>=0;HarborCity.RoadCore.DepotRoutingTrace.Reset();
            host=game;savePath=input;output=result;rate=speed;host.ScenarioMeasuring=true;
            var args=Environment.GetCommandLineArgs();int ms=Array.IndexOf(args,"-harbor-window-budget-ms"),steps=Array.IndexOf(args,"-harbor-window-max-steps");
            if(ms>=0||steps>=0)host.ScenarioWorkBudget(steps<0?32:int.Parse(args[steps+1],CultureInfo.InvariantCulture),ms<0?8:double.Parse(args[ms+1],CultureInfo.InvariantCulture));
            StartCoroutine(Measure());
        }
        IEnumerator Measure()
        {
            Directory.CreateDirectory(output);QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
            yield return null;yield return null;
            bool sharedWarmup=Array.IndexOf(Environment.GetCommandLineArgs(),"-harbor-window-shared-warmup")>=0;
            var laneArgs=Environment.GetCommandLineArgs();int laneWarmIndex=Array.IndexOf(laneArgs,"-harbor-window-lane-warm-steps");
            int laneWarmSteps=laneWarmIndex<0?0:int.Parse(laneArgs[laneWarmIndex+1],CultureInfo.InvariantCulture);
            if(laneWarmSteps<0||laneWarmSteps>20000||laneWarmSteps>0&&(!sharedWarmup||Array.IndexOf(laneArgs,"-harbor-depot-lane-routing")<0))throw new InvalidOperationException("Lane warmup requires shared warmup and explicit lane routing");
            host.ScenarioRate(sharedWarmup?0:rate);
            if(sharedWarmup)
            {
                var warmCity=host.ScenarioSession;
                if(laneWarmSteps>0)warmCity.Transport.DepotLaneRoutingEnabled=true;
                for(int i=0;i<200;i++){warmCity.Simulation.Advance(.05,1,double.PositiveInfinity);if(i%10==9)yield return null;}
                for(int i=0;i<laneWarmSteps;i++){warmCity.Simulation.Advance(.05,1,double.PositiveInfinity);if(i%10==9)yield return null;}
            }
            var warmArgs=Environment.GetCommandLineArgs();var warmSession=host.ScenarioSession;
            bool prewarm=Array.IndexOf(warmArgs,"-harbor-window-prewarm-depots")>=0;
            bool experiment=Array.IndexOf(warmArgs,"-harbor-depot-junction-routing")>=0;
            if(prewarm)
            {
                if(!sharedWarmup||!experiment)throw new InvalidOperationException("Depot prewarm needs shared warmup and the junction experiment");
                byte[] before=CityCoreSave.Write(warmSession);warmSession.Transport.DepotJunctionRoutingEnabled=true;
                var cache=warmSession.Transport.JunctionRoutes;var watch=System.Diagnostics.Stopwatch.StartNew();int depots=0;
                foreach(var plant in warmSession.City.buildings.OfType<LandfillBuilding>().OrderBy(p=>p.id))
                {
                    var access=warmSession.Transport.GetAccess(plant.id);if(access==null||!access.valid)continue;
                    cache.StateCost(warmSession.Transport.Network,plant.id,access,0,false);cache.StateCost(warmSession.Transport.Network,plant.id,access,0,true);depots++;
                    if(depots%8==0)yield return null;
                }
                watch.Stop();if(!before.SequenceEqual(CityCoreSave.Write(warmSession)))throw new InvalidOperationException("Cache prewarm changed saved city facts");
                File.WriteAllText(Path.Combine(output,"cache-prewarm.csv"),"depots,builds,expanded,tree_bytes,wall_ms\n"+string.Join(",",depots,cache.Builds,cache.Expanded,cache.TreeBytes,watch.Elapsed.TotalMilliseconds.ToString("R",CultureInfo.InvariantCulture)));
            }
            int extraFlag=Array.IndexOf(warmArgs,"-harbor-window-extra-warm-steps");
            if(extraFlag>=0)
            {
                int extra=int.Parse(warmArgs[extraFlag+1],CultureInfo.InvariantCulture);if(!prewarm||extra<0||extra>20000)throw new ArgumentException("Additional steady warmup requires prewarmed depots and a bounded step count");
                for(int i=0;i<extra;i++){if(warmSession.Simulation.Advance(.05,1,double.PositiveInfinity)!=1)throw new InvalidOperationException("Warm step lost");if(i%32==31)yield return null;}
            }
            string roadMode="normal";int modeFlag=Array.IndexOf(warmArgs,"-harbor-window-road-views");if(modeFlag>=0)roadMode=warmArgs[modeFlag+1];
            if(roadMode!="normal")
            {
                if(roadMode!="renderers-off"&&roadMode!="inactive"&&roadMode!="shadows-off")throw new ArgumentException("Unknown diagnostic road-view mode");
                var renderers=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>(r.name.StartsWith("Shared lane geometry ",StringComparison.Ordinal)||r.name.StartsWith("Road chunk ",StringComparison.Ordinal))).ToArray();
                if(renderers.Length!=host.ScenarioRoadRenderers)throw new InvalidOperationException("Road ablation did not identify exactly the owned road views");
                foreach(var renderer in renderers){if(roadMode=="renderers-off")renderer.enabled=false;else if(roadMode=="inactive")renderer.gameObject.SetActive(false);else{renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;}}
                File.WriteAllText(Path.Combine(output,"road-ablation.csv"),"mode,road_objects\n"+roadMode+","+renderers.Length+"\n");
            }
            double warm=Time.realtimeSinceStartupAsDouble;
            while(Time.realtimeSinceStartupAsDouble-warm<8)yield return null;
            if(sharedWarmup)
            {
                var warmCity=host.ScenarioSession;var warmState=CityCoreSave.Write(warmCity);
                using(var sha=SHA256.Create())File.WriteAllText(Path.Combine(output,"warm-state.sha256"),BitConverter.ToString(sha.ComputeHash(warmState)).Replace("-","").ToLowerInvariant());
                host.ScenarioRate(rate);
            }
            var city=host.ScenarioSession;
            bool junctionRouting=Array.IndexOf(Environment.GetCommandLineArgs(),"-harbor-depot-junction-routing")>=0;
            city.Transport.DepotLaneRoutingEnabled=Array.IndexOf(Environment.GetCommandLineArgs(),"-harbor-depot-lane-routing")>=0;
            city.Transport.DepotJunctionRoutingEnabled=junctionRouting;
            if(HarborCity.RoadCore.DepotRoutingTrace.Enabled){HarborCity.RoadCore.DepotRoutingTrace.Write(Path.Combine(output,"routing-warmup.csv"));HarborCity.RoadCore.DepotRoutingTrace.Reset();}
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-harbor-depot-lane-cold")>=0)city.Transport.DepotRoutes.Clear();
            city.Transport.JunctionRoutes.Trace=Array.IndexOf(Environment.GetCommandLineArgs(),"-harbor-junction-trace")>=0;
            var initialCache=city.Transport.JunctionRoutes;
            long initialBuilds=initialCache.Builds,initialExpanded=initialCache.Expanded,initialQueries=initialCache.Queries,initialHits=initialCache.Hits,initialMisses=initialCache.Misses,initialInvalidations=initialCache.Invalidations,initialEvictions=initialCache.Evictions;
            var laneCache=city.Transport.DepotRoutes;
            long laneQueries=laneCache.Queries,laneHits=laneCache.Hits,laneMisses=laneCache.Misses,laneEvictions=laneCache.Evictions,laneInvalidations=laneCache.Invalidations,laneBuilds=laneCache.TreeBuilds,laneReuses=laneCache.TreeReuses;
            var roadMemory=new List<string>{"phase,drivables,renderers,vertices,triangles,mesh_runtime_bytes,managed_gc_bytes,unity_allocated_bytes"};
            RoadMemory(roadMemory,"start");
            long startTick=city.Simulation.Tick;
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
            int measureFlag=Array.IndexOf(arguments,"-harbor-window-seconds");
            if(measureFlag>=0){duration=double.Parse(arguments[measureFlag+1],CultureInfo.InvariantCulture);if(duration<15||duration>150)throw new ArgumentOutOfRangeException("Bounded window duration");}
            bool coreProfile=Array.IndexOf(arguments,"-harbor-window-core-profile")>=0;
#if CITY_CORE_ATTRIBUTION
            if(coreProfile){HarborCity.RoadCore.CityCoreProfile.Reset();HarborCity.RoadCore.CityCoreProfile.Enabled=true;}
#else
            if(coreProfile)throw new InvalidOperationException("Core counters require an attributed diagnostic build");
#endif
            int frameLimit=60;long triggerTick=0;int option=Array.IndexOf(arguments,"-harbor-raw-frame-limit");if(option>=0)frameLimit=int.Parse(arguments[option+1],CultureInfo.InvariantCulture);
            option=Array.IndexOf(arguments,"-harbor-raw-start-tick");if(option>=0)triggerTick=long.Parse(arguments[option+1],CultureInfo.InvariantCulture);
            if(frameLimit<5||frameLimit>120||triggerTick<0)throw new ArgumentOutOfRangeException("Bounded raw frame capture");
            bool roadEdit=Array.IndexOf(arguments,"-harbor-window-road-edit")>=0,roadEdited=false;
            var roadEditRows=new List<string>{"tick,accepted,paid,plan_commit_ms,road_sync_ms,created_renderers,removed_renderers,logical_drivables,renderers,error"};
            bool routingEdit=Array.IndexOf(arguments,"-harbor-window-routing-edit")>=0;
            var editRows=new List<string>{"tick,accepted,paid,transaction_ms,error"};int editAttempts=0;bool editSucceeded=false;double nextEdit=5;
            bool recording=false,recorded=false;int capturedFrames=0;long captureStartTick=0,captureEndTick=0;
            double start=Time.realtimeSinceStartupAsDouble,last=start;long allocated=0;int gcSamples=0,peak=0,maxSteps=0;
            int gen0=GC.CollectionCount(0),gen1=GC.CollectionCount(1),gen2=GC.CollectionCount(2);
            while(Time.realtimeSinceStartupAsDouble-start<duration)
            {
                if(roadEdit&&!roadEdited&&Time.realtimeSinceStartupAsDouble-start>=(raw?1:5))
                {
                    roadEdited=true;bool accepted=false;int paid=0;string error="";double syncMs=0;int made=0,removed=0;
                    var watch=System.Diagnostics.Stopwatch.StartNew();
                    try
                    {
                        float half=city.City.map.buildHalfSize,x=half-32,z=half-40;
                        var plan=city.Transport.PlanConnectedRoad(new[]{new HarborCity.RoadCore.Point(x,host.ScenarioSurface(x,z),z),new HarborCity.RoadCore.Point(x,host.ScenarioSurface(x,z+12),z+12)},lanes:2);
                        city.Construction.CheckRoadPlacement(plan);accepted=city.Construction.Commit(plan,out paid);watch.Stop();
                        if(accepted)
                        {
                            var actors=city.Transport.Vehicles.Select(v=>new{vehicle=v,position=city.Transport.Position(v)}).ToArray();int created=host.ScenarioRoadCreated,released=host.ScenarioRoadRemoved;
                            var sync=System.Diagnostics.Stopwatch.StartNew();host.ScenarioRefreshRoads();sync.Stop();syncMs=sync.Elapsed.TotalMilliseconds;made=host.ScenarioRoadCreated-created;removed=host.ScenarioRoadRemoved-released;
                            foreach(var actor in actors)if(HarborCity.RoadCore.Point.Distance(actor.position,city.Transport.Position(actor.vehicle))!=0)throw new InvalidOperationException("Road display moved a real actor");
                        }
                    }
                    catch(Exception e){error=e.Message.Replace(",",";").Replace("\n"," ").Replace("\r"," ");watch.Stop();}
                    roadEditRows.Add(string.Join(",",city.Simulation.Tick,accepted,paid,watch.Elapsed.TotalMilliseconds.ToString("R",CultureInfo.InvariantCulture),syncMs.ToString("R",CultureInfo.InvariantCulture),made,removed,host.ScenarioRoadViews,host.ScenarioRoadRenderers,error));
                    if(!accepted||error.Length>0){File.WriteAllLines(Path.Combine(output,"road-edit.csv"),roadEditRows);throw new InvalidOperationException("Road-view edit probe did not complete: "+error);}
                }
                if(routingEdit&&!editSucceeded&&editAttempts<3&&Time.realtimeSinceStartupAsDouble-start>=nextEdit)
                {
                    if(!junctionRouting)throw new InvalidOperationException("Routing edit probe requires junction experiment");
                    editAttempts++;nextEdit+=3;long tick=city.Simulation.Tick;int paid=0;string error="";var watch=System.Diagnostics.Stopwatch.StartNew();
                    try{int road=city.Transport.Network.RoadIds.Min();var settings=city.Transport.Network.Settings(road);editSucceeded=city.Construction.Commit(city.Transport.PlanUpgradeRoad(road,settings.lanes,settings.speed+1,settings.direction),out paid);}
                    catch(InvalidOperationException e) when(e.Message.Contains("施工节点安全区")){error=e.Message.Replace(",",";").Replace("\n"," ").Replace("\r"," ");}
                    watch.Stop();editRows.Add(string.Join(",",tick,editSucceeded,paid,watch.Elapsed.TotalMilliseconds.ToString("R",CultureInfo.InvariantCulture),error));
                }
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
            if(HarborCity.RoadCore.DepotRoutingTrace.Enabled){HarborCity.RoadCore.DepotRoutingTrace.Enabled=false;HarborCity.RoadCore.DepotRoutingTrace.Write(Path.Combine(output,"routing-window.csv"));}
            var depotCache=city.Transport.DepotRoutes;
            File.WriteAllText(Path.Combine(output,"lane-cache-window.csv"),"enabled,queries,hits,misses,evictions,invalidations,tree_builds,tree_reuses,resident_depots,reserved_tree_bytes\n"+string.Join(",",city.Transport.DepotLaneRoutingEnabled,laneCache.Queries-laneQueries,laneCache.Hits-laneHits,laneCache.Misses-laneMisses,laneCache.Evictions-laneEvictions,laneCache.Invalidations-laneInvalidations,laneCache.TreeBuilds-laneBuilds,laneCache.TreeReuses-laneReuses,laneCache.Count,laneCache.ReservedTreeBytes));
            File.WriteAllText(Path.Combine(output,"routing-cache.csv"),"enabled,depots,hits,misses,evictions,invalidations,reserved_tree_bytes,reserved_graph_bytes,tree_builds,tree_reuses\n"+string.Join(",",city.Transport.DepotLaneRoutingEnabled,depotCache.Count,depotCache.Hits,depotCache.Misses,depotCache.Evictions,depotCache.Invalidations,depotCache.ReservedTreeBytes,depotCache.ReservedGraphBytes,depotCache.TreeBuilds,depotCache.TreeReuses));

#if CITY_CORE_ATTRIBUTION
            if(coreProfile){HarborCity.RoadCore.CityCoreProfile.Enabled=false;File.WriteAllText(Path.Combine(output,"core-profile.csv"),HarborCity.RoadCore.CityCoreProfile.Header()+"\n"+HarborCity.RoadCore.CityCoreProfile.Row(city.Simulation.Tick,city.Transport.ActiveCount,city.Transport.RoutePreparations,GC.CollectionCount(0)-gen0,GC.CollectionCount(1)-gen1,GC.CollectionCount(2)-gen2)+"\n");}
#endif
            if(roadEdit)File.WriteAllLines(Path.Combine(output,"road-edit.csv"),roadEditRows);
            if(routingEdit)File.WriteAllLines(Path.Combine(output,"routing-edit.csv"),editRows);
            var jr=city.Transport.JunctionRoutes;
            File.WriteAllText(Path.Combine(output,"cache-window.csv"),"builds,expanded,queries,hits,misses,invalidations,evictions\n"+string.Join(",",jr.Builds-initialBuilds,jr.Expanded-initialExpanded,jr.Queries-initialQueries,jr.Hits-initialHits,jr.Misses-initialMisses,jr.Invalidations-initialInvalidations,jr.Evictions-initialEvictions));
            File.WriteAllText(Path.Combine(output,"junction-routing.csv"),jr.TraceCsv());
            File.WriteAllText(Path.Combine(output,"junction-cache.csv"),"nodes,lanes,entries,tree_bytes,graph_payload_estimate,queries,hits,misses,evictions,invalidations,local_checks,fallbacks,unreachable,builds,expanded,topology_only_nodes,fallback_invalid,fallback_inactive,fallback_budget,fallback_ms\n"+string.Join(",",jr.Nodes,jr.Lanes,jr.Count,jr.TreeBytes,jr.GraphPayloadEstimate,jr.Queries,jr.Hits,jr.Misses,jr.Evictions,jr.Invalidations,jr.LocalChecks,jr.Fallbacks,jr.Unreachable,jr.Builds,jr.Expanded,jr.TopologyOnlyNodes,jr.InvalidAccessFallbacks,jr.InactiveDepotFallbacks,jr.BudgetFallbacks,jr.FallbackMilliseconds));
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
            File.WriteAllText(Path.Combine(output,"summary.csv"),"input_sha256,rate,real_seconds,start_tick,end_tick,sim_ratio,fps,p95_ms,p99_ms,max_ms,gc_bytes_per_frame,gc_samples,gen0,gen1,gen2,peak_vehicles,max_steps_frame,building_views,population,zone_plots,zone_renderers,road_views,utility_views,created_vehicle_views,reused_vehicle_views,inactive_vehicle_views,road_renderers,road_chunk_size\n"+string.Join(",",hash,rate,elapsed,startTick,endTick,(endTick-startTick)*.05/elapsed,frames.Count/elapsed,p95,p99,frames.Last(),gcSamples==frames.Count?(double)allocated/gcSamples:-1,gcSamples,endGen0-gen0,endGen1-gen1,endGen2-gen2,peak,maxSteps,host.ScenarioBuildingViews,city.City.population,host.ScenarioZonePlots,host.ScenarioZoneRenderers,host.ScenarioRoadViews,host.ScenarioUtilityViews,host.ScenarioCreatedVehicleViews,host.ScenarioReusedVehicleViews,host.ScenarioPooledVehicles,host.ScenarioRoadRenderers,host.ScenarioRoadChunkSize));
            RoadMemory(roadMemory,"end");File.WriteAllLines(Path.Combine(output,"road-memory.csv"),roadMemory);
            // Validation and disk output are outside the measured window.
            yield return null;
            if(city.Simulation.Tick!=endTick||city.Simulation.PendingSeconds!=endPending)throw new InvalidOperationException("Paused host silently drains saved debt");
            try
            {
                city.Validate();byte[] final=CityCoreSave.Write(city);File.WriteAllBytes(Path.Combine(output,"final.hcity"),final);
                if(Array.IndexOf(Environment.GetCommandLineArgs(),"-harbor-window-restore")>=0)
                {
                    var restored=CityCoreSave.Read(final);restored.Transport.DepotLaneRoutingEnabled=city.Transport.DepotLaneRoutingEnabled;restored.Transport.DepotJunctionRoutingEnabled=junctionRouting;restored.Simulation.Growth.Zoning.SetSurface(host.ScenarioSurface);
                    for(int i=0;i<20;i++){double request=city.Simulation.PendingSeconds>=.05?0:.05;if(city.Simulation.Advance(request,1,double.PositiveInfinity)!=1||restored.Simulation.Advance(request,1,double.PositiveInfinity)!=1)throw new InvalidOperationException("Restore check lost a full step");}
                    city.Validate();restored.Validate();if(!CityCoreSave.Write(city).SequenceEqual(CityCoreSave.Write(restored)))throw new InvalidOperationException("Warm/cold whole-city debt continuation differs");
                    File.WriteAllText(Path.Combine(output,"RestoreResult.txt"),"PASS native Unity warm/cold 20 complete steps with saved pending debt, all city/transport/money/goods/waste state byte-identical. Outside timed window; final.hcity is original measured endpoint.\n");
                }
                File.WriteAllText(Path.Combine(output,"Result.txt"),"PASS normal host visible Player, loaded captured city and saved map; not mouse construction acceptance. GC pauses not measured.\n");
            }
            catch(Exception e){File.WriteAllText(Path.Combine(output,"Result.txt"),e.ToString());Application.Quit(1);yield break;}
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"window.png"));yield return new WaitForEndOfFrame();yield return null;
            Debug.Log("Harbor window probe: validation/output complete; requesting Player shutdown");Application.Quit(0);
        }
        void RoadMemory(List<string> rows,string phase)
        {
            var roads=UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>r.name.StartsWith("Road chunk ",StringComparison.Ordinal)||r.name.StartsWith("Shared lane geometry ",StringComparison.Ordinal));long vertices=0,triangles=0,bytes=0;int count=0;
            foreach(var r in roads){var mesh=r.sharedMesh;if(mesh==null)continue;vertices+=mesh.vertexCount;triangles+=(long)mesh.GetIndexCount(0)/3;bytes+=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(mesh);count++;}
            if(count!=host.ScenarioRoadRenderers)throw new InvalidOperationException("Actual chunk renderer count does not match ownership");
            rows.Add(string.Join(",",phase,host.ScenarioRoadViews,count,vertices,triangles,bytes,GC.GetTotalMemory(false),UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()));
        }
        const double WorldStepSeconds=HarborCity.RoadCore.World.StepSeconds;
    }
}
