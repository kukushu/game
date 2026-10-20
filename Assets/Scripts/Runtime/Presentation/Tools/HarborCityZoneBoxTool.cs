using System;
using System.Collections.Generic;
using HarborCity.RoadCore;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HarborCity
{
    // A transient selection of existing cells. CityCoreZoning.Paint remains the only writer.
    internal sealed class HarborCityZoneBoxTool:IDisposable
    {
        readonly CityLocalization text;readonly Material material;readonly LineRenderer outline;
        readonly List<ZoneCell> selection=new List<ZoneCell>();
        CityCoreIndustrySession bound;Point anchor,last;bool dragging;LandUse dragUse;
        double nextPreview;string message;public bool Box=true;
        public HarborCityZoneBoxTool(Material source,CityLocalization text)
        {
            this.text=text;material=new Material(source);outline=new GameObject("Zoning rectangle selection").AddComponent<LineRenderer>();
            outline.sharedMaterial=material;outline.loop=true;outline.positionCount=64;outline.startWidth=outline.endWidth=.25f;outline.enabled=false;
        }
        public void Cancel(){dragging=false;bound=null;selection.Clear();outline.enabled=false;message=null;}
        public void DrawControls()
        {
            GUILayout.Label(text.Get("ui.zone.selection"));GUILayout.BeginHorizontal();
            if(GUILayout.Toggle(Box,text.Get("ui.zone.box"),GUI.skin.button,GUILayout.Width(100))&&!Box){Cancel();Box=true;}
            if(GUILayout.Toggle(!Box,text.Get("ui.zone.brush"),GUI.skin.button,GUILayout.Width(100))&&Box){Cancel();Box=false;}
            GUILayout.EndHorizontal();
            GUILayout.Label(message??text.Get(Box?"ui.zone.box_help":"ui.zone.brush_help"));
        }
        public void Step(Camera camera,CityLandscape land,CityCoreIndustrySession session,LandUse use,bool blocked,Action<string> notice,Action changed)
        {
            var mouse=Mouse.current;if(mouse==null||!Application.isFocused){Cancel();return;}
            if(blocked||!land.Raycast(camera.ScreenPointToRay(mouse.position.ReadValue()),out var hit))
            {
                outline.enabled=false;if(dragging&&mouse.leftButton.wasReleasedThisFrame){Cancel();message=text.Get("ui.zone.cancelled");notice(message);}return;
            }
            Drag(new Point(hit.x,hit.y,hit.z),mouse.leftButton.wasPressedThisFrame,mouse.leftButton.wasReleasedThisFrame,land,session,use,Time.realtimeSinceStartupAsDouble,notice,changed);
        }
        internal void Drag(Point point,bool pressed,bool released,CityLandscape land,CityCoreIndustrySession session,LandUse use,double now,Action<string> notice,Action changed)
        {
            if(pressed){Cancel();anchor=point;dragging=true;bound=session;dragUse=use;nextPreview=0;}
            if(!dragging)return;if(!ReferenceEquals(bound,session)||dragUse!=use){Cancel();return;}
            float limit=session.City.map.buildHalfSize;
            point.x=Math.Max(-limit,Math.Min(limit,point.x));point.z=Math.Max(-limit,Math.Min(limit,point.z));
            if(pressed){anchor=point;last=point;}
            material.color=use==LandUse.Residential?new Color(.3f,1,.4f):use==LandUse.Commercial?new Color(.2f,.7f,1):use==LandUse.Industrial?new Color(1,.75f,.15f):Color.white;
            Show(point,land);
            // Only scan cached cells at most 10 times/s while dragging; no mesh or city rebuild.
            if(released||now>=nextPreview&&(pressed||Point.Distance(last,point)>.25))
            {Select(session,point);message=text.Get("ui.zone.preview",selection.Count);last=point;nextPreview=now+.1;}
            if(!released)return;
            try
            {
                // Re-select current authoritative cells immediately before Paint, after normal simulation.
                Select(session,point);int count=session.Simulation.Growth.Zoning.Paint(selection,use==LandUse.Bulldoze?LandUse.Empty:use);
                message=text.Get(count==0?"ui.zone.no_change":"ui.zone.painted",count);notice(message);
                try{changed();}catch(Exception error){Debug.LogException(error);}
            }
            catch(Exception error){message=text.Error(error);notice(message);}
            finally{dragging=false;bound=null;selection.Clear();outline.enabled=false;}
        }
        void Select(CityCoreIndustrySession session,Point point)
        {
            var zoning=session.Simulation.Growth.Zoning;
            if(Math.Abs(point.x-anchor.x)<1&&Math.Abs(point.z-anchor.z)<1)
            {selection.Clear();foreach(var cell in zoning.Cells())if((cell.pose.x-point.x)*(cell.pose.x-point.x)+(cell.pose.z-point.z)*(cell.pose.z-point.z)<=9)selection.Add(cell);}
            else zoning.SelectRectangle((float)anchor.x,(float)anchor.z,(float)point.x,(float)point.z,selection);
        }
        void Show(Point point,CityLandscape land)
        {
            outline.enabled=true;
            for(int edge=0;edge<4;edge++)for(int i=0;i<16;i++)
            {double t=i/16.0;double x=edge==0?anchor.x+(point.x-anchor.x)*t:edge==1?point.x:edge==2?point.x+(anchor.x-point.x)*t:anchor.x;
                double z=edge==0?anchor.z:edge==1?anchor.z+(point.z-anchor.z)*t:edge==2?point.z:point.z+(anchor.z-point.z)*t;
                outline.SetPosition(edge*16+i,new Vector3((float)x,land.Height((float)x,(float)z)+.28f,(float)z));}
        }
        public void Dispose(){UnityEngine.Object.Destroy(outline.gameObject);UnityEngine.Object.Destroy(material);}
    }
}
