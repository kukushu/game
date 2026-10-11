using System;
using System.Linq;
using HarborCity;
public static class RouteReplanChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Route replanning: "+why);checks++;}
    public static void Run()
    {
        foreach(bool reverse in new[]{false,true})
        {
            var c=TestCity.Create();
            foreach(var p in c.Citizens)c.ReleaseJob(p);
            foreach(var h in c.society.families)h.nextReview=10000;
            c.traffic.dispatchTimer=-10000;c.society.settings.applicantsPerDay=0;
            var sim=new CityTraffic(c);var factory=c.buildings.OfType<IndustrialBuilding>().First();
            var shops=c.buildings.OfType<CommercialBuilding>().ToArray();factory.goodsStock=8;
            var t=sim.Dispatch(factory.id,shops[0].id,TripPurpose.Delivery);
            Check(t!=null && t.route.Count>4,"Actual loaded truck has a usable route");
            t.segment=2;t.progress=0;t.status=TripStatus.Waiting;t.blocked=31;
            if(reverse)t.destination=shops[1].id;
            else t.route.RemoveAt(t.route.Count-1);
            var prefix=t.route.Take(t.segment+1).ToArray();int node=t.Current,id=t.id,oldNext=t.Next;
            var follower=new TrafficTrip{id=c.traffic.nextId++,origin=factory.id,home=factory.id,destination=shops[0].id,
                route=new System.Collections.Generic.List<int>(prefix),segment=1,progress=.75f,status=TripStatus.Driving};
            c.traffic.trips.Add(follower);
            // The incoming follower is physically near the node and cannot pass
            // this truck. It must not veto changing the truck's remaining route.
            follower.progress=1-.3f/(c.roads.EdgeLength(follower.Current,follower.Next)/3);
            c.traffic.dispatchTimer=0;
            var saved=TestCity.RoundTrip(c);var resumed=new CityTraffic(saved);
            sim.Advance(.05f);resumed.Advance(.05f);
            Check(t.id==id && t.Current==node && t.status==TripStatus.Driving && t.progress>0,"Replan moves existing truck despite incoming follower");
            Check(t.route.Take(prefix.Length).SequenceEqual(prefix) && t.cargo==8,"Travelled prefix and cargo ownership survive replan");
            Check(reverse?t.Next!=oldNext:t.Next==oldNext,"Changed destination can reverse; intact direction remains intact");
            Check(TestCity.Snapshot(c)==TestCity.Snapshot(saved),"Waiting snapshot resumes deterministically");
            c.traffic.dispatchTimer=saved.traffic.dispatchTimer=-10000;
            sim.Advance(20);resumed.Advance(20);
            c.traffic.dispatchTimer=saved.traffic.dispatchTimer=0;
            Check(c.traffic.delivered==8 && c.traffic.failed==0 && t.cargo==0 && !c.traffic.trips.Contains(t),"Truck actually delivers and returns without deleting stalled task: reverse="+reverse+", delivered="+c.traffic.delivered+", failed="+c.traffic.failed+", cargo="+t.cargo+", active="+c.traffic.trips.Contains(t));
            Check(c.Valid() && TestCity.Snapshot(c)==TestCity.Snapshot(saved),"Route with repeated nodes remains valid and deterministic");
        }
        Console.WriteLine("PASS: "+checks+" route replanning checks");
    }
}
