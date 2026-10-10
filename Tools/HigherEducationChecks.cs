using System;
using System.Linq;
using HarborCity;

public static class HigherEducationChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Higher education: "+why);checks++;}
    static void Until(CityTraffic sim,Func<bool> done){for(int n=0;n<6000 && !done();n++)sim.Advance(.05f);}
    public static void Run()
    {
        var c=TestCity.Create();c.development.enabled=true;UtilitySupplyChecks.ConnectFixture(c);c.waste.enabled=false;c.fire.chancePerBuildingDay=0;c.society.settings.applicantsPerDay=0;c.money+=100000;
        int elementary=TestCity.Build(c,40,-5,LandUse.ElementarySchool),high=-1,university=-1;
        var p=c.Citizens.First(c.ElementaryEligible);int citizen=p.id;var h=c.society.families.First(f=>f.id==p.householdId);var sim=new CityTraffic(c);
        p.age=13;p.ageProgress=0;
        Check(c.Valid() && c.HasBasicServices(elementary),"Elementary fixture starts with a real supplied independent building");
        c.society.dayElapsed=40;c.EnrollSchool(citizen,elementary);sim.DispatchSchool(citizen);sim.Advance(1900);
        Check(p.education==1 && p.studyMinutes>=CityModel.ElementaryStudyMinutes && p.highSchoolStudyMinutes==0 && p.universityStudyMinutes==0,"Repeated actual elementary lessons earn only elementary education");
        p.age=13;p.ageProgress=.99;p.lastAgeDay=c.day-1;c.UpdateResidentAges();Until(sim,()=>p.tripId==0 && !p.schoolReturning);
        Check(p.age==14 && c.HighSchoolEligible(p) && p.location==h.home && p.schoolId<0,"School stage change physically returns before new enrollment");
        high=TestCity.Build(c,30,-5,LandUse.HighSchool);
        // Controlled qualification boundary, separate from the fully earned main student's records.
        var unqualified=c.Citizens.First(r=>r.id!=citizen && !r.canWork);int originalAge=unqualified.age,originalEducation=unqualified.education;unqualified.age=14;unqualified.education=0;
        Check(!c.EnrollSchool(unqualified.id,high),"Actual high school rejects a controlled applicant without elementary qualification");unqualified.age=originalAge;unqualified.education=originalEducation;
        c.society.dayElapsed=40;Check(c.EnrollSchool(citizen,high) && c.StudentsAt(high)==0 && c.EnrolledAt(high)==1,"High school reserves a real place without fake attendance");
        var trip=sim.DispatchSchool(citizen);Check(trip!=null && trip.destination==high && trip.walking && p.highSchoolStudyMinutes==0,"High school has an actual destination and independent learning record");
        var saved=TestCity.RoundTrip(c);p=saved.Citizens.First(r=>r.id==citizen);c=saved;sim=new CityTraffic(c);h=c.society.families.First(f=>f.id==p.householdId);
        sim.Advance(1600);Check(p.education==2 && p.highSchoolStudyMinutes>=CityModel.CourseMinutes(2) && p.universityStudyMinutes==0 && c.Valid(),"Saved high-school journey resumes and actual repeated lessons earn education two; grade="+p.education+"; minutes="+p.highSchoolStudyMinutes+"; valid="+c.Valid());
        p.age=17;p.ageProgress=.99;p.lastAgeDay=c.day-1;c.UpdateResidentAges();Until(sim,()=>p.tripId==0 && !p.schoolReturning);
        // Eligibility boundary is controlled; qualifications above were all earned by attendance.
        c.ReleaseJob(p);c.society.dayElapsed=40;
        university=TestCity.Build(c,20,-5,LandUse.University);
        Check(c.Valid() && c.HasBasicServices(high) && c.HasBasicServices(university),"Later schools remain independent supplied entities");
        Check(c.EnrollSchool(citizen,university) && !p.canWork && p.jobId<0 && c.StudentsAt(university)==0,"Full-time university enrollment reserves a place and avoids competing work dispatch");
        int actualWorkforce=c.Citizens.Count(r=>r.canWork);c.Recalculate();Check(c.Employed+c.Unemployed==actualWorkforce,"Full-time students do not fabricate employment or enter both school and work");
        sim.DispatchSchool(citizen);Until(sim,()=>p.atSchool);float before=p.universityStudyMinutes;var water=c.utilities.water;c.society.dayElapsed=60;c.utilities.water=new CityUtilityNetwork();c.Recalculate();
        sim.Advance(.1f);Check(p.universityStudyMinutes==before,"Unserved university cannot grant learning");
        c.utilities.water=water;c.Recalculate();Until(sim,()=>p.tripId==0 && !p.schoolReturning);sim.Advance(2600);
        Check(p.education==3 && p.universityStudyMinutes>=CityModel.CourseMinutes(3) && p.schoolId<0 && p.canWork && c.Valid(),"Repeated actual university lessons graduate the individual and release the real place");
        Until(sim,()=>p.tripId==0 && !p.schoolReturning);Check(p.location==h.home || p.tripId>0 || p.atWork,"University graduate resumes normal activity after actual return");
        saved=TestCity.RoundTrip(c);var graduated=saved.Citizens.First(r=>r.id==citizen);
        Check(graduated.education==3 && graduated.highSchoolStudyMinutes==p.highSchoolStudyMinutes && graduated.universityStudyMinutes==p.universityStudyMinutes && saved.GetBuilding(high) is HighSchoolBuilding && saved.GetBuilding(university) is UniversityBuilding,"All independent course records and school types survive saving");
        c.SetServiceBudget(CityServiceKind.Education,150);Check(c.CapacityAtSchool(high)==36 && c.CapacityAtSchool(university)==54,"Education budget acts on each facility's real finite capacity");
        graduated.highSchoolStudyMinutes=float.NaN;Check(!saved.Valid(),"Malformed later-stage course record is rejected");
        Console.WriteLine("PASS: "+checks+" real staged education/high-school/university/qualification/attendance/supply/save checks");
    }
}
