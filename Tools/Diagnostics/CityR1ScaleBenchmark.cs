using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Diagnostics;
using HarborCity;
using HarborCity.RoadCore;

// Shared before/after workload: real registered people/jobs/factories/shops and
// physical commuting/freight. Finite inputs, no service/consumption/full-city claim.
public static class CityR1ScaleBenchmark
{
    public static void Main(string[] args)
    {
        using(var output=new StreamWriter(args[0]))
        {
            output.WriteLine("version,residents,factories,shops,steps,active_min,active_max,mean_ms,p95_ms,p99_ms,allocated_per_step,produced,delivered");
            foreach(int count in new[]{100,1000,3000})
            {
                var session=Create(count);for(int i=0;i<20;i++)session.Advance(.05);
                var times=new double[200];int min=int.MaxValue,max=0;var timer=new Stopwatch();long memory=GC.GetAllocatedBytesForCurrentThread();
                for(int i=0;i<times.Length;i++){timer.Restart();session.Advance(.05);timer.Stop();times[i]=timer.Elapsed.TotalMilliseconds;min=Math.Min(min,session.Transport.ActiveCount);max=Math.Max(max,session.Transport.ActiveCount);}
                memory=GC.GetAllocatedBytesForCurrentThread()-memory;session.Validate();int produced=session.City.buildings.OfType<IndustrialBuilding>().Sum(b=>b.factory.produced);int goods=session.City.buildings.OfType<IndustrialBuilding>().Sum(b=>b.Stock)+session.City.buildings.OfType<CommercialBuilding>().Sum(b=>b.Stock)+session.Transport.Vehicles.Sum(Cargo);if(produced!=goods)throw new Exception("Goods conservation failed");if(!session.City.Citizens.All(p=>p.atWork&&p.workedMinutes>0))throw new Exception("Fixture workers did not actually commute and work");double mean=times.Average();Array.Sort(times);
                string row=string.Join(",",args[1],count,count/4,count/4,times.Length,min,max,mean.ToString("F6",CultureInfo.InvariantCulture),times[189].ToString("F6",CultureInfo.InvariantCulture),times[197].ToString("F6",CultureInfo.InvariantCulture),memory/times.Length,produced,session.Transport.Delivered);output.WriteLine(row);Console.WriteLine(row);using(var sha=System.Security.Cryptography.SHA256.Create())File.WriteAllText(args[0]+"."+count+".sha256",BitConverter.ToString(sha.ComputeHash(CityCoreSave.Write(session))));
                File.WriteAllText(args[0]+"."+count+".semantic.sha256",SemanticDigest(session));
            }
        }
    }
    static CityCoreIndustrySession Create(int population)=>CityIntegratedScaleFixture.Create(population);
    static int Cargo(Vehicle v)
    {
#if BASELINE_INLINE_TASKS
        return v.cargo;
#else
        return v.Task.Cargo;
#endif
    }
    static string SemanticDigest(CityCoreIndustrySession session)
    {
        using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream))
        {
            // Common business DTO schema, without changed outer save-version header.
            typeof(CityCoreSave).GetMethod("Put",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{writer,typeof(CityCoreBusinessData),session.SnapshotBusiness()});
            var world=session.Transport;writer.Write(world.Clock);writer.Write(world.Delivered);writer.Write(world.Vehicles.Count);
            foreach(var v in world.Vehicles)
            {
                writer.Write(v.id);writer.Write(v.Current);writer.Write(v.cursor);writer.Write(v.distance);writer.Write(v.changeTarget);writer.Write(v.changeElapsed);writer.Write((int)v.status);writer.Write(v.strandedPosition.x);writer.Write(v.strandedPosition.y);writer.Write(v.strandedPosition.z);writer.Write(v.Route.Count);foreach(var id in v.Route)writer.Write(id);
#if BASELINE_INLINE_TASKS
                writer.Write(v.origin);writer.Write(v.destination);writer.Write((int)v.purpose);writer.Write(v.citizenId);writer.Write(v.departedAt);writer.Write(v.cargo);writer.Write(v.price);writer.Write(v.arrivalDistance);
#else
                writer.Write(v.Task.origin);writer.Write(v.Task.destination);writer.Write((int)v.Task.purpose);writer.Write(v.Task.citizenId);writer.Write(v.Task.departedAt);writer.Write(v.Task.Cargo);writer.Write(v.Task.Price);writer.Write(v.Plan.EndDistance);
#endif
            }
            writer.Flush();using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(stream.ToArray()));
        }
    }
}
