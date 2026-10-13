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
            if (terrain.Terrain == null || terrain.Terrain.GetComponent<TerrainCollider>() == null)
                throw new Exception("Landscape must use Unity Terrain and TerrainCollider.");
            if (terrain.Terrain.materialTemplate == null || terrain.Terrain.terrainData.terrainLayers.Length != 4)
                throw new Exception("Terrain rendering assets are missing.");
            Physics.SyncTransforms();
            int samples = 0;
            float minimum = float.PositiveInfinity, maximum = float.NegativeInfinity;
            for (int z = -110; z <= 110; z += 5) for (int x = -110; x <= 110; x += 5)
            {
                float px = x + .31f, pz = z + .63f;
                float h = terrain.Height(px,pz);
                if (!terrain.Raycast(new Ray(new Vector3(px,200,pz),Vector3.down),out Vector3 hit)
                    || Mathf.Abs(hit.y - h) > .04f) throw new Exception("Terrain height and picking disagree at " + px + ", " + pz);
                minimum = Mathf.Min(minimum,h); maximum = Mathf.Max(maximum,h); samples++;
            }
            if (maximum - minimum < 20) throw new Exception("Landscape lacks expected elevation range.");
            var game=UnityEngine.Object.FindAnyObjectByType<HarborCityGame>();
            if(game!=null)
            {
                var city=(CityModel)typeof(HarborCityGame).GetField("city",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(game);
                foreach(var building in city.buildings)
                    for(int z=0;z<=6;z++) for(int x=0;x<=6;x++)
                    {
                        var point=building.Point((x/6f-.5f)*building.width,(z/6f-.5f)*building.depth);
                        if(terrain.Height(point.x,point.z)<=CityLandscape.SeaLevel) throw new Exception("Building footprint submerged: "+building.id);
                    }
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
            // Terrain height interpolation and collider triangles may differ slightly between samples.
            var weights = terrain.Terrain.terrainData.GetAlphamaps(0,0,terrain.Terrain.terrainData.alphamapWidth,terrain.Terrain.terrainData.alphamapHeight);
            for (int z = 0; z < weights.GetLength(0); z++) for (int x = 0; x < weights.GetLength(1); x++)
            {
                float sum = 0;
                for (int layer = 0; layer < 4; layer++) sum += weights[z,x,layer];
                if (Mathf.Abs(sum - 1) > .02f) throw new Exception("Terrain layer weights not normalized.");
            }
            string result = $"PASS: Unity Terrain + TerrainCollider + 4 layers; {samples} terrain raycasts (4 cm tolerance); {surfaceVertices} road/zone vertices; live building footprints; normalized layer weights; elevation {minimum:F2} to {maximum:F2}.";
            File.WriteAllText("Temp/CityLandscapeChecks.txt",result);
            Debug.Log(result);
        }
    }
}
