using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor.Profiling;
using UnityEditorInternal;

namespace HarborCity
{
    // Offline raw-Player trace analysis. Never attaches to or changes the live city.
    public static class CityWindowProfilerExport
    {
        public static void Run()
        {
            string directory=Environment.GetEnvironmentVariable("HARBOR_WINDOW_CAPTURE");
            if(string.IsNullOrEmpty(directory))throw new ArgumentException("Explicit capture directory required");
            var history=typeof(ProfilerDriver).GetMethod("SetMaxFrameHistoryLength",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
            history?.Invoke(null,new object[]{2000});
            if(!ProfilerDriver.LoadProfile(Path.Combine(directory,"window.raw"),false))throw new InvalidOperationException("Cannot load captured Player trace");
            var rows=new StringBuilder("frame,thread,path,total_ms,self_ms,calls,gc_bytes\n");var frames=new StringBuilder("frame,thread,frame_ms,gpu_ms\n");int count=0;
            for(int frame=ProfilerDriver.firstFrameIndex;frame<=ProfilerDriver.lastFrameIndex;frame++)
            for(int thread=0;thread<128;thread++)
            using(var raw=ProfilerDriver.GetRawFrameDataView(frame,thread))
            {
                if(!raw.valid)break;string name=raw.threadName;
                if(name!="Main Thread"&&name!="Render Thread"&&name.IndexOf("Gfx",StringComparison.OrdinalIgnoreCase)<0)continue;
                frames.Append(frame).Append(',').Append(Quote(name)).Append(',').Append(N(raw.frameTimeMs)).Append(',').Append(N(raw.frameGpuTimeMs>0?raw.frameGpuTimeMs:-1)).AppendLine();
                using(var data=ProfilerDriver.GetHierarchyFrameDataView(frame,thread,HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,HierarchyFrameDataView.columnDontSort,false))
                    if(data.valid){Visit(data,data.GetRootItemID(),"",frame,name,rows);count++;}
            }
            if(count==0)throw new InvalidOperationException("No readable CPU frames");
            File.WriteAllText(Path.Combine(directory,"hierarchy.csv"),rows.ToString());File.WriteAllText(Path.Combine(directory,"threads.csv"),frames.ToString());
        }
        static string N(double value)=>value.ToString("R",CultureInfo.InvariantCulture);
        static string Quote(string value)=>"\""+value.Replace("\"","\"\"")+"\"";
        static void Visit(HierarchyFrameDataView data,int id,string parent,int frame,string thread,StringBuilder rows)
        {
            string name=data.GetItemName(id),path=parent.Length==0?name:parent+"/"+name;double self=data.GetItemColumnDataAsDouble(id,HierarchyFrameDataView.columnSelfTime);
            if(name.StartsWith("City.",StringComparison.Ordinal)||name=="PlayerLoop"||name=="GC.Collect"||self>=.2)
            {
                rows.Append(frame).Append(',').Append(Quote(thread)).Append(',').Append(Quote(path));
                foreach(int column in new[]{HierarchyFrameDataView.columnTotalTime,HierarchyFrameDataView.columnSelfTime,HierarchyFrameDataView.columnCalls,HierarchyFrameDataView.columnGcMemory})rows.Append(',').Append(N(data.GetItemColumnDataAsDouble(id,column)));rows.AppendLine();
            }
            var children=new List<int>();data.GetItemChildren(id,children);foreach(int child in children)Visit(data,child,path,frame,thread,rows);
        }
    }
}
