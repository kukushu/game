using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using HarborCity.RoadCore;
static class CityPassengerPrecheckChecks
{
 static int checks;
 static void Check(bool p,string text){checks++;if(!p)throw new Exception(text);}
 static World Make(bool oneWay,bool forward,bool reverse,bool loop,bool curved,double offset,out int source,out int target)
 {
  var w=new World();int a=w.AddJunction(new Point()),b=w.AddJunction(new Point(100,0,0));int road=w.AddRoad(a,b,curved?new[]{w.JunctionPosition(a),new Point(50,0,20),w.JunctionPosition(b)}:new[]{w.JunctionPosition(a),w.JunctionPosition(b)},oneWay);
  if(loop){int c=w.AddJunction(new Point(50,0,30));w.AddRoad(b,c,new[]{w.JunctionPosition(b),w.JunctionPosition(c)});w.AddRoad(c,a,new[]{w.JunctionPosition(c),w.JunctionPosition(a)});}
  source=w.AddDepot(a);target=w.AddDepot(b);long f=Network.LaneId(road,false),r=Network.LaneId(road,true);w.RegisterAccess(source,f,20);w.RegisterAccess(target,f,80);
  int exitF=w.AddDepot(b);w.RegisterAccess(exitF,f,90);int exitR=0;if(!oneWay){exitR=w.AddDepot(a);w.RegisterAccess(exitR,r,90);}
  int originF=w.AddDepot(a);w.RegisterAccess(originF,f,w.Network.Get(f).StartDistance);int originR=0;if(!oneWay){originR=w.AddDepot(b);w.RegisterAccess(originR,r,w.Network.Get(r).StartDistance);}
  double reverseStart=oneWay?0:w.Network.Get(f).Geometry.MapReverseDistance(w.Network.Get(r).Geometry,20);
  var state=w.Snapshot();int id=1;
  foreach(bool back in new[]{false,true})if(back?reverse:forward)
  {
   var task=new TripTask{id=id,origin=back?originR:originF,destination=back?exitR:exitF,purpose=TransportPurpose.Freight,Shipment=new Shipment{remaining=1,unitPrice=0}};var car=new Vehicle{id=id,Task=task,Plan=new RoutePlan{segments=new List<long>{back?r:f},EndDistance=90},Position=new VehiclePosition(back?r:f,(back?reverseStart:20)+offset)};
   state.depots.Single(d=>d.id==task.origin).stock--;state.tasks.Add(task);state.vehicles.Add(car);id++;
  }
  state.nextVehicle=state.nextTask=id;w=new World(state);w.Validate();return w;
 }
 static void Main(string[] args)
 {
  var rows=new List<string>();
  foreach(var purpose in new[]{TransportPurpose.Work,TransportPurpose.Home,TransportPurpose.Shopping,TransportPurpose.Service})foreach(bool oneWay in new[]{false,true})foreach(bool f in new[]{false,true})foreach(bool r in new[]{false,true})foreach(bool loop in new[]{false,true})foreach(bool curved in new[]{false,true})foreach(double offset in new[]{-1.5001,-1.5,-1.4999,0,1.4999,1.5,1.5001})
  {
   if(oneWay&&r)continue;var w=Make(oneWay,f,r,loop,curved,offset,out int source,out int target);var before=SaveCodec.Write(w);long routes=w.RoutePreparations;
   var admission=w.PrepareTransport(source,target,0,0,purpose,purpose==TransportPurpose.Service?0:123,out var reason);
   Check(before.SequenceEqual(SaveCodec.Write(w)),"Preparation changed authoritative state");w.Validate();
   rows.Add(string.Join(",",purpose,oneWay,f,r,loop,curved,offset.ToString("R",CultureInfo.InvariantCulture),reason,admission==null?"null":string.Join("/",admission.vehicle.Plan.segments)+":"+admission.vehicle.distance.ToString("R",CultureInfo.InvariantCulture)+":"+admission.vehicle.Plan.EndDistance.ToString("R",CultureInfo.InvariantCulture)));
#if EXPECT_FAST_SOURCE
   if(f&&(oneWay||r||!loop)&&offset==0)Check(admission==null&&w.RoutePreparations==routes,"Fully blocked reachable source did route work");
   if(loop&&!oneWay&&f&&!r&&offset==0)Check(admission==null&&w.RoutePreparations==routes+1,"Changed best-route rule when a reachable alternative departure was free");
#endif
  }
  // Even a blocked source must preserve NoRoute when no legal direction can reach the goal.
  foreach(var purpose in new[]{TransportPurpose.Work,TransportPurpose.Home,TransportPurpose.Shopping,TransportPurpose.Service})
  {
   var w=new World();int a=w.AddJunction(new Point()),b=w.AddJunction(new Point(100,0,0));int road=w.AddRoad(a,b,new[]{w.JunctionPosition(a),w.JunctionPosition(b)},true);long lane=Network.LaneId(road,false);
   int source=w.AddDepot(a),target=w.AddDepot(b),cargoOrigin=w.AddDepot(a),cargoGoal=w.AddDepot(b);w.RegisterAccess(source,lane,80);w.RegisterAccess(target,lane,20);w.RegisterAccess(cargoOrigin,lane,0);w.RegisterAccess(cargoGoal,lane,90);
   var data=w.Snapshot();var task=new TripTask{id=1,origin=cargoOrigin,destination=cargoGoal,purpose=TransportPurpose.Freight,Shipment=new Shipment{remaining=1,unitPrice=0}};data.depots.Single(d=>d.id==cargoOrigin).stock--;data.tasks.Add(task);data.vehicles.Add(new Vehicle{id=1,Task=task,Plan=new RoutePlan{segments=new List<long>{lane},EndDistance=90},Position=new VehiclePosition(lane,80)});data.nextVehicle=data.nextTask=2;w=new World(data);w.Validate();
   byte[] before=SaveCodec.Write(w);var admission=w.PrepareTransport(source,target,0,0,purpose,purpose==TransportPurpose.Service?0:123,out var reason);
   Check(admission==null&&reason==TransportRejection.NoRoute,"Blocked but unreachable source misclassified");Check(before.SequenceEqual(SaveCodec.Write(w)),"Failed unreachable preparation changed facts");rows.Add("unreachable,"+purpose+","+reason);w.Validate();
  }
  File.WriteAllLines(args[0],rows);Console.WriteLine("PASS "+checks+" passenger/service source checks; one/two directions, one side free, spacing threshold and failed prepare state unchanged");
 }
}
