using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using HarborCity.RoadCore;
namespace HarborCity
{
    public static class RecoveryAStarReplay
    {
        static readonly CultureInfo C=CultureInfo.InvariantCulture;
        static object allocationProbe;
        static bool allocations;
        static string N(double x)=>x.ToString("R",C);
        public static long Checked,Exact,SideChanges,CostBitChanges,CostDifferences,ReachabilityDifferences;
        public static double MaxCostDifference;
        public static RoadAccess Other(Network n,RoadAccess target,bool both)
        {
            var d=n.Get(target.lane);long id=Network.LaneId(d.RoadId,!d.Reverse,d.LaneIndex);
            return both&&n.Contains(id)?new RoadAccess{endpoint=target.endpoint,lane=id,distance=d.Geometry.MapReverseDistance(n.Get(id).Geometry,target.distance),valid=target.valid}:null;
        }
        public static double Cost(Network n,List<long> route,double start,double end)
        {
            if(route==null)return double.PositiveInfinity;double cost=0;
            for(int i=0;i<route.Count;i++){var d=n.Get(route[i]);cost+=((i==route.Count-1?end:d.EndDistance)-(i==0?start:d.StartDistance))/d.Speed;}return cost;
        }
        public static void CheckPair(Network n,long lane,double distance,RoadAccess goal,bool both)
        {
            var source=new RoadAccess{lane=lane,distance=distance};var other=Other(n,goal,both);
            var expected=n.RouteToEither(source,goal,other,out int selected);
            var zero=n.ExperimentalAStar(source,goal,other,out int zs,out int ze,0);
            if(selected!=zs||((expected==null)!=(zero==null))||expected!=null&&!expected.SequenceEqual(zero))throw new Exception("A* h=0 differs from canonical Dijkstra");
            var actual=n.ExperimentalAStar(source,goal,other,out int chosen,out int expanded);Checked++;
            if((expected==null)!=(actual==null)){ReachabilityDifferences++;return;}
            if(actual==null){Exact++;return;}
            n.ValidateRoute(actual);if(actual[0]!=lane||actual[actual.Count-1]!=(chosen==0?goal:other).lane)throw new Exception("A* invalid source/terminal");
            double ec=Cost(n,expected,distance,(selected==0?goal:other).distance),ac=Cost(n,actual,distance,(chosen==0?goal:other).distance);
            if(selected!=chosen)SideChanges++;if(BitConverter.DoubleToInt64Bits(ec)!=BitConverter.DoubleToInt64Bits(ac))CostBitChanges++;
            double delta=Math.Abs(ec-ac);MaxCostDifference=Math.Max(MaxCostDifference,delta);
            // This is a classification, not an equivalence assertion: report ALL
            // cost-bit/side/route differences even below the numerical cost band.
            if(delta>1e-10*Math.Max(1,Math.Abs(ec)))CostDifferences++;
            if(selected==chosen&&expected.SequenceEqual(actual))Exact++;
        }
        public static string Checks()=>"queries="+Checked+" exact="+Exact+" sideChanges="+SideChanges+" costBitChanges="+CostBitChanges+" costDifferencesAboveNumericalBand="+CostDifferences+" reachabilityDifferences="+ReachabilityDifferences+" maxCostDelta="+N(MaxCostDifference);
        sealed class Query {internal int vehicle,target;internal RoadAccess start,goal,other;internal string route;}
        public static void Run(Network n,string input,string output)
        {
            long allocationStart=GC.GetAllocatedBytesForCurrentThread();allocationProbe=new byte[4096];allocations=GC.GetAllocatedBytesForCurrentThread()-allocationStart>=4096;GC.KeepAlive(allocationProbe);
            Directory.CreateDirectory(output);var queries=File.ReadAllLines(input).Skip(1).Where(x=>x.Length>0).Select(line=>{
                var v=line.Split(',');return new Query{vehicle=int.Parse(v[0],C),target=int.Parse(v[4],C),start=new RoadAccess{lane=long.Parse(v[2],C),distance=double.Parse(v[3],C)},goal=new RoadAccess{endpoint=int.Parse(v[4],C),lane=long.Parse(v[5],C),distance=double.Parse(v[6],C)},other=long.Parse(v[8],C)==0?null:new RoadAccess{lane=long.Parse(v[8],C),distance=double.Parse(v[9],C)},route=v[20]};}).ToArray();
            long prepareBytes=GC.GetAllocatedBytesForCurrentThread(),prepareStart=Stopwatch.GetTimestamp();n.PrepareExperimentalHeuristic();double prepareMs=(Stopwatch.GetTimestamp()-prepareStart)*1000.0/Stopwatch.Frequency;prepareBytes=allocations?GC.GetAllocatedBytesForCurrentThread()-prepareBytes:-1;
            File.WriteAllText(Path.Combine(output,"heuristic-metadata.csv"),"prepare_ms,thread_gc_bytes,seam_distance\n"+N(prepareMs)+","+prepareBytes+","+N(n.ExperimentalSeams)+"\n");
            var rows=new List<string>{"vehicle,source_lane,source_junction,source_x,source_z,target,target_lane,target_distance,other_lane,expected_expanded,astar_expanded,exact_route,selected_old,selected_astar,cost_old,cost_astar,route_old,route_astar"};
            foreach(var q in queries)
            {
                DepotJunctionRouting.CountSearches=true;DepotJunctionRouting.SearchExpanded=0;var old=n.RouteToEither(q.start,q.goal,q.other,out int os);long oe=DepotJunctionRouting.SearchExpanded;DepotJunctionRouting.CountSearches=false;
                if(old==null||string.Join("/",old)!=q.route)throw new Exception("Replay does not match frozen real query "+q.vehicle);
                var actual=n.ExperimentalAStar(q.start,q.goal,q.other,out int ns,out int ne);if(actual==null)throw new Exception("Real query became unreachable");n.ValidateRoute(actual);
                CheckPair(n,q.start.lane,q.start.distance,q.goal,q.other!=null);
                var lane=(Lane)n.Get(q.start.lane);var p=lane.Exit.Position;
                rows.Add(string.Join(",",q.vehicle,q.start.lane,lane.To,N(p.x),N(p.z),q.target,q.goal.lane,N(q.goal.distance),q.other?.lane??0,oe,ne,old.SequenceEqual(actual),os,ns,N(Cost(n,old,q.start.distance,(os==0?q.goal:q.other).distance)),N(Cost(n,actual,q.start.distance,(ns==0?q.goal:q.other).distance)),string.Join("/",old),string.Join("/",actual)));
            }
            File.WriteAllLines(Path.Combine(output,"queries.csv"),rows);
            // Alternating paired, warm replay in the same process. NOT frame/FPS
            // measurements, not actual recovery commits, no per-node tracing.
            var times=new List<string>{"round,algorithm,queries,ms,thread_gc_bytes"};
            for(int round=0;round<24;round++)foreach(bool astar in round%2==0?new[]{false,true}:new[]{true,false})
            {
                long bytes=GC.GetAllocatedBytesForCurrentThread(),began=Stopwatch.GetTimestamp();
                foreach(var q in queries){if(astar)n.ExperimentalAStar(q.start,q.goal,q.other,out int selected,out int expanded);else n.RouteToEither(q.start,q.goal,q.other,out int selected);}
                double ms=(Stopwatch.GetTimestamp()-began)*1000.0/Stopwatch.Frequency;bytes=allocations?GC.GetAllocatedBytesForCurrentThread()-bytes:-1;times.Add(string.Join(",",round,astar?"astar":"dijkstra",queries.Length,N(ms),bytes));
            }
            File.WriteAllLines(Path.Combine(output,"timings.csv"),times);File.WriteAllText(Path.Combine(output,"checks.txt"),Checks()+"\nseam_distance="+N(n.ExperimentalSeams)+"\n");
            // Separate query distribution pass, so per-query clock calls do not
            // inflate the paired 17-query totals above.
            var perQuery=new List<string>{"round,vehicle,algorithm,ms,thread_gc_bytes"};
            for(int round=0;round<24;round++)foreach(var q in queries)foreach(bool astar in round%2==0?new[]{false,true}:new[]{true,false})
            {
                long bytes=GC.GetAllocatedBytesForCurrentThread(),began=Stopwatch.GetTimestamp();
                if(astar)n.ExperimentalAStar(q.start,q.goal,q.other,out int selected,out int expanded);else n.RouteToEither(q.start,q.goal,q.other,out int selected);
                double ms=(Stopwatch.GetTimestamp()-began)*1000.0/Stopwatch.Frequency;bytes=allocations?GC.GetAllocatedBytesForCurrentThread()-bytes:-1;perQuery.Add(string.Join(",",round,q.vehicle,astar?"astar":"dijkstra",N(ms),bytes));
            }
            File.WriteAllLines(Path.Combine(output,"query-timings.csv"),perQuery);
        }
    }
}
