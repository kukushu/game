using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using HarborCity;
using HarborCity.RoadCore;
public static class RoadCoreScaleBenchmark
{
    public static void Main(string[] args)
    {
        using(var output=new StreamWriter(args[0]))
        {
            output.WriteLine("version,vehicles,queued,steps,active_min,active_max,mean_ms,p95_ms,p99_ms,bytes_per_step,semantic_sha256");
            foreach(int count in new[]{1000,3000,10000})foreach(bool queued in new[]{false,true})
            {
                var w=CityRoadCoreFixtures.Create(count,queued);for(int i=0;i<20;i++)w.Step();
                var times=new double[300];var timer=new Stopwatch();int min=int.MaxValue,max=0;long bytes=GC.GetAllocatedBytesForCurrentThread();
                for(int i=0;i<times.Length;i++){timer.Restart();w.Step();timer.Stop();times[i]=timer.Elapsed.TotalMilliseconds;min=Math.Min(min,w.ActiveCount);max=Math.Max(max,w.ActiveCount);}
                bytes=GC.GetAllocatedBytesForCurrentThread()-bytes;w.Validate();w.AssertSafety();double mean=times.Average();Array.Sort(times);
                string row=string.Join(",",args[1],count,queued,times.Length,min,max,mean.ToString("F6",CultureInfo.InvariantCulture),times[284].ToString("F6",CultureInfo.InvariantCulture),times[296].ToString("F6",CultureInfo.InvariantCulture),bytes/times.Length,Digest(w));output.WriteLine(row);Console.WriteLine(row);
            }
        }
    }
    static string Digest(World w)
    {
        using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream))
        {
            writer.Write(w.Clock);writer.Write(w.Delivered);writer.Write(w.Vehicles.Count);
            foreach(var v in w.Vehicles){writer.Write(v.id);writer.Write(v.Current);writer.Write(v.distance);writer.Write(v.cursor);writer.Write(v.changeTarget);writer.Write(v.changeElapsed);writer.Write((int)v.status);writer.Write(v.Task.id);writer.Write(v.Task.Cargo);writer.Write(v.Task.Price);writer.Write(v.Plan.EndDistance);foreach(long id in v.Route)writer.Write(id);var p=w.Position(v);writer.Write(p.x);writer.Write(p.y);writer.Write(p.z);}
            foreach(var d in w.Depots){writer.Write(d.id);writer.Write(d.stock);writer.Write(d.cash);}writer.Flush();using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(stream.ToArray()));
        }
    }
}
