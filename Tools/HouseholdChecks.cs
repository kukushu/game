using System;
using System.Linq;
using HarborCity;

public static class HouseholdChecks
{
    static int checks;
    static void Check(bool test,string message) {if(!test) throw new Exception("Households: "+message); checks++;}
    static CityModel Create()
    {
        var c=CityModel.Create(); new CityTraffic(c); c.EnableRoads((x,z)=>1); c.EnableBuildings(); c.EnableHouseholds(); return c;
    }
    public static void Run()
    {
        var c=Create(); Check(c.Valid(),"Version 4 migration valid");
        Check(c.society.families.All(h=>h.people.Count==h.members),"Every member has a personal record");
        var residentIds=c.society.families.SelectMany(h=>h.people).Select(p=>p.id).ToArray();
        Check(residentIds.Distinct().Count()==residentIds.Length,"Unique resident identities");
        c.EnsureResidents(); Check(residentIds.SequenceEqual(c.society.families.SelectMany(h=>h.people).Select(p=>p.id)),"Migration is idempotent");
        var household=c.society.families.First(h=>h.work>=0); var resident=household.people[0];
        float duration=c.CommuteMinutes(household.home,household.work), start=480+resident.id%3*30;
        c.society.dayElapsed=(start-duration*.5f)/1440*c.society.settings.secondsPerDay;
        var outward=c.ObserveResident(household,resident);
        Check(outward.travelling && outward.destination==household.work && Math.Abs(outward.progress-.5f)<.001f,"Outbound halfway progress and destination");
        c.society.dayElapsed=(start+240)/1440*c.society.settings.secondsPerDay;
        Check(c.ObserveResident(household,resident).building==household.work,"At work during shift");
        c.society.dayElapsed=(start+480+duration*.5f)/1440*c.society.settings.secondsPerDay;
        var inward=c.ObserveResident(household,resident);
        Check(inward.travelling && inward.destination==household.home && Math.Abs(inward.x-outward.x)<.01f && Math.Abs(inward.z-outward.z)<.01f,"Return route reverses same path");
        c.society.dayElapsed=0;
        Check(c.ObserveResident(household,resident).building==household.home,"Home at midnight");
        Check(c.ObserveResident(household,household.people[1]).building==household.home,"Nonworker has explicit home activity");
        Check(c.population==c.society.families.Sum(h=>h.members),"Population is actual residents");
        var f=c.society.families[0]; float preference=f.savingPreference;
        int balance=c.money; c.Recalculate(); c.Recalculate(); Check(balance==c.money,"Queries never settle money");
        var pose=c.RoadsideLots().First(b=>c.CanBuild(b,(x,z)=>1,out _)); pose.housing=HousingKind.Apartment;
        int id=c.PlaceBuilding(pose,LandUse.Residential,(x,z)=>1,out _);
        Check(id>=1296 && c.HousingCapacity(id)==8 && c.Occupancy(id)==0,"Construction creates vacant capacity");
        Check(c.money==balance-c.society.settings.apartmentCost,"Actual apartment construction cost");
        Check(c.population==c.society.families.Where(h=>h.resident).Sum(h=>h.members),"Vacant apartments do not create people");
        int oldUpkeep=c.upkeep; Check(oldUpkeep>=c.society.settings.apartmentMaintenance,"Vacancy incurs upkeep");
        int migrated=c.society.families.Count;
        for(int day=0;day<365;day++)
        {
            c.Tick(); var r=c.society.history.Last();
            if(r.closingTreasury!=r.openingTreasury+r.rent-r.maintenance) throw new Exception("Treasury conservation day "+day);
            if(r.closingSavings!=r.openingSavings+r.wages-r.rent-r.living-r.travel-r.movingCosts) throw new Exception("Household conservation day "+day);
            if(!c.Valid()) throw new Exception("Invalid simulation day "+day);
            foreach(var group in c.society.families.Where(h=>h.resident).GroupBy(h=>h.work))
                if(group.Key>=0 && group.Count()>c.JobCapacity(group.Key)) throw new Exception("Job overbooking");
        }
        Check(c.society.history.Count==180,"Bounded daily history");
        Check(f.savingPreference==preference,"Preferences stay stable for a year");
        Check(c.society.families.Count(h=>h.resident)>migrated,"Real households occupy new supply");
        Check(c.society.history.Sum(r=>r.rent)>0,"Occupied housing collects actual rent");
        Check(c.society.families.All(h=>h.options.Count<=9),"Bounded decision explanations");
        var a=Create(); var b=Create();
        int apt=CityModel.Index(21,17), villa=CityModel.Index(22,17), job=CityModel.Index(22,19);
        a.levels[job]=3;
        a.buildings[apt].askingRent=20; a.buildings[villa].housing=HousingKind.Villa; a.buildings[villa].askingRent=45;
        var saver=new Household {members=2,skill=2,savings=2000,savingPreference=1,spacePreference=0,privacyPreference=0,timePreference=.2f};
        var privateFamily=new Household {members=2,skill=2,savings=2000,savingPreference=.1f,spacePreference=1,privacyPreference=1,timePreference=.1f};
        Check(a.Evaluate(saver,apt,job,true).score>a.Evaluate(saver,villa,job,true).score,"Savings preference chooses apartment at same wage");
        Check(a.Evaluate(privateFamily,villa,job,true).score>a.Evaluate(privateFamily,apt,job,true).score,"Privacy preference chooses villa at same wage");
        a=Create();
        for(int d=0;d<20;d++) {a.Tick(); b.Tick();}
        Check(a.money==b.money && a.society.families.Select(h=>h.home+":"+h.work+":"+h.savings).SequenceEqual(b.society.families.Select(h=>h.home+":"+h.work+":"+h.savings)),"Deterministic daily decisions");
        var person=a.society.families.First(h=>h.work>=0); int home=person.home;
        a.DemolishBuilding(home); a.Recalculate();
        Check(person.resident && a.population==a.society.families.Where(h=>h.resident).Sum(h=>h.members),"Demolition does not erase people");
        a.Tick(); Check(a.Valid(),"Displacement remains valid");
        var isolated=Create(); var worker=isolated.society.families.First(h=>h.work>=0);
        isolated.roads.Remove(isolated.roads.edges.Select(e=>e.id).ToList());
        Check(isolated.CommuteMinutes(worker.home,worker.work)<0,"Road edit invalidates commute cache");
        Check(!isolated.ObserveResident(worker,worker.people[0]).travelling,"Disconnected road prevents personal commute");
        isolated.Tick(); Check(worker.wagePaid==0 && worker.resident,"Unreachable workplace pauses pay, retains resident");
        for(int d=0;d<31;d++) isolated.Tick();
        Check(!worker.resident,"Prolonged failure permits migration out");
        Console.WriteLine("PASS: "+checks+" household checks, 365-day conservation/capacity/validity run");
    }
}
