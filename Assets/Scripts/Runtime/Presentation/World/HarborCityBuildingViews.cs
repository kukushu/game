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
        readonly Dictionary<LandUse,Material> useMaterials=new Dictionary<LandUse,Material>();
        Material ForUse(LandUse use){if(useMaterials.TryGetValue(use,out var result))return result;var colour=use==LandUse.Residential?new Color(.3f,.75f,.45f):use==LandUse.Industrial?new Color(.9f,.65f,.22f):use==LandUse.Commercial?new Color(.3f,.65f,1):use==LandUse.Power?new Color(1,.85f,.25f):use==LandUse.Water?Color.cyan:use==LandUse.Sewage?new Color(.55f,.45f,.8f):new Color(.55f,.65f,.65f);result=new Material(material){color=colour};useMaterials.Add(use,result);return result;}
        readonly Material material;LineRenderer selection;Material selectionMaterial;
        public int Count=>views.Count;
        public int Created {get;private set;}
        public int Removed {get;private set;}
        public HarborCityBuildingViews(Material material){this.material=material;}
        public GameObject Instance(int id)=>views.TryGetValue(id,out var view)?view:null;
        public int Pick(Ray ray)
        {int id=0;float closest=float.PositiveInfinity;foreach(var entry in views)if(entry.Value!=null&&entry.Value.GetComponent<Renderer>().bounds.IntersectRay(ray,out float distance)&&distance<closest){closest=distance;id=entry.Key;}return id;}
        public void Select(int id)
        {
            if(id==0||!views.TryGetValue(id,out var view)){if(selection!=null)selection.enabled=false;return;}
            if(selection==null){var go=new GameObject("Selected building outline");selection=go.AddComponent<LineRenderer>();selectionMaterial=new Material(material){color=Color.white};selection.sharedMaterial=selectionMaterial;selection.positionCount=4;selection.loop=true;selection.startWidth=selection.endWidth=.12f;}
            selection.enabled=true;var t=view.transform;for(int i=0;i<4;i++)selection.SetPosition(i,t.TransformPoint(new Vector3(i==0||i==3?-.52f:.52f,.53f,i<2?-.52f:.52f)));
        }
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
                        view.name=b.Use+" #"+b.id;view.GetComponent<Renderer>().sharedMaterial=ForUse(b.Use);
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
        public void Dispose(){if(selection!=null)UnityEngine.Object.Destroy(selection.gameObject);if(selectionMaterial!=null)UnityEngine.Object.Destroy(selectionMaterial);foreach(var view in views.Values)UnityEngine.Object.Destroy(view);views.Clear();present.Clear();removed.Clear();foreach(var m in useMaterials.Values)UnityEngine.Object.Destroy(m);useMaterials.Clear();}
    }
}
