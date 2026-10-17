using System;
using System.Collections.Generic;
using HarborCity.RoadCore;
namespace HarborCity
{
    // Same 3D ribbon/building footprint check for land placement and cold road
    // widening previews. Caller supplies scratch; no second editable geometry.
    internal static class CityRoadClearance
    {
        internal static bool Intersects(Network network,CityBuilding pose,List<SpatialHit> hits)
        {
#if CITY_CORE_ATTRIBUTION
            using(CityCoreProfile.Measure(CityCoreProfile.Part.ClearanceQuery))
            {
#endif
            double radius=Math.Min(64,Math.Sqrt(pose.width*pose.width+pose.depth*pose.depth)/2+2);network.Project(new Point(pose.x,pose.groundY,pose.z),radius,true,hits);
#if CITY_CORE_ATTRIBUTION
            }
            using var overlapObservation=CityCoreProfile.Measure(CityCoreProfile.Part.ClearanceOverlap);
#endif
            foreach(var hit in hits){var road=network.Get(hit.drivable);if(Math.Abs(hit.point.y-pose.groundY)>2)continue;var g=road.Geometry;for(int i=1;i<g.Count;i++){double a=Math.Max(g.At(i-1),road.StartDistance),b=Math.Min(g.At(i),road.EndDistance);if(b<=a)continue;var p=g.Sample(a);var q=g.Sample(b);var ribbon=new ResidentialBuilding{x=(float)((p.x+q.x)/2),z=(float)((p.z+q.z)/2),yaw=-(float)(Math.Atan2(q.z-p.z,q.x-p.x)*180/Math.PI),width=(float)Math.Sqrt((q.x-p.x)*(q.x-p.x)+(q.z-p.z)*(q.z-p.z))+.08f,depth=1.1f};if(pose.Overlaps(ribbon))return true;}}
            return false;
        }
    }
}
