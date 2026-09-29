using System;
using System.Linq;
using HarborCity;

public static class JobChecks
{
    static int checks;
    static void Check(bool value,string message) {if(!value) throw new Exception("Jobs: "+message); checks++;}
    static CityModel Create()
    {
        var c=CityModel.CreateLegacySample(); new CityTraffic(c); c.EnableRoads((x,z)=>1); c.EnableBuildings(); c.EnableHouseholds();
        foreach(var h in c.society.families) {h.nextReview=10000; foreach(var p in h.people) c.ReleaseJob(p);}
        c.society.settings.applicantsPerDay=0; return c;
    }
    public static void Run()
    {
        var c=Create(); var h=c.society.families.First(f=>f.resident);
        var a=h.people[0]; var b=h.people[1]; var child=h.people[2];
        a.skill=b.skill=2;
        var shop=c.society.jobEntities.First(j=>c.tiles[j.buildingId]==3);
        var factory=c.society.jobEntities.First(j=>c.tiles[j.buildingId]==4);
        Check(c.AssignJob(a,shop.id) && c.AssignJob(b,factory.id),"Two members occupy different employers");
        Check(!c.AssignJob(child,shop.id),"Child cannot occupy a job");
        Check(!c.AssignJob(b,shop.id) && b.jobId==factory.id,"Occupied job cannot be stolen and failed assignment preserves prior job");
        var stranger=new CityResident {id=999999,age=30,canWork=true,skill=2};
        Check(!c.AssignJob(stranger,c.society.jobEntities.First(j=>j.occupiedCitizenId<0).id),"Unregistered person cannot occupy job");
        Check(c.Employed==2 && c.Unemployed==c.Citizens.Count(p=>p.canWork)-2 && c.EmployedAt(shop.buildingId)==1,"Employment counts actual individuals");
        Check(c.HouseholdSalary(h)==shop.wage+factory.wage,"Expected family salary sums individual contracts");
        c.EnableResidentTransport(); a.lastCommute=500; b.lastCommute=800;
        foreach(var p in new[]{a,b}) {p.observedHome=h.home; p.observedWork=c.Workplace(p); p.observedRevision=c.roads.revision;}
        Check(c.AverageCommute==650 && c.Evaluate(h,h.home,keepJobs:true).minutes==1300,"Actual commute average and family total use both workers");
        int people=c.Citizens.Count(), slots=c.society.jobEntities.Count, occupied=c.Occupancy(h.home);
        h.members=999; h.work=-1; h.skill=0; h.commute=999; c.population=1; c.jobs=1;
        c.EnsureResidents(); c.Recalculate();
        Check(c.population==people && c.jobs==slots && c.Occupancy(h.home)==occupied && c.Workplace(a)==shop.buildingId,"Legacy fields and macro caches cannot change micro entities");
        a.earnedWages=12; b.earnedWages=31; c.HouseholdTick();
        Check(a.wagePaid==12 && b.wagePaid==31 && h.wagePaid==43 && c.society.history.Last().wages==43,"Daily family income sums only actual personal earnings");
        c.HouseholdTick(); Check(h.wagePaid==0,"Assigned jobs alone do not mint wages");
        int[] jobIds=c.society.jobEntities.Select(j=>j.id).ToArray(); c.SyncJobs();
        Check(jobIds.SequenceEqual(c.society.jobEntities.Select(j=>j.id)),"Sync preserves stable job IDs");
        int employedBefore=c.Employed; c.roads.Remove(c.roads.edges.Select(e=>e.id).ToList()); c.Recalculate();
        Check(c.jobs==slots && c.Employed==employedBefore && c.population==people,"Road disconnection preserves people, jobs and employment");
        a.earnedWages=7; c.tiles[shop.buildingId]=0; c.SyncJobs();
        Check(a.jobId==-1 && a.earnedWages==7 && c.Job(shop.id)==null && c.Workplace(b)==factory.buildingId,"Demolition releases job without losing accrued pay");
        c.ReleaseJob(b); b.skill=0;
        Check(!c.AssignJob(b,factory.id) && factory.occupiedCitizenId==-1,"Skill requirement enforced");
        Check(c.ValidJobs(),"Relations remain consistent after releases");

        c=Create(); h=c.society.families.First(f=>f.resident); a=h.people[0];
        shop=c.society.jobEntities.First(j=>c.tiles[j.buildingId]==3);
        h.work=shop.buildingId; h.skill=2; h.savings=1234; h.commute=17;
        a.tripId=123; a.earnedWages=9; a.workedMinutes=33; a.lastCommute=17;
        c.version=4; c.society.jobEntities.Clear(); c.EnableHouseholds();
        Check(c.version==5 && c.Workplace(a)==h.work && a.skill==2,"Legacy household employer migrates to original worker");
        Check(a.tripId==123 && a.earnedWages==9 && a.workedMinutes==33 && a.lastCommute==17 && h.savings==1234,"Migration preserves trip, attendance, observations and savings");
        Check(h.people[1].canWork && h.people[1].jobId==-1,"Other legacy adults await independent job choice");
        int migrated=a.jobId; c.EnableHouseholds(); Check(a.jobId==migrated && c.ValidJobs(),"Migration is idempotent");
        Console.WriteLine("PASS: "+checks+" citizen/job relationship checks");
    }
}
