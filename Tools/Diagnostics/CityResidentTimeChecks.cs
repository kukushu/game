using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Security.Cryptography;using HarborCity;using HarborCity.RoadCore;
static class CityResidentTimeChecks {
#if TIME_PLAN
 internal static int Checks,Fallbacks,NaiveDifferences;internal static readonly List<string> Examples=new List<string>{"tick,offset,day_seconds,shift,duration,estimate,retry,visit,exact_tick,naive_tick"};
 internal static void Check(bool ok,string message){Checks++;if(!ok)throw new Exception(message);}
 // Independent sequential execution of the ORIGINAL clock/calendar comparisons.
 internal static float OracleMinute(long tick,double offset,float daySeconds){double seconds=tick*.05;double absolute=offset+seconds;float dayElapsed=(float)(absolute%(double)daySeconds);float minute=dayElapsed/daySeconds*1440;return minute;}
 internal static void Verify(long min,double offset,float days,float retry,double visit,float shift,float duration,double estimate,int mode){
  var plan=mode==0?CityResidentRequestTime.Departure(min,offset,days,retry,visit,shift,duration,estimate):mode==1?CityResidentRequestTime.ShiftEnd(min,offset,days,retry,visit,shift,duration):CityResidentRequestTime.AfterGuards(min,offset,days,retry,visit);
  if(plan.Kind==ResidentTimeCheckKind.Conservative){double a=offset+min*.05,d=Math.Floor(a/days);Check(plan.Tick==min&&(double.IsNaN(offset)||double.IsInfinity(offset)||offset<0||float.IsNaN(days)||float.IsInfinity(days)||days<1||d>int.MaxValue-1||a-d*days!=a%days),"Unjustified conservative fallback");Fallbacks++;return;}
  if(min==1000000000000L){Check(plan.Kind==ResidentTimeCheckKind.ClockEnd&&plan.Tick==long.MaxValue,"Clock-end planning wrong");return;}
  long day=(long)Math.Floor((offset+min*.05)/days);float end=shift+duration;long expected=long.MaxValue;ResidentTimeCheckKind kind=ResidentTimeCheckKind.ClockEnd;int walked=0;
  for(long t=min;t<1000000000000L;t++){if((long)Math.Floor((offset+t*.05)/days)!=day){expected=t;kind=ResidentTimeCheckKind.Calendar;break;}float minute=OracleMinute(t,offset,days);bool open=!(t*.05<retry||t*.05<visit);bool match=mode==0?!double.IsInfinity(estimate)&&minute>=Math.Max(0,shift-estimate)&&minute<end:mode==1?minute>=end:true;if(open&&match){expected=t;kind=ResidentTimeCheckKind.Predicate;break;}if(++walked>2000000)throw new Exception("Oracle budget exceeded");}
  Check(plan.Tick==expected&&plan.Kind==kind,"First-tick mismatch mode="+mode+" min="+min+" got="+plan.Tick+" expected="+expected+" kind="+plan.Kind+" / "+kind);
  if(mode==0&&kind==ResidentTimeCheckKind.Predicate&&double.IsFinite(estimate)){double start=Math.Max(0,shift-estimate);double dayIndex=Math.Floor((offset+min*.05)/days);long guard=min;while(guard<expected&&(guard*.05<retry||guard*.05<visit))guard++;long naive=Math.Max(guard,(long)Math.Ceiling((dayIndex*days+start/1440*days-offset)/.05));if(naive!=expected){NaiveDifferences++;if(Examples.Count<25)Examples.Add(string.Join(",",min,offset.ToString("R",System.Globalization.CultureInfo.InvariantCulture),days.ToString("R",System.Globalization.CultureInfo.InvariantCulture),shift,duration,estimate.ToString("R",System.Globalization.CultureInfo.InvariantCulture),retry,visit,expected,naive));}}
 }
 static void Cost(string output){long checksum=0;for(int i=0;i<200;i++)checksum+=CityResidentRequestTime.Departure(i,50,120,0,0,480,480,20).Tick;var watch=new System.Diagnostics.Stopwatch();long allocated=GC.GetAllocatedBytesForCurrentThread();watch.Start();for(int i=0;i<10000;i++){long t=i%2000;checksum+=CityResidentRequestTime.Departure(t,50,120,(float)(t*.05+.25),0,480,480,20).Tick;checksum+=CityResidentRequestTime.ShiftEnd(t,50,120,0,0,480,480).Tick;checksum+=CityResidentRequestTime.AfterGuards(t,50,120,(float)(t*.05+.25),0).Tick;}watch.Stop();allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;File.WriteAllLines(Path.Combine(output,"planner-cost.csv"),new[]{"plans,total_ms,allocated_bytes,checksum",string.Join(",",30000,watch.Elapsed.TotalMilliseconds.ToString("R",System.Globalization.CultureInfo.InvariantCulture),allocated,checksum)});Console.WriteLine("plannerCostMs="+watch.Elapsed.TotalMilliseconds+",allocated="+allocated+"; pure helper only, not a game performance win");}
 static void Pure(){var random=new Random(81031);float[] periods={1,3.7f,7.3f,60,120,123.45f,600};foreach(float period in periods)for(int n=0;n<600;n++){long tick=random.Next(0,100000);double offset=n%3==0?50:random.NextDouble()*period;float shift=(float)(random.NextDouble()*1440),duration=(float)(random.NextDouble()*500);double estimate=random.NextDouble()*500;float retry=(float)(tick*.05+(n%11)*.05);double visit=tick*.05+(n%7)*.05;for(int mode=0;mode<3;mode++)Verify(tick,offset,period,retry,visit,shift,duration,estimate,mode);}
  for(int i=1;i<=500;i++){long future=i*7;float target=OracleMinute(future,50,120);Verify(future-1,50,120,0,0,target,100,0,0);}
  foreach(long tick in new[]{0L,1L,7199L,24000000L,999999999980L,1000000000000L})foreach(double delta in new[]{-.0000000001,0,.0000000001,.25}){double clock=tick*.05;foreach(float retry in new[]{(float)(clock+delta),float.NaN,float.PositiveInfinity,float.NegativeInfinity})foreach(double visit in new[]{Math.BitDecrement(clock+delta),clock+delta,Math.BitIncrement(clock+delta),double.NaN,double.PositiveInfinity})for(int mode=0;mode<3;mode++)Verify(tick,50,120,retry,visit,480,480,17.3,mode);}
  foreach(float end in new[]{0f,1440f,float.NaN,float.PositiveInfinity})foreach(double est in new[]{0d,double.NaN,double.PositiveInfinity,double.NegativeInfinity,10000d})Verify(2398,0,120,0,0,0,end,est,0);
  foreach(double offset in new[]{0,1000000000d,250000000000d,1e300,double.NaN})Verify(999999999970,offset,123.45f,0,0,480,480,20,0);
  for(int i=0;i<1000;i++){float days=23.7f;double boundary=(1500000000L+i)*(double)days;Verify(0,Math.BitDecrement(boundary),days,0,0,480,480,0,0);Verify(0,boundary,days,0,0,480,480,0,1);}
  Check(NaiveDifferences>0,"No direct-ceiling counterexample exercised");Console.WriteLine("PASS independent first-tick checks="+Checks+",fallbacks="+Fallbacks+",naiveDifferences="+NaiveDifferences);
 }
#endif
 static string Hash(CityCoreIndustrySession s)=>Convert.ToHexString(SHA256.HashData(CityCoreSave.Write(s)));
 static void Main(string[] args){
#if TIME_PLAN
 Pure();
#endif
 var s=CityCoreSave.Read(File.ReadAllBytes(args[0]));s.Transport.DepotLaneRoutingEnabled=true;s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);var rows=new List<string>{"tick,sha256"};for(int i=0;i<2400;i++)s.Advance(.05);var calls=new List<string>();var dispatch=s.Residents.RequestCommute;s.Residents.RequestCommute=(id,from,to,back)=>{var v=dispatch(id,from,to,back);calls.Add(string.Join(",",s.Simulation.Tick,id,from,to,back,v==null?0:v.id));return v;};
#if TIME_PLAN
 CityResidentTimeProbe.Enabled=true;
#endif
 for(int i=0;i<1200;i++){s.Advance(.05);if(i%100==99)rows.Add(s.Simulation.Tick+","+Hash(s));}var cold=CityCoreSave.Read(CityCoreSave.Write(s));cold.Transport.DepotLaneRoutingEnabled=true;cold.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);for(int i=0;i<80;i++){s.Advance(.05);cold.Advance(.05);}if(Hash(s)!=Hash(cold))throw new Exception("Cold replay differs");rows.Add("cold80,"+Hash(s));File.WriteAllLines(Path.Combine(args[1],"states.csv"),rows);File.WriteAllLines(Path.Combine(args[1],"requests.csv"),calls);
#if TIME_PLAN
 CityResidentTimeProbe.Finish();Cost(args[1]);File.WriteAllLines(Path.Combine(args[1],"rounding-examples.csv"),Examples);Console.WriteLine("timeProbe="+CityResidentTimeProbe.Samples+",departures="+CityResidentTimeProbe.Departures+",returns="+CityResidentTimeProbe.Returns+",checks="+Checks);
#endif
 Console.WriteLine("PASS real30k unchanged requests/full state and cold80; no scheduling activated");}
}
#if TIME_PLAN
internal static class CityResidentTimeProbe {
 internal static bool Enabled;internal static int Samples,Departures,Returns;static readonly HashSet<int> sampledDeparture=new HashSet<int>(),sampledReturn=new HashSet<int>();static readonly List<Action> pending=new List<Action>();
 internal static void Observe(int citizen,long tick,double offset,float days,float retry,double visit,float shift,float duration,double estimate,bool eligible,bool leaving,bool timedReturn,bool returning){
  if(!Enabled)return;
  if(eligible&&Departures<128&&sampledDeparture.Add(citizen)){Samples++;Departures++;pending.Add(()=>CityResidentTimeChecks.Verify(tick,offset,days,retry,visit,shift,duration,estimate,0));var plan=CityResidentRequestTime.Departure(tick,offset,days,retry,visit,shift,duration,estimate);CityResidentTimeChecks.Check(plan.Kind!=ResidentTimeCheckKind.Conservative,"Normal departure fell back");CityResidentTimeChecks.Check((plan.Kind==ResidentTimeCheckKind.Predicate&&plan.Tick==tick)==leaving,"Live departure predicate mismatch");}
  if(timedReturn&&Returns<128&&sampledReturn.Add(citizen)){Samples++;Returns++;pending.Add(()=>CityResidentTimeChecks.Verify(tick,offset,days,retry,visit,shift,duration,estimate,1));var plan=CityResidentRequestTime.ShiftEnd(tick,offset,days,retry,visit,shift,duration);CityResidentTimeChecks.Check(plan.Kind!=ResidentTimeCheckKind.Conservative,"Normal return fell back");CityResidentTimeChecks.Check((plan.Kind==ResidentTimeCheckKind.Predicate&&plan.Tick==tick)==returning,"Live return predicate mismatch");}
 }
 internal static void Finish(){CityResidentTimeChecks.Check(Departures==128&&Returns==128,"Both live time categories must be sampled");foreach(var check in pending)check();pending.Clear();}
}
#endif
