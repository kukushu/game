using System;
using System.Linq;
using HarborCity;

public static class UtilitySupplyChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Utility supply: "+why);checks++;}
    public static void ConnectFixture(CityModel city)
    {
        if(!city.CommitRoad(city.roads.Plan(city,city.roads.Node(CityRoads.Entrance),TestCity.P(-68,1.5f),TestCity.Flat)))throw new Exception("Drain access road");
        var drain=city.RoadsidePreview(-64,-5,LandUse.Sewage,HousingKind.Villa,out string error);
        if(city.PlaceBuilding(drain,LandUse.Sewage,(x,z)=>x<-78?0:1,out error)<0)throw new Exception(error);
        if(!city.BuildUtility(UtilityKind.Water,TestCity.P(-64,0),TestCity.P(38,0),out error)
            || !city.BuildUtility(UtilityKind.Electricity,TestCity.P(-64,0),TestCity.P(38,0),out error))throw new Exception(error);
    }
    public static void Run()
    {
        var c=CityModel.Create();c.SetEntranceHeight(TestCity.Flat);
        Check(c.CommitRoad(c.roads.Plan(c,c.roads.Node(CityRoads.Entrance),TestCity.P(38,1.5f),TestCity.Flat)),"Road fixture");
        int power=TestCity.Build(c,-47,-5,LandUse.Power),water=TestCity.Build(c,-40,-5,LandUse.Water);
        int home=TestCity.Build(c,28,6,LandUse.Residential,HousingKind.Villa);
        Check(c.Supply(water).electricity && !c.Supply(home).electricity,"Short-distance building transfer does not provide global electricity");
        Check(!c.Supply(home).water,"A globally present tower cannot serve a disconnected home");
        ConnectFixture(c);
        Check(c.Supply(home).Complete,"Real power line and shared water/sewage pipe serve home");
        Check(c.power==160 && c.water==160,"Provider capacity counted once");
        var saved=TestCity.RoundTrip(c);Check(saved.Supply(home).Complete && saved.utilities.water.edges.Count>0,"Networks persist and supply is derived after load");
        int edge=c.utilities.water.edges[0].id;c.RemoveUtility(UtilityKind.Water,edge);
        Check(!c.Supply(home).water && c.Supply(home).electricity,"Deleting pipe disconnects water independently of electricity");
        Check(!c.BuildUtility(UtilityKind.Water,TestCity.P(90,0),TestCity.P(0,0),out _) && c.Valid(),"Out-of-map utility rejected without corrupting city");
        var inland=c.RoadsidePreview(0,-5,LandUse.Sewage,HousingKind.Villa,out string error);
        Check(c.PlaceBuilding(inland,LandUse.Sewage,TestCity.Flat,out error)<0 && error.Contains("水岸"),"Base-game outfall cannot be an inland sewage plant");
        // Supply and drain in separate islands must not be merged through a coverage overlap.
        c=saved;c.utilities.water=new CityUtilityNetwork();
        c.BuildUtility(UtilityKind.Water,TestCity.P(-40,0),TestCity.P(32,0),out _);
        c.BuildUtility(UtilityKind.Water,TestCity.P(-64,0),TestCity.P(-50,0),out _);
        Check(c.Supply(home).water && !c.Supply(home).sewage,"Water source without drain in same network cannot provide sewage");
        c=TestCity.RoundTrip(saved); // restored save is independent of the modified network
        c.utilities.electricity=new CityUtilityNetwork();c.Recalculate();
        Check(!c.Supply(home).electricity && c.Supply(home).water && !c.Supply(home).Complete,"Remote electricity loss prevents operation even with fresh water");
        var broken=TestCity.RoundTrip(c);broken.utilities.water=null;Check(!broken.Valid(),"Missing saved network rejected");
        c=TestCity.Create();c.development.enabled=true;ConnectFixture(c);
        var factory=c.buildings.OfType<IndustrialBuilding>().First();
        var person=c.Citizens.First(p=>c.Workplace(p)==factory.id);c.ReleaseJob(person);
        var job=c.society.jobEntities.First(j=>j.buildingId==factory.id && j.occupiedCitizenId<0);
        c.AssignJob(person,job.id);person.atWork=true;person.location=factory.id;person.tripId=0;person.arrivedDay=c.day;
        factory.factory.raw=2;c.ProcessFactoryWork(person,60);
        Check(factory.factory.produced==1,"Connected factory can process actual on-site labour and raw materials");
        c.utilities.water=new CityUtilityNetwork();c.Recalculate();c.ProcessFactoryWork(person,60);
        Check(factory.factory.produced==1 && factory.factory.raw==1,"Disconnected factory cannot turn further labour into goods");
        CheckOverload();
        Console.WriteLine("PASS: "+checks+" utility coverage/capacity/connection/save checks");
    }
    static int Load(CityModel c,CityBuilding b)=>b is ResidentialBuilding?Math.Max(1,c.society.families.Where(h=>h.resident && h.home==b.id).Sum(h=>c.LivingMembers(h))):
        b is CommercialBuilding || b is IndustrialBuilding?Math.Max(1,c.EmployedAt(b.id)*2):b is PowerBuilding?0:2;
    static void CheckOverload()
    {
        var c=TestCity.Create();c.development.enabled=true;ConnectFixture(c);
        var families=c.society.families.ToArray();int added=0;
        while(c.demand<161)
        {
            var h=families[added++%families.Length];h.people.Add(new CityResident{id=c.society.nextCitizenId++,householdId=h.id,name="供给测试居民",age=10,location=h.home});
            c.Recalculate();
        }
        Check(c.demand==161 && c.power==160,"Fixture has only one unit of electric overload");
        Check(c.buildings.Where(b=>b is WaterBuilding || b is SewageBuilding).All(b=>c.Supply(b.id).electricity) && c.water==160 && c.SewageCapacity==160,"Small shortfall keeps actual water providers powered and running");
        int off=c.buildings.Count(b=>!c.Supply(b.id).electricity);
        Check(off>0 && off<c.buildings.Count/2,"Small deficit sheds a subset rather than the whole network");
        Check(c.buildings.Where(b=>c.Supply(b.id).electricity).Sum(b=>Load(c,b))<=c.power,"Allocated real electric loads never exceed capacity");
        string supplied=string.Join(",",c.buildings.Where(b=>c.Supply(b.id).electricity).Select(b=>b.id).OrderBy(id=>id));
        c.Recalculate();Check(supplied==string.Join(",",c.buildings.Where(b=>c.Supply(b.id).electricity).Select(b=>b.id).OrderBy(id=>id)),"Unchanged network has stable load shedding");
        c=TestCity.RoundTrip(c);Check(c.Valid() && supplied==string.Join(",",c.buildings.Where(b=>c.Supply(b.id).electricity).Select(b=>b.id).OrderBy(id=>id)),"Allocation is derived identically from actual saved entities");
        c.SetServiceBudget(CityServiceKind.Water,50);
        var consumers=c.buildings.Where(b=>CityZoningState.ZoneUse(b.Use)).ToArray();
        Check(consumers.Any(b=>c.Supply(b.id).water) && consumers.Any(b=>!c.Supply(b.id).water),"Water shortfall only denies some consumers");
        Check(consumers.Where(b=>c.Supply(b.id).water).Sum(b=>Load(c,b))<=80 && consumers.Where(b=>c.Supply(b.id).sewage).Sum(b=>Load(c,b))<=80,"Water and sewage allocations respect real component budgets");
        Check(consumers.All(b=>!c.Supply(b.id).sewage || c.Supply(b.id).water),"Drain service requires water from the same pipe component");
        c.SetServiceBudget(CityServiceKind.Water,100);c.SetServiceBudget(CityServiceKind.Electricity,150);
        Check(c.buildings.All(b=>c.Supply(b.id).Complete),"Restoring enough capacity restores every connected consumer");
        c.buildings.RemoveAll(b=>b is PowerBuilding);c.Recalculate();
        Check(c.power==0 && c.buildings.All(b=>!c.Supply(b.id).electricity) && c.water==0 && c.SewageCapacity==0,"Actual loss of all power generation still shuts down water facilities");
    }
}
