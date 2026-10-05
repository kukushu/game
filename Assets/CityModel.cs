using System;
using System.Collections.Generic;
namespace HarborCity
{
    public enum LandUse { Empty, Road, Residential, Commercial, Industrial, Power, Water, Park, Bulldoze }
    [Serializable] public sealed partial class CityModel
    {
        public const int SaveFormat=8;
        public const float BuildHalfSize=84f, RoadHalfSize=BuildHalfSize-1.5f;
        public int money=65000, day=1;
        public TrafficState traffic=new TrafficState();
        public CityRoads roads=new CityRoads();
        [NonSerialized] public int population,jobs,income,upkeep,power,water,demand,happiness=70;
        public static int Cost(LandUse use)
        {
            switch (use)
            {
                case LandUse.Road: return 100;
                case LandUse.Residential: case LandUse.Commercial: case LandUse.Industrial: return 60;
                case LandUse.Power: return 3500;
                case LandUse.Water: return 2500;
                case LandUse.Park: return 500;
                case LandUse.Bulldoze: return 40;
                default: return 0;
            }
        }

        public bool EntityRoadAccess(int id)=>BuildingAccess(id);
        public void Recalculate()=>RecalculateHouseholds();
        public void Tick()=>HouseholdTick();
        public void SetEntranceHeight(Func<float,float,float> height)
        {
            if(roads.edges.Count==0) {var n=roads.Node(CityRoads.Entrance); n.y=height(n.x,n.z);roads.Changed();}
        }
        public bool CommitRoad(RoadPlan plan)
        {
            if (roads == null || !plan.Valid || plan.revision != roads.revision || money < plan.cost) return false;
            roads = plan.network; roads.ApplySplits(traffic,plan.splits); money -= plan.cost; Recalculate();
            Trace("road.created","道路施工成功",new CityLogDetail {amount=plan.cost,count=plan.stroke,origin=plan.start.id,destination=plan.end.id,x=plan.end.x,z=plan.end.z}); return true;
        }

        public bool Valid()=>day>0 && ValidBuildings() && roads!=null && roads.Valid() && CityTraffic.Valid(this) && ValidHouseholds();
        public static CityModel Create()
        {
            var city=new CityModel();city.roads.nodes.Add(new RoadNode{id=CityRoads.Entrance,x=-52.5f,z=1.5f});
            city.Recalculate();return city;
        }
    }
}
