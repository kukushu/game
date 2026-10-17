using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;

namespace HarborCity
{
    // Display ownership only: preserve all cells and triangles, grouped locally
    // in world coordinates. No building/road/land-use facts are written here.
    public sealed class HarborCityZoningViews:IDisposable
    {
        const double ChunkSize=64;
        static readonly ProfilerMarker buildSample=new ProfilerMarker("City.Views.Zoning.Build");
        sealed class Builder
        {
            internal readonly List<Vector3> vertices=new List<Vector3>();
            internal readonly List<int> indices=new List<int>();
        }
        readonly Material[] materials;
        List<GameObject> objects=new List<GameObject>();
        List<Mesh> meshes=new List<Mesh>();
        public int PlotCount {get;private set;}
        public int RendererCount=>objects.Count;
        public HarborCityZoningViews(Material[] materials)
        {if(materials==null||materials.Length!=4)throw new ArgumentException("Four display materials required");this.materials=(Material[])materials.Clone();}
        public void Sync(IReadOnlyList<ZoneCell> cells,Func<float,float,float> height)
        {
            using(buildSample.Auto())
            {
                var groups=new Dictionary<(int x,int z,int use),Builder>();
                var order=new List<(int x,int z,int use)>();int count=0;
                if(cells!=null)foreach(var cell in cells)
                {
                    // Preserve the prior display rule, including project reservations.
                    if(cell.occupiedBuilding>=0)continue;
                    var pose=cell.pose;int use=cell.use==LandUse.Residential?1:cell.use==LandUse.Commercial?2:cell.use==LandUse.Industrial?3:0;
                    var key=((int)Math.Floor(pose.x/ChunkSize),(int)Math.Floor(pose.z/ChunkSize),use);
                    if(!groups.TryGetValue(key,out var builder)){groups.Add(key,builder=new Builder());order.Add(key);}
                    HarborCitySurfaceMesh.AppendPlot(pose,height,builder.vertices,builder.indices);count++;
                }
                var nextObjects=new List<GameObject>(groups.Count);var nextMeshes=new List<Mesh>(groups.Count);
                try
                {
                    foreach(var key in order)
                    {
                        var b=groups[key];var mesh=new Mesh{name="Zoning chunk "+key.x+","+key.z+" use "+key.use};nextMeshes.Add(mesh);
                        if(b.vertices.Count>ushort.MaxValue)mesh.indexFormat=IndexFormat.UInt32;
                        mesh.SetVertices(b.vertices);mesh.SetTriangles(b.indices,0);mesh.RecalculateNormals();
                        var go=new GameObject(mesh.name);nextObjects.Add(go);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=materials[key.use];
                    }
                }
                catch{Release(nextObjects,nextMeshes);throw;}
                Release(objects,meshes);objects=nextObjects;meshes=nextMeshes;PlotCount=count;
            }
        }
        static void Release(List<GameObject> objects,List<Mesh> meshes)
        {
            foreach(var go in objects)if(go!=null){var renderer=go.GetComponent<Renderer>();if(renderer!=null)renderer.enabled=false;UnityEngine.Object.Destroy(go);}
            foreach(var mesh in meshes)if(mesh!=null)UnityEngine.Object.Destroy(mesh);
        }
        public void Dispose(){Release(objects,meshes);objects.Clear();meshes.Clear();PlotCount=0;}
    }
}
