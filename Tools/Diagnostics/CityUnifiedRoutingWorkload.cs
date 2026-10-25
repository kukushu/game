using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using HarborCity;
static class CityUnifiedRoutingWorkload
{
    static long Stat(object n,string name){var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;var f=n.GetType().GetField(name,flags);var p=n.GetType().GetProperty(name,flags);return f!=null?Convert.ToInt64(f.GetValue(n)):p!=null?Convert.ToInt64(p.GetValue(n)):-1;}
    static string Hash(byte[] bytes){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-","");}
    static void Mode(object network,string mode)
    {var f=network.GetType().GetField("RouteSharingEnabled",BindingFlags.Instance|BindingFlags.NonPublic);if(f!=null)f.SetValue(network,mode!="astar-no-sharing");}
    static void Main(string[] args)
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;string mode=args[2];int steps=int.Parse(args[3]);var city=CityCoreSave.Read(File.ReadAllBytes(args[0]));city.Transport.DepotLaneRoutingEnabled=true;city.Simulation.Growth.Zoning.SetSurface((x,z)=>2);Mode(city.Transport.Network,mode);
        for(int i=0;i<2200;i++)if(city.Simulation.Advance(.05,1,double.PositiveInfinity)!=1)throw new Exception("Warm step lost");city.Validate();foreach(var plant in city.City.buildings.OfType<LandfillBuilding>().OrderBy(p=>p.id)){var access=city.Transport.GetAccess(plant.id);if(access==null||!access.valid)continue;city.Transport.ReplayDepotQuery(plant.id,access,access,false);city.Transport.ReplayDepotQuery(plant.id,access,access,true);}string warm=Hash(CityCoreSave.Write(city));GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
        long search=Stat(city.Transport.Network,"PointSearches"),expanded=Stat(city.Transport.Network,"PointExpanded"),hits=Stat(city.Transport.Network,"SharedRouteHits"),misses=Stat(city.Transport.Network,"SharedRouteMisses");
        var durations=new double[steps];long allocated=GC.GetAllocatedBytesForCurrentThread();var timer=Stopwatch.StartNew();
        for(int i=0;i<steps;i++){long start=Stopwatch.GetTimestamp();if(city.Simulation.Advance(.05,1,double.PositiveInfinity)!=1)throw new Exception("Measured step lost");durations[i]=(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;}
        timer.Stop();allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;Array.Sort(durations);city.Validate();if(city.Simulation.Waste.BalanceError!=0)throw new Exception("Waste not conserved");var final=CityCoreSave.Write(city);File.WriteAllBytes(Path.Combine(args[1],"final.hcity"),final);
        File.WriteAllText(Path.Combine(args[1],"steps.csv"),"mode,steps,ms,mean_ms,p95_ms,p99_ms,max_ms,gc_bytes,gc_per_step,searches,expanded,hits,misses,entries,logical_cache_bytes,population,active,money,waste_generated,waste_collected,waste_lost,waste_balance,warm_sha256,final_sha256\n"+string.Join(",",mode,steps,timer.Elapsed.TotalMilliseconds,timer.Elapsed.TotalMilliseconds/steps,durations[(int)Math.Ceiling(steps*.95)-1],durations[(int)Math.Ceiling(steps*.99)-1],durations[steps-1],allocated,allocated/steps,search<0?-1:Stat(city.Transport.Network,"PointSearches")-search,expanded<0?-1:Stat(city.Transport.Network,"PointExpanded")-expanded,hits<0?-1:Stat(city.Transport.Network,"SharedRouteHits")-hits,misses<0?-1:Stat(city.Transport.Network,"SharedRouteMisses")-misses,Stat(city.Transport.Network,"SharedRouteCount"),Stat(city.Transport.Network,"SharedRouteBytes"),city.City.population,city.Transport.ActiveCount,city.City.money,city.Simulation.Waste.Generated,city.Simulation.Waste.Collected,Stat(city.Simulation.Waste,"Lost"),city.Simulation.Waste.BalanceError,warm,Hash(final)));Console.WriteLine(File.ReadAllText(Path.Combine(args[1],"steps.csv")));
    }
}
