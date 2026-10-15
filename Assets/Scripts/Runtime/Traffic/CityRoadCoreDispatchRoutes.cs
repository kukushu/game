using System;
using System.Collections.Generic;
#if UNITY_5_3_OR_NEWER
using Unity.Profiling;
#endif

namespace HarborCity.RoadCore
{
    internal sealed class DispatchBlueprint
    {
        internal readonly long[] route;
        internal readonly double start,end,seconds;
        internal DispatchBlueprint(Network network,List<long> path,double start,double end)
        {
            route=path==null?null:path.ToArray();this.start=start;this.end=end;seconds=path==null?double.PositiveInfinity:0;
            if(path!=null)for(int i=0;i<path.Count;i++){var d=network.Get(path[i]);seconds+=((i==path.Count-1?end:d.EndDistance)-(i==0?start:d.StartDistance))/d.Speed;}
        }
    }
    // Bounded, derived route plans. Every admitted actor gets its own mutable route.
    // Construction/access changes invalidate; occupancy/clock do not affect reachability.
    internal sealed class DispatchRoutes
    {
        const int Capacity=4096;
        sealed class Entry
        {
            internal long key;internal bool resident;internal DispatchBlueprint blueprint;internal Entry newer,older;
        }
        readonly Dictionary<long,Entry> freight=new Dictionary<long,Entry>(CityRoadKeyComparer.Instance);
        readonly Dictionary<long,Entry> passengers=new Dictionary<long,Entry>(CityRoadKeyComparer.Instance);
        Entry newest,oldest;
        void Touch(Entry entry)
        {
            if(ReferenceEquals(entry,newest))return;
            if(entry.newer!=null)entry.newer.older=entry.older;
            if(entry.older!=null)entry.older.newer=entry.newer;
            if(ReferenceEquals(entry,oldest))oldest=entry.newer;
            entry.newer=null;entry.older=newest;if(newest!=null)newest.newer=entry;newest=entry;if(oldest==null)oldest=entry;
        }
        void Add(long key,bool resident,DispatchBlueprint blueprint)
        {
            if(Count==Capacity)
            {
                var removed=oldest;(removed.resident?passengers:freight).Remove(removed.key);oldest=removed.newer;
                if(oldest!=null)oldest.older=null;else newest=null;
#if CITY_CORE_ATTRIBUTION
                CityCoreProfile.CacheEvictions++;
#endif
            }
            var entry=new Entry{key=key,resident=resident,blueprint=blueprint};(resident?passengers:freight).Add(key,entry);Touch(entry);
        }
        Network network;
        public int Count=>freight.Count+passengers.Count;
        public long Preparations {get;private set;}
#if UNITY_5_3_OR_NEWER
        static readonly ProfilerMarker prepareSample=new ProfilerMarker("City.RoadCore.Dispatch.PrepareRoute");
#endif
        public void Reset(Network network){this.network=network;freight.Clear();passengers.Clear();newest=oldest=null;}
        public DispatchBlueprint Get(Network current,int origin,int destination,RoadAccess source,RoadAccess target,bool resident,IJunctionEndpointProvider provider)
        {
#if CITY_CORE_ATTRIBUTION
            using var observation=HarborCity.RoadCore.CityCoreProfile.Measure(HarborCity.RoadCore.CityCoreProfile.Part.RouteLookup);
#endif

            if(!ReferenceEquals(network,current))Reset(current);var table=resident?passengers:freight;long key=((long)origin<<32)|(uint)destination;if(table.TryGetValue(key,out var cached))
            {
#if CITY_CORE_ATTRIBUTION
                CityCoreProfile.CacheHits++;
#endif
                Touch(cached);return cached.blueprint;
            }
#if UNITY_5_3_OR_NEWER
            using(prepareSample.Auto())
#endif
            {
                double start=0,end=-1;List<long> route=null;Preparations++;
                if(source!=null&&target!=null)route=AccessRouting.Find(current,source,target,resident,out start,out end);
                else if(!resident&&provider!=null){route=current.Route(provider.Junction(origin),provider.Junction(destination));if(route!=null&&route.Count>0){start=current.Get(route[0]).StartDistance;end=current.Get(route[route.Count-1]).EndDistance;}}
                var result=new DispatchBlueprint(current,route,start,end);
#if CITY_CORE_ATTRIBUTION
                if(route==null)CityCoreProfile.Unreachable++;
#endif
                Add(key,resident,result);return result;
            }
        }
    }
}
