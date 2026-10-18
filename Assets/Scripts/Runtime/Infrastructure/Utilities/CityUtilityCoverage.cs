using System;
using System.Collections.Generic;

namespace HarborCity
{
    // Ephemeral broad phase for one supply rebuild. The network and the existing
    // exact distance predicate remain authoritative; no persistent invalidation state.
    internal sealed class CityUtilityCoverage
    {
        const double Cell=16;
        readonly CityUtilityNetwork network;
        readonly Dictionary<(int,int),List<int>> cells=new Dictionary<(int,int),List<int>>();
        readonly int[] visited;
        readonly List<int> ordered=new List<int>();
        int stamp;
        internal CityUtilityCoverage(CityUtilityNetwork network)
        {
            this.network=network;visited=new int[network.edges.Count];
            for(int i=0;i<network.edges.Count;i++)
            {
                var edge=network.edges[i];var a=network.Node(edge.a);var b=network.Node(edge.b);
                int x=(int)Math.Floor(a.x/Cell),z=(int)Math.Floor(a.z/Cell),endX=(int)Math.Floor(b.x/Cell),endZ=(int)Math.Floor(b.z/Cell);
                double dx=(double)b.x-a.x,dz=(double)b.z-a.z;int sx=Math.Sign(dx),sz=Math.Sign(dz);
                double tx=sx==0?double.PositiveInfinity:((sx>0?(x+1)*Cell:x*Cell)-a.x)/dx;
                double tz=sz==0?double.PositiveInfinity:((sz>0?(z+1)*Cell:z*Cell)-a.z)/dz;
                double stepX=sx==0?double.PositiveInfinity:Cell/Math.Abs(dx),stepZ=sz==0?double.PositiveInfinity:Cell/Math.Abs(dz);
                Add(x,z,i);int remaining=Math.Abs(endX-x)+Math.Abs(endZ-z)+2;
                while(x!=endX||z!=endZ)
                {
                    if(--remaining<0)throw new InvalidOperationException("Utility coverage traversal failed");
                    // Once one coordinate reaches its endpoint, never step beyond it
                    // due to rounding at an endpoint lying exactly on a cell boundary.
                    if(x==endX){z+=sz;tz+=stepZ;}
                    else if(z==endZ){x+=sx;tx+=stepX;}
                    else if(tx<tz){x+=sx;tx+=stepX;}
                    else if(tz<tx){z+=sz;tz+=stepZ;}
                    else {Add(x+sx,z,i);Add(x,z+sz,i);x+=sx;z+=sz;tx+=stepX;tz+=stepZ;}
                    Add(x,z,i);
                }
            }
        }
        void Add(int x,int z,int edge)
        {if(!cells.TryGetValue((x,z),out var list))cells.Add((x,z),list=new List<int>());list.Add(edge);}
        internal void Query(CityBuilding building,float reach,List<UtilityEdge> result)
        {
            // Same centre-distance/half-minimum-footprint rule as LineDistance.
            // Conservative guard covers float rounding; exact predicate still filters.
            double radius=reach+Math.Min(building.width,building.depth)/2+.01;
            QueryBounds(building.x-radius,building.z-radius,building.x+radius,building.z+radius,result);
        }
        internal void QueryBounds(double minX,double minZ,double maxX,double maxZ,List<UtilityEdge> result)
        {
            result.Clear();ordered.Clear();if(stamp==int.MaxValue){Array.Clear(visited,0,visited.Length);stamp=0;}stamp++;
            int loX=(int)Math.Floor(minX/Cell),hiX=(int)Math.Floor(maxX/Cell);
            int loZ=(int)Math.Floor(minZ/Cell),hiZ=(int)Math.Floor(maxZ/Cell);
            for(int x=loX;x<=hiX;x++)for(int z=loZ;z<=hiZ;z++)if(cells.TryGetValue((x,z),out var list))foreach(int index in list)
            {if(visited[index]==stamp)continue;visited[index]=stamp;ordered.Add(index);}
            ordered.Sort();foreach(int index in ordered)result.Add(network.edges[index]);
        }
    }
}
