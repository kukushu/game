using System;
using System.Linq;
using System.Collections.Generic;

namespace HarborCity
{
    public enum CorpseStage { Waiting,Assigned,InTransit,Buried }
    [Serializable] public sealed class CityCorpse
    {
        public int citizenId,buildingId=-1,cemeteryId=-1,tripId,diedDay;
        public float x,z;
        public CorpseStage stage;
    }
    [Serializable] public sealed class CityDeathcareState
    {
        public int seed=31337,lastCheckDay,uncollectedDays=3;
        public float oldAgeRisk=.02f,severeIllnessRisk=.005f;
        public List<CityCorpse> bodies=new List<CityCorpse>();
    }
    [Serializable] public sealed class CemeteryBuilding : CityBuilding
    {
        public override LandUse Use=>LandUse.Cemetery;
        // Scaled provisional catalogue; exact original values pending comparison.
        public const int Capacity=48,Hearses=2;
    }
    public sealed partial class CityModel
    {
        public CityDeathcareState deathcare=new CityDeathcareState();
        public int LivingMembers(Household h)=>h.people.Count(p=>!p.dead);
        public IEnumerable<CityResident> AllResidents=>society.families.SelectMany(h=>h.people);
        public CityCorpse Corpse(int citizen)=>deathcare.bodies.Find(b=>b.citizenId==citizen);
        public int BodiesAt(int building)=>deathcare.bodies.Count(b=>b.buildingId==building && (b.stage==CorpseStage.Waiting || b.stage==CorpseStage.Assigned));
        public bool CorpseBacklog(int building)=>deathcare.bodies.Any(b=>b.buildingId==building && (b.stage==CorpseStage.Waiting || b.stage==CorpseStage.Assigned) && day-b.diedDay>=deathcare.uncollectedDays);
        public int BuriedAt(int cemetery)=>deathcare.bodies.Count(b=>b.cemeteryId==cemetery && b.stage==CorpseStage.Buried);
        public int CemeteryReserved(int cemetery)=>deathcare.bodies.Count(b=>b.cemeteryId==cemetery && b.stage!=CorpseStage.Waiting);
        public int HearseFleetLimit=>Math.Max(1,ServiceCapacity(CityServiceKind.Healthcare,CemeteryBuilding.Hearses));
        public int HearsesAt(int cemetery)=>traffic.trips.Count(t=>t.purpose==TripPurpose.Deathcare && t.home==cemetery);
        public ResidentActivity ObserveCorpse(CityResident p)
        {
            var body=Corpse(p.id);var a=new ResidentActivity{state="已死亡",reason="历史居民身份保留；不计入人口或就业"};if(body==null)return a;
            int place=body.stage==CorpseStage.Buried?body.cemeteryId:body.buildingId;
            var b=GetBuilding(place);a.building=place;a.located=b!=null;a.x=b?.x??body.x;a.z=b?.z??body.z;
            a.state=body.stage==CorpseStage.Buried?"已安葬":body.stage==CorpseStage.InTransit?"灵车运送中":"遗体等待收取";
            a.destination=body.cemeteryId;a.reason+="；死亡日期 第 "+body.diedDay+" 天";
            var trip=traffic.trips.Find(t=>t.id==body.tripId);
            if(body.stage==CorpseStage.InTransit && trip!=null)
            {var from=roads.Node(trip.Current);var to=roads.Node(trip.Next);a.located=from!=null && to!=null;a.travelling=true;a.building=-1;
                if(a.located){a.x=from.x+(to.x-from.x)*trip.progress;a.z=from.z+(to.z-from.z)*trip.progress;}}
            return a;
        }
        public bool DieResident(int citizen)
        {
            var p=Citizens.FirstOrDefault(r=>r.id==citizen);if(!development.enabled || p==null || p.tripId!=0)return false;
            var h=society.families.First(f=>f.id==p.householdId);
            int location=p.atWork || p.atSchool || p.schoolReturning || p.medicalStage!=MedicalStage.None?p.location:h.home;
            var building=GetBuilding(location);if(building==null)return false;
            var body=new CityCorpse{citizenId=p.id,buildingId=location,x=building.x,z=building.z,diedDay=day};
            ReleaseJob(p);ReleaseSchoolPlace(p);p.dead=true;p.canWork=false;p.health=0;p.sick=false;
            p.atWork=p.atSchool=p.schoolReturning=false;p.medicalStage=MedicalStage.None;p.medicalClinicId=-1;p.treatmentMinutes=0;p.location=location;
            deathcare.bodies.Add(body);
            if(LivingMembers(h)==0){h.resident=false;h.home=h.unit=-1;h.reason="家庭已无存活成员；身份与遗体记录保留";}
            Trace("citizen.died","建筑内居民死亡；释放岗位、学位和治疗位置，保留已赚工资与真实遗体",body,household:h.id,citizen:p.id,building:location);
            Recalculate();return true;
        }
        public void UpdateMortality()
        {
            if(!development.enabled || deathcare.lastCheckDay==day)return;deathcare.lastCheckDay=day;
            foreach(var p in Citizens.ToArray())
            {
                // Moving deaths require a verified roadside/carrier workflow; not teleported home.
                if(p.tripId!=0)continue;
                float risk=p.age>=80?Math.Min(1,(p.age-79)*deathcare.oldAgeRisk):0;
                if(p.sick && p.health<=10)risk=Math.Max(risk,(11-p.health)*deathcare.severeIllnessRisk);
                if(risk<=0)continue;
                deathcare.seed=(int)(((long)deathcare.seed*48271)%2147483647);
                if(deathcare.seed/2147483647f<risk)DieResident(p.id);
            }
        }
        bool ValidDeathcare()
        {
            if(deathcare==null || deathcare.bodies==null || deathcare.bodies.Count>100000 || deathcare.seed<=0 || deathcare.seed>=2147483647 || deathcare.lastCheckDay<0 || deathcare.lastCheckDay>day || deathcare.uncollectedDays<1 || deathcare.uncollectedDays>3650)return false;
            foreach(float risk in new[]{deathcare.oldAgeRisk,deathcare.severeIllnessRisk})if(float.IsNaN(risk) || float.IsInfinity(risk) || risk<0 || risk>1)return false;
            var people=AllResidents.ToDictionary(p=>p.id);var ids=new HashSet<int>();
            foreach(var b in deathcare.bodies)
            {
                if(b==null || !ids.Add(b.citizenId) || !people.TryGetValue(b.citizenId,out var p) || !p.dead || (int)b.stage<0 || (int)b.stage>3 || b.diedDay<1 || b.diedDay>day || !FinitePosition(b.x) || !FinitePosition(b.z))return false;
                if(b.stage==CorpseStage.Waiting || b.stage==CorpseStage.Assigned){if(GetBuilding(b.buildingId)==null)return false;}
                else if(b.buildingId!=-1)return false;
                if(b.stage==CorpseStage.Waiting){if(b.cemeteryId!=-1 || b.tripId!=0)return false;}
                else if(!(GetBuilding(b.cemeteryId) is CemeteryBuilding))return false;
                if(b.stage==CorpseStage.Assigned || b.stage==CorpseStage.InTransit)
                {var t=traffic.trips.Find(r=>r.id==b.tripId);if(t==null || t.purpose!=TripPurpose.Deathcare || t.corpseId!=b.citizenId || t.home!=b.cemeteryId || t.returning!=(b.stage==CorpseStage.InTransit))return false;}
                else if(b.tripId!=0)return false;
            }
            foreach(var p in people.Values)
            {
                if(p.dead!=ids.Contains(p.id))return false;
                if(p.dead && (p.canWork || p.jobId>=0 || p.tripId!=0 || p.schoolId>=0 || p.atWork || p.atSchool || p.schoolReturning || p.medicalStage!=MedicalStage.None))return false;
            }
            foreach(var c in buildings.OfType<CemeteryBuilding>())if(CemeteryReserved(c.id)>CemeteryBuilding.Capacity || HearsesAt(c.id)>CemeteryBuilding.Hearses*3/2)return false;
            foreach(var t in traffic.trips.Where(t=>t.purpose==TripPurpose.Deathcare))if(Corpse(t.corpseId)?.tripId!=t.id)return false;
            return true;
        }
    }
    public sealed partial class CityTraffic
    {
        public TrafficTrip DispatchHearse(int cemetery,int citizen)
        {
            var body=city.Corpse(citizen);
            if(!city.development.enabled || !(city.GetBuilding(cemetery) is CemeteryBuilding) || !city.HasBasicServices(cemetery) || body==null || body.stage!=CorpseStage.Waiting || city.CemeteryReserved(cemetery)>=CemeteryBuilding.Capacity || city.HearsesAt(cemetery)>=city.HearseFleetLimit || State.trips.Count>=TaskCapacity || State.nextId==int.MaxValue)return null;
            var route=FindRoute(cemetery,body.buildingId);if(route.Count==0 || !CanEnter(route,null))return null;
            var trip=new TrafficTrip{id=State.nextId++,origin=cemetery,home=cemetery,destination=body.buildingId,purpose=TripPurpose.Deathcare,corpseId=citizen,route=route,departedAt=State.clock};
            body.stage=CorpseStage.Assigned;body.cemeteryId=cemetery;body.tripId=trip.id;State.trips.Add(trip);
            city.Trace("deathcare.dispatched","有限灵车实际出发；遗体仍在原建筑，墓地预留一个位置",trip,citizen:citizen,building:body.buildingId,trip:trip.id);return trip;
        }
        void ScheduleDeathcare()
        {
            foreach(var body in city.deathcare.bodies.Where(b=>b.stage==CorpseStage.Waiting).OrderBy(b=>b.diedDay).ThenBy(b=>b.citizenId))
                foreach(var cemetery in city.buildings.OfType<CemeteryBuilding>().OrderBy(b=>city.TravelMinutes(b.id,body.buildingId)).ThenBy(b=>b.id))if(DispatchHearse(cemetery.id,body.citizenId)!=null)break;
        }
        bool HoldHearse(TrafficTrip t)
        {
            if(t.purpose!=TripPurpose.Deathcare || t.status!=TripStatus.Visiting)return false;
            if(!city.HasBasicServices(t.home))return HoldAtServiceTarget(t);
            t.status=TripStatus.Driving;return false;
        }
        void ArriveHearse(TrafficTrip t)
        {
            var body=city.Corpse(t.corpseId);if(body==null)throw new InvalidOperationException("灵车缺少真实遗体");
            if(!city.HasBasicServices(t.home)){t.status=TripStatus.Visiting;return;}
            var p=city.AllResidents.First(r=>r.id==body.citizenId);
            if(!t.returning)
            {
                body.stage=CorpseStage.InTransit;body.buildingId=-1;p.location=-1;p.accessNode=t.Current;
                RedirectFromCurrent(t,t.home);city.Trace("deathcare.picked_up","灵车实际抵达并接到具体遗体；尚未送达墓地",body,citizen:p.id,trip:t.id);
            }
            else
            {
                body.stage=CorpseStage.Buried;body.tripId=0;p.location=t.home;p.accessNode=t.Current;
                State.completed++;State.trips.Remove(t);city.Trace("deathcare.buried","实际送达墓地，具体死者占用一个位置",body,citizen:p.id,building:t.home,trip:t.id);
            }
        }
    }
}
