using System;
using System.Linq;
using HarborCity;

public static class ResidentAgingChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Aging: "+why);checks++;}
    static CityModel Create(out CityTraffic sim)
    {
        var c=TestCity.Create();c.development.enabled=true;UtilitySupplyChecks.ConnectFixture(c);c.waste.enabled=false;c.fire.chancePerBuildingDay=0;c.society.settings.applicantsPerDay=0;
        sim=new CityTraffic(c);return c;
    }
    static void Until(CityTraffic sim,Func<bool> done){for(int n=0;n<6000 && !done();n++)sim.Advance(.05f);}
    public static void Run()
    {
        var c=Create(out var sim);var person=c.Citizens.First();int age=person.age,population=c.population;
        c.UpdateResidentAges();double fraction=person.ageProgress;c.UpdateResidentAges();
        Check(person.age==age && fraction>0 && person.ageProgress==fraction && c.population==population,"Age progress records one resident day without extra population or duplicate updates");
        var saved=TestCity.RoundTrip(c);Check(saved.Citizens.First(p=>p.id==person.id).ageProgress==person.ageProgress && saved.lifecycle.daysPerYear==30 && saved.Valid(),"Individual progress and exposed provisional pacing survive saving");
        for(int n=0;n<29;n++)c.Tick();person=c.Citizens.First(p=>p.id==person.id);
        Check(person.age==age+1 && person.ageProgress<.00001 && c.Valid(),"Thirty actual resident-day evaluations advance one year at the provisional rate");
        c=Create(out sim);var child=c.Citizens.First(p=>!p.canWork);child.age=17;child.ageProgress=.99;child.skill=2;population=c.population;
        c.Tick();Check(child.age==18 && child.canWork && c.ResidentJob(child)!=null && c.population==population,"New adult can fill a real vacant Job without creating a citizen");
        Check(c.Valid() && c.Employed==c.Citizens.Count(p=>p.canWork && c.ResidentJob(p)!=null),"Workforce and employment follow actual updated people and contracts");
        c=Create(out sim);person=c.Citizens.First(p=>c.ResidentJob(p)!=null);var family=c.society.families.First(h=>h.id==person.householdId);int work=c.Workplace(person),jobId=person.jobId;
        person.age=64;person.ageProgress=.99;person.atWork=true;person.location=work;person.arrivedDay=c.day;person.accessNode=c.AccessBuilding(work).First();person.earnedWages=10;c.society.dayElapsed=60;
        c.Tick();
        Check(person.age==65 && !person.canWork && person.jobId<0 && c.Job(jobId).occupiedCitizenId!=person.id && person.location==work && person.atWork,"Retirement releases the concrete job but preserves physical workplace position");
        Check(family.wagePaid==10 && c.Valid(),"Previously earned pay survives retirement and the state can immediately save");
        float worked=person.workedMinutes;sim.Advance(.05f);var trip=c.traffic.trips.Find(t=>t.id==person.tripId);
        Check(trip!=null && trip.returning && trip.destination==family.home && person.workedMinutes==worked,"Retired worker actually departs for home and cannot keep producing labour");
        saved=TestCity.RoundTrip(c);int citizen=person.id;sim=new CityTraffic(saved);person=saved.Citizens.First(p=>p.id==citizen);family=saved.society.families.First(h=>h.id==person.householdId);
        Until(sim,()=>person.tripId==0 && !person.atWork);Check(person.location==family.home && !person.canWork && saved.Valid(),"Retired commuter resumes after loading and physically reaches home");
        c=Create(out sim);person=c.Citizens.First(p=>c.ResidentJob(p)!=null);family=c.society.families.First(h=>h.id==person.householdId);work=c.Workplace(person);
        trip=sim.Dispatch(family.home,work,TripPurpose.Commute,person.id);trip.householdId=family.id;trip.home=family.home;person.tripId=trip.id;person.departureDay=c.day;sim.Advance(.25f);
        int node=trip.Current;float progress=trip.progress;person.age=64;person.ageProgress=.99;c.UpdateResidentAges();
        Check(trip.returning && trip.Current==node && trip.progress==progress && trip.destination==family.home && !person.canWork && c.Valid(),"Birthday during outward commute redirects the same physical car without teleportation");
        c=Create(out sim);foreach(var p in c.Citizens)c.ReleaseJob(p);c.money+=CityModel.Cost(LandUse.ElementarySchool);int school=TestCity.Build(c,40,-5,LandUse.ElementarySchool);
        child=c.Citizens.First(c.ElementaryEligible);child.age=13;child.ageProgress=.99;family=c.society.families.First(h=>h.id==child.householdId);c.society.dayElapsed=40;
        Check(c.EnrollElementary(child.id,school) && sim.DispatchSchool(child.id)!=null,"Controlled pupil really enrolls and walks to school before age transition");
        Until(sim,()=>child.atSchool);int location=child.location;float studied=child.studyMinutes;int education=child.education;node=child.accessNode;
        c.UpdateResidentAges();
        Check(child.age==14 && child.schoolId<0 && !child.atSchool && child.schoolReturning && child.location==location && child.accessNode==node,"At-school age transition releases the place and retains actual classroom position");
        Check(child.education==education && child.studyMinutes==studied && c.Valid(),"Growing out of elementary school does not grant unearned education and is immediately saveable");
        saved=TestCity.RoundTrip(c);citizen=child.id;sim=new CityTraffic(saved);child=saved.Citizens.First(p=>p.id==citizen);family=saved.society.families.First(h=>h.id==child.householdId);
        sim.Advance(.05f);trip=saved.traffic.trips.Find(t=>t.id==child.tripId);
        Check(trip!=null && trip.walking && trip.returning && child.schoolReturning && !saved.ElementaryEligible(child),"Older child can create a real return journey despite no longer qualifying for enrollment");
        Until(sim,()=>child.tripId==0 && !child.schoolReturning);Check(child.location==family.home && child.schoolId<0 && saved.Valid(),"Older pupil physically returns home after saving the transition");
        c=Create(out sim);foreach(var p in c.Citizens)c.ReleaseJob(p);c.money+=CityModel.Cost(LandUse.ElementarySchool);school=TestCity.Build(c,40,-5,LandUse.ElementarySchool);child=c.Citizens.First(c.ElementaryEligible);
        child.age=13;child.ageProgress=.99;family=c.society.families.First(h=>h.id==child.householdId);c.society.dayElapsed=40;c.EnrollElementary(child.id,school);trip=sim.DispatchSchool(child.id);sim.Advance(.5f);node=trip.Current;progress=trip.progress;
        c.UpdateResidentAges();Check(child.age==14 && child.schoolId<0 && trip.returning && trip.Current==node && trip.progress==progress && c.Valid(),"Age transition on the way to school preserves physical walking position and redirects home");
        c.lifecycle.daysPerYear=double.NaN;Check(!c.Valid(),"Nonfinite age pacing is rejected");
        c=Create(out sim);c.Citizens.First().ageProgress=1;Check(!c.Valid(),"Malformed individual progress is rejected");
        c=Create(out sim);c.development.enabled=false;person=c.Citizens.First();age=person.age;c.UpdateResidentAges();
        Check(person.age==age && person.ageProgress==0,"Legacy prototype mode keeps its original static-age regression behavior");
        Console.WriteLine("PASS: "+checks+" individual aging/adulthood/retirement/school departure/real return/save checks");
    }
}
