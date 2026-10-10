using System;
using System.Linq;
using HarborCity;

public static class EducationChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Education: "+why);checks++;}
    static CityModel Create(out CityTraffic sim,out CityResident pupil,out int school)
    {
        var c=TestCity.Create();c.development.enabled=true;UtilitySupplyChecks.ConnectFixture(c);c.waste.enabled=false;c.society.settings.applicantsPerDay=0;
        foreach(var p in c.Citizens)c.ReleaseJob(p);
        school=TestCity.Build(c,40,-5,LandUse.ElementarySchool);pupil=c.Citizens.First(c.ElementaryEligible);c.Recalculate();sim=new CityTraffic(c);return c;
    }
    static void Until(CityTraffic sim,Func<bool> done,int steps=6000){for(int n=0;n<steps && !done();n++)sim.Advance(.05f);}
    static void Admit(CityModel c,CityTraffic sim,CityResident pupil,int school)
    {c.EnrollElementary(pupil.id,school);c.society.dayElapsed=40;sim.DispatchSchool(pupil.id);Until(sim,()=>pupil.atSchool);if(!pupil.atSchool)throw new Exception("Fixture pupil did not arrive");}
    public static void Run()
    {
        var c=Create(out var sim,out var p,out int school);var h=c.society.families.First(f=>f.id==p.householdId);
        Check(c.Valid() && c.HasBasicServices(school) && c.ElementaryDemand==6,"School is an independent served facility; demand counts real eligible children");
        Check(!c.EnrollElementary(c.Citizens.First(r=>r.canWork).id,school),"Adults cannot fill elementary places");
        Check(c.EnrollElementary(p.id,school) && c.EnrolledAt(school)==1 && c.StudentsAt(school)==0 && p.studyMinutes==0,"Enrollment reserves a real place without fake attendance or education");
        Check(c.EnrollElementary(p.id,school) && c.EnrolledAt(school)==1,"Repeated enrollment does not duplicate a place");
        c.society.dayElapsed=40;var trip=sim.DispatchSchool(p.id);
        Check(trip!=null && trip.walking && trip.cargo==0 && p.studyMinutes==0 && !p.atSchool,"Child creates a real walking school journey without a vehicle or instant learning");
        Check(c.ObserveResident(h,p).state=="步行上学" && c.ObserveResident(h,p).destination==school,"Child observation exposes the actual school destination");
        var saved=TestCity.RoundTrip(c);var child=saved.Citizens.First(r=>r.id==p.id);
        Check(child.schoolId==school && child.tripId==trip.id && saved.Valid(),"Enrolled pupil and actual walking journey survive saves");
        Until(sim,()=>p.atSchool);
        Check(p.location==school && p.tripId==0 && c.StudentsAt(school)>=1 && p.studyMinutes>0 && c.ObserveResident(h,p).building==school && c.ObserveResident(h,p).destination==school,"Only arrival starts learning and records the actual school location");
        float old=p.studyMinutes;sim.Advance(.5f);
        Check(p.studyMinutes>old && p.earnedWages==0 && p.workedMinutes==0 && p.jobId<0,"Actual lessons increase personal learning, not work or wages");
        p.studyMinutes=CityModel.ElementaryStudyMinutes-1;sim.Advance(.1f);
        Check(p.education==1 && p.skill>=1 && c.Citizens.Where(r=>r.id!=p.id).All(r=>r.education==0),"Only the pupil who actually finishes the remaining lessons gains education");
        Check(!p.canWork && p.jobId<0 && c.Employed==0,"Completing elementary lessons does not create an employed child or inflate workforce");
        saved=TestCity.RoundTrip(c);child=saved.Citizens.First(r=>r.id==p.id);
        Check(child.atSchool && child.location==school && child.education==1 && child.studyMinutes==p.studyMinutes && saved.Valid(),"Classroom presence and personal course progress survive saves");
        Until(sim,()=>p.schoolReturning);Until(sim,()=>!p.schoolReturning);
        Check(p.location==h.home && !p.atSchool && p.schoolId==school && c.Valid(),"Actual walk home retains enrollment rather than freeing a seat every afternoon");
        float accumulated=p.studyMinutes;c.Tick();Check(p.schoolMinutesToday==0 && p.studyMinutes==accumulated && p.education==1,"Daily attendance resets without erasing cumulative education");
        c=Create(out sim,out p,out school);Admit(c,sim,p,school);old=p.studyMinutes;c.utilities.water=new CityUtilityNetwork();c.Recalculate();sim.Advance(.5f);
        Check(p.studyMinutes==old && p.schoolReturning,"Unavailable service stops learning and child actually starts returning");
        c=Create(out sim,out p,out school);Admit(c,sim,p,school);old=p.studyMinutes;p.sick=true;p.health=30;sim.Advance(.5f);
        Check(p.studyMinutes==old && p.schoolReturning && p.schoolId==school,"Illness pauses study and sends the child home while keeping the place");
        c=Create(out sim,out p,out school);Admit(c,sim,p,school);int node=p.accessNode;h=c.society.families.First(f=>f.id==p.householdId);
        Check(c.DemolishBuilding(school) && p.schoolId==-1 && p.schoolReturning && p.location==-1 && p.accessNode==node && c.Valid(),"Demolished school releases enrollment but preserves the student's actual departure node");
        var observed=c.ObserveResident(h,p);
        Check(observed.located && observed.destination==h.home && observed.x==c.roads.Node(node).x,"Waiting displaced child is observed at the old school access, not teleported home");
        saved=TestCity.RoundTrip(c);child=saved.Citizens.First(r=>r.id==p.id);
        Check(child.schoolReturning && child.accessNode==node && saved.Valid(),"A displaced student waiting to walk home can be reliably saved");
        Until(sim,()=>!p.schoolReturning);Check(p.location==h.home && c.Valid(),"Displaced student actually walks home from the surviving road access");
        c=Create(out sim,out p,out school);c.EnrollElementary(p.id,school);c.society.dayElapsed=40;trip=sim.DispatchSchool(p.id);sim.Advance(.5f);node=trip.Current;
        Check(c.DemolishBuilding(school) && trip.returning && p.schoolReturning && p.schoolId==-1 && trip.Current==node && c.Valid(),"Removing a school during the walk redirects the same child from the current position and is immediately saveable");
        saved=TestCity.RoundTrip(c);Check(saved.traffic.trips.Any(t=>t.id==trip.id && t.returning) && saved.Valid(),"Redirected school journey survives native-shaped save data");
        c=Create(out sim,out p,out school);c.EnrollElementary(p.id,school);c.society.dayElapsed=40;trip=sim.DispatchSchool(p.id);sim.Advance(.5f);node=trip.Current;
        var roads=c.roads.Copy();c.roads.edges.Clear();c.roads.Changed();sim.Advance(121);
        Check(c.traffic.trips.Contains(trip) && p.tripId==trip.id && trip.Current==node && p.studyMinutes==0,"Long road interruption preserves the real child and cannot create lessons");
        c.roads=roads;c.Recalculate();sim=new CityTraffic(c);Until(sim,()=>p.location==c.society.families.First(f=>f.id==p.householdId).home && !p.schoolReturning);
        Check(c.Valid() && p.schoolId==school,"Restored roads allow safe return without losing enrollment");
        c=Create(out sim,out p,out school);foreach(var family in c.society.families)for(int slot=3;slot<8;slot++)family.people.Add(new CityResident{id=c.AllocateResidentId(),householdId=family.id,name="扩展儿童"+slot,age=10,location=family.home});c.Recalculate();
        foreach(var pupil in c.Citizens.Where(c.ElementaryEligible))c.EnrollElementary(pupil.id,school);
        Check(c.ElementaryDemand==36 && c.EnrolledAt(school)==24 && c.Valid(),"Finite school places are occupied by distinct real children; demand can exceed seats");
        int expense=c.upkeep,funds=c.money;c.SetServiceBudget(CityServiceKind.Education,50);
        Check(c.SchoolCapacity==12 && c.EnrolledAt(school)==24 && c.upkeep==expense-30 && c.money==funds && c.Valid(),"Budget cut affects new places and maintenance without deleting existing pupils");
        var waiting=c.Citizens.First(r=>c.ElementaryEligible(r) && r.schoolId<0);Check(!c.EnrollElementary(waiting.id,school),"Reduced capacity cannot admit another pupil while over budget");
        c.SetServiceBudget(CityServiceKind.Education,150);foreach(var pupil in c.Citizens.Where(c.ElementaryEligible))c.EnrollElementary(pupil.id,school);saved=TestCity.RoundTrip(c);
        Check(saved.SchoolCapacity==36 && saved.EnrolledAt(school)==36 && saved.development.educationBudget==150 && saved.Valid(),"Expanded education resources and all real enrollments survive saves");
        saved.development.educationBudget=0;Check(!saved.Valid(),"Malformed education budget is rejected");
        c=Create(out sim,out p,out school);p.studyMinutes=float.NaN;Check(!c.Valid(),"Invalid personal learning cannot enter a save");
        Console.WriteLine("PASS: "+checks+" school/enrollment/actual study/walking/demolition/budget/save checks");
    }
}
