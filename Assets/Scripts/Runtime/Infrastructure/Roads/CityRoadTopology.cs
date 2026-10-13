using System;
using System.Collections.Generic;
namespace HarborCity
{
    [Flags] public enum LogicalRoadBoundary
    {None=0,Endpoint=1,Branch=2,Entrance=4,Control=8,Attributes=16,Direction=32,CycleAnchor=64,Isolated=128}
    public sealed class LogicalRoad
    {
        // Deterministic within a snapshot. Not a persistent entity ID across edits.
        public int Id {get;}
        public int From {get;}
        public int To {get;}
        public RoadKind Kind {get;}
        public bool ForwardAllowed {get;}
        public bool ReverseAllowed {get;}
        public int Lanes=>CityRoadTypes.Lanes(Kind,!ForwardAllowed || !ReverseAllowed);
        public RoadCenterline Geometry {get;}
        internal LogicalRoad(int id,RoadKind kind,bool forward,bool reverse,RoadCenterline geometry)
        {Id=id;Kind=kind;ForwardAllowed=forward;ReverseAllowed=reverse;Geometry=geometry;From=geometry.Samples[0].NodeId;To=geometry.Samples[geometry.Samples.Count-1].NodeId;}
    }
    public readonly struct LogicalRoadArc
    {
        public readonly LogicalRoad Road;
        public readonly bool Forward;
        public int From=>Forward?Road.From:Road.To;
        public int To=>Forward?Road.To:Road.From;
        public bool VehicleAllowed=>Forward?Road.ForwardAllowed:Road.ReverseAllowed;
        internal LogicalRoadArc(LogicalRoad road,bool forward){Road=road;Forward=forward;}
    }
    internal readonly struct LogicalSpanReference
    {
        internal readonly LogicalRoad Road;
        internal readonly int Index;
        internal LogicalSpanReference(LogicalRoad road,int index){Road=road;Index=index;}
    }
    // Directed view into a geometry span, not a second mutable RoadEdge.
    public readonly struct RoadTraversal
    {
        public readonly LogicalRoad Road;
        public readonly LogicalRoadSpan Span;
        public readonly RoadCenterSample From,To;
        public readonly bool VehicleAllowed;
        public bool OneWay=>!Road.ForwardAllowed || !Road.ReverseAllowed;
        public int LaneCount=>VehicleAllowed?Road.Lanes:0;
        internal RoadTraversal(LogicalSpanReference reference,int from)
        {
            Road=reference.Road;Span=Road.Geometry.Spans[reference.Index];
            bool forward=Span.From==from;
            From=Road.Geometry.Samples[reference.Index+(forward?0:1)];
            To=Road.Geometry.Samples[reference.Index+(forward?1:0)];
            VehicleAllowed=forward?Road.ForwardAllowed:Road.ReverseAllowed;
        }
    }
    // A read-only revision snapshot. Only CityRoads is authoritative and serializable.
    public sealed class CityRoadTopology
    {
        readonly CityRoads source;
        readonly Dictionary<int,LogicalRoad> roadsById=new Dictionary<int,LogicalRoad>();
        readonly Dictionary<int,LogicalSpanReference> edges=new Dictionary<int,LogicalSpanReference>();
        readonly Dictionary<long,LogicalSpanReference> directedSpans=new Dictionary<long,LogicalSpanReference>(CityRoadKeyComparer.Instance);
        readonly Dictionary<int,IReadOnlyList<LogicalRoadArc>> outgoing=new Dictionary<int,IReadOnlyList<LogicalRoadArc>>();
        readonly Dictionary<int,IReadOnlyList<LogicalRoadPosition>> positions=new Dictionary<int,IReadOnlyList<LogicalRoadPosition>>();
        readonly Dictionary<int,LogicalRoadBoundary> boundaries;
        static readonly IReadOnlyList<LogicalRoadArc> noArcs=Array.AsReadOnly(Array.Empty<LogicalRoadArc>());
        static readonly IReadOnlyList<LogicalRoadPosition> noPositions=Array.AsReadOnly(Array.Empty<LogicalRoadPosition>());
        public int Revision {get;}
        public IReadOnlyList<LogicalRoad> Roads {get;}
        public int LegacyEdgeCount=>edges.Count;
        internal CityRoadTopology(CityRoads source,List<LogicalRoad> roads,Dictionary<int,LogicalRoadBoundary> boundaries)
        {
            this.source=source;Revision=source.revision;this.boundaries=boundaries;Roads=roads.AsReadOnly();
            var arcs=new Dictionary<int,List<LogicalRoadArc>>();var nodePositions=new Dictionary<int,List<LogicalRoadPosition>>();
            foreach(var road in roads)
            {
                roadsById.Add(road.Id,road);AddArc(new LogicalRoadArc(road,true));AddArc(new LogicalRoadArc(road,false));
                for(int i=0;i<road.Geometry.Spans.Count;i++)
                {
                    var span=road.Geometry.Spans[i];var reference=new LogicalSpanReference(road,i);
                    edges.Add(span.EdgeId,reference);directedSpans.Add(Key(span.From,span.To),reference);directedSpans.Add(Key(span.To,span.From),reference);
                }
                for(int i=0;i<road.Geometry.Samples.Count;i++)
                {
                    var p=road.Geometry.Samples[i];if(!nodePositions.TryGetValue(p.NodeId,out var list))nodePositions[p.NodeId]=list=new List<LogicalRoadPosition>();
                    list.Add(new LogicalRoadPosition(road,Math.Min(i,road.Geometry.Spans.Count-1),true,p.Distance));
                }
            }
            foreach(var p in arcs)outgoing.Add(p.Key,p.Value.AsReadOnly());
            foreach(var p in nodePositions)positions.Add(p.Key,p.Value.AsReadOnly());
            void AddArc(LogicalRoadArc arc){if(!arcs.TryGetValue(arc.From,out var list))arcs[arc.From]=list=new List<LogicalRoadArc>();list.Add(arc);}
        }
        static long Key(int from,int to)=>((long)from<<32)|(uint)to;
        public bool IsCurrent(CityRoads roads)=>ReferenceEquals(source,roads) && roads.revision==Revision;
        public LogicalRoadBoundary BoundaryAt(int node)=>boundaries.TryGetValue(node,out var reason)?reason:LogicalRoadBoundary.None;
        // Arcs include forbidden vehicle directions so walking topology stays explicit.
        public IReadOnlyList<LogicalRoadArc> ArcsFrom(int node)=>outgoing.TryGetValue(node,out var arcs)?arcs:noArcs;
        public IReadOnlyList<LogicalRoadPosition> PositionsAtNode(int node)=>positions.TryGetValue(node,out var p)?p:noPositions;
        public bool TryGetEdge(int edgeId,out LogicalRoad road,out int span)
        {if(edges.TryGetValue(edgeId,out var r)){road=r.Road;span=r.Index;return true;}road=null;span=-1;return false;}
        public bool CanTravel(int from,int to,bool walking=false)
        {
            if(!directedSpans.TryGetValue(Key(from,to),out var r))return false;
            return walking?CityRoadTypes.Frontage(r.Road.Kind):r.Road.Geometry.Spans[r.Index].From==from?r.Road.ForwardAllowed:r.Road.ReverseAllowed;
        }
        public bool TryGetTraversal(int from,int to,out RoadTraversal traversal)
        {
            if(directedSpans.TryGetValue(Key(from,to),out var reference))
            {traversal=new RoadTraversal(reference,from);return true;}
            traversal=default;return false;
        }
        // Hot traffic queries do not copy the full geometry view.
        public RoadKind KindBetween(int from,int to)=>directedSpans.TryGetValue(Key(from,to),out var r)?r.Road.Kind:RoadKind.Standard;
        public bool TryGetLength(int from,int to,out float length)
        {if(directedSpans.TryGetValue(Key(from,to),out var r)){length=r.Road.Geometry.Spans[r.Index].Length;return true;}length=0;return false;}
        public int LaneCountBetween(int from,int to)
        {
            if(!directedSpans.TryGetValue(Key(from,to),out var r))return 0;
            bool forward=r.Road.Geometry.Spans[r.Index].From==from;
            return (forward?r.Road.ForwardAllowed:r.Road.ReverseAllowed)?r.Road.Lanes:0;
        }
        public float LaneOffsetBetween(int from,int to,int lane)
        {return directedSpans.TryGetValue(Key(from,to),out var r)?CityRoadTypes.LaneOffset(r.Road.Kind,!r.Road.ForwardAllowed || !r.Road.ReverseAllowed,lane):.55f;}
        // Geometry mapping is valid even for temporarily stranded/illegal-direction trips.
        // Call CanTravel separately; mapping must not silently discard those vehicles.
        public bool TryMapLegacy(int from,int to,float progress,out LogicalRoadPosition position)
        {
            position=default;if(float.IsNaN(progress) || float.IsInfinity(progress) || progress<0 || progress>1 || !directedSpans.TryGetValue(Key(from,to),out var r))return false;
            var span=r.Road.Geometry.Spans[r.Index];bool forward=from==span.From;
            position=new LogicalRoadPosition(r.Road,r.Index,forward,r.Road.Geometry.Samples[r.Index].Distance+(forward?(double)progress:1-(double)progress)*span.Length);return true;
        }
        public LogicalRoadPosition PositionAt(LogicalRoad road,double distance,bool forward)
        {
            if(road==null || !roadsById.TryGetValue(road.Id,out var owned) || !ReferenceEquals(road,owned))throw new ArgumentException("Road belongs to another topology snapshot");
            return new LogicalRoadPosition(road,road.Geometry.SpanAt(distance),forward,distance);
        }
        public bool TryToLegacy(LogicalRoadPosition position,out int from,out int to,out float progress)
        {
            from=to=-1;progress=0;var road=position.Road;
            if(road==null || !roadsById.TryGetValue(road.Id,out var owned) || !ReferenceEquals(road,owned) || position.SpanIndex<0 || position.SpanIndex>=road.Geometry.Spans.Count)return false;
            var span=road.Geometry.Spans[position.SpanIndex];double local=(position.Distance-road.Geometry.Samples[position.SpanIndex].Distance)/span.Length;
            if(double.IsNaN(local) || double.IsInfinity(local) || local<-.00000001 || local>1.00000001)return false;
            from=position.Forward?span.From:span.To;to=position.Forward?span.To:span.From;progress=(float)Math.Max(0,Math.Min(1,position.Forward?local:1-local));return true;
        }
    }
}
