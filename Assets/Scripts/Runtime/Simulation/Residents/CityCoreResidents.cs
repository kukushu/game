using System;
using System.Collections.Generic;
using HarborCity.RoadCore;

namespace HarborCity
{
    internal enum ResidentTimeCheckKind { Predicate, Calendar, Conservative, ClockEnd }
    internal readonly struct ResidentTimeCheck
    {
        internal readonly long Tick;internal readonly ResidentTimeCheckKind Kind;
        internal ResidentTimeCheck(long tick,ResidentTimeCheckKind kind){Tick=tick;Kind=kind;}
    }
    // Pure temporal predicates only: no routes, cached citizens, dispatch or clock
    // mutation. Production polling does not call this until later shadow gates.
    internal static class CityResidentRequestTime
    {
        internal const long ClockLimit=SimulationClock.MaximumTick;
        const long LastTick=ClockLimit-1;
        static double Absolute(long tick,double offset)=>offset+tick*SimulationClock.StepSeconds;
        static double DayIndex(long tick,double offset,float daySeconds)=>Math.Floor(Absolute(tick,offset)/daySeconds);
        internal static float Minute(long tick,double offset,float daySeconds)
        {float elapsed=(float)(Absolute(tick,offset)%daySeconds);return elapsed/daySeconds*1440;}
        static bool Allowed(long tick,float retry,double visit)
        {double clock=tick*SimulationClock.StepSeconds;return !(clock<retry||clock<visit);}
        static long FirstGuard(long first,long last,float retry,double visit)
        {
            if(first>last||!Allowed(last,retry,visit))return long.MaxValue;
            while(first<last){long middle=first+(last-first)/2;if(Allowed(middle,retry,visit))last=middle;else first=middle+1;}return first;
        }
        static long FirstMinute(long first,long last,double offset,float daySeconds,double threshold)
        {
            if(first>last||!(Minute(last,offset,daySeconds)>=threshold))return long.MaxValue;
            while(first<last){long middle=first+(last-first)/2;if(Minute(middle,offset,daySeconds)>=threshold)last=middle;else first=middle+1;}return first;
        }
        static ResidentTimeCheck Window(long minimum,double offset,float daySeconds,out long last)
        {
            if(minimum<0||minimum>ClockLimit)throw new ArgumentOutOfRangeException(nameof(minimum));last=LastTick;
            if(minimum==ClockLimit)return new ResidentTimeCheck(long.MaxValue,ResidentTimeCheckKind.ClockEnd);
            if(double.IsNaN(offset)||double.IsInfinity(offset)||offset<0||float.IsNaN(daySeconds)||float.IsInfinity(daySeconds)||daySeconds<1)return new ResidentTimeCheck(minimum,ResidentTimeCheckKind.Conservative);
            double absolute=Absolute(minimum,offset),day=Math.Floor(absolute/daySeconds),remainder=absolute%daySeconds;
            // Division may round up before modulo wraps at extreme dates. Do not
            // assume minute monotonicity if its day label/remainder disagree.
            if(day<0||day>int.MaxValue-1||absolute-day*daySeconds!=remainder)return new ResidentTimeCheck(minimum,ResidentTimeCheckKind.Conservative);
            if(DayIndex(LastTick,offset,daySeconds)==day)return new ResidentTimeCheck(long.MaxValue,ResidentTimeCheckKind.ClockEnd);
            long low=minimum+1,high=LastTick;
            while(low<high){long middle=low+(high-low)/2;if(DayIndex(middle,offset,daySeconds)>day)high=middle;else low=middle+1;}
            last=low-1;return new ResidentTimeCheck(low,ResidentTimeCheckKind.Calendar);
        }
        // minimum is the next not-yet-visited tick. After a request use tick+1,
        // even when float(clock+.25) rounds down to the current clock.
        internal static ResidentTimeCheck AfterGuards(long minimum,double offset,float daySeconds,float retry,double visit)
        {
            var edge=Window(minimum,offset,daySeconds,out long last);if(edge.Kind==ResidentTimeCheckKind.Conservative||minimum==ClockLimit)return edge;
            long tick=FirstGuard(minimum,last,retry,visit);return tick==long.MaxValue?edge:new ResidentTimeCheck(tick,ResidentTimeCheckKind.Predicate);
        }
        internal static ResidentTimeCheck Departure(long minimum,double offset,float daySeconds,float retry,double visit,float shift,float duration,double effectiveEstimate)
        {
            var edge=Window(minimum,offset,daySeconds,out long last);if(edge.Kind==ResidentTimeCheckKind.Conservative||minimum==ClockLimit)return edge;
            if(double.IsInfinity(effectiveEstimate))return edge;
            long tick=FirstGuard(minimum,last,retry,visit);if(tick==long.MaxValue)return edge;
            tick=FirstMinute(tick,last,offset,daySeconds,Math.Max(0,shift-effectiveEstimate));float end=shift+duration;
            return tick!=long.MaxValue&&Minute(tick,offset,daySeconds)<end?new ResidentTimeCheck(tick,ResidentTimeCheckKind.Predicate):edge;
        }
        // For healthy/funded workers still at the same employer on the arrival
        // day. Other return reasons use AfterGuards; business eligibility stays outside.
        internal static ResidentTimeCheck ShiftEnd(long minimum,double offset,float daySeconds,float retry,double visit,float shift,float duration)
        {
            var edge=Window(minimum,offset,daySeconds,out long last);if(edge.Kind==ResidentTimeCheckKind.Conservative||minimum==ClockLimit)return edge;
            long tick=FirstGuard(minimum,last,retry,visit);if(tick==long.MaxValue)return edge;
            float end=shift+duration;tick=FirstMinute(tick,last,offset,daySeconds,end);
            return tick==long.MaxValue?edge:new ResidentTimeCheck(tick,ResidentTimeCheckKind.Predicate);
        }
    }
    [Flags] internal enum ResidentPlanChange { Day=1, Calendar=2, Topology=4, Access=8, Availability=16, Untracked=32 }
    internal readonly struct ResidentPlanInvalidation
    {
        internal readonly bool All;internal readonly int Endpoint;internal readonly ResidentPlanChange Reason;
        internal ResidentPlanInvalidation(bool all,int endpoint,ResidentPlanChange reason){All=all;Endpoint=endpoint;Reason=reason;}
    }
    // Original citizens, household membership and job assignments remain the facts.
    // This component schedules/settles their real RoadCore vehicles, never old Move.
    public sealed class CityCoreResidents
    {

        readonly CityModel city;readonly World transport;
        readonly double calendarOffset;
#if CITY_RESIDENT_GUARD_SCHEDULE
        readonly CityResidentGuardSchedule guardSchedule;
#endif
        Action<ResidentPlanInvalidation> plansInvalidated;
        int planDay,planNetwork;float planDaySeconds;long planAccess,planAvailability,coveredAccess,coveredAvailability;
        bool untrackedAccess,untrackedAvailability;
        readonly Dictionary<int,ResidentPlanChange> pendingPlanEndpoints=new Dictionary<int,ResidentPlanChange>();
        readonly List<int> planEndpoints=new List<int>();
        internal bool PlansObserved=>plansInvalidated!=null;
        internal event Action<ResidentPlanInvalidation> PlansInvalidated
        {
            add{if(value==null)return;if(plansInvalidated==null){SeedPlanVersions();transport.AccessRegistered+=AccessRegistered;transport.EndpointAvailabilityChanged+=AvailabilityChanged;}plansInvalidated+=value;}
            remove{plansInvalidated-=value;if(plansInvalidated==null){transport.AccessRegistered-=AccessRegistered;transport.EndpointAvailabilityChanged-=AvailabilityChanged;pendingPlanEndpoints.Clear();planEndpoints.Clear();}}
        }
        void SeedPlanVersions()
        {
            planDay=city.day;planDaySeconds=city.society.settings.secondsPerDay;planNetwork=transport.NetworkGeneration;
            planAccess=coveredAccess=transport.AccessRevision;planAvailability=coveredAvailability=transport.EndpointAvailabilityRevision;
            untrackedAccess=untrackedAvailability=false;pendingPlanEndpoints.Clear();planEndpoints.Clear();
        }
        void AccessRegistered(int endpoint,long revision)
        {
            // A batch shares one revision across all its endpoint notices.
            if(revision!=coveredAccess&&revision!=coveredAccess+1)untrackedAccess=true;
            coveredAccess=revision;RecordPlanEndpoint(endpoint,ResidentPlanChange.Access);
        }
        void AvailabilityChanged(int endpoint,long revision)
        {
            if(revision!=coveredAvailability+1)untrackedAvailability=true;
            coveredAvailability=revision;RecordPlanEndpoint(endpoint,ResidentPlanChange.Availability);
        }
        void RecordPlanEndpoint(int endpoint,ResidentPlanChange reason)
        {pendingPlanEndpoints.TryGetValue(endpoint,out var previous);pendingPlanEndpoints[endpoint]=previous|reason;}
        internal void FlushPlanInvalidations()
        {
            var notify=plansInvalidated;if(notify==null)return;
            ResidentPlanChange global=0;
            if(planDay!=city.day)global|=ResidentPlanChange.Day;
            if(!planDaySeconds.Equals(city.society.settings.secondsPerDay))global|=ResidentPlanChange.Calendar;
            if(planNetwork!=transport.NetworkGeneration)global|=ResidentPlanChange.Topology;
            if(untrackedAccess||coveredAccess!=transport.AccessRevision)global|=ResidentPlanChange.Access|ResidentPlanChange.Untracked;
            if(untrackedAvailability||coveredAvailability!=transport.EndpointAvailabilityRevision)global|=ResidentPlanChange.Availability|ResidentPlanChange.Untracked;
            if(global!=0){if(planAccess!=transport.AccessRevision)global|=ResidentPlanChange.Access;if(planAvailability!=transport.EndpointAvailabilityRevision)global|=ResidentPlanChange.Availability;SeedPlanVersions();notify(new ResidentPlanInvalidation(true,-1,global));return;}
            // Versions are sampled only at safe request boundaries. Vehicle motion
            // and EditEpoch are deliberately not plan-invalidating signals.
            planAccess=transport.AccessRevision;planAvailability=transport.EndpointAvailabilityRevision;
            if(pendingPlanEndpoints.Count==0)return;
            planEndpoints.Clear();foreach(int endpoint in pendingPlanEndpoints.Keys)planEndpoints.Add(endpoint);planEndpoints.Sort();
            // Listeners only mark cached dependencies dirty; no synchronous edits.
            foreach(int endpoint in planEndpoints)notify(new ResidentPlanInvalidation(false,endpoint,pendingPlanEndpoints[endpoint]));
            pendingPlanEndpoints.Clear();planEndpoints.Clear();
        }

        readonly CityEntities entities;
        // One derived free-flow scalar per actually queried resident, not a route or
        // a second actual commute observation. Independent of the bounded plan LRU.
        struct CommuteEstimate {internal int home,work,network;internal long access;internal double seconds;}
        readonly Dictionary<int,CommuteEstimate> estimates=new Dictionary<int,CommuteEstimate>();
        readonly List<int> retiredEstimates=new List<int>();int estimateEntityRevision=-1;
        internal int CachedCommuteEstimates=>estimates.Count;
        void PruneEstimates()
        {
            if(estimateEntityRevision==entities.Revision)return;
            retiredEstimates.Clear();foreach(var pair in estimates)if(entities.Member(pair.Key)==null)retiredEstimates.Add(pair.Key);
            foreach(int id in retiredEstimates)estimates.Remove(id);estimateEntityRevision=entities.Revision;
        }
        double EstimateCommute(int citizen,int home,int work)
        {
#if CITY_CORE_ATTRIBUTION
            using var lookupObservation=CityCoreProfile.Measure(CityCoreProfile.Part.CommuteLookup);
#endif
            if(!transport.HasResidentAccess(home,work))return double.PositiveInfinity;
            bool found=estimates.TryGetValue(citizen,out var value);
            if(found&&value.home==home&&value.work==work&&value.network==transport.NetworkGeneration&&value.access==transport.AccessRevision)
            {
#if CITY_CORE_ATTRIBUTION
                if(CityCoreProfile.Enabled)CityCoreProfile.CommuteEstimateHits++;
                CityCoreProfile.ObserveQuery(CityCoreProfile.QueryKind.CommuteEstimate,CityCoreProfile.QueryResult.Hit,citizen,home,work);
#endif
                return value.seconds;
            }
#if CITY_CORE_ATTRIBUTION
            using var estimateObservation=CityCoreProfile.Measure(CityCoreProfile.Part.CommuteEstimate);
            if(CityCoreProfile.Enabled)CityCoreProfile.CommuteEstimateMisses++;
            var reason=!found?CityCoreProfile.QueryResult.Absent:value.home!=home||value.work!=work?CityCoreProfile.QueryResult.EndpointsChanged:value.network!=transport.NetworkGeneration?CityCoreProfile.QueryResult.NetworkChanged:CityCoreProfile.QueryResult.AccessChanged;
            CityCoreProfile.ObserveQuery(CityCoreProfile.QueryKind.CommuteEstimate,reason,citizen,home,work);
#endif
            double seconds=transport.EstimateResidentSeconds(home,work);
            estimates[citizen]=new CommuteEstimate{home=home,work=work,network=transport.NetworkGeneration,access=transport.AccessRevision,seconds=seconds};return seconds;
        }
        internal Func<int,int,int,bool,Vehicle> RequestCommute;
        public bool Enabled {get;}
        internal CityCoreConsumption Consumption;
        public int CarriedGoods(int citizen)=>entities.Member(citizen)?.Person.carriedGoods??0;
        public CityCoreResidents(CityModel city,World transport,CityEntities entities,bool enabled=true,double calendarOffset=50)
        {
            this.city=city;this.transport=transport;this.entities=entities;this.calendarOffset=calendarOffset;Enabled=enabled;
#if CITY_RESIDENT_GUARD_SCHEDULE
            guardSchedule=new CityResidentGuardSchedule(entities,transport);
#endif

        }
        public bool Depart(TripTask v)
        {
            var member=entities.Member(v.citizenId);if(member==null)return false;var p=member.Person;var h=member.Family;
            if(!h.resident||p.dead||p.tripId!=0||p.location!=v.origin||v.Price!=0)return false;
            if(v.purpose!=TransportPurpose.Home&&v.Cargo!=0)return false;
            if(v.purpose==TransportPurpose.Work&&(p.atWork||v.origin!=h.home||v.destination!=city.Workplace(p)||!city.CanAttendWork(p)||!city.JobFunded(city.ResidentJob(p))))return false;
            if(v.purpose==TransportPurpose.Home&&(v.destination!=h.home||v.Cargo!=p.carriedGoods))return false;
            if(v.purpose==TransportPurpose.Shopping&&(Consumption==null||!Consumption.CanDepart(v,member)))return false;
            if(v.purpose!=TransportPurpose.Home&&v.purpose!=TransportPurpose.Work&&v.purpose!=TransportPurpose.Shopping)return false;
            p.tripId=v.id;p.location=-1;p.atWork=false;if(v.purpose==TransportPurpose.Work)p.departureDay=city.day;if(v.purpose==TransportPurpose.Home){p.carriedGoods=0;p.visitUntil=0;}if(v.purpose==TransportPurpose.Shopping)h.shoppingDay=city.day;return true;
        }
        public bool Arrive(TransportArrival v)
        {
            var member=entities.Member(v.citizenId);if(member==null)return false;var p=member.Person;var h=member.Family;
            if(p.dead||!h.resident||p.tripId!=v.taskId)return false;
            if(v.purpose==TransportPurpose.Home&&v.destination!=h.home)return false;
            p.tripId=0;p.location=v.destination;p.atWork=v.purpose==TransportPurpose.Work&&city.Workplace(p)==v.destination&&city.CanAttendWork(p)&&city.JobFunded(city.ResidentJob(p));
            if(v.purpose==TransportPurpose.Shopping)Consumption.BeginVisit(v,member);
            if(v.purpose==TransportPurpose.Home)Consumption.ConsumeAtHome(v.cargo);
            if(p.atWork){double seconds=city.society.settings.secondsPerDay,absolute=calendarOffset+v.arrivedAt;p.arrivedDay=1+(int)Math.Floor(absolute/seconds);p.lastCommute=(float)Math.Max(0,(v.arrivedAt-v.departedAt)*1440/seconds);p.lastDelay=(float)Math.Max(0,(p.arrivedDay-p.departureDay)*1440+absolute%seconds*1440/seconds-entities.Employment.ShiftStart(p.id,city.development.enabled&&city.GetBuilding(v.destination) is CommercialBuilding));p.observedHome=h.home;p.observedWork=v.destination;p.observedRevision=transport.NetworkGeneration;}
            return true;
        }
        public void AdvanceRequests()
        {
#if CITY_CORE_ATTRIBUTION
            using var observation=HarborCity.RoadCore.CityCoreProfile.Measure(HarborCity.RoadCore.CityCoreProfile.Part.Residents);
#endif

            if(!Enabled)return;PruneEstimates();if(PlansObserved)FlushPlanInvalidations();if(entities.FundingObserved)entities.FlushFundingChanges();
            float minute=city.ResidentMinute;
#if CITY_RESIDENT_GUARD_SCHEDULE
            foreach(var member in guardSchedule.Begin())
#else
            foreach(var member in entities.Members)
#endif
            {
                var p=member.Person;var h=member.Family;if(!h.resident||p.dead||p.tripId!=0||transport.Clock<p.retryAt||transport.Clock<p.visitUntil)continue;
                // At home these conditions cannot produce a return or departure.
                // Check them before reading job/shift data; every pass still observes
                // current health, assignment and employer funds in member order.
                bool atHome=p.location==h.home;
                if(atHome&&(p.atWork||p.departureDay==city.day||!city.CanAttendWork(p)))continue;
                var job=city.ResidentJob(p);int workplace=job?.buildingId??-1;
                bool eligible=atHome&&city.JobFunded(job);
                if(atHome&&!eligible)continue;
                float shift=entities.Employment.ShiftStart(p.id,city.development.enabled&&city.GetBuilding(workplace) is CommercialBuilding),end=shift+entities.Employment.ShiftMinutes;
                bool returning=!atHome&&(!p.atWork||p.sick||workplace!=p.location||p.arrivedDay<city.day||minute>=end||!city.JobFunded(job));
                double estimate=eligible?EstimateCommute(p.id,h.home,workplace)*1440/city.society.settings.secondsPerDay:double.PositiveInfinity;
                if(p.observedHome==h.home&&p.observedWork==workplace&&p.observedRevision==transport.NetworkGeneration)estimate=Math.Max(estimate,p.lastCommute);
                bool leaving=eligible&&!double.IsInfinity(estimate)&&minute>=Math.Max(0,shift-estimate)&&minute<end;
                if(!returning&&!leaving)continue;p.retryAt=(float)(transport.Clock+.25);RequestCommute(p.id,returning?p.location:h.home,returning?h.home:workplace,returning);if(entities.FundingObserved)entities.FlushFundingChanges();if(PlansObserved)FlushPlanInvalidations();
            }
        }
        public ResidentActivity Observe(int citizen)
        {
            var member=entities.Member(citizen);if(member==null)throw new ArgumentException("Unknown citizen");var p=member.Person;var a=new ResidentActivity();
            if(p.tripId!=0){var task=transport.FindTaskVehicle(p.tripId);if(task==null)throw new InvalidOperationException("Resident transport reference missing");var point=transport.Position(task);a.located=task.status!=VehicleStatus.AwaitingRestart;a.travelling=true;a.x=(float)point.x;a.z=(float)point.z;a.destination=task.Task.destination;a.state=task.status==VehicleStatus.AwaitingRestart?"道路拆除，等待从原出发点重新出发":task.status==VehicleStatus.Stranded?"道路中断，仍在车辆中":task.Task.purpose==TransportPurpose.Home?"回家途中":"上班途中";a.reason="实际车辆 #"+task.id;return a;}
            var building=city.GetBuilding(p.location);a.building=p.location;a.located=building!=null;a.state=p.atWork?"实际在岗":"在家 / 等待出发";if(building!=null){a.x=building.x;a.z=building.z;}return a;
        }
        public void Validate()
        {
            var owned=new HashSet<int>();foreach(var task in transport.Vehicles)if(World.IsResident(task.Task.purpose)){var member=entities.Member(task.Task.citizenId);if(member==null||!owned.Add(task.Task.citizenId)||member.Person.tripId!=task.Task.id||member.Person.atWork||member.Person.location!=-1)throw new InvalidOperationException("Resident/vehicle ownership mismatch");}
            foreach(var member in entities.Members){var p=member.Person;if(p.tripId!=0){var task=transport.FindTaskVehicle(p.tripId);if(task==null||task.Task.citizenId!=p.id)throw new InvalidOperationException("Dangling resident trip");}}
        }
    }
}
