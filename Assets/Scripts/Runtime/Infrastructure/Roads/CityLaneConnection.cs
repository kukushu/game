using System;
namespace HarborCity.RoadCore
{
    public sealed class JunctionPort
    {
        public readonly long LaneId;
        public readonly int JunctionId;
        public readonly double Station;
        public readonly Point Position,Direction;
        internal JunctionPort(Lane lane,int junction,double station)
        {LaneId=lane.Id;JunctionId=junction;Station=station;Position=lane.Geometry.Sample(station);Direction=lane.Geometry.DirectionAt(station);}
    }
    public sealed class Lane:Drivable
    {
        public JunctionPort Entry {get;private set;}
        public JunctionPort Exit {get;private set;}
        internal Lane(long id,int road,int from,int to,bool reverse,double speed,Point[] points):base(id,road,from,to,0,reverse,speed,points){}
        internal Lane(long id,int road,int from,int to,bool reverse,double speed,Geometry geometry):base(id,road,from,to,0,reverse,speed,geometry){}
        internal void ConfigurePorts(bool trimEntry,bool trimExit)
        {Entry=new JunctionPort(this,From,trimEntry?ProtectedClearance:0);Exit=new JunctionPort(this,To,Geometry.Length-(trimExit?ProtectedClearance:0));StartDistance=Entry.Station;EndDistance=Exit.Station;}
    }
    public sealed class LaneConnection:Drivable
    {
        public readonly Lane Incoming,Outgoing;
        public JunctionPort Entry=>Incoming.Exit;
        public JunctionPort Exit=>Outgoing.Entry;
        internal LaneConnection(long id,Lane incoming,Lane outgoing,LaneConnection prior=null):base(id,0,incoming.To,incoming.To,incoming.To,false,Math.Min(incoming.Speed,outgoing.Speed),GeometryFor(incoming.Exit,outgoing.Entry,prior))
        {Incoming=incoming;Outgoing=outgoing;}
        static Geometry GeometryFor(JunctionPort a,JunctionPort b,LaneConnection prior)
        {
            // Curve depends only on these four vectors, not speed, lane count,
            // road identity or another candidate's mutable JunctionPort objects.
            if(prior!=null&&Geometry.Same(a.Position,prior.Entry.Position)&&Geometry.Same(a.Direction,prior.Entry.Direction)&&Geometry.Same(b.Position,prior.Exit.Position)&&Geometry.Same(b.Direction,prior.Exit.Direction))return prior.Geometry;
            return new Geometry(Curve(a,b));
        }
        static Point[] Curve(JunctionPort a,JunctionPort b)
        {
            double distance=Point.Distance(a.Position,b.Position);if(distance<1e-8)return new[]{a.Position,b.Position};
            double handle=distance*.4;var p=new Point(a.Position.x+a.Direction.x*handle,a.Position.y+a.Direction.y*handle,a.Position.z+a.Direction.z*handle);var q=new Point(b.Position.x-b.Direction.x*handle,b.Position.y-b.Direction.y*handle,b.Position.z-b.Direction.z*handle);
            int count=Math.Max(9,Math.Min(65,(int)Math.Ceiling(distance/.3)+1));var result=new Point[count];
            for(int i=0;i<count;i++){double t=i/(double)(count-1);result[i]=Point.Lerp(Point.Lerp(Point.Lerp(a.Position,p,t),Point.Lerp(p,q,t),t),Point.Lerp(Point.Lerp(p,q,t),Point.Lerp(q,b.Position,t),t),t);}
            return result;
        }
    }
}
