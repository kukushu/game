using System;
using System.Collections.Generic;
using System.Linq;
namespace HarborCity
{
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
