using System;
using System.Collections.Generic;

namespace HarborCity.RoadCore
{
    [Serializable] public struct Point
    {
        public double x,y,z;
        public Point(double x,double y,double z){this.x=x;this.y=y;this.z=z;}
        public static Point Lerp(Point a,Point b,double t)=>new Point(a.x+(b.x-a.x)*t,a.y+(b.y-a.y)*t,a.z+(b.z-a.z)*t);
        public static double Distance(Point a,Point b){double x=a.x-b.x,y=a.y-b.y,z=a.z-b.z;return Math.Sqrt(x*x+y*y+z*z);}
    }
    [Serializable] public sealed class Junction { public int id; public Point position; }
    [Serializable] public sealed class PhysicalRoad
    {
        public int id,from,to;
        public bool oneWay;
        public double speed=12;
        public Point[] points;
        public Point startTangent,endTangent;
    }
    [Serializable] public sealed class NetworkData
    {
        public int nextJunction=1,nextRoad=1;
        public List<Junction> junctions=new List<Junction>();
        public List<PhysicalRoad> roads=new List<PhysicalRoad>();
    }
    // Shared simulation geometry. Sampling density never changes routing vertex count.
    public sealed class Geometry
    {
        readonly Point[] points; readonly double[] cumulative;
        public double Length=>cumulative[cumulative.Length-1];
        public int Count=>points.Length;
        public Point this[int i]=>points[i];
        public double At(int i)=>cumulative[i];
        public Geometry(Point[] source)
        {
            if(source==null||source.Length<2)throw new ArgumentException("A centerline needs two points");
            points=(Point[])source.Clone();cumulative=new double[points.Length];
            for(int i=1;i<points.Length;i++){cumulative[i]=cumulative[i-1]+Point.Distance(points[i-1],points[i]);if(double.IsInfinity(cumulative[i])||double.IsNaN(cumulative[i]))throw new ArgumentException("Nonfinite geometry");}
        }
        public Point Sample(double distance)
        {
            if(distance<=0)return points[0];if(distance>=Length)return points[points.Length-1];
            int lo=0,hi=points.Length-1;while(hi-lo>1){int m=(lo+hi)/2;if(cumulative[m]<=distance)lo=m;else hi=m;}
            return Point.Lerp(points[lo],points[hi],(distance-cumulative[lo])/(cumulative[hi]-cumulative[lo]));
        }
    }
    public sealed class Drivable
    {
        public readonly long Id;
        public readonly int RoadId,From,To,JunctionId;
        public readonly bool Reverse;
        public readonly double Speed;
        public readonly Geometry Geometry;
        internal Drivable(long id,int road,int from,int to,int junction,bool reverse,double speed,Point[] points)
        {Id=id;RoadId=road;From=from;To=to;JunctionId=junction;Reverse=reverse;Speed=speed;Geometry=new Geometry(points);}
        public bool IsConnection=>JunctionId!=0;
        public int LaneIndex=>IsConnection?-1:(int)((Id>>1)&31);
    }
    public sealed class Network
    {
        readonly NetworkData data;
        readonly Dictionary<long,Drivable> drivables=new Dictionary<long,Drivable>(CityRoadKeyComparer.Instance);
        readonly Dictionary<int,List<Drivable>> outgoing=new Dictionary<int,List<Drivable>>();
        readonly HashSet<int> conflictJunctions=new HashSet<int>();
        public IEnumerable<Drivable> Drivables=>drivables.Values;
        public int RoadCount=>data.roads.Count;
        public int JunctionCount=>data.junctions.Count;
        public Network(NetworkData data){this.data=data??throw new ArgumentNullException(nameof(data));Rebuild();}
        public Drivable Get(long id){Drivable d;if(!drivables.TryGetValue(id,out d))throw new InvalidOperationException("Missing drivable "+id);return d;}
        public bool Contains(long id)=>drivables.ContainsKey(id);
        public bool IsConflictJunction(int id)=>conflictJunctions.Contains(id);
        public const int MaximumRoadId=int.MaxValue>>6;
        public static long LaneId(int road,bool reverse,int index=0)
        {if(road<=0||road>MaximumRoadId||index<0||index>31)throw new ArgumentOutOfRangeException("Lane identity");return ((long)road<<6)|((long)index<<1)|(reverse?1L:0L);}
        public static long ConnectionId(long a,long b)
        {if(a<=0||a>int.MaxValue||b<=0||b>int.MaxValue)throw new ArgumentOutOfRangeException("Connection identity");return -((a<<32)|(uint)b)-1;}
        public static Point Tangent(Point a,Point b)=>new Point(b.x-a.x,b.y-a.y,b.z-a.z);
        public static Point Offset(Point p,Point tangent,double offset)
        {double l=Math.Sqrt(tangent.x*tangent.x+tangent.z*tangent.z);if(l<1e-9)return p;return new Point(p.x+tangent.z/l*offset,p.y,p.z-tangent.x/l*offset);}
        void Rebuild()
        {
            var junctions=new Dictionary<int,Junction>();int maxJ=0,maxR=0;
            foreach(var j in data.junctions){if(j.id<=0||junctions.ContainsKey(j.id)||!Finite(j.position))throw new ArgumentException("Duplicate/invalid junction");junctions.Add(j.id,j);maxJ=Math.Max(maxJ,j.id);}
            var roads=new HashSet<int>();
            foreach(var r in data.roads)
            {
                if(r.id<=0||r.id>MaximumRoadId||!roads.Add(r.id)||!junctions.ContainsKey(r.from)||!junctions.ContainsKey(r.to)||r.from==r.to||r.speed<=0||double.IsNaN(r.speed)||double.IsInfinity(r.speed))throw new ArgumentException("Invalid road identity/direction/speed");
                var g=new Geometry(r.points);if(g.Length<.1||!Finite(r.startTangent)||!Finite(r.endTangent)||Point.Distance(r.points[0],junctions[r.from].position)>1e-6||Point.Distance(r.points[r.points.Length-1],junctions[r.to].position)>1e-6)throw new ArgumentException("Road endpoints/tangents disagree with junctions");
                for(int i=0;i<r.points.Length;i++)if(!Finite(r.points[i])||(i>0&&Point.Distance(r.points[i-1],r.points[i])<1e-8))throw new ArgumentException("Invalid geometry sample");
                maxR=Math.Max(maxR,r.id);AddLane(r,false);if(!r.oneWay)AddLane(r,true);
            }
            if(data.nextRoad<=maxR||data.nextJunction<=maxJ)throw new ArgumentException("Identity allocator is behind live entities");
            var approaches=new Dictionary<int,List<Point>>();
            foreach(var road in data.roads)
            {
                Point start=road.startTangent,end=road.endTangent;
                if(Point.Distance(start,new Point())<1e-9)start=Tangent(road.points[0],road.points[1]);
                if(Point.Distance(end,new Point())<1e-9)end=Tangent(road.points[road.points.Length-2],road.points[road.points.Length-1]);
                List<Point> list;if(!approaches.TryGetValue(road.from,out list))approaches.Add(road.from,list=new List<Point>());list.Add(start);
                if(!approaches.TryGetValue(road.to,out list))approaches.Add(road.to,list=new List<Point>());list.Add(new Point(-end.x,-end.y,-end.z));
            }
            foreach(var pair in approaches)
            {
                if(pair.Value.Count>2){conflictJunctions.Add(pair.Key);continue;}
                if(pair.Value.Count!=2)continue;Point p=pair.Value[0],q=pair.Value[1];double product=Point.Distance(p,new Point())*Point.Distance(q,new Point());
                if(product<1e-9||(p.x*q.x+p.y*q.y+p.z*q.z)/product>-.98)conflictJunctions.Add(pair.Key);
            }
            var lanes=new List<Drivable>(drivables.Values);
            foreach(var a in lanes)
            {
                List<Drivable> exits;if(!outgoing.TryGetValue(a.To,out exits))continue;
                foreach(var b in exits)
                {
                    if(a.RoadId==b.RoadId)continue; // First batch excludes U-turns.
                    Point p=a.Geometry.Sample(a.Geometry.Length),q=b.Geometry.Sample(0),c=junctions[a.To].position;
                    var samples=Point.Distance(p,q)<1e-8?new[]{p,q}:new Point[9];
                    if(samples.Length>2)for(int i=0;i<9;i++){double t=i/8.0;samples[i]=Point.Lerp(Point.Lerp(p,c,t),Point.Lerp(c,q,t),t);}
                    long id=ConnectionId(a.Id,b.Id);drivables.Add(id,new Drivable(id,0,a.To,a.To,a.To,false,Math.Min(a.Speed,b.Speed),samples));
                }
            }
        }
        static bool Finite(Point p)=>!(double.IsNaN(p.x)||double.IsNaN(p.y)||double.IsNaN(p.z)||double.IsInfinity(p.x)||double.IsInfinity(p.y)||double.IsInfinity(p.z));
        void AddLane(PhysicalRoad road,bool reverse)
        {
            int n=road.points.Length;var p=new Point[n];
            for(int i=0;i<n;i++)
            {
                Point tangent=i==0?road.startTangent:i==n-1?road.endTangent:Tangent(road.points[i-1],road.points[i+1]);
                if(Point.Distance(tangent,new Point())<1e-9)tangent=Tangent(road.points[Math.Max(0,i-1)],road.points[Math.Min(n-1,i+1)]);
                p[reverse?n-1-i:i]=Offset(road.points[i],tangent,reverse?-.55:.55);
            }
            var d=new Drivable(LaneId(road.id,reverse),road.id,reverse?road.to:road.from,reverse?road.from:road.to,0,reverse,road.speed,p);
            drivables.Add(d.Id,d);List<Drivable> list;if(!outgoing.TryGetValue(d.From,out list))outgoing.Add(d.From,list=new List<Drivable>());list.Add(d);
        }
        sealed class RouteQueue
        {
            internal struct Entry { public long id;public double cost; }
            readonly List<Entry> entries=new List<Entry>();
            public int Count=>entries.Count;
            static bool Before(Entry a,Entry b)=>a.cost<b.cost||(a.cost==b.cost&&a.id<b.id);
            public void Push(long id,double cost)
            {
                var item=new Entry{id=id,cost=cost};int i=entries.Count;entries.Add(item);
                while(i>0){int parent=(i-1)/2;if(!Before(item,entries[parent]))break;entries[i]=entries[parent];i=parent;}entries[i]=item;
            }
            public Entry Pop()
            {
                var result=entries[0];var item=entries[entries.Count-1];entries.RemoveAt(entries.Count-1);if(entries.Count==0)return result;
                int i=0;while(i*2+1<entries.Count){int child=i*2+1;if(child+1<entries.Count&&Before(entries[child+1],entries[child]))child++;if(!Before(entries[child],item))break;entries[i]=entries[child];i=child;}entries[i]=item;return result;
            }
        }
        // Dijkstra over lane ends with a heap: O((L+C) log L), independent of sample count.
        // No route scans or per-vehicle searches in Step; road edits validate references once.
        public List<long> Route(int from,int to)
        {
            if(from==to)return new List<long>();
            var cost=new Dictionary<long,double>(CityRoadKeyComparer.Instance);
            var parent=new Dictionary<long,long>(CityRoadKeyComparer.Instance);
            var open=new RouteQueue();var closed=new HashSet<long>(CityRoadKeyComparer.Instance);
            List<Drivable> starts;if(!outgoing.TryGetValue(from,out starts))return null;
            foreach(var d in starts){cost[d.Id]=d.Geometry.Length/d.Speed;parent[d.Id]=0;open.Push(d.Id,cost[d.Id]);}
            while(open.Count>0)
            {
                var entry=open.Pop();long id=entry.id;if(entry.cost!=cost[id]||!closed.Add(id))continue;var a=Get(id);
                if(a.To==to)
                {
                    var lanes=new List<long>();for(long v=id;v!=0;v=parent[v])lanes.Add(v);lanes.Reverse();
                    var route=new List<long>(lanes.Count*2-1);for(int i=0;i<lanes.Count;i++){if(i>0)route.Add(ConnectionId(lanes[i-1],lanes[i]));route.Add(lanes[i]);}return route;
                }
                List<Drivable> next;if(!outgoing.TryGetValue(a.To,out next))continue;
                foreach(var b in next){long link=ConnectionId(id,b.Id);if(!Contains(link)||closed.Contains(b.Id))continue;double c=cost[id]+Get(link).Geometry.Length/Get(link).Speed+b.Geometry.Length/b.Speed;double old;if(!cost.TryGetValue(b.Id,out old)||c<old){cost[b.Id]=c;parent[b.Id]=id;open.Push(b.Id,c);}}
            }
            return null;
        }
        public void ValidateRoute(List<long> route)
        {
            if(route==null||route.Count==0||Get(route[0]).IsConnection||Get(route[route.Count-1]).IsConnection)throw new ArgumentException("Invalid route ends");
            for(int i=0;i<route.Count;i++)
            {
                var d=Get(route[i]);if((i%2==1)!=d.IsConnection)throw new ArgumentException("Route must alternate lanes and connections");
                if(i%2==1&&route[i]!=ConnectionId(route[i-1],route[i+1]))throw new ArgumentException("Disconnected route");
            }
        }
    }
}
