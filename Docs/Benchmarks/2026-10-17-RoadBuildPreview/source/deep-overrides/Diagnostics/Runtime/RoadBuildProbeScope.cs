
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Unity.Profiling;
namespace HarborCity {
public readonly struct RoadBuildProbeScope:IDisposable {
 public static bool Enabled;public static string Phase="startup";
 static readonly Dictionary<string,ProfilerMarker> markers=new Dictionary<string,ProfilerMarker>();
 static readonly List<string> rows=new List<string>{"phase,name,ms"};
 readonly long start;readonly string phase,name;readonly bool enabled;readonly ProfilerMarker.AutoScope scope;
 public RoadBuildProbeScope(string name){enabled=Enabled;this.name=name;phase=Phase;start=enabled?Stopwatch.GetTimestamp():0;scope=default;if(enabled){if(!markers.TryGetValue(name,out var marker)){marker=new ProfilerMarker("City.RoadBuild."+name);markers.Add(name,marker);}scope=marker.Auto();}}
 public void Dispose(){if(!enabled)return;scope.Dispose();rows.Add(phase+","+name+","+((Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency).ToString("R",CultureInfo.InvariantCulture));}
 public static void Write(string output){File.WriteAllLines(Path.Combine(output,"scopes.csv"),rows);}
}}
