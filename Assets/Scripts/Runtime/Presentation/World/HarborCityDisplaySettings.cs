using System;
using UnityEngine;
namespace HarborCity
{
    [Serializable]
    public sealed class HarborCityDisplaySettings
    {
        public bool layered=true;
        public float vehicleNearEnter=12,vehicleNearExit=10,vehicleSummaryEnter=2,vehicleSummaryExit=3;
        public float buildingNearEnter=24,buildingNearExit=20,buildingSummaryEnter=4,buildingSummaryExit=6;
        public float zoneSummaryEnter=1,zoneSummaryExit=1.5f;
        public int creationsPerFrame=64;public double creationMilliseconds=1;
        public float edgePadding=8;
        public double buildingParentMilliseconds=1;
        public float densityCountScale=.3f,vehicleDensityMinRadius=1,vehicleDensityMaxFraction=.12f;
    }
    internal sealed class HarborCityDisplayCamera
    {
        readonly Plane[] planes=new Plane[6];Camera camera;
        internal void Update(Camera value){camera=value;GeometryUtility.CalculateFrustumPlanes(value,planes);}
        internal bool Visible(Bounds bounds)=>GeometryUtility.TestPlanesAABB(planes,bounds);
        internal float Pixels(Bounds bounds,float size)
        {
            if(camera.orthographic)return size*camera.pixelHeight/(2*camera.orthographicSize);
            float depth=Vector3.Dot(bounds.center-camera.transform.position,camera.transform.forward)-bounds.extents.magnitude;
            return depth<=camera.nearClipPlane?float.PositiveInfinity:size*camera.pixelHeight/(2*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f)*depth);
        }
    }
    internal static class HarborCityDisplayBudget
    {
        static int frame=-1,created;static double milliseconds;
        internal static bool Available(HarborCityDisplaySettings settings){if(frame!=Time.frameCount){frame=Time.frameCount;created=0;milliseconds=0;}return created<settings.creationsPerFrame&&milliseconds<settings.creationMilliseconds;}
        internal static void Charge(long start){created++;milliseconds+=(System.Diagnostics.Stopwatch.GetTimestamp()-start)*1000.0/System.Diagnostics.Stopwatch.Frequency;}
    }
    internal sealed class HarborCityDisplayBatch:IDisposable
    {
        internal readonly Mesh Cube;readonly Material[] materials;readonly Matrix4x4[][] matrices;readonly int[] counts;
        internal int Count {get;private set;}
        internal HarborCityDisplayBatch(Material[] materials)
        {
            this.materials=materials;matrices=new Matrix4x4[materials.Length][];counts=new int[materials.Length];
            for(int i=0;i<materials.Length;i++){materials[i].enableInstancing=true;matrices[i]=new Matrix4x4[1023];}
            var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);Cube=primitive.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.Destroy(primitive);
        }
        internal void Begin(){Count=0;Array.Clear(counts,0,counts.Length);}
        internal void Add(int material,Matrix4x4 matrix){if(counts[material]==1023)Flush(material);matrices[material][counts[material]++]=matrix;Count++;}
        void Flush(int i){if(counts[i]==0)return;Graphics.DrawMeshInstanced(Cube,0,materials[i],matrices[i],counts[i]);counts[i]=0;}
        internal void Draw(){for(int i=0;i<counts.Length;i++)Flush(i);}
        public void Dispose(){} // Unity owns the shared primitive mesh.
    }
}
