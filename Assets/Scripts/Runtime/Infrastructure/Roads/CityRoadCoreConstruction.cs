using System;
using System.Collections.Generic;

namespace HarborCity.RoadCore
{
    public enum RoadGridIssue { AlreadyBuilt,TooClose }
    public sealed class RoadGridException:InvalidOperationException
    {
        public readonly RoadGridIssue Issue;
        public RoadGridException(RoadGridIssue issue):base(issue.ToString()){Issue=issue;}
    }
    // Geometry/attachment planning is a cold path operating on a disposable road draft.
    // No stock settlement, simulation advance or live actor mutation occurs here.
    internal static class RoadConstruction
    {
        struct Cut {public double distance;public Point point;public int junction;}
        static double Cross(double ax,double az,double bx,double bz)=>ax*bz-az*bx;
        public static RoadEditPlan Connect(World owner,Point[] center,bool oneWay,double speed,double snap,int lanes)
            =>ConnectMany(owner,new[]{center},oneWay,speed,snap,lanes);
        public static RoadEditPlan ConnectMany(World owner,Point[][] strokes,bool oneWay,double speed,double snap,int lanes,bool reuseExisting=false)
        {
            if(strokes==null||strokes.Length==0||strokes.Length>18)throw new ArgumentException("A road batch needs 1 to 18 strokes");
            // Experimental, deliberately narrow: no existing endpoint/road can be
            // attached, cut or joined. Grid batches retain the original draft path.
            if(!reuseExisting&&strokes.Length==1&&owner.TryPlanIndependentRoad(strokes[0],oneWay,speed,snap,lanes,out var independent))return independent;
            var draft=owner.EditDraft();
            bool added=false;
            foreach(var stroke in strokes)
            {
                if(!reuseExisting){Append(draft,stroke,oneWay,speed,snap,lanes);added=true;continue;}
                foreach(var missing in MissingGridPieces(draft,stroke))
                {CheckGridSeparation(draft,missing,lanes);Append(draft,missing,oneWay,speed,snap,lanes);added=true;}
            }
            if(!added)throw new RoadGridException(RoadGridIssue.AlreadyBuilt);
            return owner.PlanFromDraft(draft);
        }
        struct Covered {public double from,to;}
        // Exact existing centreline coverage is reused, never upgraded or charged again.
        // This policy is grid-only; ordinary single-road overlap rules are unchanged.
        static List<Point[]> MissingGridPieces(World draft,Point[] stroke)
        {
            var geometry=new Geometry(stroke);var covered=new List<Covered>();
            foreach(int id in draft.Network.RoadIds)
            {
                var road=draft.Network.Centreline(id);
                for(int i=1;i<geometry.Count;i++)for(int j=1;j<road.Count;j++)
                {
                    var a=geometry[i-1];var b=geometry[i];var c=road[j-1];var d=road[j];
                    double x=b.x-a.x,y=b.y-a.y,z=b.z-a.z,len=Point.Distance(a,b);if(len<1e-9)continue;
                    double t=((c.x-a.x)*x+(c.y-a.y)*y+(c.z-a.z)*z)/(len*len),u=((d.x-a.x)*x+(d.y-a.y)*y+(d.z-a.z)*z)/(len*len);
                    if(Point.Distance(c,Point.Lerp(a,b,t))>1e-6||Point.Distance(d,Point.Lerp(a,b,u))>1e-6)continue;
                    double lo=Math.Max(0,Math.Min(t,u)),hi=Math.Min(1,Math.Max(t,u));
                    if(hi>lo)covered.Add(new Covered{from=geometry.At(i-1)+lo*len,to=geometry.At(i-1)+hi*len});
                }
            }
            covered.Sort((a,b)=>a.from.CompareTo(b.from));var result=new List<Point[]>();double cursor=0;
            foreach(var span in covered){if(span.from>cursor+1e-6)result.Add(geometry.Slice(cursor,span.from));cursor=Math.Max(cursor,span.to);}
            if(cursor<geometry.Length-1e-6)result.Add(geometry.Slice(cursor,geometry.Length));return result;
        }
        static void CheckGridSeparation(World draft,Point[] stroke,int lanes)
        {
            // Lane offsets are .55 + laneIndex*1.1, each rendered/physical lane is 1m wide.
            // Reject parallel overlapping ribbons, while retaining real crossing intersections.
            foreach(int id in draft.Network.RoadIds)
            {
                var road=draft.Network.Centreline(id);double clearance=(lanes+draft.Network.Settings(id).lanes-1)*1.1+1;
                for(int i=1;i<stroke.Length;i++)for(int j=1;j<road.Count;j++)
                {
                    var a=stroke[i-1];var b=stroke[i];var c=road[j-1];var d=road[j];
                    double x=b.x-a.x,z=b.z-a.z,rx=d.x-c.x,rz=d.z-c.z,len=Math.Sqrt(x*x+z*z),rlen=Math.Sqrt(rx*rx+rz*rz);
                    if(len<1e-9||rlen<1e-9||Math.Abs(Cross(x,z,rx,rz))>1e-5*len*rlen)continue;
                    double t=((c.x-a.x)*x+(c.z-a.z)*z)/(len*len),u=((d.x-a.x)*x+(d.z-a.z)*z)/(len*len);
                    double lo=Math.Max(0,Math.Min(t,u)),hi=Math.Min(1,Math.Max(t,u));if((hi-lo)*len<=1e-6)continue;
                    double mid=(lo+hi)/2;var p=Point.Lerp(a,b,mid);double v=((p.x-c.x)*rx+(p.z-c.z)*rz)/(rlen*rlen);var q=Point.Lerp(c,d,Math.Max(0,Math.Min(1,v)));
                    if(Math.Abs(p.y-q.y)<.25&&Math.Abs(Cross(c.x-a.x,c.z-a.z,x,z))/len<clearance-1e-6)throw new RoadGridException(RoadGridIssue.TooClose);
                }
            }
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
