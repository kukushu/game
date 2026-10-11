using System;
using System.Collections.Generic;
using System.Linq;
namespace HarborCity
{
    public enum JunctionControl { Automatic, Uncontrolled, Signals }
    public enum SignalAspect { Red, Amber, Green }
    public sealed partial class CityRoads
    {
        // Default placement and timings are provisional vanilla approximations.
        public const float SignalGreenSeconds=3.2f,SignalAmberSeconds=.4f,SignalClearSeconds=.4f;
        [NonSerialized] Dictionary<int,List<List<int>>> signalGroups;
        [NonSerialized] int signalRevision=-1;
        public bool HasSignals(int node)
        {
            var n=Node(node);if(n==null || Neighbors(node).Count<3 || n.control==JunctionControl.Uncontrolled)return false;
            return n.control==JunctionControl.Signals || Neighbors(node).Exists(from=>!CityRoadTypes.LimitedAccess(Kind(from,node)) && EdgeWidth(from,node)>Width);
        }
        List<List<int>> SignalGroups(int node)
        {
            if(signalRevision!=revision){signalRevision=revision;signalGroups=new Dictionary<int,List<List<int>>>();}
            if(signalGroups.TryGetValue(node,out var result))return result;
            var n=Node(node);var remaining=Neighbors(node).Where(from=>CanTravel(from,node)).OrderBy(from=>Math.Atan2(Node(from).z-n.z,Node(from).x-n.x)).ThenBy(from=>from).ToList();
            result=new List<List<int>>();
            while(remaining.Count>0)
            {
                int first=remaining[0];remaining.RemoveAt(0);var group=new List<int>{first};var a=Node(first);float ax=a.x-n.x,az=a.z-n.z,al=Length(a,n);
                int opposite=remaining.OrderBy(from=>((Node(from).x-n.x)*ax+(Node(from).z-n.z)*az)/(Length(Node(from),n)*al)).DefaultIfEmpty(-1).First();
                if(opposite>=0 && ((Node(opposite).x-n.x)*ax+(Node(opposite).z-n.z)*az)/(Length(Node(opposite),n)*al)<-.85f){group.Add(opposite);remaining.Remove(opposite);}
                result.Add(group);
            }
            signalGroups[node]=result;return result;
        }
        public SignalAspect Signal(int node,int from,float clock)
        {
            if(!HasSignals(node))return SignalAspect.Green;
            var groups=SignalGroups(node);if(groups.Count==0)return SignalAspect.Red;
            double span=SignalGreenSeconds+SignalAmberSeconds+SignalClearSeconds;
            double phase=(clock+node%7*.13)% (span*groups.Count);int group=(int)(phase/span);double part=phase-group*span;
            if(!groups[group].Contains(from) || part>=SignalGreenSeconds+SignalAmberSeconds)return SignalAspect.Red;
            return part<SignalGreenSeconds?SignalAspect.Green:SignalAspect.Amber;
        }
        // Walking is bidirectional even on a one-way road. Map its approach to
        // the parallel vehicle phase rather than requiring a legal incoming lane.
        // This is a provisional node-level crossing, not CS1's sidewalk lane graph.
        public SignalAspect PedestrianSignal(int node,int from,float clock)
        {
            if(!HasSignals(node))return SignalAspect.Green;
            var n=Node(node);var a=Node(from);if(n==null || a==null || !Linked(node,from))return SignalAspect.Red;
            var approaches=SignalGroups(node).SelectMany(g=>g).ToList();
            if(approaches.Count==0)return SignalAspect.Green;
            if(approaches.Contains(from))return Signal(node,from,clock);
            int parallel=approaches.OrderByDescending(p=>Math.Abs((Node(p).x-n.x)*(a.x-n.x)+(Node(p).z-n.z)*(a.z-n.z))/(Length(Node(p),n)*Length(a,n))).ThenBy(p=>p).First();
            return Signal(node,parallel,clock);
        }
        public int PickJunction(float x,float z,float? level=null)
        {
            var p=new RoadNode{x=x,z=z};var n=nodes.Where(n=>Neighbors(n.id).Count>=3 && (!level.HasValue || Math.Abs(n.y-level.Value)<1.6f) && Length(n,p)<4.5f).OrderBy(n=>Length(n,p)).FirstOrDefault();return n?.id??-1;
        }
        public int PickJunctionApproach(int node,float x,float z)
        {var p=new RoadNode{x=x,z=z};return Neighbors(node).Where(from=>CanTravel(from,node)).OrderBy(from=>Distance(p,Node(node),Node(from))).ThenBy(from=>from).DefaultIfEmpty(-1).First();}
        public bool SetJunctionControl(int node,JunctionControl control,out string reason)
        {
            if(!Enum.IsDefined(typeof(JunctionControl),control) || Node(node)==null || Neighbors(node).Count<3){reason="请选择三岔或十字路口。";return false;}
            if(Node(node).control==control){reason="路口已经使用该设置。";return false;}
            Node(node).control=control;Changed();reason=control==JunctionControl.Automatic?"路口已恢复默认信号设置。":control==JunctionControl.Signals?"路口信号灯已开启。":"路口信号灯已关闭。";return true;
        }
        public bool ToggleStop(int node,int from,out string reason)
        {
            var n=Node(node);if(n==null || Neighbors(node).Count<3 || !CanTravel(from,node)){reason="请选择路口的一条驶入道路。";return false;}
            n.control=JunctionControl.Uncontrolled;
            if(!n.stopFrom.Remove(from)){n.stopFrom.Add(from);reason="该入口已设停车牌；车辆停车后让行。";}else reason="该入口停车牌已移除。";
            Changed();return true;
        }
        void SplitJunctionApproach(int a,int b,int middle)
        {
            foreach(var pair in new[]{(node:a,old:b),(node:b,old:a)})
            {var n=Node(pair.node);if(n.stopFrom.Remove(pair.old))n.stopFrom.Add(middle);}
        }
    }
    public sealed partial class CityTraffic
    {
        const float StopSeconds=.75f;
        void ClearJunctionQueue(TrafficTrip t){t.junctionWaitNode=-1;t.junctionQueuedAt=0;t.junctionStopTime=0;t.junctionWaitReason=null;}
        bool ControlAllows(TrafficTrip t,JunctionMovement movement)
        {
            int node=movement.node;var roads=city.roads;
            if(t.junctionWaitNode!=node){ClearJunctionQueue(t);t.junctionWaitNode=node;t.junctionQueuedAt=State.clock;}
            if(t.walking)
            {
                var crossings=CityJunctions.PedestrianCrossings(roads,node,movement.entry,movement.exit);
                if(crossings!=null && crossings.Count==0)return true;
                if(roads.PedestrianSignal(node,movement.entry,State.clock)!=SignalAspect.Green)
                {t.junctionWaitReason="等待行人通行信号";return false;}
                return true;
            }
            if(roads.HasSignals(node))
            {
                if(roads.Signal(node,movement.entry,State.clock)!=SignalAspect.Green){t.junctionWaitReason="等待信号灯";return false;}
            }
            else if(roads.Node(node).stopFrom.Contains(movement.entry))
            {
                if(t.junctionStopTime<StopSeconds){t.junctionWaitReason="停车牌：停车";return false;}
            }
            bool stopped=!roads.HasSignals(node) && roads.Node(node).stopFrom.Contains(movement.entry);
            if(!HighwayGapAllows(t,movement))return false;
            // Protect a walker actually waiting at the crossing, not somebody
            // far away or at a red phase. Physical owners are checked by Reserve.
            int junctionIndex=RouteNodeIndex(t,node);
            if(junctionIndex>=0 && DistanceTo(t,junctionIndex)>=CityJunctions.CrossingRadius(roads,node)-.02f && PendingWalker(node,movement))
            {t.junctionWaitReason="让行等待过街的行人";return false;}
            foreach(var other in State.trips)
            {
                if(other==t || other.walking || other.status!=TripStatus.Driving || other.Next!=node || Ahead(other)>.7f)continue;
                var next=Movement(other,node);
                if(!CityJunctions.Conflicts(roads,node,movement.entry,movement.exit,movement.entryLane,movement.exitLane,next.entry,next.exit,next.entryLane,next.exitLane))continue;
                if(roads.HasSignals(node) && roads.Signal(node,next.entry,State.clock)!=SignalAspect.Green)continue;
                bool otherStop=!roads.HasSignals(node) && roads.Node(node).stopFrom.Contains(next.entry);
                // A blocked exit cannot claim arrival priority: another movement
                // may have to clear that queue first. Physical owners still rule.
                if(!ExitClear(other,node) || otherStop && other.junctionStopTime<StopSeconds)continue;
                if(stopped && !otherStop){t.junctionWaitReason="停车牌：让行";return false;}
                // Unsigned ramps do not claim arrival-order priority over the highway mainline.
                // Vehicles already inside the junction remain protected by its physical reservation.
                if(!roads.HasSignals(node) && !stopped && roads.HighwayThrough(movement.entry,node,movement.exit)
                    && roads.HighwayMergeLane(next.entry,node,next.exit)>=0)continue;
                if(stopped==otherStop && other.junctionWaitNode==node && (other.junctionQueuedAt<t.junctionQueuedAt || other.junctionQueuedAt==t.junctionQueuedAt && other.id<t.id))
                {t.junctionWaitReason="等待先到的冲突车辆";return false;}
            }
            return true;
        }
        void AdvanceStopWait(TrafficTrip t)
        {
            if(!t.walking && t.status==TripStatus.Driving && t.junctionWaitNode==t.Next && !city.roads.HasSignals(t.Next) && city.roads.Node(t.Next)?.stopFrom.Contains(t.Current)==true && Ahead(t)<=.361f)
                t.junctionStopTime=Math.Min(StopSeconds,t.junctionStopTime+Step);
        }
    }
}
