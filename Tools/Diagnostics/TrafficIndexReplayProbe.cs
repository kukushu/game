using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using HarborCity;

// Correctness replay only: serialization is deliberately outside performance measurements.
public static class TrafficIndexReplayProbe
{
    public static void Main(string[] args)
    {
        var json=new JavaScriptSerializer{MaxJsonLength=int.MaxValue};
        var city=json.Deserialize<CitySaveData>(File.ReadAllText(args[0])).ToCity();
        if(!city.Valid())throw new Exception("Invalid input");
        city.analysis=new CityDailyAnalysis(city);var traffic=new CityTraffic(city);
        using(var hashes=new StreamWriter(args[1]))using(var sha=SHA256.Create())
            for(int step=0;step<340;step++)
            {
                if(args.Length>2 && args[2]=="topology")
                {
                    var topology=city.roads.GetLogicalTopology();
                    foreach(var t in city.traffic.trips)if(t.Current!=t.Next && city.roads.Linked(t.Current,t.Next))
                        if(!topology.TryMapLegacy(t.Current,t.Next,t.progress,out var p) || !topology.TryToLegacy(p,out int from,out int to,out float progress) || from!=t.Current || to!=t.Next || Math.Abs(progress-t.progress)>.000001f)
                            throw new Exception("Topology trip roundtrip failed");
                }
                traffic.Advance(.05f);
                string state=json.Serialize(city.ToSaveData());
                hashes.WriteLine(step+","+city.traffic.trips.Count+","+BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(state))).Replace("-",""));
            }
        if(!city.Valid())throw new Exception("Invalid final state");
        Console.WriteLine("PASS: 340 complete-state signatures (40 warmup + 300 measured-step trajectory), input unchanged");
    }
}
