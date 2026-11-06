using System;
using System.Globalization;
using System.IO;
using System.Linq;
using HarborCity;
using HarborCity.RoadCore;

// Small-city economy evidence. All transactions go through CitySimulation.
static class CitySmallEconomyStudy
{
    static void Main(string[] args)
    {
        string output=args[0], profile=args[1];
        int population=int.Parse(args[2]), days=int.Parse(args[3]);
        var design=new GameDesignData();
        if(profile=="Pilot"||profile=="PilotShortDay"||profile=="SmallBusiness")
        {
            design.products[0].retailPrice=40;
            design.products[0].wholesalePrice=22;
            design.products[0].labourMinutes=8;
            design.products[1].retailPrice=84;
            design.products[1].wholesalePrice=48;
            design.products[1].labourMinutes=24;
            design.employment.commercialShiftStartMinute=840;
        }
        if(profile=="SmallBusiness")design.employment.slotsPerLevel=1;
        var session=population==0?CityGrowingGridFixture.Create(design:design):
            CityScenarioBuilder.Create(new CityScenarioSettings
            {
                population=population,gridRadius=3,householdsPerResidentialBuilding=4,seedRoadNetwork=true
            },design);
        int daySeconds=profile=="Pilot"||profile=="SmallBusiness"?360:120;
        session.City.society.settings.secondsPerDay=daySeconds;
        var imports=session.City.trade.connections.Where(p=>p.active&&p.flow==OutsideFlow.Import).ToArray();
        if(!imports.Any(p=>session.Transport.GetAccess(p.id)?.valid==true))
            throw new InvalidOperationException("The study needs a real active import road");
        foreach(var factory in session.Entities.Factories)
            if(!imports.Any(port=>!double.IsInfinity(session.Transport.FreightTravelSeconds(port.id,factory.id))))
                throw new InvalidOperationException("No freight route from the import road to factory "+factory.id);
        Console.WriteLine($"{profile}, authored population {population}, {days} days, {daySeconds} seconds/day, import ports {imports.Length}");

        long ticksPerDay=(long)Math.Round(daySeconds/SimulationClock.StepSeconds);
        using(var daily=new StreamWriter(Path.Combine(output,"daily.csv")))
        {
            daily.WriteLine("elapsed_days,game_day,population,households,food_empty,daily_empty,shops,shops_cash_zero,food_factories,daily_factories,factories_cash_zero,food_bought,daily_bought,food_consumed,daily_consumed,food_unmet,daily_unmet,food_pantry,daily_pantry,raw_delivered,food_export,daily_export,household_cash,shop_cash,factory_cash,outside_cash,household_income,household_retail,household_tax,shop_revenue,shop_procurement,shop_wages,shop_tax,factory_revenue,factory_raw,factory_wages,factory_processing,factory_tax");
            Snapshot(session,daily,0);
            for(int day=1;day<=days;day++)
            {
                for(long i=0;i<ticksPerDay;i++)session.Advance(SimulationClock.StepSeconds);
                Snapshot(session,daily,day);
                daily.Flush();
            }
        }
        session.Validate();
        File.WriteAllBytes(Path.Combine(output,"final.hcity"),CityCoreSave.Write(session));
        using(var households=new StreamWriter(Path.Combine(output,"households.csv")))
        {
            households.WriteLine("household,people,food,daily_goods,savings,income,retail,tax,supplier,shopping_member,reason,employed,unfunded_jobs,wage_credit");
            foreach(var h in session.City.society.families.Where(h=>h.resident))
                households.WriteLine(string.Join(",",new object[]{h.id,h.people.Count,h.goods.food,h.goods.dailyGoods,h.savings,h.incomeReceived,h.retailPaid,h.taxPaid,h.goodsSupplier,h.shoppingMember,h.goodsProblem,h.people.Count(p=>session.City.ResidentJob(p)!=null),h.people.Count(p=>session.City.ResidentJob(p)!=null&&!session.City.JobFunded(session.City.ResidentJob(p))),h.people.Sum(p=>p.wageCredit)}.Select(x=>Convert.ToString(x,CultureInfo.InvariantCulture))));
        }
        Console.WriteLine($"Validated tick {session.Simulation.Tick}, population {session.City.population}, raw delivered {session.City.trade.rawDelivered}");
    }

    static void Snapshot(CityCoreIndustrySession session,StreamWriter output,int elapsedDays)
    {
        var city=session.City;
        var households=city.society.families.Where(h=>h.resident).ToArray();
        var shops=session.Entities.Shops;
        var factories=session.Entities.Factories;
        var consumption=city.consumption;
        object[] row={elapsedDays,city.day,city.population,households.Length,
            households.Count(h=>h.goodsInitialized&&h.goods.food==0),households.Count(h=>h.goodsInitialized&&h.goods.dailyGoods==0),
            shops.Count,shops.Count(b=>b.Account.Cash<=0),factories.Count(b=>b.factory.product==ProductKind.Food),
            factories.Count(b=>b.factory.product==ProductKind.DailyGoods),factories.Count(b=>b.factory.cash<=0),
            consumption.Bought.food,consumption.Bought.dailyGoods,consumption.Consumed.food,consumption.Consumed.dailyGoods,
            consumption.Unmet.food,consumption.Unmet.dailyGoods,households.Sum(h=>(long)h.goods.food),households.Sum(h=>(long)h.goods.dailyGoods),
            city.trade.rawDelivered,city.trade.ExportedProducts.food,city.trade.ExportedProducts.dailyGoods,
            households.Sum(h=>(long)h.savings),shops.Sum(b=>b.Account.Cash),factories.Sum(b=>b.factory.cash),city.trade.cash,
            households.Sum(h=>h.incomeReceived),households.Sum(h=>(long)h.retailPaid),households.Sum(h=>h.taxPaid),
            shops.Sum(b=>b.Account.RetailRevenue),shops.Sum(b=>b.Account.WholesaleFunded),shops.Sum(b=>b.Account.WageCosts),shops.Sum(b=>b.Account.TaxCosts),
            factories.Sum(b=>b.factory.salesRevenue),factories.Sum(b=>b.factory.rawCosts),factories.Sum(b=>b.factory.wageCosts),
            factories.Sum(b=>b.factory.productionCosts),factories.Sum(b=>b.factory.taxCosts)};
        output.WriteLine(string.Join(",",row.Select(x=>Convert.ToString(x,CultureInfo.InvariantCulture))));
    }
}
