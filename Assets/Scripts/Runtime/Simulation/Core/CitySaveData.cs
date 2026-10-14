using System;
using System.Collections.Generic;
using System.Linq;

namespace HarborCity
{
    // JsonUtility sees only concrete DTO fields. Runtime inheritance never crosses
    // the JSON boundary implicitly; unsupported formats are rejected explicitly.
    [Serializable] public sealed class BuildingPoseData
    {
        public int id,level,outageDays,abandonedDay,garbage,upgradeDays,lastUpgradeDay;
        public bool abandoned;
        public bool burning,burned;
        public float fireDamage,fireIntensity,crime;
        public float x,z,yaw,width,depth,entranceX,entranceZ;
    }
    [Serializable] public sealed class ResidentialSaveData
    {
        public HousingKind housing;
        public int housingUnits,askingRent,vacantDays,applications;
        public float heavyTraffic;
        public List<int> interestedFamilies;
    }
    [Serializable] public sealed class CommercialSaveData {public int customers,retailSold,retailDay,retailSoldToday;public double wholesaleCosts;}
    [Serializable] public sealed class LandfillSaveData {public int stored;public bool emptying;public float processingRemainder;}
    [Serializable] public sealed class BuildingSaveData
    {
        public LandUse type;
        public BuildingPoseData pose;
        // Unity serializes a null inline class as a default object. Empty or
        // singleton arrays preserve payload presence across its JSON boundary.
        public ResidentialSaveData[] residential=Array.Empty<ResidentialSaveData>();
        public FactoryState[] industrial=Array.Empty<FactoryState>();
        public int stock;
        public CommercialSaveData[] commercial=Array.Empty<CommercialSaveData>();
        public LandfillSaveData[] landfill=Array.Empty<LandfillSaveData>();
        public static BuildingSaveData FromBuilding(CityBuilding b)
        {
            var d=new BuildingSaveData {type=b.Use,pose=new BuildingPoseData {id=b.id,level=b.level,outageDays=b.outageDays,abandonedDay=b.abandonedDay,abandoned=b.abandoned,garbage=b.garbage,x=b.x,z=b.z,yaw=b.yaw,width=b.width,depth=b.depth,entranceX=b.entranceX,entranceZ=b.entranceZ}};
            d.pose.upgradeDays=b.upgradeDays;d.pose.lastUpgradeDay=b.lastUpgradeDay;d.pose.burning=b.burning;d.pose.burned=b.burned;d.pose.fireDamage=b.fireDamage;d.pose.fireIntensity=b.fireIntensity;d.pose.crime=b.crime;
            if(b is ResidentialBuilding r) d.residential=new[] {new ResidentialSaveData {housing=r.housing,housingUnits=r.housingUnits,askingRent=r.askingRent,vacantDays=r.vacantDays,applications=r.applications,heavyTraffic=r.heavyTraffic,interestedFamilies=r.interestedFamilies}};
            if(b is IndustrialBuilding f) d.industrial=new[] {f.factory};
            if(b is IGoodsBuilding goods) d.stock=goods.Stock;
            if(b is CommercialBuilding shop)d.commercial=new[]{new CommercialSaveData{customers=shop.customers,retailSold=shop.retailSold,retailDay=shop.retailDay,retailSoldToday=shop.retailSoldToday,wholesaleCosts=shop.wholesaleCosts}};
            if(b is LandfillBuilding dump)d.landfill=new[]{new LandfillSaveData{stored=dump.stored,emptying=dump.emptying,processingRemainder=dump.processingRemainder}};
            return d;
        }
        public CityBuilding ToBuilding()
        {
            if(pose==null || commercial==null || commercial.Length>1 || (type==LandUse.Commercial)!=(commercial.Length==1) || commercial.Any(c=>c==null) || landfill==null || landfill.Length>1 || (type==LandUse.Landfill)!=(landfill.Length==1) || landfill.Any(l=>l==null) || residential==null || industrial==null || residential.Length>1 || industrial.Length>1
                || (type==LandUse.Residential)!=(residential.Length==1) || (type==LandUse.Industrial)!=(industrial.Length==1)
                || residential.Any(r=>r==null) || industrial.Any(f=>f==null)
                || type!=LandUse.Commercial && type!=LandUse.Industrial && stock!=0) throw new ArgumentException("建筑存档类型与专属数据不匹配");
            var b=CityModel.NewBuilding(type);
            b.id=pose.id;b.level=pose.level;b.outageDays=pose.outageDays;b.abandonedDay=pose.abandonedDay;b.abandoned=pose.abandoned;b.garbage=pose.garbage;b.x=pose.x;b.z=pose.z;b.yaw=pose.yaw;b.width=pose.width;b.depth=pose.depth;b.entranceX=pose.entranceX;b.entranceZ=pose.entranceZ;
            b.upgradeDays=pose.upgradeDays;b.lastUpgradeDay=pose.lastUpgradeDay;b.burning=pose.burning;b.burned=pose.burned;b.fireDamage=pose.fireDamage;b.fireIntensity=pose.fireIntensity;b.crime=pose.crime;
            if(b is ResidentialBuilding r) {var data=residential[0];r.housing=data.housing;r.housingUnits=data.housingUnits;r.askingRent=data.askingRent;r.vacantDays=data.vacantDays;r.applications=data.applications;r.heavyTraffic=data.heavyTraffic;r.interestedFamilies=data.interestedFamilies;}
            if(b is IndustrialBuilding f) f.factory=industrial[0];
            if(b is IGoodsBuilding goods) goods.Stock=stock;
            if(b is CommercialBuilding shop){shop.customers=commercial[0].customers;shop.retailSold=commercial[0].retailSold;shop.retailDay=commercial[0].retailDay;shop.retailSoldToday=commercial[0].retailSoldToday;shop.wholesaleCosts=commercial[0].wholesaleCosts;}
            if(b is LandfillBuilding dump){dump.stored=landfill[0].stored;dump.emptying=landfill[0].emptying;dump.processingRemainder=landfill[0].processingRemainder;}
            return b;
        }
    }
    [Serializable] public sealed class CitySaveData
    {
        public int format, money,day,nextBuildingId;
        public CityMapState map;
        public List<BuildingSaveData> buildings;
        public CityRoads roads;
        public CityZoningState zoning;
        public CityDevelopmentState development;
        public CityUtilityState utilities;
        public CityWasteState waste;
        public CityFireState fire;
        public CityPoliceState police;
        public CityLifecycleState lifecycle;
        public CityDeathcareState deathcare;
        public CityLoanState loans;
        public TrafficState traffic;
        public HouseholdState society;
        public double retiredFactoryWages;
        public List<IndustrialExposure> residualIndustry;
        public CityModel ToCity()
        {
            if(format==23 && loans==null)loans=new CityLoanState {lastSettledDay=day};
            // Formats 23/24 predate one-way roads; their missing bool fields are
            // false, preserving all existing roads as two-way without changing IDs.
            if(format!=CityModel.SaveFormat && format!=33 && format!=32 && format!=31 && format!=30 && format!=29 && format!=28 && format!=27 && format!=26 && format!=25 && format!=24 && format!=23) throw new ArgumentException("存档格式不支持，请使用新城市（格式 "+CityModel.SaveFormat+"）");
            if(format<34)map=new CityMapState();
            if(map==null || !map.Valid())throw new ArgumentException("存档地图范围或地形版本不支持");
            if(format<32 && traffic?.trips!=null)foreach(var t in traffic.trips)if(t!=null){t.laneChangeFrom=t.lane;t.laneChangeRemaining=t.laneChangeCooldown=0;}
            // Retain old physical stock/trucks, but resume collection at converted treatment plants.
            if(format<31 && buildings!=null)foreach(var b in buildings)
                if(b?.landfill!=null)foreach(var depot in b.landfill)if(depot!=null){depot.emptying=false;depot.processingRemainder=0;}
            if(format<29)
            {
                if(roads?.nodes!=null)foreach(var n in roads.nodes){if(n==null)continue;n.control=JunctionControl.Automatic;n.stopFrom=new List<int>();}
                if(traffic?.trips!=null)foreach(var t in traffic.trips){if(t==null)continue;t.junctionWaitNode=-1;t.junctionQueuedAt=0;t.junctionStopTime=0;}
            }
            if(loans==null)throw new ArgumentException("存档缺少贷款状态");
            if(buildings==null || buildings.Any(b=>b==null) || roads==null || zoning==null || development==null || utilities==null || waste==null || fire==null || police==null || lifecycle==null || deathcare==null || traffic==null || society==null || residualIndustry==null) throw new ArgumentException("存档缺少必要实体");
            var c=new CityModel {map=map,money=money,day=day,nextBuildingId=nextBuildingId,buildings=buildings.Select(b=>b.ToBuilding()).ToList(),roads=roads,zoning=zoning,development=development,utilities=utilities,waste=waste,fire=fire,police=police,lifecycle=lifecycle,deathcare=deathcare,loans=loans,traffic=traffic,society=society,retiredFactoryWages=retiredFactoryWages,residualIndustry=residualIndustry};
            if(!c.Valid()) throw new ArgumentException("存档实体或关系校验失败");
            c.Recalculate();return c;
        }
    }
    public sealed partial class CityModel
    {
        public CitySaveData ToSaveData()=>new CitySaveData {format=SaveFormat,map=map,money=money,day=day,nextBuildingId=nextBuildingId,buildings=buildings.Select(BuildingSaveData.FromBuilding).ToList(),roads=roads,zoning=zoning,development=development,utilities=utilities,waste=waste,fire=fire,police=police,lifecycle=lifecycle,deathcare=deathcare,loans=loans,traffic=traffic,society=society,retiredFactoryWages=retiredFactoryWages,residualIndustry=residualIndustry};
    }
}
