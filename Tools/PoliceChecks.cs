using System;
using System.Linq;
using HarborCity;

public static class PoliceChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Police: "+why);checks++;}
    static CityModel Create(out CityTraffic sim,out int station,out int target)
    {
        var c=TestCity.Create();c.development.enabled=true;UtilitySupplyChecks.ConnectFixture(c);c.waste.enabled=false;c.fire.chancePerBuildingDay=0;c.society.settings.applicantsPerDay=0;
        foreach(var p in c.Citizens)c.ReleaseJob(p);c.money+=CityModel.Cost(LandUse.PoliceStation);
        station=TestCity.Build(c,40,-5,LandUse.PoliceStation);target=c.buildings.OfType<ResidentialBuilding>().First().id;c.Recalculate();sim=new CityTraffic(c);return c;
    }
    static void Until(CityTraffic sim,Func<bool> done){for(int n=0;n<6000 && !done();n++)sim.Advance(.05f);}
    public static void Run()
    {
        var c=Create(out var sim,out int station,out int target);var b=c.GetBuilding(target);
        Check(c.Valid() && c.HasBasicServices(station) && c.CrimeBuildings==0 && sim.DispatchPolice(station,target)==null,"Served station cannot invent a crime task");
        b.crime=60;var trip=sim.DispatchPolice(station,target);
        Check(trip!=null && trip.cargo==0 && trip.residentId==0 && c.PoliceCarsAt(station)==1,"Actual finite police car carries neither fabricated cargo nor criminals");
        Check(sim.DispatchPolice(station,target)==null && !c.DemolishBuilding(station),"Existing response blocks duplicate dispatch and premature station removal");
        sim.Advance(.5f);Check(b.crime==60 && trip.status!=TripStatus.Visiting,"A police car on the road does not clear building crime");
        var saved=TestCity.RoundTrip(c);Check(saved.Valid() && saved.GetBuilding(target).crime==60 && saved.traffic.trips.Any(t=>t.id==trip.id),"Real building crime and traveling response survive saving");
        Until(sim,()=>trip.status==TripStatus.Visiting);float crime=b.crime;sim.Advance(.05f);
        Check(b.crime<crime && trip.status==TripStatus.Visiting,"Physical arrival starts actual response");
        saved=TestCity.RoundTrip(c);Check(saved.GetBuilding(target).crime==b.crime && saved.traffic.trips.Single(t=>t.id==trip.id).status==TripStatus.Visiting,"On-scene task and partial response survive saving");
        var pipes=c.utilities.water.Copy();c.utilities.water=new CityUtilityNetwork();c.Recalculate();crime=b.crime;sim.Advance(.05f);
        Check(b.crime==crime && c.traffic.trips.Contains(trip),"Loss of police station supply suspends response without deleting the actual car");
        c.utilities.water=pipes;c.Recalculate();Until(sim,()=>!c.traffic.trips.Contains(trip));
        Check(b.crime==0 && c.PoliceCarsAt(station)==0 && c.Valid(),"Crime clears at the building and the actual car returns before releasing fleet capacity");
        c=Create(out sim,out station,out target);b=c.GetBuilding(target);b.crime=60;trip=sim.DispatchPolice(station,target);sim.Advance(.5f);
        int node=trip.Current;var roads=c.roads.Copy();c.roads.edges.Clear();c.roads.Changed();sim.Advance(121);
        Check(c.traffic.trips.Contains(trip) && trip.Current==node && b.crime>=60,"Long road outage preserves real response at its node, without remote crime reduction");
        c.roads=roads;c.Recalculate();Until(sim,()=>!c.traffic.trips.Contains(trip));Check(c.Valid() && c.PoliceCarsAt(station)==0,"Restored roads let the same response finish and return");
        c=Create(out sim,out station,out target);b=c.GetBuilding(target);b.crime=60;trip=sim.DispatchPolice(station,target);sim.Advance(.5f);
        int second=c.buildings.OfType<ResidentialBuilding>().Skip(1).First().id;c.GetBuilding(second).crime=60;var two=sim.DispatchPolice(station,second);sim.Advance(.5f);
        int third=c.buildings.OfType<ResidentialBuilding>().Skip(2).First().id;c.GetBuilding(third).crime=60;
        Check(two!=null && c.PoliceCarsAt(station)==2 && sim.DispatchPolice(station,third)==null,"Finite fleet cannot respond to every affected building simultaneously");
        int expense=c.upkeep,funds=c.money;c.SetServiceBudget(CityServiceKind.Police,50);
        Check(c.PoliceFleetLimit==1 && c.PoliceCarsAt(station)==2 && c.upkeep==expense-40 && c.money==funds && c.Valid(),"Lower budget preserves deployed cars and changes real maintenance");
        c.SetServiceBudget(CityServiceKind.Police,150);saved=TestCity.RoundTrip(c);Check(saved.PoliceFleetLimit==3 && saved.development.policeBudget==150 && saved.Valid(),"Police budget and deployed fleet round-trip");
        node=trip.Current;Check(c.DemolishBuilding(target) && trip.returning && trip.Current==node && c.Valid(),"Target demolition redirects the actual car from its current location");
        c=Create(out sim,out station,out target);b=c.GetBuilding(target);saved=TestCity.RoundTrip(c);c.happiness=0;saved.happiness=100;c.Tick();saved.Tick();
        Check(b.crime==3 && saved.GetBuilding(target).crime==3 && TestCity.Snapshot(c)==TestCity.Snapshot(saved),"Crime pressure uses actual unemployed adults, not independent happiness cache");
        Check(c.MapIndicator(target,CityMapView.Crime)==.03f && c.AverageCrime>0,"Crime information derives from actual building records");
        var household=c.society.families.First(h=>h.home==target);var adult=household.people.First(p=>p.canWork);adult.skill=2;
        var job=c.society.jobEntities.First(j=>j.occupiedCitizenId<0 && j.requiredSkill<=adult.skill);c.AssignJob(adult,job.id);float before=b.crime;c.Tick();
        Check(b.crime-before<3,"Real employment reduces the household unemployment contribution");
        c=Create(out sim,out station,out target);b=c.GetBuilding(target);b.crime=100;var person=c.society.families.First(h=>h.home==target).people.First();person.health=80;
        saved=TestCity.RoundTrip(c);saved.GetBuilding(target).crime=0;c.UpdateResidentHealth();saved.UpdateResidentHealth();
        Check(person.health<saved.Citizens.First(p=>p.id==person.id).health,"Actual local crime affects a located resident's health pressure");
        b.crime=float.NaN;Check(!c.Valid(),"Malformed crime value is rejected");
        Console.WriteLine("PASS: "+checks+" police/real response/crime/fleet/budget/road/health/save checks");
    }
}
