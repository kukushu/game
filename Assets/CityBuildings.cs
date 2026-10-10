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
        public bool HitsRoad(RoadNode a, RoadNode b)
        {
            float length = CityRoads.Length(a,b);
            return Overlaps((a.x+b.x)/2,(a.z+b.z)/2,-(float)(Math.Atan2(b.z-a.z,b.x-a.x)*180/Math.PI),length+.08f,CityRoads.Width+.12f);
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
        [NonSerialized] public int buildingRevision;
        [NonSerialized] Dictionary<int,CityBuilding> buildingLookup;
        [NonSerialized] int accessRevision=-1;
        [NonSerialized] CityRoads accessRoads;
        [NonSerialized] Dictionary<int,List<int>> buildingEntrances=new Dictionary<int,List<int>>();
        public CityBuilding GetBuilding(int id)
        {
            if(id<0) return null;
            if(buildingLookup==null || buildingLookup.Count!=buildings.Count) buildingLookup=buildings.ToDictionary(b=>b.id);
            return buildingLookup.TryGetValue(id,out var b)?b:null;
        }
        public ResidentialBuilding Residence(int id)=>GetBuilding(id) as ResidentialBuilding;
        public IGoodsBuilding Inventory(int id)=>GetBuilding(id) as IGoodsBuilding;
        public int Goods(int id)=>Inventory(id)?.Stock??0;
        public LandUse UseOf(int id)=>GetBuilding(id)?.Use??LandUse.Empty;
        void BuildingsChanged()
        {
            buildingRevision++; buildingLookup=null; buildingEntrances?.Clear(); commuteCache?.Clear();
        }
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
        public List<int> AccessBuilding(int id)
        {
            if(id==-1) return roads.Active(CityRoads.Entrance)?new List<int>{CityRoads.Entrance}:new List<int>();
            if (GetBuilding(id)==null) return new List<int>();
            if (buildingEntrances==null) buildingEntrances=new Dictionary<int,List<int>>();
            if (accessRoads!=roads || accessRevision!=roads.revision)
            { buildingEntrances.Clear(); accessRoads=roads; accessRevision=roads.revision; }
            if (buildingEntrances.TryGetValue(id,out var result)) return result;
            var b=GetBuilding(id); var p=new RoadNode { x=b.entranceX,z=b.entranceZ };
            RoadEdge best=null; float distance=CityRoads.Width/2+.45f;
            foreach (var e in roads.edges)
            {
                float d=CityRoads.Distance(p,roads.Node(e.a),roads.Node(e.b));
                if (d<distance) { distance=d; best=e; }
            }
            result=new List<int>();
            if (best!=null) result.Add(CityRoads.Length(p,roads.Node(best.a))<=CityRoads.Length(p,roads.Node(best.b)) ? best.a : best.b);
            buildingEntrances[id]=result; return result;
        }
        public bool BuildingAccess(int id)
        {
            foreach (int node in AccessBuilding(id)) if (roads.Connected(node)) return true;
            return false;
        }
        public int PickBuilding(float x,float z)
        {
            foreach(var b in buildings.OrderByDescending(b=>b.id)) if(b.Contains(x,z)) return b.id;
            return -1;
        }
        public bool CanBuild(CityBuilding lot, Func<float,float,float> height, out string error,int ignoreProject=-1)
        {
            error=""; float low=float.MaxValue,high=float.MinValue;
            for(int z=0;z<=6;z++) for(int x=0;x<=6;x++)
            {
                var p=lot.Point((x/6f-.5f)*lot.width,(z/6f-.5f)*lot.depth);
                if(Math.Abs(p.x)>BuildHalfSize || Math.Abs(p.z)>BuildHalfSize) { error="超出建设边界"; return false; }
                float y=height(p.x,p.z); low=Math.Min(low,y); high=Math.Max(high,y);
            }
            if(low<=.15f || high-low>1.5f) { error="水面或坡度过陡，不能建设"; return false; }
            if(lot.Use==LandUse.Sewage && development.enabled)
            {
                bool shore=false;
                for(int i=0;i<32;i++) {double angle=i*Math.PI/16;float px=lot.x+(float)Math.Cos(angle)*18,pz=lot.z+(float)Math.Sin(angle)*18;
                    if(height(px,pz)<=.15f) {shore=true;break;}}
                if(!shore) {error="排水设施须建在水岸附近";return false;}
            }
            foreach(var e in roads.edges) if(lot.HitsRoad(roads.Node(e.a),roads.Node(e.b)))
            { error="地块与道路重叠"; return false; }
            foreach(var existing in buildings) if(lot.Overlaps(existing))
            { error="地块与已有建筑或分区重叠"; return false; }
            foreach(var project in development.projects) if(project.id!=ignoreProject && lot.Overlaps(project.building.ToBuilding()))
            {error="地块已有建筑正在施工";return false;}
            return true;
        }
        public int PlaceBuilding(CityBuilding lot,LandUse use,Func<float,float,float> height,out string error)
        {
            error="";
            if(lot==null || lot.Use!=use || buildings.Count>=100000 || nextBuildingId==int.MaxValue) {error="建筑类型无效或数量已达上限";return -1;}
            if(!CanBuild(lot,height,out error)) return -1;
            int cost=lot is ResidentialBuilding r?(r.housing==HousingKind.Villa?society.settings.villaCost:society.settings.apartmentCost):Cost(use);
            if(lot is IndustrialBuilding) cost+=FactoryState.StartingCash;
            if(money<cost) {error="资金不足";return -1;}
            lot.id=nextBuildingId++; lot.level=1;
            if(lot is ResidentialBuilding home) {home.housingUnits=home.housing==HousingKind.Villa?1:8;home.askingRent=home.housing==HousingKind.Villa?society.settings.villaRent:society.settings.apartmentRent;}
            if(lot is IndustrialBuilding industry) industry.factory=new FactoryState {cash=FactoryState.StartingCash,capital=FactoryState.StartingCash};
            buildings.Add(lot); money-=cost; BuildingsChanged(); Recalculate();
            Trace("building.created","建设 "+use,new CityLogDetail {amount=cost,x=lot.x,z=lot.z,count=(lot as ResidentialBuilding)?.housingUnits??0},building:lot.id);
            if(lot is IndustrialBuilding factory) Trace("factory.capital","建设预算转入工厂期初经营资金；不自动补款",factory.factory,building:lot.id);
            return lot.id;
        }
        public bool DemolishBuilding(int id)
        {
            var b=GetBuilding(id); if(b==null || money<Cost(LandUse.Bulldoze) || b is LandfillBuilding landfill && (landfill.stored>0 || LandfillIncoming(id)>0 || traffic.trips.Any(t=>t.cargoKind==CargoKind.Waste && (t.home==id || t.destination==id || t.origin==id)))) return false;
            // Until displacement from a demolished treatment facility is implemented,
            // refuse removal of a clinic with an active patient or ambulance.
            if(b is ClinicBuilding && (Citizens.Any(p=>p.medicalClinicId==id) || traffic.trips.Any(t=>t.clinicId==id)))return false;
            if(b is FireHouseBuilding && FireEnginesAt(id)>0)return false;
            if(b is PoliceStationBuilding && PoliceCarsAt(id)>0)return false;
            if(BodiesAt(id)>0 || b is CemeteryBuilding && (CemeteryReserved(id)>0 || HearsesAt(id)>0))return false;
            Trace("building.demolished","拆除 "+b.Use,b,building:id);
            if(b is SchoolBuilding)foreach(var person in Citizens.Where(p=>p.schoolId==id))
            {new CityTraffic(this).EndSchoolEnrollment(person);}
            new CityTraffic(this).RemoveBuilding(id);
            traffic.lostGoods+=Goods(id);
            if(b is IndustrialBuilding industry)
            {
                var f=industry.factory; traffic.lostRaw+=f.raw+(f.processing?1:0);
                retiredFactoryWages+=f.wageCosts;
                residualIndustry.Add(new IndustrialExposure {x=b.x,z=b.z,noise=f.noise,pollution=f.pollution});
                Trace("factory.demolished","库存计入损失，保留环境衰减与历史工资核对",f,building:id);
            }
            foreach(var h in society.families)
            {
                if(h.home==id)
                {
                    h.resident=false;h.home=h.unit=-1;h.nextReview=day;h.reason="住所拆除，等待重新迁入";
                    foreach(var p in h.people.Where(p=>!p.dead)) {ReleaseJob(p);p.location=-1;p.atWork=false;p.tripId=0;p.medicalStage=MedicalStage.None;p.medicalClinicId=-1;p.treatmentMinutes=0;ReleaseSchoolPlace(p);p.atSchool=p.schoolReturning=false;}
                    Trace("household.left",h.reason,h,household:h.id);
                }
                foreach(var p in h.people)
                {
                    if(p.location==id) p.location=-1;
                    if(p.observedHome==id || p.observedWork==id) {p.observedHome=p.observedWork=-1;p.lastCommute=-1;}
                }
            }
            waste.lost+=b.garbage;buildings.Remove(b);money-=Cost(LandUse.Bulldoze);BuildingsChanged();Recalculate();return true;
        }
        public const float BuildingRoadSnapDistance=10f, BuildingSetback=.25f;
        // Pure temporary pose: no slot allocation, zoning grid, or road mutation.
        // Local -Z is the frontage, matching the existing meshes and entrances.
        public CityBuilding RoadsidePreview(float mouseX,float mouseZ,LandUse use,HousingKind housing,out string error)
        {
            error="";
            if(roads==null || roads.edges.Count==0) {error="附近没有道路，请先修路"; return null;}
            var mouse=new RoadNode {x=mouseX,z=mouseZ}; RoadEdge closest=null;
            float distance=BuildingRoadSnapDistance;
            foreach(var e in roads.edges)
            {
                float d=CityRoads.Distance(mouse,roads.Node(e.a),roads.Node(e.b));
                if(d<=distance && (closest==null || d<distance || e.id<closest.id)) {closest=e; distance=d;}
            }
            if(closest==null) {error="离道路太远，请移到道路中心线 "+BuildingRoadSnapDistance+" 米以内"; return null;}
            var a=roads.Node(closest.a); var b=roads.Node(closest.b);
            var p=CityRoads.Lerp(a,b,CityRoads.Projection(mouse,a,b));
            float length=CityRoads.Length(a,b),ux=(b.x-a.x)/length,uz=(b.z-a.z)/length;
            float side=(mouseX-p.x)*(-uz)+(mouseZ-p.z)*ux<0?-1:1;
            float nx=-uz*side,nz=ux*side;
            var lot=NewBuilding(use); lot.id=-1; lot.yaw=(float)(Math.Atan2(nx,nz)*180/Math.PI);
            if(lot is ResidentialBuilding home)
            {home.housing=housing;lot.width=housing==HousingKind.Villa?5.9f:2.9f; lot.depth=5.9f;}
            float offset=CityRoads.Width/2+lot.depth/2+BuildingSetback;
            lot.x=p.x+nx*offset; lot.z=p.z+nz*offset;
            var front=lot.Point(0,-lot.depth/2); lot.entranceX=front.x; lot.entranceZ=front.z;
            return lot;
        }
        bool ValidBuildings()
        {
            if(buildings==null || buildings.Count>100000 || nextBuildingId<1) return false;
            var ids=new HashSet<int>();
            foreach(var b in buildings)
            {
                if(b==null || b.id<1 || b.id>=nextBuildingId || !ids.Add(b.id) || !FinitePosition(b.x) || !FinitePosition(b.z)
                    || !FinitePosition(b.entranceX) || !FinitePosition(b.entranceZ) || float.IsNaN(b.yaw) || float.IsInfinity(b.yaw)
                    || float.IsNaN(b.width) || float.IsNaN(b.depth) || b.width<1.4f || b.width>8.9f || b.depth<1.4f || b.depth>8.9f || b.level<1 || b.level>MaximumBuildingLevel(b)
                    || b.garbage<0 || b.garbage>100000 || b.outageDays<0 || b.outageDays>100000 || b.abandonedDay<0 || b.abandonedDay>day
                    || b.abandoned!=(b.abandonedDay>0) || b.abandoned && !CityZoningState.ZoneUse(b.Use)) return false;
                if(b is ResidentialBuilding r && (r.housingUnits<1 || r.housingUnits>100 || r.askingRent<0 || r.interestedFamilies==null || (int)r.housing<0 || (int)r.housing>1)) return false;
                if(b is IGoodsBuilding inventory && (inventory.Stock<0 || inventory.Stock>(b is IndustrialBuilding?24:32))) return false;
                if(b is CommercialBuilding shop && (shop.customers<0 || shop.retailSold<0 || shop.retailSold>shop.customers || shop.retailDay<-1 || shop.retailDay>day || shop.retailSoldToday<0 || shop.retailSoldToday>shop.retailSold))return false;
                if(b is IndustrialBuilding i && i.factory==null) return false;
                if(b is LandfillBuilding dump && (dump.stored<0 || dump.stored>LandfillBuilding.Capacity))return false;
            }
            buildingLookup=null;return true;
        }
        static bool FinitePosition(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && Math.Abs(value)<=BuildHalfSize;
    }
}
