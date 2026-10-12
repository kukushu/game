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
                traffic.Advance(.05f);
                string state=json.Serialize(city.ToSaveData());
                hashes.WriteLine(step+","+city.traffic.trips.Count+","+BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(state))).Replace("-",""));
            }
        if(!city.Valid())throw new Exception("Invalid final state");
        Console.WriteLine("PASS: 340 complete-state signatures (40 warmup + 300 measured-step trajectory), input unchanged");
    }
}
