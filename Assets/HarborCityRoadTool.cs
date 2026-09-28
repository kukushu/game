using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HarborCity
{
    public sealed partial class HarborCityGame
    {
        CityRoadView roadView;
        LineRenderer roadPreview;
        RoadNode roadStart;
        RoadPlan roadPlan;
        readonly List<(int stroke,int cost)> roadUndo = new List<(int,int)>();
        Vector2 plannedPointer = new Vector2(float.MaxValue,float.MaxValue);

        void InitializeRoads()
        {
            city.EnableRoads(landscape.Height);
            roadView=GetComponentInChildren<CityRoadView>();
            if(roadView==null)
            {
                var obj=new GameObject("Road network"); obj.transform.SetParent(transform,false);
                roadView=obj.AddComponent<CityRoadView>();
            }
            if(roadPreview==null)
            {
                roadPreview=new GameObject("Road construction preview").AddComponent<LineRenderer>();
                roadPreview.transform.SetParent(transform,false); roadPreview.useWorldSpace=true;
                roadPreview.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                roadPreview.receiveShadows=false; roadPreview.numCapVertices=4;
            }
            CancelRoad();
        }
        bool CancelRoad()
        {
            bool drawing=roadStart!=null; roadStart=null; roadPlan=null;
            if(roadPreview!=null) roadPreview.positionCount=0;
            return drawing;
        }
        void RefreshRoadView()
        {
            if(roadView!=null) roadView.Rebuild(city,landscape,Mat(palette[1]),Mat(new Color(.47f,.48f,.43f)),Mat(new Color(.82f,.8f,.63f)));
        }
        bool HandleRoadInput(Mouse mouse,Ray ray,bool blocked)
        {
            if(selected!=LandUse.Road && selected!=LandUse.Bulldoze) { CancelRoad(); return false; }
            roadPreview.positionCount=0;
            if(blocked || !landscape.Raycast(ray,out var hit)) return selected==LandUse.Road;
            if(selected==LandUse.Bulldoze)
            {
                roadStart=null; roadPlan=null;
                var picked=city.roads.Pick(hit.x,hit.z);
                if(picked==null) return false;
                var run=city.roads.Run(picked.id); var points=new List<Vector3>();
                foreach(int id in run)
                {
                    var edge=city.roads.edges.Find(e=>e.id==id);
                    points.Add(CityRoadView.Point(city.roads.Node(edge.a))+Vector3.up*.2f);
                    points.Add(CityRoadView.Point(city.roads.Node(edge.b))+Vector3.up*.2f);
                }
                roadPreview.sharedMaterial=Mat(palette[8]); roadPreview.startWidth=roadPreview.endWidth=CityRoads.Width;
                // Each run is straight; project its extrema to avoid drawing a zigzag preview.
                if(points.Count>0)
                {
                    Vector3 direction=(points[1]-points[0]).normalized;
                    points.Sort((a,b)=>Vector3.Dot(a,direction).CompareTo(Vector3.Dot(b,direction)));
                }
                roadPreview.positionCount=points.Count; roadPreview.SetPositions(points.ToArray());
                if(mouse.leftButton.wasPressedThisFrame)
                {
                    if(run.Exists(id=> { var e=city.roads.edges.Find(r=>r.id==id); return e.a==CityRoads.Entrance || e.b==CityRoads.Entrance; }))
                        notice="西侧入口的连接路段需要保留。";
                    else if(city.money<40) notice="拆路资金不足。";
                    else
                    {
                        city.roads.Remove(run); city.money-=40; city.Recalculate(); RefreshRoadView(); roadUndo.Clear();
                        notice="已拆除这段道路；车辆会重新寻路或等待道路恢复。";
                    }
                }
                return true;
            }
            var end=city.roads.Snap(hit.x,hit.z,landscape.Height);
            if(roadStart==null)
            {
                roadPreview.sharedMaterial=Mat(palette[2]); roadPreview.startWidth=roadPreview.endWidth=.6f;
                var p=CityRoadView.Point(end)+Vector3.up*.25f;
                roadPreview.positionCount=2; roadPreview.SetPositions(new[]{p,p+Vector3.right*.03f});
                if(mouse.leftButton.wasPressedThisFrame)
                { roadStart=end; roadPlan=null; plannedPointer=new Vector2(float.MaxValue,float.MaxValue); notice="移动鼠标预览道路，再点击终点；右键撤回起点。"; }
                return true;
            }
            var pointer=new Vector2(end.x,end.z);
            if(roadPlan==null || Vector2.Distance(pointer,plannedPointer)>.05f || roadPlan.revision!=city.roads.revision || mouse.leftButton.wasPressedThisFrame)
            { roadPlan=city.roads.Plan(city,roadStart,end,landscape.Height); plannedPointer=pointer; }
            roadPreview.sharedMaterial=Mat(roadPlan.Valid ? palette[2] : palette[8]); roadPreview.startWidth=roadPreview.endWidth=CityRoads.Width;
            int count=Mathf.Max(2,Mathf.CeilToInt(roadPlan.length/.5f)+1); roadPreview.positionCount=count;
            for(int i=0;i<count;i++)
            {
                var p=CityRoads.Lerp(roadStart,end,(float)i/(count-1));
                roadPreview.SetPosition(i,new Vector3(p.x,landscape.Height(p.x,p.z)+.22f,p.z));
            }
            if(mouse.leftButton.wasPressedThisFrame)
            {
                if(!roadPlan.Valid) notice=roadPlan.error;
                else if(city.CommitRoad(roadPlan))
                {
                    roadUndo.Add((roadPlan.stroke,roadPlan.cost)); RefreshRoadView();
                    notice="道路已建成，花费 ¥ "+roadPlan.cost+"。可以继续点击延伸，右键结束；Ctrl+Z 撤销上一笔。";
                    roadStart=end; roadPlan=null; plannedPointer=new Vector2(float.MaxValue,float.MaxValue);
                }
            }
            return true;
        }
        void UndoRoad()
        {
            CancelRoad();
            if(roadUndo.Count==0) { notice="没有可撤销的道路施工（读取城市或拆路后重置）。"; return; }
            var edit=roadUndo[roadUndo.Count-1]; roadUndo.RemoveAt(roadUndo.Count-1);
            var ids=new List<int>(); foreach(var e in city.roads.edges) if(e.stroke==edit.stroke) ids.Add(e.id);
            city.roads.Remove(ids); city.money+=edit.cost; city.Recalculate(); RefreshRoadView();
            notice="已撤销上一笔道路并退还 ¥ "+edit.cost+"；原有路口连接保留。";
        }
        void RoadToolPanel(Color background)
        {
            Panel(new Rect(24,120,310,258),background);
            GUI.Label(new Rect(40,134,278,32),"自由道路 · 双向两车道",label);
            string details=roadStart==null ? "点击起点，再点击终点。\n端点与已有道路会自动吸附。" : roadPlan==null ? "移动鼠标选择下一段终点。"
                : "长度 "+roadPlan.length.ToString("F1")+" 米   费用 ¥ "+roadPlan.cost+"\n最大坡度 "+(roadPlan.grade*100).ToString("F1")+"%\n"+(roadPlan.Valid ? "可以建设 · 交叉处自动形成路口" : roadPlan.error);
            GUI.Label(new Rect(40,178,278,110),details,small);
            GUI.Label(new Rect(40,284,278,44),"右键 / Esc 撤回起点，再按退出\nCtrl+Z 撤销上一笔道路",small);
            if(GUI.Button(new Rect(40,337,278,27),"撤销上一笔道路",button)) UndoRoad();
        }
        Vector3 RoadPosition(int id)
        {
            if(city.roads==null) return Position(id%36,id/36);
            return CityRoadView.Point(city.roads.Node(id));
        }
    }
}
