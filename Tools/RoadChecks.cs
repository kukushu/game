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
        CurveChecks();
        Console.WriteLine("PASS: "+checks+" road checks");
    }
    static void CurveChecks()
    {
        var c=Empty(); int funds=c.money;
        var plan=c.roads.PlanCurve(c,P(-52.5f,1.5f),P(-32.5f,21.5f),P(-12.5f,1.5f),Flat);
        Check(plan.Valid,"Smooth curve plans: "+plan.error);
        Check(plan.length>40 && plan.points.Exists(p=>p.z>10),"Curve has real arc length and non-chord geometry");
        Check(c.roads.edges.Count==0 && c.money==funds,"Curve preview is atomic and read-only");
        Check(c.CommitRoad(plan) && c.money==funds-plan.cost && c.Valid(),"Curve charges total arc length once and remains valid");
        float builtLength=0; foreach(var e in c.roads.edges) builtLength+=c.roads.EdgeLength(e.a,e.b);
        Check(Math.Abs(builtLength-plan.length)<.001f && plan.cost==(int)Math.Ceiling(plan.length*100/3),"Curved construction cost matches actual built graph length");
        Check(c.roads.edges.TrueForAll(e=>e.stroke==plan.stroke),"All curve spans form one undo stroke");
        var end=c.roads.nodes.Find(n=>CityRoads.Length(n,P(-12.5f,1.5f))<.01f);
        var path=c.roads.FindPath(new List<int>{648},new List<int>{end.id});
        Check(path.Count>10 && path.Exists(id=>c.roads.Node(id).z>10),"Routing follows the curve rather than its chord");
        var saved=new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(c.roads);
        var restored=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<CityRoads>(saved);
        Check(restored.Valid() && restored.FindPath(new List<int>{648},new List<int>{end.id}).Count==path.Count,"Curved network survives save/load without a schema migration");
        int curveHome=CityModel.Index(0,19),curveWork=CityModel.Index(13,19);
        c.tiles[curveHome]=2;c.levels[curveHome]=1;c.tiles[curveWork]=4;c.levels[curveWork]=1;c.Recalculate();
        var curveTraffic=new CityTraffic(c); c.traffic.dispatchTimer=-10000;c.traffic.productionTimer=-10000;
        var curvedTrip=curveTraffic.Dispatch(curveHome,curveWork,TripPurpose.Commute);
        Check(curvedTrip!=null && curvedTrip.route.Exists(id=>c.roads.Node(id).z>10),"Actual commuter dispatch uses curved road nodes");
        curveTraffic.Advance(60);
        Check(c.traffic.completed==1 && c.traffic.failed==0,"Actual vehicle traverses and returns along a curved road");
        c.traffic.dispatchTimer=0;c.traffic.productionTimer=0;
        var run=c.roads.Run(c.roads.edges[c.roads.edges.Count/2].id);
        Check(run.Count==c.roads.edges.Count,"Demolition selects whole curve up to its junctions");
        c.roads.Remove(run); c.money+=plan.cost;
        Check(c.roads.edges.Count==0 && c.money==funds && c.Valid(),"Curve undo refunds the entire stroke");

        c=Empty(); c.tiles[CityModel.Index(18,18)]=2;
        Check(!c.roads.Plan(c,P(-20,1.5f),P(23,1.5f),Flat).Valid,"Straight chord is obstructed by housing");
        plan=c.roads.PlanCurve(c,P(-20,1.5f),P(1.5f,35),P(23,1.5f),Flat);
        Check(plan.Valid && c.CommitRoad(plan),"Curved alignment can avoid an obstructed chord: "+plan.error);
        Check(!c.roads.OverlapsLot(CityModel.Index(18,18)),"Actual curved footprint avoids housing");
        c=Empty();
        Check(c.roads.PlanCurve(c,P(0,0),P(1.5f,.2f),P(3,0),Flat).Valid,"Short gentle curves do not create invalid tiny sampling spans");
        Check(!c.roads.PlanCurve(c,P(-20,0),P(0,20),P(20,0),(x,z)=>z>8?0:1).Valid,"Water along the arc, off the chord, rejects the entire curve");
        Check(!c.roads.PlanCurve(c,P(-20,0),P(0,20),P(20,0),(x,z)=>1+z).Valid,"Terrain slope is checked along the curve");
        Check(!c.roads.PlanCurve(c,P(0,0),P(1,10),P(2,0),Flat).Valid,"Hairpin with overlapping road width is rejected");
        Check(!c.roads.PlanCurve(c,P(-20,0),P(0,200),P(20,0),Flat).Valid,"Curve leaving the construction boundary is rejected");
        Check(c.roads.edges.Count==0,"Rejected curved previews leave no partial graph");
        c.money=1;
        Check(!c.roads.PlanCurve(c,P(-20,0),P(0,20),P(20,0),Flat).Valid,"Insufficient arc-length funds reject construction");

        c=Empty(); Build(c,P(-52.5f,1.5f),P(-10,1.5f));
        int home=CityModel.Index(5,19),work=CityModel.Index(13,19);
        c.tiles[home]=2;c.levels[home]=1;c.tiles[work]=4;c.levels[work]=1;c.Recalculate();
        var traffic=new CityTraffic(c); c.traffic.dispatchTimer=-10000;c.traffic.productionTimer=-10000;
        var trip=traffic.Dispatch(home,work,TripPurpose.Commute); traffic.Advance(.35f);
        var before=CityRoads.Lerp(c.roads.Node(trip.Current),c.roads.Node(trip.Next),trip.progress);
        plan=c.roads.PlanCurve(c,P(-35,-20),P(-15,5),P(-35,25),Flat);
        Check(plan.Valid && c.CommitRoad(plan),"Curve crossing a live road commits: "+plan.error);
        var after=CityRoads.Lerp(c.roads.Node(trip.Current),c.roads.Node(trip.Next),trip.progress);
        Check(CityRoads.Length(before,after)<.001f,"Curve intersections preserve active vehicle positions");
        c.traffic.dispatchTimer=0; c.traffic.productionTimer=0;
        Check(c.Valid(),"Curve intersection graph remains valid: roads="+c.roads.Valid()+", traffic="+CityTraffic.Valid(c.traffic,c.roads,c.tiles.Length));
        Check(c.roads.nodes.Exists(n=>c.roads.Neighbors(n.id).Count==4),"Curve crossing creates a real junction");
        c.traffic.dispatchTimer=-10000;c.traffic.productionTimer=-10000;
        traffic.Advance(25);
        Check(c.traffic.completed==1,"Active vehicle completes after curved road intersection splitting");
        c=Empty();
        c.roads.nodes.Add(new RoadNode{id=1296,x=0,z=0,y=1}); c.roads.nodes.Add(new RoadNode{id=1297,x=3,z=0,y=1}); c.roads.nodes.Add(new RoadNode{id=1298,x=3,z=3,y=1});
        c.roads.nextNode=1299;c.roads.nextEdge=3;
        c.roads.edges.Add(new RoadEdge{id=1,a=1296,b=1297}); c.roads.edges.Add(new RoadEdge{id=2,a=1297,b=1298}); c.roads.Changed();
        Check(c.roads.Run(1).Count==1,"Legacy grid strokes still stop demolition at a sharp corner");
    }
}
