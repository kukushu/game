using System;
using System.Linq;

namespace HarborCity
{
    public sealed partial class CityRoads
    {
        public bool HighwayThrough(int entry,int node,int exit)
        {
            if(Kind(entry,node)!=RoadKind.Highway || Kind(node,exit)!=RoadKind.Highway || !CanTravel(entry,node) || !CanTravel(node,exit))return false;
            var a=Node(entry);var n=Node(node);var b=Node(exit);
            float length=Length(a,n)*Length(n,b);
            return length>.001f && ((n.x-a.x)*(b.x-n.x)+(n.z-a.z)*(b.z-n.z))/length>.85f;
        }
        public int HighwayMergeLane(int entry,int node,int exit)
        {
            if(Kind(entry,node)!=RoadKind.Ramp || Kind(node,exit)!=RoadKind.Highway)return -1;
            var a=Node(entry);var n=Node(node);var b=Node(exit);if(a==null || n==null || b==null)return -1;
            float turn=(n.x-a.x)*(b.z-n.z)-(n.z-a.z)*(b.x-n.x);int desired=0,count=LaneCount(node,exit);
            for(int lane=1;lane<count;lane++)
                if(turn<=0?LaneOffset(node,exit,lane)>LaneOffset(node,exit,desired):LaneOffset(node,exit,lane)<LaneOffset(node,exit,desired))desired=lane;
            return desired;
        }
    }
    public sealed partial class CityTraffic
    {
        // Provisional gap acceptance, derived from physical approaches/lanes. Explicit lights retain control.
        bool HighwayGapAllows(TrafficTrip t,JunctionMovement merge)
        {
            var roads=city.roads;
            if(roads.HasSignals(merge.node) || roads.HighwayMergeLane(merge.entry,merge.node,merge.exit)<0)return true;
            foreach(var other in State.trips)
            {
                if(other==t || other.walking || other.status!=TripStatus.Driving)continue;
                int index=other.route.FindIndex(other.segment,n=>n==merge.node);
                if(index<1 || index+1>=other.route.Count || !roads.HighwayThrough(other.route[index-1],merge.node,other.route[index+1]))continue;
                // An explicit mainline stop gives up default priority; otherwise both arms could wait for each other.
                if(roads.Node(merge.node).stopFrom.Contains(other.route[index-1]))continue;
                float distance=DistanceTo(other,index);
                float speed=480f*roads.SpeedFactor(other.Current,other.Next)/city.society.settings.secondsPerDay;
                if(distance<0 || distance>Gap+.4f+speed*.6f || !ExitClear(other,merge.node))continue;
                var main=Movement(other,merge.node);
                if(CityJunctions.Conflicts(roads,merge.node,merge.entry,merge.exit,merge.entryLane,merge.exitLane,main.entry,main.exit,main.entryLane,main.exitLane))
                {t.junctionWaitReason="匝道汇入：等待主线车流间隙";return false;}
            }
            return true;
        }
    }
}
