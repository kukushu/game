using System;
using System.Collections.Generic;
namespace HarborCity
{
    // Reusable local index of the currently open/stocked stores. Rebuilt once per
    // shopping period, not per citizen or traffic step. No inventory copies.
    internal sealed class CityRetailIndex
    {
        const float BucketSize=16;
        readonly Dictionary<(int,int),List<CommercialBuilding>> buckets=new Dictionary<(int,int),List<CommercialBuilding>>();
        readonly List<(int x,int z)> occupied=new List<(int x,int z)>();
        readonly List<Candidate> candidates=new List<Candidate>();
        internal struct Candidate {internal CommercialBuilding shop;internal double squared;}
        sealed class Order:IComparer<Candidate>{internal static readonly Order Instance=new Order();public int Compare(Candidate a,Candidate b){int c=a.squared.CompareTo(b.squared);return c!=0?c:a.shop.id.CompareTo(b.shop.id);}}
        internal int Count {get;private set;}
        internal void Begin(){foreach(var key in occupied)buckets[key].Clear();occupied.Clear();Count=0;}
        internal void Add(CommercialBuilding shop){var key=((int)Math.Floor(shop.x/BucketSize),(int)Math.Floor(shop.z/BucketSize));if(!buckets.TryGetValue(key,out var list))buckets.Add(key,list=new List<CommercialBuilding>());if(list.Count==0)occupied.Add(key);list.Add(shop);Count++;}
        internal List<Candidate> Collect(float x,float z,float radius)
        {
            candidates.Clear();int ax=(int)Math.Floor((x-radius)/BucketSize),bx=(int)Math.Floor((x+radius)/BucketSize),az=(int)Math.Floor((z-radius)/BucketSize),bz=(int)Math.Floor((z+radius)/BucketSize);double limit=(double)radius*radius;
            // Wide searches in a sparse period must not probe every empty map
            // cell. Both paths select the same buckets and use the same exact
            // distance test and total order; household/dispatch order is untouched.
            double cells=((long)bx-ax+1)*(double)((long)bz-az+1);
            if(cells>occupied.Count){foreach(var key in occupied)if(key.x>=ax&&key.x<=bx&&key.z>=az&&key.z<=bz)CollectBucket(buckets[key],x,z,limit);}
            else for(int ix=ax;ix<=bx;ix++)for(int iz=az;iz<=bz;iz++)if(buckets.TryGetValue((ix,iz),out var list))CollectBucket(list,x,z,limit);
            candidates.Sort(Order.Instance);return candidates;
        }
        void CollectBucket(List<CommercialBuilding> list,float x,float z,double limit)
        {foreach(var shop in list){double dx=shop.x-x,dz=shop.z-z,distance=dx*dx+dz*dz;if(distance<=limit)candidates.Add(new Candidate{shop=shop,squared=distance});}}
    }
}
