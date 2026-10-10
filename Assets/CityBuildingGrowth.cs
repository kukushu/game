using System;
using System.Linq;

namespace HarborCity
{
    public sealed partial class CityModel
    {
        // Qualitative dependencies follow the CS1 manual. Numeric thresholds,
        // reach, level catalogue and capacity increments remain provisional.
        public const int UpgradeQualifyingDays=3;
        public int MaximumBuildingLevel(CityBuilding b)=>development.enabled && b is ResidentialBuilding?5:3;
        public int ReachableServiceKinds(int id)
        {
            var target=GetBuilding(id);if(target==null)return 0;
            return buildings.Where(b=>b.id!=id && (b.Use==LandUse.Park || b.Use==LandUse.Landfill || b.Use==LandUse.Clinic || b is SchoolBuilding || b.Use==LandUse.FireHouse || b.Use==LandUse.PoliceStation)
                && HasBasicServices(b.id) && TravelMinutes(id,b.id)>=0 && TravelMinutes(id,b.id)<=30).Select(b=>b.Use).Distinct().Count();
        }
        public float LandValue(int id)
        {
            var b=GetBuilding(id);if(b==null)return 0;
            var environment=EnvironmentAt(b.x,b.z);float value=20+(Supply(id).Complete?15:0)+ReachableServiceKinds(id)*8;
            value-=Math.Clamp(environment.noise,0,1)*20+Math.Clamp(environment.pollution,0,1)*30+b.crime*.25f;
            if(waste.enabled)value-=Math.Clamp(b.garbage/(float)GarbageWarning,0,1)*15;
            float ruins=buildings.Where(n=>n.abandoned || n.burned).Sum(n=>Math.Max(0,1-(float)Math.Sqrt((n.x-b.x)*(n.x-b.x)+(n.z-b.z)*(n.z-b.z))/20));
            return Math.Clamp(value-Math.Min(30,ruins*15),0,100);
        }
        public float BuildingEducation(int id)
        {
            var b=GetBuilding(id);if(b==null)return 0;
            return Citizens.Where(p=>p.age>=6 && (b is ResidentialBuilding?society.families.Any(h=>h.id==p.householdId && h.home==id):Workplace(p)==id))
                .Select(p=>(float)p.education).DefaultIfEmpty(0).Average();
        }
        string UpgradeBlockReason(int id)
        {
            var b=GetBuilding(id);if(b==null || !CityZoningState.ZoneUse(b.Use))return "此设施不使用分区建筑升级";
            if(b.level>=MaximumBuildingLevel(b))return "已达到当前建筑等级上限";
            if(b.abandoned || b.burning || b.burned)return "建筑不可用";
            if(!HasBasicServices(id))return "需要可用道路、水电、排污与垃圾服务";
            if(b.crime>=CrimeWarning)return "需要改善治安";
            if(b is ResidentialBuilding)
            {
                if(Occupancy(id)==0)return "需要真实住户";
                if(Residence(id).housingUnits>=100)return "当前住宅容量已达上限";
                if(LandValue(id)<35+b.level*10)return "需要提高土地价值";
                if(BuildingEducation(id)<new[]{0,.2f,.75f,1.5f,2.25f}[b.level])return "需要提高居民实际教育";
            }
            else
            {
                if(EmployedAt(id)==0)return "需要真实员工";
                if(ReachableServiceKinds(id)<b.level+1)return "需要增加可达市政服务";
                if(BuildingEducation(id)<b.level*.5f)return "需要提高员工实际教育";
            }
            return "";
        }
        public string BuildingUpgradeReason(int id)
        {var reason=UpgradeBlockReason(id);return reason==""?"条件满足 · "+GetBuilding(id).upgradeDays+" / "+UpgradeQualifyingDays+" 天":reason;}
        void UpdateBuildingGrowth()
        {
            bool changed=false;
            foreach(var b in buildings.Where(b=>CityZoningState.ZoneUse(b.Use)))
            {
                if(b.lastUpgradeDay==day)continue;b.lastUpgradeDay=day;
                bool ready=UpgradeBlockReason(b.id)=="";
                b.upgradeDays=ready?b.upgradeDays+1:0;
                if(b.upgradeDays<UpgradeQualifyingDays)continue;
                int previous=b.level;b.level++;b.upgradeDays=0;
                if(b is ResidentialBuilding home)home.housingUnits++;
                changed=true;Trace("building.upgraded","分区建筑在实际教育与服务条件下升级；阈值为待核实近似",new CityLogDetail{previous=previous,amount=b.level},building:b.id);
            }
            if(changed){BuildingsChanged();SyncJobs();Recalculate();}
        }
        bool ValidBuildingGrowth()=>buildings.All(b=>b.upgradeDays>=0 && b.upgradeDays<UpgradeQualifyingDays && b.lastUpgradeDay>=0 && b.lastUpgradeDay<=day
            && (CityZoningState.ZoneUse(b.Use) || b.upgradeDays==0 && b.lastUpgradeDay==0));
        public float FactoryProcessingRate(int id)=>development.enabled?1+.25f*(GetBuilding(id)?.level-1??0):1;
    }
}
