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
        for(int t=0;t<40000;t++)
        {
            if(t==12000||t==15000){int budget=t==12000?50:100;live.Simulation.Utilities.SetBudget(CityServiceKind.Water,budget);cold.Simulation.Utilities.SetBudget(CityServiceKind.Water,budget);}
            live.Advance(.05);cold.Advance(.05);
            string da=string.Join(";",live.Simulation.Consumption.ShopDemand.OrderBy(x=>x.Key).Select(x=>x.Key+":"+x.Value.food+":"+x.Value.dailyGoods)),db=string.Join(";",cold.Simulation.Consumption.ShopDemand.OrderBy(x=>x.Key).Select(x=>x.Key+":"+x.Value.food+":"+x.Value.dailyGoods));
            if(da!=db){Console.WriteLine("DEMAND "+live.Simulation.Tick+" "+da+" vs "+db);foreach(var h in live.City.society.families)Console.WriteLine("H "+h.id+" supplier "+h.goodsSupplier+" store "+h.goods.food+"/"+h.goods.dailyGoods+" incoming actor "+h.shoppingMember+" needs "+live.Simulation.Consumption.Need(h,ProductKind.Food)+"/"+live.Simulation.Consumption.Need(h,ProductKind.DailyGoods));throw new Exception("Derived demand mismatch");}
            if(t%100==99){if(!CityCoreSave.Write(live).SequenceEqual(CityCoreSave.Write(cold))){GoodsDiff(live.SnapshotBusiness(),cold.SnapshotBusiness(),"city");GoodsDiff(live.Transport.Snapshot(),cold.Transport.Snapshot(),"world");Console.WriteLine("event visits="+live.Simulation.Consumption.FamilyVisits+" oracle="+cold.Simulation.Consumption.FamilyVisits);}Check(CityCoreSave.Write(live).SequenceEqual(CityCoreSave.Write(cold)),"Reference scheduling differs at "+live.Simulation.Tick);}
        }
        Check(live.City.development.externalInvestment>0&&live.Entities.Factories.Any(f=>f.externalCapital>0&&f.factory.firstDelivery),"Autonomous investment did not deliver goods");
        Check(live.Simulation.Consumption.FamilyVisits<cold.Simulation.Consumption.FamilyVisits,"No candidate reduction");
        Console.WriteLine("ECONOMY bought="+live.City.consumption.Bought.food+"/"+live.City.consumption.Bought.dailyGoods+" consumed="+live.City.consumption.Consumed.food+"/"+live.City.consumption.Consumed.dailyGoods+" external="+live.City.development.externalInvestment+" visits="+live.Simulation.Consumption.FamilyVisits+"/"+cold.Simulation.Consumption.FamilyVisits);
        foreach(var f in live.Entities.Factories)Console.WriteLine("F "+f.id+" "+f.factory.product+" cash="+f.factory.cash+" raw="+f.factory.raw+" stock="+f.Stock+" delivered="+f.factory.firstDelivery+" state="+f.factory.status+" jobs="+live.City.EmployedAt(f.id));foreach(var pair in live.Simulation.Consumption.ShopDemand)Console.WriteLine("D "+pair.Key+" "+pair.Value.food+"/"+pair.Value.dailyGoods);
        Check(live.City.consumption.Consumed.food>0&&live.City.consumption.Consumed.dailyGoods>0,"Both products must actually be consumed");
        var after=CityCoreSave.Read(CityCoreSave.Write(live));after.Simulation.Growth.Zoning.SetSurface((x,z)=>2);for(int i=0;i<600;i++){live.Advance(.05);after.Advance(.05);}Check(CityCoreSave.Write(live).SequenceEqual(CityCoreSave.Write(after)),"Sparse cold restore diverged");
        var negative=CityCoreIndustrySession.Create(true);negative.City.society.families[0].openingSavings=negative.City.society.families[0].savings;negative.City.development.enabled=true;negative.Advance(.15);
        var baseline=CityCoreSave.Read(CityCoreSave.Write(negative));baseline.Simulation.Consumption.ReferencePolling=true;
        var field=typeof(Household).GetField("pantry",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        field.SetValue(negative.City.society.families[0],default(GoodsAmounts));field.SetValue(baseline.City.society.families[0],default(GoodsAmounts));
        for(int i=0;i<20;i++){negative.Advance(.05);baseline.Advance(.05);}
        Check(negative.Simulation.Consumption.UnservedDemand.Values.Sum(n=>n.Total)!=baseline.Simulation.Consumption.UnservedDemand.Values.Sum(n=>n.Total),"Negative control failed to detect bypassed pantry notification");
    }
    static void MixedPurchaseChecks()
    {
        // Declared zero-opening-inventory stress fixture, no free goods or cash
        // injected during play. Normal migration's three/fourteen-day kit is
        // separately exercised by ScheduleChecks.
        var session=CityGrowingGridFixture.Create();
        session.Entities.MemberStructureChanged+=(m,added)=>{if(added&&!m.Family.goodsInitialized){m.Family.goodsInitialized=true;m.Family.consumptionEpoch=session.Simulation.Tick;}};
        for(int i=0;i<36000&&(session.City.consumption.Bought.food==0||session.City.consumption.Bought.dailyGoods==0);i++)session.Advance(.05);
        session.Validate();Console.WriteLine("ZERO-KIT bought="+session.City.consumption.Bought.food+"/"+session.City.consumption.Bought.dailyGoods);
        Check(session.City.consumption.Bought.food>0&&session.City.consumption.Bought.dailyGoods>0,"Two-kind actual retail missing in zero-opening-inventory stress fixture");
    }
    static void InvestmentChecks()
    {
        var s=CityCoreIndustrySession.Create();var growth=s.Simulation.Growth;var worker=s.City.Citizens.First();s.City.ReleaseJob(worker);
        Check(growth.HasWorker(2),"Qualified unemployed worker not available for investment");
        worker.sick=true;Check(!growth.HasWorker(0),"Sick resident counted as eligible investment worker");
        worker.sick=false;worker.skill=0;Check(growth.HasWorker(0)&&!growth.HasWorker(1),"Investment skill notification stale");
        worker.skill=2;worker.tripId=123;Check(!growth.HasWorker(0),"Travelling worker counted for new business");worker.tripId=0;
        worker.canWork=false;Check(!growth.HasWorker(0),"Ineligible worker counted for investment");worker.canWork=true;
        var farShop=new CommercialBuilding{id=s.City.nextBuildingId,x=120,z=-7};s.Entities.AddBuilding(farShop);
        int a=s.Transport.AddJunction(new Point(100,0,0)),b=s.Transport.AddJunction(new Point(150,0,0));
        int road=s.Transport.AddRoad(a,b,new[]{s.Transport.JunctionPosition(a),s.Transport.JunctionPosition(b)});
        s.Transport.RegisterAccess(farShop.id,Network.LaneId(road,false),20);
        s.City.development.enabled=true;
        foreach(var shop in s.Entities.Shops)shop.Account.Capital=shop.Account.Cash=2000;
        var factory=s.Entities.Factories.Single();factory.Products=GoodsAmounts.Of(ProductKind.DailyGoods,8);
        // Isolate the investment selector with declared procurement inputs. These
        // are test-only derived demand entries, not injected production or money
        // during a normal simulated city. The two stores are on disjoint networks.
        var demands=(System.Collections.Generic.Dictionary<int,GoodsAmounts>)typeof(CityCoreConsumption).GetField("shopDemand",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(s.Simulation.Consumption);
        demands.Clear();demands[2]=GoodsAmounts.Of(ProductKind.DailyGoods,8);demands[farShop.id]=GoodsAmounts.Of(ProductKind.DailyGoods,6);
        bool opportunity=growth.InvestmentOpportunity(LandUse.Industrial,out var kind,out int reserve,out var focus);
        Check(opportunity&&kind==ProductKind.DailyGoods&&reserve==6&&focus.id==farShop.id,"Covered largest store hid uncovered second-region investment: opportunity="+opportunity+" kind="+kind+" reserve="+reserve+" focus="+focus?.id+" worker="+growth.HasWorker(1)+" access="+s.City.BuildingAccess(farShop.id)+" cash="+farShop.Account.Cash);
        demands[2]=GoodsAmounts.Of(ProductKind.Food,6);demands[farShop.id]=GoodsAmounts.Of(ProductKind.DailyGoods,6);
        Check(growth.InvestmentOpportunity(LandUse.Industrial,out kind,out reserve,out focus)&&kind==ProductKind.Food&&reserve==6&&focus.id==2,"Equal investment gaps did not prefer food");
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
        InvestmentChecks();ScheduleChecks();MixedPurchaseChecks();Console.WriteLine("PASS "+checks+" two-product checks");
    }
}
