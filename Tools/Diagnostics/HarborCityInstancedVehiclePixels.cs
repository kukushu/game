using System;using System.IO;using UnityEngine;
namespace HarborCity {
// Copy into an isolated diagnostic project; never installed in the normal game.
public static class HarborCityInstancedVehiclePixels {
#if UNITY_EDITOR
public static void Run(){try{Check(Environment.GetEnvironmentVariable("HARBOR_PIXEL_RESULTS"));UnityEditor.EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);UnityEditor.EditorApplication.Exit(1);}}
#endif
[RuntimeInitializeOnLoadMethod]
static void Start(){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-harbor-instanced-vehicle-pixels");if(at<0)return;try{Check(args[at+1]);Application.Quit(0);}catch(Exception e){Debug.LogException(e);Application.Quit(1);}}
        // Check actual shader output, independently of CPU representation counters.
        public static void Check(string output)
        {
            var cameraObject=new GameObject("Instanced vehicle pixel oracle");var camera=cameraObject.AddComponent<Camera>();
            var target=new RenderTexture(256,256,24);var pixels=new Texture2D(256,256,TextureFormat.RGB24,false);
            var material=new Material(Resources.Load<Shader>("RoadCore")){color=Color.yellow};var type=typeof(HarborCityRoadCoreGame).Assembly.GetType("HarborCity.HarborCityDisplayBatch");var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var batch=(IDisposable)Activator.CreateInstance(type,flags,null,new object[]{new[]{material}},null);
            var old=RenderTexture.active;
            try{
                camera.enabled=false;camera.targetTexture=target;camera.orthographic=true;camera.orthographicSize=8;camera.aspect=1;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
                var center=new Vector3(20000,0,20000);camera.transform.position=center+Vector3.up*30;camera.transform.rotation=Quaternion.LookRotation(Vector3.down,Vector3.forward);
                camera.nearClipPlane=.1f;camera.farClipPlane=50;
                var points=new[]{center+new Vector3(-4,0,-2),center+new Vector3(3,0,3),center+new Vector3(0,0,-5)};
                type.GetMethod("Begin",flags).Invoke(batch,null);foreach(var point in points)type.GetMethod("Add",flags).Invoke(batch,new object[]{0,Matrix4x4.TRS(point,Quaternion.Euler(0,25,0),new Vector3(1.2f,.5f,2))});type.GetMethod("Draw",flags).Invoke(batch,null);camera.Render();
                RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,256,256),0,0);pixels.Apply();
                Directory.CreateDirectory(output);File.WriteAllBytes(Path.Combine(output,"InstancedVehiclePixels.png"),pixels.EncodeToPNG());
                foreach(var point in points){var p=camera.WorldToScreenPoint(point);var c=pixels.GetPixel(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.y));if(c.r<.8f||c.g<.8f||c.b>.2f)throw new Exception("Instanced vehicle missing at expected world position "+point+"; pixel="+c);}
                var empty=camera.WorldToScreenPoint(center);if(pixels.GetPixel(Mathf.RoundToInt(empty.x),Mathf.RoundToInt(empty.y)).maxColorComponent>.1f)throw new Exception("Instanced vehicles collapsed at shared origin");
                File.WriteAllText(Path.Combine(output,"InstancedVehiclePixels.txt"),"PASS three asymmetric instance transforms produce yellow pixels at exact projected world positions; empty origin remains black.\n");
            }finally{RenderTexture.active=old;camera.targetTexture=null;batch.Dispose();Release(cameraObject);Release(material);Release(pixels);target.Release();Release(target);}
        }
        static void Release(UnityEngine.Object value){if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}
}}
