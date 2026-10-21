using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace HarborCity.RoadCore
{
    // Experimental reachability quotient, NOT shortest-time routing. The immutable
    // Network remains authoritative. States merge incoming lanes only at the SAME
    // junction and with IDENTICAL legal outgoing lane sets (a bisimulation).
    // Thus every cached edge has an actual LaneConnection for every class member.
    public sealed class DepotJunctionRouting
    {
        // Diagnostic only; no change to queue order or costs.
        public static bool CountSearches;public static long SearchCalls,SearchExpanded;
        sealed class Signature : IEquatable<Signature>
        {
            internal int junction; internal long[] exits;
            public bool Equals(Signature b){if(b==null||junction!=b.junction||exits.Length!=b.exits.Length)return false;for(int i=0;i<exits.Length;i++)if(exits[i]!=b.exits[i])return false;return true;}
            public override bool Equals(object b)=>Equals(b as Signature);
            public override int GetHashCode(){unchecked{int h=junction;foreach(long x in exits)h=h*31+x.GetHashCode();return h;}}
        }
        struct Back {internal int state;internal long lane;}
        sealed class Graph
        {
            internal readonly Dictionary<long,int> state=new Dictionary<long,int>(CityRoadKeyComparer.Instance);
            internal readonly List<long[]> exits=new List<long[]>();
            internal readonly List<List<Back>> reverse=new List<List<Back>>();
            internal readonly Dictionary<long,List<int>> upstream=new Dictionary<long,List<int>>(CityRoadKeyComparer.Instance);
            internal int[] queue;internal int laneCount;internal long bytes;
            internal Graph(Network n)
            {
                var lanes=new List<Drivable>();var legal=new Dictionary<long,List<long>>(CityRoadKeyComparer.Instance);
                foreach(var d in n.Drivables)if(!d.IsConnection){lanes.Add(d);legal.Add(d.Id,new List<long>());}
                foreach(var d in n.Drivables)if(d is LaneConnection c)legal[c.Incoming.Id].Add(c.Outgoing.Id);
                lanes.Sort((a,b)=>a.Id.CompareTo(b.Id));laneCount=lanes.Count;
                var groups=new Dictionary<Signature,int>();
                foreach(var lane in lanes)
                {
                    var list=legal[lane.Id];list.Sort();var key=new Signature{junction=lane.To,exits=list.ToArray()};
                    if(!groups.TryGetValue(key,out int id)){id=exits.Count;groups.Add(key,id);exits.Add(key.exits);reverse.Add(new List<Back>());}
                    state.Add(lane.Id,id);
                }
                for(int i=0;i<exits.Count;i++)foreach(long lane in exits[i])
                {
                    reverse[state[lane]].Add(new Back{state=i,lane=lane});
                    if(!upstream.TryGetValue(lane,out var list))upstream.Add(lane,list=new List<int>());list.Add(i);
                    bytes+=24; // edge payload estimate, NOT managed retained heap
                }
                queue=new int[exits.Count];bytes+=state.Count*32L+exits.Count*52L;
            }
        }
        sealed class Entry
        {
            internal int depot;internal long stamp;internal RoadAccess anchor,other;
            internal int[] parent,next;internal long[] via,exit;
        }
        public readonly struct Sample
        {
            public readonly string kind;public readonly int depot,expanded;public readonly double ms;public readonly long bytes;
            internal Sample(string kind,int depot,int expanded,long start,long allocated){this.kind=kind;this.depot=depot;this.expanded=expanded;ms=(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;bytes=allocated<0?-1:Allocation()-allocated;}
        }
        readonly Dictionary<int,Entry> entries=new Dictionary<int,Entry>();
        HashSet<int> eligible;Network network;Graph graph;long clock;
        public int MaximumDepots {get;} public long MaximumTreeBytes {get;}
        public long Queries,Hits,Misses,Evictions,Invalidations,LocalChecks,Fallbacks,Unreachable,Builds,Expanded;
        public bool Trace;public readonly List<Sample> Samples=new List<Sample>();
        public int Nodes=>graph==null?0:graph.exits.Count;
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
        internal void SetDepots(IEnumerable<int> depots)
        {
            eligible=new HashSet<int>(depots);var removed=new List<int>();foreach(var pair in entries)if(!eligible.Contains(pair.Key))removed.Add(pair.Key);
            foreach(int id in removed){Drop(id);Invalidations++;}
        }
        void Drop(int id){var e=entries[id];TreeBytes-=Bytes(e);entries.Remove(id);}
        static long Bytes(Entry e)=>(e.parent==null?0:e.parent.Length*12L)+(e.next==null?0:e.next.Length*12L);
        public void Clear(){entries.Clear();TreeBytes=0;graph=null;network=null;}
        static RoadAccess Copy(RoadAccess a)=>new RoadAccess{endpoint=a.endpoint,lane=a.lane,distance=a.distance,use=a.use,valid=a.valid};
        static RoadAccess Opposite(Network n,RoadAccess a)=>AccessRouting.AtDirection(n,a,!n.Get(a.lane).Reverse);
        static bool Valid(Network n,RoadAccess a)=>a!=null&&a.valid&&n.Contains(a.lane)&&!n.Get(a.lane).IsConnection&&a.distance>=n.Get(a.lane).StartDistance&&a.distance<=n.Get(a.lane).EndDistance;
        Entry Get(Network n,int depot,RoadAccess anchor)
        {
            if(!Valid(n,anchor)||eligible!=null&&!eligible.Contains(depot)){Fallbacks++;return null;}
            if(!ReferenceEquals(network,n)){if(network!=null)Invalidations++;Clear();network=n;}
            if(graph==null){long t=Stopwatch.GetTimestamp(),a=Allocation();graph=new Graph(n);Observe("graph",depot,Nodes,t,a);}
            if(Nodes*24L>MaximumTreeBytes){Fallbacks++;return null;}
            if(entries.TryGetValue(depot,out var e))
            {
                if(e.anchor.lane==anchor.lane&&e.anchor.distance==anchor.distance&&e.anchor.use==anchor.use){Hits++;e.stamp=++clock;return e;}
                Drop(depot);Invalidations++;
            }
            Misses++;
            // Reserve two trees per entry, preventing a delayed return build from
            // exceeding the bound. Evict deterministically by monotonic LRU stamp.
            while(entries.Count>=MaximumDepots||(entries.Count+1)*Nodes*24L>MaximumTreeBytes)
            {int old=0;long age=long.MaxValue;foreach(var p in entries)if(p.Value.stamp<age){age=p.Value.stamp;old=p.Key;}Drop(old);Evictions++;}
            e=new Entry{depot=depot,anchor=Copy(anchor),other=Opposite(n,anchor),stamp=++clock};entries.Add(depot,e);return e;
        }
        void Forward(Entry e)
        {
            if(e.parent!=null)return;long t=Stopwatch.GetTimestamp(),a=Allocation();int size=Nodes,head=0,tail=0;
            e.parent=new int[size];e.via=new long[size];for(int i=0;i<size;i++)e.parent[i]=-1;
            foreach(var start in new[]{e.anchor,e.other})if(Valid(network,start))
            {int s=graph.state[start.lane];if(e.parent[s]!=-1)continue;e.parent[s]=-2;e.via[s]=start.lane;graph.queue[tail++]=s;}
            while(head<tail){int p=graph.queue[head++];foreach(long lane in graph.exits[p]){int s=graph.state[lane];if(e.parent[s]!=-1)continue;e.parent[s]=p;e.via[s]=lane;graph.queue[tail++]=s;}}
            TreeBytes+=size*12L;Builds++;Expanded+=tail;Observe("forward-build",e.depot,tail,t,a);
        }
        void Reverse(Entry e)
        {
            if(e.next!=null)return;long t=Stopwatch.GetTimestamp(),a=Allocation();int size=Nodes,head=0,tail=0;
            e.next=new int[size];e.exit=new long[size];for(int i=0;i<size;i++)e.next[i]=-1;
            foreach(var goal in new[]{e.anchor,e.other})if(Valid(network,goal)&&graph.upstream.TryGetValue(goal.lane,out var parents))
                foreach(int p in parents){if(e.next[p]!=-1)continue;e.next[p]=-2;e.exit[p]=goal.lane;graph.queue[tail++]=p;}
            while(head<tail){int s=graph.queue[head++];foreach(var back in graph.reverse[s]){int p=back.state;if(e.next[p]!=-1)continue;e.next[p]=s;e.exit[p]=back.lane;graph.queue[tail++]=p;}}
            TreeBytes+=size*12L;Builds++;Expanded+=tail;Observe("return-build",e.depot,tail,t,a);
        }
        static void Append(List<long> route,long lane){if(route.Count>0)route.Add(Network.ConnectionId(route[route.Count-1],lane));route.Add(lane);}
        // true means handled (including proven unreachable); false requests fallback.
        public bool Outbound(Network n,int depot,RoadAccess anchor,RoadAccess target,out List<long> route,out double start,out double end)
        {
            Queries++;long t=Stopwatch.GetTimestamp(),a=Allocation();route=null;start=end=0;
            var e=Get(n,depot,anchor);if(e==null||!Valid(n,target)){if(e!=null)Fallbacks++;return false;}
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
            if(e==null||!Valid(n,source)){if(e!=null)Fallbacks++;return false;}
            foreach(var goal in new[]{e.anchor,e.other})if(Valid(n,goal)&&lane==goal.lane&&distance<=goal.distance){route=new List<long>{lane};end=goal.distance;Observe("return",depot,0,t,a);return true;}
            Reverse(e);int s=graph.state[lane];LocalChecks++;
            if(e.next[s]!=-1)
            {
                route=new List<long>{lane};int count=0;
                while(s>=0){if(++count>Nodes)throw new InvalidOperationException("Cyclic junction tree");long next=e.exit[s];Append(route,next);if(e.next[s]==-2){end=next==e.anchor.lane?e.anchor.distance:e.other.distance;break;}s=e.next[s];}
            }
            else Unreachable++;
            Observe("return",depot,0,t,a);return true;
        }
    }
}
