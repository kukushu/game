using System;
using System.Collections.Generic;
namespace HarborCity
{
    public struct RoadLane
    {
        public long id;
        public int edgeId,from,to,index;
        public float offset;
    }
    public sealed partial class CityRoads
    {
        public int LaneCount(int from,int to)
        {
            Cache();if(!CanTravel(from,to) || !roadLookup.TryGetValue(((long)from<<32)|(uint)to,out var e))return 0;
            return CityRoadTypes.Lanes(e.kind,e.oneWay);
        }
        public float EdgeWidth(int from,int to)
        {Cache();return roadLookup.TryGetValue(((long)from<<32)|(uint)to,out var e)?CityRoadTypes.Width(e.kind):Width;}
        public float LaneOffset(int from,int to,int lane)
        {
            Cache();if(!roadLookup.TryGetValue(((long)from<<32)|(uint)to,out var e))return .55f;
            return e.kind==RoadKind.FourLane?.55f+1.1f*lane:e.oneWay && lane==1?-.55f:.55f;
        }
        public IEnumerable<RoadLane> Lanes(int from,int to)
        {
            Cache();int count=LaneCount(from,to);if(count==0)yield break;
            var e=roadLookup[((long)from<<32)|(uint)to];
            for(int lane=0;lane<count;lane++)yield return new RoadLane{id=((long)e.id<<2)|(from<to?0L:2L)|(long)(uint)lane,edgeId=e.id,from=from,to=to,index=lane,offset=LaneOffset(from,to,lane)};
        }
        // Preserve the current lane through short graph segments, narrow at a
        // merge, and distribute a one-to-two lane entry deterministically.
        public int TripLane(TrafficTrip t,int segment)
        {
            if(t.walking || segment<0 || segment>=t.route.Count-1)return 0;
            if(segment==t.segment-1)return t.previousLane;
            if(segment<t.segment)return 0;
            int lane=t.lane;
            for(int s=t.segment+1;s<=segment;s++)
            {
                int oldCount=Math.Max(1,LaneCount(t.route[s-1],t.route[s]));int count=Math.Max(1,LaneCount(t.route[s],t.route[s+1]));
                lane=oldCount==1 && count==2?t.id%2:Math.Min(lane,count-1);
            }
            return lane;
        }
    }
    public sealed partial class CityTraffic
    {
        int EntryLane(List<int> route,TrafficTrip self)
        {
            if(self?.walking==true)return 0;
            int count=route.Count<2?1:Math.Max(1,city.roads.LaneCount(route[0],route[1]));
            for(int lane=0;lane<count;lane++)
            {
                bool clear=true;
                foreach(var other in State.trips)
                {
                    if(other==self || other.walking || other.status==TripStatus.Visiting)continue;
                    if(other.Current==route[0] && (route.Count==1 || other.Next==route[1]) && other.lane==lane && Behind(other)<Gap){clear=false;break;}
                    // Spawning at a node remains conservative with an incoming car.
                    if(other.Next==route[0] && Ahead(other)<Gap){clear=false;break;}
                }
                if(clear)return lane;
            }
            return -1;
        }
        void InitializeLane(TrafficTrip trip)
        {trip.lane=trip.walking?0:Math.Max(0,EntryLane(trip.route,trip));trip.previousLane=0;ClearJunctionQueue(trip);}
        void AddTrip(TrafficTrip trip){InitializeLane(trip);State.trips.Add(trip);}
    }
}
