using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Security.Cryptography;using HarborCity;
static class CityAttendanceRosterChecks
{
 static int assertions;static void Check(bool condition,string message){assertions++;if(!condition)throw new Exception(message);}
 static void Roster(CityCoreIndustrySession s)
 {
#if ATTENDANCE_INDEX
  var actual=new List<CityMember>();using(var cursor=s.Entities.WorkingMembers())while(cursor.MoveNext())actual.Add(cursor.Current);
  Check(actual.SequenceEqual(s.Entities.Members.Where(m=>m.Person.atWork)),"Attendance order/membership differs from Members oracle");
#endif
 }
 static void Mark(List<string> rows,string name,CityCoreIndustrySession s){Roster(s);rows.Add(name+","+Convert.ToHexString(SHA256.HashData(CityCoreSave.Write(s))));}
 static void Main(string[] args)
 {
  var callbackCity=CityCoreIndustrySession.Create(startAtWork:true);var staff=callbackCity.Entities.Members.Select(m=>m.Person).ToArray();staff[1].atWork=false;bool called=false;
  callbackCity.City.logSink=e=>{if(e.type=="factory.input"){called=true;staff[1].atWork=true;staff[2].atWork=false;}};callbackCity.Advance(.05);Check(called,"Production callback fixture did not run");callbackCity.City.logSink=null;
  var rows=new List<string>{"case,sha256"};Mark(rows,"synchronous-callback",callbackCity);var s=CityCoreIndustrySession.Create(startAtWork:true);Mark(rows,"initial",s);
  var people=s.Entities.Members.Select(m=>m.Person).ToArray();
  for(int n=0;n<200;n++){foreach(var p in people.Reverse()){p.atWork=n%3!=0;Roster(s);bool previous=p.atWork;p.atWork=previous;Roster(s);}s.Advance(.05);Mark(rows,"direct-write-"+n,s);}
  foreach(var p in people)p.atWork=false;Roster(s);var h=s.Entities.Members[0].Family;
  int id=s.City.society.nextCitizenId;var newcomer=new CityResident{id=id,age=30,canWork=true,householdId=h.id};s.Entities.AddResident(h.id,newcomer);Roster(s);newcomer.atWork=true;Roster(s);newcomer.atWork=false;
  // Moving preserves the current Members ordinal, removal must detach notifications.
  var other=new Household{id=s.City.society.nextId};s.Entities.AddFamily(other);s.Entities.MoveResident(id,other.id);Roster(s);s.Entities.RemoveResident(id);Roster(s);newcomer.atWork=true;Roster(s);
  var added=new List<CityResident>();for(int n=0;n<1600;n++){var person=new CityResident{id=s.City.society.nextCitizenId,age=30,canWork=true};s.Entities.AddResident(h.id,person);added.Add(person);person.atWork=true;Roster(s);person.atWork=false;Roster(s);}
  // Alternating removals leave holes in every word and force ordinal compaction.
  foreach(var person in added.Where((p,i)=>i%2==0).Concat(added.Where((p,i)=>i%2!=0))){s.Entities.RemoveResident(person.id);Roster(s);person.atWork=true;Roster(s);}
  Mark(rows,"removed-and-detached",s);var cold=CityCoreSave.Read(CityCoreSave.Write(s));Roster(cold);Mark(rows,"cold",cold);
  for(int i=0;i<1800;i++){if(i==20)people[0].sick=true;if(i==80)people[0].sick=false;if(i==200)people[1].canWork=false;if(i==300)people[1].canWork=true;s.Advance(.05);Roster(s);if(i%100==99)Mark(rows,"lifecycle-"+i,s);}
  File.WriteAllLines(Path.Combine(args[0],"attendance.csv"),rows);File.WriteAllBytes(Path.Combine(args[0],"attendance.hcity"),CityCoreSave.Write(s));Console.WriteLine("PASS attendance direct writes, same-value setters, Members order, structure, detach, cold, and lifecycle; assertions="+assertions);
 }
}
