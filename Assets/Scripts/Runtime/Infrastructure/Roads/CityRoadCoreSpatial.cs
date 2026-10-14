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
        struct Segment {public Drivable road;public int sample;}
        readonly Dictionary<long,List<Segment>> cells=new Dictionary<long,List<Segment>>(CityRoadKeyComparer.Instance);
        sealed class Order:IComparer<SpatialHit>{public static readonly Order Instance=new Order();public int Compare(SpatialHit a,SpatialHit b){int c=a.separation.CompareTo(b.separation);return c!=0?c:a.drivable.CompareTo(b.drivable);}}
        static int Coordinate(double value){double c=Math.Floor(value/Cell);if(c< -100000000||c>100000000||double.IsNaN(c))throw new ArgumentException("Road outside spatial index domain");return (int)c;}
        static long Key(int x,int z)=>((long)x<<32)|(uint)z;
        void Add(int x,int z,Segment segment){List<Segment> list;long key=Key(x,z);if(!cells.TryGetValue(key,out list))cells.Add(key,list=new List<Segment>());list.Add(segment);}
        public RoadSpatialIndex(Network network)
        {
            foreach(var road in network.Drivables)for(int sample=1;sample<road.Geometry.Count;sample++)
            {
                var segment=new Segment{road=road,sample=sample};Point a=road.Geometry[sample-1],b=road.Geometry[sample];int x=Coordinate(a.x),z=Coordinate(a.z),endX=Coordinate(b.x),endZ=Coordinate(b.z),sx=Math.Sign(b.x-a.x),sz=Math.Sign(b.z-a.z);
                if(Math.Abs((long)endX-x)+Math.Abs((long)endZ-z)>100000)throw new ArgumentException("Single geometry segment exceeds spatial index budget");
                double dx=b.x-a.x,dz=b.z-a.z,deltaX=sx==0?double.PositiveInfinity:Cell/Math.Abs(dx),deltaZ=sz==0?double.PositiveInfinity:Cell/Math.Abs(dz),tx=sx==0?double.PositiveInfinity:((sx>0?x+1:x)*Cell-a.x)/dx,tz=sz==0?double.PositiveInfinity:((sz>0?z+1:z)*Cell-a.z)/dz;
                Add(x,z,segment);while(x!=endX||z!=endZ){if(tx<tz){x+=sx;tx+=deltaX;}else if(tz<tx){z+=sz;tz+=deltaZ;}else{if(x!=endX)Add(x+sx,z,segment);if(z!=endZ)Add(x,z+sz,segment);x+=sx;z+=sz;tx+=deltaX;tz+=deltaZ;}Add(x,z,segment);}
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
                    var road=segment.road;if(road.IsConnection&&!connections)continue;double lo=Math.Max(road.StartDistance,road.Geometry.At(segment.sample-1)),hi=Math.Min(road.EndDistance,road.Geometry.At(segment.sample));if(hi<lo)continue;Point a=road.Geometry.Sample(lo),b=road.Geometry.Sample(hi);double dx=b.x-a.x,dy=b.y-a.y,dz=b.z-a.z,den=dx*dx+dy*dy+dz*dz,t=den<1e-18?0:Math.Max(0,Math.Min(1,((p.x-a.x)*dx+(p.y-a.y)*dy+(p.z-a.z)*dz)/den));Point q=Point.Lerp(a,b,t);double gap=Point.Distance(p,q);if(gap>radius)continue;
                    var hit=new SpatialHit(road.Id,lo+Point.Distance(a,q),gap,q);int index=-1;for(int i=0;i<output.Count;i++)if(output[i].drivable==road.Id){index=i;break;}if(index<0)output.Add(hit);else if(gap<output[index].separation)output[index]=hit;
                }
            }
            output.Sort(Order.Instance);
        }
    }
}
