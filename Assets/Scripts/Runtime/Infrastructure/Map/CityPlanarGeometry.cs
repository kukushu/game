using System;
namespace HarborCity
{
    // Pure map/utility/building calculations, independent of any editable road
    // network or traffic engine. Preserve the original float evaluation order.
    public static class CityPlanarGeometry
    {
        public static float Length(RoadNode a,RoadNode b)=>(float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.z-b.z)*(a.z-b.z));
        public static RoadNode Lerp(RoadNode a,RoadNode b,float t)=>new RoadNode{x=a.x+(b.x-a.x)*t,y=a.y+(b.y-a.y)*t,z=a.z+(b.z-a.z)*t};
        public static float Projection(RoadNode p,RoadNode a,RoadNode b)
        {float dx=b.x-a.x,dz=b.z-a.z,d=dx*dx+dz*dz;return d<.0001f?0:Math.Clamp(((p.x-a.x)*dx+(p.z-a.z)*dz)/d,0,1);}
        public static float Distance(RoadNode p,RoadNode a,RoadNode b)
        {float t=Projection(p,a,b),dx=p.x-(a.x+(b.x-a.x)*t),dz=p.z-(a.z+(b.z-a.z)*t);return (float)Math.Sqrt(dx*dx+dz*dz);}
    }
}
