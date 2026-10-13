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
        RoadNode curveControl,previousCurveControl;
        int roadMode;
        float roadElevation;
        bool undergroundView;
        int roadOperation;
        int junctionTool;
        bool roadOneWay;
        RoadKind roadKind;
        string roadUpgradePreview;
        RoadPlan roadPlan;
        readonly List<(int stroke,int cost)> roadUndo = new List<(int,int)>();
        Vector2 plannedPointer = new Vector2(float.MaxValue,float.MaxValue);

        void InitializeRoads()
        {
            city.SetEntranceHeight(landscape.Height);
            city.developmentHeight=landscape.Height;
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
            ResetRoad();
        }
        bool CancelRoad()
        {
            if(curveControl!=null || previousCurveControl!=null)
            {curveControl=null;previousCurveControl=null;roadPlan=null;roadPreview.positionCount=0;notice="已撤回方向点；可以重新确定方向。";return true;}
            bool drawing=roadStart!=null;ResetRoad();return drawing;
        }
        void ResetRoad()
        {
            roadStart=null;curveControl=null;previousCurveControl=null;roadPlan=null;
            roadUpgradePreview=null;
            if(roadPreview!=null) roadPreview.positionCount=0;
        }
        void RefreshRoadView()
        {
            if(roadView!=null) roadView.Rebuild(city,landscape,Mat(palette[1]),Mat(new Color(.47f,.48f,.43f)),Mat(new Color(.82f,.8f,.63f)));
        }
        void SetRoadElevation(float value)
        {
            roadElevation=Mathf.Clamp(value,-12,18);roadPlan=null;
            notice="道路高度 "+roadElevation.ToString("+0;-0;0")+"；PgUp/PgDn 调整，Home 回地面。";
            SetUndergroundView(roadElevation<0);
        }
        void SetUndergroundView(bool value)
        {
            undergroundView=value;
            if(landscape.Terrain!=null){landscape.Terrain.drawHeightmap=!value;landscape.Terrain.drawTreesAndFoliage=!value;}
            var sea=transform.Find("Sea");if(sea!=null)sea.gameObject.SetActive(!value);
            if(roadView!=null)roadView.underground=value;
            RefreshRoadView();
        }
        void BulldozeToolPanel()
        {
            GUI.Label(new Rect(40,177,278,25),"拆除道路 / 建筑",label);
            if(GUI.Button(new Rect(40,210,134,30),undergroundView?"地表视图":"● 地表视图",button))SetUndergroundView(false);
            if(GUI.Button(new Rect(182,210,136,30),undergroundView?"● 地下视图":"地下视图",button))SetUndergroundView(true);
            GUI.Label(new Rect(40,254,278,112),"点击红色预览的路段拆除。\n地下视图只选取隧道，不拆地表建筑。\n地表视图可拆地面路和高架。\n无需把道路高度调到同一深度。\n每次拆除 ¥40；西侧入口点保留。",small);
        }
        bool HandleRoadInput(Mouse mouse,Ray ray,bool blocked)
        {
            if(selected!=LandUse.Road && selected!=LandUse.Bulldoze) { ResetRoad(); return false; }
            roadPreview.positionCount=0;
            if(blocked)return selected==LandUse.Road;
            if(selected==LandUse.Bulldoze)
            {
                ResetRoad();
                var picked=city.roads.PickVisibleRay(ray.origin.x,ray.origin.y,ray.origin.z,ray.direction.x,ray.direction.y,ray.direction.z,undergroundView);
                if(picked==null)return undergroundView;
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
                roadPreview.sharedMaterial=Mat(palette[8]); roadPreview.startWidth=roadPreview.endWidth=CityRoadTypes.Width(picked.kind);
                roadPreview.positionCount=points.Count; roadPreview.SetPositions(points.ToArray());
                if(mouse.leftButton.wasPressedThisFrame)
                {
                    if(city.money<40) notice="拆路资金不足。";
                    else if(city.RemoveRoads(run))
                    {
                        RefreshRoadView(); roadUndo.Clear();
                        city.Trace("road.demolished","拆除道路",new CityLogDetail {ids=run,amount=40});
                        notice="已拆除这段道路；车辆会重新寻路或等待道路恢复。";
                    }
                    else notice="道路已变化，请重新选择拆除路段。";
                }
                return true;
            }
            if(!landscape.Raycast(ray,out var hit))return true;
            if(roadOperation==3)
            {
                ResetRoad();int node=city.roads.PickJunction(hit.x,hit.z,landscape.Height(hit.x,hit.z)+roadElevation);if(node<0)return true;
                int from=city.roads.PickJunctionApproach(node,hit.x,hit.z);
                roadUpgradePreview="路口 #"+node+" · "+(city.roads.HasSignals(node)?"信号控制":"无信号")+(from<0?"":"\n入口 #"+from+" · "+(city.roads.Node(node).stopFrom.Contains(from)?"停车牌":"无停车牌"));
                if(mouse.leftButton.wasPressedThisFrame)
                {
                    string reason;bool changed=junctionTool==1?city.roads.ToggleStop(node,from,out reason):city.roads.SetJunctionControl(node,junctionTool==2?JunctionControl.Automatic:city.roads.HasSignals(node)?JunctionControl.Uncontrolled:JunctionControl.Signals,out reason);
                    if(changed)city.Trace("junction.control_changed",reason,new CityLogDetail{ids=new List<int>{node,from}});
                    notice=reason;
                }
                return true;
            }
            if(roadOperation>0)
            {
                ResetRoad();var picked=city.roads.Pick(hit.x,hit.z,level:landscape.Height(hit.x,hit.z)+roadElevation);if(picked==null)return true;
                roadPreview.sharedMaterial=Mat(palette[2]);roadPreview.startWidth=roadPreview.endWidth=CityRoadTypes.Width(roadOperation==1?roadKind:picked.kind);
                roadPreview.positionCount=2;roadPreview.SetPositions(new[]{RoadPosition(picked.a)+Vector3.up*.2f,RoadPosition(picked.b)+Vector3.up*.2f});
                if(roadOperation==1)city.PreviewRoadUpgrade(picked.id,roadKind,roadOneWay,out _,out roadUpgradePreview,landscape.Height);
                if(mouse.leftButton.wasPressedThisFrame)
                {string reason;bool changed=roadOperation==2?city.ChangeRoadDirection(picked.id,true,true,out reason):city.UpgradeRoad(picked.id,roadKind,roadOneWay,out reason,landscape.Height);if(changed){RefreshRoadView();roadUndo.Clear();}notice=reason;}
                return true;
            }
            var end=city.roads.Snap(hit.x,hit.z,landscape.Height,roadElevation);
            if(roadStart==null)
            {
                roadPreview.sharedMaterial=Mat(palette[2]); roadPreview.startWidth=roadPreview.endWidth=.6f;
                var p=CityRoadView.Point(end)+Vector3.up*.25f;
                roadPreview.positionCount=2; roadPreview.SetPositions(new[]{p,p+Vector3.right*.03f});
                if(mouse.leftButton.wasPressedThisFrame)
                { roadStart=end; roadPlan=null; plannedPointer=new Vector2(float.MaxValue,float.MaxValue); notice=roadMode>0?"点击方向点，再移动鼠标选择终点。":"移动鼠标预览道路，再点击终点；右键撤回起点。"; }
                return true;
            }
            // Direction points remain free; only actual endpoints snap to roads.
            if(roadMode>0 && curveControl==null && previousCurveControl==null)end=new RoadNode{x=hit.x,z=hit.z,y=landscape.Height(hit.x,hit.z)};
            var control=curveControl??(roadMode==2 && previousCurveControl!=null?CityRoads.ContinuationControl(roadStart,previousCurveControl,end):null);
            var pointer=new Vector2(end.x,end.z);
            if(roadPlan==null || Vector2.Distance(pointer,plannedPointer)>.05f || roadPlan.revision!=city.roads.revision || mouse.leftButton.wasPressedThisFrame)
            { roadPlan=city.roads.PlanTerrainSpan(city,roadStart,end,landscape.Height,roadOneWay,roadKind,Engineered(roadStart,end),roadMode>0?control:null); plannedPointer=pointer; }
            roadPreview.sharedMaterial=Mat(roadPlan.Valid ? palette[2] : palette[8]); roadPreview.startWidth=roadPreview.endWidth=CityRoadTypes.Width(roadKind);
            var preview=new List<Vector3>();
            for(int k=1;k<roadPlan.points.Count;k++)
            {
                var a=roadPlan.points[k-1]; var b=roadPlan.points[k]; int steps=Mathf.Max(1,Mathf.CeilToInt(CityRoads.Length(a,b)/.5f));
                for(int i=k==1?0:1;i<=steps;i++)
                {var p=CityRoads.Lerp(a,b,(float)i/steps); preview.Add(new Vector3(p.x,(roadPlan.engineered?p.y:landscape.Height(p.x,p.z))+.22f,p.z));}
            }
            roadPreview.positionCount=preview.Count; roadPreview.SetPositions(preview.ToArray());
            if(mouse.leftButton.wasPressedThisFrame)
            {
                // A blocked straight chord can still have a valid curved alternative.
                if(roadMode>0 && control==null)
                {
                    if(CityRoads.Length(roadStart,end)<1.5f) notice="弯道端点太近，请选择更远的终点。";
                    else {curveControl=end; roadPlan=null; plannedPointer=new Vector2(float.MaxValue,float.MaxValue); notice="移动鼠标选择终点，点击确认建设；右键撤回方向点。";}
                }
                else if(!roadPlan.Valid) {notice=roadPlan.error; city.Trace("road.rejected",notice,new CityLogDetail {x=end.x,z=end.z},level:"warning");}
                else if(city.CommitRoad(roadPlan))
                {
                    roadUndo.Add((roadPlan.stroke,roadPlan.cost)); RefreshRoadView();
                    notice="道路已建成，花费 ¥ "+roadPlan.cost+"。可以继续点击延伸，右键结束；Ctrl+Z 撤销上一笔。";
                    previousCurveControl=roadMode==2?control:null;roadStart=end;curveControl=null;roadPlan=null;plannedPointer=new Vector2(float.MaxValue,float.MaxValue);
                }
            }
            return true;
        }
        void UndoRoad()
        {
            ResetRoad();
            if(roadUndo.Count==0) { notice="没有可撤销的道路施工（读取城市或拆路后重置）。"; return; }
            var edit=roadUndo[roadUndo.Count-1]; roadUndo.RemoveAt(roadUndo.Count-1);
            var ids=new List<int>(); foreach(var e in city.roads.edges) if(e.stroke==edit.stroke) ids.Add(e.id);
            city.RemoveRoads(ids,0); city.money+=edit.cost; RefreshRoadView();
            city.Trace("road.undone","撤销道路施工并退款",new CityLogDetail {ids=ids,amount=edit.cost});
            notice="已撤销上一笔道路并退还 ¥ "+edit.cost+"；原有路口连接保留。";
        }
        void RoadToolPanel(Color background)
        {
            Panel(new Rect(24,120,310,484),background);
            GUI.Label(new Rect(40,134,278,32),CityRoadTypes.Name(roadKind),label);
            int type=GUI.Toolbar(new Rect(40,173,278,28),roadOneWay?1:0,new[]{"双向道路","单行道路"},button);
            if((type==1)!=roadOneWay){ResetRoad();roadOneWay=type==1;if(!CityRoadTypes.Valid(roadKind,roadOneWay))roadKind=RoadKind.Standard;}
            int operation=GUI.Toolbar(new Rect(40,208,278,28),roadOperation,new[]{"建设","改类型","反向","路口"},button);
            if(operation!=roadOperation){ResetRoad();roadOperation=operation;}
            if(roadOperation==3)
            {int mode=GUI.Toolbar(new Rect(40,243,278,28),junctionTool,new[]{"开关信号","停车牌","恢复默认"},button);if(mode!=junctionTool){ResetRoad();junctionTool=mode;}}
            else
            {int mode=GUI.Toolbar(new Rect(40,243,278,28),roadMode,new[]{"直线","曲线","自由曲线"},button);if(mode!=roadMode){ResetRoad();roadMode=mode;}}
            int surface=GUI.Toolbar(new Rect(40,278,278,28),CityRoadTypes.LimitedAccess(roadKind)?-1:(int)roadKind,new[]{"普通铺装","双向土路","四车道"},button);
            if(surface>=0 && surface!=(int)roadKind){ResetRoad();roadKind=(RoadKind)surface;roadUpgradePreview=null;if(roadKind!=RoadKind.Standard)roadOneWay=false;}
            int highway=GUI.Toolbar(new Rect(40,310,278,28),CityRoadTypes.LimitedAccess(roadKind)?(int)roadKind-3:-1,new[]{"三车道高速","单车道匝道"},button);
            if(highway>=0 && highway+3!=(int)roadKind){ResetRoad();roadKind=(RoadKind)(highway+3);roadOneWay=true;roadUpgradePreview=null;}
            string details=roadStart==null ? (roadMode>0?"起点 → 方向点 → 终点。":"点击起点，再点击终点。")+"\n端点与已有道路会自动吸附。" : roadPlan==null ? (curveControl!=null?"移动鼠标选择终点，再点击确认。":previousCurveControl!=null?"下一段延续末端方向，点击终点。":"移动鼠标选择下一点。")
                : "长度 "+roadPlan.length.ToString("F1")+" 单位   费用 ¥ "+roadPlan.cost+"\n最大坡度 "+(roadPlan.grade*100).ToString("F1")+"%\n"+(roadPlan.Valid ? roadPlan.automaticSpan?"自动桥隧 · 按两端高度连接":"可以建设 · 同高度交叉处形成路口" : roadPlan.error);
            if(roadOperation>0)details=roadOperation==2?"左键点击单行路段，反转整个路口间路段。\n有车辆占用时暂不能修改。":"左键点击路段，改为所选类型。\n"+(roadUpgradePreview??"移到道路上预览费用。");
            if(roadOperation==3)details="信号：点路口；停车牌：点驶入道路。\n停车牌关信号；恢复默认保留牌。\n"+(roadUpgradePreview??"移到路口查看状态。");
            else if(roadOneWay)details+="\n单行方向：起点 → 终点。";
            if(CityRoadTypes.LimitedAccess(roadKind) && roadOperation==0)details+="\n无沿路分区、建筑入口或人行道。";
            GUI.Label(new Rect(40,342,278,88),details,small);
            GUI.Label(new Rect(40,432,278,44),"右键 / Esc 撤回或退出；右键拖动旋转\nCtrl+Z 撤销上一笔道路",small);
            if(GUI.Button(new Rect(40,486,278,27),"撤销上一笔道路",button)) UndoRoad();
            if(GUI.Button(new Rect(40,521,55,27),"降",button))SetRoadElevation(roadElevation-3);
            if(GUI.Button(new Rect(99,521,55,27),"地面",button))SetRoadElevation(0);
            if(GUI.Button(new Rect(158,521,55,27),"升",button))SetRoadElevation(roadElevation+3);
            if(GUI.Button(new Rect(217,521,101,27),undergroundView?"地表视图":"地下视图",button))SetUndergroundView(!undergroundView);
            GUI.Label(new Rect(40,556,278,40),"高度 "+roadElevation.ToString("+0;-0;0")+" · PgUp/PgDn；Home 回地面\n先选地面起点，再升/降终点建立坡道",small);
        }
        bool Engineered(RoadNode a,RoadNode b)=>Mathf.Abs(a.y-landscape.Height(a.x,a.z))>.15f || Mathf.Abs(b.y-landscape.Height(b.x,b.z))>.15f;
        Vector3 RoadPosition(int id)
        {
            
            return CityRoadView.Point(city.roads.Node(id));
        }
    }
}
