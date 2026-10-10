using System;
using System.Linq;
using HarborCity;

public static class FireChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Fire: "+why);checks++;}
    static CityModel Create(out CityTraffic sim,out int station,out int target)
    {
        var c=TestCity.Create();c.development.enabled=true;UtilitySupplyChecks.ConnectFixture(c);c.waste.enabled=false;c.fire.chancePerBuildingDay=0;c.society.settings.applicantsPerDay=0;
        foreach(var p in c.Citizens)c.ReleaseJob(p);c.money+=CityModel.Cost(LandUse.FireHouse);
        station=TestCity.Build(c,40,-5,LandUse.FireHouse);target=c.buildings.OfType<ResidentialBuilding>().First().id;c.Recalculate();sim=new CityTraffic(c);return c;
    }
    static void Until(CityTraffic sim,Func<bool> done,int steps=5000){for(int n=0;n<steps && !done();n++)sim.Advance(.05f);}
    public static void Run()
    {
        var c=Create(out var sim,out int station,out int target);var b=c.GetBuilding(target);
        Check(c.Valid() && c.HasBasicServices(station) && c.ActiveFires==0,"Independent served station has no fabricated fires");
        Check(c.IgniteBuilding(target) && !c.IgniteBuilding(target) && c.ActiveFires==1 && b.fireDamage==0,"Ignition creates one real building incident, not damage or duplicates");
        var trip=sim.DispatchFire(station,target);Check(trip!=null && trip.cargo==0 && trip.residentId==0 && c.FireEnginesAt(station)==1,"Fire response uses a finite actual service vehicle without fake freight or citizens");
        Check(sim.DispatchFire(station,target)==null && !c.DemolishBuilding(station),"Duplicate dispatch and unsafe station removal are rejected");
        sim.Advance(.5f);Check(b.burning && b.fireIntensity>1 && b.fireDamage>0 && trip.status!=TripStatus.Visiting,"An engine on the road contributes no extinguishing capacity");
        var saved=TestCity.RoundTrip(c);Check(saved.GetBuilding(target).burning && saved.traffic.trips.Any(t=>t.id==trip.id) && saved.Valid(),"Real fire and dispatched engine survive saves");
        Until(sim,()=>trip.status==TripStatus.Visiting);float intensity=b.fireIntensity;sim.Advance(.5f);
        Check(trip.status==TripStatus.Visiting && b.fireIntensity<intensity && b.burning,"Actual arrival starts suppression and retains the physical engine at the scene");
        saved=TestCity.RoundTrip(c);Check(saved.traffic.trips.Single(t=>t.id==trip.id).status==TripStatus.Visiting && saved.GetBuilding(target).fireIntensity==b.fireIntensity,"On-scene engine and fire progress survive saves");
        Until(sim,()=>!b.burning);Check(!b.burned && b.fireDamage>0 && c.ActiveFires==0,"Actual response extinguishes fire before destruction; accumulated damage is retained");
        Until(sim,()=>!c.traffic.trips.Contains(trip));Check(c.FireEnginesAt(station)==0 && c.Valid(),"Engine actually returns before releasing its fleet slot");
        c=Create(out sim,out station,out target);b=c.GetBuilding(target);c.IgniteBuilding(target);int population=c.population;int familySize=c.society.families.First(h=>h.home==target).people.Count;
        b.fireDamage=99;c.AdvanceFires(20);
        Check(b.burned && b.abandoned && !b.burning && c.population==population-familySize && c.HousingCapacity(target)==0 && c.BurnedBuildings==1 && c.Valid(),"Uncontained fire destroys actual housing and displaces its real family");
        saved=TestCity.RoundTrip(c);Check(saved.GetBuilding(target).burned && saved.GetBuilding(target).fireDamage==100 && saved.Valid(),"Ruins and actual displaced population survive saves");
        for(int n=0;n<28;n++)c.Tick();Check(b.burned && b.abandoned && c.HousingCapacity(target)==0,"Burned buildings do not use ordinary four-week recovery");
        Check(c.DemolishBuilding(target) && c.GetBuilding(target)==null && c.Valid(),"Bulldozing removes the stable ruin entity for redevelopment");
        c=Create(out sim,out station,out target);c.IgniteBuilding(target);trip=sim.DispatchFire(station,target);sim.Advance(1);
        int second=c.buildings.OfType<ResidentialBuilding>().Skip(1).First().id;c.IgniteBuilding(second);var two=sim.DispatchFire(station,second);sim.Advance(1);
        int third=c.buildings.OfType<ResidentialBuilding>().Skip(2).First().id;c.IgniteBuilding(third);
        Check(two!=null && c.FireEnginesAt(station)==2 && sim.DispatchFire(station,third)==null,"Finite fleet can respond to two distinct incidents, not every fire at once");
        int expense=c.upkeep,funds=c.money;c.SetServiceBudget(CityServiceKind.Fire,50);
        Check(c.FireFleetLimit==1 && c.FireEnginesAt(station)==2 && c.upkeep==expense-40 && c.money==funds && c.Valid(),"Budget reduction limits new dispatch without deleting two working engines");
        c.SetServiceBudget(CityServiceKind.Fire,150);saved=TestCity.RoundTrip(c);Check(saved.FireFleetLimit==3 && saved.development.fireBudget==150 && saved.Valid(),"Fire budget and physical fleet survive saves");
        c=Create(out sim,out station,out target);c.IgniteBuilding(target);trip=sim.DispatchFire(station,target);sim.Advance(.5f);int node=trip.Current;var roads=c.roads.Copy();c.roads.edges.Clear();c.roads.Changed();sim.Advance(121);
        Check(c.traffic.trips.Contains(trip) && trip.Current==node && c.GetBuilding(target).burned && trip.returning,"Delayed engine persists at its physical node even when the unserved target burns");
        c.roads=roads;c.Recalculate();sim=new CityTraffic(c);Until(sim,()=>!c.traffic.trips.Contains(trip));Check(c.FireEnginesAt(station)==0 && c.Valid(),"Restored roads allow the existing engine to return safely");
        c=Create(out sim,out station,out target);c.IgniteBuilding(target);trip=sim.DispatchFire(station,target);sim.Advance(.5f);node=trip.Current;
        Check(c.DemolishBuilding(target) && trip.returning && trip.Current==node && c.Valid(),"Demolished target redirects the same engine without teleportation or cargo loss");
        c=Create(out sim,out station,out target);c.fire.chancePerBuildingDay=1;saved=TestCity.RoundTrip(c);c.Tick();saved.Tick();
        Check(c.ActiveFires==10 && c.fire.seed==saved.fire.seed && TestCity.Snapshot(c)==TestCity.Snapshot(saved),"Natural incidents derive from eligible buildings and saved deterministic random state");
        c=Create(out sim,out station,out target);c.GetBuilding(target).fireIntensity=float.NaN;Check(!c.Valid(),"Malformed fire state is rejected before saving");
        c=Create(out sim,out station,out target);var worker=c.Citizens.First(p=>p.canWork);worker.skill=2;
        var job=c.society.jobEntities.First(j=>c.GetBuilding(j.buildingId) is IndustrialBuilding);
        Check(c.AssignJob(worker,job.id),"Real worker occupies a concrete factory job before the incident");
        worker.location=job.buildingId;worker.atWork=true;worker.arrivedDay=c.day;worker.accessNode=c.AccessBuilding(job.buildingId).First();c.society.dayElapsed=50;
        float worked=worker.workedMinutes,wages=worker.earnedWages;var factory=c.Factory(job.buildingId);double produced=factory.produced;
        c.IgniteBuilding(job.buildingId);sim.Advance(.05f);
        Check(worker.jobId==job.id && worker.workedMinutes==worked && worker.earnedWages==wages && factory.produced==produced,"Burning factory retains contracts but pays no fabricated labour or production");
        var other=c.Citizens.First(p=>p.canWork && p.id!=worker.id);other.skill=2;
        var vacancy=c.society.jobEntities.First(j=>j.buildingId==job.buildingId && j.occupiedCitizenId<0);
        Check(!c.AssignJob(other,vacancy.id) && sim.Dispatch(c.society.families.First(h=>h.id==other.householdId).home,job.buildingId,TripPurpose.Commute,other.id)==null,"Active fire rejects new employment and outgoing work commute");
        c.GetBuilding(job.buildingId).fireDamage=99;c.AdvanceFires(20);
        Check(worker.jobId<0 && !c.society.jobEntities.Any(j=>j.buildingId==job.buildingId) && c.Valid(),"Destroyed factory removes concrete jobs and releases actual workers");
        Console.WriteLine("PASS: "+checks+" fire/physical response/ruins/fleet/budget/road/save checks");
    }
}
