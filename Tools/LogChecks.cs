using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using HarborCity;

public static class LogChecks
{
    static int checks;
    static readonly JavaScriptSerializer json=new JavaScriptSerializer {MaxJsonLength=int.MaxValue};
    static void Check(bool value,string reason) {if(!value) throw new Exception("Logs: "+reason); checks++;}
    static string Serialize(object o)
    {
        if(o is CityLogEvent || o is CityModel)
            return json.Serialize(o.GetType().GetFields().Where(f=>!f.IsNotSerialized).ToDictionary(f=>f.Name,f=>f.GetValue(o)));
        return json.Serialize(o);
    }
    static CityModel City()
    {
        var c=CityModel.CreateLegacySample(); new CityTraffic(c); c.EnableRoads((x,z)=>1); c.EnableBuildings(); c.EnableHouseholds(); c.EnableResidentTransport(); return c;
    }
    public static void Run()
    {
        string root=Path.Combine("Temp","LogChecks-"+Guid.NewGuid().ToString("N"));
        var c=City(); var reference=City();
        string export;
        using(var log=new CitySimulationLog(root,Serialize,1000))
        {
            c.logSink=log.Write; log.Snapshot("baseline",c);
            c.Trace("test.escape","中文 \"quote\"\n换行",new CityLogDetail {reason="路径\\含中文"},citizen:10);
            new CityTraffic(c).Advance(360.1f); new CityTraffic(reference).Advance(360.1f);
            Check(c.money==reference.money && json.Serialize(c.society)==json.Serialize(reference.society) && json.Serialize(c.traffic)==json.Serialize(reference.traffic),"Logging does not change decisions, clock, trips or economy");
            export=log.Export(Path.Combine(root,"Exports"),c);
            Check(log.Failure==null,"Journal/export has no write errors");
            var files=Directory.GetFiles(export,"events-*.jsonl").OrderBy(p=>p,StringComparer.Ordinal).ToArray();
            Check(files.Length>1,"Long sessions rotate without dropping history");
            var records=files.SelectMany(File.ReadAllLines).Select(line=>json.Deserialize<Dictionary<string,object>>(line)).ToArray();
            Check(records.Select((r,i)=>Convert.ToInt64(r["seq"])==i+1).All(v=>v),"All JSONL lines parse with contiguous sequence IDs");
            Check((string)records[0]["message"]=="中文 \"quote\"\n换行" && records[0]["data"] is Dictionary<string,object>,"Unicode, quotes, newlines and nested structured payload survive");
            Check(new[]{"trip.started","trip.arrived","trip.finished","citizen.pay","household.decision","city.day"}.All(type=>records.Any(r=>(string)r["type"]==type)),"Journal covers actual trips, decisions and settlement");
            Check(records.Any(r=>(string)r["type"]=="trip.started" && Convert.ToInt32(r["citizenId"])>0 && Convert.ToInt32(r["householdId"])>0),"Trip events correlate residents and households");
            Check(File.Exists(Path.Combine(export,"baseline.json")) && File.Exists(Path.Combine(export,"latest.json")) && File.Exists(Path.Combine(export,"summary.json")),"Export includes baseline, final snapshot and summary");
            Check(File.ReadAllLines(Path.Combine(export,"daily.csv")).Length==4,"Every simulated day exported to CSV");
            int oldLines=records.Length; c.Trace("test.after_export","导出后继续记录"); log.Flush();
            Check(files.SelectMany(File.ReadAllLines).Count()==oldLines,"Export is immutable while recording continues");
        }
        using(var broken=new CitySimulationLog(root,o=>throw new IOException("test disk failure")))
        {c.logSink=broken.Write; c.Trace("test.failure","故障隔离"); Check(broken.Failure!=null,"Writer failure is visible without crashing gameplay");}
        c.logSink=e=>throw new Exception("sink failure"); c.Trace("test.sink","不影响模拟"); Check(c.Valid(),"Failing observer cannot break simulation");
        Console.WriteLine("PASS: "+checks+" structured log checks");
    }
}
