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
  Console.WriteLine("PASS "+assertions+" bounded route-cache checks; exact paths/anchors, mixed directions/keys, negative paths, hot retention, reset and network replacement");
 }
}
