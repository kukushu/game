using System;
using System.Collections.Generic;
using UnityEngine;

namespace HarborCity
{
    // Read-only endpoint presentation. Never registers access or changes trade.
    internal sealed class HarborCityOutsideViews:IDisposable
    {
        sealed class Marker {internal GameObject root;internal LineRenderer ring,stem;internal Vector3 anchor;internal GUIContent label;internal bool import;internal Color colour;}
        readonly Dictionary<int,Marker> markers=new Dictionary<int,Marker>();
        readonly HashSet<int> present=new HashSet<int>();readonly List<int> removed=new List<int>();
        readonly Material imports,exports,inactive;readonly CityLocalization text;
        CityCoreIndustrySession bound;int revision=-1,network=-1;long access=-1;GUIStyle style;
        internal HarborCityOutsideViews(Material material,CityLocalization text)
        {this.text=text;imports=new Material(material){color=Color.cyan};exports=new Material(material){color=new Color(1,.45f,.08f)};inactive=new Material(material){color=Color.gray};}
        LineRenderer Line(GameObject parent,string name,bool loop)
        {var go=new GameObject(name);go.transform.SetParent(parent.transform,false);var line=go.AddComponent<LineRenderer>();line.loop=loop;line.useWorldSpace=true;line.startWidth=line.endWidth=.6f;return line;}
        internal void Sync(CityCoreIndustrySession session)
        {
            if(ReferenceEquals(bound,session)&&session!=null&&revision==session.Entities.Revision&&network==session.Transport.NetworkGeneration&&access==session.Transport.AccessRevision)return;
            bound=session;revision=session?.Entities.Revision??-1;network=session?.Transport.NetworkGeneration??-1;access=session?.Transport.AccessRevision??-1;present.Clear();
            if(session!=null)foreach(var port in session.City.trade.connections)
            {
                present.Add(port.id);if(!markers.TryGetValue(port.id,out var marker)){marker=new Marker{root=new GameObject("Outside endpoint #"+port.id)};marker.ring=Line(marker.root,"Endpoint ring",true);marker.stem=Line(marker.root,"Endpoint beacon",false);markers.Add(port.id,marker);}
                var entry=session.Transport.GetAccess(port.id);bool connected=entry!=null&&entry.valid;Vector3 point=new Vector3(port.x,port.y,port.z);
                if(connected){var p=session.Transport.Network.Get(entry.lane).Geometry.Sample(entry.distance);point=new Vector3((float)p.x,(float)p.y,(float)p.z);}
                marker.import=port.flow==OutsideFlow.Import;var material=port.active&&connected?(marker.import?imports:exports):inactive;marker.colour=material.color;
                marker.label=new GUIContent(text.Get(connected?(port.active?(marker.import?"ui.trade.marker_import":"ui.trade.marker_export"):(marker.import?"ui.trade.marker_import_off":"ui.trade.marker_export_off")):"ui.trade.marker_disconnected",port.id));
                marker.anchor=point+Vector3.up*12;marker.ring.sharedMaterial=marker.stem.sharedMaterial=material;marker.ring.positionCount=32;
                for(int i=0;i<32;i++){float angle=i*Mathf.PI*2/32;marker.ring.SetPosition(i,point+new Vector3(Mathf.Cos(angle)*3,1,Mathf.Sin(angle)*3));}
                marker.stem.positionCount=2;marker.stem.SetPosition(0,point+Vector3.up);marker.stem.SetPosition(1,marker.anchor);
            }
            removed.Clear();foreach(var pair in markers)if(!present.Contains(pair.Key))removed.Add(pair.Key);foreach(int id in removed){UnityEngine.Object.Destroy(markers[id].root);markers.Remove(id);}
        }
        internal void Draw(Camera camera,Rect menu)
        {
            if(camera==null||Event.current.type!=EventType.Repaint)return;
            if(style==null)style=new GUIStyle(GUI.skin.label){fontSize=16,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,normal={textColor=Color.white}};
            Color original=GUI.color;
            foreach(var marker in markers.Values)
            {
                var projected=camera.WorldToScreenPoint(marker.anchor);if(projected.z<=0||projected.x<0||projected.x>Screen.width||projected.y<0||projected.y>Screen.height)continue;
                float width=Mathf.Max(170,style.CalcSize(marker.label).x+20),x=Mathf.Clamp(projected.x+(marker.import?-width-8:8),4,Mathf.Max(4,Screen.width-width-4)),y=Mathf.Clamp(Screen.height-projected.y-16,4,Mathf.Max(4,Screen.height-36));
                var rect=new Rect(x,y,width,32);if(rect.Overlaps(menu))rect.y=menu.yMax+8+(marker.import?0:36);
                GUI.color=new Color(.03f,.06f,.08f,1);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=marker.colour;GUI.DrawTexture(new Rect(rect.x,rect.y,5,rect.height),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(rect,marker.label,style);
            }
            GUI.color=original;
        }
        public void Dispose(){foreach(var marker in markers.Values)UnityEngine.Object.Destroy(marker.root);markers.Clear();UnityEngine.Object.Destroy(imports);UnityEngine.Object.Destroy(exports);UnityEngine.Object.Destroy(inactive);}
    }
}
