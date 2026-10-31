using System;using System.Collections.Generic;using HarborCity;using HarborCity.RoadCore;
// Diagnostic shadow only. All decisions and payments still come from AdvanceRequests.
internal sealed class ResidentWakeShadow {
 readonly CityEntities owner;readonly World world;HarborCity.Diagnostics.CityWakeQueue queue;
 Dictionary<int,int> ordinal;List<CityMember> members;HashSet<int> conservative;
 readonly List<int> ready=new List<int>(),candidateOrder=new List<int>();long[] readyAt;long frame;int lastRequest;bool structureDirty=true;static int lastPending;bool inPass;int cursor;
 internal static long Frames,OracleVisits,Candidates,Requests,Misses,Rebuilds;
 readonly CityModel city;readonly double offset;readonly bool full;
 bool mode;bool[] hasPlan;ResidentTimeCheck[] plans;
 readonly Dictionary<int,HashSet<int>> dependencies=new Dictionary<int,HashSet<int>>();
 int[][] endpointKeys;
 internal static long TemporalPlans,LocalChanges,GlobalChanges,SuppressedLearning,PeakQueueBytes,PeakDependencies;long dependencyLinks;
 internal ResidentWakeShadow(CityEntities e,World w,CityCoreResidents requests,CityModel city,double offset):this(e,w){
  this.city=city;this.offset=offset;full=true;mode=city.development.enabled;
  e.JobAssignmentsChanged+=m=>Changed(m.Person.id);e.FundingEligibilityChanged+=m=>Changed(m.Person.id);
  e.ResidentStateChanged+=(m,r)=>Changed(m.Person.id);requests.PlansInvalidated+=Invalidate;
 }
 void Invalidate(ResidentPlanInvalidation change){
  if(structureDirty||ordinal==null)return;
  if(change.All){GlobalChanges++;for(int i=0;i<members.Count;i++)Changed(members[i].Person.id);}
  else if(dependencies.TryGetValue(change.Endpoint,out var set)){LocalChanges++;foreach(int i in set)Changed(members[i].Person.id);}
 }
 void CheckMode(){if(full&&mode!=city.development.enabled){mode=city.development.enabled;GlobalChanges++;for(int i=0;i<members.Count;i++)Changed(members[i].Person.id);}}
 void BindDependencies(int i){
  var m=members[i];int home=m.Family.home,work=city.Workplace(m.Person),location=m.Person.location;
  var previous=endpointKeys[i];if(previous!=null&&previous[0]==home&&previous[1]==work&&previous[2]==location)return;
  if(previous!=null)foreach(int endpoint in previous)if(dependencies.TryGetValue(endpoint,out var set)){if(set.Remove(i))dependencyLinks--;if(set.Count==0)dependencies.Remove(endpoint);}
  var keys=previous??new int[3];keys[0]=home;keys[1]=work;keys[2]=location;endpointKeys[i]=keys;
  foreach(int endpoint in keys){if(endpoint<0)continue;if(!dependencies.TryGetValue(endpoint,out var set)){set=new HashSet<int>();dependencies.Add(endpoint,set);}if(set.Add(i))dependencyLinks++;}PeakDependencies=Math.Max(PeakDependencies,dependencyLinks);
 }
 internal void Evaluate(CityMember member,float shift,bool eligible,double estimate,bool timedReturn,bool request){
  if(!full)return;int i=ordinal[member.Person.id];if(readyAt[i]!=frame){SuppressedLearning++;return;}
  hasPlan[i]=false;if(request)return;var p=member.Person;
  if(eligible){plans[i]=CityResidentRequestTime.Departure(Tick+1,offset,city.society.settings.secondsPerDay,p.retryAt,p.visitUntil,shift,owner.Employment.ShiftMinutes,estimate);hasPlan[i]=true;}
  else if(timedReturn){plans[i]=CityResidentRequestTime.ShiftEnd(Tick+1,offset,city.society.settings.secondsPerDay,p.retryAt,p.visitUntil,shift,owner.Employment.ShiftMinutes);hasPlan[i]=true;}
  if(hasPlan[i])TemporalPlans++;
 }

 internal ResidentWakeShadow(CityEntities e,World w){owner=e;world=w;e.MembersChanged+=()=>structureDirty=true;e.RequestGuardsChanged+=(m,g)=>Changed(m.Person.id);}
 long Tick=>owner.Simulation.Tick;
 void Classify(int i){if(full)BindDependencies(i);var p=members[i].Person;if(p.tripId!=0){queue.Cancel(i);conservative.Remove(i);return;}if(full&&hasPlan[i]){conservative.Remove(i);queue.Schedule(i,plans[i].Tick);return;}
 double until=Math.Max((double)p.retryAt,p.visitUntil);if(!double.IsNaN(until)&&!double.IsInfinity(until)&&until>world.Clock){conservative.Remove(i);queue.Schedule(i,HarborCity.Diagnostics.CityWakeQueue.FirstTick(until,Tick,SimulationClock.StepSeconds));}else{queue.Cancel(i);conservative.Add(i);}}
 internal void Changed(int citizen){if(structureDirty||ordinal==null)return;if(!ordinal.TryGetValue(citizen,out int i)){structureDirty=true;return;}if(full){hasPlan[i]=false;conservative.Remove(i);queue.Schedule(i,Tick);}else Classify(i);
  // A synchronous request callback can release a later member in the same pass.
  // Only future Members slots may become candidates now; previous slots wait until next tick.
  var p=members[i].Person;if(inPass&&i>cursor&&p.tripId==0&&!(world.Clock<p.retryAt||world.Clock<p.visitUntil)&&readyAt[i]!=frame){readyAt[i]=frame;Candidates++;}
 }
 internal struct Pass:IDisposable {readonly ResidentWakeShadow owner;internal Pass(ResidentWakeShadow owner){this.owner=owner;}public void Dispose(){owner.inPass=false;}}
 internal Pass Begin(){var e=owner;
  if(structureDirty||members.Count!=e.Members.Count){structureDirty=false;members=new List<CityMember>(e.Members);ordinal=new Dictionary<int,int>();conservative=new HashSet<int>();queue=new HarborCity.Diagnostics.CityWakeQueue(Math.Max(1,members.Count));readyAt=new long[members.Count];if(full){hasPlan=new bool[members.Count];plans=new ResidentTimeCheck[members.Count];endpointKeys=new int[members.Count][];dependencies.Clear();dependencyLinks=0;}PeakQueueBytes=Math.Max(PeakQueueBytes,queue.LogicalBytes);frame=0;Rebuilds++;for(int i=0;i<members.Count;i++){ordinal.Add(members[i].Person.id,i);Classify(i);}}
  CheckMode();Frames++;frame++;lastRequest=-1;cursor=-1;inPass=true;queue.DrainDue(Tick,ready);foreach(int i in ready)conservative.Add(i);candidateOrder.Clear();foreach(int i in conservative)candidateOrder.Add(i);candidateOrder.Sort();foreach(int i in candidateOrder)readyAt[i]=frame;Candidates+=candidateOrder.Count;return new Pass(this);
 }
 internal struct Visit:IDisposable {int i;ResidentWakeShadow owner;internal Visit(ResidentWakeShadow owner,CityMember m){this.owner=owner;if(!owner.ordinal.TryGetValue(m.Person.id,out i)||!ReferenceEquals(owner.members[i],m))throw new InvalidOperationException("Shadow Members order invalidated without notification");if(i<=owner.cursor)throw new InvalidOperationException("Shadow visit Members order mismatch");owner.cursor=i;OracleVisits++;}public void Dispose(){owner.CheckMode();if(owner.readyAt[i]==owner.frame)owner.Classify(i);lastPending=owner.queue.Count;}}
 internal Visit Observe(CityMember m)=>new Visit(this,m);
 internal void Request(int citizen){int i=ordinal[citizen];Requests++;if(readyAt[i]!=frame){Misses++;throw new InvalidOperationException("Shadow missed request at tick "+Tick+" citizen "+citizen);}if(i<=lastRequest)throw new InvalidOperationException("Shadow request Members order mismatch");lastRequest=i;}
 internal static string Summary()=>"peakQueueBytes="+PeakQueueBytes+",peakDependencyLinks="+PeakDependencies+",temporalPlans="+TemporalPlans+",localChanges="+LocalChanges+",globalChanges="+GlobalChanges+",suppressedLearning="+SuppressedLearning+","+"frames="+Frames+",oracleVisits="+OracleVisits+",candidateVisits="+Candidates+",requests="+Requests+",misses="+Misses+",rebuilds="+Rebuilds+",pending="+lastPending;
}
