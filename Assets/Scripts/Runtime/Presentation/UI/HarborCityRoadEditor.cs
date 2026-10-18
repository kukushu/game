using System;
using UnityEngine;
using HarborCity.RoadCore;
namespace HarborCity
{
    // Tool state and highlighting are presentation only. A fresh construction
    // transaction is prepared by the domain command at the moment of application.
    public sealed class HarborCityRoadEditor:IDisposable
    {
        readonly CityLocalization text;readonly Material source;
        CityCoreIndustrySession session;int selected,drawn;Network drawnNetwork;
        LineRenderer line;Material lineMaterial;string notice="";
        public bool IsSelecting {get;private set;}
        public int Selected=>selected;
        public Action Changed;
        Rect Bounds=>new Rect(Math.Max(8,Screen.width-380),148,Math.Min(372,Screen.width-16),Math.Max(60,Screen.height-258));
        public HarborCityRoadEditor(CityLocalization text,Material source){this.text=text;this.source=source;}
        public void Bind(CityCoreIndustrySession current){if(ReferenceEquals(session,current))return;session=current;Clear();}
        public void Begin(){Clear();IsSelecting=session!=null;}
        public void Clear(){selected=drawn=0;IsSelecting=false;notice="";drawnNetwork=null;if(line!=null)line.enabled=false;}
        public bool BlocksInput(Vector2 point)=>(IsSelecting||selected!=0)&&Bounds.Contains(new Vector2(point.x,Screen.height-point.y));
        public void Pick(Point point){if(session==null)return;selected=session.Construction.PickRoad(point);notice=selected==0?text.Get("ui.road.no_hit"):"";Sync();}
        public void Sync()
        {
            if(session==null)return;if(selected!=0&&!session.Transport.Network.ContainsRoad(selected)){selected=0;notice=text.Get("road.edit_missing");}
            if(selected==0){drawn=0;drawnNetwork=null;if(line!=null)line.enabled=false;return;}
            var network=session.Transport.Network;if(drawn==selected&&ReferenceEquals(drawnNetwork,network))return;
            if(line==null){line=new GameObject("Selected logical road").AddComponent<LineRenderer>();lineMaterial=new Material(source){color=Color.yellow};line.sharedMaterial=lineMaterial;line.startWidth=line.endWidth=.3f;}
            var geometry=network.Centreline(selected);line.positionCount=geometry.Count;for(int i=0;i<geometry.Count;i++){var p=geometry[i];line.SetPosition(i,new Vector3((float)p.x,(float)p.y+.2f,(float)p.z));}line.enabled=true;drawn=selected;drawnNetwork=network;
        }
        enum Command {Narrow,Wide,ToggleDirection,Reverse,Remove}
        public bool Upgrade(int lanes){if(lanes!=1&&lanes!=2)throw new ArgumentOutOfRangeException(nameof(lanes));return Apply(lanes==1?Command.Narrow:Command.Wide);}
        public bool ToggleDirection()=>Apply(Command.ToggleDirection);
        public bool Reverse()=>Apply(Command.Reverse);
        public bool Remove()=>Apply(Command.Remove);
        bool Apply(Command command)
        {
            Network before=null;
            try
            {
                if(session==null||selected==0||!session.Transport.Network.ContainsRoad(selected))throw new CityRuleException("road.edit_missing");
                before=session.Transport.Network;var settings=before.Settings(selected);int paid;
                if(command==Command.Remove){session.Construction.RemoveRoad(selected);selected=0;notice=text.Get("ui.road.removed");}
                else
                {
                    int lanes=command==Command.Narrow?1:command==Command.Wide?2:settings.lanes;var direction=settings.direction;
                    if(command==Command.ToggleDirection)direction=direction==RoadDirection.Both?RoadDirection.Forward:RoadDirection.Both;
                    if(command==Command.Reverse){if(direction==RoadDirection.Both)throw new CityRuleException("road.reverse_oneway");direction=direction==RoadDirection.Forward?RoadDirection.Backward:RoadDirection.Forward;}
                    paid=session.Construction.UpgradeRoad(selected,lanes,direction);notice=text.Get("ui.road.changed",paid);
                }
            }
            catch(Exception e){notice=text.Error(e);return false;}
            if(!ReferenceEquals(before,session.Transport.Network))Changed?.Invoke();Sync();return true;
        }
        public void Draw()
        {
            if(session==null||!IsSelecting&&selected==0)return;Sync();var color=GUI.color;var content=GUI.contentColor;GUI.color=new Color(.07f,.1f,.12f,1);GUI.DrawTexture(Bounds,Texture2D.whiteTexture);GUI.color=color;GUI.contentColor=Color.white;
            GUILayout.BeginArea(Bounds,GUI.skin.box);if(GUILayout.Button(text.Get("ui.road.close"))){Clear();GUILayout.EndArea();GUI.contentColor=content;return;}
            GUILayout.Label(text.Get("ui.road.choose"));
            if(selected!=0)
            {
                var s=session.Transport.Network.Settings(selected);GUILayout.Label(text.Get("ui.road.summary",selected,session.Transport.Network.Centreline(selected).Length.ToString("F1"),s.lanes,text.Get("ui.road.direction."+s.direction.ToString().ToLowerInvariant())));
                if(GUILayout.Button(text.Get("ui.road.narrow")))Upgrade(1);if(GUILayout.Button(text.Get("ui.road.wide")))Upgrade(2);
                if(GUILayout.Button(text.Get("ui.road.toggle")))ToggleDirection();
                if(s.direction!=RoadDirection.Both&&GUILayout.Button(text.Get("ui.road.reverse")))Reverse();
                if(GUILayout.Button(text.Get("ui.road.remove")))Remove();
            }
            GUILayout.Label(notice);GUILayout.EndArea();GUI.contentColor=content;
        }
        public void Dispose(){if(line!=null)UnityEngine.Object.Destroy(line.gameObject);if(lineMaterial!=null)UnityEngine.Object.Destroy(lineMaterial);}
    }
}
