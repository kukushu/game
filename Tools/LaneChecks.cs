using System;
using System.Linq;
using HarborCity;
public static class LaneChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Lanes: "+why);checks++;}
    static float Distance(CityModel c,TrafficTrip t)
    {float d=0;for(int i=0;i<t.segment;i++)d+=c.roads.EdgeLength(t.route[i],t.route[i+1]);return d+t.progress*c.roads.EdgeLength(t.Current,t.Next);}
    public static void Run()
    {
        var c=RoadDirectionChecks.Loop();var road=c.roads.edges.First(e=>e.oneWay);var lanes=c.roads.Lanes(road.a,road.b).ToList();
        Check(lanes.Count==2 && lanes[0].id!=lanes[1].id && Math.Abs(lanes[0].offset-lanes[1].offset)>1,"One-way small road exposes two separate stable physical lanes");
        Check(!c.roads.Lanes(road.b,road.a).Any(),"One-way road has no reverse vehicle lane");
        var two=c.roads.edges.First(e=>!e.oneWay);Check(c.roads.Lanes(two.a,two.b).Count()==1 && c.roads.Lanes(two.b,two.a).Count()==1,"Two-way road has one lane per direction");
        var saved=TestCity.RoundTrip(c);Check(lanes.Select(l=>l.id).SequenceEqual(saved.roads.Lanes(road.a,road.b).Select(l=>l.id)),"Derived lane IDs survive road saves");
        int origin=TestCity.Build(c,-35,7,LandUse.Industrial);c.Inventory(origin).Stock=24;
        var sim=new CityTraffic(c);var a=sim.Dispatch(origin,CityTraffic.Outside,TripPurpose.Export);var b=sim.Dispatch(origin,CityTraffic.Outside,TripPurpose.Export);
        Check(a!=null && b!=null && a.lane!=b.lane && a.Current==b.Current && a.Next==b.Next,"Two real cargo vehicles depart simultaneously through different lanes");
        Check(sim.Dispatch(origin,CityTraffic.Outside,TripPurpose.Export)==null && c.Inventory(origin).Stock==8,"A third truck waits when both physical departure lanes are occupied");
        sim.Advance(.25f);Check(a.lane!=b.lane && Math.Abs(Distance(c,a)-Distance(c,b))<.001f && a.blocked==0 && b.blocked==0,"Parallel same-direction freight does not follow one shared queue");
        int aId=a.id,bId=b.id;var before=CityRoads.Lerp(c.roads.Node(b.Current),c.roads.Node(b.Next),b.progress);
        var mid=CityRoads.Lerp(c.roads.Node(b.Current),c.roads.Node(b.Next),.5f);
        var branch=c.roads.Plan(c,mid,TestCity.P(mid.x,-15),TestCity.Flat);
        Check(c.CommitRoad(branch) && a.lane!=b.lane && CityRoads.Length(before,CityRoads.Lerp(c.roads.Node(b.Current),c.roads.Node(b.Next),b.progress))<.001f,"Splitting an occupied one-way road preserves each vehicle's lane and position");
        saved=TestCity.RoundTrip(c);var savedSim=new CityTraffic(saved);
        Check(saved.traffic.trips.First(t=>t.id==bId).lane==b.lane && saved.Valid(),"The second lane survives in-flight JSON save");
        bool waited=false,overlap=false;
        for(int i=0;i<2400;i++)
        {
            sim.Advance(.05f);savedSim.Advance(.05f);waited |= a.blocked>0 || b.blocked>0;
            if(!a.returning && !b.returning && a.status==TripStatus.Driving && b.status==TripStatus.Driving && a.lane==b.lane && a.route.SequenceEqual(b.route))
                overlap |= Math.Abs(Distance(c,a)-Distance(c,b))<1.439f;
        }
        Check(waited && !overlap,"Two lanes merge into the entrance's one lane with safe headway and actual yielding");
        Check(c.traffic.trips.All(t=>t.id!=aId && t.id!=bId) && c.traffic.completed>=2 && c.traffic.failed==0 && c.traffic.lostGoods==0 && c.Valid(),"Both exports and return trips complete without deleting or losing cargo");
        Check(TestCity.Snapshot(saved)==TestCity.Snapshot(c),"Saved parallel freight and lane merge continue identically");
        var bad=TestCity.RoundTrip(c);bad.traffic.trips.Add(new TrafficTrip{id=bad.traffic.nextId++,origin=origin,home=origin,destination=CityTraffic.Outside,route=sim.FindRoute(origin,CityTraffic.Outside),lane=2});
        Check(!bad.Valid(),"Invalid third lane is rejected");
        c=TestCity.Create();sim=new CityTraffic(c);var person=c.Citizens.First(p=>p.jobId>0);c.society.dayElapsed=600f/1440*c.society.settings.secondsPerDay;sim.Advance(.05f);a=c.traffic.trips.FirstOrDefault(t=>t.residentId==person.id && t.purpose==TripPurpose.Commute);
        Check(a!=null && a.lane==0 && c.Valid(),"Existing real residents on two-way roads remain valid single-lane traffic");
        Console.WriteLine("PASS: "+checks+" actual lanes/parallel exports/merge/headway/split/save checks");
    }
}
