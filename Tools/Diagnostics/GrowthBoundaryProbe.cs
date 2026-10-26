using System;using System.IO;using System.Linq;using System.Diagnostics;using System.Globalization;
namespace HarborCity {
 // Temp-only counts: filter observations intentionally have overhead; not release timing.
 internal static class GrowthBoundaryProbe {
  internal enum Part{Whole,Statistics,Demand,Cells,Search,Footprint,Overlap,CanPlace,Entrance}
  static bool enabled;static long startTick;static int current;static long[,] calls,ticks,counts;
  internal static void Reset(long tick,int steps){enabled=true;startTick=tick;current=-1;calls=new long[steps,9];ticks=new long[steps,9];counts=new long[steps,5];}
  internal readonly struct Scope:IDisposable{readonly Part part;readonly long start;readonly int row;internal Scope(Part p){part=p;row=current;start=enabled&&row>=0?Stopwatch.GetTimestamp():0;}public void Dispose(){if(start==0)return;calls[row,(int)part]++;ticks[row,(int)part]+=Stopwatch.GetTimestamp()-start;}}
  internal static Scope Begin(long tick){current=enabled?(int)(tick-startTick):-1;return new Scope(Part.Whole);}
  internal static Scope Measure(Part p)=>new Scope(p);
  internal static void Size(){if(enabled)counts[current,4]++;}
  internal static bool Frontage(ZoneCell cell){if(enabled)counts[current,0]++;if(cell.row!=0)return false;if(enabled)counts[current,1]++;if(!cell.available)return false;if(enabled)counts[current,2]++;return true;}
  internal static void Matched(){if(enabled)counts[current,3]++;}
  internal static bool Candidate(ZoneCell cell,LandUse use){if(enabled)counts[current,0]++;if(cell.row!=0)return false;if(enabled)counts[current,1]++;if(!cell.available)return false;if(enabled)counts[current,2]++;if(cell.use!=use)return false;if(enabled)counts[current,3]++;return true;}
  internal static void Write(string output){if(!enabled)return;enabled=false;var rows=new System.Collections.Generic.List<string>{"tick,phase,calls,diagnostic_ms,filter_checks,row0_checks,row0_available,matching_starts,size_attempts"};for(int i=0;i<calls.GetLength(0);i++)if(calls[i,0]>0)for(int j=0;j<9;j++)rows.Add(string.Join(",",startTick+i+1,(Part)j,calls[i,j],(ticks[i,j]*1000.0/Stopwatch.Frequency).ToString("R",CultureInfo.InvariantCulture),counts[i,0],counts[i,1],counts[i,2],counts[i,3],counts[i,4]));File.WriteAllLines(Path.Combine(output,"growth-counts.csv"),rows);}
 }
}
