using System;
using System.Linq;
using HarborCity;

public static class MapExpansionChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Map expansion: "+why);checks++;}
    public static void Run()
    {
        var c=TestCity.Empty();c.money=1000000;
        Check(c.map.Valid() && CityModel.BuildHalfSize==168 && CityMapState.TerrainHalfSize==240,"Four times build area with persisted map profile");
        Check(c.CommitRoad(c.roads.Plan(c,c.roads.Node(CityRoads.Entrance),TestCity.P(156,1.5f),TestCity.Flat)),"Normal roads reach new eastern land");
        var zones=c.ZoningCells(TestCity.Flat).Where(z=>z.pose.x>100).ToList();
        Check(zones.Count>0 && c.PaintZones(zones,LandUse.Commercial)>0,"New land has actual zone designations");
        int shop=TestCity.Build(c,145,7,LandUse.Commercial);
        Check(c.BuildingAccess(shop),"Stable-ID building on new land has real road access");
        Check(c.BuildUtility(UtilityKind.Water,TestCity.P(0,0),TestCity.P(155,0),out _)
            && c.BuildUtility(UtilityKind.Electricity,TestCity.P(0,0),TestCity.P(155,0),out _),"Pipes and power extend beyond old limits");
        Check(!c.roads.Plan(c,TestCity.P(156,1.5f),TestCity.P(168,1.5f),TestCity.Flat).Valid
            && !c.BuildUtility(UtilityKind.Water,TestCity.P(155,0),TestCity.P(169,0),out _),"Road margin and utility outer boundary remain enforced");
        var sim=new CityTraffic(c);var trip=sim.Dispatch(CityTraffic.Outside,shop,TripPurpose.Import);
        Check(trip!=null,"Real loaded import routes from original outside anchor to expanded district");
        var restored=TestCity.RoundTrip(c);var resumed=new CityTraffic(restored);
        sim.Advance(160);resumed.Advance(160);
        Check(c.Valid() && c.Goods(shop)>=8 && c.traffic.completed>0 && c.traffic.failed==0 && c.traffic.lostGoods==0,"Actual distant delivery and return finish without losses");
        Check(TestCity.Snapshot(c)==TestCity.Snapshot(restored),"Expanded coordinates, zones, utility network and active transport persist deterministically");
        var save=c.ToSaveData();save.format=33;save.map=null;var migrated=save.ToCity();
        Check(migrated.map.Valid() && migrated.GetBuilding(shop).x==c.GetBuilding(shop).x,"Legacy format explicitly migrates map without moving entities");
        save=c.ToSaveData();save.map=null;bool rejected=false;try{save.ToCity();}catch(ArgumentException){rejected=true;}
        Check(rejected,"New saves reject missing map metadata");
        c.map=new CityMapState{terrainVersion=999};
        Check(!c.Valid(),"Unsupported terrain profile cannot silently load");
        Console.WriteLine("PASS: "+checks+" expanded map checks");
    }
}
