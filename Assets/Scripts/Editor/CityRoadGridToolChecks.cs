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
            if(!session.Simulation.Finance.UnlimitedFunds)throw new Exception("Normal gameplay must start with temporary unlimited funds");
            if(!session.City.utilities.roadConduits)throw new Exception("Normal roads must carry utilities by default");
            if(session.City.buildings.Count(b=>b.Use==LandUse.Power)!=CityNewTownTemplate.FacilitiesPerService||session.City.water<=0)throw new Exception("Normal Play did not create functioning infrastructure template");
            CheckHud(game,session);
            Capture(game,"new-town-template",new Vector3(-148,2,0),2.8f);
            session.Simulation.Finance.UnlimitedFunds=false;
            var land=(CityLandscape)type.GetField("landscape",flags).GetValue(game);
            var tool=type.GetField("gridTool",flags).GetValue(game);var drag=tool.GetType().GetMethod("Drag",flags);
            string message="";Action<string> notice=value=>message=value;Action changed=()=>type.GetMethod("RenderRoads",flags).Invoke(game,null);
            Action<Point,bool,bool> send=(p,pressed,released)=>drag.Invoke(tool,new object[]{p,pressed,released,land,session,false,false,notice,changed});
            var before=CityCoreSave.Write(session);int cash=session.City.money,initialRoads=session.Transport.Network.RoadCount,initialJunctions=session.Transport.Network.JunctionCount;
            send(new Point(-32,2,-32),true,false);send(new Point(32,2,32),false,false);
            var lines=UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None).Where(l=>l.name=="Grid road preview").ToArray();
            if(lines.Length!=6||lines.Any(l=>!l.enabled||l.positionCount!=17))throw new Exception("Grid preview missing/incorrect");
            if(!before.SequenceEqual(CityCoreSave.Write(session)))throw new Exception("Preview changed simulation");
            Capture(game,"preview");send(new Point(32,2,32),false,true);
            // Six 64m centre lines, one lane per direction setting: 384 * (40/3) = 5120.
            if(session.Transport.Network.RoadCount!=initialRoads+12||session.Transport.Network.JunctionCount!=initialJunctions+9||session.City.money!=cash-5120||!message.Contains("2×2"))throw new Exception("Drag release did not build/pay once: "+message);
            if(lines.Any(l=>l.enabled))throw new Exception("Preview survived release");session.Validate();Capture(game,"built");
            var built=CityCoreSave.Write(session);
            send(new Point(64,2,64),true,false);send(new Point(96,2,96),false,false);
            tool.GetType().GetMethod("Cancel").Invoke(tool,null);
            send(new Point(96,2,96),false,true);
            if(!built.SequenceEqual(CityCoreSave.Write(session))||lines.Any(l=>l.enabled))throw new Exception("Cancel built roads");
            // A pointer slightly off the shared corner snaps to the real junction.
            send(new Point(32.4,2,-31.7),true,false);send(new Point(96.5,2,32.5),false,true);
            if(session.Transport.Network.RoadCount!=initialRoads+22||session.Transport.Network.JunctionCount!=initialJunctions+15||!message.Contains("2×2"))throw new Exception("Shared-border drag failed: "+message);
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
            var fullPreview=new CityRoadGrid(new Point(-384,2,-384),new Point(384,2,384),24,672,(x,z)=>2);
            var unchanged=CityCoreSave.Write(session);tool.GetType().GetMethod("Show",flags).Invoke(tool,new object[]{fullPreview,false});
            var fullLines=UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None).Where(l=>l.name=="Grid road preview"&&l.enabled).ToArray();
            if(fullLines.Length!=66||!unchanged.SequenceEqual(CityCoreSave.Write(session)))throw new Exception("32x32 preview missing lines or mutated city");
            tool.GetType().GetMethod("Cancel").Invoke(tool,null);if(fullLines.Any(l=>l.enabled))throw new Exception("32x32 preview cancellation failed");
            Debug.Log("PASS Unity grid drag handler, 32x32 pooled preview lines, preview read-only, 2x2 connected road mesh creation, one payment, release cleanup and cancellation. Programmatic handler test; physical mouse not tested.");
            Debug.Log("PASS snapped shared-border extension, no repeat payment, parallel-overlap rejection and persistent visible failure reasons.");
            var zoneTool=type.GetField("zoneBoxTool",flags).GetValue(game);var zoneDrag=zoneTool.GetType().GetMethod("Drag",flags);
            Action<Point,bool,bool,LandUse,double> zone=(point,pressed,released,use,time)=>zoneDrag.Invoke(zoneTool,new object[]{point,pressed,released,land,session,use,time,notice,(Action)(()=>type.GetMethod("RenderZones",flags).Invoke(game,null))});
            var low=new Point(-25,2,-25);var high=new Point(25,2,25);double time=0;
            foreach(var use in new[]{LandUse.Residential,LandUse.Commercial,LandUse.Industrial})
            {
                var unpainted=CityCoreSave.Write(session);zone(low,true,false,use,time++);zone(high,false,false,use,time++);
                var outline=UnityEngine.Object.FindObjectsByType<LineRenderer>().Single(l=>l.name=="Zoning rectangle selection");
                if(!outline.enabled||outline.positionCount!=64||!unpainted.SequenceEqual(CityCoreSave.Write(session)))throw new Exception("Zoning preview changed city or missing outline");
                if(use==LandUse.Residential)Capture(game,"zoning-box-preview");zone(high,false,true,use,time++);
                var chosen=session.Simulation.Growth.Zoning.Cells().Where(c=>c.pose.x>=-25&&c.pose.x<=25&&c.pose.z>=-25&&c.pose.z<=25).ToArray();
                if(outline.enabled||chosen.Length==0||chosen.Any(c=>c.use!=use)||!message.Contains("已修改"))throw new Exception("Box zoning release failed: "+message);
            }
            var painted=CityCoreSave.Write(session);zone(low,true,false,LandUse.Residential,time++);zone(high,false,false,LandUse.Residential,time++);
            zoneTool.GetType().GetMethod("Cancel").Invoke(zoneTool,null);zone(high,false,true,LandUse.Residential,time++);
            if(!painted.SequenceEqual(CityCoreSave.Write(session)))throw new Exception("Cancelled zoning changed facts");
            zone(low,true,false,LandUse.Bulldoze,time++);zone(high,false,true,LandUse.Bulldoze,time++);
            if(session.City.zoning.designations.Count!=0)throw new Exception("Rectangle clear left painted cells");
            painted=CityCoreSave.Write(session);zone(new Point(150,2,150),true,false,LandUse.Residential,time++);zone(new Point(160,2,160),false,true,LandUse.Residential,time++);
            if(!painted.SequenceEqual(CityCoreSave.Write(session))||!message.Contains("没有需要"))throw new Exception("Empty rectangle changed city or lacked feedback");
            Debug.Log("PASS Unity R/C/I rectangle outline and read-only preview, release painting, cancellation, rectangle erase and empty-selection feedback. Programmatic handler test; physical mouse not tested.");
        }
        static void CheckHud(HarborCityRoadCoreGame game,CityCoreIndustrySession session)
        {
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var type=game.GetType();var select=type.GetMethod("SelectPage",flags);var page=type.GetField("toolPage",flags);
            var before=CityCoreSave.Write(session);
            Func<string,Rect> rect=name=>(Rect)type.GetProperty(name,flags).GetValue(game);
            Func<Vector2,bool> blocked=point=>(bool)type.GetMethod("BlocksWorldInput",flags).Invoke(game,new object[]{new Vector2(point.x,Screen.height-point.y)});
            if(rect("ToolPanelBounds").width!=0||!blocked(rect("StatusBounds").center)||!blocked(rect("ToolbarBounds").center))throw new Exception("HUD initial visibility/input boundaries incorrect");
            // Compact corner cards and single bottom row block only their actual bounds.
            if(!blocked(rect("TimeBounds").center)||rect("ToolbarBounds").height!=40||rect("StatusBounds").x!=8||rect("TimeBounds").y!=8||rect("StatusBounds").Overlaps(rect("TimeBounds")))throw new Exception("New chrome leaks map input");
            if(Screen.width>=800&&Screen.height>=480&&blocked(new Vector2(24,96)))throw new Exception("Closed HUD still blocks old menu/map area");
            foreach(string name in new[]{"Roads","Zones","Utilities","Trade","City","Options"})
            {
                var value=Enum.Parse(page.FieldType,name);select.Invoke(game,new[]{value});
                if(rect("ToolPanelBounds").x!=8||rect("ToolPanelBounds").y!=96||rect("ToolPanelBounds").width>280||rect("ToolPanelBounds").height>256)throw new Exception("Tool panel is not compact and left-aligned");
                if(!blocked(rect("ToolPanelBounds").center))throw new Exception("Open category leaks map input: "+name);
                if(name=="Roads")type.GetField("building",flags).SetValue(game,true);
                select.Invoke(game,new[]{value});
                if(rect("ToolPanelBounds").width!=0)throw new Exception("Same category did not collapse");
                if(name=="Roads"&&!(bool)type.GetField("building",flags).GetValue(game))throw new Exception("Collapse cancelled the selected tool");
            }
            if((bool)type.GetField("building",flags).GetValue(game)||!before.SequenceEqual(CityCoreSave.Write(session)))throw new Exception("Category changes changed city or retained unrelated tool");
            var probe=ScriptableObject.CreateInstance<CityHudGuiProbe>();
            try
            {
                probe.Game=game;probe.ShowUtility();probe.position=new Rect(0,0,Screen.width,Screen.height);
                foreach(string name in new[]{"None","Roads","Zones","Utilities","Trade","City","Options"})
                {
                    page.SetValue(game,Enum.Parse(page.FieldType,name));
                    probe.SendEvent(new Event{type=EventType.Layout});probe.SendEvent(new Event{type=EventType.Repaint});
                    if(probe.Failure!=null)throw new Exception("HUD GUI rendering failed: "+name,probe.Failure);
                }
                page.SetValue(game,Enum.Parse(page.FieldType,"None"));
                probe.SendEvent(new Event{type=EventType.Layout});probe.SendEvent(new Event{type=EventType.Repaint});
                foreach(int index in new[]{1,4,1,4,2,0})
                {
                    var bounds=probe.SpeedBounds;float width=(bounds.width-94)/5;
                    var pointer=new Vector2(bounds.x+86+width*index+width/2,bounds.y+20)/EditorGUIUtility.pixelsPerPoint;
                    probe.Focus();probe.SendEvent(new Event{type=EventType.MouseDown,button=0,mousePosition=pointer});
                    probe.SendEvent(new Event{type=EventType.MouseUp,button=0,mousePosition=pointer});
                    float expectedRate=new[]{0f,1f,3f,20f,50f}[index];
                    if((float)type.GetField("speed",flags).GetValue(game)!=expectedRate)throw new Exception("Speed button failed: index="+index+" actual="+type.GetField("speed",flags).GetValue(game)+" bounds="+bounds+" pointer="+pointer+" window="+probe.position+" input="+probe.LastInput);
                }
                if(probe.Draws<14)throw new Exception("HUD GUI events were not rendered");
            }
            finally{probe.Close();page.SetValue(game,Enum.Parse(page.FieldType,"None"));}
            Debug.Log("PASS compact HUD: categories start collapsed, collapse retains tool, switching cancels unrelated tool, open UI blocks map input, old menu area freed, complete city state unchanged. Programmatic check, not physical mouse/UI rendering acceptance.");
        }
        static void Capture(HarborCityRoadCoreGame game,string name,Vector3? focus=null,float scale=1)
        {
            var folder=Environment.GetEnvironmentVariable("HARBOR_GRID_CHECK_IMAGES");if(string.IsNullOrEmpty(folder))return;
            Directory.CreateDirectory(folder);var camera=(Camera)game.GetType().GetField("cameraView",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(game);
            var targetPoint=focus??new Vector3(0,2,0);camera.transform.position=targetPoint+new Vector3(0,108,-85)*scale;camera.transform.LookAt(targetPoint);
            var target=new RenderTexture(800,600,24);var previous=RenderTexture.active;camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            var image=new Texture2D(800,600,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,800,600),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);
        }
    }
    // Exercise real IMGUI layout/repaint events in the isolated Editor, not camera-only captures.
    public sealed class CityHudGuiProbe:EditorWindow
    {
        public HarborCityRoadCoreGame Game;public Exception Failure;public int Draws;public Rect SpeedBounds;public string LastInput;
        void OnGUI()
        {
            if(Game==null)return;var priorMatrix=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(1/EditorGUIUtility.pixelsPerPoint,1/EditorGUIUtility.pixelsPerPoint,1));
            try{if(Event.current.isMouse)LastInput+=" BEFORE "+Event.current.type+" pos="+Event.current.mousePosition+" hot="+GUIUtility.hotControl+" enabled="+GUI.enabled;SpeedBounds=(Rect)Game.GetType().GetProperty("TimeBounds",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(Game);var skin=GUI.skin;var background=GUI.backgroundColor;var content=GUI.contentColor;Game.GetType().GetMethod("DrawInterface",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(Game,null);if(GUI.skin!=skin||GUI.backgroundColor!=background||GUI.contentColor!=content)throw new Exception("City theme leaked GUI state");if(Event.current.isMouse||Event.current.type==EventType.Used)LastInput+=" AFTER "+Event.current.type+" hot="+GUIUtility.hotControl;Draws++;}
            catch(Exception error){Failure=error;}finally{GUI.matrix=priorMatrix;}
        }
    }
}
