using System;
using System.Collections.Generic;
using System.Linq;
using HarborCity.RoadCore;
namespace HarborCity
{
    // Land designations remain CityZoningState. These road-aligned plots are
    // derived planning data, never a replacement building/entity database.
    public sealed partial class CityCoreZoning
    {
        readonly CityModel city;readonly CityEntities entities;readonly World world;
        readonly List<ZoneCell> cells=new List<ZoneCell>();readonly Dictionary<string,ZoneCell> byKey=new Dictionary<string,ZoneCell>();
        readonly List<SpatialHit> hits=new List<SpatialHit>();
        readonly IReadOnlyList<ZoneCell> view;int roads=-1,buildings=-1,zones=-1,projects=-1;
        Func<float,float,float> height;float geometryExtent;
        readonly List<CityBuilding> projectPoses=new List<CityBuilding>();
        OccupancyIndex occupancyIndex;
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
        {
            if(!Ready)return view;
            bool geometry=roads!=world.NetworkGeneration||geometryExtent!=city.map.buildHalfSize;
            bool land=zones!=city.zoning.revision;
            bool occupancy=buildings!=entities.BuildingRevision||projects!=city.development.revision;
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
                try{RebuildGeometry();}catch{roads=-1;throw;} // Retry any interrupted derived update from a cold cache.
            }
            if(land||geometry)
            {
#if CITY_CORE_ATTRIBUTION
                using var part=CityCoreProfile.Measure(CityCoreProfile.Part.ZoningLandUse);
#endif
                RefreshLandUse(land||LastGeometryFull?cells:refreshedCells,land||LastGeometryFull);
            }
            if(occupancy||geometry)
            {
#if CITY_CORE_ATTRIBUTION
                using var part=CityCoreProfile.Measure(CityCoreProfile.Part.ZoningOccupancy);
#endif
                RefreshOccupancy(occupancy||LastGeometryFull?cells:refreshedCells,occupancy||LastGeometryFull);
            }
            roads=world.NetworkGeneration;geometryExtent=city.map.buildHalfSize;
            buildings=entities.BuildingRevision;zones=city.zoning.revision;projects=city.development.revision;
            return view;
        }
        void RefreshLandUse(IEnumerable<ZoneCell> targets,bool rebuildStamps)
        {
            float size=CityZoningState.CellSize;
            if(rebuildStamps){stamps.Clear();
            foreach(var fact in city.zoning.designations)
            {
                var values=fact.key.Split(':');var pose=new ResidentialBuilding{x=float.Parse(values[0],System.Globalization.CultureInfo.InvariantCulture),z=float.Parse(values[1],System.Globalization.CultureInfo.InvariantCulture),yaw=float.Parse(values[2],System.Globalization.CultureInfo.InvariantCulture),width=size,depth=size};var key=((int)Math.Floor(pose.x/size),(int)Math.Floor(pose.z/size));if(!stamps.TryGetValue(key,out var bucket))stamps.Add(key,bucket=new List<Stamp>());bucket.Add(new Stamp{fact=fact,pose=pose});
            }
            }
            foreach(var cell in targets)
            {
                var pose=cell.pose;int bx=(int)Math.Floor(pose.x/size),bz=(int)Math.Floor(pose.z/size),newest=-1;cell.use=LandUse.Empty;
                for(int x=bx-1;x<=bx+1;x++)for(int z=bz-1;z<=bz+1;z++)if(stamps.TryGetValue((x,z),out var land))foreach(var stamp in land)if(stamp.fact.id>newest&&stamp.pose.Contains(pose.x,pose.z)){newest=stamp.fact.id;cell.use=stamp.fact.use;}
            }
        }
        void RefreshOccupancy(IEnumerable<ZoneCell> targets,bool rebuildIndex)
        {
            if(rebuildIndex||occupancyIndex==null){projectPoses.Clear();foreach(var project in city.development.projects)projectPoses.Add(project.building.ToBuilding());
            occupancyIndex=new OccupancyIndex();foreach(var building in entities.Buildings)occupancyIndex.Add(building,building.id);foreach(var pose in projectPoses)occupancyIndex.Add(pose,-1);}
            foreach(var cell in targets)
            {
                cell.available=!occupancyIndex.Find(cell.pose,out int owner);cell.occupiedBuilding=owner;
            }
        }
        public void SelectRectangle(float x0,float z0,float x1,float z1,List<ZoneCell> selection)
        {
            if(selection==null)throw new ArgumentNullException(nameof(selection));
            if(float.IsNaN(x0)||float.IsInfinity(x0)||float.IsNaN(x1)||float.IsInfinity(x1)||float.IsNaN(z0)||float.IsInfinity(z0)||float.IsNaN(z1)||float.IsInfinity(z1))throw new ArgumentException("Invalid zoning rectangle");
            float left=Math.Min(x0,x1),right=Math.Max(x0,x1),bottom=Math.Min(z0,z1),top=Math.Max(z0,z1);
            var cells=Cells();if(ReferenceEquals(selection,cells))throw new ArgumentException("Selection must not alias zoning cells");
            selection.Clear();foreach(var cell in cells)if(cell.pose.x>=left&&cell.pose.x<=right&&cell.pose.z>=bottom&&cell.pose.z<=top)selection.Add(cell);
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
