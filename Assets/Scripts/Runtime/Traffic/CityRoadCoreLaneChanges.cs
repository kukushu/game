using System;
using System.Collections.Generic;
namespace HarborCity.RoadCore
{
    internal static class LaneChanges
    {
        public static Point Position(Network network,long source,double distance,long target,double elapsed)
        {var lane=network.Get(source);Point p=lane.Geometry.Sample(distance);if(target==0)return p;var other=network.Get(target);double t=Math.Max(0,Math.Min(1,elapsed));t=t*t*(3-2*t);return Point.Lerp(p,other.Geometry.Sample(lane.Geometry.MapDistance(other.Geometry,distance)),t);}
        public static bool Route(Network network,Vehicle v,long first,List<long> result)
        {
            long previous=first;if(result!=null)result.Add(first);
            for(int i=v.cursor+2;i<v.Plan.segments.Count;i+=2)
            {
                long next=v.Plan.segments[i];if(!network.Contains(Network.ConnectionId(previous,next)))
                {var old=network.Get(next);next=Network.LaneId(old.RoadId,old.Reverse,network.Get(previous).LaneIndex);if(!network.Contains(next)||!network.Contains(Network.ConnectionId(previous,next)))return false;}
                if(result!=null){result.Add(Network.ConnectionId(previous,next));result.Add(next);}previous=next;
            }
            return previous==v.Plan.segments[v.Plan.segments.Count-1];
        }
        public static bool TryStart(Network network,Dictionary<long,List<LaneOccupant>> occupancy,Vehicle v,int targetIndex)
        {
            if(v.status!=VehicleStatus.Active||v.changeTarget!=0||targetIndex<0||targetIndex>31)return false;var lane=network.Get(v.Current);
            if(lane.IsConnection||Math.Abs(targetIndex-lane.LaneIndex)!=1||v.cursor+2>=v.Plan.segments.Count||v.distance<network.Clearance(lane)+1||v.distance+lane.Speed>lane.Geometry.Length-network.Clearance(lane))return false;
            long target=Network.LaneId(lane.RoadId,lane.Reverse,targetIndex);if(!network.Contains(target)||!Route(network,v,target,null))return false;
            var other=network.Get(target);double d=lane.Geometry.MapDistance(other.Geometry,v.distance);List<LaneOccupant> bucket;
            if(occupancy.TryGetValue(target,out bucket)){int slot=Slot(bucket,d);double gap=World.MinimumSpacing+lane.Speed*World.StepSeconds;if(slot>0&&bucket[slot-1].distance-d<gap||slot<bucket.Count&&d-bucket[slot].distance<gap)return false;}
            else occupancy.Add(target,bucket=new List<LaneOccupant>());
            v.changeTarget=target;v.changeElapsed=0;v.shadowOccupant.distance=d;bucket.Insert(Slot(bucket,d),v.shadowOccupant);return true;
        }
        public static int Choose(Network network,IEnumerable<Vehicle> vehicles,Dictionary<long,List<LaneOccupant>> occupancy,int tick)
        {
            int started=0;
            foreach(var v in vehicles)
            {
                if(v.status!=VehicleStatus.Active||v.changeTarget!=0||(tick+v.id)%5!=0)continue;var lane=network.Get(v.Current);if(lane.IsConnection||lane.ParallelLaneCount<2)continue;
                List<LaneOccupant> source=occupancy[lane.Id];int slot=Slot(source,v.distance);double headway=slot>0?source[slot-1].distance-v.distance:double.PositiveInfinity;if(headway>=4)continue;
                for(int direction=-1;direction<=1;direction+=2)
                {
                    int index=lane.LaneIndex+direction;if(index<0||index>=lane.ParallelLaneCount)continue;long target=Network.LaneId(lane.RoadId,lane.Reverse,index);var other=network.Get(target);double distance=lane.Geometry.MapDistance(other.Geometry,v.distance);List<LaneOccupant> bucket;
                    if(occupancy.TryGetValue(target,out bucket)){int at=Slot(bucket,distance);if(at>0&&bucket[at-1].distance-distance<headway+3)continue;}
                    if(TryStart(network,occupancy,v,index)){started++;break;}
                }
            }
            return started;
        }
        static int Slot(List<LaneOccupant> bucket,double position){int lo=0,hi=bucket.Count;while(lo<hi){int mid=(lo+hi)/2;if(bucket[mid].distance>position)lo=mid+1;else hi=mid;}return lo;}
    }
}
