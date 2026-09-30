using System;
using System.Collections.Generic;
using HarborCity;

public static class RoadChecks
{
    static int checks;
    static void Check(bool ok,string message) { if(!ok) throw new Exception("Roads: "+message); checks++; }
    static float Flat(float x,float z)=>1;
    static RoadNode P(float x,float z)=>new RoadNode{x=x,z=z,y=1};
    static CityModel Empty()
    {
        var c=new CityModel(); c.tiles[648]=1; c.EnableRoads(Flat); return c;
    }
    static RoadPlan Build(CityModel c,RoadNode a,RoadNode b)
    {
        var plan=c.roads.Plan(c,a,b,Flat);
        Check(plan.Valid,"Valid build: "+plan.error); Check(c.CommitRoad(plan),"Atomic commit"); return plan;
    }
    public static void Run()
    {
        var near=Empty(); Build(near,P(-52.5f,1.5f),P(20,1.5f));
        var edgeSnap=near.roads.Snap(-13.15f,0,Flat);
        Check(Math.Abs(edgeSnap.z-1.5f)<.001f,"Clicking visible shoulder snaps to main road instead of creating false junction");
        Build(near,edgeSnap,P(-13.15f,-25));
        var branch=near.roads.nodes.Find(n=>Math.Abs(n.z+25)<.01f);
        Check(near.roads.Connected(branch.id) && near.Valid(),"Shoulder-started branch really connects to outside");
        var c=CityModel.CreateLegacySample(); var traffic=new CityTraffic(c); traffic.Advance(8);
        int population=c.population,jobs=c.jobs,trips=c.traffic.trips.Count;
        c.EnableRoads(Flat);
        Check(c.version==2 && c.Valid(),"Old grid and active routes migrate to version 2");
        Check(c.population==population && c.jobs==jobs && c.traffic.trips.Count==trips,"Migration preserves buildings, economy and trips");
        traffic=new CityTraffic(c); traffic.Advance(90);
        Check(c.traffic.completed>10 && c.Valid(),"Migrated traffic completes trips and remains saveable");

        c=Empty(); int funds=c.money;
        var first=Build(c,P(-52.5f,1.5f),P(-12.5f,13.5f));
        Check(c.money==funds-first.cost,"Costs charged once by length");
        Check(c.roads.nodes.Exists(n=>n.id>=1296 && Math.Abs(n.x/3- Math.Round(n.x/3))>.1f),"Arbitrary world positions, not grid stair steps");
        int edgeCount=c.roads.edges.Count;
        Check(!c.CommitRoad(first) && c.roads.edges.Count==edgeCount,"Stale preview cannot double-commit");
        var snapped=c.roads.Snap(-32.4f,7.4f,Flat);
        Check(CityRoads.Distance(snapped,P(-52.5f,1.5f),P(-12.5f,13.5f))<.01f,"Snap lands on road centerline");
        Build(c,snapped,P(-32.4f,25));
        Check(c.roads.nodes.Exists(n=>c.roads.Neighbors(n.id).Count==3),"Mid-segment branch forms connected T junction");
        Build(c,P(-40,-8),P(-40,22));
        Check(c.roads.nodes.Exists(n=>c.roads.Neighbors(n.id).Count==4),"Crossing creates degree-four intersection");
        Check(c.Valid(),"Intersection graph validates");
        var goal=c.roads.Snap(-40,22,Flat);
        var goalNode=c.roads.nodes.Find(n=>CityRoads.Length(n,goal)<.1f);
        Check(c.roads.FindPath(new List<int>{648},new List<int>{goalNode.id}).Count>3,"Route can turn across new intersection");

        c=Empty(); Build(c,P(-52.5f,1.5f),P(-10,1.5f));
        int home=CityModel.Index(5,19),work=CityModel.Index(13,19);
        c.tiles[home]=2;c.levels[home]=1;c.tiles[work]=4;c.levels[work]=1;c.Recalculate();
        Check(c.population==12 && c.jobs==12,"Buildings connect to free road and contribute to economy");
        traffic=new CityTraffic(c); c.traffic.dispatchTimer=-10000;c.traffic.productionTimer=-10000;
        var trip=traffic.Dispatch(home,work,TripPurpose.Commute);
        Check(trip!=null,"Commuter uses free-road access");
        traffic.Advance(.35f);
        var before=CityRoads.Lerp(c.roads.Node(trip.Current),c.roads.Node(trip.Next),trip.progress);
        var mid=CityRoads.Lerp(c.roads.Node(trip.Current),c.roads.Node(trip.Next),.55f);
        Build(c,mid,P(mid.x,-12));
        var after=CityRoads.Lerp(c.roads.Node(trip.Current),c.roads.Node(trip.Next),trip.progress);
        Check(CityRoads.Length(before,after)<.001f,"Splitting occupied road preserves car position");
        traffic.Advance(25);
        Check(c.traffic.completed==1,"Car still reaches destination after live road splitting");
        var blocked=c.roads.Plan(c,P(-20,4.5f),P(-40,4.5f),Flat);
        Check(!blocked.Valid && blocked.error.Contains("建筑"),"Preview blocks building/zone collisions");
        Check(!c.Place(7,18,LandUse.Residential,out _),"Buildings cannot overwrite free roads");

        c=Empty(); funds=c.money;
        Check(!c.roads.Plan(c,P(0,0),P(.2f,0),Flat).Valid,"Too-short road rejected");
        Check(!c.roads.Plan(c,P(0,0),P(CityModel.BuildHalfSize+6,0),Flat).Valid,"Outside construction boundary rejected");
        Check(!c.roads.Plan(c,P(0,0),P(10,0),(x,z)=>0).Valid,"Underwater road rejected");
        Check(!c.roads.Plan(c,P(0,0),P(10,0),(x,z)=>1+x).Valid,"Steep grade rejected");
        Check(c.money==funds && c.roads.edges.Count==0,"Failed previews never mutate city or charge money");
        c.money=0; Check(!c.roads.Plan(c,P(0,0),P(10,0),Flat).Valid,"Insufficient funds rejected");
        c.money=funds; first=Build(c,P(-52.5f,1.5f),P(-20,1.5f));
        Check(!c.roads.Plan(c,P(-50,1.5f),P(-30,1.5f),Flat).Valid,"Overlapping road rejected");
        var ids=new List<int>(); foreach(var e in c.roads.edges) if(e.stroke==first.stroke) ids.Add(e.id);
        c.roads.Remove(ids); c.money+=first.cost;
        Check(c.money==funds && c.roads.edges.Count==0 && c.Valid(),"Undo removes only stroke, refunds construction and preserves graph validity");

        c=Empty();
        // Short links occur when an existing road is split near a sampling node.
        for(int k=0;k<=40;k++) c.roads.nodes.Add(new RoadNode{id=1296+k,x=-40+k*.6f,z=1.5f,y=1});
        c.roads.nextNode=1337;
        for(int k=0;k<40;k++) c.roads.edges.Add(new RoadEdge{id=c.roads.nextEdge++,a=1296+k,b=1297+k});
        c.roads.Changed();
        home=CityModel.Index(5,19);work=CityModel.Index(11,19);
        c.tiles[home]=2;c.levels[home]=1;c.tiles[work]=4;c.levels[work]=1;c.Recalculate();
        traffic=new CityTraffic(c); c.traffic.dispatchTimer=-10000;c.traffic.productionTimer=-10000;
        var leader=traffic.Dispatch(home,work,TripPurpose.Commute); traffic.Advance(1.5f);
        var follower=traffic.Dispatch(home,work,TripPurpose.Commute);
        Check(leader!=null && follower!=null,"Two vehicles dispatch onto subdivided road");
        bool gap=true;
        for(int k=0;k<55;k++)
        {
            traffic.Advance(.05f);
            var a=CityRoads.Lerp(c.roads.Node(leader.Current),c.roads.Node(leader.Next),leader.progress);
            var b=CityRoads.Lerp(c.roads.Node(follower.Current),c.roads.Node(follower.Next),follower.progress);
            if(leader.status==TripStatus.Driving && !leader.returning) gap &= CityRoads.Length(a,b)>1.39f;
        }
        Check(gap,"Headway remains physical across multiple short links");
        Console.WriteLine("PASS: "+checks+" road checks");
    }
}
