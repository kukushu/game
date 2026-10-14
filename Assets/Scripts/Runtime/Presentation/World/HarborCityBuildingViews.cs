using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

namespace HarborCity
{
    // Display objects belong exclusively to this adapter. None is consulted by
    // construction, movement, finance, or persistence.
    public sealed class HarborCityBuildingViews : IDisposable
    {
        static readonly ProfilerMarker syncSample=new ProfilerMarker("City.Views.Buildings.Sync");
        readonly Dictionary<int,GameObject> views=new Dictionary<int,GameObject>();
        readonly HashSet<int> present=new HashSet<int>();
        readonly List<int> removed=new List<int>();
        readonly Material material;
        public int Count=>views.Count;
        public int Created {get;private set;}
        public int Removed {get;private set;}
        public HarborCityBuildingViews(Material material){this.material=material;}
        public GameObject Instance(int id)=>views.TryGetValue(id,out var view)?view:null;
        public void Sync(IReadOnlyList<CityBuilding> buildings)
        {
            using(syncSample.Auto())
            {
                present.Clear();
                if(buildings!=null)for(int i=0;i<buildings.Count;i++)
                {
                    var b=buildings[i];present.Add(b.id);
                    if(!views.TryGetValue(b.id,out var view))
                    {
                        view=GameObject.CreatePrimitive(PrimitiveType.Cube);
                        view.name=b.Use+" #"+b.id;view.GetComponent<Renderer>().sharedMaterial=material;
                        views.Add(b.id,view);Created++;
                    }
                    var transform=view.transform;
                    var position=new Vector3(b.x,b.groundY+1.5f,b.z);
                    var rotation=Quaternion.Euler(0,b.yaw,0);
                    var scale=new Vector3(b.width,3,b.depth);
                    if(transform.position!=position)transform.position=position;
                    if(transform.rotation!=rotation)transform.rotation=rotation;
                    if(transform.localScale!=scale)transform.localScale=scale;
                }
                removed.Clear();foreach(var entry in views)if(!present.Contains(entry.Key))removed.Add(entry.Key);
                foreach(int id in removed){UnityEngine.Object.Destroy(views[id]);views.Remove(id);Removed++;}
            }
        }
        public void Dispose(){foreach(var view in views.Values)UnityEngine.Object.Destroy(view);views.Clear();present.Clear();removed.Clear();}
    }
}
