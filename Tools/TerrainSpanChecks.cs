using System;
using System.Linq;
using System.Collections.Generic;
using HarborCity;
public static class TerrainSpanChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Terrain spans: "+why);checks++;}
    public static float River(float x,float z)=>Math.Abs(x)<10?-2:4;
    public static float Hill(float x,float z)=>Math.Abs(x)<10?9:4;
    public static float LowRiver(float x,float z)=>Math.Abs(x)<10?-2:1;
    public static void Run()
    {
        foreach(var height in new Func<float,float,float>[] {River,Hill})
        foreach(var kind in new[]{RoadKind.Standard,RoadKind.FourLane})
        foreach(bool curve in new[]{false,true})
        {
            var c=TestCity.Empty();c.money=200000;c.SetEntranceHeight(height);
            var from=new RoadNode{x=-24,z=1.5f,y=4};var to=new RoadNode{x=24,z=1.5f,y=4};
            var control=curve?new RoadNode{x=0,z=10,y=4}:null;
            var p=c.roads.PlanTerrainSpan(c,from,to,height,kind:kind,control:control);
            Check(p.Valid && p.engineered && p.automaticSpan && p.grade<.001f,"Two banks/portals use deck grade, straight and curved: "+p.error);
            Check(p.cost>CityRoadTypes.ConstructionCost(p.length,kind),"Bridge/tunnel expense participates in quoted total");
            Check(c.CommitRoad(p),"Automatic mixed-structure stroke actually builds");
            var structure=height(0,0)<4?RoadStructure.Elevated:RoadStructure.Tunnel;
            Check(c.roads.edges.Any(e=>e.structure==structure) && c.roads.edges.Any(e=>e.structure==RoadStructure.Ground),"Solid banks/portals and inner bridge/tunnel recorded separately");
            Check(c.roads.nodes.All(n=>Math.Abs(n.y-4)<.001f),"Road height does not drop to riverbed or climb hill");
            Check(TestCity.RoundTrip(c).Valid(),"Mixed structure saves using existing format");
            var ids=c.roads.Run(c.roads.edges.Last().id);Check(c.RemoveRoads(ids,0) && c.roads.edges.Count==0,"Whole automatic stroke remains undoable/demolishable");
        }
        var city=TestCity.Empty();city.money=200000;
        var a=new RoadNode{x=-20,z=0,y=1};var b=new RoadNode{x=20,z=0,y=1};
        var low=city.roads.PlanTerrainSpan(city,a,b,(x,z)=>Math.Abs(x)<10?-2:1);
        Check(low.Valid && low.points.Any(p=>p.y>=1.5f) && low.points.First().y==1 && low.points.Last().y==1 && low.grade<=.35f,"Low dry banks receive real bridge approaches without moving endpoints");
        Check(!city.roads.PlanTerrainSpan(city,new RoadNode{x=-10.5f,z=0,y=.2f},new RoadNode{x=10.5f,z=0,y=.2f},(x,z)=>Math.Abs(x)<10?-2:.2f).Valid,"Too little dry approach distance still rejected");
        b.y=25;Check(!city.roads.PlanTerrainSpan(city,a,b,(x,z)=>x<0?1:25).Valid,"Endpoint grade still enforced");
        b.y=1;Check(!city.roads.PlanTerrainSpan(city,a,b,(x,z)=>-2).Valid,"Cannot automatically create wet ground endpoints");
        var normal=city.roads.PlanTerrainSpan(city,a,b,TestCity.Flat);
        Check(normal.Valid && !normal.engineered && !normal.automaticSpan,"Ordinary flat construction stays ordinary");
        foreach(var height in new Func<float,float,float>[] {River,Hill,LowRiver})
        {
            var c=TestCity.Empty();c.money=200000;c.SetEntranceHeight(height);
            var from=c.roads.Node(CityRoads.Entrance);var end=new RoadNode{x=32,z=1.5f,y=height(32,1.5f)};
            var p=c.roads.PlanTerrainSpan(c,from,end,height);Check(c.CommitRoad(p),"Actual freight span connected from outside");
            int factory=TestCity.Build(c,-23,7,LandUse.Industrial),shop=TestCity.Build(c,23,7,LandUse.Commercial);
            c.Inventory(factory).Stock=8;c.society.settings.applicantsPerDay=0;c.traffic.dispatchTimer=-10000;
            var sim=new CityTraffic(c);var t=sim.Dispatch(factory,shop,TripPurpose.Delivery);
            Check(t!=null && t.route.Any(id=>Math.Abs(c.roads.Node(id).x)<10),"Actual loaded truck routes through automatic bridge/tunnel");
            sim.Advance(2);c.traffic.dispatchTimer=0;var saved=TestCity.RoundTrip(c);var resumed=new CityTraffic(saved);
            c.traffic.dispatchTimer=saved.traffic.dispatchTimer=-10000;sim.Advance(40);resumed.Advance(40);
            c.traffic.dispatchTimer=saved.traffic.dispatchTimer=0;
            Check(c.traffic.delivered==8 && c.traffic.failed==0 && !c.traffic.trips.Contains(t) && c.Goods(shop)==8,"Goods physically delivered and truck returns");
            Check(c.Valid() && TestCity.Snapshot(c)==TestCity.Snapshot(saved),"In-flight save continues deterministically across automatic span");
        }
        Console.WriteLine("PASS: "+checks+" automatic terrain-span checks");
    }
}
