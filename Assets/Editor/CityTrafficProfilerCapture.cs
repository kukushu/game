using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

namespace HarborCity
{
    // Editor-only, explicitly invoked. Export happens after recording stops, never inside simulation.
    public static class CityTrafficProfilerCapture
    {
        const int FrameCount=120;
        static int startedFrame;
        static bool previousEnabled,previousProfileEditor,capturing;
        [MenuItem("Harbor/Diagnostics/Capture 120 traffic frames %#F9")]
        public static void Capture()
        {
            if(!EditorApplication.isPlaying || EditorApplication.isPaused)
            { Debug.LogWarning("Traffic capture requires an unpaused Play session. Select 3x in the game first."); return; }
            if(capturing){Debug.LogWarning("Traffic capture is already running.");return;}
            capturing=true;
            EditorApplication.update-=Tick;
            previousEnabled=ProfilerDriver.enabled;
            previousProfileEditor=ProfilerDriver.profileEditor;
            startedFrame=Time.frameCount;
            ProfilerDriver.profileEditor=false;
            ProfilerDriver.enabled=true;
            EditorApplication.update+=Tick;
        }
        static void Tick()
        {
            if(EditorApplication.isPlaying && !EditorApplication.isPaused && Time.frameCount-startedFrame<FrameCount)return;
            EditorApplication.update-=Tick;
            capturing=false;
            ProfilerDriver.enabled=false;
            try { Export(); }
            finally { ProfilerDriver.profileEditor=previousProfileEditor; ProfilerDriver.enabled=previousEnabled; }
        }
        static readonly Unity.Profiling.ProfilerMarker overheadMarker=new Unity.Profiling.ProfilerMarker("City.Traffic.Diagnostics.EmptyScope");
        [MenuItem("Harbor/Diagnostics/Measure marker overhead")]
        public static void MeasureOverhead()
        {
            if(capturing){Debug.LogWarning("Wait for traffic capture to finish before measuring marker overhead.");return;}
            // Explicit diagnostic, outside measured gameplay frames. Do not infer whole-game speed from this.
            bool enabled=ProfilerDriver.enabled,editor=ProfilerDriver.profileEditor;
            var times=new double[9];var allocations=new long[9];
            var clock=new System.Diagnostics.Stopwatch();
            try
            {
                ProfilerDriver.profileEditor=true;
                ProfilerDriver.enabled=true;
                for(int i=0;i<100;i++){using(overheadMarker.Auto()){} }
                for(int round=0;round<times.Length;round++)
                {
                    long before=GC.GetAllocatedBytesForCurrentThread();clock.Restart();
                    for(int i=0;i<5000;i++){using(overheadMarker.Auto()){} }
                    clock.Stop();times[round]=clock.Elapsed.TotalMilliseconds/5000;
                    allocations[round]=GC.GetAllocatedBytesForCurrentThread()-before;
                }
            }
            finally { ProfilerDriver.profileEditor=editor; ProfilerDriver.enabled=enabled; }
            Array.Sort(times);
            string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../Logs/TrafficProfiler"));Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory,"overhead.txt"),"Empty Auto scope, recording enabled, 9 x 5000 after warmup. Includes loop/Stopwatch cost; not an A/B gameplay benchmark.\nMedian ms/scope="+N(times[4])+"\nManaged allocation bytes/batch="+string.Join(",",allocations));
            Debug.Log("Traffic marker overhead exported to "+directory);
        }
        [MenuItem("Harbor/Diagnostics/Export recent traffic profiler frames")]
        public static void Export()
        {
            string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../Logs/TrafficProfiler"));
            Directory.CreateDirectory(directory);
            var rows=new StringBuilder("frame,path,total_ms,self_ms,calls,gc_bytes\n");
            var frames=new StringBuilder("frame,traffic_tasks,fixed_steps,movement_ms,movement_calls,movement_gc_bytes\n");
            int last=ProfilerDriver.lastFrameIndex,first=Math.Max(ProfilerDriver.firstFrameIndex,last-FrameCount+1),valid=0;
            for(int f=first;f<=last;f++)
            {
                using(var data=ProfilerDriver.GetHierarchyFrameDataView(f,0,HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,HierarchyFrameDataView.columnDontSort,false))
                {
                    if(!data.valid)continue;
                    double ms=0,calls=0,gc=0;
                    Visit(data,data.GetRootItemID(),"",f,rows,ref ms,ref calls,ref gc);
                    using(var raw=ProfilerDriver.GetRawFrameDataView(f,0))
                    {
                        if(!raw.valid)continue;
                        int trips=raw.GetMarkerId("City.Traffic.ActiveTrips"),steps=raw.GetMarkerId("City.Traffic.FixedSteps");
                        frames.Append(f).Append(',').Append(trips<0?-1:raw.GetCounterValueAsLong(trips)).Append(',')
                            .Append(steps<0?-1:raw.GetCounterValueAsLong(steps)).Append(',')
                            .Append(N(ms)).Append(',').Append(N(calls)).Append(',').Append(N(gc)).AppendLine();
                    }
                    valid++;
                }
            }
            File.WriteAllText(Path.Combine(directory,"samples.csv"),rows.ToString());
            File.WriteAllText(Path.Combine(directory,"frames.csv"),frames.ToString());
            Debug.Log("Traffic Profiler: exported "+valid+" frames to "+directory+". Missing counters use -1; no recorded sample is not a zero-cost result.");
        }
        static string N(double value)=>value.ToString("R",CultureInfo.InvariantCulture);
        static void Visit(HierarchyFrameDataView data,int id,string parent,int frame,StringBuilder rows,ref double movement,ref double calls,ref double gc)
        {
            string name=data.GetItemName(id),path=parent.Length==0?name:parent+"/"+name;
            if(name.StartsWith("City.",StringComparison.Ordinal) || name=="PlayerLoop")
            {
                double total=data.GetItemColumnDataAsDouble(id,HierarchyFrameDataView.columnTotalTime);
                double self=data.GetItemColumnDataAsDouble(id,HierarchyFrameDataView.columnSelfTime);
                double count=data.GetItemColumnDataAsDouble(id,HierarchyFrameDataView.columnCalls);
                double allocated=data.GetItemColumnDataAsDouble(id,HierarchyFrameDataView.columnGcMemory);
                rows.Append(frame).Append(',').Append('"').Append(path.Replace("\"","\"\"")).Append('"').Append(',')
                    .Append(N(total)).Append(',').Append(N(self)).Append(',').Append(N(count)).Append(',').Append(N(allocated)).AppendLine();
                if(name=="City.Traffic.Movement"){movement+=total;calls+=count;gc+=allocated;}
            }
            var children=new List<int>();data.GetItemChildren(id,children);
            foreach(int child in children)Visit(data,child,path,frame,rows,ref movement,ref calls,ref gc);
        }
    }
}

