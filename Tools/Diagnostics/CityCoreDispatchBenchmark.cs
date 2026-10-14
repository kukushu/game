using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using HarborCity;

public static class CityCoreDispatchBenchmark
{
    public static void Main(string[] args)
    {
        var session=CityCoreIndustrySession.Create(startAtWork:true);foreach(var p in session.City.Citizens){p.atWork=false;p.location=3;}
        var people=session.City.Citizens.ToArray();if(session.Transport.DispatchResident(people[0].id,3,1)==null)throw new Exception("Unable to create blocking resident car");
        for(int i=0;i<500;i++)if(session.Transport.DispatchResident(people[1].id,3,1)!=null)throw new Exception("Overlap allowed");
        var samples=new double[5000];var timer=new Stopwatch();long start=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<samples.Length;i++){timer.Restart();var rejected=session.Transport.DispatchResident(people[1].id,3,1);timer.Stop();samples[i]=timer.Elapsed.TotalMilliseconds;if(rejected!=null||session.Transport.ActiveCount!=1)throw new Exception("Fixture changed");}
        long allocated=GC.GetAllocatedBytesForCurrentThread()-start;double mean=samples.Average();Array.Sort(samples);session.Validate();
        string row=string.Join(",",args[1],samples.Length,session.Transport.ActiveCount,mean.ToString("F7",CultureInfo.InvariantCulture),samples[4749].ToString("F7",CultureInfo.InvariantCulture),samples[4949].ToString("F7",CultureInfo.InvariantCulture),(allocated/(double)samples.Length).ToString("F1",CultureInfo.InvariantCulture));
        File.WriteAllLines(args[0],new[]{"version,blocked_attempts,actual_active,mean_dispatch_ms,p95_ms,p99_ms,allocated_bytes_per_attempt",row});Console.WriteLine(row);
    }
}
