using System;
using System.Collections.Generic;
using System.Linq;

namespace HarborCity
{
    // JsonUtility sees only concrete DTO fields. Runtime inheritance never crosses
    // the JSON boundary implicitly; unsupported formats are rejected explicitly.
    [Serializable] public sealed class BuildingPoseData
    {
        public int id,level;
        public float x,z,yaw,width,depth,entranceX,entranceZ;
    }
    [Serializable] public sealed class ResidentialSaveData
    {
        public HousingKind housing;
        public int housingUnits,askingRent,vacantDays,applications;
        public float heavyTraffic;
        public List<int> interestedFamilies;
    }
    [Serializable] public sealed class BuildingSaveData
    {
        public LandUse type;
        public BuildingPoseData pose;
        // Unity serializes a null inline class as a default object. Empty or
        // singleton arrays preserve payload presence across its JSON boundary.
        public ResidentialSaveData[] residential=Array.Empty<ResidentialSaveData>();
        public FactoryState[] industrial=Array.Empty<FactoryState>();
        public int stock;
        public static BuildingSaveData FromBuilding(CityBuilding b)
        {
            var d=new BuildingSaveData {type=b.Use,pose=new BuildingPoseData {id=b.id,level=b.level,x=b.x,z=b.z,yaw=b.yaw,width=b.width,depth=b.depth,entranceX=b.entranceX,entranceZ=b.entranceZ}};
            if(b is ResidentialBuilding r) d.residential=new[] {new ResidentialSaveData {housing=r.housing,housingUnits=r.housingUnits,askingRent=r.askingRent,vacantDays=r.vacantDays,applications=r.applications,heavyTraffic=r.heavyTraffic,interestedFamilies=r.interestedFamilies}};
            if(b is IndustrialBuilding f) d.industrial=new[] {f.factory};
            if(b is IGoodsBuilding goods) d.stock=goods.Stock;
            return d;
        }
        public CityBuilding ToBuilding()
        {
            if(pose==null || residential==null || industrial==null || residential.Length>1 || industrial.Length>1
                || (type==LandUse.Residential)!=(residential.Length==1) || (type==LandUse.Industrial)!=(industrial.Length==1)
                || residential.Any(r=>r==null) || industrial.Any(f=>f==null)
                || type!=LandUse.Commercial && type!=LandUse.Industrial && stock!=0) throw new ArgumentException("建筑存档类型与专属数据不匹配");
            var b=CityModel.NewBuilding(type);
            b.id=pose.id;b.level=pose.level;b.x=pose.x;b.z=pose.z;b.yaw=pose.yaw;b.width=pose.width;b.depth=pose.depth;b.entranceX=pose.entranceX;b.entranceZ=pose.entranceZ;
            if(b is ResidentialBuilding r) {var data=residential[0];r.housing=data.housing;r.housingUnits=data.housingUnits;r.askingRent=data.askingRent;r.vacantDays=data.vacantDays;r.applications=data.applications;r.heavyTraffic=data.heavyTraffic;r.interestedFamilies=data.interestedFamilies;}
            if(b is IndustrialBuilding f) f.factory=industrial[0];
            if(b is IGoodsBuilding goods) goods.Stock=stock;
            return b;
        }
    }
    [Serializable] public sealed class CitySaveData
    {
        public int format, money,day,nextBuildingId;
        public List<BuildingSaveData> buildings;
        public CityRoads roads;
        public TrafficState traffic;
        public HouseholdState society;
        public double retiredFactoryWages;
        public List<IndustrialExposure> residualIndustry;
        public CityModel ToCity()
        {
            if(format!=CityModel.SaveFormat) throw new ArgumentException("存档格式不支持，请使用新城市（格式 "+CityModel.SaveFormat+"）");
            if(buildings==null || buildings.Any(b=>b==null) || roads==null || traffic==null || society==null || residualIndustry==null) throw new ArgumentException("存档缺少必要实体");
            var c=new CityModel {money=money,day=day,nextBuildingId=nextBuildingId,buildings=buildings.Select(b=>b.ToBuilding()).ToList(),roads=roads,traffic=traffic,society=society,retiredFactoryWages=retiredFactoryWages,residualIndustry=residualIndustry};
            if(!c.Valid()) throw new ArgumentException("存档实体或关系校验失败");
            c.Recalculate();return c;
        }
    }
    public sealed partial class CityModel
    {
        public CitySaveData ToSaveData()=>new CitySaveData {format=SaveFormat,money=money,day=day,nextBuildingId=nextBuildingId,buildings=buildings.Select(BuildingSaveData.FromBuilding).ToList(),roads=roads,traffic=traffic,society=society,retiredFactoryWages=retiredFactoryWages,residualIndustry=residualIndustry};
    }
}
