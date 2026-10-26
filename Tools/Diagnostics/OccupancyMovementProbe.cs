using System;using System.IO;using System.Linq;using System.Diagnostics;using System.Globalization;using System.Collections.Generic;using HarborCity.RoadCore;
namespace HarborCity {
 // Temp-only timing and change census. Never participates in movement decisions.
 internal static class OccupancyMovementProbe {
  internal enum Part {Reindex,ClearBuckets,ResetReservations,RegisterActors,SortBuckets,Move,EnsureIndex,ChooseChanges,ClearBuffers,GatherIntents,SortIntents,AdvanceIntents,CommitPositions}
  static bool timing,insideMove;internal static bool Counting;static readonly long[] ticks=new long[13],calls=new long[13];static long indexInMove;
  static readonly long[] total=new long[21],step=new long[21];static readonly List<string> rows=new List<string>();static readonly HashSet<long> dirty=new HashSet<long>(),membership=new HashSet<long>();
  static readonly string[] names={"registered_vehicle_visits","active_intents","stationary","same_drivable_moved","changed_drivable","changing_before","changing_after","finished_change","changed_claim_signature","started_lane_changes","historical_buckets","nonempty_buckets","bucket_entries","position_dirty_buckets","membership_dirty_buckets","departures_indexed","retired_vehicles","registered_active","registered_stranded","registered_restart","registered_delivered"};
  static string N(double x)=>x.ToString("R",CultureInfo.InvariantCulture);
  internal static void Reset(string mode){timing=mode=="occupancy-timing"||mode=="occupancy-trim"||mode=="occupancy-active";Counting=mode=="occupancy-counts";insideMove=false;indexInMove=0;Array.Clear(ticks,0,ticks.Length);Array.Clear(calls,0,calls.Length);Array.Clear(total,0,total.Length);rows.Clear();}
  internal static void BeginStep(){if(!Counting)return;Array.Clear(step,0,step.Length);dirty.Clear();membership.Clear();}
  internal static void EndStep(long tick){if(!Counting)return;step[13]=dirty.Count;step[14]=membership.Count;for(int i=0;i<step.Length;i++)total[i]+=step[i];rows.Add(tick+","+string.Join(",",step));}
  internal readonly struct Scope:IDisposable {readonly Part part;readonly long start;readonly bool inMove;
   internal Scope(Part value){part=value;start=timing?Stopwatch.GetTimestamp():0;inMove=insideMove;if(value==Part.Move)insideMove=true;}
   public void Dispose(){if(start!=0){long duration=Stopwatch.GetTimestamp()-start;ticks[(int)part]+=duration;calls[(int)part]++;if(part==Part.Reindex&&inMove)indexInMove+=duration;}if(part==Part.Move)insideMove=false;}
  }
  internal static Scope Measure(Part part)=>new Scope(part);
  internal static void Actor(Vehicle v){if(!Counting)return;step[0]++;step[v.status==VehicleStatus.Active?17:v.status==VehicleStatus.Stranded?18:v.status==VehicleStatus.AwaitingRestart?19:20]++;}
  internal static void Departure(){if(Counting)step[15]++;}
  internal static void Retired(int count){if(Counting)step[16]+=count;}
  internal static void Bucket(int size){if(!Counting)return;step[10]++;if(size>0)step[11]++;step[12]+=size;}
  internal static void Started(int count){if(Counting)step[9]+=count;}
  internal static void ShadowAdded(long lane){if(Counting){dirty.Add(lane);membership.Add(lane);}}
  internal readonly struct Before {
   internal readonly long lane,target,movement;internal readonly double distance;internal readonly int junction;
   internal Before(Vehicle v,int junction,long movement){lane=v.Current;target=v.changeTarget;distance=v.distance;this.junction=junction;this.movement=movement;}
  }
  internal static Before Capture(Vehicle v,int junction,long movement)=>Counting?new Before(v,junction,movement):default;
  internal static void Committed(Before old,Vehicle v,int junction,long movement)
  {
   if(!Counting)return;step[1]++;bool moved=old.lane!=v.Current||old.distance!=v.distance;
   if(!moved)step[2]++;else if(old.lane==v.Current)step[3]++;else step[4]++;
   if(old.target!=0)step[5]++;if(v.changeTarget!=0)step[6]++;if(old.target!=0&&v.changeTarget==0)step[7]++;
   if(old.junction!=junction||old.movement!=movement)step[8]++;
   if(moved){dirty.Add(old.lane);dirty.Add(v.Current);if(old.target!=0)dirty.Add(old.target);if(v.changeTarget!=0)dirty.Add(v.changeTarget);}
   if(old.lane!=v.Current||old.target!=v.changeTarget){membership.Add(old.lane);membership.Add(v.Current);if(old.target!=0)membership.Add(old.target);if(v.changeTarget!=0)membership.Add(v.changeTarget);}
  }
  internal static void Write(string folder)
  {
   if(timing&&calls[(int)Part.Move]==0||Counting&&total[1]==0)throw new Exception("Occupancy hooks were not observed; verify the matching build variant and runtime mode");
   if(timing){double ms=1000.0/Stopwatch.Frequency;File.WriteAllLines(Path.Combine(folder,"occupancy-timing.csv"),new[]{"phase,calls,inclusive_ms"}.Concat(Enum.GetValues(typeof(Part)).Cast<Part>().Select(p=>p+","+calls[(int)p]+","+N(ticks[(int)p]*ms))));File.WriteAllText(Path.Combine(folder,"movement-exclusive.csv"),"move_ms,index_inside_move_ms,move_excluding_index_ms\n"+N(ticks[(int)Part.Move]*ms)+","+N(indexInMove*ms)+","+N((ticks[(int)Part.Move]-indexInMove)*ms));}
   if(Counting){File.WriteAllLines(Path.Combine(folder,"occupancy-counts.csv"),new[]{"metric,total"}.Concat(names.Select((n,i)=>n+","+total[i])));File.WriteAllLines(Path.Combine(folder,"occupancy-steps.csv"),new[]{"tick,"+string.Join(",",names)}.Concat(rows));}
   timing=Counting=false;
  }
 }
}
