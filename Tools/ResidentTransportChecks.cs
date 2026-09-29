using System;
using System.Linq;
using HarborCity;

public static class ResidentTransportChecks
{
    static int checks;
    static void Check(bool condition,string message) {if(!condition) throw new Exception("Resident transport: "+message); checks++;}
    static CityModel Create(out CityTraffic sim,out Household h,out CityResident p)
    {
        var c=CityModel.Create(); sim=new CityTraffic(c); c.EnableRoads((x,z)=>1); c.EnableBuildings(); c.EnableHouseholds(); c.EnableResidentTransport();
        h=c.society.families.First(f=>f.work>=0); p=h.people[0];
        foreach(var other in c.society.families) if(other!=h) other.resident=false;
        c.society.settings.applicantsPerDay=0;
        foreach(var other in c.society.families) other.nextReview=10000;
        c.traffic.dispatchTimer=-10000; c.traffic.productionTimer=-10000;
        c.society.dayElapsed=(480+p.id%3*30-c.CommuteMinutes(h.home,h.work))/1440*c.society.settings.secondsPerDay;
        return c;
    }
    public static void Run()
    {
        var c=Create(out var sim,out var h,out var p);
        sim.Advance(.05f); Check(p.tripId>0 && !p.atWork,"Dispatch creates actual resident task without instant arrival");
        var trip=c.traffic.trips.Single(); var a=c.ObserveResident(h,p);
        Check(a.travelling && a.destination==h.work && trip.residentId==p.id,"Observer and vehicle share identity and destination");
        for(int i=0;i<2000 && !p.atWork;i++) sim.Advance(.05f);
        Check(p.atWork && p.tripId==0 && p.location==h.work && p.lastCommute>0,"Only actual arrival starts work and records commute");
        sim.Advance(10); Check(p.earnedWages>0 && p.workedMinutes>0,"Actual attendance earns wages");
        p.lastCommute=150; p.observedHome=h.home; p.observedWork=h.work; p.observedRevision=c.roads.revision;
        Check(c.Evaluate(h,h.home,h.work).minutes>=150,"Experienced congestion enters household candidate score");
        c.roads.Changed(); Check(c.ExpectedCommute(h,h.home,h.work)<150,"Road changes invalidate old commute observation");
        for(int i=0;i<2400 && (p.atWork || p.tripId>0);i++) sim.Advance(.05f);
        Check(!p.atWork && p.tripId==0 && p.location==h.home,"Return vehicle actually reaches home");
        sim.Advance(c.society.settings.secondsPerDay-c.society.dayElapsed+.1f);
        Check(h.wagePaid>0 && h.wagePaid<=c.Wage(h.work),"Daily wages derive from attendance, never exceed daily salary");
        var report=c.society.history.Last();
        Check(report.closingSavings==report.openingSavings+report.wages-report.rent-report.living-report.travel-report.movingCosts,"Integrated daily cash conservation");
        c.traffic.dispatchTimer=c.traffic.productionTimer=0;
        Check(c.Valid(),"Integrated transport remains saveable");
        c=Create(out sim,out h,out p); sim.Advance(.1f); trip=c.traffic.trips.Single();
        c.roads.Remove(c.roads.edges.Select(e=>e.id).ToList()); sim.Advance(3);
        Check(p.tripId==trip.id && !p.atWork && p.earnedWages==0 && trip.status==TripStatus.Waiting,"Broken road cannot create arrival or wages");
        c=Create(out sim,out h,out p);
        for(int i=0;i<CityTraffic.Capacity;i++) c.traffic.trips.Add(new TrafficTrip {id=c.traffic.nextId++,origin=h.home,home=h.home,destination=h.work,status=TripStatus.Visiting,delay=1000,route=sim.FindRoute(h.home,h.work)});
        sim.Advance(.05f); Check(p.tripId>0 && c.traffic.trips.Count>CityTraffic.Capacity,"Display/background capacity does not cap resident tasks");
        c=Create(out sim,out h,out p);
        c.society.dayElapsed=(540+p.id%3*30)/1440f*c.society.settings.secondsPerDay;
        sim.Advance(c.society.settings.secondsPerDay-c.society.dayElapsed+.1f);
        Check(p.lastDelay>=60 && h.wagePaid>0 && h.wagePaid<c.Wage(h.work),"Late actual arrival reduces daily pay");
        c=CityModel.Create(); sim=new CityTraffic(c); c.EnableRoads((x,z)=>1); c.EnableBuildings(); c.EnableHouseholds(); c.EnableResidentTransport();
        sim.Advance(360.1f);
        Check(c.day==4 && c.Valid(),"Three-day integrated city stays valid across day boundaries");
        Check(c.society.history.All(r=>r.closingSavings==r.openingSavings+r.wages-r.rent-r.living-r.travel-r.movingCosts),"Three-day actual payroll conserves household money");
        Check(c.society.families.Any(f=>f.people.Any(person=>person.lastCommute>0)) && c.society.history.Any(r=>r.wages>0),"Full city produces actual arrivals and paid work");
        Console.WriteLine("PASS: "+checks+" resident transport checks");
    }
}
