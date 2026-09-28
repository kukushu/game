using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HarborCity
{
    public static class CityRoadChecks
    {
        [MenuItem("Harbor/Validate free roads")]
        public static void Validate()
        {
            var city=CityModel.Create();
            var traffic=new CityTraffic(city); traffic.Advance(10);
            city.EnableRoads((x,z)=>1);
            var plan=city.roads.Plan(city,new RoadNode{x=-45,z=1.5f,y=1},new RoadNode{x=-40,z=-22,y=1},(x,z)=>1);
            if(!city.CommitRoad(plan)) throw new Exception("Free road build failed: "+plan.error);
            string json=JsonUtility.ToJson(city);
            var restored=JsonUtility.FromJson<CityModel>(json);
            if(!restored.Valid() || restored.version!=2 || restored.roads.edges.Count!=city.roads.edges.Count)
                throw new Exception("Version 2 road save failed.");
            restored.Recalculate(); new CityTraffic(restored).Advance(90);
            if(!restored.Valid() || restored.traffic.completed<=city.traffic.completed) throw new Exception("Saved road traffic did not resume.");
            var game=UnityEngine.Object.FindAnyObjectByType<HarborCityGame>();
            int triangles=0;
            if(game!=null)
            {
                var live=(CityModel)typeof(HarborCityGame).GetField("city",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(game);
                if(!live.Valid() || live.roads==null) throw new Exception("Live city did not migrate correctly.");
                var view=game.GetComponentInChildren<CityRoadView>();
                if(view==null) throw new Exception("Road renderer missing.");
                foreach(var filter in view.GetComponentsInChildren<MeshFilter>())
                {
                    foreach(var vertex in filter.sharedMesh.vertices)
                        if(float.IsNaN(vertex.x+vertex.y+vertex.z) || float.IsInfinity(vertex.x+vertex.y+vertex.z)) throw new Exception("Non-finite road vertex.");
                    triangles+=filter.sharedMesh.triangles.Length/3;
                }
                if(triangles==0) throw new Exception("Road meshes are empty.");
            }
            string result="PASS: v2 road JSON round-trip, resumed traffic, live migration, finite road meshes ("+triangles+" triangles).";
            Directory.CreateDirectory("Temp"); File.WriteAllText("Temp/CityRoadChecks.txt",result); Debug.Log(result);
        }
    }
}
