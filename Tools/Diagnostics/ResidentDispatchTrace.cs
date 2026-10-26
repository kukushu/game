using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Diagnostics;using System.Globalization;using HarborCity.RoadCore;
namespace HarborCity {
 // Observation only, copied into Temp. Never called by formal Assets.
 internal static class ResidentDispatchTrace {
  internal static bool Enabled;static bool inDispatch;static long pendingIndexTicks,pendingIndexCalls,indexTicks,indexCalls;static double Ms(long ticks)=>ticks*1000.0/Stopwatch.Frequency;static string N(double d)=>d.ToString("R",CultureInfo.InvariantCulture);
  sealed class Summary {internal long count,success,business,ticks,indexTicks,indexCalls;internal readonly long[] rejections=new long[8];}
  sealed class Person {internal int attempts,success,fail,eligible,waiting,late,unreachable,ready,thresholdChanges;internal double lastThreshold=double.NaN;internal int lastDay,lastWork,lastNetwork;internal long firstTick,lastTick;internal string homeReason="unobserved";}
  struct Attempt {internal long tick,indexCalls;internal int citizen,origin,destination;internal TransportPurpose purpose;internal string result,homeReason;internal double ms,indexMs;}
  static readonly Dictionary<(TransportPurpose,string,string),Summary> summary=new Dictionary<(TransportPurpose,string,string),Summary>();
  static readonly Dictionary<int,Person> people=new Dictionary<int,Person>();static readonly List<Attempt> attempts=new List<Attempt>();static readonly long[] qualifications=new long[10];
  static Person Get(int id){if(!people.TryGetValue(id,out var p)){p=new Person();people.Add(id,p);}return p;}
  internal static void Reset(bool enabled){Enabled=enabled;inDispatch=false;indexTicks=indexCalls=0;if(!enabled)return;summary.Clear();people.Clear();attempts.Clear();attempts.Capacity=Math.Max(attempts.Capacity,100000);Array.Clear(qualifications,0,qualifications.Length);}
  internal static long Start(TransportPurpose purpose){inDispatch=Enabled&&(purpose==TransportPurpose.Home||purpose==TransportPurpose.Work);pendingIndexTicks=pendingIndexCalls=0;return inDispatch?Stopwatch.GetTimestamp():0;}
  internal readonly struct IndexScope:IDisposable {
   readonly long start;readonly bool parent;internal IndexScope(bool enabled){start=enabled?Stopwatch.GetTimestamp():0;parent=inDispatch;}
   public void Dispose(){if(start==0)return;long ticks=Stopwatch.GetTimestamp()-start;indexCalls++;indexTicks+=ticks;if(parent){pendingIndexCalls++;pendingIndexTicks+=ticks;}}
  }
  internal static IndexScope Index()=>new IndexScope(Enabled);
  internal static void Qualification(CityModel city,World world,CityResident p,Household h,int workplace,double minute,double shift,double end,double estimate,bool eligible,bool returning,bool leaving)
  {
   if(!Enabled)return;qualifications[0]++;
   if(returning){var item=Get(p.id);item.homeReason=!p.atWork?"NotAtWork":p.sick?"Sick":workplace!=p.location?"WorkplaceChanged":p.arrivedDay<city.day?"PriorDay":minute>=end?"ShiftEnded":"EmployerUnfunded";}
   if(!eligible){if(p.atWork)qualifications[1]++;else if(p.location!=h.home)qualifications[2]++;else if(p.departureDay==city.day)qualifications[3]++;else if(!p.canWork||p.sick)qualifications[4]++;else qualifications[5]++;return;}
   var person=Get(p.id);person.eligible++;
   if(double.IsInfinity(estimate)){qualifications[6]++;person.unreachable++;return;}
   double threshold=Math.Max(0,shift-estimate);
   if(minute<threshold){qualifications[7]++;person.waiting++;}else if(minute>=end){qualifications[8]++;person.late++;}else {qualifications[9]++;person.ready++;}
   // Stability is an observation, not proof that unobserved events may be ignored.
   if(!double.IsNaN(person.lastThreshold)&&(person.lastThreshold!=threshold||person.lastDay!=city.day||person.lastWork!=workplace||person.lastNetwork!=world.NetworkGeneration))person.thresholdChanges++;
   person.lastThreshold=threshold;person.lastDay=city.day;person.lastWork=workplace;person.lastNetwork=world.NetworkGeneration;
   if(leaving!=(minute>=threshold&&minute<end))throw new Exception("Trace did not reproduce existing ready predicate");
  }
  internal static void Record(CityModel city,World world,int citizen,TransportPurpose purpose,int origin,int destination,TransportRejection rejection,bool success,bool business,long started)
  {
   if(started==0)return;long elapsed=Stopwatch.GetTimestamp()-started;inDispatch=false;var person=Get(citizen);string result=success?"Success":business?"BusinessRejected":rejection.ToString();string reason=purpose==TransportPurpose.Home?person.homeReason:"Work";
   var key=(purpose,result,reason);if(!summary.TryGetValue(key,out var s)){s=new Summary();summary.Add(key,s);}s.count++;s.ticks+=elapsed;s.indexTicks+=pendingIndexTicks;s.indexCalls+=pendingIndexCalls;if(success)s.success++;else if(business)s.business++;else s.rejections[(int)rejection]++;
   person.attempts++;if(success)person.success++;else person.fail++;if(person.firstTick==0)person.firstTick=city.entities.Simulation.Tick+1;person.lastTick=city.entities.Simulation.Tick+1;
   attempts.Add(new Attempt{tick=person.lastTick,citizen=citizen,origin=origin,destination=destination,purpose=purpose,result=result,homeReason=reason,ms=Ms(elapsed),indexMs=Ms(pendingIndexTicks),indexCalls=pendingIndexCalls});
  }
  internal static void Write(string folder)
  {
   if(!Enabled)return;if(qualifications[0]==0)throw new Exception("Retry hooks were not observed; build the retry variant before running this fixed-city diagnostic");Enabled=false;
   File.WriteAllLines(Path.Combine(folder,"dispatch-summary.csv"),new[]{"purpose,result,home_reason,count,measured_ms,reindex_calls,reindex_ms"}.Concat(summary.OrderBy(p=>p.Key.Item1).ThenBy(p=>p.Key.Item2).ThenBy(p=>p.Key.Item3).Select(p=>string.Join(",",p.Key.Item1,p.Key.Item2,p.Key.Item3,p.Value.count,N(Ms(p.Value.ticks)),p.Value.indexCalls,N(Ms(p.Value.indexTicks))))));
   File.WriteAllText(Path.Combine(folder,"reindex-total.csv"),"calls,ms\n"+indexCalls+","+N(Ms(indexTicks)));
   File.WriteAllLines(Path.Combine(folder,"dispatch-attempts-local.csv"),new[]{"tick,citizen,purpose,origin,destination,result,home_reason,ms,reindex_calls,reindex_ms"}.Concat(attempts.Select(a=>string.Join(",",a.tick,a.citizen,a.purpose,a.origin,a.destination,a.result,a.homeReason,N(a.ms),a.indexCalls,N(a.indexMs)))));
   File.WriteAllLines(Path.Combine(folder,"resident-distribution.csv"),new[]{"citizen,attempts,success,fail,first_tick,last_tick,eligible,waiting,shift_ended,unreachable,ready,threshold_changes"}.Concat(people.OrderByDescending(p=>p.Value.attempts).ThenBy(p=>p.Key).Select(p=>{var s=p.Value;return string.Join(",",p.Key,s.attempts,s.success,s.fail,s.firstTick,s.lastTick,s.eligible,s.waiting,s.late,s.unreachable,s.ready,s.thresholdChanges);}))); 
   File.WriteAllLines(Path.Combine(folder,"qualification-summary.csv"),new[]{"category,count"}.Concat(new[]{"passed_basic_gate","not_eligible_at_work","not_eligible_away","not_eligible_already_departed","not_eligible_cannot_attend","not_eligible_unfunded","eligible_unreachable","eligible_before_departure","eligible_shift_ended","eligible_ready"}.Select((n,i)=>n+","+qualifications[i])));
  }
 }
}
