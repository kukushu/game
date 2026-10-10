using System;
using System.Linq;
using HarborCity;

public static class BuildingConditionChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Building conditions: "+why);checks++;}
    public static void Run()
    {
        var c=TestCity.Create();c.development.enabled=true;c.waste.enabled=false;UtilitySupplyChecks.ConnectFixture(c);
        var water=c.utilities.water.Copy();var homes=c.buildings.OfType<ResidentialBuilding>().Select(b=>b.id).ToArray();
        int population=c.population,jobs=c.jobs;c.utilities.water=new CityUtilityNetwork();c.Recalculate();
        c.Tick();Check(c.population==population && c.jobs==jobs && c.buildings.All(b=>!b.abandoned),"Short outage does not instantly erase occupants or jobs");
        for(int i=1;i<c.development.provisionalOutageDays;i++)c.Tick();
        Check(c.population==0 && c.Employed==0 && c.jobs==0,"Sustained outage vacates real households and releases concrete jobs");
        Check(c.buildings.Where(b=>CityZoningState.ZoneUse(b.Use)).All(b=>b.abandoned) && homes.All(id=>c.GetBuilding(id)!=null && c.HousingCapacity(id)==0),"Abandoned entities remain with stable IDs but offer no usable housing");
        Check(c.Valid() && TestCity.RoundTrip(c).Valid(),"Abandoned city relationships and dates save correctly");
        var traffic=new CityTraffic(c);int shop=c.buildings.OfType<CommercialBuilding>().First().id;
        Check(traffic.Dispatch(-1,shop,TripPurpose.Import)==null,"Abandoned commerce cannot receive new import activity");
        c.utilities.water=water;c.Recalculate();int abandonedDay=c.day;
        for(int i=0;i<CityModel.AbandonedRecoveryDays-1;i++)c.Tick();
        Check(c.population==0 && c.buildings.Where(b=>CityZoningState.ZoneUse(b.Use)).All(b=>b.abandoned),"Supply restoration alone cannot bypass the four-week minimum");
        c.Tick();Check(c.day-abandonedDay==28 && c.population>0 && c.jobs>0 && homes.All(id=>!c.GetBuilding(id).abandoned),"Restored buildings re-open after minimum, then actual families return and concrete jobs are regenerated");
        Check(c.Valid() && TestCity.RoundTrip(c).population==c.population,"Recovery and migration survive saving without phantom population");
        var damaged=TestCity.RoundTrip(c);damaged.buildings[0].abandoned=true;
        Check(!damaged.Valid(),"Inconsistent abandoned flag and date rejected");
        Console.WriteLine("PASS: "+checks+" outage/abandonment/recovery/entity-save checks");
    }
}
