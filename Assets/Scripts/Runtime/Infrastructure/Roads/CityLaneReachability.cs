using System.Collections.Generic;

namespace HarborCity.RoadCore
{
    // Derived from actual allowed lane connections, never physical proximity.
    // Network instances are immutable; replacing one on construction replaces this
    // cache too. Bounded FIFO avoids O(L squared) permanent reachability storage.
    internal sealed class LaneReachability
    {
        const int MaximumSources=64;
        readonly Dictionary<long,List<long>> next=new Dictionary<long,List<long>>(CityRoadKeyComparer.Instance);
        readonly Dictionary<long,HashSet<long>> reached=new Dictionary<long,HashSet<long>>(CityRoadKeyComparer.Instance);
        readonly Queue<long> sources=new Queue<long>();
        internal LaneReachability(Network network)
        {
            foreach(var d in network.Drivables)
            {
                if(!d.IsConnection)continue;
                var connection=(LaneConnection)d;long from=connection.Incoming.Id,to=connection.Outgoing.Id;
                if(!next.TryGetValue(from,out var exits))next.Add(from,exits=new List<long>());
                exits.Add(to);
            }
        }
        internal bool CanReach(RoadAccess source,RoadAccess target)
        {
            if(source.lane==target.lane&&target.distance>=source.distance)return true;
            var set=Reachable(source.lane);return set!=null&&set.Contains(target.lane);
        }
        internal bool HasCycle(long source){var set=Reachable(source);return set!=null&&set.Contains(source);}
        HashSet<long> Reachable(long source)
        {
            if(!next.TryGetValue(source,out var first))return null;
            if(!reached.TryGetValue(source,out var set))
            {
                set=new HashSet<long>(CityRoadKeyComparer.Instance);var queue=new Queue<long>();
                // Do not seed the source itself: reaching an upstream station on
                // the same lane requires a real directed cycle back to that lane.
                foreach(long lane in first)if(set.Add(lane))queue.Enqueue(lane);
                while(queue.Count>0)
                {
                    long lane=queue.Dequeue();if(!next.TryGetValue(lane,out var exits))continue;
                    foreach(long exit in exits)if(set.Add(exit))queue.Enqueue(exit);
                }
                if(reached.Count==MaximumSources)reached.Remove(sources.Dequeue());
                reached.Add(source,set);sources.Enqueue(source);
            }
            return set;
        }
    }
}
