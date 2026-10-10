using System;
using System.Linq;
using HarborCity;

public static class BuildingChecks
{
    static int checks;
    static float Flat(float x,float z)=>1;
    static RoadNode P(float x,float z)=>new RoadNode{x=x,z=z,y=1};
    static void Check(bool ok,string message) { if(!ok) throw new Exception("Buildings: "+message); checks++; }
    static void CheckEmptyStart()
    {
        var c=TestCity.Empty(); var sim=new CityTraffic(c);
        c.SetEntranceHeight(Flat);
        Check(c.Valid() && c.buildings.Count==0 && c.roads.edges.Count==0 && c.population==0 && c.jobs==0 && c.society.families.Count==0,"New game is an empty valid city");
        sim.Advance(121);
        Check(c.population==0 && c.traffic.trips.Count==0 && c.money==65000,"Empty map creates no residents, traffic or expenses");
        var entry=c.roads.Node(CityRoads.Entrance);
        Check(c.roads.Snap(entry.x+.2f,entry.z,Flat).id==entry.id && entry.y==1,"Unbuilt outside anchor follows terrain and accepts snapping");
        var plan=c.roads.Plan(c,entry,P(-12,1.5f),Flat);
        Check(c.CommitRoad(plan),"First player road can connect an empty map: "+plan.error);
        int frontage=0;
        foreach(var use in new[]{LandUse.Power,LandUse.Water,LandUse.Commercial,LandUse.Industrial,LandUse.Residential})
        {
            var choice=c.RoadsidePreview(-48.137f+frontage++*7.013f,6,use,HousingKind.Apartment,out _);
            Check(c.PlaceBuilding(choice,use,Flat,out _)>=0,"Player constructs "+use+" on new map");
        }
        sim.Advance(1200);
        Check(c.population>0 && c.Employed>0 && c.society.history.Any(d=>d.wages>0) && c.Valid(),"Player-built city attracts residents with actual jobs, trips and wages");
        Check(c.buildings.OfType<IndustrialBuilding>().All(b=>b.factory.imported>0 && b.factory.produced>0 && b.factory.wageCosts>0),"Continuously placed homes and factories support real worker attendance, raw trucks, production and payroll");
        Check(c.buildings.OfType<ResidentialBuilding>().All(b=>b.housing==HousingKind.Apartment && b.housingUnits==8),"New map uses standard player housing only");
        c.money=65000;
        plan=c.roads.Plan(c,P(-12,1.5f),P(78,1.5f),Flat);
        Check(c.CommitRoad(plan),"Road extends beyond the former map boundary: "+plan.error);
        var expanded=c.RoadsidePreview(65.137f,6,LandUse.Industrial,HousingKind.Apartment,out _);
        int expandedId=c.PlaceBuilding(expanded,LandUse.Industrial,Flat,out _);
        Check(expandedId>=0 && c.BuildingAccess(expandedId) && c.PickBuilding(expanded.x,expanded.z)==expandedId,"Expanded region supports building, road access and picking");
        Check(c.Valid() && sim.FindRoute(expandedId,CityTraffic.Outside).Count>1,"Expanded buildings remain saveable and connected to outside traffic");
    }
    public static void Run()
    {
        CheckEmptyStart();ContinuousPlacementChecks();
        Console.WriteLine("PASS: "+checks+" building placement checks");
    }
    static CityModel ContinuousCity(out CityTraffic sim)
    {
        var c=CityModel.Create(); sim=new CityTraffic(c); c.SetEntranceHeight(Flat);
        return c;
    }
    static CityBuilding Preview(CityModel c,float x,float z,LandUse use=LandUse.Commercial,HousingKind housing=HousingKind.Apartment)
        => c.RoadsidePreview(x,z,use,housing,out _);
    static void ContinuousPlacementChecks()
    {
        var c=ContinuousCity(out var sim);
        Check(Preview(c,0,0)==null,"No-road placement does not invent a lot");
        Check(c.CommitRoad(c.roads.Plan(c,c.roads.Node(CityRoads.Entrance),P(40,1.5f),Flat)),"Continuous placement fixture road built");
        int slots=c.buildings.Count,revision=c.roads.revision,money=c.money;
        var a=Preview(c,-30.137f,6.2f); var b=Preview(c,-30.127f,6.2f);
        Check(a!=null && Math.Abs(a.x+30.137f)<.0001f && Math.Abs(b.x-a.x-.01f)<.0001f,"Sub-centimetre mouse translation moves the centre continuously, with no 3-unit phase");

        Check(c.buildings.Count==slots && c.buildings.Count==slots && c.money==money && c.roads.revision==revision && a.id==-1,"Temporary previews do not allocate IDs, charge money or change the road graph");
        Check(Math.Abs(a.z-(1.5f+CityRoads.Width/2+a.depth/2+CityModel.BuildingSetback))<.0001f,"Centre uses road half-width, building half-depth and setback");
        var front=a.Point(0,-a.depth/2);
        Check(Math.Abs(front.x-a.entranceX)<.0001f && Math.Abs(front.z-a.entranceZ)<.0001f && Math.Abs(front.z-1.5f-(CityRoads.Width/2+CityModel.BuildingSetback))<.0001f,"Entrance is on the road-facing frontage within AccessBuilding reach");
        var opposite=Preview(c,-30.137f,-4);
        Check(opposite.z<1.5f && Math.Abs(Math.Abs(opposite.yaw)-180)<.001f && opposite.entranceZ>opposite.z,"Opposite side reverses frontage without disconnecting the entrance");
        Check(Preview(c,0,11.6f)==null && Preview(c,0,-8.6f)==null,"Ten-unit snap range rejects distant ground hits on both sides");
        var endpoint=Preview(c,44,5);
        Check(Math.Abs(endpoint.x-40)<.001f,"Projection clamps to edge endpoint rather than extending an imaginary road");
        var apartment=Preview(c,-20.347f,6,LandUse.Residential);
        var villa=Preview(c,-10.257f,6,LandUse.Residential,HousingKind.Villa);
        Check(apartment.width==2.9f && apartment.depth==5.9f && villa.width==5.9f && villa.depth==5.9f,"Housing dimensions remain compatible with current apartment and villa models");
        Check(Math.Abs(apartment.entranceZ-a.entranceZ)<.001f && Math.Abs(villa.entranceZ-a.entranceZ)<.001f,"Deeper housing still puts its entrance at the same road frontage");
        Check(c.CanBuild(a,Flat,out _) && c.CanBuild(opposite,Flat,out _),"Both continuous roadside poses pass the existing CanBuild");
        int id=c.PlaceBuilding(a,LandUse.Commercial,Flat,out _);
        Check(id==c.nextBuildingId-1 && c.BuildingAccess(id) && c.AccessBuilding(id).Count==1 && c.PickBuilding(a.x,a.z)==id,"Exact preview becomes a normal stable-ID building with traffic access");
        var overlap=Preview(c,a.x,6.2f);
        Check(overlap!=null && !c.CanBuild(overlap,Flat,out string overlapReason) && overlapReason.Contains("已有建筑"),"Overlaps produce a real rejected preview instead of hiding available placement");
        int before=c.buildings.Count; money=c.money;
        Check(c.PlaceBuilding(overlap,LandUse.Commercial,Flat,out _)<0 && c.buildings.Count==before && c.money==money,"Rejected continuous commit leaves IDs and funds unchanged");
        Check(!c.CanBuild(apartment,(x,z)=>0,out _),"Continuous preview preserves water validation");
        Check(!c.CanBuild(apartment,(x,z)=>x*2+100,out _),"Continuous preview preserves terrain slope validation");
        var copy=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<CommercialBuilding>(new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(a));
        Check(copy.id==a.id && copy.x==a.x && copy.yaw==a.yaw && copy.entranceX==a.entranceX && copy.entranceZ==a.entranceZ,"Arbitrary preview pose and entrance survive the existing serialized building fields");

        // Curved roads are ordinary short edges: use the nearest edge's tangent.
        c=ContinuousCity(out sim);
        var curve=c.roads.PlanCurve(c,c.roads.Node(CityRoads.Entrance),P(-32.5f,21.5f),P(-12.5f,1.5f),Flat);
        Check(c.CommitRoad(curve),"Curved continuous placement fixture built");
        var e=c.roads.edges[c.roads.edges.Count/3]; var start=c.roads.Node(e.a); var end=c.roads.Node(e.b);
        var p=CityRoads.Lerp(start,end,.437f); float len=CityRoads.Length(start,end),nx=-(end.z-start.z)/len,nz=(end.x-start.x)/len;
        var curved=Preview(c,p.x+nx*4,p.z+nz*4);
        var entrance=new RoadNode{x=curved.entranceX,z=curved.entranceZ};
        Check(Math.Abs(CityRoads.Distance(entrance,start,end)-(CityRoads.Width/2+CityModel.BuildingSetback))<.005f,"Curved frontage uses projected nearest-edge position and tangent");
        id=c.PlaceBuilding(curved,LandUse.Commercial,Flat,out string curveError);
        Check(id>=0 && c.BuildingAccess(id) && sim.FindRoute(-1,id).Count>1,"Continuous curved placement retains real road access: "+curveError);
        var truck=sim.Dispatch(-1,id,TripPurpose.Import);
        Check(truck!=null,"Freight can dispatch to a freely positioned curved-road building");
        c.society.settings.applicantsPerDay=0;
        for(int i=0;i<2000 && truck.cargo>0;i++) sim.Advance(.05f);
        Check(truck.cargo==0 && c.Inventory(id).Stock>=8,"Real truck arrival supplies the continuously placed curved-road commercial building");
        var fe=c.roads.edges[c.roads.edges.Count*2/3]; var fa=c.roads.Node(fe.a); var fb=c.roads.Node(fe.b);
        var fp=CityRoads.Lerp(fa,fb,.313f); float fl=CityRoads.Length(fa,fb);
        var factoryPose=Preview(c,fp.x-(fb.z-fa.z)/fl*4,fp.z+(fb.x-fa.x)/fl*4,LandUse.Industrial);
        int factory=c.PlaceBuilding(factoryPose,LandUse.Industrial,Flat,out string factoryError);
        Check(factory>=0 && c.BuildingAccess(factory),"Freely positioned curved-road factory retains its entrance: "+factoryError);
        c.Inventory(factory).Stock=8;
        var delivery=sim.Dispatch(factory,id,TripPurpose.Delivery);
        Check(delivery!=null,"Delivery can route between independently positioned factory and shop");
        for(int i=0;i<2000 && delivery.cargo>0;i++) sim.Advance(.05f);
        Check(delivery.cargo==0 && c.Inventory(id).Stock>=16 && c.Factory(factory).salesRevenue==360,"Continuous placement preserves actual commercial receipt and factory sales income");

        // A branch near a proposed pose is rejected by the same road overlap check.
        c=ContinuousCity(out sim);
        Check(c.CommitRoad(c.roads.Plan(c,c.roads.Node(CityRoads.Entrance),P(30,1.5f),Flat)),"Junction placement fixture main road built");
        Check(c.CommitRoad(c.roads.Plan(c,P(-2,1.5f),P(-2,30),Flat)),"Junction placement fixture branch built");
        var junction=Preview(c,0,1.8f);
        Check(junction!=null && !c.CanBuild(junction,Flat,out string junctionError) && junctionError.Contains("道路"),"Junction encroachment is rejected by CanBuild without special parcels");
        c=ContinuousCity(out sim);
        Check(c.CommitRoad(c.roads.Plan(c,P(0,82),P(30,82),Flat)),"Boundary placement fixture built");
        var outside=Preview(c,15,84);
        Check(outside!=null && !c.CanBuild(outside,Flat,out string boundaryError) && boundaryError.Contains("边界"),"Continuous preview retains rotated-footprint boundary validation");
    }
}
