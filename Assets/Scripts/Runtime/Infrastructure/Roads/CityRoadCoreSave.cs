using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace HarborCity.RoadCore
{
    // Explicit new format. Binary double values round-trip exactly, including fixed-step remainder.
    public static class SaveCodec
    {
        const int Magic=0x48435243,Version=14,Limit=1000000;
        static void PointOut(BinaryWriter w,Point p){w.Write(p.x);w.Write(p.y);w.Write(p.z);}
        static Point PointIn(BinaryReader r)=>new Point(r.ReadDouble(),r.ReadDouble(),r.ReadDouble());
        static int Count(BinaryReader r){int count=r.ReadInt32();if(count<0||count>Limit)throw new InvalidDataException("Invalid entity count");return count;}
        public static byte[] Write(World world)
        {
            var d=world.Snapshot();using(var stream=new MemoryStream())using(var w=new BinaryWriter(stream))
            {
                w.Write(Magic);w.Write(Version);w.Write(d.nextVehicle);w.Write(d.nextTask);w.Write(d.nextDepot);w.Write(d.standaloneClock.tick);w.Write(d.standaloneClock.pendingSeconds);w.Write(0);w.Write(d.delivered);w.Write(d.externalBusiness);w.Write(d.networkRevision);
                w.Write(d.network.nextJunction);w.Write(d.network.nextRoad);w.Write(d.network.junctions.Count);
                foreach(var j in d.network.junctions){w.Write(j.id);PointOut(w,j.position);}
                w.Write(d.network.roads.Count);foreach(var road in d.network.roads){w.Write(road.id);w.Write(road.from);w.Write(road.to);w.Write((int)road.direction);w.Write(road.lanes);w.Write(road.speed);PointOut(w,road.startTangent);PointOut(w,road.endTangent);w.Write(road.points.Length);foreach(var p in road.points)PointOut(w,p);w.Write(road.rightVectors!=null);if(road.rightVectors!=null)foreach(var p in road.rightVectors)PointOut(w,p);}
                w.Write(d.depots.Count);foreach(var depot in d.depots){w.Write(depot.id);w.Write(depot.junction);w.Write(depot.stock);w.Write(depot.capacity);w.Write(depot.cash);}
                w.Write(d.accesses.Count);foreach(var access in d.accesses){w.Write(access.endpoint);w.Write(access.preferredIndex);w.Write(access.lane);w.Write(access.distance);w.Write((int)access.use);w.Write(access.valid);}
                w.Write(d.tasks.Count);foreach(var t in d.tasks){w.Write(t.id);w.Write(t.origin);w.Write(t.destination);w.Write((int)t.purpose);w.Write(t.citizenId);w.Write(t.departedAt);w.Write((int)t.state);w.Write(t.Shipment!=null);if(t.Shipment!=null){w.Write((int)t.Shipment.kind);w.Write(t.Shipment.remaining);w.Write(t.Shipment.unitPrice);}}
                w.Write(d.vehicles.Count);foreach(var v in d.vehicles){w.Write(v.id);w.Write(v.Task.id);w.Write(v.Current);w.Write(v.distance);w.Write(v.Plan.EndDistance);w.Write(v.changeTarget);w.Write(v.changeElapsed);w.Write(v.cursor);w.Write((int)v.status);w.Write(v.Position.IsFrozen);PointOut(w,v.Position.FrozenPoint);w.Write(v.Plan.segments.Count);foreach(long id in v.Plan.segments)w.Write(id);}
                byte[] payload=stream.ToArray(),hash;using(var sha=SHA256.Create())hash=sha.ComputeHash(payload);
                var result=new byte[payload.Length+hash.Length];Buffer.BlockCopy(payload,0,result,0,payload.Length);Buffer.BlockCopy(hash,0,result,payload.Length,hash.Length);return result;
            }
        }
        public static World Read(byte[] bytes,SimulationClock clock=null,IEnumerable<int> endpoints=null)
        {
            if(bytes==null||bytes.Length<8||BitConverter.ToInt32(bytes,0)!=Magic||BitConverter.ToInt32(bytes,4)!=Version)throw new InvalidDataException("Unsupported save format: expected HarborCity road-core version 14. Legacy city saves cannot be loaded.");
            if(bytes.Length<40)throw new InvalidDataException("Truncated road-core save");
            byte[] digest;using(var sha=SHA256.Create())digest=sha.ComputeHash(bytes,0,bytes.Length-32);
            for(int i=0;i<32;i++)if(digest[i]!=bytes[bytes.Length-32+i])throw new InvalidDataException("Unsupported or corrupted road-core save (checksum mismatch)");
            using(var stream=new MemoryStream(bytes,0,bytes.Length-32,false))using(var r=new BinaryReader(stream))
            {
                if(bytes.Length<8||r.ReadInt32()!=Magic||r.ReadInt32()!=Version)throw new InvalidDataException("Unsupported save format: expected HarborCity road-core version 14. Legacy city saves cannot be loaded.");
                var d=new WorldData{nextVehicle=r.ReadInt32(),nextTask=r.ReadInt32(),nextDepot=r.ReadInt32(),standaloneClock=new SimulationClockData{tick=r.ReadInt64(),pendingSeconds=r.ReadDouble()}};if(Count(r)!=0)throw new InvalidDataException("Endpoint index is derived, not saved");d.delivered=r.ReadInt32();d.externalBusiness=r.ReadBoolean();d.networkRevision=r.ReadInt32();
                d.network.nextJunction=r.ReadInt32();d.network.nextRoad=r.ReadInt32();int count=Count(r);
                for(int i=0;i<count;i++)d.network.junctions.Add(new Junction{id=r.ReadInt32(),position=PointIn(r)});
                count=Count(r);for(int i=0;i<count;i++){var road=new PhysicalRoad{id=r.ReadInt32(),from=r.ReadInt32(),to=r.ReadInt32(),direction=(RoadDirection)r.ReadInt32(),lanes=r.ReadInt32(),speed=r.ReadDouble(),startTangent=PointIn(r),endTangent=PointIn(r)};road.points=new Point[Count(r)];for(int j=0;j<road.points.Length;j++)road.points[j]=PointIn(r);if(r.ReadBoolean()){road.rightVectors=new Point[road.points.Length];for(int j=0;j<road.points.Length;j++)road.rightVectors[j]=PointIn(r);}d.network.roads.Add(road);}
                count=Count(r);for(int i=0;i<count;i++)d.depots.Add(new Depot{id=r.ReadInt32(),junction=r.ReadInt32(),stock=r.ReadInt32(),capacity=r.ReadInt32(),cash=r.ReadDouble()});
                count=Count(r);for(int i=0;i<count;i++)d.accesses.Add(new RoadAccess{endpoint=r.ReadInt32(),preferredIndex=r.ReadInt32(),lane=r.ReadInt64(),distance=r.ReadDouble(),use=(AccessUse)r.ReadInt32(),valid=r.ReadBoolean()});
                var tasks=new Dictionary<int,TripTask>();count=Count(r);for(int i=0;i<count;i++){var t=new TripTask{id=r.ReadInt32(),origin=r.ReadInt32(),destination=r.ReadInt32(),purpose=(TransportPurpose)r.ReadInt32(),citizenId=r.ReadInt32(),departedAt=r.ReadDouble(),state=(TripState)r.ReadInt32()};if(r.ReadBoolean())t.Shipment=new Shipment{kind=(CargoKind)r.ReadInt32(),remaining=r.ReadInt32(),unitPrice=r.ReadDouble()};if(tasks.ContainsKey(t.id))throw new InvalidDataException("Duplicate task ID");tasks.Add(t.id,t);d.tasks.Add(t);}
                count=Count(r);for(int i=0;i<count;i++){int id=r.ReadInt32(),taskId=r.ReadInt32();if(!tasks.TryGetValue(taskId,out var task))throw new InvalidDataException("Vehicle task missing");var v=new Vehicle{id=id,Task=task,Position=new VehiclePosition(r.ReadInt64(),r.ReadDouble()),Plan=new RoutePlan{EndDistance=r.ReadDouble()},changeTarget=r.ReadInt64(),changeElapsed=r.ReadDouble(),cursor=r.ReadInt32(),status=(VehicleStatus)r.ReadInt32()};bool frozen=r.ReadBoolean();Point frozenPoint=PointIn(r);if(frozen!=(v.status==VehicleStatus.Stranded))throw new InvalidDataException("Invalid frozen position state");if(frozen)v.Freeze(frozenPoint);int n=Count(r);for(int j=0;j<n;j++)v.Plan.segments.Add(r.ReadInt64());d.vehicles.Add(v);}
                if(stream.Position!=stream.Length)throw new InvalidDataException("Unexpected trailing save data");if(d.externalBusiness&&endpoints==null)throw new InvalidDataException("City endpoint index must be rebuilt from buildings");var world=new World(d,clock,endpoints);return world;
            }
        }
    }
}
