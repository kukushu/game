using System;
using System.Collections.Generic;

namespace HarborCity.RoadCore
{
    // A synchronous query scope, discarded after job assignment. No saved facts
    // and no reusable lifetime across road/access edits. Exact routing still decides jobs.
    internal sealed class ResidentRouteBounds
    {
        readonly World world;
        readonly Network network;
        readonly double maximumSpeed;
        readonly Dictionary<int,Point[]> positions=new Dictionary<int,Point[]>();
        internal ResidentRouteBounds(World world)
        {
            this.world=world;network=world.Network;
            foreach(var d in network.Drivables)maximumSpeed=Math.Max(maximumSpeed,d.Speed);
        }
        Point[] Positions(int id)
        {
            if(positions.TryGetValue(id,out var found))return found;
            var a=world.GetAccess(id);
            if(a==null||!a.valid||!network.Contains(a.lane))found=Array.Empty<Point>();
            else
            {
                var lane=network.Get(a.lane);var opposite=AccessRouting.AtDirection(network,a,!lane.Reverse);
                var p=lane.Geometry.Sample(a.distance);
                found=opposite==null?new[]{p}:new[]{p,network.Get(opposite.lane).Geometry.Sample(opposite.distance)};
            }
            positions.Add(id,found);return found;
        }
        internal double Seconds(int origin,int destination)
        {
            if(!ReferenceEquals(world.Network,network))throw new InvalidOperationException("Expired resident estimate query");
            var a=Positions(origin);var b=Positions(destination);
            // Invalid/missing access must go through the authoritative exact check.
            if(a.Length==0||b.Length==0||maximumSpeed<=0)return 0;
            double distance=double.PositiveInfinity;
            foreach(var p in a)foreach(var q in b)distance=Math.Min(distance,Point.Distance(p,q));
            // Triangle inequality over the continuous lane/connection path. All
            // drivable speeds are <=maximumSpeed; rounding allowance only weakens it.
            return Math.Max(0,distance/maximumSpeed-1e-6);
        }
    }
}
