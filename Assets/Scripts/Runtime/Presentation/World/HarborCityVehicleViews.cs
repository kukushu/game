using System;
using System.Collections.Generic;
using HarborCity.RoadCore;
using Unity.Profiling;
using UnityEngine;
namespace HarborCity
{
    // Owns display objects only. Pool retention never limits simulated or shown
    // vehicles; all transport facts remain in World. Same cube/collider, with
    // a bounded shared material palette matching the task's zoning colour.
    public sealed class HarborCityVehicleViews:IDisposable
    {
        const int RetainedInactiveLimit=4096;
        static readonly ProfilerMarker sample=new ProfilerMarker("City.Views.Vehicles.Sync");
        HarborCityLayeredVehicles layered;long legacyPoseSamples;
        public long PoseSamples=>layered?.PoseSamples??legacyPoseSamples;
        public long IndexPoseSamples=>layered?.IndexPoseSamples??0;public int Represented=>layered?.Represented??Count;public int SummaryVehicles=>layered?.SummaryVehicles??0;public int Batched=>layered?.Batched??0;public double IndexMs=>layered?.IndexMs??0;
        readonly Material material;
        readonly Material residential,commercial,industrial;
        readonly Dictionary<int,Transform> cars=new Dictionary<int,Transform>();
        readonly List<int> removed=new List<int>();
        readonly Stack<Transform> pool=new Stack<Transform>();World bound;
        public int Count=>cars.Count;
        public int Pooled=>pool.Count;
        public int Created {get;private set;}
        public int Reused {get;private set;}
        public int Released {get;private set;}
        public HarborCityVehicleViews(Material material)
        {
            this.material=material;
            residential=new Material(material){color=HarborCityZoningViews.Colour(LandUse.Residential)};
            commercial=new Material(material){color=HarborCityZoningViews.Colour(LandUse.Commercial)};
            industrial=new Material(material){color=HarborCityZoningViews.Colour(LandUse.Industrial)};
        }
        Material ForPurpose(TransportPurpose purpose)=>purpose==TransportPurpose.Freight?industrial:purpose==TransportPurpose.Work||purpose==TransportPurpose.Home?residential:purpose==TransportPurpose.Shopping?commercial:material;
        static void Release(UnityEngine.Object value){if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}
        public Transform Instance(int id)=>cars.TryGetValue(id,out var car)?car:null;
        static Vector3 V(Point p)=>new Vector3((float)p.x,(float)p.y,(float)p.z);
        public void Sync(World world,Camera camera,HarborCityDisplaySettings settings)
        {
            if(!settings.layered||camera==null){if(layered!=null){Clear();}Sync(world);return;}
            using(sample.Auto()){
                if(!ReferenceEquals(bound,world)||layered==null){Clear();bound=world;if(world!=null)layered=new HarborCityLayeredVehicles(this,world,new[]{residential,commercial,industrial,material});}
                layered?.Sync(camera,settings);
            }
        }
        internal bool Prepare(int id,TransportPurpose purpose,HarborCityDisplaySettings settings,out Transform car)
        {
            if(cars.TryGetValue(id,out car)){car.GetComponent<Renderer>().sharedMaterial=ForPurpose(purpose);return true;}
            if(pool.Count>0){car=pool.Pop();Reused++;}
            else{if(!HarborCityDisplayBudget.Available(settings))return false;long at=System.Diagnostics.Stopwatch.GetTimestamp();var go=GameObject.CreatePrimitive(PrimitiveType.Cube);car=go.transform;car.localScale=new Vector3(.65f,.5f,1);Created++;HarborCityDisplayBudget.Charge(at);}
            car.name="Vehicle "+id;car.GetComponent<Renderer>().sharedMaterial=ForPurpose(purpose);car.gameObject.SetActive(true);cars.Add(id,car);return true;
        }
        internal void RetireExcept(HashSet<int> keep)
        {removed.Clear();foreach(var pair in cars)if(!keep.Contains(pair.Key))removed.Add(pair.Key);foreach(int id in removed){var car=cars[id];cars.Remove(id);car.gameObject.SetActive(false);Released++;if(pool.Count<RetainedInactiveLimit)pool.Push(car);else Release(car.gameObject);}}
        internal void AssertDisplayIndex()=>layered?.AssertExact();
        public void Sync(World world)
        {
            using(sample.Auto())
            {
                if(!ReferenceEquals(bound,world)){Clear();bound=world;}
                if(world==null)return;
                // Retire missing IDs first, so arrivals can supply this frame's
                // departures without allocating a second display object batch.
                removed.Clear();foreach(var entry in cars)if(world.FindVehicle(entry.Key)==null)removed.Add(entry.Key);
                foreach(int id in removed){var car=cars[id];cars.Remove(id);car.gameObject.SetActive(false);Released++;if(pool.Count<RetainedInactiveLimit)pool.Push(car);else Release(car.gameObject);}
                foreach(var v in world.Vehicles)
                {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    legacyPoseSamples++;
#endif
                    if(!cars.TryGetValue(v.id,out var car))
                    {
                        if(pool.Count>0){car=pool.Pop();Reused++;car.rotation=Quaternion.identity;}
                        else{var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.transform.localScale=new Vector3(.65f,.5f,1);car=go.transform;Created++;}
                        // Task purpose is immutable for this actor's trip. Apply
                        // on binding, including every reuse from a different task.
                        car.GetComponent<Renderer>().sharedMaterial=ForPurpose(v.Task.purpose);
                        car.gameObject.name="Freight "+v.id;cars.Add(v.id,car);
                    }
                    // A demolition-displaced task waiting for a legal departure is
                    // off-road; its last-known point is not a physical parked car.
                    car.gameObject.SetActive(v.status!=VehicleStatus.Delivered&&v.status!=VehicleStatus.AwaitingRestart);car.position=V(world.Position(v))+Vector3.up*.4f;
                    if(v.status==VehicleStatus.Active){var d=world.Network.Get(v.Current);var direction=V(d.Geometry.Sample(v.distance+.1))-V(d.Geometry.Sample(Math.Max(0,v.distance-.1)));if(direction.sqrMagnitude>1e-8)car.rotation=Quaternion.LookRotation(direction);}
                }
            }
        }
        public void Clear(){layered?.Dispose();layered=null;legacyPoseSamples=0;foreach(var car in cars.Values)if(car!=null)Release(car.gameObject);foreach(var car in pool)if(car!=null)Release(car.gameObject);cars.Clear();pool.Clear();removed.Clear();bound=null;}
        public void Dispose(){Clear();if(residential!=null)Release(residential);if(commercial!=null)Release(commercial);if(industrial!=null)Release(industrial);}
    }
}
