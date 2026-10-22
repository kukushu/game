using System;
using System.IO;
using System.Linq;
using HarborCity;
using HarborCity.RoadCore;

// Compare matching-tick native window endpoints. Wall-clock requested debt is
// checked separately by VerifyCityScenarioWindow; never edit either real city.
static class CityRoadViewStateComparison
{
    static void Main(string[] args)
    {
        var a=CityCoreSave.Read(File.ReadAllBytes(args[0]));var b=CityCoreSave.Read(File.ReadAllBytes(args[1]));
        if(a.Simulation.Tick!=b.Simulation.Tick)throw new Exception("Different fixed-step endpoints; not an equal-work state comparison");
        if(!SaveCodec.Write(a.Transport).SequenceEqual(SaveCodec.Write(b.Transport)))throw new Exception("Complete transport facts differ");
        var left=a.SnapshotBusiness();var right=b.SnapshotBusiness();double pa=left.clock.pendingSeconds,pb=right.clock.pendingSeconds;
        left.clock.pendingSeconds=right.clock.pendingSeconds=0;
        var json=new CityScenarioJson();if(json.Serialize(left)!=json.Serialize(right))throw new Exception("Complete business facts differ at equal tick");
        Console.WriteLine("PASS native endpoints at tick "+a.Simulation.Tick+": complete transport bytes and all business facts identical; only independently verified wall-time debt excluded ("+pa+" / "+pb+"). Original states unchanged.");
    }
}
