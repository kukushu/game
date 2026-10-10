using System;
using System.Linq;

namespace HarborCity
{
    [Serializable] public sealed class CityPoliceState
    {
        public int lastCheckDay;
        // Provisional CS1 approximation: building crime, not CS2 individual criminals.
        public float dailyBase=1,unemploymentWeight=2,servicePressureWeight=2,responsePerMinute=2;
    }
    [Serializable] public sealed class PoliceStationBuilding : CityBuilding
    {
        public override LandUse Use=>LandUse.PoliceStation;
        public const int Cars=2;
    }
    public sealed partial class CityModel
    {
        public CityPoliceState police=new CityPoliceState();
        public const float CrimeWarning=25;
        public int CrimeBuildings=>buildings.Count(b=>!b.burned && b.crime>=CrimeWarning);
        public float AverageCrime=>buildings.Where(b=>CityZoningState.ZoneUse(b.Use) && !b.abandoned).Select(b=>b.crime).DefaultIfEmpty(0).Average();
        public int PoliceFleetLimit=>Math.Max(1,ServiceCapacity(CityServiceKind.Police,PoliceStationBuilding.Cars));
        public int PoliceCarsAt(int id)=>traffic.trips.Count(t=>t.purpose==TripPurpose.PoliceResponse && t.home==id);
        void UpdateCrime()
        {
            if(police.lastCheckDay==day)return;police.lastCheckDay=day;
            foreach(var b in buildings.Where(b=>CityZoningState.ZoneUse(b.Use) && !b.abandoned && !b.burning))
            {
                var people=Citizens.Where(p=>b is ResidentialBuilding?society.families.Any(h=>h.id==p.householdId && h.home==b.id):Workplace(p)==b.id).ToList();
                if(people.Count==0)continue;
                int workforce=people.Count(p=>p.canWork);
                float unemployed=workforce==0?0:people.Count(p=>p.canWork && ResidentJob(p)==null)/(float)workforce;
                // Until complete happiness exists, actual unmet basic services are
                // a named proxy, not the old independent happiness cache.
                float pressure=Supply(b.id).Complete?0:1;
                if(waste.enabled)pressure=Math.Max(pressure,Math.Clamp(b.garbage/(float)GarbageWarning,0,1));
                b.crime=Math.Min(100,b.crime+police.dailyBase+police.unemploymentWeight*unemployed+police.servicePressureWeight*pressure);
            }
        }
        bool ValidPolice()
        {
            if(police==null || police.lastCheckDay<0 || police.lastCheckDay>day)return false;
            foreach(float rate in new[]{police.dailyBase,police.unemploymentWeight,police.servicePressureWeight,police.responsePerMinute})
                if(float.IsNaN(rate) || float.IsInfinity(rate) || rate<0 || rate>100)return false;
            foreach(var b in buildings)if(float.IsNaN(b.crime) || float.IsInfinity(b.crime) || b.crime<0 || b.crime>100 || b.crime>0 && !CityZoningState.ZoneUse(b.Use))return false;
            foreach(var station in buildings.OfType<PoliceStationBuilding>())if(PoliceCarsAt(station.id)>PoliceStationBuilding.Cars*3/2)return false;
            return true;
        }
        public void AdvancePolice(float minutes)
        {
            if(!development.enabled || minutes<=0 || float.IsNaN(minutes) || float.IsInfinity(minutes))return;
            foreach(var b in buildings.Where(b=>b.crime>0 && !b.burned && !b.burning))
            {
                int cars=traffic.trips.Count(t=>t.purpose==TripPurpose.PoliceResponse && !t.returning && t.destination==b.id && t.status==TripStatus.Visiting
                    && roads.Active(t.Current) && AccessBuilding(b.id).Contains(t.Current) && HasBasicServices(t.home));
                if(cars==0)continue;
                b.crime=Math.Max(0,b.crime-cars*minutes*police.responsePerMinute);
                if(b.crime==0)Trace("police.resolved","警车实际到场处理建筑治安问题",b,building:b.id);
            }
        }
    }
    public sealed partial class CityTraffic
    {
        public TrafficTrip DispatchPolice(int station,int target)
        {
            var b=city.GetBuilding(target);
            if(!city.development.enabled || !(city.GetBuilding(station) is PoliceStationBuilding) || !city.HasBasicServices(station) || b==null || b.crime<CityModel.CrimeWarning || b.burned || b.burning
                || city.PoliceCarsAt(station)>=city.PoliceFleetLimit || State.trips.Count>=TaskCapacity || State.nextId==int.MaxValue
                || State.trips.Any(t=>t.purpose==TripPurpose.PoliceResponse && !t.returning && t.destination==target))return null;
            var route=FindRoute(station,target);if(route.Count==0 || !CanEnter(route,null))return null;
            var trip=new TrafficTrip{id=State.nextId++,origin=station,home=station,destination=target,purpose=TripPurpose.PoliceResponse,route=route,departedAt=State.clock};
            State.trips.Add(trip);city.Trace("police.dispatched","有限警车实际出发，途中不消除犯罪积压",trip,building:target,trip:trip.id);return trip;
        }
        void SchedulePolice()
        {
            foreach(var target in city.buildings.Where(b=>b.crime>=CityModel.CrimeWarning && !b.burned && !b.burning).OrderByDescending(b=>b.crime))
                foreach(var station in city.buildings.OfType<PoliceStationBuilding>().OrderBy(b=>city.TravelMinutes(b.id,target.id)).ThenBy(b=>b.id))if(DispatchPolice(station.id,target.id)!=null)break;
        }
        bool HoldPoliceCar(TrafficTrip t)
        {
            if(t.purpose!=TripPurpose.PoliceResponse || t.returning || t.status!=TripStatus.Visiting)return false;
            var b=city.GetBuilding(t.destination);
            if(b!=null && b.crime>0 && !b.burning && !b.burned)return HoldAtServiceTarget(t);
            RedirectFromCurrent(t,t.home);return false;
        }
        void ArrivePolice(TrafficTrip t)
        {
            if(t.returning){State.completed++;State.trips.Remove(t);return;}
            var b=city.GetBuilding(t.destination);
            if(b==null || b.crime<=0 || b.burning || b.burned){RedirectFromCurrent(t,t.home);return;}
            t.status=TripStatus.Visiting;t.delay=t.blocked=0;city.Trace("police.arrived","警车实际到达目标建筑",t,building:t.destination,trip:t.id);
        }
    }
}
