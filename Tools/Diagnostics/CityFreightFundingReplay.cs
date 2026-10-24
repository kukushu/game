using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Collections.Generic;
using HarborCity;

// Compile the same replay against frozen and current sources. Full saves are
// compared exactly; loading, checkpoints and validation are not player timings.
static class CityFreightFundingReplay
{
    static string Hash(byte[] value){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(value)).Replace("-","");}
    static void Main(string[] args)
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
        var s=CityCoreSave.Read(File.ReadAllBytes(args[0]));s.Transport.DepotLaneRoutingEnabled=true;
        s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
        var rows=new List<string>{"step,tick,population,active,sha256"};
        int steps=int.Parse(args[2]);var timer=Stopwatch.StartNew();
        for(int i=0;i<=steps;i++)
        {
            if(i%400==0||i==steps){s.Validate();rows.Add(string.Join(",",i,s.Simulation.Tick,s.City.population,s.Transport.ActiveCount,Hash(CityCoreSave.Write(s))));}
            if(i==steps)break;
            if(s.Simulation.Advance(.05,1,double.PositiveInfinity)!=1)throw new Exception("Lost fixed step");
            s.Transport.AssertSafety();if(s.Simulation.Waste.BalanceError!=0)throw new Exception("Waste conservation failed");
        }
        timer.Stop();byte[] final=CityCoreSave.Write(s);File.WriteAllLines(args[1]+".csv",rows);File.WriteAllBytes(args[1]+".hcity",final);
        var restored=CityCoreSave.Read(final);restored.Transport.DepotLaneRoutingEnabled=true;restored.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
        for(int i=0;i<80;i++){if(s.Simulation.Advance(.05,1,double.PositiveInfinity)!=1||restored.Simulation.Advance(.05,1,double.PositiveInfinity)!=1)throw new Exception("Restore lost a step");}
        s.Validate();restored.Validate();if(!CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(restored)))throw new Exception("Cold continuation differs");
        Console.WriteLine("PASS "+steps+" exact fixed steps, per-step safety/waste, complete checkpoints and 80-step cold continuation; model wall ms including validation="+timer.Elapsed.TotalMilliseconds);
    }
}
