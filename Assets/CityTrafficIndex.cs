#if UNITY_5_3_OR_NEWER && ENABLE_PROFILER && (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CITY_TRAFFIC_PROFILING_DISABLED
#define CITY_TRAFFIC_PROFILING
#endif
using System.Collections.Generic;

namespace HarborCity
{
    // Borrowed candidate lists, never authoritative state or serialized data.
    // Only Following uses these buckets: it reduces all candidates with Min,
    // so bucket insertion order cannot replace arrival/queue priority anywhere.
    internal sealed class DirectedTrafficIndex
    {
        static readonly List<TrafficTrip> empty=new List<TrafficTrip>();
        readonly Dictionary<(int from,int to),List<TrafficTrip>> buckets=new Dictionary<(int,int),List<TrafficTrip>>();
        readonly Dictionary<TrafficTrip,(int from,int to)> members=new Dictionary<TrafficTrip,(int,int)>();
        CityRoads graph;
        int revision=-1;
        public bool Active {get;private set;}
#if CITY_TRAFFIC_PROFILING
        static readonly Unity.Profiling.ProfilerMarker buildSample=new Unity.Profiling.ProfilerMarker("City.Traffic.Index.Build");
        static readonly Unity.Profiling.ProfilerMarker syncSample=new Unity.Profiling.ProfilerMarker("City.Traffic.Index.Sync");
#endif
        public void Begin(List<TrafficTrip> trips,CityRoads roads)
        {
#if CITY_TRAFFIC_PROFILING
            using var sample=buildSample.Auto();
#endif
            // Purge obsolete edge keys when the graph changes, not every step.
            if(graph!=roads || revision!=roads.revision){buckets.Clear();graph=roads;revision=roads.revision;}
            else foreach(var bucket in buckets.Values)bucket.Clear();
            members.Clear();Active=true;
            foreach(var t in trips)Add(t);
        }
        public void End(){Active=false;}
        List<TrafficTrip> Bucket((int,int) key)
        {if(!buckets.TryGetValue(key,out var result))buckets[key]=result=new List<TrafficTrip>();return result;}
        public List<TrafficTrip> OnEdge(int from,int to)=>buckets.TryGetValue((from,to),out var result)?result:empty;
        public void Add(TrafficTrip t)
        {
            if(!Active || members.ContainsKey(t))return;
            var key=(t.Current,t.Next);members.Add(t,key);Bucket(key).Add(t);
        }
        public void Remove(TrafficTrip t)
        {
            if(!Active || !members.TryGetValue(t,out var key))return;
            buckets[key].Remove(t);members.Remove(t);
        }
        public void Refresh(TrafficTrip t)
        {
#if CITY_TRAFFIC_PROFILING
            using var sample=syncSample.Auto();
#endif
            // An arrival/removal must not resurrect a snapshot object.
            if(!Active || !members.TryGetValue(t,out var previous))return;
            var key=(t.Current,t.Next);if(key==previous)return;
            buckets[previous].Remove(t);Bucket(key).Add(t);members[t]=key;
        }
    }
    public sealed partial class CityTraffic
    {
#if !CITY_TRAFFIC_EDGE_INDEX_DISABLED
        readonly DirectedTrafficIndex followingIndex=new DirectedTrafficIndex();
#endif
        List<TrafficTrip> FollowingCandidates(int from,int to)
        {
#if CITY_TRAFFIC_EDGE_INDEX_DISABLED
            return State.trips;
#else
            return followingIndex.Active?followingIndex.OnEdge(from,to):State.trips;
#endif
        }
        void RemoveTrip(TrafficTrip trip)
        {
            bool removed=State.trips.Remove(trip);
#if !CITY_TRAFFIC_EDGE_INDEX_DISABLED
            if(removed)followingIndex.Remove(trip);
#endif
        }
    }
}
