using System;
using System.Collections.Generic;
using HarborCity.RoadCore;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;

namespace HarborCity
{
    // Rebuildable display data only. Network owns all geometry and stable IDs.
    // Original ribbon quads are assigned by their centre to spatial chunks;
    // boundary pairs are duplicated, never clipped/simplified or welded.
    public sealed class HarborCityRoadViews:IDisposable
    {
        internal readonly struct Piece
        {
            internal readonly (int x,int z) key;internal readonly int first,count;
            internal Piece((int,int) key,int first,int count){this.key=key;this.first=first;this.count=count;}
        }
        sealed class Ribbon
        {
            internal Geometry geometry;internal double start,end;internal List<Piece> pieces;
            internal bool Matches(Drivable d)
            {
                if(start!=d.StartDistance||end!=d.EndDistance||geometry.Count!=d.Geometry.Count)return false;
                if(ReferenceEquals(geometry,d.Geometry))return true;
                for(int i=0;i<geometry.Count;i++){var a=geometry[i];var b=d.Geometry[i];if(a.x!=b.x||a.y!=b.y||a.z!=b.z)return false;}return true;
            }
        }
        sealed class Chunk {internal GameObject instance;internal Mesh mesh;internal HashSet<long> members;}
        sealed class Buffer
        {
            internal readonly List<Vector3> vertices=new List<Vector3>(),normals=new List<Vector3>();
            internal readonly List<int> indices=new List<int>();
        }
        static readonly ProfilerMarker sample=new ProfilerMarker("City.Views.Roads.Sync"),buildSample=new ProfilerMarker("City.Views.Roads.BuildChunks");
        readonly Dictionary<long,Ribbon> ribbons=new Dictionary<long,Ribbon>(CityRoadKeyComparer.Instance);
        readonly Dictionary<(int x,int z),Chunk> chunks=new Dictionary<(int,int),Chunk>();
        readonly HashSet<long> present=new HashSet<long>(CityRoadKeyComparer.Instance);
        readonly List<Vector3> vertices=new List<Vector3>(),normals=new List<Vector3>();
        readonly List<int> indices=new List<int>();Mesh scratch;
        readonly Material material;World bound;Network network;Func<float,float,float> surface;bool surfaceDirty;
        // Count is logical geometry bindings; RendererCount is actual draw objects.
        public int Count=>ribbons.Count;public int RendererCount=>chunks.Count;public int ChunkSize {get;}
        public int Created {get;private set;}public int Removed {get;private set;}public int Retained {get;private set;}
        public int LastChangedDrivables {get;private set;}public int LastRebuiltChunks {get;private set;}
        public double LastSyncMilliseconds {get;private set;}
        public IEnumerable<GameObject> RenderObjects {get{foreach(var c in chunks.Values)yield return c.instance;}}
        public HarborCityRoadViews(Material material,int chunkSize=64)
        {if(material==null)throw new ArgumentNullException(nameof(material));if(chunkSize<16||chunkSize>256)throw new ArgumentOutOfRangeException(nameof(chunkSize));this.material=material;ChunkSize=chunkSize;}
        internal IEnumerable<Piece> Pieces(long id)=>ribbons[id].pieces;
        internal GameObject ChunkObject((int,int) key)=>chunks.TryGetValue(key,out var chunk)?chunk.instance:null;
        static void DestroyOwned(UnityEngine.Object value){if(value==null)return;if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}
        static void Release(Chunk c){if(c.instance!=null){var renderer=c.instance.GetComponent<Renderer>();if(renderer!=null)renderer.enabled=false;}DestroyOwned(c.mesh);DestroyOwned(c.instance);}
        void Samples(Geometry geometry,double start,double end,Func<float,float,float> height)
        {vertices.Clear();HarborCitySurfaceMesh.RibbonVertices(geometry,start,end,height,vertices);}
        Ribbon Describe(Drivable d,Func<float,float,float> height)
        {
            Samples(d.Geometry,d.StartDistance,d.EndDistance,height);var pieces=new List<Piece>();
            for(int span=0;span<vertices.Count/2-1;span++)
            {
                var centre=(vertices[span*2]+vertices[span*2+1]+vertices[span*2+2]+vertices[span*2+3])*.25f;
                var key=((int)Math.Floor(centre.x/ChunkSize),(int)Math.Floor(centre.z/ChunkSize));
                if(pieces.Count>0&&pieces[pieces.Count-1].key==key){var last=pieces[pieces.Count-1];pieces[pieces.Count-1]=new Piece(key,last.first,last.count+1);}else pieces.Add(new Piece(key,span,1));
            }
            return new Ribbon{geometry=d.Geometry,start=d.StartDistance,end=d.EndDistance,pieces=pieces};
        }
        void Normals()
        {
            // Reuse ONE scratch mesh to preserve Unity's original per-ribbon
            // normal calculation, including normals at a chunk boundary.
            if(scratch==null)scratch=new Mesh{name="Road ribbon normal scratch"};scratch.Clear();
            scratch.indexFormat=vertices.Count>ushort.MaxValue?IndexFormat.UInt32:IndexFormat.UInt16;
            indices.Clear();for(int span=0;span<vertices.Count/2-1;span++){int a=span*2;indices.Add(a);indices.Add(a+2);indices.Add(a+1);indices.Add(a+1);indices.Add(a+2);indices.Add(a+3);}
            scratch.SetVertices(vertices);scratch.SetTriangles(indices,0);scratch.RecalculateNormals();normals.Clear();scratch.GetNormals(normals);
        }
        static void Append(Buffer b,Piece piece,List<Vector3> vertices,List<Vector3> normals)
        {
            int offset=b.vertices.Count;for(int i=piece.first*2;i<=(piece.first+piece.count)*2+1;i++){b.vertices.Add(vertices[i]);b.normals.Add(normals[i]);}
            for(int span=0;span<piece.count;span++){int a=offset+span*2;b.indices.Add(a);b.indices.Add(a+2);b.indices.Add(a+1);b.indices.Add(a+1);b.indices.Add(a+2);b.indices.Add(a+3);}
        }
        Chunk Build((int x,int z) key,HashSet<long> members,Buffer buffer)
        {
            var c=new Chunk{members=members};
            try
            {
                c.mesh=new Mesh{name="Road chunk "+key.x+","+key.z};c.mesh.indexFormat=buffer.vertices.Count>ushort.MaxValue?IndexFormat.UInt32:IndexFormat.UInt16;
                c.mesh.SetVertices(buffer.vertices);c.mesh.SetNormals(buffer.normals);c.mesh.SetTriangles(buffer.indices,0);c.mesh.RecalculateBounds();
                c.instance=new GameObject(c.mesh.name);c.instance.AddComponent<MeshFilter>().sharedMesh=c.mesh;
                var renderer=c.instance.AddComponent<MeshRenderer>();renderer.enabled=false;renderer.sharedMaterial=material;return c;
            }
            catch{Release(c);throw;}
        }
        public void Sync(World world,Func<float,float,float> height)
        {
            long started=System.Diagnostics.Stopwatch.GetTimestamp();
            try{using(sample.Auto())SyncCore(world,height);}
            finally{LastSyncMilliseconds=(System.Diagnostics.Stopwatch.GetTimestamp()-started)*1000.0/System.Diagnostics.Stopwatch.Frequency;}
        }
        void SyncCore(World world,Func<float,float,float> height)
        {
            LastChangedDrivables=LastRebuiltChunks=0;
            bool full=surfaceDirty||!ReferenceEquals(bound,world)||!Equals(surface,height);
            if(!full&&(world==null||ReferenceEquals(network,world.Network)))return;
            var updates=new Dictionary<long,Ribbon>(CityRoadKeyComparer.Instance);var removed=new List<long>();
            var dirty=new HashSet<(int,int)>();present.Clear();
            if(world!=null)foreach(var d in world.Network.Drivables)
            {
                if(d.TravelLength<1e-8)continue;present.Add(d.Id);
                if(!full&&ribbons.TryGetValue(d.Id,out var prior)&&prior.Matches(d))continue;
                var next=Describe(d,height);updates.Add(d.Id,next);foreach(var piece in next.pieces)dirty.Add(piece.key);
                if(ribbons.TryGetValue(d.Id,out prior))foreach(var piece in prior.pieces)dirty.Add(piece.key);
            }
            foreach(var pair in ribbons)if(full||!present.Contains(pair.Key)){removed.Add(pair.Key);foreach(var piece in pair.Value.pieces)dirty.Add(piece.key);}
            var members=new Dictionary<(int,int),HashSet<long>>();
            foreach(var key in dirty)members.Add(key,chunks.TryGetValue(key,out var old)?new HashSet<long>(old.members,CityRoadKeyComparer.Instance):new HashSet<long>(CityRoadKeyComparer.Instance));
            foreach(long id in removed)foreach(var piece in ribbons[id].pieces)members[piece.key].Remove(id);
            foreach(var pair in updates)
            {
                if(ribbons.TryGetValue(pair.Key,out var old))foreach(var piece in old.pieces)members[piece.key].Remove(pair.Key);
                foreach(var piece in pair.Value.pieces)members[piece.key].Add(pair.Key);
            }
            var buffers=new Dictionary<(int,int),Buffer>();var affected=new HashSet<long>(CityRoadKeyComparer.Instance);
            foreach(var pair in members)if(pair.Value.Count>0){buffers.Add(pair.Key,new Buffer());affected.UnionWith(pair.Value);}
            var order=new List<long>(affected);order.Sort();var replacements=new Dictionary<(int,int),Chunk>();
            try
            {
                using(buildSample.Auto())
                {
                    foreach(long id in order)
                    {
                        var r=updates.TryGetValue(id,out var changed)?changed:ribbons[id];Samples(r.geometry,r.start,r.end,height);Normals();
                        foreach(var piece in r.pieces)if(buffers.TryGetValue(piece.key,out var buffer))Append(buffer,piece,vertices,normals);
                    }
                    var keys=new List<(int x,int z)>(buffers.Keys);keys.Sort((a,b)=>{int c=a.x.CompareTo(b.x);return c!=0?c:a.z.CompareTo(b.z);});
                    foreach(var key in keys)replacements.Add(key,Build(key,members[key],buffers[key]));
                }
            }
            catch{foreach(var chunk in replacements.Values)Release(chunk);throw;}
            // Publish only once every replacement succeeds. Failure leaves the
            // displayed city, ownership and source identities entirely intact.
            int retained=chunks.Count;foreach(var key in dirty)if(chunks.TryGetValue(key,out var old)){Release(old);chunks.Remove(key);Removed++;retained--;}
            foreach(var pair in replacements){pair.Value.instance.GetComponent<Renderer>().enabled=true;chunks.Add(pair.Key,pair.Value);Created++;}
            foreach(long id in removed)ribbons.Remove(id);foreach(var pair in updates)ribbons[pair.Key]=pair.Value;
            Retained+=retained;int removedOnly=0;foreach(long id in removed)if(!updates.ContainsKey(id))removedOnly++;LastChangedDrivables=updates.Count+removedOnly;LastRebuiltChunks=replacements.Count;
            bound=world;network=world?.Network;surface=height;surfaceDirty=false;
        }
        public void InvalidateSurface(){surfaceDirty=true;}
        public void Dispose()
        {foreach(var c in chunks.Values)Release(c);chunks.Clear();ribbons.Clear();present.Clear();DestroyOwned(scratch);scratch=null;bound=null;network=null;surface=null;surfaceDirty=false;vertices.Clear();normals.Clear();indices.Clear();}
    }
}
