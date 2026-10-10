using System;
using System.Linq;
using HarborCity;

public static class ServiceBudgetChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Service budgets: "+why);checks++;}
    public static void Run()
    {
        var c=TestCity.Create();c.development.enabled=true;UtilitySupplyChecks.ConnectFixture(c);c.waste.enabled=false;
        int normal=c.upkeep,funds=c.money;
        Check(c.power==160 && c.water==160 && c.GarbageFleetLimit==3,"Default budgets preserve connected baseline supply");
        Check(c.SetServiceBudget(CityServiceKind.Electricity,50) && c.power==80 && c.upkeep==normal-45 && c.money==funds,"Power budget changes real connected capacity and expense, not instant cash");
        Check(c.SetServiceBudget(CityServiceKind.Water,150) && c.water==240 && c.upkeep==normal-45+66,"Water budget affects both actual fresh and sewage facilities");
        Check(c.SetServiceBudget(CityServiceKind.Electricity,150) && c.power==240,"Higher budget scales the producing facility rather than consumer coverage");
        var saved=TestCity.RoundTrip(c);Check(saved.power==240 && saved.water==240 && saved.upkeep==c.upkeep && saved.Valid(),"Budget settings and derived services survive saves");
        c.society.settings.applicantsPerDay=0;int expected=c.upkeep;funds=c.money;c.Tick();
        Check(c.money==funds+c.income-expected && c.society.history.Last().maintenance==expected,"Budget expense enters actual daily city settlement");
        Check(!c.SetServiceBudget((CityServiceKind)99,100) && !c.SetServiceBudget(CityServiceKind.Water,49),"Invalid service or range cannot mutate settings");
        saved.development.garbageBudget=0;Check(!saved.Valid(),"Malformed saved budget rejected before loading");
        c=TestCity.Create();c.development.enabled=true;UtilitySupplyChecks.ConnectFixture(c);int depot=TestCity.Build(c,-33,-7,LandUse.Landfill);
        int target=c.buildings.OfType<ResidentialBuilding>().OrderByDescending(b=>b.x).First().id;c.GetBuilding(target).garbage=32;c.waste.generated=32;
        c.SetServiceBudget(CityServiceKind.Garbage,150);var sim=new CityTraffic(c);sim.Advance(2.5f);
        int count=c.traffic.trips.Count(t=>t.cargoKind==CargoKind.Waste);
        Check(count==4 && c.GarbageFleetLimit==4,"Higher garbage budget provides a real finite larger fleet");
        c.SetServiceBudget(CityServiceKind.Garbage,50);
        Check(c.traffic.trips.Count(t=>t.cargoKind==CargoKind.Waste)==4 && c.GarbageFleetLimit==1 && sim.DispatchWaste(depot,target)==null && c.WasteBalanceError==0,"Budget cuts stop new dispatch without deleting working trucks or inventories");
        c.waste.enabled=false;sim.Advance(60);
        Check(c.WasteBalanceError==0 && c.Valid(),"Trucks already dispatched safely finish after a budget cut");
        Console.WriteLine("PASS: "+checks+" service budget/capacity/fleet/expense/save checks");
    }
}
