using System;
using System.IO;
using HarborCity.RoadCore;
using UnityEngine;

namespace HarborCity
{
    // Geometry checks supplement, and do not replace, real mouse acceptance.
    public static class CityUsabilityChecks
    {
        public static void Run()
        {
            int checks=0;
            Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception(message);checks++;};
            Func<float,float,float> ground=(x,z)=>2+x*.04f+z*.08f;
            var points=new[]{new Point(-8,ground(-8,0),0),new Point(0,ground(0,0),0),new Point(6,ground(6,3),3)};
            var geometry=new Geometry(points);
            var mesh=HarborCitySurfaceMesh.Ribbon(geometry,1,geometry.Length-1,ground);
            foreach(var v in mesh.vertices)check(v.y>ground(v.x,v.z)+.08f,"Road surface intersects ground");
            foreach(var n in mesh.normals)check(n.y>0,"Road triangles face underground");
            check(mesh.vertices.Length>geometry.Count*2,"Road display did not subdivide shared spans");
            check(Point.Distance(geometry[0],points[0])==0&&geometry.Length>14,"View mutated geometry");
            UnityEngine.Object.DestroyImmediate(mesh);
            var bridge=new Geometry(new[]{new Point(0,20,0),new Point(8,20,0)});
            mesh=HarborCitySurfaceMesh.Ribbon(bridge,0,bridge.Length,ground);
            foreach(var v in mesh.vertices)check(Math.Abs(v.y-20.12)<.0001,"Bridge display was draped to ground");
            UnityEngine.Object.DestroyImmediate(mesh);
            mesh=HarborCitySurfaceMesh.Plot(new ResidentialBuilding{x=4,z=3,yaw=37,width=2.96f,depth=2.96f},ground);
            foreach(var v in mesh.vertices)check(Math.Abs(v.y-ground(v.x,v.z)-.16f)<.0001,"Zoning overlay height mismatch");
            foreach(var n in mesh.normals)check(n.y>0,"Zone triangles face underground");
            UnityEngine.Object.DestroyImmediate(mesh);
            var city=CityGrowingGridFixture.Create((x,z)=>2);
            var snapshot=CityCoreSave.Write(city);
            var plan=city.Transport.PlanConnectedRoad(new[]{new Point(70,2,60),new Point(85,2,60)});
            city.Construction.CheckRoadPlacement(plan);
            check(Convert.ToBase64String(snapshot)==Convert.ToBase64String(CityCoreSave.Write(city)),"Preview mutated city facts");
            city.Transport.AddJunction(new Point(90,2,60));
            snapshot=CityCoreSave.Write(city);bool stale=false;
            try{city.Construction.CheckRoadPlacement(plan);}catch(InvalidOperationException){stale=true;}
            check(stale,"Stale plan was accepted");
            check(Convert.ToBase64String(snapshot)==Convert.ToBase64String(CityCoreSave.Write(city)),"Failed preflight changed facts");
            string report="PASS "+checks+" usability geometry/preflight checks. Does not constitute mouse acceptance.";
            Directory.CreateDirectory("Temp/R3Usability");File.WriteAllText("Temp/R3Usability/Checks.txt",report);Debug.Log(report);
        }
    }
}
