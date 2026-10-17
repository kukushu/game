using System;
using System.Collections.Generic;
using System.Linq;
using HarborCity.RoadCore;
namespace HarborCity
{
    // Land designations remain CityZoningState. These road-aligned plots are
    // derived planning data, never a replacement building/entity database.
    public sealed class CityCoreZoning
    {
        readonly CityModel city;readonly CityEntities entities;readonly World world;
        readonly List<ZoneCell> cells=new List<ZoneCell>();readonly Dictionary<string,ZoneCell> byKey=new Dictionary<string,ZoneCell>();
        readonly List<SpatialHit> hits=new List<SpatialHit>();
        readonly IReadOnlyList<ZoneCell> view;int roads=-1,buildings=-1,zones=-1,projects=-1;
        Func<float,float,float> height;float geometryExtent;
        readonly List<CityBuilding> projectPoses=new List<CityBuilding>();
        sealed class Stamp {internal ZoneDesignation fact;internal CityBuilding pose;}
        readonly struct PlotClaim
        {
            internal readonly CityBuilding pose;internal readonly double x,z;
            internal PlotClaim(CityBuilding pose)
            {this.pose=pose;double angle=pose.yaw*Math.PI/180,c=Math.Abs(Math.Cos(angle)),s=Math.Abs(Math.Sin(angle));x=(pose.width*c+pose.depth*s)/2;z=(pose.width*s+pose.depth*c)/2;}
            internal bool CouldOverlap(PlotClaim other)=>Math.Abs((double)pose.x-other.pose.x)<=x+other.x+.0001&&Math.Abs((double)pose.z-other.pose.z)<=z+other.z+.0001;
        }
        // Cold derived index, rebuilt only with occupancy. Centre buckets contain
        // each pose once; query extent includes the largest actual footprint.
        // Original entity order chooses the same first hit, independent of buckets.
        sealed class OccupancyIndex
        {
            struct Entry {internal PlotClaim claim;internal int order,owner;}
            readonly Dictionary<(int,int),List<Entry>> buckets=new Dictionary<(int,int),List<Entry>>();
            double maxX,maxZ;int next;
            internal void Add(CityBuilding pose,int owner)
            {
                var claim=new PlotClaim(pose);maxX=Math.Max(maxX,claim.x);maxZ=Math.Max(maxZ,claim.z);
                var key=((int)Math.Floor(pose.x/16),(int)Math.Floor(pose.z/16));
                if(!buckets.TryGetValue(key,out var list))buckets.Add(key,list=new List<Entry>());
                list.Add(new Entry{claim=claim,order=next++,owner=owner});
            }
            internal bool Find(CityBuilding pose,out int owner)
            {
                owner=-1;if(buckets.Count==0)return false;var claim=new PlotClaim(pose);int first=int.MaxValue;
                int loX=(int)Math.Floor((pose.x-claim.x-maxX-.0001)/16),hiX=(int)Math.Floor((pose.x+claim.x+maxX+.0001)/16);
                int loZ=(int)Math.Floor((pose.z-claim.z-maxZ-.0001)/16),hiZ=(int)Math.Floor((pose.z+claim.z+maxZ+.0001)/16);
                for(int x=loX;x<=hiX;x++)for(int z=loZ;z<=hiZ;z++)if(buckets.TryGetValue((x,z),out var list))foreach(var entry in list)
                {
                    if(entry.order>=first)continue;
#if CITY_CORE_ATTRIBUTION
                    CityCoreProfile.ZoningCandidatePairs++;
#endif
                    if(!claim.CouldOverlap(entry.claim))continue;
#if CITY_CORE_ATTRIBUTION
                    CityCoreProfile.ZoningExactOverlaps++;
#endif
                    if(pose.Overlaps(entry.claim.pose)){first=entry.order;owner=entry.owner;}
                }
                return first!=int.MaxValue;
            }
        }
        readonly Dictionary<(int,int),List<Stamp>> stamps=new Dictionary<(int,int),List<Stamp>>();
        public bool Ready=>height!=null;
        public CityCoreZoning(CityModel city,CityEntities entities,World world){this.city=city;this.entities=entities;this.world=world;view=cells.AsReadOnly();}
        public void SetSurface(Func<float,float,float> sampler){height=sampler??throw new ArgumentNullException(nameof(sampler));roads=-1;}
        public IReadOnlyList<ZoneCell> Cells()
        {using var roadProbeScope=new global::HarborCity.RoadBuildProbeScope("ZoningCells");
            if(!Ready)return view;
            bool geometry=roads!=world.NetworkGeneration||geometryExtent!=city.map.buildHalfSize;
            bool land=geometry||zones!=city.zoning.revision;
            bool occupancy=geometry||buildings!=entities.BuildingRevision||projects!=city.development.revision;
            if(!geometry&&!land&&!occupancy)return view;
#if CITY_CORE_ATTRIBUTION
            using var zoningObservation=CityCoreProfile.Measure(CityCoreProfile.Part.ZoningRebuild);
#endif
            // One derived cache, three distinct invalidation causes. Building or
            // project edits cannot change static road/terrain geometry.
            if(geometry)
            {
#if CITY_CORE_ATTRIBUTION
                using var part=CityCoreProfile.Measure(CityCoreProfile.Part.ZoningGeometry);
#endif
                RebuildGeometry();
            }
            if(land)
            {
#if CITY_CORE_ATTRIBUTION
                using var part=CityCoreProfile.Measure(CityCoreProfile.Part.ZoningLandUse);
#endif
                RefreshLandUse();
            }
            if(occupancy)
            {
#if CITY_CORE_ATTRIBUTION
                using var part=CityCoreProfile.Measure(CityCoreProfile.Part.ZoningOccupancy);
#endif
                RefreshOccupancy();
            }
            roads=world.NetworkGeneration;geometryExtent=city.map.buildHalfSize;
            buildings=entities.BuildingRevision;zones=city.zoning.revision;projects=city.development.revision;
            return view;
        }
        void RebuildGeometry()
        {
            cells.Clear();byKey.Clear();var claimed=new Dictionary<(int,int),List<PlotClaim>>();float size=CityZoningState.CellSize;
            foreach(int id in world.Network.RoadIds.OrderBy(id=>id))
            {
                var settings=world.Network.Settings(id);var g=world.Network.Centreline(id);bool reverse=settings.direction==RoadDirection.Backward;var lane=world.Network.Get(Network.LaneId(id,reverse));
                double lo=reverse?lane.Geometry.MapReverseDistance(g,lane.EndDistance):lane.Geometry.MapDistance(g,lane.StartDistance),hi=reverse?lane.Geometry.MapReverseDistance(g,lane.StartDistance):lane.Geometry.MapDistance(g,lane.EndDistance);
                float width=settings.lanes*2.2f;int columns=(int)Math.Floor((hi-lo-width)/size);
                for(int column=0;column<columns;column++)
                {
                    double station=lo+width/2+(column+.5)*size;var centre=g.Sample(station);var tangent=g.DirectionAt(station);double horizontal=Math.Sqrt(tangent.x*tangent.x+tangent.z*tangent.z);if(horizontal<1e-6||Math.Abs(centre.y-height((float)centre.x,(float)centre.z))>.75)continue;float dx=(float)(tangent.x/horizontal),dz=(float)(tangent.z/horizontal);
                    for(int side=-1;side<=1;side+=2)for(int row=0;row<CityZoningState.Depth;row++)
                    {
                        float offset=side*(width/2+CityModel.BuildingSetback+(row+.5f)*size),yaw=(-(float)(Math.Atan2(dz,dx)*180/Math.PI)+(side<0?180:0)+360)%360;
                        var pose=new ResidentialBuilding{x=(float)centre.x-dz*offset,z=(float)centre.z+dx*offset,yaw=yaw,width=size-.04f,depth=size-.04f};
                        if(!CanPlace(pose,false))continue;int bx=(int)Math.Floor(pose.x/size),bz=(int)Math.Floor(pose.z/size);bool overlaps=false;var claim=new PlotClaim(pose);
                        for(int x=bx-2;x<=bx+2&&!overlaps;x++)for(int z=bz-2;z<=bz+2&&!overlaps;z++)if(claimed.TryGetValue((x,z),out var nearby))foreach(var other in nearby)
                        {
#if CITY_CORE_ATTRIBUTION
                            CityCoreProfile.ZoningCandidatePairs++;
#endif
                            if(!claim.CouldOverlap(other))continue;
#if CITY_CORE_ATTRIBUTION
                            CityCoreProfile.ZoningExactOverlaps++;
#endif
                            if(pose.Overlaps(other.pose)){overlaps=true;break;}
                        }
                        if(overlaps)continue;var cell=new ZoneCell{key=ZoneCell.Key(pose),block="core:"+id,column=column,row=row,side=side,pose=pose,available=true};if(byKey.ContainsKey(cell.key))continue;
                        cells.Add(cell);byKey.Add(cell.key,cell);if(!claimed.TryGetValue((bx,bz),out var bucket))claimed.Add((bx,bz),bucket=new List<PlotClaim>());bucket.Add(claim);
                    }
                }
            }
        }
        void RefreshLandUse()
        {
            float size=CityZoningState.CellSize;stamps.Clear();
            foreach(var fact in city.zoning.designations)
            {
                var values=fact.key.Split(':');var pose=new ResidentialBuilding{x=float.Parse(values[0],System.Globalization.CultureInfo.InvariantCulture),z=float.Parse(values[1],System.Globalization.CultureInfo.InvariantCulture),yaw=float.Parse(values[2],System.Globalization.CultureInfo.InvariantCulture),width=size,depth=size};var key=((int)Math.Floor(pose.x/size),(int)Math.Floor(pose.z/size));if(!stamps.TryGetValue(key,out var bucket))stamps.Add(key,bucket=new List<Stamp>());bucket.Add(new Stamp{fact=fact,pose=pose});
            }
            foreach(var cell in cells)
            {
                var pose=cell.pose;int bx=(int)Math.Floor(pose.x/size),bz=(int)Math.Floor(pose.z/size),newest=-1;cell.use=LandUse.Empty;
                for(int x=bx-1;x<=bx+1;x++)for(int z=bz-1;z<=bz+1;z++)if(stamps.TryGetValue((x,z),out var land))foreach(var stamp in land)if(stamp.fact.id>newest&&stamp.pose.Contains(pose.x,pose.z)){newest=stamp.fact.id;cell.use=stamp.fact.use;}
            }
        }
        void RefreshOccupancy()
        {
            projectPoses.Clear();foreach(var project in city.development.projects)projectPoses.Add(project.building.ToBuilding());
            var index=new OccupancyIndex();foreach(var building in entities.Buildings)index.Add(building,building.id);foreach(var pose in projectPoses)index.Add(pose,-1);
            foreach(var cell in cells)
            {
                cell.available=!index.Find(cell.pose,out int owner);cell.occupiedBuilding=owner;
            }
        }
        public int Paint(IEnumerable<ZoneCell> selection,LandUse use)
        {
            entities.StructuralBoundary();Cells();if(use!=LandUse.Empty&&!CityZoningState.ZoneUse(use))throw new ArgumentException("Invalid zoning use");var lookup=city.zoning.designations.ToDictionary(d=>d.key);int changed=0;
            foreach(var cell in selection.ToArray())
            {
                if(!byKey.TryGetValue(cell.key,out var current)||!ReferenceEquals(cell,current))continue;
                if(current.use==use)continue;
                // A brush replaces the local world-space stamps, including ones
                // from the prior road alignment. Clearing must erase those too.
                int bx=(int)Math.Floor(cell.pose.x/CityZoningState.CellSize),bz=(int)Math.Floor(cell.pose.z/CityZoningState.CellSize);var replaced=new List<ZoneDesignation>();
                for(int x=bx-1;x<=bx+1;x++)for(int z=bz-1;z<=bz+1;z++)if(stamps.TryGetValue((x,z),out var land))foreach(var stamp in land)if(stamp.pose.Contains(cell.pose.x,cell.pose.z)&&!replaced.Contains(stamp.fact))replaced.Add(stamp.fact);
                foreach(var old in replaced){city.zoning.designations.Remove(old);lookup.Remove(old.key);}
                if(lookup.TryGetValue(cell.key,out var exact)){city.zoning.designations.Remove(exact);lookup.Remove(cell.key);}
                if(use!=LandUse.Empty){var d=new ZoneDesignation{id=city.zoning.nextId++,key=cell.key,use=use};city.zoning.designations.Add(d);lookup.Add(d.key,d);}if(use!=LandUse.Empty||replaced.Count>0)changed++;
            }
            if(changed>0)city.zoning.revision++;return changed;
        }
        internal bool CanPlace(CityBuilding pose,bool checkBuildings=true,int ignoreProject=-1)
        {
#if CITY_CORE_ATTRIBUTION
            using var placementObservation=CityCoreProfile.Measure(CityCoreProfile.Part.Placement);
#endif
            if(!Ready)return false;float low=float.MaxValue,high=float.MinValue;
#if CITY_CORE_ATTRIBUTION
            using(CityCoreProfile.Measure(CityCoreProfile.Part.PlacementTerrain))
            {
#endif
            foreach(var corner in new[]{pose.Point(-pose.width/2,-pose.depth/2),pose.Point(pose.width/2,-pose.depth/2),pose.Point(pose.width/2,pose.depth/2),pose.Point(-pose.width/2,pose.depth/2)}){if(Math.Abs(corner.x)>city.map.buildHalfSize||Math.Abs(corner.z)>city.map.buildHalfSize)return false;float y=height(corner.x,corner.z);if(float.IsNaN(y)||float.IsInfinity(y))return false;low=Math.Min(low,y);high=Math.Max(high,y);}
            if(low<=.15f||high-low>1.5f)return false;pose.groundY=height(pose.x,pose.z);
#if CITY_CORE_ATTRIBUTION
            }
            using(CityCoreProfile.Measure(CityCoreProfile.Part.PlacementBuildings))
            {
#endif
            if(checkBuildings&&(city.buildings.Any(b=>pose.Overlaps(b))||city.development.projects.Any(p=>p.id!=ignoreProject&&pose.Overlaps(p.building.ToBuilding()))))return false;
#if CITY_CORE_ATTRIBUTION
            }
#endif
            return !CityRoadClearance.Intersects(world.Network,pose,hits);
        }
        internal SpatialHit? Entrance(CityBuilding b)
        {
            var p=b.Point(0,-b.depth/2);b.entranceX=p.x;b.entranceZ=p.z;b.entranceY=height(p.x,p.z);world.Network.Project(new Point(p.x,b.entranceY,p.z),4,false,hits);foreach(var hit in hits)if(Math.Abs(hit.point.y-b.entranceY)<.75)return hit;return null;
        }
    }
}
