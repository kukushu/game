using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HarborCity
{
    public static class CityTrafficChecks
    {
        [Serializable]
        sealed class LegacyCity
        {
            public int version = 1, money = 65000, day = 1;
            public int[] tiles, levels;
        }
        [MenuItem("Harbor/Validate traffic saves")]
        public static void Validate()
        {
            var city = CityModel.Create();
            var simulation = new CityTraffic(city);
            simulation.Advance(30);
            string json = JsonUtility.ToJson(city);
            var restored = JsonUtility.FromJson<CityModel>(json);
            if (!restored.Valid() || restored.traffic.trips.Count != city.traffic.trips.Count)
                throw new Exception("Traffic save failed validation.");
            restored.Recalculate();
            var resumed = new CityTraffic(restored);
            // Locks are transient; rebuilding them must preserve legal continued trips.
            resumed.Advance(60);
            if (!restored.Valid() || restored.traffic.completed <= city.traffic.completed)
                throw new Exception("Restored traffic did not continue.");
            var legacy = CityModel.Create();
            legacy = JsonUtility.FromJson<CityModel>(JsonUtility.ToJson(new LegacyCity { tiles = legacy.tiles, levels = legacy.levels }));
            if (!legacy.Valid()) throw new Exception("Legacy city rejected before migration.");
            legacy.Recalculate();
            new CityTraffic(legacy).Advance(5);
            if (!legacy.Valid() || legacy.traffic.trips.Count == 0) throw new Exception("Legacy save migration failed.");
            string result = "PASS: Unity JsonUtility round-trip, continued traffic, legacy save migration.";
            Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/CityTrafficChecks.txt",result);
            Debug.Log(result);
        }
    }
}
