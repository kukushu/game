using System;
using System.Collections.Generic;

namespace HarborCity.RoadCore
{
    public readonly struct SpatialHit
    {
        public readonly long drivable;public readonly double distance,separation;public readonly Point point;
        internal SpatialHit(long id,double distance,double separation,Point point){drivable=id;this.distance=distance;this.separation=separation;this.point=point;}
    }
    // Lazy, immutable-per-Network local geometry index. Segment traversal is linear
    // in crossed cells, not a diagonal segment's rectangular area. Main-thread query.
    internal sealed class RoadSpatialIndex
    {
        const double Cell=8;
        // The Network owns immutable, already-clipped lane geometry. Project
        // queries must not repeatedly sample its same segment endpoints. One
        // object per segment is shared by the crossed-cell buckets.
        sealed class Segment
        {
            internal readonly long drivable;internal readonly bool connection;internal readonly Point a,b;
            internal readonly double lo,dx,dy,dz,den;internal readonly bool valid;
            internal Segment(Drivable road,int sample)
            {
                drivable=road.Id;connection=road.IsConnection;lo=Math.Max(road.StartDistance,road.Geometry.At(sample-1));double hi=Math.Min(road.EndDistance,road.Geometry.At(sample));valid=hi>=lo;
                if(!valid)return;a=road.Geometry.Sample(lo);b=road.Geometry.Sample(hi);dx=b.x-a.x;dy=b.y-a.y;dz=b.z-a.z;den=dx*dx+dy*dy+dz*dz;
            }
        }
        readonly Dictionary<long,List<Segment>> cells;
        struct Shape
        {
            internal Geometry geometry;internal double start,end;
            internal Shape(Drivable d){geometry=d.Geometry;start=d.StartDistance;end=d.EndDistance;}
            internal bool Same(Drivable d)=>ReferenceEquals(geometry,d.Geometry)&&BitConverter.DoubleToInt64Bits(start)==BitConverter.DoubleToInt64Bits(d.StartDistance)&&BitConverter.DoubleToInt64Bits(end)==BitConverter.DoubleToInt64Bits(d.EndDistance);
        }
        readonly Dictionary<long,Shape> shapes=new Dictionary<long,Shape>(CityRoadKeyComparer.Instance);
        // A bucket is writable only during construction. Shared buckets/segments
        // are never edited; no index retains another Network or index instance.
        readonly HashSet<long> writable;
        sealed class Order:IComparer<SpatialHit>{public static readonly Order Instance=new Order();public int Compare(SpatialHit a,SpatialHit b){int c=a.separation.CompareTo(b.separation);return c!=0?c:a.drivable.CompareTo(b.drivable);}}
        static int Coordinate(double value){double c=Math.Floor(value/Cell);if(c< -100000000||c>100000000||double.IsNaN(c))throw new ArgumentException("Road outside spatial index domain");return (int)c;}
        static long Key(int x,int z)=>((long)x<<32)|(uint)z;
        void Add(int x,int z,Segment segment)
        {
            List<Segment> list;long key=Key(x,z);
            if(!cells.TryGetValue(key,out list))cells.Add(key,list=new List<Segment>());
            else if(writable!=null&&!writable.Contains(key))cells[key]=list=new List<Segment>(list);
            writable?.Add(key);list.Add(segment);
        }
        interface ICellVisitor {void Visit(int x,int z);}
        struct SegmentCells:ICellVisitor
        {
            internal RoadSpatialIndex index;internal Segment segment;
            public void Visit(int x,int z)=>index.Add(x,z,segment);
        }
        struct DirtyCells:ICellVisitor
        {
            internal HashSet<long> keys;
            public void Visit(int x,int z)=>keys.Add(Key(x,z));
        }
        // The same supercover walk inserts segments and locates their old cells.
        // Replay only changed Shapes, avoiding both a global bucket scan and a
        // persistent drivable-to-cells table. Includes corner side cells and the
        // raw geometry's clipped/invalid segments, exactly as insertion does.
        static void WalkCells<T>(Point a,Point b,ref T visitor) where T:struct,ICellVisitor
        {
            int x=Coordinate(a.x),z=Coordinate(a.z),endX=Coordinate(b.x),endZ=Coordinate(b.z),sx=Math.Sign(b.x-a.x),sz=Math.Sign(b.z-a.z);
            if(Math.Abs((long)endX-x)+Math.Abs((long)endZ-z)>100000)throw new ArgumentException("Single geometry segment exceeds spatial index budget");
            double dx=b.x-a.x,dz=b.z-a.z,deltaX=sx==0?double.PositiveInfinity:Cell/Math.Abs(dx),deltaZ=sz==0?double.PositiveInfinity:Cell/Math.Abs(dz),tx=sx==0?double.PositiveInfinity:((sx>0?x+1:x)*Cell-a.x)/dx,tz=sz==0?double.PositiveInfinity:((sz>0?z+1:z)*Cell-a.z)/dz;
            visitor.Visit(x,z);while(x!=endX||z!=endZ){if(tx<tz){x+=sx;tx+=deltaX;}else if(tz<tx){z+=sz;tz+=deltaZ;}else{if(x!=endX)visitor.Visit(x+sx,z);if(z!=endZ)visitor.Visit(x,z+sz);x+=sx;z+=sz;tx+=deltaX;tz+=deltaZ;}visitor.Visit(x,z);}
        }
        public RoadSpatialIndex(Network network,RoadSpatialIndex prior=null)
        {
            HashSet<long> changed=null;
            cells=prior==null?new Dictionary<long,List<Segment>>(CityRoadKeyComparer.Instance):new Dictionary<long,List<Segment>>(prior.cells,CityRoadKeyComparer.Instance);
            foreach(var road in network.Drivables)shapes.Add(road.Id,new Shape(road));
            if(prior!=null)
            {
                changed=new HashSet<long>(CityRoadKeyComparer.Instance);writable=new HashSet<long>(CityRoadKeyComparer.Instance);
                foreach(var road in network.Drivables){Shape old;if(!prior.shapes.TryGetValue(road.Id,out old)||!old.Same(road))changed.Add(road.Id);}
                foreach(long id in prior.shapes.Keys)if(!shapes.ContainsKey(id))changed.Add(id);
                var dirtyCells=new HashSet<long>(CityRoadKeyComparer.Instance);var visitor=new DirtyCells{keys=dirtyCells};
                foreach(long id in changed)if(prior.shapes.TryGetValue(id,out var old))
                    for(int sample=1;sample<old.geometry.Count;sample++)WalkCells(old.geometry[sample-1],old.geometry[sample],ref visitor);
                foreach(long cell in dirtyCells)
                {
                    var bucket=prior.cells[cell];var replacement=new List<Segment>();foreach(var segment in bucket)if(!changed.Contains(segment.drivable))replacement.Add(segment);
                    cells[cell]=replacement;writable.Add(cell);
                }
            }
            foreach(var road in network.Drivables)
            {
                if(changed!=null&&!changed.Contains(road.Id))continue;
                for(int sample=1;sample<road.Geometry.Count;sample++)
            {
                var visitor=new SegmentCells{index=this,segment=new Segment(road,sample)};
                WalkCells(road.Geometry[sample-1],road.Geometry[sample],ref visitor);
            }
            }
        }
        public void Project(Point p,double radius,bool connections,List<SpatialHit> output)
        {
            if(double.IsNaN(p.x)||double.IsNaN(p.y)||double.IsNaN(p.z)||double.IsInfinity(p.x)||double.IsInfinity(p.y)||double.IsInfinity(p.z))throw new ArgumentException("Nonfinite geometry query");
            if(radius<0||radius>64||double.IsNaN(radius))throw new ArgumentOutOfRangeException(nameof(radius));output.Clear();int minX=Coordinate(p.x-radius),maxX=Coordinate(p.x+radius),minZ=Coordinate(p.z-radius),maxZ=Coordinate(p.z+radius);
            for(int x=minX;x<=maxX;x++)for(int z=minZ;z<=maxZ;z++)
            {
                List<Segment> bucket;if(!cells.TryGetValue(Key(x,z),out bucket))continue;
                foreach(var segment in bucket)
                {
                    if((segment.connection&&!connections)||!segment.valid)continue;Point a=segment.a;double t=segment.den<1e-18?0:Math.Max(0,Math.Min(1,((p.x-a.x)*segment.dx+(p.y-a.y)*segment.dy+(p.z-a.z)*segment.dz)/segment.den));Point q=Point.Lerp(a,segment.b,t);double gap=Point.Distance(p,q);if(gap>radius)continue;
                    var hit=new SpatialHit(segment.drivable,segment.lo+Point.Distance(a,q),gap,q);int index=-1;for(int i=0;i<output.Count;i++)if(output[i].drivable==segment.drivable){index=i;break;}if(index<0)output.Add(hit);else if(gap<output[index].separation)output[index]=hit;
                }
            }
            output.Sort(Order.Instance);
        }
    }
}
