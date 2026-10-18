using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarborCity.RoadCore;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HarborCity
{
    public static class CityRoadGridToolChecks
    {
        // Exercises the same drag handler and renderers as the mouse adapter, not physical input.
        public static void Validate()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Use an isolated batch project");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var game=new GameObject("Grid tool check").AddComponent<HarborCityRoadCoreGame>();var type=game.GetType();
            type.GetMethod("Start",flags).Invoke(game,null);
            var session=(CityCoreIndustrySession)type.GetField("session",flags).GetValue(game);
            var land=(CityLandscape)type.GetField("landscape",flags).GetValue(game);
            var tool=type.GetField("gridTool",flags).GetValue(game);var drag=tool.GetType().GetMethod("Drag",flags);
            string message="";Action<string> notice=value=>message=value;Action changed=()=>type.GetMethod("RenderRoads",flags).Invoke(game,null);
            Action<Point,bool,bool> send=(p,pressed,released)=>drag.Invoke(tool,new object[]{p,pressed,released,land,session,false,false,notice,changed});
            var before=CityCoreSave.Write(session);int cash=session.City.money;
            send(new Point(-32,2,-32),true,false);send(new Point(32,2,32),false,false);
            var lines=UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None).Where(l=>l.name=="Grid road preview").ToArray();
            if(lines.Length!=6||lines.Any(l=>!l.enabled||l.positionCount!=17))throw new Exception("Grid preview missing/incorrect");
            if(!before.SequenceEqual(CityCoreSave.Write(session)))throw new Exception("Preview changed simulation");
            Capture(game,"preview");send(new Point(32,2,32),false,true);
            // Six 64m centre lines, one lane per direction setting: 384 * (40/3) = 5120.
            if(session.Transport.Network.RoadCount!=12||session.Transport.Network.JunctionCount!=9||session.City.money!=cash-5120||!message.Contains("2×2"))throw new Exception("Drag release did not build/pay once: "+message);
            if(lines.Any(l=>l.enabled))throw new Exception("Preview survived release");session.Validate();Capture(game,"built");
            var built=CityCoreSave.Write(session);
            send(new Point(64,2,64),true,false);send(new Point(96,2,96),false,false);
            tool.GetType().GetMethod("Cancel").Invoke(tool,null);
            send(new Point(96,2,96),false,true);
            if(!built.SequenceEqual(CityCoreSave.Write(session))||lines.Any(l=>l.enabled))throw new Exception("Cancel built roads");
            // A pointer slightly off the shared corner snaps to the real junction.
            send(new Point(32.4,2,-31.7),true,false);send(new Point(96.5,2,32.5),false,true);
            if(session.Transport.Network.RoadCount!=22||session.Transport.Network.JunctionCount!=15||!message.Contains("2×2"))throw new Exception("Shared-border drag failed: "+message);
            var extended=CityCoreSave.Write(session);
            send(new Point(32.4,2,-31.7),true,false);send(new Point(96.5,2,32.5),false,true);
            string feedback=(string)tool.GetType().GetField("feedback",flags).GetValue(tool);
            if(!extended.SequenceEqual(CityCoreSave.Write(session))||!feedback.Contains("无需重复"))throw new Exception("Repeat drag not reported without charge");
            send(new Point(-30.5,2,-20),true,false);send(new Point(33.5,2,44),false,true);
            feedback=(string)tool.GetType().GetField("feedback",flags).GetValue(tool);
            if(!extended.SequenceEqual(CityCoreSave.Write(session))||!feedback.Contains("重叠"))throw new Exception("Near-parallel drag did not visibly reject: "+feedback);
            send(new Point(-100,2,-100),true,false);send(new Point(-99,2,-99),false,true);
            feedback=(string)tool.GetType().GetField("feedback",flags).GetValue(tool);
            if(!extended.SequenceEqual(CityCoreSave.Write(session))||!feedback.Contains("至少"))throw new Exception("Small drag missing visible reason");
            Debug.Log("PASS Unity grid drag handler, six pooled preview lines, preview read-only, 2x2 connected road mesh creation, one payment, release cleanup and cancellation. Programmatic handler test; physical mouse not tested.");
            Debug.Log("PASS snapped shared-border extension, no repeat payment, parallel-overlap rejection and persistent visible failure reasons.");
        }
        static void Capture(HarborCityRoadCoreGame game,string name)
        {
            var folder=Environment.GetEnvironmentVariable("HARBOR_GRID_CHECK_IMAGES");if(string.IsNullOrEmpty(folder))return;
            Directory.CreateDirectory(folder);var camera=(Camera)game.GetType().GetField("cameraView",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(game);
            camera.transform.position=new Vector3(0,110,-85);camera.transform.LookAt(new Vector3(0,2,0));
            var target=new RenderTexture(800,600,24);var previous=RenderTexture.active;camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            var image=new Texture2D(800,600,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,800,600),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
