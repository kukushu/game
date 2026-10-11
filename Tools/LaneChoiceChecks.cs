using System;
using System.Linq;
using HarborCity;

public static class LaneChoiceChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Lane choice: "+why);checks++;}
    static CityModel Street(out CityTraffic sim,out TrafficTrip follower,out TrafficTrip leader)
    {
        var c=TestCity.Empty();
        if(!c.CommitRoad(c.roads.Plan(c,c.roads.Node(CityRoads.Entrance),TestCity.P(45,1.5f),TestCity.Flat,false,RoadKind.FourLane)))throw new Exception("Wide fixture");
        int factory=TestCity.Build(c,35,7,LandUse.Industrial);c.Inventory(factory).Stock=24;
        sim=new CityTraffic(c);follower=sim.Dispatch(factory,CityTraffic.Outside,TripPurpose.Export);leader=sim.Dispatch(factory,CityTraffic.Outside,TripPurpose.Export);
        if(follower==null || leader==null)throw new Exception("Real export fixture");
        Place(c,follower,10,0);Place(c,leader,8,0);
        return c;
    }
    static void Place(CityModel c,TrafficTrip t,float x,int lane)
    {
        for(int s=0;s<t.route.Count-1;s++)
        {
            var a=c.roads.Node(t.route[s]);var b=c.roads.Node(t.route[s+1]);
            if(x>=Math.Min(a.x,b.x) && x<Math.Max(a.x,b.x)){t.segment=s;t.progress=(a.x-x)/(a.x-b.x);t.lane=t.previousLane=t.laneChangeFrom=lane;t.laneChangeRemaining=t.laneChangeCooldown=0;return;}
        }
        throw new Exception("No fixture position "+x);
    }
    static float X(CityModel c,TrafficTrip t)=>CityRoads.Lerp(c.roads.Node(t.Current),c.roads.Node(t.Next),t.progress).x;
    public static void Run()
    {
        var c=Street(out var sim,out var t,out var lead);float beforeX=X(c,t);
        Check(c.roads.TripLaneOffset(new TrafficTrip{route=new System.Collections.Generic.List<int>{CityRoads.Entrance}},0)==.55f
            && c.roads.TripLaneOffset(t,t.route.Count-1)==.55f,"Stopped single-node and terminal-node vehicles render without indexing past their routes");
        sim.Advance(.05f);
        Check(t.lane==1 && t.laneChangeFrom==0 && t.laneChangeRemaining>0,"Real blocked export starts changing to the available lane");
        Check(Math.Abs(X(c,t)-beforeX)<1 && c.Valid(),"Changing does not teleport longitudinal position or invalidate cargo");
        float offset=c.roads.TripLaneOffset(t,t.segment);
        Check(offset>=.55f && offset<1.65f,"Start of change renders from the old lane rather than jumping to target");
        sim.Advance(.05f);float during=c.roads.TripLaneOffset(t,t.segment);
        Check(during>offset && during<1.65f,"Lateral position advances continuously with simulation time");
        var saved=TestCity.RoundTrip(c);var copy=new CityTraffic(saved);int id=t.id;
        Check(saved.Valid() && saved.traffic.trips.First(p=>p.id==id).laneChangeRemaining==t.laneChangeRemaining,"In-progress lane reservation survives save reload");
        var splitAt=CityRoads.Lerp(c.roads.Node(t.Current),c.roads.Node(t.Next),.5f);
        var branch=c.roads.Plan(c,splitAt,TestCity.P(splitAt.x,20),TestCity.Flat,false,RoadKind.FourLane);
        float x=X(c,t);Check(c.CommitRoad(branch) && Math.Abs(X(c,t)-x)<.001f && t.laneChangeRemaining>0 && c.Valid(),"Occupied road split retains position and both lane reservations");
        saved=TestCity.RoundTrip(c);copy=new CityTraffic(saved);sim=new CityTraffic(c);
        sim.Advance(15);copy.Advance(15);
        Check(c.traffic.failed==0 && c.traffic.lostGoods==0 && c.traffic.completed>=2 && c.traffic.trips.All(p=>p.id!=id) && c.Valid(),"Changed exports and merged returns physically complete without cargo loss");
        Check(TestCity.Snapshot(c)==TestCity.Snapshot(saved),"Saved change and road edit continue deterministically");
        c=Street(out sim,out t,out lead);
        int factory=t.origin;var behind=sim.Dispatch(factory,CityTraffic.Outside,TripPurpose.Export);Place(c,behind,13,1);
        sim.Advance(.05f);Check(t.lane==0 && t.laneChangeRemaining==0,"A close target-lane rear vehicle prevents cutting in");
        c=Street(out sim,out t,out lead);var front=sim.Dispatch(t.origin,CityTraffic.Outside,TripPurpose.Export);Place(c,front,7,1);
        sim.Advance(.05f);Check(t.lane==0 && t.laneChangeRemaining==0,"A close target-lane front vehicle prevents overlapping");
        c=Street(out sim,out t,out lead);var changing=sim.Dispatch(t.origin,CityTraffic.Outside,TripPurpose.Export);Place(c,changing,13,0);
        changing.laneChangeFrom=1;changing.laneChangeRemaining=.3f;changing.laneChangeCooldown=1;
        sim.Advance(.05f);Check(t.lane==0,"Another changing vehicle still occupies its source lane");
        c=Street(out sim,out t,out lead);sim.Advance(.05f);int edge=c.roads.edges.First(e=>e.a==t.Current && e.b==t.Next || e.b==t.Current && e.a==t.Next).id;
        Check(!c.UpgradeRoad(edge,RoadKind.Standard,false,out _,TestCity.Flat) && c.Valid(),"Occupied changing road cannot be narrowed underneath its vehicles");
        var legacy=c.ToSaveData();legacy.format=31;var old=legacy.ToCity();Check(old.Valid() && old.traffic.trips.All(p=>p.laneChangeRemaining==0) && old.traffic.trips.First(p=>p.id==t.id).lane==t.lane,"Format 31 migration preserves actual target lane and adds no fictitious change");
        c=Street(out sim,out t,out lead);t.laneChangeRemaining=float.NaN;Check(!c.Valid(),"Invalid saved lateral progress is rejected");
        c=RoadDirectionChecks.Loop();
        Check(c.CommitRoad(c.roads.Plan(c,TestCity.P(30,1.5f),TestCity.P(45,1.5f),TestCity.Flat,true)),"One-way real turning junction fixture");
        factory=TestCity.Build(c,-35,7,LandUse.Industrial);c.Inventory(factory).Stock=8;sim=new CityTraffic(c);t=sim.Dispatch(factory,CityTraffic.Outside,TripPurpose.Export);
        Place(c,t,10,0);sim=new CityTraffic(c);sim.Advance(.05f);
        Check(t.lane==1 && t.laneChangeRemaining>0,"One-way left turn selects lane 1, which is physically left rather than copying wide-road indices");
        Turn(false);Turn(true);
        Console.WriteLine("PASS: "+checks+" real turn planning/safe changes/dual occupancy/split/cargo/save checks");
    }
    static void Turn(bool left)
    {
        var c=TestCity.Empty();c.CommitRoad(c.roads.Plan(c,c.roads.Node(CityRoads.Entrance),TestCity.P(45,1.5f),TestCity.Flat,false,RoadKind.FourLane));
        if(!c.CommitRoad(c.roads.Plan(c,TestCity.P(0,1.5f),TestCity.P(0,left?-30:30),TestCity.Flat,false,RoadKind.FourLane)))throw new Exception("Turn fixture");
        int factory=TestCity.Build(c,35,7,LandUse.Industrial),shop=TestCity.Build(c,-6,left?-20:20,LandUse.Commercial);c.Inventory(factory).Stock=8;
        var sim=new CityTraffic(c);var t=sim.Dispatch(factory,shop,TripPurpose.Delivery);c.Inventory(shop).Stock=8;
        Place(c,t,21,left?1:0);sim.Advance(.05f);
        Check(t.lane==(left?0:1) && t.laneChangeRemaining>0,(left?"Left":"Right")+" turn chooses the physical inner/outer lane ahead of the real junction");
        sim.Advance(.7f);Check(t.laneChangeRemaining==0 && t.lane==(left?0:1),"Turn lane change finishes before reaching intersection");
        Check(t.laneChangeCooldown>0,"Completed lane change retains a cooldown against oscillation");
        sim.Advance(15);Check(c.Goods(shop)==16 && c.traffic.failed==0 && c.traffic.lostGoods==0 && c.Valid(),"Actual turn delivery and return remain valid");
        c.Inventory(factory).Stock=8;t=sim.Dispatch(factory,shop,TripPurpose.Delivery);Place(c,t,5,left?1:0);sim=new CityTraffic(c);sim.Advance(.05f);
        Check(t.lane==(left?1:0) && t.laneChangeRemaining==0,"No lateral change starts inside the actual intersection/crosswalk area");
    }
}
