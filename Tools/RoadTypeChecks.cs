using System;
using System.Linq;
using System.Collections.Generic;
using HarborCity;
public static class RoadTypeChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Road types: "+why);checks++;}
    public static CityModel Gravel()
    {
        var c=TestCity.Empty();var p=c.roads.Plan(c,c.roads.Node(CityRoads.Entrance),TestCity.P(45,1.5f),TestCity.Flat,false,RoadKind.Gravel);
        if(!c.CommitRoad(p))throw new Exception(p.error);return c;
    }
    static float Travelled(CityModel c,TrafficTrip t)
    {float d=0;for(int i=0;i<t.segment;i++)d+=c.roads.EdgeLength(t.route[i],t.route[i+1]);return d+t.progress*c.roads.EdgeLength(t.Current,t.Next);}
    public static void Run()
    {
        var c=TestCity.Empty();var a=c.roads.Node(CityRoads.Entrance);var b=TestCity.P(45,1.5f);
        var gravel=c.roads.Plan(c,a,b,TestCity.Flat,false,RoadKind.Gravel);var paved=c.roads.Plan(c,a,b,TestCity.Flat);
        Check(gravel.Valid && paved.Valid && gravel.cost*2==paved.cost,"Equal-length gravel costs half as much to build");
        Check(!c.roads.Plan(c,a,b,TestCity.Flat,true,RoadKind.Gravel).Valid && !c.roads.PlanCurve(c,a,TestCity.P(-20,20),b,TestCity.Flat,true,RoadKind.Gravel).Valid,"Unsupported one-way gravel is rejected for straight and curved roads");
        int funds=c.money;Check(c.CommitRoad(gravel) && c.money==funds-gravel.cost && c.roads.edges.All(e=>e.kind==RoadKind.Gravel),"Actual gravel graph and charge commit together");
        int upkeep=c.RoadMaintenance;int id=c.roads.edges[0].id;var ids=c.roads.edges.Select(e=>e.id).ToArray();var zones=c.ZoningCells(TestCity.Flat).Select(z=>z.key).ToArray();
        Check(c.PreviewRoadUpgrade(id,RoadKind.Standard,false,out int cost,out _) && cost==paved.cost-gravel.cost,"Paving preview charges actual length and cost difference");
        c.money=0;string before=TestCity.Snapshot(c);Check(!c.UpgradeRoad(id,RoadKind.Standard,false,out _) && before==TestCity.Snapshot(c),"Insufficient funds cannot partially edit roads or money");
        c.money=funds;Check(c.UpgradeRoad(id,RoadKind.Standard,false,out _) && c.money==funds-cost && c.RoadMaintenance>upkeep && c.Valid(),"Paid paving changes real upkeep and valid state");
        Check(ids.SequenceEqual(c.roads.edges.Select(e=>e.id)) && zones.SequenceEqual(c.ZoningCells(TestCity.Flat).Select(z=>z.key)),"Same-width upgrade preserves graph IDs and planning cells");
        before=TestCity.Snapshot(c);Check(!c.UpgradeRoad(id,RoadKind.Standard,false,out _) && before==TestCity.Snapshot(c),"Repeated upgrade is rejected without repeated payment");
        funds=c.money;Check(c.UpgradeRoad(id,RoadKind.Gravel,false,out _) && c.money==funds && c.RoadMaintenance==upkeep,"Provisional downgrade produces no cash or upkeep residue");
        before=TestCity.Snapshot(c);Check(!c.ChangeRoadDirection(id,true,false,out _) && !c.UpgradeRoad(id,RoadKind.Gravel,true,out _) && before==TestCity.Snapshot(c),"Direction editing cannot create unsupported gravel type");
        var mid=CityRoads.Lerp(c.roads.Node(c.roads.edges[5].a),c.roads.Node(c.roads.edges[5].b),.5f);
        var branch=c.roads.Plan(c,mid,TestCity.P(mid.x,20),TestCity.Flat);Check(c.CommitRoad(branch) && c.roads.edges.Where(e=>e.stroke==1).All(e=>e.kind==RoadKind.Gravel),"Crossing splits retain original material");
        var copied=TestCity.RoundTrip(c);Check(copied.Valid() && copied.roads.edges.Count(e=>e.kind==RoadKind.Gravel)==c.roads.edges.Count(e=>e.kind==RoadKind.Gravel) && copied.RoadMaintenance==c.RoadMaintenance,"Mixed surfaces survive save and upkeep recalculation");
        c=Gravel();a=c.roads.Node(CityRoads.Entrance);var points=new[]{a,TestCity.P(a.x,10),TestCity.P(45,10),TestCity.P(45,1.5f)};
        for(int i=1;i<points.Length;i++)if(!c.CommitRoad(c.roads.Plan(c,points[i-1],points[i],TestCity.Flat)))throw new Exception("Detour fixture failed");
        int end=c.roads.nodes.First(n=>CityRoads.Length(n,points[3])<.01f).id;
        var car=c.roads.FindPath(new List<int>{a.id},new List<int>{end});var foot=c.roads.FindPath(new List<int>{a.id},new List<int>{end},true);
        Check(car.Any(n=>c.roads.Node(n).z>8) && foot.All(n=>Math.Abs(c.roads.Node(n).z-1.5f)<.01f),"Cars take longer but quicker paved detour; walkers use short route");
        c=Gravel();int origin=TestCity.Build(c,-35,7,LandUse.Industrial),destination=TestCity.Build(c,35,7,LandUse.Commercial);c.Inventory(origin).Stock=8;c.society.settings.applicantsPerDay=0;
        var sim=new CityTraffic(c);var t=sim.Dispatch(origin,destination,TripPurpose.Delivery);float estimate=c.TravelMinutes(origin,destination),footEstimate=c.TravelMinutes(origin,destination,true);
        var fast=TestCity.RoundTrip(c);var fastTrip=fast.traffic.trips.Single();float position=fastTrip.progress;funds=fast.money;
        Check(fast.UpgradeRoad(fast.roads.edges[0].id,RoadKind.Standard,false,out _) && fastTrip.progress==position && fastTrip.id==t.id && fastTrip.route.SequenceEqual(t.route),"Paving under moving tasks preserves vehicle and route position");
        Check(fast.TravelMinutes(origin,destination)<estimate && fast.TravelMinutes(origin,destination,true)==footEstimate,"Upgrade invalidates commute estimates without changing pedestrian cost");
        before=TestCity.Snapshot(fast);Check(!fast.UpgradeRoad(fast.roads.edges[0].id,RoadKind.Standard,true,out _) && before==TestCity.Snapshot(fast),"Occupied directional upgrade remains atomic and protected");
        var fastSim=new CityTraffic(fast);sim.Advance(2);fastSim.Advance(2);
        Check(Math.Abs(Travelled(c,t)/Travelled(fast,fastTrip)-.75f)<.005f,"Real freight moves at the surface speed, not a cosmetic indicator: "+Travelled(c,t)+" / "+Travelled(fast,fastTrip));
        copied=TestCity.RoundTrip(c);var savedSim=new CityTraffic(copied);
        sim.Advance(60);savedSim.Advance(60);fastSim.Advance(60);
        Check(c.Goods(destination)>=8 && fast.Goods(destination)>=8 && c.traffic.failed==0 && fast.traffic.failed==0 && c.traffic.trips.Count==0 && fast.traffic.trips.Count==0,"Both surface types actually deliver cargo and complete return");
        Check(TestCity.Snapshot(c)==TestCity.Snapshot(copied),"Saved slow-road freight continuation remains identical");
        var old=fast.ToSaveData();old.format=25;Check(old.ToCity().Valid() && old.ToCity().roads.edges.All(e=>e.kind==RoadKind.Standard),"Format 25 ordinary roads preserve entity state");
        var damaged=TestCity.RoundTrip(c);damaged.roads.edges[0].kind=(RoadKind)99;Check(!damaged.Valid(),"Unknown road profiles cannot enter valid saves");
        c=Gravel();origin=TestCity.Build(c,-35,7,LandUse.Industrial);destination=TestCity.Build(c,35,7,LandUse.Commercial);c.Inventory(origin).Stock=8;
        sim=new CityTraffic(c);t=sim.Dispatch(origin,destination,TripPurpose.Delivery);var split=TestCity.RoundTrip(c);var splitTrip=split.traffic.trips.Single();
        mid=CityRoads.Lerp(split.roads.Node(splitTrip.route[4]),split.roads.Node(splitTrip.route[5]),.5f);
        double oldWeekly=c.roads.edges.Sum(e=>c.roads.EdgeLength(e.a,e.b)/CityRoadTypes.CellLength*CityRoadTypes.UpkeepPerCellWeek(e.kind));
        if(!split.CommitRoad(split.roads.Plan(split,mid,TestCity.P(mid.x,20),TestCity.Flat)))throw new Exception("Split speed fixture failed");
        double splitWeekly=split.roads.edges.Where(e=>e.stroke==1).Sum(e=>split.roads.EdgeLength(e.a,e.b)/CityRoadTypes.CellLength*CityRoadTypes.UpkeepPerCellWeek(e.kind));
        Check(Math.Abs(oldWeekly-splitWeekly)<.0001,"Splitting a road does not invent recurring maintenance");
        var splitSim=new CityTraffic(split);sim.Advance(2);splitSim.Advance(2);
        Check(Math.Abs(Travelled(c,t)-Travelled(split,splitTrip))<.001f && split.Valid(),"Splitting an active route does not discard movement time or change real speed");
        Console.WriteLine("PASS: "+checks+" road materials/fees/upgrades/fast routing/actual freight/save checks");
    }
}
