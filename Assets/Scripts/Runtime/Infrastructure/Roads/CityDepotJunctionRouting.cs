using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace HarborCity.RoadCore
{
    // Experimental weighted junction-state Dijkstra. The immutable
    // Network remains authoritative. States merge incoming lanes only at the SAME
    // junction, IDENTICAL legal outgoing lane sets AND bit-identical turn costs.
    // Labels are at lane ends: incoming travel is already paid. Equal future
    // costs are a weighted bisimulation; differing connection costs must split.
    public sealed class DepotJunctionRouting
    {
        // Diagnostic only; no change to queue order or costs.
        public static bool CountSearches;public static long SearchCalls,SearchExpanded;
        sealed class Signature : IEquatable<Signature>
        {
            internal long terminal;internal int junction; internal long[] exits;internal double[] costs;
            public bool Equals(Signature b){if(b==null||junction!=b.junction||terminal!=b.terminal||exits.Length!=b.exits.Length)return false;for(int i=0;i<exits.Length;i++)if(exits[i]!=b.exits[i]||BitConverter.DoubleToInt64Bits(costs[i])!=BitConverter.DoubleToInt64Bits(b.costs[i]))return false;return true;}
            public override bool Equals(object b)=>Equals(b as Signature);
            public override int GetHashCode(){unchecked{int h=junction^terminal.GetHashCode();for(int i=0;i<exits.Length;i++){h=h*31+exits[i].GetHashCode();h=h*31+costs[i].GetHashCode();}return h;}}
        }
        struct Back {internal int state;internal long lane;internal double turn,travel;}
        sealed class Graph
        {
            internal readonly Dictionary<long,int> state=new Dictionary<long,int>(CityRoadKeyComparer.Instance);
            internal readonly List<long[]> exits=new List<long[]>();
            internal readonly List<double[]> costs=new List<double[]>(),travel=new List<double[]>();
            internal readonly List<List<long>> members=new List<List<long>>();
            internal readonly List<List<Back>> reverse=new List<List<Back>>();
            internal readonly Dictionary<long,List<int>> upstream=new Dictionary<long,List<int>>(CityRoadKeyComparer.Instance);
            internal readonly Network.RouteQueue heap=new Network.RouteQueue();internal int laneCount,topologyStates;internal long bytes;
            internal Graph(Network n,HashSet<long> terminals)
            {
                var lanes=new List<Drivable>();var legal=new Dictionary<long,List<long>>(CityRoadKeyComparer.Instance);
                foreach(var d in n.Drivables)if(!d.IsConnection){lanes.Add(d);legal.Add(d.Id,new List<long>());}
                foreach(var d in n.Drivables)if(d is LaneConnection c)legal[c.Incoming.Id].Add(c.Outgoing.Id);
                lanes.Sort((a,b)=>a.Id.CompareTo(b.Id));laneCount=lanes.Count;
                var groups=new Dictionary<Signature,int>();var topology=new HashSet<string>();
                foreach(var lane in lanes)
                {
                    var list=legal[lane.Id];list.Sort();var weights=new double[list.Count];for(int i=0;i<list.Count;i++){var connection=n.Get(Network.ConnectionId(lane.Id,list[i]));weights[i]=connection.Geometry.Length/connection.Speed;}
                    var key=new Signature{terminal=terminals.Contains(lane.Id)?lane.Id:0,junction=lane.To,exits=list.ToArray(),costs=weights};topology.Add(lane.To+":"+string.Join(",",list));
                    if(!groups.TryGetValue(key,out int id)){id=exits.Count;groups.Add(key,id);exits.Add(key.exits);costs.Add(weights);var spans=new double[list.Count];for(int i=0;i<list.Count;i++){var d=n.Get(list[i]);spans[i]=d.TravelLength/d.Speed;}travel.Add(spans);members.Add(new List<long>());reverse.Add(new List<Back>());}
                    state.Add(lane.Id,id);members[id].Add(lane.Id);
                }
                topologyStates=topology.Count;
                for(int i=0;i<exits.Count;i++)for(int e=0;e<exits[i].Length;e++)
                {
                    long lane=exits[i][e];reverse[state[lane]].Add(new Back{state=i,lane=lane,turn=costs[i][e],travel=travel[i][e]});
                    if(!upstream.TryGetValue(lane,out var list))upstream.Add(lane,list=new List<int>());list.Add(i);
                    bytes+=56; // edge payload estimate, NOT managed retained heap
                }
                bytes+=state.Count*40L+exits.Count*100L;
            }
        }
        sealed class Entry
        {
            internal int depot;internal long stamp;internal RoadAccess anchor,other;
            internal int[] parent,next;internal long[] via,exit;internal double[] forwardCost,returnCost;
        }
        public readonly struct Sample
        {
            public readonly string kind;public readonly int depot,expanded;public readonly double ms;public readonly long bytes;
            internal Sample(string kind,int depot,int expanded,long start,long allocated){this.kind=kind;this.depot=depot;this.expanded=expanded;ms=(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;bytes=allocated<0?-1:Allocation()-allocated;}
        }
        readonly Dictionary<int,Entry> entries=new Dictionary<int,Entry>();
        readonly HashSet<long> terminals=new HashSet<long>();
        Func<int,RoadAccess> resolveAccess;Func<long> accessRevision;long pinRevision=-1;
        HashSet<int> eligible;Network network;Graph graph;long clock;
        public int MaximumDepots {get;} public long MaximumTreeBytes {get;}
        public long Queries,Hits,Misses,Evictions,Invalidations,LocalChecks,Fallbacks,Unreachable,Builds,Expanded;
        public long InvalidAccessFallbacks,InactiveDepotFallbacks,BudgetFallbacks;public double FallbackMilliseconds;
        internal void FallbackCost(long started){FallbackMilliseconds+=(Stopwatch.GetTimestamp()-started)*1000.0/Stopwatch.Frequency;}
        public bool Trace;public readonly List<Sample> Samples=new List<Sample>();
        public int Nodes=>graph==null?0:graph.exits.Count;
        public int TopologyOnlyNodes=>graph==null?0:graph.topologyStates;
        internal IEnumerable<long> Members(int state)=>graph.members[state];
        public int Lanes=>graph==null?0:graph.laneCount;
        public int Count=>entries.Count;
        public long TreeBytes {get;private set;}
        public long GraphPayloadEstimate=>graph==null?0:graph.bytes;
        public DepotJunctionRouting(int maximumDepots=128,long maximumTreeBytes=64L*1024*1024)
        {if(maximumDepots<1||maximumTreeBytes<1)throw new ArgumentOutOfRangeException();MaximumDepots=maximumDepots;MaximumTreeBytes=maximumTreeBytes;}
        static long Allocation(){
#if CITY_SCENARIO_DOTNET
            return GC.GetAllocatedBytesForCurrentThread();
#else
            return -1; // Unity thread allocation API unavailable; use Player recorder.
#endif
        }
        void Observe(string kind,int depot,int expanded,long start,long bytes){if(Trace)Samples.Add(new Sample(kind,depot,expanded,start,bytes));}
        public string TraceCsv(){var s=new StringBuilder("kind,depot,expanded,ms,gc_bytes\n");foreach(var x in Samples)s.Append(x.kind).Append(',').Append(x.depot).Append(',').Append(x.expanded).Append(',').Append(x.ms.ToString("R",CultureInfo.InvariantCulture)).Append(',').Append(x.bytes).Append('\n');return s.ToString();}
        // Waste owns the active-facility registry. Retired depots use the original
        // path, while in-flight tasks/access facts remain owned by World.
        internal void SetDepots(IEnumerable<int> depots,Func<int,RoadAccess> resolver=null,Func<long> revision=null)
        {
            eligible=new HashSet<int>(depots);resolveAccess=resolver;accessRevision=revision;pinRevision=-1;var removed=new List<int>();foreach(var pair in entries)if(!eligible.Contains(pair.Key))removed.Add(pair.Key);
            foreach(int id in removed){Drop(id);Invalidations++;}
        }
        void Drop(int id){var e=entries[id];TreeBytes-=Bytes(e);entries.Remove(id);}
        static long Bytes(Entry e)=>(e.parent==null?0:e.parent.Length*20L)+(e.next==null?0:e.next.Length*20L);
        public void Clear(){entries.Clear();TreeBytes=0;graph=null;network=null;terminals.Clear();pinRevision=-1;}
        static RoadAccess Copy(RoadAccess a)=>new RoadAccess{endpoint=a.endpoint,lane=a.lane,distance=a.distance,use=a.use,valid=a.valid};
        static RoadAccess Opposite(Network n,RoadAccess a)=>AccessRouting.AtDirection(n,a,!n.Get(a.lane).Reverse);
        static bool Valid(Network n,RoadAccess a)=>a!=null&&a.valid&&n.Contains(a.lane)&&!n.Get(a.lane).IsConnection&&a.distance>=n.Get(a.lane).StartDistance&&a.distance<=n.Get(a.lane).EndDistance;
        Entry Get(Network n,int depot,RoadAccess anchor)
        {
            if(!ReferenceEquals(network,n)){if(network!=null)Invalidations++;Clear();network=n;}
            if(!Valid(n,anchor)){Fallbacks++;InvalidAccessFallbacks++;return null;}
            if(eligible!=null&&!eligible.Contains(depot)){Fallbacks++;InactiveDepotFallbacks++;return null;}
            // A depot exactly at a lane end is a terminal on THAT lane only.
            // Pin those lanes before grouping; adding a new terminal refines the
            // quotient and invalidates all affected derived labels conservatively.
            bool changed=false;
            if(resolveAccess!=null&&(pinRevision<0||accessRevision==null||pinRevision!=accessRevision()))
            {
                var pins=new HashSet<long>();foreach(int id in eligible){var point=resolveAccess(id);if(!Valid(n,point))continue;foreach(var goal in new[]{point,Opposite(n,point)})if(Valid(n,goal)&&goal.distance==n.Get(goal.lane).EndDistance)pins.Add(goal.lane);}
                changed=!terminals.SetEquals(pins);terminals.Clear();terminals.UnionWith(pins);pinRevision=accessRevision==null?0:accessRevision();
            }
            foreach(var goal in new[]{anchor,Opposite(n,anchor)})if(Valid(n,goal)&&goal.distance==n.Get(goal.lane).EndDistance)changed|=terminals.Add(goal.lane);
            if(changed&&graph!=null){entries.Clear();TreeBytes=0;graph=null;Invalidations++;}
            if(graph==null){long t=Stopwatch.GetTimestamp(),a=Allocation();graph=new Graph(n,terminals);Observe("graph",depot,Nodes,t,a);}
            if(Nodes*40L>MaximumTreeBytes){Fallbacks++;BudgetFallbacks++;return null;}
            if(entries.TryGetValue(depot,out var e))
            {
                if(e.anchor.lane==anchor.lane&&e.anchor.distance==anchor.distance&&e.anchor.use==anchor.use){Hits++;e.stamp=++clock;return e;}
                Drop(depot);Invalidations++;
            }
            Misses++;
            // Reserve two trees per entry, preventing a delayed return build from
            // exceeding the bound. Evict deterministically by monotonic LRU stamp.
            while(entries.Count>=MaximumDepots||(entries.Count+1)*Nodes*40L>MaximumTreeBytes)
            {int old=0;long age=long.MaxValue;foreach(var p in entries)if(p.Value.stamp<age){age=p.Value.stamp;old=p.Key;}Drop(old);Evictions++;}
            e=new Entry{depot=depot,anchor=Copy(anchor),other=Opposite(n,anchor),stamp=++clock};entries.Add(depot,e);return e;
        }
        void Forward(Entry e)
        {
            if(e.parent!=null)return;long t=Stopwatch.GetTimestamp(),a=Allocation();int size=Nodes,expanded=0;var heap=graph.heap;heap.Clear();
            e.parent=new int[size];e.via=new long[size];e.forwardCost=new double[size];for(int i=0;i<size;i++){e.parent[i]=-1;e.forwardCost[i]=double.PositiveInfinity;}
            foreach(var start in new[]{e.anchor,e.other})if(Valid(network,start))
            {int state=graph.state[start.lane];var lane=network.Get(start.lane);double cost=(lane.EndDistance-start.distance)/lane.Speed;if(cost>=e.forwardCost[state])continue;e.forwardCost[state]=cost;e.parent[state]=-2;e.via[state]=start.lane;heap.Push(state,cost);}
            while(heap.Count>0)
            {
                var item=heap.Pop();int p=(int)item.id;if(item.cost!=e.forwardCost[p])continue;expanded++;
                for(int k=0;k<graph.exits[p].Length;k++){long lane=graph.exits[p][k];int state=graph.state[lane];double candidate=(item.cost+graph.costs[p][k])+graph.travel[p][k];if(candidate>=e.forwardCost[state])continue;e.forwardCost[state]=candidate;e.parent[state]=p;e.via[state]=lane;heap.Push(state,candidate);}
            }
            TreeBytes+=size*20L;Builds++;Expanded+=expanded;Observe("forward-build",e.depot,expanded,t,a);
        }
        void Reverse(Entry e)
        {
            if(e.next!=null)return;long t=Stopwatch.GetTimestamp(),a=Allocation();int size=Nodes,expanded=0;var heap=graph.heap;heap.Clear();
            e.next=new int[size];e.exit=new long[size];e.returnCost=new double[size];for(int i=0;i<size;i++){e.next[i]=-1;e.returnCost[i]=double.PositiveInfinity;}
            foreach(var goal in new[]{e.anchor,e.other})if(Valid(network,goal))
            {
                if(goal.distance==network.Get(goal.lane).EndDistance){int terminal=graph.state[goal.lane];if(e.returnCost[terminal]>0){e.returnCost[terminal]=0;e.next[terminal]=-2;e.exit[terminal]=0;heap.Push(terminal,0);}}
                if(!graph.upstream.TryGetValue(goal.lane,out var parents))continue;
                foreach(int p in parents){int k=Array.BinarySearch(graph.exits[p],goal.lane);double candidate=graph.costs[p][k]+(goal.distance-network.Get(goal.lane).StartDistance)/network.Get(goal.lane).Speed;if(candidate>=e.returnCost[p])continue;e.returnCost[p]=candidate;e.next[p]=-2;e.exit[p]=goal.lane;heap.Push(p,candidate);}
            }
            while(heap.Count>0)
            {
                var item=heap.Pop();int state=(int)item.id;if(item.cost!=e.returnCost[state])continue;expanded++;
                foreach(var back in graph.reverse[state]){int p=back.state;double candidate=(back.turn+back.travel)+item.cost;if(candidate>=e.returnCost[p])continue;e.returnCost[p]=candidate;e.next[p]=state;e.exit[p]=back.lane;heap.Push(p,candidate);}
            }
            TreeBytes+=size*20L;Builds++;Expanded+=expanded;Observe("return-build",e.depot,expanded,t,a);
        }
        internal double StateCost(Network n,int depot,RoadAccess anchor,int state,bool returning)
        {var e=Get(n,depot,anchor);if(e==null)throw new InvalidOperationException("No diagnostic state tree");if(returning){Reverse(e);return e.returnCost[state];}Forward(e);return e.forwardCost[state];}
        internal List<long> StatePath(Network n,int depot,RoadAccess anchor,int state,bool returning,out double start,out double end)
        {
            var e=Get(n,depot,anchor);start=end=0;var route=new List<long>();
            if(!returning){Forward(e);if(e.parent[state]==-1)return null;var lanes=new List<long>();for(int s=state;s>=0;s=e.parent[s])lanes.Add(e.via[s]);lanes.Reverse();foreach(long lane in lanes)Append(route,lane);start=route[0]==e.anchor.lane?e.anchor.distance:e.other.distance;end=n.Get(route[route.Count-1]).EndDistance;return route;}
            Reverse(e);if(e.next[state]==-1)return null;route.Add(graph.members[state][0]);start=n.Get(route[0]).EndDistance;int count=0;
            for(int s=state;s>=0;s=e.next[s]){if(++count>Nodes)throw new InvalidOperationException("Cyclic diagnostic state tree");long lane=e.exit[s];if(lane==0){end=n.Get(route[route.Count-1]).EndDistance;break;}Append(route,lane);if(e.next[s]==-2){end=lane==e.anchor.lane?e.anchor.distance:e.other.distance;break;}}return route;
        }
        static void Append(List<long> route,long lane){if(route.Count>0)route.Add(Network.ConnectionId(route[route.Count-1],lane));route.Add(lane);}
        // true means handled (including proven unreachable); false requests fallback.
        public bool Outbound(Network n,int depot,RoadAccess anchor,RoadAccess target,out List<long> route,out double start,out double end)
        {
            Queries++;long t=Stopwatch.GetTimestamp(),a=Allocation();route=null;start=end=0;
            var e=Get(n,depot,anchor);if(e==null||!Valid(n,target)){if(e!=null){Fallbacks++;InvalidAccessFallbacks++;}return false;}
            var other=Opposite(n,target);
            foreach(var goal in new[]{target,other})if(Valid(n,goal))foreach(var source in new[]{e.anchor,e.other})
                if(Valid(n,source)&&source.lane==goal.lane&&source.distance<=goal.distance){route=new List<long>{source.lane};start=source.distance;end=goal.distance;Observe("outbound",depot,0,t,a);return true;}
            Forward(e);
            foreach(var goal in new[]{target,other})if(Valid(n,goal)&&graph.upstream.TryGetValue(goal.lane,out var parents))foreach(int p in parents)
            {
                LocalChecks++;if(e.parent[p]==-1)continue;var lanes=new List<long>();for(int s=p;s>=0;s=e.parent[s])lanes.Add(e.via[s]);lanes.Reverse();
                start=lanes[0]==e.anchor.lane?e.anchor.distance:e.other.distance;end=goal.distance;
                route=new List<long>(lanes.Count*2+1);bool reached=false;
                // A cached junction path may already pass the target road before
                // reaching the first candidate upstream state. Stop at that first
                // legal occurrence instead of adding a gratuitous circuit.
                foreach(long lane in lanes){Append(route,lane);if(lane==goal.lane&&(route.Count>1||start<=end)){reached=true;break;}}
                if(!reached)Append(route,goal.lane);Observe("outbound",depot,0,t,a);return true;
            }
            Unreachable++;Observe("outbound",depot,0,t,a);return true;
        }
        public bool Return(Network n,int depot,RoadAccess anchor,long lane,double distance,out List<long> route,out double end)
        {
            Queries++;long t=Stopwatch.GetTimestamp(),a=Allocation();route=null;end=0;
            var e=Get(n,depot,anchor);var source=new RoadAccess{lane=lane,distance=distance};
            if(e==null||!Valid(n,source)){if(e!=null){Fallbacks++;InvalidAccessFallbacks++;}return false;}
            foreach(var goal in new[]{e.anchor,e.other})if(Valid(n,goal)&&lane==goal.lane&&distance<=goal.distance){route=new List<long>{lane};end=goal.distance;Observe("return",depot,0,t,a);return true;}
            Reverse(e);int s=graph.state[lane];LocalChecks++;
            if(e.next[s]!=-1)
            {
                route=new List<long>{lane};int count=0;
                while(s>=0){if(++count>Nodes)throw new InvalidOperationException("Cyclic junction tree");long next=e.exit[s];if(next==0){end=network.Get(route[route.Count-1]).EndDistance;break;}Append(route,next);if(e.next[s]==-2){end=next==e.anchor.lane?e.anchor.distance:e.other.distance;break;}s=e.next[s];}
            }
            else Unreachable++;
            Observe("return",depot,0,t,a);return true;
        }
    }
}
