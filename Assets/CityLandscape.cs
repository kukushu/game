using System.Collections.Generic;
using UnityEngine;

namespace HarborCity
{
    // Fixed landscape seed/shape: older city saves keep exactly the same lot coordinates.
    public sealed class CityLandscape : MonoBehaviour
    {
        const int Resolution = 513;
        const float HalfSize = 120f;
        const float BaseHeight = -8f;
        const float HeightRange = 64f;
        Terrain terrain;
        TerrainData data;
        TerrainCollider ground;
        readonly List<Object> ownedAssets = new List<Object>();
        public Terrain Terrain => terrain;
        public const float SeaLevel = 0f;

        static float Hill(float x, float z, float cx, float cz, float radius, float height)
        {
            float dx = (x - cx) / radius, dz = (z - cz) / radius;
            return height * Mathf.Exp(-(dx * dx + dz * dz));
        }

        static float Elevation(float x, float z)
        {
            float radius = Mathf.Sqrt(x * x + z * z);
            float angle = Mathf.Atan2(z, x);
            float shoreline = 91 + 8 * Mathf.Sin(angle * 3 + .8f) + 5 * Mathf.Cos(angle * 5);
            float coast = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(shoreline - 15, shoreline + 10, radius));
            float hills = Hill(x,z,-37,38,20,18) + Hill(x,z,39,46,19,25)
                + Hill(x,z,-39,-39,22,13) + Hill(x,z,57,-26,24,12)
                + Hill(x,z,-62,60,24,17);
            float centre = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(28, 54, radius));
            float noise = (Mathf.PerlinNoise(x * .035f + 41, z * .035f + 73) - .5f) * 2;
            float land = 2.1f + hills * centre + noise * (.25f + centre * 1.8f);
            // Keep the west entrance as an accessible valley through the hills.
            float entranceValley = x < 0 ? Mathf.Exp(-Mathf.Pow((z - 1.5f) / 7f, 2)) : 0;
            land = Mathf.Lerp(land, 2.1f, entranceValley * .9f);
            return Mathf.Lerp(land, -7f, coast);
        }

        public void Build()
        {
            if (terrain != null) return;
            var material = Resources.Load<Material>("HarborCity/Terrain");
            if (material == null) throw new System.InvalidOperationException("Missing HarborCity Terrain material.");
            data = new TerrainData { name = "Harbor Island v1", heightmapResolution = Resolution,
                size = new Vector3(HalfSize * 2,HeightRange,HalfSize * 2), alphamapResolution = 256, baseMapResolution = 512 };
            ownedAssets.Add(data);
            var heights = new float[Resolution, Resolution];
            for (int z = 0; z < Resolution; z++) for (int x = 0; x < Resolution; x++)
            {
                float wx = x / (float)(Resolution - 1) * HalfSize * 2 - HalfSize;
                float wz = z / (float)(Resolution - 1) * HalfSize * 2 - HalfSize;
                heights[z,x] = Mathf.Clamp01((Elevation(wx,wz) - BaseHeight) / HeightRange);
            }
            data.SetHeights(0,0,heights);
            var colors = new[] { new Color(.39f,.54f,.32f), new Color(.30f,.43f,.28f),
                new Color(.49f,.50f,.45f), new Color(.75f,.70f,.52f) };
            var layers = new TerrainLayer[colors.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                // Original procedural placeholders; replace with approved art assets later.
                var texture = new Texture2D(64,64,TextureFormat.RGBA32,true) { name = "Ground layer " + i, wrapMode = TextureWrapMode.Repeat };
                var pixels = new Color[64 * 64];
                for (int z = 0; z < 64; z++) for (int x = 0; x < 64; x++)
                {
                    float shade = .92f + .16f * Mathf.PerlinNoise(x * .19f + i * 13,z * .19f + 7);
                    pixels[z * 64 + x] = new Color(colors[i].r * shade,colors[i].g * shade,colors[i].b * shade,1);
                }
                texture.SetPixels(pixels); texture.Apply(true,true); ownedAssets.Add(texture);
                layers[i] = new TerrainLayer { name = "Harbor surface " + i, diffuseTexture = texture,
                    tileSize = new Vector2(12,12), smoothness = .05f,
                    smoothnessSource = TerrainLayerSmoothnessSource.ConstantOnly, metallic = 0 };
                ownedAssets.Add(layers[i]);
            }
            data.terrainLayers = layers;
            int size = data.alphamapResolution;
            var weights = new float[size,size,4];
            for (int z = 0; z < size; z++) for (int x = 0; x < size; x++)
            {
                float u = x / (float)(size - 1), v = z / (float)(size - 1);
                float h = data.GetInterpolatedHeight(u,v) + BaseHeight;
                float sand = 1 - Mathf.SmoothStep(0,1,Mathf.InverseLerp(.5f,2.2f,h));
                float rock = Mathf.Max(Mathf.SmoothStep(0,1,Mathf.InverseLerp(27,43,data.GetSteepness(u,v))),
                    Mathf.SmoothStep(0,1,Mathf.InverseLerp(20,27,h))) * (1 - sand);
                float upland = Mathf.SmoothStep(0,1,Mathf.InverseLerp(6,15,h)) * (1 - sand - rock);
                weights[z,x,0] = 1 - sand - rock - upland;
                weights[z,x,1] = upland; weights[z,x,2] = rock; weights[z,x,3] = sand;
            }
            data.SetAlphamaps(0,0,weights);
            var obj = UnityEngine.Terrain.CreateTerrainGameObject(data);
            obj.name = "Harbor Terrain";
            obj.transform.SetParent(transform,false);
            obj.transform.localPosition = new Vector3(-HalfSize,BaseHeight,-HalfSize);
            terrain = obj.GetComponent<Terrain>(); ground = obj.GetComponent<TerrainCollider>();
            terrain.materialTemplate = material; terrain.drawInstanced = true;
            terrain.heightmapPixelError = 2; terrain.basemapDistance = 300;
            terrain.Flush(); Physics.SyncTransforms();
        }

        // All placement and camera height queries now use Unity Terrain's height data.
        public float Height(float x, float z)
        {
            Vector3 origin = terrain.transform.position;
            return data.GetInterpolatedHeight(Mathf.Clamp01((x - origin.x) / data.size.x),
                Mathf.Clamp01((z - origin.z) / data.size.z)) + origin.y;
        }

        public bool Raycast(Ray ray, out Vector3 point)
        {
            if (ground.Raycast(ray,out var hit,1000)) { point = hit.point; return true; }
            point = default; return false;
        }

        public void LotRange(Vector3 centre, out float low, out float high)
        {
            low = float.PositiveInfinity; high = float.NegativeInfinity;
            for (int z = 0; z <= 6; z++) for (int x = 0; x <= 6; x++)
            {
                float h = Height(centre.x - 1.5f + x * .5f,centre.z - 1.5f + z * .5f);
                low = Mathf.Min(low,h); high = Mathf.Max(high,h);
            }
        }

        public GameObject Surface(string objectName, Transform parent, Vector3 centre, float width, float depth, float lift, Material material)
        {
            const int segments = 6;
            var vertices = new Vector3[(segments + 1) * (segments + 1)];
            var triangles = new int[segments * segments * 6];
            for (int z = 0; z <= segments; z++) for (int x = 0; x <= segments; x++)
            {
                float px = centre.x + (x / (float)segments - .5f) * width;
                float pz = centre.z + (z / (float)segments - .5f) * depth;
                vertices[z * (segments + 1) + x] = parent.InverseTransformPoint(new Vector3(px,Height(px,pz) + lift,pz));
            }
            int t = 0;
            for (int z = 0; z < segments; z++) for (int x = 0; x < segments; x++)
            {
                int a = z * (segments + 1) + x, b = a + segments + 1;
                triangles[t++] = a; triangles[t++] = b; triangles[t++] = a + 1;
                triangles[t++] = a + 1; triangles[t++] = b; triangles[t++] = b + 1;
            }
            var mesh = new Mesh { name = objectName, vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var obj = new GameObject(objectName); obj.transform.SetParent(parent,false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial = material;
            obj.AddComponent<CityGeneratedMesh>(); return obj;
        }

        void OnDestroy()
        {
            if (ground != null) ground.terrainData = null;
            if (terrain != null) terrain.terrainData = null;
            foreach (var asset in ownedAssets) if (asset != null) Destroy(asset);
        }
    }

    public sealed class CityGeneratedMesh : MonoBehaviour
    {
        void OnDestroy()
        {
            var filter = GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null) Destroy(filter.sharedMesh);
        }
    }
}
