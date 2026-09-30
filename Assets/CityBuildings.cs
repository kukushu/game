using System;
using System.Collections.Generic;

namespace HarborCity
{
    [Serializable]
    public sealed class CityBuilding
    {
        public int id;
        public float x, z, yaw, entranceX, entranceZ;
        public float width = 2.9f, depth = 2.9f;
        public bool legacy;
        public HousingKind housing;
        public FactoryState factory;
        public float heavyTraffic;
        public int housingUnits, askingRent, vacantDays, applications;
        public List<int> interestedFamilies=new List<int>();
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
        public bool Overlaps(CityBuilding other)
        {
            foreach (float angle in new[] { yaw, yaw+90, other.yaw, other.yaw+90 })
            {
                double a = angle*Math.PI/180, b = (yaw-angle)*Math.PI/180, c = (other.yaw-angle)*Math.PI/180;
                double distance = Math.Abs((other.x-x)*Math.Cos(a)-(other.z-z)*Math.Sin(a));
                double radius = (width*Math.Abs(Math.Cos(b))+depth*Math.Abs(Math.Sin(b))
                    +other.width*Math.Abs(Math.Cos(c))+other.depth*Math.Abs(Math.Sin(c)))/2;
                if (distance >= radius-.015) return false;
            }
            return true;
        }
        public bool HitsRoad(RoadNode a, RoadNode b)
        {
            float length = CityRoads.Length(a,b);
            return Overlaps(new CityBuilding { x=(a.x+b.x)/2, z=(a.z+b.z)/2,
                yaw=-(float)(Math.Atan2(b.z-a.z,b.x-a.x)*180/Math.PI), width=length+.08f, depth=CityRoads.Width+.12f });
        }
    }

    public sealed partial class CityModel
    {
        // In v3 these arrays are stable entity slots, NOT spatial grid cells.
        // Existing slot IDs survive migration, demolition never reuses an ID.
        public List<CityBuilding> buildings;
        [NonSerialized] public int buildingRevision;
        [NonSerialized] int accessRevision = -1;
        [NonSerialized] CityRoads accessRoads;
        [NonSerialized] Dictionary<int,List<int>> buildingEntrances = new Dictionary<int,List<int>>();

        public void EnableBuildings()
        {
            if (version >= 3) return;
            if (roads == null) throw new InvalidOperationException("Migrate roads first");
            buildings = new List<CityBuilding>();
            for (int i=0;i<tiles.Length;i++)
            {
                var p=CityRoads.Lot(i);
                buildings.Add(new CityBuilding { id=i,x=p.x,z=p.z,entranceX=p.x,entranceZ=p.z,legacy=true });
            }
            version=3; buildingRevision++; Recalculate();
        }
        public List<int> AccessBuilding(int id)
        {
            if (id == -1) return roads.Access(-1);
            if (id<0 || id>=tiles.Length || tiles[id]<2) return new List<int>();
            if (buildingEntrances==null) buildingEntrances=new Dictionary<int,List<int>>();
            if (accessRoads!=roads || accessRevision!=roads.revision)
            { buildingEntrances.Clear(); accessRoads=roads; accessRevision=roads.revision; }
            if (buildingEntrances.TryGetValue(id,out var result)) return result;
            var b=buildings[id]; var p=new RoadNode { x=b.entranceX,z=b.entranceZ };
            RoadEdge best=null; float distance=b.legacy ? 3.85f : CityRoads.Width/2+.45f;
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
            for(int i=tiles.Length-1;i>=0;i--) if(tiles[i]>1 && buildings[i].Contains(x,z)) return i;
            return -1;
        }
        public bool CanBuild(CityBuilding lot, Func<float,float,float> height, out string error)
        {
            error=""; float low=float.MaxValue,high=float.MinValue;
            for(int z=0;z<=6;z++) for(int x=0;x<=6;x++)
            {
                var p=lot.Point((x/6f-.5f)*lot.width,(z/6f-.5f)*lot.depth);
                if(Math.Abs(p.x)>BuildHalfSize || Math.Abs(p.z)>BuildHalfSize) { error="超出建设边界"; return false; }
                float y=height(p.x,p.z); low=Math.Min(low,y); high=Math.Max(high,y);
            }
            if(low<=.15f || high-low>1.5f) { error="水面或坡度过陡，不能建设"; return false; }
            foreach(var e in roads.edges) if(lot.HitsRoad(roads.Node(e.a),roads.Node(e.b)))
            { error="地块与道路重叠"; return false; }
            for(int i=0;i<tiles.Length;i++) if(tiles[i]>1 && lot.Overlaps(buildings[i]))
            { error="地块与已有建筑或分区重叠"; return false; }
            return true;
        }
        public int PlaceBuilding(CityBuilding lot,LandUse use,Func<float,float,float> height,out string error)
        {
            error="";
            if(version<3 || use<LandUse.Residential || use>LandUse.Park || tiles.Length>=100000) return -1;
            if(!CanBuild(lot,height,out error)) return -1;
            int cost=society!=null && use==LandUse.Residential ? (lot.housing==HousingKind.Villa?society.settings.villaCost:society.settings.apartmentCost) : Cost(use);
            if(money<cost) { error="资金不足"; return -1; }
            int id=tiles.Length; Array.Resize(ref tiles,id+1); Array.Resize(ref levels,id+1);
            lot.id=id; buildings.Add(lot); tiles[id]=(int)use;
            if(society!=null)
            {
                levels[id]=1;
                if(use==LandUse.Residential) {lot.housingUnits=lot.housing==HousingKind.Villa?1:8; lot.askingRent=lot.housing==HousingKind.Villa?society.settings.villaRent:society.settings.apartmentRent;}
            }
            if(traffic!=null)
            {
                Array.Resize(ref traffic.stock,id+1); Array.Resize(ref traffic.nextCommute,id+1); Array.Resize(ref traffic.nextShopping,id+1);
            }
            money-=cost; buildingRevision++; Recalculate();
            Trace("building.created","建设 "+use,new CityLogDetail {amount=cost,x=lot.x,z=lot.z,count=lot.housingUnits},building:id); return id;
        }
        public bool DemolishBuilding(int id)
        {
            if(id<0 || id>=tiles.Length || tiles[id]<2 || money<Cost(LandUse.Bulldoze)) return false;
            Trace("building.demolished","拆除 "+(LandUse)tiles[id],buildings[id],building:id);
            money-=Cost(LandUse.Bulldoze); tiles[id]=levels[id]=0;
            if(traffic!=null)
            {
                traffic.lostGoods+=traffic.stock[id]; traffic.stock[id]=0;
                var f=buildings[id].factory;
                if(f!=null && f.initialized)
                {
                    traffic.lostRaw+=f.raw+(f.processing?1:0);
                    if(f.processing) f.discardedBatches++;
                    f.raw=0; f.processing=false; f.progress=0;
                    Trace("factory.demolished","拆除库存和在制品计入损失；残留环境影响逐步消退",f,building:id);
                }
            }
            buildingRevision++; Recalculate(); return true;
        }
        // A world-space phase along each straight line survives traffic-edge splitting.
        public List<CityBuilding> RoadsideLots()
        {
            var result=new List<CityBuilding>(); var seen=new HashSet<string>();
            foreach(var e in roads.edges)
            {
                var a=roads.Node(e.a); var b=roads.Node(e.b);
                float length=CityRoads.Length(a,b), ux=(b.x-a.x)/length, uz=(b.z-a.z)/length;
                if(ux<-.0001f || Math.Abs(ux)<.0001f && uz<0) { ux=-ux; uz=-uz; }
                float start=a.x*ux+a.z*uz, end=b.x*ux+b.z*uz;
                for(int k=(int)Math.Ceiling((Math.Min(start,end)-1.5f)/3-.0001f);k<=(int)Math.Floor((Math.Max(start,end)-1.5f)/3+.0001f);k++)
                for(int side=-1;side<=1;side+=2)
                {
                    float along=k*3+1.5f-start;
                    float x=a.x+ux*along-uz*3.05f*side, z=a.z+uz*along+ux*3.05f*side;
                    string key=Math.Round(x*100)+":"+Math.Round(z*100);
                    if(!seen.Add(key)) continue;
                    var lot=new CityBuilding { x=x,z=z,yaw=-(float)(Math.Atan2(uz,ux)*180/Math.PI)+(side<0?180:0) };
                    var entrance=lot.Point(0,-lot.depth/2); lot.entranceX=entrance.x; lot.entranceZ=entrance.z;
                    result.Add(lot);
                }
            }
            return result;
        }
        bool ValidBuildings()
        {
            if(buildings==null || buildings.Count!=tiles.Length || tiles.Length<Size*Size || tiles.Length>100000) return false;
            for(int i=0;i<buildings.Count;i++)
            {
                var b=buildings[i];
                if(b==null || b.id!=i || !FinitePosition(b.x) || !FinitePosition(b.z) || !FinitePosition(b.entranceX)
                    || !FinitePosition(b.entranceZ) || float.IsNaN(b.yaw) || float.IsInfinity(b.yaw)
                    || float.IsNaN(b.width) || float.IsNaN(b.depth) || b.width<2.9f || b.width>8.9f || b.depth<2.9f || b.depth>8.9f || tiles[i]==1
                    || version>=4 && tiles[i]==2 && (b.housingUnits<1 || b.housingUnits>100 || b.askingRent<0 || (int)b.housing<0 || (int)b.housing>2)) return false;
            }
            return true;
        }
        static bool FinitePosition(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && Math.Abs(value)<=BuildHalfSize;
    }
}
