using System;
using System.Linq;
using HarborCity;

public static class ResidentTransportChecks
{
    static int checks;
    static void Check(bool condition,string message) {if(!condition) throw new Exception("Resident transport: "+message); checks++;}
    static CityModel Create(out CityTraffic sim,out Household h,out CityResident p)
    {
        var c=CityModel.CreateLegacySample(); sim=new CityTraffic(c); c.EnableRoads((x,z)=>1); c.EnableBuildings(); c.EnableHouseholds(); c.EnableResidentTransport();
        h=c.society.families.First(f=>f.people.Any(person=>c.Workplace(person)>=0)); p=h.people.First(person=>c.Workplace(person)>=0);
        foreach(var family in c.society.families) foreach(var person in family.people) if(person!=p) c.ReleaseJob(person);
        foreach(var other in c.society.families) if(other!=h) other.resident=false;
        c.society.settings.applicantsPerDay=0;
        foreach(var other in c.society.families) other.nextReview=10000;
        c.traffic.dispatchTimer=-10000; c.traffic.productionTimer=-10000;
        c.society.dayElapsed=(480+p.id%3*30-c.CommuteMinutes(h.home,c.Workplace(p)))/1440*c.society.settings.secondsPerDay;
        return c;
    }
    public static void Run()
    {
        foreach(float dayLength in new[]{60f,120f,240f})
        {
            float reference=-1;
            foreach(TripPurpose purpose in Enum.GetValues(typeof(TripPurpose)))
            {
                var sample=Create(out var driver,out var family,out var person);
                sample.society.settings.secondsPerDay=dayLength; sample.society.dayElapsed=0;
                var route=driver.FindRoute(family.home,sample.Workplace(person));
                var task=new TrafficTrip {id=sample.traffic.nextId++,origin=family.home,home=family.home,
                    destination=sample.Workplace(person),route=route,purpose=purpose,status=TripStatus.Driving,
                    residentId=purpose==TripPurpose.Commute?person.id:0,householdId=family.id};
                if(task.residentId>0) person.tripId=task.id;
                sample.traffic.trips.Add(task); driver.Advance(.05f);
                float distance=task.progress*sample.roads.EdgeLength(task.Current,task.Next)/3;
                for(int n=0;n<task.segment;n++) distance+=sample.roads.EdgeLength(route[n],route[n+1])/3;
                if(reference<0) reference=distance;
                Check(distance>0 && Math.Abs(distance-reference)<.0001f && Math.Abs(distance-.05f*480/dayLength)<.0001f,
                    purpose+" shares actual road movement and day-length scaling at "+dayLength+" seconds/day");
            }
        }
        var c=Create(out var sim,out var h,out var p);
        sim.Advance(.05f); Check(p.tripId>0 && !p.atWork,"Dispatch creates actual resident task without instant arrival");
        var trip=c.traffic.trips.Single(); var a=c.ObserveResident(h,p);
        Check(a.travelling && a.destination==c.Workplace(p) && trip.residentId==p.id,"Observer and vehicle share identity and destination");
        for(int i=0;i<2000 && !p.atWork;i++) sim.Advance(.05f);
        Check(p.atWork && p.tripId==0 && p.location==c.Workplace(p) && p.lastCommute>0,"Only actual arrival starts work and records commute");
        sim.Advance(10); Check(p.earnedWages>0 && p.workedMinutes>0,"Actual attendance earns wages");
        p.lastCommute=150; p.observedHome=h.home; p.observedWork=c.Workplace(p); p.observedRevision=c.roads.revision;
        Check(c.Evaluate(h,h.home,keepJobs:true).minutes>=150,"Experienced congestion enters household candidate score");
        c.roads.Changed(); Check(c.ExpectedCommute(p,h.home,c.Workplace(p))<150,"Road changes invalidate old commute observation");
        for(int i=0;i<2400 && (p.atWork || p.tripId>0);i++) sim.Advance(.05f);
        Check(!p.atWork && p.tripId==0 && p.location==h.home,"Return vehicle actually reaches home");
        sim.Advance(c.society.settings.secondsPerDay-c.society.dayElapsed+.1f);
        Check(h.wagePaid>0 && h.wagePaid<=c.Wage(c.Workplace(p)),"Daily wages derive from attendance, never exceed daily salary");
        var report=c.society.history.Last();
        Check(report.closingSavings==report.openingSavings+report.wages-report.rent-report.living-report.travel-report.movingCosts,"Integrated daily cash conservation");
        c.traffic.dispatchTimer=c.traffic.productionTimer=0;
        Check(c.Valid(),"Integrated transport remains saveable");
        c=Create(out sim,out h,out p); sim.Advance(.1f); trip=c.traffic.trips.Single();
        c.roads.Remove(c.roads.edges.Select(e=>e.id).ToList()); sim.Advance(3);
        Check(p.tripId==trip.id && !p.atWork && p.earnedWages==0 && trip.status==TripStatus.Waiting,"Broken road cannot create arrival or wages");
        c=Create(out sim,out h,out p);
        for(int i=0;i<CityTraffic.Capacity;i++) c.traffic.trips.Add(new TrafficTrip {id=c.traffic.nextId++,origin=h.home,home=h.home,destination=c.Workplace(p),status=TripStatus.Visiting,delay=1000,route=sim.FindRoute(h.home,c.Workplace(p))});
        sim.Advance(.05f); Check(p.tripId>0 && c.traffic.trips.Count>CityTraffic.Capacity,"Display/background capacity does not cap resident tasks");
        c=Create(out sim,out h,out p);
        c.society.dayElapsed=(540+p.id%3*30)/1440f*c.society.settings.secondsPerDay;
        sim.Advance(c.society.settings.secondsPerDay-c.society.dayElapsed+.1f);
        Check(p.lastDelay>=60 && h.wagePaid>0 && h.wagePaid<c.Wage(c.Workplace(p)),"Late actual arrival reduces daily pay");
        c=CityModel.CreateLegacySample(); sim=new CityTraffic(c); c.EnableRoads((x,z)=>1); c.EnableBuildings(); c.EnableHouseholds(); c.EnableResidentTransport();
        sim.Advance(360.1f);
        Check(c.day==4 && c.Valid(),"Three-day integrated city stays valid across day boundaries");
        Check(c.society.history.All(r=>r.closingSavings==r.openingSavings+r.wages-r.rent-r.living-r.travel-r.movingCosts),"Three-day actual payroll conserves household money");
        Check(c.society.families.Any(f=>f.people.Any(person=>person.lastCommute>0)) && c.society.history.Any(r=>r.wages>0),"Full city produces actual arrivals and paid work");
        Console.WriteLine("PASS: "+checks+" resident transport checks");
    }
}
