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
        internal readonly LaneOccupant sourceOccupant,shadowOccupant;
        public Vehicle(){sourceOccupant=new LaneOccupant(this,0);shadowOccupant=new LaneOccupant(this,0,true);}
        public int id {get;internal set;} public int origin {get;internal set;} public int destination {get;internal set;}
        public int cargo {get;internal set;}=1;
        public double price {get;internal set;}=5; public double distance {get;internal set;}
        public double arrivalDistance {get;internal set;}=-1;
        public long changeTarget {get;internal set;}
        public double changeElapsed {get;internal set;}
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
        public int coreFormat=6,nextVehicle=1,nextDepot=1;
        public bool externalBusiness;
        public double clock,accumulator;
        public int delivered;
        public NetworkData network=new NetworkData();
        public List<Depot> depots=new List<Depot>();
        public List<Vehicle> vehicles=new List<Vehicle>();
        public List<RoadAccess> accesses=new List<RoadAccess>();
    }
    public sealed class World
    {
        public const double StepSeconds=.05,MinimumSpacing=1.5;
        readonly WorldData data;
        readonly ITransportBusiness business;
        public Network Network {get;private set;}
        readonly IReadOnlyList<Vehicle> vehicleView;readonly IReadOnlyList<Depot> depotView;
        public IEnumerable<Vehicle> Vehicles=>vehicleView;
        public IEnumerable<Depot> Depots=>depotView;
        public double Clock=>data.clock;
        public int Delivered=>data.delivered;
        public int ActiveCount {get;private set;}
        public int NetworkGeneration {get;private set;}
        internal long EditEpoch {get;private set;}
        readonly Dictionary<long,List<LaneOccupant>> occupancy=new Dictionary<long,List<LaneOccupant>>(CityRoadKeyComparer.Instance);
        readonly JunctionReservations reservations=new JunctionReservations(null);
        readonly Dictionary<int,Depot> depotsById=new Dictionary<int,Depot>();
        readonly Dictionary<int,Vehicle> vehiclesById=new Dictionary<int,Vehicle>();
        readonly Dictionary<int,RoadAccess> accesses=new Dictionary<int,RoadAccess>();
        bool indexDirty=true;
        readonly Dictionary<long,double> entries=new Dictionary<long,double>(CityRoadKeyComparer.Instance);
        readonly List<Intent> intents=new List<Intent>();
        struct Intent { public Vehicle vehicle;public int cursor;public double distance,limit; }
        internal struct Remap { public Vehicle vehicle;public List<long> route;public int cursor;public double distance,arrival; }
        sealed class IntentOrder:IComparer<Intent>
        {public static readonly IntentOrder Instance=new IntentOrder();public int Compare(Intent a,Intent b)=>a.vehicle.id.CompareTo(b.vehicle.id);}
#if UNITY_5_3_OR_NEWER
        static readonly ProfilerMarker stepSample=new ProfilerMarker("City.RoadCore.Step"),indexSample=new ProfilerMarker("City.RoadCore.Occupancy"),intentSample=new ProfilerMarker("City.RoadCore.IntentAndReservations"),commitSample=new ProfilerMarker("City.RoadCore.CommitAndSettlement");
#endif
        public World():this(new WorldData()){}
        public World(ITransportBusiness owner):this(new WorldData{externalBusiness=true},owner){}
        public World(WorldData state,ITransportBusiness owner=null)
        {
            if(state==null||state.coreFormat!=6||state.externalBusiness!=(owner!=null))throw new ArgumentException("Unsupported road-core save or missing business owner. Legacy city saves are not supported.");
            data=state; // Take ownership of a private copy, never the caller's mutable save DTO.
            data=Snapshot();vehicleView=data.vehicles.AsReadOnly();depotView=data.depots.AsReadOnly();foreach(var depot in data.depots)depotsById.Add(depot.id,depot);
            if(data.externalBusiness&&data.depots.Count!=0)throw new ArgumentException("External city cannot contain duplicate fixture inventories");foreach(var access in data.accesses)accesses.Add(access.endpoint,access);
            foreach(var vehicle in data.vehicles)vehiclesById.Add(vehicle.id,vehicle);business=owner??new DepotBusiness(depotsById);Network=new Network(data.network);Validate();AssertSafety();
        }
        // Only serializers may receive this copy; callers cannot mutate the live facts.
        public WorldData Snapshot()
        {
            var copy=new WorldData{coreFormat=6,externalBusiness=data.externalBusiness,nextVehicle=data.nextVehicle,nextDepot=data.nextDepot,clock=data.clock,accumulator=data.accumulator,delivered=data.delivered,network=CopyNetwork()};
            foreach(var access in data.accesses)copy.accesses.Add(access.Copy());
            foreach(var d in data.depots)copy.depots.Add(new Depot{id=d.id,junction=d.junction,stock=d.stock,capacity=d.capacity,cash=d.cash});
            foreach(var v in data.vehicles)copy.vehicles.Add(new Vehicle{id=v.id,origin=v.origin,destination=v.destination,cargo=v.cargo,price=v.price,distance=v.distance,arrivalDistance=v.arrivalDistance,changeTarget=v.changeTarget,changeElapsed=v.changeElapsed,cursor=v.cursor,status=v.status,strandedPosition=v.strandedPosition,route=new List<long>(v.route)});
            return copy;
        }
        NetworkData CopyNetwork()
        {
            var n=new NetworkData{nextJunction=data.network.nextJunction,nextRoad=data.network.nextRoad};
            foreach(var j in data.network.junctions)n.junctions.Add(new Junction{id=j.id,position=j.position});
            foreach(var r in data.network.roads)n.roads.Add(new PhysicalRoad{id=r.id,from=r.from,to=r.to,oneWay=r.oneWay,lanes=r.lanes,speed=r.speed,points=(Point[])r.points.Clone(),rightVectors=r.rightVectors==null?null:(Point[])r.rightVectors.Clone(),startTangent=r.startTangent,endTangent=r.endTangent});return n;
        }
        public int AddJunction(Point point)
        {
            var n=CopyNetwork();int id=n.nextJunction++;n.junctions.Add(new Junction{id=id,position=point});var replacement=new Network(n);if(!new RoadEditPlan(this,n,replacement).TryCommit())throw new InvalidOperationException("Road plan became stale");return id;
        }
        public Point JunctionPosition(int id){foreach(var j in data.network.junctions)if(j.id==id)return j.position;throw new ArgumentException("Missing junction");}
        public int SnapJunction(Point p,double radius)
        {int best=0;foreach(var j in data.network.junctions){double d=Point.Distance(j.position,p);if(d<radius){radius=d;best=j.id;}}return best;}
        public int AddRoad(int from,int to,Point[] points,bool oneWay=false,double speed=12,int lanes=1)
        {
            int id=data.network.nextRoad;var plan=PlanRoad(from,to,points,oneWay,speed,lanes);if(!plan.TryCommit())throw new InvalidOperationException("Road plan became stale");return id;
        }
        public RoadEditPlan PlanRoad(int from,int to,Point[] points,bool oneWay=false,double speed=12,int lanes=1)
        {var n=CopyNetwork();int id=n.nextRoad++;n.roads.Add(new PhysicalRoad{id=id,from=from,to=to,points=(Point[])points.Clone(),oneWay=oneWay,lanes=lanes,speed=speed});var network=new Network(n);ValidateEditReservations(network,null);return new RoadEditPlan(this,n,network);}
        public RoadEditPlan PlanConnectedRoad(Point[] center,bool oneWay=false,double speed=12,double snap=3,int lanes=1)=>RoadConstruction.Connect(this,center,oneWay,speed,snap,lanes);
        internal World EditDraft()=>new World(Snapshot(),data.externalBusiness?business:null);
        internal RoadEditPlan PlanFromDraft(World draft)
        {
            var candidate=draft.Snapshot();var mapped=new Dictionary<int,Vehicle>();foreach(var v in candidate.vehicles)mapped.Add(v.id,v);
            var remaps=new List<Remap>();var interrupted=new List<KeyValuePair<Vehicle,Point>>();
            foreach(var v in data.vehicles)
            {
                var next=mapped[v.id];if(v.status==VehicleStatus.Active&&next.status==VehicleStatus.Stranded)interrupted.Add(new KeyValuePair<Vehicle,Point>(v,next.strandedPosition));
                else if(draft.Network.Contains(next.Current))remaps.Add(new Remap{vehicle=v,route=next.route,cursor=next.cursor,distance=next.distance,arrival=next.arrivalDistance});
            }
            return new RoadEditPlan(this,candidate.network,draft.Network,remaps,interrupted,candidate.accesses);
        }
        internal bool TryCommit(RoadEditPlan plan)
        {
            if(!ReferenceEquals(plan.owner,this)||!ReferenceEquals(plan.source,Network)||plan.epoch!=EditEpoch)return false;
            ValidateEditReservations(plan.network,plan.remaps);
            foreach(var remap in plan.remaps){if(remap.vehicle.status!=VehicleStatus.Stranded)plan.network.ValidateRoute(remap.route);long target=RemappedChangeTarget(remap.vehicle,plan.network,remap.route[remap.cursor]);if(Point.Distance(Position(remap.vehicle),LaneChanges.Position(plan.network,remap.route[remap.cursor],remap.distance,target,remap.vehicle.changeElapsed))>1e-7)throw new InvalidOperationException("Edit moved an existing vehicle");}
            var staged=Snapshot();staged.network=plan.data;if(plan.accesses!=null)staged.accesses=plan.accesses;
            var stagedVehicles=new Dictionary<int,Vehicle>();foreach(var vehicle in staged.vehicles)stagedVehicles.Add(vehicle.id,vehicle);
            foreach(var remap in plan.remaps){var vehicle=stagedVehicles[remap.vehicle.id];vehicle.route=remap.route;vehicle.cursor=remap.cursor;vehicle.distance=remap.distance;vehicle.changeTarget=RemappedChangeTarget(remap.vehicle,plan.network,vehicle.Current);if(vehicle.status!=VehicleStatus.Stranded)vehicle.arrivalDistance=remap.arrival;}
            foreach(var item in plan.interruptions){var vehicle=stagedVehicles[item.Key.id];vehicle.strandedPosition=item.Value;vehicle.status=VehicleStatus.Stranded;}
            // Cold transaction validation proves references, endpoint bounds and occupancy
            // together before the first live write. No business callback mutates inventory.
            new World(staged,data.externalBusiness?business:null);
            // Every fallible geometry/migration/reservation check precedes these writes.
            foreach(var remap in plan.remaps){long target=RemappedChangeTarget(remap.vehicle,plan.network,remap.route[remap.cursor]);remap.vehicle.route=remap.route;remap.vehicle.cursor=remap.cursor;remap.vehicle.distance=remap.distance;remap.vehicle.changeTarget=target;if(remap.vehicle.status!=VehicleStatus.Stranded)remap.vehicle.arrivalDistance=remap.arrival;}
            foreach(var item in plan.interruptions){item.Key.strandedPosition=item.Value;item.Key.status=VehicleStatus.Stranded;}
            if(plan.accesses!=null){data.accesses=plan.accesses;accesses.Clear();foreach(var access in plan.accesses)accesses.Add(access.endpoint,access);}
            data.network=plan.data;Network=plan.network;NetworkGeneration++;EditEpoch++;occupancy.Clear();Reindex();return true;
        }
        public int AddDepot(int junction,int stock=100)
        {if(data.externalBusiness)throw new InvalidOperationException("City owns its inventories; synthetic depots are disabled");JunctionPosition(junction);if(stock<0||stock>100000)throw new ArgumentOutOfRangeException(nameof(stock));int id=data.nextDepot++;var depot=new Depot{id=id,junction=junction,stock=stock};data.depots.Add(depot);depotsById.Add(id,depot);return id;}
        public Depot GetDepot(int id){Depot depot;if(depotsById.TryGetValue(id,out depot))return depot;throw new ArgumentException("Missing depot");}
        public RoadAccess GetAccess(int endpoint){RoadAccess access;return accesses.TryGetValue(endpoint,out access)?access:null;}
        public void RegisterAccess(int endpoint,long lane,double distance,AccessUse use=AccessUse.All)
        {
            var drivable=Network.Get(lane);if(endpoint<0||accesses.ContainsKey(endpoint)||drivable.IsConnection||distance<0||distance>drivable.Geometry.Length||double.IsNaN(distance)||double.IsInfinity(distance)||use==0||(use&~AccessUse.All)!=0)throw new ArgumentException("Invalid road access");
            var access=new RoadAccess{endpoint=endpoint,lane=lane,distance=distance,use=use};data.accesses.Add(access);accesses.Add(endpoint,access);
            EditEpoch++;
        }
        public Vehicle Dispatch(int origin,int destination,int amount=1,double price=5)
        {
            if(amount<=0||price<0||double.IsNaN(price)||double.IsInfinity(price))throw new ArgumentOutOfRangeException("Cargo/price");
            if(origin==destination||!business.Exists(origin)||!business.Exists(destination))return null;
            RoadAccess source=GetAccess(origin),target=GetAccess(destination);List<long> route;double start=0,end=-1;
            if(source!=null&&target!=null){if(!source.valid||!target.valid||(source.use&AccessUse.Freight)==0||(target.use&AccessUse.Freight)==0)return null;route=Network.Route(source,target);start=source.distance;end=target.distance;}
            else {var provider=business as IJunctionEndpointProvider;if(provider==null)return null;route=Network.Route(provider.Junction(origin),provider.Junction(destination));}
            if(route==null||route.Count==0)return null;
            if(indexDirty)Reindex();List<LaneOccupant> bucket;
            if(occupancy.TryGetValue(route[0],out bucket)){int slot=InsertionSlot(bucket,start);if(slot>0&&bucket[slot-1].distance-start<MinimumSpacing||slot<bucket.Count&&start-bucket[slot].distance<MinimumSpacing)return null;}
            var entry=Network.Get(route[0]);int reservation=start<Clearance(entry)-1e-9?entry.From:start>entry.Geometry.Length-Clearance(entry)+1e-9?entry.To:0;
            long spawnMovement=reservation==entry.To&&route.Count>1?route[1]:0;
            if(Network.IsConflictJunction(reservation)&&!reservations.CanReserve(reservation,spawnMovement,data.nextVehicle))return null;
            var v=new Vehicle{id=data.nextVehicle,origin=origin,destination=destination,cargo=amount,price=price,route=route,distance=start,arrivalDistance=end};
            if(data.vehicles.Count==data.vehicles.Capacity)data.vehicles.Capacity=Math.Max(4,data.vehicles.Capacity*2);
            if(!business.Depart(v))return null;data.nextVehicle++;data.vehicles.Add(v);vehiclesById.Add(v.id,v);ActiveCount++;EditEpoch++;
            if(!occupancy.TryGetValue(v.Current,out bucket))occupancy.Add(v.Current,bucket=new List<LaneOccupant>());v.sourceOccupant.distance=start;bucket.Insert(InsertionSlot(bucket,start),v.sourceOccupant);if(Network.IsConflictJunction(reservation))reservations.Reserve(reservation,spawnMovement,v.id);return v;
        }
        static int InsertionSlot(List<LaneOccupant> bucket,double position){int lo=0,hi=bucket.Count;while(lo<hi){int mid=(lo+hi)/2;if(bucket[mid].distance>position)lo=mid+1;else hi=mid;}return lo;}
        static double EndDistance(Vehicle v,Drivable road)=>v.arrivalDistance<0?road.Geometry.Length:v.arrivalDistance;
        public Point Position(Vehicle v)=>v.status==VehicleStatus.Stranded?v.strandedPosition:LaneChanges.Position(Network,v.Current,v.distance,v.changeTarget,v.changeElapsed);
        long RemappedChangeTarget(Vehicle v,Network candidate,long current)
        {if(v.changeTarget==0)return 0;var lane=candidate.Get(current);var old=Network.Get(v.changeTarget);long target=Network.LaneId(lane.RoadId,lane.Reverse,old.LaneIndex);if(lane.IsConnection||!candidate.Contains(target))throw new InvalidOperationException("Construction removes a changing lane; retry after the maneuver completes");return target;}
        public bool TryChangeLane(int vehicleId,int targetIndex)
        {Vehicle v;if(!vehiclesById.TryGetValue(vehicleId,out v))return false;if(indexDirty)Reindex();if(!LaneChanges.TryStart(Network,occupancy,v,targetIndex))return false;EditEpoch++;return true;}
        public void RefreshEndpoints()
        {foreach(var v in data.vehicles)if(v.status==VehicleStatus.Active&&(!business.Exists(v.origin)||!business.Exists(v.destination))){v.strandedPosition=Position(v);v.status=VehicleStatus.Stranded;}EditEpoch++;Reindex();}
        double Clearance(Drivable lane)=>Network.Clearance(lane);
        static long Movement(Network network,IList<long> route,int cursor,Drivable lane,int junction)
        {if(lane.IsConnection)return lane.Id;if(junction==lane.To&&cursor+1<route.Count)return route[cursor+1];if(junction==lane.From&&cursor>0&&network.Get(route[cursor-1]).IsConnection)return route[cursor-1];return 0;}
        void ValidateEditReservations(Network candidate,List<Remap> changes)
        {
            var mapped=new Dictionary<int,Remap>();if(changes!=null)foreach(var change in changes)mapped.Add(change.vehicle.id,change);
            var owners=new JunctionReservations(candidate);
            foreach(var vehicle in data.vehicles)
            {
                Remap remap;bool changed=mapped.TryGetValue(vehicle.id,out remap);var route=changed?remap.route:vehicle.route;int cursor=changed?remap.cursor:vehicle.cursor;double distance=changed?remap.distance:vehicle.distance;
                if(vehicle.status==VehicleStatus.Delivered||!candidate.Contains(route[cursor]))continue;var lane=candidate.Get(route[cursor]);int junction=lane.JunctionId;
                if(junction==0&&distance<candidate.Clearance(lane)-1e-9)junction=lane.From;
                if(!lane.IsConnection&&distance>lane.Geometry.Length-candidate.Clearance(lane)+1e-9)junction=lane.To;
                if(!candidate.IsConflictJunction(junction))continue;
                long movement=vehicle.changeTarget!=0?0:Movement(candidate,route,cursor,lane,junction);if(!owners.CanReserve(junction,movement,vehicle.id))throw new InvalidOperationException("施工节点安全区内有冲突车辆，请等待它们驶离后重试；道路和任务未修改。");owners.Reserve(junction,movement,vehicle.id);
            }
        }
        public int Advance(double simulationSeconds,Action beforeStep=null,Action afterStep=null)
        {
            if(simulationSeconds<0||double.IsNaN(simulationSeconds)||double.IsInfinity(simulationSeconds))throw new ArgumentException("Invalid time");
            data.accumulator+=simulationSeconds;int steps=0;while(data.accumulator+1e-12>=StepSeconds){beforeStep?.Invoke();Step();afterStep?.Invoke();data.accumulator-=StepSeconds;steps++;}return steps;
        }
        void Reindex()
        {
            foreach(var b in occupancy.Values)b.Clear();ActiveCount=0;reservations.Reset(Network);
            foreach(var v in data.vehicles)
            {
                if(v.status==VehicleStatus.Active&&(!business.Exists(v.origin)||!business.Exists(v.destination))){v.strandedPosition=Position(v);v.status=VehicleStatus.Stranded;}
                if(v.status==VehicleStatus.Delivered||!Network.Contains(v.Current))continue;
                if(v.status==VehicleStatus.Active)ActiveCount++;List<LaneOccupant> b;if(!occupancy.TryGetValue(v.Current,out b))occupancy.Add(v.Current,b=new List<LaneOccupant>());v.sourceOccupant.distance=v.distance;b.Add(v.sourceOccupant);
                if(v.changeTarget!=0&&Network.Contains(v.changeTarget)){var target=Network.Get(v.changeTarget);List<LaneOccupant> shadow;if(!occupancy.TryGetValue(target.Id,out shadow))occupancy.Add(target.Id,shadow=new List<LaneOccupant>());v.shadowOccupant.distance=Network.Get(v.Current).Geometry.MapDistance(target.Geometry,v.distance);shadow.Add(v.shadowOccupant);}
                var d=Network.Get(v.Current);int junction=d.JunctionId;
                // Keep reservation until the body has cleared the outgoing lane.
                if(junction==0&&v.distance<Clearance(d)-1e-9)junction=d.From;
                if(!d.IsConnection&&v.distance>d.Geometry.Length-Clearance(d)+1e-9)junction=d.To;
                if(junction!=0&&Network.IsConflictJunction(junction))reservations.Reserve(junction,v.changeTarget!=0?0:Movement(Network,v.route,v.cursor,d,junction),v.id);
            }
            foreach(var b in occupancy.Values)b.Sort(LaneOccupantOrder.Instance);
            indexDirty=false;
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
                if(Network.HasParallelLanes)EditEpoch+=LaneChanges.Choose(Network,data.vehicles,occupancy,(int)Math.Round(data.clock/StepSeconds));
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
                            var occupant=bucket[i];var v=occupant.vehicle;if(occupant.shadow||v.status!=VehicleStatus.Active)continue;
                            double limit=i>0?bucket[i-1].distance-MinimumSpacing:double.PositiveInfinity;
                            if(v.changeTarget!=0){var lane=Network.Get(v.Current);var target=Network.Get(v.changeTarget);double projected=lane.Geometry.MapDistance(target.Geometry,v.distance);var shadows=occupancy[v.changeTarget];int slot=InsertionSlot(shadows,projected);if(slot>0)limit=Math.Min(limit,target.Geometry.MapDistance(lane.Geometry,Math.Max(0,shadows[slot-1].distance-MinimumSpacing)));limit=Math.Min(limit,lane.Geometry.Length-Clearance(lane));}
                            intents.Add(new Intent{vehicle=v,cursor=v.cursor,distance=v.distance,limit=limit});
                        }
                    }
                    intents.Sort(IntentOrder.Instance);
                    for(int index=0;index<intents.Count;index++)
                    {
                            var intent=intents[index];var v=intent.vehicle;int cursor=intent.cursor;double distance=intent.distance,time=StepSeconds,limit=intent.limit;
                            while(time>1e-12)
                            {
                                var d=Network.Get(v.route[cursor]);double end=cursor==v.route.Count-1?EndDistance(v,d):d.Geometry.Length;double target=Math.Min(end,Math.Min(limit,distance+d.Speed*time));
                                // Waiting vehicles stop outside the shared junction area, not
                                // at the geometric connection point inside another approach.
                                if(!d.IsConnection&&Network.IsConflictJunction(d.To)&&target>d.Geometry.Length-Clearance(d)+1e-9)
                                {
                                    long movement=Movement(Network,v.route,cursor,d,d.To);if(!reservations.CanReserve(d.To,movement,v.id))target=Math.Min(target,d.Geometry.Length-Clearance(d));
                                    else
                                    {
                                        bool room=true;
                                        if(cursor+2<v.route.Count)
                                        {
                                            var exit=Network.Get(v.route[cursor+2]);List<LaneOccupant> bucket;double reserved;
                                            double needed=MinimumSpacing+Clearance(exit);
                                            if(occupancy.TryGetValue(exit.Id,out bucket)&&bucket.Count>0&&bucket[bucket.Count-1].distance<needed)room=false;
                                            if(entries.TryGetValue(exit.Id,out reserved)&&reserved<needed)room=false;
                                        }
                                        if(room)reservations.Reserve(d.To,movement,v.id);else target=Math.Min(target,d.Geometry.Length-Clearance(d));
                                    }
                                }
                                target=Math.Max(distance,target);time-=Math.Max(0,target-distance)/d.Speed;distance=target;
                                if(distance<d.Geometry.Length-1e-9||cursor==v.route.Count-1)break;
                                var next=Network.Get(v.route[cursor+1]);
                                if(next.IsConnection&&Network.IsConflictJunction(next.JunctionId)&&!reservations.CanReserve(next.JunctionId,next.Id,v.id))break;
                                if(next.IsConnection&&cursor+2<v.route.Count)
                                {
                                    long exit=v.route[cursor+2];List<LaneOccupant> exitBucket;double reserved;
                                    if(occupancy.TryGetValue(exit,out exitBucket)&&exitBucket.Count>0&&exitBucket[exitBucket.Count-1].distance<MinimumSpacing)break;
                                    if(entries.TryGetValue(exit,out reserved)&&reserved<MinimumSpacing)break;
                                }
                                List<LaneOccupant> ahead;double rear=double.PositiveInfinity;
                                if(occupancy.TryGetValue(next.Id,out ahead)&&ahead.Count>0)rear=ahead[ahead.Count-1].distance;
                                double planned;if(entries.TryGetValue(next.Id,out planned))rear=Math.Min(rear,planned);
                                if(rear<MinimumSpacing)break;
                                if(next.IsConnection&&Network.IsConflictJunction(next.JunctionId))reservations.Reserve(next.JunctionId,next.Id,v.id);
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
                    if(v.changeTarget!=0){v.changeElapsed+=StepSeconds;if(v.changeElapsed>=1-1e-9){var lane=Network.Get(v.Current);var target=Network.Get(v.changeTarget);var route=new List<long>();if(!LaneChanges.Route(Network,v,target.Id,route))throw new InvalidOperationException("Lane-change route became invalid");v.distance=lane.Geometry.MapDistance(target.Geometry,v.distance);v.route=route;v.cursor=0;v.changeTarget=0;v.changeElapsed=0;}}
                    if(v.cursor!=v.route.Count-1||v.distance<EndDistance(v,Network.Get(v.Current))-1e-9)continue;
                    if(!business.Deliver(v))continue;
                    data.delivered+=v.cargo;v.cargo=0;v.status=VehicleStatus.Delivered;ActiveCount--;
                }
                // Retire finished tasks in one compaction pass; historical traffic never grows
                // the hot simulation scan. Stock/cash and delivered ledger are authoritative.
                int retained=0;for(int i=0;i<data.vehicles.Count;i++)if(data.vehicles[i].status!=VehicleStatus.Delivered)data.vehicles[retained++]=data.vehicles[i];else vehiclesById.Remove(data.vehicles[i].id);
                if(retained<data.vehicles.Count)data.vehicles.RemoveRange(retained,data.vehicles.Count-retained);
                data.clock+=StepSeconds;
                EditEpoch++;
                indexDirty=true;
            }
        }
        public int SplitRoad(int roadId,int sample)
        {
            return SplitCandidate(CopyNetwork(),roadId,sample);
        }
        public int SplitRoadAt(int roadId,double centerDistance)
        {
            var n=CopyNetwork();var road=n.roads.Find(r=>r.id==roadId);if(road==null)throw new ArgumentException("Missing road");var geometry=new Geometry(road.points);
            if(centerDistance<=.1||centerDistance>=geometry.Length-.1||double.IsNaN(centerDistance))throw new ArgumentException("Cut must be inside the road");
            for(int i=1;i<geometry.Count-1;i++)if(Math.Abs(geometry.At(i)-centerDistance)<1e-8)return SplitCandidate(n,roadId,i);
            int after=1;while(geometry.At(after)<centerDistance)after++;double t=(centerDistance-geometry.At(after-1))/(geometry.At(after)-geometry.At(after-1));
            var right=Network.RightVectors(road);var points=new List<Point>(road.points);var vectors=new List<Point>(right);points.Insert(after,geometry.Sample(centerDistance));vectors.Insert(after,Point.Lerp(right[after-1],right[after],t));road.points=points.ToArray();road.rightVectors=vectors.ToArray();
            return SplitCandidate(n,roadId,after);
        }
        int SplitCandidate(NetworkData n,int roadId,int sample)
        {
            var r=n.roads.Find(x=>x.id==roadId);if(r==null||sample<=0||sample>=r.points.Length-1)throw new ArgumentException("Split must be an internal geometry sample");var vectors=Network.RightVectors(r);
            int junction=n.nextJunction++,suffixId=n.nextRoad++;
            n.junctions.Add(new Junction{id=junction,position=r.points[sample]});
            var suffix=new PhysicalRoad{id=suffixId,from=junction,to=r.to,oneWay=r.oneWay,lanes=r.lanes,speed=r.speed,points=new Point[r.points.Length-sample],rightVectors=new Point[r.points.Length-sample],endTangent=r.endTangent};
            Array.Copy(r.points,sample,suffix.points,0,suffix.points.Length);
            Array.Copy(vectors,sample,suffix.rightVectors,0,suffix.rightVectors.Length);
            var tangent=Network.Tangent(r.points[sample-1],r.points[sample+1]);suffix.startTangent=tangent;r.endTangent=tangent;
            var prefix=new Point[sample+1];Array.Copy(r.points,prefix,prefix.Length);r.points=prefix;r.rightVectors=new Point[sample+1];Array.Copy(vectors,r.rightVectors,r.rightVectors.Length);r.to=junction;n.roads.Add(suffix);
            var replacement=new Network(n);
            var accessPlan=new List<RoadAccess>();
            foreach(var existing in data.accesses)
            {
                var access=existing.Copy();if(access.valid&&Network.Get(access.lane).RoadId==roadId)
                {
                    bool reverse=Network.Get(access.lane).Reverse;long first=Network.LaneId(reverse?suffixId:roadId,reverse,Network.Get(access.lane).LaneIndex),second=Network.LaneId(reverse?roadId:suffixId,reverse,Network.Get(access.lane).LaneIndex);double boundary=replacement.Get(first).Geometry.Length;
                    access.lane=access.distance<=boundary?first:second;if(access.distance>boundary)access.distance-=boundary;
                    if(Point.Distance(Network.Get(existing.lane).Geometry.Sample(existing.distance),replacement.Get(access.lane).Geometry.Sample(access.distance))>1e-7)throw new InvalidOperationException("Split moved building access");
                }
                accessPlan.Add(access);
            }
            var remaps=new List<Remap>();
            foreach(var v in data.vehicles)
            {
                if(v.status==VehicleStatus.Stranded)
                {
                    if(!Network.Contains(v.Current))continue; // Its surface was already removed.
                    var old=Network.Get(v.Current);long current=v.Current;double distance=v.distance;
                    if(old.RoadId==roadId)
                    {
                        long first=Network.LaneId(old.Reverse?suffixId:roadId,old.Reverse,old.LaneIndex),second=Network.LaneId(old.Reverse?roadId:suffixId,old.Reverse,old.LaneIndex);
                        double boundary=replacement.Get(first).Geometry.Length;current=distance>=boundary?second:first;if(distance>=boundary)distance-=boundary;
                    }
                    else if(old.IsConnection)
                    {
                        long packed=-current-1,incoming=packed>>32,outgoing=(uint)packed;
                        var entry=Network.Get(incoming);var exit=Network.Get(outgoing);
                        if(entry.RoadId==roadId&&!entry.Reverse)incoming=Network.LaneId(suffixId,false,entry.LaneIndex);
                        if(exit.RoadId==roadId&&exit.Reverse)outgoing=Network.LaneId(suffixId,true,exit.LaneIndex);
                        current=Network.ConnectionId(incoming,outgoing);
                    }
                    if(Point.Distance(v.strandedPosition,LaneChanges.Position(replacement,current,distance,RemappedChangeTarget(v,replacement,current),v.changeElapsed))>1e-7)throw new InvalidOperationException("Split moved interrupted vehicle occupancy");
                    remaps.Add(new Remap{vehicle=v,route=new List<long>{current},cursor=0,distance=distance});continue;
                }
                int oldCursor=v.cursor;double oldDistance=v.distance;
                var lanes=new List<long>();int newCursor=0;double newDistance=oldDistance,newArrival=v.arrivalDistance;
                for(int i=0;i<v.route.Count;i+=2)
                {
                    long id=v.route[i];bool split=Network.Get(id).RoadId==roadId;
                    long first=id,second=0;
                    if(split){bool reverse=Network.Get(id).Reverse;first=Network.LaneId(reverse?suffixId:roadId,reverse,Network.Get(id).LaneIndex);second=Network.LaneId(reverse?roadId:suffixId,reverse,Network.Get(id).LaneIndex);}
                    bool endsInFirst=split&&i==v.route.Count-1&&v.arrivalDistance>=0&&v.arrivalDistance<=replacement.Get(first).Geometry.Length;
                    if(split&&i==v.route.Count-1&&v.arrivalDistance>=0&&!endsInFirst)newArrival=v.arrivalDistance-replacement.Get(first).Geometry.Length;
                    int laneIndex=lanes.Count*2;
                    if(i==oldCursor){newCursor=laneIndex;if(split&&!endsInFirst&&oldDistance>=replacement.Get(first).Geometry.Length){newCursor+=2;newDistance=oldDistance-replacement.Get(first).Geometry.Length;}}
                    // Existing connections change their incident lane IDs, but retain world geometry.
                    if(i+1==oldCursor)newCursor=laneIndex+(split?3:1);
                    lanes.Add(first);if(split&&!endsInFirst)lanes.Add(second);
                }
                var route=new List<long>();for(int i=0;i<lanes.Count;i++){if(i>0)route.Add(Network.ConnectionId(lanes[i-1],lanes[i]));route.Add(lanes[i]);}
                replacement.ValidateRoute(route);
                if(Point.Distance(Position(v),LaneChanges.Position(replacement,route[newCursor],newDistance,RemappedChangeTarget(v,replacement,route[newCursor]),v.changeElapsed))>1e-7)throw new InvalidOperationException("Split cannot preserve vehicle position");
                remaps.Add(new Remap{vehicle=v,route=route,cursor=newCursor,distance=newDistance,arrival=newArrival});
            }
            ValidateEditReservations(replacement,remaps);
            new RoadEditPlan(this,n,replacement,remaps,accesses:accessPlan).TryCommit();return junction;
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
                    remaps.Add(new Remap{vehicle=v,route=route,cursor=v.cursor-prefix,distance=v.distance,arrival=v.arrivalDistance});
                }
            }
            ValidateEditReservations(replacement,remaps);
            var accessPlan=data.accesses.ConvertAll(a=>a.Copy());foreach(var access in accessPlan)if(access.valid&&!replacement.Contains(access.lane)){access.valid=false;access.lane=0;access.distance=0;}
            new RoadEditPlan(this,n,replacement,remaps,interrupted,accessPlan).TryCommit();
        }
        public void Validate()
        {
            if(data.accumulator< -1e-9||double.IsNaN(data.accumulator)||double.IsInfinity(data.accumulator)||data.clock<0||double.IsNaN(data.clock)||double.IsInfinity(data.clock)||data.delivered<0)throw new ArgumentException("Invalid clock");
            var depots=new HashSet<int>();int maxD=0,maxV=0;foreach(var d in data.depots){JunctionPosition(d.junction);if(d.id<=0||!depots.Add(d.id)||d.stock<0||d.stock>d.capacity||d.cash<0||double.IsNaN(d.cash)||double.IsInfinity(d.cash))throw new ArgumentException("Invalid depot");maxD=Math.Max(maxD,d.id);}
            var attached=new HashSet<int>();foreach(var access in data.accesses)
            {
                if(access.endpoint<0||!attached.Add(access.endpoint)||access.use==0||(access.use&~AccessUse.All)!=0)throw new ArgumentException("Invalid access identity/permission");
                if(access.valid){var lane=Network.Get(access.lane);if(lane.IsConnection||access.distance<0||access.distance>lane.Geometry.Length||double.IsNaN(access.distance)||double.IsInfinity(access.distance))throw new ArgumentException("Invalid lane access");}
                else if(access.lane!=0||access.distance!=0)throw new ArgumentException("Invalid access retains dangling road reference");
            }
            var ids=new HashSet<int>();foreach(var v in data.vehicles)
            {
                if(v.id<=0||!ids.Add(v.id)||v.status!=VehicleStatus.Stranded&&(!business.Exists(v.origin)||!business.Exists(v.destination))||v.cargo<0||v.price<0||double.IsNaN(v.price)||double.IsInfinity(v.price)||v.status<VehicleStatus.Active||v.status>VehicleStatus.Stranded)throw new ArgumentException("Invalid vehicle");maxV=Math.Max(maxV,v.id);
                if(v.route==null||v.route.Count==0||v.cursor<0||v.cursor>=v.route.Count||v.distance<0||double.IsNaN(v.distance)||double.IsInfinity(v.distance))throw new ArgumentException("Invalid vehicle cursor");
                if(v.status==VehicleStatus.Stranded)
                {
                    double length=Point.Distance(v.strandedPosition,new Point());if(double.IsNaN(length)||double.IsInfinity(length))throw new ArgumentException("Invalid stranded position");
                    if(Network.Contains(v.Current)&&(v.distance>Network.Get(v.Current).Geometry.Length+1e-8||Point.Distance(v.strandedPosition,LaneChanges.Position(Network,v.Current,v.distance,v.changeTarget,v.changeElapsed))>1e-7))throw new ArgumentException("Interrupted occupancy disagrees with position");continue;
                }
                Network.ValidateRoute(v.route);if(v.cursor<0||v.cursor>=v.route.Count||v.distance<0||double.IsNaN(v.distance)||double.IsInfinity(v.distance)||v.distance>Network.Get(v.Current).Geometry.Length+1e-8)throw new ArgumentException("Invalid vehicle position");
                if(double.IsNaN(v.changeElapsed)||double.IsInfinity(v.changeElapsed)||v.changeElapsed<0||v.changeElapsed>=1||v.changeTarget==0&&v.changeElapsed!=0)throw new ArgumentException("Invalid lane change phase");
                if(v.changeTarget!=0){var source=Network.Get(v.Current);var other=Network.Get(v.changeTarget);if(source.IsConnection||other.IsConnection||source.RoadId!=other.RoadId||source.Reverse!=other.Reverse||Math.Abs(source.LaneIndex-other.LaneIndex)!=1||!LaneChanges.Route(Network,v,other.Id,null))throw new ArgumentException("Invalid lane change target/route");}
                var target=GetAccess(v.destination);var end=Network.Get(v.route[v.route.Count-1]);
                if(target!=null){if(!target.valid||end.Id!=target.lane||Math.Abs(EndDistance(v,end)-target.distance)>1e-7)throw new ArgumentException("Route does not reach lane access");}
                else {var provider=business as IJunctionEndpointProvider;if(provider==null||end.To!=provider.Junction(v.destination))throw new ArgumentException("Route does not reach task destination");}
                if(double.IsNaN(v.arrivalDistance)||double.IsInfinity(v.arrivalDistance)||v.arrivalDistance<-1||EndDistance(v,end)>end.Geometry.Length||v.cursor==v.route.Count-1&&v.distance>EndDistance(v,end)+1e-8)throw new ArgumentException("Invalid terminal distance");
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
