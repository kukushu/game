using System;
using System.Linq;
using HarborCity;
public static class TrafficChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Traffic: "+why);checks++;}
    public static void Run()
    {
        var c=TestCity.Create();c.society.settings.applicantsPerDay=0;
        foreach(var p in c.Citizens)c.ReleaseJob(p);foreach(var h in c.society.families)h.nextReview=10000;
        var factory=c.buildings.OfType<IndustrialBuilding>().First();var shop=c.buildings.OfType<CommercialBuilding>().First();
        var sim=new CityTraffic(c);c.traffic.dispatchTimer=-10000;
        factory.goodsStock=8;var t=sim.Dispatch(factory.id,shop.id,TripPurpose.Delivery);
        Check(t!=null && factory.goodsStock==0 && shop.stock==0,"Only cargo ownership changes at dispatch");
        var before=CityRoads.Lerp(c.roads.Node(t.Current),c.roads.Node(t.Next),t.progress);
        c.roads.Remove(c.roads.edges.Select(e=>e.id).ToList());sim.Advance(2);
        Check(t.cargo==8 && t.status==TripStatus.Waiting && shop.stock==0 && factory.factory.salesRevenue==0,"Disconnected road cannot deliver or pay seller");
        sim.Advance(121);Check(!c.traffic.trips.Contains(t) && factory.goodsStock==8 && c.traffic.failed==1,"Failed goods task restores supplier-owned stock");
        c=TestCity.Create();sim=new CityTraffic(c);sim.Advance(45);c.traffic.dispatchTimer=0;
        var clone=TestCity.RoundTrip(c);var resumed=new CityTraffic(clone);
        sim.Advance(30);resumed.Advance(30);
        Check(c.Valid() && clone.Valid() && c.traffic.completed==clone.traffic.completed && c.traffic.delivered==clone.traffic.delivered,"Loaded in-flight tasks continue with stable entity IDs");
        Check(c.buildings.OfType<IGoodsBuilding>().Select(b=>b.Stock).SequenceEqual(clone.buildings.OfType<IGoodsBuilding>().Select(b=>b.Stock)),"Resume preserves both stock owners");
        Check(c.society.families.SelectMany(h=>h.people).Select(p=>p.tripId).SequenceEqual(clone.society.families.SelectMany(h=>h.people).Select(p=>p.tripId)),"Resume preserves resident-trip relationships");
        Check(sim.Dispatch(999999,shop.id,TripPurpose.Delivery)==null,"Unknown stable IDs reject dispatch");
        Console.WriteLine("PASS: "+checks+" traffic lifecycle checks");
    }
}
