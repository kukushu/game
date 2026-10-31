using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using HarborCity;
using HarborCity.RoadCore;
static class CityColdScaleChecks{
 static CityCoreIndustrySession Read(byte[] bytes){var s=CityCoreSave.Read(bytes);s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);return s;}
 static void Main(string[] args){int warm=args.Length>2?int.Parse(args[2]):0,steps=args.Length>3?int.Parse(args[3]):1;if(warm<0||warm>20000||steps<1||steps>4000)throw new ArgumentOutOfRangeException("Bounded warmup/window required");var bytes=File.ReadAllBytes(args[0]);var s=Read(bytes);for(int i=0;i<warm;i++){if(s.Advance(.05)!=1)throw new Exception("Warm step lost");if(i%600==599)Console.WriteLine("Warm tick "+s.Simulation.Tick);}byte[] warmState=warm==0?bytes:CityCoreSave.Write(s);if(warm>0)File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(args[1]),"window-start.hcity"),warmState);long startTick=s.Simulation.Tick;var rows=new System.Collections.Generic.List<string>{CityCoreProfile.Header()};CityCoreProfile.Reset();CityCoreProfile.Enabled=true;int g0=GC.CollectionCount(0),g1=GC.CollectionCount(1),g2=GC.CollectionCount(2);var watch=new Stopwatch();long allocated=0;for(int i=0;i<steps;i++){long before=GC.GetAllocatedBytesForCurrentThread();watch.Start();if(s.Advance(.05)!=1)throw new Exception("Measured step lost");watch.Stop();allocated+=GC.GetAllocatedBytesForCurrentThread()-before;rows.Add(CityCoreProfile.Row(s.Simulation.Tick,s.Transport.ActiveCount,s.Transport.RoutePreparations,GC.CollectionCount(0)-g0,GC.CollectionCount(1)-g1,GC.CollectionCount(2)-g2));}CityCoreProfile.Enabled=false;s.Validate();File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(args[1]),"window-final.hcity"),CityCoreSave.Write(s));
  File.WriteAllLines(args[1],new[]{CityCoreProfile.Header(),CityCoreProfile.Row(s.Simulation.Tick,s.Transport.ActiveCount,s.Transport.RoutePreparations,GC.CollectionCount(0)-g0,GC.CollectionCount(1)-g1,GC.CollectionCount(2)-g2)});
  File.WriteAllLines(Path.Combine(Path.GetDirectoryName(args[1]),"window-cumulative.csv"),rows);
  var reference=Read(warmState);for(int i=0;i<steps;i++)if(reference.Advance(.05)!=1)throw new Exception("Reference step lost");reference.Validate();if(!CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(reference)))throw new Exception("Attribution changed city state");
  if(!bytes.SequenceEqual(File.ReadAllBytes(args[0])))throw new Exception("Input save changed");
  Console.WriteLine("PASS exact profiling on/off city state; ticks "+startTick+" -> "+s.Simulation.Tick+"; measured "+watch.Elapsed.TotalMilliseconds+" ms, "+(CityCoreProfile.AllocationAvailable?allocated:-1)+" allocated bytes; warm steps "+warm+" outside timing; diagnostic model, not Player performance");
 }
}
