using System.Collections.Generic;
using UnityEngine;

namespace HarborCity
{
    public sealed class CityRoadView : MonoBehaviour
    {
        readonly List<Mesh> meshes = new List<Mesh>();
        Transform content;
        public static Vector3 Point(RoadNode n) => new Vector3(n.x,n.y,n.z);
        public static float SurfaceHeight(CityRoads roads,CityLandscape landscape,Vector3 p,int a,int b)
        {
            var na=roads.Node(a); var nb=roads.Node(b);
            if(na==null || nb==null) return landscape.Height(p.x,p.z);
            float t=CityRoads.Projection(new RoadNode{x=p.x,z=p.z},na,nb);
            var center=CityRoads.Lerp(na,nb,t);
            // Preserve the road's longitudinal profile and the terrain's cross slope.
            return center.y+landscape.Height(p.x,p.z)-landscape.Height(center.x,center.z);
        }
        public void Rebuild(CityModel city,CityLandscape landscape,Material asphalt,Material shoulder,Material paint)
        {
            if(content!=null) { content.gameObject.SetActive(false); Destroy(content.gameObject); }
            foreach(var mesh in meshes) Destroy(mesh); meshes.Clear();
            content=new GameObject("Road geometry").transform; content.SetParent(transform,false);
            var baseMesh=new Builder(); var roadMesh=new Builder(); var markings=new Builder();
            foreach(var edge in city.roads.edges)
            {
                Vector3 a=Point(city.roads.Node(edge.a)),b=Point(city.roads.Node(edge.b));
                float length=Vector3.Distance(a,b);
                Strip(baseMesh,a,b,CityRoads.Width+.18f,.065f,edge,city,landscape);
                Strip(roadMesh,a,b,CityRoads.Width,.08f,edge,city,landscape);
                float start=city.roads.Neighbors(edge.a).Count>2 ? 1.2f : .3f;
                float end=length-(city.roads.Neighbors(edge.b).Count>2 ? 1.2f : .3f);
                if(end>start) Strip(markings,Vector3.Lerp(a,b,start/length),Vector3.Lerp(a,b,Mathf.Min(end,start+.8f)/length),.07f,.10f,edge,city,landscape);
            }
            foreach(var n in city.roads.nodes) if(city.roads.Active(n.id))
            {
                // Caps also fill the union of arbitrary-angle road mouths at a junction.
                Disk(baseMesh,n,CityRoads.Width/2+.09f,.065f,landscape);
                Disk(roadMesh,n,CityRoads.Width/2,.08f,landscape);
            }
            AddMesh("Road shoulders",baseMesh,shoulder); AddMesh("Road surface",roadMesh,asphalt); AddMesh("Road markings",markings,paint);
        }
        void Strip(Builder builder,Vector3 a,Vector3 b,float width,float lift,RoadEdge edge,CityModel city,CityLandscape land)
        {
            Vector3 direction=b-a; direction.y=0; Vector3 right=Vector3.Cross(Vector3.up,direction.normalized)*width/2;
            int steps=Mathf.Max(1,Mathf.CeilToInt(direction.magnitude/.5f));
            for(int i=0;i<steps;i++)
            {
                Vector3 p=Vector3.Lerp(a,b,(float)i/steps),q=Vector3.Lerp(a,b,(float)(i+1)/steps);
                var points=new[]{p-right,p+right,q-right,q+right};
                for(int j=0;j<4;j++) points[j].y=SurfaceHeight(city.roads,land,points[j],edge.a,edge.b)+lift;
                builder.Quad(points);
            }
        }
        void Disk(Builder builder,RoadNode node,float radius,float lift,CityLandscape land)
        {
            Vector3 center=Point(node)+Vector3.up*lift;
            for(int i=0;i<20;i++)
            {
                float angle=i*Mathf.PI*2/20,next=(i+1)*Mathf.PI*2/20;
                Vector3 a=center+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                Vector3 b=center+new Vector3(Mathf.Cos(next)*radius,0,Mathf.Sin(next)*radius);
                a.y=node.y+land.Height(a.x,a.z)-land.Height(node.x,node.z)+lift;
                b.y=node.y+land.Height(b.x,b.z)-land.Height(node.x,node.z)+lift;
                builder.Triangle(center,b,a);
            }
        }
        void AddMesh(string name,Builder builder,Material material)
        {
            if(builder.vertices.Count==0) return;
            var mesh=new Mesh { name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(builder.vertices); mesh.SetTriangles(builder.triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); meshes.Add(mesh);
            var obj=new GameObject(name); obj.transform.SetParent(content,false);
            obj.AddComponent<MeshFilter>().sharedMesh=mesh; obj.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        void OnDestroy() { foreach(var mesh in meshes) if(mesh!=null) Destroy(mesh); }
        sealed class Builder
        {
            public readonly List<Vector3> vertices=new List<Vector3>();
            public readonly List<int> triangles=new List<int>();
            public void Quad(Vector3[] points)
            {
                int i=vertices.Count; vertices.AddRange(points); triangles.AddRange(new[]{i,i+2,i+1,i+1,i+2,i+3});
            }
            public void Triangle(Vector3 a,Vector3 b,Vector3 c)
            {
                int i=vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c); triangles.AddRange(new[]{i,i+1,i+2});
            }
        }
    }
}
