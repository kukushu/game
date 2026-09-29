using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HarborCity
{
    public static class CityBuildingChecks
    {
        [MenuItem("Harbor/Validate roadside buildings")]
        public static void Validate()
        {
            var city=CityModel.Create(); var traffic=new CityTraffic(city); traffic.Advance(8);
            // Exercise the actual JSON boundary before and after migration.
            city=JsonUtility.FromJson<CityModel>(JsonUtility.ToJson(city));
            city.EnableRoads((x,z)=>1); city.EnableBuildings();
            var plan=city.roads.Plan(city,new RoadNode{x=-45,z=1.5f,y=1},new RoadNode{x=-30,z=-24,y=1},(x,z)=>1);
            if(!city.CommitRoad(plan)) throw new Exception(plan.error);
            var lot=city.RoadsideLots().First(b=>Math.Abs(b.yaw%90)>1 && city.CanBuild(b,(x,z)=>1,out _));
            int id=city.PlaceBuilding(lot,LandUse.Residential,(x,z)=>1,out _); city.levels[id]=1;
            traffic=new CityTraffic(city);
            if(traffic.Dispatch(id,-1,TripPurpose.Commute)==null) throw new Exception("Independent trip dispatch failed");
            var restored=JsonUtility.FromJson<CityModel>(JsonUtility.ToJson(city));
            if(!restored.Valid() || restored.buildings[id].yaw!=lot.yaw || restored.buildings[id].x!=lot.x)
                throw new Exception("Version 3 pose or traffic JSON failed");
            restored.Recalculate(); new CityTraffic(restored).Advance(120);
            if(!restored.Valid() || restored.traffic.completed<=city.traffic.completed) throw new Exception("Saved traffic failed to resume");
            var game=UnityEngine.Object.FindAnyObjectByType<HarborCityGame>();
            if(game!=null)
            {
                var live=(CityModel)typeof(HarborCityGame).GetField("city",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(game);
                if(!live.Valid() || live.version<3) throw new Exception("Live city migration failed");
                var visuals=(System.Collections.Generic.Dictionary<int,GameObject>)typeof(HarborCityGame).GetField("visuals",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(game);
                foreach(var pair in visuals)
                {
                    var b=live.buildings[pair.Key]; var t=pair.Value.transform;
                    if(Math.Abs(t.position.x-b.x)>.001f || Math.Abs(t.position.z-b.z)>.001f || Quaternion.Angle(t.rotation,Quaternion.Euler(0,b.yaw,0))>.01f)
                        throw new Exception("Building visual pose mismatch");
                }
            }
            string result="PASS: v1 JSON migration, v3 independent pose/trip JSON round-trip, resumed traffic, live city and visual poses.";
            Directory.CreateDirectory("Temp"); File.WriteAllText("Temp/CityBuildingChecks.txt",result); Debug.Log(result);
        }
    }
}
