using System;using System.IO;using System.Collections.Generic;using HarborCity.RoadCore;
static class WasteCacheProbe {
 static readonly bool enabled=Array.IndexOf(Environment.GetCommandLineArgs(),"waste-cache-counts")>=0;
 static readonly List<string> rows=new List<string>{"tick,hits,misses,evictions,searches,expanded,entries,logical_bytes,service_point_queries"};
 internal struct Scope:IDisposable {
  World world;long tick,hits,misses,evictions,searches,expanded,queries;
  internal Scope(World w,long t){world=enabled?w:null;tick=t;var n=w.Network;hits=n.SharedRouteHits;misses=n.SharedRouteMisses;evictions=n.SharedRouteEvictions;searches=n.PointSearches;expanded=n.PointExpanded;queries=w.ServicePointQueries;}
  public void Dispose(){if(world==null)return;var n=world.Network;rows.Add(string.Join(",",tick,n.SharedRouteHits-hits,n.SharedRouteMisses-misses,n.SharedRouteEvictions-evictions,n.PointSearches-searches,n.PointExpanded-expanded,n.SharedRouteCount,n.SharedRouteBytes,world.ServicePointQueries-queries));}
 }
 internal static Scope Begin(World w,long tick)=>new Scope(w,tick);
 internal static void Write(string output){if(enabled)File.WriteAllLines(Path.Combine(output,"waste-cache.csv"),rows);}
}
