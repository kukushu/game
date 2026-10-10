using System;
using System.Linq;
using HarborCity;

public static class HealthChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Health: "+why);checks++;}
    static CityModel Create(){var c=TestCity.Create();c.development.enabled=true;UtilitySupplyChecks.ConnectFixture(c);c.society.settings.applicantsPerDay=0;return c;}
    public static void Run()
    {
        var c=Create();var p=c.Citizens.First();var h=c.society.families.First(f=>f.id==p.householdId);
        Check(c.AverageHealth==100 && c.SickResidents==0,"New residents start with personal health, not a preset sick population");
        p.health=80;c.UpdateResidentHealth();Check(p.health==82 && !p.sick,"Clean connected location supports healthy residents");
        int job=p.jobId;c.GetBuilding(h.home).garbage=CityModel.GarbageWarning;p.health=41;c.UpdateResidentHealth();
        Check(p.health==35 && p.sick && p.jobId==job && c.ResidentJob(p)!=null,"Real garbage exposure causes illness without freeing the job contract");
        Check(c.SickResidents==1 && c.AverageHealth<100,"Health statistics aggregate real resident states");
        Check(c.ObserveResident(h,p).state=="患病，等待就医" && !c.CanAttendWork(p),"Observer explains illness and attendance uses personal state");
        c.GetBuilding(h.home).garbage=0;float sickHealth=p.health;c.UpdateResidentHealth();
        Check(p.sick && p.health==sickHealth,"Clean surroundings alone do not fabricate completed medical treatment");
        var copy=TestCity.RoundTrip(c);var restored=copy.Citizens.First(r=>r.id==p.id);
        Check(restored.sick && restored.health==p.health && restored.jobId==job && copy.Valid(),"Illness and the retained job survive saves");
        p.health=float.NaN;Check(!c.Valid(),"Malformed health cannot enter a valid save");p.health=101;Check(!c.Valid(),"Out-of-range health is rejected");
        c=Create();p=c.Citizens.First(r=>c.ResidentJob(r)!=null);h=c.society.families.First(f=>f.id==p.householdId);
        foreach(var other in c.Citizens.Where(r=>r!=p))c.ReleaseJob(other);
        p.sick=true;p.health=30;int workplace=c.Workplace(p);job=p.jobId;var sim=new CityTraffic(c);
        c.society.dayElapsed=(480+p.id%3*30)/1440f*c.society.settings.secondsPerDay;
        Check(sim.Dispatch(h.home,workplace,TripPurpose.Commute,p.id)==null,"Ill resident cannot directly dispatch an outbound work trip");
        sim.Advance(.05f);Check(p.tripId==0 && p.workedMinutes==0 && p.earnedWages==0 && p.jobId==job,"Sick resident stays home without fake pay or unemployment");
        p.location=workplace;p.atWork=true;p.arrivedDay=c.day;
        sim.Advance(.3f);Check(p.workedMinutes==0 && p.earnedWages==0 && p.tripId>0 && c.traffic.trips.Find(t=>t.id==p.tripId).returning,"Ill worker actually travels home and produces no labour while waiting");
        c=Create();p=c.Citizens.First();h=c.society.families.First(f=>f.id==p.householdId);c.development.enabled=false;
        c.GetBuilding(h.home).garbage=CityModel.GarbageWarning;p.health=50;c.UpdateResidentHealth();
        Check(p.health==50 && !p.sick,"Old microeconomic prototype remains isolated from new baseline health rules");
        Console.WriteLine("PASS: "+checks+" personal health, attendance and save checks");
    }
}
