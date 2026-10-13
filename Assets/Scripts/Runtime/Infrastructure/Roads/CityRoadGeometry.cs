using System;
using System.Collections.Generic;
namespace HarborCity
{
    // Values copied from the authoritative graph; these are not editable RoadNodes.
    public readonly struct RoadCenterSample
    {
        public readonly int NodeId;
        public readonly float X,Y,Z;
        public readonly double Distance;
        internal RoadCenterSample(RoadNode n,double distance){NodeId=n.id;X=n.x;Y=n.y;Z=n.z;Distance=distance;}
        internal RoadCenterSample(float x,float y,float z,double distance){NodeId=-1;X=x;Y=y;Z=z;Distance=distance;}
    }
    public readonly struct LogicalRoadSpan
    {
        public readonly int EdgeId,From,To,LegacyFrom,LegacyTo,Stroke;
        public readonly RoadStructure Structure;
        public readonly float Length;
        internal LogicalRoadSpan(RoadEdge e,int from,int to,float length)
        {EdgeId=e.id;From=from;To=to;LegacyFrom=e.a;LegacyTo=e.b;Stroke=e.stroke;Structure=e.structure;Length=length;}
    }
    public sealed class RoadCenterline
    {
        readonly RoadCenterSample[] samples;
        readonly LogicalRoadSpan[] spans;
        public IReadOnlyList<RoadCenterSample> Samples {get;}
        public IReadOnlyList<LogicalRoadSpan> Spans {get;}
        public double Length=>samples[samples.Length-1].Distance;
        internal RoadCenterline(RoadCenterSample[] samples,LogicalRoadSpan[] spans)
        {this.samples=samples;this.spans=spans;Samples=Array.AsReadOnly(samples);Spans=Array.AsReadOnly(spans);}
        internal int SpanAt(double distance)
        {
            if(double.IsNaN(distance) || double.IsInfinity(distance) || distance<0 || distance>Length)throw new ArgumentOutOfRangeException(nameof(distance));
            int lo=0,hi=spans.Length-1;
            while(lo<hi){int mid=(lo+hi)/2;if(distance>=samples[mid+1].Distance)lo=mid+1;else hi=mid;}
            return lo;
        }
        // Piecewise centreline, deliberately not the current smoothed lane trajectory.
        public RoadCenterSample SampleAt(double distance)
        {
            int i=SpanAt(distance);var a=samples[i];var b=samples[i+1];
            float t=(float)((distance-a.Distance)/spans[i].Length);
            return new RoadCenterSample(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t,a.Z+(b.Z-a.Z)*t,distance);
        }
    }
    public readonly struct LogicalRoadPosition
    {
        public readonly LogicalRoad Road;
        public readonly int SpanIndex;
        public readonly bool Forward;
        public readonly double Distance;
        internal LogicalRoadPosition(LogicalRoad road,int span,bool forward,double distance)
        {Road=road;SpanIndex=span;Forward=forward;Distance=distance;}
    }
}
