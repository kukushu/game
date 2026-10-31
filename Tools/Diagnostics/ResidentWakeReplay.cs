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
#if FULL_WAKE_SHADOW
 static CityCoreIndustrySession Alone(bool atWork){var s=CityCoreIndustrySession.Create();foreach(var m in s.Entities.Members)m.Person.retryAt=100000;var p=s.Entities.Members[0].Person;p.retryAt=0;if(atWork){p.location=1;p.atWork=true;p.arrivedDay=s.City.day;}return s;}
 static void FullControls(){
  foreach(string reason in new[]{"funding","sickness","job","availability"}){
   var s=Alone(reason!="availability");var p=s.Entities.Members[0].Person;
   if(reason=="availability")s.Transport.SetEndpointAvailable(1,false);
   int calls=0;var dispatch=s.Residents.RequestCommute;s.Residents.RequestCommute=(id,from,to,back)=>{if(id==p.id)calls++;return dispatch(id,from,to,back);};
   long plans=ResidentWakeShadow.TemporalPlans;s.Advance(.05);if(calls!=0||ResidentWakeShadow.TemporalPlans<=plans)throw new Exception("Full control did not sleep: "+reason);
   if(reason=="funding")s.City.Factory(1).cash=0;
   if(reason=="sickness")p.sick=true;
   if(reason=="job")s.City.ReleaseJob(p);
   if(reason=="availability")s.Transport.SetEndpointAvailable(1,true);
   s.Advance(.05);if(calls!=1)throw new Exception("Full control did not wake: "+reason);
  }
  var facts=Alone(false);int factNotices=0;facts.Entities.ResidentStateChanged+=(m,r)=>{if(r==CityResidentStateChange.TripDay||r==CityResidentStateChange.CommuteObservation)factNotices++;};var fp=facts.Entities.Members[0].Person;fp.departureDay=3;fp.arrivedDay=4;fp.observedHome=3;fp.observedWork=1;fp.observedRevision=99;int expected=5;foreach(int bits in new[]{0,unchecked((int)0x80000000),unchecked((int)0x7fc00001),unchecked((int)0xffc00123),0x7f800000}){fp.lastCommute=BitConverter.Int32BitsToSingle(bits);expected++;if(BitConverter.SingleToInt32Bits(fp.lastCommute)!=bits||factNotices!=expected)throw new Exception("Temporal fact notification/float bits changed");}
  var dayFact=Alone(true);dayFact.Advance(.05);int dayCalls=0;dayFact.Residents.RequestCommute=(id,from,to,back)=>{dayCalls++;return null;};dayFact.Entities.Members[0].Person.arrivedDay=0;dayFact.Advance(.05);if(dayCalls!=1)throw new Exception("Arrival-day fact did not wake timed return");
  var observation=Alone(false);observation.City.society.settings.secondsPerDay=1000;var observed=observation.Entities.Members[0].Person;observed.observedHome=3;observed.observedWork=1;observed.observedRevision=observation.Transport.NetworkGeneration;observed.lastCommute=0;int observedCalls=0;observation.Residents.RequestCommute=(id,from,to,back)=>{observedCalls++;return null;};observation.Advance(.05);if(observedCalls!=0)throw new Exception("Observation control did not sleep");observed.lastCommute=600;observation.Advance(.05);if(observedCalls!=1)throw new Exception("Commute observation did not wake earlier departure");
  var mode=Alone(true);mode.Advance(.05);long global=ResidentWakeShadow.GlobalChanges;mode.City.development.enabled=true;mode.Advance(.05);if(ResidentWakeShadow.GlobalChanges<=global)throw new Exception("Mode-only shift policy not invalidated");
  var moved=Alone(false);moved.City.society.settings.secondsPerDay=1000;moved.Entities.AddBuilding(new ResidentialBuilding{id=4,housingUnits=1});moved.Transport.RegisterAccess(4,Network.LaneId(2,false),20);int homeCalls=0;var homeDispatch=moved.Residents.RequestCommute;moved.Residents.RequestCommute=(id,from,to,back)=>{if(id==moved.Entities.Members[0].Person.id)homeCalls++;return homeDispatch(id,from,to,back);};moved.Advance(.05);if(homeCalls!=0)throw new Exception("Home control did not sleep");moved.Entities.Members[0].Family.home=4;moved.Advance(.05);if(homeCalls!=1)throw new Exception("Home change failed to wake return");
  // A plan for a missing endpoint must not learn from noncandidate oracle visits.
  var bad=Alone(false);bad.Transport.SetEndpointAvailable(1,false);bad.Advance(.05);
  typeof(CityCoreResidents).GetField("plansInvalidated",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(bad.Residents,null);
  bad.Transport.SetEndpointAvailable(1,true);bool detected=false;
  try{bad.Advance(.05);}catch(InvalidOperationException e){if(!e.Message.StartsWith("Shadow missed request at tick "))throw;detected=true;}
  if(!detected)throw new Exception("Missing endpoint consumer negative control not detected");
  var state=Alone(true);state.Advance(.05);typeof(CityResident).GetField("ill",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(state.Entities.Members[0].Person,true);detected=false;
  try{state.Advance(.05);}catch(InvalidOperationException e){if(!e.Message.StartsWith("Shadow missed request at tick "))throw;detected=true;}
  if(!detected)throw new Exception("Missing sickness consumer negative control not detected");
  var funding=Alone(true);funding.Advance(.05);typeof(FactoryState).GetField("balance",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(funding.City.Factory(1),0d);detected=false;
  try{funding.Advance(.05);}catch(InvalidOperationException e){if(!e.Message.StartsWith("Shadow missed request at tick "))throw;detected=true;}
  if(!detected)throw new Exception("Missing funding consumer negative control not detected");
  Console.WriteLine("PASS full temporal sleep + funding/sickness/job/unreachable endpoint wakes; endpoint/state bypass negative controls detected");
 }
#endif
 static void PositiveControl(){var city=CityCoreIndustrySession.Create();var person=city.Entities.Members[0].Person;foreach(var m in city.Entities.Members)m.Person.retryAt=10;city.Advance(.05);long before=ResidentWakeShadow.Requests;person.retryAt=0;city.Advance(.05);if(ResidentWakeShadow.Requests<=before||person.tripId==0)throw new Exception("Earlier deadline did not wake via public setter");Console.WriteLine("PASS earlier public deadline now wakes and dispatches");}
 static void NegativeControl(){var city=CityCoreIndustrySession.Create();var person=city.Entities.Members[0].Person;foreach(var m in city.Entities.Members)m.Person.retryAt=10;city.Advance(.05);
  // Deliberate corruption of the notification contract in this diagnostic only.
  typeof(CityResident).GetField("retry",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(person,0f);
  try{city.Advance(.05);}catch(InvalidOperationException e){if(e.Message.StartsWith("Shadow missed request at tick ")){Console.WriteLine("PASS negative control detects deliberately bypassed notification");return;}throw;}
  throw new Exception("Negative control did not expose missed notification");}

#endif
 static void DemolitionCase(List<string> rows){var s=CityCoreIndustrySession.Create();s.Advance(.05);var actor=s.Transport.Vehicles.FirstOrDefault(v=>v.status==VehicleStatus.Active&&!s.Transport.Network.Get(v.Current).IsConnection);if(actor==null)throw new Exception("Demolition fixture has no moving vehicle");int road=s.Transport.Network.Get(actor.Current).RoadId;s.Transport.RemoveRoad(road);for(int i=0;i<100;i++)s.Advance(.05);s.Validate();rows.Add("demolition100,"+Hash(s));var cold=CityCoreSave.Read(CityCoreSave.Write(s));for(int i=0;i<80;i++){s.Advance(.05);cold.Advance(.05);}s.Validate();cold.Validate();if(Hash(s)!=Hash(cold))throw new Exception("Demolition cold replay differs");rows.Add("demolition-cold80,"+Hash(s));}
 static void LifecycleCase(List<string> rows){var s=CityCoreIndustrySession.Create();var person=s.Entities.Members[0].Person;int job=person.jobId;bool restored=false;
  for(int i=0;i<1200;i++){if(i==20)person.sick=true;if(i==60)person.sick=false;if(i==100)s.City.ReleaseJob(person);if(i>=120&&!restored&&person.tripId==0&&!person.atWork)restored=s.City.AssignJob(person,job);if(i==200)person.canWork=false;if(i==240)person.canWork=true;if(i==400)s.Transport.SplitRoadAt(1,17);
   s.Advance(.05);if(i%120==119){s.Validate();rows.Add("lifecycle-"+s.Simulation.Tick+","+Hash(s));}}
  var cold=CityCoreSave.Read(CityCoreSave.Write(s));for(int i=0;i<80;i++){s.Advance(.05);cold.Advance(.05);}s.Validate();cold.Validate();if(Hash(s)!=Hash(cold))throw new Exception("Lifecycle cold replay differs");rows.Add("lifecycle-cold80,"+Hash(s));}
 static void Main(string[] args){
#if WAKE_SHADOW
 if(args.Length>2&&args[2]=="negative-control"){NotificationControls();PositiveControl();NegativeControl();
#if FULL_WAKE_SHADOW
 FullControls();
#endif
 return;}
#endif
 var s=CityCoreSave.Read(File.ReadAllBytes(args[0]));s.Transport.DepotLaneRoutingEnabled=true;s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);for(int i=0;i<2400;i++)s.Advance(.05);
  long callCount=0;using var trace=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);var traceBytes=new byte[28];var dispatch=s.Residents.RequestCommute;
  s.Residents.RequestCommute=(id,from,to,back)=>{var v=dispatch(id,from,to,back);BitConverter.TryWriteBytes(traceBytes.AsSpan(0,8),s.Simulation.Tick);BitConverter.TryWriteBytes(traceBytes.AsSpan(8,4),id);BitConverter.TryWriteBytes(traceBytes.AsSpan(12,4),from);BitConverter.TryWriteBytes(traceBytes.AsSpan(16,4),to);BitConverter.TryWriteBytes(traceBytes.AsSpan(20,4),back?1:0);BitConverter.TryWriteBytes(traceBytes.AsSpan(24,4),v==null?0:v.id);trace.AppendData(traceBytes);callCount++;return v;};
  long retainedHeap=GC.GetTotalMemory(true);var durations=new double[3000];long allocation=0;var timer=new System.Diagnostics.Stopwatch();
  var rows=new List<string>{"tick,sha256"};rows.Add(s.Simulation.Tick+","+Hash(s));
  for(int i=0;i<3000;i++){long before=GC.GetAllocatedBytesForCurrentThread();timer.Restart();if(s.Advance(.05)!=1)throw new Exception("Lost fixed step");timer.Stop();durations[i]=timer.Elapsed.TotalMilliseconds;allocation+=GC.GetAllocatedBytesForCurrentThread()-before;if((i+1)%240==0)rows.Add(s.Simulation.Tick+","+Hash(s));}
  File.WriteAllLines(Path.Combine(args[1],"requests.csv"),new[]{"requests,sha256",callCount+","+Convert.ToHexString(trace.GetHashAndReset())});Array.Sort(durations);File.WriteAllLines(Path.Combine(args[1],"step-cost.csv"),new[]{"steps,mean_ms,p95_ms,p99_ms,max_ms,allocated_per_step,retained_managed_bytes_after_warmup",string.Join(",",durations.Length,durations.Average().ToString("R",System.Globalization.CultureInfo.InvariantCulture),durations[2849].ToString("R",System.Globalization.CultureInfo.InvariantCulture),durations[2969].ToString("R",System.Globalization.CultureInfo.InvariantCulture),durations[2999].ToString("R",System.Globalization.CultureInfo.InvariantCulture),(allocation/3000d).ToString("R",System.Globalization.CultureInfo.InvariantCulture),retainedHeap)});
  s.Validate();byte[] saved=CityCoreSave.Write(s);File.WriteAllBytes(Path.Combine(args[1],"final.hcity"),saved);var cold=CityCoreSave.Read(saved);cold.Transport.DepotLaneRoutingEnabled=true;cold.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
  for(int i=0;i<80;i++){s.Advance(.05);cold.Advance(.05);}s.Validate();cold.Validate();if(!CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(cold)))throw new Exception("Cold replay differs");
  LifecycleCase(rows);DemolitionCase(rows);File.WriteAllLines(Path.Combine(args[1],"states.csv"),rows);Console.WriteLine("PASS full real city checkpoints + cross-day 3000 steps + 80-step exact cold continuation");
#if WAKE_SHADOW
  Console.WriteLine(ResidentWakeShadow.Summary());
#endif
 }
}
