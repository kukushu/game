using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HarborCity.RoadCore;
using UnityEngine;

namespace HarborCity
{
    public static class CityRoadViewChecks
    {
        static int checks;
        static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
        static void Geometry(World world,HarborCityRoadViews views,Func<float,float,float> ground)
        {
            var result=HarborCityRoadGeometryChecks.Verify(world,views,ground,views.RenderObjects.First().GetComponent<Renderer>().sharedMaterial,HarborCitySurfaceMesh.Ribbon);Check(result.checks>0,"No native geometry checks");
        }
        public static void Run()
        {
            var material=new Material(Resources.Load<Shader>("RoadCore"));var views=new HarborCityRoadViews(material);Func<float,float,float> ground=(x,z)=>2+x*.002f+z*.001f;var world=new World();
            int a=world.AddJunction(new Point(-30,2,0)),b=world.AddJunction(new Point(0,2,0)),c=world.AddJunction(new Point(30,2,0));int road=world.AddRoad(a,b,new[]{world.JunctionPosition(a),world.JunctionPosition(b)});world.AddRoad(b,c,new[]{world.JunctionPosition(b),world.JunctionPosition(c)});
            views.Sync(world,ground);Geometry(world,views,ground);var before=SaveCodec.Write(world);int created=views.Created;var ids=views.RenderObjects.ToDictionary(go=>go.name,go=>go);
            for(int i=0;i<20;i++)views.Sync(world,ground);Check(views.Created==created&&before.SequenceEqual(SaveCodec.Write(world)),"Repeated sync rebuilt views or changed world");
            int x=world.AddJunction(new Point(230,2,230)),y=world.AddJunction(new Point(250,2,230));world.AddRoad(x,y,new[]{world.JunctionPosition(x),world.JunctionPosition(y)});views.Sync(world,ground);foreach(var pair in ids)Check(views.RenderObjects.Any(go=>go.name==pair.Key&&ReferenceEquals(go,pair.Value)),"Independent edit recreated old object");Geometry(world,views,ground);
            world.SplitRoadAt(road,14);views.Sync(world,ground);Geometry(world,views,ground);world.RemoveRoad(road);views.Sync(world,ground);Geometry(world,views,ground);
            var plan=world.PlanUpgradeRoad(2,2,12,RoadDirection.Both);Check(plan.TryCommit(),"Widen fixture did not commit");views.Sync(world,ground);Geometry(world,views,ground);
            Func<float,float,float> changed=(u,v)=>ground(u,v)+.25f;views.Sync(world,changed);Geometry(world,views,changed);
            before=SaveCodec.Write(world);var loaded=SaveCodec.Read(before);views.Sync(loaded,ground);Geometry(loaded,views,ground);Check(before.SequenceEqual(SaveCodec.Write(loaded)),"World binding mutated state");
            views.InvalidateSurface();views.Sync(loaded,ground);Geometry(loaded,views,ground);views.Sync(null,ground);Check(views.Count==0,"Null binding leaked views");views.Dispose();Check(material!=null,"Borrowed material destroyed");UnityEngine.Object.DestroyImmediate(material);
            string output=Environment.GetEnvironmentVariable("HARBOR_ROAD_VIEW_RESULT");if(string.IsNullOrEmpty(output))throw new Exception("Explicit diagnostic output required");Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"RoadViews.txt"),"PASS "+checks+" native road view geometry/lifecycle checks; not mouse acceptance.");
            CityUsabilityChecks.Run();Debug.Log("PASS "+checks+" road view checks");
        }
    }
}
