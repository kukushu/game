using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarborCity;
using HarborCity.RoadCore;
using UnityEngine;

// Compiled only in the disposable Unity project, with the exact baseline
// SurfaceMesh source. This is not an alternate renderer in the shipped game.
public static class HarborCityRoadBatchValidation
{
    static long checks;static readonly List<string> rows=new List<string>{"case,chunk_size,drivables,renderers,reference_vertices,vertices,triangles,sync_ms,rebuilt_chunks"};
    static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
    static void Verify(string name,World world,HarborCityRoadViews views,Material material,Func<float,float,float> height)
    {
        byte[] before=SaveCodec.Write(world);views.Sync(world,height);
        var result=HarborCityRoadGeometryChecks.Verify(world,views,height,material,HarborCitySurfaceMeshReference.Ribbon);checks+=result.checks;
        Check(before.SequenceEqual(SaveCodec.Write(world)),"Display changed complete transport state: "+name);
        rows.Add(string.Join(",",name,views.ChunkSize,views.Count,views.RendererCount,result.referenceVertices,result.vertices,result.triangles,views.LastSyncMilliseconds,views.LastRebuiltChunks));
    }
    static void Fixture(int size,Material material)
    {
        var world=new World();int a=world.AddJunction(new Point(-80,2,-32)),b=world.AddJunction(new Point(80,2,-32)),c=world.AddJunction(new Point(96,2,60));
        int road=world.AddRoad(a,b,new[]{world.JunctionPosition(a),new Point(-24,2,16),new Point(0,2,8),world.JunctionPosition(b)},lanes:2);
        world.AddRoad(b,c,new[]{world.JunctionPosition(b),world.JunctionPosition(c)});
        using(var views=new HarborCityRoadViews(material,size))
        {
            float offset=0;Func<float,float,float> height=(x,z)=>2+(float)Math.Sin(x*.02)*.12f+z*.001f+offset;
            Verify("curve-slope",world,views,material,height);
            var objects=views.RenderObjects.ToArray();int created=views.Created;byte[] before=SaveCodec.Write(world);
            for(int i=0;i<30;i++)views.Sync(world,height);
            Check(views.Created==created&&views.LastRebuiltChunks==0&&objects.SequenceEqual(views.RenderObjects)&&before.SequenceEqual(SaveCodec.Write(world)),"Unchanged sync changed geometry or facts");
            var settings=world.Network.Settings(road);Check(world.PlanUpgradeRoad(road,settings.lanes,settings.speed+1,settings.direction).TryCommit(),"Speed edit failed");
            views.Sync(world,height);Check(views.LastRebuiltChunks==0&&objects.SequenceEqual(views.RenderObjects),"Speed-only edit rebuilt unchanged geometry");
            int x=world.AddJunction(new Point(230,2,230)),y=world.AddJunction(new Point(250,2,230));world.AddRoad(x,y,new[]{world.JunctionPosition(x),world.JunctionPosition(y)});
            Verify("far-add",world,views,material,height);Check(objects.All(go=>views.RenderObjects.Contains(go)),"Far edit recreated unrelated chunks");
            world.SplitRoadAt(road,18);Verify("split",world,views,material,height);
            Check(world.PlanUpgradeRoad(2,3,14,RoadDirection.Both).TryCommit(),"Widen edit failed");Verify("widen",world,views,material,height);
            Check(world.PlanUpgradeRoad(2,3,14,RoadDirection.Backward).TryCommit(),"Direction edit failed");Verify("direction",world,views,material,height);
            world.RemoveRoad(road);Verify("remove",world,views,material,height);
            objects=views.RenderObjects.ToArray();bool failed=false;
            try{views.Sync(world,(u,v)=>throw new InvalidOperationException("intentional height failure"));}catch(InvalidOperationException){failed=true;}
            Check(failed&&objects.SequenceEqual(views.RenderObjects)&&objects.All(go=>go.GetComponent<Renderer>().enabled),"Failed rebuild discarded visible city");
            offset=.2f;views.InvalidateSurface();Verify("terrain-invalidated",world,views,material,height);
            var loaded=SaveCodec.Read(SaveCodec.Write(world));Verify("load-rebind",loaded,views,material,height);
            Verify("null-surface",loaded,views,material,null);
            views.Sync(null,null);Check(views.Count==0&&views.RendererCount==0,"Clear retained chunks");
            var busy=CityRoadCoreFixtures.Create(12,false);busy.Advance(.05);Verify("moving-vehicles",busy,views,material,null);
            var vehicle=busy.Vehicles.First(v=>v.status==VehicleStatus.Active);var position=busy.Position(vehicle);before=SaveCodec.Write(busy);views.Sync(busy,null);
            Check(before.SequenceEqual(SaveCodec.Write(busy))&&Point.Distance(position,busy.Position(vehicle))==0,"View changed in-flight vehicle");
            var stale=busy.PlanConnectedRoad(new[]{new Point(300,0,300),new Point(320,0,300)});busy.AddJunction(new Point(350,0,350));before=SaveCodec.Write(busy);
            Check(!stale.TryCommit()&&before.SequenceEqual(SaveCodec.Write(busy)),"Failed construction changed city");Verify("failed-transaction",busy,views,material,null);
        }
        Check(material!=null,"View destroyed borrowed material");
    }
    static void Dense(Material material)
    {
        var world=new World();for(int i=0;i<300;i++){int a=world.AddJunction(new Point(4,2,4+i*.02)),b=world.AddJunction(new Point(40,2,4+i*.02));world.AddRoad(a,b,new[]{world.JunctionPosition(a),world.JunctionPosition(b)});}
        using(var views=new HarborCityRoadViews(material)){Verify("uint32-dense",world,views,material,null);Check(views.RenderObjects.Any(go=>go.GetComponent<MeshFilter>().sharedMesh.vertexCount>ushort.MaxValue&&go.GetComponent<MeshFilter>().sharedMesh.indexFormat==UnityEngine.Rendering.IndexFormat.UInt32),"Dense fixture did not test UInt32");}
    }
    public static void Run()
    {
        string output=Environment.GetEnvironmentVariable("HARBOR_ROAD_BATCH_OUTPUT"),input=Environment.GetEnvironmentVariable("HARBOR_ROAD_BATCH_INPUT");Directory.CreateDirectory(output);
        var material=new Material(Resources.Load<Shader>("RoadCore"));
        try
        {
            foreach(int size in new[]{32,64,128})Fixture(size,material);Dense(material);
            if(!string.IsNullOrEmpty(input))
            {
                var city=CityCoreSave.Read(File.ReadAllBytes(input));var landscape=new GameObject("Batch check terrain").AddComponent<CityLandscape>();landscape.Build(city.City.map);
                try{using(var views=new HarborCityRoadViews(material)){byte[] before=CityCoreSave.Write(city);Verify("real-city",city.Transport,views,material,landscape.Height);Check(before.SequenceEqual(CityCoreSave.Write(city)),"Real city changed through view");}}
                finally{UnityEngine.Object.DestroyImmediate(landscape.gameObject);}
            }
            Check(!Resources.FindObjectsOfTypeAll<Mesh>().Any(mesh=>mesh.name.StartsWith("Road chunk ",StringComparison.Ordinal)||mesh.name=="Road ribbon normal scratch"),"Owned native mesh leaked after disposal");
            File.WriteAllLines(Path.Combine(output,"geometry.csv"),rows);File.WriteAllText(Path.Combine(output,"Result.txt"),"PASS "+checks+" frozen-source native Unity checks: exact positions/normals/indices, all ribbon spans/triangles, bounded chunks, unchanged/far-edit reuse, split/widen/direction/remove, speed-only no rebuild, height failure rollback, terrain invalidation, load/rebind/clear, moving vehicles, stale transaction and UInt32. No mouse acceptance.\n");
        }
        catch(Exception e){File.WriteAllText(Path.Combine(output,"Result.txt"),e.ToString());throw;}
        finally{UnityEngine.Object.DestroyImmediate(material);}
    }
}
