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
        var c=CityModel.Create(); var sim=new CityTraffic(c);
        c.EnableRoads(Flat); c.EnableBuildings(); c.EnableHouseholds(); c.EnableResidentTransport();
        Check(c.Valid() && c.tiles.All(t=>t==0) && c.roads.edges.Count==0 && c.population==0 && c.jobs==0 && c.society.families.Count==0,"New game is an empty valid city");
        sim.Advance(121);
        Check(c.population==0 && c.traffic.trips.Count==0 && c.money==65000,"Empty map creates no residents, traffic or expenses");
        var entry=c.roads.Node(CityRoads.Entrance);
        Check(c.roads.Snap(entry.x+.2f,entry.z,Flat).id==entry.id && entry.y==1,"Unbuilt outside anchor follows terrain and accepts snapping");
        var plan=c.roads.Plan(c,entry,P(-12,1.5f),Flat);
        Check(c.CommitRoad(plan),"First player road can connect an empty map: "+plan.error);
        foreach(var use in new[]{LandUse.Power,LandUse.Water,LandUse.Commercial,LandUse.Industrial,LandUse.Residential})
        {
            var lots=c.RoadsideLots();
            if(use==LandUse.Residential) foreach(var lot in lots)
            {lot.housing=HousingKind.Apartment; lot.depth=5.9f; var center=lot.Point(0,1.5f); lot.x=center.x; lot.z=center.z;}
            var choice=lots.First(l=>c.CanBuild(l,Flat,out _));
            Check(c.PlaceBuilding(choice,use,Flat,out _)>=0,"Player constructs "+use+" on new map");
        }
        sim.Advance(1200);
        Check(c.population>0 && c.Employed>0 && c.society.history.Any(d=>d.wages>0) && c.Valid(),"Player-built city attracts residents with actual jobs, trips and wages");
        Check(c.buildings.Where(b=>c.tiles[b.id]==2).All(b=>b.housing==HousingKind.Apartment && !b.legacy && b.housingUnits==8),"New map uses standard player housing only");
        c.money=65000;
        plan=c.roads.Plan(c,P(-12,1.5f),P(78,1.5f),Flat);
        Check(c.CommitRoad(plan),"Road extends beyond the former map boundary: "+plan.error);
        var expanded=c.RoadsideLots().First(l=>l.x>60 && c.CanBuild(l,Flat,out _));
        int expandedId=c.PlaceBuilding(expanded,LandUse.Industrial,Flat,out _);
        Check(expandedId>=0 && c.BuildingAccess(expandedId) && c.PickBuilding(expanded.x,expanded.z)==expandedId,"Expanded region supports building, road access and picking");
        Check(c.Valid() && sim.FindRoute(expandedId,CityTraffic.Outside).Count>1,"Expanded buildings remain saveable and connected to outside traffic");
    }
    public static void Run()
    {
        CheckEmptyStart();
        var c=CityModel.CreateLegacySample(); var traffic=new CityTraffic(c); traffic.Advance(8);
        int pop=c.population,jobs=c.jobs,stock=c.traffic.stock.Sum(),tripCount=c.traffic.trips.Count;
        c.EnableRoads(Flat); c.EnableBuildings();
        Check(c.Valid() && c.version==3,"v1/v2 migrate to valid v3");
        Check(c.population==pop && c.jobs==jobs && stock==c.traffic.stock.Sum() && tripCount==c.traffic.trips.Count,"Migration retains economy, goods and active trips");
        Check(c.buildings[625].x==CityRoads.Lot(625).x && c.buildings[625].yaw==0,"Existing buildings keep their pose");
        traffic.Advance(90); Check(c.traffic.completed>10 && c.Valid(),"Migrated trips continue");
        var plan=c.roads.Plan(c,P(-45,1.5f),P(-30,-24),Flat);
        Check(c.CommitRoad(plan),"Diagonal street built: "+plan.error);
        var candidates=c.RoadsideLots().Where(b=>Math.Abs(b.yaw%90)>1 && c.CanBuild(b,Flat,out _)).ToList();
        Check(candidates.Count>=10,"Continuous diagonal frontage on both sides");
        var lot=candidates[candidates.Count/2];
        int id=c.PlaceBuilding(lot,LandUse.Residential,Flat,out string error);
        Check(id>=1296 && c.Valid(),"New independent entity beyond original grid: "+error);
        Check(c.PickBuilding(lot.x,lot.z)==id && c.BuildingAccess(id),"Rotated picking and road entrance");
        Check(c.PlaceBuilding(lot,LandUse.Commercial,Flat,out _) == -1,"Cannot overlap a rotated building");
        c.levels[id]=1; c.Recalculate();
        Check(c.population==pop+12,"Independent building participates in economy");
        Check(traffic.FindRoute(id,CityTraffic.Outside).Count>1,"New building has an outside route");
        var trip=traffic.Dispatch(id,CityTraffic.Outside,TripPurpose.Commute);
        Check(trip!=null && trip.home==id,"Traffic can address IDs outside original grid");
        traffic.Advance(120); Check(c.Valid(),"New traffic IDs remain saveable");
        var cross=c.roads.Plan(c,P(lot.x-5,lot.z-5),P(lot.x+5,lot.z+5),Flat);
        Check(!cross.Valid,"Road cannot cut through rotated footprint");
        var oldX=lot.x; var oldYaw=lot.yaw;
        var edge=c.roads.edges.First(e=>e.stroke==plan.stroke);
        c.roads.Remove(c.roads.edges.Where(e=>e.stroke==plan.stroke).Select(e=>e.id).ToList()); c.Recalculate();
        Check(!c.BuildingAccess(id) && lot.x==oldX && lot.yaw==oldYaw,"Road deletion disconnects but never moves buildings");
        Check(c.DemolishBuilding(id) && c.PickBuilding(lot.x,lot.z)==-1,"Demolish independent building");
        int second=c.PlaceBuilding(c.RoadsideLots().First(b=>c.CanBuild(b,Flat,out _)),LandUse.Park,Flat,out _);
        Check(second>id,"Demolition never reuses a trip endpoint ID");
        Check(!c.CanBuild(new CityBuilding{x=0,z=0},(x,z)=>0,out _),"Water rejected");
        Check(!c.CanBuild(new CityBuilding{x=0,z=0},(x,z)=>x*3+10,out _),"Steep footprint rejected");
        Check(!c.CanBuild(new CityBuilding{x=CityModel.BuildHalfSize,z=CityModel.BuildHalfSize,yaw=45},Flat,out _),"Rotated corners cannot exceed map");
        c.buildings[second].x=float.NaN; Check(!c.Valid(),"Corrupt pose rejected");
        var a=new CityBuilding{x=0,z=0,yaw=45}; var b=new CityBuilding{x=3,z=3,yaw=45};
        Check(!a.Overlaps(b) && !b.Overlaps(a),"Separated rotated boxes");
        b.x=b.z=1; Check(a.Overlaps(b) && b.Overlaps(a),"Rotated intersection is symmetric");
        Console.WriteLine("PASS: "+checks+" building checks");
    }
}
