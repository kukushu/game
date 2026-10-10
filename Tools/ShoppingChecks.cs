using System;
using System.Linq;
using HarborCity;

public static class ShoppingChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Shopping: "+why);checks++;}
    static CityModel Fixture(out Household h,out CityResident p,out CommercialBuilding shop)
    {
        var c=TestCity.Create();c.development.enabled=true;c.waste.enabled=false;UtilitySupplyChecks.ConnectFixture(c);c.society.settings.applicantsPerDay=0;
        h=c.society.families.First();p=h.people.First();shop=c.buildings.OfType<CommercialBuilding>().Last();shop.stock=8;
        // Isolate explicit shopping trips from work departures and freight scheduling.
        c.society.settings.secondsPerDay=1000;c.society.dayElapsed=800;c.traffic.clock=800;
        foreach(var family in c.society.families){family.shoppingDay=c.day;foreach(var person in family.people)person.departureDay=c.day;}
        return c;
    }
    static void ReachShop(CityTraffic sim,TrafficTrip t)
    {for(int i=0;i<1000 && t.status!=TripStatus.Visiting;i++)sim.Advance(.05f);}
    static void ReachHome(CityTraffic sim,CityResident p)
    {for(int i=0;i<2000 && p.tripId!=0;i++)sim.Advance(.05f);}
    public static void Run()
    {
        var c=Fixture(out var h,out var p,out var shop);var sim=new CityTraffic(c);int employed=c.Employed,job=p.jobId;float commute=p.lastCommute;
        var t=sim.DispatchShopping(h.home,shop.id,p.id);
        Check(t!=null && t.cargo==0 && shop.stock==8 && p.tripId==t.id,"Actual departure does not consume stock");
        c.analysis=new CityDailyAnalysis(c);Check(c.analysis.Refresh(true).currentDay.pendingCommuters==0,"A shopping passenger is not reported as a pending worker commute");
        Check(sim.DispatchShopping(h.home,shop.id,p.id)==null && sim.DispatchShopping(h.home,shop.id,h.people.Last().id)==null,"No duplicate shopper or child trip");
        ReachShop(sim,t);
        Check(t.status==TripStatus.Visiting && shop.stock==7 && t.cargo==1 && shop.retailSold==1 && shop.customers==1 && c.traffic.consumedGoods==0,"Only actual arrival transfers inventory into passenger cargo");
        var observed=c.ObserveTransport(h,p);Check(observed.building==shop.id && !observed.travelling && observed.state=="到店购物","Stopped shopper is located inside the real destination");
        var saved=TestCity.RoundTrip(c);Check(saved.Valid() && saved.traffic.trips.Single(trip=>trip.residentId==p.id).cargo==1 && ((CommercialBuilding)saved.GetBuilding(shop.id)).retailSold==1,"Native DTO carries retail counters and shopping cargo");
        ReachHome(new CityTraffic(saved),saved.Citizens.First(person=>person.id==p.id));
        Check(saved.traffic.consumedGoods==1 && saved.traffic.trips.All(trip=>trip.residentId!=p.id) && saved.Citizens.First(person=>person.id==p.id).location==h.home,"Saved shopper returns through roads before household consumption");
        ReachHome(sim,p);
        Check(c.Employed==employed && p.jobId==job && p.lastCommute==commute && p.workedMinutes==0,"Shopping does not count as employment, attendance or commute samples");
        Check(c.traffic.consumedGoods==1 && p.tripId==0 && p.location==h.home && c.Valid(),"Physical household consumption closes the trip");
        c=Fixture(out h,out p,out shop);sim=new CityTraffic(c);t=sim.DispatchShopping(h.home,shop.id,p.id);shop.stock=0;ReachShop(sim,t);
        Check(t.cargo==0 && shop.retailSold==0 && shop.customers==1,"Running out before arrival records an empty actual visit");
        ReachHome(sim,p);Check(c.traffic.consumedGoods==0 && p.tripId==0,"Empty return cannot manufacture consumption");
        c=Fixture(out h,out p,out shop);sim=new CityTraffic(c);t=sim.DispatchShopping(h.home,shop.id,p.id);ReachShop(sim,t);int at=t.Current;
        Check(c.DemolishBuilding(shop.id) && t.Current==at && t.returning && t.status==TripStatus.Driving && t.cargo==1,"Demolished shop redirects a stopped shopper from its actual node");
        ReachHome(sim,p);Check(c.traffic.consumedGoods==1 && c.Valid(),"Purchased goods survive shop demolition and actual return");
        c=Fixture(out h,out p,out shop);sim=new CityTraffic(c);t=sim.DispatchShopping(h.home,shop.id,p.id);ReachShop(sim,t);
        Check(c.DemolishBuilding(h.home) && c.traffic.lostGoods==1 && c.traffic.consumedGoods==0 && !c.traffic.trips.Contains(t),"Removal of the receiving household explicitly accounts for carried goods");
        var invalid=TestCity.RoundTrip(c);invalid.traffic.consumedGoods=-1;Check(!invalid.Valid(),"Malformed negative household consumption is rejected");
        Console.WriteLine("PASS: "+checks+" actual shopping/inventory/resident/save/demolition checks");
    }
}
