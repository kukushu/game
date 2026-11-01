using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using System.Security.Cryptography;using HarborCity;using HarborCity.RoadCore;
static class CityResidentFundingWaitChecks {
 static int checks;static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
 static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
 static void Cash(FactoryState f,double next){f.capital+=next-f.cash;f.cash=next;}
 static CityCoreIndustrySession Fixture(){var design=new GameDesignData();design.employment.commercialShiftStartMinute=480;var seed=CityCoreIndustrySession.Create();var s=new CityCoreIndustrySession(seed.City,seed.Simulation.CalendarOffset,seed.Transport.Snapshot(),seed.Simulation.ClockSnapshot,design:design);foreach(var m in s.Entities.Members)m.Person.retryAt=0;Cash(s.City.Factory(1),0);return s;}
 static void Controls(string output){var rows=new List<string>{"case,requests,sha256"};
 foreach(string name in new[]{"restore","net-zero","tiny","away","sick","residence","reassign","later","earlier","shop-mode","replacement","detach"}){
  var s=Fixture();var people=s.Entities.Members.Select(m=>m.Person).ToArray();var f=s.City.Factory(1);var trace=new List<string>();
  if(name=="shop-mode"||name=="replacement"){s.City.ReleaseJob(people[0]);Check(s.City.AssignJob(people[0],s.City.society.jobEntities.First(j=>j.buildingId==2&&j.occupiedCitizenId<0).id),"Shop job unavailable");foreach(var h in s.City.society.families)h.openingSavings=h.savings;s.City.development.enabled=true;}
  s.Residents.RequestCommute=(id,from,to,back)=>{trace.Add(id+":"+back);if((name=="later"&&id==people[0].id)||(name=="earlier"&&id==people[1].id))Cash(f,100);return null;};
  s.Residents.AdvanceRequests();Check(trace.Count==0,"Unfunded home requested "+name);
  if(name=="restore"||name=="tiny")Cash(f,name=="tiny"?double.Epsilon:100);
  if(name=="net-zero"){Cash(f,100);Cash(f,0);}
  if(name=="away"){people[0].location=1;people[0].atWork=false;}
  if(name=="sick"){people[0].sick=true;Cash(f,100);s.Residents.AdvanceRequests();Check(!trace.Any(x=>x.StartsWith(people[0].id+":")),"Sick member departed");trace.Clear();foreach(var p in people)p.retryAt=0;people[0].sick=false;}
  if(name=="residence"){var h=s.Entities.Members[0].Family;h.resident=false;Cash(f,100);s.Residents.AdvanceRequests();Check(trace.Count==0,"Nonresident departed");h.resident=true;}
  if(name=="reassign"){s.City.ReleaseJob(people[0]);Check(s.City.AssignJob(people[0],s.City.society.jobEntities.First(j=>j.buildingId==2&&j.occupiedCitizenId<0).id),"Reassignment failed");}
  if(name=="later")people[0].location=2;
  if(name=="earlier")people[1].location=2;
  if(name=="shop-mode")s.City.development.enabled=false;
  if(name=="replacement"){var shop=s.Entities.Shops[0];shop.Account.Capital+=100-shop.Account.Cash;shop.Account.Cash=100;var copy=(CommercialBuilding)BuildingSaveData.FromBuilding(shop).ToBuilding();s.Entities.ReplaceBuilding(copy);Check(ReferenceEquals(copy.Account,shop.Account),"Account ownership lost");}
  if(name=="detach"){s.Entities.RemoveResident(people[3].id);Cash(f,100);}
  s.Residents.AdvanceRequests();if(name=="net-zero")Check(trace.Count==0,"Transient credit emitted request");else Check(trace.Count>0,"Funding wake missing "+name);
  if(name=="earlier")Check(!trace.Any(x=>x.StartsWith(people[0].id+":"))&&trace[0].StartsWith(people[1].id+":"),"Revisited earlier member");
  if(name=="later")Check(trace[0].StartsWith(people[0].id+":"),"Later wake reordered pass");
  rows.Add(name+","+string.Join(";",trace)+","+Hash(CityCoreSave.Write(s)));
 }
 foreach(string name in new[]{"deplete-later","retry-funding"}){
  var s=Fixture();var f=s.City.Factory(1);var people=s.Entities.Members.Select(m=>m.Person).ToArray();var trace=new List<string>();
  s.Residents.RequestCommute=(id,a,b,back)=>{trace.Add(s.Simulation.Tick+":"+id);if(name=="deplete-later"&&id==people[0].id)Cash(f,0);return null;};
  if(name=="deplete-later"){Cash(f,100);s.Residents.AdvanceRequests();Check(trace.Count==1,"Later member ignored exhausted funds");}
  else{s.Residents.AdvanceRequests();foreach(var p in people)p.retryAt=.05f;Cash(f,100);s.Advance(.05);s.Advance(.05);Check(trace.Count==0,"Funding wake bypassed exact retry guard");s.Advance(.05);Check(trace.Count>0,"Funding retry never woke");}
  rows.Add(name+","+string.Join(";",trace)+","+Hash(CityCoreSave.Write(s)));
 }
 var pending=Fixture();pending.Residents.AdvanceRequests();Cash(pending.City.Factory(1),100);var recovered=CityCoreSave.Read(CityCoreSave.Write(pending));var left=new List<int>();var right=new List<int>();pending.Residents.RequestCommute=(id,a,b,c)=>{left.Add(id);return null;};recovered.Residents.RequestCommute=(id,a,b,c)=>{right.Add(id);return null;};pending.Residents.AdvanceRequests();recovered.Residents.AdvanceRequests();Check(left.Count>0&&left.SequenceEqual(right)&&CityCoreSave.Write(pending).SequenceEqual(CityCoreSave.Write(recovered)),"Pending income save/cold wake differs");rows.Add("pending-cold,"+string.Join(";",left)+","+Hash(CityCoreSave.Write(pending)));
 File.WriteAllLines(Path.Combine(output,"controls.csv"),rows);
 var bad=Fixture();bad.Residents.AdvanceRequests();var factory=bad.City.Factory(1);factory.capital+=100;typeof(FactoryState).GetField("balance",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(factory,100d);
 var cold=CityCoreSave.Read(CityCoreSave.Write(bad));int warmCount=0,coldCount=0;bad.Residents.RequestCommute=(a,b,c,d)=>{warmCount++;return null;};cold.Residents.RequestCommute=(a,b,c,d)=>{coldCount++;return null;};bad.Residents.AdvanceRequests();cold.Residents.AdvanceRequests();
#if CITY_RESIDENT_FUNDING_SCHEDULE
 Check(warmCount==0&&coldCount>0,"Missing funding notification escaped independent cold oracle");
#else
 Check(warmCount==coldCount&&coldCount>0,"Polling negative-control fixture invalid");
#endif
 Console.WriteLine("PASS "+checks+" funding wait controls and missing-notification negative control");
 }
 static void Replay(string input,string output,int steps){var s=CityCoreSave.Read(File.ReadAllBytes(input));s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);s.Transport.DepotLaneRoutingEnabled=true;
 var original=s.Residents.RequestCommute;var trace=new BinaryWriter(File.Create(Path.Combine(output,"requests.bin")));s.Residents.RequestCommute=(id,from,to,back)=>{var v=original(id,from,to,back);trace.Write(s.Simulation.Tick);trace.Write(id);trace.Write(from);trace.Write(to);trace.Write(back);trace.Write(v?.id??0);return v;};
 var cost=new List<string>{"tick,step_ms,allocated_bytes"};var rows=new List<string>{"tick,sha256"};try{for(int i=0;i<steps;i++){long bytes=GC.GetAllocatedBytesForCurrentThread();var clock=System.Diagnostics.Stopwatch.StartNew();Check(s.Simulation.Advance(.05,1,double.PositiveInfinity)==1,"Lost step");clock.Stop();cost.Add(string.Join(",",s.Simulation.Tick,clock.Elapsed.TotalMilliseconds.ToString("R",System.Globalization.CultureInfo.InvariantCulture),GC.GetAllocatedBytesForCurrentThread()-bytes));if(i==199){long retained=GC.GetTotalMemory(true);File.WriteAllText(Path.Combine(output,"managed-after200.csv"),"tick,managed_retained_bytes\n"+s.Simulation.Tick+","+retained);}if(i%400==399||i==steps-1){byte[] data=CityCoreSave.Write(s);rows.Add(s.Simulation.Tick+","+Hash(data));if(i==steps-1)File.WriteAllBytes(Path.Combine(output,"final.hcity"),data);Console.WriteLine("State tick "+s.Simulation.Tick);}}}finally{s.Residents.RequestCommute=original;trace.Dispose();}
 File.WriteAllLines(Path.Combine(output,"cost.csv"),cost);File.WriteAllLines(Path.Combine(output,"states.csv"),rows);
 var schedule=s.Residents.GetType().GetField("guardSchedule",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(s.Residents);if(schedule!=null)File.WriteAllLines(Path.Combine(output,"schedule.csv"),new[]{"metric,value"}.Concat(new[]{"Rebuilds","Changes","FundingChecks","FundingSleeps"}.Select(n=>n+","+schedule.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(schedule))));var cold=CityCoreSave.Read(File.ReadAllBytes(Path.Combine(output,"final.hcity")));cold.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);cold.Transport.DepotLaneRoutingEnabled=true;for(int i=0;i<80;i++){s.Advance(.05);cold.Advance(.05);}Check(CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(cold)),"Funding wait cold80 differs");Console.WriteLine("PASS original fixed steps, full states, request results and cold80; compare all builds");
 }
 static void Main(string[] args){Directory.CreateDirectory(args[1]);if(args[0]=="controls")Controls(args[1]);else Replay(args[2],args[1],int.Parse(args[3]));}
}
