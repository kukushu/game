using System;using System.Collections.Generic;using HarborCity;using HarborCity.RoadCore;
// Diagnostic shadow only. All decisions and payments still come from AdvanceRequests.
internal sealed class ResidentWakeShadow {
 readonly CityEntities owner;readonly World world;HarborCity.Diagnostics.CityWakeQueue queue;
 Dictionary<int,int> ordinal;List<CityMember> members;HashSet<int> conservative;
 readonly List<int> ready=new List<int>(),candidateOrder=new List<int>();long[] readyAt;long frame;int lastRequest;bool structureDirty=true;static int lastPending;
 internal static long Frames,OracleVisits,Candidates,Requests,Misses,Rebuilds;
 internal ResidentWakeShadow(CityEntities e,World w){owner=e;world=w;e.WakeStructureChanged+=()=>structureDirty=true;}
 long Tick=>owner.Simulation.Tick;
 void Classify(int i){var p=members[i].Person;if(p.tripId!=0){queue.Cancel(i);conservative.Remove(i);return;}double until=Math.Max((double)p.retryAt,p.visitUntil);if(!double.IsNaN(until)&&!double.IsInfinity(until)&&until>world.Clock){conservative.Remove(i);queue.Schedule(i,HarborCity.Diagnostics.CityWakeQueue.FirstTick(until,Tick,SimulationClock.StepSeconds));}else{queue.Cancel(i);conservative.Add(i);}}
 internal void Changed(int citizen){if(structureDirty||ordinal==null)return;if(!ordinal.TryGetValue(citizen,out int i)){structureDirty=true;return;}Classify(i);}
 internal void Begin(){var e=owner;
  if(structureDirty||members.Count!=e.Members.Count){structureDirty=false;members=new List<CityMember>(e.Members);ordinal=new Dictionary<int,int>();conservative=new HashSet<int>();queue=new HarborCity.Diagnostics.CityWakeQueue(Math.Max(1,members.Count));readyAt=new long[members.Count];frame=0;Rebuilds++;for(int i=0;i<members.Count;i++){ordinal.Add(members[i].Person.id,i);Classify(i);}}
  Frames++;frame++;lastRequest=-1;queue.DrainDue(Tick,ready);foreach(int i in ready)conservative.Add(i);candidateOrder.Clear();foreach(int i in conservative)candidateOrder.Add(i);candidateOrder.Sort();foreach(int i in candidateOrder)readyAt[i]=frame;Candidates+=candidateOrder.Count;
 }
 internal struct Visit:IDisposable {int i;ResidentWakeShadow owner;internal Visit(ResidentWakeShadow owner,CityMember m){this.owner=owner;if(!owner.ordinal.TryGetValue(m.Person.id,out i)||!ReferenceEquals(owner.members[i],m))throw new InvalidOperationException("Shadow Members order invalidated without notification");OracleVisits++;}public void Dispose(){if(owner.readyAt[i]==owner.frame)owner.Classify(i);lastPending=owner.queue.Count;}}
 internal Visit Observe(CityMember m)=>new Visit(this,m);
 internal void Request(int citizen){int i=ordinal[citizen];Requests++;if(readyAt[i]!=frame){Misses++;throw new InvalidOperationException("Shadow missed request at tick "+Tick+" citizen "+citizen);}if(i<=lastRequest)throw new InvalidOperationException("Shadow request Members order mismatch");lastRequest=i;}
 internal static string Summary()=>"frames="+Frames+",oracleVisits="+OracleVisits+",candidateVisits="+Candidates+",requests="+Requests+",misses="+Misses+",rebuilds="+Rebuilds+",pending="+lastPending;
}
