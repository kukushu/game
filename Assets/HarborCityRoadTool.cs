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
        RoadNode curveEnd;
        bool curvedRoad;
        RoadPlan roadPlan;
        readonly List<(int stroke,int cost)> roadUndo = new List<(int,int)>();
        Vector2 plannedPointer = new Vector2(float.MaxValue,float.MaxValue);

        void InitializeRoads()
        {
            city.SetEntranceHeight(landscape.Height);
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
            bool drawing=roadStart!=null; roadStart=null; curveEnd=null; roadPlan=null;
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
                roadStart=null; curveEnd=null; roadPlan=null;
                var picked=city.roads.Pick(hit.x,hit.z);
                if(picked==null) return false;
                var run=city.roads.Run(picked.id); var points=new List<Vector3>();
                var runEdges=city.roads.edges.FindAll(e=>run.Contains(e.id));
                var degree=new Dictionary<int,int>();
                foreach(var edge in runEdges)
                {
                    foreach(int id in new[]{edge.a,edge.b}) {if(!degree.ContainsKey(id)) degree[id]=0; degree[id]++;}
                }
                int current=runEdges[0].a;
                foreach(var pair in degree) if(pair.Value==1) {current=pair.Key; break;}
                points.Add(CityRoadView.Point(city.roads.Node(current))+Vector3.up*.2f);
                while(runEdges.Count>0)
                {
                    var edge=runEdges.Find(e=>e.a==current || e.b==current); if(edge==null) break;
                    current=edge.a==current?edge.b:edge.a; runEdges.Remove(edge);
                    points.Add(CityRoadView.Point(city.roads.Node(current))+Vector3.up*.2f);
                }
                roadPreview.sharedMaterial=Mat(palette[8]); roadPreview.startWidth=roadPreview.endWidth=CityRoads.Width;
                roadPreview.positionCount=points.Count; roadPreview.SetPositions(points.ToArray());
                if(mouse.leftButton.wasPressedThisFrame)
                {
                    if(run.Exists(id=> { var e=city.roads.edges.Find(r=>r.id==id); return e.a==CityRoads.Entrance || e.b==CityRoads.Entrance; }))
                        notice="西侧入口的连接路段需要保留。";
                    else if(city.money<40) notice="拆路资金不足。";
                    else
                    {
                        city.roads.Remove(run); city.money-=40; city.Recalculate(); RefreshRoadView(); roadUndo.Clear();
                        city.Trace("road.demolished","拆除道路",new CityLogDetail {ids=run,amount=40});
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
                { roadStart=end; roadPlan=null; plannedPointer=new Vector2(float.MaxValue,float.MaxValue); notice=curvedRoad?"点击弯道终点，然后移动鼠标调整弧度。":"移动鼠标预览道路，再点击终点；右键撤回起点。"; }
                return true;
            }
            // The curvature handle remains free; only road endpoints snap to roads.
            if(curvedRoad && curveEnd!=null) end=new RoadNode {x=hit.x,z=hit.z,y=landscape.Height(hit.x,hit.z)};
            var pointer=new Vector2(end.x,end.z);
            if(roadPlan==null || Vector2.Distance(pointer,plannedPointer)>.05f || roadPlan.revision!=city.roads.revision || mouse.leftButton.wasPressedThisFrame)
            { roadPlan=curvedRoad && curveEnd!=null?city.roads.PlanCurve(city,roadStart,end,curveEnd,landscape.Height):city.roads.Plan(city,roadStart,end,landscape.Height); plannedPointer=pointer; }
            roadPreview.sharedMaterial=Mat(roadPlan.Valid ? palette[2] : palette[8]); roadPreview.startWidth=roadPreview.endWidth=CityRoads.Width;
            var preview=new List<Vector3>();
            for(int k=1;k<roadPlan.points.Count;k++)
            {
                var a=roadPlan.points[k-1]; var b=roadPlan.points[k]; int steps=Mathf.Max(1,Mathf.CeilToInt(CityRoads.Length(a,b)/.5f));
                for(int i=k==1?0:1;i<=steps;i++)
                {var p=CityRoads.Lerp(a,b,(float)i/steps); preview.Add(new Vector3(p.x,landscape.Height(p.x,p.z)+.22f,p.z));}
            }
            roadPreview.positionCount=preview.Count; roadPreview.SetPositions(preview.ToArray());
            if(mouse.leftButton.wasPressedThisFrame)
            {
                // A blocked straight chord can still have a valid curved alternative.
                if(curvedRoad && curveEnd==null)
                {
                    if(CityRoads.Length(roadStart,end)<1.5f) notice="弯道端点太近，请选择更远的终点。";
                    else {curveEnd=end; roadPlan=null; plannedPointer=new Vector2(float.MaxValue,float.MaxValue); notice="移动鼠标调整弧度，点击确认建设；右键取消。";}
                }
                else if(!roadPlan.Valid) {notice=roadPlan.error; city.Trace("road.rejected",notice,new CityLogDetail {x=end.x,z=end.z},level:"warning");}
                else if(city.CommitRoad(roadPlan))
                {
                    roadUndo.Add((roadPlan.stroke,roadPlan.cost)); RefreshRoadView();
                    notice="道路已建成，花费 ¥ "+roadPlan.cost+"。可以继续点击延伸，右键结束；Ctrl+Z 撤销上一笔。";
                    roadStart=curvedRoad?curveEnd:end; curveEnd=null; roadPlan=null; plannedPointer=new Vector2(float.MaxValue,float.MaxValue);
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
            city.Trace("road.undone","撤销道路施工并退款",new CityLogDetail {ids=ids,amount=edit.cost});
            notice="已撤销上一笔道路并退还 ¥ "+edit.cost+"；原有路口连接保留。";
        }
        void RoadToolPanel(Color background)
        {
            Panel(new Rect(24,120,310,258),background);
            GUI.Label(new Rect(40,134,278,32),"自由道路 · 双向两车道",label);
            bool mode=GUI.Toolbar(new Rect(40,173,278,28),curvedRoad?1:0,new[]{"直线","弯道"},button)==1;
            if(mode!=curvedRoad) {CancelRoad(); curvedRoad=mode;}
            string details=roadStart==null ? (curvedRoad?"起点 → 终点 → 调弧度并确认。":"点击起点，再点击终点。")+"\n端点与已有道路会自动吸附。" : roadPlan==null ? (curveEnd!=null?"移动鼠标调整弧度，再点击确认。":"移动鼠标选择下一段终点。")
                : "长度 "+roadPlan.length.ToString("F1")+" 米   费用 ¥ "+roadPlan.cost+"\n最大坡度 "+(roadPlan.grade*100).ToString("F1")+"%\n"+(roadPlan.Valid ? "可以建设 · 交叉处自动形成路口" : roadPlan.error);
            GUI.Label(new Rect(40,208,278,76),details,small);
            GUI.Label(new Rect(40,284,278,44),"右键 / Esc 撤回起点，再按退出\nCtrl+Z 撤销上一笔道路",small);
            if(GUI.Button(new Rect(40,337,278,27),"撤销上一笔道路",button)) UndoRoad();
        }
        Vector3 RoadPosition(int id)
        {
            
            return CityRoadView.Point(city.roads.Node(id));
        }
    }
}
