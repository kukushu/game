using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
#if UNITY_5_3_OR_NEWER
using Unity.Profiling;
#endif
namespace HarborCity.RoadCore
{
    // Opt-in observational instrumentation; single main simulation thread only.
    // Never used to choose a route, reduce work or change simulation state.
    public static class CityCoreProfile
    {
        public enum QueryKind {CommuteEstimate,Blueprint,Evicted,Reset,CostQuote,Materialized}
        public enum QueryResult {Hit,Absent,EndpointsChanged,NetworkChanged,AccessChanged,Miss}
        public readonly struct RouteQuery
        {
            public readonly QueryKind Kind;public readonly QueryResult Result;public readonly int Citizen,Origin,Destination;public readonly bool BothDirections;public readonly Part Context;
            internal RouteQuery(QueryKind kind,QueryResult result,int citizen,int origin,int destination,bool bothDirections,Part context){Kind=kind;Result=result;Citizen=citizen;Origin=origin;Destination=destination;BothDirections=bothDirections;Context=context;}
        }
        // Optional bounded diagnostic observer. Never used by production decisions.
        public static Action<RouteQuery> QueryObserver;
        public static void ObserveQuery(QueryKind kind,QueryResult result,int citizen,int origin,int destination,bool bothDirections=true)
        {
            if(!Enabled||QueryObserver==null)return;
            Part context=depth>0?(Part)stack[depth-1].part:Part.Step;
            if(context==Part.RouteLookup&&depth>1)context=(Part)stack[depth-2].part;
            QueryObserver(new RouteQuery(kind,result,citizen,origin,destination,bothDirections,context));
        }
        public enum Part {Step,Residents,Job,Production,Logistics,Dispatch,RouteLookup,RouteSearch,Admission,Occupancy,Movement,Settlement,Growth,Utilities,Population,Consumption,Trade,Payroll,ZoningRebuild,GrowthSearch,Placement,Finance,FinanceAccrual,Waste,ZoningGeometry,ZoningLandUse,ZoningOccupancy,UtilityRebuild,UtilityPowerCoverage,UtilityWaterCoverage,CommuteEstimate,CostSearch}
        const int PartCount=32;
        public static bool Enabled;
        public static bool AllocationAvailable {get;}=ProbeAllocationCounter();
        static bool ProbeAllocationCounter(){long before=GC.GetAllocatedBytesForCurrentThread();var probe=new byte[128];GC.KeepAlive(probe);return GC.GetAllocatedBytesForCurrentThread()>before;}
        public static long CommuteEstimateHits,CommuteEstimateMisses;
        public static long Unreachable,SourceBlocked,CacheFlushes,CacheHits,CacheEvictions,ZoningCandidatePairs,ZoningExactOverlaps;
        public static readonly long[] Calls=new long[PartCount],Ticks=new long[PartCount],SelfTicks=new long[PartCount],Bytes=new long[PartCount],SelfBytes=new long[PartCount];
        struct Frame {public int part;public long start,bytes,children,childBytes;}
        static readonly Frame[] stack=new Frame[64];static int depth;
#if UNITY_5_3_OR_NEWER
        static readonly ProfilerMarker[] markers={new ProfilerMarker("City.Attribution.Step"),new ProfilerMarker("City.Attribution.Residents"),new ProfilerMarker("City.Attribution.Job"),new ProfilerMarker("City.Attribution.Production"),new ProfilerMarker("City.Attribution.Logistics"),new ProfilerMarker("City.Attribution.Dispatch"),new ProfilerMarker("City.Attribution.RouteLookup"),new ProfilerMarker("City.Attribution.RouteSearch"),new ProfilerMarker("City.Attribution.Admission"),new ProfilerMarker("City.Attribution.Occupancy"),new ProfilerMarker("City.Attribution.Movement"),new ProfilerMarker("City.Attribution.Settlement"),new ProfilerMarker("City.Attribution.Growth"),new ProfilerMarker("City.Attribution.Utilities"),new ProfilerMarker("City.Attribution.Population"),new ProfilerMarker("City.Attribution.Consumption"),new ProfilerMarker("City.Attribution.Trade"),new ProfilerMarker("City.Attribution.Payroll"),new ProfilerMarker("City.Attribution.ZoningRebuild"),new ProfilerMarker("City.Attribution.GrowthSearch"),new ProfilerMarker("City.Attribution.Placement"),new ProfilerMarker("City.Attribution.Finance"),new ProfilerMarker("City.Attribution.FinanceAccrual"),new ProfilerMarker("City.Attribution.Waste"),new ProfilerMarker("City.Attribution.ZoningGeometry"),new ProfilerMarker("City.Attribution.ZoningLandUse"),new ProfilerMarker("City.Attribution.ZoningOccupancy"),new ProfilerMarker("City.Attribution.UtilityRebuild"),new ProfilerMarker("City.Attribution.UtilityPowerCoverage"),new ProfilerMarker("City.Attribution.UtilityWaterCoverage"),new ProfilerMarker("City.Attribution.CommuteEstimate"),new ProfilerMarker("City.Attribution.CostSearch")};
#endif
        public readonly struct Scope:IDisposable
        {
            readonly bool active;readonly int slot;
#if UNITY_5_3_OR_NEWER
            readonly ProfilerMarker.AutoScope unity;
#endif
            internal Scope(Part part)
            {
                active=Enabled;slot=-1;
#if UNITY_5_3_OR_NEWER
                unity=default;
#endif
                if(!active)return;if(depth>=stack.Length)throw new InvalidOperationException("Profile scope overflow");slot=depth++;stack[slot]=new Frame{part=(int)part,start=Stopwatch.GetTimestamp(),bytes=GC.GetAllocatedBytesForCurrentThread()};
#if UNITY_5_3_OR_NEWER
                unity=markers[(int)part].Auto();
#endif
            }
            public void Dispose()
            {
                if(!active)return;
#if UNITY_5_3_OR_NEWER
                unity.Dispose();
#endif
                if(depth-1!=slot)throw new InvalidOperationException("Unbalanced profile scopes");var f=stack[slot];depth--;long time=Stopwatch.GetTimestamp()-f.start,allocated=GC.GetAllocatedBytesForCurrentThread()-f.bytes;Calls[f.part]++;Ticks[f.part]+=time;SelfTicks[f.part]+=time-f.children;if(AllocationAvailable){Bytes[f.part]+=allocated;SelfBytes[f.part]+=allocated-f.childBytes;}else{Bytes[f.part]=SelfBytes[f.part]=-1;}if(depth>0){stack[depth-1].children+=time;stack[depth-1].childBytes+=allocated;}
            }
        }
        public static Scope Measure(Part part)=>new Scope(part);
        public static void Reset(){if(depth!=0)throw new InvalidOperationException("Cannot reset nested sample");CommuteEstimateHits=CommuteEstimateMisses=Unreachable=SourceBlocked=CacheFlushes=CacheHits=CacheEvictions=ZoningCandidatePairs=ZoningExactOverlaps=0;Array.Clear(Calls,0,PartCount);Array.Clear(Ticks,0,PartCount);Array.Clear(SelfTicks,0,PartCount);Array.Clear(Bytes,0,PartCount);Array.Clear(SelfBytes,0,PartCount);if(!AllocationAvailable)for(int i=0;i<PartCount;i++)Bytes[i]=SelfBytes[i]=-1;}
        public static string Header()
        {var b=new StringBuilder("tick,active,route_preparations,gc0,gc1,gc2,unreachable,source_blocked,cache_flushes,cache_hits,cache_evictions,commute_estimate_hits,commute_estimate_misses,allocation_counter_supported,zoning_candidate_pairs,zoning_exact_overlaps");foreach(Part p in Enum.GetValues(typeof(Part)))b.Append(',').Append(p).Append("_ms,").Append(p).Append("_self_ms,").Append(p).Append("_calls,").Append(p).Append("_bytes,").Append(p).Append("_self_bytes");return b.ToString();}
        public static string Row(long tick,int active,long routes,int gc0,int gc1,int gc2)
        {var b=new StringBuilder();b.Append(tick).Append(',').Append(active).Append(',').Append(routes).Append(',').Append(gc0).Append(',').Append(gc1).Append(',').Append(gc2).Append(',').Append(Unreachable).Append(',').Append(SourceBlocked).Append(',').Append(CacheFlushes).Append(',').Append(CacheHits).Append(',').Append(CacheEvictions).Append(',').Append(CommuteEstimateHits).Append(',').Append(CommuteEstimateMisses).Append(',').Append(AllocationAvailable?1:0).Append(',').Append(ZoningCandidatePairs).Append(',').Append(ZoningExactOverlaps);for(int i=0;i<PartCount;i++)b.Append(',').Append((Ticks[i]*1000.0/Stopwatch.Frequency).ToString("F6",CultureInfo.InvariantCulture)).Append(',').Append((SelfTicks[i]*1000.0/Stopwatch.Frequency).ToString("F6",CultureInfo.InvariantCulture)).Append(',').Append(Calls[i]).Append(',').Append(Bytes[i]).Append(',').Append(SelfBytes[i]);return b.ToString();}
    }
}
