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
    // Cold geometry computation, rebuilt only when Network changes. Paths that merge,
    // cross or share a lane endpoint conflict; separated parallel paths may coexist.
    internal sealed class ConnectionConflicts
    {
        readonly Dictionary<long,HashSet<long>> conflicts=new Dictionary<long,HashSet<long>>(CityRoadKeyComparer.Instance);
        public ConnectionConflicts(Network network)
        {
            var local=new Dictionary<int,List<Drivable>>();
            var protectedPaths=new Dictionary<long,Geometry>(CityRoadKeyComparer.Instance);
            foreach(var d in network.Drivables)if(d.IsConnection)
            {
                List<Drivable> list;if(!local.TryGetValue(d.JunctionId,out list))local.Add(d.JunctionId,list=new List<Drivable>());list.Add(d);conflicts.Add(d.Id,new HashSet<long>(CityRoadKeyComparer.Instance){d.Id});
                var connection=(LaneConnection)d;var entry=connection.Incoming;var exit=connection.Outgoing;var path=new List<Point>();double start=Math.Max(entry.StartDistance,entry.EndDistance-World.MinimumSpacing),end=Math.Min(exit.EndDistance,exit.StartDistance+World.MinimumSpacing);path.Add(entry.Geometry.Sample(start));
                for(int i=1;i<entry.Geometry.Count;i++)if(entry.Geometry.At(i)>start&&entry.Geometry.At(i)<entry.EndDistance)path.Add(entry.Geometry[i]);
                for(int i=0;i<d.Geometry.Count;i++)path.Add(d.Geometry[i]);for(int i=0;i<exit.Geometry.Count;i++)if(exit.Geometry.At(i)>exit.StartDistance&&exit.Geometry.At(i)<end)path.Add(exit.Geometry[i]);path.Add(exit.Geometry.Sample(end));protectedPaths.Add(d.Id,new Geometry(path.ToArray()));
            }
            foreach(var bucket in local.Values)for(int i=0;i<bucket.Count;i++)for(int j=i+1;j<bucket.Count;j++)if(Overlap(protectedPaths[bucket[i].Id],protectedPaths[bucket[j].Id])){conflicts[bucket[i].Id].Add(bucket[j].Id);conflicts[bucket[j].Id].Add(bucket[i].Id);}
        }
        public bool HasOther(long id)=>conflicts[id].Count>1;
        public bool Contains(long a,long b){HashSet<long> set;return conflicts.TryGetValue(a,out set)&&set.Contains(b);}
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
        static bool Overlap(Geometry a,Geometry b)
        {for(int i=1;i<a.Count;i++)for(int j=1;j<b.Count;j++)if(SegmentDistance(a[i-1],a[i],b[j-1],b[j])<.9*.9)return true;return false;}
    }
}
