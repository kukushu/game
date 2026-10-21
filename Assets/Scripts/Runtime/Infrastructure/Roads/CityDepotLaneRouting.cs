using System;using System.Collections.Generic;using System.Linq;
namespace HarborCity.RoadCore {
public sealed partial class Network {
 // Derived from actual legal LaneConnections. No independent editable topology.
 internal sealed class DepotGraph {
  internal readonly Network network;internal readonly Drivable[] lanes;internal readonly Dictionary<long,int> index=new Dictionary<long,int>(CityRoadKeyComparer.Instance);internal readonly List<int>[] incoming;
  internal DepotGraph(Network n){network=n;lanes=n.Drivables.Where(d=>!d.IsConnection).OrderBy(d=>d.Id).ToArray();incoming=new List<int>[lanes.Length];for(int i=0;i<lanes.Length;i++){index.Add(lanes[i].Id,i);incoming[i]=new List<int>();}for(int i=0;i<lanes.Length;i++){List<Drivable> next;if(!n.outgoing.TryGetValue(lanes[i].To,out next))continue;foreach(var d in next)if(n.Contains(ConnectionId(lanes[i].Id,d.Id)))incoming[index[d.Id]].Add(i);}}
  internal double Link(int a,int b){var d=network.Get(ConnectionId(lanes[a].Id,lanes[b].Id));return d.Geometry.Length/d.Speed;}
  internal IEnumerable<int> Next(int a){List<Drivable> next;if(network.outgoing.TryGetValue(lanes[a].To,out next))foreach(var d in next)if(network.Contains(ConnectionId(lanes[a].Id,d.Id)))yield return index[d.Id];}
  internal long ReservedBytes=>lanes.LongLength*80+incoming.Sum(x=>(long)x.Count*4); // conservative logical graph accounting, not a managed-heap measurement
 }
 DepotGraph depotGraph;internal DepotGraph GetDepotGraph()=>depotGraph??(depotGraph=new DepotGraph(this));
 internal sealed class DepotTree {
  internal readonly double[] distance;internal readonly int[] hop,rank;readonly DepotGraph graph;readonly RoadAccess anchor;readonly bool reverse;
  internal DepotTree(DepotGraph g,RoadAccess a,bool backwards){using var sample=DepotRoutingTrace.Start(backwards?"reverse-tree":"forward-tree");graph=g;anchor=a;reverse=backwards;int count=g.lanes.Length;distance=new double[count];hop=new int[count];rank=new int[count];for(int i=0;i<count;i++){distance[i]=double.PositiveInfinity;hop[i]=-1;rank[i]=int.MaxValue;}var queue=new RouteQueue();int root=g.index[a.lane];distance[root]=backwards?(a.distance-g.lanes[root].StartDistance)/g.lanes[root].Speed:(g.lanes[root].EndDistance-a.distance)/g.lanes[root].Speed;queue.Push(a.lane,distance[root]);int ordinal=0;
   while(queue.Count>0){var item=queue.Pop();int v=g.index[item.id];if(item.cost!=distance[v]||rank[v]!=int.MaxValue)continue;rank[v]=ordinal++;if(sample!=null)sample.expanded++;
    if(backwards){foreach(int p in g.incoming[v]){double cost=distance[v]+g.Link(p,v)+g.lanes[p].TravelLength/g.lanes[p].Speed;if(rank[p]==int.MaxValue&&cost<distance[p]){distance[p]=cost;hop[p]=v;queue.Push(g.lanes[p].Id,cost);}}}
    else foreach(int t in g.Next(v)){double cost=distance[v]+g.Link(v,t)+g.lanes[t].TravelLength/g.lanes[t].Speed;if(rank[t]==int.MaxValue&&cost<distance[t]){distance[t]=cost;hop[t]=v;queue.Push(g.lanes[t].Id,cost);}}
   }
  }
  internal List<long> Find(RoadAccess point,out double start,out double end){return reverse?Return(point,out start,out end):Outbound(point,out start,out end);}
  List<long> Expand(List<int> ids){var r=new List<long>(ids.Count*2-1);for(int i=0;i<ids.Count;i++){long lane=graph.lanes[ids[i]].Id;if(i>0)r.Add(ConnectionId(r[r.Count-1],lane));r.Add(lane);}return r;}
  List<long> Outbound(RoadAccess goal,out double start,out double end){start=anchor.distance;end=goal.distance;int target=graph.index[goal.lane],root=graph.index[anchor.lane];if(goal.lane==anchor.lane&&goal.distance>=anchor.distance)return new List<long>{anchor.lane};double best=double.PositiveInfinity;int before=-1;
   foreach(int p in graph.incoming[target]){double candidate=distance[p]+graph.Link(p,target)+(goal.distance-graph.lanes[target].StartDistance)/graph.lanes[target].Speed;if(candidate<best||candidate==best&&before>=0&&rank[p]<rank[before]){best=candidate;before=p;}}
   if(before<0)return null;var path=new List<int>();for(int p=before;p>=0;p=hop[p]){path.Add(p);if(path.Count>hop.Length)throw new InvalidOperationException("Cyclic forward tree");}path.Reverse();if(path[0]!=root)throw new InvalidOperationException("Missing tree root");path.Add(target);return Expand(path);
  }
  List<long> Return(RoadAccess source,out double start,out double end){start=source.distance;end=anchor.distance;int root=graph.index[anchor.lane],first=graph.index[source.lane];if(source.lane==anchor.lane&&anchor.distance>=source.distance)return new List<long>{source.lane};double best=double.PositiveInfinity;int next=-1;
   foreach(int t in graph.Next(first)){double cost=(graph.lanes[first].EndDistance-source.distance)/graph.lanes[first].Speed+graph.Link(first,t)+distance[t];if(cost<best||cost==best&&next>=0&&graph.lanes[t].Id<graph.lanes[next].Id){best=cost;next=t;}}
   if(next<0)return null;var path=new List<int>{first};for(int p=next;p>=0;p=hop[p]){path.Add(p);if(path.Count>hop.Length+1)throw new InvalidOperationException("Cyclic reverse tree");}if(path[path.Count-1]!=root)throw new InvalidOperationException("Missing reverse root");return Expand(path);
  }
 }
}
// Owned by one World, bounded LRU. Network identity and depot access bits invalidate;
// unrelated building entrances, movement and clock do not invalidate static trees.
public sealed class DepotLaneRouting {
 sealed class Entry {internal int id;internal RoadAccess access;internal Network.DepotTree[] forward=new Network.DepotTree[2],backward=new Network.DepotTree[2];internal long age;}
 readonly Dictionary<int,Entry> entries=new Dictionary<int,Entry>();Network network;Network.DepotGraph graph;long clock;public const long ByteLimit=64L*1024*1024;public const int Capacity=128;
 public long Hits,Misses,Evictions,Invalidations,Queries;public int Count=>entries.Count;public long ReservedTreeBytes=>graph==null?0:entries.Count*(graph.lanes.LongLength*80+256);public long ReservedGraphBytes=>graph?.ReservedBytes??0;
 public void Clear(){Invalidations+=entries.Count;entries.Clear();network=null;graph=null;}
 static RoadAccess Copy(RoadAccess a)=>new RoadAccess{endpoint=a.endpoint,lane=a.lane,distance=a.distance,use=a.use,valid=a.valid};
 static bool Same(RoadAccess a,RoadAccess b)=>a!=null&&b!=null&&a.valid==b.valid&&a.lane==b.lane&&BitConverter.DoubleToInt64Bits(a.distance)==BitConverter.DoubleToInt64Bits(b.distance)&&a.use==b.use;
 internal bool TryFind(World owner,int depot,RoadAccess anchor,RoadAccess point,bool returning,out List<long> result,out double start,out double end){result=null;start=end=0;Queries++;if(anchor==null||point==null||!anchor.valid||!point.valid)return false;
  if(!ReferenceEquals(network,owner.Network)){Clear();network=owner.Network;}
  foreach(var old in entries.Values.ToArray())if(!Same(old.access,owner.GetAccess(old.id))){entries.Remove(old.id);Invalidations++;}
  graph=network.GetDepotGraph();if(graph.lanes.LongLength*80+256>ByteLimit)return false;
  if(!entries.TryGetValue(depot,out var e)){Misses++;while(entries.Count>=Capacity||(entries.Count+1)*(graph.lanes.LongLength*80+256)>ByteLimit){var victim=entries.Values.OrderBy(x=>x.age).First();entries.Remove(victim.id);Evictions++;}e=new Entry{id=depot,access=Copy(anchor)};entries.Add(depot,e);}else Hits++;e.age=++clock;
  RoadAccess a=anchor,b=AccessRouting.AtDirection(network,anchor,!network.Get(anchor.lane).Reverse);RoadAccess[] goals={a,b};double best=double.PositiveInfinity;
  var other=returning?null:AccessRouting.AtDirection(network,point,!network.Get(point.lane).Reverse);
  for(int i=0;i<2;i++){if(goals[i]==null)continue;var trees=returning?e.backward:e.forward;if(trees[i]==null)trees[i]=new Network.DepotTree(graph,Copy(goals[i]),returning);
   foreach(var destination in other==null?new[]{point}:new[]{point,other}){var route=trees[i].Find(destination,out double s,out double t);if(route==null)continue;double cost=new DispatchBlueprint(network,route,s,t).seconds;if(cost<best){best=cost;result=route;start=s;end=t;}}}
  return true; // true + null is a proven static NoRoute, never a skipped admission.
 }
}
}
