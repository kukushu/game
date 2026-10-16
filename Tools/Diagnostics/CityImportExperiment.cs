using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarborCity;
using HarborCity.RoadCore;

// Test-only observers. Building stock, money and all transport facts stay in domains.
static class ImportObservation
{
    internal static Probe Current;
    internal static void Depart(World world,Vehicle v){if(Current!=null&&ReferenceEquals(Current.World,world))Current.Depart(v,false);}
    internal static void Arrive(World world,Vehicle v,double at){if(Current!=null&&ReferenceEquals(Current.World,world))Current.Arrive(v,at);}
    internal sealed class Flight
    {
        internal Vehicle v;internal int cargo,origin,destination;internal bool carry;internal CargoKind kind;
        internal double departed,freeFlow,units;internal Point previous;internal double sampledUnits;
    }
    internal sealed class Port
    {
        public int id;public double x,z;public bool active;public int departed,arrived,departedCargo,arrivedCargo,newArrived,carryArrived;
        public double slotSeconds,fullSeconds;public int peak;
        public List<double> completedSeconds=new List<double>(),completedNewSeconds=new List<double>();
    }
    internal sealed class Factory
    {
        public int id;public int arrivedCargo,arrivedTrucks;public double? firstArrival,waitingSince;public double waitedSeconds;
    }
    internal sealed class Probe:IDisposable
    {
        internal readonly CityCoreIndustrySession Session;internal World World=>Session.Transport;
        internal readonly Dictionary<int,Flight> flights=new Dictionary<int,Flight>();
        internal readonly Dictionary<int,Port> ports=new Dictionary<int,Port>();
        internal readonly Dictionary<int,Factory> factories=new Dictionary<int,Factory>();
        readonly StreamWriter trips,retail,demand;readonly double started;readonly long firstTick;
        readonly Dictionary<int,int> rawIncoming=new Dictionary<int,int>();
        internal long goodsArrived;internal double observerMs;internal long observerBytes;
        internal Probe(CityCoreIndustrySession s,string output)
        {
            Session=s;started=s.Simulation.Seconds;firstTick=s.Simulation.Tick;
            foreach(var p in s.City.trade.connections)if(p.flow==OutsideFlow.Import)ports.Add(p.id,new Port{id=p.id,x=p.x,z=p.z,active=p.active});
            foreach(var f in s.Entities.Factories)factories.Add(f.id,new Factory{id=f.id});
            trips=new StreamWriter(Path.Combine(output,"trips.jsonl"));retail=new StreamWriter(Path.Combine(output,"retail.jsonl"));demand=new StreamWriter(Path.Combine(output,"demand.jsonl"));
            foreach(var v in World.Vehicles)Depart(v,true);
            Demand();Retail();
        }
        internal void Depart(Vehicle v,bool carry)
        {
            if(v.Task.purpose!=TransportPurpose.Freight||v.Task.Shipment==null)return;
            long at=Stopwatch.GetTimestamp(),bytes=GC.GetAllocatedBytesForCurrentThread();
            var f=new Flight{v=v,cargo=v.Task.Cargo,origin=v.Task.origin,destination=v.Task.destination,kind=v.Task.Shipment.kind,carry=carry,departed=v.Task.departedAt,previous=World.Position(v)};
            if(!carry)
            {
                var source=World.GetAccess(f.origin);if(source==null||source.lane!=v.Route[0])throw new Exception("New freight route does not start at its real access");
                for(int i=0;i<v.Route.Count;i++){var d=World.Network.Get(v.Route[i]);double length=(i==v.Route.Count-1?v.Plan.EndDistance:d.EndDistance)-(i==0?source.distance:d.StartDistance);f.units+=length;f.freeFlow+=length/d.Speed;}
                if(ports.TryGetValue(f.origin,out var port)&&f.kind==CargoKind.RawMaterial){port.departed++;port.departedCargo+=f.cargo;}
            }
            flights.Add(v.Task.id,f);
            trips.WriteLine(Experiment.Json.Serialize(new{eventType=carry?"carry-in":"depart",tick=Session.Simulation.Tick,task=v.Task.id,vehicle=v.id,f.origin,f.destination,kind=f.kind.ToString(),f.cargo,f.carry,f.departed,routeWorldUnits=carry?(double?)null:f.units,freeFlowSeconds=carry?(double?)null:f.freeFlow}));
            observerMs+=(Stopwatch.GetTimestamp()-at)*1000.0/Stopwatch.Frequency;observerBytes+=GC.GetAllocatedBytesForCurrentThread()-bytes;
        }
        internal void Arrive(Vehicle v,double at)
        {
            if(v.Task.purpose!=TransportPurpose.Freight||v.Task.Shipment==null)return;
            long stamp=Stopwatch.GetTimestamp(),bytes=GC.GetAllocatedBytesForCurrentThread();
            if(!flights.TryGetValue(v.Task.id,out var f))throw new Exception("Unobserved freight arrival");
            f.sampledUnits+=Point.Distance(f.previous,World.Position(v));double duration=at-f.departed;
            if(f.kind==CargoKind.RawMaterial&&ports.TryGetValue(f.origin,out var p))
            {
                p.arrived++;p.arrivedCargo+=f.cargo;p.completedSeconds.Add(duration);if(f.carry)p.carryArrived++;else{p.newArrived++;p.completedNewSeconds.Add(duration);}
                var factory=factories[f.destination];factory.arrivedCargo+=f.cargo;factory.arrivedTrucks++;if(!factory.firstArrival.HasValue)factory.firstArrival=at-started;
                if(factory.waitingSince.HasValue){factory.waitedSeconds+=at-factory.waitingSince.Value;factory.waitingSince=null;}
            }
            else if(f.kind==CargoKind.Goods&&Session.Entities.Building(f.destination) is CommercialBuilding)goodsArrived+=f.cargo;
            trips.WriteLine(Experiment.Json.Serialize(new{eventType="arrive",tick=Session.Simulation.Tick+1,task=v.Task.id,vehicle=v.id,f.origin,f.destination,kind=f.kind.ToString(),f.cargo,f.carry,at,duration,freeFlowSeconds=f.carry?(double?)null:f.freeFlow,routeWorldUnits=f.carry?(double?)null:f.units,sampledWorldUnits=f.sampledUnits,remainingCargo=v.Task.Cargo}));
            flights.Remove(v.Task.id);observerMs+=(Stopwatch.GetTimestamp()-stamp)*1000.0/Stopwatch.Frequency;observerBytes+=GC.GetAllocatedBytesForCurrentThread()-bytes;
        }
        internal void AfterStep()
        {
            var counts=new Dictionary<int,int>();
            foreach(var f in flights.Values)
            {
                var point=World.Position(f.v);f.sampledUnits+=Point.Distance(f.previous,point);f.previous=point;
                if(ports.ContainsKey(f.origin)){counts.TryGetValue(f.origin,out int n);counts[f.origin]=n+1;}
            }
            foreach(var p in ports.Values){counts.TryGetValue(p.id,out int n);if(n>Session.Simulation.Design.Economy.MaxGateTrucks)throw new Exception("Observed gate cap violated");p.slotSeconds+=n*World.StepSeconds;if(n==Session.Simulation.Design.Economy.MaxGateTrucks)p.fullSeconds+=World.StepSeconds;p.peak=Math.Max(p.peak,n);}
            if((Session.Simulation.Tick-firstTick)%20==0){Demand();Retail();trips.Flush();retail.Flush();demand.Flush();}
        }
        void Demand()
        {
            rawIncoming.Clear();foreach(var f in flights.Values)if(f.kind==CargoKind.RawMaterial){rawIncoming.TryGetValue(f.destination,out int n);rawIncoming[f.destination]=n+f.cargo;}
            long gap=0,affordable=0;int requesting=0,staffedStarved=0;
            foreach(var f in Session.Entities.Factories)
            {
                rawIncoming.TryGetValue(f.id,out int incoming);int missing=Math.Max(0,Session.Simulation.Design.Economy.RawReorderTarget-f.factory.raw-incoming);
                int funded=(int)Math.Min(missing,Math.Max(0,Math.Floor(f.factory.cash/Session.Simulation.Design.Economy.RawUnitPrice)));
                gap+=missing;affordable+=funded;if(funded>0&&Session.City.BuildingAccess(f.id)&&Session.City.Supply(f.id).Complete){requesting++;if(!factories[f.id].waitingSince.HasValue)factories[f.id].waitingSince=Session.Simulation.Seconds;}
                else if(factories[f.id].waitingSince.HasValue){factories[f.id].waitedSeconds+=Session.Simulation.Seconds-factories[f.id].waitingSince.Value;factories[f.id].waitingSince=null;}
                if(Session.Simulation.Payroll.WorkersAt(f.id)>0&&f.factory.raw==0&&!f.factory.processing)staffedStarved++;
            }
            demand.WriteLine(Experiment.Json.Serialize(new{tick=Session.Simulation.Tick,reorderGap=gap,affordableGap=affordable,requesting,paidStaffedNoRaw=staffedStarved,rawDelivered=Session.City.trade.rawDelivered,produced=Session.Entities.Factories.Sum(f=>(long)f.factory.produced),consumed=Session.City.consumption.ConsumedGoods}));
        }
        void Retail()
        {
            var watched=new HashSet<int>(flights.Values.Where(f=>f.kind==CargoKind.Goods).Select(f=>f.destination));
            var method=typeof(CityCoreConsumption).GetMethod("Open",BindingFlags.NonPublic|BindingFlags.Instance);
            var workers=new Dictionary<int,int>();foreach(var m in Session.Entities.Members)if(m.Person.atWork){int id=Session.City.Workplace(m.Person);workers.TryGetValue(id,out int n);workers[id]=n+1;}
            retail.WriteLine(Experiment.Json.Serialize(new{tick=Session.Simulation.Tick,day=Session.City.day,minute=Session.City.ResidentMinute,stock=Session.Entities.Shops.Sum(s=>(long)s.Stock),shops=Session.Entities.Shops.Where(s=>s.Stock>0||watched.Contains(s.id)).Select(s=>new{s.id,s.Stock,s.Account.Cash,s.Account.WholesaleFunded,s.Account.WageCosts,actualAtWork=workers.TryGetValue(s.id,out int n)?n:0,paidStaffed=Session.Simulation.Payroll.WorkersAt(s.id),open=(bool)method.Invoke(Session.Simulation.Consumption,new object[]{s})}).ToArray(),consumed=Session.City.consumption.ConsumedGoods}));
        }
        internal object Summary(double elapsed)=>new{ports=ports.Values.Select(p=>new{p.id,p.x,p.z,p.active,p.departed,p.arrived,p.departedCargo,p.arrivedCargo,p.newArrived,p.carryArrived,slotOccupancy=elapsed==0?0:p.slotSeconds/(elapsed*Session.Simulation.Design.Economy.MaxGateTrucks),fullFraction=elapsed==0?0:p.fullSeconds/elapsed,p.peak,allCompletedSeconds=Experiment.Dist(p.completedSeconds),newCompletedSeconds=Experiment.Dist(p.completedNewSeconds),incomplete=flights.Values.Where(f=>f.origin==p.id&&f.kind==CargoKind.RawMaterial).Select(f=>new{task=f.v.Task.id,f.carry,age=Session.Simulation.Seconds-f.departed,f.cargo}).ToArray()}).ToArray(),goodsArrived,servedFactories=factories.Values.Count(f=>f.arrivedCargo>0),firstArrivalSeconds=Experiment.Dist(factories.Values.Where(f=>f.firstArrival.HasValue).Select(f=>f.firstArrival.Value)),unservedFactories=factories.Values.Count(f=>f.arrivedCargo==0),waitingDefinition="first observed affordable refill need within this run; not historical order creation"};
        internal void WriteFactories(string path)
        {using(var w=new StreamWriter(path)){w.WriteLine("factory,raw_delivered,trucks,first_arrival_seconds,observed_wait_seconds,still_waiting_since");foreach(var f in factories.Values){double wait=f.waitedSeconds+(f.waitingSince.HasValue?Session.Simulation.Seconds-f.waitingSince.Value:0);w.WriteLine(string.Join(",",f.id,f.arrivedCargo,f.arrivedTrucks,f.firstArrival,wait,f.waitingSince));}}}
        public void Dispose(){trips.Dispose();retail.Dispose();demand.Dispose();}
    }
}

static class Experiment
{
    internal static readonly CityScenarioJson Json=new CityScenarioJson();
    static CityCoreIndustrySession active;
    static void Write(string path,object value)=>File.WriteAllText(path,Json.Serialize(value));
    internal static object Dist(IEnumerable<double> source){var a=source.OrderBy(x=>x).ToArray();return a.Length==0?null:new{count=a.Length,mean=a.Average(),p50=a[(int)Math.Ceiling(a.Length*.5)-1],p95=a[(int)Math.Ceiling(a.Length*.95)-1],p99=a[(int)Math.Ceiling(a.Length*.99)-1],max=a.Last()};}
    static string Hash(byte[] bytes)=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
    static CityCoreIndustrySession Load(byte[] bytes){var s=CityCoreSave.Read(bytes);s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);s.Validate();return s;}
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static string BusinessHash(CityCoreBusinessData data)
    {using(var m=new MemoryStream()){using(var w=new BinaryWriter(m,System.Text.Encoding.UTF8,true))typeof(CityCoreSave).GetMethod("Put",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{w,typeof(CityCoreBusinessData),data});return Hash(m.ToArray());}}
    static void Build(string input,string output,int radius,int spacing)
    {
        var original=File.ReadAllBytes(input);var s=Load(original);active=s;var before=s.SnapshotBusiness();int money=s.City.money;var positions=s.Transport.Vehicles.ToDictionary(v=>v.id,v=>s.Transport.Position(v));var statuses=s.Transport.Vehicles.ToDictionary(v=>v.id,v=>v.status);
        Require(s.City.trade.connections.Count==2&&s.City.trade.connections.Count(p=>p.active&&p.flow==OutsideFlow.Import)==1,"Expected preserved one-import reference city");
        double edge=radius*spacing,border=s.City.map.buildHalfSize-1,offset=Math.Round(radius/2.0)*spacing;int paid=0;
        var points=new List<(Point outer,Point inner,string label)>();
        foreach(var side in new[]{"west","east","south","north"})foreach(double along in new[]{0.0,-offset,offset})
        {
            var outer=side=="west"?new Point(-border,2,along):side=="east"?new Point(border,2,along):side=="south"?new Point(along,2,-border):new Point(along,2,border);
            var inner=side=="west"?new Point(-edge,2,along):side=="east"?new Point(edge,2,along):side=="south"?new Point(along,2,-edge):new Point(along,2,edge);
            points.Add((outer,inner,side+":"+along));
        }
        Require(edge<border&&offset>0&&offset<edge,"Invalid legal outer-grid connection roster");
        var imports=new List<int>{s.City.trade.connections.Single(p=>p.flow==OutsideFlow.Import).id};var cardinal=new HashSet<int>{imports[0]};
        foreach(var p in points)
        {
            if(p.label=="west:0")continue;
            var plan=s.Transport.PlanConnectedRoad(new[]{p.outer,p.inner},lanes:s.Transport.Network.Settings(s.Transport.Network.RoadIds.First()).lanes,snap:.1);
            s.Construction.CheckRoadPlacement(plan);Require(s.Construction.Commit(plan,out int cost),"Boundary stub plan became stale");paid+=cost;
            int count=s.City.trade.connections.Count;s.Simulation.Trade.ConnectBorder(p.outer);var added=s.City.trade.connections.Skip(count).ToArray();
            Require(added.Length==2&&added.Count(c=>c.flow==OutsideFlow.Import)==1,"Expected one legal directional import/export pair");
            foreach(var c in added){if(c.flow==OutsideFlow.Import){imports.Add(c.id);if(p.label.EndsWith(":0",StringComparison.Ordinal))cardinal.Add(c.id);}s.Entities.SetConnectionActive(c.id,false);}
            Console.WriteLine("Added common legal stub "+p.label+"; cost="+cost);
        }
        Require(imports.Count==12&&cardinal.Count==4,"Wrong importer roster");s.Validate();
        Require(s.Transport.Vehicles.Count==positions.Count,"Construction removed vehicles");foreach(var v in s.Transport.Vehicles)Require(v.status==statuses[v.id]&&Point.Distance(positions[v.id],s.Transport.Position(v))<1e-7,"Construction moved or interrupted an existing vehicle");
        var after=s.SnapshotBusiness();after.money=before.money;after.treasury=before.treasury;after.nextBuilding=before.nextBuilding;after.trade.connections=before.trade.connections;
        Require(BusinessHash(after)==BusinessHash(before),"Common preparation changed non-permitted business facts");
        byte[] common=CityCoreSave.Write(s);File.WriteAllBytes(Path.Combine(output,"common-inactive.hcity"),common);
        var arms=new List<object>();
        foreach(int number in new[]{1,4,12})
        {
            foreach(int id in imports.Skip(1))s.Entities.SetConnectionActive(id,number==12||number==4&&cardinal.Contains(id));
            s.Validate();string dir=Path.Combine(output,"arm-"+number);Directory.CreateDirectory(dir);byte[] bytes=CityCoreSave.Write(s);File.WriteAllBytes(Path.Combine(dir,"initial.hcity"),bytes);
            foreach(int id in imports.Skip(1))s.Entities.SetConnectionActive(id,false);
            Require(common.SequenceEqual(CityCoreSave.Write(s)),"Arms differ beyond endpoint activation and endpoint availability");
            arms.Add(new{imports=number,initialHash=Hash(bytes),population=s.Entities.Members.Count});
        }
        Write(Path.Combine(output,"construction.json"),new{inputHash=Hash(original),commonHash=Hash(common),population=s.Entities.Members.Count,roadsBefore=before==null?0:s.Transport.Network.RoadCount-11,roadsAfter=s.Transport.Network.RoadCount,commonAddedRoads=11,commonConstructionCost=paid,treasuryBefore=money,treasuryAfter=s.City.money,arms,imports,cardinal=cardinal.ToArray(),sameBusinessExceptDeclaredFields=true,positionsPreserved=true,onlyArmDifference="active imports and matching endpoint availability; same roads, registered IDs, export capacity, finances and business facts"});
    }
    static void Budget(string input,string output,int samples)
    {
        var s=Load(File.ReadAllBytes(input));byte[] before=CityCoreSave.Write(s);var network=new Network(s.Transport.Snapshot().network);var fs=s.Entities.Factories;var rows=new List<object>();
        foreach(var p in s.City.trade.connections.Where(p=>p.flow==OutsideFlow.Import))
        {
            var a=s.Transport.GetAccess(p.id);int reachable=0;foreach(var f in fs){var b=s.Transport.GetAccess(f.id);if(b!=null&&b.valid&&network.CanReach(a,b))reachable++;}
            var times=new List<double>();var lengths=new List<double>();
            for(int i=0;i<Math.Min(samples,fs.Count);i++){var f=fs[i*fs.Count/Math.Min(samples,fs.Count)];var route=AccessRouting.Find(network,a,s.Transport.GetAccess(f.id),false,out double start,out double end);if(route==null)continue;double units=0,seconds=0;for(int n=0;n<route.Count;n++){var d=network.Get(route[n]);double l=(n==route.Count-1?end:d.EndDistance)-(n==0?start:d.StartDistance);units+=l;seconds+=l/d.Speed;}lengths.Add(units);times.Add(seconds);}
            rows.Add(new{p.id,p.x,p.z,p.active,reachableFactories=reachable,sampleCount=times.Count,sampleRouteWorldUnits=Dist(lengths),sampleFreeFlowSeconds=Dist(times),estimatedRawPerGameDayIfFull=times.Count==0?0:s.Simulation.Design.Economy.MaxGateTrucks*s.Simulation.Design.Economy.FreightBatch*s.City.society.settings.secondsPerDay/times.Average()});
        }
        Require(before.SequenceEqual(CityCoreSave.Write(s)),"Budget query changed saved facts");Write(Path.Combine(output,"budget.json"),new{population=s.Entities.Members.Count,secondsPerDay=s.City.society.settings.secondsPerDay,gateSlots=s.Simulation.Design.Economy.MaxGateTrucks,payload=s.Simulation.Design.Economy.FreightBatch,factories=fs.Count,rows,caveat="Free-flow full-utilization estimate using deterministic stratified factory-ID sample; not a guarantee or actual capacity. All directed reachability counts are exact. Port order unchanged."});
    }
    static void Facts(string input,string output)
    {
        var s=Load(File.ReadAllBytes(input));active=s;var before=CityCoreSave.Write(s);var fs=s.Entities.Factories;
        Write(Path.Combine(output,"facts.json"),new{tick=s.Simulation.Tick,population=s.Entities.Members.Count,rawStock=fs.Sum(f=>(long)f.factory.raw),rawConsumed=fs.Sum(f=>(long)f.factory.consumed),rawImported=fs.Sum(f=>(long)f.factory.imported),produced=fs.Sum(f=>(long)f.factory.produced),processing=fs.Count(f=>f.factory.processing),attendanceMinutes=fs.Sum(f=>f.factory.attendanceMinutes),productiveMinutes=fs.Sum(f=>f.factory.productiveMinutes),factoryCash=fs.Sum(f=>f.factory.cash),factoryWages=fs.Sum(f=>f.factory.wageCosts),processingRates=fs.Select(f=>s.City.FactoryProcessingRate(f.id)).Distinct().ToArray(),requiredLabourPerItem=s.Simulation.Design.Production.LabourMinutesPerItem,shopStock=s.Entities.Shops.Sum(b=>(long)b.Stock),shopCash=s.Entities.Shops.Sum(b=>b.Account.Cash),shopFunded=s.Entities.Shops.Sum(b=>b.Account.WholesaleFunded),shopWages=s.Entities.Shops.Sum(b=>b.Account.WageCosts),consumed=s.City.consumption.ConsumedGoods,note="Cold read-only facts. Attendance is actual attendance, not proof all labour could be funded/productive or all output demanded."});
        Require(before.SequenceEqual(CityCoreSave.Write(s)),"Facts query changed saved state");
    }
    static void Run(string input,string output,int steps)
    {
        Require(steps>=20&&steps<=4800,"Bounded 20..4800-step run required");var initial=File.ReadAllBytes(input);var s=Load(initial);active=s;File.WriteAllBytes(Path.Combine(output,"initial.hcity"),initial);
        long initialTick=s.Simulation.Tick;double started=s.Simulation.Seconds;int delivered=s.City.trade.rawDelivered,dispatched=s.City.trade.rawDispatched;long produced=s.Entities.Factories.Sum(f=>(long)f.factory.produced);int consumed=s.City.consumption.ConsumedGoods;
        var rawBefore=s.Entities.Factories.ToDictionary(f=>f.id,f=>f.factory.imported);double wholesaleBefore=s.Entities.Shops.Sum(b=>b.wholesaleCosts);var ms=new List<double>();var allocations=new List<double>();var observerMs=new List<double>();var observerAlloc=new List<double>();var outsideMs=new List<double>();var outsideAlloc=new List<double>();int peak=0;string expected=null;long compareTick=-1;bool checkedReplay=false;
        using(var observation=new ImportObservation.Probe(s,output))
        using(var csv=new StreamWriter(Path.Combine(output,"steps.csv")))
        {
            ImportObservation.Current=observation;csv.WriteLine("tick,ms,allocated,active,routes,hook_ms,hook_bytes");
            for(int i=0;i<steps;i++)
            {
                double hookStart=observation.observerMs;long hookBytes=observation.observerBytes,at=Stopwatch.GetTimestamp(),bytes=GC.GetAllocatedBytesForCurrentThread(),routes=s.Transport.RoutePreparations;
                Require(s.Advance(World.StepSeconds)==1,"Fixed step lost");double elapsed=(Stopwatch.GetTimestamp()-at)*1000.0/Stopwatch.Frequency;long allocated=GC.GetAllocatedBytesForCurrentThread()-bytes;
                ms.Add(elapsed);allocations.Add(allocated);observerMs.Add(observation.observerMs-hookStart);observerAlloc.Add(observation.observerBytes-hookBytes);peak=Math.Max(peak,s.Transport.ActiveCount);
                csv.WriteLine(string.Join(",",s.Simulation.Tick,elapsed.ToString("R",System.Globalization.CultureInfo.InvariantCulture),allocated,s.Transport.ActiveCount,s.Transport.RoutePreparations-routes,observation.observerMs-hookStart,observation.observerBytes-hookBytes));long outsideAt=Stopwatch.GetTimestamp(),outsideBytes=GC.GetAllocatedBytesForCurrentThread();observation.AfterStep();outsideMs.Add((Stopwatch.GetTimestamp()-outsideAt)*1000.0/Stopwatch.Frequency);outsideAlloc.Add(GC.GetAllocatedBytesForCurrentThread()-outsideBytes);
                if(i==steps/2&&i+21<steps)
                {
                    byte[] checkpoint=CityCoreSave.Write(s);File.WriteAllBytes(Path.Combine(output,"inflight.hcity"),checkpoint);ImportObservation.Current=null;var replay=Load(checkpoint);for(int k=0;k<20;k++)Require(replay.Advance(World.StepSeconds)==1,"Replay skipped step");expected=Hash(CityCoreSave.Write(replay));compareTick=s.Simulation.Tick+20;ImportObservation.Current=observation;
                }
                if(s.Simulation.Tick==compareTick){Require(Hash(CityCoreSave.Write(s))==expected,"Warm/restore city continuation differs");checkedReplay=true;}
                if(i%200==0){csv.Flush();Console.WriteLine("Progress tick "+s.Simulation.Tick+" raw delivered="+(s.City.trade.rawDelivered-delivered)+" active="+s.Transport.ActiveCount);}
            }
            s.Validate();double seconds=s.Simulation.Seconds-started;Require(observation.ports.Values.Sum(p=>p.departedCargo)==s.City.trade.rawDispatched-dispatched,"Observer departure ledger mismatch");Require(observation.ports.Values.Sum(p=>p.arrivedCargo)==s.City.trade.rawDelivered-delivered,"Observer arrival ledger mismatch");foreach(var f in s.Entities.Factories)Require(observation.factories[f.id].arrivedCargo==f.factory.imported-rawBefore[f.id],"Per-factory arrival ledger mismatch");
            Require(Math.Abs(s.Entities.Shops.Sum(b=>b.wholesaleCosts)-wholesaleBefore-observation.goodsArrived*s.Simulation.Design.Economy.GoodsWholesalePrice)<1e-6,"Observed delivered goods disagree with actual wholesale settlement");
            var final=CityCoreSave.Write(s);File.WriteAllBytes(Path.Combine(output,"final.hcity"),final);Require(final.SequenceEqual(CityCoreSave.Write(Load(final))),"Final save round-trip differs");observation.WriteFactories(Path.Combine(output,"factories.csv"));
            Write(Path.Combine(output,"report.json"),new{status="passed-logistics-invariants; retail profitability not required",initialHash=Hash(initial),finalHash=Hash(final),initialTick,finalTick=s.Simulation.Tick,population=s.Entities.Members.Count,steps,seconds,stepMs=Dist(ms),allocatedBytes=Dist(allocations),synchronousHookMs=Dist(observerMs),synchronousHookBytes=Dist(observerAlloc),outsideObserverMs=Dist(outsideMs),outsideObserverBytes=Dist(outsideAlloc),peakActive=peak,rawDispatched=s.City.trade.rawDispatched-dispatched,rawDelivered=s.City.trade.rawDelivered-delivered,rawPerGameDay=(s.City.trade.rawDelivered-delivered)*s.City.society.settings.secondsPerDay/seconds,produced=s.Entities.Factories.Sum(f=>(long)f.factory.produced)-produced,consumed=s.City.consumption.ConsumedGoods-consumed,shopsStock=s.Entities.Shops.Sum(b=>(long)b.Stock),shopCash=s.Entities.Shops.Sum(b=>b.Account.Cash),replayChecked=checkedReplay,replayHash=expected,oldTasks=s.Transport.Tasks.Count(t=>s.Simulation.Seconds-t.departedAt>s.City.society.settings.secondsPerDay),oldestTask=s.Transport.Tasks.Count==0?0:s.Transport.Tasks.Max(t=>s.Simulation.Seconds-t.departedAt),observation=observation.Summary(seconds),note="Includes synchronous event-observation cost, reported separately; outside-step scans/JSON/replay excluded. No uninstrumented FPS or algorithm speedup claim. GC pause unknown."});
        }
        ImportObservation.Current=null;Console.WriteLine("PASS actual logistics ledgers and whole-city validation; in-flight replay="+checkedReplay);
    }
    static int Main(string[] args)
    {
        string output=args[2];Directory.CreateDirectory(output);
        try{Require(IntPtr.Size==8,"Explicit 64-bit host required");if(args[0]=="build")Build(args[1],output,int.Parse(args[3]),int.Parse(args[4]));else if(args[0]=="budget")Budget(args[1],output,int.Parse(args[3]));else if(args[0]=="run")Run(args[1],output,int.Parse(args[3]));else if(args[0]=="facts")Facts(args[1],output);else throw new Exception("Unknown experiment command");return 0;}
        catch(Exception e){File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());if(active!=null)try{File.WriteAllBytes(Path.Combine(output,"failure.hcity"),CityCoreSave.Write(active));}catch(Exception saveError){File.WriteAllText(Path.Combine(output,"failure-save-error.txt"),saveError.ToString());}Console.Error.WriteLine(e);return 1;}
    }
}
