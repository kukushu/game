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
   var rows=new List<string>{"index,tick,step_ms,allocated_bytes,active_vehicles"};ResidentRequestProbe.Reset(mode=="counts"||mode=="sampled",mode=="sampled",64);ResidentDispatchTrace.Reset(mode=="retry");CityCoreProfile.Reset();
   for(int i=0;i<600;i++){yield return null;CityCoreProfile.Enabled=mode=="legacy";long b=GC.GetAllocatedBytesForCurrentThread(),start=Stopwatch.GetTimestamp();if(s.Simulation.Advance(.05,1,double.PositiveInfinity)!=1)throw new Exception("Measured step lost");long elapsed=Stopwatch.GetTimestamp()-start,bytes=allocationSupported?GC.GetAllocatedBytesForCurrentThread()-b:-1;CityCoreProfile.Enabled=false;rows.Add(string.Join(",",i,s.Simulation.Tick,N(elapsed*1000.0/Stopwatch.Frequency),bytes,s.Transport.ActiveCount));}
   ResidentRequestProbe.Write(output);ResidentDispatchTrace.Write(output);File.WriteAllLines(Path.Combine(output,"steps.csv"),rows);File.WriteAllLines(Path.Combine(output,"core.csv"),new[]{CityCoreProfile.Header(),CityCoreProfile.Row(s.Simulation.Tick,s.Transport.ActiveCount,0,0,0,0)});
   s.Validate();byte[] final=CityCoreSave.Write(s);File.WriteAllBytes(Path.Combine(output,"final.hcity"),final);var cold=CityCoreSave.Read(final);cold.Transport.DepotLaneRoutingEnabled=true;cold.Simulation.Growth.Zoning.SetSurface(host.ScenarioSurface);
   for(int i=0;i<20;i++){s.Simulation.Advance(.05,1,double.PositiveInfinity);cold.Simulation.Advance(.05,1,double.PositiveInfinity);}s.Validate();cold.Validate();if(!CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(cold)))throw new Exception("Cold continuation differs");
   ResidentRequestProbe.Bench(s,output);File.WriteAllText(Path.Combine(output,"Result.txt"),"PASS 2400 warm steps + 8s display warm; 600 actual fixed steps, city safety/economy/waste validation, 20-step exact hot/cold continuation. Scripted one step/frame; these are not normal 50x FPS results. Mode="+mode);Application.Quit(0);
  }
 }
}
