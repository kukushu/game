using System;
using System.Linq;
using System.Collections.Generic;
using HarborCity;
public static class RoadChecks
{
    static int checks;
    static RoadNode P(float x,float z)=>TestCity.P(x,z);
    static float Flat(float x,float z)=>1;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Roads: "+why);checks++;}
    static RoadPlan Build(CityModel c,RoadNode a,RoadNode b)
    {var p=c.roads.Plan(c,a,b,Flat);Check(p.Valid && c.CommitRoad(p),"Atomic straight construction: "+p.error);return p;}
    public static void Run()
    {
        var c=TestCity.Empty();int funds=c.money;
        var p=Build(c,c.roads.Node(CityRoads.Entrance),P(30,1.5f));
        Check(c.money==funds-p.cost && !c.CommitRoad(p),"Length-based charge and stale preview rejection");
        var snap=c.roads.Snap(-30.137f,.2f,Flat);Check(Math.Abs(snap.z-1.5f)<.001f,"Shoulder snaps to real centreline");
        Build(c,snap,P(snap.x,24));Build(c,P(-21.5f,-20),P(-21.5f,20));
        Check(c.roads.nodes.Any(n=>c.roads.Neighbors(n.id).Count==3) && c.roads.nodes.Any(n=>c.roads.Neighbors(n.id).Count==4),"T and cross intersections remain real graph connections");
        Check(c.Valid(),"Free road graph valid in entity city");
        foreach(var end in new[]{P(.2f,0),P(90,0)})Check(!c.roads.Plan(c,P(0,0),end,Flat).Valid,"Short/out-of-bounds rejected");
        Check(!c.roads.Plan(c,P(0,-10),P(10,-10),(x,z)=>0).Valid,"Water rejected");
        Check(!c.roads.Plan(c,P(0,-10),P(10,-10),(x,z)=>10+x).Valid,"Steep slope rejected");
        Check(!c.roads.Plan(c,P(0,1.5f),P(10,1.5f),Flat).Valid,"Overlapping road rejected");
        c=TestCity.Empty();funds=c.money;
        var curve=c.roads.PlanCurve(c,c.roads.Node(CityRoads.Entrance),P(-32.5f,21.5f),P(-12.5f,1.5f),Flat);
        Check(curve.Valid && c.roads.edges.Count==0,"Curved preview remains atomic");
        Check(c.CommitRoad(curve) && c.Valid(),"Curved construction remains valid");
        var endNode=c.roads.nodes.First(n=>CityRoads.Length(n,P(-12.5f,1.5f))<.01f);
        var path=c.roads.FindPath(new List<int>{CityRoads.Entrance},new List<int>{endNode.id});
        Check(path.Count>10 && path.Any(n=>c.roads.Node(n).z>10),"Route follows curved geometry");
        float length=c.roads.edges.Sum(e=>c.roads.EdgeLength(e.a,e.b));
        Check(Math.Abs(length-curve.length)<.001f && c.money==funds-curve.cost,"Curve is billed by actual graph length");
        Check(TestCity.RoundTrip(c).roads.FindPath(new List<int>{CityRoads.Entrance},new List<int>{endNode.id}).Count==path.Count,"Curved graph survives new save format");
        var run=c.roads.Run(c.roads.edges[4].id);Check(run.Count==c.roads.edges.Count,"Whole curved stroke selected for demolition");
        c.roads.Remove(run);c.money+=curve.cost;Check(c.roads.edges.Count==0 && c.money==funds && c.Valid(),"Curve undo preserves identity graph and refunds");
        Check(!c.roads.PlanCurve(c,P(0,0),P(1,10),P(2,0),Flat).Valid,"Hairpin rejected");
        Check(!c.roads.PlanCurve(c,P(-20,0),P(0,200),P(20,0),Flat).Valid,"Out-of-bounds curve rejected");
        Check(!c.roads.PlanCurve(c,P(-20,0),P(0,20),P(20,0),(x,z)=>z>8?0:1).Valid,"Water along arc rejected");
        Check(c.roads.PlanCurve(c,P(0,0),P(1.5f,.2f),P(3,0),Flat).Valid,"Short gentle arc accepted");

        c=TestCity.Empty();var firstControl=P(-32.5f,21.5f);var firstEnd=P(-12.5f,1.5f);
        curve=c.roads.PlanCurve(c,c.roads.Node(CityRoads.Entrance),firstControl,firstEnd,Flat);Check(c.CommitRoad(curve),"Freeform initial curve is an actual road stroke");
        var nextEnd=P(27.5f,-8.5f);var nextControl=CityRoads.ContinuationControl(firstEnd,firstControl,nextEnd);
        float ax=firstEnd.x-firstControl.x,az=firstEnd.z-firstControl.z,bx=nextControl.x-firstEnd.x,bz=nextControl.z-firstEnd.z;
        Check(Math.Abs(ax*bz-az*bx)<.001f && ax*bx+az*bz>0,"Freeform continuation preserves outgoing tangent direction");
        int existing=c.roads.edges.Count;var continuation=c.roads.PlanCurve(c,firstEnd,nextControl,nextEnd,Flat);
        Check(continuation.Valid && c.roads.edges.Count==existing && c.CommitRoad(continuation),"Continuous curve preview is atomic and uses ordinary road validation");
        int terminal=c.roads.nodes.First(n=>CityRoads.Length(n,nextEnd)<.01f).id;
        Check(c.roads.FindPath(new List<int>{CityRoads.Entrance},new List<int>{terminal}).Count>path.Count && TestCity.RoundTrip(c).Valid(),"Continuous curves create connected traffic routes and reliable saves");
        Check(c.ZoningCells(Flat).Any(cell=>cell.available),"Continuous curves still create independent roadside planning land");
        var continuedRun=c.roads.Run(c.roads.edges.Last().id);Check(continuedRun.All(id=>c.roads.edges.Find(e=>e.id==id).stroke==continuation.stroke) && continuedRun.Count<c.roads.edges.Count,"Each continued curve remains independently demolishable");

        c=TestCity.Empty();Build(c,c.roads.Node(CityRoads.Entrance),P(30,1.5f));
        int origin=TestCity.Build(c,-42,7,LandUse.Industrial),destination=TestCity.Build(c,20,7,LandUse.Commercial);
        c.Inventory(origin).Stock=8;c.society.settings.applicantsPerDay=0;c.traffic.dispatchTimer=-10000;
        var sim=new CityTraffic(c);var t=sim.Dispatch(origin,destination,TripPurpose.Delivery);Check(t!=null,"Freight dispatch from concrete entities");
        sim.Advance(.05f);var before=CityRoads.Lerp(c.roads.Node(t.Current),c.roads.Node(t.Next),t.progress);
        var mid=CityRoads.Lerp(c.roads.Node(t.Current),c.roads.Node(t.Next),.55f);
        Build(c,mid,P(mid.x,-20));var after=CityRoads.Lerp(c.roads.Node(t.Current),c.roads.Node(t.Next),t.progress);
        Check(CityRoads.Length(before,after)<.001f,"Splitting occupied road preserves vehicle position");
        sim.Advance(30);Check(t.cargo==0 && c.Goods(destination)==8,"Delivery completes after live road splitting");
        c.traffic.dispatchTimer=0;Check(c.Valid(),"Graph and entity references remain saveable");
        Console.WriteLine("PASS: "+checks+" free-road/curve checks");
    }
}

