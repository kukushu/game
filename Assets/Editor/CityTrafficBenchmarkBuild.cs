using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

namespace HarborCity
{
    public static class CityTrafficBenchmarkBuild
    {
        // Invoke in an isolated project, never replace the user's open scene.
        public static void Build()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Directory.CreateDirectory("Assets/Benchmark");EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),"Assets/Benchmark/Traffic.unity");
            string output=Environment.GetEnvironmentVariable("HARBOR_BENCHMARK_PLAYER");
            var defines=new System.Collections.Generic.List<string>{"CITY_TRAFFIC_BENCHMARK"};
            if(Environment.GetEnvironmentVariable("HARBOR_BENCHMARK_DISABLE_RADIUS_CACHE")=="1")defines.Add("CITY_JUNCTION_RADIUS_CACHE_DISABLED");
            if(Environment.GetEnvironmentVariable("HARBOR_BENCHMARK_DISABLE_EDGE_INDEX")=="1")defines.Add("CITY_TRAFFIC_EDGE_INDEX_DISABLED");
            if(Environment.GetEnvironmentVariable("HARBOR_BENCHMARK_ATTRIBUTION")=="1")defines.Add("CITY_TRAFFIC_ATTRIBUTION");
            if(Environment.GetEnvironmentVariable("HARBOR_BENCHMARK_ROAD_KEY_PROTOTYPE")=="1")defines.Add("CITY_ROAD_KEY_HASH_PROTOTYPE");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Benchmark/Traffic.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development,extraScriptingDefines=defines.ToArray()});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Benchmark player build failed");
        }
        public static void BuildResearchValidation()
        {
            if(!Application.isBatchMode)throw new Exception("Research validation requires an isolated project");
            // Match the existing validation build's camera/input fixture resolution.
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,UnityEditor.ScriptingImplementation.Mono2x);
            PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/SampleScene.unity"},locationPathName=Environment.GetEnvironmentVariable("HARBOR_TRAFFIC_VALIDATION_PLAYER"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development,extraScriptingDefines=new[]{"CITY_TRAFFIC_ATTRIBUTION","CITY_ROAD_KEY_HASH_PROTOTYPE"}});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Research validation build failed");
        }
        public static void Export()
        {
            string directory=Environment.GetEnvironmentVariable("HARBOR_BENCHMARK_RESULTS");
            // The default 300-frame history silently drops the first benchmark runs.
            // This editor binding is internal in Unity 6000.6; fail explicitly if it changes.
            var history=typeof(ProfilerDriver).GetMethod("SetMaxFrameHistoryLength",System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if(history==null)throw new Exception("Profiler frame-history binding unavailable");
            history.Invoke(null,new object[]{10000});
            var rows=new StringBuilder("ordinal,profiler_frame,path,total_ms,self_ms,calls,gc_bytes\n");
            var clocks=new StringBuilder("ordinal,profiler_frame,wall_ms,source\n");
            var recordings=Directory.GetFiles(directory,"traffic-run-*.raw");
            if(recordings.Length==0)recordings=new[]{Path.Combine(directory,"traffic.raw")};
            else if(recordings.Length!=9)throw new Exception("Expected nine bounded recordings");
            Array.Sort(recordings,StringComparer.Ordinal);
            var exported=new System.Collections.Generic.HashSet<long>();
            foreach(string recording in recordings)
            {
            ProfilerDriver.ClearAllFrames();GC.Collect();
            if(!ProfilerDriver.LoadProfile(recording,false))throw new Exception("Cannot read benchmark recording: "+recording);
            for(int frame=ProfilerDriver.firstFrameIndex;frame<=ProfilerDriver.lastFrameIndex;frame++)
            {
                using(var raw=ProfilerDriver.GetRawFrameDataView(frame,0))
                using(var data=ProfilerDriver.GetHierarchyFrameDataView(frame,0,HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,HierarchyFrameDataView.columnDontSort,false))
                {
                    if(!raw.valid || !data.valid)continue;int marker=raw.GetMarkerId("City.Benchmark.Frame");if(marker<0)continue;
                    long ordinal=raw.GetCounterValueAsLong(marker);if(ordinal<=0)continue;
                    if(!exported.Add(ordinal))throw new Exception("Duplicate benchmark ordinal: "+ordinal);
                    double wall=raw.frameTimeMs;string clockSource="profiler-duration";
                    using(var next=ProfilerDriver.GetRawFrameDataView(frame+1,0))
                        if(next.valid && next.frameStartTimeNs>=raw.frameStartTimeNs){wall=(next.frameStartTimeNs-raw.frameStartTimeNs)/1000000.0;clockSource="next-frame-start";}
                    clocks.Append(ordinal).Append(',').Append(frame).Append(',').Append(wall.ToString("R",CultureInfo.InvariantCulture)).Append(',').Append(clockSource).AppendLine();
                    Visit(data,data.GetRootItemID(),"",ordinal,frame,rows);
                }
            }
            }
            // Fail before publishing partial tables when Unity drops frames under memory pressure.
            foreach(string line in File.ReadAllLines(Path.Combine(directory,"frames.csv")))
                if(!line.StartsWith("ordinal,",StringComparison.Ordinal) && line.Length>0 && !exported.Contains(long.Parse(line.Split(',')[0],CultureInfo.InvariantCulture)))
                    throw new Exception("Missing benchmark frame during Profiler export: "+line);
            File.WriteAllText(Path.Combine(directory,"modules.csv"),rows.ToString());
            File.WriteAllText(Path.Combine(directory,"profiler-frames.csv"),clocks.ToString());
        }
        static void Visit(HierarchyFrameDataView data,int id,string parent,long ordinal,int frame,StringBuilder rows)
        {
            string name=data.GetItemName(id),path=parent.Length==0?name:parent+"/"+name;
            if(name.StartsWith("City.Traffic.",StringComparison.Ordinal))
            {
                rows.Append(ordinal).Append(',').Append(frame).Append(',').Append('"').Append(path.Replace("\"","\"\"")).Append('"');
                foreach(int column in new[]{HierarchyFrameDataView.columnTotalTime,HierarchyFrameDataView.columnSelfTime,HierarchyFrameDataView.columnCalls,HierarchyFrameDataView.columnGcMemory})rows.Append(',').Append(data.GetItemColumnDataAsDouble(id,column).ToString("R",CultureInfo.InvariantCulture));
                rows.AppendLine();
            }
            var children=new System.Collections.Generic.List<int>();data.GetItemChildren(id,children);foreach(int child in children)Visit(data,child,path,ordinal,frame,rows);
        }
    }
}
