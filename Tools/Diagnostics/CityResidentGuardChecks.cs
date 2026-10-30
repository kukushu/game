using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Security.Cryptography;using HarborCity;
static class CityResidentGuardChecks
{
 static int assertions;static void Check(bool ok,string message){assertions++;if(!ok)throw new Exception(message);}
 static string Hash(CityCoreIndustrySession s)=>Convert.ToHexString(SHA256.HashData(CityCoreSave.Write(s)));
 static void Main(string[] args)
 {
  var s=CityCoreIndustrySession.Create();var person=s.Entities.Members[0].Person;var rows=new List<string>{"case,sha256"};
#if REQUEST_GUARDS
  int writes=0,trips=0,retries=0,visits=0;CityMember last=null;
  s.Entities.RequestGuardsChanged+=(member,kind)=>{writes++;last=member;Check(member.Person.EntityOwner==s.Entities,"Wrong owner");if(kind==CityResidentGuard.Trip)trips++;if(kind==CityResidentGuard.Retry)retries++;if(kind==CityResidentGuard.Visit)visits++;};
#endif
  person.retryAt=10;person.retryAt=0;person.retryAt=0;person.visitUntil=20;person.visitUntil=0;person.tripId=0;
#if REQUEST_GUARDS
  Check(writes==6&&retries==3&&visits==2&&trips==1&&last==s.Entities.Member(person.id),"Missed direct/same-value guard write");
#endif
  foreach(int bits in new[]{0,unchecked((int)0x80000000),0x7fc01234,unchecked((int)0xffc04321),0x7f800000,unchecked((int)0xff800000)}){person.retryAt=BitConverter.Int32BitsToSingle(bits);Check(BitConverter.SingleToInt32Bits(person.retryAt)==bits,"Float write bits changed");}
  foreach(long bits in new[]{0L,unchecked((long)0x8000000000000000),0x7ff8000000001234L,unchecked((long)0xfff8000000004321),0x7ff0000000000000L,unchecked((long)0xfff0000000000000)}){person.visitUntil=BitConverter.Int64BitsToDouble(bits);Check(BitConverter.DoubleToInt64Bits(person.visitUntil)==bits,"Double write bits changed");}
  person.retryAt=0;person.visitUntil=0;rows.Add("direct,"+Hash(s));
  var family=s.Entities.Members[0].Family;var extra=new CityResident{id=s.City.society.nextCitizenId,age=20,canWork=true};s.Entities.AddResident(family.id,extra);extra.retryAt=15;extra.visitUntil=12;extra.retryAt=0;extra.visitUntil=0;
  var other=new Household{id=s.City.society.nextId};s.Entities.AddFamily(other);s.Entities.MoveResident(extra.id,other.id);extra.retryAt=1;extra.retryAt=0;s.Entities.RemoveResident(extra.id);
#if REQUEST_GUARDS
  int detached=writes;
#endif
  extra.retryAt=1;extra.visitUntil=2;extra.tripId=7;
#if REQUEST_GUARDS
  Check(writes==detached&&extra.EntityOwner==null,"Removed member still notifies");
#endif
  rows.Add("detached,"+Hash(s));
  var cold=CityCoreSave.Read(CityCoreSave.Write(s));Check(Hash(cold)==Hash(s),"Cold initial state differs");
#if REQUEST_GUARDS
  int oldWrites=writes,coldWrites=0;cold.Entities.RequestGuardsChanged+=(member,kind)=>{coldWrites++;Check(member.Person.EntityOwner==cold.Entities,"Cold owner wrong");};
  var cp=cold.Entities.Members[0].Person;cp.retryAt=10;cp.retryAt=0;Check(coldWrites==2&&writes==oldWrites,"Cold notification ownership leaked");
#endif
  // Same inputs on both warm and restored sessions, with real dispatch/arrival/visit writes.
  for(int i=0;i<2000;i++){if(i==40){person.sick=true;cold.Entities.Member(person.id).Person.sick=true;}if(i==80){person.sick=false;cold.Entities.Member(person.id).Person.sick=false;}s.Advance(.05);cold.Advance(.05);if(i%100==99){Check(Hash(cold)==Hash(s),"Cold continuation differs");rows.Add("step-"+i+","+Hash(s));}}
  s.Validate();cold.Validate();
#if REQUEST_GUARDS
  Check(trips>1&&retries>20,"Actual dispatch path did not notify");Console.WriteLine("guard notifications="+writes+",trip="+trips+",retry="+retries+",visit="+visits);
#endif
  File.WriteAllLines(Path.Combine(args[0],"guards.csv"),rows);File.WriteAllBytes(Path.Combine(args[0],"guards.hcity"),CityCoreSave.Write(s));Console.WriteLine("PASS request guard bits, direct writes, lifecycle, cold replay; assertions="+assertions);
 }
}
