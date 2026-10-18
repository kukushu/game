using System;
using System.Collections.Generic;
using HarborCity.RoadCore;

namespace HarborCity
{
    // Read-only coverage projection. Roads and manual pipes remain their unique saved facts.
    // No utility edge is installed into city.utilities and no traffic direction is changed.
    internal sealed class CityRoadUtilityConduits
    {
        internal readonly CityUtilityNetwork network=new CityUtilityNetwork();
        internal readonly Dictionary<int,int> components=new Dictionary<int,int>();
        internal readonly int firstRoadEdge;
        int Node(float x,float z){int id=network.nextNode++;network.nodes.Add(new RoadNode{id=id,x=x,z=z});return id;}
        void Edge(int a,int b){if(a!=b)network.edges.Add(new UtilityEdge{id=network.nextEdge++,a=a,b=b});}
        internal CityRoadUtilityConduits(Network roads,CityUtilityNetwork manual)
        {
            var manualNodes=new Dictionary<int,int>();
            foreach(var node in manual.nodes)manualNodes.Add(node.id,Node(node.x,node.z));
            foreach(var edge in manual.edges)Edge(manualNodes[edge.a],manualNodes[edge.b]);
            firstRoadEdge=network.nextEdge;var junctions=new Dictionary<int,int>();
            foreach(int id in roads.RoadIds)
            {
                var settings=roads.Settings(id);var g=roads.Centreline(id);
                if(!junctions.TryGetValue(settings.from,out int from)){from=Node((float)g[0].x,(float)g[0].z);junctions.Add(settings.from,from);}
                if(!junctions.TryGetValue(settings.to,out int to)){to=Node((float)g[g.Count-1].x,(float)g[g.Count-1].z);junctions.Add(settings.to,to);}
                int last=from;
                for(int i=1;i<g.Count-1;i++)
                {
                    var a=g[i-1];var b=g[i];var c=g[i+1];double x=b.x-a.x,z=b.z-a.z,rx=c.x-b.x,rz=c.z-b.z;
                    if(Math.Abs(x*rz-z*rx)<1e-7&&x*rx+z*rz>0)continue;
                    int next=Node((float)b.x,(float)b.z);Edge(last,next);last=next;
                }
                Edge(last,to);
            }
            var parent=new int[network.nextNode];for(int i=1;i<parent.Length;i++)parent[i]=i;
            foreach(var edge in network.edges)Join(parent,edge.a,edge.b);
            // Connect manual pipes/wires where they touch the road conduit. Union only;
            // never draw a fictitious long connector that could supply unrelated buildings.
            var coverage=new CityUtilityCoverage(network);var candidates=new List<UtilityEdge>();
            foreach(var edge in network.edges)
            {
                if(edge.id>=firstRoadEdge)break;var a=network.Node(edge.a);var b=network.Node(edge.b);
                coverage.QueryBounds(Math.Min(a.x,b.x)-.08,Math.Min(a.z,b.z)-.08,Math.Max(a.x,b.x)+.08,Math.Max(a.z,b.z)+.08,candidates);
                foreach(var road in candidates)if(road.id>=firstRoadEdge&&Touches(a,b,network.Node(road.a),network.Node(road.b)))Join(parent,edge.a,road.a);
            }
            foreach(var node in network.nodes)components.Add(node.id,Find(parent,node.id));
        }
        static int Find(int[] parent,int id){while(parent[id]!=id){parent[id]=parent[parent[id]];id=parent[id];}return id;}
        static void Join(int[] parent,int a,int b){a=Find(parent,a);b=Find(parent,b);if(a<b)parent[b]=a;else parent[a]=b;}
        static bool Touches(RoadNode a,RoadNode b,RoadNode c,RoadNode d)
        {
            double x=b.x-a.x,z=b.z-a.z,rx=d.x-c.x,rz=d.z-c.z,den=x*rz-z*rx;
            if(Math.Abs(den)>1e-9)
            {double t=((c.x-a.x)*rz-(c.z-a.z)*rx)/den,u=((c.x-a.x)*z-(c.z-a.z)*x)/den;if(t>=0&&t<=1&&u>=0&&u<=1)return true;}
            return CityRoads.Distance(a,c,d)<.08f||CityRoads.Distance(b,c,d)<.08f||CityRoads.Distance(c,a,b)<.08f||CityRoads.Distance(d,a,b)<.08f;
        }
    }
}
