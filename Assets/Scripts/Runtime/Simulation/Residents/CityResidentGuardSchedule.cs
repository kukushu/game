using System;
using System.Collections.Generic;
using HarborCity.RoadCore;

namespace HarborCity
{
    // Experimental derived index only. Original members, guards and dispatch
    // remain authoritative. No job/funding/time-of-day decisions live here.
    internal sealed class CityResidentGuardSchedule
    {
        readonly CityEntities owner;readonly World world;readonly CityModel city;int day;readonly HashSet<int> dayBlocked=new HashSet<int>();readonly List<int> dayWake=new List<int>();
        CityMember[] members;readonly Dictionary<int,int> slots=new Dictionary<int,int>();
        HarborCity.Diagnostics.CityWakeQueue timers;
        ulong[] ready;int[] next,previous;int head=-1,scan=-1,cursor=-1;bool inPass;readonly List<int> due=new List<int>();bool structureDirty=true;
        internal long Rebuilds,Changes;
        internal CityResidentGuardSchedule(CityEntities owner,World world,CityModel city)
        {this.owner=owner;this.world=world;this.city=city;day=city.day;owner.ResidentStateChanged+=(m,r)=>{if(r!=CityResidentStateChange.CommuteObservation)Changed(m);};owner.MembersChanged+=()=>structureDirty=true;owner.RequestGuardsChanged+=(m,g)=>Changed(m);}
        void Changed(CityMember member)
        {
            Changes++;if(structureDirty)return;
            if(!slots.TryGetValue(member.Person.id,out int i)||members[i]!=member)throw new InvalidOperationException("Guard schedule ownership mismatch");
            Classify(i);
        }
        void SetReady(int i,bool value)
        {
            ulong bit=1UL<<(i&63);bool before=(ready[i>>6]&bit)!=0;if(before==value)return;
            if(value){int left=PreviousReady(i),right=left<0?head:next[left];previous[i]=left;next[i]=right;if(left<0)head=i;else next[left]=i;if(right>=0)previous[right]=i;ready[i>>6]|=bit;
                if(inPass&&i>cursor&&(scan<0||i<scan))scan=i;}
            else{int left=previous[i],right=next[i];if(inPass&&scan==i)scan=right;if(left<0)head=right;else next[left]=right;if(right>=0)previous[right]=left;ready[i>>6]&=~bit;}
        }
        int PreviousReady(int i)
        {
            if(i==0)return -1;int at=i-1,word=at>>6;ulong bits=ready[word]&(ulong.MaxValue>>(63-(at&63)));
            while(bits==0){if(--word<0)return -1;bits=ready[word];}
            bits|=bits>>1;bits|=bits>>2;bits|=bits>>4;bits|=bits>>8;bits|=bits>>16;bits|=bits>>32;
            ulong high=bits-(bits>>1);return (word<<6)+offsets[unchecked((int)((high*0x03f79d71b4cb0a89UL)>>58))];
        }
        void Classify(int i)
        {
            if(day!=city.day)RefreshDay();
            var m=members[i];var p=m.Person;dayBlocked.Remove(i);
            if(!m.Family.resident||p.dead||(p.location==m.Family.home&&(p.atWork||!city.CanAttendWork(p)))){SetReady(i,false);timers.Cancel(i);return;}
            if(p.tripId==0&&p.location==m.Family.home&&p.departureDay==city.day){dayBlocked.Add(i);SetReady(i,false);timers.Cancel(i);return;}
            if(p.tripId!=0){SetReady(i,false);timers.Cancel(i);return;}
            // Match the original two '<' comparisons separately. NaN does not
            // block its own guard and must not swallow the other finite guard.
            double deadline=world.Clock;
            if(world.Clock<p.retryAt)deadline=p.retryAt;
            if(world.Clock<p.visitUntil&&p.visitUntil>deadline)deadline=p.visitUntil;
            if(deadline>world.Clock){SetReady(i,false);timers.Schedule(i,double.IsPositiveInfinity(deadline)?long.MaxValue:HarborCity.Diagnostics.CityWakeQueue.FirstTick(deadline,owner.Simulation.Tick,SimulationClock.StepSeconds));}
            else{timers.Cancel(i);SetReady(i,true);}
        }
        void RefreshDay()
        {
            if(day==city.day)return;day=city.day;dayWake.Clear();foreach(int i in dayBlocked)dayWake.Add(i);dayBlocked.Clear();foreach(int i in dayWake)Classify(i);dayWake.Clear();
        }
        internal Pass Begin()
        {
            inPass=false;
            if(structureDirty){structureDirty=false;members=new CityMember[owner.Members.Count];slots.Clear();ready=new ulong[(members.Length+63)/64];next=new int[members.Length];previous=new int[members.Length];head=-1;dayBlocked.Clear();dayWake.Clear();day=city.day;timers=new HarborCity.Diagnostics.CityWakeQueue(Math.Max(1,members.Length));Rebuilds++;
                for(int i=0;i<members.Length;i++){var m=owner.Members[i];members[i]=m;slots.Add(m.Person.id,i);Classify(i);}}
            RefreshDay();timers.DrainDue(owner.Simulation.Tick,due);foreach(int i in due)Classify(i);
            scan=head;cursor=-1;inPass=true;return new Pass(this);
        }
        static readonly int[] offsets={0,1,48,2,57,49,28,3,61,58,50,42,38,29,17,4,62,55,59,36,53,51,43,22,45,39,33,30,24,18,12,5,63,47,56,27,60,41,37,16,54,35,52,21,44,32,23,11,46,26,40,15,34,20,31,10,25,14,19,9,13,8,7,6};
        internal struct Pass
        {
            readonly CityResidentGuardSchedule index;
            internal Pass(CityResidentGuardSchedule index){this.index=index;}
            public Enumerator GetEnumerator()=>new Enumerator(index);
        }
        internal struct Enumerator
        {
            readonly CityResidentGuardSchedule index;
            internal Enumerator(CityResidentGuardSchedule index){this.index=index;Current=null;}
            public CityMember Current {get;private set;}
            public bool MoveNext()
            {
                index.RefreshDay();int i=index.scan;if(i<0){index.inPass=false;Current=null;return false;}
                index.cursor=i;index.scan=index.next[i];Current=index.members[i];return true;
            }
        }
    }
}
