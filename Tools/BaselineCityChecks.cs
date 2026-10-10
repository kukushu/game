using System;
using System.Linq;
using HarborCity;

public static class BaselineCityChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Baseline city: "+why);checks++;}
    public static void Run()
    {
        var c=CityModel.Create();c.SetEntranceHeight(TestCity.Flat);c.developmentHeight=TestCity.Flat;
        Check(c.CommitRoad(c.roads.Plan(c,c.roads.Node(CityRoads.Entrance),TestCity.P(38,1.5f),TestCity.Flat)),"Empty city starts with a paid outside road");
        var land=c.ZoningCells(TestCity.Flat);
        c.PaintZones(land.Where(x=>x.pose.x<-23),LandUse.Residential);c.PaintZones(land.Where(x=>x.pose.x>=-23 && x.pose.x<0),LandUse.Commercial);c.PaintZones(land.Where(x=>x.pose.x>=0),LandUse.Industrial);
        TestCity.Build(c,-47,-5,LandUse.Power);TestCity.Build(c,-40,-5,LandUse.Water);UtilitySupplyChecks.ConnectFixture(c);TestCity.Build(c,-33,-7,LandUse.Landfill);
        var sim=new CityTraffic(c);sim.Advance(600);int openingPopulation=c.population;
        Check(openingPopulation>0 && c.Employed>0 && c.traffic.consumedGoods>0 && c.LandfillWaste>0 && c.Valid(),"Zoning, real people, jobs, shopping and garbage operate together");
        sim.Advance(2401);
        Check(c.population>0 && c.Employed>0 && c.WasteBalanceError==0 && c.Valid(),"Basic city survives 25 simulated days without phantom waste or broken relations");
        Check(c.society.history.All(d=>d.closingTreasury==d.openingTreasury+d.tax-d.maintenance) && c.society.history.Count>=25,"Long normal-ruleset city accounts close each day; recorded="+c.society.history.Count+", mismatches="+c.society.history.Count(d=>d.closingTreasury!=d.openingTreasury+d.tax-d.maintenance));
        Check(c.CommitRoad(c.roads.Plan(c,TestCity.P(38,1.5f),TestCity.P(38,25),TestCity.Flat)) && c.CommitRoad(c.roads.Plan(c,TestCity.P(38,25),TestCity.P(-48,25),TestCity.Flat)),"A running city can extend its real street network");
        land=c.ZoningCells(TestCity.Flat);int painted=c.PaintZones(land.Where(x=>x.available && x.pose.z>17),LandUse.Residential);
        Check(painted>0,"New streets provide unoccupied planning land without replacing old entities");
        c.SetServiceBudget(CityServiceKind.Electricity,150);c.SetServiceBudget(CityServiceKind.Water,150);
        Check(c.BuildUtility(UtilityKind.Water,TestCity.P(38,0),TestCity.P(38,25),out _) && c.BuildUtility(UtilityKind.Water,TestCity.P(38,25),TestCity.P(-48,25),out _)
            && c.BuildUtility(UtilityKind.Electricity,TestCity.P(38,0),TestCity.P(38,25),out _) && c.BuildUtility(UtilityKind.Electricity,TestCity.P(38,25),TestCity.P(-48,25),out _),"Expansion must receive actual utilities through extended networks");
        sim.Advance(600);
        Check(c.population>openingPopulation && c.buildings.Any(b=>b.Use==LandUse.Residential && b.z>17),"Served new frontage grows real occupied houses and increases population");
        var restored=TestCity.RoundTrip(c);restored.developmentHeight=TestCity.Flat;new CityTraffic(restored).Advance(240);
        Check(restored.population>0 && restored.WasteBalanceError==0 && restored.Valid(),"Expanded city continues real activity after save and restart");
        var fast=TestCity.Empty();var stepped=TestCity.Empty();fast.society.settings.applicantsPerDay=stepped.society.settings.applicantsPerDay=0;
        new CityTraffic(fast).Advance(2400);var fine=new CityTraffic(stepped);for(int i=0;i<48000;i++)fine.Advance(.05f);
        Check(fast.day==stepped.day && fast.traffic.clock==stepped.traffic.clock && fast.society.dayElapsed==stepped.society.dayElapsed,"A long fast-forward processes the same fixed simulation steps as granular updates");
        Console.WriteLine("PASS: "+checks+" normal city/long simulation/expansion/restart checks; population="+restored.population+", jobs="+restored.jobs+", consumed="+restored.traffic.consumedGoods);
    }
}
