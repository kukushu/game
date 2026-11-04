using System;
using System.Linq;
using HarborCity;
using HarborCity.RoadCore;
static class CityTwoGoodsChecks
{
    static int checks;
    static void Check(bool b,string message){checks++;if(!b)throw new Exception(message);}
    static void Reject(Action a){bool failed=false;try{a();}catch(ArgumentException){failed=true;}catch(InvalidOperationException){failed=true;}Check(failed,"Invalid product accepted");}
    static bool GoodsDiff(object a,object b,string path)
    {
        if(a==null||b==null){if(a!=b){Console.WriteLine(path+" null mismatch");return true;}return false;}
        var t=a.GetType();if(t.IsPrimitive||t.IsEnum||a is string||a is decimal){if(!a.Equals(b)){Console.WriteLine(path+" "+a+" != "+b);return true;}return false;}
        if(a is System.Collections.IList x&&b is System.Collections.IList y){if(x.Count!=y.Count){Console.WriteLine(path+" count "+x.Count+" != "+y.Count);return true;}for(int i=0;i<x.Count;i++)if(GoodsDiff(x[i],y[i],path+"["+i+"]"))return true;return false;}
        foreach(var f in t.GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance))if(!f.IsNotSerialized&&GoodsDiff(f.GetValue(a),f.GetValue(b),path+"."+f.Name))return true;
        foreach(var p in t.GetProperties())if(p.IsDefined(typeof(CitySavedFactAttribute),false)&&GoodsDiff(p.GetValue(a),p.GetValue(b),path+"."+p.Name))return true;
        return false;
    }
    static void ScheduleChecks()
    {
        var live=CityGrowingGridFixture.Create();var cold=CityCoreSave.Read(CityCoreSave.Write(live));cold.Simulation.Growth.Zoning.SetSurface((x,z)=>2);cold.Simulation.Consumption.ReferencePolling=true;
        for(int t=0;t<12000;t++)
        {
            foreach(var session in new[]{live,cold})if(!session.Entities.Factories.Any(f=>f.factory.product==ProductKind.Food)){var first=session.Entities.Factories.FirstOrDefault(f=>f.factory.produced==0&&!f.factory.processing&&f.Stock==0);if(first!=null)first.factory.product=ProductKind.Food;}
            live.Advance(.05);cold.Advance(.05);
            string da=string.Join(";",live.Simulation.Consumption.ShopDemand.OrderBy(x=>x.Key).Select(x=>x.Key+":"+x.Value.food+":"+x.Value.dailyGoods)),db=string.Join(";",cold.Simulation.Consumption.ShopDemand.OrderBy(x=>x.Key).Select(x=>x.Key+":"+x.Value.food+":"+x.Value.dailyGoods));
            if(da!=db){Console.WriteLine("DEMAND "+live.Simulation.Tick+" "+da+" vs "+db);foreach(var h in live.City.society.families)Console.WriteLine("H "+h.id+" supplier "+h.goodsSupplier+" store "+h.goods.food+"/"+h.goods.dailyGoods+" incoming actor "+h.shoppingMember+" needs "+live.Simulation.Consumption.Need(h,ProductKind.Food)+"/"+live.Simulation.Consumption.Need(h,ProductKind.DailyGoods));throw new Exception("Derived demand mismatch");}
            if(t%100==99){if(!CityCoreSave.Write(live).SequenceEqual(CityCoreSave.Write(cold))){GoodsDiff(live.SnapshotBusiness(),cold.SnapshotBusiness(),"city");GoodsDiff(live.Transport.Snapshot(),cold.Transport.Snapshot(),"world");Console.WriteLine("event visits="+live.Simulation.Consumption.FamilyVisits+" oracle="+cold.Simulation.Consumption.FamilyVisits);}Check(CityCoreSave.Write(live).SequenceEqual(CityCoreSave.Write(cold)),"Reference scheduling differs at "+live.Simulation.Tick);}
        }
        Check(live.Simulation.Consumption.FamilyVisits<cold.Simulation.Consumption.FamilyVisits,"No candidate reduction");
        Check(live.City.consumption.Consumed.food>0&&live.City.consumption.Consumed.dailyGoods>0,"Both products must actually be consumed");
        var after=CityCoreSave.Read(CityCoreSave.Write(live));after.Simulation.Growth.Zoning.SetSurface((x,z)=>2);for(int i=0;i<600;i++){live.Advance(.05);after.Advance(.05);}Check(CityCoreSave.Write(live).SequenceEqual(CityCoreSave.Write(after)),"Sparse cold restore diverged");
        var negative=CityCoreIndustrySession.Create(true);negative.City.society.families[0].openingSavings=negative.City.society.families[0].savings;negative.City.development.enabled=true;negative.Advance(.15);
        var baseline=CityCoreSave.Read(CityCoreSave.Write(negative));baseline.Simulation.Consumption.ReferencePolling=true;
        var field=typeof(Household).GetField("pantry",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        field.SetValue(negative.City.society.families[0],default(GoodsAmounts));field.SetValue(baseline.City.society.families[0],default(GoodsAmounts));
        for(int i=0;i<20;i++){negative.Advance(.05);baseline.Advance(.05);}
        Check(negative.Simulation.Consumption.UnservedDemand.Values.Sum(n=>n.Total)!=baseline.Simulation.Consumption.UnservedDemand.Values.Sum(n=>n.Total),"Negative control failed to detect bypassed pantry notification");
    }
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
        ScheduleChecks();Console.WriteLine("PASS "+checks+" two-product checks");
    }
}
