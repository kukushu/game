using System;
using System.Collections.Generic;
using HarborCity.RoadCore;

namespace HarborCity
{
    // Original citizens, household membership and job assignments remain the facts.
    // This component schedules/settles their real RoadCore vehicles, never old Move.
    public sealed class CityCoreResidents
    {

        readonly CityModel city;readonly World transport;
        readonly double calendarOffset;
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

            if(!Enabled)return;PruneEstimates();
            float minute=city.ResidentMinute;
            foreach(var member in entities.Members)
            {
                var p=member.Person;var h=member.Family;if(!h.resident||p.dead||p.tripId!=0||transport.Clock<p.retryAt||transport.Clock<p.visitUntil)continue;float shift=entities.Employment.ShiftStart(p.id,city.development.enabled&&city.GetBuilding(city.Workplace(p)) is CommercialBuilding),end=shift+entities.Employment.ShiftMinutes;
                bool returning=p.location!=h.home&&(!p.atWork||p.sick||city.Workplace(p)!=p.location||p.arrivedDay<city.day||minute>=end||!city.JobFunded(city.ResidentJob(p)));
                bool eligible=!p.atWork&&p.location==h.home&&p.departureDay!=city.day&&city.CanAttendWork(p)&&city.JobFunded(city.ResidentJob(p));double estimate=eligible?EstimateCommute(p.id,h.home,city.Workplace(p))*1440/city.society.settings.secondsPerDay:double.PositiveInfinity;
                if(p.observedHome==h.home&&p.observedWork==city.Workplace(p)&&p.observedRevision==transport.NetworkGeneration)estimate=Math.Max(estimate,p.lastCommute);
                bool leaving=eligible&&!double.IsInfinity(estimate)&&minute>=Math.Max(0,shift-estimate)&&minute<end;
                if(!returning&&!leaving)continue;p.retryAt=(float)(transport.Clock+.25);RequestCommute(p.id,returning?p.location:h.home,returning?h.home:city.Workplace(p),returning);
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
