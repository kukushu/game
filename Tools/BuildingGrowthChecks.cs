using System;
using System.Linq;
using HarborCity;

public static class BuildingGrowthChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Growth: "+why);checks++;}
    static CityModel Create()
    {
        var c=TestCity.Create();c.development.enabled=true;UtilitySupplyChecks.ConnectFixture(c);c.waste.enabled=false;c.fire.chancePerBuildingDay=0;c.police.dailyBase=c.police.unemploymentWeight=c.police.servicePressureWeight=0;c.society.settings.applicantsPerDay=0;
        foreach(var b in c.buildings.OfType<IndustrialBuilding>()){b.factory.noise=b.factory.pollution=0;}
        return c;
    }
    static void Services(CityModel c)
    {
        c.money+=50000;TestCity.Build(c,-30,-5,LandUse.Clinic);TestCity.Build(c,-36,5,LandUse.ElementarySchool);
        TestCity.Build(c,-54,-5,LandUse.FireHouse);TestCity.Build(c,-24,5,LandUse.PoliceStation);TestCity.Build(c,-50,5,LandUse.Landfill);TestCity.Build(c,-43,-5,LandUse.Park);c.Recalculate();
    }
    static void Days(CityModel c,int n){for(int i=0;i<n;i++)c.Tick();}
    public static void Run()
    {
        var c=Create();var home=c.buildings.OfType<ResidentialBuilding>().First();int id=home.id;
        Check(c.LandValue(id)==35 && c.ReachableServiceKinds(id)==0,"Connected supply supports land value without fabricated public services");
        float initial=c.LandValue(id);Services(c);
        Check(c.ReachableServiceKinds(id)==6 && c.LandValue(id)>initial,"Actual reachable, supplied distinct service types improve local land value");
        float serviced=c.LandValue(id);TestCity.Build(c,0,-5,LandUse.Park);Check(c.LandValue(id)==serviced,"Duplicating a service type does not invent another service category");
        home.crime=80;Check(c.LandValue(id)<serviced && Math.Abs(c.MapIndicator(id,CityMapView.LandValue)-c.LandValue(id)/100)<.00001f,"Crime reduces derived land value and map reads the same state: "+serviced+" / "+c.LandValue(id)+" / "+c.MapIndicator(id,CityMapView.LandValue)+" / "+home.crime);home.crime=0;
        var source=c.buildings.OfType<IndustrialBuilding>().First();source.factory.pollution=1;float polluted=c.LandValue(id);
        Check(polluted<serviced,"Actual pollution activity lowers local value");source.factory.pollution=0;
        var neighbor=c.buildings.OfType<ResidentialBuilding>().Skip(1).First();neighbor.abandoned=true;neighbor.abandonedDay=c.day;
        Check(c.LandValue(id)<serviced,"Nearby actual ruins or abandonment reduce local value");neighbor.abandoned=false;neighbor.abandonedDay=0;
        Days(c,3);Check(home.level==1 && home.upgradeDays==0 && c.BuildingUpgradeReason(id).Contains("教育"),"Nearby schools and legacy skills do not fabricate residents' education or upgrades");
        foreach(var person in c.society.families.First(h=>h.home==id).people)person.education=3;
        int units=home.housingUnits,people=c.population;float x=home.x,z=home.z;var member=c.society.families.First(h=>h.home==id).people.First();int oldJob=member.jobId;
        Days(c,2);Check(home.level==1 && home.upgradeDays==2,"An upgrade requires actual consecutive qualifying days");
        var saved=TestCity.RoundTrip(c);saved.Tick();c.Tick();
        Check(home.level==2 && home.housingUnits==units+1 && c.population==people && home.id==id && home.x==x && home.z==z && member.jobId==oldJob,"Upgrade preserves stable pose, residents and jobs, creates housing capacity without creating people");
        Check(saved.GetBuilding(id).level==2 && TestCity.Snapshot(saved)==TestCity.Snapshot(c),"Saved partial qualification resumes deterministically");
        Days(c,9);Check(home.level==5 && home.housingUnits==units+4 && c.Valid(),"Qualified residential building progresses through the current five-level catalogue");
        Days(c,6);Check(home.level==5 && home.upgradeDays==0,"Maximum level does not accumulate invalid upgrade progress");
        c=Create();Services(c);home=c.buildings.OfType<ResidentialBuilding>().First();id=home.id;
        foreach(var p in c.society.families.First(h=>h.home==id).people)p.education=3;Days(c,2);home.crime=60;c.Tick();
        Check(home.level==1 && home.upgradeDays==0,"Real service or crime failure cancels accumulated qualification");
        home.crime=0;Days(c,3);Check(home.level==2,"Restored actual conditions qualify afresh");
        c=Create();Services(c);var factory=c.buildings.OfType<IndustrialBuilding>().First();int business=factory.id;
        foreach(var p in c.Citizens.Where(p=>c.Workplace(p)==business))p.education=3;
        var jobs=c.society.jobEntities.Where(j=>j.buildingId==business).ToList();int occupied=c.EmployedAt(business),employed=c.Employed;
        Days(c,3);Check(factory.level==2 && c.JobCapacity(business)==8 && c.society.jobEntities.Any(j=>jobs.Any(old=>old.id==j.id && old.occupiedCitizenId==j.occupiedCitizenId)) && c.Employed>=employed,"Business upgrade creates concrete additional Job entities and preserves existing contracts");
        Days(c,3);Check(factory.level==3 && c.JobCapacity(business)==12 && c.Valid(),"Higher industrial level remains a stable building with finite real jobs");
        var worker=c.Citizens.First(p=>c.Workplace(p)==business);worker.location=business;worker.atWork=true;worker.tripId=0;worker.arrivedDay=c.day;worker.accessNode=c.AccessBuilding(business).First();
        var state=factory.factory;state.raw=1;state.imported=1;c.ProcessFactoryWork(worker,40);
        Check(state.produced==1 && state.consumed==1 && state.productiveMinutes==40 && state.attendanceMinutes==40,"Higher level raises actual processing rate, preserves real raw input and credited labour");
        state.stepProductive=4;state.stepAttendance=4;c.AdvanceIndustry(1);float higherPollution=state.pollution;state.noise=state.pollution=0;factory.level=1;state.stepProductive=4;state.stepAttendance=4;c.AdvanceIndustry(1);
        Check(higherPollution<state.pollution,"Higher-level industry emits less pollution for equal actual processing activity");
        c=Create();home=c.buildings.OfType<ResidentialBuilding>().First();home.upgradeDays=CityModel.UpgradeQualifyingDays;Check(!c.Valid(),"Malformed saved upgrade progress is rejected");
        Console.WriteLine("PASS: "+checks+" land value/education/upgrade/housing/jobs/industry/save checks");
    }
}
