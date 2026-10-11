using System;
using System.Linq;
using HarborCity;
public static class TrafficControlChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Traffic controls: "+why);checks++;}
    static int Center(CityModel c)=>c.roads.nodes.First(n=>Math.Abs(n.x)<.01f && Math.Abs(n.z-1.5f)<.01f).id;
    static int Arm(CityModel c,int node,float x,float z)=>c.roads.Neighbors(node).OrderBy(from=>CityRoads.Length(c.roads.Node(from),TestCity.P(x,z))).First();
    static void Approach(CityModel c,TrafficTrip t,int node)
    {int i=t.route.IndexOf(node);t.segment=i-1;t.progress=1-.4f/(c.roads.EdgeLength(t.Current,t.Next)/3);t.blocked=0;}
    static bool Inside(CityModel c,TrafficTrip t,int node)=>t.Current==node && t.progress*c.roads.EdgeLength(t.Current,t.Next)/3<.4f;
    static float GreenClock(CityModel c,int node,int from)
    {for(float time=0;time<20;time+=.1f)if(c.roads.Signal(node,from,time)==SignalAspect.Green && c.roads.Signal(node,from,time+.7f)==SignalAspect.Green)return time;throw new Exception("No green fixture");}
    public static void Run()
    {
        var c=JunctionChecks.Crossroads();int node=Center(c),w=Arm(c,node,-10,1.5f),e=Arm(c,node,10,1.5f),n=Arm(c,node,0,10),s=Arm(c,node,0,-10);
        Check(!c.roads.HasSignals(node),"Small-road junction has no automatic lights");
        int edge=c.roads.edges.First(r=>r.a==node && r.b==w || r.a==w && r.b==node).id;
        Check(c.UpgradeRoad(edge,RoadKind.FourLane,false,out _,TestCity.Flat) && c.roads.HasSignals(node),"Wide-road intersection derives default signals from actual road profiles");
        Check(c.roads.SetJunctionControl(node,JunctionControl.Uncontrolled,out _) && !c.roads.HasSignals(node) && c.roads.SetJunctionControl(node,JunctionControl.Automatic,out _) && c.roads.HasSignals(node),"Signal override and restore use real per-node controls");
        bool opposite=true,alternated=false,amber=false,clearance=false;
        for(float time=0;time<12;time+=.05f)
        {
            var west=c.roads.Signal(node,w,time);var north=c.roads.Signal(node,n,time);
            opposite &= west==c.roads.Signal(node,e,time) && north==c.roads.Signal(node,s,time) && !(west==SignalAspect.Green && north==SignalAspect.Green);
            alternated |= north==SignalAspect.Green;amber |= west==SignalAspect.Amber || north==SignalAspect.Amber;clearance |= west==SignalAspect.Red && north==SignalAspect.Red;
        }
        Check(opposite && alternated && amber && clearance,"Opposite arms share green; crossing arms alternate with amber and all-red clearance");
        int westFactory=TestCity.Build(c,-35,7,LandUse.Industrial),eastShop=TestCity.Build(c,35,7,LandUse.Commercial),northFactory=TestCity.Build(c,5,30,LandUse.Industrial),southShop=TestCity.Build(c,5,-30,LandUse.Commercial);
        c.Inventory(westFactory).Stock=8;c.Inventory(northFactory).Stock=8;var sim=new CityTraffic(c);
        var a=sim.Dispatch(westFactory,eastShop,TripPurpose.Delivery);var b=sim.Dispatch(northFactory,southShop,TripPurpose.Delivery);
        Approach(c,a,node);Approach(c,b,node);c.traffic.clock=GreenClock(c,node,w);sim=new CityTraffic(c);sim.Advance(.2f);
        Check(a.segment>=a.route.IndexOf(node) && b.Next==node && b.blocked>0 && b.junctionWaitReason.Contains("信号"),"Actual green freight enters while perpendicular real cargo stops at red");
        var saved=TestCity.RoundTrip(c);var savedSim=new CityTraffic(saved);int ai=a.id,bi=b.id;bool overlap=false;
        for(int i=0;i<1600;i++){sim.Advance(.05f);savedSim.Advance(.05f);overlap |= Inside(c,a,node) && Inside(c,b,node);}
        Check(!overlap && c.Goods(eastShop)>=8 && c.Goods(southShop)>=8 && c.traffic.trips.All(t=>t.id!=ai && t.id!=bi) && c.traffic.failed==0 && c.traffic.lostGoods==0 && c.Valid(),"Changing phases delivers both real cargos and returns without crossing conflicts or deletion");
        Check(TestCity.Snapshot(c)==TestCity.Snapshot(saved),"Saved phase clock and queued freight continue identically");
        c=JunctionChecks.Crossroads();node=Center(c);w=Arm(c,node,-10,1.5f);n=Arm(c,node,0,10);
        Check(c.roads.ToggleStop(node,w,out _) && c.roads.Node(node).stopFrom.Contains(w) && !c.roads.HasSignals(node),"Stop signs are associated with a specific incoming arm");
        westFactory=TestCity.Build(c,-35,7,LandUse.Industrial);eastShop=TestCity.Build(c,35,7,LandUse.Commercial);northFactory=TestCity.Build(c,5,30,LandUse.Industrial);southShop=TestCity.Build(c,5,-30,LandUse.Commercial);
        c.Inventory(westFactory).Stock=8;c.Inventory(northFactory).Stock=8;sim=new CityTraffic(c);a=sim.Dispatch(westFactory,eastShop,TripPurpose.Delivery);b=sim.Dispatch(northFactory,southShop,TripPurpose.Delivery);Approach(c,a,node);Approach(c,b,node);sim=new CityTraffic(c);sim.Advance(.2f);
        Check(a.Next==node && a.blocked>0 && a.junctionStopTime>0 && b.Current==node,"Actual stopping cargo yields to the unsigned road");
        saved=TestCity.RoundTrip(c);float waited=saved.traffic.trips.First(t=>t.id==a.id).junctionStopTime;savedSim=new CityTraffic(saved);
        Check(saved.traffic.trips.First(t=>t.id==a.id).junctionStopTime==waited,"Loading never adds artificial stop time");
        bool released=false;for(int i=0;i<1600;i++){sim.Advance(.05f);savedSim.Advance(.05f);released |= a.Current==node;}
        Check(released && c.Goods(eastShop)>=8 && c.Goods(southShop)>=8 && c.traffic.failed==0 && c.Valid(),"Stopped vehicle leaves after genuine wait and completes its delivery");
        Check(TestCity.Snapshot(c)==TestCity.Snapshot(saved),"Part-completed stop wait and arrival order survive save continuation");
        Check(c.roads.ToggleStop(node,w,out _) && !c.roads.Node(node).stopFrom.Contains(w),"Stop sign can be removed without rewriting roads");
        for(int order=0;order<2;order++)
        {
            c=JunctionChecks.Crossroads();node=Center(c);
            westFactory=TestCity.Build(c,-35,7,LandUse.Industrial);eastShop=TestCity.Build(c,35,7,LandUse.Commercial);northFactory=TestCity.Build(c,5,30,LandUse.Industrial);southShop=TestCity.Build(c,5,-30,LandUse.Commercial);
            c.Inventory(westFactory).Stock=8;c.Inventory(northFactory).Stock=8;sim=new CityTraffic(c);a=sim.Dispatch(westFactory,eastShop,TripPurpose.Delivery);b=sim.Dispatch(northFactory,southShop,TripPurpose.Delivery);
            Approach(c,a,node);Approach(c,b,node);c.traffic.clock=5;a.junctionWaitNode=b.junctionWaitNode=node;
            a.junctionQueuedAt=4;b.junctionQueuedAt=order==0?3:4;
            if(order==1)c.traffic.trips.Reverse();
            sim=new CityTraffic(c);sim.Advance(.1f);
            var first=order==0?b:a;var second=order==0?a:b;
            Check(first.segment>=first.route.IndexOf(node) && second.Next==node && second.blocked>0,"Conflict priority follows saved arrival order, then stable ID, independently of trip list ordering");
            sim.Advance(80);
            Check(c.Goods(eastShop)>=8 && c.Goods(southShop)>=8 && c.traffic.failed==0 && c.Valid(),"Both arrival-ordered cargos eventually deliver and return");
        }
        c=JunctionChecks.Crossroads();node=Center(c);
        westFactory=TestCity.Build(c,-35,7,LandUse.Industrial);eastShop=TestCity.Build(c,35,7,LandUse.Commercial);northFactory=TestCity.Build(c,5,30,LandUse.Industrial);southShop=TestCity.Build(c,5,-30,LandUse.Commercial);
        c.Inventory(westFactory).Stock=16;c.Inventory(northFactory).Stock=8;sim=new CityTraffic(c);a=sim.Dispatch(westFactory,eastShop,TripPurpose.Delivery);b=sim.Dispatch(northFactory,southShop,TripPurpose.Delivery);
        Approach(c,a,node);Approach(c,b,node);var exitQueue=sim.Dispatch(westFactory,eastShop,TripPurpose.Delivery);exitQueue.segment=exitQueue.route.IndexOf(node);
        float exitDistance=.6f;while(exitDistance>=c.roads.EdgeLength(exitQueue.Current,exitQueue.Next)/3){exitDistance-=c.roads.EdgeLength(exitQueue.Current,exitQueue.Next)/3;exitQueue.segment++;}exitQueue.progress=exitDistance/(c.roads.EdgeLength(exitQueue.Current,exitQueue.Next)/3);
        c.traffic.clock=5;a.junctionWaitNode=b.junctionWaitNode=node;a.junctionQueuedAt=3;b.junctionQueuedAt=4;sim=new CityTraffic(c);sim.Advance(.1f);
        Check(b.segment>=b.route.IndexOf(node) && a.Next==node,"An earlier arrival with blocked exit cannot prevent a ready crossing movement from clearing traffic");
        sim.Advance(80);Check(c.Goods(eastShop)>=16 && c.Goods(southShop)>=8 && c.traffic.failed==0 && c.Valid(),"Exit-blocked priority resumes all three real cargos without task loss");
        c=JunctionChecks.Crossroads();node=Center(c);w=Arm(c,node,-10,1.5f);c.roads.ToggleStop(node,w,out _);
        var mid=CityRoads.Lerp(c.roads.Node(node),c.roads.Node(w),.5f);var branch=c.roads.Plan(c,mid,TestCity.P(-15,-12),TestCity.Flat);
        Check(c.CommitRoad(branch) && !c.roads.Node(node).stopFrom.Contains(w) && c.roads.Node(node).stopFrom.Count==1 && c.Valid(),"Splitting the signed incoming edge preserves the sign on its replacement arm");
        int signed=c.roads.Node(node).stopFrom.Single();edge=c.roads.edges.First(r=>r.a==signed && r.b==node || r.a==node && r.b==signed).id;c.roads.Remove(new System.Collections.Generic.List<int>{edge});
        Check(c.roads.Node(node).stopFrom.Count==0 && !c.roads.HasSignals(node) && c.Valid(),"Demolition removes dangling stop references and stops signaling a two-arm junction");
        var bad=TestCity.RoundTrip(c);bad.roads.Node(node).control=(JunctionControl)99;Check(!bad.Valid(),"Invalid control kind is rejected");
        c=TestCity.Create();var legacy=c.ToSaveData();legacy.format=28;saved=legacy.ToCity();Check(saved.Valid() && saved.roads.nodes.All(p=>p.control==JunctionControl.Automatic && p.stopFrom.Count==0),"Format 28 gains default node controls without changing people or buildings");
        Console.WriteLine("PASS: "+checks+" traffic signals/real red waits/stop signs/yield/split/demolition/save checks");
    }
}
