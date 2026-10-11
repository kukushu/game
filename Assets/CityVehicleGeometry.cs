using System;
namespace HarborCity
{
    // Derived geometry only: trips retain their route, lane and simulation progress.
    public static class CityVehicleGeometry
    {
        // Temporary geometry is a value, not a graph entity with a stop-sign list.
        public struct Point
        {
            public float x,y,z;
            public static implicit operator Point(RoadNode n)=>new Point{x=n.x,y=n.y,z=n.z};
            public RoadNode ToNode()=>new RoadNode{x=x,y=y,z=z};
        }
        static Point Mix(Point a,Point b,float t)=>new Point{x=a.x+(b.x-a.x)*t,z=a.z+(b.z-a.z)*t,y=a.y+(b.y-a.y)*t};
        public static RoadNode Corner(RoadNode a,RoadNode n,RoadNode b,float incoming,float outgoing)=>CornerPoint(a,n,b,incoming,outgoing).ToNode();
        static Point CornerPoint(RoadNode a,RoadNode n,RoadNode b,float incoming,float outgoing)
        {
            float il=CityRoads.Length(a,n),ol=CityRoads.Length(n,b);
            if(il<.001f || ol<.001f)return n;
            float ix=(n.x-a.x)/il,iz=(n.z-a.z)/il,ox=(b.x-n.x)/ol,oz=(b.z-n.z)/ol;
            var p=new Point{x=n.x+iz*incoming,z=n.z-ix*incoming,y=n.y};
            var q=new Point{x=n.x+oz*outgoing,z=n.z-ox*outgoing,y=n.y};
            float cross=ix*oz-iz*ox;
            // Intersect offset lane tangents, rather than pulling a corner toward the road center.
            if(Math.Abs(cross)<.01f)return Mix(p,q,.5f);
            float distance=((q.x-p.x)*oz-(q.z-p.z)*ox)/cross;
            // Near reversals have no usable miter; keep their conservative bounded fallback.
            if(Math.Abs(distance)>Math.Min(il,ol)*2)return Mix(p,q,.5f);
            return new Point{x=p.x+ix*distance,z=p.z+iz*distance,y=n.y};
        }
        static Point Vertex(CityRoads roads,TrafficTrip trip,int index)
        {
            var n=roads.Node(trip.route[index]);
            if(trip.route.Count==1)return n;
            if(index>0 && index+1<trip.route.Count)
                return CornerPoint(roads.Node(trip.route[index-1]),n,roads.Node(trip.route[index+1]),roads.TripLaneOffset(trip,index-1),roads.TripLaneOffset(trip,index));
            int s=index==0?0:index-1;var a=roads.Node(trip.route[s]);var b=roads.Node(trip.route[s+1]);
            float length=CityRoads.Length(a,b),offset=roads.TripLaneOffset(trip,s);
            return length<.001f?(Point)n:new Point{x=n.x+(b.z-a.z)/length*offset,z=n.z-(b.x-a.x)/length*offset,y=n.y};
        }
        const float Cut=.4f;
        // Each corner consumes the last/first 40% of adjoining segments. The two
        // halves share one quadratic, so neither the vehicle nor its route snaps at the node.
        public static RoadNode Position(CityRoads roads,TrafficTrip trip,int segment,float fraction)
            =>PositionValue(roads,trip,segment,fraction).ToNode();
        public static Point PositionValue(CityRoads roads,TrafficTrip trip,int segment,float fraction)
        {
            if(trip.route.Count==1)return roads.Node(trip.route[0]);
            if(segment>=trip.route.Count-1)return Vertex(roads,trip,trip.route.Count-1);
            segment=Math.Max(0,Math.Min(segment,trip.route.Count-2));fraction=Math.Max(0,Math.Min(1,fraction));
            var a=Vertex(roads,trip,segment);var b=Vertex(roads,trip,segment+1);
            if(fraction<Cut && segment>0)
            {
                var p=Vertex(roads,trip,segment-1);
                return Curve(Mix(p,a,1-Cut),a,Mix(a,b,Cut),.5f+fraction/(2*Cut));
            }
            if(fraction>1-Cut && segment+2<trip.route.Count)
            {
                var q=Vertex(roads,trip,segment+2);
                return Curve(Mix(a,b,1-Cut),b,Mix(b,q,Cut),(fraction-(1-Cut))/(2*Cut));
            }
            return Mix(a,b,fraction);
        }
        static Point Curve(Point a,Point control,Point b,float t)=>Mix(Mix(a,control,t),Mix(control,b,t),t);
    }
}
