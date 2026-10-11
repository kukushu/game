using System;
using System.Linq;
using HarborCity;
public static class SewageDiagnosticChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Sewage diagnostics: "+why);checks++;}
    public static void Run()
    {
        var c=TestCity.Create();c.development.enabled=true;UtilitySupplyChecks.ConnectFixture(c);var drain=c.buildings.OfType<SewageBuilding>().Single();
        Check(c.SewageCapacity==160 && c.InstalledSewageCapacity==160 && c.WaterDemand==c.buildings.Where(b=>CityZoningState.ZoneUse(b.Use)).Sum(b=>b.Use==LandUse.Residential?Math.Max(1,c.society.families.Where(h=>h.resident && h.home==b.id).Sum(h=>c.LivingMembers(h))):Math.Max(1,c.EmployedAt(b.id)*2)),"Demand and available capacity derive from real consumers and working connected outlets");
        Check(c.WaterFacilityDetails(drain.id).Contains("水管：已接通") && c.WaterFacilityDetails(drain.id).Contains("运行中") && c.Supply(drain.id).pipes.Count==1,"Facility separates pipe connection from operating prerequisites");
        var saved=TestCity.RoundTrip(c);Check(saved.SewageCapacity==c.SewageCapacity && saved.WaterDemand==c.WaterDemand && saved.WaterFacilityDetails(drain.id)==c.WaterFacilityDetails(drain.id),"Native DTO load derives diagnostics instead of saving another truth");
        c.utilities.electricity=new CityUtilityNetwork();c.Recalculate();
        Check(c.SewageCapacity==0 && c.InstalledSewageCapacity==160 && c.Supply(drain.id).pipes.Count==1 && c.WaterFacilityDetails(drain.id).Contains("停运 · 缺电力"),"Connected but unpowered outlet contributes zero available capacity");
        c=TestCity.RoundTrip(saved);c.utilities.water=new CityUtilityNetwork();c.Recalculate();
        Check(c.SewageCapacity==0 && c.Supply(drain.id).pipes.Count==0 && c.WaterFacilityDetails(drain.id).Contains("停运 · 缺水管"),"Powered outlet without pipes is distinguishable from an unpowered outlet");
        c=TestCity.RoundTrip(saved);c.SetServiceBudget(CityServiceKind.Water,50);c.Recalculate();
        Check(c.SewageCapacity==c.ServiceCapacity(CityServiceKind.Water,160),"Budget changes alter actual outlet capacity and display together");
        var supply=new BuildingSupply{electricity=true,water=true};supply.pipes.Add(new PipeSupply{id=1,load=170,fresh=320,drain=160});
        Check(supply.Problem.Contains("排污容量不足") && supply.Problem.Contains("170") && supply.Problem.Contains("160"),"Connected undersized network reports precise demand/capacity");
        supply.pipes[0].drain=0;Check(supply.Problem.Contains("没有运行中的排污口"),"No operating outlet differs from capacity shortage");
        Console.WriteLine("PASS: "+checks+" sewage capacity/connection/power/diagnostic/save checks");
    }
}
