using System;
using System.Linq;
using UnityEditor;
namespace HarborCity
{
    public static class CityRoadChecks
    {
        [MenuItem("Harbor/Validate world roads")]
        public static void Validate()
        {
            var c=CityModel.Create();c.SetEntranceHeight(CityBuildingChecks.Flat);
            var p=c.roads.PlanCurve(c,c.roads.Node(CityRoads.Entrance),CityBuildingChecks.P(-32.5f,21.5f),CityBuildingChecks.P(-12.5f,1.5f),CityBuildingChecks.Flat);
            CityBuildingChecks.Check(p.Valid && c.CommitRoad(p) && c.Valid(),"Atomic free curve construction");
            var e=c.roads.edges[c.roads.edges.Count/2];var a=c.roads.Node(e.a);var b=c.roads.Node(e.b);var middle=CityRoads.Lerp(a,b,.5f);float length=CityRoads.Length(a,b);
            var preview=c.RoadsidePreview(middle.x-(b.z-a.z)/length*6,middle.z+(b.x-a.x)/length*6,LandUse.Commercial,HousingKind.Apartment,out string error);
            CityBuildingChecks.Check(preview is CommercialBuilding && c.CanBuild(preview,CityBuildingChecks.Flat,out error),"Curve-oriented continuous preview: "+error);
            int id=c.PlaceBuilding(preview,LandUse.Commercial,CityBuildingChecks.Flat,out error);var copy=CityBuildingChecks.Copy(c);
            CityBuildingChecks.Check(id>0 && copy.Valid() && copy.BuildingAccess(id) && copy.GetBuilding(id).yaw==preview.yaw && copy.roads.edges.Count==c.roads.edges.Count,"Curved graph, pose and access native JSON");
            CityBuildingChecks.Result("CityRoadChecks","PASS: atomic free curve, continuous typed placement, native road/pose/access JSON");
        }
    }
}
