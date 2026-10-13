using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace HarborCity
{
    // Search vertices are road boundaries and caller endpoints. Geometry samples
    // contribute their original float costs, but are not priority-queue vertices.
    internal static class CityLogicalRoadRouter
    {
        sealed class Parent
        {
            internal int Head;
            internal List<int> Samples;
        }
#if UNITY_5_3_OR_NEWER && ENABLE_PROFILER
        static readonly Unity.Profiling.ProfilerMarker searchSample=new Unity.Profiling.ProfilerMarker("City.Roads.Route.Logical");
        static readonly Unity.Profiling.ProfilerMarker fallbackSample=new Unity.Profiling.ProfilerMarker("City.Roads.Route.LegacyFallback");
#endif
        // Null requests the legacy oracle for ties/float degeneracy; empty means unreachable.
        internal static List<int> Search(CityRoads roads,List<int> starts,List<int> goals,bool walking)
        {
#if UNITY_5_3_OR_NEWER && ENABLE_PROFILER
            using var sample=searchSample.Auto();
#endif
            var empty=new List<int>();if(starts.Count==0 || goals.Count==0)return empty;
            var topology=roads.GetLogicalTopology();var cuts=new HashSet<int>(starts);cuts.UnionWith(goals);
            var distance=new Dictionary<int,float>();var parents=new Dictionary<int,Parent>();
            var open=new SortedSet<(float cost,int id)>();
            foreach(int start in starts)if(roads.Active(start)){distance[start]=0;parents[start]=null;open.Add((0,start));}
            while(open.Count>0)
            {
                var current=open.Min;open.Remove(current);
                if(goals.Contains(current.id))
                {
                    var path=new List<int>();int node=current.id;
                    while(parents[node]!=null)
                    {
                        var parent=parents[node];for(int i=parent.Samples.Count-1;i>0;i--)path.Add(parent.Samples[i]);node=parent.Head;
                    }
                    path.Add(node);path.Reverse();return path;
                }
                foreach(var position in topology.PositionsAtNode(current.id))
                {
                    var road=position.Road;var geometry=road.Geometry;
                    if(walking && !CityRoadTypes.Frontage(road.Kind))continue;
                    int begin=position.SpanIndex+(position.Distance==geometry.Length?1:0);
                    foreach(int direction in directions)
                    {
                        if(!walking && !(direction==1?road.ForwardAllowed:road.ReverseAllowed))continue;
                        if(begin+direction<0 || begin+direction>=geometry.Samples.Count)continue;
                        float cost=current.cost;int previous=current.id;
                        var samples=new List<int>{current.id};
                        for(int index=begin+direction;index>=0 && index<geometry.Samples.Count;index+=direction)
                        {
                            // Do not sum a corridor first: float addition order is observable.
                            int next=geometry.Samples[index].NodeId;
                            float nextCost=RoundFloat(cost+roads.TravelCost(previous,next,walking)/3.6f);if(nextCost<=cost)return null;
                            cost=nextCost;samples.Add(next);
                            if(cuts.Contains(next) || topology.BoundaryAt(next)!=LogicalRoadBoundary.None)
                            {
                                if(distance.TryGetValue(next,out float old))
                                {
                                    if(old<cost)break;
                                    if(old==cost)
                                    {
                                        // Queue compression omits intermediate predecessor
                                        // ordering. Preserve the exact old choice conservatively.
                                        return null;
                                    }
                                    open.Remove((old,next));
                                }
                                distance[next]=cost;parents[next]=new Parent{Head=current.id,Samples=samples};open.Add((cost,next));break;
                            }
                            previous=next;
                        }
                    }
                }
            }
            return empty;
        }
        internal static List<int> Fallback(CityRoads roads,List<int> starts,List<int> goals,bool walking)
        {
#if UNITY_5_3_OR_NEWER && ENABLE_PROFILER
            using var sample=fallbackSample.Auto();
#endif
            return roads.CalculateLegacyPath(starts,goals,walking);
        }
        static readonly int[] directions={1,-1};
        [StructLayout(LayoutKind.Explicit)] struct FloatBits
        { [FieldOffset(0)] internal float Value; [FieldOffset(0)] internal int Bits; }
        // Old Dijkstra stores every sample cost in its float queue/dictionary.
        // Force that same 32-bit boundary without allocating or queueing samples.
        static float RoundFloat(float value)
        {var stored=new FloatBits{Value=value};return new FloatBits{Bits=stored.Bits}.Value;}
    }
}
