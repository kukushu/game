using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Security.Cryptography;using HarborCity;using HarborCity.RoadCore;
static class ResidentWakeReplay {
 static string Hash(CityCoreIndustrySession s)=>Convert.ToHexString(SHA256.HashData(CityCoreSave.Write(s)));

#if WAKE_SHADOW
 static void NotificationControls(){
  var s=CityCoreIndustrySession.Create();var people=s.Entities.Members.Select(m=>m.Person).ToArray();foreach(var p in people)p.retryAt=10;s.Advance(.05);
  // Remove + add keeps the count equal: a count check cannot conceal a missing structural event.
  long rebuilds=ResidentWakeShadow.Rebuilds;var removed=people[people.Length-1];s.Entities.RemoveResident(removed.id);var family=s.Entities.Members[0].Family;
  var added=new CityResident{id=s.City.society.nextCitizenId,age=30,canWork=true,location=family.home,retryAt=10};s.Entities.AddResident(family.id,added);s.Advance(.05);if(ResidentWakeShadow.Rebuilds!=rebuilds+1)throw new Exception("Same-count structural edit not notified");removed.retryAt=0;
  // A callback may wake a later member before its turn in this exact Members pass.
  var requests=new List<int>();var residents=s.Residents;var original=residents.RequestCommute;int first=people[0].id,later=people[1].id;
  residents.RequestCommute=(id,from,to,back)=>{requests.Add(id);if(id==first)s.Entities.Member(later).Person.retryAt=0;return original(id,from,to,back);};
  s.Entities.Member(first).Person.retryAt=0;s.Advance(.05);residents.RequestCommute=original;
  if(!requests.Contains(first)||!requests.Contains(later)||requests.IndexOf(first)>=requests.IndexOf(later))throw new Exception("Synchronous later-member wake/order not exercised");s.Validate();
  Console.WriteLine("PASS same-count add/remove, detached writes and synchronous later-member wake in Members order");
 }
 static void PositiveControl(){var city=CityCoreIndustrySession.Create();var person=city.Entities.Members[0].Person;foreach(var m in city.Entities.Members)m.Person.retryAt=10;city.Advance(.05);long before=ResidentWakeShadow.Requests;person.retryAt=0;city.Advance(.05);if(ResidentWakeShadow.Requests<=before||person.tripId==0)throw new Exception("Earlier deadline did not wake via public setter");Console.WriteLine("PASS earlier public deadline now wakes and dispatches");}
 static void NegativeControl(){var city=CityCoreIndustrySession.Create();var person=city.Entities.Members[0].Person;foreach(var m in city.Entities.Members)m.Person.retryAt=10;city.Advance(.05);
  // Deliberate corruption of the notification contract in this diagnostic only.
  typeof(CityResident).GetField("retry",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(person,0f);
  try{city.Advance(.05);}catch(InvalidOperationException e){if(e.Message.StartsWith("Shadow missed request at tick ")){Console.WriteLine("PASS negative control detects deliberately bypassed notification");return;}throw;}
  throw new Exception("Negative control did not expose missed notification");}

#endif
 static void LifecycleCase(List<string> rows){var s=CityCoreIndustrySession.Create();var person=s.Entities.Members[0].Person;int job=person.jobId;bool restored=false;
  for(int i=0;i<1200;i++){if(i==20)person.sick=true;if(i==60)person.sick=false;if(i==100)s.City.ReleaseJob(person);if(i>=120&&!restored&&person.tripId==0&&!person.atWork)restored=s.City.AssignJob(person,job);if(i==200)person.canWork=false;if(i==240)person.canWork=true;if(i==400)s.Transport.SplitRoadAt(1,17);
   s.Advance(.05);if(i%120==119){s.Validate();rows.Add("lifecycle-"+s.Simulation.Tick+","+Hash(s));}}
  var cold=CityCoreSave.Read(CityCoreSave.Write(s));for(int i=0;i<80;i++){s.Advance(.05);cold.Advance(.05);}s.Validate();cold.Validate();if(Hash(s)!=Hash(cold))throw new Exception("Lifecycle cold replay differs");rows.Add("lifecycle-cold80,"+Hash(s));}
 static void Main(string[] args){
#if WAKE_SHADOW
 if(args.Length>2&&args[2]=="negative-control"){NotificationControls();PositiveControl();NegativeControl();return;}
#endif
 var s=CityCoreSave.Read(File.ReadAllBytes(args[0]));s.Transport.DepotLaneRoutingEnabled=true;s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);for(int i=0;i<2400;i++)s.Advance(.05);
  var rows=new List<string>{"tick,sha256"};rows.Add(s.Simulation.Tick+","+Hash(s));
  for(int i=0;i<3000;i++){if(s.Advance(.05)!=1)throw new Exception("Lost fixed step");if((i+1)%240==0)rows.Add(s.Simulation.Tick+","+Hash(s));}
  s.Validate();byte[] saved=CityCoreSave.Write(s);File.WriteAllBytes(Path.Combine(args[1],"final.hcity"),saved);var cold=CityCoreSave.Read(saved);cold.Transport.DepotLaneRoutingEnabled=true;cold.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
  for(int i=0;i<80;i++){s.Advance(.05);cold.Advance(.05);}s.Validate();cold.Validate();if(!CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(cold)))throw new Exception("Cold replay differs");
  LifecycleCase(rows);File.WriteAllLines(Path.Combine(args[1],"states.csv"),rows);Console.WriteLine("PASS full real city checkpoints + cross-day 3000 steps + 80-step exact cold continuation");
#if WAKE_SHADOW
  Console.WriteLine(ResidentWakeShadow.Summary());
#endif
 }
}
