using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using HarborCity;
using HarborCity.RoadCore;

// Isolated retained-heap experiment, not a player FPS benchmark. The same source
// can be compiled with the previous lane-cache file through SourceOverrides.
static class CityDepotWorkingSetChecks
{
    static void Main(string[] a)
    {
        var s=CityCoreSave.Read(File.ReadAllBytes(a[0]));s.Transport.DepotLaneRoutingEnabled=true;
        var depots=s.City.buildings.OfType<LandfillBuilding>().Select(p=>s.Transport.GetAccess(p.id)).Where(p=>p!=null&&p.valid).OrderBy(p=>p.endpoint).ToArray();
        if(depots.Length<2)throw new Exception("Working-set experiment needs multiple real depots");
        var target=s.Transport.GetAccess(s.Entities.Buildings.First(b=>b.Use==LandUse.Residential).id);
        var cache=s.Transport.DepotRoutes;byte[] initial=CityCoreSave.Write(s);
        long before=GC.GetTotalMemory(true);var rows=new List<string>{"round,depots,resident_depots,tree_builds,evictions,reserved_tree_bytes,retained_heap_delta,ms"};
        for(int round=1;round<=2;round++)
        {
            long builds=cache.TreeBuilds,evictions=cache.Evictions;var clock=Stopwatch.StartNew();
            foreach(var depot in depots)
            foreach(bool returning in new[]{false,true})
            {
                var plan=s.Transport.ReplayDepotQuery(depot.endpoint,depot,target,returning);
                if(plan.route!=null){s.Transport.Network.ValidateRoute(plan.route.ToList());if(returning&&(plan.route[0]!=target.lane||plan.start!=target.distance))throw new Exception("Return moved source");}
            }
            clock.Stop();long retained=GC.GetTotalMemory(true)-before;
            if(cache.Count>DepotLaneRouting.Capacity)throw new Exception("Depot-count cap exceeded");
            if(round==2&&cache.Count==depots.Length&&(cache.TreeBuilds!=builds||cache.Evictions!=evictions))throw new Exception("Resident working set rebuilt");
            rows.Add(string.Join(",",round,depots.Length,cache.Count,cache.TreeBuilds-builds,cache.Evictions-evictions,cache.ReservedTreeBytes,retained,clock.Elapsed.TotalMilliseconds));
        }
        if(!initial.SequenceEqual(CityCoreSave.Write(s)))throw new Exception("Routing changed authoritative saved city");
        File.WriteAllLines(a[1],rows);Console.WriteLine("PASS two complete depot rounds, legal routes, unchanged city and bounded cache; "+rows.Last());
        if(a.Length>2&&a[2]=="--count-cap")
        {
            // Cache-only stress: registered building entrances are test anchors,
            // not new simulated garbage facilities or service tasks.
            var anchors=s.City.buildings.Select(b=>s.Transport.GetAccess(b.id)).Where(p=>p!=null&&p.valid).Take(DepotLaneRouting.Capacity+2).ToArray();
            if(anchors.Length<=DepotLaneRouting.Capacity)throw new Exception("Insufficient registered stress anchors");
            var stress=new DepotLaneRouting();
            foreach(var anchor in anchors)if(!stress.TryFind(s.Transport,anchor.endpoint,anchor,target,false,out _,out _,out _))throw new Exception("Cache fell back");
            if(stress.Count!=DepotLaneRouting.Capacity||stress.Evictions==0||stress.ReservedTreeBytes<=64L*1024*1024)throw new Exception("Former byte cap still prevents 128-depot retention");
            if(!initial.SequenceEqual(CityCoreSave.Write(s)))throw new Exception("Stress anchors changed city facts");
            File.WriteAllText(a[1]+".count-cap.csv","registered_test_anchors,resident_entries,reserved_tree_bytes,evictions\n"+string.Join(",",anchors.Length,stress.Count,stress.ReservedTreeBytes,stress.Evictions));
            Console.WriteLine("PASS >64MiB resident estimate, 128 count cap, eviction and unchanged authoritative city; cache-only anchors="+anchors.Length);
        }
    }
}
