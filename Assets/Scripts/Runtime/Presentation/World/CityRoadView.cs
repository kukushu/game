using System.Collections.Generic;
using UnityEngine;

namespace HarborCity
{
    public sealed class CityRoadView : MonoBehaviour
    {
        readonly List<Mesh> meshes = new List<Mesh>();
        Transform content;
        public bool underground;
        // Explicit native validation only. Normal gameplay always uses shared geometry.
#if CITY_ROAD_RENDER_LEGACY
        internal static bool LegacyGeometryForValidation=true;
#else
        internal static bool LegacyGeometryForValidation;
#endif
        public static Vector3 Point(RoadNode n) => new Vector3(n.x,n.y,n.z);
        public static float SurfaceHeight(CityRoads roads,CityLandscape landscape,Vector3 p,int a,int b)
        {
            if(!LegacyGeometryForValidation && roads.GetLogicalTopology().TryProjectCenter(a,b,p.x,p.z,out var center))
                return center.Y+(center.Ground?landscape.Height(p.x,p.z)-landscape.Height(center.X,center.Z):0);
            return LegacySurfaceHeight(roads,landscape,p,a,b);
        }
        internal static float LegacySurfaceHeight(CityRoads roads,CityLandscape landscape,Vector3 p,int a,int b)
        {
            var na=roads.Node(a); var nb=roads.Node(b);
            if(na==null || nb==null) return landscape.Height(p.x,p.z);
            float dx=nb.x-na.x,dz=nb.z-na.z,length2=dx*dx+dz*dz;
            float t=length2<.0001f?0:Mathf.Clamp01(((p.x-na.x)*dx+(p.z-na.z)*dz)/length2);
            float centerX=na.x+dx*t,centerZ=na.z+dz*t,centerY=na.y+(nb.y-na.y)*t;
            // Preserve the road's longitudinal profile and the terrain's cross slope.
            return centerY+(roads.Ground(a,b)?landscape.Height(p.x,p.z)-landscape.Height(centerX,centerZ):0);
        }
        public void Rebuild(CityModel city,CityLandscape landscape,Material asphalt,Material shoulder,Material paint)
        {
            if(content!=null) { content.gameObject.SetActive(false); Destroy(content.gameObject); }
            foreach(var mesh in meshes) Destroy(mesh); meshes.Clear();
            content=new GameObject("Road geometry").transform; content.SetParent(transform,false);
            var baseMesh=new Builder(); var roadMesh=new Builder(); var gravelMesh=new Builder(); var markings=new Builder();
            var supports=new Builder();
            var pavedNodes=new HashSet<int>();foreach(var e in city.roads.edges)if(e.kind!=RoadKind.Gravel){pavedNodes.Add(e.a);pavedNodes.Add(e.b);}
            var entry=city.roads.Node(CityRoads.Entrance);
            if(entry!=null && !city.roads.Active(entry.id)) Disk(markings,entry,.6f,.10f,landscape);
            foreach(var edge in city.roads.edges)
            {
                Vector3 a,b;
                if(!LegacyGeometryForValidation && city.roads.GetLogicalTopology().TryGetTraversal(edge.a,edge.b,out var geometry))
                {a=new Vector3(geometry.From.X,geometry.From.Y,geometry.From.Z);b=new Vector3(geometry.To.X,geometry.To.Y,geometry.To.Z);}
                else{a=Point(city.roads.Node(edge.a));b=Point(city.roads.Node(edge.b));}
                if(edge.structure==RoadStructure.Tunnel && !underground)
                {
                    foreach(int node in new[]{edge.a,edge.b})if(city.roads.Neighbors(node).Exists(other=>!city.roads.Underground(node,other)))
                    {
                        var p=Point(city.roads.Node(node));p.y=landscape.Height(p.x,p.z)+.1f;
                        var right=Vector3.Cross(Vector3.up,(b-a).normalized).normalized*(CityRoadTypes.Width(edge.kind)/2+.2f);
                        Column(supports,p-right+Vector3.up*1.4f,p-right,.16f);Column(supports,p+right+Vector3.up*1.4f,p+right,.16f);
                    }
                    continue;
                }
                float length=Vector3.Distance(a,b);
                float width=CityRoadTypes.Width(edge.kind);
                if(edge.structure==RoadStructure.Elevated)
                {
                    var center=(a+b)/2;float ground=landscape.Height(center.x,center.z);
                    if(center.y-ground>1){var top=center+Vector3.up*.04f;var bottom=new Vector3(center.x,ground,center.z);Column(supports,top,bottom,.28f);}
                    var right=Vector3.Cross(Vector3.up,(b-a).normalized).normalized*width/2;
                    supports.Quad(new[]{a-right+Vector3.up*.065f,a-right-Vector3.up*.2f,b-right+Vector3.up*.065f,b-right-Vector3.up*.2f});
                    supports.Quad(new[]{a+right-Vector3.up*.2f,a+right+Vector3.up*.065f,b+right-Vector3.up*.2f,b+right+Vector3.up*.065f});
                }
                if(edge.structure==RoadStructure.Tunnel)
                {
                    foreach(var p in new[]{a,b})if(city.roads.edges.Exists(e=>e.structure!=RoadStructure.Tunnel && (e.a==(p==a?edge.a:edge.b) || e.b==(p==a?edge.a:edge.b))))
                    {
                        var right=Vector3.Cross(Vector3.up,(b-a).normalized).normalized*(width/2+.2f);
                        Column(supports,p-right+Vector3.up*1.4f,p-right,.16f);Column(supports,p+right+Vector3.up*1.4f,p+right,.16f);
                    }
                }
                Strip(baseMesh,a,b,width+.18f,.065f,edge,city,landscape);
                Strip(edge.kind==RoadKind.Gravel?gravelMesh:roadMesh,a,b,width,.08f,edge,city,landscape);
                float start=city.roads.Neighbors(edge.a).Count>2 ? 1.2f : .3f;
                float end=length-(city.roads.Neighbors(edge.b).Count>2 ? 1.2f : .3f);
                if(edge.kind!=RoadKind.Gravel && !CityRoadTypes.LimitedAccess(edge.kind) && end>start) Strip(markings,Vector3.Lerp(a,b,start/length),Vector3.Lerp(a,b,Mathf.Min(end,start+.8f)/length),.07f,.10f,edge,city,landscape);
                if(edge.kind==RoadKind.Highway && end>start)
                {
                    var right=Vector3.Cross(Vector3.up,(b-a).normalized)*.55f;
                    foreach(float side in new[]{-1f,1f})Strip(markings,Vector3.Lerp(a,b,start/length)+right*side,Vector3.Lerp(a,b,Mathf.Min(end,start+.8f)/length)+right*side,.05f,.10f,edge,city,landscape);
                }
                if(edge.kind==RoadKind.FourLane && end>start)
                {
                    var right=Vector3.Cross(Vector3.up,(b-a).normalized)*1.1f;
                    foreach(float side in new[]{-1f,1f})Strip(markings,Vector3.Lerp(a,b,start/length)+right*side,Vector3.Lerp(a,b,Mathf.Min(end,start+.8f)/length)+right*side,.05f,.10f,edge,city,landscape);
                }
                if(edge.oneWay)
                {
                    var center=(a+b)/2;var forward=(b-a).normalized;var right=Vector3.Cross(Vector3.up,forward).normalized;
                    var tip=center+forward*.4f;var left=center-forward*.2f-right*.24f;var wing=center-forward*.2f+right*.24f;
                    tip.y=SurfaceHeight(city.roads,landscape,tip,edge.a,edge.b)+.11f;
                    left.y=SurfaceHeight(city.roads,landscape,left,edge.a,edge.b)+.11f;
                    wing.y=SurfaceHeight(city.roads,landscape,wing,edge.a,edge.b)+.11f;
                    markings.Triangle(tip,wing,left);
                }
            }
            foreach(var n in city.roads.nodes) if(city.roads.Active(n.id))
            {
                if(!underground && city.roads.Neighbors(n.id).TrueForAll(other=>city.roads.Underground(n.id,other)))continue;
                // Caps also fill the union of arbitrary-angle road mouths at a junction.
                float width=CityRoads.Width;foreach(var other in city.roads.Neighbors(n.id))width=Mathf.Max(width,city.roads.EdgeWidth(n.id,other));
                bool groundNode=city.roads.Neighbors(n.id).TrueForAll(other=>city.roads.Ground(n.id,other));
                Disk(baseMesh,n,width/2+.09f,.065f,landscape,groundNode);
                bool gravel=!pavedNodes.Contains(n.id);
                Disk(gravel?gravelMesh:roadMesh,n,width/2,.08f,landscape,groundNode);
                if(city.roads.Neighbors(n.id).Count>=3)
                foreach(int arm in city.roads.Neighbors(n.id))
                {
                    var edge=city.roads.edges.Find(e=>e.a==n.id && e.b==arm || e.b==n.id && e.a==arm);
                    if(edge==null || edge.kind==RoadKind.Gravel)continue;
                    var crossing=CityJunctions.Crosswalk(city.roads,n.id,arm);if(crossing==null)continue;
                    var a=Point(crossing[0]);var b=Point(crossing[1]);float span=Vector3.Distance(a,b);
                    var forward=Point(city.roads.Node(arm))-Point(n);forward.y=0;forward.Normalize();
                    for(float stripe=.18f;stripe<span-.1f;stripe+=.35f)
                    {
                        var p=Vector3.Lerp(a,b,stripe/span);
                        Strip(markings,p-forward*.27f,p+forward*.27f,.16f,.105f,edge,city,landscape);
                    }
                }
            }
            AddMesh("Bridge supports and tunnel portals",supports,shoulder);
            AddMesh("Road shoulders",baseMesh,shoulder); AddMesh("Road surface",roadMesh,asphalt); AddMesh("Gravel surface",gravelMesh,shoulder); AddMesh("Road markings",markings,paint);
        }
        void Column(Builder builder,Vector3 top,Vector3 bottom,float radius)
        {
            var offsets=new[]{new Vector3(-radius,0,-radius),new Vector3(radius,0,-radius),new Vector3(radius,0,radius),new Vector3(-radius,0,radius)};
            for(int i=0;i<4;i++){int j=(i+1)%4;builder.Quad(new[]{top+offsets[i],bottom+offsets[i],top+offsets[j],bottom+offsets[j]});}
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
        void Disk(Builder builder,RoadNode node,float radius,float lift,CityLandscape land,bool groundNode=true)
        {
            Vector3 center=Point(node)+Vector3.up*lift;
            for(int i=0;i<20;i++)
            {
                float angle=i*Mathf.PI*2/20,next=(i+1)*Mathf.PI*2/20;
                Vector3 a=center+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                Vector3 b=center+new Vector3(Mathf.Cos(next)*radius,0,Mathf.Sin(next)*radius);
                a.y=node.y+(groundNode?land.Height(a.x,a.z)-land.Height(node.x,node.z):0)+lift;
                b.y=node.y+(groundNode?land.Height(b.x,b.z)-land.Height(node.x,node.z):0)+lift;
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
