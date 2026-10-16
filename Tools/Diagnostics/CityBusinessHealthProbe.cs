using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HarborCity;
using HarborCity.RoadCore;
// Cold O(B+P+V+T+C) inspection of saved facts. No simulation step or domain command.
static class CityBusinessHealthProbe
{
    static void Main(string[] args)
    {
        if(IntPtr.Size!=8)throw new Exception("64-bit inspection required");
        byte[] input=File.ReadAllBytes(args[0]);var s=CityCoreSave.Read(input);s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);s.Validate();
        var workers=new Dictionary<int,int>();
        foreach(var member in s.Entities.Members)
        {
            var p=member.Person;if(!p.atWork||p.jobId<0)continue;var job=s.Entities.Job(p.jobId);if(job==null)throw new Exception("Invalid worker job");workers.TryGetValue(job.buildingId,out int n);workers[job.buildingId]=n+1;
        }
        var factories=s.Entities.Factories;var shops=s.Entities.Shops;var tasks=s.Transport.Tasks;
        var goods=tasks.Where(t=>t.Shipment?.kind==CargoKind.Goods).ToArray();var raw=tasks.Where(t=>t.Shipment?.kind==CargoKind.RawMaterial).ToArray();
        int employed=s.Entities.Members.Count(m=>m.Person.canWork&&m.Person.jobId>=0);
        var vehicles=s.Transport.Vehicles.ToDictionary(v=>v.Task.id);var gateCounts=s.City.trade.connections.ToDictionary(p=>p.id,p=>0);foreach(var task in tasks){if(gateCounts.ContainsKey(task.origin))gateCounts[task.origin]++;if(gateCounts.ContainsKey(task.destination))gateCounts[task.destination]++;}
        var ports=s.City.trade.connections.Select(p=>new{id=p.id,flow=p.flow.ToString(),p.active,p.x,p.z,tasks=gateCounts[p.id]}).ToArray();
        var shipments=tasks.Where(t=>t.Shipment!=null).GroupBy(t=>t.Shipment.kind).Select(g=>new{kind=g.Key.ToString(),tasks=g.Count(),cargo=g.Sum(t=>(long)t.Cargo),oldestSeconds=g.Max(t=>s.Simulation.Seconds-t.departedAt)}).ToArray();
        var rawDetails=raw.Concat(goods).Select(t=>{var vehicle=vehicles[t.id];var position=s.Transport.Position(vehicle);return new{task=t.id,kind=t.Shipment.kind.ToString(),t.origin,t.destination,t.Cargo,ageSeconds=s.Simulation.Seconds-t.departedAt,status=vehicle.status.ToString(),lane=vehicle.Current,vehicle.distance,position};}).ToArray();
        long produced=factories.Sum(f=>(long)f.factory.produced),factoryStock=factories.Sum(f=>(long)f.Stock),shopStock=shops.Sum(b=>(long)b.Stock),inTransit=goods.Sum(t=>(long)t.Cargo),carried=s.Entities.Members.Sum(m=>(long)m.Person.carriedGoods),consumed=s.City.consumption.ConsumedGoods,exported=s.City.trade.goodsExported;
        long retiredProduced=s.Entities.RetiredBuildings.OfType<IndustrialBuilding>().Sum(f=>(long)f.factory.produced),retiredStock=s.Entities.RetiredBuildings.Sum(b=>b is IndustrialBuilding f?(long)f.Stock:b is CommercialBuilding shop?(long)shop.Stock:0);
        long goodsError=produced+retiredProduced-factoryStock-shopStock-retiredStock-inTransit-carried-consumed-exported;
        if(goodsError!=0)throw new Exception("Independent goods conservation check failed");
        if(!input.SequenceEqual(CityCoreSave.Write(s)))throw new Exception("Inspection changed persisted facts");
        var report=new{mode="cold saved-fact inspection; not FPS or throughput measurement",tick=s.Simulation.Tick,seconds=s.Simulation.Seconds,daySeconds=s.City.society.settings.secondsPerDay,population=s.Entities.Members.Count,employed,actualAtWork=workers.Values.Sum(),ports,maxGateTrucks=s.Simulation.Design.Economy.MaxGateTrucks,freightBatch=s.Simulation.Design.Economy.FreightBatch,shipments,rawDetails,
            factories=new{count=factories.Count,withRaw=factories.Count(f=>f.factory.raw>0),withProduction=factories.Count(f=>f.factory.produced>0),withWorkers=factories.Count(f=>workers.ContainsKey(f.id)),withWorkersButNoRaw=factories.Count(f=>workers.ContainsKey(f.id)&&f.factory.raw==0),cash=factories.Sum(f=>f.factory.cash),wages=factories.Sum(f=>f.factory.wageCosts)},
            goods=new{produced,factoryStock,shopStock,retiredProduced,retiredStock,inTransit,carried,consumed,exported,goodsError},trade=new{s.City.trade.rawDispatched,s.City.trade.rawDelivered},unchangedSave=true};
        File.WriteAllText(args[1],new CityScenarioJson().Serialize(report));Console.WriteLine("PASS read-only inspection and independent goods balance; consumption="+consumed+" in-transit goods="+inTransit);
    }
}
