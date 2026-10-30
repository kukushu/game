using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Security.Cryptography;using HarborCity;
static class CityFundingChecks {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static string Hash(CityCoreIndustrySession s)=>Convert.ToHexString(SHA256.HashData(CityCoreSave.Write(s)));
 static void FactoryCash(FactoryState f,double next){f.capital+=next-f.cash;f.cash=next;}
 static void ShopCash(CityShopAccount a,double next){a.Capital+=next-a.Cash;a.Cash=next;}
#if FUNDING
 sealed class Watch:IDisposable {
  readonly CityCoreIndustrySession s;readonly Dictionary<int,bool> before=new Dictionary<int,bool>();internal readonly List<int> Seen=new List<int>();readonly Action<CityMember> receive;
  internal Watch(CityCoreIndustrySession s){this.s=s;Seed();receive=m=>Seen.Add(m.Person.id);s.Entities.FundingEligibilityChanged+=receive;}
  void Seed(){before.Clear();foreach(var b in s.Entities.Buildings)if(b is IndustrialBuilding||b is CommercialBuilding)before[b.id]=s.City.EmployerFunded(b.id);}
  internal void Flush(){var expected=s.Entities.Members.Where(m=>{var job=s.City.ResidentJob(m.Person);return job!=null&&before.TryGetValue(job.buildingId,out bool old)&&old!=s.City.JobFunded(job);}).Select(m=>m.Person.id).ToArray();s.Entities.FlushFundingChanges();Check(Seen.SequenceEqual(expected),"Funding oracle mismatch");Seen.Clear();Seed();}
  public void Dispose(){s.Entities.FundingEligibilityChanged-=receive;}
 }
 static void Targeted(){var s=CityCoreIndustrySession.Create();foreach(var h in s.City.society.families)h.openingSavings=h.savings;s.City.development.enabled=true;var f=s.City.Factory(1);var shop=s.Entities.Shops[0];ShopCash(shop.Account,200);var p=s.Entities.Members[1].Person;s.City.ReleaseJob(p);Check(s.City.AssignJob(p,s.City.society.jobEntities.First(j=>j.buildingId==2&&j.occupiedCitizenId<0).id),"Shop staffing failed");
  using(var w=new Watch(s)){
   FactoryCash(f,100);w.Flush();FactoryCash(f,0);w.Flush();FactoryCash(f,double.Epsilon);w.Flush();FactoryCash(f,0);FactoryCash(f,10);FactoryCash(f,20);w.Flush();
   FactoryCash(f,0);ShopCash(shop.Account,0);w.Flush();FactoryCash(f,10);ShopCash(shop.Account,10);w.Flush();
   s.City.development.enabled=false;ShopCash(shop.Account,0);w.Flush();FactoryCash(f,0);w.Flush();s.City.development.enabled=true;w.Flush();
   FactoryCash(f,10);ShopCash(shop.Account,10);w.Flush();
   FactoryCash(f,0);var moving=s.Entities.Members[2].Person;s.City.ReleaseJob(moving);Check(s.City.AssignJob(moving,s.City.society.jobEntities.First(j=>j.buildingId==2&&j.occupiedCitizenId<0).id),"Pending-change reassignment failed");w.Flush();FactoryCash(f,10);w.Flush();
   // Failed deterministic funding/admission cannot debit or emit a change.
   Check(!s.Business.CanFundLocalFreight(shop,1,999),"Unfunded freight accepted");string saved=Hash(s);Check(s.Simulation.RequestFreight(1,2,1,999)==null,"Failed freight committed");w.Flush();Check(saved==Hash(s),"Failed freight changed full state");
   // Preserve a queued eligibility change across the account-preserving replacement.
   ShopCash(shop.Account,0);var copy=(CommercialBuilding)BuildingSaveData.FromBuilding(shop).ToBuilding();s.Entities.ReplaceBuilding(copy);Check(ReferenceEquals(copy.Account,shop.Account),"Shop account not reused");w.Flush();shop=copy;
   ShopCash(shop.Account,10);w.Flush();s.Entities.RemoveResident(p.id);ShopCash(shop.Account,0);w.Flush();
   s.CloseBuilding(1);FactoryCash(f,20);w.Flush();Check(f.FundingOwner==null,"Retired factory still bound");
   var cold=CityCoreSave.Read(CityCoreSave.Write(s));using(var cw=new Watch(cold)){ShopCash(cold.Entities.Shops[0].Account,10);cw.Flush();}Check(w.Seen.Count==0,"Cold observer leaked to original");
  }
  ShopCash(shop.Account,10);var pending=(HashSet<int>)typeof(CityEntities).GetField("dirtyFunding",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(s.Entities);Check(pending.Count==0,"No-observer writes accumulated pending changes");
  var hand=CityCoreIndustrySession.Create();var next=new CityCoreIndustrySession(hand.City,hand.Simulation.CalendarOffset,hand.Transport.Snapshot(),hand.Simulation.ClockSnapshot);Check(next.City.Factory(1).FundingOwner==next.Entities,"Same-city session handover lost owner");next.Validate();
  foreach(long bits in new[]{0L,unchecked((long)0x8000000000000000),0x7ff8000000001234L,unchecked((long)0xfff8000000004321),0x7ff0000000000000L}){double value=BitConverter.Int64BitsToDouble(bits);var detached=new FactoryState();detached.cash=value;var account=new CityShopAccount();account.Cash=value;Check(BitConverter.DoubleToInt64Bits(detached.cash)==bits&&BitConverter.DoubleToInt64Bits(account.Cash)==bits,"Cash bits changed");}
  // Negative control: bypass the notification entry, and require the independent oracle to catch it.
  var bad=CityCoreIndustrySession.Create();using(var w=new Watch(bad)){typeof(FactoryState).GetField("balance",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(bad.City.Factory(1),0d);bool caught=false;try{w.Flush();}catch(Exception e){if(e.Message!="Funding oracle mismatch")throw;caught=true;}Check(caught,"Bypassed cash notification was silently repaired");}
  var empty=CityCoreIndustrySession.CreateEmpty();int addedCalls=0;empty.Entities.FundingEligibilityChanged+=m=>addedCalls++;var newFactory=new IndustrialBuilding{id=empty.City.nextBuildingId};FactoryCash(newFactory.factory,10);empty.Entities.AddBuilding(newFactory);FactoryCash(newFactory.factory,0);empty.Entities.FlushFundingChanges();Check(newFactory.factory.FundingOwner==empty.Entities&&addedCalls==0,"Added factory ownership/no-staff notification failed");
  CallbackCase();Console.WriteLine("PASS funding targeted lifecycle/mode/rollback/bits/oracle-negative; checks="+checks);
 }
 static void Maintenance(string output,string save){var s=CityCoreSave.Read(File.ReadAllBytes(save));s.Transport.DepotLaneRoutingEnabled=true;s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);for(int i=0;i<2400;i++)s.Advance(.05);
  var member=s.Entities.Members.First(m=>s.City.GetBuilding(s.City.Workplace(m.Person)) is IndustrialBuilding);int employer=s.City.Workplace(member.Person);var f=s.City.Factory(employer);int staff=s.Entities.Members.Count(m=>s.City.Workplace(m.Person)==employer);FactoryCash(f,100);
  long notifications=0;long before=GC.GetTotalMemory(true);s.Entities.FundingEligibilityChanged+=m=>notifications++;long retained=GC.GetTotalMemory(true)-before;
  var rows=new List<string>{"scenario,operations,total_ms,allocated_bytes,notifications,employees,observer_retained_bytes"};
  Action<string,int,Action<int>,long> run=(name,count,action,expected)=>{for(int i=0;i<10;i++)action(i);notifications=0;var watch=new System.Diagnostics.Stopwatch();long allocated=GC.GetAllocatedBytesForCurrentThread();watch.Start();for(int i=0;i<count;i++)action(i);watch.Stop();allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;Check(notifications==expected,"Maintenance notification count wrong: "+name);rows.Add(string.Join(",",name,count,watch.Elapsed.TotalMilliseconds.ToString("R",System.Globalization.CultureInfo.InvariantCulture),allocated,notifications,staff,retained));};
  run("positive_balance",10000,i=>{FactoryCash(f,100+(i&1));s.Entities.FlushFundingChanges();},0);
  run("cross_zero_and_recover",2000,i=>{FactoryCash(f,0);s.Entities.FlushFundingChanges();FactoryCash(f,100);s.Entities.FlushFundingChanges();},4000L*staff);
  run("unchanged_RebuildJobs",10,i=>s.Entities.RebuildJobs(),0);
  // Writes without a listener must not retain changes. Keep the same session alive.
  File.WriteAllLines(Path.Combine(output,"funding-cost.csv"),rows);Console.WriteLine("PASS real30k warm funding maintenance cost/counts; controlled balance writes, model only");
 }
 static void CallbackCase(){var s=CityCoreIndustrySession.Create();var people=s.Entities.Members.Select(m=>m.Person).ToArray();foreach(var p in people)p.retryAt=0;var account=s.City.Factory(1);FactoryCash(account,0);var notified=new List<int>();s.Entities.FundingEligibilityChanged+=m=>notified.Add(m.Person.id);
  // With legacy mode all workers use this factory, so use a returning first member
  // to recover employer funds before later at-home members are considered.
  people[0].location=2;var original=s.Residents.RequestCommute;var requests=new List<int>();
  s.Residents.RequestCommute=(id,from,to,back)=>{requests.Add(id);if(id==people[0].id)FactoryCash(account,100);return original(id,from,to,back);};s.Advance(.05);s.Residents.RequestCommute=original;
  Check(notified.SequenceEqual(people.Select(p=>p.id)),"Callback funding did not notify in Members order");Check(requests.Contains(people[1].id),"Later member did not observe recovered funding");Check(requests.IndexOf(people[0].id)<requests.IndexOf(people[1].id),"Request order changed");
 }
#endif
 static void Replay(string output){var s=CityCoreIndustrySession.Create();foreach(var h in s.City.society.families)h.openingSavings=h.savings;var rows=new List<string>{"case,sha256"};var requests=new List<string>();var dispatch=s.Residents.RequestCommute;s.Residents.RequestCommute=(id,from,to,back)=>{var v=dispatch(id,from,to,back);requests.Add(string.Join(",",s.Simulation.Tick,id,from,to,back,v==null?0:v.id));return v;};
#if FUNDING
  s.Entities.FundingEligibilityChanged+=m=>{Check(s.Entities.Member(m.Person.id)==m,"Notification has stale member");};
#endif
  for(int i=0;i<1600;i++){if(i==30)FactoryCash(s.City.Factory(1),0);if(i==80)FactoryCash(s.City.Factory(1),300);if(i==120)s.City.development.enabled=true;if(i==180)s.City.development.enabled=false;s.Advance(.05);if(i%100==99||i==150)rows.Add("tick-"+s.Simulation.Tick+","+Hash(s));}
  var bytes=CityCoreSave.Write(s);File.WriteAllBytes(Path.Combine(output,"final.hcity"),bytes);var cold=CityCoreSave.Read(bytes);for(int i=0;i<80;i++){s.Advance(.05);cold.Advance(.05);}Check(Hash(cold)==Hash(s),"Cold continuation differs");rows.Add("cold80,"+Hash(s));File.WriteAllLines(Path.Combine(output,"states.csv"),rows);File.WriteAllLines(Path.Combine(output,"requests.csv"),requests);Console.WriteLine("PASS funding replay states, request sequence, wire and cold80");}
 static void Main(string[] args){
#if FUNDING
 if(args.Length>1){Maintenance(args[0],args[1]);return;}
 Targeted();
#endif
 Replay(args[0]);}
}
