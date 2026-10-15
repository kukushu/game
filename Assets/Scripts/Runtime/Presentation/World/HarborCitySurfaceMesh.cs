using System;
using System.Collections.Generic;
using HarborCity.RoadCore;
using UnityEngine;

namespace HarborCity
{
    // Display-only terrain clearance. Never writes the shared road geometry or
    // physical positions. Subdivision follows each original segment, not chords.
    public static class HarborCitySurfaceMesh
    {
        public static Vector3 Visible(Point p, Func<float,float,float> height, float lift=.12f)
        {
            float y=(float)p.y;
            if(height!=null&&Math.Abs(y-height((float)p.x,(float)p.z))<.75f)
                y=Math.Max(y,height((float)p.x,(float)p.z));
            return new Vector3((float)p.x,y+lift,(float)p.z);
        }
        public static Mesh Ribbon(Geometry g, double start, double end, Func<float,float,float> height)
        {
            var vertices=new List<Vector3>();
            for(int segment=1;segment<g.Count;segment++)
            {
                double lo=Math.Max(start,g.At(segment-1)),hi=Math.Min(end,g.At(segment));if(hi<=lo)continue;
                int count=Math.Max(1,(int)Math.Ceiling((hi-lo)/.5));
                for(int i=vertices.Count==0?0:1;i<=count;i++)
                {
                    double distance=lo+(hi-lo)*i/count;var p=g.Sample(distance);var t=g.DirectionAt(distance);
                    var a=Visible(Network.Offset(p,t,-.5),height);var b=Visible(Network.Offset(p,t,.5),height);
                    if(height!=null&&Math.Abs(p.y-height((float)p.x,(float)p.z))<.75)
                    {float top=Math.Max(Math.Max(a.y,b.y),height((float)p.x,(float)p.z)+.12f);a.y=b.y=top;}
                    vertices.Add(a);vertices.Add(b);
                }
            }
            var indices=new int[Math.Max(0,vertices.Count/2-1)*6];
            for(int i=0;i<indices.Length/6;i++){int a=i*2,b=i*6;indices[b]=a;indices[b+1]=a+2;indices[b+2]=a+1;indices[b+3]=a+1;indices[b+4]=a+2;indices[b+5]=a+3;}
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.triangles=indices;mesh.RecalculateNormals();return mesh;
        }
        public static Mesh Plot(CityBuilding pose,Func<float,float,float> height)
        {
            int nx=Math.Max(1,(int)Math.Ceiling(pose.width/.5f)),nz=Math.Max(1,(int)Math.Ceiling(pose.depth/.5f));
            var vertices=new Vector3[(nx+1)*(nz+1)];var indices=new int[nx*nz*6];var rotation=Quaternion.Euler(0,pose.yaw,0);
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++)
            {var p=rotation*new Vector3((pose.width-.12f)*(x/(float)nx-.5f),0,(pose.depth-.12f)*(z/(float)nz-.5f))+new Vector3(pose.x,0,pose.z);p.y=(height==null?pose.groundY:height(p.x,p.z))+.16f;vertices[z*(nx+1)+x]=p;}
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){int a=z*(nx+1)+x,b=(z*nx+x)*6;indices[b]=a;indices[b+1]=a+nx+1;indices[b+2]=a+1;indices[b+3]=a+1;indices[b+4]=a+nx+1;indices[b+5]=a+nx+2;}
            var mesh=new Mesh{name="Terrain-following zoning overlay"};mesh.vertices=vertices;mesh.triangles=indices;mesh.RecalculateNormals();return mesh;
        }
    }
}
