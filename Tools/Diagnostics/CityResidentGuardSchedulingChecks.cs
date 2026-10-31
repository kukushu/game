using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Security.Cryptography;using System.Reflection;using HarborCity;
static class CityResidentGuardSchedulingChecks {
 static string Hash(CityCoreIndustrySession s)=>Convert.ToHexString(SHA256.HashData(CityCoreSave.Write(s)));
 static void IndexOracle(){
  var a=CityCoreIndustrySession.Create();var b=CityCoreIndustrySession.Create();foreach(var session in new[]{a,b})for(int i=0;i<164;i++)session.Entities.AddResident(1,new CityResident{id=session.City.society.nextCitizenId,age=20,canWork=true,location=3});
  var index=new CityResidentGuardSchedule(a.Entities,a.Transport,a.City);var random=new Random(11031);var actual=new List<int>();var expected=new List<int>();
  for(int step=0;step<1000;step++){
   for(int k=0;k<20;k++){int who=random.Next(a.Entities.Members.Count),choice=random.Next(6);foreach(var session in new[]{a,b}){var p=session.Entities.Members[who].Person;p.tripId=choice==0?1:0;p.retryAt=choice==1?1:choice==2?float.NaN:0;p.visitUntil=choice==3?1:choice==4?double.NaN:0;}}
   actual.Clear();expected.Clear();foreach(var m in index.Begin()){actual.Add(m.Person.id);if(m.Person.id%17==0){var p=a.Entities.Members[(m.Person.id*31+step)%a.Entities.Members.Count].Person;p.tripId=0;p.retryAt=0;p.visitUntil=0;}}
   foreach(var m in b.Entities.Members){var p=m.Person;if(p.tripId!=0||b.Transport.Clock<p.retryAt||b.Transport.Clock<p.visitUntil)continue;expected.Add(p.id);if(p.id%17==0){p=b.Entities.Members[(p.id*31+step)%b.Entities.Members.Count].Person;p.tripId=0;p.retryAt=0;p.visitUntil=0;}}
   if(!actual.SequenceEqual(expected))throw new Exception("Independent multiword guard index oracle mismatch step="+step);
  }
  Console.WriteLine("PASS 1000 independent 168-slot multiword index passes including synchronous earlier/later wakes; not a city capacity benchmark");
 }
 static void Main(string[] args){IndexOracle();
  var rows=new List<string>{"case,requests,guard_bits,sha256"};
  foreach(int mode in Enumerable.Range(0,18)){
   var s=CityCoreIndustrySession.Create();var people=s.Entities.Members.Select(m=>m.Person).ToArray();foreach(var p in people)p.retryAt=100000;
   var calls=new List<int>();s.Residents.RequestCommute=(id,from,to,back)=>{calls.Add(id);if(mode==7&&id==people[0].id)people[1].retryAt=0;if(mode==8&&id==people[1].id)people[0].retryAt=0;return null;};
   s.Residents.AdvanceRequests();var first=people[0];
   if(mode==0)first.retryAt=0;
   if(mode==1){first.retryAt=float.NaN;first.visitUntil=.1;}
   if(mode==2){first.retryAt=.1f;first.visitUntil=double.NaN;}
   if(mode==3){first.retryAt=float.PositiveInfinity;first.visitUntil=double.NaN;}
   if(mode==4){first.retryAt=float.NegativeInfinity;first.visitUntil=0;}
   if(mode==5){first.retryAt=0;first.visitUntil=double.PositiveInfinity;}
   if(mode==6){first.tripId=123;first.retryAt=0;s.Residents.AdvanceRequests();first.tripId=0;}
   if(mode==7)first.retryAt=0;
   if(mode==8)people[1].retryAt=0;
   if(mode==9){var removed=people[3];int job=removed.jobId;s.Entities.RemoveResident(removed.id);var added=new CityResident{id=s.City.society.nextCitizenId,age=30,skill=2,canWork=true,location=3,retryAt=0};s.Entities.AddResident(1,added);if(!s.City.AssignJob(added,job))throw new Exception("Replacement job failed");}
   if(mode>=10){first.retryAt=0;if(mode==10)first.sick=true;if(mode==11)first.canWork=false;if(mode==12)first.dead=true;if(mode==13||mode==16||mode==17)first.departureDay=s.City.day;if(mode==14)s.Entities.Members[0].Family.resident=false;if(mode==15)first.atWork=true;s.Residents.AdvanceRequests();if(mode==10)first.sick=false;if(mode==11)first.canWork=true;if(mode==12)first.dead=false;if(mode==13)first.departureDay=0;if(mode==14)s.Entities.Members[0].Family.resident=true;if(mode==15)first.atWork=false;if(mode==16){s.City.day++;s.Residents.AdvanceRequests();}if(mode==17){int day=s.City.day;s.City.day++;first.departureDay=s.City.day;s.City.day=day;}}
   for(int i=0;i<12;i++)s.Advance(.05);
   string bits=BitConverter.SingleToInt32Bits(first.retryAt)+";"+BitConverter.DoubleToInt64Bits(first.visitUntil);first.retryAt=0;first.visitUntil=0;rows.Add(mode+","+string.Join(";",calls)+","+bits+","+Hash(s));
  }
  File.WriteAllLines(Path.Combine(args[0],"controls.csv"),rows);
  var bad=CityCoreIndustrySession.Create();foreach(var m in bad.Entities.Members)m.Person.retryAt=100000;bad.Residents.AdvanceRequests();var citizen=bad.Entities.Members[0].Person;
  typeof(CityResident).GetField("retry",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(citizen,0f);
  var oracle=CityCoreSave.Read(CityCoreSave.Write(bad));int warm=0,cold=0;bad.Residents.RequestCommute=(a,b,c,d)=>{warm++;return null;};oracle.Residents.RequestCommute=(a,b,c,d)=>{cold++;return null;};bad.Residents.AdvanceRequests();oracle.Residents.AdvanceRequests();
#if CITY_RESIDENT_GUARD_SCHEDULE
  if(warm!=0||cold!=1)throw new Exception("Independent cold oracle did not expose bypassed guard notification");
#else
  if(warm!=1||cold!=1)throw new Exception("Frozen polling oracle fixture invalid");
#endif
  Console.WriteLine("PASS 18 paired guard cases; deliberately bypassed notification checked against independent cold oracle");
 }
}
