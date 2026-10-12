#if UNITY_5_3_OR_NEWER && ENABLE_PROFILER && CITY_TRAFFIC_ATTRIBUTION && !CITY_TRAFFIC_PROFILING_DISABLED
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;

namespace HarborCity
{
    // Opt-in diagnostics only. No query uses these observations to make decisions.
    internal static class CityTrafficAttribution
    {
        internal static readonly ProfilerMarker junction=new ProfilerMarker("City.Traffic.Static.IsJunction");
        internal static readonly ProfilerMarker distance=new ProfilerMarker("City.Traffic.Route.DistanceTo");
        internal static readonly ProfilerMarker routeNodeIndex=new ProfilerMarker("City.Traffic.Route.NodeIndex");
        internal static readonly ProfilerMarker movement=new ProfilerMarker("City.Traffic.Junction.MovementInfo");
        internal static readonly ProfilerMarker mixed=new ProfilerMarker("City.Traffic.Junction.MixedConflict");
        internal static readonly ProfilerMarker tripLane=new ProfilerMarker("City.Traffic.Route.TripLane");
        internal static readonly ProfilerMarker laneEligibility=new ProfilerMarker("City.Traffic.ChooseLane.Eligibility");
        internal static readonly ProfilerMarker laneLookBack=new ProfilerMarker("City.Traffic.ChooseLane.LookBack");
        internal static readonly ProfilerMarker laneLookAhead=new ProfilerMarker("City.Traffic.ChooseLane.LookAhead");
        internal static readonly ProfilerMarker crossingGate=new ProfilerMarker("City.Traffic.Crossing.DynamicGate");
        internal static readonly ProfilerMarker validNodes=new ProfilerMarker("City.Traffic.RouteValidation.Nodes");
        internal static readonly ProfilerMarker validEdges=new ProfilerMarker("City.Traffic.RouteValidation.Edges");
        internal static readonly ProfilerMarker validAccess=new ProfilerMarker("City.Traffic.RouteValidation.Access");
        internal struct Work
        {
            public int junctionCalls,uniqueJunctions,distanceCalls,distanceEdges,edgeLengths,tripLaneCalls,tripLaneEdges,validationNodes,validationEdges,validationAccess,movementRecords;
            public const string Header="junction_calls,unique_junction_nodes,distance_calls,distance_summed_edges,edge_length_calls,trip_lane_calls,trip_lane_forward_edges,validation_nodes,validation_edges,validation_access,movement_records";
            public void Append(System.Text.StringBuilder b)
            {b.Append(junctionCalls).Append(',').Append(uniqueJunctions).Append(',').Append(distanceCalls).Append(',').Append(distanceEdges).Append(',').Append(edgeLengths).Append(',').Append(tripLaneCalls).Append(',').Append(tripLaneEdges).Append(',').Append(validationNodes).Append(',').Append(validationEdges).Append(',').Append(validationAccess).Append(',').Append(movementRecords);}
        }
        internal static Work Current;
        static CityRoads graph;
        static int revision;
        static HashSet<int> seen;
        internal static void Begin(CityRoads roads)
        {
            Current=default;
            if(graph!=roads || revision!=roads.revision)
            {seen=new HashSet<int>(roads.nodes.Select(n=>n.id));graph=roads;revision=roads.revision;}
            seen.Clear();
        }
        internal static void NoteJunction(int node)
        {Current.junctionCalls++;if(seen!=null && seen.Add(node))Current.uniqueJunctions++;}
    }
}
#endif
