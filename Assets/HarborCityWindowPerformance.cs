#if UNITY_EDITOR || CITY_TRAFFIC_WINDOW_MEASUREMENT
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace HarborCity
{
    // Opt-in full-game measurement. Rendering, OnGUI and the normal Update all remain active.
    // The only time clamp is the endpoint of the finite test, as in the traffic benchmark.
    internal static class CityTrafficWindowProbe
    {
        internal static CityTraffic Expected;
        internal static int Steps;
        internal const int Samples=300;
        internal static void NoteStep(CityTraffic t){if(ReferenceEquals(t,Expected))Steps++;}
        internal static float Clamp(CityTraffic t,float seconds)=>ReferenceEquals(t,Expected)?t.ClampWindowSeconds(Samples-Steps,seconds):seconds;
    }
    public sealed partial class CityTraffic {internal float ClampWindowSeconds(int left,float seconds)=>Math.Max(0,Math.Min(seconds,(float)(left*FixedStepSeconds-State.remainder)));}
    public partial class HarborCityGame
    {
        internal CityModel WindowCity=>city;
        internal CityTraffic WindowTraffic=>traffic;
        internal float WindowSpeed {get=>speed;set=>speed=value;}
        internal void WindowLoad(string file){validationSavePath=file;Load();}
        internal void WindowView(){focus=new Vector3(0,0,0);zoom=105;UpdateCamera();}
    }
    [DefaultExecutionOrder(-32000)]
    public sealed class HarborCityWindowPerformance : MonoBehaviour
    {
        const int Repeats=3;
        struct Frame {internal double ms,simulationMs;internal long gc;internal int steps,tasks;internal bool focused;}
        readonly List<Frame> frames=new List<Frame>(10000);
        readonly StringBuilder rows=new StringBuilder("run,mode,frame,wall_ms,simulation_ms,gc_bytes,steps,tasks,focused\n");
        readonly StringBuilder summaries=new StringBuilder("run,mode,frames,wall_seconds,fps,p95_frame_ms,p99_frame_ms,simulation_seconds_per_wall_second,steps,gc_bytes_per_frame,min_tasks,max_tasks,state_sha256\n");
        HarborCityGame game;string directory,input,source;int run=-1,lastSteps;long previousStart;double waitingUntil;bool recording,ready;
        ProfilerRecorder gc,simulation;bool gcSupported;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-harbor-window-performance");if(i<0)return;
            int o=Array.IndexOf(args,"-window-output");if(i+1>=args.Length || o<0 || o+1>=args.Length)throw new Exception("Missing full-window measurement arguments");
            // Development-only counters require an active profiling session.
            // Release remains unprofiled; unavailable counters are explicitly -1.
            if(UnityEngine.Debug.isDebugBuild)UnityEngine.Profiling.Profiler.enabled=true;
            var p=new GameObject("Opt-in full-window performance").AddComponent<HarborCityWindowPerformance>();p.input=args[i+1];p.directory=args[o+1];
        }
        IEnumerator Start()
        {
            if(input==null){enabled=false;yield break;}
            for(int i=0;i<12;i++)yield return null;
            bool initialized=false;
            try
            {
                game=FindFirstObjectByType<HarborCityGame>();if(game==null)throw new Exception("Normal game not running");
                Directory.CreateDirectory(directory);source=File.ReadAllText(input);input=Path.Combine(directory,"input.json");File.WriteAllText(input,source);
                QualitySettings.vSyncCount=0;Application.targetFrameRate=60;Application.runInBackground=true;Time.timeScale=1;Time.maximumDeltaTime=.33333334f;
                gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
                simulation=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"City.Simulation",1);
                initialized=true;
            }
            catch(Exception e){Fail(e);}
            if(!initialized)yield break;
            // Valid alone is insufficient: this counter can return zero in Player despite real allocations.
            // Verify against a known allocation outside all measured runs; report unsupported as -1.
            yield return null;
            var allocationControl=new byte[1024*1024];
            yield return null;
            gcSupported=gc.Valid && gc.LastValue>=allocationControl.Length;GC.KeepAlive(allocationControl);
            try
            {
                File.WriteAllText(Path.Combine(directory,"configuration.txt"),"full normal game Update + terrain/buildings/vehicles/render/UI\nunity="+Application.unityVersion+"\nsave_sha256="+Hash(source)+"\nresolution="+Screen.width+"x"+Screen.height+"\nsteps=300\nwarmup=40\nrepeats=3\ntarget_fps=60\nvsync=0\nmaximum_delta=0.33333334\ngc_recorder_valid="+gc.Valid+"\ngc_allocation_control_passed="+gcSupported+"\n");
                NextRun();ready=true;
            }
            catch(Exception e){Fail(e);}
        }
        void NextRun()
        {
            recording=false;CityTrafficWindowProbe.Expected=null;game.WindowSpeed=0;
            if(++run>=Repeats*2){Finish();return;}
            game.WindowLoad(input);if(!game.WindowCity.Valid())throw new Exception("Invalid loaded full-game city");
            var lookup=(Dictionary<long,RoadEdge>)typeof(CityRoads).GetField("roadLookup",System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(game.WindowCity.roads);
            bool optimized=ReferenceEquals(lookup.Comparer,CityRoadKeyComparer.Instance);
#if CITY_ROAD_KEY_HASH_DISABLED
            if(optimized)throw new Exception("Legacy full-window control unexpectedly uses optimized comparer");
#else
            if(!optimized)throw new Exception("Normal full-window game did not enable the production comparer");
#endif
            File.WriteAllText(Path.Combine(directory,"active-comparer.txt"),"optimized="+optimized+"\ndevelopment="+UnityEngine.Debug.isDebugBuild+"\ncomparer="+lookup.Comparer.GetType().FullName+"\n");
            game.WindowView();game.WindowTraffic.Advance(2f);frames.Clear();
            GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();waitingUntil=Time.realtimeSinceStartupAsDouble+3;
        }
        void Update()
        {
            if(!ready)return;
            try
            {
                if(!recording)
                {
                    if(Time.realtimeSinceStartupAsDouble<waitingUntil)return;
                    if(run==0 && !File.Exists(Path.Combine(directory,"START.txt")))
                    {if(!File.Exists(Path.Combine(directory,"READY.txt")))File.WriteAllText(Path.Combine(directory,"READY.txt"),"Normal game city visible; create START.txt to begin controlled 1x/3x measurements.");return;}
                    CityTrafficWindowProbe.Steps=0;lastSteps=0;CityTrafficWindowProbe.Expected=game.WindowTraffic;previousStart=Stopwatch.GetTimestamp();
                    // Reset also stops collection in Unity 6; restart before the measured frame.
                    if(gc.Valid){gc.Reset();gc.Start();}
                    if(simulation.Valid){simulation.Reset();simulation.Start();}
                    recording=true;game.WindowSpeed=run%2==0?1:3;return;
                }
                long now=Stopwatch.GetTimestamp();int steps=CityTrafficWindowProbe.Steps;
                var frame=new Frame{ms=(now-previousStart)*1000.0/Stopwatch.Frequency,steps=steps-lastSteps,tasks=game.WindowCity.traffic.trips.Count,gc=gcSupported?gc.LastValue:-1,simulationMs=simulation.Valid?simulation.LastValue/1000000.0:-1,focused=Application.isFocused};
                frames.Add(frame);previousStart=now;lastSteps=steps;
                if(steps>=CityTrafficWindowProbe.Samples){if(steps!=CityTrafficWindowProbe.Samples)throw new Exception("Full-window sample overshoot");SaveRun();NextRun();}
            }
            catch(Exception e){Fail(e);}
        }
        void SaveRun()
        {
            CityTrafficWindowProbe.Expected=null;game.WindowSpeed=0;
            double wall=0;long allocated=0;int min=int.MaxValue,max=0;var sorted=new double[frames.Count];
            for(int i=0;i<frames.Count;i++)
            {var f=frames[i];wall+=f.ms;allocated+=f.gc;min=Math.Min(min,f.tasks);max=Math.Max(max,f.tasks);sorted[i]=f.ms;
                rows.Append(run).Append(',').Append(run%2==0?"1x":"3x").Append(',').Append(i).Append(',').Append(N(f.ms)).Append(',').Append(N(f.simulationMs)).Append(',').Append(f.gc).Append(',').Append(f.steps).Append(',').Append(f.tasks).Append(',').Append(f.focused?1:0).AppendLine();}
            Array.Sort(sorted);
            var save=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(game.WindowCity.ToSaveData()));save.traffic.remainder=0;string json=JsonUtility.ToJson(save);File.WriteAllText(Path.Combine(directory,"state-"+run+".json"),json);
            summaries.Append(run).Append(',').Append(run%2==0?"1x":"3x").Append(',').Append(frames.Count).Append(',').Append(N(wall/1000)).Append(',').Append(N(frames.Count*1000/wall)).Append(',').Append(N(sorted[(int)Math.Ceiling(.95*sorted.Length)-1])).Append(',').Append(N(sorted[(int)Math.Ceiling(.99*sorted.Length)-1])).Append(',').Append(N(15*1000/wall)).Append(',').Append(lastSteps).Append(',').Append(gcSupported?N((double)allocated/frames.Count):"-1").Append(',').Append(min).Append(',').Append(max).Append(',').Append(Hash(json)).AppendLine();
            File.WriteAllText(Path.Combine(directory,"summary.csv"),summaries.ToString());File.WriteAllText(Path.Combine(directory,"frames.csv"),rows.ToString());
        }
        void Finish(){ready=false;gc.Dispose();simulation.Dispose();File.WriteAllText(Path.Combine(directory,"PASS.txt"),"6 full-game rendered runs, exactly 300 measured steps each, actual deltaTime at 1x/3x, full UI/vehicles/terrain active.");Application.Quit(0);}
        void Fail(Exception e){ready=false;CityTrafficWindowProbe.Expected=null;UnityEngine.Debug.LogException(e);if(directory!=null)File.WriteAllText(Path.Combine(directory,"FAIL.txt"),e.ToString());Application.Quit(1);}
        void OnDestroy(){CityTrafficWindowProbe.Expected=null;gc.Dispose();simulation.Dispose();}
        static string N(double x)=>x.ToString("R",CultureInfo.InvariantCulture);
        static string Hash(string x){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(x))).Replace("-","");}
    }
}
#endif
