using System;
using System.Collections.Generic;

namespace HarborCity.RoadCore
{
    public sealed partial class World
    {
        // This is derived World state, not another physical position owner.
        bool fullIndexRequired=true;
        internal long FullIndexBuilds,IncrementalIndexUpdates;
        readonly HashSet<Vehicle> pendingOccupants=new HashSet<Vehicle>();
        readonly HashSet<List<LaneOccupant>> dirtyOccupancyBuckets=new HashSet<List<LaneOccupant>>();

        internal void InvalidateOccupantState(){indexDirty=true;fullIndexRequired=true;}
        void PruneMotionOrder()
        {
            int retained=0;for(int i=0;i<motionOrder.Count;i++)if(motionOrder[i].status!=VehicleStatus.Delivered)motionOrder[retained++]=motionOrder[i];
            if(retained<motionOrder.Count)motionOrder.RemoveRange(retained,motionOrder.Count-retained);
        }
        void TrackMovedOccupant(Vehicle v)
        {
            if(v.indexedLane!=v.Current||v.sourceOccupant.distance!=v.distance||v.indexedShadow!=v.changeTarget)
                pendingOccupants.Add(v);
        }
        List<LaneOccupant> OccupantBucket(long lane)
        {
            if(!occupancy.TryGetValue(lane,out var bucket))occupancy.Add(lane,bucket=new List<LaneOccupant>());
            return bucket;
        }
        void RemoveIndexed(long lane,LaneOccupant occupant)
        {
            if(lane==0)return;
            if(!occupancy.TryGetValue(lane,out var bucket)||!bucket.Remove(occupant))
            {
                if(!fullIndexRequired)throw new InvalidOperationException("Missing indexed vehicle occupant");
                return; // A structural rebuild already invalidated the old buckets.
            }
            dirtyOccupancyBuckets.Add(bucket);
        }
        void RemoveIndexedOccupant(Vehicle v)
        {
            RemoveIndexed(v.indexedLane,v.sourceOccupant);RemoveIndexed(v.indexedShadow,v.shadowOccupant);
            v.indexedLane=v.indexedShadow=0;pendingOccupants.Remove(v);
        }
        void EnsureIndex()
        {
            if(!indexDirty)return;
            if(fullIndexRequired){Reindex();return;}
            IncrementalIndexUpdates++;
            foreach(var v in pendingOccupants)
            {
                if(v.status==VehicleStatus.Delivered||v.status==VehicleStatus.AwaitingRestart)continue;
                if(!Network.Contains(v.Current)){Reindex();return;}
                if(v.indexedLane!=v.Current)
                {
                    RemoveIndexed(v.indexedLane,v.sourceOccupant);
                    OccupantBucket(v.Current).Add(v.sourceOccupant);v.indexedLane=v.Current;
                }
                v.sourceOccupant.distance=v.distance;dirtyOccupancyBuckets.Add(OccupantBucket(v.Current));
                if(v.indexedShadow!=v.changeTarget)
                {
                    RemoveIndexed(v.indexedShadow,v.shadowOccupant);
                    if(v.changeTarget!=0)OccupantBucket(v.changeTarget).Add(v.shadowOccupant);
                    v.indexedShadow=v.changeTarget;
                }
                if(v.changeTarget!=0)
                {
                    v.shadowOccupant.distance=Network.Get(v.Current).Geometry.MapDistance(Network.Get(v.changeTarget).Geometry,v.distance);
                    dirtyOccupancyBuckets.Add(OccupantBucket(v.changeTarget));
                }
            }
            pendingOccupants.Clear();
            foreach(var bucket in dirtyOccupancyBuckets)if(bucket.Count>1)bucket.Sort(LaneOccupantOrder.Instance);
            dirtyOccupancyBuckets.Clear();
            // Movement reserves speculative paths during its immutable-input phase.
            // Keep the original full physical-claim rebuild for this first stage;
            // carrying those speculative claims across steps is not equivalent.
            ActiveCount=0;awaitingRestarts=0;reservations.Reset(Network);
            foreach(var v in data.vehicles)
            {
                if(v.status==VehicleStatus.Active&&(!EndpointExists(v.Task.origin)||!EndpointExists(v.Task.destination))){Reindex();return;}
                if(v.status==VehicleStatus.AwaitingRestart){awaitingRestarts++;continue;}
                if(v.status==VehicleStatus.Delivered)continue;
                if(!Network.Contains(v.Current))
                {
                    if(v.status!=VehicleStatus.Stranded)throw new InvalidOperationException("Active actor lost its drivable");
                    foreach(var placement in lostOccupancy.Get(Network,v))
                    {
                        var road=Network.Get(placement.lane);int junction=road.JunctionId;
                        if(junction==0&&placement.occupant.distance<EntryReservationEnd(road))junction=road.From;
                        if(!road.IsConnection&&placement.occupant.distance>StopDistance(road))junction=road.To;
                        if(Network.IsConflictJunction(junction))reservations.Reserve(junction,0,v.id);
                    }
                    continue;
                }
                if(v.status==VehicleStatus.Active)ActiveCount++;
                var d=Network.Get(v.Current);int j=d.JunctionId;
                if(j==0&&v.distance<EntryReservationEnd(d)-1e-9)j=d.From;
                if(!d.IsConnection&&v.distance>StopDistance(d)+1e-9)j=d.To;
                if(j!=0&&Network.IsConflictJunction(j))reservations.Reserve(j,v.changeTarget!=0?0:Movement(Network,v.Plan.segments,v.cursor,d,j),v.id);
            }
            indexDirty=false;
        }

        // Cold test oracle: compare BEFORE the old validator can repair the index.
        // No hashing/tolerances; retain occupant identity, exact distance and claims.
        internal void AssertIncrementalIndex()
        {
            EnsureIndex();if(motionOrder.Count!=data.vehicles.Count)throw new InvalidOperationException("Motion ordering membership differs");
            int previous=0;foreach(var actor in motionOrder){if(actor.id<=previous||!vehiclesById.TryGetValue(actor.id,out var owned)||!ReferenceEquals(actor,owned))throw new InvalidOperationException("Motion ID ordering differs");previous=actor.id;}
            var before=new Dictionary<long,List<(LaneOccupant occupant,double distance)>>();
            foreach(var pair in occupancy)if(pair.Value.Count>0)
            {
                var list=new List<(LaneOccupant,double)>();foreach(var item in pair.Value)list.Add((item,item.distance));before.Add(pair.Key,list);
            }
            var claims=reservations.CopyClaims();int active=ActiveCount,restarts=awaitingRestarts;Reindex();
            foreach(var pair in occupancy)if(pair.Value.Count>0)
            {
                if(!before.TryGetValue(pair.Key,out var list)||list.Count!=pair.Value.Count)throw new InvalidOperationException("Incremental bucket membership differs");
                for(int i=0;i<list.Count;i++)if(!ReferenceEquals(list[i].occupant,pair.Value[i])||BitConverter.DoubleToInt64Bits(list[i].distance)!=BitConverter.DoubleToInt64Bits(pair.Value[i].distance))throw new InvalidOperationException("Incremental bucket order/distance differs");
                before.Remove(pair.Key);
            }
            if(before.Count!=0||ActiveCount!=active||awaitingRestarts!=restarts)throw new InvalidOperationException("Incremental actor counts differ");
            var rebuilt=reservations.CopyClaims();if(claims.Count!=rebuilt.Count)throw new InvalidOperationException("Incremental claim count differs");
            for(int i=0;i<claims.Count;i++)if(claims[i]!=rebuilt[i])throw new InvalidOperationException("Incremental physical claims differ");
        }
    }
}
