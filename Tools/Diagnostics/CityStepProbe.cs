using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using HarborCity;
using HarborCity.RoadCore;
// Bounded captured-city attribution; reports only the actual tested interval.
static class CityStepProbe
{
    static void InspectCommuteCache(CityCoreIndustrySession session,string output)
    {
        var cacheField=typeof(CityCoreResidents).GetField("estimates",BindingFlags.Instance|BindingFlags.NonPublic);
        if(cacheField==null)throw new Exception("Diagnostic cache field changed; do not infer miss reasons");
        var cache=(IDictionary)cacheField.GetValue(session.Residents);Type item=cacheField.FieldType.GetGenericArguments()[1];
        var fields=new[]{"home","work","network","access"}.Select(n=>item.GetField(n,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)).ToArray();
        if(fields.Any(f=>f==null))throw new Exception("Diagnostic cache key fields changed");
        int eligible=0,absent=0,endpoints=0,network=0,access=0,hit=0;
        foreach(var member in session.Entities.Members)
        {
            var p=member.Person;var h=member.Family;var city=session.City;
            if(!h.resident||p.dead||p.tripId!=0||session.Transport.Clock<p.retryAt||session.Transport.Clock<p.visitUntil||p.atWork||p.location!=h.home||p.departureDay==city.day||!city.CanAttendWork(p)||!city.JobFunded(city.ResidentJob(p))||!session.Transport.HasResidentAccess(h.home,city.Workplace(p)))continue;
            eligible++;object value=cache[p.id];if(value==null){absent++;continue;}
            if((int)fields[0].GetValue(value)!=h.home||(int)fields[1].GetValue(value)!=city.Workplace(p)){endpoints++;continue;}
            if((int)fields[2].GetValue(value)!=session.Transport.NetworkGeneration){network++;continue;}
            if((long)fields[3].GetValue(value)!=session.Transport.AccessRevision){access++;continue;}hit++;
        }
        // Cold diagnostic only; no estimate getter, dispatch or state mutation.
        File.WriteAllText(Path.Combine(output,"commute-cache-before.json"),"{\"tick\":"+session.Simulation.Tick+",\"eligible_before_population_boundary\":"+eligible+",\"cache_entries\":"+cache.Count+",\"absent\":"+absent+",\"changed_endpoints\":"+endpoints+",\"changed_network\":"+network+",\"changed_access\":"+access+",\"hit\":"+hit+"}");
    }
    static void Main(string[] args)
    {
        int steps=int.Parse(args[2]);if(steps<1||steps>10000)throw new ArgumentOutOfRangeException();
        var s=CityCoreSave.Read(File.ReadAllBytes(args[0]));s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);s.Validate();
        int warmup=args.Length>3?int.Parse(args[3]):0;if(warmup<0||warmup>1000)throw new ArgumentOutOfRangeException("warmup");
        for(int i=0;i<warmup;i++){if(s.Advance(.05)!=1)throw new Exception("Warm replay skipped a fixed step");if((i+1)%120==0)Console.WriteLine("Normal warm replay "+(i+1)+" tick="+s.Simulation.Tick);}
        if(warmup>0)File.WriteAllBytes(Path.Combine(args[1],"warm-boundary.hcity"),CityCoreSave.Write(s));
        if(args.Length>4&&args[4]=="1")InspectCommuteCache(s,args[1]);
        using(var writer=new StreamWriter(Path.Combine(args[1],"steps.csv")))
        {
            writer.WriteLine("probe_tick,ms,allocated,probe_active,"+CityCoreProfile.Header());writer.Flush();
            long progressAt=Stopwatch.GetTimestamp();for(int i=0;i<steps;i++)
            {
                CityCoreProfile.Reset();CityCoreProfile.Enabled=true;
                int cg0=GC.CollectionCount(0),cg1=GC.CollectionCount(1),cg2=GC.CollectionCount(2);
                long before=GC.GetAllocatedBytesForCurrentThread(),routes=s.Transport.RoutePreparations,start=Stopwatch.GetTimestamp();
                if(s.Advance(.05)!=1)throw new Exception("Probe skipped a fixed step");
                double ms=(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;long allocated=GC.GetAllocatedBytesForCurrentThread()-before;CityCoreProfile.Enabled=false;
                writer.WriteLine(s.Simulation.Tick+","+ms.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+allocated+","+s.Transport.ActiveCount+","+CityCoreProfile.Row(s.Simulation.Tick,s.Transport.ActiveCount,s.Transport.RoutePreparations-routes,GC.CollectionCount(0)-cg0,GC.CollectionCount(1)-cg1,GC.CollectionCount(2)-cg2));writer.Flush();
                if(steps<=20||Stopwatch.GetTimestamp()-progressAt>10L*Stopwatch.Frequency){Console.WriteLine("Completed step "+s.Simulation.Tick+" ms="+ms+" route preparations="+(s.Transport.RoutePreparations-routes));progressAt=Stopwatch.GetTimestamp();}
            }
        }
        s.Validate();File.WriteAllBytes(Path.Combine(args[1],"final.hcity"),CityCoreSave.Write(s));Console.WriteLine("PASS limited "+steps+"-step invariants; not economic-health or Unity performance acceptance");
    }
}
