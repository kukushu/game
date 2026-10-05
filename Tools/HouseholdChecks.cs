using System;
using System.Linq;
using HarborCity;
public static class HouseholdChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Households: "+why);checks++;}
    public static void Run()
    {
        var c=TestCity.Create();var sim=new CityTraffic(c);
        Check(c.Valid() && c.Citizens.Select(p=>p.id).Distinct().Count()==c.Citizens.Count(),"Independent resident identities");
        var h=c.society.families.First();var current=c.Evaluate(h,h.home,keepJobs:true);
        Check(current.citizenIds.Count==h.people.Count && current.jobIds.Count==h.people.Count,"Per-resident candidate job plans");
        Check(c.HousingCapacity(h.home)==8 && c.Occupancy(h.home)==1,"Capacity and occupancy use residential subtype");
        int shop=c.buildings.OfType<CommercialBuilding>().First().id;
        Check(c.HousingCapacity(shop)==0 && c.Occupancy(shop)==0 && c.Evaluate(h,shop).rejection!="","Commercial building cannot be housing");
        c.money=100000;int villa=TestCity.Build(c,38,7,LandUse.Residential,HousingKind.Villa);
        Check(c.HousingCapacity(villa)==1 && c.Residence(villa).housing==HousingKind.Villa,"Villa retains its distinct housing data");
        var privateHome=c.Evaluate(h,villa,true);Check(privateHome.privacyScore>current.privacyScore,"Existing housing preferences remain meaningful");
        for(int day=0;day<365;day++)
        {
            while(c.day<day+2) sim.Advance(.1f);
            if(!c.Valid()){System.IO.File.WriteAllText("Temp/InvalidCity.json",TestCity.Snapshot(c)); throw new Exception("Invalid long-run day "+c.day+" traffic="+CityTraffic.Valid(c)+" households="+c.ValidHouseholds()+" jobs="+c.ValidJobs()+" industry="+c.ValidIndustry());}
            var r=c.society.history.Last();
            if(r.closingSavings!=r.openingSavings+r.wages-r.rent-r.living-r.travel-r.movingCosts || r.closingTreasury!=r.openingTreasury+r.rent-r.maintenance)throw new Exception("Cash mismatch day "+c.day);
            if(c.society.families.Where(f=>f.resident).GroupBy(f=>f.home+":"+f.unit).Any(g=>g.Count()>1))throw new Exception("Duplicate housing unit");
        }
        Check(c.day==366,"365 days of actual travel, capacity and economic conservation");
        Check(c.society.history.Any(r=>r.wages>0) && c.Citizens.Any(p=>p.lastCommute>0),"Real arrivals and paid work continue long-term");
        Check(TestCity.RoundTrip(c).Valid(),"Long-running city serializes with concrete entities");
        Console.WriteLine("PASS: "+checks+" household checks, 365-day conservation/capacity run");
    }
}


