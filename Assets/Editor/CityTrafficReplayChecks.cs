using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HarborCity
{
    public static class CityTrafficReplayChecks
    {
        [MenuItem("Harbor/Export live traffic diagnostic snapshot")]
        public static void ExportLive()
        {
            var game=UnityEngine.Object.FindAnyObjectByType<HarborCityGame>();
            var field=typeof(HarborCityGame).GetField("city",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var city=game==null?null:field.GetValue(game) as CityModel;
            if(!Application.isPlaying || city==null)throw new InvalidOperationException("Run the city first.");
            Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/TrafficJam.json",JsonUtility.ToJson(city.ToSaveData(),true));
            Debug.Log("Exported live traffic diagnostic copy to Temp/TrafficJam.json; live simulation unchanged.");
        }
        [MenuItem("Harbor/Replay traffic diagnostic snapshot")]
        public static void Run()
        {
            // Replays an exported copy only; never advances or overwrites the live city.
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-harbor-traffic-snapshot");
            string input=index>=0 && index+1<args.Length?args[index+1]:"Temp/TrafficJam.json";
            if(!File.Exists(input)) throw new Exception("Copy a city snapshot to "+input+" first.");
            var c=JsonUtility.FromJson<CitySaveData>(File.ReadAllText(input)).ToCity();
            if(!c.Valid()) throw new Exception("Invalid traffic diagnostic snapshot");
            int completed=c.traffic.completed, failed=c.traffic.failed;
            int targetIndex=Array.IndexOf(args,"-harbor-traffic-trip");
            int target=targetIndex>=0 && targetIndex+1<args.Length?int.Parse(args[targetIndex+1]):0;
            if(target>0 && !c.traffic.trips.Any(t=>t.id==target))throw new Exception("Diagnostic trip not found: "+target);
            var stuck=c.traffic.trips.Where(t=>t.blocked>30).Select(t=>t.id).ToArray();
            var passengers=c.traffic.trips.Where(t=>(target>0?t.id==target:t.blocked>30) && t.returning && t.residentId>0).Select(t=>new {t.id,t.residentId,t.home}).ToArray();
            var restored=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(c.ToSaveData())).ToCity();
            var sim=new CityTraffic(c); sim.Advance(30);
            new CityTraffic(restored).Advance(30);
            bool deterministic=JsonUtility.ToJson(c.ToSaveData())==JsonUtility.ToJson(restored.ToSaveData());
            bool arrived=passengers.All(t=>!c.traffic.trips.Any(x=>x.id==t.id) && c.Citizens.Any(p=>p.id==t.residentId && p.location==t.home));
            int remaining=c.traffic.trips.Count(t=>stuck.Contains(t.id) && t.blocked>30);
            int targetRemaining=target>0?c.traffic.trips.Count(t=>t.id==target && t.blocked>30):remaining;
            string result="Traffic replay 30s: completed +"+(c.traffic.completed-completed)+", failed +"+(c.traffic.failed-failed)+
                ", originally blocked="+stuck.Length+", still blocked="+remaining+", target="+target+", target still blocked="+targetRemaining+", returning passengers home="+arrived+", native save deterministic="+deterministic+", valid="+c.Valid();
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(input)),"TrafficReplayChecks.txt"),result);
            if(targetRemaining>0 || !arrived || !deterministic || c.traffic.failed!=failed || !c.Valid()) throw new Exception(result);
            Debug.Log("PASS: "+result);
        }
    }
}
