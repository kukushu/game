using System;
using System.Collections.Generic;
using HarborCity.RoadCore;
using UnityEngine;
using Unity.Profiling;

namespace HarborCity
{
    // Stable Drivable IDs own display objects here. Geometry is borrowed read-only;
    // equality avoids rebuilding unchanged ribbons after an independent road edit.
    public sealed class HarborCityRoadViews:IDisposable
    {
        sealed class View
        {
            internal GameObject instance;internal Mesh mesh;internal Geometry geometry;internal double start,end;
            internal bool Matches(Drivable d)
            {
                if(instance==null||start!=d.StartDistance||end!=d.EndDistance||geometry.Count!=d.Geometry.Count)return false;
                if(ReferenceEquals(geometry,d.Geometry))return true;
                for(int i=0;i<geometry.Count;i++){var a=geometry[i];var b=d.Geometry[i];if(a.x!=b.x||a.y!=b.y||a.z!=b.z)return false;}return true;
            }
        }
        static readonly ProfilerMarker sample=new ProfilerMarker("City.Views.Roads.Sync");
        readonly Dictionary<long,View> views=new Dictionary<long,View>(CityRoadKeyComparer.Instance);
        readonly HashSet<long> present=new HashSet<long>(CityRoadKeyComparer.Instance);readonly List<long> removed=new List<long>();
        readonly Material material;World bound;Network network;Func<float,float,float> surface;bool surfaceDirty;
        public int Count=>views.Count;public int Created {get;private set;}public int Removed {get;private set;}public int Retained {get;private set;}
        public HarborCityRoadViews(Material material){this.material=material;}
        public GameObject Instance(long id)=>views.TryGetValue(id,out var view)?view.instance:null;
        static void DestroyOwned(UnityEngine.Object value){if(value==null)return;if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}
        static void Release(View view){DestroyOwned(view.mesh);DestroyOwned(view.instance);}
        void Clear(){foreach(var view in views.Values)Release(view);Removed+=views.Count;views.Clear();present.Clear();removed.Clear();network=null;}
        View Create(Drivable d,Func<float,float,float> height)
        {
            Mesh mesh=HarborCitySurfaceMesh.Ribbon(d.Geometry,d.StartDistance,d.EndDistance,height);mesh.name="Shared lane geometry "+d.Id;GameObject go=null;
            try{go=new GameObject(mesh.name);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;return new View{instance=go,mesh=mesh,geometry=d.Geometry,start=d.StartDistance,end=d.EndDistance};}
            catch{DestroyOwned(mesh);DestroyOwned(go);throw;}
        }
        public void Sync(World world,Func<float,float,float> height)
        {
            using(sample.Auto())
            {
                if(surfaceDirty||!ReferenceEquals(bound,world)||!Equals(surface,height)){Clear();bound=world;surface=height;surfaceDirty=false;}
                if(world==null||ReferenceEquals(network,world.Network))return;
                present.Clear();
                foreach(var d in world.Network.Drivables)
                {
                    if(d.TravelLength<1e-8)continue;present.Add(d.Id);
                    if(views.TryGetValue(d.Id,out var existing)&&existing.Matches(d)){Retained++;continue;}
                    var next=Create(d,height);if(existing!=null){Release(existing);Removed++;}views[d.Id]=next;Created++;
                }
                removed.Clear();foreach(long id in views.Keys)if(!present.Contains(id))removed.Add(id);
                foreach(long id in removed){Release(views[id]);views.Remove(id);Removed++;}network=world.Network;
            }
        }
        // A terrain-edit feature must explicitly change/invalidate its surface;
        // the current fixed landscape is not a second source of road geometry.
        public void InvalidateSurface(){surfaceDirty=true;}
        public void Dispose(){Clear();bound=null;surface=null;}
    }
}
