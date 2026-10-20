using System;
using System.Collections.Generic;
using System.Linq;

namespace HarborCity
{
    [Serializable]
    public abstract class CityBuilding
    {
        public int id;
        public float x, z, yaw, entranceX, entranceZ;
        public float groundY,entranceY;
        public float width = 2.9f, depth = 2.9f;
        public int level=1;
        public int upgradeDays,lastUpgradeDay;
        public int outageDays,abandonedDay,garbage;
        public bool abandoned;
        public bool burning,burned;
        public float fireDamage,fireIntensity,crime;
        public abstract LandUse Use { get; }
        public RoadNode Point(float right, float forward)
        {
            double angle = yaw * Math.PI / 180;
            return new RoadNode { x = x + right * (float)Math.Cos(angle) + forward * (float)Math.Sin(angle),
                z = z - right * (float)Math.Sin(angle) + forward * (float)Math.Cos(angle) };
        }
        public bool Contains(float px, float pz)
        {
            double a = yaw * Math.PI / 180;
            float dx = px-x, dz = pz-z;
            return Math.Abs(dx*Math.Cos(a)-dz*Math.Sin(a)) <= width/2
                && Math.Abs(dx*Math.Sin(a)+dz*Math.Cos(a)) <= depth/2;
        }
        // Separating axes of both oriented rectangles; touching boundaries are allowed.
        public bool Overlaps(CityBuilding other) => Overlaps(other.x,other.z,other.yaw,other.width,other.depth);
        bool Overlaps(float ox,float oz,float oyaw,float ow,float od)
        {
            foreach (float angle in new[] { yaw, yaw+90, oyaw, oyaw+90 })
            {
                double a = angle*Math.PI/180, b = (yaw-angle)*Math.PI/180, c = (oyaw-angle)*Math.PI/180;
                double distance = Math.Abs((ox-x)*Math.Cos(a)-(oz-z)*Math.Sin(a));
                double radius = (width*Math.Abs(Math.Cos(b))+depth*Math.Abs(Math.Sin(b))
                    +ow*Math.Abs(Math.Cos(c))+od*Math.Abs(Math.Sin(c)))/2;
                if (distance >= radius-.015) return false;
            }
            return true;
        }
    }

    [Serializable] public sealed class ResidentialBuilding : CityBuilding
    {
        public override LandUse Use => LandUse.Residential;
        public HousingKind housing;
        public int housingUnits, askingRent, vacantDays, applications;
        public float heavyTraffic;
        public List<int> interestedFamilies=new List<int>();
    }
    public interface IGoodsBuilding { int Stock {get; set;} }
    [Serializable] public sealed class CommercialBuilding : CityBuilding, IGoodsBuilding
    {
        public override LandUse Use => LandUse.Commercial;
        public int stock,customers,retailSold,retailSoldToday;
        public int retailDay=-1;
        public double wholesaleCosts;
        public CityShopAccount Account {get;internal set;}=new CityShopAccount();
        public int Stock {get=>stock; set=>stock=value;}
    }
    [Serializable] public sealed class IndustrialBuilding : CityBuilding, IGoodsBuilding
    {
        public override LandUse Use => LandUse.Industrial;
        public FactoryState factory=new FactoryState();
        public int goodsStock;
        public int Stock {get=>goodsStock; set=>goodsStock=value;}
    }
    [Serializable] public sealed class PowerBuilding : CityBuilding {public override LandUse Use=>LandUse.Power;}
    [Serializable] public sealed class WaterBuilding : CityBuilding {public override LandUse Use=>LandUse.Water;}
    [Serializable] public sealed class SewageBuilding : CityBuilding {public override LandUse Use=>LandUse.Sewage;}
    [Serializable] public sealed class ParkBuilding : CityBuilding {public override LandUse Use=>LandUse.Park;}

    public sealed partial class CityModel
    {
        public List<CityBuilding> buildings=new List<CityBuilding>();
        public int nextBuildingId=1;
        [NonSerialized] Dictionary<int,CityBuilding> buildingLookup;
        public CityBuilding GetBuilding(int id)
        {
            if(entities!=null)return entities.Building(id);
            if(id<0) return null;
            if(buildingLookup==null || buildingLookup.Count!=buildings.Count) buildingLookup=buildings.ToDictionary(b=>b.id);
            return buildingLookup.TryGetValue(id,out var b)?b:null;
        }
        public ResidentialBuilding Residence(int id)=>GetBuilding(id) as ResidentialBuilding;
        public IGoodsBuilding Inventory(int id)=>GetBuilding(id) as IGoodsBuilding;
        public int Goods(int id)=>Inventory(id)?.Stock??0;
        public LandUse UseOf(int id)=>GetBuilding(id)?.Use??LandUse.Empty;
        public static CityBuilding NewBuilding(LandUse use)
        {
            switch(use)
            {
                case LandUse.Residential:return new ResidentialBuilding();
                case LandUse.Commercial:return new CommercialBuilding();
                case LandUse.Industrial:return new IndustrialBuilding();
                case LandUse.Power:return new PowerBuilding();
                case LandUse.Water:return new WaterBuilding();
                case LandUse.Park:return new ParkBuilding();
                case LandUse.Sewage:return new SewageBuilding();
                case LandUse.Landfill:return new LandfillBuilding{width=5.9f,depth=5.9f};
                case LandUse.Clinic:return new ClinicBuilding{width=5.9f,depth=5.9f};
                case LandUse.ElementarySchool:return new ElementarySchoolBuilding{width=5.9f,depth=5.9f};
                case LandUse.HighSchool:return new HighSchoolBuilding{width=5.9f,depth=5.9f};
                case LandUse.University:return new UniversityBuilding{width=5.9f,depth=5.9f};
                case LandUse.Cemetery:return new CemeteryBuilding{width=5.9f,depth=5.9f};
                case LandUse.FireHouse:return new FireHouseBuilding{width=5.9f,depth=5.9f};
                case LandUse.PoliceStation:return new PoliceStationBuilding{width=5.9f,depth=5.9f};
                default:throw new ArgumentException("Unsupported building type: "+use);
            }
        }
        public bool BuildingAccess(int id)
        {
            return entities!=null&&entities.RoadAccess(id);
        }
        public const float BuildingRoadSnapDistance=10f, BuildingSetback=.25f;
        bool FinitePosition(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && Math.Abs(value)<=map.buildHalfSize;
    }
}
