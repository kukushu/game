using System;
using System.Linq;
using System.Collections.Generic;
using HarborCity;
public static class HighwayChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Highway: "+why);checks++;}
    static void Road(CityModel c,RoadNode a,RoadNode b,RoadKind kind)
    {var plan=c.roads.Plan(c,a,b,TestCity.Flat,CityRoadTypes.LimitedAccess(kind),kind);Check(plan.Valid && c.CommitRoad(plan),"Build "+kind+": "+plan.error);}
    public static CityModel Loop()
    {
        var c=TestCity.Empty();var a=TestCity.P(-45,1.5f);var b=TestCity.P(40,1.5f);var d=TestCity.P(40,25);var e=TestCity.P(-45,25);
        Road(c,c.roads.Node(CityRoads.Entrance),a,RoadKind.Standard);Road(c,a,b,RoadKind.Highway);
        Road(c,b,d,RoadKind.Ramp);Road(c,d,e,RoadKind.Standard);Road(c,e,a,RoadKind.Ramp);return c;
    }
    static void Place(CityModel c,TrafficTrip t,float x,int lane)
    {
        int s=t.route.FindIndex(n=>c.roads.Node(n).x<=x && t.route.IndexOf(n)+1<t.route.Count && c.roads.Node(t.route[t.route.IndexOf(n)+1]).x>x);
        if(s<0)throw new Exception("Missing highway approach");
        var a=c.roads.Node(t.route[s]);var b=c.roads.Node(t.route[s+1]);t.segment=s;t.progress=(x-a.x)/(b.x-a.x);t.lane=t.previousLane=t.laneChangeFrom=lane;
    }
    public static void Run()
    {
        var c=Loop();var highway=c.roads.edges.First(e=>e.kind==RoadKind.Highway);var ramp=c.roads.edges.First(e=>e.kind==RoadKind.Ramp);
        var lanes=c.roads.Lanes(highway.a,highway.b).ToList();
        Check(lanes.Count==3 && lanes.Select(l=>l.id).Distinct().Count()==3 && lanes.Select(l=>l.offset).Distinct().Count()==3,"Three distinct real forward lanes");
        Check(!c.roads.CanTravel(highway.b,highway.a) && c.roads.LaneCount(highway.b,highway.a)==0,"Highway has no reverse lane");
        Check(c.roads.LaneCount(ramp.a,ramp.b)==1 && c.roads.LaneOffset(ramp.a,ramp.b,0)==0,"Ramp is one centered physical lane");
        Check(c.roads.edges.SelectMany(e=>c.roads.Lanes(e.a,e.b).Concat(c.roads.Lanes(e.b,e.a))).Select(l=>l.id).Distinct().Count()==c.roads.edges.Sum(e=>CityRoadTypes.Lanes(e.kind,e.oneWay)*(e.oneWay?1:2)),"Lane IDs do not alias on descending node IDs or across edges");
        Check(!c.roads.CanTravel(highway.a,highway.b,true) && !c.roads.CanTravel(ramp.a,ramp.b,true),"No walking along limited-access roads");
        Check(c.roads.FindPath(new List<int>{CityRoads.Entrance},new List<int>{ramp.b},true).Count==0,"Walkers do not route through a highway/ramp loop");
        Check(!c.roads.nodes.Any(n=>c.roads.HasSignals(n.id)),"Highway width alone does not spawn traffic lights");
        Check(c.ZoningCells(TestCity.Flat).Count>0 && c.ZoningCells(TestCity.Flat).All(z=>z.pose.z>15 || z.pose.x<-40),"Zoning remains on ordinary streets, not highway/ramp frontage");
        Check(c.RoadsidePreview(0,7,LandUse.Residential,HousingKind.Apartment,out _)==null,"Cannot directly place houses beside the highway");
        Check(!c.roads.Plan(c,TestCity.P(0,-20),TestCity.P(20,-20),TestCity.Flat,false,RoadKind.Highway).Valid
            && !c.roads.Plan(c,TestCity.P(0,-20),TestCity.P(20,-20),TestCity.Flat,false,RoadKind.Ramp).Valid,"Both highway types require one-way direction");
        var shops=new[]{TestCity.Build(c,25,31,LandUse.Commercial),TestCity.Build(c,5,31,LandUse.Commercial),TestCity.Build(c,-20,31,LandUse.Commercial)};
        var sim=new CityTraffic(c);var trips=new List<TrafficTrip>();
        for(int i=0;i<3;i++){var t=sim.Dispatch(CityTraffic.Outside,shops[i],TripPurpose.Import);Check(t!=null,"Real city import dispatch");Place(c,t,-30,i);trips.Add(t);}
        sim=new CityTraffic(c);sim.Advance(.05f);
        Check(trips.All(t=>t.blocked==0) && trips.Select(t=>t.lane).Distinct().Count()==3 && c.Valid(),"Three real loaded imports advance in independent highway lanes");
        Check(trips.All(t=>Math.Abs(CityRoads.Lerp(c.roads.Node(t.Current),c.roads.Node(t.Next),t.progress).x-(-28.5f))<.01f),"Highway speed changes actual movement, 2.5 times ordinary paved roads");
        var saved=TestCity.RoundTrip(c);var resumed=new CityTraffic(saved);var before=TestCity.Snapshot(c);
        Check(!c.ChangeRoadDirection(highway.id,false,false,out _) && before==TestCity.Snapshot(c),"No conversion to illegal two-way highway");
        Check(!c.UpgradeRoad(highway.id,RoadKind.Standard,false,out _,TestCity.Flat) && before==TestCity.Snapshot(c),"Occupied highway cannot be narrowed/direction-edited");
        for(int i=0;i<1600;i++){sim.Advance(.05f);resumed.Advance(.05f);}
        Check(shops.All(id=>c.Goods(id)>=8) && trips.All(t=>!c.traffic.trips.Any(p=>p.id==t.id)) && c.traffic.failed==0 && c.traffic.lostGoods==0 && c.Valid(),"Imports use off-ramp, deliver to real shops, return via on-ramp and finish");
        Check(TestCity.Snapshot(c)==TestCity.Snapshot(saved),"Third lane and merge save resumes identically");
        c=Loop();int shop=TestCity.Build(c,25,31,LandUse.Commercial);sim=new CityTraffic(c);
        var changing=sim.Dispatch(CityTraffic.Outside,shop,TripPurpose.Import);Place(c,changing,-30,1);
        var leader=sim.Dispatch(CityTraffic.Outside,shop,TripPurpose.Import);Place(c,leader,-28,1);
        sim=new CityTraffic(c);sim.Advance(.05f);
        Check(changing.lane==2 && changing.laneChangeFrom==1 && changing.laneChangeRemaining>0 && c.Valid(),"Highway safely changes one adjacent lane into the third lane");
        saved=TestCity.RoundTrip(c);resumed=new CityTraffic(saved);Check(saved.traffic.trips.First(t=>t.id==changing.id).laneChangeFrom==1,"Active third-lane change persists");
        sim.Advance(80);resumed.Advance(80);
        Check(c.Valid() && c.traffic.failed==0 && c.traffic.lostGoods==0 && c.Goods(shop)>=16 && TestCity.Snapshot(c)==TestCity.Snapshot(saved),"Saved third-lane maneuver delivers and merges without losing tasks");
        c=Loop();highway=c.roads.edges.First(e=>e.kind==RoadKind.Highway);
        var reversed=c.roads.edges.Last(e=>e.kind==RoadKind.Ramp);
        Check(c.ChangeRoadDirection(reversed.id,true,true,out _) && c.Valid(),"Empty ramp can reverse without changing its kind");
        Check(c.ChangeRoadDirection(highway.id,true,true,out _) && c.roads.edges.Where(e=>e.kind==RoadKind.Highway).All(e=>c.roads.Lanes(e.a,e.b).Select(l=>l.id).Distinct().Count()==3),"Reversed highway still has three unique lane IDs");
        c=TestCity.Empty();Road(c,c.roads.Node(CityRoads.Entrance),TestCity.P(30,1.5f),RoadKind.Standard);
        int id=c.roads.edges[5].id;var oldZones=c.ZoningCells(TestCity.Flat).Where(z=>z.pose.x>-10 && z.pose.x<10).ToList();c.PaintZones(oldZones,LandUse.Residential);
        int money=c.money;Check(c.PreviewRoadUpgrade(id,RoadKind.Highway,true,out int cost,out _,TestCity.Flat) && cost>0
            && c.UpgradeRoad(id,RoadKind.Highway,true,out _,TestCity.Flat) && c.money==money-cost && c.ZoningCells(TestCity.Flat).Count==0,"Paid upgrade removes highway zoning and changes real direction");
        Check(c.UpgradeRoad(id,RoadKind.Standard,false,out _,TestCity.Flat) && c.ZoningCells(TestCity.Flat).Count>0,"Downgrading restores ordinary street zoning");
        TestCity.Build(c,0,7,LandUse.Residential);before=TestCity.Snapshot(c);
        Check(!c.UpgradeRoad(id,RoadKind.Ramp,true,out _,TestCity.Flat) && before==TestCity.Snapshot(c),"Upgrade cannot silently strand an existing building entrance");
        var legacy=TestCity.Create().ToSaveData();legacy.format=32;Check(legacy.ToCity().Valid(),"Format 32 existing city remains readable");
        c=TestCity.Empty();var curve=c.roads.PlanCurve(c,c.roads.Node(CityRoads.Entrance),TestCity.P(-25,1.5f),TestCity.P(-5,25),TestCity.Flat,true,RoadKind.Highway);
        Check(curve.Valid && c.CommitRoad(curve) && c.roads.edges.All(e=>e.kind==RoadKind.Highway && e.oneWay) && TestCity.RoundTrip(c).Valid(),"Curved highway construction and native graph persistence");
        foreach(var kind in new[]{RoadKind.Highway,RoadKind.Ramp})
        foreach(float level in new[]{6f,-6f})
        {
            c=TestCity.Empty();var a=TestCity.P(-30,-25);a.y+=level;var b=TestCity.P(30,-25);b.y+=level;
            var plan=c.roads.Plan(c,a,b,TestCity.Flat,true,kind,true);
            Check(plan.Valid && c.CommitRoad(plan) && c.roads.edges.All(e=>e.structure==(level>0?RoadStructure.Elevated:RoadStructure.Tunnel)) && TestCity.RoundTrip(c).Valid(),"Existing bridge/tunnel construction supports "+kind);
            var ids=c.roads.Run(c.roads.edges.First().id);Check(c.RemoveRoads(ids,0) && c.roads.edges.Count==0,"Existing demolition removes limited-access bridge/tunnel");
        }
        Console.WriteLine("PASS: "+checks+" highway/ramp/direction/frontage/three-lane freight/save/elevation checks");
    }
}
