using System;
using System.Collections.Generic;
using System.Linq;

namespace HarborCity
{
    public enum UtilityKind { Electricity,Water }
    [Serializable] public sealed class UtilityEdge {public int id,a,b;}
    [Serializable] public sealed class CityUtilityNetwork
    {
        public int nextNode=1,nextEdge=1;
        public List<RoadNode> nodes=new List<RoadNode>();
        public List<UtilityEdge> edges=new List<UtilityEdge>();
        [NonSerialized] public int revision;
        [NonSerialized] Dictionary<int,RoadNode> lookup;
        public RoadNode Node(int id)
        {if(lookup==null || lookup.Count!=nodes.Count)lookup=nodes.ToDictionary(n=>n.id);return lookup.TryGetValue(id,out var node)?node:null;}
        public CityUtilityNetwork Copy()=>new CityUtilityNetwork{nextNode=nextNode,nextEdge=nextEdge,
            nodes=nodes.Select(n=>n.Copy()).ToList(),edges=edges.Select(e=>new UtilityEdge{id=e.id,a=e.a,b=e.b}).ToList(),revision=revision};
        static float Cross(float ax,float az,float bx,float bz)=>ax*bz-az*bx;
        int AddNode(RoadNode p)
        {
            var existing=nodes.FirstOrDefault(n=>CityRoads.Length(n,p)<.08f);if(existing!=null)return existing.id;
            var n=p.Copy();n.id=nextNode++;n.y=0;nodes.Add(n);return n.id;
        }
        void AddEdge(int a,int b)
        {if(a!=b && !edges.Any(e=>e.a==a && e.b==b || e.a==b && e.b==a))edges.Add(new UtilityEdge{id=nextEdge++,a=a,b=b});}
        public RoadNode Snap(float x,float z)
        {
            var p=new RoadNode{x=x,z=z};var near=nodes.OrderBy(n=>CityRoads.Length(n,p)).FirstOrDefault();
            if(near!=null && CityRoads.Length(near,p)<.5f)return near.Copy();
            float distance=.5f;RoadNode best=p;
            foreach(var e in edges)
            {var a=Node(e.a);var b=Node(e.b);var q=CityRoads.Lerp(a,b,CityRoads.Projection(p,a,b));if(CityRoads.Length(q,p)<distance){best=q;distance=CityRoads.Length(q,p);}}
            return best;
        }
        public CityUtilityNetwork Plan(RoadNode from,RoadNode to,out string error)
        {
            error="";
            if(from==null || to==null || !Position(from) || !Position(to) || CityRoads.Length(from,to)<.4f) {error="连接点无效、超界或线段过短";return null;}
            if(nodes.Count>=19990 || edges.Count>=39990 || nextNode>int.MaxValue-100 || nextEdge>int.MaxValue-100) {error="管线数量已达上限";return null;}
            var copy=Copy();var crosses=new List<RoadNode>{from,to};float dx=to.x-from.x,dz=to.z-from.z,length=CityRoads.Length(from,to);
            foreach(var e in edges)
            {
                var a=Node(e.a);var b=Node(e.b);float ex=b.x-a.x,ez=b.z-a.z,cross=Cross(dx,dz,ex,ez);
                if(Math.Abs(cross)<.0001f)
                {
                    if(Math.Abs(Cross(a.x-from.x,a.z-from.z,dx,dz))/length<.08f)
                    {
                        float p=((a.x-from.x)*dx+(a.z-from.z)*dz)/(length*length),q=((b.x-from.x)*dx+(b.z-from.z)*dz)/(length*length);
                        if(Math.Min(1,Math.Max(p,q))-Math.Max(0,Math.Min(p,q))>.001f){error="与现有管线重叠";return null;}
                    }
                    continue;
                }
                float t=Cross(a.x-from.x,a.z-from.z,ex,ez)/cross,u=Cross(a.x-from.x,a.z-from.z,dx,dz)/cross;
                if(t<-.00001f || t>1.00001f || u<-.00001f || u>1.00001f)continue;
                var pnt=CityRoads.Lerp(a,b,Math.Clamp(u,0,1));crosses.Add(pnt);
                if(CityRoads.Length(pnt,a)>.08f && CityRoads.Length(pnt,b)>.08f)
                {
                    int node=copy.AddNode(pnt);copy.edges.RemoveAll(line=>line.id==e.id);copy.AddEdge(e.a,node);copy.AddEdge(node,e.b);
                }
            }
            crosses=crosses.OrderBy(p=>CityRoads.Projection(p,from,to)).ToList();var unique=new List<RoadNode>();
            foreach(var p in crosses)if(unique.Count==0 || CityRoads.Length(p,unique[unique.Count-1])>.08f)unique.Add(p);
            for(int i=1;i<unique.Count;i++)copy.AddEdge(copy.AddNode(unique[i-1]),copy.AddNode(unique[i]));
            copy.revision++;if(!copy.Valid()){error="管线几何校验失败";return null;}return copy;
        }
        public bool Remove(int id)
        {int count=edges.RemoveAll(e=>e.id==id);if(count==0)return false;revision++;return true;}
        public UtilityEdge Pick(float x,float z,float radius=.8f)
        {
            var p=new RoadNode{x=x,z=z};UtilityEdge best=null;
            foreach(var edge in edges){float d=CityRoads.Distance(p,Node(edge.a),Node(edge.b));if(d<radius){best=edge;radius=d;}}
            return best;
        }
        public Dictionary<int,int> Components()
        {
            var adjacent=nodes.ToDictionary(n=>n.id,n=>new List<int>());foreach(var e in edges){adjacent[e.a].Add(e.b);adjacent[e.b].Add(e.a);}
            var components=new Dictionary<int,int>();
            foreach(var node in nodes)if(!components.ContainsKey(node.id) && adjacent[node.id].Count>0)
            {
                var queue=new Queue<int>();queue.Enqueue(node.id);components[node.id]=node.id;
                while(queue.Count>0){int n=queue.Dequeue();foreach(int next in adjacent[n])if(!components.ContainsKey(next)){components[next]=node.id;queue.Enqueue(next);}}
            }
            return components;
        }
        public bool Valid()
        {
            if(nodes==null || edges==null || nodes.Count>20000 || edges.Count>40000 || nextNode<1 || nextEdge<1) return false;
            var ids=new HashSet<int>();var lines=new HashSet<int>();var pairs=new HashSet<string>();
            foreach(var n in nodes)if(n==null || n.id<1 || n.id>=nextNode || !ids.Add(n.id) || !Position(n) || n.y!=0)return false;
            lookup=null;
            foreach(var e in edges)if(e==null || e.id<1 || e.id>=nextEdge || !lines.Add(e.id) || !ids.Contains(e.a) || !ids.Contains(e.b) || e.a==e.b
                || !pairs.Add(Math.Min(e.a,e.b)+":"+Math.Max(e.a,e.b)) || CityRoads.Length(Node(e.a),Node(e.b))<.08f)return false;
            return true;
        }
        static bool Position(RoadNode p)=>!float.IsNaN(p.x) && !float.IsNaN(p.z) && !float.IsInfinity(p.x) && !float.IsInfinity(p.z)
            && Math.Abs(p.x)<=CityModel.BuildHalfSize && Math.Abs(p.z)<=CityModel.BuildHalfSize;
    }
}
