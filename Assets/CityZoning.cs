using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HarborCity
{
    [Serializable] public sealed class ZoneDesignation
    {
        public int id;
        public string key;
        public LandUse use;
    }
    [Serializable] public sealed class CityZoningState
    {
        // Prototype world scale. Four rows follow CS1; metres per unit remain unverified.
        public const float CellSize=1.5f;
        public const int Depth=4;
        public const string MapId="harbor-terrain-v1";
        public int nextId=1;
        public string mapId=MapId;
        public float cellSize=CellSize;
        public List<ZoneDesignation> designations=new List<ZoneDesignation>();
        [NonSerialized] public int revision;
        public static bool ZoneUse(LandUse use)=>use==LandUse.Residential || use==LandUse.Commercial || use==LandUse.Industrial;
        public bool Valid()
        {
            if(mapId!=MapId || cellSize!=CellSize || nextId<1 || designations==null || designations.Count>400000) return false;
            var ids=new HashSet<int>();var keys=new HashSet<string>();
            return designations.All(d=>d!=null && d.id>0 && d.id<nextId && ids.Add(d.id)
                && ZoneUse(d.use) && ZoneCell.ValidKey(d.key) && keys.Add(d.key));
        }
    }
    public sealed class ZoneCell
    {
        public string key,block;
        public int column,row,side,occupiedBuilding=-1;
        public bool available;
        public LandUse use;
        public CityBuilding pose;
        public static string Key(CityBuilding b)
        {
            var values=new[]{Math.Round(b.x,3),Math.Round(b.z,3),Math.Round(b.yaw,3)%360};
            return string.Join(":",values.Select(v=>(v==0?0:v).ToString("F3",CultureInfo.InvariantCulture)));
        }
        public static bool ValidKey(string key)
        {
            if(key==null || key.Length>64) return false;
            var parts=key.Split(':');if(parts.Length!=3) return false;
            var values=new float[3];
            for(int i=0;i<3;i++) if(!float.TryParse(parts[i],NumberStyles.Float,CultureInfo.InvariantCulture,out values[i]) || float.IsNaN(values[i]) || float.IsInfinity(values[i])) return false;
            return Math.Abs(values[0])<=CityModel.BuildHalfSize && Math.Abs(values[1])<=CityModel.BuildHalfSize && values[2]>=0 && values[2]<360;
        }
    }
    public sealed partial class CityModel
    {
        public CityZoningState zoning=new CityZoningState();
        [NonSerialized] CityRoads zoneRoads;
        [NonSerialized] int zoneRoadRevision=-1,zoneBuildingRevision=-1,zoneRevision=-1,zoneDevelopmentRevision=-1;
        [NonSerialized] Func<float,float,float> zoneHeight;
        [NonSerialized] List<ZoneCell> zoneCells;

        public IReadOnlyList<ZoneCell> ZoningCells(Func<float,float,float> height)
        {
            if(zoneCells!=null && zoneRoads==roads && zoneRoadRevision==roads.revision && zoneBuildingRevision==buildingRevision
                && zoneRevision==zoning.revision && zoneDevelopmentRevision==development.revision && Equals(zoneHeight,height)) return zoneCells;
            zoneRoads=roads;zoneRoadRevision=roads.revision;zoneBuildingRevision=buildingRevision;zoneRevision=zoning.revision;zoneHeight=height;
            zoneDevelopmentRevision=development.revision;
            zoneCells=new List<ZoneCell>();
            var uses=zoning.designations.ToDictionary(d=>d.key,d=>d.use);
            var claimed=new Dictionary<(int,int),List<ZoneCell>>();
            foreach(var group in roads.edges.OrderBy(e=>e.id).GroupBy(e=>e.stroke))
            {
                var remaining=group.ToList();
                while(remaining.Count>0)
                {
                    var degrees=new Dictionary<int,int>();
                    foreach(var e in remaining) foreach(int n in new[]{e.a,e.b}) {if(!degrees.ContainsKey(n)) degrees[n]=0;degrees[n]++;}
                    int start=degrees.Where(p=>p.Value!=2).Select(p=>p.Key).DefaultIfEmpty(degrees.Keys.Min()).Min();
                    int current=start;var path=new List<RoadNode>{roads.Node(start)};
                    do
                    {
                        var edge=remaining.FirstOrDefault(e=>e.a==current || e.b==current);if(edge==null) break;
                        remaining.Remove(edge);current=edge.a==current?edge.b:edge.a;path.Add(roads.Node(current));
                    } while(current!=start && degrees[current]==2);
                    GenerateZoneRun(path,group.Key+":"+start+":"+current,height,uses,claimed);
                }
            }
            return zoneCells;
        }
        void GenerateZoneRun(List<RoadNode> path,string block,Func<float,float,float> height,Dictionary<string,LandUse> uses,Dictionary<(int,int),List<ZoneCell>> claimed)
        {
            float size=CityZoningState.CellSize,total=0;
            var lengths=new List<float>();for(int i=1;i<path.Count;i++) {float length=CityRoads.Length(path[i-1],path[i]);lengths.Add(length);total+=length;}
            int columns=(int)Math.Floor((total-CityRoads.Width)/size);
            for(int column=0;column<columns;column++)
            {
                float distance=CityRoads.Width/2+(column+.5f)*size;int span=0;
                while(span<lengths.Count-1 && distance>lengths[span]) {distance-=lengths[span];span++;}
                var a=path[span];var b=path[span+1];var center=CityRoads.Lerp(a,b,distance/lengths[span]);
                float dx=(b.x-a.x)/lengths[span],dz=(b.z-a.z)/lengths[span];
                for(int side=-1;side<=1;side+=2) for(int row=0;row<CityZoningState.Depth;row++)
                {
                    float offset=side*(CityRoads.Width/2+BuildingSetback+(row+.5f)*size);
                    float yaw=(-(float)(Math.Atan2(dz,dx)*180/Math.PI)+(side<0?180:0)+360)%360;
                    var pose=new ResidentialBuilding{x=center.x-dz*offset,z=center.z+dx*offset,yaw=yaw,width=size-.04f,depth=size-.04f};
                    bool valid=true;float low=float.MaxValue,high=float.MinValue;
                    foreach(var corner in new[]{pose.Point(-size/2,-size/2),pose.Point(size/2,-size/2),pose.Point(size/2,size/2),pose.Point(-size/2,size/2)})
                    {
                        if(Math.Abs(corner.x)>BuildHalfSize || Math.Abs(corner.z)>BuildHalfSize) {valid=false;break;}
                        float y=height(corner.x,corner.z);low=Math.Min(low,y);high=Math.Max(high,y);
                    }
                    if(!valid || low<=.15f || high-low>1.5f || roads.edges.Any(e=>pose.HitsRoad(roads.Node(e.a),roads.Node(e.b)))) continue;
                    int bx=(int)Math.Floor(pose.x/size),bz=(int)Math.Floor(pose.z/size);
                    for(int x=bx-2;x<=bx+2;x++) for(int z=bz-2;z<=bz+2;z++)
                        if(claimed.TryGetValue((x,z),out var near) && near.Any(c=>pose.Overlaps(c.pose))) valid=false;
                    if(!valid) continue;
                    var cell=new ZoneCell{key=ZoneCell.Key(pose),block=block,column=column,row=row,side=side,pose=pose,available=true};
                    if(uses.TryGetValue(cell.key,out var use)) cell.use=use;
                    var occupied=buildings.FirstOrDefault(e=>pose.Overlaps(e));
                    if(occupied!=null) {cell.occupiedBuilding=occupied.id;cell.available=false;}
                    if(development.projects.Any(p=>pose.Overlaps(p.building.ToBuilding()))) cell.available=false;
                    zoneCells.Add(cell);if(!claimed.ContainsKey((bx,bz))) claimed[(bx,bz)]=new List<ZoneCell>();claimed[(bx,bz)].Add(cell);
                }
            }
        }
        public ZoneCell PickZone(float x,float z,Func<float,float,float> height)=>ZoningCells(height).FirstOrDefault(c=>c.pose.Contains(x,z));
        public int PaintZones(IEnumerable<ZoneCell> selection,LandUse use)
        {
            if(use!=LandUse.Empty && !CityZoningState.ZoneUse(use)) return 0;
            if(zoneCells==null || zoneRoads!=roads || zoneRoadRevision!=roads.revision || zoneBuildingRevision!=buildingRevision) return 0;
            var current=new HashSet<ZoneCell>(zoneCells);var lookup=zoning.designations.ToDictionary(d=>d.key);int changed=0;
            foreach(var cell in selection.Distinct())
            {
                if(cell==null || !current.Contains(cell) || cell.use==use) continue;
                if(use!=LandUse.Empty && (zoning.nextId==int.MaxValue || zoning.designations.Count>=400000)) break;
                // Rezoning occupied land is a planning change, never a demolition.
                if(lookup.TryGetValue(cell.key,out var old)) {zoning.designations.Remove(old);lookup.Remove(cell.key);}
                if(use!=LandUse.Empty)
                {
                    var d=new ZoneDesignation{id=zoning.nextId++,key=cell.key,use=use};zoning.designations.Add(d);lookup[cell.key]=d;
                }
                cell.use=use;changed++;
            }
            if(changed>0) {zoning.revision++;zoneRevision=zoning.revision;Trace("zoning.painted","沿路土地用途变更",new CityLogDetail{count=changed,reason=use.ToString()});}
            return changed;
        }
        public bool RemoveRoads(List<int> ids,int charge=40)
        {
            if(ids==null || ids.Count==0 || charge<0 || money<charge || ids.Any(id=>!roads.edges.Any(e=>e.id==id))) return false;
            roads.Remove(ids);money-=charge;Recalculate();return true;
        }
    }
}
