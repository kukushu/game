using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Security.Cryptography;
using HarborCity.RoadCore;

namespace HarborCity
{
    // Explicit concrete business DTO schema, no legacy CitySaveData.ToCity or road migration.
    // Save is a cold path; reflection preserves every original business scalar's exact bits.
    public static class CityCoreSave
    {
        const int Magic=0x48434932,Version=12,MaximumCount=1000000;
        static readonly Dictionary<Type,FieldInfo[]> fields=new Dictionary<Type,FieldInfo[]>();
        static FieldInfo[] Fields(Type type)
        {FieldInfo[] result;if(!fields.TryGetValue(type,out result)){result=type.GetFields(BindingFlags.Public|BindingFlags.Instance).Where(f=>!f.IsNotSerialized).OrderBy(f=>f.Name,StringComparer.Ordinal).ToArray();fields.Add(type,result);}return result;}
        static void Put(BinaryWriter writer,Type type,object value)
        {
            if(!type.IsValueType){writer.Write(value!=null);if(value==null)return;}
            if(type==typeof(string)){writer.Write((string)value);return;}
            if(type==typeof(long)){writer.Write((long)value);return;}if(type==typeof(int)){writer.Write((int)value);return;}if(type==typeof(float)){writer.Write((float)value);return;}if(type==typeof(double)){writer.Write((double)value);return;}if(type==typeof(bool)){writer.Write((bool)value);return;}
            if(type.IsEnum){writer.Write(Convert.ToInt32(value));return;}
            if(type.IsArray){var array=(Array)value;writer.Write(array.Length);foreach(var item in array)Put(writer,type.GetElementType(),item);return;}
            if(type.IsGenericType&&type.GetGenericTypeDefinition()==typeof(List<>)){var list=(IList)value;writer.Write(list.Count);foreach(var item in list)Put(writer,type.GetGenericArguments()[0],item);return;}
            if(type.Namespace!="HarborCity"&&type!=typeof(SimulationClockData))throw new InvalidDataException("Unsupported business fact type "+type);
            var members=Fields(type);writer.Write(type.FullName);writer.Write(members.Length);foreach(var field in members){writer.Write(field.Name);Put(writer,field.FieldType,field.GetValue(value));}
        }
        static int Count(BinaryReader reader){int count=reader.ReadInt32();if(count<0||count>MaximumCount)throw new InvalidDataException("Invalid business count");return count;}
        static object Get(BinaryReader reader,Type type)
        {
            if(!type.IsValueType&&!reader.ReadBoolean())return null;
            if(type==typeof(string))return reader.ReadString();if(type==typeof(long))return reader.ReadInt64();if(type==typeof(int))return reader.ReadInt32();if(type==typeof(float))return reader.ReadSingle();if(type==typeof(double))return reader.ReadDouble();if(type==typeof(bool))return reader.ReadBoolean();if(type.IsEnum)return Enum.ToObject(type,reader.ReadInt32());
            if(type.IsArray){int count=Count(reader);var result=Array.CreateInstance(type.GetElementType(),count);for(int i=0;i<count;i++)result.SetValue(Get(reader,type.GetElementType()),i);return result;}
            if(type.IsGenericType&&type.GetGenericTypeDefinition()==typeof(List<>)){int count=Count(reader);var result=(IList)Activator.CreateInstance(type);for(int i=0;i<count;i++)result.Add(Get(reader,type.GetGenericArguments()[0]));return result;}
            if(type.Namespace!="HarborCity"&&type!=typeof(SimulationClockData))throw new InvalidDataException("Unsupported business schema");
            var members=Fields(type);if(reader.ReadString()!=type.FullName||Count(reader)!=members.Length)throw new InvalidDataException("Business schema changed; start a new city");
            var value=Activator.CreateInstance(type);foreach(var field in members){if(reader.ReadString()!=field.Name)throw new InvalidDataException("Business field schema changed");field.SetValue(value,Get(reader,field.FieldType));}return value;
        }
        public static byte[] Write(CityCoreIndustrySession session)
        {
            session.Validate();using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream))
            {
                writer.Write(Magic);writer.Write(Version);Put(writer,typeof(CityCoreBusinessData),session.SnapshotBusiness());byte[] transport=SaveCodec.Write(session.Transport);writer.Write(transport.Length);writer.Write(transport);
                byte[] payload=stream.ToArray(),digest;using(var sha=SHA256.Create())digest=sha.ComputeHash(payload);var bytes=new byte[payload.Length+32];Buffer.BlockCopy(payload,0,bytes,0,payload.Length);Buffer.BlockCopy(digest,0,bytes,payload.Length,32);return bytes;
            }
        }
        internal static CityCoreBusinessData CopyBusiness(CityCoreBusinessData data)
        {
            using(var stream=new MemoryStream()){using(var writer=new BinaryWriter(stream,System.Text.Encoding.UTF8,true))Put(writer,typeof(CityCoreBusinessData),data);stream.Position=0;using(var reader=new BinaryReader(stream))return (CityCoreBusinessData)Get(reader,typeof(CityCoreBusinessData));}
        }
        public static CityCoreIndustrySession Read(byte[] bytes)
        {
            if(bytes==null||bytes.Length<40||BitConverter.ToInt32(bytes,0)!=Magic||BitConverter.ToInt32(bytes,4)!=Version)throw new InvalidDataException("Unsupported city-core save. Legacy city/demo saves cannot be loaded.");
            byte[] digest;using(var sha=SHA256.Create())digest=sha.ComputeHash(bytes,0,bytes.Length-32);for(int i=0;i<32;i++)if(digest[i]!=bytes[bytes.Length-32+i])throw new InvalidDataException("Corrupted city-core save");
            using(var stream=new MemoryStream(bytes,0,bytes.Length-32,false))using(var reader=new BinaryReader(stream))
            {
                reader.ReadInt32();reader.ReadInt32();var data=(CityCoreBusinessData)Get(reader,typeof(CityCoreBusinessData));if(data.version!=4)throw new InvalidDataException("Unsupported business version");
                int count=reader.ReadInt32();if(count<0||count>stream.Length-stream.Position)throw new InvalidDataException("Truncated transport state");byte[] core=reader.ReadBytes(count);if(stream.Position!=stream.Length)throw new InvalidDataException("Trailing city save data");
                var city=new CityModel{utilities=data.utilities,map=data.map,zoning=data.zoning,development=data.development,money=data.money,day=data.day,nextBuildingId=data.nextBuilding,society=data.society,retiredFactoryWages=data.retiredFactoryWages,residualIndustry=data.residualIndustry,buildings=data.buildings.Select(b=>b.ToBuilding()).ToList()};
                var timeline=new SimulationClock(data.clock);var transport=SaveCodec.Read(core,timeline,city.buildings.Where(b=>!b.abandoned&&!b.burned).Select(b=>b.id));
                var session=new CityCoreIndustrySession(city,data.calendarOffset,transport.Snapshot(),data.clock,data.nextDispatchTick,data.retiredBuildings.Select(b=>b.ToBuilding()),data.commuteEnabled,data.payrollDay);session.Validate();return session;
            }
        }
    }
}
