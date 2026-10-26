using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Security.Cryptography;using HarborCity;using HarborCity.RoadCore;
static class CityIncrementalTrafficChecks
{
 static readonly List<string> rows=new List<string>{"scenario,tick,active,sha256"};static int oracles;
 static void Oracle(CityCoreIndustrySession s){
#if INCREMENTAL_INDEX_TEST
  s.Transport.AssertIncrementalIndex();oracles++;
#endif
 }
 static string Hash(byte[] data)=>Convert.ToHexString(SHA256.HashData(data));
 static void Mark(string name,CityCoreIndustrySession s){Oracle(s);s.Validate();rows.Add(string.Join(",",name,s.Simulation.Tick,s.Transport.ActiveCount,Hash(CityCoreSave.Write(s))));}
 static void Run(string name,CityCoreIndustrySession s,int count,int stride)
 {
  s.Transport.DepotLaneRoutingEnabled=true;s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);Mark(name,s);
  for(int i=0;i<count;i++){
   if(name=="edits"){
    if(i==40)s.Transport.SplitRoadAt(1,17);
    if(i==100)s.Transport.RemoveRoad(2);
    if(i==140)s.Transport.AddRoad(2,3,new[]{s.Transport.JunctionPosition(2),new Point(18,0,0),s.Transport.JunctionPosition(3)},lanes:2);
   }
   if(s.Simulation.Advance(.05,1,double.PositiveInfinity)!=1)throw new Exception("Lost complete step");
   if(stride>0&&i%stride==stride-1)Oracle(s);
   if(i%200==199)Mark(name,s);
  }
  Mark(name+"-end",s);var cold=CityCoreSave.Read(CityCoreSave.Write(s));cold.Transport.DepotLaneRoutingEnabled=true;cold.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
  for(int i=0;i<80;i++){s.Simulation.Advance(.05,1,double.PositiveInfinity);cold.Simulation.Advance(.05,1,double.PositiveInfinity);if(i%10==9){Oracle(s);Oracle(cold);s.Validate();cold.Validate();if(!CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(cold)))throw new Exception("Cold/hot state differs");}}
  Mark(name+"-cold80",s);
 }
 static void UnorderedLoad()
 {
  var data=CityRoadCoreFixtures.Create(120,true).Snapshot();data.vehicles.Reverse();var w=new World(data);
  for(int i=0;i<800;i++){
   w.Step();
#if INCREMENTAL_INDEX_TEST
   if(i%13==12){w.AssertIncrementalIndex();oracles++;}
#endif
   if(i%100==99){
#if INCREMENTAL_INDEX_TEST
    w.AssertIncrementalIndex();oracles++;
#endif
    w.AssertSafety();rows.Add(string.Join(",","unordered-load",w.Clock, w.ActiveCount,Hash(SaveCodec.Write(w))));
   }
  }
 }
 static void Main(string[] args){Directory.CreateDirectory(args[1]);Run("commute",CityCoreIndustrySession.Create(),6000,1);Run("edits",CityCoreIndustrySession.Create(),1200,1);Run("30000",CityCoreSave.Read(File.ReadAllBytes(args[0])),int.Parse(args[2]),127);UnorderedLoad();File.WriteAllLines(Path.Combine(args[1],"states.csv"),rows);Console.WriteLine("PASS complete city, safety/ledgers, cold continuation and reversed vehicle order; exact pre-rebuild index oracles="+oracles);}
}
