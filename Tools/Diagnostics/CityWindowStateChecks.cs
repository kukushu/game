using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarborCity;
using HarborCity.RoadCore;
static class CityWindowStateChecks
{
 static byte[] Business(CityCoreIndustrySession s)
 {
  var snapshot=s.SnapshotBusiness();snapshot.clock.pendingSeconds=0; // Independent DTO only; not the live clock.
  using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream))
  {typeof(CityCoreSave).GetMethod("Put",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{writer,typeof(CityCoreBusinessData),snapshot});writer.Flush();return stream.ToArray();}
 }
 static void Main(string[] args)
 {
  var a=CityCoreSave.Read(File.ReadAllBytes(args[0]));var b=CityCoreSave.Read(File.ReadAllBytes(args[1]));
  int aligned=0;
  if(args.Length>2&&args[2]=="align"){
   a.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);b.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
   if(Math.Abs(a.Simulation.Tick-b.Simulation.Tick)>10)throw new Exception("Window alignment limited to ten explicit normal steps");
   while(a.Simulation.Tick<b.Simulation.Tick){if(a.Advance(.05)!=1)throw new Exception("Unexpected earlier window step debt");aligned++;}
   while(b.Simulation.Tick<a.Simulation.Tick){if(b.Advance(.05)!=1)throw new Exception("Unexpected earlier window step debt");aligned++;}
  }
  if(a.Simulation.Tick!=b.Simulation.Tick)throw new Exception("Different simulation ticks; cannot claim complete same-tick equality");
  double da=a.Simulation.ClockSnapshot.pendingSeconds,db=b.Simulation.ClockSnapshot.pendingSeconds;
  if(da<0||db<0||da>=.05||db>=.05)throw new Exception("Window retains full-step debt");
  if(!Business(a).SequenceEqual(Business(b))||!SaveCodec.Write(a.Transport).SequenceEqual(SaveCodec.Write(b.Transport)))throw new Exception("Same-tick complete business/transport facts differ");
  Console.WriteLine("PASS all business and transport save facts identical at tick "+a.Simulation.Tick+"; explicitly aligned normal steps="+aligned+"; only normalized wall-clock fractional debt "+da+" / "+db);
 }
}
