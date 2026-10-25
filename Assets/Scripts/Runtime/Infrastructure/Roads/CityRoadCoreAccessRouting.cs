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
            start=0;end=0;if(!source.valid||!target.valid)return null;if(!bothDirections){start=source.distance;end=target.distance;return network.Route(source,target);}
            var otherSource=Opposite(network,source);var otherTarget=Opposite(network,target);
            if(otherSource!=null&&otherTarget!=null)
            {
                int choice0,choice1;var first=network.RouteToEither(source,target,otherTarget,out choice0);var second=network.RouteToEither(otherSource,target,otherTarget,out choice1);
                double firstEnd=choice0==0?target.distance:otherTarget.distance,secondEnd=choice1==0?target.distance:otherTarget.distance;
                double cost=Cost(network,first,source.distance,firstEnd),candidate=Cost(network,second,otherSource.distance,secondEnd);
                if(second!=null&&candidate<cost){start=otherSource.distance;end=secondEnd;return second;}
                if(first!=null){start=source.distance;end=firstEnd;}return first;
            }
            // Do not add a second search on one-way or other <=2-combination cases.
            var sources=new[]{source,otherSource};var goals=new[]{target,otherTarget};List<long> best=null;double bestCost=double.PositiveInfinity;
            foreach(var a in sources)foreach(var b in goals)
            {if(a==null||b==null)continue;var route=network.Route(a,b);if(route==null||route.Count==0)continue;double cost=Cost(network,route,a.distance,b.distance);if(cost<bestCost){bestCost=cost;best=route;start=a.distance;end=b.distance;}}return best;
        }
        public static double EstimateSeconds(Network network,RoadAccess source,RoadAccess target,bool bothDirections)
        {
            if(!source.valid||!target.valid)return double.PositiveInfinity;
            return network.MinimumTravelSeconds(source,target,bothDirections?Opposite(network,source):null,bothDirections?Opposite(network,target):null);
        }
        public static bool Matches(Network network,RoadAccess access,long lane,double distance,bool bothDirections)
        {
            if(access.lane==lane)return Math.Abs(access.distance-distance)<1e-7;if(!bothDirections)return false;var other=Opposite(network,access);return other!=null&&other.lane==lane&&Math.Abs(other.distance-distance)<1e-7;
        }
        public static List<long> FromLane(Network network,long lane,double distance,RoadAccess target,bool bothDirections,out double end)
        {
            var source=new RoadAccess{lane=lane,distance=distance};end=target.distance;
            if(bothDirections)
            {
                var opposite=Opposite(network,target);
                if(opposite!=null)
                {
                    // The physical source is fixed. Both legal destination curbs
                    // reuse normalized lane-pair paths; exact stop costs choose a side.
                    var route=network.RouteToEither(source,target,opposite,out int selected);
                    if(selected==1)end=opposite.distance;return route;
                }
            }
            return network.Route(source,target);
        }
        static double Cost(Network network,List<long> route,double start,double end)
        {if(route==null)return double.PositiveInfinity;double cost=0;for(int i=0;i<route.Count;i++){var d=network.Get(route[i]);cost+=((i==route.Count-1?end:d.EndDistance)-(i==0?start:d.StartDistance))/d.Speed;}return cost;}
    }
}
