using System;
using System.Collections.Generic;

namespace HarborCity.RoadCore
{
    public sealed partial class World
    {
        // This is derived World state, not another physical position owner.
        bool fullIndexRequired=true;
        internal long FullIndexBuilds,IncrementalIndexUpdates;
#if TRAFFIC_HOTPATH_CENSUS
        internal long CensusRegistrations,CensusUniqueRegistrations,CensusBuckets,CensusAlreadyOrdered,CensusUnordered,CensusOccupants,CensusOrderComparisons;
#endif
        readonly HashSet<Vehicle> pendingJunctionOccupants=new HashSet<Vehicle>();
        internal long JunctionClassifications,JunctionChanges,JunctionFlushes;
        void PhysicalClaim(Vehicle v,out int junction,out long movement)
        {
            junction=0;movement=0;
            if(v.status==VehicleStatus.Delivered||v.status==VehicleStatus.AwaitingRestart)return;
            if(!Network.Contains(v.Current)){if(v.status!=VehicleStatus.Stranded)throw new InvalidOperationException("Active actor lost its drivable");return;}
            var d=Network.Get(v.Current);int j=d.JunctionId;
            if(j==0&&v.distance<EntryReservationEnd(d)-1e-9)j=d.From;
            if(!d.IsConnection&&v.distance>StopDistance(d)+1e-9)j=d.To;
            if(j!=0&&Network.IsConflictJunction(j)){junction=j;movement=v.changeTarget!=0?0:Movement(Network,v.Plan.segments,v.cursor,d,j);}
        }
        void TrackJunctionOccupant(Vehicle v)
        {
            JunctionClassifications++;PhysicalClaim(v,out int junction,out long movement);
            if(junction!=v.indexedJunction||movement!=v.indexedMovement)pendingJunctionOccupants.Add(v);
        }
        void RefreshJunctionOccupants()
        {
            // Preserve the old lazy reset boundary. Drop speculative claims, then
            // remove ALL old changed claims before inserting any final claim.
            reservations.ClearTemporary();JunctionFlushes++;
            foreach(var v in pendingJunctionOccupants)if(v.indexedJunction!=0)reservations.RemovePhysical(v.indexedJunction,v.indexedMovement,v.id);
            foreach(var v in pendingJunctionOccupants){PhysicalClaim(v,out int junction,out long movement);v.indexedJunction=junction;v.indexedMovement=movement;if(junction!=0)reservations.Reserve(junction,movement,v.id);JunctionChanges++;}
            pendingJunctionOccupants.Clear();
        }
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
            {
#if TRAFFIC_HOTPATH_CENSUS
                CensusRegistrations++;if(pendingOccupants.Add(v))CensusUniqueRegistrations++;
#else
                pendingOccupants.Add(v);
#endif
                TrackJunctionOccupant(v);
            }
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
            v.indexedLane=v.indexedShadow=0;pendingOccupants.Remove(v);if(v.indexedJunction!=0)pendingJunctionOccupants.Add(v);
        }
        void EnsureIndex()
        {
            if(!indexDirty)return;
            if(fullIndexRequired){Reindex();return;}
#if UNITY_5_3_OR_NEWER
            using var indexObservation=indexSample.Auto();
#endif
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
            foreach(var bucket in dirtyOccupancyBuckets)if(bucket.Count>1)
            {
#if TRAFFIC_HOTPATH_CENSUS
                CensusBuckets++;CensusOccupants+=bucket.Count;bool ordered=true;
                for(int i=1;i<bucket.Count;i++){CensusOrderComparisons++;if(LaneOccupantOrder.Instance.Compare(bucket[i-1],bucket[i])>0){ordered=false;break;}}
                if(ordered)CensusAlreadyOrdered++;else CensusUnordered++;
#endif
#if TRAFFIC_HOTPATH_REFERENCE
                bucket.Sort(LaneOccupantOrder.Instance);
#else
                LaneOccupantOrder.SortIfNeeded(bucket);
#endif
            }
            dirtyOccupancyBuckets.Clear();
            RefreshJunctionOccupants();
            indexDirty=false;
        }

        // Independent frozen-rule oracle. Never repairs buckets/claims after comparison.
        internal void AssertJunctionIndex()
        {
            EnsureIndex();var expected=new JunctionReservations(Network);int active=0,restarts=0;
            foreach(var v in data.vehicles)
            {
                if(v.status==VehicleStatus.AwaitingRestart){restarts++;continue;}
                if(v.status==VehicleStatus.Delivered)continue;
                if(!Network.Contains(v.Current)){
                    if(v.status!=VehicleStatus.Stranded)throw new InvalidOperationException("Oracle active actor lost drivable");
                    foreach(var p in lostOccupancy.Get(Network,v)){var road=Network.Get(p.lane);int j=road.JunctionId;if(j==0&&p.occupant.distance<EntryReservationEnd(road))j=road.From;if(!road.IsConnection&&p.occupant.distance>StopDistance(road))j=road.To;if(Network.IsConflictJunction(j))expected.Reserve(j,0,v.id);}continue;
                }
                if(v.status==VehicleStatus.Active)active++;
                var d=Network.Get(v.Current);int junction=d.JunctionId;
                if(junction==0&&v.distance<EntryReservationEnd(d)-1e-9)junction=d.From;
                if(!d.IsConnection&&v.distance>StopDistance(d)+1e-9)junction=d.To;
                if(junction!=0&&Network.IsConflictJunction(junction))expected.Reserve(junction,v.changeTarget!=0?0:Movement(Network,v.Plan.segments,v.cursor,d,junction),v.id);
            }
            if(active!=ActiveCount||restarts!=awaitingRestarts)throw new InvalidOperationException("Incremental lifecycle counts differ");
            var a=reservations.CopyClaims();var b=expected.CopyClaims();if(a.Count!=b.Count)throw new InvalidOperationException("Physical junction count differs");
            for(int i=0;i<a.Count;i++)if(a[i]!=b[i])throw new InvalidOperationException("Physical junction membership/direction differs");
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
