using System;
using System.Collections.Generic;
namespace HarborCity
{
    // Provisional right-hand traffic geometry, not a claim about CS1 internals.
    public static class CityJunctions
    {
        const float Clearance=.46f;
        // Follow the right sidewalk around the node. Only arms between the
        // entry and exit on this angular arc are crossed. U-turns remain conservative.
        public static List<int> PedestrianCrossings(CityRoads roads,int node,int entry,int exit)
        {
            var n=roads.Node(node);var a=roads.Node(entry);var b=roads.Node(exit);
            if(n==null || a==null || b==null || entry==exit || !roads.Linked(node,entry) || !roads.Linked(node,exit))return null;
            if(Corner(roads,node,entry,exit)==null)return null;
            double start=Math.Atan2(a.z-n.z,a.x-n.x),end=PositiveAngle(Math.Atan2(b.z-n.z,b.x-n.x)-start);
            var result=new List<int>();
            foreach(int arm in roads.Neighbors(node))
            {
                if(arm==entry || arm==exit)continue;
                var p=roads.Node(arm);double delta=PositiveAngle(Math.Atan2(p.z-n.z,p.x-n.x)-start);
                if(delta>.001 && delta<end-.001)result.Add(arm);
            }
            return result;
        }
        static double PositiveAngle(double value)=>value<0?value+Math.PI*2:value;
        static RoadNode Corner(CityRoads roads,int node,int entry,int exit)
        {
            var n=roads.Node(node);var a=roads.Node(entry);var b=roads.Node(exit);
            if(n==null || a==null || b==null)return null;
            float al=CityRoads.Length(a,n),bl=CityRoads.Length(n,b);if(al<.01f || bl<.01f)return null;
            float ix=(n.x-a.x)/al,iz=(n.z-a.z)/al,ox=(b.x-n.x)/bl,oz=(b.z-n.z)/bl;
            float incoming=roads.EdgeWidth(entry,node)/2+.2f,outgoing=roads.EdgeWidth(node,exit)/2+.2f;
            float px=iz*incoming,pz=-ix*incoming,qx=oz*outgoing,qz=-ox*outgoing,det=ix*oz-iz*ox;
            if(Math.Abs(det)<.0001f)
            {
                if(ix*ox+iz*oz<0)return null;
                float offset=Math.Max(incoming,outgoing);
                return new RoadNode{x=n.x+iz*offset,z=n.z-ix*offset,y=n.y};
            }
            float t=((qx-px)*oz-(qz-pz)*ox)/det,dx=px+ix*t,dz=pz+iz*t;
            if(dx*dx+dz*dz>16*Math.Max(incoming*incoming,outgoing*outgoing))return null;
            return new RoadNode{x=n.x+dx,z=n.z+dz,y=n.y};
        }
        public static RoadNode SidewalkCorner(CityRoads roads,int node,int entry,int exit)
            =>Corner(roads,node,entry,exit);
        public static float CrossingRadius(CityRoads roads,int node)
        {
            float width=CityRoads.Width;
            foreach(int arm in roads.Neighbors(node))width=Math.Max(width,roads.EdgeWidth(node,arm));
            // Covers sidewalk offset, corner interpolation and vehicle body;
            // world units -> existing road units. This is not an original-game parameter.
            float radius=width+1;
            foreach(int entry in roads.Neighbors(node))foreach(int exit in roads.Neighbors(node))if(entry!=exit)
            {
                var p=Corner(roads,node,entry,exit);
                if(p!=null)radius=Math.Max(radius,CityRoads.Length(p,roads.Node(node))+.65f);
            }
            return radius/3;
        }
        static RoadNode SidewalkPoint(CityRoads roads,List<int> route,int index)
        {
            var n=roads.Node(route[index]);
            int before=index>0?index-1:index,after=index+1<route.Count?index+1:index;
            if(before<index && after>index)
            {
                var corner=SidewalkCorner(roads,n.id,route[before],route[after]);if(corner!=null)return corner;
            }
            var a=roads.Node(route[before]);var b=roads.Node(route[after]);
            float dx=0,dz=0;int count=0;
            if(before<index){float length=CityRoads.Length(a,n);if(length>.001f){float offset=roads.EdgeWidth(a.id,n.id)/2+.2f;dx+=(n.z-a.z)/length*offset;dz-=(n.x-a.x)/length*offset;count++;}}
            if(after>index){float length=CityRoads.Length(n,b);if(length>.001f){float offset=roads.EdgeWidth(n.id,b.id)/2+.2f;dx+=(b.z-n.z)/length*offset;dz-=(b.x-n.x)/length*offset;count++;}}
            return new RoadNode{x=n.x+(count>0?dx/count:0),z=n.z+(count>0?dz/count:0),y=n.y};
        }
        public static RoadNode WalkingPosition(CityRoads roads,TrafficTrip trip,int segment,float fraction)
        {
            var a=SidewalkPoint(roads,trip.route,segment);var b=SidewalkPoint(roads,trip.route,Math.Min(segment+1,trip.route.Count-1));
            return CityRoads.Lerp(a,b,fraction);
        }
        public static float WalkingLength(CityRoads roads,TrafficTrip trip,int segment)
            =>Math.Max(.001f,CityRoads.SpatialLength(SidewalkPoint(roads,trip.route,segment),SidewalkPoint(roads,trip.route,Math.Min(segment+1,trip.route.Count-1))));
        public static List<RoadNode> Crosswalk(CityRoads roads,int node,int arm)
        {
            var n=roads.Node(node);var a=roads.Node(arm);if(n==null || a==null || roads.Neighbors(node).Count<3)return null;
            float length=CityRoads.Length(n,a);if(length<.01f)return null;
            if(roads.Neighbors(node).Count==4)
            {
                double direction=Math.Atan2(a.z-n.z,a.x-n.x);int previous=-1,next=-1;double before=Math.PI*2,after=Math.PI*2;
                foreach(int other in roads.Neighbors(node))if(other!=arm)
                {
                    var p=roads.Node(other);double delta=PositiveAngle(Math.Atan2(p.z-n.z,p.x-n.x)-direction);
                    if(delta<after){after=delta;next=other;}if(Math.PI*2-delta<before){before=Math.PI*2-delta;previous=other;}
                }
                var left=Corner(roads,node,previous,arm);var right=Corner(roads,node,arm,next);
                if(left!=null && right!=null)return new List<RoadNode>{left,right};
            }
            float dx=(a.x-n.x)/length,dz=(a.z-n.z)/length,along=0,across=roads.EdgeWidth(node,arm)/2+.2f;
            foreach(int other in roads.Neighbors(node))
            {
                if(other==arm)continue;
                var b=roads.Node(other);float bl=CityRoads.Length(n,b);if(bl<.01f)return null;
                float dot=((b.x-n.x)*dx+(b.z-n.z)*dz)/bl,sine=(float)Math.Sqrt(Math.Max(0,1-dot*dot));
                if(sine<.01f)continue;
                along=Math.Max(along,(roads.EdgeWidth(node,other)/2+.2f+Math.Abs(dot)*across)/sine);
            }
            if(along<=0 || along>CityRoadTypes.Width(RoadKind.FourLane)*2)return null;
            return new List<RoadNode>{new RoadNode{x=n.x+dx*along+dz*across,z=n.z+dz*along-dx*across,y=n.y},new RoadNode{x=n.x+dx*along-dz*across,z=n.z+dz*along+dx*across,y=n.y}};
        }
        public static bool PedestrianConflicts(CityRoads roads,int node,int walkEntry,int walkExit,int carEntry,int carExit)
        {
            if(walkEntry<0 || walkExit<0)return true;
            var crossings=PedestrianCrossings(roads,node,walkEntry,walkExit);
            if(crossings!=null && crossings.Count==0)return false;
            if(carEntry<0 || carExit<0)return true;
            return crossings==null || crossings.Contains(carEntry) || crossings.Contains(carExit);
        }
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
            var trip=new TrafficTrip{route=new List<int>{entry,node,exit},segment=1,lane=outLane,previousLane=inLane};
            var path=new List<RoadNode>();
            // Sample the same lane corner used by vehicles and route highlighting.
            for(int i=0;i<=8;i++)path.Add(CityVehicleGeometry.Position(r,trip,0,.6f+i*.05f));
            for(int i=1;i<=8;i++)path.Add(CityVehicleGeometry.Position(r,trip,1,i*.05f));
            return path;
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
        sealed class JunctionMovement {public int trip,node,entry,exit,entryLane,exitLane;public bool walking;}
        readonly Dictionary<int,List<JunctionMovement>> junctions=new Dictionary<int,List<JunctionMovement>>();
        int junctionRevision=-1;
        JunctionMovement Movement(TrafficTrip t,int node)
        {
            int index=t.route.IndexOf(node);
            return new JunctionMovement{trip=t.id,node=node,entry=index>0?t.route[index-1]:-1,exit=index>=0 && index+1<t.route.Count?t.route[index+1]:-1,entryLane=city.roads.TripLane(t,index-1),exitLane=city.roads.TripLane(t,index),walking=t.walking};
        }
        void RestoreJunctions()
        {
            junctions.Clear();junctionRevision=city.roads.revision;
            // Existing departures physically own their movement until clear. Do not
            // resolve old overlaps by deleting cars or moving them during load.
            foreach(var t in State.trips)if(t.status==TripStatus.Driving && Behind(t)<.4f && IsJunction(t.Current))
            {if(!junctions.TryGetValue(t.Current,out var owners))junctions[t.Current]=owners=new List<JunctionMovement>();owners.Add(Movement(t,t.Current));}
            // Rebuild walking reservations throughout the physical crossing
            // region, including short samples before and after the center.
            foreach(var t in State.trips)if(t.walking && t.status==TripStatus.Driving)
                for(int index=1;index<t.route.Count-1;index++)
                {
                    int node=t.route[index];if(!IsJunction(node) || Math.Abs(DistanceTo(t,index))>=CityJunctions.CrossingRadius(city.roads,node))continue;
                    if(!junctions.TryGetValue(node,out var owners))junctions[node]=owners=new List<JunctionMovement>();
                    if(!owners.Exists(m=>m.trip==t.id))owners.Add(Movement(t,node));
                }
            foreach(var t in State.trips)if(t.status==TripStatus.Driving && Ahead(t)<=.35f)Reserve(t.Next,t.id);
        }
        float DistanceTo(TrafficTrip t,int index)
        {
            if(index>t.segment){float ahead=Ahead(t);for(int i=t.segment+1;i<index;i++)ahead+=Units(t.route[i],t.route[i+1]);return ahead;}
            float behind=Behind(t);for(int i=index;i<t.segment;i++)behind+=Units(t.route[i],t.route[i+1]);return -behind;
        }
        bool StillOccupies(TrafficTrip t,int node)
        {
            if(!t.walking)return t.Next==node && Ahead(t)<=.36f || t.Current==node && Behind(t)<.4f;
            int index=t.route.IndexOf(node);return index>=0 && Math.Abs(DistanceTo(t,index))<CityJunctions.CrossingRadius(city.roads,node)+.011f;
        }
        void ReleaseJunctions()
        {
            if(junctionRevision!=city.roads.revision){RestoreJunctions();return;}
            foreach(var pair in junctions)
                pair.Value.RemoveAll(m=>{
                    var t=State.trips.Find(p=>p.id==m.trip);
                    return t==null || t.status!=TripStatus.Driving || !StillOccupies(t,pair.Key);
                });
        }
        bool ExitClear(TrafficTrip t,int node)
        {
            int index=t.Current==node?t.segment:t.segment+1;float offset=0;
            for(int i=index;i<t.route.Count-1 && offset<Gap+.4f;i++)
            {
                foreach(var other in State.trips)
                    if(other!=t && !other.walking && other.status!=TripStatus.Visiting && other.Current==t.route[i] && other.Next==t.route[i+1] && OccupiesLane(other,city.roads.TripLane(t,i)) && offset+Behind(other)<Gap+.4f)return false;
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
            // Reaching a destination's access node does not imply crossing it.
            if(t.walking && movement.exit<0)return true;
            t.junctionWaitReason=null;
            if(!ControlAllows(t,movement))return false;
            if(t.walking && !WalkingEntryClear(t,movement))return false;
            if(!t.walking && !ExitClear(t,node)){t.junctionWaitReason="出口队列没有空间";return false;}
            foreach(var owner in owners)
                if(movement.walking || owner.walking)
                {
                    if(movement.walking!=owner.walking && MixedConflicts(movement,owner)){t.junctionWaitReason=t.walking?"等待路口内车辆驶离":"等待过街行人走完";return false;}
                }
                else
                if(CityJunctions.Conflicts(city.roads,node,movement.entry,movement.exit,movement.entryLane,movement.exitLane,owner.entry,owner.exit,owner.entryLane,owner.exitLane)){t.junctionWaitReason="等待路口内冲突车辆驶离";return false;}
            owners.Add(movement);return true;
        }
        bool MixedConflicts(JunctionMovement a,JunctionMovement b)
        {
            var walk=a.walking?a:b;var car=a.walking?b:a;
            return CityJunctions.PedestrianConflicts(city.roads,a.node,walk.entry,walk.exit,car.entry,car.exit);
        }
        bool WalkingEntryClear(TrafficTrip t,JunctionMovement movement)
        {
            float radius=CityJunctions.CrossingRadius(city.roads,movement.node);
            foreach(var car in State.trips)
            {
                if(car.walking || car.status!=TripStatus.Driving)continue;
                int index=car.route.IndexOf(movement.node);if(index<0)continue;
                float distance=DistanceTo(car,index);
                // Cars already inside must clear instead of being trapped by
                // a newly arriving pedestrian. Cars waiting at the edge do not.
                if(distance<radius-.02f && distance>-radius && MixedConflicts(movement,Movement(car,movement.node)))
                {t.junctionWaitReason="等待横道内车辆驶离";return false;}
            }
            return true;
        }
        bool PendingWalker(int node,JunctionMovement car)
        {
            float radius=CityJunctions.CrossingRadius(city.roads,node);
            foreach(var walker in State.trips)
            {
                if(!walker.walking || walker.status!=TripStatus.Driving)continue;
                int index=walker.route.IndexOf(node);if(index<1 || index+1>=walker.route.Count)continue;
                float distance=DistanceTo(walker,index);if(distance<0 || distance>radius+.061f)continue;
                var movement=Movement(walker,node);
                if(city.roads.PedestrianSignal(node,movement.entry,State.clock)==SignalAspect.Green && MixedConflicts(car,movement))return true;
            }
            return false;
        }
        float LimitCrossingAdvance(TrafficTrip t,float proposed,float units)
        {
            // Look across short graph samples; a crossing is not necessarily Next.
            float maxRadius=((CityRoadTypes.Width(RoadKind.FourLane)/2+.2f)*4+.65f)/3;
            for(int index=t.segment+1;index<t.route.Count;index++)
            {
                float distance=DistanceTo(t,index);if(distance>maxRadius+(proposed-t.progress)*units+.061f)break;
                int node=t.route[index];if(!IsJunction(node))continue;
                float radius=CityJunctions.CrossingRadius(city.roads,node);
                if(distance-(proposed-t.progress)*units>radius+.01f)continue;
                var movement=Movement(t,node);bool allowed;
                if(t.walking)allowed=movement.exit<0 || Reserve(node,t.id);
                else
                {
                    // The entrant is checked at the outer boundary. Once inside,
                    // continue clearing; walkers will wait for its actual position.
                    if(distance<radius-.02f)continue;
                    allowed=!(junctions.TryGetValue(node,out var owners) && owners.Exists(m=>m.walking && MixedConflicts(movement,m))) && !PendingWalker(node,movement);
                    if(!allowed)t.junctionWaitReason="让行横道上的行人";
                }
                if(!allowed)proposed=Math.Min(proposed,t.progress+Math.Max(0,distance-radius-.01f)/units);
            }
            return proposed;
        }
    }
}
