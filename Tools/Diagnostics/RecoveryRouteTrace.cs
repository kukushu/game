using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using HarborCity.RoadCore;
namespace HarborCity
{
    // Temp-only input/workload observation. Never a production route cache.
    public static class RecoveryRouteTrace
    {
        static bool active;static int vehicle,purpose;static Sample current;static readonly List<Sample> rows=new List<Sample>();
        sealed class Context:IDisposable{readonly int oldVehicle=vehicle,oldPurpose=purpose;public Context(int v,int p){vehicle=v;purpose=p;}public void Dispose(){vehicle=oldVehicle;purpose=oldPurpose;}}
        public static IDisposable Owner(int id,int p)=>active?new Context(id,p):null;
        public sealed class Sample:IDisposable
        {
            internal int vehicle,purpose,target,count,expanded,calls,candidates,legal;internal long lane,targetLane,otherLane;internal double start,end,otherEnd,ms;internal bool both,success,oldTrace;internal long began;internal int traceStart;internal long[] route;internal readonly List<long> nodes=new List<long>();
            public void Dispose(){ms=(Stopwatch.GetTimestamp()-began)*1000.0/Stopwatch.Frequency;for(int i=traceStart;i<DepotRoutingTrace.Rows.Count;i++){calls++;expanded+=DepotRoutingTrace.Rows[i].expanded;}DepotRoutingTrace.Enabled=oldTrace;rows.Add(this);current=null;}
        }
        public static Sample Begin(Network n,long lane,double start,RoadAccess target,bool both)
        {
            if(!active||vehicle==0)return null;var d=n.Get(target.lane);long other=both?Network.LaneId(d.RoadId,!d.Reverse,d.LaneIndex):0;
            var s=new Sample{vehicle=vehicle,purpose=purpose,target=target.endpoint,lane=lane,start=start,targetLane=target.lane,end=target.distance,both=both,otherLane=n.Contains(other)?other:0,oldTrace=DepotRoutingTrace.Enabled,traceStart=DepotRoutingTrace.Rows.Count,began=Stopwatch.GetTimestamp()};
            if(s.otherLane!=0)s.otherEnd=d.Geometry.MapReverseDistance(n.Get(other).Geometry,target.distance);DepotRoutingTrace.Enabled=true;current=s;return s;
        }
        public static void Expand(long lane){current?.nodes.Add(lane);}
        public static void Edge(bool legal){if(current!=null){if(legal)current.legal++;else current.candidates++;}}
        public static List<long> Result(Sample sample,List<long> result){if(sample!=null){sample.success=result!=null;sample.count=result?.Count??0;sample.route=result?.ToArray();}return result;}
        public static void Reset(){rows.Clear();DepotRoutingTrace.Reset();active=true;vehicle=purpose=0;}
        static string N(double d)=>d.ToString("R",CultureInfo.InvariantCulture);
        public static void Write(string output)
        {
            active=false;File.WriteAllLines(Path.Combine(output,"recovery-queries.csv"),new[]{"vehicle,purpose,source_lane,start_distance,target_endpoint,target_lane,target_distance,both_directions,other_lane,other_distance,success,route_segments,searches,expanded,candidate_links,legal_links,ms,route"}.Concat(rows.Select(s=>string.Join(",",s.vehicle,s.purpose,s.lane,N(s.start),s.target,s.targetLane,N(s.end),s.both,s.otherLane,N(s.otherEnd),s.success,s.count,s.calls,s.expanded,s.candidates,s.legal,N(s.ms),s.route==null?"":string.Join("/",s.route)))));
            File.WriteAllLines(Path.Combine(output,"recovery-expanded.csv"),new[]{"query,lane"}.Concat(rows.SelectMany((s,i)=>s.nodes.Select(lane=>i+","+lane))));
        }
    }
}
