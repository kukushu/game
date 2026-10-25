// Frozen from a703b0b Network. Test-only, not in Assets.
using System;
using System.Collections.Generic;
namespace HarborCity.RoadCore { public sealed partial class Network {
        internal List<long> FrozenJunctionRoute(int from,int to)
        {
            if(from==to)return new List<long>();
            var cost=new Dictionary<long,double>(CityRoadKeyComparer.Instance);
            var parent=new Dictionary<long,long>(CityRoadKeyComparer.Instance);
            var open=new RouteQueue();var closed=new HashSet<long>(CityRoadKeyComparer.Instance);
            List<Drivable> starts;if(!outgoing.TryGetValue(from,out starts))return null;
            foreach(var d in starts){cost[d.Id]=d.TravelLength/d.Speed;parent[d.Id]=0;open.Push(d.Id,cost[d.Id]);}
            while(open.Count>0)
            {
                var entry=open.Pop();long id=entry.id;if(entry.cost!=cost[id]||!closed.Add(id))continue;var a=Get(id);
                if(a.To==to)
                {
                    var lanes=new List<long>();for(long v=id;v!=0;v=parent[v])lanes.Add(v);lanes.Reverse();
                    var route=new List<long>(lanes.Count*2-1);for(int i=0;i<lanes.Count;i++){if(i>0)route.Add(ConnectionId(lanes[i-1],lanes[i]));route.Add(lanes[i]);}return route;
                }
                List<Drivable> next;if(!outgoing.TryGetValue(a.To,out next))continue;
                foreach(var b in next){long link=ConnectionId(id,b.Id);if(!Contains(link)||closed.Contains(b.Id))continue;double c=cost[id]+Get(link).Geometry.Length/Get(link).Speed+b.TravelLength/b.Speed;double old;if(!cost.TryGetValue(b.Id,out old)||c<old){cost[b.Id]=c;parent[b.Id]=id;open.Push(b.Id,c);}}
            }
            return null;
        }
        internal List<long> FrozenRouteToEither(RoadAccess start,RoadAccess goal,RoadAccess other,out int selected)
        {
#if CITY_CORE_ATTRIBUTION
            using var observation=CityCoreProfile.Measure(CityCoreProfile.Part.RouteSearch);
#endif
            using var routeObservation=DepotRoutingTrace.Start("original-dijkstra");
            if(DepotJunctionRouting.CountSearches)DepotJunctionRouting.SearchCalls++;
            selected=-1;var first=Get(start.lane);Get(goal.lane);
            if(!start.valid||first.IsConnection||start.distance<first.StartDistance||start.distance>first.EndDistance)return null;
            RoadAccess a=ValidCostAccess(goal)?goal:null,b=other!=null&&ValidCostAccess(other)?other:null;
            if(a==null&&b==null)return null;
            // Any other path must first reach the end of this source lane,
            // which cannot beat a legal forward arrival on that same lane.
            double directCost=double.PositiveInfinity;
            if(a!=null&&first.Id==a.lane&&a.distance>=start.distance){directCost=0.0+(a.distance-start.distance)/first.Speed;selected=0;}
            if(b!=null&&first.Id==b.lane&&b.distance>=start.distance){double cost=0.0+(b.distance-start.distance)/first.Speed;if(cost<directCost){directCost=cost;selected=1;}}
            if(selected>=0)return new List<long>{first.Id};
            var scratch=accessSearch??(accessSearch=new RouteScratch());
            using(scratch.Acquire())
            {
                var queue=scratch.queue;var costs=scratch.costs;var parents=scratch.parents;var settled=scratch.settled;
                double best=double.PositiveInfinity;long beforeGoal=0;
                double initial=(first.EndDistance-start.distance)/first.Speed;costs[first.Id]=initial;parents[first.Id]=0;queue.Push(first.Id,initial);
                while(queue.Count>0)
                {
                    var item=queue.Pop();if(item.cost>best)break;if(item.cost!=costs[item.id]||!settled.Add(item.id))continue;
                    if(routeObservation!=null)routeObservation.expanded++;
                    if(DepotJunctionRouting.CountSearches)DepotJunctionRouting.SearchExpanded++;
                    var next=AccessConnections(item.id);if(next==null)continue;
                    foreach(var connection in next)
                    {
                        var lane=connection.Outgoing;double prefix=item.cost+connection.Geometry.Length/connection.Speed;bool terminal=false;
                        if(a!=null&&lane.Id==a.lane){double cost=prefix+(a.distance-lane.StartDistance)/lane.Speed;if(cost<best||cost==best&&selected>0){best=cost;selected=0;beforeGoal=item.id;}terminal=true;}
                        if(b!=null&&lane.Id==b.lane){double cost=prefix+(b.distance-lane.StartDistance)/lane.Speed;if(cost<best){best=cost;selected=1;beforeGoal=item.id;}terminal=true;}
                        // With one goal this is exactly the original terminal rule.
                        // With two goals, allow traversal at the same cost so a
                        // zero-length boundary cannot hide the earlier-ranked goal.
                        if(terminal&&(a==null||b==null))continue;
                        if(settled.Contains(lane.Id))continue;double candidate=prefix+lane.TravelLength/lane.Speed,old;
                        if(!costs.TryGetValue(lane.Id,out old)||candidate<old){costs[lane.Id]=candidate;parents[lane.Id]=item.id;queue.Push(lane.Id,candidate);}
                    }
                }
                if(selected<0)return null;
                var lanes=new List<long>();for(long id=beforeGoal;id!=0;id=parents[id])lanes.Add(id);lanes.Reverse();lanes.Add((selected==0?a:b).lane);
                var result=new List<long>();for(int i=0;i<lanes.Count;i++){if(i>0)result.Add(ConnectionId(lanes[i-1],lanes[i]));result.Add(lanes[i]);}return result;
            }
        }

}}
