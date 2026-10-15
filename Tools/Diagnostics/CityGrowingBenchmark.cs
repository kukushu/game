using System;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using HarborCity;
using HarborCity.RoadCore;
public static class CityGrowingBenchmark
{
    public static void Main(string[] args)
    {
        var s=CityGrowingGridFixture.Create();s.Advance(18);const int steps=12000;var times=new double[steps];var timer=new Stopwatch();int peak=0;bool allocation=CityCoreProfile.AllocationAvailable;long memory=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<steps;i++){timer.Restart();s.Advance(.05);timer.Stop();times[i]=timer.Elapsed.TotalMilliseconds;peak=Math.Max(peak,s.Transport.ActiveCount);}
        memory=allocation?GC.GetAllocatedBytesForCurrentThread()-memory:-steps;s.Validate();double mean=times.Average();Array.Sort(times);
        File.WriteAllText(args[0],"steps,population,buildings,roads,day,active_peak,mean_ms,p95_ms,p99_ms,max_ms,allocated_per_step,consumed,retail_payments\n"+string.Join(",",steps,s.City.population,s.City.buildings.Count,s.Transport.Network.RoadCount,s.City.day,peak,mean.ToString("F6",CultureInfo.InvariantCulture),times[11399].ToString("F6",CultureInfo.InvariantCulture),times[11879].ToString("F6",CultureInfo.InvariantCulture),times[11999].ToString("F6",CultureInfo.InvariantCulture),memory/steps,s.City.consumption.ConsumedGoods,s.City.consumption.RetailPayments));
        using(var sha=System.Security.Cryptography.SHA256.Create())File.WriteAllText(args[0]+".sha256",BitConverter.ToString(sha.ComputeHash(CityCoreSave.Write(s))));Console.WriteLine(File.ReadAllText(args[0]));
    }
}
