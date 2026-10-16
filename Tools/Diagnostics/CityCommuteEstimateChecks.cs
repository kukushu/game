using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarborCity;
using HarborCity.RoadCore;
static class CityCommuteEstimateChecks
{
    static int checks;
    static void Check(bool b,string why){checks++;if(!b)throw new Exception(why);}
#if EXPECT_COMMUTE_CACHE
    static double Cached(CityCoreIndustrySession s,CityMember m,int home,int work)
    {return (double)typeof(CityCoreResidents).GetMethod("EstimateCommute",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(s.Residents,new object[]{m.Person.id,home,work});}
    static void RestrictAccess(CityCoreIndustrySession s,int id,AccessUse use)
    {var edit=s.Transport.CaptureRoadEdit();edit.accesses.Single(a=>a.endpoint==id).use=use;Check(s.Transport.PlanRestoreRoadEdit(edit).TryCommit(),"Staged access restriction edit failed");}
    static void Lifecycle(byte[] initial)
    {
        var s=CityCoreSave.Read(initial);s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);Check(s.Residents.CachedCommuteEstimates==0,"Derived estimates were saved as facts");s.Advance(.05);
        var m=s.Entities.Members.First(x=>x.Person.canWork&&x.Person.jobId>=0&&x.Person.tripId==0);int home=m.Family.home,work=s.City.Workplace(m.Person);
        var before=CityCoreSave.Write(s);for(int i=0;i<100;i++)Check(Cached(s,m,home,work)==s.Transport.EstimateResidentSeconds(home,work),"Warm estimate differs from authority");Check(before.SequenceEqual(CityCoreSave.Write(s)),"Estimate observation changed city facts");
        // Pure key-invalidation unit queries use existing registered alternatives;
        // they do not claim to move a household or assign a speculative job.
        int stored=s.Residents.CachedCommuteEstimates;
        foreach(var h in s.Entities.Buildings.OfType<ResidentialBuilding>().Take(3))foreach(var b in s.Entities.Buildings.Where(b=>b is CommercialBuilding||b is IndustrialBuilding).Take(3))
            Check(Cached(s,m,h.id,b.id)==s.Transport.EstimateResidentSeconds(h.id,b.id),"Home/work query key reused stale value");
        Check(s.Residents.CachedCommuteEstimates==stored,"Query alternatives grew multiple authoritative records per resident");Check(before.SequenceEqual(CityCoreSave.Write(s)),"Alternative estimate query changed housing/jobs/facts");
        var a=s.Transport.GetAccess(home);RestrictAccess(s,home,AccessUse.Freight);Check(double.IsPositiveInfinity(Cached(s,m,home,work)),"Access restriction ignored");
        RestrictAccess(s,home,AccessUse.All);Check(Cached(s,m,home,work)==s.Transport.EstimateResidentSeconds(home,work),"Restored access estimate stale");
        s.Entities.SetAvailability(work,true,false);Check(double.IsPositiveInfinity(Cached(s,m,home,work)),"Unavailable endpoint bypassed by warm estimate");s.Entities.SetAvailability(work,false,false);Check(Cached(s,m,home,work)==s.Transport.EstimateResidentSeconds(home,work),"Endpoint reopening stale");
        int road=s.Transport.Network.Get(a.lane).RoadId;var settings=s.Transport.Network.Settings(road);s.Transport.PlanUpgradeRoad(road,settings.lanes,24,settings.direction).TryCommit();Check(Cached(s,m,home,work)==s.Transport.EstimateResidentSeconds(home,work),"Road speed edit left stale estimate");
        s.Transport.RemoveRoad(road);Check(Cached(s,m,home,work)==s.Transport.EstimateResidentSeconds(home,work),"Removed road left stale estimate");
        var fresh=CityCoreSave.Read(initial);fresh.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);fresh.Advance(.05);var departing=fresh.Entities.Members.First(x=>x.Person.canWork&&x.Person.jobId>=0&&x.Person.tripId==0);int count=fresh.Residents.CachedCommuteEstimates;Check(count>0,"No actual estimates cached");fresh.Entities.RemoveResident(departing.Person.id);fresh.Residents.AdvanceRequests();Check(fresh.Residents.CachedCommuteEstimates==count-1,"Removed resident estimate retained");fresh.Validate();
    }
#endif
    static void Main(string[] args)
    {
        var initial=File.ReadAllBytes(args[0]);
#if EXPECT_COMMUTE_CACHE
        Lifecycle(initial);
#endif
        var s=CityCoreSave.Read(initial);s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
        for(int i=0;i<1200;i++)
        {
            if(i==100){var road=s.Transport.Network.RoadIds.Last();var settings=s.Transport.Network.Settings(road);s.Transport.PlanUpgradeRoad(road,settings.lanes,18,settings.direction).TryCommit();}
            s.Advance(.05);
            if(i==600){var saved=CityCoreSave.Write(s);var restored=CityCoreSave.Read(saved);restored.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);for(int j=0;j<100;j++){s.Advance(.05);restored.Advance(.05);}Check(CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(restored)),"Warm/restored actual continuation differs");}
        }
        s.Validate();File.WriteAllBytes(args[1],CityCoreSave.Write(s));Console.WriteLine("PASS "+checks+" commute cache/restore checks plus 1300 normal steps; exact old-reference file comparison follows");
    }
}

