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
    // One bounded derived OD cache: scalar quotes may materialize a blueprint later.
    // Capacity and LRU key order are unchanged; every actor owns its mutable route.
    // Construction/access changes invalidate; occupancy/clock do not affect reachability.
    internal sealed class DispatchRoutes
    {
        const int Capacity=4096;
        sealed class Entry
        {
            internal long key;internal bool resident;internal DispatchBlueprint blueprint;internal double seconds;internal Entry newer,older;
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
        void Add(long key,bool resident,DispatchBlueprint blueprint,double seconds)
        {
            if(Count==Capacity)
            {
                var removed=oldest;(removed.resident?passengers:freight).Remove(removed.key);oldest=removed.newer;
                if(oldest!=null)oldest.older=null;else newest=null;
#if CITY_CORE_ATTRIBUTION
                CityCoreProfile.CacheEvictions++;
                CityCoreProfile.ObserveQuery(CityCoreProfile.QueryKind.Evicted,CityCoreProfile.QueryResult.Miss,0,(int)(removed.key>>32),(int)removed.key,removed.resident);
#endif
            }
            var entry=new Entry{key=key,resident=resident,blueprint=blueprint,seconds=seconds};(resident?passengers:freight).Add(key,entry);Touch(entry);
        }
        Network network;
        public int Count=>freight.Count+passengers.Count;
        public long Preparations {get;private set;}
        public long CostPreparations {get;private set;}
#if UNITY_5_3_OR_NEWER
        static readonly ProfilerMarker prepareSample=new ProfilerMarker("City.RoadCore.Dispatch.PrepareRoute"),costSample=new ProfilerMarker("City.RoadCore.Dispatch.PrepareCost");
#endif
        public void Reset(Network network){
#if CITY_CORE_ATTRIBUTION
            CityCoreProfile.ObserveQuery(CityCoreProfile.QueryKind.Reset,CityCoreProfile.QueryResult.Miss,0,0,0);
#endif
            this.network=network;freight.Clear();passengers.Clear();newest=oldest=null;}
        // World has proved that all old routing surfaces/ports are unchanged and
        // the added component is disconnected, with no access edits. Old OD costs,
        // unreachable results and immutable blueprints therefore remain exact.
        internal void RetainIndependentAddition(Network before,Network after)
        {if(ReferenceEquals(network,before))network=after;else Reset(after);}
        public double GetTravelSeconds(Network current,int origin,int destination,RoadAccess source,RoadAccess target)
        {
#if CITY_CORE_ATTRIBUTION
            using var observation=CityCoreProfile.Measure(CityCoreProfile.Part.RouteLookup);
#endif
            if(!ReferenceEquals(network,current))Reset(current);long key=((long)origin<<32)|(uint)destination;
            if(passengers.TryGetValue(key,out var cached))
            {
#if CITY_CORE_ATTRIBUTION
                CityCoreProfile.CacheHits++;CityCoreProfile.ObserveQuery(CityCoreProfile.QueryKind.CostQuote,CityCoreProfile.QueryResult.Hit,0,origin,destination,true);
#endif
                Touch(cached);return cached.seconds;
            }
#if CITY_CORE_ATTRIBUTION
            CityCoreProfile.ObserveQuery(CityCoreProfile.QueryKind.CostQuote,CityCoreProfile.QueryResult.Miss,0,origin,destination,true);
#endif
#if UNITY_5_3_OR_NEWER
            using(costSample.Auto())
#endif
            {
                CostPreparations++;double seconds=AccessRouting.EstimateSeconds(current,source,target,true);Add(key,true,null,seconds);return seconds;
            }
        }
        public DispatchBlueprint Get(Network current,int origin,int destination,RoadAccess source,RoadAccess target,bool resident,IJunctionEndpointProvider provider)
        {
#if CITY_CORE_ATTRIBUTION
            using var observation=CityCoreProfile.Measure(CityCoreProfile.Part.RouteLookup);
#endif
            if(!ReferenceEquals(network,current))Reset(current);var table=resident?passengers:freight;long key=((long)origin<<32)|(uint)destination;table.TryGetValue(key,out var cached);
            if(cached!=null)
            {
#if CITY_CORE_ATTRIBUTION
                CityCoreProfile.CacheHits++;CityCoreProfile.ObserveQuery(CityCoreProfile.QueryKind.Blueprint,CityCoreProfile.QueryResult.Hit,0,origin,destination,resident);
#endif
                Touch(cached);if(cached.blueprint!=null)return cached.blueprint;
            }
#if CITY_CORE_ATTRIBUTION
            else CityCoreProfile.ObserveQuery(CityCoreProfile.QueryKind.Blueprint,CityCoreProfile.QueryResult.Miss,0,origin,destination,resident);
            CityCoreProfile.ObserveQuery(CityCoreProfile.QueryKind.Materialized,cached==null?CityCoreProfile.QueryResult.Miss:CityCoreProfile.QueryResult.Hit,0,origin,destination,resident);
#endif
#if UNITY_5_3_OR_NEWER
            using(prepareSample.Auto())
#endif
            {
                double start=0,end=-1;List<long> route=null;Preparations++;
                if(cached!=null&&double.IsPositiveInfinity(cached.seconds)){/* Exact static quote already proves NoRoute. */}
                else if(source!=null&&target!=null)route=AccessRouting.Find(current,source,target,resident,out start,out end);
                else if(!resident&&provider!=null){route=current.Route(provider.Junction(origin),provider.Junction(destination));if(route!=null&&route.Count>0){start=current.Get(route[0]).StartDistance;end=current.Get(route[route.Count-1]).EndDistance;}}
                var result=new DispatchBlueprint(current,route,start,end);
#if CITY_CORE_ATTRIBUTION
                if(route==null)CityCoreProfile.Unreachable++;
#endif
                if(cached==null)Add(key,resident,result,result.seconds);
                else
                {
                    if(BitConverter.DoubleToInt64Bits(cached.seconds)!=BitConverter.DoubleToInt64Bits(result.seconds))throw new InvalidOperationException("Static quote and materialized route disagree");
                    cached.blueprint=result;
                }
                return result;
            }
        }
    }
}
