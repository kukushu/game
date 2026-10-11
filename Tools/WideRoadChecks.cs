using System;
using System.Linq;
using HarborCity;
public static class WideRoadChecks
{
    static int checks;
    static void Check(bool value,string why){if(!value)throw new Exception("Wide roads: "+why);checks++;}
    static CityModel Street(RoadKind kind=RoadKind.Standard)
    {
        var c=TestCity.Empty();var p=c.roads.Plan(c,c.roads.Node(CityRoads.Entrance),TestCity.P(45,1.5f),TestCity.Flat,false,kind);
        if(!c.CommitRoad(p))throw new Exception(p.error);return c;
    }
    public static void Run()
    {
        var junctionCity=Street(RoadKind.FourLane);
        var north=junctionCity.roads.Plan(junctionCity,TestCity.P(0,1.5f),TestCity.P(0,25),TestCity.Flat,false,RoadKind.FourLane);
        Check(north.Valid && junctionCity.CommitRoad(north),"Four-lane branch connects to the middle of a sampled four-lane street: "+north.error);
        var south=junctionCity.roads.Plan(junctionCity,TestCity.P(0,-20),TestCity.P(0,1.5f),TestCity.Flat,false,RoadKind.FourLane);
        Check(south.Valid && junctionCity.CommitRoad(south) && junctionCity.Valid(),"Fourth wide arm joins existing T junction: "+south.error);
        var junctionSaved=TestCity.RoundTrip(junctionCity);Check(junctionSaved.Valid() && junctionSaved.roads.nodes.Any(n=>junctionSaved.roads.Neighbors(n.id).Count==4),"Connected wide crossroads retains real four-arm topology in save");
        foreach(float x in new[]{.4f,1.1f,2.2f})
        {
            junctionCity=Street(RoadKind.FourLane);var snapped=junctionCity.roads.Snap(x,1.5f,TestCity.Flat);north=junctionCity.roads.Plan(junctionCity,snapped,TestCity.P(snapped.x,25),TestCity.Flat,false,RoadKind.FourLane);
            Check(north.Valid && junctionCity.CommitRoad(north) && junctionCity.Valid(),"Connection is independent of internal three-unit sampling phase: "+x+" "+north.error);
        }
        junctionCity=Street(RoadKind.FourLane);north=junctionCity.roads.Plan(junctionCity,TestCity.P(0,1.5f),TestCity.P(18,19.5f),TestCity.Flat,false,RoadKind.FourLane);
        Check(north.Valid && junctionCity.CommitRoad(north) && junctionCity.Valid(),"Angled wide junction admits only its local connected footprint: "+north.error);
        junctionCity=Street(RoadKind.FourLane);var disconnected=junctionCity.roads.Plan(junctionCity,TestCity.P(4.5f,7.2f),TestCity.P(18,7.2f),TestCity.Flat,false,RoadKind.FourLane);Check(junctionCity.CommitRoad(disconnected),"Nearby separate wide street fixture has nonoverlapping pavement");
        string untouched=TestCity.Snapshot(junctionCity);north=junctionCity.roads.Plan(junctionCity,TestCity.P(0,1.5f),TestCity.P(10,20),TestCity.Flat,false,RoadKind.FourLane);
        Check(!north.Valid && north.error.Contains("邻近") && untouched==TestCity.Snapshot(junctionCity),"A genuine junction does not waive collision with a nearby unconnected road");
        var c=Street();int edge=c.roads.edges[0].id,funds=c.money;var ids=c.roads.edges.Select(e=>e.id).ToArray();
        var old=c.ZoningCells(TestCity.Flat);c.PaintZones(old.Where(z=>z.pose.x<0),LandUse.Residential);
        Check(c.PreviewRoadUpgrade(edge,RoadKind.FourLane,false,out int cost,out _,TestCity.Flat) && cost==1300,"Widening previews actual whole-street incremental construction cost");
        string before=TestCity.Snapshot(c);Check(!c.UpgradeRoad(edge,RoadKind.FourLane,false,out _) && before==TestCity.Snapshot(c),"Widening cannot bypass terrain verification");
        Check(c.UpgradeRoad(edge,RoadKind.FourLane,false,out _,TestCity.Flat) && c.money==funds-cost && ids.SequenceEqual(c.roads.edges.Select(e=>e.id)) && c.Valid(),"Paid widening preserves stable road IDs and valid city");
        var e=c.roads.edges[0];Check(c.roads.Lanes(e.a,e.b).Count()==2 && c.roads.Lanes(e.b,e.a).Count()==2 && c.roads.Lanes(e.a,e.b).Concat(c.roads.Lanes(e.b,e.a)).Select(l=>l.id).Distinct().Count()==4,"Wide street has four distinct directed lanes");
        Check(c.roads.LaneOffset(e.a,e.b,1)>1.6f && c.roads.LaneOffset(e.b,e.a,1)>1.6f && c.roads.EdgeWidth(e.a,e.b)>5,"Both directions occupy their own carriageway half");
        var zones=c.ZoningCells(TestCity.Flat);Check(zones.Any(z=>z.use==LandUse.Residential) && zones.All(z=>!z.pose.HitsRoad(c.roads.Node(e.a),c.roads.Node(e.b),CityRoadTypes.Width(e.kind))),"Widened land retains overlapping zoning paint outside new road footprint");
        var keys=zones.Select(z=>z.key).ToHashSet();Check(c.zoning.designations.All(d=>keys.Contains(d.key)),"Obsolete former roadside keys do not secretly regrow inside pavement");
        var copy=TestCity.RoundTrip(c);Check(copy.Valid() && copy.roads.edges.All(r=>r.kind==RoadKind.FourLane) && copy.ZoningCells(TestCity.Flat).Count==zones.Count,"Native entities, four-lane profile and roadside geometry survive JSON");
        before=TestCity.Snapshot(c);Check(!c.ChangeRoadDirection(edge,true,false,out _) && !c.UpgradeRoad(edge,RoadKind.FourLane,true,out _,TestCity.Flat) && before==TestCity.Snapshot(c),"Unsupported four-lane one-way type cannot enter the graph");
        int origin=TestCity.Build(c,-35,7,LandUse.Industrial);c.Inventory(origin).Stock=24;
        var lot=c.GetBuilding(origin);Check(c.BuildingAccess(origin) && Math.Abs(lot.entranceZ-1.5f)>CityRoads.Width/2 && c.CanBuild(c.RoadsidePreview(-10,-5,LandUse.Commercial,HousingKind.Apartment,out _),TestCity.Flat,out _),"Real buildings align with and access wider road edges");
        var sim=new CityTraffic(c);var a=sim.Dispatch(origin,CityTraffic.Outside,TripPurpose.Export);var b=sim.Dispatch(origin,CityTraffic.Outside,TripPurpose.Export);
        Check(a!=null && b!=null && a.lane!=b.lane && sim.Dispatch(origin,CityTraffic.Outside,TripPurpose.Export)==null,"Real bidirectional wide-road exports use two lanes and enforce entry capacity");
        sim.Advance(.05f);Check(a.blocked==0 && b.blocked==0 && Math.Abs(a.progress-b.progress)<.001f,"Actual freight advances in parallel without same-lane headway");
        before=TestCity.Snapshot(c);Check(!c.UpgradeRoad(edge,RoadKind.Standard,false,out _,TestCity.Flat) && before==TestCity.Snapshot(c),"Occupied narrowing preserves current second-lane vehicles and cargo");
        copy=TestCity.RoundTrip(c);var copySim=new CityTraffic(copy);int ai=a.id,bi=b.id;
        for(int i=0;i<2000;i++){sim.Advance(.05f);copySim.Advance(.05f);}
        Check(c.traffic.trips.All(t=>t.id!=ai && t.id!=bi) && c.traffic.completed>=2 && c.traffic.failed==0 && c.traffic.lostGoods==0 && c.Valid(),"Actual exports, return trips and narrow entrance merge finish without losing goods");
        Check(TestCity.Snapshot(c)==TestCity.Snapshot(copy),"In-flight wide-road saved city continues identically");
        c=Street();TestCity.Build(c,-20,7,LandUse.Residential);before=TestCity.Snapshot(c);
        Check(!c.UpgradeRoad(c.roads.edges[0].id,RoadKind.FourLane,false,out string why,TestCity.Flat) && why.Contains("建筑") && before==TestCity.Snapshot(c),"Existing housing blocks widening atomically instead of being moved or deleted");
        c=Street();c.development.enabled=true;c.developmentHeight=TestCity.Flat;c.PaintZones(c.ZoningCells(TestCity.Flat),LandUse.Residential);new CityTraffic(c).Advance(7);
        Check(c.development.projects.Count>0,"Actual construction fixture has active projects");before=TestCity.Snapshot(c);
        Check(!c.UpgradeRoad(c.roads.edges[0].id,RoadKind.FourLane,false,out _,TestCity.Flat) && before==TestCity.Snapshot(c),"Pending building projects block conflicting widening without cancellation");
        c=Street();float Shore(float x,float z)=>z>3.8f?0:1;before=TestCity.Snapshot(c);
        Check(!c.UpgradeRoad(c.roads.edges[0].id,RoadKind.FourLane,false,out why,Shore) && why.Contains("水面") && before==TestCity.Snapshot(c),"Full expanded road width is checked against water");
        c=Street();var parallel=c.roads.Plan(c,TestCity.P(-45,5.2f),TestCity.P(35,5.2f),TestCity.Flat);Check(c.CommitRoad(parallel),"Separate narrow streets can coexist before widening");before=TestCity.Snapshot(c);
        Check(!c.UpgradeRoad(c.roads.edges[0].id,RoadKind.FourLane,false,out why,TestCity.Flat) && why.Contains("邻近") && before==TestCity.Snapshot(c),"Wider footprint cannot silently overlap an unconnected parallel road");
        c=TestCity.Empty();Check(!c.roads.Plan(c,TestCity.P(-20,82),TestCity.P(20,82),TestCity.Flat,false,RoadKind.FourLane).Valid,"Wide construction checks road edges against map boundary");
        Check(!c.roads.Plan(c,TestCity.P(20,20),TestCity.P(83,20),TestCity.Flat,false,RoadKind.FourLane).Valid,"Wide road end cap also fits inside construction boundary");
        var curve=c.roads.PlanCurve(c,c.roads.Node(CityRoads.Entrance),TestCity.P(-20,1.5f),TestCity.P(20,25),TestCity.Flat,false,RoadKind.FourLane);
        Check(curve.Valid && c.CommitRoad(curve) && c.roads.edges.All(r=>r.kind==RoadKind.FourLane) && c.Valid(),"Wide curved construction creates real four-lane graph spans");
        c=Street();c.money=0;before=TestCity.Snapshot(c);Check(!c.UpgradeRoad(c.roads.edges[0].id,RoadKind.FourLane,false,out _,TestCity.Flat) && before==TestCity.Snapshot(c),"Insufficient funds leave geometry, land use and IDs unchanged");
        c=RoadDirectionChecks.Loop();origin=TestCity.Build(c,-35,7,LandUse.Industrial);c.Inventory(origin).Stock=16;sim=new CityTraffic(c);sim.Dispatch(origin,CityTraffic.Outside,TripPurpose.Export);b=sim.Dispatch(origin,CityTraffic.Outside,TripPurpose.Export);
        var legacy=c.ToSaveData();legacy.format=27;copy=legacy.ToCity();Check(copy.Valid() && copy.traffic.trips.First(t=>t.id==b.id).lane==1 && copy.roads.edges.All(r=>r.kind==RoadKind.Standard),"Previous format 27 preserves small-road profiles and real second-lane tasks");
        Console.WriteLine("PASS: "+checks+" wide-road construction/paid upgrade/zoning/real freight/collision/save checks");
    }
}
