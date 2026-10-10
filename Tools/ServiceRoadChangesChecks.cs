using System;
using System.Linq;
using HarborCity;

public static class ServiceRoadChangesChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Service road edits: "+why);checks++;}
    static void Until(CityTraffic sim,Func<bool> done){for(int n=0;n<6000 && !done();n++)sim.Advance(.05f);}
    public static void Run()
    {
        foreach(bool fire in new[]{false,true})
        {
            var city=TestCity.Create();city.development.enabled=true;UtilitySupplyChecks.ConnectFixture(city);city.waste.enabled=false;city.fire.chancePerBuildingDay=0;city.society.settings.applicantsPerDay=0;
            foreach(var p in city.Citizens)city.ReleaseJob(p);city.money+=12000;
            int station=TestCity.Build(city,40,-5,fire?LandUse.FireHouse:LandUse.PoliceStation);var home=city.buildings.OfType<ResidentialBuilding>().First();
            if(fire)city.IgniteBuilding(home.id);else home.crime=90;
            var sim=new CityTraffic(city);var trip=fire?sim.DispatchFire(station,home.id):sim.DispatchPolice(station,home.id);
            Until(sim,()=>trip.status==TripStatus.Visiting);Check(trip.status==TripStatus.Visiting,"Real service car arrives before road editing");
            int previous=trip.Current;float value=fire?home.fireIntensity:home.crime;
            var junction=TestCity.P(home.entranceX,1.5f);var end=TestCity.P(home.entranceX,-10);
            var plan=city.roads.Plan(city,junction,end,TestCity.Flat);
            Check(city.CommitRoad(plan),"Actual junction construction splits the former frontage edge: "+plan.error);
            Check(!city.AccessBuilding(home.id).Contains(previous) && trip.Current==previous,"New entrance is distinct and the stopped car has not teleported");
            city.AdvanceFires(.1f);city.AdvancePolice(.1f);
            Check(fire?home.fireIntensity>=value:home.crime==value,"Former entrance supplies no remote response after actual road editing");
            sim.Advance(.05f);
            Check(trip.status!=TripStatus.Visiting && trip.Current==previous && !trip.returning && trip.destination==home.id,"Original task resumes from its physical node toward the new entrance");
            var saved=TestCity.RoundTrip(city);var originalId=trip.id;city=saved;sim=new CityTraffic(city);trip=city.traffic.trips.Single(t=>t.id==originalId);home=city.Residence(home.id);
            Check(city.Valid() && trip.status!=TripStatus.Visiting && trip.Current==previous,"Reapproaching the changed entrance survives saving");
            Until(sim,()=>trip.status==TripStatus.Visiting);
            Check(trip.status==TripStatus.Visiting && city.AccessBuilding(home.id).Contains(trip.Current) && trip.Current!=previous,"Same car actually reaches the updated road entrance");
            Until(sim,()=>!city.traffic.trips.Contains(trip));
            Check(city.Valid() && (fire?!home.burning && !home.burned:home.crime==0),"Existing response completes at new access and returns normally");
        }
        Console.WriteLine("PASS: "+checks+" on-scene service/actual junction/entrance/reapproach/save checks");
    }
}
