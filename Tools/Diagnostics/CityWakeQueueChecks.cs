using System;using System.Collections.Generic;using System.Diagnostics;using System.Linq;using HarborCity.Diagnostics;
static class CityWakeQueueChecks {
 static int checks;static void Check(bool ok){checks++;if(!ok)throw new Exception("Wake queue mismatch #"+checks);}
 static void Main(){
  var q=new CityWakeQueue(2048);var oracle=new Dictionary<int,long>();var random=new Random(7919);var ready=new List<int>();long now=0;
  for(int i=0;i<200000;i++){
   int who=random.Next(2048),op=random.Next(4);
   if(op<2){long due=now+random.Next(200);q.Schedule(who,due);oracle[who]=due;}
   else if(op==2){Check(q.Cancel(who)==oracle.Remove(who));}
   else{now+=random.Next(4);var expected=oracle.Where(p=>p.Value<=now).Select(p=>p.Key).OrderBy(x=>x).ToArray();q.DrainDue(now,ready);Check(ready.SequenceEqual(expected));foreach(int id in expected)oracle.Remove(id);}
   Check(q.Count==oracle.Count&&q.Count<=2048);
  }
  // Earlier/later reschedules replace pending work, including already-cancelled events.
  q=new CityWakeQueue(4);q.Schedule(3,100);q.Schedule(1,90);q.Schedule(3,20);q.Schedule(1,150);q.DrainDue(100,ready);Check(ready.SequenceEqual(new[]{3}));q.Cancel(1);q.DrainDue(200,ready);Check(ready.Count==0);
  // Overdue objects must be restored to member order even when their due ticks differ.
  q.Schedule(2,5);q.Schedule(0,10);q.DrainDue(20,ready);Check(ready.SequenceEqual(new[]{0,2}));
  foreach(long baseTick in new long[]{0,1,5,7199,24000000,999999999980})for(int offset=0;offset<20;offset++){
   long t=baseTick+offset;double exact=t*.05;double[] values={exact,(double)(float)exact,Math.BitDecrement(exact),Math.BitIncrement(exact)};
   foreach(double value in values){long got=CityWakeQueue.FirstTick(value,baseTick,.05);Check(got>=baseTick&&got<=1000000000000L);Check(got*.05>=value);Check(got==baseTick||(got-1)*.05<value);}
  }
  // Million *queue slots* is not a million-agent city performance result.
  q=null;GC.Collect();long before=GC.GetTotalMemory(true);var clock=Stopwatch.StartNew();q=new CityWakeQueue(1000000);for(int i=0;i<1000000;i++)q.Schedule(i,i%17);clock.Stop();long retained=GC.GetTotalMemory(true)-before;
  q.DrainDue(16,ready);Check(ready.Count==1000000&&q.Count==0);for(int i=0;i<ready.Count;i++)Check(ready[i]==i);
  Console.WriteLine("PASS checks="+checks+" millionQueueSlotsLogicalBytes="+q.LogicalBytes+" observedDotNetRetainedBytes="+retained+" buildAndScheduleMs="+clock.Elapsed.TotalMilliseconds.ToString("F3")+"; no city load, no Player/FPS claim");
 }
}
