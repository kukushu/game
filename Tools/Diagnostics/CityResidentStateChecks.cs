using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Security.Cryptography;using HarborCity;
static class CityResidentStateChecks {
 static int checks;static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
 static string Hash(CityCoreIndustrySession s)=>Convert.ToHexString(SHA256.HashData(CityCoreSave.Write(s)));
#if RESIDENT_STATE
 static void Targeted(){var s=CityCoreIndustrySession.Create();var people=s.Entities.Members.Select(m=>m.Person).ToArray();var p=people[0];var h=s.Entities.Members[0].Family;var seen=new List<string>();s.Entities.ResidentStateChanged+=(m,r)=>{Check(s.Entities.Member(m.Person.id)==m,"Stale state member");seen.Add(m.Person.id+":"+r);};
  Action<string,int[]> take=(reason,ids)=>{Check(seen.SequenceEqual(ids.Select(id=>id+":"+reason)),"State notification membership/order mismatch: "+reason);seen.Clear();};var all=people.Select(x=>x.id).ToArray();var one=new[]{p.id};
  p.canWork=false;take("Ability",one);p.canWork=false;take("Ability",Array.Empty<int>());p.canWork=true;take("Ability",one);
  p.sick=true;take("Sickness",one);p.sick=false;take("Sickness",one);p.dead=true;take("Death",one);p.dead=false;take("Death",one);
  p.location=2;take("Location",one);p.location=3;take("Location",one);p.atWork=true;take("Attendance",one);p.atWork=false;take("Attendance",one);p.health=99;take("Sickness",Array.Empty<int>());p.health=100;
  h.resident=false;take("Residence",all);h.home=2;take("Home",all);h.home=3;take("Home",all);h.resident=true;take("Residence",all);
  var other=new Household{id=s.City.society.nextId};s.Entities.AddFamily(other);s.Entities.MoveResident(p.id,other.id);take("Membership",one);Check(p.location==3,"Move teleported resident");other.home=3;take("Home",one);other.resident=true;take("Residence",one);s.Entities.MoveResident(p.id,h.id);take("Membership",one);
  // Family.people is now2,3,4,1 while authoritative Members remains1,2,3,4.
  h.home=2;take("Home",all);h.home=3;take("Home",all);other.resident=false;other.home=-1;take("Home",Array.Empty<int>());
  s.Entities.SetAvailability(3,true,false);take("HousingAvailability",all);s.Entities.SetAvailability(3,false,false);take("HousingAvailability",all);s.Entities.SetAvailability(3,false,false);take("HousingAvailability",Array.Empty<int>());
  s.Entities.ReplaceBuilding(BuildingSaveData.FromBuilding(s.City.GetBuilding(3)).ToBuilding());take("HousingAvailability",all);s.Entities.ValidateHomeIndex();
  string before=Hash(s);bool rejected=false;try{s.CloseBuilding(3);}catch(InvalidOperationException){rejected=true;}Check(rejected&&Hash(s)==before,"Occupied housing close changed city");take("Home",Array.Empty<int>());
  var added=new CityResident{id=s.City.society.nextCitizenId,age=20,canWork=true,location=3};s.Entities.AddResident(h.id,added);added.sick=true;take("Sickness",new[]{added.id});s.Entities.RemoveResident(added.id);added.sick=false;added.canWork=false;take("Sickness",Array.Empty<int>());Check(added.EntityOwner==null,"Removed state owner not detached");
  var cold=CityCoreSave.Read(CityCoreSave.Write(CityCoreIndustrySession.Create()));var coldSeen=new List<int>();cold.Entities.ResidentStateChanged+=(m,r)=>coldSeen.Add(m.Person.id);cold.Entities.Members[0].Person.sick=true;Check(coldSeen.SequenceEqual(new[]{1})&&seen.Count==0,"Cold state ownership leaked");
  var hand=CityCoreIndustrySession.Create();var next=new CityCoreIndustrySession(hand.City,hand.Simulation.CalendarOffset,hand.Transport.Snapshot(),hand.Simulation.ClockSnapshot);Check(next.Entities.Members[0].Family.EntityOwner==next.Entities,"Family handover failed");next.Validate();
  // Independent remembered fact detects deliberate bypass, before Validate can check any index.
  var bad=CityCoreIndustrySession.Create();bool notified=false;var first=bad.Entities.Members[0].Person;bool initial=first.sick;bad.Entities.ResidentStateChanged+=(m,r)=>{if(m.Person==first&&r==CityResidentStateChange.Sickness)notified=true;};typeof(CityResident).GetField("ill",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(first,true);bool caught=false;try{Check(first.sick==initial||notified,"Independent state oracle missed invalidation");}catch(Exception e){if(e.Message!="Independent state oracle missed invalidation")throw;caught=true;}Check(caught,"Negative control was not detected");Console.WriteLine("PASS negative control exposes missed state invalidation when backing field bypasses owner");
  Console.WriteLine("PASS state notifications/direct writes/family/move/availability/order/detach/cold; checks="+checks);
 }
#endif
 static void Replay(string output){var s=CityCoreIndustrySession.Create();var rows=new List<string>{"case,sha256"};var calls=new List<string>();var request=s.Residents.RequestCommute;var people=s.Entities.Members.Select(m=>m.Person).ToArray();people[1].sick=true;
#if RESIDENT_STATE
  s.Entities.ResidentStateChanged+=(m,r)=>Check(s.Entities.Member(m.Person.id)==m,"Stale replay member");
#endif
  s.Residents.RequestCommute=(id,from,to,back)=>{if(id==people[0].id)people[1].sick=false;var v=request(id,from,to,back);calls.Add(string.Join(",",s.Simulation.Tick,id,from,to,back,v==null?0:v.id));return v;};
  for(int i=0;i<1600;i++){if(i==50)people[2].canWork=false;if(i==100)people[2].canWork=true;s.Advance(.05);if(i%100==99)rows.Add("tick-"+s.Simulation.Tick+","+Hash(s));}
  Check(calls.Any(c=>c.Split(',')[1]==people[1].id.ToString()),"Later-member callback did not request");File.WriteAllBytes(Path.Combine(output,"final.hcity"),CityCoreSave.Write(s));var cold=CityCoreSave.Read(CityCoreSave.Write(s));for(int i=0;i<80;i++){s.Advance(.05);cold.Advance(.05);}Check(Hash(s)==Hash(cold),"Ordinary cold continuation differs");rows.Add("cold80,"+Hash(s));
  File.WriteAllLines(Path.Combine(output,"states.csv"),rows);File.WriteAllLines(Path.Combine(output,"requests.csv"),calls);
  // Expose an existing baseline ordering defect rather than calling it a PASS.
  var moved=CityCoreIndustrySession.Create();var family=new Household{id=moved.City.society.nextId};moved.Entities.AddFamily(family);moved.Entities.MoveResident(1,family.id);moved.Entities.MoveResident(1,1);var restored=CityCoreSave.Read(CityCoreSave.Write(moved));string warmOrder=string.Join(";",moved.Entities.Members.Select(m=>m.Person.id)),coldOrder=string.Join(";",restored.Entities.Members.Select(m=>m.Person.id));for(int i=0;i<20;i++){moved.Advance(.05);restored.Advance(.05);}bool equal=Hash(moved)==Hash(restored);File.WriteAllLines(Path.Combine(output,"existing-order-defect.csv"),new[]{"warm_order,cold_order,continued_state_equal",warmOrder+","+coldOrder+","+equal});Console.WriteLine("KNOWN BASELINE CHECK moved-family warm/cold order="+warmOrder+" / "+coldOrder+"; stateEqual="+equal);
  Console.WriteLine("PASS frozen-comparable state/request replay and ordinary cold80; reordered-family cold result recorded separately");
 }
 static void Main(string[] args){
#if RESIDENT_STATE
 Targeted();
#endif
 Replay(args[0]);}
}
