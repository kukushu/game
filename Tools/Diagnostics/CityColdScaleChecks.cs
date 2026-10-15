using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using HarborCity;
using HarborCity.RoadCore;
static class CityColdScaleChecks{
 static CityCoreIndustrySession Read(byte[] bytes){var s=CityCoreSave.Read(bytes);s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);return s;}
 static void Main(string[] args){var bytes=File.ReadAllBytes(args[0]);var s=Read(bytes);CityCoreProfile.Reset();CityCoreProfile.Enabled=true;var watch=Stopwatch.StartNew();long allocated=GC.GetAllocatedBytesForCurrentThread();s.Advance(.05);allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;watch.Stop();CityCoreProfile.Enabled=false;s.Validate();
  File.WriteAllLines(args[1],new[]{CityCoreProfile.Header(),CityCoreProfile.Row(s.Simulation.Tick,s.Transport.ActiveCount,s.Transport.RoutePreparations,0,0,0)});
  var reference=Read(bytes);reference.Advance(.05);reference.Validate();if(!CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(reference)))throw new Exception("Attribution changed cold city state");
  Console.WriteLine("PASS exact cold-step profiling state; measured "+watch.Elapsed.TotalMilliseconds+" ms, "+allocated+" allocated bytes; diagnostic build, not normal performance");
 }
}
