using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HarborCity;
using HarborCity.RoadCore;
static class CityCommuteAttribution
{
 sealed class Count {internal long events,repeatedMiss,evictedAgain,previouslySeen,shortResidentRepeat;internal readonly HashSet<(int,int,bool)> pairs=new HashSet<(int,int,bool)>();}
 static string phase;static long tick;static int epoch;
 static readonly Dictionary<(string,CityCoreProfile.QueryKind,CityCoreProfile.QueryResult,CityCoreProfile.Part),Count> counts=new Dictionary<(string,CityCoreProfile.QueryKind,CityCoreProfile.QueryResult,CityCoreProfile.Part),Count>();
 static readonly Dictionary<(int,int,bool),int> ever=new Dictionary<(int,int,bool),int>();
 static readonly HashSet<(int,int,bool)> evicted=new HashSet<(int,int,bool)>();
 static readonly Dictionary<int,(int,int,long)> lastMiss=new Dictionary<int,(int,int,long)>();
 static readonly HashSet<(string,int,int,bool)> phaseMiss=new HashSet<(string,int,int,bool)>();
 static void Observe(CityCoreProfile.RouteQuery q)
 {
  var group=(phase,q.Kind,q.Result,q.Context);if(!counts.TryGetValue(group,out var c))counts.Add(group,c=new Count());c.events++;var key=(q.Origin,q.Destination,q.BothDirections);c.pairs.Add(key);
  if(q.Kind==CityCoreProfile.QueryKind.Reset){epoch++;return;}
  if(q.Kind==CityCoreProfile.QueryKind.Evicted){evicted.Add(key);return;}
  if(q.Kind==CityCoreProfile.QueryKind.Blueprint||q.Kind==CityCoreProfile.QueryKind.CostQuote)
  {if(q.Result==CityCoreProfile.QueryResult.Miss){if(!phaseMiss.Add((phase,q.Origin,q.Destination,q.BothDirections)))c.repeatedMiss++;if(ever.ContainsKey(key))c.previouslySeen++;if(evicted.Contains(key))c.evictedAgain++;evicted.Remove(key);}ever[key]=epoch;}
  if(q.Kind==CityCoreProfile.QueryKind.CommuteEstimate&&q.Result!=CityCoreProfile.QueryResult.Hit)
  {if(lastMiss.TryGetValue(q.Citizen,out var prior)&&prior.Item1==q.Origin&&prior.Item2==q.Destination&&tick-prior.Item3<=5)c.shortResidentRepeat++;lastMiss[q.Citizen]=(q.Origin,q.Destination,tick);}
 }
 static void Dump(string output)
 {
  using(var w=new StreamWriter(Path.Combine(output,"queries.csv")))
  {w.WriteLine("phase,kind,result,context,events,unique_home_work_pairs,repeated_miss_in_phase,previously_seen_miss,requery_after_eviction,same_resident_repeat_within_5_ticks");foreach(var pair in counts.OrderBy(p=>p.Key.Item1).ThenBy(p=>p.Key.Item2).ThenBy(p=>p.Key.Item3).ThenBy(p=>p.Key.Item4))
   {var c=pair.Value;w.WriteLine(string.Join(",",pair.Key.Item1,pair.Key.Item2,pair.Key.Item3,pair.Key.Item4,c.events,c.pairs.Count,c.repeatedMiss,c.previouslySeen,c.evictedAgain,c.shortResidentRepeat));}}
 }
 static void Run(CityCoreIndustrySession s,CityCoreIndustrySession shadow,string name,int steps,string output)
 {
  phase=name;using(var w=new StreamWriter(Path.Combine(output,name+"-steps.csv")))
  {w.WriteLine(CityCoreProfile.Header());for(int i=0;i<steps;i++){tick=s.Simulation.Tick+1;CityCoreProfile.Reset();CityCoreProfile.Enabled=true;long routes=s.Transport.RoutePreparations;int gc0=GC.CollectionCount(0),gc1=GC.CollectionCount(1),gc2=GC.CollectionCount(2);if(s.Advance(.05)!=1)throw new Exception("Lost step");string row=CityCoreProfile.Row(s.Simulation.Tick,s.Transport.ActiveCount,s.Transport.RoutePreparations-routes,GC.CollectionCount(0)-gc0,GC.CollectionCount(1)-gc1,GC.CollectionCount(2)-gc2);CityCoreProfile.Enabled=false;w.WriteLine(row);if(shadow.Advance(.05)!=1)throw new Exception("Shadow lost step");if((i+1)%120==0)Console.WriteLine(name+" tick="+s.Simulation.Tick);}}
  if(!CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(shadow)))throw new Exception("Observer changed full city in "+name);
  Dump(output);Console.WriteLine("PASS "+name+" observer on/off full city identical tick="+s.Simulation.Tick+" network="+s.Transport.NetworkGeneration+" access="+s.Transport.AccessRevision);
 }
 static void Main(string[] args)
 {
  Directory.CreateDirectory(args[1]);var bytes=File.ReadAllBytes(args[0]);var s=CityCoreSave.Read(bytes);var shadow=CityCoreSave.Read(bytes);foreach(var item in new[]{s,shadow})item.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
  CityCoreProfile.QueryObserver=Observe;
  try{
   Run(s,shadow,"cold-first",1,args[1]);Run(s,shadow,"cold-warm",19,args[1]);Run(s,shadow,"warm-replay",374,args[1]);Run(s,shadow,"spike",1,args[1]);Run(s,shadow,"ordinary",20,args[1]);
   phase="road-edit";tick=s.Simulation.Tick;CityCoreProfile.Enabled=true;bool done=false;
   foreach(int road in s.Transport.Network.RoadIds.OrderBy(id=>id).Take(12).ToArray())
   {double distance=s.Transport.Network.Centreline(road).Length*.5;if(distance<6)continue;byte[] before=CityCoreSave.Write(s);var positions=s.Transport.Vehicles.ToDictionary(v=>v.id,v=>s.Transport.Position(v));
    try{s.Transport.SplitRoadAt(road,distance);}catch(InvalidOperationException){if(!before.SequenceEqual(CityCoreSave.Write(s)))throw new Exception("Failed edit changed facts");continue;}
    foreach(var v in s.Transport.Vehicles)if(Point.Distance(positions[v.id],s.Transport.Position(v))>1e-7)throw new Exception("Edit moved vehicle");CityCoreProfile.Enabled=false;shadow.Transport.SplitRoadAt(road,distance);done=true;break;}
   CityCoreProfile.Enabled=false;if(!done)throw new Exception("No tested legal edit; record failure instead of claiming coverage");
   Run(s,shadow,"after-road-edit",10,args[1]);s.Validate();shadow.Validate();File.WriteAllBytes(Path.Combine(args[1],"final.hcity"),CityCoreSave.Write(s));Console.WriteLine("PASS bounded cold/warm/spike/edit attribution; all complete state identical; observer overhead is NOT normal performance");
  }finally{CityCoreProfile.QueryObserver=null;CityCoreProfile.Enabled=false;Dump(args[1]);}
 }
}
