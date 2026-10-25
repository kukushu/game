using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using HarborCity;
using HarborCity.RoadCore;
static class CitySpatialProjectionChecks
{
 static int queries,hitsWritten,clipOnlyChanges,connectionChanges;
 static readonly List<SpatialHit> hits=new List<SpatialHit>();
 static readonly List<(Network network,byte[] buckets)> retained=new List<(Network,byte[])>();
 static readonly BindingFlags fields=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
 static byte[] Buckets(Network n)
 {
  var index=typeof(Network).GetField("spatialIndex",fields).GetValue(n);var buckets=(IDictionary)index.GetType().GetField("cells",fields).GetValue(index);
  var segment=index.GetType().GetNestedType("Segment",BindingFlags.NonPublic);string[] names={"lo","dx","dy","dz","den"};var numbers=names.Select(name=>segment.GetField(name,fields)).ToArray();
  var id=segment.GetField("drivable",fields);var connection=segment.GetField("connection",fields);var valid=segment.GetField("valid",fields);var a=segment.GetField("a",fields);var b=segment.GetField("b",fields);
  using(var stream=new MemoryStream())using(var w=new BinaryWriter(stream))
  {
   var keys=buckets.Keys.Cast<long>().OrderBy(k=>k).ToArray();w.Write(keys.Length);
   foreach(long key in keys)
   {
    var entries=(IList)buckets[key];w.Write(key);w.Write(entries.Count);
    foreach(var entry in entries){w.Write((long)id.GetValue(entry));w.Write((bool)connection.GetValue(entry));w.Write((bool)valid.GetValue(entry));foreach(var f in numbers)w.Write((double)f.GetValue(entry));foreach(var f in new[]{a,b}){var p=(Point)f.GetValue(entry);w.Write(p.x);w.Write(p.y);w.Write(p.z);}}
   }
   w.Flush();return stream.ToArray();
  }
 }
 static void Query(Network n,Point p,double radius,bool connections,BinaryWriter output)
 {
  n.Project(p,radius,connections,hits);output.Write(hits.Count);
  foreach(var h in hits){output.Write(h.drivable);output.Write(h.distance);output.Write(h.separation);output.Write(h.point.x);output.Write(h.point.y);output.Write(h.point.z);hitsWritten++;}queries++;
 }
 static void Probe(Network n,BinaryWriter output,int count,bool keep=true)
 {
  var rng=new Random(83071);var roads=n.Drivables.OrderBy(d=>d.Id).ToArray();
  for(int i=0;i<count;i++)
  {
   var road=roads[rng.Next(roads.Length)];double along=road.StartDistance+road.TravelLength*rng.NextDouble();var centre=road.Geometry.Sample(along);
   var p=new Point(centre.x+(rng.NextDouble()-.5)*20,centre.y+(rng.NextDouble()-.5)*8,centre.z+(rng.NextDouble()-.5)*20);
   Query(n,p,i%4==0?64:i%3==0?0:i%2==0?.000001:4,i%2==0,output);
   if(i%10==0){Query(n,road.Geometry.Sample(road.StartDistance),0,true,output);Query(n,road.Geometry.Sample(road.EndDistance),.000001,false,output);}
  }
  var buckets=Buckets(n);output.Write(buckets.Length);output.Write(buckets);
  if(retained.Count>0)
  {
   var before=retained[retained.Count-1].network;
   bool sharedGeometry=false;int changedConnections=0;
   foreach(var d in n.Drivables)if(before.Contains(d.Id))
   {
    var previous=before.Get(d.Id);bool sameGeometry=ReferenceEquals(previous.Geometry,d.Geometry);
    sharedGeometry|=sameGeometry;
    if(sameGeometry&&(BitConverter.DoubleToInt64Bits(previous.StartDistance)!=BitConverter.DoubleToInt64Bits(d.StartDistance)||BitConverter.DoubleToInt64Bits(previous.EndDistance)!=BitConverter.DoubleToInt64Bits(d.EndDistance)))clipOnlyChanges++;
    if(d.IsConnection&&!sameGeometry)changedConnections++;
   }
   // Independent cold fixtures may reuse IDs; do not count those as edits.
   if(sharedGeometry)connectionChanges+=changedConnections;
  }
  foreach(var item in retained)if(!Buckets(item.network).SequenceEqual(item.buckets))throw new Exception("A later index mutated a retained prior index");
  if(keep)retained.Add((n,buckets));
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
   // Multiple topology generations without a query must inherit the last
   // available index, not assume the immediately preceding Network was warm.
   int e=world.AddJunction(new Point(0,2,100));int added=world.AddRoad(b,e,new[]{world.JunctionPosition(b),world.JunctionPosition(e)},lanes:2);
   if(!world.PlanUpgradeRoad(added,3,17,RoadDirection.Forward).TryCommit())throw new Exception("Delayed upgrade rejected");world.RemoveRoad(added);Probe(world.Network,output,700);
   if(!world.PlanUpgradeRoad(1,world.Network.Settings(1).lanes,17,world.Network.Settings(1).direction).TryCommit())throw new Exception("Speed edit rejected");Probe(world.Network,output,700);
   // Exact grid corners, side cells, negative/axis-aligned and purely vertical
   // segments; zero right vectors keep lane centres on known grid boundaries.
   var grid=new NetworkData{nextRoad=5,nextJunction=6};var points=new[]{new Point(-32,0,-32),new Point(32,0,32),new Point(32,0,-32),new Point(32,8,-32),new Point(-32,8,-32)};
   for(int i=0;i<points.Length;i++)grid.junctions.Add(new Junction{id=i+1,position=points[i]});
   for(int i=0;i<4;i++)grid.roads.Add(new PhysicalRoad{id=i+1,from=i+1,to=i+2,points=new[]{points[i],points[i+1]},rightVectors=new[]{new Point(),new Point()}});
   var priorGrid=new Network(grid);Probe(priorGrid,output,500);var cutData=new NetworkData{nextRoad=grid.nextRoad,nextJunction=grid.nextJunction,junctions=new List<Junction>(grid.junctions),roads=grid.roads.Skip(1).ToList()};var cutGrid=new Network(cutData,priorGrid);Probe(cutGrid,output,500);
   var city=CityCoreSave.Read(File.ReadAllBytes(args[0]));Probe(city.Transport.Network,output,2000,false);city.Validate();
   int removedRoad=args.Length>2?int.Parse(args[2]):87;city.Construction.RemoveRoad(removedRoad);new CityCoreRoadBindings(city.City,city.Transport).RefreshIfChanged();Probe(city.Transport.Network,output,1000,false);city.Validate();
   var checkpoint=CityCoreSave.Write(city);output.Write(checkpoint.Length);output.Write(checkpoint);
   var cold=CityCoreSave.Read(checkpoint);for(int i=0;i<80;i++){city.Simulation.Advance(.05,1,double.PositiveInfinity);cold.Simulation.Advance(.05,1,double.PositiveInfinity);}city.Validate();cold.Validate();
   var final=CityCoreSave.Write(city);if(!final.SequenceEqual(CityCoreSave.Write(cold)))throw new Exception("Cold continuation changed");output.Write(final.Length);output.Write(final);
  }
  if(clipOnlyChanges==0||connectionChanges==0)throw new Exception("Missing clip-only or connection-shape transition coverage");
  Console.WriteLine("PASS "+queries+" exact-output spatial queries; "+hitsWritten+" ordered hits; "+clipOnlyChanges+" clip-only / "+connectionChanges+" connection transitions; ordered bucket contents, retained-index immutability, curved/elevated/partial/corner/axis/vertical/radius/split/upgrade/remove/high IDs/unqueried generations/real city demolition and 80 hot/cold steps");
 }
}
