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
    static string EventData(object value)
    {
        if(value==null)return "null";var type=value.GetType();
        if(type.IsPrimitive||type.IsEnum||value is string||value is decimal)return type.FullName+":"+Convert.ToString(value,System.Globalization.CultureInfo.InvariantCulture);
        if(value is System.Collections.IEnumerable items){var entries=new System.Collections.Generic.List<string>();foreach(var item in items)entries.Add(EventData(item));return "["+string.Join(";",entries)+"]";}
        var parts=new System.Collections.Generic.List<string>();
        foreach(var field in type.GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance).Where(f=>!f.IsNotSerialized).OrderBy(f=>f.Name))parts.Add(field.Name+"="+EventData(field.GetValue(value)));
        foreach(var property in type.GetProperties().Where(p=>p.IsDefined(typeof(CitySavedFactAttribute),false)).OrderBy(p=>p.Name))parts.Add(property.Name+"="+EventData(property.GetValue(value)));
        return type.FullName+"{"+string.Join(";",parts)+"}";
    }
    static string EventState(CityLogEvent e)=>string.Join("|",e.tick,e.type,e.level,e.message,e.day,e.minute.ToString("R",System.Globalization.CultureInfo.InvariantCulture),e.householdId,e.citizenId,e.jobId,e.buildingId,e.tripId,e.roadRevision,EventData(e.data));
    static string TaskState(TripTask t)=>string.Join("|",t.id,t.origin,t.destination,t.purpose,t.citizenId,t.departedAt.ToString("R",System.Globalization.CultureInfo.InvariantCulture),t.state,t.Shipment?.kind,t.Cargo,t.Price.ToString("R",System.Globalization.CultureInfo.InvariantCulture),t.Shipment?.Products.food??0,t.Shipment?.Products.dailyGoods??0);
    static void ScheduleChecks(bool observeEvents)
    {
        var live=CityGrowingGridFixture.Create();var cold=CityCoreSave.Read(CityCoreSave.Write(live));cold.Simulation.Growth.Zoning.SetSurface((x,z)=>2);cold.Simulation.Consumption.ReferencePolling=true;
        var liveEvents=new System.Collections.Generic.List<string>();var referenceEvents=new System.Collections.Generic.List<string>();long eventCount=0;bool eventError=false;
        Action<TripTask> liveCommit=t=>liveEvents.Add("transport.commit|"+live.Simulation.Tick+"|"+TaskState(t)),referenceCommit=t=>referenceEvents.Add("transport.commit|"+cold.Simulation.Tick+"|"+TaskState(t));
        Action<int> liveRetire=id=>liveEvents.Add("transport.retire|"+live.Simulation.Tick+"|"+id),referenceRetire=id=>referenceEvents.Add("transport.retire|"+cold.Simulation.Tick+"|"+id);
        if(observeEvents){live.City.logSink=e=>{try{liveEvents.Add(EventState(e));}catch{eventError=true;}};cold.City.logSink=e=>{try{referenceEvents.Add(EventState(e));}catch{eventError=true;}};live.Transport.TaskCommitted+=liveCommit;cold.Transport.TaskCommitted+=referenceCommit;live.Transport.TaskRetired+=liveRetire;cold.Transport.TaskRetired+=referenceRetire;}
        for(int t=0;t<40000;t++)
        {
            if(t==12000||t==15000){int budget=t==12000?50:100;live.Simulation.Utilities.SetBudget(CityServiceKind.Water,budget);cold.Simulation.Utilities.SetBudget(CityServiceKind.Water,budget);}
            live.Advance(.05);cold.Advance(.05);
            string da=string.Join(";",live.Simulation.Consumption.ShopDemand.OrderBy(x=>x.Key).Select(x=>x.Key+":"+x.Value.food+":"+x.Value.dailyGoods)),db=string.Join(";",cold.Simulation.Consumption.ShopDemand.OrderBy(x=>x.Key).Select(x=>x.Key+":"+x.Value.food+":"+x.Value.dailyGoods));
            if(da!=db){Console.WriteLine("DEMAND "+live.Simulation.Tick+" "+da+" vs "+db);foreach(var h in live.City.society.families)Console.WriteLine("H "+h.id+" supplier "+h.goodsSupplier+" store "+h.goods.food+"/"+h.goods.dailyGoods+" incoming actor "+h.shoppingMember+" needs "+live.Simulation.Consumption.Need(h,ProductKind.Food)+"/"+live.Simulation.Consumption.Need(h,ProductKind.DailyGoods));throw new Exception("Derived demand mismatch");}
            if(t%100==99){if(!CityCoreSave.Write(live).SequenceEqual(CityCoreSave.Write(cold))){GoodsDiff(live.SnapshotBusiness(),cold.SnapshotBusiness(),"city");GoodsDiff(live.Transport.Snapshot(),cold.Transport.Snapshot(),"world");Console.WriteLine("event visits="+live.Simulation.Consumption.FamilyVisits+" oracle="+cold.Simulation.Consumption.FamilyVisits);}Check(CityCoreSave.Write(live).SequenceEqual(CityCoreSave.Write(cold)),"Reference scheduling differs at "+live.Simulation.Tick);if(observeEvents){Check(!eventError&&liveEvents.SequenceEqual(referenceEvents),"Reference business event data/order differs at "+live.Simulation.Tick);eventCount+=liveEvents.Count;liveEvents.Clear();referenceEvents.Clear();}}
        }
        if(observeEvents){Check(eventCount>0,"Business event comparison observed no events");Console.WriteLine("EVENTS matched="+eventCount);live.City.logSink=null;cold.City.logSink=null;live.Transport.TaskCommitted-=liveCommit;cold.Transport.TaskCommitted-=referenceCommit;live.Transport.TaskRetired-=liveRetire;cold.Transport.TaskRetired-=referenceRetire;}
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
        var nearShop=new CommercialBuilding{id=s.City.nextBuildingId,x=27,z=-7};s.Entities.AddBuilding(nearShop);
        var access=s.Transport.GetAccess(2);s.Transport.RegisterAccess(nearShop.id,access.lane,access.distance);
        nearShop.Account.Capital=nearShop.Account.Cash=2000;
        demands.Clear();demands[2]=GoodsAmounts.Of(ProductKind.DailyGoods,8);demands[nearShop.id]=GoodsAmounts.Of(ProductKind.DailyGoods,6);
        Check(growth.InvestmentOpportunity(LandUse.Industrial,out kind,out reserve,out focus)&&kind==ProductKind.DailyGoods&&reserve==6&&focus.id==nearShop.id,"The same eight goods covered two separate store orders");
        factory.Products=default;factory.externalCapital=1200;factory.factory.firstDelivery=false;factory.factory.cash=0;
        s.Entities.ProductInventoryChanged(factory);demands.Clear();demands[2]=GoodsAmounts.Of(ProductKind.DailyGoods,8);
        Check(growth.InvestmentOpportunity(LandUse.Industrial,out kind,out reserve,out focus)&&kind==ProductKind.DailyGoods&&reserve==8,"Insolvent factory with no first delivery blocked replacement forever");
    }
    static void ImportBeforeFactoryOpening()
    {
        var city=CityGrowingGridFixture.Create(connectOutside:false);
        for(int i=0;i<12000;i++)city.Advance(.05);
        Check(city.Entities.Shops.Count>0,"Missing-import control never created demand at a shop");
        Check(city.Simulation.Consumption.ShopDemand.Count>0,"Missing-import control has no shop procurement demand");
        Check(city.Entities.Factories.Count==0&&!city.City.development.projects.Any(p=>p.building.type==LandUse.Industrial),"Industrial project opened without an import route");
        city.Simulation.Trade.ConnectBorder(new Point(-167,2,0));
        for(int i=0;i<9600;i++)city.Advance(.05);
        Check(city.Entities.Factories.Any(f=>f.factory.imported>0),"Connecting a reachable import did not enable industrial production");
        city.Validate();
    }
    static void RecoveryChecks()
    {
        var s=CityCoreIndustrySession.Create();s.City.development.enabled=true;
        var worker=s.City.Citizens.First();var family=s.City.society.families.First();
        var factory=s.Entities.Factories.Single();
        int job=worker.jobId;
        Check(s.Simulation.Growth.NearTermProduction(factory)==0,"Factory without utilities counted as productive capacity");
        var growing=CityGrowingGridFixture.Create();IndustrialBuilding productive=null;
        for(int i=0;i<12000&&productive==null;i++)
        {growing.Advance(.05);productive=growing.Entities.Factories.FirstOrDefault(f=>growing.Simulation.Growth.NearTermProduction(f)>0);}
        Check(productive!=null,"Owned material and funded labour not counted as near-term supply");
        int before=productive.factory.raw;productive.factory.raw=0;productive.factory.processing=false;
        int forecast=growing.Simulation.Growth.NearTermProduction(productive);
        Check(forecast<=growing.Simulation.Shipments.Raw(productive.id),"Forecast invented raw material");
        productive.factory.raw=before;productive.factory.cash=0;
        Check(growing.Simulation.Growth.NearTermProduction(productive)==0,"Unfunded factory counted as productive capacity");
        factory.factory.cash=0;
        worker.wageCredit=7.25;worker.location=family.home;worker.atWork=false;
        worker.tripId=123;
        s.Simulation.Population.ReleaseUnfundedJobs();
        Check(worker.jobId==job,"Recovery changed a travelling resident's job");
        worker.tripId=0;worker.atWork=true;
        s.Simulation.Population.ReleaseUnfundedJobs();
        Check(worker.jobId==job,"Recovery changed a resident's active shift");
        worker.atWork=false;worker.location=factory.id;
        s.Simulation.Population.ReleaseUnfundedJobs();
        Check(worker.jobId==job,"Recovery reassigned a resident away from home");
        worker.location=family.home;worker.carriedProducts=GoodsAmounts.Of(ProductKind.Food,1);
        s.Simulation.Population.ReleaseUnfundedJobs();
        Check(worker.jobId==job,"Recovery changed employment before carried goods were settled");
        worker.carriedProducts=default;
        // Declared transport fixture: a factory receivable is still in transit.
        factory.Products=GoodsAmounts.Of(ProductKind.DailyGoods,1);
        var shop=s.Entities.Shops.Single();shop.Account.Capital=shop.Account.Cash=2000;
        var shipment=s.Simulation.RequestFreight(factory.id,shop.id,1);
        Check(shipment!=null,"Recovery receivable fixture did not depart");
        s.Simulation.Population.ReleaseUnfundedJobs();
        Check(worker.jobId==job,"Recovery dismissed an employee while freight settlement was pending");
        // A separate no-freight fixture isolates the recovery boundary.
        var home=CityCoreIndustrySession.Create();home.City.development.enabled=true;
        var resident=home.City.Citizens.First();var household=home.City.society.families.First();
        var employer=home.Entities.Factories.Single();employer.factory.cash=0;
        resident.location=household.home;resident.atWork=false;resident.wageCredit=7.25;
        int previous=resident.jobId;
        Check(home.Simulation.Population.ReleaseUnfundedJobs()&&resident.jobId<0&&home.City.Job(previous).occupiedCitizenId<0,"Unfunded employee was not released at home");
        Check(resident.wageCredit==7.25,"Job recovery discarded earned wages");
        Check(!home.Simulation.Population.ReleaseUnfundedJobs(),"Repeated recovery changed an already released job");
    }
    static void HouseholdJobPriorityChecks()
    {
        var s=CityCoreIndustrySession.Create();s.City.development.enabled=true;
        var original=s.City.society.families.First();
        s.City.ReleaseJob(original.people[0]);s.City.ReleaseJob(original.people[1]);
        ((ResidentialBuilding)s.Entities.Building(original.home)).housingUnits=3;
        for(int i=0;i<2;i++)
        {
            var h=new Household{id=s.City.society.nextId,home=original.home,unit=i+1,resident=true,savings=500,openingSavings=500};
            s.Entities.AddFamily(h);
            s.Entities.AddResident(h.id,new CityResident{id=s.City.society.nextCitizenId,age=30,canWork=true,skill=2,location=original.home});
        }
        var newcomers=s.City.society.families.Skip(1).ToArray();
        Check(s.Simulation.Population.FindJobs(),"Priority fixture assigned no jobs");
        Check(newcomers.All(h=>h.people.Single().jobId>=0),"Additional earners displaced households without a funded job");
        Check(original.people[0].jobId<0&&original.people[1].jobId<0&&original.people[2].jobId>=0,"Priority displaced an existing contract");
        var ill=newcomers[0].people.Single();s.City.ReleaseJob(ill);ill.sick=true;
        s.Simulation.Population.FindJobs();
        Check(ill.jobId<0,"Sick resident recruited ahead of available workers");
    }
    static void OpenMarketChecks()
    {
        var design=GameDesignData.OpenTown();design.economy.outsideFoodOrdersPerDay=8;design.economy.outsideDailyGoodsOrdersPerDay=5;
        var city=CityGrowingGridFixture.Create(design:design);
        var orders=new System.Collections.Generic.Dictionary<(int,ProductKind),int>();
        bool exceeded=false;double expectedFunded=0;bool savedTransit=false;
        city.Transport.TaskCommitted+=task=>
        {
            if(task.purpose!=TransportPurpose.Freight||city.Entities.Connection(task.destination)?.flow!=OutsideFlow.Export)return;
            var kind=((IndustrialBuilding)city.Entities.Building(task.origin)).factory.product;var key=(city.City.day,kind);
            orders.TryGetValue(key,out int amount);amount+=task.Cargo;orders[key]=amount;
            exceeded|=amount>city.Simulation.Design.Economy.DailyExportOrders[kind];expectedFunded+=task.Cargo*task.Price;
        };
        int dayTicks=city.Simulation.Design.Employment.NewCityDaySeconds*20;
        for(int i=0;i<dayTicks*20;i++)
        {
            city.Advance(.05);
            if(!savedTransit&&city.Transport.Tasks.Any(t=>city.Entities.Connection(t.destination)?.flow==OutsideFlow.Export))
            {
                var bytes=CityCoreSave.Write(city);var restored=CityCoreSave.Read(bytes);
                Check(BitConverter.ToInt32(bytes,4)==20&&CityCoreSave.Write(restored).SequenceEqual(bytes),"Open-market active export did not restore exactly");
                Check(restored.City.trade.AcceptedExportOrders.Equals(city.City.trade.AcceptedExportOrders)&&restored.City.trade.exportFunded==city.City.trade.exportFunded,"Cold export lost booked orders or external prepayment");
                var old=(byte[])bytes.Clone();Buffer.BlockCopy(BitConverter.GetBytes(19),0,old,4,4);bool refused=false;
                try{CityCoreSave.Read(old);}catch(System.IO.InvalidDataException){refused=true;}Check(refused,"Old finite-market save header was accepted");
                restored.Simulation.Growth.Zoning.SetSurface((x,z)=>2);
                for(int tick=0;tick<dayTicks+600;tick++){city.Advance(.05);restored.Advance(.05);}
                i+=dayTicks+600;
                Check(CityCoreSave.Write(city).SequenceEqual(CityCoreSave.Write(restored)),"Export cold continuation changed day-order accounting");
                savedTransit=true;
            }
        }
        city.Validate();
        Check(savedTransit&&!exceeded&&orders.Count>2,"Daily external order cap or physical export control failed");
        Check(city.City.trade.exportFunded==expectedFunded,"External prepayment differs from committed shipments");
        Check(city.City.trade.ExportedProducts.food>0&&city.City.trade.ExportedProducts.dailyGoods>0,"Both product kinds did not reach external customers");
        var zero=GameDesignData.OpenTown();zero.economy.outsideFoodOrdersPerDay=zero.economy.outsideDailyGoodsOrdersPerDay=0;
        var closed=CityGrowingGridFixture.Create(design:zero);
        for(int i=0;i<dayTicks*8;i++)closed.Advance(.05);
        closed.Validate();Check(closed.City.trade.rawDelivered>0&&closed.City.trade.exportFunded==0&&closed.City.trade.goodsExported==0,"Zero external orders still funded or exported goods");
        var invalid=GameDesignData.OpenTown();invalid.economy.outsideFoodOrdersPerDay=-1;Reject(()=>new CityGameDesign(invalid));
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
        OpenMarketChecks();HouseholdJobPriorityChecks();RecoveryChecks();InvestmentChecks();ImportBeforeFactoryOpening();ScheduleChecks(false);ScheduleChecks(true);MixedPurchaseChecks();Console.WriteLine("PASS "+checks+" two-product checks");
    }
}
