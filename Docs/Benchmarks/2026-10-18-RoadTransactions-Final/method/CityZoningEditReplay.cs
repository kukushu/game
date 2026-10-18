using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Security.Cryptography;using HarborCity;using HarborCity.RoadCore;
// Run twice with identical .NET x64 host, frozen or current zoning sources.
static class CityZoningEditReplay
{
    static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","").ToLowerInvariant();}
    static void Main(string[] args)
    {
        var s=CityCoreSave.Read(File.ReadAllBytes(args[0]));s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);double h=s.City.map.buildHalfSize;string output=args[1];Directory.CreateDirectory(output);
        var rows=new List<string>{"stage,state_sha256,cells,roads,active"};
        Action<string> record=stage=>{int cells=s.Simulation.Growth.Zoning.Cells().Count;rows.Add(stage+","+Hash(CityCoreSave.Write(s))+","+cells+","+s.Transport.Network.RoadCount+","+s.Transport.ActiveCount);File.WriteAllLines(Path.Combine(output,"replay.csv"),rows);Console.WriteLine(stage);};record("initial");
        Action<double,double,double,double> add=(x,z,X,Z)=>{var points=new Point[9];for(int i=0;i<9;i++)points[i]=Point.Lerp(new Point(x,2,z),new Point(X,2,Z),i/8.0);var plan=s.Transport.PlanConnectedRoad(points,false,snap:4,lanes:1);s.Construction.CheckRoadPlacement(plan);if(!s.Construction.Commit(plan,out _))throw new Exception("Stale replay plan");};
        var positions=s.Transport.Vehicles.ToDictionary(v=>v.id,v=>s.Transport.Position(v));
        add(-h+5,-h+8,-h+17,-h+8);record("setup_a");add(-h+5,-h+32,-h+17,-h+32);record("setup_b");add(-h+5,-h+56,-h+17,-h+56);int ordinary=s.Transport.Network.RoadIds.Max();record("add");add(-h+11,-h+8,-h+11,-h+32);record("connect_split");
        foreach(string operation in new[]{"widen","delete"})
        {
            var before=CityCoreSave.Write(s);try{if(operation=="widen")s.Construction.UpgradeRoad(ordinary,2,RoadDirection.Both);else s.Construction.RemoveRoad(ordinary);}
            catch(InvalidOperationException){if(!before.SequenceEqual(CityCoreSave.Write(s)))throw new Exception("Rejected replay edit mutated facts");}record(operation);
        }
        add(-h+5,-h+80,-h+17,-h+80);record("continuous_a");add(-h+5,-h+104,-h+17,-h+104);record("continuous_b");
        foreach(var pair in positions){var p=s.Transport.Position(s.Transport.FindVehicle(pair.Key));if(p.x!=pair.Value.x||p.y!=pair.Value.y||p.z!=pair.Value.z)throw new Exception("Margin edit moved an existing vehicle");}
        if(args.Length>2&&args[2]=="expanded")
        {
            var outcomes=new List<string>{"operation,status,reason"};
            Action<string,Action> attempt=(name,command)=>{var before=CityCoreSave.Write(s);try{command();outcomes.Add(name+",accepted,none");}catch(Exception error)when(error is InvalidOperationException||error is ArgumentException){if(!before.SequenceEqual(CityCoreSave.Write(s)))throw new Exception(name+" rejection changed full state");outcomes.Add(name+",rejected,\""+error.Message.Replace("\"","\"\"")+"\"");}record(name);};
            attempt("margin_reverse",()=>s.Construction.UpgradeRoad(ordinary,1,RoadDirection.Backward));
            var busy=s.Transport.Vehicles.First(v=>v.status==VehicleStatus.Active&&!s.Transport.Network.Get(v.Current).IsConnection);int busyRoad=s.Transport.Network.Get(busy.Current).RoadId;var settings=s.Transport.Network.Settings(busyRoad);
            attempt("occupied_widen",()=>s.Construction.UpgradeRoad(busyRoad,settings.lanes+1,settings.direction));
            attempt("occupied_reverse",()=>s.Construction.UpgradeRoad(busyRoad,settings.lanes,settings.direction==RoadDirection.Backward?RoadDirection.Forward:RoadDirection.Backward));
            var line=s.Transport.Network.Centreline(busyRoad);var middle=line.Sample(line.Length*.5);var direction=line.DirectionAt(line.Length*.5);
            attempt("occupied_cross",()=>add(middle.x-direction.z*8,middle.z+direction.x*8,middle.x+direction.z*8,middle.z-direction.x*8));
            attempt("occupied_delete",()=>s.Construction.RemoveRoad(busyRoad));
            attempt("invalid_zero_stroke",()=>add(-h+5,-h+128,-h+5,-h+128));
            var stale=s.Transport.PlanConnectedRoad(new[]{new Point(-h+5,2,-h+128),new Point(-h+17,2,-h+128)},snap:1);
            var paid=s.Transport.PlanConnectedRoad(new[]{new Point(-h+5,2,-h+152),new Point(-h+17,2,-h+152)},snap:1);s.Construction.CheckRoadPlacement(paid);if(!s.Construction.Commit(paid,out _))throw new Exception("Fresh payment fixture stale");record("payment_once");
            var afterPayment=CityCoreSave.Write(s);attempt("stale_commit",()=>s.Construction.Commit(stale,out _));if(!afterPayment.SequenceEqual(CityCoreSave.Write(s)))throw new Exception("Stale plan charged or changed facts");
            attempt("double_commit",()=>s.Construction.Commit(paid,out _));if(!afterPayment.SequenceEqual(CityCoreSave.Write(s)))throw new Exception("Repeated commit charged or changed facts");
            File.WriteAllLines(Path.Combine(output,"expanded-operations.csv"),outcomes);
        }
        for(int i=0;i<160;i++)if(s.Advance(.05)!=1)throw new Exception("Lost normal step");record("after160");
        var checkpoint=CityCoreSave.Write(s);var restored=CityCoreSave.Read(checkpoint);restored.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
        for(int i=0;i<80;i++){s.Advance(.05);restored.Advance(.05);}if(!CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(restored)))throw new Exception("In-flight warm/cold restore diverged");record("after240_restore");
        File.WriteAllBytes(Path.Combine(output,"final.hcity"),CityCoreSave.Write(s));File.WriteAllText(Path.Combine(output,"Result.txt"),"PASS multi-edit real transactions, failed edits unchanged, original car XYZ, 240 normal fixed steps and 80-step warm/cold in-flight continuation. Compare replay.csv between frozen/current same-runtime hosts.\n");
    }
}
