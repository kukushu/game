using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Security.Cryptography;using System.Reflection;using HarborCity;using HarborCity.RoadCore;
static class CityPlanInvalidationChecks {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static string Hash(CityCoreIndustrySession s)=>Convert.ToHexString(SHA256.HashData(CityCoreSave.Write(s)));
#if PLAN_INVALIDATIONS
 static readonly List<string> cases=new List<string>{"case,scope,endpoint,reason"};
 static void Targeted(string output){
  var s=CityCoreIndustrySession.Create();var events=new List<ResidentPlanInvalidation>();Action<ResidentPlanInvalidation> observer=e=>events.Add(e);s.Residents.PlansInvalidated+=observer;
  Action<string,bool,int,ResidentPlanChange> take=(name,all,id,reason)=>{s.Residents.FlushPlanInvalidations();Check(events.Count==1,name+" event count="+events.Count);var e=events[0];Check(e.All==all&&e.Endpoint==id&&e.Reason==reason,name+" wrong scope "+e.Reason);cases.Add(name+","+all+","+id+","+(int)reason);events.Clear();s.Residents.FlushPlanInvalidations();Check(events.Count==0,name+" repeated");};
  s.Residents.FlushPlanInvalidations();Check(events.Count==0,"First observation emitted a change");
  long lane=Network.LaneId(1,false);s.Transport.RegisterAccesses(new[]{new AccessRegistration(91,lane,20),new AccessRegistration(90,lane,21)});s.Residents.FlushPlanInvalidations();Check(events.Count==2&&!events.Any(e=>e.All)&&events.Select(e=>e.Endpoint).SequenceEqual(new[]{90,91}),"Shared batch revision lost endpoint/order");events.Clear();
  long rev=s.Transport.AccessRevision;string before=Hash(s);bool rejected=false;try{s.Transport.RegisterAccesses(new[]{new AccessRegistration(92,lane,20),new AccessRegistration(90,lane,21)});}catch(ArgumentException){rejected=true;}s.Residents.FlushPlanInvalidations();Check(rejected&&rev==s.Transport.AccessRevision&&Hash(s)==before&&events.Count==0,"Failed registration published/mutated");
  long availability=s.Transport.EndpointAvailabilityRevision;s.Transport.SetEndpointAvailable(1,true);s.Residents.FlushPlanInvalidations();Check(events.Count==0&&s.Transport.EndpointAvailabilityRevision==availability,"No-op availability notified");
  s.Transport.SetEndpointAvailable(1,false);s.Transport.SetEndpointAvailable(1,true);take("availability-coalesced",false,1,ResidentPlanChange.Availability);
  s.Transport.RegisterAccess(93,lane,22);s.Transport.SetEndpointAvailable(2,false);s.Transport.SetEndpointAvailable(2,true);s.Residents.FlushPlanInvalidations();Check(events.Count==2&&events[0].Endpoint==2&&events[1].Endpoint==93,"Mixed local scopes not ordered");events.Clear();
  var stale=s.Transport.PlanUpgradeRoad(1,2,12,RoadDirection.Both);s.Transport.RefreshEndpoints();before=Hash(s);Check(!stale.TryCommit()&&Hash(s)==before,"Stale edit changed city");s.Residents.FlushPlanInvalidations();Check(events.Count==0,"Failed edit invalidated plans");
  Check(s.Transport.PlanUpgradeRoad(1,2,13,RoadDirection.Both).TryCommit(),"Upgrade failed");take("speed",true,-1,ResidentPlanChange.Topology);
  Check(s.Transport.PlanUpgradeRoad(1,2,13,RoadDirection.Forward).TryCommit(),"Direction failed");take("direction",true,-1,ResidentPlanChange.Topology);
  s.Transport.SplitRoadAt(1,10);take("split",true,-1,ResidentPlanChange.Topology);
  int a=s.Transport.AddJunction(new Point(100,0,100)),b=s.Transport.AddJunction(new Point(140,0,100));int road=s.Transport.AddRoad(a,b,new[]{new Point(100,0,100),new Point(140,0,100)});take("remote-new-road",true,-1,ResidentPlanChange.Topology);
  s.Transport.RemoveRoad(road);take("remove",true,-1,ResidentPlanChange.Topology);
  s.City.day++;s.Transport.RegisterAccess(94,Network.LaneId(2,false),20);take("day-and-access",true,-1,ResidentPlanChange.Day|ResidentPlanChange.Access);
  s.City.society.settings.secondsPerDay=121;take("calendar",true,-1,ResidentPlanChange.Calendar);
  // Lose a callback deliberately. The authoritative revision must force a safe
  // global fallback, without a resident scan that hides the missing notice.
  typeof(World).GetField("AccessRegistered",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(s.Transport,null);s.Transport.RegisterAccess(95,Network.LaneId(2,false),21);take("missing-access-callback",true,-1,ResidentPlanChange.Access|ResidentPlanChange.Untracked);
  s.Residents.PlansInvalidated-=observer;Check(!s.Residents.PlansObserved,"Observer not detached");s.Transport.SetEndpointAvailable(2,false);s.Transport.SetEndpointAvailable(2,true);s.Residents.PlansInvalidated+=observer;s.Residents.FlushPlanInvalidations();Check(events.Count==0,"Reattach retained old changes");
  typeof(World).GetField("EndpointAvailabilityChanged",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(s.Transport,null);s.Transport.SetEndpointAvailable(2,false);s.Transport.SetEndpointAvailable(2,true);take("missing-availability-callback",true,-1,ResidentPlanChange.Availability|ResidentPlanChange.Untracked);s.Residents.PlansInvalidated-=observer;
  // Negative/unreachable dependencies must include the absent destination, not
  // just the lanes of a successful old route. Test-only derived plan consumer.
  var u=CityCoreIndustrySession.Create();var employer=new IndustrialBuilding{id=4,x=-10,z=-7,entranceX=-10,entranceZ=-3};employer.factory.capital=100;employer.factory.cash=100;u.Entities.AddBuilding(employer);foreach(int person in new[]{2,4}){var p=u.Entities.Member(person).Person;u.City.ReleaseJob(p);Check(u.City.AssignJob(p,u.City.society.jobEntities.First(j=>j.buildingId==4&&j.occupiedCitizenId<0).id),"Missing endpoint fixture assignment failed");}Check(!u.Transport.HasResidentAccess(3,4),"Missing endpoint unexpectedly reachable");var dependencies=new Dictionary<int,int[]>{{1,new[]{3,1}},{2,new[]{3,4}},{3,new[]{3,1}},{4,new[]{3,4}}};var dirty=new HashSet<int>();u.Residents.PlansInvalidated+=e=>{foreach(var m in u.Entities.Members)if(e.All||dependencies[m.Person.id].Contains(e.Endpoint))dirty.Add(m.Person.id);};u.Transport.RegisterAccess(4,lane,20,AccessUse.Resident);u.Residents.FlushPlanInvalidations();Check(dirty.SetEquals(new[]{2,4})&&u.Transport.HasResidentAccess(3,4),"Unreachable plan did not wake locally");Check(u.Entities.Members.Where(m=>dirty.Contains(m.Person.id)).Select(m=>m.Person.id).SequenceEqual(new[]{2,4}),"Consumer changed member order");dirty.Clear();u.Transport.SetEndpointAvailable(4,false);u.Residents.FlushPlanInvalidations();Check(dirty.SetEquals(new[]{2,4})&&!u.Transport.HasResidentAccess(3,4),"Availability did not invalidate negative dependencies");
  // A callback after an earlier member changes an access. Pump must run before
  // the next member's request; observer only marks derived data.
  var v=CityCoreIndustrySession.Create();var order=new List<string>();v.Residents.PlansInvalidated+=e=>order.Add("notice");int requests=0;v.Residents.RequestCommute=(id,from,to,back)=>{order.Add("request");if(requests++==0)v.Transport.RegisterAccess(99,lane,20);return null;};for(int i=0;i<40&&requests<2;i++)v.Advance(.05);Check(requests>=2&&order.Take(3).SequenceEqual(new[]{"request","notice","request"}),"Change during request not flushed before later member");
  var moving=CityCoreIndustrySession.Create();int movementNotices=0;moving.Residents.PlansInvalidated+=e=>movementNotices++;int maxActive=0;for(int i=0;i<30;i++){moving.Advance(.05);maxActive=Math.Max(maxActive,moving.Transport.ActiveCount);}Check(maxActive>0,"Motion fixture never admitted a vehicle");Check(movementNotices==0,"Ordinary admission/motion invalidated static plans: "+movementNotices);
  var day=CityCoreIndustrySession.Create();int dayNotices=0;day.Residents.PlansInvalidated+=e=>{if((e.Reason&ResidentPlanChange.Day)!=0)dayNotices++;};for(int i=0;i<1402;i++)day.Advance(.05);Check(dayNotices==1,"Real calendar boundary missed/repeated: "+dayNotices);
  var rebind=CityCoreIndustrySession.Create();int rebindGlobals=0;rebind.Residents.PlansInvalidated+=e=>{Check(e.All&&(e.Reason&ResidentPlanChange.Topology)!=0,"Rebind must conservatively cover network revision");rebindGlobals++;};rebind.Transport.RemoveRoad(1);rebind.Residents.FlushPlanInvalidations();Check(!rebind.Transport.GetAccess(3).valid,"Deletion did not invalidate entrance");var network=rebind.Transport.Network;Check(rebind.Transport.RebindAccess(3,Network.LaneId(2,false),10),"Invalid entrance not rebound");rebind.Residents.FlushPlanInvalidations();Check(rebindGlobals==2&&object.ReferenceEquals(network,rebind.Transport.Network),"Rebind should notify while reusing network");
  var fresh=CityCoreIndustrySession.Create();var cold=CityCoreSave.Read(CityCoreSave.Write(fresh));int coldNotices=0;cold.Residents.PlansInvalidated+=e=>coldNotices++;cold.Residents.FlushPlanInvalidations();Check(coldNotices==0&&Hash(fresh)==Hash(cold),"Cold observation changed saved facts");
  // Independent oracle catches a deliberately incorrect local consumer BEFORE
  // full Validate or a global repair could conceal the lost dependency.
  bool badDirty=false;bool caught=false;try{Check(!u.Transport.HasResidentAccess(3,4)||badDirty,"NEGATIVE missing dependency");u.Transport.SetEndpointAvailable(4,true);Check(!u.Transport.HasResidentAccess(3,4)||badDirty,"NEGATIVE missing dependency");}catch(Exception e){if(e.Message!="NEGATIVE missing dependency")throw;caught=true;}Check(caught,"Missing-dependency negative control not detected");
  File.WriteAllLines(Path.Combine(output,"targeted.csv"),cases);Console.WriteLine("PASS plan invalidation targeted checks="+checks+"; negative dependency control detected");
 }
 static void Cost(string output){var s=CityCoreIndustrySession.Create();long notices=0;s.Residents.PlansInvalidated+=e=>notices++;for(int i=0;i<100;i++){s.Transport.SetEndpointAvailable(1,false);s.Transport.SetEndpointAvailable(1,true);s.Residents.FlushPlanInvalidations();}var rows=new List<string>{"case,iterations,total_ms,allocated_bytes,notifications"};foreach(int mode in new[]{0,1,2}){for(int warm=0;warm<20000;warm++){if(mode==1){s.Transport.SetEndpointAvailable(1,false);s.Transport.SetEndpointAvailable(1,true);}if(mode==2)s.City.day++;s.Residents.FlushPlanInvalidations();}var sw=new System.Diagnostics.Stopwatch();long count=notices,bytes=GC.GetAllocatedBytesForCurrentThread();sw.Start();for(int i=0;i<10000;i++){if(mode==1){s.Transport.SetEndpointAvailable(1,false);s.Transport.SetEndpointAvailable(1,true);}if(mode==2)s.City.day++;s.Residents.FlushPlanInvalidations();}sw.Stop();bytes=GC.GetAllocatedBytesForCurrentThread()-bytes;rows.Add(string.Join(",",mode,10000,sw.Elapsed.TotalMilliseconds.ToString("R",System.Globalization.CultureInfo.InvariantCulture),bytes,notices-count));}File.WriteAllLines(Path.Combine(output,"notification-cost.csv"),rows);}
#endif
 static void Main(string[] args){
#if PLAN_INVALIDATIONS
 Targeted(args[1]);Cost(args[1]);if(args.Length>2)return;
#endif
 var s=CityCoreSave.Read(File.ReadAllBytes(args[0]));s.Transport.DepotLaneRoutingEnabled=true;s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
#if PLAN_INVALIDATIONS
 long globals=0,locals=0;s.Residents.PlansInvalidated+=e=>{if(e.All)globals++;else locals++;};
#endif
 for(int i=0;i<2400;i++)s.Advance(.05);var calls=new List<string>();var dispatch=s.Residents.RequestCommute;s.Residents.RequestCommute=(id,from,to,back)=>{var v=dispatch(id,from,to,back);calls.Add(string.Join(",",s.Simulation.Tick,id,from,to,back,v==null?0:v.id));return v;};var rows=new List<string>{"tick,sha256"};for(int i=0;i<1200;i++){s.Advance(.05);if(i%100==99)rows.Add(s.Simulation.Tick+","+Hash(s));}var cold=CityCoreSave.Read(CityCoreSave.Write(s));cold.Transport.DepotLaneRoutingEnabled=true;cold.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
#if PLAN_INVALIDATIONS
 cold.Residents.PlansInvalidated+=e=>{};
#endif
 for(int i=0;i<80;i++){s.Advance(.05);cold.Advance(.05);}Check(Hash(s)==Hash(cold),"Cold replay differs");rows.Add("cold80,"+Hash(s));File.WriteAllLines(Path.Combine(args[1],"states.csv"),rows);File.WriteAllLines(Path.Combine(args[1],"requests.csv"),calls);
#if PLAN_INVALIDATIONS
 File.WriteAllLines(Path.Combine(args[1],"observations.csv"),new[]{"steps,global_notices,local_notices",string.Join(",",3680,globals,locals)});
#endif
 Console.WriteLine("PASS real30k requests/native18 state/cold80; checks="+checks+"; still full polling");}
}
