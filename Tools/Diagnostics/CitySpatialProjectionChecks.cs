using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HarborCity;
using HarborCity.RoadCore;
static class CitySpatialProjectionChecks
{
 static int queries,hitsWritten;
 static readonly List<SpatialHit> hits=new List<SpatialHit>();
 static void Query(Network n,Point p,double radius,bool connections,BinaryWriter output)
 {
  n.Project(p,radius,connections,hits);output.Write(hits.Count);
  foreach(var h in hits){output.Write(h.drivable);output.Write(h.distance);output.Write(h.separation);output.Write(h.point.x);output.Write(h.point.y);output.Write(h.point.z);hitsWritten++;}queries++;
 }
 static void Probe(Network n,BinaryWriter output,int count)
 {
  var rng=new Random(83071);var roads=n.Drivables.OrderBy(d=>d.Id).ToArray();
  for(int i=0;i<count;i++)
  {
   var road=roads[rng.Next(roads.Length)];double along=road.StartDistance+road.TravelLength*rng.NextDouble();var centre=road.Geometry.Sample(along);
   var p=new Point(centre.x+(rng.NextDouble()-.5)*20,centre.y+(rng.NextDouble()-.5)*8,centre.z+(rng.NextDouble()-.5)*20);
   Query(n,p,i%4==0?64:i%3==0?0:i%2==0?.000001:4,i%2==0,output);
   if(i%10==0){Query(n,road.Geometry.Sample(road.StartDistance),0,true,output);Query(n,road.Geometry.Sample(road.EndDistance),.000001,false,output);}
  }
 }
 static void Main(string[] args)
 {
  using(var output=new BinaryWriter(File.Create(args[1])))
  {
   var world=new World();int a=world.AddJunction(new Point(-100,2,0)),b=world.AddJunction(new Point(0,2,0)),c=world.AddJunction(new Point(100,2,0)),d=world.AddJunction(new Point(0,2,-100));
   int r=world.AddRoad(a,b,new[]{world.JunctionPosition(a),new Point(-50,3,5),world.JunctionPosition(b)});
   world.AddRoad(b,c,new[]{world.JunctionPosition(b),world.JunctionPosition(c)});world.AddRoad(d,b,new[]{world.JunctionPosition(d),world.JunctionPosition(b)});
   Probe(world.Network,output,2000);world.SplitRoadAt(r,40);Probe(world.Network,output,1000);
   var plan=world.PlanUpgradeRoad(2,2,18,RoadDirection.Backward);if(!plan.TryCommit())throw new Exception("Test upgrade rejected");Probe(world.Network,output,1000);world.RemoveRoad(2);Probe(world.Network,output,1000);
   var data=world.Snapshot().network;foreach(var j in data.junctions)j.id+=70000;foreach(var road in data.roads){road.id+=1000000;road.from+=70000;road.to+=70000;}data.nextRoad+=1000000;data.nextJunction+=70000;Probe(new Network(data),output,1000);
   var city=CityCoreSave.Read(File.ReadAllBytes(args[0]));Probe(city.Transport.Network,output,2000);city.Validate();
  }
  Console.WriteLine("PASS "+queries+" exact-output spatial queries; "+hitsWritten+" ordered hits; curved/elevated/partial/boundary/radius/dynamic split/upgrade/remove/high IDs/real 30k network");
 }
}
