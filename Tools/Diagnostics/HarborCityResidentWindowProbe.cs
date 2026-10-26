using System;using System.IO;using System.Linq;using System.Collections;using System.Collections.Generic;using System.Diagnostics;using System.Globalization;using UnityEngine;using HarborCity.RoadCore;
namespace HarborCity {
 // Copied into a disposable Player project. Formal Assets are never patched.
 public sealed class HarborCityResidentWindowProbe:MonoBehaviour {
  HarborCityRoadCoreGame host;string output,mode;
  public void Configure(HarborCityRoadCoreGame h,string input,string result,float rate){host=h;output=result;host.ScenarioMeasuring=true;var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"-resident-mode");mode=i<0?"stock":a[i+1];StartCoroutine(Run());}
  static string N(double x)=>x.ToString("R",CultureInfo.InvariantCulture);
  IEnumerator Run(){Directory.CreateDirectory(output);long allocationStart=GC.GetAllocatedBytesForCurrentThread();GC.KeepAlive(new byte[4096]);bool allocationSupported=GC.GetAllocatedBytesForCurrentThread()>allocationStart;File.WriteAllText(Path.Combine(output,"allocation-counter.txt"),allocationSupported?"supported":"unsupported; do not interpret zero as zero GC");host.ScenarioRate(0);Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=60;yield return null;
   var s=host.ScenarioSession;s.Transport.DepotLaneRoutingEnabled=true;CityCoreProfile.Enabled=false;
   for(int i=0;i<2400;i++){if(s.Simulation.Advance(.05,1,double.PositiveInfinity)!=1)throw new Exception("Warm step lost");if(i%10==9)yield return null;}
   double t=Time.realtimeSinceStartupAsDouble;while(Time.realtimeSinceStartupAsDouble-t<8)yield return null;
   File.WriteAllBytes(Path.Combine(output,"start.hcity"),CityCoreSave.Write(s));
   Func<string,long> indexCounter=name=>{var field=s.Transport.GetType().GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);return field==null?-1:Convert.ToInt64(field.GetValue(s.Transport));};long fullBefore=indexCounter("FullIndexBuilds"),deltaBefore=indexCounter("IncrementalIndexUpdates");
   int measuredSteps=600;var cli=Environment.GetCommandLineArgs();int stepsFlag=Array.IndexOf(cli,"-resident-steps");if(stepsFlag>=0)measuredSteps=int.Parse(cli[stepsFlag+1]);
   var phaseTicks=new long[measuredSteps,CityCoreProfile.Ticks.Length];var phaseCalls=new long[measuredSteps,CityCoreProfile.Ticks.Length];var previousTicks=new long[CityCoreProfile.Ticks.Length];var previousCalls=new long[CityCoreProfile.Ticks.Length];
   var boundary=new double[measuredSteps,8];var collections=new int[measuredSteps,3];
   var rows=new List<string>{"index,tick,step_ms,allocated_bytes,active_vehicles"};ResidentRequestProbe.Reset(mode=="counts"||mode=="sampled",mode=="sampled",64);ResidentDispatchTrace.Reset(mode=="retry"||mode=="counts"||mode=="sampled");OccupancyMovementProbe.Reset(mode);CityCoreProfile.Reset();
   for(int i=0;i<measuredSteps;i++){yield return null;
    boundary[i,0]=s.Simulation.Seconds;boundary[i,1]=s.City.development.nextGrowth;boundary[i,2]=s.City.development.nextEmployment;boundary[i,3]=s.City.day;boundary[i,4]=s.City.development.projects.Count;
    int g0=GC.CollectionCount(0),g1=GC.CollectionCount(1),g2=GC.CollectionCount(2);OccupancyMovementProbe.BeginStep();CityCoreProfile.Enabled=mode=="legacy";long b=GC.GetAllocatedBytesForCurrentThread(),start=Stopwatch.GetTimestamp();if(s.Simulation.Advance(.05,1,double.PositiveInfinity)!=1)throw new Exception("Measured step lost");long elapsed=Stopwatch.GetTimestamp()-start,bytes=allocationSupported?GC.GetAllocatedBytesForCurrentThread()-b:-1;CityCoreProfile.Enabled=false;
    collections[i,0]=GC.CollectionCount(0)-g0;collections[i,1]=GC.CollectionCount(1)-g1;collections[i,2]=GC.CollectionCount(2)-g2;
    boundary[i,5]=s.City.development.projects.Count;boundary[i,6]=s.City.day;boundary[i,7]=s.Simulation.Seconds;
    for(int k=0;k<previousTicks.Length;k++){phaseTicks[i,k]=CityCoreProfile.Ticks[k]-previousTicks[k];previousTicks[k]=CityCoreProfile.Ticks[k];phaseCalls[i,k]=CityCoreProfile.Calls[k]-previousCalls[k];previousCalls[k]=CityCoreProfile.Calls[k];}
    OccupancyMovementProbe.EndStep(s.Simulation.Tick);rows.Add(string.Join(",",i,s.Simulation.Tick,N(elapsed*1000.0/Stopwatch.Frequency),bytes,s.Transport.ActiveCount));}
   if(mode=="legacy"){
    var detail=new List<string>{"index,tick,phase,ms,calls"};var events=new List<string>{"index,start_seconds,growth_due,employment_due,start_day,projects_before,projects_after,end_day,end_seconds,gc0,gc1,gc2"};
    for(int i=0;i<measuredSteps;i++){for(int k=0;k<previousTicks.Length;k++)detail.Add(string.Join(",",i,s.Simulation.Tick-measuredSteps+i+1,(CityCoreProfile.Part)k,N(phaseTicks[i,k]*1000.0/Stopwatch.Frequency),phaseCalls[i,k]));events.Add(i+","+string.Join(",",Enumerable.Range(0,8).Select(k=>N(boundary[i,k])))+","+collections[i,0]+","+collections[i,1]+","+collections[i,2]);}
    File.WriteAllLines(Path.Combine(output,"step-phases.csv"),detail);File.WriteAllLines(Path.Combine(output,"boundaries.csv"),events);
   }
   File.WriteAllText(Path.Combine(output,"index-work.csv"),"full_builds,incremental_updates\n"+(fullBefore<0?-1:indexCounter("FullIndexBuilds")-fullBefore)+","+(deltaBefore<0?-1:indexCounter("IncrementalIndexUpdates")-deltaBefore));
   ResidentRequestProbe.Write(output);ResidentDispatchTrace.Write(output);OccupancyMovementProbe.Write(output);File.WriteAllLines(Path.Combine(output,"steps.csv"),rows);File.WriteAllLines(Path.Combine(output,"core.csv"),new[]{CityCoreProfile.Header(),CityCoreProfile.Row(s.Simulation.Tick,s.Transport.ActiveCount,0,0,0,0)});
   s.Transport.GetType().GetMethod("AssertIncrementalIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)?.Invoke(s.Transport,null);
   s.Validate();byte[] final=CityCoreSave.Write(s);File.WriteAllBytes(Path.Combine(output,"final.hcity"),final);var cold=CityCoreSave.Read(final);cold.Transport.DepotLaneRoutingEnabled=true;cold.Simulation.Growth.Zoning.SetSurface(host.ScenarioSurface);
   for(int i=0;i<20;i++){s.Simulation.Advance(.05,1,double.PositiveInfinity);cold.Simulation.Advance(.05,1,double.PositiveInfinity);}s.Validate();cold.Validate();if(!CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(cold)))throw new Exception("Cold continuation differs");
   ResidentRequestProbe.Bench(s,output);File.WriteAllText(Path.Combine(output,"Result.txt"),"PASS 2400 warm steps + 8s display warm; actual fixed steps (count in steps.csv), city safety/economy/waste validation, 20-step exact hot/cold continuation. Scripted one step/frame; these are not normal 50x FPS results. Mode="+mode);Application.Quit(0);
  }
 }
}
