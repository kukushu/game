using System;
using System.Linq;
using HarborCity.RoadCore;
static class CityRouteCacheChecks
{
 static int assertions;
 static void Check(bool value,string message){assertions++;if(!value)throw new Exception(message);}
 static void Main()
 {
  var w=new World();int a=w.AddJunction(new Point()),b=w.AddJunction(new Point(30000,0,0));int road=w.AddRoad(a,b,new[]{w.JunctionPosition(a),w.JunctionPosition(b)});
  var n=w.Network;var cache=new DispatchRoutes();var source=new RoadAccess{lane=Network.LaneId(road,false),distance=10};var goal=new RoadAccess{lane=source.lane,distance=99};
  var hot=cache.Get(n,1,99,source,goal,true,null);long first=cache.Preparations;
  for(int i=0;i<7000;i++)
  {
   goal.distance=200+i;bool resident=i%2==0;var plan=cache.Get(n,1,1000+i,source,goal,resident,null);
   if(i%53==0){var expected=AccessRouting.Find(n,source,goal,resident,out double start,out double end);Check(plan.route.SequenceEqual(expected)&&plan.start==start&&plan.end==end,"Cached path/direction/anchors differ from exact routing");}
   goal.distance=99;Check(ReferenceEquals(hot,cache.Get(n,1,99,source,goal,true,null)),"Hot route discarded under capacity pressure");Check(cache.Count<=4096,"Cache bound exceeded");
  }
  Check(cache.Count==4096&&cache.Preparations==first+7000,"Cache did extra work or lost shared capacity accounting");
  foreach(int origin in new[]{int.MinValue,-1,0,1,int.MaxValue})foreach(int destination in new[]{int.MinValue,-1,0,99,int.MaxValue})
  {var x=cache.Get(n,origin,destination,source,goal,false,null);var y=cache.Get(n,origin,destination,source,goal,true,null);Check(!ReferenceEquals(x,y)&&ReferenceEquals(x,cache.Get(n,origin,destination,source,goal,false,null)),"Signed key or passenger/freight alias");}
  cache.Reset(n);Check(cache.Count==0,"Explicit access invalidation retained entries");var again=cache.Get(n,1,99,source,goal,true,null);Check(!ReferenceEquals(hot,again),"Invalidated blueprint reused");
  int c=w.AddJunction(new Point(0,0,30)),d=w.AddJunction(new Point(100,0,30));int isolated=w.AddRoad(c,d,new[]{w.JunctionPosition(c),w.JunctionPosition(d)});n=w.Network;
  var unreachable=new RoadAccess{lane=Network.LaneId(isolated,false),distance=50};long before=cache.Preparations;var no=cache.Get(n,1,123,source,unreachable,true,null);Check(no.route==null&&cache.Count==1&&cache.Preparations==before+1,"Network replacement did not invalidate");
  Check(ReferenceEquals(no,cache.Get(n,1,123,source,unreachable,true,null))&&cache.Preparations==before+1,"Negative reachability was not cached");
  Selective(n,source,goal,unreachable);
  WorldAccesses();
  Console.WriteLine("PASS "+assertions+" bounded route-cache checks; exact paths/anchors, mixed directions/keys, negative paths, hot retention, reset and network replacement");
 }
 static void Selective(Network n,RoadAccess source,RoadAccess goal,RoadAccess unreachable)
 {
  var cache=new DispatchRoutes();
  int[] origins={10,30,40,int.MinValue,70},destinations={20,10,50,int.MaxValue,10};
  var plans=new DispatchBlueprint[5,2];
  for(int i=0;i<5;i++)for(int j=0;j<2;j++)plans[i,j]=cache.Get(n,origins[i],destinations[i],source,i==4?unreachable:goal,j==1,null);
  cache.GetTravelSeconds(n,10,80,source,goal);
  Check(cache.Count==11,"Selective fixture lost scalar or mixed-purpose entries");
  cache.InvalidateEndpoint(10);Check(cache.Count==4,"Endpoint invalidation missed origin/destination/failure/quote entries");
  for(int i=0;i<5;i++)for(int j=0;j<2;j++)
  {
   var next=cache.Get(n,origins[i],destinations[i],source,i==4?unreachable:goal,j==1,null);
   Check(ReferenceEquals(next,plans[i,j])==(i==2||i==3),"Unrelated route removed or affected route retained");
   var expected=AccessRouting.Find(n,source,i==4?unreachable:goal,j==1,out double start,out double end);
   Check((next.route==null)==(expected==null)&&(expected==null||next.route.SequenceEqual(expected)&&next.start==start&&next.end==end),"Recomputed selective route differs");
   plans[i,j]=next;
  }
  double quoted=cache.GetTravelSeconds(n,10,80,source,goal);long quotes=cache.CostPreparations;
  cache.InvalidateEndpoint(10);cache.GetTravelSeconds(n,10,80,source,goal);
  Check(cache.CostPreparations==quotes+1,"Scalar-only quote survived endpoint invalidation");
  for(int i=0;i<5;i++)for(int j=0;j<2;j++)plans[i,j]=cache.Get(n,origins[i],destinations[i],source,i==4?unreachable:goal,j==1,null);
  cache.InvalidateEndpoints(new System.Collections.Generic.HashSet<int>{40,int.MinValue});
  for(int i=0;i<5;i++)for(int j=0;j<2;j++)Check(ReferenceEquals(plans[i,j],cache.Get(n,origins[i],destinations[i],source,i==4?unreachable:goal,j==1,null))==(i!=2&&i!=3),"Batch endpoint invalidation changed unrelated pair");
  var hot=cache.Get(n,10,20,source,goal,true,null);
  for(int i=0;i<5000;i++){cache.Get(n,10000+i,20000+i,source,goal,false,null);Check(cache.Count<=4096&&ReferenceEquals(hot,cache.Get(n,10,20,source,goal,true,null)),"LRU links corrupt after selective removals");}
  cache.Reset(n);cache.Get(n,1,2,source,goal,false,null);cache.InvalidateEndpoint(1);Check(cache.Count==0,"Last entry removal retained LRU head/tail");
  cache.Get(n,3,4,source,goal,false,null);cache.InvalidateEndpoints(new System.Collections.Generic.HashSet<int>{3});Check(cache.Count==0,"Singleton bulk removal broke LRU");
 }
 static void WorldAccesses()
 {
  var w=new World();int a=w.AddJunction(new Point()),b=w.AddJunction(new Point(200,0,0)),island=w.AddJunction(new Point(0,0,100));
  int road=w.AddRoad(a,b,new[]{w.JunctionPosition(a),w.JunctionPosition(b)}),source=w.AddDepot(a),target=w.AddDepot(b),fresh=w.AddDepot(island);
  long lane=Network.LaneId(road,false);w.RegisterAccess(source,lane,10);w.RegisterAccess(target,lane,100);
  double seconds=w.FreightTravelSeconds(source,target);long prepared=w.RoutePreparations;
  Check(w.Dispatch(fresh,target)==null,"Fallback fixture should be disconnected");
  Check(w.RoutePreparations==prepared+1&&w.CachedRoutes==2,"Fallback failure not cached");
  w.RegisterAccess(fresh,lane,20);Check(w.CachedRoutes==1,"New access must invalidate its previous fallback failure only");
  prepared=w.RoutePreparations;Check(w.FreightTravelSeconds(source,target)==seconds&&w.RoutePreparations==prepared,"Unrelated world route lost on new access");
  Check(!double.IsInfinity(w.FreightTravelSeconds(fresh,target))&&w.RoutePreparations==prepared+1,"New entrance reused stale fallback failure");
  int x=w.AddDepot(a),y=w.AddDepot(b);
  prepared=w.RoutePreparations;int count=w.CachedRoutes;
  w.RegisterAccesses(new[]{new AccessRegistration(x,lane,30),new AccessRegistration(y,lane,130)});
  Check(w.CachedRoutes==count&&w.FreightTravelSeconds(source,target)==seconds&&w.RoutePreparations==prepared,"Bulk unrelated access registration flushed cache");
  byte[] before=SaveCodec.Write(w);bool rejected=false;try{w.RegisterAccesses(new[]{new AccessRegistration(x,lane,31),new AccessRegistration(y,lane,131)});}catch(ArgumentException){rejected=true;}
  Check(rejected&&before.SequenceEqual(SaveCodec.Write(w))&&w.CachedRoutes==count,"Failed access registration changed world/cache");
  Check(w.PlanUpgradeRoad(road,1,20,RoadDirection.Both).TryCommit()&&w.CachedRoutes==0,"Road edit retained stale ordinary routes");
  Check(w.FreightTravelSeconds(source,target)!=seconds,"Speed edit reused stale travel time");
 }
}
