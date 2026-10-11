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
            return CityRoadTypes.LaneOffset(e.kind,e.oneWay,lane);
        }
        public IEnumerable<RoadLane> Lanes(int from,int to)
        {
            Cache();int count=LaneCount(from,to);if(count==0)yield break;
            var e=roadLookup[((long)from<<32)|(uint)to];
            for(int lane=0;lane<count;lane++)yield return new RoadLane{id=((long)e.id<<3)|(from<to?0L:4L)|(long)(uint)lane,edgeId=e.id,from=from,to=to,index=lane,offset=LaneOffset(from,to,lane)};
        }
        // Preserve the current lane through short graph segments, narrow at a
        // merge, and distribute a one-to-two lane entry deterministically.
        public int TripLane(TrafficTrip t,int segment)
        {
            if(t.walking || segment<0 || segment>=t.route.Count-1)return 0;
            if(segment==t.segment-1)return t.previousLane;
            if(segment<t.segment)return Math.Min(t.previousLane,Math.Max(0,LaneCount(t.route[segment],t.route[segment+1])-1));
            int lane=t.lane;
            for(int s=t.segment+1;s<=segment;s++)
            {
                int oldCount=Math.Max(1,LaneCount(t.route[s-1],t.route[s]));int count=Math.Max(1,LaneCount(t.route[s],t.route[s+1]));
                lane=oldCount==1 && count>1?t.id%count:Math.Min(lane,count-1);
            }
            return lane;
        }
        public float TripLaneOffset(TrafficTrip t,int segment)
        {
            if(segment<0 || segment>=t.route.Count-1)return .55f;
            int s=Math.Max(0,Math.Min(segment,t.route.Count-2));
            float target=LaneOffset(t.route[s],t.route[s+1],TripLane(t,s));
            if(t.laneChangeRemaining<=0 || Math.Abs(s-t.segment)>1 || LaneCount(t.route[s],t.route[s+1])<2
                || Math.Abs(LaneOffset(t.route[s],t.route[s+1],0)-LaneOffset(t.Current,t.Next,0))>.01f)return target;
            target=LaneOffset(t.route[s],t.route[s+1],t.lane);
            float blend=1-t.laneChangeRemaining/CityTraffic.LaneChangeSeconds;blend=blend*blend*(3-2*blend);
            float from=LaneOffset(t.route[s],t.route[s+1],t.laneChangeFrom);
            return from+(target-from)*blend;
        }
    }
    public sealed partial class CityTraffic
    {
        public const float LaneChangeSeconds=.35f;
        const float LaneChangeRest=1.25f;
        static bool OccupiesLane(TrafficTrip t,int lane)=>t.lane==lane || t.laneChangeRemaining>0 && t.laneChangeFrom==lane;
        void AdvanceLaneChange(TrafficTrip t)
        {
            bool changing=t.laneChangeRemaining>0;
            t.laneChangeRemaining=Math.Max(0,t.laneChangeRemaining-Step);t.laneChangeCooldown=Math.Max(0,t.laneChangeCooldown-Step);
            if(changing && t.laneChangeRemaining==0 && t.segment>0
                && city.roads.LaneCount(t.route[t.segment-1],t.Current)>t.lane
                && Math.Abs(city.roads.LaneOffset(t.route[t.segment-1],t.Current,0)-city.roads.LaneOffset(t.Current,t.Next,0))<.01f)
                t.previousLane=t.lane;
        }
        // Physical lane ordering matters: small one-way roads number their left
        // lane 1, whereas four-lane roads number their rightmost lane 1.
        int TurnLane(List<int> route,int segment,int fallback)
        {
            if(segment<0 || segment+1>=route.Count || city.roads.LaneCount(route[segment],route[segment+1])<2)return fallback;
            int count=city.roads.LaneCount(route[segment],route[segment+1]);
            float speed=480f*city.roads.SpeedFactor(route[segment],route[segment+1])/city.society.settings.secondsPerDay;
            float lookahead=Math.Max(8,speed*(count-1)*(LaneChangeRest+LaneChangeSeconds)+4),distance=0;
            for(int s=segment;s+2<route.Count && distance<=lookahead;s++)
            {
                distance+=Units(route[s],route[s+1]);int node=route[s+1];
                if(city.roads.Neighbors(node).Count<3)continue;
                var a=city.roads.Node(route[s]);var n=city.roads.Node(node);var b=city.roads.Node(route[s+2]);
                float ix=n.x-a.x,iz=n.z-a.z,ox=b.x-n.x,oz=b.z-n.z;
                float norm=(float)Math.Sqrt((ix*ix+iz*iz)*(ox*ox+oz*oz));
                float turn=norm>.001f?(ix*oz-iz*ox)/norm:0;
                if(Math.Abs(turn)<.3f)return fallback;
                int desired=0;
                for(int lane=1;lane<count;lane++)
                    if(turn<0?city.roads.LaneOffset(route[segment],route[segment+1],lane)>city.roads.LaneOffset(route[segment],route[segment+1],desired)
                        :city.roads.LaneOffset(route[segment],route[segment+1],lane)<city.roads.LaneOffset(route[segment],route[segment+1],desired))desired=lane;
                return desired;
            }
            return fallback;
        }
        float LaneHeadway(TrafficTrip t,int lane)
        {
            float best=4,offset=-Behind(t);
            for(int s=t.segment;s<t.route.Count-1 && offset<4;s++)
            {
                foreach(var other in State.trips)
                    if(other!=t && !other.walking && other.status!=TripStatus.Visiting && other.Current==t.route[s] && other.Next==t.route[s+1] && OccupiesLane(other,lane))
                    {float distance=offset+Behind(other);if(distance>=-.001f)best=Math.Min(best,distance);}
                offset+=Units(t.route[s],t.route[s+1]);
                if(city.roads.Neighbors(t.route[s+1]).Count>2 || city.roads.LaneCount(t.route[s],t.route[s+1])<2)break;
            }
            return best;
        }
        bool LaneSpace(TrafficTrip t,int lane)
        {
            float speed=480f*city.roads.SpeedFactor(t.Current,t.Next)/city.society.settings.secondsPerDay;
            float clearance=Gap+.2f+speed*.2f;
            int first=t.segment;
            float back=-Behind(t);while(first>0 && -back<clearance){first--;back-=Units(t.route[first],t.route[first+1]);}
            float offset=back;
            for(int s=first;s<t.route.Count-1 && offset<clearance;s++)
            {
                foreach(var other in State.trips)
                {
                    if(other==t || other.walking || other.status==TripStatus.Visiting || !OccupiesLane(other,lane))continue;
                    if(other.Current==t.route[s] && other.Next==t.route[s+1] && Math.Abs(offset+Behind(other))<clearance)return false;
                    if(s==t.segment && other.Next==t.Current && Behind(t)+Ahead(other)<clearance)return false;
                }
                offset+=Units(t.route[s],t.route[s+1]);
            }
            return true;
        }
        void TryChooseLane(TrafficTrip t)
        {
            if(t.walking || t.laneChangeRemaining>0 || t.laneChangeCooldown>0 || city.roads.LaneCount(t.Current,t.Next)<2)return;
            float speed=480f*city.roads.SpeedFactor(t.Current,t.Next)/city.society.settings.secondsPerDay;
            // Never modify a reserved movement or sweep through a crosswalk.
            for(int i=t.segment;i>=0;i--)
            {float d=-DistanceTo(t,i);if(d>4)break;if(IsJunction(t.route[i]) && d<CityJunctions.CrossingRadius(city.roads,t.route[i])+.2f)return;}
            for(int i=t.segment+1;i<t.route.Count;i++)
            {float d=DistanceTo(t,i);if(d>4+speed*LaneChangeSeconds)break;if(IsJunction(t.route[i]) && d<CityJunctions.CrossingRadius(city.roads,t.route[i])+speed*LaneChangeSeconds+.2f)return;}
            int desired=TurnLane(t.route,t.segment,-1),target=-1,count=city.roads.LaneCount(t.Current,t.Next);
            if(desired==t.lane)return;
            bool forTurn=desired>=0;
            if(forTurn){target=t.lane+Math.Sign(desired-t.lane);if(!LaneSpace(t,target))return;}
            else
            {
                float headway=LaneHeadway(t,t.lane),best=headway+1;if(headway>Gap+1)return;
                for(int lane=Math.Max(0,t.lane-1);lane<=Math.Min(count-1,t.lane+1);lane++)
                    if(lane!=t.lane && LaneHeadway(t,lane)>=best && LaneSpace(t,lane)){best=LaneHeadway(t,lane);target=lane;}
                if(target<0)return;
            }
            t.laneChangeFrom=t.lane;t.lane=target;t.laneChangeRemaining=LaneChangeSeconds;t.laneChangeCooldown=LaneChangeRest;
            city.Trace("trip.lane_changed",forTurn?"提前选择转弯车道":"前方受阻，安全换道",t,trip:t.id);
        }
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
                    if(other.Current==route[0] && (route.Count==1 || other.Next==route[1]) && OccupiesLane(other,lane) && Behind(other)<Gap){clear=false;break;}
                    // Spawning at a node remains conservative with an incoming car.
                    if(other.Next==route[0] && Ahead(other)<Gap){clear=false;break;}
                }
                if(clear)return lane;
            }
            return -1;
        }
        void InitializeLane(TrafficTrip trip)
        {trip.lane=trip.walking?0:Math.Max(0,EntryLane(trip.route,trip));trip.previousLane=0;trip.laneChangeFrom=trip.lane;trip.laneChangeRemaining=trip.laneChangeCooldown=0;ClearJunctionQueue(trip);}
        void AddTrip(TrafficTrip trip){InitializeLane(trip);State.trips.Add(trip);}
    }
}
