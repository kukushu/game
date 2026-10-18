using System;
using System.Collections.Generic;

namespace HarborCity.RoadCore
{
    // Geometry/attachment planning is a cold path operating on a disposable road draft.
    // No stock settlement, simulation advance or live actor mutation occurs here.
    internal static class RoadConstruction
    {
        struct Cut {public double distance;public Point point;public int junction;}
        static double Cross(double ax,double az,double bx,double bz)=>ax*bz-az*bx;
        public static RoadEditPlan Connect(World owner,Point[] center,bool oneWay,double speed,double snap,int lanes)
            =>ConnectMany(owner,new[]{center},oneWay,speed,snap,lanes);
        public static RoadEditPlan ConnectMany(World owner,Point[][] strokes,bool oneWay,double speed,double snap,int lanes)
        {
            if(strokes==null||strokes.Length==0||strokes.Length>18)throw new ArgumentException("A road batch needs 1 to 18 strokes");
            var draft=owner.EditDraft();
            foreach(var stroke in strokes)Append(draft,stroke,oneWay,speed,snap,lanes);
            return owner.PlanFromDraft(draft);
        }
        static void Append(World draft,Point[] center,bool oneWay,double speed,double snap,int lanes)
        {
            if(center==null||center.Length<2)throw new ArgumentException("A road stroke needs two points");
            if(snap<0||double.IsNaN(snap)||double.IsInfinity(snap))throw new ArgumentOutOfRangeException(nameof(snap));
            var points=(Point[])center.Clone();
            int start=Attach(draft,points[0],snap),end=Attach(draft,points[points.Length-1],snap);
            points[0]=draft.JunctionPosition(start);points[points.Length-1]=draft.JunctionPosition(end);var geometry=new Geometry(points);
            var cuts=new List<Cut>{new Cut{distance=0,point=points[0],junction=start},new Cut{distance=geometry.Length,point=points[points.Length-1],junction=end}};
            foreach(var road in draft.SnapshotRoadGeometry().roads)for(int segment=0;segment<points.Length-1;segment++)for(int span=0;span<road.points.Length-1;span++)
            {
                Point a=points[segment],b=points[segment+1],c=road.points[span],d=road.points[span+1];double rx=b.x-a.x,rz=b.z-a.z,sx=d.x-c.x,sz=d.z-c.z,den=Cross(rx,rz,sx,sz);
                if(Math.Abs(den)<1e-10)
                {
                    double rr=rx*rx+rz*rz;if(rr>1e-10&&Math.Abs(Cross(c.x-a.x,c.z-a.z,rx,rz))<1e-7)
                    {double t1=((c.x-a.x)*rx+(c.z-a.z)*rz)/rr,t2=((d.x-a.x)*rx+(d.z-a.z)*rz)/rr;double lo=Math.Max(0,Math.Min(t1,t2)),hi=Math.Min(1,Math.Max(t1,t2));if(hi-lo>1e-5&&Math.Abs(Point.Lerp(a,b,(lo+hi)/2).y-Point.Lerp(c,d,.5).y)<.25)throw new InvalidOperationException("New stroke overlaps an existing road; use an upgrade operation");}
                    continue;
                }
                double t=Cross(c.x-a.x,c.z-a.z,sx,sz)/den,u=Cross(c.x-a.x,c.z-a.z,rx,rz)/den;
                if(t< -1e-7||t>1+1e-7||u< -1e-7||u>1+1e-7)continue;
                Point intersection=Point.Lerp(a,b,t),existing=Point.Lerp(c,d,Math.Max(0,Math.Min(1,u)));if(Math.Abs(intersection.y-existing.y)>.25)continue;
                double distance=geometry.At(segment)+Point.Distance(a,intersection);if(distance<=.01||distance>=geometry.Length-.01)continue;
                cuts.Add(new Cut{distance=distance,point=existing});
            }
            cuts.Sort((a,b)=>a.distance.CompareTo(b.distance));var resolved=new List<Cut>();
            foreach(var cut in cuts)
            {
                if(resolved.Count>0&&cut.distance-resolved[resolved.Count-1].distance<.01)continue;
                var attached=cut;if(attached.junction==0)attached.junction=Attach(draft,attached.point,.05);resolved.Add(attached);
            }
            for(int i=1;i<resolved.Count;i++)
            {
                var piece=new List<Point>{draft.JunctionPosition(resolved[i-1].junction)};
                for(int sample=1;sample<geometry.Count-1;sample++)if(geometry.At(sample)>resolved[i-1].distance+.01&&geometry.At(sample)<resolved[i].distance-.01)piece.Add(geometry[sample]);
                piece.Add(draft.JunctionPosition(resolved[i].junction));draft.AddRoad(resolved[i-1].junction,resolved[i].junction,piece.ToArray(),oneWay,speed,lanes);
            }
        }
        static int Attach(World draft,Point point,double radius)
        {
            int junction=draft.SnapJunction(point,radius);if(junction!=0){JoinCoincidentRoads(draft,junction);return junction;}
            int bestRoad=0;double best=radius,along=0;Point position=point;
            foreach(var road in draft.SnapshotRoadGeometry().roads)
            {
                var geometry=new Geometry(road.points);
                for(int i=1;i<geometry.Count;i++)
                {
                    Point a=geometry[i-1],b=geometry[i];double x=b.x-a.x,y=b.y-a.y,z=b.z-a.z,den=x*x+y*y+z*z;
                    double t=Math.Max(0,Math.Min(1,((point.x-a.x)*x+(point.y-a.y)*y+(point.z-a.z)*z)/den));Point p=Point.Lerp(a,b,t);double distance=Point.Distance(point,p);
                    if(Math.Abs(point.y-p.y)>.25||distance>best)continue;best=distance;bestRoad=road.id;along=geometry.At(i-1)+Point.Distance(a,p);position=p;
                }
            }
            if(bestRoad==0)return draft.AddJunction(point);
            junction=draft.SnapJunction(position,.05);if(junction==0)junction=draft.SplitRoadAt(bestRoad,along);JoinCoincidentRoads(draft,junction);return junction;
        }
        static void JoinCoincidentRoads(World draft,int junction)
        {
            Point anchor=draft.JunctionPosition(junction);
            foreach(var road in draft.SnapshotRoadGeometry().roads)
            {
                if(road.from==junction||road.to==junction)continue;var geometry=new Geometry(road.points);
                for(int i=1;i<geometry.Count;i++)
                {
                    Point a=geometry[i-1],b=geometry[i];double x=b.x-a.x,y=b.y-a.y,z=b.z-a.z,den=x*x+y*y+z*z,t=Math.Max(0,Math.Min(1,((anchor.x-a.x)*x+(anchor.y-a.y)*y+(anchor.z-a.z)*z)/den));Point p=Point.Lerp(a,b,t);
                    if(Point.Distance(anchor,p)>1e-6)continue;double distance=geometry.At(i-1)+Point.Distance(a,p);
                    if(distance<=.1||distance>=geometry.Length-.1)throw new InvalidOperationException("Coincident independent road endpoints require explicit topology repair");
                    draft.SplitRoadAt(road.id,distance,junction);break;
                }
            }
        }
    }
}
