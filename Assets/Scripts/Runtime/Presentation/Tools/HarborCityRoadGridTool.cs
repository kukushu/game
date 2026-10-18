using System;
using System.Collections.Generic;
using HarborCity.RoadCore;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HarborCity
{
    // Owns only the drag and pooled preview renderers. No authoritative road data.
    internal sealed class HarborCityRoadGridTool:IDisposable
    {
        readonly List<LineRenderer> lines=new List<LineRenderer>();
        readonly Material material;readonly CityLocalization text;
        Point anchor,last;bool dragging,hasLast,lastOneWay,lastWide;int spacing=32,lastSpacing;
        CityRoadGrid preview;string hint;
        public bool Enabled;
        public HarborCityRoadGridTool(Material source,CityLocalization text){material=new Material(source){color=new Color(1,.75f,.15f)};this.text=text;}
        public void Cancel(){Enabled=false;Reset();}
        void Reset(){dragging=hasLast=false;preview=null;hint=null;Hide();}
        void Hide(){foreach(var line in lines)line.enabled=false;}
        public void DrawSettings()
        {
            GUILayout.BeginHorizontal();GUILayout.Label(text.Get("ui.grid.spacing"),GUILayout.Width(90));
            foreach(int value in new[]{24,32,48,64})if(GUILayout.Toggle(spacing==value,value+" m",GUI.skin.button,GUILayout.Width(65)))spacing=value;
            GUILayout.EndHorizontal();GUILayout.Label(hint??text.Get("ui.grid.help"));
        }
        public void Step(Camera camera,CityLandscape land,CityCoreIndustrySession session,bool blocked,bool oneWay,bool wide,Action<string> notice,Action changed)
        {
            var mouse=Mouse.current;if(mouse==null||!Application.isFocused){Reset();return;}
            if(blocked){Hide();hasLast=false;if(mouse.leftButton.wasReleasedThisFrame)Reset();return;}
            if(!land.Raycast(camera.ScreenPointToRay(mouse.position.ReadValue()),out var hit)){Hide();hasLast=false;preview=null;if(mouse.leftButton.wasReleasedThisFrame)Reset();return;}
            var point=new Point(hit.x,hit.y,hit.z);
            Drag(point,mouse.leftButton.wasPressedThisFrame,mouse.leftButton.wasReleasedThisFrame,land,session,oneWay,wide,notice,changed);
        }
        internal void Drag(Point point,bool pressed,bool released,CityLandscape land,CityCoreIndustrySession session,bool oneWay,bool wide,Action<string> notice,Action changed)
        {
            if(pressed){anchor=point;dragging=true;hasLast=false;}
            if(!dragging)return;
            if(!hasLast||Point.Distance(last,point)>.1||lastSpacing!=spacing||lastOneWay!=oneWay||lastWide!=wide)
            {
                last=point;hasLast=true;lastSpacing=spacing;lastOneWay=oneWay;lastWide=wide;preview=null;Hide();
                try{preview=new CityRoadGrid(anchor,point,spacing,session.City.map.buildHalfSize,land.Height,oneWay);hint=text.Get("ui.grid.preview",preview.Columns,preview.Rows,preview.Strokes.Length);Show(preview,wide);}
                catch(Exception error){hint=text.Error(error);}
            }
            if(!released)return;
            try
            {
                if(preview==null)throw new CityRuleException("ui.grid.too_small",spacing);
                var plan=session.Transport.PlanConnectedRoads(preview.Strokes,oneWay,snap:1,lanes:wide?2:1);
                session.Construction.CheckRoadPlacement(plan);
                if(!session.Construction.Commit(plan,out int paid))throw new CityRuleException("road.edit_stale");
                string success=text.Get("ui.grid.success",preview.Columns,preview.Rows,paid);notice(success);
                try{changed();}catch(Exception viewError){notice(success+text.Get("ui.build.view_error"));Debug.LogException(viewError);}
            }
            catch(Exception error){notice(text.Error(error));}
            finally{Reset();}
        }
        void Show(CityRoadGrid grid,bool wide)
        {
            while(lines.Count<grid.Strokes.Length){var line=new GameObject("Grid road preview").AddComponent<LineRenderer>();line.sharedMaterial=material;lines.Add(line);}
            for(int i=0;i<grid.Strokes.Length;i++){var line=lines[i];line.enabled=true;line.startWidth=line.endWidth=wide?4:2;line.positionCount=grid.Strokes[i].Length;for(int j=0;j<line.positionCount;j++){var p=grid.Strokes[i][j];line.SetPosition(j,new Vector3((float)p.x,(float)p.y+.24f,(float)p.z));}}
        }
        public void Dispose(){foreach(var line in lines)if(line!=null)UnityEngine.Object.Destroy(line.gameObject);UnityEngine.Object.Destroy(material);}
    }
}
