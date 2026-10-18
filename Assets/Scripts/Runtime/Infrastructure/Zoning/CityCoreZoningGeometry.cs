using System;
using System.Collections.Generic;
using System.Linq;
using HarborCity.RoadCore;

namespace HarborCity
{
    public sealed partial class CityCoreZoning
    {
        // Derived road candidates, not zoning facts. All accepted cells still
        // belong to this one CityCoreZoning; facts remain world/city/entities.
        sealed class Candidate {internal ZoneCell cell;internal bool valid;}
        sealed class Run
        {
            internal int id;internal Geometry centre;internal double lo,hi;internal float width;
            internal readonly List<Candidate> candidates=new List<Candidate>();
            internal readonly List<ZoneCell> accepted=new List<ZoneCell>();
            internal float minX=float.PositiveInfinity,minZ=float.PositiveInfinity,maxX=float.NegativeInfinity,maxZ=float.NegativeInfinity;
        }
        readonly struct Claim
        {
            internal readonly int road;internal readonly ZoneCell cell;internal readonly PlotClaim plot;
            internal Claim(int road,ZoneCell cell){this.road=road;this.cell=cell;plot=new PlotClaim(cell.pose);}
        }
        readonly Dictionary<int,Run> runs=new Dictionary<int,Run>();
        readonly Dictionary<(int,int),HashSet<int>> runBuckets=new Dictionary<(int,int),HashSet<int>>();
        readonly Dictionary<(int,int),List<Claim>> claims=new Dictionary<(int,int),List<Claim>>();
        readonly Dictionary<string,int> cellOwners=new Dictionary<string,int>();
        readonly Dictionary<long,Drivable> previousDrivables=new Dictionary<long,Drivable>(CityRoadKeyComparer.Instance);
        readonly List<ZoneCell> refreshedCells=new List<ZoneCell>();
        public int LastGeometryRoads {get;private set;}
        public int LastResolvedRoads {get;private set;}
        public bool LastGeometryFull {get;private set;}
        const float RunBucket=16;
        static bool SameGeometry(Geometry a,Geometry b)
        {if(ReferenceEquals(a,b))return true;if(a.Count!=b.Count)return false;for(int i=0;i<a.Count;i++){var x=a[i];var y=b[i];if(BitConverter.DoubleToInt64Bits(x.x)!=BitConverter.DoubleToInt64Bits(y.x)||BitConverter.DoubleToInt64Bits(x.y)!=BitConverter.DoubleToInt64Bits(y.y)||BitConverter.DoubleToInt64Bits(x.z)!=BitConverter.DoubleToInt64Bits(y.z))return false;}return true;}
        static bool SameDrivable(Drivable a,Drivable b)=>a.StartDistance==b.StartDistance&&a.EndDistance==b.EndDistance&&SameGeometry(a.Geometry,b.Geometry);
        void IndexRun(Run run,bool add)
        {
            if(run.candidates.Count==0)return;
            for(int x=(int)Math.Floor(run.minX/RunBucket);x<=(int)Math.Floor(run.maxX/RunBucket);x++)for(int z=(int)Math.Floor(run.minZ/RunBucket);z<=(int)Math.Floor(run.maxZ/RunBucket);z++)
            {
                var key=(x,z);if(add){if(!runBuckets.TryGetValue(key,out var bucket))runBuckets.Add(key,bucket=new HashSet<int>());bucket.Add(run.id);}
                else if(runBuckets.TryGetValue(key,out var bucket)){bucket.Remove(run.id);if(bucket.Count==0)runBuckets.Remove(key);}
            }
        }
        void Touch(double loX,double loZ,double hiX,double hiZ,SortedSet<int> pending,int after=0)
        {
            int x0=(int)Math.Floor(loX/RunBucket),x1=(int)Math.Floor(hiX/RunBucket),z0=(int)Math.Floor(loZ/RunBucket),z1=(int)Math.Floor(hiZ/RunBucket);
            // Conservative whole-run fallback for exceptionally large extents.
            if((long)(x1-x0+1)*(z1-z0+1)>100000){foreach(int id in runs.Keys)if(id>after)pending.Add(id);return;}
            for(int x=x0;x<=x1;x++)for(int z=z0;z<=z1;z++)if(runBuckets.TryGetValue((x,z),out var bucket))foreach(int id in bucket)if(id>after)pending.Add(id);
        }
        void TouchDrivable(Drivable road,SortedSet<int> pending)
        {
            // Project includes connections and clipped segments. Use the entire
            // old/new geometry in XZ, with a wider bound than the cell's query
            // radius sqrt(2*1.46^2)/2+2. Ignoring height is conservative.
            var g=road.Geometry;double x=g[0].x,z=g[0].z,X=x,Z=z;
            for(int i=1;i<g.Count;i++){x=Math.Min(x,g[i].x);z=Math.Min(z,g[i].z);X=Math.Max(X,g[i].x);Z=Math.Max(Z,g[i].z);}
            Touch(x-4,z-4,X+4,Z+4,pending);
        }
        Run Generate(int id,Geometry g,double lo,double hi,float width)
        {
            var run=new Run{id=id,centre=g,lo=lo,hi=hi,width=width};float size=CityZoningState.CellSize;
            int columns=(int)Math.Floor((hi-lo-width)/size);
            for(int column=0;column<columns;column++)
            {
                double station=lo+width/2+(column+.5)*size;var centre=g.Sample(station);var tangent=g.DirectionAt(station);double horizontal=Math.Sqrt(tangent.x*tangent.x+tangent.z*tangent.z);if(horizontal<1e-6||Math.Abs(centre.y-height((float)centre.x,(float)centre.z))>.75)continue;float dx=(float)(tangent.x/horizontal),dz=(float)(tangent.z/horizontal);
                for(int side=-1;side<=1;side+=2)for(int row=0;row<CityZoningState.Depth;row++)
                {
                    float offset=side*(width/2+CityModel.BuildingSetback+(row+.5f)*size),yaw=(-(float)(Math.Atan2(dz,dx)*180/Math.PI)+(side<0?180:0)+360)%360;
                    var pose=new ResidentialBuilding{x=(float)centre.x-dz*offset,z=(float)centre.z+dx*offset,yaw=yaw,width=size-.04f,depth=size-.04f};
                    var cell=new ZoneCell{block="core:"+id,column=column,row=row,side=side,pose=pose,available=true};run.candidates.Add(new Candidate{cell=cell});
                    run.minX=Math.Min(run.minX,pose.x);run.maxX=Math.Max(run.maxX,pose.x);run.minZ=Math.Min(run.minZ,pose.z);run.maxZ=Math.Max(run.maxZ,pose.z);
                }
            }
            return run;
        }
        void RemoveClaims(Run run)
        {
            float size=CityZoningState.CellSize;
            foreach(var cell in run.accepted)
            {
                var key=((int)Math.Floor(cell.pose.x/size),(int)Math.Floor(cell.pose.z/size));var bucket=claims[key];
                for(int i=bucket.Count-1;i>=0;i--)if(ReferenceEquals(bucket[i].cell,cell))bucket.RemoveAt(i);if(bucket.Count==0)claims.Remove(key);
                if(byKey.TryGetValue(cell.key,out var current)&&ReferenceEquals(current,cell)){byKey.Remove(cell.key);cellOwners.Remove(cell.key);}
            }
            run.accepted.Clear();
        }
        void Resolve(Run run)
        {
            float size=CityZoningState.CellSize;
            foreach(var candidate in run.candidates)
            {
                if(!candidate.valid)continue;var cell=candidate.cell;var pose=cell.pose;var plot=new PlotClaim(pose);int bx=(int)Math.Floor(pose.x/size),bz=(int)Math.Floor(pose.z/size);bool overlaps=false;
                for(int x=bx-2;x<=bx+2&&!overlaps;x++)for(int z=bz-2;z<=bz+2&&!overlaps;z++)if(claims.TryGetValue((x,z),out var bucket))foreach(var other in bucket)
                {
                    if(other.road>run.id)continue;
#if CITY_CORE_ATTRIBUTION
                    CityCoreProfile.ZoningCandidatePairs++;
#endif
                    if(!plot.CouldOverlap(other.plot))continue;
#if CITY_CORE_ATTRIBUTION
                    CityCoreProfile.ZoningExactOverlaps++;
#endif
                    if(pose.Overlaps(other.cell.pose)){overlaps=true;break;}
                }
                if(overlaps)continue;if(cell.key==null)cell.key=ZoneCell.Key(pose);
                if(cellOwners.TryGetValue(cell.key,out int owner)&&owner<=run.id)continue;
                run.accepted.Add(cell);byKey[cell.key]=cell;cellOwners[cell.key]=run.id;
                if(!claims.TryGetValue((bx,bz),out var accepted))claims.Add((bx,bz),accepted=new List<Claim>());accepted.Add(new Claim(run.id,cell));
            }
        }
        void Propagate(IEnumerable<ZoneCell> changed,int id,SortedSet<int> pending)
        {
            // The exact old greedy test only visits +/-2 1.5-unit buckets.
            // Eight units includes all such candidate centres, also across
            // arbitrary display chunks. Propagation is by priority, not radius:
            // every later road whose accepted result changes propagates again.
            foreach(var cell in changed)Touch(cell.pose.x-8,cell.pose.z-8,cell.pose.x+8,cell.pose.z+8,pending,id);
        }
        void RebuildGeometry()
        {
            bool full=roads<0||geometryExtent!=city.map.buildHalfSize;LastGeometryFull=full;LastGeometryRoads=LastResolvedRoads=0;refreshedCells.Clear();
            if(full){runs.Clear();runBuckets.Clear();claims.Clear();cellOwners.Clear();byKey.Clear();previousDrivables.Clear();}
            var pending=new SortedSet<int>();var staticDirty=new SortedSet<int>();var ids=new HashSet<int>(world.Network.RoadIds);var removed=new List<Run>();
            foreach(var run in runs.Values)if(!ids.Contains(run.id))removed.Add(run);
            foreach(var run in removed){IndexRun(run,false);runs.Remove(run.id);}
            foreach(int id in ids)
            {
                var settings=world.Network.Settings(id);var g=world.Network.Centreline(id);bool reverse=settings.direction==RoadDirection.Backward;var lane=world.Network.Get(Network.LaneId(id,reverse));
                double lo=reverse?lane.Geometry.MapReverseDistance(g,lane.EndDistance):lane.Geometry.MapDistance(g,lane.StartDistance),hi=reverse?lane.Geometry.MapReverseDistance(g,lane.StartDistance):lane.Geometry.MapDistance(g,lane.EndDistance);float width=settings.lanes*2.2f;
                if(runs.TryGetValue(id,out var prior)&&lo==prior.lo&&hi==prior.hi&&width==prior.width&&SameGeometry(g,prior.centre))continue;
                if(prior!=null){IndexRun(prior,false);removed.Add(prior);}var next=Generate(id,g,lo,hi,width);runs[id]=next;IndexRun(next,true);staticDirty.Add(id);
            }
            var now=new Dictionary<long,Drivable>(CityRoadKeyComparer.Instance);
            foreach(var d in world.Network.Drivables)
            {
                now.Add(d.Id,d);if(previousDrivables.TryGetValue(d.Id,out var old)&&SameDrivable(old,d))continue;
                if(!full){if(old!=null)TouchDrivable(old,staticDirty);TouchDrivable(d,staticDirty);}
            }
            foreach(var pair in previousDrivables)if(!now.ContainsKey(pair.Key))TouchDrivable(pair.Value,staticDirty);
            previousDrivables.Clear();foreach(var pair in now)previousDrivables.Add(pair.Key,pair.Value);
            foreach(var run in removed){var old=run.accepted.ToArray();RemoveClaims(run);Propagate(old,run.id,pending);}
            foreach(int id in staticDirty)if(runs.TryGetValue(id,out var run)){foreach(var candidate in run.candidates)candidate.valid=CanPlace(candidate.cell.pose,false);pending.Add(id);LastGeometryRoads++;}
            while(pending.Count>0)
            {
                int id=pending.Min;pending.Remove(id);if(!runs.TryGetValue(id,out var run))continue;
                var old=run.accepted.ToArray();RemoveClaims(run);Resolve(run);refreshedCells.AddRange(run.accepted);LastResolvedRoads++;
                if(!full&&!old.SequenceEqual(run.accepted)){Propagate(old,id,pending);Propagate(run.accepted,id,pending);}
            }
            // Keep the original public iteration order (growth uses it). This
            // cheap flatten is O(accepted cells); geometry/terrain/SAT/key work
            // above only visits invalidated road runs and dependency cascades.
            cells.Clear();foreach(int id in runs.Keys.OrderBy(id=>id))cells.AddRange(runs[id].accepted);
        }
    }
}
