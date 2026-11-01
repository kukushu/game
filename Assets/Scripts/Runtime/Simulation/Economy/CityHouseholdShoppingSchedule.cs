#if CITY_HOUSEHOLD_SHOPPING_SCHEDULE
using System;
using System.Collections.Generic;
using HarborCity.RoadCore;
using HarborCity.Diagnostics;
namespace HarborCity
{
    // Conservative candidate index, not a second household state. Unknown/raw
    // age and carriedGoods never put a family to sleep. Original predicates run.
    internal sealed class CityHouseholdShoppingSchedule
    {
        readonly CityModel city;readonly CityEntities owner;readonly World world;readonly RetailRules rules;
        Household[] families;readonly Dictionary<int,int> slots=new Dictionary<int,int>();
        readonly HashSet<Household> dirty=new HashSet<Household>();readonly List<int> due=new List<int>();
        ulong[] ready;CityWakeQueue timers;bool structureDirty=true;int day;
        internal long Rebuilds,Notifications,Classifications,Candidates,Passes;
        internal CityHouseholdShoppingSchedule(CityModel city,CityEntities owner,World world,RetailRules rules)
        {
            this.city=city;this.owner=owner;this.world=world;this.rules=rules;
            owner.ShoppingFamilyFactsChanged+=Changed;owner.MembersChanged+=()=>structureDirty=true;
            owner.ResidentStateChanged+=(m,r)=>{if(r==CityResidentStateChange.Death||r==CityResidentStateChange.Sickness||r==CityResidentStateChange.Location||r==CityResidentStateChange.Attendance||r==CityResidentStateChange.Membership)MemberChanged(m);};
            owner.RequestGuardsChanged+=(m,g)=>{if(g==CityResidentGuard.Trip||g==CityResidentGuard.Retry)MemberChanged(m);};
        }
        void MemberChanged(CityMember member)
        {
            var h=member.Family;
            // These family gates have their own notifications/day refresh. A
            // person's movement cannot make a gated family eligible by itself.
            if(h.resident&&h.shoppingDay!=city.day&&h.savings>=rules.UnitPrice)Changed(h);
        }
        void Changed(Household family)
        {Notifications++;if(structureDirty)return;if(!slots.TryGetValue(family.id,out int i)||families[i]!=family){structureDirty=true;return;}dirty.Add(family);}
        void SetReady(int i,bool value){ulong bit=1UL<<(i&63);if(value)ready[i>>6]|=bit;else ready[i>>6]&=~bit;}
        void Classify(int i)
        {
            Classifications++;var h=families[i];timers.Cancel(i);SetReady(i,false);
            if(!h.resident||h.shoppingDay==city.day||h.savings<rules.UnitPrice)return;
            double earliest=double.PositiveInfinity;
            foreach(var p in h.people)
            {
                if(p.dead||p.sick||p.atWork||p.tripId!=0||p.location!=h.home)continue;
                // Shopping uses >=, not the commute loop's negated < guard.
                if(world.Clock>=p.retryAt){SetReady(i,true);return;}
                if(!float.IsNaN(p.retryAt)&&p.retryAt<earliest)earliest=p.retryAt;
            }
            if(double.IsPositiveInfinity(earliest))return;
            if(owner.Simulation.Tick>1000000000000L||earliest>50000000000.0){SetReady(i,true);return;}
            timers.Schedule(i,CityWakeQueue.FirstTick(earliest,owner.Simulation.Tick,SimulationClock.StepSeconds));
        }
        void Refresh()
        {
            if(structureDirty)
            {
                structureDirty=false;families=city.society.families.ToArray();slots.Clear();ready=new ulong[(families.Length+63)/64];timers=new CityWakeQueue(Math.Max(1,families.Length));dirty.Clear();day=city.day;Rebuilds++;
                for(int i=0;i<families.Length;i++){slots.Add(families[i].id,i);Classify(i);}return;
            }
            if(day!=city.day){day=city.day;dirty.Clear();for(int i=0;i<families.Length;i++)Classify(i);return;}
            foreach(var h in dirty)Classify(slots[h.id]);dirty.Clear();
        }
        internal Pass Begin()
        {Passes++;Refresh();timers.DrainDue(owner.Simulation.Tick,due);foreach(int i in due)Classify(i);return new Pass(this);}
        int Next(int after)
        {
            Refresh();if(after>=families.Length)return -1;
            int word=after>>6;ulong bits=ready[word]&(ulong.MaxValue<<(after&63));
            while(bits==0){if(++word>=ready.Length)return -1;bits=ready[word];}
            int offset=0;while((bits&1)==0){bits>>=1;offset++;}return (word<<6)+offset;
        }
        internal void AssertOracle()
        {
            Begin();for(int i=0;i<families.Length;i++)
            {
                var h=families[i];if(!h.resident||h.shoppingDay==city.day||h.savings<rules.UnitPrice)continue;
                foreach(var p in h.people)if(!p.dead&&!p.sick&&p.age>=18&&!p.atWork&&p.tripId==0&&p.location==h.home&&p.carriedGoods==0&&world.Clock>=p.retryAt)
                    if((ready[i>>6]&(1UL<<(i&63)))==0)throw new InvalidOperationException("Shopping wake missed eligible family "+h.id);
            }
        }
        internal long LogicalBytes=>(families?.LongLength??0)*8+(ready?.LongLength??0)*8+(timers?.LogicalBytes??0);
        internal readonly struct Pass
        {readonly CityHouseholdShoppingSchedule owner;internal Pass(CityHouseholdShoppingSchedule owner){this.owner=owner;}public Enumerator GetEnumerator()=>new Enumerator(owner);}
        internal struct Enumerator
        {
            readonly CityHouseholdShoppingSchedule owner;int cursor;
            internal Enumerator(CityHouseholdShoppingSchedule owner){this.owner=owner;cursor=-1;Current=null;}
            public Household Current {get;private set;}
            public bool MoveNext(){int i=owner.Next(cursor+1);if(i<0){Current=null;return false;}cursor=i;Current=owner.families[i];owner.Candidates++;return true;}
        }
    }
}
#endif
