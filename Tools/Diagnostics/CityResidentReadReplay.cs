using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Security.Cryptography;using HarborCity;using HarborCity.RoadCore;
static class CityResidentReadReplay
{
 static readonly List<string> rows=new List<string>{"scenario,tick,active,sha256"};
 static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
 static void Mark(string name,CityCoreIndustrySession s){s.Validate();rows.Add(string.Join(",",name,s.Simulation.Tick,s.Transport.ActiveCount,Hash(CityCoreSave.Write(s))));}
 static void Replay(string name,CityCoreIndustrySession s,int steps,string output)
 {
  s.Transport.DepotLaneRoutingEnabled=true;s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);Mark(name,s);
  for(int i=0;i<steps;i++){if(s.Simulation.Advance(.05,1,double.PositiveInfinity)!=1)throw new Exception("Lost fixed step");if(i%200==199)Mark(name,s);}
  byte[] saved=CityCoreSave.Write(s);if(name=="30000")File.WriteAllBytes(Path.Combine(output,"final.hcity"),saved);var cold=CityCoreSave.Read(saved);cold.Transport.DepotLaneRoutingEnabled=true;cold.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
  for(int i=0;i<80;i++){s.Simulation.Advance(.05,1,double.PositiveInfinity);cold.Simulation.Advance(.05,1,double.PositiveInfinity);if(i%10==9){s.Validate();cold.Validate();if(!CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(cold)))throw new Exception("Cold continuation mismatch");}}
  Mark(name+"-cold80",s);
 }
 static void Main(string[] args)
 {
  Directory.CreateDirectory(args[1]);Replay("commute-days",CityCoreIndustrySession.Create(),6000,args[1]);Replay("on-site-days",CityCoreIndustrySession.Create(true),6000,args[1]);Replay("30000",CityCoreSave.Read(File.ReadAllBytes(args[0])),int.Parse(args[2]),args[1]);File.WriteAllLines(Path.Combine(args[1],"states.csv"),rows);Console.WriteLine("PASS full-city checkpoints and 80-step exact hot/cold continuation for each scenario. Compare states.csv against frozen-source host before claiming old/new equivalence.");
 }
}
