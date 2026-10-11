using System;
using System.Collections.Generic;
namespace HarborCity
{
    // Provisional right-hand traffic geometry, not a claim about CS1 internals.
    public static class CityJunctions
    {
        const float Clearance=.46f;
        public static bool Conflicts(CityRoads roads,int node,int entryA,int exitA,int entryB,int exitB)
            =>Conflicts(roads,node,entryA,exitA,0,0,entryB,exitB,0,0);
        public static bool Conflicts(CityRoads roads,int node,int entryA,int exitA,int inLaneA,int outLaneA,int entryB,int exitB,int inLaneB,int outLaneB)
        {
            if(entryA<0 || exitA<0 || entryB<0 || exitB<0)return true;
            if(entryA==entryB && inLaneA==inLaneB || exitA==exitB && outLaneA==outLaneB)return true;
            var a=Path(roads,node,entryA,exitA,inLaneA,outLaneA);var b=Path(roads,node,entryB,exitB,inLaneB,outLaneB);
            if(a==null || b==null)return true;
            for(int i=1;i<a.Count;i++)for(int j=1;j<b.Count;j++)
                if(SegmentDistance(a[i-1],a[i],b[j-1],b[j])<Clearance)return true;
            return false;
        }
        static List<RoadNode> Path(CityRoads r,int node,int entry,int exit,int inLane,int outLane)
        {
            var n=r.Node(node);var a=r.Node(entry);var b=r.Node(exit);if(n==null || a==null || b==null)return null;
            float inLength=CityRoads.Length(a,n),outLength=CityRoads.Length(n,b);if(inLength<.01f || outLength<.01f)return null;
            float ix=(n.x-a.x)/inLength,iz=(n.z-a.z)/inLength,ox=(b.x-n.x)/outLength,oz=(b.z-n.z)/outLength;
            float radius=Math.Min(1.2f,Math.Min(inLength,outLength)*.45f);
            float incomingOffset=r.LaneOffset(entry,node,inLane),outgoingOffset=r.LaneOffset(node,exit,outLane);
            // The center uses TrafficPoint's averaged right-side offset; the
            // approach/departure boundaries remain provisional geometry.
            var center=new RoadNode{x=n.x+(iz*incomingOffset+oz*outgoingOffset)/2,z=n.z-(ix*incomingOffset+ox*outgoingOffset)/2};
            return new List<RoadNode>{
                new RoadNode{x=n.x-ix*radius+iz*incomingOffset,z=n.z-iz*radius-ix*incomingOffset},center,
                new RoadNode{x=n.x+ox*radius+oz*outgoingOffset,z=n.z+oz*radius-ox*outgoingOffset}};
        }
        static float Cross(RoadNode a,RoadNode b,RoadNode c)=>(b.x-a.x)*(c.z-a.z)-(b.z-a.z)*(c.x-a.x);
        static float SegmentDistance(RoadNode a,RoadNode b,RoadNode c,RoadNode d)
        {
            float ac=Cross(a,b,c),ad=Cross(a,b,d),ca=Cross(c,d,a),cb=Cross(c,d,b);
            if(ac*ad<0 && ca*cb<0)return 0;
            return Math.Min(Math.Min(CityRoads.Distance(a,c,d),CityRoads.Distance(b,c,d)),Math.Min(CityRoads.Distance(c,a,b),CityRoads.Distance(d,a,b)));
        }
    }
    public sealed partial class CityTraffic
    {
        sealed class JunctionMovement {public int trip,node,entry,exit,entryLane,exitLane;}
        readonly Dictionary<int,List<JunctionMovement>> junctions=new Dictionary<int,List<JunctionMovement>>();
        int junctionRevision=-1;
        JunctionMovement Movement(TrafficTrip t,int node)
        {
            int index=t.Current==node?t.segment:t.Next==node?t.segment+1:-1;
            return new JunctionMovement{trip=t.id,node=node,entry=index>0?t.route[index-1]:-1,exit=index>=0 && index+1<t.route.Count?t.route[index+1]:-1,entryLane=city.roads.TripLane(t,index-1),exitLane=city.roads.TripLane(t,index)};
        }
        void RestoreJunctions()
        {
            junctions.Clear();junctionRevision=city.roads.revision;
            // Existing departures physically own their movement until clear. Do not
            // resolve old overlaps by deleting cars or moving them during load.
            foreach(var t in State.trips)if(!t.walking && t.status==TripStatus.Driving && Behind(t)<.4f && IsJunction(t.Current))
            {if(!junctions.TryGetValue(t.Current,out var owners))junctions[t.Current]=owners=new List<JunctionMovement>();owners.Add(Movement(t,t.Current));}
            foreach(var t in State.trips)if(!t.walking && t.status==TripStatus.Driving && Ahead(t)<=.35f)Reserve(t.Next,t.id);
        }
        void ReleaseJunctions()
        {
            if(junctionRevision!=city.roads.revision){RestoreJunctions();return;}
            foreach(var pair in junctions)
                pair.Value.RemoveAll(m=>{
                    var t=State.trips.Find(p=>p.id==m.trip);
                    return t==null || t.status!=TripStatus.Driving || !(t.Next==pair.Key && Ahead(t)<=.36f || t.Current==pair.Key && Behind(t)<.4f);
                });
        }
        bool ExitClear(TrafficTrip t,int node)
        {
            int index=t.Current==node?t.segment:t.segment+1;float offset=0;
            for(int i=index;i<t.route.Count-1 && offset<Gap+.4f;i++)
            {
                foreach(var other in State.trips)
                    if(other!=t && !other.walking && other.status!=TripStatus.Visiting && other.Current==t.route[i] && other.Next==t.route[i+1] && other.lane==city.roads.TripLane(t,i) && offset+Behind(other)<Gap+.4f)return false;
                offset+=Units(t.route[i],t.route[i+1]);
            }
            return true;
        }
        bool Reserve(int node,int id)
        {
            if(!IsJunction(node))return true;
            var t=State.trips.Find(p=>p.id==id);if(t==null)return false;
            if(!junctions.TryGetValue(node,out var owners))junctions[node]=owners=new List<JunctionMovement>();
            if(owners.Exists(m=>m.trip==id))return true;
            var movement=Movement(t,node);
            t.junctionWaitReason=null;
            if(!ControlAllows(t,movement))return false;
            if(!ExitClear(t,node)){t.junctionWaitReason="出口队列没有空间";return false;}
            foreach(var owner in owners)
                if(CityJunctions.Conflicts(city.roads,node,movement.entry,movement.exit,movement.entryLane,movement.exitLane,owner.entry,owner.exit,owner.entryLane,owner.exitLane)){t.junctionWaitReason="等待路口内冲突车辆驶离";return false;}
            owners.Add(movement);return true;
        }
    }
}
