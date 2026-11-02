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
        internal Material ForUse(LandUse use){if(useMaterials.TryGetValue(use,out var result))return result;var colour=use==LandUse.Residential?new Color(.3f,.75f,.45f):use==LandUse.Industrial?new Color(.9f,.65f,.22f):use==LandUse.Commercial?new Color(.3f,.65f,1):use==LandUse.Power?new Color(1,.85f,.25f):use==LandUse.Water?Color.cyan:use==LandUse.Sewage?new Color(.55f,.45f,.8f):new Color(.55f,.65f,.65f);result=new Material(material){color=colour};useMaterials.Add(use,result);return result;}
        HarborCityLayeredBuildings layered;CityEntities bound;readonly Stack<GameObject> pool=new Stack<GameObject>();
        public int Represented=>layered?.Represented??Count;public int Batched=>layered?.Batched??0;public int Summary=>layered?.Summary??0;public double IndexMs=>layered?.IndexMs??0;
        readonly Material material;LineRenderer selection;Material selectionMaterial;
        public int Count=>views.Count;
        public int Created {get;private set;}
        public int Removed {get;private set;}
        public HarborCityBuildingViews(Material material){this.material=material;}
        public GameObject Instance(int id)=>views.TryGetValue(id,out var view)?view:null;
        public int Pick(Ray ray)
        {if(layered!=null)return layered.Pick(ray);int id=0;float closest=float.PositiveInfinity;foreach(var entry in views)if(entry.Value!=null&&entry.Value.GetComponent<Renderer>().bounds.IntersectRay(ray,out float distance)&&distance<closest){closest=distance;id=entry.Key;}return id;}
        internal int Selected;
        internal void AssertDisplayIndex()=>layered?.AssertExact();
        public void Select(int id)
        {
            Selected=id;
            Matrix4x4 matrix;
            if(layered!=null){if(id==0||!layered.Matrix(id,out matrix)){if(selection!=null)selection.enabled=false;return;}}
            else{if(id==0||!views.TryGetValue(id,out var view)){if(selection!=null)selection.enabled=false;return;}matrix=view.transform.localToWorldMatrix;}
            if(selection==null){var go=new GameObject("Selected building outline");selection=go.AddComponent<LineRenderer>();selectionMaterial=new Material(material){color=Color.white};selection.sharedMaterial=selectionMaterial;selection.positionCount=4;selection.loop=true;selection.startWidth=selection.endWidth=.12f;}
            selection.enabled=true;for(int i=0;i<4;i++)selection.SetPosition(i,matrix.MultiplyPoint3x4(new Vector3(i==0||i==3?-.52f:.52f,.53f,i<2?-.52f:.52f)));
        }
        internal void Bind(CityEntities entities)
        {if(!ReferenceEquals(bound,entities)||layered==null){ClearLayered();bound=entities;if(entities!=null)layered=new HarborCityLayeredBuildings(this,entities);}}
        public void Sync(CityEntities entities,Camera camera,HarborCityDisplaySettings settings)
        {
            if(!settings.layered||camera==null){if(layered!=null){ClearLayered();}Sync(entities?.Buildings);return;}
            using(syncSample.Auto()){
                Bind(entities);
                layered?.Sync(camera,settings);
            }
        }
        void ClearLayered(){layered?.Dispose();layered=null;bound=null;foreach(var view in views.Values)UnityEngine.Object.Destroy(view);views.Clear();foreach(var view in pool)UnityEngine.Object.Destroy(view);pool.Clear();}
        internal bool Prepare(int id,LandUse use,Matrix4x4 matrix,HarborCityDisplaySettings settings)
        {
            if(views.ContainsKey(id))return true;
            GameObject go;
            {
                if(pool.Count>0)go=pool.Pop();else{if(!HarborCityDisplayBudget.Available(settings))return false;long at=System.Diagnostics.Stopwatch.GetTimestamp();go=GameObject.CreatePrimitive(PrimitiveType.Cube);Created++;HarborCityDisplayBudget.Charge(at);}
                go.SetActive(false);go.name=use+" #"+id;views.Add(id,go);
            }
            UpdatePrepared(id,use,matrix);return true;
        }
        internal void UpdatePrepared(int id,LandUse use,Matrix4x4 matrix){if(!views.TryGetValue(id,out var go))return;go.GetComponent<Renderer>().sharedMaterial=ForUse(use);go.transform.SetPositionAndRotation(matrix.GetColumn(3),matrix.rotation);go.transform.localScale=matrix.lossyScale;}
        internal void Activate(int id,bool active){if(views.TryGetValue(id,out var go))go.SetActive(active);}
        internal void RetireExcept(HashSet<int> keep)
        {removed.Clear();foreach(var pair in views)if(!keep.Contains(pair.Key))removed.Add(pair.Key);foreach(int id in removed){var go=views[id];go.SetActive(false);views.Remove(id);Removed++;if(pool.Count<2048)pool.Push(go);else UnityEngine.Object.Destroy(go);} // pending cell objects remain reserved until its batch is ready
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
        public void Dispose(){ClearLayered();if(selection!=null)UnityEngine.Object.Destroy(selection.gameObject);if(selectionMaterial!=null)UnityEngine.Object.Destroy(selectionMaterial);foreach(var view in views.Values)UnityEngine.Object.Destroy(view);views.Clear();present.Clear();removed.Clear();foreach(var m in useMaterials.Values)UnityEngine.Object.Destroy(m);useMaterials.Clear();}
    }
}
