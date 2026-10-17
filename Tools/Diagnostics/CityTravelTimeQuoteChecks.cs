using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HarborCity;
using HarborCity.RoadCore;
static class CityTravelTimeQuoteChecks
{
 static BinaryWriter answers;
 static int checks,numericChecks;static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 static void Compare(Network n,RoadAccess a,RoadAccess b,bool both)
 {
  double start,end;var route=AccessRouting.Find(n,a,b,both,out start,out end);double expected=new DispatchBlueprint(n,route,start,end).seconds;
  if(answers!=null){answers.Write(a.lane);answers.Write(a.distance);answers.Write(b.lane);answers.Write(b.distance);answers.Write(both);answers.Write(start);answers.Write(end);answers.Write(route==null?-1:route.Count);if(route!=null)foreach(long id in route)answers.Write(id);}
  double actual=AccessRouting.EstimateSeconds(n,a,b,both);
  Check(BitConverter.DoubleToInt64Bits(expected)==BitConverter.DoubleToInt64Bits(actual),"Travel time changed bits: "+expected.ToString("R")+" != "+actual.ToString("R")+" src="+a.lane+"@"+a.distance.ToString("R")+" dst="+b.lane+"@"+b.distance.ToString("R")+" both="+both);numericChecks++;
 }
 static Network Grid(bool varied)
 {
  var d=new NetworkData();const int n=7;int road=1000000;Func<int,int,int,int> id=(island,x,z)=>70000+island*n*n+z*n+x;Func<int,int,int,Point> p=(island,x,z)=>new Point(island*500+x*20,2,z*20);
  for(int island=0;island<2;island++)for(int z=0;z<n;z++)for(int x=0;x<n;x++)
  {d.junctions.Add(new Junction{id=id(island,x,z),position=p(island,x,z)});foreach(bool horizontal in new[]{true,false}){int nx=x+(horizontal?1:0),nz=z+(horizontal?0:1);if(nx>=n||nz>=n)continue;Point a=p(island,x,z),b=p(island,nx,nz);road+=17;d.roads.Add(new PhysicalRoad{id=road,from=id(island,x,z),to=id(island,nx,nz),points=road%3==0?new[]{a,new Point((a.x+b.x)*.5+1,3,(a.z+b.z)*.5+1),b}:new[]{a,b},lanes=road%3+1,speed=varied?(road%7==0?.01:road%11==0?1000000:(road%31+1)*.7):12,direction=road%5==0?RoadDirection.Forward:road%11==0?RoadDirection.Backward:RoadDirection.Both});}}
  d.nextRoad=road+1;d.nextJunction=80000;return new Network(d);
 }
 static void Main(string[] args)
 {
  if(args.Length>1)answers=new BinaryWriter(File.Create(args[1]));
  var random=new Random(41297);
  foreach(bool varied in new[]{false,true})
  {var n=Grid(varied);var lanes=n.Drivables.Where(d=>!d.IsConnection).OrderBy(d=>d.Id).ToArray();for(int i=0;i<2500;i++)
   {var a=lanes[random.Next(lanes.Length)];var b=i%5==0?a:lanes[random.Next(lanes.Length)];double x=a.StartDistance+a.TravelLength*(i%4==0?1:i%3==0?0:random.NextDouble()),y=b.StartDistance+b.TravelLength*(i%4==0?0:i%3==0?1:random.NextDouble());foreach(bool both in new[]{false,true})Compare(n,new RoadAccess{lane=a.Id,distance=x},new RoadAccess{lane=b.Id,distance=y},both);}
   foreach(var a in lanes.Take(30)){Compare(n,new RoadAccess{lane=a.Id,distance=a.EndDistance+1},new RoadAccess{lane=a.Id,distance=a.StartDistance},false);Compare(n,new RoadAccess{valid=false,lane=a.Id},new RoadAccess{lane=a.Id,distance=a.StartDistance},true);}}
  var straight=new World();int j=straight.AddJunction(new Point()),k=straight.AddJunction(new Point(100,0,0));int r=straight.AddRoad(j,k,new[]{straight.JunctionPosition(j),straight.JunctionPosition(k)});long lane=Network.LaneId(r,false);Compare(straight.Network,new RoadAccess{lane=lane,distance=0},new RoadAccess{lane=lane,distance=-0.0},false);
  var source=new RoadAccess{lane=lane,distance=10};var target=new RoadAccess{lane=lane,distance=80};var cache=new DispatchRoutes();
  double quote=cache.GetTravelSeconds(straight.Network,1,2,source,target);Check(cache.Count==1&&cache.CostPreparations==1&&cache.Preparations==0,"Quote unnecessarily built a route");
  var plan=cache.Get(straight.Network,1,2,source,target,true,null);Check(plan.route!=null&&plan.seconds==quote&&cache.CostPreparations==1&&cache.Preparations==1&&cache.Count==1,"Materialization changed cost, repeated quote, or duplicated key");
  for(int i=0;i<20;i++){Check(cache.GetTravelSeconds(straight.Network,1,2,source,target)==quote,"Cached quote changed");Check(ReferenceEquals(plan,cache.Get(straight.Network,1,2,source,target,true,null)),"Blueprint not reused");}
  Check(cache.Preparations==1&&cache.CostPreparations==1,"Repeated key recomputed");
  var disconnected=Grid(false);var islands=disconnected.Drivables.Where(d=>!d.IsConnection).OrderBy(d=>d.Id).ToArray();
  var noSource=new RoadAccess{lane=islands[0].Id,distance=islands[0].StartDistance};var noTarget=new RoadAccess{lane=islands[islands.Length-1].Id,distance=islands[islands.Length-1].StartDistance};
  var noCache=new DispatchRoutes();Check(double.IsPositiveInfinity(noCache.GetTravelSeconds(disconnected,91,92,noSource,noTarget)),"Disconnected quote must fail");
  var noPlan=noCache.Get(disconnected,91,92,noSource,noTarget,true,null);Check(noPlan.route==null&&double.IsPositiveInfinity(noPlan.seconds)&&noCache.Count==1,"NoRoute materialization changed reachability");
  for(int i=0;i<5000;i++)cache.GetTravelSeconds(straight.Network,10000+i,20000+i,source,target);
  Check(cache.Count==4096,"Quote cache capacity changed");
  var changed=straight.Snapshot().network;changed.roads[0].speed=24;var replacement=new Network(changed);double next=cache.GetTravelSeconds(replacement,1,2,source,target);
  double rs,re;var rp=AccessRouting.Find(replacement,source,target,true,out rs,out re);Check(cache.Count==1&&next==new DispatchBlueprint(replacement,rp,rs,re).seconds&&next!=quote,"Rebuilt network retained stale quote");
  if(args.Length>0){var session=CityCoreSave.Read(File.ReadAllBytes(args[0]));var used=new HashSet<(int,int)>();foreach(var member in session.Entities.Members){int home=member.Family.home,work=session.City.Workplace(member.Person);if(!session.Transport.HasResidentAccess(home,work)||!used.Add((home,work)))continue;Compare(session.Transport.Network,session.Transport.GetAccess(home),session.Transport.GetAccess(work),true);if(used.Count>=2000)break;}Console.WriteLine("Real city unique legal OD="+used.Count);}
  answers?.Dispose();Console.WriteLine("PASS "+checks+" checks including "+numericChecks+" bit-exact scalar quotes vs existing complete routes: forward/backward, one-way, disconnected, curved/high-ID/multi-lane, partial/upstream/zero/invalid and varied speeds");
 }
}
