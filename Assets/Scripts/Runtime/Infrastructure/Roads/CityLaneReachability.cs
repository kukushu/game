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
                long packed=-d.Id-1,from=packed>>32,to=(uint)packed;
                if(!next.TryGetValue(from,out var exits))next.Add(from,exits=new List<long>());
                exits.Add(to);
            }
        }
        internal bool CanReach(RoadAccess source,RoadAccess target)
        {
            if(source.lane==target.lane&&target.distance>=source.distance)return true;
            if(!next.TryGetValue(source.lane,out var first))return false;
            if(!reached.TryGetValue(source.lane,out var set))
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
                reached.Add(source.lane,set);sources.Enqueue(source.lane);
            }
            return set.Contains(target.lane);
        }
    }
}
