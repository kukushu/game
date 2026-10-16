using System;
using System.Collections.Generic;
namespace HarborCity.RoadCore
{
    // Immutable-network derived connectivity. SCCs make mutual reachability local;
    // cross-component queries retain a bounded DAG search cache, not an L-by-L matrix.
    internal sealed class LaneReachability
    {
        const int MaximumSources=64;
        readonly Dictionary<long,int> indices=new Dictionary<long,int>(CityRoadKeyComparer.Instance);
        readonly int[] component;
        readonly bool[] cycles;
        readonly List<int>[] exits;
        readonly Dictionary<int,HashSet<int>> reached=new Dictionary<int,HashSet<int>>();
        readonly Queue<int> sources=new Queue<int>();
        struct Frame {internal int node,next;internal Frame(int node,int next){this.node=node;this.next=next;}}
        internal LaneReachability(Network network)
        {
            foreach(var d in network.Drivables)if(!d.IsConnection)indices.Add(d.Id,indices.Count);
            int count=indices.Count;var forward=new List<int>[count];var reverse=new List<int>[count];
            foreach(var d in network.Drivables)if(d.IsConnection)
            {
                var link=(LaneConnection)d;int a=indices[link.Incoming.Id],b=indices[link.Outgoing.Id];
                if(forward[a]==null)forward[a]=new List<int>();forward[a].Add(b);
                if(reverse[b]==null)reverse[b]=new List<int>();reverse[b].Add(a);
            }
            // Iterative postorder avoids recursion depth depending on city size.
            var visited=new bool[count];var order=new List<int>(count);var frames=new Stack<Frame>();
            for(int root=0;root<count;root++)
            {
                if(visited[root])continue;visited[root]=true;frames.Push(new Frame(root,0));
                while(frames.Count>0)
                {
                    var frame=frames.Pop();var next=forward[frame.node];
                    if(next!=null&&frame.next<next.Count)
                    {
                        int child=next[frame.next];frames.Push(new Frame(frame.node,frame.next+1));
                        if(!visited[child]){visited[child]=true;frames.Push(new Frame(child,0));}
                    }
                    else order.Add(frame.node);
                }
            }
            component=new int[count];for(int i=0;i<count;i++)component[i]=-1;
            var sizes=new List<int>();var stack=new Stack<int>();
            for(int i=order.Count-1;i>=0;i--)
            {
                int root=order[i];if(component[root]>=0)continue;int id=sizes.Count,size=0;component[root]=id;stack.Push(root);
                while(stack.Count>0)
                {
                    int node=stack.Pop();size++;var next=reverse[node];if(next==null)continue;
                    foreach(int child in next)if(component[child]<0){component[child]=id;stack.Push(child);}
                }
                sizes.Add(size);
            }
            cycles=new bool[sizes.Count];exits=new List<int>[sizes.Count];
            for(int i=0;i<sizes.Count;i++)cycles[i]=sizes[i]>1;
            for(int node=0;node<count;node++)
            {
                if(forward[node]==null)continue;int from=component[node];
                foreach(int child in forward[node])
                {
                    int to=component[child];if(from==to){if(child==node)cycles[from]=true;continue;}
                    if(exits[from]==null)exits[from]=new List<int>();exits[from].Add(to);
                }
            }
            foreach(var edges in exits)if(edges!=null)
            {
                edges.Sort();int written=0;for(int i=0;i<edges.Count;i++)if(written==0||edges[i]!=edges[written-1])edges[written++]=edges[i];
                if(written<edges.Count)edges.RemoveRange(written,edges.Count-written);
            }
        }
        internal bool CanReach(RoadAccess source,RoadAccess target)=>CanReach(source.lane,source.distance,target.lane,target.distance);
        internal bool CanReach(long source,double sourceDistance,long target,double targetDistance)
        {
            if(!indices.TryGetValue(source,out int a)||!indices.TryGetValue(target,out int b))return false;
            if(source==target&&targetDistance>=sourceDistance)return true;
            int from=component[a],to=component[b];
            if(from==to)return source!=target||cycles[from];
            return Reachable(from)?.Contains(to)==true;
        }
        internal bool HasCycle(long source)=>indices.TryGetValue(source,out int node)&&cycles[component[node]];
        HashSet<int> Reachable(int source)
        {
            if(exits[source]==null)return null;
            if(!reached.TryGetValue(source,out var set))
            {
                set=new HashSet<int>();var queue=new Queue<int>();foreach(int next in exits[source])if(set.Add(next))queue.Enqueue(next);
                while(queue.Count>0){int node=queue.Dequeue();if(exits[node]==null)continue;foreach(int next in exits[node])if(set.Add(next))queue.Enqueue(next);}
                if(reached.Count==MaximumSources)reached.Remove(sources.Dequeue());reached.Add(source,set);sources.Enqueue(source);
            }
            return set;
        }
    }
}
