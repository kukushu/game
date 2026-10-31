using System;using System.IO;using System.Linq;using System.Globalization;using System.Collections.Generic;using HarborCity.RoadCore;
namespace HarborCity {
// Injected into a disposable test project only. Observations never feed scheduling.
internal static class WasteRoutingProbe {
 static CityCoreIndustrySession session;static CityWasteDiagnostics diagnostics;
 static bool requested,enabled,inside;static long serial;
 struct Context {internal long id,tick;internal int frame,depot,pickup,vehicle,task,committedTask;internal bool returning,prepared,admitted,committed;internal string reason;internal Query query;internal int firstBlocked,otherBlocked,hasOther;internal long firstLane,otherLane;}
 sealed class Query {internal Context context;internal long anchorLane,pointLane;internal double anchorDistance,pointDistance;internal int anchorEndpoint,pointEndpoint,rowStart,rowEnd,routeFound=-1,segments;internal long chosenLane;internal double chosenDistance;}
 static Context current;
 static readonly List<Query> queries=new List<Query>();
 static readonly Dictionary<(long,int,bool,string,bool,bool),long> outcomes=new Dictionary<(long,int,bool,string,bool,bool),long>();
 static readonly string[] rejectionNames=Enum.GetNames(typeof(TransportRejection));
 static Dictionary<(int,string),long> before;
 static string N(double v)=>v.ToString("R",CultureInfo.InvariantCulture);
 static int Frame(){
#if UNITY_5_3_OR_NEWER
  return UnityEngine.Time.frameCount;
#else
  return -1;
#endif
 }
 internal static void Warm(CityCoreIndustrySession s){requested=Array.IndexOf(Environment.GetCommandLineArgs(),"-harbor-waste-routing-census")>=0;if(!requested)return;session=s;diagnostics=new CityWasteDiagnostics();if(s.Simulation.Waste.Diagnostics!=null)throw new InvalidOperationException("Existing waste observer must not be replaced");s.Simulation.Waste.Diagnostics=diagnostics;DepotRoutingTrace.Enabled=false;}
 static Dictionary<(int,string),long> Snapshot(){var result=new Dictionary<(int,string),long>();foreach(var s in diagnostics.Snapshot()){
  // Copy rejection numbers now: the existing diagnostic DTO's readonly array is shallow-copied.
  foreach(var p in s.AdmissionRejections)result[(s.station,"rejected_"+p.Key)]=p.Value;
  foreach(var f in typeof(WasteStationDiagnostic).GetFields())if(f.FieldType==typeof(long))result[(s.station,f.Name)]=(long)f.GetValue(s);
 }return result;}
 internal static void Begin(string output){if(!requested)return;if(enabled||inside||DepotRoutingTrace.QueryObserver!=null)throw new InvalidOperationException("Overlapping waste routing observation");queries.Clear();outcomes.Clear();serial=0;before=Snapshot();DepotRoutingTrace.Reset();DepotRoutingTrace.Enabled=true;DepotRoutingTrace.QueryObserver=Observe;enabled=true;File.WriteAllText(Path.Combine(output,"waste-census-mode.txt"),"enabled");}
 internal readonly struct Attempt:IDisposable {
  readonly long id;readonly bool active;
  internal Attempt(int depot,int pickup,int vehicle,int task,bool returning){active=enabled;id=0;if(!active)return;if(inside)throw new InvalidOperationException("Nested waste attempt");inside=true;id=++serial;current=new Context{id=id,tick=session.Simulation.Tick+1,frame=Frame(),depot=depot,pickup=pickup,vehicle=vehicle,task=task,returning=returning,reason="unfinished",firstBlocked=-1,otherBlocked=-1,hasOther=-1};}
  public void Prepared(bool admitted,string reason){if(!active)return;if(current.id!=id||current.prepared)throw new InvalidOperationException("Duplicate/mismatched attempt result");current.prepared=true;current.admitted=admitted;current.reason=reason;}
  public void Prepared(bool admitted,TransportRejection reason){if(active)Prepared(admitted,rejectionNames[(int)reason]);}
  public void Commit(int vehicle,int task){if(!active)return;if(current.id!=id||!current.admitted||current.committed)throw new InvalidOperationException("Invalid attempt commit observation");current.committed=true;current.vehicle=vehicle;current.committedTask=task;}
  public void Dispose(){if(!active)return;if(current.id!=id)throw new InvalidOperationException("Mismatched waste observation scope");if(current.query!=null)current.query.context=current;var key=(current.tick,current.depot,current.returning,current.reason,current.query!=null,current.committed);outcomes.TryGetValue(key,out long count);outcomes[key]=count+1;inside=false;}
 }
 internal static Attempt Outbound(int depot,int target)=>new Attempt(depot,target,0,0,false);
 internal static Attempt Return(int depot,int pickup,int vehicle,int task)=>new Attempt(depot,pickup,vehicle,task,true);
 static void Observe(int depot,RoadAccess anchor,RoadAccess point,bool returning){if(!enabled)return;if(!inside||current.depot!=depot||current.returning!=returning||current.query!=null)throw new InvalidOperationException("Unowned or repeated service query");if(queries.Count>=100000)throw new InvalidOperationException("Diagnostic query capture bound exceeded; reject capture, never truncate work");var q=new Query{context=current,anchorLane=anchor.lane,anchorDistance=anchor.distance,anchorEndpoint=anchor.endpoint,pointLane=point.lane,pointDistance=point.distance,pointEndpoint=point.endpoint,rowStart=DepotRoutingTrace.Rows.Count};current.query=q;queries.Add(q);}
 internal static void SourceGuard(bool first,bool hasOther,bool second,long firstLane,long otherLane){if(!enabled)return;if(!inside||current.returning)throw new InvalidOperationException("Unowned service source guard");current.firstBlocked=first?1:0;current.otherBlocked=second?1:0;current.hasOther=hasOther?1:0;current.firstLane=firstLane;current.otherLane=otherLane;}
 internal static void RouteResult(bool found,int segments,long chosenLane,double chosenDistance){if(!enabled)return;var q=current.query;if(!inside||q==null||q.routeFound!=-1)throw new InvalidOperationException("Missing/multiple service route result");q.routeFound=found?1:0;q.segments=segments;q.chosenLane=chosenLane;q.chosenDistance=chosenDistance;q.rowEnd=DepotRoutingTrace.Rows.Count;}
 internal static void Write(string output){if(!requested)return;if(!enabled||inside)throw new InvalidOperationException("Incomplete waste observation boundary");enabled=false;DepotRoutingTrace.QueryObserver=null;DepotRoutingTrace.Enabled=false;session.Simulation.Waste.Diagnostics=null;
  var qrows=new List<string>{"attempt,completed_tick,frame,depot,pickup,returning,vehicle,previous_task,committed_task,anchor_endpoint,anchor_lane,anchor_distance,point_endpoint,point_lane,point_distance,route_found,segments,admitted,committed,rejection,searches,expanded,search_ms,service_query_ms,first_lane,other_lane,has_other,first_blocked,other_blocked,chosen_lane,chosen_distance,chosen_blocked"};
  foreach(var q in queries){if(!q.context.prepared||q.routeFound<0)throw new InvalidOperationException("Query has no authoritative outcome; do not infer failure");int searches=0,expanded=0;double ms=0;for(int i=q.rowStart;i<q.rowEnd;i++){var r=DepotRoutingTrace.Rows[i];if(r.kind!="lane-astar")continue;searches++;expanded+=r.expanded;ms+=r.ms;}if(q.rowEnd>=DepotRoutingTrace.Rows.Count)throw new InvalidOperationException("Missing outer service sample");var outer=DepotRoutingTrace.Rows[q.rowEnd];if(outer.kind!="service-query"||outer.depot!=q.context.depot||outer.direction!=(q.context.returning?"return":"outbound"))throw new InvalidOperationException("Search/sample alignment differs");var c=q.context;int chosenBlocked=-1;if(!c.returning&&q.routeFound==1){chosenBlocked=q.chosenLane==c.firstLane?c.firstBlocked:c.hasOther==1&&q.chosenLane==c.otherLane?c.otherBlocked:-1;if(chosenBlocked<0||c.admitted&&chosenBlocked!=0||c.reason=="SourceOccupied"&&chosenBlocked!=1)throw new InvalidOperationException("Chosen curb and authoritative occupancy checks differ");}qrows.Add(string.Join(",",c.id,c.tick,c.frame,c.depot,c.pickup,c.returning,c.vehicle,c.task,c.committedTask,q.anchorEndpoint,q.anchorLane,N(q.anchorDistance),q.pointEndpoint,q.pointLane,N(q.pointDistance),q.routeFound,q.segments,c.admitted,c.committed,c.reason,searches,expanded,N(ms),N(outer.ms),c.firstLane,c.otherLane,c.hasOther,c.firstBlocked,c.otherBlocked,q.chosenLane,N(q.chosenDistance),chosenBlocked));}
  var arows=new List<string>{"completed_tick,depot,returning,rejection,route_queried,committed,attempts"};foreach(var p in outcomes.OrderBy(p=>p.Key.Item1).ThenBy(p=>p.Key.Item2).ThenBy(p=>p.Key.Item3).ThenBy(p=>p.Key.Item4))arows.Add(string.Join(",",p.Key.Item1,p.Key.Item2,p.Key.Item3,p.Key.Item4,p.Key.Item5,p.Key.Item6,p.Value));
  var after=Snapshot();var drows=new List<string>{"depot,metric,delta"};foreach(var key in before.Keys.Union(after.Keys).OrderBy(k=>k.Item1).ThenBy(k=>k.Item2)){before.TryGetValue(key,out long a);after.TryGetValue(key,out long b);drows.Add(key.Item1+","+key.Item2+","+(b-a));}
  long Delta(string name)=>after.Where(p=>p.Key.Item2==name).Sum(p=>p.Value)-before.Where(p=>p.Key.Item2==name).Sum(p=>p.Value);
  if(outcomes.Any(p=>p.Key.Item4=="unfinished")||outcomes.Where(p=>!p.Key.Item3).Sum(p=>p.Value)!=Delta("admissionAttempts")||outcomes.Where(p=>!p.Key.Item3&&p.Key.Item6).Sum(p=>p.Value)!=Delta("dispatched")||outcomes.Where(p=>p.Key.Item3&&!p.Key.Item6).Sum(p=>p.Value)!=Delta("returnContinuationRejected"))throw new InvalidOperationException("Independent waste counters disagree with captured attempts");
  if(queries.Count!=DepotRoutingTrace.Rows.Count(r=>r.kind=="service-query"))throw new InvalidOperationException("Unmatched service trace rows");
  File.WriteAllLines(Path.Combine(output,"waste-queries.csv"),qrows);File.WriteAllLines(Path.Combine(output,"waste-attempts.csv"),arows);File.WriteAllLines(Path.Combine(output,"waste-stations.csv"),drows);DepotRoutingTrace.Write(Path.Combine(output,"waste-searches.csv"));File.WriteAllText(Path.Combine(output,"WasteCensusResult.txt"),"PASS all query/result/sample links and independent attempt/dispatch/rejection counters; observation only");
 }
}
}
