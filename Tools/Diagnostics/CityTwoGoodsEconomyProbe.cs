using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using HarborCity;
using HarborCity.RoadCore;

// Read-only observations of the actual fixed-step model. No logSink, free goods,
// staffing overrides or balance adjustments. The water-cut mode reproduces the
// original regression input and is kept separate from uninterrupted operation.
static class CityTwoGoodsEconomyProbe
{
    static string Row(params object[] fields)=>string.Join(",",fields.Select(x=>Convert.ToString(x,CultureInfo.InvariantCulture)));
    static void Main(string[] args)
    {
        string output=Path.GetFullPath(args[0]);bool water=args.Length>1&&args[1]=="water-cut";
        if(Directory.Exists(output))throw new Exception("Choose fresh evidence output");Directory.CreateDirectory(output);
        var s=CityGrowingGridFixture.Create();
        var companies=new List<string>{"tick,day,minute,id,type,product,cash,capital,revenue,raw_cost,wages,processing,tax,stock_food,stock_daily,raw,produced,sold,shipped,attendance,productive,employed,paid_workers,customers,retail_units,wholesale_funded,supply,access,status,reason"};
        var city=new List<string>{"tick,day,minute,population,families,factories_food,factories_daily,shops,bought_food,bought_daily,consumed_food,consumed_daily,unmet_food,unmet_daily,requested_food,requested_daily,household_cash,employer_cash,factory_stock,shop_stock,active,problems"};
        var visits=new List<string>{"tick,day,minute,shop,visits,units,revenue,food_stock,daily_stock,cash,paid_workers,supply,access"};
        var tasks=new List<string>{"tick,day,minute,task,purpose,from,to,citizen,kind,food,daily,cargo,price"};
        var investment=new List<string>{"tick,day,minute,project,home,food_need,daily_need,existing_shops,reachable_shops,reachable_positive_cash,home_is_focus"};
        var market=new List<string>{"tick,day,minute,shop,cash,demand_food,demand_daily,target_food,target_daily,stock_food,stock_daily,incoming_food,incoming_daily,paid_workers,supply,access"};
        bool dailySeen=false;
        var availability=new List<string>{"tick,day,minute,household,need_food,cash,shopping_member,healthy_adults,available_at_home,at_work,travelling,supplier,problem,adult_jobs"};
        var projects=new HashSet<int>();
        var zero=new HashSet<int>();var priorCash=new Dictionary<int,double>();var priorVisits=new Dictionary<int,(int count,int sold,double revenue)>();
        s.Transport.TaskCommitted+=t=>tasks.Add(Row(s.Simulation.Tick,s.City.day,s.City.ResidentMinute,t.id,t.purpose,t.origin,t.destination,t.citizenId,t.Shipment?.kind,t.Shipment?.Products.food??0,t.Shipment?.Products.dailyGoods??0,t.Cargo,t.Price));
        Action<CityBuilding,string> company=(b,reason)=>
        {
            if(b is IndustrialBuilding f){var a=f.factory;companies.Add(Row(s.Simulation.Tick,s.City.day,s.City.ResidentMinute,b.id,"factory",a.product,a.cash,a.capital,a.salesRevenue,a.rawCosts,a.wageCosts,a.productionCosts,a.taxCosts,f.Products.food,f.Products.dailyGoods,a.raw,a.produced,a.sold,a.shipped,a.attendanceMinutes,a.productiveMinutes,s.City.EmployedAt(b.id),s.Simulation.Payroll.WorkersAt(b.id),0,0,0,s.City.Supply(b.id).Complete,s.City.BuildingAccess(b.id),a.status,reason));}
            else if(b is CommercialBuilding shop){var a=shop.Account;companies.Add(Row(s.Simulation.Tick,s.City.day,s.City.ResidentMinute,b.id,"shop","both",a.Cash,a.Capital,a.RetailRevenue,0,a.WageCosts,0,a.TaxCosts,shop.Products.food,shop.Products.dailyGoods,0,0,0,0,0,0,s.City.EmployedAt(b.id),s.Simulation.Payroll.WorkersAt(b.id),shop.customers,shop.retailSold,a.WholesaleFunded,s.City.Supply(b.id).Complete,s.City.BuildingAccess(b.id),"",reason));}
        };
        for(int tick=0;tick<40000;tick++)
        {
            if(water&&(tick==12000||tick==15000))s.Simulation.Utilities.SetBudget(CityServiceKind.Water,tick==12000?50:100);
            var beforeDemand=s.City.development.nextGrowth<=s.Simulation.Seconds?s.Simulation.Consumption.UnservedDemand.ToArray():null;
            s.Advance(.05);
            bool firstDaily=!dailySeen&&s.Simulation.Consumption.Requested.dailyGoods>0;if(firstDaily){dailySeen=true;Console.WriteLine("FIRST_DAILY tick="+s.Simulation.Tick+" day="+s.City.day+" minute="+s.City.ResidentMinute);}
            foreach(var p in s.City.development.projects)if(projects.Add(p.id)&&p.building.type==LandUse.Commercial&&beforeDemand!=null)
            {
                int focus=beforeDemand.Where(x=>s.City.BuildingAccess(x.Key)).OrderByDescending(x=>x.Value.Total).ThenBy(x=>x.Key).Select(x=>x.Key).FirstOrDefault();
                foreach(var home in beforeDemand){var reachable=s.Entities.Shops.Where(shop=>s.City.BuildingAccess(shop.id)&&s.Transport.CanReachResident(home.Key,shop.id)).ToArray();investment.Add(Row(s.Simulation.Tick,s.City.day,s.City.ResidentMinute,p.id,home.Key,home.Value.food,home.Value.dailyGoods,s.Entities.Shops.Count,reachable.Length,reachable.Count(shop=>shop.Account.Cash>0),home.Key==focus));}
            }
            foreach(var b in s.Entities.Buildings)
            {
                if(!(b is IndustrialBuilding)&&!(b is CommercialBuilding))continue;
                double cash=b is IndustrialBuilding f?f.factory.cash:((CommercialBuilding)b).Account.Cash;
                if(!priorCash.ContainsKey(b.id))company(b,"opened");
                if(cash<=0&&zero.Add(b.id)){company(b,"first_cash_zero");File.WriteAllBytes(Path.Combine(output,"zero-"+b.id+".hcity"),CityCoreSave.Write(s));}
                if(b is CommercialBuilding shop)
                {
                    priorVisits.TryGetValue(b.id,out var previous);
                    if(shop.customers!=previous.count)visits.Add(Row(s.Simulation.Tick,s.City.day,s.City.ResidentMinute,b.id,shop.customers-previous.count,shop.retailSold-previous.sold,shop.Account.RetailRevenue-previous.revenue,shop.Products.food,shop.Products.dailyGoods,cash,s.Simulation.Payroll.WorkersAt(b.id),s.City.Supply(b.id).Complete,s.City.BuildingAccess(b.id)));
                    priorVisits[b.id]=(shop.customers,shop.retailSold,shop.Account.RetailRevenue);
                }
                priorCash[b.id]=cash;if(tick%240==239||tick==39999)company(b,"sample");
                if(firstDaily)company(b,"first_daily_need");
                if(b is CommercialBuilding store&&(tick%240==239||tick==39999||firstDaily)){var d=s.Simulation.Consumption.DemandAt(b.id);var incoming=s.Simulation.Shipments.Goods(b.id);market.Add(Row(s.Simulation.Tick,s.City.day,s.City.ResidentMinute,b.id,store.Account.Cash,d.food,d.dailyGoods,s.Simulation.Consumption.RestockTarget(b.id,ProductKind.Food),s.Simulation.Consumption.RestockTarget(b.id,ProductKind.DailyGoods),store.Products.food,store.Products.dailyGoods,incoming.food,incoming.dailyGoods,s.Simulation.Payroll.WorkersAt(b.id),s.City.Supply(b.id).Complete,s.City.BuildingAccess(b.id)));}
            }
            if(tick%240==239||tick==39999)foreach(var h in s.City.society.families){var adults=h.people.Where(p=>!p.dead&&!p.sick&&p.age>=18).ToArray();availability.Add(Row(s.Simulation.Tick,s.City.day,s.City.ResidentMinute,h.id,s.Simulation.Consumption.Need(h,ProductKind.Food),h.savings,h.shoppingMember,adults.Length,adults.Count(p=>!p.atWork&&p.tripId==0&&p.location==h.home&&p.carriedGoods==0&&s.Transport.Clock>=p.retryAt),adults.Count(p=>p.atWork),adults.Count(p=>p.tripId!=0),h.goodsSupplier,h.goodsProblem,string.Join(";",adults.Select(p=>p.id+":"+s.City.Workplace(p)+":"+(s.City.GetBuilding(s.City.Workplace(p))?.Use.ToString()??"none")))));}
            if(tick%240==239||tick==39999){var c=s.City;var demand=s.Simulation.Consumption.Requested;city.Add(Row(s.Simulation.Tick,c.day,c.ResidentMinute,c.population,c.society.families.Count,s.Entities.Factories.Count(f=>f.factory.product==ProductKind.Food),s.Entities.Factories.Count(f=>f.factory.product==ProductKind.DailyGoods),s.Entities.Shops.Count,c.consumption.Bought.food,c.consumption.Bought.dailyGoods,c.consumption.Consumed.food,c.consumption.Consumed.dailyGoods,c.consumption.Unmet.food,c.consumption.Unmet.dailyGoods,demand.food,demand.dailyGoods,c.society.families.Sum(h=>(long)h.savings),s.Entities.Factories.Sum(f=>f.factory.cash)+s.Entities.Shops.Sum(b=>b.Account.Cash),s.Entities.Factories.Sum(f=>f.Stock),s.Entities.Shops.Sum(b=>b.Stock),s.Transport.ActiveCount,string.Join(";",c.society.families.GroupBy(h=>h.goodsProblem).OrderBy(g=>g.Key).Select(g=>g.Key+":"+g.Count()))));}
        }
        s.Validate();File.WriteAllBytes(Path.Combine(output,"final.hcity"),CityCoreSave.Write(s));
        File.WriteAllLines(Path.Combine(output,"availability.csv"),availability);
        File.WriteAllLines(Path.Combine(output,"summary.csv"),new[]{"tick,population,initial_food,bought_food,produced_food,consumed_food,unmet_food,export_food,export_daily,raw_dispatched,raw_delivered,external_investment,household_cash,company_cash",Row(s.Simulation.Tick,s.City.population,s.City.consumption.Initial.food,s.City.consumption.Bought.food,s.Entities.Factories.Where(f=>f.factory.product==ProductKind.Food).Sum(f=>f.factory.produced),s.City.consumption.Consumed.food,s.City.consumption.Unmet.food,s.City.trade.ExportedProducts.food,s.City.trade.ExportedProducts.dailyGoods,s.City.trade.rawDispatched,s.City.trade.rawDelivered,s.City.development.externalInvestment,s.City.society.families.Sum(h=>(long)h.savings),s.Entities.Factories.Sum(f=>f.factory.cash)+s.Entities.Shops.Sum(b=>b.Account.Cash))});
        File.WriteAllLines(Path.Combine(output,"companies.csv"),companies);File.WriteAllLines(Path.Combine(output,"city.csv"),city);File.WriteAllLines(Path.Combine(output,"visits.csv"),visits);File.WriteAllLines(Path.Combine(output,"tasks.csv"),tasks);File.WriteAllLines(Path.Combine(output,"investment.csv"),investment);File.WriteAllLines(Path.Combine(output,"market.csv"),market);
        File.WriteAllLines(Path.Combine(output,"jobs.csv"),new[]{"job,building,wage,skill,occupant"}.Concat(s.City.society.jobEntities.Select(j=>Row(j.id,j.buildingId,j.wage,j.requiredSkill,j.occupiedCitizenId))));
        File.WriteAllLines(Path.Combine(output,"families.csv"),new[]{"id,home,people,cash,opening,income,tax,retail,food,daily,unmet_food,unmet_daily,need_food,need_daily,supplier,problem"}.Concat(s.City.society.families.Select(h=>Row(h.id,h.home,h.people.Count,h.savings,h.openingSavings,h.incomeReceived,h.taxPaid,h.retailPaid,h.goods.food,h.goods.dailyGoods,h.unmetProducts.food,h.unmetProducts.dailyGoods,s.Simulation.Consumption.Need(h,ProductKind.Food),s.Simulation.Consumption.Need(h,ProductKind.DailyGoods),h.goodsSupplier,h.goodsProblem))));
        Console.WriteLine("VALIDATED "+(water?"water-cut":"uninterrupted")+" tick="+s.Simulation.Tick+" population="+s.City.population+" bought="+s.City.consumption.Bought.food+"/"+s.City.consumption.Bought.dailyGoods+" unmet="+s.City.consumption.Unmet.food+"/"+s.City.consumption.Unmet.dailyGoods+" output="+output);
    }
}
