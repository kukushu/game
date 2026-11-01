using System;
using System.Collections.Generic;
namespace HarborCity.RoadCore
{
    public sealed partial class Network
    {
        // Derived bounds for repeated targets inside one synchronous request batch.
        // Original heap ordering, strict improvements and route recovery stay intact.
        internal bool GoalBoundsEnabled=true;
        int goalBatchDepth,goalBuilds;
        readonly Dictionary<long,int> goalRequests=new Dictionary<long,int>();
        readonly Dictionary<long,GoalBound> goalBounds=new Dictionary<long,GoalBound>();
        internal long GoalBuildCount,GoalPruned,GoalUses;
        internal double GoalBuildMilliseconds;
        internal long GoalBuildNanoseconds=>(long)(GoalBuildMilliseconds*1000000);
        internal long GoalReservedBytes=>goalBounds.Count*(depotGraph==null?0:depotGraph.lanes.LongLength*16+256); // tree arrays only; graph is separately shared
        internal readonly struct GoalBatch:System.IDisposable
        {
            readonly Network owner;
            internal GoalBatch(Network n){owner=n;if(n.goalBatchDepth++==0){n.goalRequests.Clear();n.goalBuilds=0;}}
            public void Dispose(){owner.goalBatchDepth--;}
        }
        internal GoalBatch BeginGoalBatch()=>new GoalBatch(this);
        sealed class GoalBound
        {
            internal readonly DepotGraph graph;internal readonly DepotTree tree;internal readonly int root;
            internal GoalBound(Network n,Lane goal){graph=n.GetDepotGraph();root=graph.index[goal.Id];tree=new DepotTree(graph,new RoadAccess{lane=goal.Id,distance=goal.StartDistance},true);}
            internal double Upper(Lane source)
            {
                int p=graph.index[source.Id];if(p==root)return double.PositiveInfinity;
                double cost=0;for(int i=0;i<tree.hop.Length;i++)
                {
                    int next=tree.hop[p];if(next<0)return double.PositiveInfinity;
                    cost+=graph.Link(p,next);if(next==root)return cost;
                    cost+=graph.lanes[next].TravelLength/graph.lanes[next].Speed;p=next;
                }
                throw new InvalidOperationException("Cyclic reverse route hint");
            }
            internal bool Exclude(long id,double prefix,double upper)
            {
                if(double.IsInfinity(upper)||double.IsNaN(upper))return false;
                int p=graph.index[id];double d=tree.distance[p];if(double.IsInfinity(d)||double.IsNaN(d))return false;
                double own=graph.lanes[p].TravelLength/graph.lanes[p].Speed;
                // Guard cancellation plus opposite addition orders. All edge terms
                // are nonnegative. Bound grows with full graph size; unsafe ranges
                // fall back to unpruned A*. double.Epsilon is NOT machine epsilon.
                double scale=d+own+prefix+upper+1;
                double roundingTerms=(graph.lanes.Length+1)*8.0*2.2204460492503131e-16;
                if(roundingTerms>=.001)return false;
                double margin=scale*(8*roundingTerms/(1-roundingTerms));
                if(double.IsInfinity(scale)||double.IsNaN(scale)||double.IsInfinity(margin))return false;
                return prefix+Math.Max(0,d-own)-margin>upper;
            }
        }
        GoalBound GoalHint(Lane target)
        {
            if(!GoalBoundsEnabled||goalBatchDepth==0)return null;
            if(goalBounds.TryGetValue(target.Id,out var hint)){GoalUses++;return hint;}
            goalRequests.TryGetValue(target.Id,out int count);goalRequests[target.Id]=++count;
            if(count<16||goalBuilds>=4)return null;
            if(goalBounds.Count>=4)goalBounds.Clear(); // bounded derived hints; no actor/route state is stored
            var timer=System.Diagnostics.Stopwatch.StartNew();hint=new GoalBound(this,target);timer.Stop();
            GoalBuildMilliseconds+=timer.Elapsed.TotalMilliseconds;
            goalBounds.Add(target.Id,hint);goalBuilds++;GoalBuildCount++;GoalUses++;return hint;
        }


    }
}
