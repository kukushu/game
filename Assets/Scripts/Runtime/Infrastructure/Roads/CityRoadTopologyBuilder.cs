using System;
using System.Collections.Generic;
namespace HarborCity
{
    // Independent conversion layer, never called from traffic/rendering/construction hot paths.
    internal static class CityRoadTopologyBuilder
    {
#if UNITY_5_3_OR_NEWER && ENABLE_PROFILER
        static readonly Unity.Profiling.ProfilerMarker buildSample=new Unity.Profiling.ProfilerMarker("City.Roads.Topology.Build");
#endif
        internal static CityRoadTopology Build(CityRoads source)
        {
#if UNITY_5_3_OR_NEWER && ENABLE_PROFILER
            using var sample=buildSample.Auto();
#endif
            var nodes=new Dictionary<int,RoadNode>();var incident=new Dictionary<int,List<RoadEdge>>();
            foreach(var n in source.nodes){nodes.Add(n.id,n);incident.Add(n.id,new List<RoadEdge>());}
            var sorted=new List<RoadEdge>(source.edges);sorted.Sort((a,b)=>a.id.CompareTo(b.id));
            foreach(var e in sorted){incident[e.a].Add(e);incident[e.b].Add(e);}
            var boundaries=new Dictionary<int,LogicalRoadBoundary>();
            foreach(var n in source.nodes)
            {
                var list=incident[n.id];var reason=LogicalRoadBoundary.None;
                if(list.Count==0)reason|=LogicalRoadBoundary.Isolated;
                else if(list.Count==1)reason|=LogicalRoadBoundary.Endpoint;
                else if(list.Count>2)reason|=LogicalRoadBoundary.Branch;
                if(n.id==CityRoads.Entrance)reason|=LogicalRoadBoundary.Entrance;
                if(n.control!=JunctionControl.Automatic || n.stopFrom.Count>0)reason|=LogicalRoadBoundary.Control;
                if(list.Count==2)
                {
                    var a=list[0];var b=list[1];
                    if(a.kind!=b.kind || a.oneWay!=b.oneWay)reason|=LogicalRoadBoundary.Attributes;
                    else if(a.oneWay && (a.a==n.id)==(b.a==n.id))reason|=LogicalRoadBoundary.Direction;
                }
                if(reason!=LogicalRoadBoundary.None)boundaries.Add(n.id,reason);
            }
            var visited=new HashSet<int>();var roads=new List<LogicalRoad>();var ends=new List<int>(boundaries.Keys);ends.Sort();
            foreach(int node in ends)foreach(var e in incident[node])if(!visited.Contains(e.id))Walk(node,e);
            // Components without any boundary are closed degree-two cycles.
            foreach(var seed in sorted)if(!visited.Contains(seed.id))
            {
                int start=seed.a,current=seed.a;var e=seed;
                do
                {
                    current=Other(e,current);start=Math.Min(start,current);
                    var list=incident[current];e=list[0].id==e.id?list[1]:list[0];
                }while(current!=seed.a);
                boundaries.Add(start,LogicalRoadBoundary.CycleAnchor);Walk(start,incident[start][0]);
            }
            roads.Sort((a,b)=>a.Id.CompareTo(b.Id));return new CityRoadTopology(source,roads,boundaries);
            void Walk(int start,RoadEdge first)
            {
                int current=start,id=first.id;var edge=first;bool forward=true,reverse=true;double distance=0;
                var points=new List<RoadCenterSample>{new RoadCenterSample(nodes[start],0)};var spans=new List<LogicalRoadSpan>();
                while(true)
                {
                    if(!visited.Add(edge.id))throw new InvalidOperationException("Logical road conversion revisited an edge");
                    int next=Other(edge,current);float length=CityRoads.SpatialLength(nodes[current],nodes[next]);
                    // Valid old graphs require >= .39 world units. Reject broken inputs instead of losing geometry.
                    if(float.IsNaN(length) || float.IsInfinity(length) || length<=0)throw new ArgumentException("Invalid road span length");
                    id=Math.Min(id,edge.id);forward&=!edge.oneWay || edge.a==current;reverse&=!edge.oneWay || edge.b==current;
                    spans.Add(new LogicalRoadSpan(edge,current,next,length));distance+=length;points.Add(new RoadCenterSample(nodes[next],distance));
                    current=next;if(boundaries.ContainsKey(current))break;
                    var list=incident[current];edge=list[0].id==edge.id?list[1]:list[0];
                }
                roads.Add(new LogicalRoad(id,first.kind,forward,reverse,new RoadCenterline(points.ToArray(),spans.ToArray())));
            }
        }
        static int Other(RoadEdge edge,int node)=>edge.a==node?edge.b:edge.a;
    }
}
