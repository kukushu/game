using System;
using System.Linq;
using HarborCity;
using HarborCity.RoadCore;
static class CityTwoGoodsChecks
{
    static int checks;
    static void Check(bool b,string message){checks++;if(!b)throw new Exception(message);}
    static void Reject(Action a){bool failed=false;try{a();}catch(ArgumentException){failed=true;}catch(InvalidOperationException){failed=true;}Check(failed,"Invalid product accepted");}
    static void Main()
    {
        var catalog=new CityGameDesign(new GameDesignData()).Products;
        Check(catalog[ProductKind.Food].RetailPrice==20&&catalog[ProductKind.DailyGoods].LabourMinutes==60,"Product configuration");
        var a=GoodsAmounts.Of(ProductKind.Food,2).Add(ProductKind.DailyGoods,3);var b=a.Add(ProductKind.Food,-1);
        Check(a.food==2&&b.food==1&&b.Total==4,"Value inventory alias");Reject(()=>a.Add(ProductKind.Food,-3));Reject(()=>new ProductCatalog(new[]{ProductCatalog.Defaults()[0],ProductCatalog.Defaults()[0]}));
        var session=CityCoreIndustrySession.Create();
        var admission=session.Transport.PrepareTransport(1,2,5,45,TransportPurpose.Freight,0);Check(admission!=null,"Fixture route");
        admission.vehicle.Task.Shipment.Products=a;var vehicle=session.Transport.CommitAdmission(admission);
        byte[] bytes=SaveCodec.Write(session.Transport);Check(BitConverter.ToInt32(bytes,4)==16,"Traffic schema");
        var cold=SaveCodec.Read(bytes,new SimulationClock(session.Simulation.ClockSnapshot),endpoints:session.City.buildings.Select(x=>x.id));
        Check(cold.Tasks.Single().Shipment.Products.Equals(a),"Cold mixed cargo");Check(SaveCodec.Write(cold).SequenceEqual(bytes),"Exact cargo persistence");
        var copy=cold.Snapshot();copy.tasks.Single().Shipment.Products=default;Check(cold.Tasks.Single().Cargo==5,"Snapshot cargo alias");
        var raw=new Shipment{remaining=8};raw.kind=CargoKind.RawMaterial;Check(raw.remaining==8&&raw.Products.Total==0,"Raw admission amount");var waste=new Shipment{kind=CargoKind.Waste,remaining=7};Check(waste.Copy().remaining==7,"Waste copy");
        var building=BuildingSaveData.FromBuilding(new CommercialBuilding{Products=a});Check(((CommercialBuilding)building.ToBuilding()).Products.Equals(a),"Building mixed inventory persistence");
        var old=(byte[])bytes.Clone();Buffer.BlockCopy(BitConverter.GetBytes(15),0,old,4,4);bool refused=false;try{SaveCodec.Read(old);}catch(System.IO.InvalidDataException){refused=true;}Check(refused,"Old traffic format accepted");
        Console.WriteLine("PASS "+checks+" two-product foundation checks");
    }
}
