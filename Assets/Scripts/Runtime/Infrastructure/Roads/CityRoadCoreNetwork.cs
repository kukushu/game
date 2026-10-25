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
    public enum RoadDirection { Both,Forward,Backward }
    public readonly struct RoadSettings
    {
        public readonly int id,from,to,lanes;public readonly double speed;public readonly RoadDirection direction;
        internal RoadSettings(PhysicalRoad road){id=road.id;from=road.from;to=road.to;lanes=road.lanes;speed=road.speed;direction=road.direction;}
    }
    [Serializable] public sealed class PhysicalRoad
    {
        public int id,from,to,lanes=1;
        public RoadDirection direction;
        public double speed=12;
        public Point[] points;
        public Point[] rightVectors;
        public Point startTangent,endTangent;
    }
    [Serializable] public sealed partial class NetworkData
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
        internal bool Matches(Point[] source)
        {
            if(source==null||source.Length!=points.Length)return false;
            for(int i=0;i<source.Length;i++)if(!Same(source[i],points[i]))return false;return true;
        }
        internal static bool Same(Point a,Point b)=>BitConverter.DoubleToInt64Bits(a.x)==BitConverter.DoubleToInt64Bits(b.x)&&BitConverter.DoubleToInt64Bits(a.y)==BitConverter.DoubleToInt64Bits(b.y)&&BitConverter.DoubleToInt64Bits(a.z)==BitConverter.DoubleToInt64Bits(b.z);
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
        public Point DirectionAt(double distance)
        {
            int lo=0,hi=points.Length-1;while(hi-lo>1){int mid=(lo+hi)/2;if(cumulative[mid]<=distance)lo=mid;else hi=mid;}var d=Network.Tangent(points[lo],points[hi]);double length=Point.Distance(d,new Point());return length<1e-12?new Point():new Point(d.x/length,d.y/length,d.z/length);
        }
        public Point[] Slice(double start,double end)
        {
            if(start<0||end>Length||end<start)throw new ArgumentException("Invalid geometry range");var result=new List<Point>{Sample(start)};for(int i=1;i<Count-1;i++)if(At(i)>start&&At(i)<end)result.Add(this[i]);result.Add(Sample(end));return result.ToArray();
        }
        public double MapDistance(Geometry target,double distance)
        {
            if(Count!=target.Count)throw new ArgumentException("Parallel geometry sample correspondence missing");
            if(distance<=0)return 0;if(distance>=Length)return target.Length;
            int lo=0,hi=Count-1;while(hi-lo>1){int m=(lo+hi)/2;if(cumulative[m]<=distance)lo=m;else hi=m;}
            double t=(distance-cumulative[lo])/(cumulative[hi]-cumulative[lo]);return target.At(lo)+(target.At(hi)-target.At(lo))*t;
        }
        public double MapReverseDistance(Geometry target,double distance)
        {
            if(Count!=target.Count)throw new ArgumentException("Opposite geometry sample correspondence missing");
            if(distance<=0)return target.Length;if(distance>=Length)return 0;
            int lo=0,hi=Count-1;while(hi-lo>1){int m=(lo+hi)/2;if(cumulative[m]<=distance)lo=m;else hi=m;}
            double t=(distance-cumulative[lo])/(cumulative[hi]-cumulative[lo]);return target.At(Count-1-lo)+(target.At(Count-1-hi)-target.At(Count-1-lo))*t;
        }
    }
    public abstract class Drivable
    {
        public readonly long Id;
        public readonly int RoadId,From,To,JunctionId;
        public readonly bool Reverse;
        public readonly double Speed;
        public readonly Geometry Geometry;
        public double StartDistance {get;internal set;}
        public double EndDistance {get;internal set;}
        public double TravelLength=>EndDistance-StartDistance;
        public double ProtectedClearance {get;internal set;}
        public int ParallelLaneCount {get;internal set;}
        internal Drivable(long id,int road,int from,int to,int junction,bool reverse,double speed,Point[] points)
            :this(id,road,from,to,junction,reverse,speed,new Geometry(points)){}
        internal Drivable(long id,int road,int from,int to,int junction,bool reverse,double speed,Geometry geometry)
        {Id=id;RoadId=road;From=from;To=to;JunctionId=junction;Reverse=reverse;Speed=speed;Geometry=geometry;EndDistance=Geometry.Length;ProtectedClearance=Math.Min(2,Geometry.Length/3);}
        public bool IsConnection=>JunctionId!=0;
        public int LaneIndex=>IsConnection?-1:(int)((Id>>1)&31);
    }
    public sealed partial class Network
    {
        readonly NetworkData data;
        readonly Dictionary<long,Drivable> drivables=new Dictionary<long,Drivable>(CityRoadKeyComparer.Instance);
        readonly Dictionary<int,List<Drivable>> outgoing=new Dictionary<int,List<Drivable>>();
        readonly HashSet<int> conflictJunctions=new HashSet<int>();
        readonly Dictionary<int,int> laneCounts=new Dictionary<int,int>();
        readonly Dictionary<int,double> junctionRadii=new Dictionary<int,double>();
        readonly Dictionary<int,Geometry> centrelines=new Dictionary<int,Geometry>();
        readonly Dictionary<int,RoadSettings> settings=new Dictionary<int,RoadSettings>();
        public IEnumerable<int> RoadIds=>centrelines.Keys;
        public Geometry Centreline(int road)=>centrelines[road];
        ConnectionConflicts connectionConflicts;
        RoadSpatialIndex spatialIndex;
        RoadSpatialIndex spatialPrior;
        LaneReachability reachability;
        public bool HasLaneCycle(long lane)
        {if(!Contains(lane)||Get(lane).IsConnection)return false;if(reachability==null)reachability=new LaneReachability(this);return reachability.HasCycle(lane);}
        public bool CanReach(RoadAccess source,RoadAccess target)
        {
            if(source==null||target==null||!source.valid||!target.valid||!Contains(source.lane)||!Contains(target.lane)||Get(source.lane).IsConnection||Get(target.lane).IsConnection)return false;
            return CanReach(source.lane,source.distance,target.lane,target.distance);
        }
        public bool CanReach(long source,double sourceDistance,long target,double targetDistance)
        {
            if(!Contains(source)||!Contains(target))return false;var a=Get(source);var b=Get(target);if(a.IsConnection||b.IsConnection||sourceDistance<a.StartDistance||sourceDistance>a.EndDistance||targetDistance<b.StartDistance||targetDistance>b.EndDistance)return false;
            if(reachability==null)reachability=new LaneReachability(this);
            return reachability.CanReach(source,sourceDistance,target,targetDistance);
        }
        public IEnumerable<Drivable> Drivables=>drivables.Values;
        public int RoadCount=>data.roads.Count;
        public int JunctionCount=>data.junctions.Count;
        public bool ContainsRoad(int roadId)=>settings.ContainsKey(roadId);
        public RoadSettings Settings(int roadId){if(!settings.TryGetValue(roadId,out var road))throw new ArgumentException("Missing road");return road;}
        public bool HasParallelLanes {get;private set;}
        public Network(NetworkData data):this(data,null){}
        // Prior contributes immutable Geometry only. Mutable lanes/ports, indexes,
        // reachability, scratch and reservation caches belong to this candidate.
        internal Network(NetworkData data,Network prior){this.data=data??throw new ArgumentNullException(nameof(data));Rebuild(prior);spatialPrior=prior?.spatialIndex??prior?.spatialPrior;}
        public Drivable Get(long id){Drivable d;if(!drivables.TryGetValue(id,out d))throw new InvalidOperationException("Missing drivable "+id);return d;}
        public bool Contains(long id)=>drivables.ContainsKey(id);
        public bool IsConflictJunction(int id)=>conflictJunctions.Contains(id);
        public bool MovementsConflict(long a,long b)=>connectionConflicts.Contains(a,b);
        public void Project(Point point,double radius,bool includeConnections,List<SpatialHit> output)
        {if(spatialIndex==null){var index=new RoadSpatialIndex(this,spatialPrior);spatialIndex=index;spatialPrior=null;}spatialIndex.Project(point,radius,includeConnections,output);}
        public int LaneCount(int road)=>laneCounts[road];
        public double Clearance(Drivable lane)
        =>lane.ProtectedClearance;
        public const int MaximumRoadId=int.MaxValue>>6;
        public static long LaneId(int road,bool reverse,int index=0)
        {if(road<=0||road>MaximumRoadId||index<0||index>31)throw new ArgumentOutOfRangeException("Lane identity");return ((long)road<<6)|((long)index<<1)|(reverse?1L:0L);}
        public static long ConnectionId(long a,long b)
        {if(a<=0||a>int.MaxValue||b<=0||b>int.MaxValue)throw new ArgumentOutOfRangeException("Connection identity");return -((a<<32)|(uint)b)-1;}
        public static Point Tangent(Point a,Point b)=>new Point(b.x-a.x,b.y-a.y,b.z-a.z);
        public static Point Offset(Point p,Point tangent,double offset)
        {double l=Math.Sqrt(tangent.x*tangent.x+tangent.z*tangent.z);if(l<1e-9)return p;return new Point(p.x+tangent.z/l*offset,p.y,p.z-tangent.x/l*offset);}
        internal static Point[] RightVectors(PhysicalRoad road)
        {
            if(road.rightVectors!=null)return (Point[])road.rightVectors.Clone();int n=road.points.Length;var result=new Point[n];
            for(int i=0;i<n;i++){Point tangent=i==0?road.startTangent:i==n-1?road.endTangent:Tangent(road.points[i-1],road.points[i+1]);if(Point.Distance(tangent,new Point())<1e-9)tangent=Tangent(road.points[Math.Max(0,i-1)],road.points[Math.Min(n-1,i+1)]);result[i]=Offset(new Point(),tangent,1);}return result;
        }
        void Rebuild(Network prior)
        {
            var junctions=new Dictionary<int,Junction>();int maxJ=0,maxR=0;
            foreach(var j in data.junctions){if(j.id<=0||junctions.ContainsKey(j.id)||!Finite(j.position))throw new ArgumentException("Duplicate/invalid junction");junctions.Add(j.id,j);maxJ=Math.Max(maxJ,j.id);}
            var roads=new HashSet<int>();
            foreach(var r in data.roads)
            {
                if(r.id<=0||r.id>MaximumRoadId||!roads.Add(r.id)||!junctions.ContainsKey(r.from)||!junctions.ContainsKey(r.to)||r.from==r.to||r.direction<RoadDirection.Both||r.direction>RoadDirection.Backward||r.lanes<1||r.lanes>8||r.speed<=0||double.IsNaN(r.speed)||double.IsInfinity(r.speed))throw new ArgumentException("Invalid road identity/direction/speed/lanes");
                Geometry previous;var g=prior!=null&&prior.centrelines.TryGetValue(r.id,out previous)&&previous.Matches(r.points)?previous:new Geometry(r.points);if(g.Length<.1||!Finite(r.startTangent)||!Finite(r.endTangent)||Point.Distance(r.points[0],junctions[r.from].position)>1e-6||Point.Distance(r.points[r.points.Length-1],junctions[r.to].position)>1e-6)throw new ArgumentException("Road endpoints/tangents disagree with junctions");
                for(int i=0;i<r.points.Length;i++)if(!Finite(r.points[i])||(i>0&&Point.Distance(r.points[i-1],r.points[i])<1e-8))throw new ArgumentException("Invalid geometry sample");
                if(r.rightVectors!=null){if(r.rightVectors.Length!=r.points.Length)throw new ArgumentException("Cross-section count mismatch");foreach(var right in r.rightVectors)if(!Finite(right)||Math.Abs(right.y)>1e-9||Point.Distance(right,new Point())>1.000001)throw new ArgumentException("Invalid road cross-section");}
                centrelines.Add(r.id,g);settings.Add(r.id,new RoadSettings(r));
                maxR=Math.Max(maxR,r.id);laneCounts.Add(r.id,r.lanes);HasParallelLanes|=r.lanes>1;double radius=(r.lanes-.5)*1.1+1;double priorRadius;junctionRadii.TryGetValue(r.from,out priorRadius);junctionRadii[r.from]=Math.Max(priorRadius,radius);junctionRadii.TryGetValue(r.to,out priorRadius);junctionRadii[r.to]=Math.Max(priorRadius,radius);for(int lane=0;lane<r.lanes;lane++){if(r.direction!=RoadDirection.Backward)AddLane(r,false,lane,prior);if(r.direction!=RoadDirection.Forward)AddLane(r,true,lane,prior);}
            }
            if(data.nextRoad<=maxR||data.nextJunction<=maxJ)throw new ArgumentException("Identity allocator is behind live entities");
            foreach(var lane in drivables.Values){double from,to;junctionRadii.TryGetValue(lane.From,out from);junctionRadii.TryGetValue(lane.To,out to);lane.ProtectedClearance=Math.Min(Math.Max(2,Math.Max(from,to)),lane.Geometry.Length/3);}
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
            var ports=new HashSet<int>(conflictJunctions);var widths=new Dictionary<int,int>();foreach(var road in data.roads)foreach(int junction in new[]{road.from,road.to}){if(widths.TryGetValue(junction,out int width)&&width!=road.lanes)ports.Add(junction);else widths[junction]=road.lanes;}
            var lanes=new List<Drivable>(drivables.Values);foreach(Lane lane in lanes)lane.ConfigurePorts(ports.Contains(lane.From),ports.Contains(lane.To));
            foreach(var a in lanes)
            {
                List<Drivable> exits;if(!outgoing.TryGetValue(a.To,out exits))continue;
                foreach(var b in exits)
                {
                    if(a.RoadId==b.RoadId)continue; // First batch excludes U-turns.
                    // A continuous geometry cut cannot silently change parallel lanes.
                    // Real intersections may connect different lane indices; explicit
                    // turn restrictions and lane-change intentions are separate rules.
                    if(!IsConflictJunction(a.To)&&b.LaneIndex!=Math.Min(a.LaneIndex,LaneCount(b.RoadId)-1))continue;
                    long id=ConnectionId(a.Id,b.Id);Drivable previous;LaneConnection old=prior!=null&&prior.drivables.TryGetValue(id,out previous)?previous as LaneConnection:null;drivables.Add(id,new LaneConnection(id,(Lane)a,(Lane)b,old));
                }
            }
            connectionConflicts=new ConnectionConflicts(this);
            foreach(var d in drivables.Values)if(d.IsConnection&&!conflictJunctions.Contains(d.JunctionId)&&connectionConflicts.HasOther(d.Id))conflictJunctions.Add(d.JunctionId);
        }
        static bool Finite(Point p)=>!(double.IsNaN(p.x)||double.IsNaN(p.y)||double.IsNaN(p.z)||double.IsInfinity(p.x)||double.IsInfinity(p.y)||double.IsInfinity(p.z));
        void AddLane(PhysicalRoad road,bool reverse,int index,Network prior)
        {
            int n=road.points.Length;var p=new Point[n];var right=RightVectors(road);
            for(int i=0;i<n;i++)
            {
                double offset=(.55+index*1.1)*(reverse?-1:1);p[reverse?n-1-i:i]=new Point(road.points[i].x+right[i].x*offset,road.points[i].y,road.points[i].z+right[i].z*offset);
            }
            long id=LaneId(road.id,reverse,index);Drivable previous;Geometry geometry=prior!=null&&prior.drivables.TryGetValue(id,out previous)&&previous.Geometry.Matches(p)?previous.Geometry:new Geometry(p);
            var d=new Lane(id,road.id,reverse?road.to:road.from,reverse?road.from:road.to,reverse,road.speed,geometry);
            d.ParallelLaneCount=road.lanes;
            drivables.Add(d.Id,d);List<Drivable> list;if(!outgoing.TryGetValue(d.From,out list))outgoing.Add(d.From,list=new List<Drivable>());list.Add(d);
        }
        internal sealed class RouteQueue
        {
            internal struct Entry { public long id;public double cost; }
            readonly List<Entry> entries=new List<Entry>();
            public int Count=>entries.Count;
            internal void Clear()=>entries.Clear();
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
        // Derived scratch belongs to this immutable Network, not a route or actor.
        // Current routing is synchronous/single-threaded. Results never borrow scratch.
        sealed class RouteScratch
        {
            internal readonly RouteQueue queue=new RouteQueue();
            internal readonly Dictionary<long,double> costs=new Dictionary<long,double>(CityRoadKeyComparer.Instance);
            internal readonly Dictionary<long,long> parents=new Dictionary<long,long>(CityRoadKeyComparer.Instance);
            internal readonly HashSet<long> settled=new HashSet<long>(CityRoadKeyComparer.Instance);
            bool busy;
            internal readonly struct Lease:IDisposable
            {readonly RouteScratch owner;internal Lease(RouteScratch owner){this.owner=owner;}public void Dispose(){owner.busy=false;}}
            internal Lease Acquire()
            {
                if(busy)throw new InvalidOperationException("Reentrant network route search");
                queue.Clear();costs.Clear();parents.Clear();settled.Clear();busy=true;return new Lease(this);
            }
        }
        RouteScratch accessSearch;
        // Dijkstra over lane ends with a heap: O((L+C) log L), independent of sample count.
        // No route scans or per-vehicle searches in Step; road edits validate references once.
        public List<long> Route(int from,int to)
        {
            if(from==to)return new List<long>();
            var cost=new Dictionary<long,double>(CityRoadKeyComparer.Instance);
            var parent=new Dictionary<long,long>(CityRoadKeyComparer.Instance);
            var open=new RouteQueue();var closed=new HashSet<long>(CityRoadKeyComparer.Instance);
            List<Drivable> starts;if(!outgoing.TryGetValue(from,out starts))return null;
            foreach(var d in starts){cost[d.Id]=d.TravelLength/d.Speed;parent[d.Id]=0;open.Push(d.Id,cost[d.Id]);}
            while(open.Count>0)
            {
                var entry=open.Pop();long id=entry.id;if(entry.cost!=cost[id]||!closed.Add(id))continue;var a=Get(id);
                if(a.To==to)
                {
                    var lanes=new List<long>();for(long v=id;v!=0;v=parent[v])lanes.Add(v);lanes.Reverse();
                    var route=new List<long>(lanes.Count*2-1);for(int i=0;i<lanes.Count;i++){if(i>0)route.Add(ConnectionId(lanes[i-1],lanes[i]));route.Add(lanes[i]);}return route;
                }
                List<Drivable> next;if(!outgoing.TryGetValue(a.To,out next))continue;
                foreach(var b in next){long link=ConnectionId(id,b.Id);if(!Contains(link)||closed.Contains(b.Id))continue;double c=cost[id]+Get(link).Geometry.Length/Get(link).Speed+b.TravelLength/b.Speed;double old;if(!cost.TryGetValue(b.Id,out old)||c<old){cost[b.Id]=c;parent[b.Id]=id;open.Push(b.Id,c);}}
            }
            return null;
        }
        public List<long> Route(RoadAccess start,RoadAccess goal)
        {int selected;return RouteToEither(start,goal,null,out selected);}
        // A single source, two terminal access directions. Keep canonical queue
        // and parent ordering; ties prefer the original first goal.
        internal List<long> RouteToEither(RoadAccess start,RoadAccess goal,RoadAccess other,out int selected)
        {
#if CITY_CORE_ATTRIBUTION
            using var observation=CityCoreProfile.Measure(CityCoreProfile.Part.RouteSearch);
#endif
            using var routeObservation=DepotRoutingTrace.Start("original-dijkstra");
            if(DepotJunctionRouting.CountSearches)DepotJunctionRouting.SearchCalls++;
            selected=-1;var first=Get(start.lane);Get(goal.lane);
            if(!start.valid||first.IsConnection||start.distance<first.StartDistance||start.distance>first.EndDistance)return null;
            RoadAccess a=ValidCostAccess(goal)?goal:null,b=other!=null&&ValidCostAccess(other)?other:null;
            if(a==null&&b==null)return null;
            // Any other path must first reach the end of this source lane,
            // which cannot beat a legal forward arrival on that same lane.
            double directCost=double.PositiveInfinity;
            if(a!=null&&first.Id==a.lane&&a.distance>=start.distance){directCost=0.0+(a.distance-start.distance)/first.Speed;selected=0;}
            if(b!=null&&first.Id==b.lane&&b.distance>=start.distance){double cost=0.0+(b.distance-start.distance)/first.Speed;if(cost<directCost){directCost=cost;selected=1;}}
            if(selected>=0)return new List<long>{first.Id};
            var scratch=accessSearch??(accessSearch=new RouteScratch());
            using(scratch.Acquire())
            {
                var queue=scratch.queue;var costs=scratch.costs;var parents=scratch.parents;var settled=scratch.settled;
                double best=double.PositiveInfinity;long beforeGoal=0;
                double initial=(first.EndDistance-start.distance)/first.Speed;costs[first.Id]=initial;parents[first.Id]=0;queue.Push(first.Id,initial);
                while(queue.Count>0)
                {
                    var item=queue.Pop();if(item.cost>best)break;if(item.cost!=costs[item.id]||!settled.Add(item.id))continue;
                    if(routeObservation!=null)routeObservation.expanded++;
                    if(DepotJunctionRouting.CountSearches)DepotJunctionRouting.SearchExpanded++;
                    var current=Get(item.id);List<Drivable> next;if(!outgoing.TryGetValue(current.To,out next))continue;
                    foreach(var lane in next)
                    {
                        // Reuse the same immutable connection for both cost fields.
                        long link=ConnectionId(current.Id,lane.Id);Drivable connection;if(!drivables.TryGetValue(link,out connection))continue;double prefix=item.cost+connection.Geometry.Length/connection.Speed;bool terminal=false;
                        if(a!=null&&lane.Id==a.lane){double cost=prefix+(a.distance-lane.StartDistance)/lane.Speed;if(cost<best||cost==best&&selected>0){best=cost;selected=0;beforeGoal=current.Id;}terminal=true;}
                        if(b!=null&&lane.Id==b.lane){double cost=prefix+(b.distance-lane.StartDistance)/lane.Speed;if(cost<best){best=cost;selected=1;beforeGoal=current.Id;}terminal=true;}
                        // With one goal this is exactly the original terminal rule.
                        // With two goals, allow traversal at the same cost so a
                        // zero-length boundary cannot hide the earlier-ranked goal.
                        if(terminal&&(a==null||b==null))continue;
                        if(settled.Contains(lane.Id))continue;double candidate=prefix+lane.TravelLength/lane.Speed,old;
                        if(!costs.TryGetValue(lane.Id,out old)||candidate<old){costs[lane.Id]=candidate;parents[lane.Id]=current.Id;queue.Push(lane.Id,candidate);}
                    }
                }
                if(selected<0)return null;
                var lanes=new List<long>();for(long id=beforeGoal;id!=0;id=parents[id])lanes.Add(id);lanes.Reverse();lanes.Add((selected==0?a:b).lane);
                var result=new List<long>();for(int i=0;i<lanes.Count;i++){if(i>0)result.Add(ConnectionId(lanes[i-1],lanes[i]));result.Add(lanes[i]);}return result;
            }
        }
        // Static free-flow scalar only. Two legal source/goal directions share one
        // frontier; no vehicle plan or route reconstruction is needed for a quote.
        internal double MinimumTravelSeconds(RoadAccess source,RoadAccess goal,RoadAccess alternateSource=null,RoadAccess alternateGoal=null)
        {
#if CITY_CORE_ATTRIBUTION
            using var observation=CityCoreProfile.Measure(CityCoreProfile.Part.CostSearch);
#endif
            if(!ValidCostAccess(source))source=null;if(!ValidCostAccess(goal))goal=null;
            if(!ValidCostAccess(alternateSource))alternateSource=null;if(!ValidCostAccess(alternateGoal))alternateGoal=null;
            if(source==null&&alternateSource==null||goal==null&&alternateGoal==null)return double.PositiveInfinity;
            var scratch=accessSearch??(accessSearch=new RouteScratch());
            using(scratch.Acquire())
            {
                double best=double.PositiveInfinity;SeedCost(scratch,source,goal,alternateGoal,ref best);SeedCost(scratch,alternateSource,goal,alternateGoal,ref best);
                while(scratch.queue.Count>0)
                {
                    var item=scratch.queue.Pop();if(item.cost>best)break;if(item.cost!=scratch.costs[item.id]||!scratch.settled.Add(item.id))continue;
                    if(DepotJunctionRouting.CountSearches)DepotJunctionRouting.SearchExpanded++;
                    var current=Get(item.id);List<Drivable> next;if(!outgoing.TryGetValue(current.To,out next))continue;
                    foreach(var lane in next)
                    {
                        long link=ConnectionId(current.Id,lane.Id);if(!Contains(link))continue;
                        double prefix=item.cost+Get(link).Geometry.Length/Get(link).Speed;bool terminal=false;
                        if(goal!=null&&lane.Id==goal.lane){double candidate=prefix+(goal.distance-lane.StartDistance)/lane.Speed;if(candidate<best)best=candidate;terminal=true;}
                        if(alternateGoal!=null&&lane.Id==alternateGoal.lane){double candidate=prefix+(alternateGoal.distance-lane.StartDistance)/lane.Speed;if(candidate<best)best=candidate;terminal=true;}
                        // Any continuation through a goal costs at least its valid
                        // partial-lane arrival. It cannot improve the minimum quote.
                        if(terminal||scratch.settled.Contains(lane.Id))continue;
                        double cost=prefix+lane.TravelLength/lane.Speed,old;
                        if(!scratch.costs.TryGetValue(lane.Id,out old)||cost<old){scratch.costs[lane.Id]=cost;scratch.queue.Push(lane.Id,cost);}
                    }
                }
                return best;
            }
        }
        bool ValidCostAccess(RoadAccess access)
        {
            if(access==null||!access.valid)return false;var d=Get(access.lane);
            return !d.IsConnection&&access.distance>=d.StartDistance&&access.distance<=d.EndDistance;
        }
        void SeedCost(RouteScratch scratch,RoadAccess start,RoadAccess goal,RoadAccess otherGoal,ref double best)
        {
            if(start==null)return;var lane=Get(start.lane);
            if(goal!=null&&start.lane==goal.lane&&goal.distance>=start.distance){double direct=0.0+(goal.distance-start.distance)/lane.Speed;if(direct<best)best=direct;}
            if(otherGoal!=null&&start.lane==otherGoal.lane&&otherGoal.distance>=start.distance){double direct=0.0+(otherGoal.distance-start.distance)/lane.Speed;if(direct<best)best=direct;}
            double initial=(lane.EndDistance-start.distance)/lane.Speed,prior;
            if(!scratch.costs.TryGetValue(lane.Id,out prior)||initial<prior){scratch.costs[lane.Id]=initial;scratch.queue.Push(lane.Id,initial);}
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
