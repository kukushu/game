using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Security.Cryptography;using System.Reflection;using HarborCity;using HarborCity.RoadCore;
static class CityResidentTemporalChecks {
 static string Hash(CityCoreIndustrySession s)=>Convert.ToHexString(SHA256.HashData(CityCoreSave.Write(s)));
 static CityCoreIndustrySession Alone(bool work=false){var s=CityCoreIndustrySession.Create();foreach(var m in s.Entities.Members)m.Person.retryAt=100000;var p=s.Entities.Members[0].Person;p.retryAt=0;if(work){p.location=1;p.atWork=true;p.arrivedDay=s.City.day;}else s.City.society.settings.secondsPerDay=1000;return s;}
 static void Main(string[] args){var rows=new List<string>{"case,requests,sha256"};
  foreach(string name in new[]{"observation","elapsed","period","mode","funding","sickness","job","unreachable","road-cost","stale-edit","later-member"}){
   bool work=name=="funding"||name=="sickness"||name=="job";var s=Alone(work);var p=s.Entities.Members[0].Person;
   if(name=="observation"){p.observedHome=3;p.observedWork=1;p.observedRevision=s.Transport.NetworkGeneration;p.lastCommute=0;}
   if(name=="mode"){s.City.society.settings.secondsPerDay=120;foreach(var h in s.City.society.families)h.openingSavings=h.savings;s.City.development.enabled=true;var a=s.Entities.Shops[0].Account;a.Capital+=100-a.Cash;a.Cash=100;s.City.ReleaseJob(p);if(!s.City.AssignJob(p,s.City.society.jobEntities.First(j=>j.buildingId==2&&j.occupiedCitizenId<0).id))throw new Exception("Commercial assignment failed");}
   if(name=="unreachable"){s.City.society.settings.secondsPerDay=120;s.Transport.SetEndpointAvailable(1,false);}
   var calls=new List<string>();s.Residents.RequestCommute=(id,from,to,back)=>{calls.Add(s.Simulation.Tick+":"+id+":"+back);if(name=="later-member"&&id==s.Entities.Members[0].Person.id){var later=s.Entities.Members[1].Person;later.observedHome=3;later.observedWork=1;later.observedRevision=s.Transport.NetworkGeneration;later.lastCommute=600;later.retryAt=0;}return null;};
   s.Advance(.05);if(calls.Count!=0)throw new Exception("Waiting fixture requested early: "+name);
   if(name=="observation")p.lastCommute=600;
   if(name=="elapsed"){s.City.society.dayElapsed=600f/1440*1000;s.Residents.AdvanceRequests();}
   if(name=="period")s.City.society.settings.secondsPerDay=120;
   if(name=="mode")s.City.development.enabled=false;
   if(name=="funding"){var f=s.City.Factory(1);f.capital-=f.cash;f.cash=0;}
   if(name=="sickness")p.sick=true;
   if(name=="job")s.City.ReleaseJob(p);
   if(name=="unreachable")s.Transport.SetEndpointAvailable(1,true);
   if(name=="road-cost"&&!s.Transport.PlanUpgradeRoad(1,2,.01,RoadDirection.Both).TryCommit())throw new Exception("Speed edit failed");
   if(name=="stale-edit"){var edit=s.Transport.PlanUpgradeRoad(1,2,10,RoadDirection.Both);s.Transport.RefreshEndpoints();if(edit.TryCommit())throw new Exception("Stale edit committed");}
   if(name=="later-member"){p.observedHome=3;p.observedWork=1;p.observedRevision=s.Transport.NetworkGeneration;p.lastCommute=600;}
   for(int i=0;i<12;i++)s.Advance(.05);
   if(name!="stale-edit"&&calls.Count==0)throw new Exception("No actual request after wake: "+name);
   if(name=="stale-edit"&&calls.Count!=0)throw new Exception("Failed edit changed departure time");
   rows.Add(name+","+string.Join(";",calls)+","+Hash(s));
  }
  var longRun=Alone();var trace=new List<string>();longRun.Residents.RequestCommute=(id,a,b,back)=>{trace.Add(longRun.Simulation.Tick+":"+id+":"+back);return null;};for(int i=0;i<3000;i++)longRun.Advance(.05);var cold=CityCoreSave.Read(CityCoreSave.Write(longRun));cold.Residents.RequestCommute=(a,b,c,d)=>null;for(int i=0;i<3600;i++){longRun.Advance(.05);cold.Advance(.05);}if(trace.Count==0||Hash(longRun)!=Hash(cold))throw new Exception("Scheduled first departure or cold continuation differs");rows.Add("long-window-cold,"+string.Join(";",trace)+","+Hash(longRun));
  File.WriteAllLines(Path.Combine(args[0],"controls.csv"),rows);
  // Bypass the observation setter deliberately. An independently cold-built
  // oracle must disagree, before any validator can repair a derived schedule.
  var bad=Alone();var person=bad.Entities.Members[0].Person;person.observedHome=3;person.observedWork=1;person.observedRevision=bad.Transport.NetworkGeneration;person.lastCommute=0;bad.Advance(.05);typeof(CityResident).GetField("commute",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(person,600f);var oracle=CityCoreSave.Read(CityCoreSave.Write(bad));int warmCalls=0,coldCalls=0;bad.Residents.RequestCommute=(a,b,c,d)=>{warmCalls++;return null;};oracle.Residents.RequestCommute=(a,b,c,d)=>{coldCalls++;return null;};bad.Residents.AdvanceRequests();oracle.Residents.AdvanceRequests();
#if CITY_RESIDENT_TIME_SCHEDULE
  if(warmCalls!=0||coldCalls!=1)throw new Exception("Missed observation negative control not exposed");
#else
  if(warmCalls!=1||coldCalls!=1)throw new Exception("Original polling oracle invalid");
#endif
  Console.WriteLine("PASS 12 temporal cases, first scheduled departure/cold continuation, independent missed-observation negative control");
 }
}
