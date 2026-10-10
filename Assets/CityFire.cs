using System;
using System.Linq;

namespace HarborCity
{
    [Serializable] public sealed class CityFireState
    {
        public int seed=9173,lastCheckDay;
        // Provisional rates. The original manual establishes fire response and
        // burned buildings requiring bulldozing, not the internal probabilities.
        public float chancePerBuildingDay=.001f,damagePerMinute=.08f,suppressionPerMinute=.025f;
    }
    [Serializable] public sealed class FireHouseBuilding : CityBuilding
    {
        public override LandUse Use=>LandUse.FireHouse;
        public const int Engines=2;
    }
    public sealed partial class CityModel
    {
        public CityFireState fire=new CityFireState();
        public int ActiveFires=>buildings.Count(b=>b.burning);
        public int BurnedBuildings=>buildings.Count(b=>b.burned);
        public int FireFleetLimit=>Math.Max(1,ServiceCapacity(CityServiceKind.Fire,FireHouseBuilding.Engines));
        public int FireEnginesAt(int id)=>traffic.trips.Count(t=>t.purpose==TripPurpose.FireResponse && t.home==id);
        public bool IgniteBuilding(int id)
        {
            var b=GetBuilding(id);if(!development.enabled || b==null || !CityZoningState.ZoneUse(b.Use) || b.burned || b.burning || b.abandoned)return false;
            b.burning=true;b.fireIntensity=1;Trace("fire.started","建筑发生火灾；需要实际消防响应",b,building:id,level:"warning");return true;
        }
        void GenerateFires()
        {
            if(fire.lastCheckDay==day)return;fire.lastCheckDay=day;
            foreach(var b in buildings.Where(b=>CityZoningState.ZoneUse(b.Use) && !b.abandoned && !b.burned && !b.burning).OrderBy(b=>b.id))
            {fire.seed=(int)(((long)fire.seed*48271)%2147483647);if(fire.seed/2147483647.0<fire.chancePerBuildingDay)IgniteBuilding(b.id);}
        }
        bool ValidFire()
        {
            if(fire==null || fire.seed<=0 || fire.seed>=2147483647 || fire.lastCheckDay<0 || fire.lastCheckDay>day)return false;
            foreach(float rate in new[]{fire.chancePerBuildingDay,fire.damagePerMinute,fire.suppressionPerMinute})if(float.IsNaN(rate) || float.IsInfinity(rate) || rate<0 || rate>1)return false;
            foreach(var b in buildings)
                if(float.IsNaN(b.fireDamage) || float.IsInfinity(b.fireDamage) || b.fireDamage<0 || b.fireDamage>100 || float.IsNaN(b.fireIntensity) || float.IsInfinity(b.fireIntensity) || b.fireIntensity<0 || b.fireIntensity>4
                    || b.burning!=(b.fireIntensity>0) || b.burning && b.abandoned || b.fireDamage==100 && !b.burned || b.burned && (b.burning || !b.abandoned || b.fireDamage!=100) || (b.burning || b.burned || b.fireDamage>0) && !CityZoningState.ZoneUse(b.Use))return false;
            foreach(var station in buildings.OfType<FireHouseBuilding>())if(FireEnginesAt(station.id)>FireHouseBuilding.Engines*3/2)return false;
            return true;
        }
        void BurnBuilding(CityBuilding b)
        {
            b.fireDamage=100;b.fireIntensity=0;b.burning=false;b.burned=true;b.abandoned=true;b.abandonedDay=day;
            new CityTraffic(this).RemoveBuilding(b.id);
            foreach(var h in society.families.Where(h=>h.resident && h.home==b.id))
            {
                h.resident=false;h.home=h.unit=-1;h.reason="住宅烧毁，等待重新迁入";
                foreach(var p in h.people.Where(p=>!p.dead)){ReleaseJob(p);p.tripId=0;p.location=-1;p.atWork=false;p.accessNode=-1;p.medicalStage=MedicalStage.None;p.medicalClinicId=-1;p.treatmentMinutes=0;ReleaseSchoolPlace(p);p.atSchool=p.schoolReturning=false;}
                Trace("household.displaced",h.reason,h,household:h.id,building:b.id);
            }
            BuildingsChanged();SyncJobs();Recalculate();Trace("fire.burned","建筑烧毁；保留废墟实体，必须拆除后才能重新开发",b,building:b.id,level:"warning");
        }
        public void AdvanceFires(float minutes)
        {
            if(!development.enabled || minutes<=0 || float.IsNaN(minutes) || float.IsInfinity(minutes))return;
            foreach(var b in buildings.Where(b=>b.burning).ToList())
            {
                int responders=traffic.trips.Count(t=>t.purpose==TripPurpose.FireResponse && !t.returning && t.destination==b.id && t.status==TripStatus.Visiting
                    && roads.Active(t.Current) && AccessBuilding(b.id).Contains(t.Current) && HasBasicServices(t.home));
                b.fireDamage=Math.Min(100,b.fireDamage+minutes*fire.damagePerMinute*b.fireIntensity);
                if(b.fireDamage>=100){BurnBuilding(b);continue;}
                b.fireIntensity=Math.Clamp(b.fireIntensity+minutes*(.003f-responders*fire.suppressionPerMinute),0,4);
                if(b.fireIntensity==0){b.burning=false;Trace("fire.extinguished","消防车实际到场处置后扑灭火灾",b,building:b.id);}
            }
        }
    }
    public sealed partial class CityTraffic
    {
        public TrafficTrip DispatchFire(int station,int target)
        {
            var b=city.GetBuilding(target);
            if(!city.development.enabled || !(city.GetBuilding(station) is FireHouseBuilding) || !city.HasBasicServices(station) || b==null || !b.burning || city.FireEnginesAt(station)>=city.FireFleetLimit
                || State.trips.Count>=TaskCapacity || State.nextId==int.MaxValue || State.trips.Any(t=>t.purpose==TripPurpose.FireResponse && !t.returning && t.destination==target))return null;
            var route=FindRoute(station,target);if(route.Count==0 || !CanEnter(route,null))return null;
            var trip=new TrafficTrip{id=State.nextId++,origin=station,home=station,destination=target,purpose=TripPurpose.FireResponse,route=route,departedAt=State.clock};
            State.trips.Add(trip);city.Trace("fire.dispatched","有限消防车从设施实际出发，途中不计灭火能力",trip,building:target,trip:trip.id);return trip;
        }
        void ScheduleFire()
        {
            foreach(var target in city.buildings.Where(b=>b.burning))
                foreach(var station in city.buildings.OfType<FireHouseBuilding>().OrderBy(b=>city.TravelMinutes(b.id,target.id)).ThenBy(b=>b.id))if(DispatchFire(station.id,target.id)!=null)break;
        }
        bool HoldFireEngine(TrafficTrip t)
        {
            if(t.purpose!=TripPurpose.FireResponse || t.returning || t.status!=TripStatus.Visiting)return false;
            if(city.GetBuilding(t.destination)?.burning==true)return HoldAtServiceTarget(t);
            RedirectFromCurrent(t,t.home);city.Trace("fire.returning","任务结束，消防车从实际火场位置返回",t,trip:t.id);return false;
        }
        void ArriveFire(TrafficTrip t)
        {
            if(t.returning){State.completed++;State.trips.Remove(t);return;}
            if(city.GetBuilding(t.destination)?.burning!=true){RedirectFromCurrent(t,t.home);return;}
            t.status=TripStatus.Visiting;t.delay=0;t.blocked=0;city.Trace("fire.arrived","消防车实际到场，开始处置",t,building:t.destination,trip:t.id);
        }
    }
}
