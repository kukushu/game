using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Globalization;
using HarborCity;
using HarborCity.RoadCore;

// Normal domain transaction + in-flight replay. No view/input shortcut and no FPS claim.
static class CityRoadPreviewReplay
{
    static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","").ToLowerInvariant();}
    static void Main(string[] args)
    {
        var s=CityCoreSave.Read(File.ReadAllBytes(args[0]));s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);s.Validate();string output=args[1];Directory.CreateDirectory(output);
        var rows=new List<string>{"stage,state_sha256,roads,population,active"};
        Action<string> record=stage=>{rows.Add(string.Join(",",stage,Hash(CityCoreSave.Write(s)),s.Transport.Network.RoadCount,s.Entities.Members.Count,s.Transport.ActiveCount));File.WriteAllLines(Path.Combine(output,"replay.csv"),rows);Console.WriteLine("Recorded "+stage);};record("initial");
        var positions=s.Transport.Vehicles.ToDictionary(v=>v.id,v=>s.Transport.Position(v));var before=CityCoreSave.Write(s);double hsize=s.City.map.buildHalfSize;
        var from=new Point(-hsize+8,2,-hsize+8);var to=new Point(-hsize+28,2,-hsize+8);var points=new Point[9];for(int i=0;i<9;i++)points[i]=Point.Lerp(from,to,i/8.0);
        var plan=s.Transport.PlanConnectedRoad(points,false,snap:4,lanes:1);s.Construction.CheckRoadPlacement(plan);
        if(!before.SequenceEqual(CityCoreSave.Write(s)))throw new Exception("Prepared transaction changed facts");record("prepared");
        if(!s.Construction.Commit(plan,out int paid)||paid!=267)throw new Exception("Unexpected commit or fee");
        foreach(var pair in positions){var actual=s.Transport.Position(s.Transport.FindVehicle(pair.Key));if(actual.x!=pair.Value.x||actual.y!=pair.Value.y||actual.z!=pair.Value.z)throw new Exception("Edit moved a real vehicle");}
        s.Validate();record("committed");var rejectedBefore=CityCoreSave.Write(s);bool rejected=false;
        try{s.Construction.Commit(plan,out paid);}catch(InvalidOperationException){rejected=true;}
        if(!rejected||!rejectedBefore.SequenceEqual(CityCoreSave.Write(s)))throw new Exception("Stale duplicate plan changed facts");record("duplicate_rejected");
        s.Simulation.Growth.Zoning.Cells();for(int i=0;i<160;i++)if(s.Advance(.05)!=1)throw new Exception("Lost replay step");s.Validate();record("after160");
        var checkpoint=CityCoreSave.Write(s);var restored=CityCoreSave.Read(checkpoint);restored.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
        for(int i=0;i<80;i++)if(s.Advance(.05)!=1||restored.Advance(.05)!=1)throw new Exception("Lost restored replay step");s.Validate();restored.Validate();if(!CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(restored)))throw new Exception("In-flight restore diverged");record("after240_restore");
        File.WriteAllLines(Path.Combine(output,"replay.csv"),rows);File.WriteAllBytes(Path.Combine(output,"final.hcity"),CityCoreSave.Write(s));File.WriteAllText(Path.Combine(output,"Result.txt"),"PASS original transaction, 267 fee, unchanged preview/duplicate failure, exact physical positions, 240 normal steps and 80-step warm/cold in-flight replay. Compare replay.csv between old/new sources; not FPS.\n");Console.WriteLine(rows.Last());
    }
}
