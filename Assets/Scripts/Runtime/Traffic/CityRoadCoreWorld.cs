using System;
using System.Collections.Generic;
#if UNITY_5_3_OR_NEWER
using Unity.Profiling;
#endif

namespace HarborCity.RoadCore
{
    public enum VehicleStatus { Active,Delivered,Stranded }
    [Serializable] public sealed class Depot
    {
        public int id {get;internal set;} public int junction {get;internal set;}
        public int stock {get;internal set;} public int capacity {get;internal set;}=100000;
        public double cash {get;internal set;}=100000;
    }
    [Serializable] public sealed class Vehicle
    {
        public int id {get;internal set;} public int origin {get;internal set;} public int destination {get;internal set;}
        public int cargo {get;internal set;}=1;
        public double price {get;internal set;}=5; public double distance {get;internal set;}
        internal List<long> route;
        List<long> exposedRouteOwner;IReadOnlyList<long> exposedRoute;
        public IReadOnlyList<long> Route {get {if(!ReferenceEquals(exposedRouteOwner,route)){exposedRouteOwner=route;exposedRoute=route.AsReadOnly();}return exposedRoute;}}
        public int cursor {get;internal set;}
        public VehicleStatus status {get;internal set;}
        public Point strandedPosition {get;internal set;}
        public long Current=>route[cursor];
    }
    [Serializable] public sealed class WorldData
    {
        public int coreFormat=1,nextVehicle=1,nextDepot=1;
        public double clock,accumulator;
        public int delivered;
        public NetworkData network=new NetworkData();
        public List<Depot> depots=new List<Depot>();
        public List<Vehicle> vehicles=new List<Vehicle>();
    }
    public sealed class World
    {
        public const double StepSeconds=.05,MinimumSpacing=1.5;
        readonly WorldData data;
        public Network Network {get;private set;}
        readonly IReadOnlyList<Vehicle> vehicleView;readonly IReadOnlyList<Depot> depotView;
        public IEnumerable<Vehicle> Vehicles=>vehicleView;
        public IEnumerable<Depot> Depots=>depotView;
        public double Clock=>data.clock;
        public int Delivered=>data.delivered;
        public int ActiveCount {get;private set;}
        public int NetworkGeneration {get;private set;}
        readonly Dictionary<long,List<Vehicle>> occupancy=new Dictionary<long,List<Vehicle>>(CityRoadKeyComparer.Instance);
        readonly Dictionary<int,int> locks=new Dictionary<int,int>();
        readonly Dictionary<int,Depot> depotsById=new Dictionary<int,Depot>();
        readonly Dictionary<long,double> entries=new Dictionary<long,double>(CityRoadKeyComparer.Instance);
        readonly List<Intent> intents=new List<Intent>();
        struct Intent { public Vehicle vehicle;public int cursor;public double distance,limit; }
        struct Remap { public Vehicle vehicle;public List<long> route;public int cursor;public double distance; }
        sealed class IntentOrder:IComparer<Intent>
        {public static readonly IntentOrder Instance=new IntentOrder();public int Compare(Intent a,Intent b)=>a.vehicle.id.CompareTo(b.vehicle.id);}
        sealed class OccupantOrder:IComparer<Vehicle>
        {public static readonly OccupantOrder Instance=new OccupantOrder();public int Compare(Vehicle a,Vehicle b){int c=b.distance.CompareTo(a.distance);return c!=0?c:a.id.CompareTo(b.id);}}
#if UNITY_5_3_OR_NEWER
        static readonly ProfilerMarker stepSample=new ProfilerMarker("City.RoadCore.Step"),indexSample=new ProfilerMarker("City.RoadCore.Occupancy"),intentSample=new ProfilerMarker("City.RoadCore.IntentAndReservations"),commitSample=new ProfilerMarker("City.RoadCore.CommitAndSettlement");
#endif
        public World():this(new WorldData()){}
        public World(WorldData state)
        {
            if(state==null||state.coreFormat!=1)throw new ArgumentException("Unsupported road-core save. Legacy city saves are not supported.");
            data=state; // Take ownership of a private copy, never the caller's mutable save DTO.
            data=Snapshot();vehicleView=data.vehicles.AsReadOnly();depotView=data.depots.AsReadOnly();foreach(var depot in data.depots)depotsById.Add(depot.id,depot);Network=new Network(data.network);Validate();AssertSafety();
        }
        // Only serializers may receive this copy; callers cannot mutate the live facts.
        public WorldData Snapshot()
        {
            var copy=new WorldData{coreFormat=1,nextVehicle=data.nextVehicle,nextDepot=data.nextDepot,clock=data.clock,accumulator=data.accumulator,delivered=data.delivered,network=CopyNetwork()};
            foreach(var d in data.depots)copy.depots.Add(new Depot{id=d.id,junction=d.junction,stock=d.stock,capacity=d.capacity,cash=d.cash});
            foreach(var v in data.vehicles)copy.vehicles.Add(new Vehicle{id=v.id,origin=v.origin,destination=v.destination,cargo=v.cargo,price=v.price,distance=v.distance,cursor=v.cursor,status=v.status,strandedPosition=v.strandedPosition,route=new List<long>(v.route)});
            return copy;
        }
        NetworkData CopyNetwork()
        {
            var n=new NetworkData{nextJunction=data.network.nextJunction,nextRoad=data.network.nextRoad};
            foreach(var j in data.network.junctions)n.junctions.Add(new Junction{id=j.id,position=j.position});
            foreach(var r in data.network.roads)n.roads.Add(new PhysicalRoad{id=r.id,from=r.from,to=r.to,oneWay=r.oneWay,speed=r.speed,points=(Point[])r.points.Clone(),startTangent=r.startTangent,endTangent=r.endTangent});return n;
        }
        public int AddJunction(Point point)
        {
            var n=CopyNetwork();int id=n.nextJunction++;n.junctions.Add(new Junction{id=id,position=point});var replacement=new Network(n);data.network=n;Network=replacement;NetworkGeneration++;return id;
        }
        public Point JunctionPosition(int id){foreach(var j in data.network.junctions)if(j.id==id)return j.position;throw new ArgumentException("Missing junction");}
        public int SnapJunction(Point p,double radius)
        {int best=0;foreach(var j in data.network.junctions){double d=Point.Distance(j.position,p);if(d<radius){radius=d;best=j.id;}}return best;}
        public int AddRoad(int from,int to,Point[] points,bool oneWay=false,double speed=12)
        {
            var n=CopyNetwork();int id=n.nextRoad++;n.roads.Add(new PhysicalRoad{id=id,from=from,to=to,points=(Point[])points.Clone(),oneWay=oneWay,speed=speed});
            var network=new Network(n);ValidateEditReservations(network,null);data.network=n;Network=network;NetworkGeneration++;occupancy.Clear();Reindex();return id;
        }
        public int AddDepot(int junction,int stock=100)
        {JunctionPosition(junction);if(stock<0||stock>100000)throw new ArgumentOutOfRangeException(nameof(stock));int id=data.nextDepot++;var depot=new Depot{id=id,junction=junction,stock=stock};data.depots.Add(depot);depotsById.Add(id,depot);return id;}
        public Depot GetDepot(int id){Depot depot;if(depotsById.TryGetValue(id,out depot))return depot;throw new ArgumentException("Missing depot");}
        public Vehicle Dispatch(int origin,int destination)
        {
            var a=GetDepot(origin);var b=GetDepot(destination);if(a.stock<1||a.id==b.id)return null;
            var route=Network.Route(a.junction,b.junction);if(route==null||route.Count==0)return null;
            Reindex();List<Vehicle> bucket;if(occupancy.TryGetValue(route[0],out bucket)&&bucket.Count>0&&bucket[bucket.Count-1].distance<MinimumSpacing)return null;
            var entry=Network.Get(route[0]);if(Network.IsConflictJunction(entry.From)&&locks.ContainsKey(entry.From))return null;
            var v=new Vehicle{id=data.nextVehicle++,origin=origin,destination=destination,route=route};data.vehicles.Add(v);a.stock--;ActiveCount++;return v;
        }
        public Point Position(Vehicle v)=>v.status==VehicleStatus.Stranded?v.strandedPosition:Network.Get(v.Current).Geometry.Sample(v.distance);
        static double Clearance(Drivable lane)=>Math.Min(2,lane.Geometry.Length/3);
        void ValidateEditReservations(Network candidate,List<Remap> changes)
        {
            var mapped=new Dictionary<int,Remap>();if(changes!=null)foreach(var change in changes)mapped.Add(change.vehicle.id,change);
            var owners=new Dictionary<int,int>();
            foreach(var vehicle in data.vehicles)
            {
                Remap remap;bool changed=mapped.TryGetValue(vehicle.id,out remap);var route=changed?remap.route:vehicle.route;int cursor=changed?remap.cursor:vehicle.cursor;double distance=changed?remap.distance:vehicle.distance;
                if(vehicle.status==VehicleStatus.Delivered||!candidate.Contains(route[cursor]))continue;var lane=candidate.Get(route[cursor]);int junction=lane.JunctionId;
                if(junction==0&&distance<Clearance(lane)-1e-9)junction=lane.From;
                if(!lane.IsConnection&&distance>lane.Geometry.Length-Clearance(lane)+1e-9)junction=lane.To;
                if(!candidate.IsConflictJunction(junction))continue;
                int owner;if(owners.TryGetValue(junction,out owner)&&owner!=vehicle.id)throw new InvalidOperationException("施工节点安全区内有多辆车，请等待它们驶离后重试；道路和任务未修改。");owners[junction]=vehicle.id;
            }
        }
        public int Advance(double simulationSeconds)
        {
            if(simulationSeconds<0||double.IsNaN(simulationSeconds)||double.IsInfinity(simulationSeconds))throw new ArgumentException("Invalid time");
            data.accumulator+=simulationSeconds;int steps=0;while(data.accumulator+1e-12>=StepSeconds){Step();data.accumulator-=StepSeconds;steps++;}return steps;
        }
        void Reindex()
        {
            foreach(var b in occupancy.Values)b.Clear();ActiveCount=0;locks.Clear();
            foreach(var v in data.vehicles)
            {
                if(v.status==VehicleStatus.Delivered||!Network.Contains(v.Current))continue;
                if(v.status==VehicleStatus.Active)ActiveCount++;List<Vehicle> b;if(!occupancy.TryGetValue(v.Current,out b))occupancy.Add(v.Current,b=new List<Vehicle>());b.Add(v);
                var d=Network.Get(v.Current);int junction=d.JunctionId;
                // Keep reservation until the body has cleared the outgoing lane.
                if(junction==0&&v.distance<Clearance(d)-1e-9)junction=d.From;
                if(!d.IsConnection&&v.distance>d.Geometry.Length-Clearance(d)+1e-9)junction=d.To;
                if(junction!=0&&Network.IsConflictJunction(junction)){int owner;if(locks.TryGetValue(junction,out owner)&&owner!=v.id)throw new InvalidOperationException("Conflicting junction occupants");locks[junction]=v.id;}
            }
            foreach(var b in occupancy.Values)b.Sort(OccupantOrder.Instance);
        }
        public void Step()
        {
#if UNITY_5_3_OR_NEWER
            using(stepSample.Auto())
#endif
            {
#if UNITY_5_3_OR_NEWER
                using(indexSample.Auto())
#endif
                Reindex();
                entries.Clear();intents.Clear();
#if UNITY_5_3_OR_NEWER
                using(intentSample.Auto())
#endif
                {
                    // Snapshot following limits, then arbitrate in explicit stable entity order.
                    foreach(var pair in occupancy)
                    {
                        var bucket=pair.Value;
                        for(int i=0;i<bucket.Count;i++)
                        {
                            var v=bucket[i];if(v.status!=VehicleStatus.Active)continue;
                            intents.Add(new Intent{vehicle=v,cursor=v.cursor,distance=v.distance,limit=i>0?bucket[i-1].distance-MinimumSpacing:double.PositiveInfinity});
                        }
                    }
                    intents.Sort(IntentOrder.Instance);
                    for(int index=0;index<intents.Count;index++)
                    {
                            var intent=intents[index];var v=intent.vehicle;int cursor=intent.cursor;double distance=intent.distance,time=StepSeconds,limit=intent.limit;
                            while(time>1e-12)
                            {
                                var d=Network.Get(v.route[cursor]);double target=Math.Min(d.Geometry.Length,Math.Min(limit,distance+d.Speed*time));
                                // Waiting vehicles stop outside the shared junction area, not
                                // at the geometric connection point inside another approach.
                                if(!d.IsConnection&&Network.IsConflictJunction(d.To)&&target>d.Geometry.Length-Clearance(d)+1e-9)
                                {
                                    int holder;if(locks.TryGetValue(d.To,out holder)&&holder!=v.id)target=Math.Min(target,d.Geometry.Length-Clearance(d));
                                    else
                                    {
                                        bool room=true;
                                        if(cursor+2<v.route.Count)
                                        {
                                            var exit=Network.Get(v.route[cursor+2]);List<Vehicle> bucket;double reserved;
                                            double needed=MinimumSpacing+Clearance(exit);
                                            if(occupancy.TryGetValue(exit.Id,out bucket)&&bucket.Count>0&&bucket[bucket.Count-1].distance<needed)room=false;
                                            if(entries.TryGetValue(exit.Id,out reserved)&&reserved<needed)room=false;
                                        }
                                        if(room)locks[d.To]=v.id;else target=Math.Min(target,d.Geometry.Length-Clearance(d));
                                    }
                                }
                                target=Math.Max(distance,target);time-=Math.Max(0,target-distance)/d.Speed;distance=target;
                                if(distance<d.Geometry.Length-1e-9||cursor==v.route.Count-1)break;
                                var next=Network.Get(v.route[cursor+1]);int owner;
                                if(next.IsConnection&&Network.IsConflictJunction(next.JunctionId)&&locks.TryGetValue(next.JunctionId,out owner)&&owner!=v.id)break;
                                if(next.IsConnection&&cursor+2<v.route.Count)
                                {
                                    long exit=v.route[cursor+2];List<Vehicle> exitBucket;double reserved;
                                    if(occupancy.TryGetValue(exit,out exitBucket)&&exitBucket.Count>0&&exitBucket[exitBucket.Count-1].distance<MinimumSpacing)break;
                                    if(entries.TryGetValue(exit,out reserved)&&reserved<MinimumSpacing)break;
                                }
                                List<Vehicle> ahead;double rear=double.PositiveInfinity;
                                if(occupancy.TryGetValue(next.Id,out ahead)&&ahead.Count>0)rear=ahead[ahead.Count-1].distance;
                                double planned;if(entries.TryGetValue(next.Id,out planned))rear=Math.Min(rear,planned);
                                if(rear<MinimumSpacing)break;
                                if(next.IsConnection&&Network.IsConflictJunction(next.JunctionId))locks[next.JunctionId]=v.id;
                                cursor++;distance=0;limit=rear-MinimumSpacing;
                                // Reserve the entry conservatively for the rest of this step.
                                entries[next.Id]=0;
                            }
                            intent.cursor=cursor;intent.distance=distance;intents[index]=intent;
                    }
                }
#if UNITY_5_3_OR_NEWER
                using(commitSample.Auto())
#endif
                foreach(var intent in intents)
                {
                    var v=intent.vehicle;v.cursor=intent.cursor;v.distance=intent.distance;
                    if(v.cursor!=v.route.Count-1||v.distance<Network.Get(v.Current).Geometry.Length-1e-9)continue;
                    var destination=GetDepot(v.destination);double payment=v.price*v.cargo;
                    if(destination.capacity-destination.stock<v.cargo||destination.cash<payment)continue;
                    destination.stock+=v.cargo;destination.cash-=payment;GetDepot(v.origin).cash+=payment;data.delivered+=v.cargo;v.cargo=0;v.status=VehicleStatus.Delivered;ActiveCount--;
                }
                // Retire finished tasks in one compaction pass; historical traffic never grows
                // the hot simulation scan. Stock/cash and delivered ledger are authoritative.
                int retained=0;for(int i=0;i<data.vehicles.Count;i++)if(data.vehicles[i].status!=VehicleStatus.Delivered)data.vehicles[retained++]=data.vehicles[i];
                if(retained<data.vehicles.Count)data.vehicles.RemoveRange(retained,data.vehicles.Count-retained);
                data.clock+=StepSeconds;
            }
        }
        public int SplitRoad(int roadId,int sample)
        {
            var n=CopyNetwork();var r=n.roads.Find(x=>x.id==roadId);if(r==null||sample<=0||sample>=r.points.Length-1)throw new ArgumentException("Split must be an internal geometry sample");
            int junction=n.nextJunction++,suffixId=n.nextRoad++;
            n.junctions.Add(new Junction{id=junction,position=r.points[sample]});
            var suffix=new PhysicalRoad{id=suffixId,from=junction,to=r.to,oneWay=r.oneWay,speed=r.speed,points=new Point[r.points.Length-sample],endTangent=r.endTangent};
            Array.Copy(r.points,sample,suffix.points,0,suffix.points.Length);
            var tangent=Network.Tangent(r.points[sample-1],r.points[sample+1]);suffix.startTangent=tangent;r.endTangent=tangent;
            var prefix=new Point[sample+1];Array.Copy(r.points,prefix,prefix.Length);r.points=prefix;r.to=junction;n.roads.Add(suffix);
            var replacement=new Network(n);
            var remaps=new List<Remap>();
            foreach(var v in data.vehicles)
            {
                if(v.status==VehicleStatus.Stranded)
                {
                    if(!Network.Contains(v.Current))continue; // Its surface was already removed.
                    var old=Network.Get(v.Current);long current=v.Current;double distance=v.distance;
                    if(old.RoadId==roadId)
                    {
                        long first=Network.LaneId(old.Reverse?suffixId:roadId,old.Reverse),second=Network.LaneId(old.Reverse?roadId:suffixId,old.Reverse);
                        double boundary=replacement.Get(first).Geometry.Length;current=distance>=boundary?second:first;if(distance>=boundary)distance-=boundary;
                    }
                    else if(old.IsConnection)
                    {
                        long packed=-current-1,incoming=packed>>32,outgoing=(uint)packed;
                        var entry=Network.Get(incoming);var exit=Network.Get(outgoing);
                        if(entry.RoadId==roadId&&!entry.Reverse)incoming=Network.LaneId(suffixId,false);
                        if(exit.RoadId==roadId&&exit.Reverse)outgoing=Network.LaneId(suffixId,true);
                        current=Network.ConnectionId(incoming,outgoing);
                    }
                    if(Point.Distance(v.strandedPosition,replacement.Get(current).Geometry.Sample(distance))>1e-7)throw new InvalidOperationException("Split moved interrupted vehicle occupancy");
                    remaps.Add(new Remap{vehicle=v,route=new List<long>{current},cursor=0,distance=distance});continue;
                }
                int oldCursor=v.cursor;double oldDistance=v.distance;
                var lanes=new List<long>();int newCursor=0;double newDistance=oldDistance;
                for(int i=0;i<v.route.Count;i+=2)
                {
                    long id=v.route[i];bool split=id==Network.LaneId(roadId,false)||id==Network.LaneId(roadId,true);
                    long first=id,second=0;
                    if(split){bool reverse=Network.Get(id).Reverse;first=Network.LaneId(reverse?suffixId:roadId,reverse);second=Network.LaneId(reverse?roadId:suffixId,reverse);}
                    int laneIndex=lanes.Count*2;
                    if(i==oldCursor){newCursor=laneIndex;if(split&&oldDistance>=replacement.Get(first).Geometry.Length){newCursor+=2;newDistance=oldDistance-replacement.Get(first).Geometry.Length;}}
                    // Existing connections change their incident lane IDs, but retain world geometry.
                    if(i+1==oldCursor)newCursor=laneIndex+(split?3:1);
                    lanes.Add(first);if(split)lanes.Add(second);
                }
                var route=new List<long>();for(int i=0;i<lanes.Count;i++){if(i>0)route.Add(Network.ConnectionId(lanes[i-1],lanes[i]));route.Add(lanes[i]);}
                replacement.ValidateRoute(route);
                if(Point.Distance(Position(v),replacement.Get(route[newCursor]).Geometry.Sample(newDistance))>1e-7)throw new InvalidOperationException("Split cannot preserve vehicle position");
                remaps.Add(new Remap{vehicle=v,route=route,cursor=newCursor,distance=newDistance});
            }
            ValidateEditReservations(replacement,remaps);
            foreach(var remap in remaps){remap.vehicle.route=remap.route;remap.vehicle.cursor=remap.cursor;remap.vehicle.distance=remap.distance;}
            data.network=n;Network=replacement;NetworkGeneration++;occupancy.Clear();Reindex();return junction;
        }
        public void RemoveRoad(int id)
        {
            var n=CopyNetwork();if(n.roads.RemoveAll(r=>r.id==id)==0)throw new ArgumentException("Missing road");var replacement=new Network(n);
            var remaps=new List<Remap>();var interrupted=new List<KeyValuePair<Vehicle,Point>>();
            foreach(var v in data.vehicles)
            {
                if(v.status!=VehicleStatus.Active)continue;
                bool invalid=false;for(int i=v.cursor;i<v.route.Count;i++)if(!replacement.Contains(v.route[i])){invalid=true;break;}
                if(invalid)interrupted.Add(new KeyValuePair<Vehicle,Point>(v,Position(v)));
                else
                {
                    // Traversed history is not a route dependency. A current connection
                    // retains its incoming lane; a current lane becomes the new route start.
                    int prefix=Network.Get(v.Current).IsConnection?v.cursor-1:v.cursor;
                    var route=v.route.GetRange(prefix,v.route.Count-prefix);replacement.ValidateRoute(route);
                    remaps.Add(new Remap{vehicle=v,route=route,cursor=v.cursor-prefix,distance=v.distance});
                }
            }
            ValidateEditReservations(replacement,remaps);
            foreach(var remap in remaps){remap.vehicle.route=remap.route;remap.vehicle.cursor=remap.cursor;}
            foreach(var item in interrupted){item.Key.strandedPosition=item.Value;item.Key.status=VehicleStatus.Stranded;}
            data.network=n;Network=replacement;NetworkGeneration++;occupancy.Clear();Reindex();
        }
        public void Validate()
        {
            if(data.accumulator< -1e-9||double.IsNaN(data.accumulator)||double.IsInfinity(data.accumulator)||data.clock<0||double.IsNaN(data.clock)||double.IsInfinity(data.clock)||data.delivered<0)throw new ArgumentException("Invalid clock");
            var depots=new HashSet<int>();int maxD=0,maxV=0;foreach(var d in data.depots){JunctionPosition(d.junction);if(d.id<=0||!depots.Add(d.id)||d.stock<0||d.stock>d.capacity||d.cash<0||double.IsNaN(d.cash)||double.IsInfinity(d.cash))throw new ArgumentException("Invalid depot");maxD=Math.Max(maxD,d.id);}
            var ids=new HashSet<int>();foreach(var v in data.vehicles)
            {
                if(v.id<=0||!ids.Add(v.id)||!depots.Contains(v.origin)||!depots.Contains(v.destination)||v.cargo<0||v.price<0||double.IsNaN(v.price)||double.IsInfinity(v.price)||v.status<VehicleStatus.Active||v.status>VehicleStatus.Stranded)throw new ArgumentException("Invalid vehicle");maxV=Math.Max(maxV,v.id);
                if(v.route==null||v.route.Count==0||v.cursor<0||v.cursor>=v.route.Count||v.distance<0||double.IsNaN(v.distance)||double.IsInfinity(v.distance))throw new ArgumentException("Invalid vehicle cursor");
                if(v.status==VehicleStatus.Stranded)
                {
                    double length=Point.Distance(v.strandedPosition,new Point());if(double.IsNaN(length)||double.IsInfinity(length))throw new ArgumentException("Invalid stranded position");
                    if(Network.Contains(v.Current)&&(v.distance>Network.Get(v.Current).Geometry.Length+1e-8||Point.Distance(v.strandedPosition,Network.Get(v.Current).Geometry.Sample(v.distance))>1e-7))throw new ArgumentException("Interrupted occupancy disagrees with position");continue;
                }
                Network.ValidateRoute(v.route);if(v.cursor<0||v.cursor>=v.route.Count||v.distance<0||double.IsNaN(v.distance)||double.IsInfinity(v.distance)||v.distance>Network.Get(v.Current).Geometry.Length+1e-8)throw new ArgumentException("Invalid vehicle position");
                if(Network.Get(v.route[v.route.Count-1]).To!=GetDepot(v.destination).junction)throw new ArgumentException("Route does not reach task destination");
                if(v.status==VehicleStatus.Delivered&&v.cargo!=0)throw new ArgumentException("Delivered cargo still on truck");
            }
            if(data.nextDepot<=maxD||data.nextVehicle<=maxV)throw new ArgumentException("Entity allocator is behind live entities");
        }
        public void AssertSafety()
        {
            Reindex();foreach(var bucket in occupancy.Values)for(int i=1;i<bucket.Count;i++)if(bucket[i-1].distance-bucket[i].distance<MinimumSpacing-1e-8)throw new InvalidOperationException("Vehicle overlap");
        }
    }
}
