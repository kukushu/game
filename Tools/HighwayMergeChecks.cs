using System;
using System.Linq;
using HarborCity;

public static class HighwayMergeChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Highway merge: "+why);checks++;}
    static void Road(CityModel c,float ax,float az,float bx,float bz,RoadKind kind)
    {var p=c.roads.Plan(c,c.roads.Snap(ax,az,TestCity.Flat),c.roads.Snap(bx,bz,TestCity.Flat),TestCity.Flat,CityRoadTypes.LimitedAccess(kind),kind);if(!c.CommitRoad(p))throw new Exception("Merge fixture: "+p.error);}
    public static CityModel Fixture(out int factory,out int shop)
    {
        var c=HighwayChecks.Loop();c.money=1000000;
        Road(c,-45,1.5f,-45,-25,RoadKind.Standard);Road(c,-45,-25,40,-25,RoadKind.Standard);
        Road(c,-10,-25,0,1.5f,RoadKind.Ramp);
        factory=TestCity.Build(c,-12,-31,LandUse.Industrial);shop=TestCity.Build(c,30,31,LandUse.Commercial);return c;
    }
    public static int Center(CityModel c)=>c.roads.nodes.First(n=>Math.Abs(n.x)<.75f && Math.Abs(n.z-1.5f)<.01f).id;
    public static void Approach(CityModel c,TrafficTrip t,int node,int lane,float distance=.34f)
    {
        int s=t.route.IndexOf(node)-1;
        if(s<0)throw new Exception("Trip does not use merge");
        while(distance>c.roads.EdgeLength(t.route[s],t.route[s+1])/3 && s>0){distance-=c.roads.EdgeLength(t.route[s],t.route[s+1])/3;s--;}
        t.segment=s;t.progress=1-distance/(c.roads.EdgeLength(t.Current,t.Next)/3);t.lane=t.previousLane=t.laneChangeFrom=lane;t.laneChangeRemaining=t.laneChangeCooldown=0;
    }
    public static void Run()
    {
        for(int order=0;order<2;order++)
        {
            var c=Fixture(out int factory,out int shop);int node=Center(c);c.Inventory(factory).Stock=8;var sim=new CityTraffic(c);
            var main=sim.Dispatch(CityTraffic.Outside,shop,TripPurpose.Import);var ramp=sim.Dispatch(factory,shop,TripPurpose.Delivery);
            Check(main!=null && ramp!=null && ramp.route.Contains(node),"Real mainline/import and ramp/factory delivery routes");
            int index=ramp.route.IndexOf(node);
            Check(c.roads.HighwayMergeLane(ramp.route[index-1],node,ramp.route[index+1])==2,"Right-side ramp joins actual outer right lane");
            Approach(c,main,node,2);Approach(c,ramp,node,0);c.traffic.clock=10;main.junctionWaitNode=ramp.junctionWaitNode=node;main.junctionQueuedAt=9;ramp.junctionQueuedAt=1;
            if(order==1)c.traffic.trips.Reverse();sim=new CityTraffic(c);sim.Advance(.1f);
            Check(main.segment>=main.route.IndexOf(node) && ramp.Next==node && ramp.blocked>0 && ramp.junctionWaitReason!=null,"Earlier ramp yields to physical mainline gap regardless of trip-list order: "+main.segment+"/"+main.route.IndexOf(node)+" rampNext="+ramp.Next+" node="+node+" blocked="+ramp.blocked+" reason="+ramp.junctionWaitReason);
            var saved=TestCity.RoundTrip(c);var resumed=new CityTraffic(saved);sim.Advance(100);resumed.Advance(100);
            Check(c.Goods(shop)==16 && c.traffic.failed==0 && c.traffic.lostGoods==0 && c.traffic.trips.Count==0 && c.Valid(),"Mainline clears then ramp delivers and physically returns");
            Check(TestCity.Snapshot(c)==TestCity.Snapshot(saved),"Saved yielding queue continues deterministically");
        }
        {
            var c=Fixture(out int factory,out int shop);int node=Center(c);c.Inventory(factory).Stock=8;var sim=new CityTraffic(c);
            var main=sim.Dispatch(CityTraffic.Outside,shop,TripPurpose.Import);var ramp=sim.Dispatch(factory,shop,TripPurpose.Delivery);
            Approach(c,main,node,0);Approach(c,ramp,node,0);sim=new CityTraffic(c);sim.Advance(.1f);
            Check(main.segment>=main.route.IndexOf(node) && ramp.segment>=ramp.route.IndexOf(node),"Nonconflicting inner highway lane and outer ramp can move together");
            sim.Advance(100);Check(c.Goods(shop)==16 && c.traffic.failed==0 && c.Valid(),"Parallel lane merge completes real deliveries");
        }
        {
            var c=Fixture(out int factory,out int shop);int node=Center(c);c.Inventory(factory).Stock=8;var sim=new CityTraffic(c);
            var main=sim.Dispatch(CityTraffic.Outside,shop,TripPurpose.Import);var ramp=sim.Dispatch(factory,shop,TripPurpose.Delivery);
            Approach(c,main,node,2,2.1f);Approach(c,ramp,node,0);sim=new CityTraffic(c);sim.Advance(.05f);
            Check(ramp.Next==node && ramp.junctionWaitReason.Contains("主线"),"Gap scan sees approaching highway traffic across short graph samples");
            sim.Advance(100);Check(c.Goods(shop)==16 && c.traffic.failed==0 && c.Valid(),"Far approach protection still releases ramp after genuine gap");
        }
        {
            var c=Fixture(out int factory,out int shop);Road(c,-10,25,0,1.5f,RoadKind.Ramp);int node=Center(c);
            int top=c.roads.Neighbors(node).First(n=>c.roads.Kind(n,node)==RoadKind.Ramp && c.roads.Node(n).z>1.5f);
            int east=c.roads.Neighbors(node).First(n=>c.roads.Kind(node,n)==RoadKind.Highway && c.roads.Node(n).x>0);
            Check(c.roads.HighwayMergeLane(top,node,east)==0,"Left-side ramp joins leftmost lane without random spreading");
        }
        {
            var c=Fixture(out int factory,out int shop);int node=Center(c);c.Inventory(factory).Stock=8;var sim=new CityTraffic(c);
            var main=sim.Dispatch(CityTraffic.Outside,shop,TripPurpose.Import);var ramp=sim.Dispatch(factory,shop,TripPurpose.Delivery);
            Approach(c,main,node,2);Approach(c,ramp,node,0);c.roads.SetJunctionControl(node,JunctionControl.Signals,out _);
            int entry=ramp.route[ramp.route.IndexOf(node)-1];float clock=0;
            while(clock<20 && c.roads.Signal(node,entry,clock)!=SignalAspect.Green)clock+=.1f;
            c.traffic.clock=clock+.2f;sim=new CityTraffic(c);sim.Advance(.1f);
            Check(ramp.segment>=ramp.route.IndexOf(node) && main.Next==node,"Explicit ramp green overrides default mainline priority while mainline red holds");
            sim.Advance(100);Check(c.Goods(shop)==16 && c.traffic.failed==0 && c.Valid(),"Explicit signaled merge completes both cargos");
        }
        {
            var c=Fixture(out int factory,out int shop);int node=Center(c);var sim=new CityTraffic(c);
            var main=sim.Dispatch(CityTraffic.Outside,shop,TripPurpose.Import);
            Approach(c,main,node,2,10);sim=new CityTraffic(c);sim.Advance(.05f);
            Check(main.lane==1 && main.laneChangeFrom==2 && main.laneChangeRemaining>0,"Exit selection looks beyond a straight merge and recognizes a two-arm highway-to-ramp transition");
            sim.Advance(100);Check(c.Goods(shop)==8 && c.traffic.failed==0 && c.Valid(),"Prepared exit actually delivers and returns");
        }
        {
            var c=Fixture(out int factory,out int shop);int node=Center(c);c.Inventory(factory).Stock=8;var sim=new CityTraffic(c);
            var main=sim.Dispatch(CityTraffic.Outside,shop,TripPurpose.Import);var ramp=sim.Dispatch(factory,shop,TripPurpose.Delivery);
            Approach(c,main,node,2);Approach(c,ramp,node,0);c.roads.ToggleStop(node,main.route[main.route.IndexOf(node)-1],out _);
            sim=new CityTraffic(c);sim.Advance(.1f);
            Check(ramp.segment>=ramp.route.IndexOf(node) && main.Next==node && main.blocked>0,"Explicit mainline stop yields to unsigned ramp without circular priority deadlock");
            sim.Advance(100);Check(c.Goods(shop)==16 && c.traffic.failed==0 && c.Valid(),"Stopped mainline and ramp both actually finish");
        }
        {
            var c=Fixture(out int factory,out int shop);
            var factories=new[]{factory,TestCity.Build(c,-30,-31,LandUse.Industrial),TestCity.Build(c,10,-31,LandUse.Industrial)};foreach(int id in factories)c.Inventory(id).Stock=24;
            var shops=new[]{shop,TestCity.Build(c,-35,31,LandUse.Commercial),TestCity.Build(c,-20,31,LandUse.Commercial),TestCity.Build(c,-5,31,LandUse.Commercial),TestCity.Build(c,10,31,LandUse.Commercial)};
            var sim=new CityTraffic(c);int imports=0,deliveries=0;
            for(int i=0;i<6000;i++)
            {
                if(imports<12 && sim.Dispatch(CityTraffic.Outside,shops[imports%5],TripPurpose.Import)!=null)imports++;
                if(deliveries<8 && sim.Dispatch(factories[deliveries/3],shops[(12+deliveries)%5],TripPurpose.Delivery)!=null)deliveries++;
                sim.Advance(.05f);
            }
            Check(imports==12 && deliveries==8 && shops.Sum(c.Goods)==160 && c.traffic.failed==0 && c.traffic.lostGoods==0 && c.traffic.trips.Count==0 && c.Valid(),"Twenty naturally dispatched loaded vehicles from highway and ramp all deliver and return without deadlock/loss: "+imports+"/"+deliveries+" stock="+shops.Sum(c.Goods)+" failed="+c.traffic.failed+" lost="+c.traffic.lostGoods+" trips="+c.traffic.trips.Count+" valid="+c.Valid());
        }
        Console.WriteLine("PASS: "+checks+" real highway priority/physical-side/parallel-gap/save checks");
    }
}
