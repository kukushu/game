using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HarborCity;
using HarborCity.RoadCore;
// Cold indexed aggregation plus explicit endpoint-route searches per freight task.
// No simulation step or domain command. Route work is not an O(1) query claim.
static class CityBusinessHealthProbe
{
    static void Main(string[] args)
    {
        if(IntPtr.Size!=8)throw new Exception("64-bit inspection required");
        byte[] input=File.ReadAllBytes(args[0]);var s=CityCoreSave.Read(input);s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);s.Validate();
        // Compare the current reader's full canonical state before/after queries;
        // the inspection does not add support for any legacy wire format.
        byte[] canonical=CityCoreSave.Write(s);
        var workers=new Dictionary<int,int>();
        foreach(var member in s.Entities.Members)
        {
            var p=member.Person;if(!p.atWork||p.jobId<0)continue;var job=s.Entities.Job(p.jobId);if(job==null)throw new Exception("Invalid worker job");workers.TryGetValue(job.buildingId,out int n);workers[job.buildingId]=n+1;
        }
        var factories=s.Entities.Factories;var shops=s.Entities.Shops;var tasks=s.Transport.Tasks;
        object movementWindow=null;
        if(args.Length>2)
        {
            byte[] earlierBytes=File.ReadAllBytes(args[2]);var earlier=CityCoreSave.Read(earlierBytes);earlier.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);earlier.Validate();
            if(earlier.Simulation.Tick>s.Simulation.Tick)throw new ArgumentException("Previous save is later than current save");
            if(earlier.Transport.NetworkGeneration!=s.Transport.NetworkGeneration)throw new ArgumentException("Progress inspection requires unchanged road generation");
            var previous=earlier.Transport.Vehicles.Where(v=>v.status==VehicleStatus.Active).ToDictionary(v=>v.id);
            var retained=new List<Vehicle>();var unchanged=new HashSet<int>();
            foreach(var vehicle in s.Transport.Vehicles.Where(v=>v.status==VehicleStatus.Active))
            {
                if(!previous.TryGetValue(vehicle.id,out var old)||old.Task.id!=vehicle.Task.id||old.Task.purpose!=vehicle.Task.purpose||old.Task.origin!=vehicle.Task.origin||old.Task.destination!=vehicle.Task.destination)continue;
                retained.Add(vehicle);
                if(old.cursor==vehicle.cursor&&old.Current==vehicle.Current&&BitConverter.DoubleToInt64Bits(old.distance)==BitConverter.DoubleToInt64Bits(vehicle.distance)&&BitConverter.DoubleToInt64Bits(old.Plan.EndDistance)==BitConverter.DoubleToInt64Bits(vehicle.Plan.EndDistance)&&old.Route.SequenceEqual(vehicle.Route))unchanged.Add(vehicle.id);
            }
            movementWindow=new{previousTick=earlier.Simulation.Tick,currentTick=s.Simulation.Tick,seconds=s.Simulation.Seconds-earlier.Simulation.Seconds,
                previousActive=previous.Count,retainedTasks=retained.Count,sameRouteCursorAndPosition=unchanged.Count,
                byPurpose=retained.GroupBy(v=>v.Task.purpose).OrderBy(g=>g.Key).Select(g=>new{purpose=g.Key.ToString(),retained=g.Count(),sameRouteCursorAndPosition=g.Count(v=>unchanged.Contains(v.id))}).ToArray(),
                note="Endpoint snapshots show no net route progress; they do not prove uninterrupted immobility or diagnose a junction deadlock."};
            if(!earlierBytes.SequenceEqual(File.ReadAllBytes(args[2])))throw new Exception("Inspection changed previous save file");
        }
        var goods=tasks.Where(t=>t.Shipment?.kind==CargoKind.Goods).ToArray();var raw=tasks.Where(t=>t.Shipment?.kind==CargoKind.RawMaterial).ToArray();
        int employed=s.Entities.Members.Count(m=>m.Person.canWork&&m.Person.jobId>=0);
        var vehicles=s.Transport.Vehicles.ToDictionary(v=>v.Task.id);var gateCounts=s.City.trade.connections.ToDictionary(p=>p.id,p=>0);foreach(var task in tasks){if(gateCounts.ContainsKey(task.origin))gateCounts[task.origin]++;if(gateCounts.ContainsKey(task.destination))gateCounts[task.destination]++;}
        var ports=s.City.trade.connections.Select(p=>new{id=p.id,flow=p.flow.ToString(),p.active,p.x,p.z,tasks=gateCounts[p.id]}).ToArray();
        var shipments=tasks.Where(t=>t.Shipment!=null).GroupBy(t=>t.Shipment.kind).Select(g=>new{kind=g.Key.ToString(),tasks=g.Count(),cargo=g.Sum(t=>(long)t.Cargo),oldestSeconds=g.Max(t=>s.Simulation.Seconds-t.departedAt)}).ToArray();
        var routingNetwork=new Network(s.Transport.Snapshot().network); // Separate derived query buffers: do not warm the live city's route caches.
        var rawDetails=raw.Concat(goods).Select(t=>{
            var vehicle=vehicles[t.id];var position=s.Transport.Position(vehicle);var from=s.Transport.GetAccess(t.origin);var to=s.Transport.GetAccess(t.destination);
            double start=0,end=0;var route=from!=null&&to!=null&&from.valid&&to.valid?AccessRouting.Find(routingNetwork,from,to,false,out start,out end):null;double metres=0,freeFlowSeconds=0;
            if(route!=null)for(int i=0;i<route.Count;i++){var d=routingNetwork.Get(route[i]);double length=(i==route.Count-1?end:d.EndDistance)-(i==0?start:d.StartDistance);metres+=length;freeFlowSeconds+=length/d.Speed;}
            return new{task=t.id,kind=t.Shipment.kind.ToString(),t.origin,t.destination,t.Cargo,ageSeconds=s.Simulation.Seconds-t.departedAt,status=vehicle.status.ToString(),lane=vehicle.Current,vehicle.distance,position,endpointReachable=route!=null,endpointRouteWorldUnits=route==null?(double?)null:metres,endpointFreeFlowSeconds=route==null?(double?)null:freeFlowSeconds,freeFlowDays=route==null?(double?)null:freeFlowSeconds/s.City.society.settings.secondsPerDay};}).ToArray();
        long produced=factories.Sum(f=>(long)f.factory.produced),factoryStock=factories.Sum(f=>(long)f.Stock),shopStock=shops.Sum(b=>(long)b.Stock),inTransit=goods.Sum(t=>(long)t.Cargo),carried=s.Entities.Members.Sum(m=>(long)m.Person.carriedGoods),consumed=s.City.consumption.ConsumedGoods,exported=s.City.trade.goodsExported;
        long retiredProduced=s.Entities.RetiredBuildings.OfType<IndustrialBuilding>().Sum(f=>(long)f.factory.produced),retiredStock=s.Entities.RetiredBuildings.Sum(b=>b is IndustrialBuilding f?(long)f.Stock:b is CommercialBuilding shop?(long)shop.Stock:0);
        long goodsError=produced+retiredProduced-factoryStock-shopStock-retiredStock-inTransit-carried-consumed-exported;
        if(goodsError!=0)throw new Exception("Independent goods conservation check failed");
        if(!canonical.SequenceEqual(CityCoreSave.Write(s)))throw new Exception("Inspection changed persisted facts");
        if(!input.SequenceEqual(File.ReadAllBytes(args[0])))throw new Exception("Inspection changed source save file");
        var freightShops=new HashSet<int>(goods.Select(t=>t.destination));
        var stockedShops=shops.Where(b=>b.Stock>0||freightShops.Contains(b.id)).Select(b=>new{b.id,b.Stock,b.x,b.z,b.abandoned,b.burned,cash=b.Account.Cash,wholesaleFunded=b.Account.WholesaleFunded,wages=b.Account.WageCosts,workersAtWork=workers.TryGetValue(b.id,out int n)?n:0,retailSold=b.retailSold,access=s.City.BuildingAccess(b.id),supply=s.City.Supply(b.id).Complete}).ToArray();
        var report=new{mode="cold saved-fact inspection plus explicit endpoint routing; not FPS or throughput measurement",tick=s.Simulation.Tick,seconds=s.Simulation.Seconds,daySeconds=s.City.society.settings.secondsPerDay,population=s.Entities.Members.Count,employed,actualAtWork=workers.Values.Sum(),ports,maxGateTrucks=s.Simulation.Design.Economy.MaxGateTrucks,freightBatch=s.Simulation.Design.Economy.FreightBatch,shipments,rawDetails,stockedShops,movementWindow,
            factories=new{count=factories.Count,withRaw=factories.Count(f=>f.factory.raw>0),withProduction=factories.Count(f=>f.factory.produced>0),withWorkers=factories.Count(f=>workers.ContainsKey(f.id)),withWorkersButNoRaw=factories.Count(f=>workers.ContainsKey(f.id)&&f.factory.raw==0),cash=factories.Sum(f=>f.factory.cash),wages=factories.Sum(f=>f.factory.wageCosts)},
            goods=new{produced,factoryStock,shopStock,retiredProduced,retiredStock,inTransit,carried,consumed,exported,goodsError},trade=new{s.City.trade.rawDispatched,s.City.trade.rawDelivered},unchangedSave=true};
        File.WriteAllText(args[1],new CityScenarioJson().Serialize(report));Console.WriteLine("PASS read-only inspection and independent goods balance; consumption="+consumed+" in-transit goods="+inTransit);
    }
}
