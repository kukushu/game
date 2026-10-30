using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HarborCity.RoadCore;
static class CityRouteWorkspaceChecks
{
 static int checks;
 static void Check(bool ok,string reason){checks++;if(!ok)throw new Exception(reason);}
 static void Main(string[] args)
 {
  var data=new NetworkData();const int n=8;int roadId=1000000;
  Func<int,int,int,int> id=(island,x,z)=>70000+island*n*n+z*n+x;
  Func<int,int,int,Point> point=(island,x,z)=>new Point(island*500+x*20,2,z*20);
  for(int island=0;island<2;island++)for(int z=0;z<n;z++)for(int x=0;x<n;x++)
  {data.junctions.Add(new Junction{id=id(island,x,z),position=point(island,x,z)});foreach(bool horizontal in new[]{true,false})
   {int nx=x+(horizontal?1:0),nz=z+(horizontal?0:1);if(nx>=n||nz>=n)continue;var a=point(island,x,z);var b=point(island,nx,nz);roadId+=17;data.roads.Add(new PhysicalRoad{id=roadId,from=id(island,x,z),to=id(island,nx,nz),points=roadId%3==0?new[]{a,new Point((a.x+b.x)*.5+1,3,(a.z+b.z)*.5+1),b}:new[]{a,b},lanes=roadId%2+1,speed=12,direction=roadId%5==0?RoadDirection.Forward:RoadDirection.Both});}}
  data.nextRoad=roadId+1;data.nextJunction=80000;var network=new Network(data);var lanes=network.Drivables.Where(d=>!d.IsConnection).OrderBy(d=>d.Id).ToArray();var rng=new Random(31791);
  network.RouteSharingEnabled=false;var retained=new List<Tuple<List<long>,long[]>>();int reachable=0,unreachable=0;
  using(var output=new BinaryWriter(File.Create(args[0])))
   for(int i=0;i<600;i++)
   {
    if(i==300){var scratch=typeof(Network).GetField("accessSearch",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(network);var epoch=scratch?.GetType().GetField("epoch",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);epoch?.SetValue(scratch,uint.MaxValue);}
    var a=lanes[rng.Next(lanes.Length)];var b=lanes[rng.Next(lanes.Length)];double start=a.StartDistance+a.TravelLength*.3,end=b.StartDistance+b.TravelLength*.7;
    var route=network.Route(new RoadAccess{lane=a.Id,distance=start},new RoadAccess{lane=b.Id,distance=end});output.Write(route!=null);
    if(route!=null){reachable++;network.ValidateRoute(route);Check(route[0]==a.Id&&route[route.Count-1]==b.Id,"Route endpoints changed");output.Write(route.Count);foreach(long step in route)output.Write(step);if(i%11==0)retained.Add(Tuple.Create(route,route.ToArray()));}else unreachable++;
    Check(network.Route(new RoadAccess{lane=a.Id,distance=a.EndDistance+1},new RoadAccess{lane=b.Id,distance=end})==null,"Invalid distance accepted");
    foreach(var saved in retained)Check(saved.Item1.SequenceEqual(saved.Item2),"Later search mutated an earlier caller-owned route");
   }
  Check(reachable>100&&unreachable>100,"Mixed reachable/disconnected workload missing");
  Console.WriteLine("PASS "+checks+" route checks: mixed directed/curved/high-ID networks, disconnected/invalid queries and retained route independence; reachable="+reachable+", no-route="+unreachable);
 }
}
