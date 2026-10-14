using System;
using System.Collections.Generic;

namespace HarborCity.RoadCore
{
    internal static class AccessRouting
    {
        // One saved association, opposite curb access is derived from the same road
        // sample correspondence. Initial open-median access rule; no driveway animation.
        static RoadAccess Opposite(Network network,RoadAccess access)
        {
            var lane=network.Get(access.lane);long id=Network.LaneId(lane.RoadId,!lane.Reverse,lane.LaneIndex);if(!network.Contains(id))return null;
            return new RoadAccess{endpoint=access.endpoint,lane=id,distance=lane.Geometry.MapReverseDistance(network.Get(id).Geometry,access.distance),use=access.use};
        }
        public static RoadAccess AtDirection(Network network,RoadAccess access,bool reverse)=>network.Get(access.lane).Reverse==reverse?access:Opposite(network,access);
        public static List<long> Find(Network network,RoadAccess source,RoadAccess target,bool bothDirections,out double start,out double end)
        {
            start=0;end=0;if(!source.valid||!target.valid)return null;if(!bothDirections){start=source.distance;end=target.distance;return network.Route(source,target);}var sources=new[]{source,Opposite(network,source)};var goals=new[]{target,Opposite(network,target)};
            List<long> best=null;double cost=double.PositiveInfinity;
            foreach(var a in sources)foreach(var b in goals)
            {
                if(a==null||b==null)continue;var route=network.Route(a,b);if(route==null||route.Count==0)continue;double candidate=0;
                for(int i=0;i<route.Count;i++){var d=network.Get(route[i]);candidate+=(i==route.Count-1?b.distance:d.Geometry.Length)/d.Speed;if(i==0)candidate-=a.distance/d.Speed;}
                if(candidate<cost){cost=candidate;best=route;start=a.distance;end=b.distance;}
            }
            return best;
        }
        public static bool Matches(Network network,RoadAccess access,long lane,double distance,bool bothDirections)
        {
            if(access.lane==lane)return Math.Abs(access.distance-distance)<1e-7;if(!bothDirections)return false;var other=Opposite(network,access);return other!=null&&other.lane==lane&&Math.Abs(other.distance-distance)<1e-7;
        }
        public static List<long> FromLane(Network network,long lane,double distance,RoadAccess target,bool bothDirections,out double end)
        {
            var source=new RoadAccess{lane=lane,distance=distance};var route=network.Route(source,target);end=target.distance;double cost=Cost(network,route,distance,end);
            if(bothDirections){var opposite=Opposite(network,target);if(opposite!=null){var candidate=network.Route(source,opposite);double other=Cost(network,candidate,distance,opposite.distance);if(other<cost){route=candidate;end=opposite.distance;}}}return route;
        }
        static double Cost(Network network,List<long> route,double start,double end)
        {if(route==null)return double.PositiveInfinity;double cost=0;for(int i=0;i<route.Count;i++){var d=network.Get(route[i]);cost+=(i==route.Count-1?end:d.Geometry.Length)/d.Speed;if(i==0)cost-=start/d.Speed;}return cost;}
    }
}
