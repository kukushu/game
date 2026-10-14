using System;
using System.Collections.Generic;
#if UNITY_5_3_OR_NEWER
using Unity.Profiling;
#endif

namespace HarborCity.RoadCore
{
    public enum VehicleStatus { Active,Delivered,Stranded }
    public enum TransportPurpose { Freight,Work,Home }
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
        public TransportPurpose purpose {get;internal set;}
        public int citizenId {get;internal set;}
        public double departedAt {get;internal set;}
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
        public int coreFormat=10,nextVehicle=1,nextDepot=1,networkRevision;
        public bool externalBusiness;
        public SimulationClockData standaloneClock=new SimulationClockData();
        public List<int> availableEndpoints=new List<int>();
        public int delivered;
        public NetworkData network=new NetworkData();
        public List<Depot> depots=new List<Depot>();
        public List<Vehicle> vehicles=new List<Vehicle>();
        public List<RoadAccess> accesses=new List<RoadAccess>();
    }
    public sealed class World
    {
        public const double StepSeconds=SimulationClock.StepSeconds,MinimumSpacing=1.5;
        readonly WorldData data;
        readonly DepotBusiness business;
        readonly SimulationClock timeline;
        readonly HashSet<int> availableEndpoints=new HashSet<int>();
        readonly List<TransportArrival> arrivals=new List<TransportArrival>();
        public IReadOnlyList<TransportArrival> Arrivals=>arrivalsView;
        readonly IReadOnlyList<TransportArrival> arrivalsView;
        public Network Network {get;private set;}
        readonly IReadOnlyList<Vehicle> vehicleView;readonly IReadOnlyList<Depot> depotView;
        public IReadOnlyList<Vehicle> Vehicles=>vehicleView;
        public IEnumerable<Depot> Depots=>depotView;
        public double Clock=>timeline.Seconds;
        public int Delivered=>data.delivered;
        public int ActiveCount {get;private set;}
        public int NetworkGeneration=>data.networkRevision;
        internal long EditEpoch {get;private set;}
        public long AccessRevision {get;private set;}
        readonly Dictionary<long,List<LaneOccupant>> occupancy=new Dictionary<long,List<LaneOccupant>>(CityRoadKeyComparer.Instance);
        readonly JunctionReservations reservations=new JunctionReservations(null);
        readonly Dictionary<int,Depot> depotsById=new Dictionary<int,Depot>();
        readonly Dictionary<int,Vehicle> vehiclesById=new Dictionary<int,Vehicle>();
        readonly Dictionary<int,RoadAccess> accesses=new Dictionary<int,RoadAccess>();
        readonly DispatchRoutes dispatchRoutes=new DispatchRoutes();
        readonly LostOccupancy lostOccupancy=new LostOccupancy();
        public long RoutePreparations=>dispatchRoutes.Preparations;
        public int CachedRoutes=>dispatchRoutes.Count;
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

        public World(WorldData state,SimulationClock clock=null,IEnumerable<int> endpoints=null)
        {
            if(state==null||state.coreFormat!=10||state.externalBusiness!=(clock!=null))throw new ArgumentException("Unsupported road-core save or missing business owner. Legacy city saves are not supported.");
            if(state.externalBusiness&&(state.standaloneClock.tick!=0||state.standaloneClock.pendingSeconds!=0))throw new ArgumentException("City transport cannot own a second clock");
            data=state; // Take ownership of a private copy, never the caller's mutable save DTO.
            timeline=clock??new SimulationClock(state.standaloneClock);data=Snapshot();if(endpoints!=null)data.availableEndpoints=new List<int>(endpoints);arrivalsView=arrivals.AsReadOnly();foreach(int endpoint in data.availableEndpoints)if(endpoint<=0||!availableEndpoints.Add(endpoint))throw new ArgumentException("Duplicate/invalid endpoint");vehicleView=data.vehicles.AsReadOnly();depotView=data.depots.AsReadOnly();foreach(var depot in data.depots)depotsById.Add(depot.id,depot);
            if(data.externalBusiness&&data.depots.Count!=0)throw new ArgumentException("External city cannot contain duplicate fixture inventories");foreach(var access in data.accesses)accesses.Add(access.endpoint,access);
            foreach(var vehicle in data.vehicles)vehiclesById.Add(vehicle.id,vehicle);business=data.externalBusiness?null:new DepotBusiness(depotsById);Network=new Network(data.network);Validate();AssertSafety();
        }
        // Only serializers may receive this copy; callers cannot mutate the live facts.
        public WorldData Snapshot()
        {
            var copy=new WorldData{coreFormat=10,networkRevision=data.networkRevision,externalBusiness=data.externalBusiness,nextVehicle=data.nextVehicle,nextDepot=data.nextDepot,standaloneClock=data.externalBusiness?new SimulationClockData():timeline.Snapshot(),availableEndpoints=new List<int>(data.availableEndpoints),delivered=data.delivered,network=CopyNetwork()};
            foreach(var access in data.accesses)copy.accesses.Add(access.Copy());
            foreach(var d in data.depots)copy.depots.Add(new Depot{id=d.id,junction=d.junction,stock=d.stock,capacity=d.capacity,cash=d.cash});
            foreach(var v in data.vehicles)copy.vehicles.Add(new Vehicle{id=v.id,origin=v.origin,destination=v.destination,purpose=v.purpose,citizenId=v.citizenId,departedAt=v.departedAt,cargo=v.cargo,price=v.price,distance=v.distance,arrivalDistance=v.arrivalDistance,changeTarget=v.changeTarget,changeElapsed=v.changeElapsed,cursor=v.cursor,status=v.status,strandedPosition=v.strandedPosition,route=new List<long>(v.route)});
            return copy;
        }
        NetworkData CopyNetwork()=>CloneNetwork(data.network);
        static NetworkData CloneNetwork(NetworkData source)
        {
            var n=new NetworkData{nextJunction=source.nextJunction,nextRoad=source.nextRoad};
            foreach(var j in source.junctions)n.junctions.Add(new Junction{id=j.id,position=j.position});
            foreach(var r in source.roads)n.roads.Add(new PhysicalRoad{id=r.id,from=r.from,to=r.to,direction=r.direction,lanes=r.lanes,speed=r.speed,points=(Point[])r.points.Clone(),rightVectors=r.rightVectors==null?null:(Point[])r.rightVectors.Clone(),startTangent=r.startTangent,endTangent=r.endTangent});return n;
        }
        internal RoadEditState CaptureRoadEdit(){var accesses=new List<RoadAccess>();foreach(var access in data.accesses)accesses.Add(access.Copy());return new RoadEditState{network=CopyNetwork(),accesses=accesses};}
        internal RoadEditPlan PlanRestoreRoadEdit(RoadEditState state)
        {
            var n=CloneNetwork(state.network);n.nextRoad=Math.Max(n.nextRoad,data.network.nextRoad);n.nextJunction=Math.Max(n.nextJunction,data.network.nextJunction);
            foreach(var depot in data.depots)if(!n.junctions.Exists(j=>j.id==depot.junction)){var current=data.network.junctions.Find(j=>j.id==depot.junction);n.junctions.Add(new Junction{id=current.id,position=current.position});}
            var candidate=new Network(n);var oldAccess=new Dictionary<int,RoadAccess>();foreach(var access in state.accesses)oldAccess.Add(access.endpoint,access);var proposed=new List<RoadAccess>();var hits=new List<SpatialHit>();
            foreach(var access in data.accesses)
            {
                RoadAccess prior;if(oldAccess.TryGetValue(access.endpoint,out prior)){proposed.Add(prior.Copy());continue;}var restored=access.Copy();if(restored.valid){candidate.Project(Network.Get(access.lane).Geometry.Sample(access.distance),1e-6,false,hits);if(hits.Count==0){restored.valid=false;restored.lane=0;restored.distance=0;}else{restored.lane=hits[0].drivable;restored.distance=hits[0].distance;restored.preferredIndex=candidate.Get(restored.lane).LaneIndex;}}proposed.Add(restored);
            }
            var targetAccesses=new Dictionary<int,RoadAccess>();foreach(var access in proposed)targetAccesses.Add(access.endpoint,access);var remaps=new List<Remap>();var interrupted=new List<KeyValuePair<Vehicle,Point>>();
            foreach(var v in data.vehicles)
            {
                if(v.status!=VehicleStatus.Active)continue;var old=Network.Get(v.Current);if(v.changeTarget!=0&&!candidate.Contains(v.changeTarget))throw new InvalidOperationException("正在换道使用待撤路面，等待完成后再撤销；城市未修改。");
                Point center=old.Geometry.Sample(v.distance);candidate.Project(center,1e-6,true,hits);SpatialHit? located=null;foreach(var hit in hits){var next=candidate.Get(hit.drivable);if(next.IsConnection!=old.IsConnection)continue;if(!next.IsConnection&&(next.LaneIndex!=old.LaneIndex||next.Reverse!=old.Reverse))continue;located=hit;break;}
                if(!located.HasValue){interrupted.Add(new KeyValuePair<Vehicle,Point>(v,Position(v)));continue;}var current=candidate.Get(located.Value.drivable);long source=current.IsConnection?(uint)(-current.Id-1):current.Id;double start=current.IsConnection?0:located.Value.distance,end=-1;List<long> route=null;RoadAccess goal;
                if(targetAccesses.TryGetValue(v.destination,out goal)&&goal.valid)route=AccessRouting.FromLane(candidate,source,start,goal,v.purpose!=TransportPurpose.Freight,out end);
                else if(goal==null){var provider=business as IJunctionEndpointProvider;if(provider!=null){var tail=candidate.Route(candidate.Get(source).To,provider.Junction(v.destination));if(tail!=null){route=new List<long>{source};if(tail.Count>0){long link=Network.ConnectionId(source,tail[0]);if(candidate.Contains(link)){route.Add(link);route.AddRange(tail);}else route=null;}if(route!=null)end=candidate.Get(route[route.Count-1]).Geometry.Length;}}}
                if(route==null){interrupted.Add(new KeyValuePair<Vehicle,Point>(v,Position(v)));continue;}int cursor=0;if(current.IsConnection){long packed=-current.Id-1;route.Insert(0,current.Id);route.Insert(0,packed>>32);cursor=1;}remaps.Add(new Remap{vehicle=v,route=route,cursor=cursor,distance=located.Value.distance,arrival=end});
            }
            return new RoadEditPlan(this,n,candidate,remaps,interrupted,proposed);
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
        {var n=CopyNetwork();int id=n.nextRoad++;n.roads.Add(new PhysicalRoad{id=id,from=from,to=to,points=(Point[])points.Clone(),direction=oneWay?RoadDirection.Forward:RoadDirection.Both,lanes=lanes,speed=speed});var network=new Network(n);ValidateEditReservations(network,null);return new RoadEditPlan(this,n,network);}
        public RoadEditPlan PlanConnectedRoad(Point[] center,bool oneWay=false,double speed=12,double snap=3,int lanes=1)=>RoadConstruction.Connect(this,center,oneWay,speed,snap,lanes);
        public RoadEditPlan PlanUpgradeRoad(int id,int lanes,double speed,RoadDirection direction)
        {
            var n=CopyNetwork();var road=n.roads.Find(r=>r.id==id);if(road==null)throw new ArgumentException("Missing road");road.lanes=lanes;road.speed=speed;road.direction=direction;var candidate=new Network(n);
            foreach(var v in data.vehicles)if(Network.Contains(v.Current)&&!candidate.Contains(v.Current)||v.changeTarget!=0&&!candidate.Contains(v.changeTarget))throw new InvalidOperationException("待撤方向/车道仍有车辆，等待驶离后再升级；城市未修改。");
            var proposed=new List<RoadAccess>();foreach(var original in data.accesses)
            {
                var access=original.Copy();if(access.valid&&!candidate.Contains(access.lane))
                {
                    var old=Network.Get(access.lane);bool reverse=direction==RoadDirection.Backward||direction==RoadDirection.Both&&old.Reverse;long next=Network.LaneId(old.RoadId,reverse,Math.Min(old.LaneIndex,lanes-1));var replacement=candidate.Get(next);access.lane=next;access.preferredIndex=replacement.LaneIndex;access.distance=old.Reverse==reverse?old.Geometry.MapDistance(replacement.Geometry,access.distance):old.Geometry.MapReverseDistance(replacement.Geometry,access.distance);
                }
                proposed.Add(access);
            }
            var endpoints=new Dictionary<int,RoadAccess>();foreach(var access in proposed)endpoints.Add(access.endpoint,access);var remaps=new List<Remap>();var interrupted=new List<KeyValuePair<Vehicle,Point>>();
            foreach(var v in data.vehicles)
            {
                if(v.status!=VehicleStatus.Active)continue;var current=candidate.Get(v.Current);int beginning=current.IsConnection?v.cursor-1:v.cursor;var route=v.route.GetRange(beginning,v.route.Count-beginning);int cursor=current.IsConnection?1:0;double end=v.arrivalDistance;bool intact=true;
                foreach(long item in route)if(!candidate.Contains(item)){intact=false;break;}RoadAccess target;endpoints.TryGetValue(v.destination,out target);if(intact&&target!=null)intact=target.valid&&AccessRouting.Matches(candidate,target,route[route.Count-1],EndDistance(v,Network.Get(v.route[v.route.Count-1])),v.purpose!=TransportPurpose.Freight);
                if(!intact)
                {
                    long source=current.IsConnection?(uint)(-current.Id-1):current.Id;double start=current.IsConnection?0:v.distance;
                    if(target==null||!target.valid){interrupted.Add(new KeyValuePair<Vehicle,Point>(v,Position(v)));continue;}
                    route=AccessRouting.FromLane(candidate,source,start,target,v.purpose!=TransportPurpose.Freight,out end);if(route==null){interrupted.Add(new KeyValuePair<Vehicle,Point>(v,Position(v)));continue;}
                    if(current.IsConnection){long packed=-current.Id-1;route.Insert(0,current.Id);route.Insert(0,packed>>32);}
                }
                remaps.Add(new Remap{vehicle=v,route=route,cursor=cursor,distance=v.distance,arrival=end});
            }
            return new RoadEditPlan(this,n,candidate,remaps,interrupted,proposed);
        }
        internal World EditDraft()=>new World(Snapshot(),data.externalBusiness?timeline.Copy():null);
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
            new World(staged,data.externalBusiness?timeline.Copy():null);
            // Every fallible geometry/migration/reservation check precedes these writes.
            foreach(var remap in plan.remaps){long target=RemappedChangeTarget(remap.vehicle,plan.network,remap.route[remap.cursor]);remap.vehicle.route=remap.route;remap.vehicle.cursor=remap.cursor;remap.vehicle.distance=remap.distance;remap.vehicle.changeTarget=target;if(remap.vehicle.status!=VehicleStatus.Stranded)remap.vehicle.arrivalDistance=remap.arrival;}
            foreach(var item in plan.interruptions){item.Key.strandedPosition=item.Value;item.Key.status=VehicleStatus.Stranded;}
            if(plan.accesses!=null){data.accesses=plan.accesses;accesses.Clear();foreach(var access in plan.accesses)accesses.Add(access.endpoint,access);}
            data.network=plan.data;Network=plan.network;data.networkRevision++;EditEpoch++;dispatchRoutes.Reset(Network);lostOccupancy.Reset(Network);occupancy.Clear();Reindex();return true;
        }
        public int AddDepot(int junction,int stock=100)
        {if(data.externalBusiness)throw new InvalidOperationException("City owns its inventories; synthetic depots are disabled");JunctionPosition(junction);if(stock<0||stock>100000)throw new ArgumentOutOfRangeException(nameof(stock));int id=data.nextDepot++;var depot=new Depot{id=id,junction=junction,stock=stock};data.depots.Add(depot);depotsById.Add(id,depot);return id;}
        public Depot GetDepot(int id){Depot depot;if(depotsById.TryGetValue(id,out depot))return depot;throw new ArgumentException("Missing depot");}
        public Vehicle FindVehicle(int id){Vehicle vehicle;return vehiclesById.TryGetValue(id,out vehicle)?vehicle:null;}
        public RoadAccess GetAccess(int endpoint){RoadAccess access;return accesses.TryGetValue(endpoint,out access)?access:null;}
        public void RegisterAccess(int endpoint,long lane,double distance,AccessUse use=AccessUse.All)
        {
            var drivable=Network.Get(lane);if(endpoint<0||accesses.ContainsKey(endpoint)||drivable.IsConnection||distance<0||distance>drivable.Geometry.Length||double.IsNaN(distance)||double.IsInfinity(distance)||use==0||(use&~AccessUse.All)!=0)throw new ArgumentException("Invalid road access");
            var access=new RoadAccess{endpoint=endpoint,preferredIndex=drivable.LaneIndex,lane=lane,distance=distance,use=use};data.accesses.Add(access);accesses.Add(endpoint,access);
            dispatchRoutes.Reset(Network);
            AccessRevision++;
            EditEpoch++;
        }
        public bool RebindAccess(int endpoint,long lane,double distance)
        =>RebindAccesses(new[]{new AccessBinding(endpoint,lane,distance)});
        public bool RebindAccesses(IReadOnlyList<AccessBinding> bindings)
        {
            if(bindings==null||bindings.Count==0)return false;var replacements=new Dictionary<int,RoadAccess>();foreach(var binding in bindings){var old=GetAccess(binding.endpoint);var road=Network.Get(binding.lane);if(old==null||old.valid)return false;if(road.IsConnection||binding.distance<0||binding.distance>road.Geometry.Length||double.IsNaN(binding.distance)||double.IsInfinity(binding.distance))throw new ArgumentException("Invalid replacement access");replacements.Add(binding.endpoint,new RoadAccess{endpoint=binding.endpoint,preferredIndex=road.LaneIndex,lane=binding.lane,distance=binding.distance,use=old.use});}
            var proposed=new List<RoadAccess>();foreach(var access in data.accesses){RoadAccess replacement;proposed.Add(replacements.TryGetValue(access.endpoint,out replacement)?replacement:access.Copy());}var networkData=CopyNetwork();return new RoadEditPlan(this,networkData,new Network(networkData),accesses:proposed).TryCommit();
        }
        public bool TryResumeStranded(int id)
        {
            Vehicle v;if(!vehiclesById.TryGetValue(id,out v)||v.status!=VehicleStatus.Stranded||!EndpointExists(v.origin)||!EndpointExists(v.destination))return false;long current=v.Current;double distance=v.distance;
            if(!Network.Contains(current))
            {
                if(v.changeTarget!=0)return false;var hits=new List<SpatialHit>();Network.Project(v.strandedPosition,1e-6,false,hits);if(hits.Count==0)return false;current=hits[0].drivable;distance=hits[0].distance;
            }
            var surface=Network.Get(current);long source=current;double start=distance;if(surface.IsConnection){long packed=-current-1;source=(uint)packed;start=0;}
            var target=GetAccess(v.destination);List<long> route=null;double end=-1;
            if(target!=null&&target.valid)route=AccessRouting.FromLane(Network,source,start,target,v.purpose!=TransportPurpose.Freight,out end);
            else if(target==null){var provider=business as IJunctionEndpointProvider;if(provider==null)return false;var remainder=Network.Route(Network.Get(source).To,provider.Junction(v.destination));if(remainder!=null){route=new List<long>{source};if(remainder.Count>0){long link=Network.ConnectionId(source,remainder[0]);if(!Network.Contains(link))return false;route.Add(link);route.AddRange(remainder);}end=Network.Get(route[route.Count-1]).Geometry.Length;}}
            if(route==null||route.Count==0)return false;int cursor=0;if(surface.IsConnection){long packed=-current-1;route.Insert(0,current);route.Insert(0,packed>>32);cursor=1;}
            if(Point.Distance(v.strandedPosition,LaneChanges.Position(Network,current,distance,v.changeTarget,v.changeElapsed))>1e-7)return false;
            var snapshot=Snapshot();var copied=snapshot.vehicles.Find(actor=>actor.id==id);copied.route=route;copied.cursor=cursor;copied.distance=distance;copied.arrivalDistance=end;copied.status=VehicleStatus.Active;new World(snapshot,data.externalBusiness?timeline.Copy():null);
            v.route=route;v.cursor=cursor;v.distance=distance;v.arrivalDistance=end;v.status=VehicleStatus.Active;lostOccupancy.Forget(id);EditEpoch++;Reindex();return true;
        }
        public Vehicle Dispatch(int origin,int destination,int amount=1,double price=5)
        =>DispatchTask(origin,destination,amount,price,TransportPurpose.Freight,0);
        public Vehicle DispatchResident(int citizen,int origin,int destination,bool returning=false)
        {if(citizen<=0)throw new ArgumentOutOfRangeException(nameof(citizen));return DispatchTask(origin,destination,0,0,returning?TransportPurpose.Home:TransportPurpose.Work,citizen);}
        internal TransportAdmission PrepareTransport(int origin,int destination,int amount,double price,TransportPurpose purpose,int citizen)
        {
#if CITY_CORE_ATTRIBUTION
            using var observation=HarborCity.RoadCore.CityCoreProfile.Measure(HarborCity.RoadCore.CityCoreProfile.Part.Admission);
#endif

            if(data.nextVehicle>=int.MaxValue)throw new InvalidOperationException("Vehicle IDs exhausted");
            if(amount<0||purpose==TransportPurpose.Freight&&amount==0||price<0||double.IsNaN(price)||double.IsInfinity(price))throw new ArgumentOutOfRangeException("Cargo/price");
            if(origin==destination||!EndpointExists(origin)||!EndpointExists(destination))return null;
            RoadAccess source=GetAccess(origin),target=GetAccess(destination);if(source!=null&&target!=null){var use=purpose==TransportPurpose.Freight?AccessUse.Freight:AccessUse.Resident;if(!source.valid||!target.valid||(source.use&use)==0||(target.use&use)==0)return null;}
            // Freight has exactly one saved source lane/station. Physical source
            // blockage is independent of its destination; reject before path search.
            if(purpose==TransportPurpose.Freight&&FreightSourceBlocked(origin))return null;
            if(purpose==TransportPurpose.Freight&&source!=null&&target!=null&&!Network.CanReach(source,target))
            {
#if CITY_CORE_ATTRIBUTION
                CityCoreProfile.Unreachable++;
#endif
                return null;
            }
            var blueprint=dispatchRoutes.Get(Network,origin,destination,source,target,purpose!=TransportPurpose.Freight,business as IJunctionEndpointProvider);var route=blueprint.route;double start=blueprint.start,end=blueprint.end;if(route==null||route.Length==0)return null;
            if(indexDirty)Reindex();List<LaneOccupant> bucket;
            if(occupancy.TryGetValue(route[0],out bucket)){int slot=InsertionSlot(bucket,start);if(slot>0&&bucket[slot-1].distance-start<MinimumSpacing||slot<bucket.Count&&start-bucket[slot].distance<MinimumSpacing)return null;}
            var entry=Network.Get(route[0]);int reservation=start<Clearance(entry)-1e-9?entry.From:start>entry.Geometry.Length-Clearance(entry)+1e-9?entry.To:0;
            long spawnMovement=reservation==entry.To&&route.Length>1?route[1]:0;
            if(Network.IsConflictJunction(reservation)&&!reservations.CanReserve(reservation,spawnMovement,data.nextVehicle))return null;
            var v=new Vehicle{id=data.nextVehicle,origin=origin,destination=destination,purpose=purpose,citizenId=citizen,departedAt=Clock,cargo=amount,price=price,route=new List<long>(route),distance=start,arrivalDistance=end};
            if(data.vehicles.Count==data.vehicles.Capacity)data.vehicles.Capacity=Math.Max(4,data.vehicles.Capacity*2);
            return new TransportAdmission(this,EditEpoch,v);
        }
        internal Vehicle CommitAdmission(TransportAdmission admission)
        {
            if(admission==null||admission.owner!=this||admission.epoch!=EditEpoch||admission.vehicle.id!=data.nextVehicle)throw new InvalidOperationException("Stale transport admission");var v=admission.vehicle;var entry=Network.Get(v.Current);double start=v.distance;List<LaneOccupant> bucket;int reservation=start<Clearance(entry)-1e-9?entry.From:start>entry.Geometry.Length-Clearance(entry)+1e-9?entry.To:0;long spawnMovement=reservation==entry.To&&v.route.Count>1?v.route[1]:0;data.nextVehicle++;data.vehicles.Add(v);vehiclesById.Add(v.id,v);ActiveCount++;EditEpoch++;
            if(!occupancy.TryGetValue(v.Current,out bucket))occupancy.Add(v.Current,bucket=new List<LaneOccupant>());v.sourceOccupant.distance=start;bucket.Insert(InsertionSlot(bucket,start),v.sourceOccupant);if(Network.IsConflictJunction(reservation))reservations.Reserve(reservation,spawnMovement,v.id);return v;
        }
        Vehicle DispatchTask(int origin,int destination,int amount,double price,TransportPurpose purpose,int citizen)
        {if(data.externalBusiness)throw new InvalidOperationException("Use CitySimulation.RequestFreight/RequestCommute");var prepared=PrepareTransport(origin,destination,amount,price,purpose,citizen);if(prepared==null||!business.Depart(prepared.vehicle))return null;return CommitAdmission(prepared);}
        static int InsertionSlot(List<LaneOccupant> bucket,double position){int lo=0,hi=bucket.Count;while(lo<hi){int mid=(lo+hi)/2;if(bucket[mid].distance>position)lo=mid+1;else hi=mid;}return lo;}
        internal bool FreightSourceBlocked(int origin)
        {
            var source=GetAccess(origin);if(source==null||!source.valid)return false;if(indexDirty)Reindex();
            if(!occupancy.TryGetValue(source.lane,out var bucket))return false;int slot=InsertionSlot(bucket,source.distance);
            return slot>0&&bucket[slot-1].distance-source.distance<MinimumSpacing||slot<bucket.Count&&source.distance-bucket[slot].distance<MinimumSpacing;
        }
        public double EstimateResidentSeconds(int origin,int destination)
        {var a=GetAccess(origin);var b=GetAccess(destination);if(a==null||b==null||!a.valid||!b.valid||(a.use&AccessUse.Resident)==0||(b.use&AccessUse.Resident)==0||!EndpointExists(origin)||!EndpointExists(destination))return double.PositiveInfinity;return dispatchRoutes.Get(Network,origin,destination,a,b,true,null).seconds;}
        static double EndDistance(Vehicle v,Drivable road)=>v.arrivalDistance<0?road.Geometry.Length:v.arrivalDistance;
        public Point Position(Vehicle v)=>v.status==VehicleStatus.Stranded?v.strandedPosition:LaneChanges.Position(Network,v.Current,v.distance,v.changeTarget,v.changeElapsed);
        long RemappedChangeTarget(Vehicle v,Network candidate,long current)
        {if(v.changeTarget==0)return 0;var lane=candidate.Get(current);var old=Network.Get(v.changeTarget);long target=Network.LaneId(lane.RoadId,lane.Reverse,old.LaneIndex);if(lane.IsConnection||!candidate.Contains(target))throw new InvalidOperationException("Construction removes a changing lane; retry after the maneuver completes");return target;}
        public bool TryChangeLane(int vehicleId,int targetIndex)
        {Vehicle v;if(!vehiclesById.TryGetValue(vehicleId,out v))return false;if(indexDirty)Reindex();if(!LaneChanges.TryStart(Network,occupancy,v,targetIndex))return false;EditEpoch++;return true;}
        public void RefreshEndpoints()
        {foreach(var v in data.vehicles)if(v.status==VehicleStatus.Active&&(!EndpointExists(v.origin)||!EndpointExists(v.destination))){v.strandedPosition=Position(v);v.status=VehicleStatus.Stranded;}EditEpoch++;Reindex();}
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
        public int Advance(double simulationSeconds)
        {
            if(simulationSeconds<0||double.IsNaN(simulationSeconds)||double.IsInfinity(simulationSeconds))throw new ArgumentException("Invalid time");
            if(data.externalBusiness)throw new InvalidOperationException("CitySimulation owns time");timeline.Request(simulationSeconds);int steps=0;while(timeline.HasStep){Step();timeline.ConsumeDebt();steps++;}return steps;
        }
        void Reindex()
        {
#if CITY_CORE_ATTRIBUTION
            using var observation=HarborCity.RoadCore.CityCoreProfile.Measure(HarborCity.RoadCore.CityCoreProfile.Part.Occupancy);
#endif

            foreach(var b in occupancy.Values)b.Clear();ActiveCount=0;reservations.Reset(Network);
            foreach(var v in data.vehicles)
            {
                if(v.status==VehicleStatus.Active&&(!EndpointExists(v.origin)||!EndpointExists(v.destination))){v.strandedPosition=Position(v);v.status=VehicleStatus.Stranded;}
                if(v.status==VehicleStatus.Delivered)continue;
                if(!Network.Contains(v.Current))
                {
                    if(v.status!=VehicleStatus.Stranded)throw new InvalidOperationException("Active actor lost its drivable");
                    foreach(var placement in lostOccupancy.Get(Network,v))
                    {
                        List<LaneOccupant> bucket;if(!occupancy.TryGetValue(placement.lane,out bucket))occupancy.Add(placement.lane,bucket=new List<LaneOccupant>());bucket.Add(placement.occupant);var road=Network.Get(placement.lane);int obstacleJunction=road.JunctionId;if(obstacleJunction==0&&placement.occupant.distance<Clearance(road))obstacleJunction=road.From;if(!road.IsConnection&&placement.occupant.distance>road.Geometry.Length-Clearance(road))obstacleJunction=road.To;if(Network.IsConflictJunction(obstacleJunction))reservations.Reserve(obstacleJunction,0,v.id);
                    }
                    continue;
                }
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
            if(data.externalBusiness)throw new InvalidOperationException("CitySimulation owns city steps");MoveStep(timeline.Context);foreach(var arrival in arrivals)if(business.Deliver(arrival))AcknowledgeArrival(arrival.id);RetireCompleted();timeline.Commit(false);
        }
        internal void MoveStep(TimeContext context)
        {
#if CITY_CORE_ATTRIBUTION
            using var observation=HarborCity.RoadCore.CityCoreProfile.Measure(HarborCity.RoadCore.CityCoreProfile.Part.Movement);
#endif

            if(context.Tick!=timeline.Tick)throw new InvalidOperationException("Mismatched simulation step");arrivals.Clear();
#if UNITY_5_3_OR_NEWER
            using(stepSample.Auto())
#endif
            {
#if UNITY_5_3_OR_NEWER
                using(indexSample.Auto())
#endif
                Reindex();
                if(Network.HasParallelLanes)EditEpoch+=LaneChanges.Choose(Network,data.vehicles,occupancy,(int)(timeline.Tick%int.MaxValue));
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
                    arrivals.Add(new TransportArrival(v,context.End));
                }
                EditEpoch++;
                indexDirty=true;
            }
        }
        internal bool EndpointExists(int id)=>data.externalBusiness?availableEndpoints.Contains(id):business.Exists(id);
        internal void SetEndpointAvailable(int id,bool present)
        {if(!data.externalBusiness)throw new InvalidOperationException("Fixture endpoints are depots");if(present){if(availableEndpoints.Add(id))data.availableEndpoints.Add(id);}else if(availableEndpoints.Remove(id))data.availableEndpoints.Remove(id);}
        internal void AcknowledgeArrival(int id)
        {var v=FindVehicle(id);if(v==null||v.status!=VehicleStatus.Active||v.cursor!=v.route.Count-1||v.distance<EndDistance(v,Network.Get(v.Current))-1e-9)throw new InvalidOperationException("Cannot acknowledge non-arrival");data.delivered+=v.cargo;v.cargo=0;v.status=VehicleStatus.Delivered;ActiveCount--;}
        internal void RetireCompleted()
        {int retained=0;for(int i=0;i<data.vehicles.Count;i++)if(data.vehicles[i].status!=VehicleStatus.Delivered)data.vehicles[retained++]=data.vehicles[i];else vehiclesById.Remove(data.vehicles[i].id);if(retained<data.vehicles.Count)data.vehicles.RemoveRange(retained,data.vehicles.Count-retained);arrivals.Clear();indexDirty=true;}
        public int SplitRoad(int roadId,int sample)
        {
            return SplitCandidate(CopyNetwork(),roadId,sample);
        }
        public int SplitRoadAt(int roadId,double centerDistance,int existingJunction=0)
        {
            var n=CopyNetwork();var road=n.roads.Find(r=>r.id==roadId);if(road==null)throw new ArgumentException("Missing road");var geometry=new Geometry(road.points);
            if(centerDistance<=.1||centerDistance>=geometry.Length-.1||double.IsNaN(centerDistance))throw new ArgumentException("Cut must be inside the road");
            for(int i=1;i<geometry.Count-1;i++)if(Math.Abs(geometry.At(i)-centerDistance)<1e-8)return SplitCandidate(n,roadId,i,existingJunction);
            int after=1;while(geometry.At(after)<centerDistance)after++;double t=(centerDistance-geometry.At(after-1))/(geometry.At(after)-geometry.At(after-1));
            var right=Network.RightVectors(road);var points=new List<Point>(road.points);var vectors=new List<Point>(right);points.Insert(after,geometry.Sample(centerDistance));vectors.Insert(after,Point.Lerp(right[after-1],right[after],t));road.points=points.ToArray();road.rightVectors=vectors.ToArray();
            return SplitCandidate(n,roadId,after,existingJunction);
        }
        int SplitCandidate(NetworkData n,int roadId,int sample,int existingJunction=0)
        {
            var r=n.roads.Find(x=>x.id==roadId);if(r==null||sample<=0||sample>=r.points.Length-1)throw new ArgumentException("Split must be an internal geometry sample");var vectors=Network.RightVectors(r);
            int junction=existingJunction==0?n.nextJunction++:existingJunction,suffixId=n.nextRoad++;
            if(existingJunction==0)n.junctions.Add(new Junction{id=junction,position=r.points[sample]});
            else {var anchor=n.junctions.Find(j=>j.id==existingJunction);if(anchor==null||Point.Distance(anchor.position,r.points[sample])>1e-6)throw new ArgumentException("Existing junction does not coincide with cut point");}
            var suffix=new PhysicalRoad{id=suffixId,from=junction,to=r.to,direction=r.direction,lanes=r.lanes,speed=r.speed,points=new Point[r.points.Length-sample],rightVectors=new Point[r.points.Length-sample],endTangent=r.endTangent};
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
            var migratedAccesses=new Dictionary<int,RoadAccess>();foreach(var access in accessPlan)migratedAccesses.Add(access.endpoint,access);
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
                RoadAccess goal;RoadAccess desiredGoal=null;if(migratedAccesses.TryGetValue(v.destination,out goal)&&goal.valid)desiredGoal=AccessRouting.AtDirection(replacement,goal,Network.Get(v.route[v.route.Count-1]).Reverse);
                for(int i=0;i<v.route.Count;i+=2)
                {
                    long id=v.route[i];bool split=Network.Get(id).RoadId==roadId;
                    long first=id,second=0;
                    if(split){bool reverse=Network.Get(id).Reverse;first=Network.LaneId(reverse?suffixId:roadId,reverse,Network.Get(id).LaneIndex);second=Network.LaneId(reverse?roadId:suffixId,reverse,Network.Get(id).LaneIndex);}
                    bool endsInFirst=split&&i==v.route.Count-1&&v.arrivalDistance>=0&&(desiredGoal!=null?desiredGoal.lane==first:v.arrivalDistance<=replacement.Get(first).Geometry.Length);
                    if(split&&i==v.route.Count-1&&v.arrivalDistance>=0)newArrival=desiredGoal!=null?desiredGoal.distance:endsInFirst?v.arrivalDistance:v.arrivalDistance-replacement.Get(first).Geometry.Length;
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
            if(data.networkRevision<0)throw new ArgumentException("Invalid network revision");
            if(data.delivered<0)throw new ArgumentException("Invalid clock");
            var depots=new HashSet<int>();int maxD=0,maxV=0;foreach(var d in data.depots){JunctionPosition(d.junction);if(d.id<=0||!depots.Add(d.id)||d.stock<0||d.stock>d.capacity||d.cash<0||double.IsNaN(d.cash)||double.IsInfinity(d.cash))throw new ArgumentException("Invalid depot");maxD=Math.Max(maxD,d.id);}
            var attached=new HashSet<int>();foreach(var access in data.accesses)
            {
                if(access.endpoint<0||access.preferredIndex<0||access.preferredIndex>31||!attached.Add(access.endpoint)||access.use==0||(access.use&~AccessUse.All)!=0)throw new ArgumentException("Invalid access identity/permission");
                if(access.valid){var lane=Network.Get(access.lane);if(lane.IsConnection||access.distance<0||access.distance>lane.Geometry.Length||double.IsNaN(access.distance)||double.IsInfinity(access.distance))throw new ArgumentException("Invalid lane access");}
                else if(access.lane!=0||access.distance!=0)throw new ArgumentException("Invalid access retains dangling road reference");
            }
            var ids=new HashSet<int>();foreach(var v in data.vehicles)
            {
                if(v.purpose<TransportPurpose.Freight||v.purpose>TransportPurpose.Home||v.purpose==TransportPurpose.Freight&&v.citizenId!=0||v.purpose!=TransportPurpose.Freight&&(v.citizenId<=0||v.cargo!=0||v.price!=0)||v.departedAt<0||v.departedAt>Clock||double.IsNaN(v.departedAt)||double.IsInfinity(v.departedAt))throw new ArgumentException("Invalid transport purpose/owner/time");
                if(!data.externalBusiness&&v.purpose!=TransportPurpose.Freight)throw new ArgumentException("Synthetic depots cannot own residents");
                if(v.id<=0||!ids.Add(v.id)||v.status!=VehicleStatus.Stranded&&(!EndpointExists(v.origin)||!EndpointExists(v.destination))||v.cargo<0||v.price<0||double.IsNaN(v.price)||double.IsInfinity(v.price)||v.status<VehicleStatus.Active||v.status>VehicleStatus.Stranded)throw new ArgumentException("Invalid vehicle");maxV=Math.Max(maxV,v.id);
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
                if(target!=null){if(!target.valid||!AccessRouting.Matches(Network,target,end.Id,EndDistance(v,end),v.purpose!=TransportPurpose.Freight))throw new ArgumentException("Route does not reach lane access");}
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
