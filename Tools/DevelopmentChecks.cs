using System;
using System.Linq;
using HarborCity;

public static class DevelopmentChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Development: "+why);checks++;}
    static CityModel Planned()
    {
        var c=CityModel.Create();c.SetEntranceHeight(TestCity.Flat);c.developmentHeight=TestCity.Flat;
        Check(c.CommitRoad(c.roads.Plan(c,c.roads.Node(CityRoads.Entrance),TestCity.P(38,1.5f),TestCity.Flat)),"Blank-map outside road");
        var cells=c.ZoningCells(TestCity.Flat);
        c.PaintZones(cells.Where(x=>x.pose.x<-23),LandUse.Residential);
        c.PaintZones(cells.Where(x=>x.pose.x>=-23 && x.pose.x<0),LandUse.Commercial);
        c.PaintZones(cells.Where(x=>x.pose.x>=0),LandUse.Industrial);
        return c;
    }
    public static void Run()
    {
        var c=Planned();var sim=new CityTraffic(c);int money=c.money;
        sim.Advance(7);Check(c.development.projects.Count==3 && c.buildings.Count==0,"Zoning construction can start without supply, as in the CS1 manual");
        c=Planned();sim=new CityTraffic(c);
        TestCity.Build(c,-47,-5,LandUse.Power);TestCity.Build(c,-40,-5,LandUse.Water);UtilitySupplyChecks.ConnectFixture(c);money=c.money;
        sim.Advance(7);Check(c.development.projects.Count==3 && c.buildings.Count==3 && c.money==money,"Zoned R/C/I begin real delayed construction without government building fees");
        var forgedCrime=TestCity.RoundTrip(c);forgedCrime.development.projects[0].building.pose.crime=30;
        Check(!forgedCrime.Valid(),"Unfinished construction cannot load fabricated crime before any real building activity");
        var copy=TestCity.RoundTrip(c);copy.developmentHeight=TestCity.Flat;
        Check(copy.Valid() && copy.development.enabled && copy.development.projects.Count==3 && copy.development.projects.Select(p=>p.id).SequenceEqual(c.development.projects.Select(p=>p.id)),"In-progress construction and ruleset survive save");
        var damaged=TestCity.RoundTrip(c);damaged.development.projects[0].building.pose.entranceX=float.NaN;
        Check(!damaged.Valid(),"Nonfinite in-progress entrance cannot poison future buildings");
        damaged=TestCity.RoundTrip(c);damaged.development.projects.First(p=>p.building.type==LandUse.Industrial).building.industrial[0].cash=double.NaN;
        Check(!damaged.Valid(),"Malformed pending factory finance rejected before completion");
        Check(c.ZoningCells(TestCity.Flat).Where(x=>c.development.projects.Any(p=>p.cells.Contains(x.key))).All(x=>!x.available),"Construction reserves real land before building completion");
        var pending=c.development.projects[0].building.ToBuilding();
        Check(!c.CanBuild(pending,TestCity.Flat,out _),"Public or developer construction cannot occupy an active private project");
        var resumed=new CityTraffic(copy);resumed.Advance(20);
        Check(copy.buildings.Any(b=>b.Use==LandUse.Residential) && copy.buildings.Any(b=>b.Use==LandUse.Commercial) && copy.buildings.Any(b=>b.Use==LandUse.Industrial) && copy.money==money,"Construction completes into independent buildings without treasury construction or factory capital cost");
        Check(copy.buildings.OfType<ResidentialBuilding>().All(b=>b.askingRent==0 && b.housingUnits==1) && copy.jobs>0 && copy.population==0,"New low-density housing and concrete jobs exist before migration, without invented population");
        resumed.Advance(600);
        Check(copy.population==copy.Citizens.Count() && copy.population>0 && copy.Employed>0 && copy.jobs==copy.society.jobEntities.Count,"Real families migrate and individuals fill real jobs");
        Check(copy.Citizens.Any(p=>p.lastCommute>=0) && copy.traffic.completed>0,"Employed residents actually travel on roads");
        Check(copy.society.history.Any(d=>d.tax>0) && copy.society.history.All(d=>d.rent==0 && d.closingTreasury==d.openingTreasury+d.tax-d.maintenance),"City income is taxes, not household rent");
        Check(copy.society.history.All(d=>d.closingSavings==d.openingSavings+d.wages-d.rent-d.living-d.travel-d.movingCosts),"Migration and actual wage ledger conserves household savings");
        Check(copy.Valid() && TestCity.RoundTrip(copy).Valid(),"Developed city saves after actual traffic and payroll");
        var factory=copy.buildings.OfType<IndustrialBuilding>().First();factory.factory.cash=0;
        Check(copy.society.jobEntities.Where(j=>j.buildingId==factory.id).All(copy.JobFunded),"Prototype cash constraint is disabled in baseline ruleset");
        Check(copy.upkeep==copy.roads.edges.Sum(e=>(int)Math.Ceiling(copy.roads.EdgeLength(e.a,e.b)/3))+228,"Private homes do not charge government maintenance");
        c=Planned();TestCity.Build(c,-47,-5,LandUse.Power);TestCity.Build(c,-40,-5,LandUse.Water);sim=new CityTraffic(c);sim.Advance(7);
        var project=c.development.projects.First(p=>p.building.type==LandUse.Residential);
        c.PaintZones(c.ZoningCells(TestCity.Flat).Where(x=>project.cells.Contains(x.key)),LandUse.Empty);sim.Advance(13);
        Check(!c.buildings.OfType<ResidentialBuilding>().Any() && c.development.projects.All(p=>p.id!=project.id),"Dezoning cancels unfinished project without spawning a phantom building");
        c=CityModel.Create();c.SetEntranceHeight(TestCity.Flat);c.developmentHeight=TestCity.Flat;
        c.CommitRoad(c.roads.Plan(c,c.roads.Node(CityRoads.Entrance),TestCity.P(20,1.5f),TestCity.Flat));
        var narrow=c.ZoningCells(TestCity.Flat).First(cell=>cell.row==0 && cell.pose.x>-30 && cell.pose.x<-20);
        c.PaintZones(new[]{narrow},LandUse.Residential);sim=new CityTraffic(c);sim.Advance(7);
        Check(c.development.projects.Count==1 && c.development.projects[0].cells.Count==1,"A single roadside zoned cell can reserve a fitting real construction project");
        copy=TestCity.RoundTrip(c);copy.developmentHeight=TestCity.Flat;new CityTraffic(copy).Advance(13);
        var tiny=copy.buildings.OfType<ResidentialBuilding>().Single();
        Check(tiny.width==1.4f && tiny.depth==1.4f && copy.Valid() && copy.population==0,"Small building grows with an independent ID and footprint, without invented residents");
        Check(copy.ZoningCells(TestCity.Flat).Count(cell=>cell.occupiedBuilding==tiny.id)==1,"Tiny entity masks only its actual planning cell");
        c=Planned();sim=new CityTraffic(c);sim.Advance(7);
        Check(c.development.projects.Select(p=>p.cells.Count).Distinct().Count()>1,"Placeholder footprint catalogue produces different-sized real lots");
        damaged=TestCity.RoundTrip(c);damaged.development.projects[0].cells.RemoveAt(0);
        Check(!damaged.Valid(),"Pending footprint must match its exact reserved cell count");
        damaged=TestCity.RoundTrip(c);damaged.development.projects[0].building.pose.width=3.14f;
        Check(!damaged.Valid(),"Non-grid growth dimensions rejected before completion");
        Console.WriteLine("PASS: "+checks+" zoning growth/migration/tax/save checks");
    }
}
