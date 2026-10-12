#if UNITY_5_3_OR_NEWER && CITY_TRAFFIC_BENCHMARK
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Unity.Profiling;

namespace HarborCity
{
    // Compiled only into the dedicated benchmark player; no callbacks in the normal game.
    public static class CityTrafficBenchmarkProbe
    {
        public static bool Recording;
        public static bool AllocationSupported;
        public static int Count;
        public static double[] Milliseconds;
        public static long[] Bytes;
        public static int[] Tasks;
        public struct Scope : IDisposable
        {
            readonly long start,allocated;
            readonly int tasks;
            readonly bool record;
            public Scope(int tasks)
            {this.tasks=tasks;record=Recording;allocated=record && AllocationSupported?GC.GetAllocatedBytesForCurrentThread():0;start=record?Stopwatch.GetTimestamp():0;}
            public void Dispose()
            {
                if(!record)return;
                int i=Count++;
                Milliseconds[i]=(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;
                Bytes[i]=AllocationSupported?GC.GetAllocatedBytesForCurrentThread()-allocated:-1;Tasks[i]=tasks;
            }
        }
    }

    public sealed class CityTrafficBenchmark : MonoBehaviour
    {
        const int Samples=300,Warmup=40,Repeats=3;
        static readonly ProfilerCounterValue<int> frameCounter=new ProfilerCounterValue<int>(ProfilerCategory.Scripts,"City.Benchmark.Frame",ProfilerMarkerDataUnit.Count,ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
        string source,directory,codeVersion;
        CityModel city;CityTraffic traffic;
        int run=-1,idle,frameOrdinal;
        string mode;
        readonly StringBuilder steps=new StringBuilder("run,mode,step,ms,gc_bytes,tasks\n");
        readonly StringBuilder frames=new StringBuilder("ordinal,run,mode,delta,unscaled_delta,maximum_delta,requested_seconds,steps,advance_ms,tasks,partial\n");
        readonly StringBuilder summaries=new StringBuilder("run,mode,mean_ms,p95_ms,p99_ms,gc_bytes_per_step,min_tasks,max_tasks,state_sha256\n");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartBenchmark()
        {
            var args=Environment.GetCommandLineArgs();if(Array.IndexOf(args,"-harbor-traffic-benchmark")<0)return;
            new GameObject("Traffic benchmark (detached save)").AddComponent<CityTrafficBenchmark>();
        }
        static string Arg(string name)
        {var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,name);if(i<0 || i+1>=args.Length)throw new ArgumentException(name);return args[i+1];}
        void Awake()
        {
            try
            {
                source=File.ReadAllText(Arg("-harbor-traffic-benchmark"));directory=Arg("-benchmark-output");codeVersion=Arg("-benchmark-version");
                Directory.CreateDirectory(directory);QualitySettings.vSyncCount=0;Application.targetFrameRate=60;Application.runInBackground=true;Time.timeScale=1;Time.maximumDeltaTime=.33333334f;
                CityTrafficBenchmarkProbe.Milliseconds=new double[Samples+20];CityTrafficBenchmarkProbe.Bytes=new long[Samples+20];CityTrafficBenchmarkProbe.Tasks=new int[Samples+20];
                long allocationBefore=GC.GetAllocatedBytesForCurrentThread();var allocationCheck=new byte[4096];GC.KeepAlive(allocationCheck);
                CityTrafficBenchmarkProbe.AllocationSupported=GC.GetAllocatedBytesForCurrentThread()>allocationBefore;
                UnityEngine.Profiling.Profiler.logFile=Path.Combine(directory,"traffic.raw");UnityEngine.Profiling.Profiler.enableBinaryLog=true;UnityEngine.Profiling.Profiler.enabled=true;
                File.WriteAllText(Path.Combine(directory,"configuration.txt"),"version="+codeVersion+"\nsave_sha256="+Hash(source)+"\nunity="+Application.unityVersion+"\ncpu="+SystemInfo.processorType+"\nos="+SystemInfo.operatingSystem+"\nsteps="+Samples+"\nwarmup="+Warmup+"\nrepeats="+Repeats+"\ntarget_fps=60\nmaximum_delta=0.33333334\nDedicated empty-scene Development Player, no city rendering/UI, fine Profiler markers enabled. Live modes use actual Time.deltaTime; fixed mode requests one 0.05s step per Update. Not full game FPS.\n");
                NextRun();
            }
            catch(Exception e){Failure(e);}
        }
        void NextRun()
        {
            CityTrafficBenchmarkProbe.Recording=false;
            if(++run>=Repeats*3){Finish();return;}
            mode=run%3==0?"fixed":run%3==1?"1x":"3x";
            city=JsonUtility.FromJson<CitySaveData>(source).ToCity();if(city==null || !city.Valid())throw new Exception("Invalid fixed save");
            city.analysis=new CityDailyAnalysis(city);traffic=new CityTraffic(city);
            GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
            for(int i=0;i<Warmup;i++)traffic.Advance(.05f);
            CityTrafficBenchmarkProbe.Count=0;idle=2;
        }
        void Update()
        {
            try
            {
                if(traffic==null)return;
                if(idle-->0)return;
                if(CityTrafficBenchmarkProbe.Count>=Samples){SaveRun();NextRun();return;}
                CityTrafficBenchmarkProbe.Recording=true;
                double request=mode=="fixed"?.05:Time.deltaTime*(mode=="3x"?3:1);
                double remaining=(Samples-CityTrafficBenchmarkProbe.Count)*.05-city.traffic.remainder;
                bool partial=request>remaining;request=Math.Min(request,remaining);
                int before=CityTrafficBenchmarkProbe.Count;long start=Stopwatch.GetTimestamp();traffic.Advance((float)request);
                double ms=(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;
                CityTrafficBenchmarkProbe.Recording=false;
                frameCounter.Value=++frameOrdinal;
                frames.Append(frameOrdinal).Append(',').Append(run).Append(',').Append(mode).Append(',').Append(N(Time.deltaTime)).Append(',').Append(N(Time.unscaledDeltaTime)).Append(',').Append(N(Time.maximumDeltaTime)).Append(',').Append(N(request)).Append(',').Append(CityTrafficBenchmarkProbe.Count-before).Append(',').Append(N(ms)).Append(',').Append(city.traffic.trips.Count).Append(',').Append(partial?1:0).AppendLine();
            }
            catch(Exception e){Failure(e);}
        }
        void SaveRun()
        {
            if(CityTrafficBenchmarkProbe.Count!=Samples || !city.Valid())throw new Exception("Benchmark step count/state invalid");
            var sorted=new double[Samples];Array.Copy(CityTrafficBenchmarkProbe.Milliseconds,sorted,Samples);Array.Sort(sorted);
            double sum=0;long bytes=0;int min=int.MaxValue,max=0;
            for(int i=0;i<Samples;i++)
            {
                sum+=CityTrafficBenchmarkProbe.Milliseconds[i];bytes+=CityTrafficBenchmarkProbe.Bytes[i];int tasks=CityTrafficBenchmarkProbe.Tasks[i];min=Math.Min(min,tasks);max=Math.Max(max,tasks);
                steps.Append(run).Append(',').Append(mode).Append(',').Append(i).Append(',').Append(N(CityTrafficBenchmarkProbe.Milliseconds[i])).Append(',').Append(CityTrafficBenchmarkProbe.Bytes[i]).Append(',').Append(tasks).AppendLine();
            }
            // Frame grouping changes only the fractional accumulator. Preserve it separately, normalize only the comparison copy.
            var save=city.ToSaveData();var copy=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(save));copy.traffic.remainder=0;
            string json=JsonUtility.ToJson(copy);
            File.WriteAllText(Path.Combine(directory,"state-"+run+".json"),json);
            summaries.Append(run).Append(',').Append(mode).Append(',').Append(N(sum/Samples)).Append(',').Append(N(sorted[(int)Math.Ceiling(.95*Samples)-1])).Append(',').Append(N(sorted[(int)Math.Ceiling(.99*Samples)-1])).Append(',').Append(CityTrafficBenchmarkProbe.AllocationSupported?N((double)bytes/Samples):"-1").Append(',').Append(min).Append(',').Append(max).Append(',').Append(Hash(json)).AppendLine();
        }
        void Finish()
        {
            traffic=null;UnityEngine.Profiling.Profiler.enabled=false;UnityEngine.Profiling.Profiler.enableBinaryLog=false;
            File.WriteAllText(Path.Combine(directory,"steps.csv"),steps.ToString());File.WriteAllText(Path.Combine(directory,"frames.csv"),frames.ToString());File.WriteAllText(Path.Combine(directory,"summary.csv"),summaries.ToString());
            File.WriteAllText(Path.Combine(directory,"PASS.txt"),"9 runs completed, detached save, 40 warmup + 300 measured steps each; accumulator excluded only from state comparison.\n");Application.Quit(0);
        }
        void Failure(Exception e){CityTrafficBenchmarkProbe.Recording=false;UnityEngine.Profiling.Profiler.enabled=false;UnityEngine.Debug.LogException(e);if(directory!=null)File.WriteAllText(Path.Combine(directory,"FAIL.txt"),e.ToString());traffic=null;Application.Quit(1);}
        static string N(double value)=>value.ToString("R",CultureInfo.InvariantCulture);
        static string Hash(string value){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","");}
    }
}
#endif
