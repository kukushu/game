using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using HarborCity;
// Same entry compiled against frozen B/C or current D model sources using the
// existing SourceOverrides host. Reflection only chooses the experimental flag
// before timing; all measured steps are the real CitySimulation.
static class CityDepotRoutingPairedSteps
{
    static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");}
    static void Main(string[] a)
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
        var city=CityCoreSave.Read(File.ReadAllBytes(a[0]));city.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
        for(int i=0;i<200;i++)if(city.Simulation.Advance(.05,1,double.PositiveInfinity)!=1)throw new Exception("Warm step lost");
        string warm=Hash(CityCoreSave.Write(city));
        if(a[2]!="A")city.Transport.GetType().GetProperty(a[2]=="B"?"DepotLaneRoutingEnabled":"DepotJunctionRoutingEnabled").SetValue(city.Transport,true);
        long bytes=GC.GetAllocatedBytesForCurrentThread();var clock=Stopwatch.StartNew();
        for(int i=0;i<4000;i++)if(city.Simulation.Advance(.05,1,double.PositiveInfinity)!=1)throw new Exception("Step lost");
        clock.Stop();long allocation=GC.GetAllocatedBytesForCurrentThread()-bytes;city.Validate();var w=city.Simulation.Waste;
        if(w.BalanceError!=0)throw new Exception("Waste conservation failed");
        File.WriteAllText(a[1],"mode,warm_sha256,steps,ms,gc_bytes,population,money,active,waste_vehicles,generated,collected,in_transit,stored,processed,balance_error,sha256\n"+string.Join(",",a[2],warm,4000,clock.Elapsed.TotalMilliseconds,allocation,city.City.population,city.City.money,city.Transport.ActiveCount,w.Vehicles,w.Generated,w.Collected,w.InTransit,w.Stored,w.Processed,w.BalanceError,Hash(CityCoreSave.Write(city))));
        Console.WriteLine("PASS warmed "+a[2]+" 4000 actual steps: "+clock.Elapsed.TotalMilliseconds+" ms");
    }
}
