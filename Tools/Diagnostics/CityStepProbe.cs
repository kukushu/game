using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using HarborCity;
using HarborCity.RoadCore;
// Bounded few-step attribution of the real captured city; never a full-city pass.
static class CityStepProbe
{
    static void Main(string[] args)
    {
        int steps=int.Parse(args[2]);if(steps<1||steps>20)throw new ArgumentOutOfRangeException();
        var s=CityCoreSave.Read(File.ReadAllBytes(args[0]));s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);s.Validate();
        using(var writer=new StreamWriter(Path.Combine(args[1],"steps.csv")))
        {
            writer.WriteLine("probe_tick,ms,allocated,probe_active,"+CityCoreProfile.Header());writer.Flush();
            for(int i=0;i<steps;i++)
            {
                Console.WriteLine("Starting actual step "+(s.Simulation.Tick+1));CityCoreProfile.Reset();CityCoreProfile.Enabled=true;
                int cg0=GC.CollectionCount(0),cg1=GC.CollectionCount(1),cg2=GC.CollectionCount(2);
                long before=GC.GetAllocatedBytesForCurrentThread(),routes=s.Transport.RoutePreparations,start=Stopwatch.GetTimestamp();
                if(s.Advance(.05)!=1)throw new Exception("Probe skipped a fixed step");
                double ms=(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;long allocated=GC.GetAllocatedBytesForCurrentThread()-before;CityCoreProfile.Enabled=false;
                writer.WriteLine(s.Simulation.Tick+","+ms.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+allocated+","+s.Transport.ActiveCount+","+CityCoreProfile.Row(s.Simulation.Tick,s.Transport.ActiveCount,s.Transport.RoutePreparations-routes,GC.CollectionCount(0)-cg0,GC.CollectionCount(1)-cg1,GC.CollectionCount(2)-cg2));writer.Flush();
                Console.WriteLine("Completed step "+s.Simulation.Tick+" ms="+ms+" route preparations="+(s.Transport.RoutePreparations-routes));
            }
        }
        s.Validate();File.WriteAllBytes(Path.Combine(args[1],"final.hcity"),CityCoreSave.Write(s));Console.WriteLine("PASS limited "+steps+"-step invariants; NOT multi-day or Unity performance acceptance");
    }
}
