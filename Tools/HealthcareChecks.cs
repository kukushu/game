using System;
using System.Linq;
using HarborCity;

public static class HealthcareChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Healthcare: "+why);checks++;}
    static CityModel Create(out CityTraffic sim,out CityResident person,out int clinic)
    {
        var c=TestCity.Create();c.development.enabled=true;UtilitySupplyChecks.ConnectFixture(c);c.waste.enabled=false;c.society.settings.applicantsPerDay=0;
        person=c.Citizens.First(p=>c.ResidentJob(p)!=null);int id=person.id;
        foreach(var p in c.Citizens.Where(p=>p.id!=id))c.ReleaseJob(p);
        clinic=TestCity.Build(c,40,-5,LandUse.Clinic);person.health=30;person.sick=true;c.Recalculate();sim=new CityTraffic(c);return c;
    }
    static void Until(CityTraffic sim,Func<bool> done,int steps=6000){for(int n=0;n<steps && !done();n++)sim.Advance(.05f);}
    public static void Run()
    {
        var c=Create(out var sim,out var p,out int clinic);var h=c.society.families.First(f=>f.id==p.householdId);int job=p.jobId;
        Check(c.Valid() && c.HasBasicServices(clinic),"Clinic is an independent public entity connected to actual services");
        var trip=sim.DispatchHealthcare(p.id);
        Check(trip!=null && trip.medicalPickup && !trip.walking && c.ClinicReservations(clinic)==1 && c.ClinicPatients(clinic)==0,"Dispatch reserves a place without inventing arrival or healing");
        var observed=c.ObserveResident(h,p);Check(observed.building==h.home && !observed.travelling && observed.destination==clinic,"Resident remains visibly at home while ambulance is coming");
        Check(sim.DispatchHealthcare(p.id)==null && p.health==30 && p.jobId==job,"No duplicate pickup, instantaneous healing or released job");
        Check(!c.DemolishBuilding(clinic),"Pending patient prevents unsafe clinic removal until displacement support exists");
        Until(sim,()=>!trip.medicalPickup);
        Check(!trip.medicalPickup && trip.destination==clinic && p.medicalStage==MedicalStage.Assigned && c.ObserveResident(h,p).travelling,"Actual pickup begins the loaded journey to the clinic");
        var saved=TestCity.RoundTrip(c);var savedPerson=saved.Citizens.First(r=>r.id==p.id);
        Check(savedPerson.tripId==trip.id && saved.traffic.trips.Single(t=>t.id==trip.id).clinicId==clinic && saved.Valid(),"Loaded ambulance and patient reservation survive saves");
        Until(sim,()=>p.medicalStage==MedicalStage.Treatment);
        Check(p.location==clinic && p.tripId==0 && c.ClinicPatients(clinic)==1 && c.ClinicAmbulances(clinic)==0 && !p.atWork,"Only physical admission occupies treatment and releases the delivered ambulance");
        Check(c.ObserveResident(h,p).building==clinic && c.ObserveResident(h,p).destination==clinic && c.ObserveResident(h,p).state=="诊所治疗中","Member observation uses the real treatment location and destination");
        var pipes=c.utilities.water;c.utilities.water=new CityUtilityNetwork();c.Recalculate();float health=p.health,minutes=p.treatmentMinutes;
        sim.Advance(1);Check(p.health==health && p.treatmentMinutes==minutes,"Disconnected clinic cannot produce treatment");
        c.utilities.water=pipes;c.Recalculate();saved=TestCity.RoundTrip(c);savedPerson=saved.Citizens.First(r=>r.id==p.id);
        Check(savedPerson.medicalStage==MedicalStage.Treatment && savedPerson.location==clinic && savedPerson.treatmentMinutes==minutes,"Actual patient and treatment progress survive saves");
        Until(sim,()=>p.medicalStage==MedicalStage.Returning);
        Check(!p.sick && p.health>=80 && c.ClinicPatients(clinic)==0 && p.location==clinic && p.jobId==job,"Real treatment restores health without teleporting home or altering employment");
        Until(sim,()=>p.medicalStage==MedicalStage.None);
        Check(p.medicalStage==MedicalStage.None && p.location==h.home && p.medicalClinicId==-1 && c.Valid(),"Patient walks home and releases medical associations after actual arrival");
        c=Create(out sim,out p,out clinic);trip=sim.DispatchHealthcare(p.id);sim.Advance(1);
        var second=c.Citizens.First(r=>r.id!=p.id);second.sick=true;second.health=30;var ambulance2=sim.DispatchHealthcare(second.id);
        Check(ambulance2!=null && !ambulance2.walking && c.ClinicAmbulances(clinic)==2,"Distinct patients can occupy the finite two-ambulance fleet");
        var child=c.Citizens.First(r=>r.age<18);child.sick=true;child.health=30;var walking=sim.DispatchHealthcare(child.id);
        Check(walking!=null && walking.walking && !walking.medicalPickup && c.ClinicAmbulances(clinic)==2,"No available ambulance produces real walking, including children, not a third car");
        foreach(var resident in c.Citizens.Where(r=>r.medicalStage==MedicalStage.None)){resident.sick=true;resident.health=30;sim.DispatchHealthcare(resident.id);}
        Check(c.ClinicReservations(clinic)==ClinicBuilding.PatientCapacity && c.Citizens.Any(r=>r.sick && r.medicalStage==MedicalStage.None) && c.Valid(),"Finite real reservations reject additional patients when clinic is full");
        int expense=c.upkeep,funds=c.money;c.SetServiceBudget(CityServiceKind.Healthcare,50);
        Check(c.ClinicCapacity==4 && c.ClinicFleetLimit==1 && c.ClinicReservations(clinic)==8 && c.ClinicAmbulances(clinic)==2 && c.upkeep==expense-40 && c.money==funds && c.Valid(),"Medical budget reduces new allowances and maintenance without deleting existing patients or ambulances");
        var waiting=c.Citizens.First(r=>r.sick && r.medicalStage==MedicalStage.None);
        Check(sim.DispatchHealthcare(waiting.id)==null,"Budget cut does not add a ninth patient to a now smaller allowance");
        c.SetServiceBudget(CityServiceKind.Healthcare,150);saved=TestCity.RoundTrip(c);
        Check(saved.ClinicCapacity==12 && saved.ClinicFleetLimit==3 && saved.development.healthcareBudget==150 && saved.Valid(),"Expanded medical resources and budget survive saves");
        saved.development.healthcareBudget=0;Check(!saved.Valid(),"Malformed medical budget is rejected before loading");
        c=Create(out sim,out p,out clinic);trip=sim.DispatchHealthcare(p.id);Until(sim,()=>!trip.medicalPickup);var roads=c.roads.Copy();int node=trip.Current;
        c.roads.edges.Clear();c.roads.Changed();sim.Advance(121);
        Check(c.traffic.trips.Contains(trip) && p.tripId==trip.id && trip.Current==node && p.medicalStage==MedicalStage.Assigned,"Loaded ambulance waits through a long road interruption without losing its resident");
        c.roads=roads;c.Recalculate();sim=new CityTraffic(c);Until(sim,()=>p.medicalStage==MedicalStage.None);
        Check(!p.sick && p.medicalStage==MedicalStage.None && c.Valid(),"Restored roads allow the same real patient to finish care and return");
        c=Create(out sim,out p,out clinic);trip=sim.DispatchHealthcare(p.id);h=c.society.families.First(f=>f.id==p.householdId);
        Check(c.DemolishBuilding(h.home) && p.medicalStage==MedicalStage.None && p.medicalClinicId==-1 && !c.traffic.trips.Contains(trip) && c.Valid(),"Removed household releases medical reservation and task consistently");
        c=Create(out sim,out p,out clinic);p.medicalStage=MedicalStage.Treatment;p.medicalClinicId=clinic;p.location=clinic;p.treatmentMinutes=float.NaN;
        Check(!c.Valid(),"Invalid treatment progress cannot enter a save");
        c=Create(out sim,out p,out clinic);h=c.society.families.First(f=>f.id==p.householdId);c.money+=CityModel.Cost(LandUse.Clinic);int nearer=TestCity.Build(c,-30,-5,LandUse.Clinic);
        Check(c.CommuteMinutes(h.home,clinic)<0 && c.TravelMinutes(h.home,nearer)>=0 && c.TravelMinutes(h.home,nearer)<c.TravelMinutes(h.home,clinic),"Public services use generic route estimates without pretending to provide Job entities");
        trip=sim.DispatchHealthcare(p.id);Check(trip!=null && trip.clinicId==nearer,"Medical selection actually prefers the nearer reachable clinic instead of the lowest building ID");
        Console.WriteLine("PASS: "+checks+" clinic/patient/ambulance/walking/treatment/restart checks");
    }
}
