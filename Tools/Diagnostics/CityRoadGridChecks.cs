using System;
using System.Linq;
using HarborCity;
using HarborCity.RoadCore;

static class CityRoadGridChecks
{
    static int checks;
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    static float Flat(float x,float z)=>2;
    static byte[] Save(CityCoreIndustrySession s)=>CityCoreSave.Write(s);
    static void Main()
    {
        foreach(int direction in new[]{-1,1})foreach(int spacing in new[]{24,32,48,64})
        {
            var grid=new CityRoadGrid(new Point(0,2,0),new Point(direction*spacing*2.5,2,direction*spacing*3.5),spacing,300,Flat);
            Check(grid.Columns==2&&grid.Rows==3&&grid.Strokes.Length==7,"Drag direction/spacing counts");
            foreach(var stroke in grid.Strokes)foreach(var p in stroke)Check(p.y==2&&Math.Abs(p.x)<=spacing*2&&Math.Abs(p.z)<=spacing*3,"Grid extent/height");
        }
        var capped=new CityRoadGrid(new Point(0,2,0),new Point(1000,2,1000),24,2048,Flat);
        Check(capped.Columns==8&&capped.Rows==8&&capped.Strokes.Length==18,"Bounded grid");
        var city=CityCoreIndustrySession.CreateEmpty();city.City.map=CityMapState.DiagnosticFlat(168);city.Simulation.Growth.Zoning.SetSurface(Flat);
        var rectangle=new CityRoadGrid(new Point(-32,2,-48),new Point(32,2,48),32,168,Flat);
        var before=Save(city);int cash=city.City.money;
        var plan=city.Transport.PlanConnectedRoads(rectangle.Strokes,snap:1);
        Check(before.SequenceEqual(Save(city)),"Planning changed live city");
        int quoted=city.Construction.CheckRoadPlacement(plan);
        Check(city.Construction.Commit(plan,out int paid)&&paid==quoted&&city.City.money==cash-paid,"Single batch payment");
        Check(city.Transport.Network.JunctionCount==12&&city.Transport.Network.RoadCount==17,"Shared intersection topology");city.Validate();
        var bytes=Save(city);var restored=CityCoreSave.Read(bytes);restored.Simulation.Growth.Zoning.SetSurface(Flat);Check(bytes.SequenceEqual(Save(restored)),"Grid save round trip");
        for(int i=0;i<80;i++){city.Advance(.05);restored.Advance(.05);}Check(Save(city).SequenceEqual(Save(restored)),"Saved continuation");
        before=Save(city);bool rejected=false;
        try{city.Transport.PlanConnectedRoads(new[]{new[]{new Point(96,2,-60),new Point(96,2,60)},rectangle.Strokes[0]},snap:1);}catch(InvalidOperationException){rejected=true;}
        Check(rejected&&before.SequenceEqual(Save(city)),"Late overlapping stroke partially committed");
        var extension=new CityRoadGrid(new Point(64,2,-32),new Point(96,2,0),32,168,Flat);
        city.Simulation.Finance.CheckSpend(city.City.money);city.Simulation.Finance.SpendApproved(city.City.money);before=Save(city);rejected=false;
        try{var expensive=city.Transport.PlanConnectedRoads(extension.Strokes,snap:1);city.Construction.CheckRoadPlacement(expensive);}catch(InvalidOperationException){rejected=true;}
        Check(rejected&&before.SequenceEqual(Save(city)),"Insufficient money changed city");
        foreach(bool oneWay in new[]{false,true})
        {
            var w=new World();var g=new CityRoadGrid(new Point(-32,2,-32),new Point(32,2,32),32,168,Flat,oneWay);
            Check(w.PlanConnectedRoads(g.Strokes,oneWay,snap:1,lanes:2).TryCommit(),"Wide/one-way batch");w.Validate();
            int a=w.SnapJunction(new Point(-32,2,-32),.1),b=w.SnapJunction(new Point(32,2,32),.1);
            Check(w.Network.Route(a,b)!=null&&w.Network.Route(b,a)!=null,"Grid directional connectivity");
        }
        var maximum=new World();var large=new CityRoadGrid(new Point(-96,2,-96),new Point(96,2,96),24,168,Flat);
        Check(maximum.PlanConnectedRoads(large.Strokes,snap:1).TryCommit()&&maximum.Network.RoadCount==144&&maximum.Network.JunctionCount==81,"8x8 grid topology");maximum.AssertSafety();
        var busy=CityRoadCoreFixtures.Create(20,false);busy.Advance(.1);var unchanged=SaveCodec.Write(busy);var original=busy.Snapshot();
        var remote=new CityRoadGrid(new Point(-100,2,-100),new Point(-68,2,-68),32,168,Flat);
        Check(busy.PlanConnectedRoads(remote.Strokes,snap:1).TryCommit(),"Grid with active vehicles");
        var after=busy.Snapshot();after.network=original.network;after.networkRevision=original.networkRevision;
        Check(unchanged.SequenceEqual(SaveCodec.Write(new World(after))),"Grid changed unrelated vehicles/tasks/inventory/time");
        var cold=SaveCodec.Read(SaveCodec.Write(busy));for(int i=0;i<80;i++){busy.Step();cold.Step();}Check(SaveCodec.Write(busy).SequenceEqual(SaveCodec.Write(cold)),"Active vehicle grid save continuation");
        var stale=busy.PlanConnectedRoads(new[]{new[]{new Point(-150,2,-150),new Point(-120,2,-150)}},snap:1);busy.Step();unchanged=SaveCodec.Write(busy);
        Check(!stale.TryCommit()&&unchanged.SequenceEqual(SaveCodec.Write(busy)),"Stale grid plan changed city");
        Console.WriteLine("PASS "+checks+" grid geometry, topology, atomic rollback, payment, direction and save checks");
    }
}
