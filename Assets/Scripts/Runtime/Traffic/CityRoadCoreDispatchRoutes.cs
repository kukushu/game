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
            if(path!=null)for(int i=0;i<path.Count;i++){var d=network.Get(path[i]);seconds+=(i==path.Count-1?end:d.Geometry.Length)/d.Speed;if(i==0)seconds-=start/d.Speed;}
        }
    }
    // Bounded, derived route plans. Every admitted actor gets its own mutable route.
    // Construction/access changes invalidate; occupancy/clock do not affect reachability.
    internal sealed class DispatchRoutes
    {
        readonly Dictionary<long,DispatchBlueprint> freight=new Dictionary<long,DispatchBlueprint>(CityRoadKeyComparer.Instance);
        readonly Dictionary<long,DispatchBlueprint> passengers=new Dictionary<long,DispatchBlueprint>(CityRoadKeyComparer.Instance);
        Network network;
        public int Count=>freight.Count+passengers.Count;
        public long Preparations {get;private set;}
#if UNITY_5_3_OR_NEWER
        static readonly ProfilerMarker prepareSample=new ProfilerMarker("City.RoadCore.Dispatch.PrepareRoute");
#endif
        public void Reset(Network network){this.network=network;freight.Clear();passengers.Clear();}
        public DispatchBlueprint Get(Network current,int origin,int destination,RoadAccess source,RoadAccess target,bool resident,IJunctionEndpointProvider provider)
        {
            if(!ReferenceEquals(network,current))Reset(current);var table=resident?passengers:freight;long key=((long)origin<<32)|(uint)destination;DispatchBlueprint result;if(table.TryGetValue(key,out result))return result;
#if UNITY_5_3_OR_NEWER
            using(prepareSample.Auto())
#endif
            {
                double start=0,end=-1;List<long> route=null;Preparations++;
                if(source!=null&&target!=null)route=AccessRouting.Find(current,source,target,resident,out start,out end);
                else if(!resident&&provider!=null){route=current.Route(provider.Junction(origin),provider.Junction(destination));if(route!=null&&route.Count>0)end=current.Get(route[route.Count-1]).Geometry.Length;}
                result=new DispatchBlueprint(current,route,start,end);if(Count>=4096)Reset(current);table.Add(key,result);return result;
            }
        }
    }
}
