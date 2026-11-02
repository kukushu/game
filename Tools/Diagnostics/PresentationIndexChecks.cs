using System;using System.IO;using System.Linq;using HarborCity;using HarborCity.RoadCore;
static class PresentationIndexChecks
{
 static void Main(string[] args)
 {
  // Multi-lane motion, departures, arrivals, topology and frozen/cold actors.
  var w=new World();int a=w.AddJunction(new Point(-160,0,-64)),b=w.AddJunction(new Point(0,0,0)),c=w.AddJunction(new Point(160,0,64));int r=w.AddRoad(a,b,new[]{w.JunctionPosition(a),new Point(-64,0,-64),w.JunctionPosition(b)},lanes:2);w.AddRoad(b,c,new[]{w.JunctionPosition(b),w.JunctionPosition(c)},lanes:2);int src=w.AddDepot(a,1000),dst=w.AddDepot(c,0);
  using(var index=new CityVehicleDisplayIndex(w))for(int i=0;i<700;i++){if(i%15==0)w.Dispatch(src,dst);w.Step();index.Sync();index.AssertExact();if(i==100){w.SplitRoad(r,1);index.Sync();index.AssertExact();}}
  using(var index=new CityVehicleDisplayIndex(w)){w.RemoveRoad(1);index.Sync();index.AssertExact();}
  using(var index=new CityVehicleDisplayIndex(SaveCodec.Read(SaveCodec.Write(w))))index.AssertExact();
  foreach(string path in args){var bytes=File.ReadAllBytes(path);var observed=CityCoreSave.Read(bytes);var reference=CityCoreSave.Read(bytes);using(var index=new CityVehicleDisplayIndex(observed.Transport)){
   for(int tick=0;tick<300;tick++){observed.Advance(.05);reference.Advance(.05);if(tick%3==0){index.Sync();index.AssertExact();}}
   index.Sync();index.AssertExact();if(!CityCoreSave.Write(observed).SequenceEqual(CityCoreSave.Write(reference)))throw new Exception("Display observation changed full simulation state");
   var cold=CityCoreSave.Read(CityCoreSave.Write(observed));using(var restored=new CityVehicleDisplayIndex(cold.Transport)){restored.AssertExact();cold.Advance(.05);observed.Advance(.05);restored.Sync();restored.AssertExact();if(!CityCoreSave.Write(cold).SequenceEqual(CityCoreSave.Write(observed)))throw new Exception("Cold continuation differs");}
   Console.WriteLine("PASS "+path+"; updates="+index.Updates+"; boundary/lane-change position samples="+index.PreciseIndexSamples);
  }}
  Console.WriteLine("PASS independent exact spatial oracle; multi-step journal coalescing; disposal/rebind; topology; save/cold; complete city bytes");
 }
}

