using System;
using System.Linq;
using HarborCity;

public static class MapViewChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Map views: "+why);checks++;}
    public static void Run()
    {
        var c=TestCity.Create();c.development.enabled=true;UtilitySupplyChecks.ConnectFixture(c);
        int home=c.buildings.OfType<ResidentialBuilding>().First().id,shop=c.buildings.OfType<CommercialBuilding>().First().id;
        Check(c.MapIndicator(home,CityMapView.Electricity)==1 && c.MapIndicator(home,CityMapView.Water)==1,"Coverage indicators use real connected supply");
        c.utilities.water=new CityUtilityNetwork();c.Recalculate();
        Check(c.MapIndicator(home,CityMapView.Water)==0 && c.MapIndicator(home,CityMapView.Electricity)==1,"Disconnected pipes change water indicators without falsely changing electricity");
        c.GetBuilding(home).garbage=CityModel.GarbageWarning/2;
        Check(c.MapIndicator(home,CityMapView.Garbage)==.5f,"Garbage map shows actual building accumulation");
        int depot=TestCity.Build(c,34,-7,LandUse.Landfill);((LandfillBuilding)c.GetBuilding(depot)).stored=LandfillBuilding.Capacity/2;
        Check(c.MapIndicator(depot,CityMapView.Garbage)==.5f,"Landfill map shows real storage rather than a generic facility penalty");
        Check(c.MapIndicator(shop,CityMapView.Employment)==c.EmployedAt(shop)/(float)c.JobCapacity(shop),"Employment map is occupied Job entities over actual Job entities");
        foreach(var factory in c.buildings.OfType<IndustrialBuilding>()){factory.factory.noise=0;factory.factory.pollution=0;}
        Check(c.MapIndicator(home,CityMapView.Noise)==0 && c.MapIndicator(home,CityMapView.Pollution)==0,"Inactive industrial buildings do not fabricate fixed environment penalties");
        var source=c.buildings.OfType<IndustrialBuilding>().First();source.factory.noise=.6f;source.factory.pollution=.7f;
        Check(Math.Abs(c.MapIndicator(source.id,CityMapView.Noise)-.6f)<.0001f && c.MapIndicator(source.id,CityMapView.Pollution)==.7f,"Environment map reads production activity at any building position");
        c.residualIndustry.Add(new IndustrialExposure{x=c.GetBuilding(home).x,z=c.GetBuilding(home).z,pollution=.5f});
        Check(c.MapIndicator(home,CityMapView.Pollution)>=.5f,"Residual pollution is represented even after the emitting factory is gone");
        string before=TestCity.Snapshot(c);foreach(var b in c.buildings)foreach(CityMapView view in Enum.GetValues(typeof(CityMapView)))c.MapIndicator(b.id,view);
        Check(TestCity.Snapshot(c)==before,"Reading every map indicator does not alter population, inventory or simulation state");
        Console.WriteLine("PASS: "+checks+" entity-driven map indicator checks");
    }
}
