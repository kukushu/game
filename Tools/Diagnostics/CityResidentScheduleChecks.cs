using System;using System.Linq;using System.Collections.Generic;using HarborCity;
static class CityResidentScheduleChecks {
 static int checks;static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
 static void Main(){
  var s=CityCoreIndustrySession.Create();foreach(var m in s.Entities.Members)m.Person.tripId=123;
  for(int i=0;i<100000;i++)s.Entities.AddResident(1,new CityResident{id=s.City.society.nextCitizenId,age=30,canWork=true,location=3,tripId=0});
  foreach(var m in s.Entities.Members)m.Person.tripId=123;
  var index=new CityResidentSchedule(s.Entities,s.Transport,s.City);Check(!index.Begin().GetEnumerator().MoveNext(),"Sparse fixture not asleep");
  var first=s.Entities.Members[0].Person;var last=s.Entities.Members[s.Entities.Members.Count-1].Person;first.tripId=0;last.tripId=0;
  long before=index.PredecessorSteps;var actual=index.Begin().SelectIds();Check(actual.SequenceEqual(new[]{first.id,last.id}),"Sparse predecessor lost order");Check(index.PredecessorSteps-before<64,"Sparse insertion scans city words");
  long builds=index.Rebuilds;for(int i=0;i<150;i++)s.Entities.AddResident(1,new CityResident{id=s.City.society.nextCitizenId,age=30,canWork=true,location=3});
  var added=index.Begin().SelectIds();Check(added.Count==152&&index.Rebuilds==builds&&index.MemberAdds==150,"Append rebuilt global schedule");
  for(int i=0;i<20;i++)s.Entities.RemoveResident(s.Entities.Members.Last().Person.id);var retained=index.Begin().SelectIds();Check(retained.Count==132&&index.Rebuilds==builds&&index.MemberRemoves==20,"Removal rebuilt global schedule");
  // Geometric queue growth must retain existing pending deadlines and stable keys.
  var q=new HarborCity.Diagnostics.CityWakeQueue(2);q.Schedule(1,7);q.EnsureCapacity(1000);q.Schedule(999,5);var due=new List<int>();q.DrainDue(6,due);Check(due.SequenceEqual(new[]{999}),"Growing timer lost heap order");q.DrainDue(7,due);Check(due.SequenceEqual(new[]{1}),"Growing timer lost old deadline");
  // Churn compacts only after half the historical slots are removed; no ID reuse.
  var tiny=CityCoreIndustrySession.Create();var sparse=new CityResidentSchedule(tiny.Entities,tiny.Transport,tiny.City);sparse.Begin();for(int i=0;i<160;i++)tiny.Entities.AddResident(1,new CityResident{id=tiny.City.society.nextCitizenId,age=30,canWork=true,location=3});sparse.Begin();for(int i=0;i<100;i++)tiny.Entities.RemoveResident(tiny.Entities.Members.Last().Person.id);Check(sparse.Begin().SelectIds().SequenceEqual(tiny.Entities.Members.Select(m=>m.Person.id)),"Compaction changed member execution order");Check(sparse.Rebuilds==2,"Churn compaction not bounded");
  var a=CityCoreIndustrySession.Create();var b=CityCoreIndustrySession.Create();var ta=new List<int>();var tb=new List<int>();a.Residents.RequestCommute=(id,x,y,z)=>{ta.Add(id);return null;};b.Residents.RequestCommute=(id,x,y,z)=>{tb.Add(id);return null;};b.Residents.SetReferencePolling(true);
  for(int i=0;i<1000;i++){if(i==100)a.Residents.SetReferencePolling(true);if(i==300)a.Residents.SetReferencePolling(false);a.Advance(.05);b.Advance(.05);}Check(ta.SequenceEqual(tb)&&CityCoreSave.Write(a).SequenceEqual(CityCoreSave.Write(b)),"Runtime fallback/resume changed state or requests");
  var unemployed=CityCoreIndustrySession.Create();foreach(var m in unemployed.Entities.Members)m.Person.retryAt=100000;var newcomer=new CityResident{id=unemployed.City.society.nextCitizenId,age=30,canWork=true,skill=2,location=3};unemployed.Entities.AddResident(1,newcomer);int count=0;unemployed.Residents.RequestCommute=(id,x,y,z)=>{if(id==newcomer.id)count++;return null;};unemployed.Residents.AdvanceRequests();Check(count==0,"Unemployed departed");int job=unemployed.Entities.Members[0].Person.jobId;unemployed.City.ReleaseJob(unemployed.Entities.Members[0].Person);Check(unemployed.City.AssignJob(newcomer,job),"New assignment failed");unemployed.Residents.AdvanceRequests();Check(count==1,"Assignment did not wake previously unemployed member");
  Console.WriteLine("PASS "+checks+" sparse100k derived-index/growth/churn/timer/fallback/assignment checks; synthetic index fixture is not 100k gameplay");
 }
 static List<int> SelectIds(this CityResidentSchedule.Pass pass){var result=new List<int>();foreach(var m in pass)result.Add(m.Person.id);return result;}
}
