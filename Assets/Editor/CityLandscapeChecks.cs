using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HarborCity
{
    public static class CityLandscapeChecks
    {
        [MenuItem("Harbor/Validate terrain")]
        public static void Validate()
        {
            var terrain = UnityEngine.Object.FindAnyObjectByType<CityLandscape>();
            if (terrain == null) throw new InvalidOperationException("Enter Play mode before checking terrain.");
            Physics.SyncTransforms();
            int samples = 0;
            float minimum = float.PositiveInfinity, maximum = float.NegativeInfinity;
            for (int z = -110; z <= 110; z += 5) for (int x = -110; x <= 110; x += 5)
            {
                float px = x + .31f, pz = z + .63f;
                float h = terrain.Height(px,pz);
                if (!terrain.Raycast(new Ray(new Vector3(px,200,pz),Vector3.down),out Vector3 hit)
                    || Mathf.Abs(hit.y - h) > .002f) throw new Exception("Terrain height and picking disagree at " + px + ", " + pz);
                minimum = Mathf.Min(minimum,h); maximum = Mathf.Max(maximum,h); samples++;
            }
            if (maximum - minimum < 20) throw new Exception("Landscape lacks expected elevation range.");
            var city = CityModel.Create();
            for (int i = 0; i < city.tiles.Length; i++)
            {
                if (city.tiles[i] == 0) continue;
                Vector3 p = new Vector3((i % 36 - 17.5f) * 3,0,(i / 36 - 17.5f) * 3);
                terrain.LotRange(p,out float low,out float high);
                if (low <= CityLandscape.SeaLevel || high - low > 1.5f) throw new Exception("Starter city lot too steep or submerged: " + i);
            }
            int surfaceVertices = 0;
            foreach (var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>())
            {
                if (filter.name != "Lot" && filter.name != "Lane marking") continue;
                float lift = filter.name == "Lot" ? .06f : .09f;
                foreach (Vector3 vertex in filter.sharedMesh.vertices)
                {
                    Vector3 p = filter.transform.TransformPoint(vertex);
                    if (Mathf.Abs(p.y - terrain.Height(p.x,p.z) - lift) > .002f) throw new Exception("A road or zone is floating/buried.");
                    surfaceVertices++;
                }
            }
            string result = $"PASS: {samples} terrain raycasts; {surfaceVertices} road/zone vertices; starter city slopes; elevation {minimum:F2} to {maximum:F2}.";
            File.WriteAllText("Temp/CityLandscapeChecks.txt",result);
            Debug.Log(result);
        }
    }
}
