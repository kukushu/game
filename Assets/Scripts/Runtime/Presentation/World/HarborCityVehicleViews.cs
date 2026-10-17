using System;
using System.Collections.Generic;
using HarborCity.RoadCore;
using Unity.Profiling;
using UnityEngine;
namespace HarborCity
{
    // Owns display objects only. Pool retention never limits simulated or shown
    // vehicles; all transport facts remain in World. Same cube/material/collider.
    public sealed class HarborCityVehicleViews:IDisposable
    {
        const int RetainedInactiveLimit=4096;
        static readonly ProfilerMarker sample=new ProfilerMarker("City.Views.Vehicles.Sync");
        readonly Material material;
        readonly Dictionary<int,Transform> cars=new Dictionary<int,Transform>();
        readonly List<int> removed=new List<int>();
        readonly Stack<Transform> pool=new Stack<Transform>();World bound;
        public int Count=>cars.Count;
        public int Pooled=>pool.Count;
        public int Created {get;private set;}
        public int Reused {get;private set;}
        public int Released {get;private set;}
        public HarborCityVehicleViews(Material material){this.material=material;}
        public Transform Instance(int id)=>cars.TryGetValue(id,out var car)?car:null;
        static Vector3 V(Point p)=>new Vector3((float)p.x,(float)p.y,(float)p.z);
        public void Sync(World world)
        {
            using(sample.Auto())
            {
                if(!ReferenceEquals(bound,world)){Clear();bound=world;}
                if(world==null)return;
                // Retire missing IDs first, so arrivals can supply this frame's
                // departures without allocating a second display object batch.
                removed.Clear();foreach(var entry in cars)if(world.FindVehicle(entry.Key)==null)removed.Add(entry.Key);
                foreach(int id in removed){var car=cars[id];cars.Remove(id);car.gameObject.SetActive(false);Released++;if(pool.Count<RetainedInactiveLimit)pool.Push(car);else UnityEngine.Object.Destroy(car.gameObject);}
                foreach(var v in world.Vehicles)
                {
                    if(!cars.TryGetValue(v.id,out var car))
                    {
                        if(pool.Count>0){car=pool.Pop();Reused++;car.rotation=Quaternion.identity;}
                        else{var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.GetComponent<Renderer>().sharedMaterial=material;go.transform.localScale=new Vector3(.65f,.5f,1);car=go.transform;Created++;}
                        car.gameObject.name="Freight "+v.id;cars.Add(v.id,car);
                    }
                    car.gameObject.SetActive(v.status!=VehicleStatus.Delivered);car.position=V(world.Position(v))+Vector3.up*.4f;
                    if(v.status==VehicleStatus.Active){var d=world.Network.Get(v.Current);var direction=V(d.Geometry.Sample(v.distance+.1))-V(d.Geometry.Sample(Math.Max(0,v.distance-.1)));if(direction.sqrMagnitude>1e-8)car.rotation=Quaternion.LookRotation(direction);}
                }
            }
        }
        public void Clear(){foreach(var car in cars.Values)if(car!=null)UnityEngine.Object.Destroy(car.gameObject);foreach(var car in pool)if(car!=null)UnityEngine.Object.Destroy(car.gameObject);cars.Clear();pool.Clear();removed.Clear();bound=null;}
        public void Dispose()=>Clear();
    }
}
