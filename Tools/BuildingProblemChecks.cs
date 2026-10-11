using System;
using System.Linq;
using HarborCity;

public static class BuildingProblemChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Building problems: "+why);checks++;}
    public static void Run()
    {
        var c=TestCity.Create();c.development.enabled=true;UtilitySupplyChecks.ConnectFixture(c);
        int home=c.buildings.OfType<ResidentialBuilding>().First().id;
        var pipes=c.utilities.water;var power=c.utilities.electricity;var generator=c.buildings.First(b=>b is PowerBuilding);
        c.utilities.electricity=new CityUtilityNetwork();c.utilities.water=new CityUtilityNetwork();
        c.buildings.RemoveAll(b=>b is PowerBuilding);c.Recalculate();c.GetBuilding(home).garbage=32;
        var problems=c.BuildingProblems().Where(p=>p.buildingId==home).ToList();
        Check(problems.Any(p=>p.kind==BuildingProblemKind.Electricity) && problems.Any(p=>p.kind==BuildingProblemKind.Water)
            && problems.Any(p=>p.kind==BuildingProblemKind.Sewage) && problems.Any(p=>p.kind==BuildingProblemKind.Garbage),"Simultaneous problems are all visible");
        c.GetBuilding(home).outageDays=2;
        Check(c.BuildingProblems().Where(p=>p.buildingId==home).All(p=>p.severity==2),"Existing outage counter drives pre-abandonment severity");
        c.utilities.water=pipes;c.utilities.electricity=power;c.buildings.Add(generator);c.GetBuilding(home).garbage=0;c.Recalculate();
        Check(!c.BuildingProblems().Any(p=>p.buildingId==home),"Resolving actual problems removes badges");
        Console.WriteLine("PASS: "+checks+" derived building problem/multiple causes/severity/recovery checks");
    }
}
