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
        // One display palette for zoning overlays and task-coloured vehicles.
        public static Color Colour(LandUse use)=>use==LandUse.Residential?new Color(.25f,.8f,.4f):use==LandUse.Commercial?new Color(.25f,.65f,1):use==LandUse.Industrial?new Color(1,.7f,.2f):new Color(.35f,.5f,.4f);
        const double ChunkSize=64;
        static readonly ProfilerMarker buildSample=new ProfilerMarker("City.Views.Zoning.Build");
        readonly struct Plot:IEquatable<Plot>
        {
            readonly float x,z,yaw,width,depth,groundY;
            internal Plot(CityBuilding pose){x=pose.x;z=pose.z;yaw=pose.yaw;width=pose.width;depth=pose.depth;groundY=pose.groundY;}
            static bool Same(float a,float b)=>BitConverter.SingleToInt32Bits(a)==BitConverter.SingleToInt32Bits(b);
            public bool Equals(Plot b)=>Same(x,b.x)&&Same(z,b.z)&&Same(yaw,b.yaw)&&Same(width,b.width)&&Same(depth,b.depth)&&Same(groundY,b.groundY);
            internal CityBuilding Pose()=>new ResidentialBuilding{x=x,z=z,yaw=yaw,width=width,depth=depth,groundY=groundY};
        }
        readonly struct Visual
        {
            internal readonly Plot plot;internal readonly (int x,int z,int use) key;internal readonly bool visible;
            internal Visual(ZoneCell cell){plot=new Plot(cell.pose);visible=cell.occupiedBuilding<0;key=((int)Math.Floor(cell.pose.x/ChunkSize),(int)Math.Floor(cell.pose.z/ChunkSize),cell.use==LandUse.Residential?1:cell.use==LandUse.Commercial?2:cell.use==LandUse.Industrial?3:0);}
            internal bool Same(Visual other)=>visible==other.visible&&(!visible||key==other.key&&plot.Equals(other.plot));
        }
        sealed class Chunk {internal List<Plot> plots;internal GameObject instance;internal Mesh mesh;internal int first;}
        readonly List<Visual> previous=new List<Visual>();
        readonly Dictionary<(int x,int z,int use),Chunk> chunks=new Dictionary<(int,int,int),Chunk>();
        Func<float,float,float> surface;bool surfaceDirty;
        readonly Material[] materials;
        List<GameObject> objects=new List<GameObject>();
        List<Mesh> meshes=new List<Mesh>();
        public int PlotCount {get;private set;}
        public int RendererCount=>objects.Count;
        public int RebuiltChunks {get;private set;}
        public HarborCityZoningViews(Material[] materials)
        {if(materials==null||materials.Length!=4)throw new ArgumentException("Four display materials required");this.materials=(Material[])materials.Clone();}
        public void Sync(IReadOnlyList<ZoneCell> cells,Func<float,float,float> height)
        {
            using(buildSample.Auto())
            {
                bool invalidate=surfaceDirty||!Equals(surface,height);
                if(!invalidate&&cells!=null&&cells.Count==previous.Count)
                {
                    SyncChanges(cells,height);return;
                }
                var groups=new Dictionary<(int x,int z,int use),List<Plot>>();
                var order=new List<(int x,int z,int use)>();var first=new Dictionary<(int,int,int),int>();int count=0,index=0;
                if(cells!=null)foreach(var cell in cells)
                {
                    // Preserve the prior display rule, including project reservations.
                    if(cell.occupiedBuilding>=0){index++;continue;}
                    var pose=cell.pose;int use=cell.use==LandUse.Residential?1:cell.use==LandUse.Commercial?2:cell.use==LandUse.Industrial?3:0;
                    var key=((int)Math.Floor(pose.x/ChunkSize),(int)Math.Floor(pose.z/ChunkSize),use);
                    if(!groups.TryGetValue(key,out var plots)){groups.Add(key,plots=new List<Plot>());order.Add(key);first.Add(key,index);}
                    plots.Add(new Plot(pose));count++;index++;
                }
                var replacements=new Dictionary<(int x,int z,int use),Chunk>();
                try
                {
                    foreach(var key in order)
                    {
                        var plots=groups[key];if(!invalidate&&chunks.TryGetValue(key,out var prior)&&Same(prior.plots,plots))continue;
                        replacements.Add(key,BuildChunk(key,plots,height));
                    }
                }
                catch{foreach(var chunk in replacements.Values)Release(chunk);throw;}
                // Commit only after every changed chunk has been built successfully.
                foreach(var pair in chunks)if(!groups.ContainsKey(pair.Key)||replacements.ContainsKey(pair.Key))Release(pair.Value);
                var next=new Dictionary<(int x,int z,int use),Chunk>();objects.Clear();meshes.Clear();
                foreach(var key in order){var chunk=replacements.TryGetValue(key,out var replacement)?replacement:chunks[key];chunk.first=first[key];next.Add(key,chunk);objects.Add(chunk.instance);meshes.Add(chunk.mesh);}
                chunks.Clear();foreach(var pair in next)chunks.Add(pair.Key,pair.Value);
                previous.Clear();if(cells!=null)foreach(var cell in cells)previous.Add(new Visual(cell));
                surface=height;surfaceDirty=false;RebuiltChunks=replacements.Count;PlotCount=count;
            }
        }
        void SyncChanges(IReadOnlyList<ZoneCell> cells,Func<float,float,float> height)
        {
            // Immutable display projection only. Occupancy/project revisions often
            // leave almost every visual unchanged; do not regroup all those plots.
            var changes=new List<(int index,Visual value)>();var dirty=new HashSet<(int x,int z,int use)>();
            for(int i=0;i<cells.Count;i++)
            {
                var value=new Visual(cells[i]);var old=previous[i];if(old.Same(value))continue;
                changes.Add((i,value));if(old.visible)dirty.Add(old.key);if(value.visible)dirty.Add(value.key);
            }
            RebuiltChunks=0;if(changes.Count==0)return;
            var groups=new Dictionary<(int x,int z,int use),List<Plot>>();var first=new Dictionary<(int,int,int),int>();int cursor=0,count=PlotCount;
            foreach(var change in changes){if(previous[change.index].visible)count--;if(change.value.visible)count++;}
            for(int i=0;i<previous.Count;i++)
            {
                var value=cursor<changes.Count&&changes[cursor].index==i?changes[cursor++].value:previous[i];
                if(!value.visible||!dirty.Contains(value.key))continue;
                if(!groups.TryGetValue(value.key,out var plots)){groups.Add(value.key,plots=new List<Plot>());first.Add(value.key,i);}plots.Add(value.plot);
            }
            var replacements=new Dictionary<(int x,int z,int use),Chunk>();
            try
            {
                foreach(var pair in groups)
                {
                    if(chunks.TryGetValue(pair.Key,out var old)&&Same(old.plots,pair.Value))continue;
                    replacements.Add(pair.Key,BuildChunk(pair.Key,pair.Value,height));
                }
            }
            catch{foreach(var chunk in replacements.Values)Release(chunk);throw;}
            foreach(var key in dirty)
            {
                if(chunks.TryGetValue(key,out var old)&&(!groups.ContainsKey(key)||replacements.ContainsKey(key))){Release(old);chunks.Remove(key);}
                if(replacements.TryGetValue(key,out var replacement))chunks.Add(key,replacement);
                if(chunks.TryGetValue(key,out var current))current.first=first[key];
            }
            foreach(var change in changes)previous[change.index]=change.value;
            var ordered=new List<Chunk>(chunks.Values);ordered.Sort((a,b)=>a.first.CompareTo(b.first));objects.Clear();meshes.Clear();foreach(var chunk in ordered){objects.Add(chunk.instance);meshes.Add(chunk.mesh);}
            RebuiltChunks=replacements.Count;PlotCount=count;
        }
        static bool Same(List<Plot> a,List<Plot> b)
        {if(a.Count!=b.Count)return false;for(int i=0;i<a.Count;i++)if(!a[i].Equals(b[i]))return false;return true;}
        Chunk BuildChunk((int x,int z,int use) key,List<Plot> plots,Func<float,float,float> height)
        {
            var vertices=new List<Vector3>();var indices=new List<int>();
            foreach(var plot in plots)HarborCitySurfaceMesh.AppendPlot(plot.Pose(),height,vertices,indices);
            var chunk=new Chunk{plots=plots};
            try
            {
                var mesh=new Mesh{name="Zoning chunk "+key.x+","+key.z+" use "+key.use};chunk.mesh=mesh;
                if(vertices.Count>ushort.MaxValue)mesh.indexFormat=IndexFormat.UInt32;
                mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();
                var go=new GameObject(mesh.name);chunk.instance=go;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=materials[key.use];return chunk;
            }
            catch{Release(chunk);throw;}
        }
        static void Release(Chunk chunk)
        {if(chunk.instance!=null){var renderer=chunk.instance.GetComponent<Renderer>();if(renderer!=null)renderer.enabled=false;DestroyOwned(chunk.instance);}DestroyOwned(chunk.mesh);}
        static void DestroyOwned(UnityEngine.Object value){if(value==null)return;if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}
        public void InvalidateSurface(){surfaceDirty=true;}
        public void Dispose(){foreach(var chunk in chunks.Values)Release(chunk);chunks.Clear();previous.Clear();objects.Clear();meshes.Clear();surface=null;surfaceDirty=false;PlotCount=RebuiltChunks=0;}
    }
}
