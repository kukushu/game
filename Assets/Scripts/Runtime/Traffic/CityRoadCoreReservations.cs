using System;
using System.Collections.Generic;

namespace HarborCity.RoadCore
{
    // Static paths own conflicts; dynamic claims only scan this junction's occupants.
    // Zero movement is an unknown/curb insertion and conservatively blocks all paths.
    internal sealed class JunctionReservations
    {
        struct Claim {public long movement;public int owner;}
        readonly Dictionary<int,List<Claim>> claims=new Dictionary<int,List<Claim>>();
        Network network;
        public JunctionReservations(Network network){this.network=network;}
        public void Reset(Network network){if(!ReferenceEquals(this.network,network))claims.Clear();this.network=network;foreach(var bucket in claims.Values)bucket.Clear();}
        internal List<(int junction,int owner,long movement)> CopyClaims()
        {
            var result=new List<(int,int,long)>();foreach(var pair in claims)foreach(var claim in pair.Value)result.Add((pair.Key,claim.owner,claim.movement));
            result.Sort((a,b)=>{int c=a.Item1.CompareTo(b.Item1);if(c!=0)return c;c=a.Item2.CompareTo(b.Item2);return c!=0?c:a.Item3.CompareTo(b.Item3);});return result;
        }
        public bool CanReserve(int junction,long movement,int owner)
        {
            List<Claim> bucket;if(!claims.TryGetValue(junction,out bucket))return true;
            foreach(var claim in bucket)if(claim.owner!=owner&&(movement==0||claim.movement==0||network.MovementsConflict(movement,claim.movement)))return false;
            return true;
        }
        public void Reserve(int junction,long movement,int owner)
        {
            if(!CanReserve(junction,movement,owner))throw new InvalidOperationException("Conflicting junction occupants");
            List<Claim> bucket;if(!claims.TryGetValue(junction,out bucket))claims.Add(junction,bucket=new List<Claim>());
            foreach(var claim in bucket)if(claim.owner==owner&&claim.movement==movement)return;
            bucket.Add(new Claim{movement=movement,owner=owner});
        }
    }
    // Immutable-network derived queries. Compute a pair only when requested, and
    // cache both answers for this Network's lifetime. No dynamic occupancy here.
    internal sealed class ConnectionConflicts
    {
        sealed class Group
        {
            internal readonly List<Entry> entries=new List<Entry>();
        }
        sealed class Entry
        {
            internal readonly LaneConnection connection;internal readonly Group group;internal readonly int index;
            internal Dictionary<long,bool> answers;
            ProtectedPath path;
            internal Entry(LaneConnection connection,Group group){this.connection=connection;this.group=group;index=group.entries.Count;group.entries.Add(this);}
            internal ProtectedPath Path=>path??(path=BuildPath(connection));
        }
        readonly struct Bounds
        {
            readonly double minX,minY,minZ,maxX,maxY,maxZ,padding;
            static double Padding(double x,double y,double z,double X,double Y,double Z)=>1+1e-12*Math.Max(Math.Max(Math.Max(Math.Abs(x),Math.Abs(X)),Math.Max(Math.Abs(y),Math.Abs(Y))),Math.Max(Math.Abs(z),Math.Abs(Z)));
            internal Bounds(Point a,Point b){minX=Math.Min(a.x,b.x);minY=Math.Min(a.y,b.y);minZ=Math.Min(a.z,b.z);maxX=Math.Max(a.x,b.x);maxY=Math.Max(a.y,b.y);maxZ=Math.Max(a.z,b.z);padding=Padding(minX,minY,minZ,maxX,maxY,maxZ);}
            internal Bounds(Geometry g){double x=g[0].x,y=g[0].y,z=g[0].z,X=x,Y=y,Z=z;for(int i=1;i<g.Count;i++){x=Math.Min(x,g[i].x);y=Math.Min(y,g[i].y);z=Math.Min(z,g[i].z);X=Math.Max(X,g[i].x);Y=Math.Max(Y,g[i].y);Z=Math.Max(Z,g[i].z);}minX=x;minY=y;minZ=z;maxX=X;maxY=Y;maxZ=Z;padding=Padding(x,y,z,X,Y,Z);}
            // Deliberately wider than the exact 0.9 distance threshold. This is
            // only conservative rejection; every surviving pair uses the oracle.
            internal bool Separate(Bounds b){double margin=Math.Max(padding,b.padding);return minX-b.maxX>margin||b.minX-maxX>margin||minY-b.maxY>margin||b.minY-maxY>margin||minZ-b.maxZ>margin||b.minZ-maxZ>margin;}
        }
        sealed class ProtectedPath
        {
            internal readonly Geometry geometry;internal readonly Bounds bounds;internal readonly Bounds[] segments;
            internal ProtectedPath(Geometry g){geometry=g;bounds=new Bounds(g);segments=new Bounds[g.Count-1];for(int i=1;i<g.Count;i++)segments[i-1]=new Bounds(g[i-1],g[i]);}
        }
        readonly Dictionary<long,Entry> entries=new Dictionary<long,Entry>(CityRoadKeyComparer.Instance);
        public ConnectionConflicts(Network network)
        {
            var local=new Dictionary<int,Group>();
            foreach(var d in network.Drivables)if(d.IsConnection)
            {
                if(!local.TryGetValue(d.JunctionId,out var group))local.Add(d.JunctionId,group=new Group());
                entries.Add(d.Id,new Entry((LaneConnection)d,group));
            }
        }
        static ProtectedPath BuildPath(LaneConnection d)
        {
            var entry=d.Incoming;var exit=d.Outgoing;var path=new List<Point>();double start=Math.Max(entry.StartDistance,entry.EndDistance-World.MinimumSpacing),end=Math.Min(exit.EndDistance,exit.StartDistance+World.MinimumSpacing);path.Add(entry.Geometry.Sample(start));
            for(int i=1;i<entry.Geometry.Count;i++)if(entry.Geometry.At(i)>start&&entry.Geometry.At(i)<entry.EndDistance)path.Add(entry.Geometry[i]);
            for(int i=0;i<d.Geometry.Count;i++)path.Add(d.Geometry[i]);for(int i=0;i<exit.Geometry.Count;i++)if(exit.Geometry.At(i)>exit.StartDistance&&exit.Geometry.At(i)<end)path.Add(exit.Geometry[i]);path.Add(exit.Geometry.Sample(end));return new ProtectedPath(new Geometry(path.ToArray()));
        }
        static bool Pair(Entry a,Entry b)
        {
            if(ReferenceEquals(a,b))return true;
            if(!ReferenceEquals(a.group,b.group))return false;
            if(a.answers!=null&&a.answers.TryGetValue(b.connection.Id,out bool known))return known;
            // Preserve the original constructor's bucket i<j argument order,
            // including its floating-point operations, regardless of first caller.
            bool overlap=a.index<b.index?Overlap(a.Path,b.Path):Overlap(b.Path,a.Path);
            if(a.answers==null)a.answers=new Dictionary<long,bool>(CityRoadKeyComparer.Instance);if(b.answers==null)b.answers=new Dictionary<long,bool>(CityRoadKeyComparer.Instance);
            a.answers.Add(b.connection.Id,overlap);b.answers.Add(a.connection.Id,overlap);return overlap;
        }
        public bool HasOther(long id){var a=entries[id];foreach(var b in a.group.entries)if(!ReferenceEquals(a,b)&&Pair(a,b))return true;return false;}
        public bool Contains(long a,long b)
        {if(!entries.TryGetValue(a,out var first))return false;if(first.answers!=null&&first.answers.TryGetValue(b,out bool known))return known;return entries.TryGetValue(b,out var second)&&Pair(first,second);}
        static double Squared(Point a,Point b){double x=a.x-b.x,y=a.y-b.y,z=a.z-b.z;return x*x+y*y+z*z;}
        static double Dot(Point a,Point b)=>a.x*b.x+a.y*b.y+a.z*b.z;
        static Point Sub(Point a,Point b)=>new Point(a.x-b.x,a.y-b.y,a.z-b.z);
        static double Clamp(double x)=>Math.Max(0,Math.Min(1,x));
        // Closest distance of finite 3D segments, including zero-length connectors.
        static double SegmentDistance(Point p,Point q,Point a,Point b)
        {
            Point u=Sub(q,p),v=Sub(b,a),w=Sub(p,a);double uu=Dot(u,u),vv=Dot(v,v),uv=Dot(u,v),uw=Dot(u,w),vw=Dot(v,w),s,t;
            if(uu<1e-18){s=0;t=vv<1e-18?0:Clamp(vw/vv);}
            else if(vv<1e-18){t=0;s=Clamp(-uw/uu);}
            else {double den=uu*vv-uv*uv;s=den>1e-18?Clamp((uv*vw-uw*vv)/den):0;t=(uv*s+vw)/vv;if(t<0){t=0;s=Clamp(-uw/uu);}else if(t>1){t=1;s=Clamp((uv-uw)/uu);}}
            return Squared(Point.Lerp(p,q,s),Point.Lerp(a,b,t));
        }
        static bool Overlap(ProtectedPath a,ProtectedPath b)
        {if(a.bounds.Separate(b.bounds))return false;var ag=a.geometry;var bg=b.geometry;for(int i=1;i<ag.Count;i++)for(int j=1;j<bg.Count;j++)if(!a.segments[i-1].Separate(b.segments[j-1])&&SegmentDistance(ag[i-1],ag[i],bg[j-1],bg[j])<.9*.9)return true;return false;}
    }
}
