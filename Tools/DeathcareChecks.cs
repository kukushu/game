using System;
using System.Linq;
using HarborCity;

public static class DeathcareChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Deathcare: "+why);checks++;}
    static CityModel Create(out CityTraffic sim,out int cemetery)
    {
        var c=TestCity.Create();c.development.enabled=true;UtilitySupplyChecks.ConnectFixture(c);c.waste.enabled=false;c.fire.chancePerBuildingDay=0;c.society.settings.applicantsPerDay=0;c.money+=100000;
        cemetery=TestCity.Build(c,40,-5,LandUse.Cemetery);sim=new CityTraffic(c);return c;
    }
    static void Until(CityTraffic sim,Func<bool> done){for(int n=0;n<6000 && !done();n++)sim.Advance(.05f);}
    public static void Run()
    {
        var c=Create(out var sim,out int cemetery);var p=c.Citizens.First(r=>c.ResidentJob(r)!=null);var h=c.society.families.First(f=>f.id==p.householdId);int citizen=p.id,job=p.jobId,work=c.Workplace(p),population=c.population;
        p.atWork=true;p.location=work;p.accessNode=c.AccessBuilding(work).First();p.earnedWages=10;
        Check(c.DieResident(citizen) && p.dead && !p.canWork && p.jobId<0 && c.Job(job).occupiedCitizenId<0 && c.population==population-1 && c.Citizens.All(r=>r.id!=citizen),"Concrete worker death releases one actual job and removes one living resident, retaining history");
        var body=c.Corpse(citizen);Check(body.buildingId==work && body.stage==CorpseStage.Waiting && c.ObserveCorpse(p).building==work && c.Valid(),"Dead worker remains at the actual workplace as a saveable distinct corpse");
        Check(!c.DieResident(citizen) && c.deathcare.bodies.Count==1,"Repeated death cannot create a duplicate corpse or remove population twice");
        c.Tick();Check(h.wagePaid==10 && p.earnedWages==0 && c.Valid(),"Already earned wages are paid while no new labour comes from the dead worker");
        var trip=sim.DispatchHearse(cemetery,citizen);
        Check(trip!=null && trip.corpseId==citizen && trip.residentId==0 && trip.cargo==0 && body.stage==CorpseStage.Assigned && c.BodiesAt(work)==1 && c.BuriedAt(cemetery)==0 && c.CemeteryReserved(cemetery)==1,"Dispatch reserves finite burial space while the corpse remains at its real origin");
        Check(sim.DispatchHearse(cemetery,citizen)==null && !c.DemolishBuilding(work),"A body cannot be assigned twice or erased by bulldozing its origin");
        c=TestCity.RoundTrip(c);body=c.Corpse(citizen);p=c.AllResidents.First(r=>r.id==citizen);sim=new CityTraffic(c);
        Until(sim,()=>body.stage==CorpseStage.InTransit);
        Check(body.stage==CorpseStage.InTransit && c.BodiesAt(work)==0 && c.BuriedAt(cemetery)==0 && c.ObserveCorpse(p).travelling && c.Valid(),"Only actual hearse arrival picks up the corpse; transit is not burial");
        var supplied=TestCity.RoundTrip(c).utilities.electricity;
        c.utilities.electricity=new CityUtilityNetwork();c.Recalculate();sim.Advance(121);
        Check(body.stage==CorpseStage.InTransit && c.HearsesAt(cemetery)==1 && c.BuriedAt(cemetery)==0 && c.Valid(),"An unpowered cemetery cannot bury a loaded corpse or discard the waiting hearse");
        c=TestCity.RoundTrip(c);body=c.Corpse(citizen);c.utilities.electricity=supplied;c.Recalculate();sim=new CityTraffic(c);
        Check(body.stage==CorpseStage.InTransit && c.HasBasicServices(cemetery) && c.Valid(),"Saved power-outage wait retains the specific corpse and restores service without remote burial");
        var roads=c.roads.Copy();c.roads.edges.Clear();c.roads.Changed();sim.Advance(121);
        Check(body.stage==CorpseStage.InTransit && c.traffic.trips.Any(t=>t.id==body.tripId) && c.Valid(),"Long road outage retains the specific loaded hearse instead of losing its corpse");
        c=TestCity.RoundTrip(c);c.roads=roads;body=c.Corpse(citizen);sim=new CityTraffic(c);Until(sim,()=>body.stage==CorpseStage.Buried);
        Check(body.stage==CorpseStage.Buried && body.tripId==0 && c.HearsesAt(cemetery)==0 && c.BuriedAt(cemetery)==1 && c.Valid(),"Saved loaded hearse resumes, actually arrives, buries one person and releases its vehicle");
        Check(!c.DemolishBuilding(cemetery),"Stored bodies cannot be silently erased by deleting the cemetery before transfer exists");
        var saved=TestCity.RoundTrip(c);Check(saved.AllResidents.First(r=>r.id==citizen).dead && saved.Corpse(citizen).stage==CorpseStage.Buried && saved.population==saved.Citizens.Count(),"Buried historical identity and living-only population survive saves");
        saved.deathcare.bodies.Add(saved.Corpse(citizen));Check(!saved.Valid(),"Duplicate corpse identity is rejected");
        saved=TestCity.RoundTrip(c);saved.AllResidents.First(r=>r.id==citizen).canWork=true;Check(!saved.Valid(),"A dead person cannot be a living worker");
        c=Create(out sim,out cemetery);p=c.Citizens.First();h=c.society.families.First(f=>f.id==p.householdId);
        // Controlled capacity fixture; no claim these synthetic corpses were natural deaths.
        for(int n=0;n<CemeteryBuilding.Capacity;n++)
        {int id=c.AllocateResidentId();h.people.Add(new CityResident{id=id,householdId=h.id,name="容量边界",age=80,dead=true,health=0,location=cemetery});c.deathcare.bodies.Add(new CityCorpse{citizenId=id,cemeteryId=cemetery,stage=CorpseStage.Buried,diedDay=c.day,x=40,z=-5});}
        Check(c.Valid() && c.CemeteryReserved(cemetery)==CemeteryBuilding.Capacity,"Finite capacity counts actual corpse records, not an independent inventory number");
        c.DieResident(p.id);Check(sim.DispatchHearse(cemetery,p.id)==null && c.Corpse(p.id).stage==CorpseStage.Waiting,"Full cemetery cannot reserve or pick up another body");
        c=Create(out sim,out cemetery);p=c.Citizens.First();h=c.society.families.First(f=>f.id==p.householdId);int home=h.home;c.DieResident(p.id);
        for(int n=0;n<6;n++)c.Tick();Check(c.GetBuilding(home).abandoned && c.BodiesAt(home)==1 && c.Valid(),"Actual persistent uncollected body eventually causes building abandonment without erasing the corpse");
        Check(c.BuildingConditionLabel(home).Contains("已废弃") && c.BuildingConditionLabel(home).Contains("遗体") && h.reason.Contains("遗体"),"Abandonment feedback identifies the actual unresolved corpse cause");
        c.GetBuilding(home).burning=true;Check(c.BuildingConditionLabel(home).StartsWith("火灾中"),"Immediate fire feedback takes precedence over a waiting corpse");
        c=Create(out sim,out cemetery);p=c.Citizens.First();h=c.society.families.First(f=>f.id==p.householdId);home=h.home;c.DieResident(p.id);trip=sim.DispatchHearse(cemetery,p.id);
        roads=c.roads.Copy();c.roads.edges.Clear();c.roads.Changed();for(int n=0;n<6;n++)c.Tick();
        Check(c.GetBuilding(home).abandoned && c.Corpse(p.id).stage==CorpseStage.Assigned && c.traffic.trips.Any(t=>t.id==trip.id) && c.Valid(),"Abandonment during blocked pickup preserves the actual assigned hearse and specific corpse");
        c=TestCity.RoundTrip(c);citizen=p.id;c.roads=roads;sim=new CityTraffic(c);body=c.Corpse(citizen);
        Until(sim,()=>body.stage==CorpseStage.Buried);Check(body.stage==CorpseStage.Buried && c.BuriedAt(cemetery)==1 && c.Valid(),"Saved abandoned-building pickup resumes to actual burial after the road is restored");
        c=Create(out sim,out cemetery);h=c.society.families.First();foreach(var r in h.people.ToArray())c.DieResident(r.id);
        int family=h.id;c.society.settings.applicantsPerDay=1;c.Tick();Check(!h.resident && h.people.All(r=>r.dead) && c.society.families.Any(f=>f.id!=family && f.resident && f.home>=0) && c.Valid(),"An entirely dead family never resurrects when housing accepts new migrants");
        c=Create(out sim,out cemetery);p=c.Citizens.First();p.age=80;c.deathcare.oldAgeRisk=1;c.Tick();Check(p.dead && c.Corpse(p.id)!=null && c.Valid(),"Daily individual age-risk evaluation creates a real corpse and linked death");
        c=Create(out sim,out cemetery);p=c.Citizens.First();trip=sim.Dispatch(c.society.families.First().home,c.Workplace(p),TripPurpose.Commute,p.id);trip.householdId=p.householdId;p.tripId=trip.id;
        Check(!c.DieResident(p.id) && !p.dead,"Unimplemented moving death is explicitly rejected instead of teleporting the person to a building");
        Console.WriteLine("PASS: "+checks+" living residents/real corpses/finite hearses/arrival/capacity/road/save checks");
    }
}
