using System;
using System.Collections.Generic;
using HarborCity.RoadCore;

namespace HarborCity
{
    public enum OutsideFlow { Import,Export }
    public sealed class CityOutsideConnection
    {
        public int id {get;internal set;}
        public float x {get;internal set;}
        public float y {get;internal set;}
        public float z {get;internal set;}
        public bool active {get;internal set;}=true;
        public OutsideFlow flow {get;internal set;}
    }
    [Serializable] public sealed class OutsideConnectionData {public int id;public float x,y,z;public bool active;public OutsideFlow flow;}
    [Serializable] public sealed class CityTradeData
    {
        public List<OutsideConnectionData> connections=new List<OutsideConnectionData>();
        public long nextDispatchTick;
        public int factoryCursor,rawDispatched,rawDelivered,goodsExported;
        public double rawReceipts,exportFunded,exportPayments,openingCash,cash;
    }
    // Domain facts are externally read-only. Public writable fields exist only
    // on cold persistence DTOs, never a second live ledger.
    public sealed class CityTradeState
    {
        readonly List<CityOutsideConnection> ports=new List<CityOutsideConnection>();
        public IReadOnlyList<CityOutsideConnection> connections {get;}
        public long nextDispatchTick {get;internal set;}
        public int factoryCursor {get;internal set;}
        public int rawDispatched {get;internal set;}
        public int rawDelivered {get;internal set;}
        public int goodsExported {get;internal set;}
        public double rawReceipts {get;internal set;}
        public double exportFunded {get;internal set;}
        public double exportPayments {get;internal set;}
        public double openingCash {get;}
        public double cash {get;internal set;}
        public CityTradeState():this(new EconomyDefinition().outsideOpeningCash){}
        public CityTradeState(double openingCash){this.openingCash=openingCash;cash=openingCash;connections=ports.AsReadOnly();}
        public CityTradeState(CityTradeData data):this(data?.openingCash??throw new ArgumentNullException(nameof(data)))
        {
            if(data.connections==null)throw new ArgumentException("Missing outside endpoints");
            foreach(var c in data.connections){if(c==null)throw new ArgumentException("Invalid outside endpoint");ports.Add(new CityOutsideConnection{id=c.id,x=c.x,y=c.y,z=c.z,active=c.active,flow=c.flow});}
            nextDispatchTick=data.nextDispatchTick;factoryCursor=data.factoryCursor;rawDispatched=data.rawDispatched;rawDelivered=data.rawDelivered;goodsExported=data.goodsExported;rawReceipts=data.rawReceipts;exportFunded=data.exportFunded;exportPayments=data.exportPayments;cash=data.cash;
        }
        internal void AddConnection(CityOutsideConnection connection)=>ports.Add(connection);
        public CityTradeData Snapshot()
        {
            var data=new CityTradeData{nextDispatchTick=nextDispatchTick,factoryCursor=factoryCursor,rawDispatched=rawDispatched,rawDelivered=rawDelivered,goodsExported=goodsExported,rawReceipts=rawReceipts,exportFunded=exportFunded,exportPayments=exportPayments,openingCash=openingCash,cash=cash};
            foreach(var c in ports)data.connections.Add(new OutsideConnectionData{id=c.id,x=c.x,y=c.y,z=c.z,active=c.active,flow=c.flow});return data;
        }
    }

    public sealed class CityCoreTrade
    {
        readonly EconomyRules rules;
        readonly CityModel city;
        readonly CityEntities entities;
        readonly World transport;
        readonly CitySimulation simulation;
        readonly Dictionary<int,int> incomingRaw=new Dictionary<int,int>();
        readonly Dictionary<int,int> gateTrucks=new Dictionary<int,int>();
        readonly List<SpatialHit> hits=new List<SpatialHit>();
        readonly CityImportSources importSources;

        internal CityCoreTrade(CityModel city,CityEntities entities,World transport,CitySimulation simulation)
        {this.city=city;this.entities=entities;this.transport=transport;this.simulation=simulation;rules=simulation.Design.Economy;importSources=new CityImportSources(transport);}

        public int ConnectBorder(Point point)
        {
            entities.StructuralBoundary();
            if(Math.Max(Math.Abs(point.x),Math.Abs(point.z))<city.map.buildHalfSize-2)
                throw new CityRuleException("trade.connection_requires_boundary");
            transport.Network.Project(point,4,false,hits);
            SpatialHit? chosen=null;
            foreach(var hit in hits)
                if(Math.Abs(hit.point.y-point.y)<.75&&Math.Max(Math.Abs(hit.point.x),Math.Abs(hit.point.z))>=city.map.buildHalfSize-2){chosen=hit;break;}
            if(!chosen.HasValue)throw new CityRuleException("trade.connection_requires_road");
            var position=chosen.Value.point;
            foreach(var existing in city.trade.connections)
                if(Point.Distance(new Point(existing.x,existing.y,existing.z),position)<4)
                    throw new CityRuleException("trade.connection_exists");
            var lane=transport.Network.Get(chosen.Value.drivable);bool xBorder=Math.Abs(position.x)>Math.Abs(position.z);double inward=xBorder?-Math.Sign(position.x):-Math.Sign(position.z);
            var registrations=new List<AccessRegistration>();var connections=new List<CityOutsideConnection>();
            foreach(bool reverse in new[]{lane.Reverse,!lane.Reverse})
            {
                var access=AccessRouting.AtDirection(transport.Network,new RoadAccess{lane=lane.Id,distance=chosen.Value.distance},reverse);if(access==null)continue;
                var directed=transport.Network.Get(access.lane);var tangent=directed.Geometry.DirectionAt(access.distance);double dot=(xBorder?tangent.x:tangent.z)*inward;if(Math.Abs(dot)<.2)throw new CityRuleException("trade.connection_requires_road");
                var p=directed.Geometry.Sample(access.distance);int id=city.nextBuildingId+connections.Count;
                transport.CheckNewAccess(id,access.lane,access.distance,AccessUse.Freight);
                connections.Add(new CityOutsideConnection{id=id,x=(float)p.x,y=(float)p.y,z=(float)p.z,flow=dot>0?OutsideFlow.Import:OutsideFlow.Export});registrations.Add(new AccessRegistration(id,access.lane,access.distance,AccessUse.Freight));
            }
            entities.CheckNewConnections(connections);transport.RegisterAccesses(registrations);foreach(var connection in connections)entities.AddConnection(connection);
            return connections[0].id;
        }

        internal void Step(TimeContext time)
        {
            if(!city.development.enabled||city.trade.connections.Count==0||time.Tick<city.trade.nextDispatchTick)return;
            city.trade.nextDispatchTick=time.Tick+rules.TradeIntervalTicks;
            incomingRaw.Clear();gateTrucks.Clear();
            foreach(var task in transport.Tasks)
            {
                if(task.Shipment?.kind==CargoKind.RawMaterial){incomingRaw.TryGetValue(task.destination,out int n);incomingRaw[task.destination]=n+task.Cargo;}
                if(entities.Connection(task.origin)!=null){gateTrucks.TryGetValue(task.origin,out int n);gateTrucks[task.origin]=n+1;}
                else if(entities.Connection(task.destination)!=null){gateTrucks.TryGetValue(task.destination,out int n);gateTrucks[task.destination]=n+1;}
            }
            int count=entities.Factories.Count;
            if(count==0){city.trade.factoryCursor=0;return;}
            int start=city.trade.factoryCursor%count;
            importSources.Begin(city.trade.connections,gateTrucks,rules.MaxGateTrucks);
            for(int offset=0;offset<count;offset++)
            {
                var factory=entities.Factories[(start+offset)%count];
                if(!city.BuildingAccess(factory.id)||!city.Supply(factory.id).Complete)continue;
                incomingRaw.TryGetValue(factory.id,out int reserved);
                int amount=Math.Min(rules.FreightBatch,rules.RawReorderTarget-factory.factory.raw-reserved);
                amount=Math.Min(amount,(int)Math.Min(rules.FreightBatch,Math.Floor(factory.factory.cash/rules.RawUnitPrice)));
                if(amount>0)
                {
                    importSources.ForFactory();int gate;
                    while((gate=importSources.Choose(factory.id))!=0)
                    {
                        var vehicle=simulation.RequestMaterial(gate,factory.id,amount,CargoKind.RawMaterial,rules.RawUnitPrice);
                        if(vehicle==null){importSources.Reject(gate);continue;}
                        gateTrucks.TryGetValue(gate,out int trucks);incomingRaw[factory.id]=reserved+amount;gateTrucks[gate]=trucks+1;
                        importSources.Accepted(gate,gateTrucks,rules.MaxGateTrucks);break;
                    }
                }
                // Local shops get the preceding Logistics phase's first chance.
                // Keep a buffer; export only actual surplus, funded by the market.
                if(factory.Stock<rules.ExportThreshold||transport.FreightSourceBlocked(factory.id))continue;
                foreach(var gate in city.trade.connections)
                {
                    gateTrucks.TryGetValue(gate.id,out int trucks);
                    if(!gate.active||gate.flow!=OutsideFlow.Export||trucks>=rules.MaxGateTrucks||city.trade.cash<rules.FreightBatch*rules.GoodsWholesalePrice)continue;
                    var vehicle=simulation.RequestMaterial(factory.id,gate.id,rules.FreightBatch,CargoKind.Goods,rules.GoodsWholesalePrice);
                    if(vehicle==null)continue;gateTrucks[gate.id]=trucks+1;break;
                }
            }
            city.trade.factoryCursor=(start+1)%count;
        }

        internal bool Depart(TripTask task)
        {
            if(task.Shipment?.kind==CargoKind.Goods)
            {
                var destination=entities.Connection(task.destination);var source=entities.Building(task.origin) as IndustrialBuilding;
                double funded=task.Cargo*rules.GoodsWholesalePrice;
                if(destination==null||!destination.active||destination.flow!=OutsideFlow.Export||source==null||source.abandoned||source.burned||task.Cargo<=0||source.Stock<task.Cargo||task.Price!=rules.GoodsWholesalePrice||city.trade.cash<funded)return false;
                source.Stock-=task.Cargo;source.factory.shipped+=task.Cargo;city.trade.cash-=funded;city.trade.exportFunded+=funded;return true;
            }
            var gate=entities.Connection(task.origin);
            var factory=entities.Building(task.destination) as IndustrialBuilding;
            double payment=task.Cargo*rules.RawUnitPrice;
            if(task.purpose!=TransportPurpose.Freight||task.Shipment?.kind!=CargoKind.RawMaterial||gate==null||!gate.active||gate.flow!=OutsideFlow.Import||factory==null||factory.abandoned||factory.burned||task.Cargo<=0||task.Price!=rules.RawUnitPrice||factory.factory.cash<payment)return false;
            // Admission already holds a valid route/physical entry; pay once at
            // departure. Interrupted goods remain paid and owned by Shipment.
            factory.factory.cash-=payment;factory.factory.rawCosts+=payment;
            city.trade.cash+=payment;city.trade.rawReceipts+=payment;city.trade.rawDispatched+=task.Cargo;
            return true;
        }

        internal bool Deliver(TransportArrival arrival)
        {
            if(arrival.kind==CargoKind.Goods)
            {
                var destination=entities.Connection(arrival.destination);var source=entities.Building(arrival.origin) as IndustrialBuilding;
                if(destination==null||!destination.active||destination.flow!=OutsideFlow.Export||source==null||source.abandoned||source.burned)return false;
                city.RecordFactorySale(arrival.origin,arrival.cargo,arrival.taskId,arrival.destination,notify:false,unitPrice:arrival.price);city.trade.goodsExported+=arrival.cargo;city.trade.exportPayments+=arrival.cargo*arrival.price;return true;
            }
            var gate=entities.Connection(arrival.origin);
            var factory=entities.Building(arrival.destination) as IndustrialBuilding;
            if(arrival.kind!=CargoKind.RawMaterial||gate==null||factory==null||factory.abandoned||factory.burned||factory.factory.raw+arrival.cargo>FactoryState.RawCapacity)return false;
            factory.factory.raw+=arrival.cargo;factory.factory.imported+=arrival.cargo;city.trade.rawDelivered+=arrival.cargo;
            return true;
        }

        public void Validate()
        {
            var state=city.trade;
            if(state==null||state.connections==null||state.rawDispatched<state.rawDelivered||state.rawDelivered<0||state.goodsExported<0||state.nextDispatchTick<0||state.factoryCursor<0||!Finite(state.openingCash)||!Finite(state.cash)||!Finite(state.rawReceipts)||!Finite(state.exportFunded)||!Finite(state.exportPayments)||state.exportPayments>state.exportFunded||Math.Abs(state.cash-(state.openingCash+state.rawReceipts-state.exportFunded))>1e-6)
                throw new InvalidOperationException("Invalid outside market ledger");
            int cargo=0;double escrow=0;foreach(var task in transport.Tasks){if(task.Shipment?.kind==CargoKind.RawMaterial)cargo+=task.Cargo;else if(entities.Connection(task.destination)!=null)escrow+=task.Cargo*task.Price;}
            if(state.rawDispatched!=state.rawDelivered+cargo)throw new InvalidOperationException("Imported material lost or duplicated");
            if(Math.Abs(state.exportFunded-state.exportPayments-escrow)>1e-6)throw new InvalidOperationException("Export escrow lost or double-settled");
        }
        static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value)&&value>=0;
    }
}
