using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HarborCity
{
    public static class CityTrafficReplayChecks
    {
        [MenuItem("Harbor/Replay traffic diagnostic snapshot")]
        public static void Run()
        {
            // Replays an exported copy only; never advances or overwrites the live city.
            const string input="Temp/TrafficJam.json";
            if(!File.Exists(input)) throw new Exception("Copy a city snapshot to "+input+" first.");
            var c=JsonUtility.FromJson<CitySaveData>(File.ReadAllText(input)).ToCity();
            if(!c.Valid()) throw new Exception("Invalid traffic diagnostic snapshot");
            int completed=c.traffic.completed, failed=c.traffic.failed;
            var stuck=c.traffic.trips.Where(t=>t.blocked>30).Select(t=>t.id).ToArray();
            var sim=new CityTraffic(c); sim.Advance(30);
            int remaining=c.traffic.trips.Count(t=>stuck.Contains(t.id) && t.blocked>30);
            string result="Traffic replay 30s: completed +"+(c.traffic.completed-completed)+", failed +"+(c.traffic.failed-failed)+
                ", originally blocked="+stuck.Length+", still blocked="+remaining+", valid="+c.Valid();
            File.WriteAllText("Temp/TrafficReplayChecks.txt",result);
            if(remaining>0 || c.traffic.failed!=failed || !c.Valid()) throw new Exception(result);
            Debug.Log("PASS: "+result);
        }
    }
}
