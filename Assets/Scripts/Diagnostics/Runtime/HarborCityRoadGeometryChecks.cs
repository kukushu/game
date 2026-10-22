using System;
using System.Collections.Generic;
using System.Linq;
using HarborCity.RoadCore;
using UnityEngine;

namespace HarborCity
{
    // Explicit test only. A frozen pre-change Ribbon factory can be supplied by
    // the disposable validation project; no reference renderer in production.
    public static class HarborCityRoadGeometryChecks
    {
        sealed class Data {internal Vector3[] vertices,normals;internal int[] indices;internal int v,t;}
        public readonly struct Result
        {
            public readonly long checks,referenceVertices,vertices,triangles;
            internal Result(long checks,long original,long vertices,long triangles){this.checks=checks;referenceVertices=original;this.vertices=vertices;this.triangles=triangles;}
        }
        static bool Same(Vector3 a,Vector3 b)=>BitConverter.SingleToInt32Bits(a.x)==BitConverter.SingleToInt32Bits(b.x)&&BitConverter.SingleToInt32Bits(a.y)==BitConverter.SingleToInt32Bits(b.y)&&BitConverter.SingleToInt32Bits(a.z)==BitConverter.SingleToInt32Bits(b.z);
        public static Result Verify(World world,HarborCityRoadViews views,Func<float,float,float> height,Material material,Func<Geometry,double,double,Func<float,float,float>,Mesh> reference)
        {
            long checks=0,original=0,vertices=0,triangles=0;Action<bool,string> check=(ok,why)=>{checks++;if(!ok)throw new InvalidOperationException(why);};
            var data=new Dictionary<GameObject,Data>();
            foreach(var go in views.RenderObjects)
            {
                check(go.transform.position==Vector3.zero&&go.transform.rotation==Quaternion.identity&&go.transform.localScale==Vector3.one,"Road batch transform changed");
                var renderer=go.GetComponent<MeshRenderer>();check(renderer.enabled&&go.activeInHierarchy&&renderer.sharedMaterial==material,"Roads hidden or material changed");
                var mesh=go.GetComponent<MeshFilter>().sharedMesh;check(mesh.bounds.size.x<=views.ChunkSize+2&&mesh.bounds.size.z<=views.ChunkSize+2,"Road chunk has unbounded spatial extent");
                var d=new Data{vertices=mesh.vertices,normals=mesh.normals,indices=mesh.triangles};data.Add(go,d);vertices+=d.vertices.Length;triangles+=d.indices.Length/3;
                check(mesh.indexFormat==UnityEngine.Rendering.IndexFormat.UInt32||d.vertices.Length<=ushort.MaxValue,"Vertex index truncation");
                foreach(var p in d.vertices){var min=mesh.bounds.min;var max=mesh.bounds.max;check(p.x>=min.x-.0001f&&p.y>=min.y-.0001f&&p.z>=min.z-.0001f&&p.x<=max.x+.0001f&&p.y<=max.y+.0001f&&p.z<=max.z+.0001f,"Mesh bounds exclude geometry");}
            }
            int logical=0;long expectedTriangles=0;
            foreach(var drivable in world.Network.Drivables.OrderBy(d=>d.Id))
            {
                if(drivable.TravelLength<1e-8)continue;logical++;var expected=reference(drivable.Geometry,drivable.StartDistance,drivable.EndDistance,height);
                try
                {
                    var v=expected.vertices;var n=expected.normals;var t=expected.triangles;original+=v.Length;expectedTriangles+=t.Length/3;int covered=0;
                    foreach(var piece in views.Pieces(drivable.Id))
                    {
                        check(piece.first==covered&&piece.count>0&&piece.first+piece.count<=v.Length/2-1,"Missing/duplicate ribbon spans");covered+=piece.count;
                        var go=views.ChunkObject(piece.key);check(go!=null&&data.ContainsKey(go),"Drivable references missing chunk");var actual=data[go];int offset=actual.v;
                        for(int i=piece.first*2;i<=(piece.first+piece.count)*2+1;i++)
                        {check(actual.v<actual.vertices.Length&&Same(v[i],actual.vertices[actual.v])&&Same(n[i],actual.normals[actual.v]),"Frozen ribbon position or normal differs at drivable "+drivable.Id);actual.v++;}
                        for(int span=piece.first;span<piece.first+piece.count;span++)
                        {
                            var centre=(v[span*2]+v[span*2+1]+v[span*2+2]+v[span*2+3])*.25f;
                            check(piece.key==((int)Math.Floor(centre.x/views.ChunkSize),(int)Math.Floor(centre.z/views.ChunkSize)),"Incorrect spatial assignment");
                            for(int j=0;j<6;j++){check(actual.t<actual.indices.Length&&actual.indices[actual.t]==offset+t[span*6+j]-piece.first*2,"Frozen triangle winding/index differs");actual.t++;}
                        }
                    }
                    check(covered==Math.Max(0,v.Length/2-1),"Original ribbon spans dropped");
                }
                finally{UnityEngine.Object.DestroyImmediate(expected);}
            }
            check(logical==views.Count&&data.Count==views.RendererCount,"Logical and renderer ownership counts incorrect");
            foreach(var d in data.Values)check(d.v==d.vertices.Length&&d.t==d.indices.Length,"Extra/unverified chunk geometry");
            check(expectedTriangles==triangles,"Original triangles dropped or duplicated");return new Result(checks,original,vertices,triangles);
        }
    }
}
