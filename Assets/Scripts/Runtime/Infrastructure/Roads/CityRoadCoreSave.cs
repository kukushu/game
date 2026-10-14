using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace HarborCity.RoadCore
{
    // Explicit new format. Binary double values round-trip exactly, including fixed-step remainder.
    public static class SaveCodec
    {
        const int Magic=0x48435243,Version=2,Limit=1000000;
        static void PointOut(BinaryWriter w,Point p){w.Write(p.x);w.Write(p.y);w.Write(p.z);}
        static Point PointIn(BinaryReader r)=>new Point(r.ReadDouble(),r.ReadDouble(),r.ReadDouble());
        static int Count(BinaryReader r){int count=r.ReadInt32();if(count<0||count>Limit)throw new InvalidDataException("Invalid entity count");return count;}
        public static byte[] Write(World world)
        {
            var d=world.Snapshot();using(var stream=new MemoryStream())using(var w=new BinaryWriter(stream))
            {
                w.Write(Magic);w.Write(Version);w.Write(d.nextVehicle);w.Write(d.nextDepot);w.Write(d.clock);w.Write(d.accumulator);w.Write(d.delivered);w.Write(d.externalBusiness);
                w.Write(d.network.nextJunction);w.Write(d.network.nextRoad);w.Write(d.network.junctions.Count);
                foreach(var j in d.network.junctions){w.Write(j.id);PointOut(w,j.position);}
                w.Write(d.network.roads.Count);foreach(var road in d.network.roads){w.Write(road.id);w.Write(road.from);w.Write(road.to);w.Write(road.oneWay);w.Write(road.speed);PointOut(w,road.startTangent);PointOut(w,road.endTangent);w.Write(road.points.Length);foreach(var p in road.points)PointOut(w,p);}
                w.Write(d.depots.Count);foreach(var depot in d.depots){w.Write(depot.id);w.Write(depot.junction);w.Write(depot.stock);w.Write(depot.capacity);w.Write(depot.cash);}
                w.Write(d.vehicles.Count);foreach(var v in d.vehicles){w.Write(v.id);w.Write(v.origin);w.Write(v.destination);w.Write(v.cargo);w.Write(v.price);w.Write(v.distance);w.Write(v.cursor);w.Write((int)v.status);PointOut(w,v.strandedPosition);w.Write(v.route.Count);foreach(long id in v.route)w.Write(id);}
                byte[] payload=stream.ToArray(),hash;using(var sha=SHA256.Create())hash=sha.ComputeHash(payload);
                var result=new byte[payload.Length+hash.Length];Buffer.BlockCopy(payload,0,result,0,payload.Length);Buffer.BlockCopy(hash,0,result,payload.Length,hash.Length);return result;
            }
        }
        public static World Read(byte[] bytes,ITransportBusiness business=null)
        {
            if(bytes==null||bytes.Length<8||BitConverter.ToInt32(bytes,0)!=Magic||BitConverter.ToInt32(bytes,4)!=Version)throw new InvalidDataException("Unsupported save format: expected HarborCity road-core version 2. Legacy city saves cannot be loaded.");
            if(bytes.Length<40)throw new InvalidDataException("Truncated road-core save");
            byte[] digest;using(var sha=SHA256.Create())digest=sha.ComputeHash(bytes,0,bytes.Length-32);
            for(int i=0;i<32;i++)if(digest[i]!=bytes[bytes.Length-32+i])throw new InvalidDataException("Unsupported or corrupted road-core save (checksum mismatch)");
            using(var stream=new MemoryStream(bytes,0,bytes.Length-32,false))using(var r=new BinaryReader(stream))
            {
                if(bytes.Length<8||r.ReadInt32()!=Magic||r.ReadInt32()!=Version)throw new InvalidDataException("Unsupported save format: expected HarborCity road-core version 2. Legacy city saves cannot be loaded.");
                var d=new WorldData{nextVehicle=r.ReadInt32(),nextDepot=r.ReadInt32(),clock=r.ReadDouble(),accumulator=r.ReadDouble(),delivered=r.ReadInt32(),externalBusiness=r.ReadBoolean()};
                d.network.nextJunction=r.ReadInt32();d.network.nextRoad=r.ReadInt32();int count=Count(r);
                for(int i=0;i<count;i++)d.network.junctions.Add(new Junction{id=r.ReadInt32(),position=PointIn(r)});
                count=Count(r);for(int i=0;i<count;i++){var road=new PhysicalRoad{id=r.ReadInt32(),from=r.ReadInt32(),to=r.ReadInt32(),oneWay=r.ReadBoolean(),speed=r.ReadDouble(),startTangent=PointIn(r),endTangent=PointIn(r)};road.points=new Point[Count(r)];for(int j=0;j<road.points.Length;j++)road.points[j]=PointIn(r);d.network.roads.Add(road);}
                count=Count(r);for(int i=0;i<count;i++)d.depots.Add(new Depot{id=r.ReadInt32(),junction=r.ReadInt32(),stock=r.ReadInt32(),capacity=r.ReadInt32(),cash=r.ReadDouble()});
                count=Count(r);for(int i=0;i<count;i++){var v=new Vehicle{id=r.ReadInt32(),origin=r.ReadInt32(),destination=r.ReadInt32(),cargo=r.ReadInt32(),price=r.ReadDouble(),distance=r.ReadDouble(),cursor=r.ReadInt32(),status=(VehicleStatus)r.ReadInt32(),strandedPosition=PointIn(r),route=new List<long>()};int n=Count(r);for(int j=0;j<n;j++)v.route.Add(r.ReadInt64());d.vehicles.Add(v);}
                if(stream.Position!=stream.Length)throw new InvalidDataException("Unexpected trailing save data");var world=new World(d,business);return world;
            }
        }
    }
}
