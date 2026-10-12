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
            var defines=Environment.GetEnvironmentVariable("HARBOR_BENCHMARK_DISABLE_RADIUS_CACHE")=="1"?new[]{"CITY_TRAFFIC_BENCHMARK","CITY_JUNCTION_RADIUS_CACHE_DISABLED"}:new[]{"CITY_TRAFFIC_BENCHMARK"};
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Benchmark/Traffic.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development,extraScriptingDefines=defines});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Benchmark player build failed");
        }
        public static void Export()
        {
            string directory=Environment.GetEnvironmentVariable("HARBOR_BENCHMARK_RESULTS");
            // The default 300-frame history silently drops the first benchmark runs.
            // This editor binding is internal in Unity 6000.6; fail explicitly if it changes.
            var history=typeof(ProfilerDriver).GetMethod("SetMaxFrameHistoryLength",System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if(history==null)throw new Exception("Profiler frame-history binding unavailable");
            history.Invoke(null,new object[]{10000});
            if(!ProfilerDriver.LoadProfile(Path.Combine(directory,"traffic.raw"),false))throw new Exception("Cannot read benchmark recording");
            var rows=new StringBuilder("ordinal,profiler_frame,path,total_ms,self_ms,calls,gc_bytes\n");
            for(int frame=ProfilerDriver.firstFrameIndex;frame<=ProfilerDriver.lastFrameIndex;frame++)
            {
                using(var raw=ProfilerDriver.GetRawFrameDataView(frame,0))
                using(var data=ProfilerDriver.GetHierarchyFrameDataView(frame,0,HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,HierarchyFrameDataView.columnDontSort,false))
                {
                    if(!raw.valid || !data.valid)continue;int marker=raw.GetMarkerId("City.Benchmark.Frame");if(marker<0)continue;
                    long ordinal=raw.GetCounterValueAsLong(marker);if(ordinal<=0)continue;
                    Visit(data,data.GetRootItemID(),"",ordinal,frame,rows);
                }
            }
            File.WriteAllText(Path.Combine(directory,"modules.csv"),rows.ToString());
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
