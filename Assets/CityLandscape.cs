using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HarborCity
{
    // Fixed landscape seed/shape: older city saves keep exactly the same lot coordinates.
    public sealed class CityLandscape : MonoBehaviour
    {
        const int Resolution = 241;
        const float HalfSize = 120f;
        readonly float[,] heights = new float[Resolution, Resolution];
        MeshCollider ground;
        readonly List<Material> materials = new List<Material>();
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
            var vertices = new Vector3[Resolution * Resolution];
            var triangles = new List<int>[4];
            for (int i = 0; i < 4; i++) triangles[i] = new List<int>();
            for (int z = 0; z < Resolution; z++) for (int x = 0; x < Resolution; x++)
            {
                float y = Elevation(x - HalfSize, z - HalfSize);
                heights[x,z] = y;
                vertices[z * Resolution + x] = new Vector3(x - HalfSize,y,z - HalfSize);
            }
            for (int z = 0; z < Resolution - 1; z++) for (int x = 0; x < Resolution - 1; x++)
            {
                int a = z * Resolution + x, b = a + Resolution;
                AddTriangle(vertices,triangles,a,b,a + 1);
                AddTriangle(vertices,triangles,a + 1,b,b + 1);
            }
            var mesh = new Mesh { name = "Harbor hills and coastline", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = vertices; mesh.subMeshCount = 4;
            for (int i = 0; i < 4; i++) mesh.SetTriangles(triangles[i],i);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            gameObject.AddComponent<CityGeneratedMesh>();
            var colors = new[] { new Color(.39f,.54f,.32f), new Color(.30f,.43f,.28f),
                new Color(.49f,.50f,.45f), new Color(.75f,.70f,.52f) };
            foreach (Color color in colors)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = color; mat.SetFloat("_Smoothness",.05f); materials.Add(mat);
            }
            gameObject.AddComponent<MeshRenderer>().sharedMaterials = materials.ToArray();
            ground = gameObject.AddComponent<MeshCollider>(); ground.sharedMesh = mesh;
        }

        static void AddTriangle(Vector3[] vertices, List<int>[] groups, int a, int b, int c)
        {
            float height = (vertices[a].y + vertices[b].y + vertices[c].y) / 3;
            float slope = Vector3.Angle(Vector3.Cross(vertices[b] - vertices[a],vertices[c] - vertices[a]),Vector3.up);
            int material = height < 1.3f ? 3 : slope > 34 || height > 23 ? 2 : height > 10 ? 1 : 0;
            groups[material].Add(a); groups[material].Add(b); groups[material].Add(c);
        }

        // Match the mesh's two triangles per square, rather than sampling a different surface.
        public float Height(float x, float z)
        {
            float gx = Mathf.Clamp(x + HalfSize,0,Resolution - 1.001f);
            float gz = Mathf.Clamp(z + HalfSize,0,Resolution - 1.001f);
            int ix = Mathf.FloorToInt(gx), iz = Mathf.FloorToInt(gz);
            float u = gx - ix, v = gz - iz;
            if (u + v <= 1) return heights[ix,iz] * (1 - u - v) + heights[ix + 1,iz] * u + heights[ix,iz + 1] * v;
            return heights[ix + 1,iz + 1] * (u + v - 1) + heights[ix,iz + 1] * (1 - u) + heights[ix + 1,iz] * (1 - v);
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

        void OnDestroy() { foreach (var material in materials) Destroy(material); }
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
