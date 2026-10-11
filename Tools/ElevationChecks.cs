using System;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using HarborCity;
public static class ElevationChecks
{
    static int checks;
    static RoadNode P(float x,float z,float y=1)=>new RoadNode{x=x,z=z,y=y};
    static void Check(bool ok,string why){if(!ok)throw new Exception("Elevation: "+why);checks++;}
    static RoadPlan Build(CityModel c,RoadNode a,RoadNode b,bool elevated=false,RoadKind kind=RoadKind.Standard)
    {var p=c.roads.Plan(c,a,b,TestCity.Flat,false,kind,elevated);Check(p.Valid && c.CommitRoad(p),"construction: "+p.error);return p;}
    public static void Run()
    {
        foreach(float level in new[]{7f,-5f})
        {
            var c=TestCity.Empty();c.money=200000;
            Build(c,c.roads.Node(CityRoads.Entrance),P(-30,1.5f));
            Build(c,P(-30,1.5f),P(-8,1.5f,level),true);
            var deck=Build(c,P(-8,1.5f,level),P(8,1.5f,level),true);
            Check(deck.cost>CityRoadTypes.ConstructionCost(deck.length,RoadKind.Standard),"structure has real additional construction expense");
            Build(c,P(8,1.5f,level),P(30,1.5f),true);
            Build(c,P(30,1.5f),P(45,1.5f));
            Build(c,P(0,-18),P(0,18));
            Check(c.roads.nodes.Where(n=>Math.Abs(n.x)<.01f && Math.Abs(n.z-1.5f)<.01f).All(n=>c.roads.Neighbors(n.id).Count<=2),"crossing is grade separated");
            Check(c.roads.Pick(0,1.5f,level:level).structure==(level>1?RoadStructure.Elevated:RoadStructure.Tunnel),"layer-specific selection");
            Check(Math.Abs(c.roads.Snap(0,1.5f,TestCity.Flat,level-1).y-level)<.01f,"snap selects intended layer");
            Check(c.roads.edges.Where(e=>e.stroke==deck.stroke).All(e=>e.structure==(level>1?RoadStructure.Elevated:RoadStructure.Tunnel)),"real structure recorded");
            Check(!c.ZoningCells(TestCity.Flat).Any(z=>z.block.StartsWith(deck.stroke+":")),"no zoning along decks");
            var surface=c.roads.Pick(0,10,level:1);var upper=c.roads.Pick(0,1.5f,level:level);
            float rayZ=1.5f+20-level-.09f;
            var visible=c.roads.PickVisibleRay(0,20,rayZ,0,-1,-1,level<1);
            Check(visible!=null && visible.stroke==deck.stroke,"oblique mouse ray picks the actual bridge/tunnel rather than its terrain projection");
            Check(c.roads.PickVisibleRay(0,20,rayZ,0,-1,-1,false).structure==(level>1?RoadStructure.Elevated:RoadStructure.Ground),"surface mode ignores tunnels and picks nearest visible deck");
            Check(TestCity.RoundTrip(c).roads.PickVisibleRay(0,20,rayZ,0,-1,-1,level<1).id==visible.id,"visible-road pick survives save reload");
            Check(c.roads.PickVisibleRay(70,20,70,0,-1,0,level<1)==null && c.roads.PickVisibleRay(0,20,1.5f,1,0,0,level<1)==null,"empty space and parallel rays do not select unrelated roads");
            var slopeEdge=c.roads.edges.First(e=>e.structure==(level>1?RoadStructure.Elevated:RoadStructure.Tunnel) && Math.Abs(c.roads.Node(e.a).y-c.roads.Node(e.b).y)>.01f);
            var midpoint=CityRoads.Lerp(c.roads.Node(slopeEdge.a),c.roads.Node(slopeEdge.b),.5f);
            Check(c.roads.PickVisibleRay(midpoint.x,midpoint.y+20.09f,midpoint.z+20,0,-1,-1,level<1)?.stroke==slopeEdge.stroke,"sloped ramp can be selected at its actual height");
            Check(c.roads.FindPath(new List<int>{upper.a},new List<int>{surface.a}).Count==0,"disconnected layers cannot route into each other");
            int origin=TestCity.Build(c,-42,7,LandUse.Industrial),dest=TestCity.Build(c,40,7,LandUse.Commercial);
            c.Inventory(origin).Stock=8;c.society.settings.applicantsPerDay=0;c.traffic.dispatchTimer=-10000;
            var sim=new CityTraffic(c);var trip=sim.Dispatch(origin,dest,TripPurpose.Delivery);Check(trip!=null && trip.route.Any(id=>Math.Abs(c.roads.Node(id).y-level)<.01f),"real freight routes over/under crossing");
            sim.Advance(2);c.traffic.dispatchTimer=0;var restored=TestCity.RoundTrip(c);var twin=new CityTraffic(restored);c.traffic.dispatchTimer=-10000;restored.traffic.dispatchTimer=-10000;
            sim.Advance(40);twin.Advance(40);
            Check(c.Goods(dest)==8 && restored.Goods(dest)==8 && c.traffic.lostGoods==0,"delivery/return after save retains cargo");
            c.traffic.dispatchTimer=0;restored.traffic.dispatchTimer=0;Check(c.Valid() && restored.Valid(),"new format valid");
            int funds=c.money;Check(c.UpgradeRoad(upper.id,RoadKind.FourLane,false,out string reason,TestCity.Flat),"deck widening: "+reason);
            Check(c.money<funds && c.roads.edges.Where(e=>e.stroke==deck.stroke).All(e=>e.structure==(level>1?RoadStructure.Elevated:RoadStructure.Tunnel)),"paid upgrade preserves structure");
            var ids=c.roads.edges.Where(e=>e.stroke==deck.stroke).Select(e=>e.id).ToList();c.RemoveRoads(ids,0);
            Check(c.roads.edges.Any(e=>e.id==surface.id),"deleting selected deck retains the crossing road on the other level");
            Check(c.roads.FindPath(c.AccessBuilding(origin),c.AccessBuilding(dest)).Count==0,"demolition really cuts route");
        }
        var city=TestCity.Empty();city.money=200000;Build(city,P(-20,0),P(20,0));
        Check(!city.roads.Plan(city,P(0,-20,2),P(0,20,2),TestCity.Flat,engineered:true).Valid,"insufficient clearance rejected");
        Check(!city.roads.Plan(city,P(-20,5),P(-10,5,7),TestCity.Flat,engineered:true).Valid,"steep ramp rejected");
        Check(city.roads.Plan(city,P(-20,-10,7),P(20,-10,7),(x,z)=>-2,engineered:true).Valid,"bridge spans water");
        Check(!city.roads.Plan(city,P(-20,-10,1),P(20,-10,1),(x,z)=>-2,engineered:true).Valid,"low bridge rejected");
        Check(city.roads.Plan(city,P(-20,-10,-5),P(20,-10,-5),(x,z)=>-2,engineered:true).Valid,"underwater tunnel cover");
        var curve=city.roads.PlanCurve(city,P(-20,-20,7),P(0,-5,7),P(20,-20,7),TestCity.Flat,engineered:true);
        Check(curve.Valid && city.CommitRoad(curve) && city.roads.edges.Where(e=>e.stroke==curve.stroke).All(e=>Math.Abs(city.roads.Node(e.a).y-7)<.01f),"curved deck stays elevated");
        var legacy=TestCity.Empty();Build(legacy,legacy.roads.Node(CityRoads.Entrance),P(20,1.5f));
        var serializer=new JavaScriptSerializer{MaxJsonLength=int.MaxValue};string oldJson=serializer.Serialize(legacy.ToSaveData()).Replace("\"format\":30","\"format\":29").Replace(",\"structure\":0","");
        var old=serializer.Deserialize<CitySaveData>(oldJson).ToCity();Check(old.Valid() && old.roads.edges.All(e=>e.structure==RoadStructure.Ground),"real format29 JSON without structure defaults to ground");
        var built=TestCity.Create();built.money=200000;
        var factory=built.buildings.First(b=>b.Use==LandUse.Industrial);var start=P(factory.x-8,factory.z,-5);var end=P(factory.x+8,factory.z,-5);
        Check(built.roads.Plan(built,start,end,TestCity.Flat,engineered:true).Valid,"deep tunnel can pass beneath actual building");
        start.y=end.y=7;Check(!built.roads.Plan(built,start,end,TestCity.Flat,engineered:true).Valid,"elevated road still reserves building footprint");
        start.y=end.y=.8f;Check(!built.roads.Plan(built,start,end,TestCity.Flat,engineered:true).Valid,"insufficient building cover rejected");
        var invalid=TestCity.RoundTrip(city);invalid.roads.edges[0].structure=(RoadStructure)99;Check(!invalid.Valid(),"invalid structure rejected");
        Console.WriteLine("PASS: "+checks+" elevation/bridge/tunnel checks");
    }
}
