using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;
namespace HarborCity
{
    public static class CityZoningViewChecks
    {
        static int checks;
        static void Check(bool ok,string reason){checks++;if(!ok)throw new Exception(reason);}
        static List<GameObject> Objects(HarborCityZoningViews view)=>(List<GameObject>)typeof(HarborCityZoningViews).GetField("objects",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(view);
        static int Use(ZoneCell c)=>c.use==LandUse.Residential?1:c.use==LandUse.Commercial?2:c.use==LandUse.Industrial?3:0;
        static void Geometry(HarborCityZoningViews view,List<ZoneCell> cells,Func<float,float,float> height,Material[] palette)
        {
            var groups=cells.Where(c=>c.occupiedBuilding<0).GroupBy(c=>((int)Math.Floor(c.pose.x/64),(int)Math.Floor(c.pose.z/64),Use(c))).ToArray();
            Check(view.PlotCount==cells.Count(c=>c.occupiedBuilding<0)&&view.RendererCount==groups.Length,"All true display cells and chunks retained");
            for(int i=0;i<groups.Length;i++)
            {
                var expectedVertices=new List<Vector3>();var expectedIndices=new List<int>();
                foreach(var cell in groups[i]){var mesh=HarborCitySurfaceMesh.Plot(cell.pose,height);int offset=expectedVertices.Count;expectedVertices.AddRange(mesh.vertices);expectedIndices.AddRange(mesh.triangles.Select(t=>t+offset));UnityEngine.Object.DestroyImmediate(mesh);}
                var go=Objects(view)[i];var actual=go.GetComponent<MeshFilter>().sharedMesh;
                Check(actual.vertices.SequenceEqual(expectedVertices)&&actual.triangles.SequenceEqual(expectedIndices),"Exact full-cell geometry or winding differs");
                var reference=new Mesh();if(expectedVertices.Count>65535)reference.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;reference.SetVertices(expectedVertices);reference.SetTriangles(expectedIndices,0);reference.RecalculateNormals();
                Check(actual.normals.SequenceEqual(reference.normals),"Full group normals differ");UnityEngine.Object.DestroyImmediate(reference);
                Check(go.GetComponent<Renderer>().enabled&&ReferenceEquals(go.GetComponent<Renderer>().sharedMaterial,palette[groups[i].Key.Item3]),"Hidden geometry or changed colour");
            }
        }
        public static void Run()
        {
            var palette=Enumerable.Range(0,4).Select(i=>new Material(Resources.Load<Shader>("RoadCore"))).ToArray();var view=new HarborCityZoningViews(palette);Func<float,float,float> height=(x,z)=>2+(float)Math.Sin(x*.03)*.2f+z*.002f;
            try
            {
                var cells=Enumerable.Range(0,600).Select(i=>new ZoneCell{key="test:"+i,use=(LandUse)(i%4),occupiedBuilding=i%13==0?7:-1,available=i%7!=0,pose=new ResidentialBuilding{x=(i%30-15)*9.3f,z=(i/30-10)*9.3f,yaw=i%4*23.5f,width=1.46f,depth=1.46f,groundY=2}}).ToList();
                view.Sync(cells,height);Geometry(view,cells,height,palette);var old=Objects(view).ToDictionary(go=>go.name);
                view.Sync(cells,height);Check(view.RebuiltChunks==0&&Objects(view).All(go=>ReferenceEquals(old[go.name],go)),"Unchanged sync recreated chunks");
                cells.Add(new ZoneCell{key="local",use=LandUse.Industrial,pose=new ResidentialBuilding{x=300,z=300,width=1.46f,depth=1.46f,groundY=2}});view.Sync(cells,height);Geometry(view,cells,height,palette);
                Check(view.RebuiltChunks==1&&Objects(view).Where(go=>old.ContainsKey(go.name)).All(go=>ReferenceEquals(old[go.name],go)),"Independent new chunk rebuilt remote geometry");
                cells[3].use=LandUse.Residential;cells[4].occupiedBuilding=4;cells[5].pose.x+=64;view.Sync(cells,height);Geometry(view,cells,height,palette);
                old=Objects(view).ToDictionary(go=>go.name);bool failed=false;try{view.Sync(cells,(x,z)=>throw new InvalidOperationException("intentional terrain failure"));}catch(InvalidOperationException){failed=true;}
                Check(failed&&Objects(view).All(go=>ReferenceEquals(old[go.name],go)),"Failure discarded old chunks");
                view.InvalidateSurface();view.Sync(cells,height);Check(view.RebuiltChunks==view.RendererCount,"Explicit terrain invalidation missed chunks");Geometry(view,cells,height,palette);
                view.Sync(null,height);Check(view.PlotCount==0&&view.RendererCount==0,"Clear retained orphan chunks");view.Dispose();Check(palette.All(m=>m!=null),"Borrowed materials destroyed");
                File.WriteAllText(Environment.GetEnvironmentVariable("HARBOR_ZONING_VIEW_RESULT"),"PASS "+checks+" exact group vertices/triangles/normals/colours and chunk retention, painting, occupancy, cross-boundary move, terrain invalidation, failure atomicity, clear and borrowed materials.\n");
            }
            finally{view.Dispose();foreach(var material in palette)UnityEngine.Object.DestroyImmediate(material);}
        }
    }
}
