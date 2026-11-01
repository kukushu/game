using System;
using System.Collections.Generic;
using HarborCity.RoadCore;
using HarborCity.Diagnostics;
namespace HarborCity
{
    // One derived execution index. Decisions remain in CityCoreResidents.
    // Slots preserve Members' relative order; removals leave holes, additions append.
    internal sealed class CityResidentSchedule
    {
        readonly CityEntities owner;readonly World world;readonly CityModel city;
        readonly bool temporal,fundingWait;readonly double offset;
        bool initialized,mode,calendarOk,inPass;int day,used,live,head=-1,scan=-1,cursor=-1;
        float daySeconds,sampledElapsed;double sampledClock=double.NaN;
        CityMember[] members;ulong[] ready;int[] next,previous,wordTree;bool[] queued,planned;
        struct Dependencies {internal int home,work,location;internal bool valid;}
        Dependencies[] keys;
        readonly Dictionary<int,int> slots=new Dictionary<int,int>();
        readonly Dictionary<int,HashSet<int>> endpoints=new Dictionary<int,HashSet<int>>();
        readonly HashSet<int> livePlans=new HashSet<int>(),dayBlocked=new HashSet<int>();
        readonly List<int> dirty=new List<int>(),due=new List<int>();CityWakeQueue timers;
        internal long Rebuilds,Changes,FundingChecks,FundingSleeps,Classifications,Visits,MemberAdds,MemberRemoves,CapacityChanges,PredecessorSteps;
        internal int ReadyCount {get;private set;}
        internal int SlotCount=>used;
        internal CityResidentSchedule(CityEntities owner,World world,CityModel city,CityCoreResidents requests=null,double offset=50,bool fundingWait=false)
        {
            this.owner=owner;this.world=world;this.city=city;this.offset=offset;this.fundingWait=fundingWait;temporal=requests!=null;
            day=city.day;mode=city.development.enabled;daySeconds=city.society.settings.secondsPerDay;
            if(temporal||fundingWait){owner.JobAssignmentsChanged+=Changed;owner.FundingEligibilityChanged+=Changed;}
            if(temporal)requests.PlansInvalidated+=Invalidate;
            owner.ResidentStateChanged+=(m,r)=>{if(temporal||r!=CityResidentStateChange.CommuteObservation)Changed(m);};
            owner.RequestGuardsChanged+=(m,g)=>Changed(m);owner.MemberStructureChanged+=Membership;
        }
        internal void Reset(){initialized=false;inPass=false;}
        void Changed(CityMember member)
        {
            Changes++;if(!initialized)return;
            if(!slots.TryGetValue(member.Person.id,out int i)||members[i]!=member)throw new InvalidOperationException("Resident schedule ownership mismatch");
            Mark(i);
        }
        void Mark(int i){if(!queued[i]){queued[i]=true;dirty.Add(i);}}
        void Membership(CityMember member,bool added)
        {
            if(!initialized)return;
            if(added){Append(member);MemberAdds++;}
            else{
                if(!slots.TryGetValue(member.Person.id,out int i)||members[i]!=member)throw new InvalidOperationException("Removing foreign scheduled resident");
                ClearPlan(i);Unregister(i);timers.Cancel(i);SetReady(i,false);dayBlocked.Remove(i);slots.Remove(member.Person.id);members[i]=null;queued[i]=false;live--;MemberRemoves++;
            }
        }
        void Capacity(int minimum)
        {
            if(members!=null&&minimum<=members.Length)return;
            int n=members==null?64:members.Length;while(n<minimum)n=checked(n*2);
            Array.Resize(ref members,n);Array.Resize(ref queued,n);Array.Resize(ref next,n);Array.Resize(ref previous,n);Array.Resize(ref ready,(n+63)/64);
            if(temporal){Array.Resize(ref planned,n);Array.Resize(ref keys,n);}
            if(timers==null)timers=new CityWakeQueue(n);else timers.EnsureCapacity(n);
            wordTree=new int[ready.Length+1];
            // Linear Fenwick construction; predecessor never scans empty city blocks.
            for(int i=1;i<wordTree.Length;i++){wordTree[i]+=ready[i-1]!=0?1:0;int parent=i+(i&-i);if(parent<wordTree.Length)wordTree[parent]+=wordTree[i];}
            CapacityChanges++;
        }
        void Append(CityMember member){Capacity(used+1);int i=used++;members[i]=member;slots.Add(member.Person.id,i);live++;Mark(i);}
        void Build()
        {
            members=null;ready=null;next=previous=wordTree=null;queued=planned=null;keys=null;timers=null;slots.Clear();endpoints.Clear();livePlans.Clear();dayBlocked.Clear();dirty.Clear();
            used=live=ReadyCount=0;head=-1;initialized=true;day=city.day;mode=city.development.enabled;daySeconds=city.society.settings.secondsPerDay;sampledClock=double.NaN;
            Capacity(Math.Max(1,owner.Members.Count));foreach(var member in owner.Members)Append(member);Rebuilds++;
        }
        void WordChanged(int word,int delta){for(int i=word+1;i<wordTree.Length;i+=i&-i)wordTree[i]+=delta;}
        static int Highest(ulong bits){bits|=bits>>1;bits|=bits>>2;bits|=bits>>4;bits|=bits>>8;bits|=bits>>16;bits|=bits>>32;ulong high=bits-(bits>>1);return offsets[unchecked((int)((high*0x03f79d71b4cb0a89UL)>>58))];}
        int PreviousReady(int i)
        {
            if(i==0)return -1;int word=(i-1)>>6;ulong bits=ready[word]&(ulong.MaxValue>>(63-((i-1)&63)));
            if(bits!=0)return (word<<6)+Highest(bits);
            int rank=0;for(int k=word;k>0;k-=k&-k){PredecessorSteps++;rank+=wordTree[k];}if(rank==0)return -1;
            int at=0,power=1;while(power<=ready.Length/2)power*=2;
            for(;power>0;power>>=1){PredecessorSteps++;int target=at+power;if(target<wordTree.Length&&wordTree[target]<rank){rank-=wordTree[target];at=target;}}
            return (at<<6)+Highest(ready[at]);
        }
        void SetReady(int i,bool value)
        {
            int word=i>>6;ulong bit=1UL<<(i&63);bool before=(ready[word]&bit)!=0;if(before==value)return;
            if(value){int left=PreviousReady(i),right=left<0?head:next[left];previous[i]=left;next[i]=right;if(left<0)head=i;else next[left]=i;if(right>=0)previous[right]=i;if(ready[word]==0)WordChanged(word,1);ready[word]|=bit;ReadyCount++;if(inPass&&i>cursor&&(scan<0||i<scan))scan=i;}
            else{int left=previous[i],right=next[i];if(inPass&&scan==i)scan=right;if(left<0)head=right;else next[left]=right;if(right>=0)previous[right]=left;ready[word]&=~bit;if(ready[word]==0)WordChanged(word,-1);ReadyCount--;}
        }
        void Classify(int i)
        {
            Classifications++;var m=members[i];var p=m.Person;dayBlocked.Remove(i);
            if(!m.Family.resident||p.dead||p.location==m.Family.home&&(p.atWork||!city.CanAttendWork(p))){SetReady(i,false);timers.Cancel(i);return;}
            if(p.tripId==0&&p.location==m.Family.home&&p.departureDay==city.day){dayBlocked.Add(i);SetReady(i,false);timers.Cancel(i);return;}
            if(p.tripId!=0){SetReady(i,false);timers.Cancel(i);return;}
            if(fundingWait&&p.location==m.Family.home&&WaitForEmployment(p)){SetReady(i,false);timers.Cancel(i);return;}
            double deadline=world.Clock;
            if(world.Clock<p.retryAt)deadline=p.retryAt;
            if(world.Clock<p.visitUntil&&p.visitUntil>deadline)deadline=p.visitUntil;
            if(deadline>world.Clock){SetReady(i,false);timers.Schedule(i,double.IsPositiveInfinity(deadline)?long.MaxValue:CityWakeQueue.FirstTick(deadline,owner.Simulation.Tick,SimulationClock.StepSeconds));}
            else{timers.Cancel(i);SetReady(i,true);}
        }
        bool WaitForEmployment(CityResident person)
        {
            var job=city.ResidentJob(person);
            // Assignment writes and new/deleted jobs already notify this owner.
            if(job==null){FundingSleeps++;return true;}
            var employer=owner.Building(job.buildingId);
            bool tracked=employer is IndustrialBuilding f&&ReferenceEquals(f.factory.FundingOwner,owner)||employer is CommercialBuilding shop&&ReferenceEquals(shop.Account.FundingOwner,owner);
            if(!tracked)return false;FundingChecks++;if(city.JobFunded(job))return false;FundingSleeps++;return true;
        }
        void ClearPlan(int i){if(!temporal)return;planned[i]=false;livePlans.Remove(i);}
        void DrainChanges()
        {
            for(int at=0;at<dirty.Count;at++){int i=dirty[at];if(!queued[i]||members[i]==null)continue;queued[i]=false;ClearPlan(i);Classify(i);}dirty.Clear();
        }
        void Invalidate(ResidentPlanInvalidation change)
        {
            if(!initialized)return;
            if(change.All){InvalidatePlans();return;}
            if(endpoints.TryGetValue(change.Endpoint,out var affected))foreach(int i in affected)if(planned[i])Mark(i);
        }
        void InvalidatePlans(){if(temporal&&initialized)foreach(int i in livePlans)Mark(i);}
        void RefreshContext()
        {
            bool changedDay=day!=city.day,changedPolicy=temporal&&(mode!=city.development.enabled||!daySeconds.Equals(city.society.settings.secondsPerDay));
            bool clockChanged=temporal&&sampledClock!=world.Clock,elapsedChanged=temporal&&!sampledElapsed.Equals(city.society.dayElapsed);
            if(!changedDay&&!changedPolicy&&!clockChanged&&!elapsedChanged)return;
            day=city.day;mode=city.development.enabled;daySeconds=city.society.settings.secondsPerDay;bool wasValid=calendarOk;
            if(temporal){sampledClock=world.Clock;sampledElapsed=city.society.dayElapsed;double absolute=offset+sampledClock;calendarOk=daySeconds>=1&&!float.IsInfinity(daySeconds)&&sampledElapsed.Equals((float)(absolute%daySeconds))&&city.day==1+(int)Math.Floor(absolute/daySeconds);}
            if(temporal&&(changedDay||changedPolicy||wasValid!=calendarOk||elapsedChanged&&!clockChanged))InvalidatePlans();
            if(changedDay){foreach(int i in dayBlocked)Mark(i);dayBlocked.Clear();}
        }
        void RemoveKey(int endpoint,int i){if(endpoint<0)return;if(endpoints.TryGetValue(endpoint,out var set)){set.Remove(i);if(set.Count==0)endpoints.Remove(endpoint);}}
        void AddKey(int endpoint,int i){if(endpoint<0)return;if(!endpoints.TryGetValue(endpoint,out var set)){set=new HashSet<int>();endpoints.Add(endpoint,set);}set.Add(i);}
        void Unregister(int i){if(!temporal||!keys[i].valid)return;var k=keys[i];RemoveKey(k.home,i);RemoveKey(k.work,i);RemoveKey(k.location,i);keys[i]=default;}
        internal void PlanWait(CityMember member,int work,float shift,bool eligible,double estimate,bool timedReturn)
        {
            if(!temporal||!calendarOk)return;int i=slots[member.Person.id];var p=member.Person;ResidentTimeCheck plan;
            if(eligible)plan=CityResidentRequestTime.Departure(owner.Simulation.Tick+1,offset,daySeconds,p.retryAt,p.visitUntil,shift,owner.Employment.ShiftMinutes,estimate);
            else if(timedReturn)plan=CityResidentRequestTime.ShiftEnd(owner.Simulation.Tick+1,offset,daySeconds,p.retryAt,p.visitUntil,shift,owner.Employment.ShiftMinutes);
            else return;
            var k=keys[i];int home=member.Family.home,location=p.location;
            if(!k.valid||k.home!=home||k.work!=work||k.location!=location){Unregister(i);keys[i]=new Dependencies{valid=true,home=home,work=work,location=location};AddKey(home,i);AddKey(work,i);AddKey(location,i);}
            planned[i]=true;livePlans.Add(i);SetReady(i,false);timers.Schedule(i,plan.Tick);
        }
        internal Pass Begin()
        {
            inPass=false;
            // Cold build or explicit mode reset; sparse holes compact only after
            // at least half the historical slots were removed (amortized churn).
            if(!initialized||used>128&&used-live>live)Build();
            RefreshContext();DrainChanges();timers.DrainDue(owner.Simulation.Tick,due);foreach(int i in due)Mark(i);DrainChanges();scan=head;cursor=-1;inPass=true;return new Pass(this,null);
        }
        static readonly int[] offsets={0,1,48,2,57,49,28,3,61,58,50,42,38,29,17,4,62,55,59,36,53,51,43,22,45,39,33,30,24,18,12,5,63,47,56,27,60,41,37,16,54,35,52,21,44,32,23,11,46,26,40,15,34,20,31,10,25,14,19,9,13,8,7,6};
        internal readonly struct Pass
        {
            readonly CityResidentSchedule index;readonly IReadOnlyList<CityMember> polling;
            internal Pass(CityResidentSchedule index,IReadOnlyList<CityMember> polling){this.index=index;this.polling=polling;}
            internal static Pass Reference(IReadOnlyList<CityMember> members)=>new Pass(null,members);
            public Enumerator GetEnumerator()=>new Enumerator(index,polling);
        }
        internal struct Enumerator
        {
            readonly CityResidentSchedule index;readonly IReadOnlyList<CityMember> polling;int at;
            internal Enumerator(CityResidentSchedule index,IReadOnlyList<CityMember> polling){this.index=index;this.polling=polling;at=-1;Current=null;}
            public CityMember Current {get;private set;}
            public bool MoveNext()
            {
                if(index==null){if(++at>=polling.Count)return false;Current=polling[at];return true;}
                index.RefreshContext();index.DrainChanges();int i=index.scan;if(i<0){index.inPass=false;Current=null;return false;}
                index.cursor=i;index.scan=index.next[i];Current=index.members[i];index.Visits++;return true;
            }
        }
    }
}
