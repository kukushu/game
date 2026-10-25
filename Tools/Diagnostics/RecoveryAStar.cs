using System;
using System.Collections.Generic;
namespace HarborCity.RoadCore
{
    // Research only: compiled by the replay/check runners, never part of Assets.
    public sealed partial class Network
    {
        sealed class AStarQueue
        {
            internal struct Entry { internal long id; internal double cost,priority; }
            readonly List<Entry> entries=new List<Entry>();
            internal int Count=>entries.Count;
            static bool Before(Entry a,Entry b)=>a.priority<b.priority||a.priority==b.priority&&a.id<b.id;
            internal void Clear()=>entries.Clear();
            internal void Push(long id,double cost,double h)
            {
                var item=new Entry{id=id,cost=cost,priority=cost+h};int i=entries.Count;entries.Add(item);
                while(i>0){int parent=(i-1)/2;if(!Before(item,entries[parent]))break;entries[i]=entries[parent];i=parent;}entries[i]=item;
            }
            internal Entry Pop()
            {
                var result=entries[0];var last=entries[entries.Count-1];entries.RemoveAt(entries.Count-1);if(entries.Count==0)return result;
                int i=0;while(i*2+1<entries.Count){int child=i*2+1;if(child+1<entries.Count&&Before(entries[child+1],entries[child]))child++;if(!Before(entries[child],last))break;entries[i]=entries[child];i=child;}entries[i]=last;return result;
            }
        }
        AStarQueue experimentalQueue;
        Dictionary<long,double> experimentalH;
        double experimentalMaxSpeed,experimentalSeams;
        internal double ExperimentalSeams=>experimentalSeams;
        internal double PrepareExperimentalHeuristic()
        {
            if(experimentalMaxSpeed>0)return experimentalMaxSpeed;
            foreach(var d in drivables.Values)
            {
                experimentalMaxSpeed=Math.Max(experimentalMaxSpeed,d.Speed);
                if(d is LaneConnection c)experimentalSeams+=Point.Distance(c.Incoming.Exit.Position,c.Geometry.Sample(0))+Point.Distance(c.Geometry.Sample(c.Geometry.Length),c.Outgoing.Entry.Position);
            }
            return experimentalMaxSpeed;
        }
        // g is at the lane's EXIT, not its beginning. Subtract the total graph
        // seam distance, conservatively covering every seam of a simple path.
        // Numeric rounding and canonical equal-cost choices remain experimental.
        internal List<long> ExperimentalAStar(RoadAccess start,RoadAccess goal,RoadAccess other,out int selected,out int expanded,double scale=1)
        {
            if(scale<0||scale>1)throw new ArgumentOutOfRangeException(nameof(scale));
            expanded=0;selected=-1;var first=Get(start.lane);Get(goal.lane);
            if(!start.valid||first.IsConnection||start.distance<first.StartDistance||start.distance>first.EndDistance)return null;
            RoadAccess a=ValidCostAccess(goal)?goal:null,b=other!=null&&ValidCostAccess(other)?other:null;if(a==null&&b==null)return null;
            double directCost=double.PositiveInfinity;
            if(a!=null&&first.Id==a.lane&&a.distance>=start.distance){directCost=0.0+(a.distance-start.distance)/first.Speed;selected=0;}
            if(b!=null&&first.Id==b.lane&&b.distance>=start.distance){double cost=0.0+(b.distance-start.distance)/first.Speed;if(cost<directCost){directCost=cost;selected=1;}}
            if(selected>=0)return new List<long>{first.Id};
            double speed=scale==0?1:PrepareExperimentalHeuristic();
            Point pa=a==null?new Point():Get(a.lane).Geometry.Sample(a.distance),pb=b==null?new Point():Get(b.lane).Geometry.Sample(b.distance);
            var hs=experimentalH??(experimentalH=new Dictionary<long,double>(CityRoadKeyComparer.Instance));hs.Clear();
            double H(long id)
            {
                if(scale==0)return 0;if(hs.TryGetValue(id,out var cached))return cached;
                var p=((Lane)Get(id)).Exit.Position;double distance=a==null?double.PositiveInfinity:Point.Distance(p,pa);if(b!=null)distance=Math.Min(distance,Point.Distance(p,pb));
                double h=Math.Max(0,distance-experimentalSeams)/speed*scale*(1-1e-12);hs.Add(id,h);return h;
            }
            var scratch=accessSearch??(accessSearch=new RouteScratch());using(scratch.Acquire())
            {
                var queue=experimentalQueue??(experimentalQueue=new AStarQueue());queue.Clear();var costs=scratch.costs;var parents=scratch.parents;var settled=scratch.settled;
                double best=double.PositiveInfinity;long beforeGoal=0;double initial=(first.EndDistance-start.distance)/first.Speed;costs[first.Id]=initial;parents[first.Id]=0;queue.Push(first.Id,initial,H(first.Id));
                while(queue.Count>0)
                {
                    var item=queue.Pop();if(item.priority>best)break;if(item.cost!=costs[item.id]||!settled.Add(item.id))continue;expanded++;
                    var next=AccessConnections(item.id);if(next==null)continue;
                    foreach(var connection in next)
                    {
                        var lane=connection.Outgoing;double prefix=item.cost+connection.Geometry.Length/connection.Speed;bool terminal=false;
                        if(a!=null&&lane.Id==a.lane){double cost=prefix+(a.distance-lane.StartDistance)/lane.Speed;if(cost<best||cost==best&&selected>0){best=cost;selected=0;beforeGoal=item.id;}terminal=true;}
                        if(b!=null&&lane.Id==b.lane){double cost=prefix+(b.distance-lane.StartDistance)/lane.Speed;if(cost<best){best=cost;selected=1;beforeGoal=item.id;}terminal=true;}
                        if(terminal&&(a==null||b==null))continue;
                        double candidate=prefix+lane.TravelLength/lane.Speed,old;
                        if(!costs.TryGetValue(lane.Id,out old)||candidate<old){costs[lane.Id]=candidate;parents[lane.Id]=item.id;settled.Remove(lane.Id);queue.Push(lane.Id,candidate,H(lane.Id));}
                    }
                }
                if(selected<0)return null;
                var lanes=new List<long>();for(long id=beforeGoal;id!=0;id=parents[id])lanes.Add(id);lanes.Reverse();lanes.Add((selected==0?a:b).lane);
                var result=new List<long>();for(int i=0;i<lanes.Count;i++){if(i>0)result.Add(ConnectionId(lanes[i-1],lanes[i]));result.Add(lanes[i]);}return result;
            }
        }
    }
}
