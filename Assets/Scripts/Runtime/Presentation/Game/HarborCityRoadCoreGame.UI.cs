using System;
using UnityEngine;

namespace HarborCity
{
    public sealed partial class HarborCityRoadCoreGame
    {
        enum ToolPage { None, Roads, Zones, Utilities, Trade, City, Options }
        static readonly string[] PageTitles={"ui.hud.page.roads","ui.hud.page.zones","ui.hud.page.utilities","ui.hud.page.trade","ui.hud.page.city","ui.hud.page.game"};
        ToolPage toolPage;
        GUIStyle hudText,heading,wrapped,muted,small,metric,tabText;
        GUISkin citySkin;
        static readonly Color Cyan=new Color(.26f,.83f,.95f),Muted=new Color(.78f,.85f,.90f);
        static readonly Color[] CategoryColours={new Color(.37f,.76f,.96f),new Color(.43f,.86f,.57f),new Color(.98f,.77f,.36f),new Color(.69f,.59f,.95f),new Color(.39f,.81f,.78f),new Color(.70f,.77f,.83f)};
        string hudNoticeSeen;double hudNoticeUntil;
        GUIStyle rateButton;
        Rect ToolbarBounds=>new Rect(Math.Max(8,(Screen.width-736)/2),Screen.height-48,Math.Min(736,Screen.width-16),40);
        Rect StatusBounds=>new Rect(8,8,Math.Min(408,(Screen.width-24)*.51f),78);
        Rect TimeBounds=>new Rect(Screen.width-Math.Min(352,(Screen.width-24)*.46f)-8,8,Math.Min(352,(Screen.width-24)*.46f),64);
        Rect NoticeBounds=>Time.realtimeSinceStartupAsDouble<hudNoticeUntil?new Rect(Math.Max(8,(Screen.width-480)/2),96,Math.Min(480,Screen.width-16),30):new Rect();
        Rect ToolPanelBounds=>toolPage==ToolPage.None?new Rect():new Rect(8,96,Math.Min(280,Screen.width-16),Math.Min(256,Screen.height-152));
        bool BlocksHudInput(Vector2 point)
        {
            var p=new Vector2(point.x,Screen.height-point.y);
            return TimeBounds.Contains(p)||ToolbarBounds.Contains(p)||StatusBounds.Contains(p)||ToolPanelBounds.Contains(p)||NoticeBounds.Contains(p);
        }
        void SelectPage(ToolPage page)
        {
            // Collapsing a panel retains the selected map tool; switching categories cancels it.
            if(toolPage==page){toolPage=ToolPage.None;return;}
            CancelTool();observation.Close();financePanel.IsOpen=false;toolPage=page;menuScroll=Vector2.zero;
        }
        static void Fill(Rect r,Color colour)
        {var old=GUI.color;GUI.color=colour;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        void HudBox(Rect bounds)
        {
            Fill(new Rect(bounds.x+3,bounds.y+4,bounds.width,bounds.height),new Color(0,0,0,.23f));
            Fill(bounds,new Color(.035f,.065f,.09f,.97f));
            Fill(new Rect(bounds.x,bounds.y,bounds.width,1),new Color(.26f,.38f,.46f,.8f));
        }
        void EnsureCitySkin()
        {
            if(citySkin!=null)return;
            citySkin=Instantiate(GUI.skin);citySkin.name="HarborCity UI";
            hudText=new GUIStyle(GUI.skin.label){fontSize=14,padding=new RectOffset(0,0,0,0),normal={textColor=new Color(.87f,.93f,.97f)}};
            heading=new GUIStyle(hudText){fontSize=17,fontStyle=FontStyle.Bold};
            wrapped=new GUIStyle(hudText){wordWrap=true,fontSize=13};
            muted=new GUIStyle(wrapped){normal={textColor=Muted}};
            small=new GUIStyle(muted){fontSize=12};metric=new GUIStyle(heading){fontSize=22};
            tabText=new GUIStyle(hudText){alignment=TextAnchor.MiddleCenter,fontSize=13};
            citySkin.label=new GUIStyle(hudText){wordWrap=true};
            citySkin.button=new GUIStyle(GUI.skin.button){fontSize=14,wordWrap=true,padding=new RectOffset(12,12,4,4),margin=new RectOffset(3,3,3,3),border=new RectOffset(0,0,0,0)};
            foreach(var state in new[]{citySkin.button.normal,citySkin.button.hover,citySkin.button.active,citySkin.button.onNormal,citySkin.button.onHover,citySkin.button.onActive}){state.background=Texture2D.whiteTexture;state.textColor=Color.white;}
            rateButton=new GUIStyle(citySkin.button){fontSize=12,padding=new RectOffset(4,4,3,3)};
            citySkin.button.hover.textColor=Cyan;citySkin.button.onNormal.textColor=Cyan;
            citySkin.toggle=new GUIStyle(citySkin.button);citySkin.toggle.onNormal.textColor=Cyan;
            citySkin.box=new GUIStyle(GUI.skin.box){padding=new RectOffset(10,10,8,8),normal={textColor=Color.white}};
        }
        static void IconLine(Vector2 from,Vector2 to,Color colour,float width=2)
        {
            var matrix=GUI.matrix;var delta=to-from;GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,from);
            Fill(new Rect(from.x,from.y-width/2,delta.magnitude,width),colour);GUI.matrix=matrix;
        }
        static void DrawCategoryIcon(int index,Vector2 p,Color colour)
        {
            if(index==0){IconLine(p+new Vector2(-8,-10),p+new Vector2(-8,10),colour);IconLine(p+new Vector2(8,-10),p+new Vector2(8,10),colour);for(int i=-8;i<10;i+=7)Fill(new Rect(p.x-1,p.y+i,2,4),colour);}
            else if(index==1){for(int y=0;y<3;y++)for(int x=0;x<3;x++)Fill(new Rect(p.x-11+x*8,p.y-11+y*8,6,6),colour);}
            else if(index==2){IconLine(p+new Vector2(3,-12),p+new Vector2(-7,1),colour,3);IconLine(p+new Vector2(-7,1),p+new Vector2(7,1),colour,3);IconLine(p+new Vector2(7,1),p+new Vector2(-3,12),colour,3);}
            else if(index==3){Fill(new Rect(p.x-13,p.y-7,17,13),colour);Fill(new Rect(p.x+5,p.y-3,9,9),colour);Fill(new Rect(p.x-9,p.y+8,5,4),colour);Fill(new Rect(p.x+7,p.y+8,5,4),colour);}
            else if(index==4){for(int i=0;i<3;i++)Fill(new Rect(p.x-12+i*9,p.y+9-(i+1)*7,6,(i+1)*7),colour);}
            else{IconLine(p+new Vector2(-10,-7),p+new Vector2(10,-7),colour);IconLine(p+new Vector2(-10,0),p+new Vector2(10,0),colour);IconLine(p+new Vector2(-10,7),p+new Vector2(10,7),colour);Fill(new Rect(p.x-6,p.y-10,4,6),colour);Fill(new Rect(p.x+3,p.y-3,4,6),colour);Fill(new Rect(p.x-3,p.y+4,4,6),colour);}
        }
        void DrawToolDock()
        {
            var b=ToolbarBounds;HudBox(b);float width=(b.width-98)/6;
            for(int i=0;i<6;i++)
            {
                var r=new Rect(b.x+4+i*width,b.y+4,width-3,32);bool selected=toolPage==(ToolPage)(i+1),hover=r.Contains(Event.current.mousePosition);
                if(selected||hover)Fill(r,selected?new Color(.12f,.27f,.34f):new Color(.1f,.16f,.20f));
                if(GUI.Button(r,new GUIContent("",localization.Get(PageTitles[i])),GUIStyle.none))SelectPage((ToolPage)(i+1));
                var p=new Vector2(r.x+17,r.center.y);var matrix=GUI.matrix;GUIUtility.ScaleAroundPivot(new Vector2(.65f,.65f),p);DrawCategoryIcon(i,p,CategoryColours[i]);GUI.matrix=matrix;
                GUI.Label(new Rect(r.x+32,r.y,r.width-34,r.height),localization.Get(PageTitles[i]),tabText);
                if(selected)Fill(new Rect(r.x+6,r.yMax-2,r.width-12,2),CategoryColours[i]);
            }
            if(GUI.Button(new Rect(b.xMax-89,b.y+5,82,30),new GUIContent(localization.Get("ui.hud.cancel"),ActiveTool),rateButton))CancelTool();
        }
        void DrawTimeControls()
        {
            var b=TimeBounds;HudBox(b);
            GUI.Label(new Rect(b.x+8,b.y+12,76,20),world.Clock.ToString("F1")+" s",small);
            float width=(b.width-94)/5,x=b.x+86;
            foreach(float rate in new[]{0f,1f,3f,20f,50f})
            {
                var old=GUI.backgroundColor;GUI.backgroundColor=speed==rate?new Color(.12f,.51f,.63f):new Color(.11f,.18f,.24f);
                if(GUI.Button(new Rect(x,b.y+6,width-3,28),rate==0?localization.Get("ui.hud.pause"):rate+"×",rateButton))ScenarioRate(rate);GUI.backgroundColor=old;x+=width;
            }
            GUI.Label(new Rect(b.x+8,b.y+42,b.width-16,18),new GUIContent(localization.Get("ui.design.rate_compact",speed,double.IsNaN(actualRate)?"…":actualRate.ToString("F1")),localization.Get("ui.hud.rate",speed,double.IsNaN(actualRate)?"…":actualRate.ToString("F1"))),small);
        }
        string ActiveTool=>gridTool.Enabled?localization.Get("ui.hud.active.grid"):building?localization.Get("ui.hud.active.road"):utilityTool.HasValue?localization.Get("ui.hud.active.pipe"):facilityTool!=LandUse.Empty?localization.Get("ui.hud.active.facility"):zoneTool!=LandUse.Empty?localization.Get("ui.hud.active.zone"):outsideTool?localization.Get("ui.hud.active.outside"):roadEditor.IsSelecting?localization.Get("ui.hud.active.edit"):localization.Get("ui.hud.active.inspect");
        void DrawInterface()
        {
            if(world==null)
            {
                if(localization==null){var language=Resources.Load<TextAsset>("GameData/Localization/zh-CN");if(language!=null)localization=new CityLocalization(JsonUtility.FromJson<TranslationData>(language.text));}
                if(localization!=null)GUI.Box(new Rect(8,8,Math.Min(Screen.width-16,760),80),localization.Get("ui.build.restart_play"));return;
            }
            EnsureCitySkin();
            if(Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.Escape){CancelTool();toolPage=ToolPage.None;observation.Close();financePanel.IsOpen=false;Event.current.Use();}
            var oldSkin=GUI.skin;var oldContent=GUI.contentColor;var oldBackground=GUI.backgroundColor;
            try
            {
                GUI.skin=citySkin;GUI.contentColor=Color.white;GUI.backgroundColor=new Color(.16f,.24f,.30f);
                outsideViews?.Draw(cameraView,MenuBounds);
                DrawCityStatus();DrawToolDock();DrawTimeControls();
                if(hudNoticeSeen!=notice){if(hudNoticeSeen!=null&&!string.IsNullOrEmpty(notice))hudNoticeUntil=Time.realtimeSinceStartupAsDouble+4;hudNoticeSeen=notice;}
                if(NoticeBounds.width>0){HudBox(NoticeBounds);GUI.Label(new Rect(NoticeBounds.x+8,NoticeBounds.y+7,NoticeBounds.width-16,20),new GUIContent(notice,notice),small);}
                if(toolPage!=ToolPage.None)DrawToolPage();
                roadEditor?.Bind(session);roadEditor?.Draw();observation?.Bind(session);observation?.Draw();financePanel?.Draw(session?.Simulation);
                if(Event.current.type==EventType.Repaint&&!string.IsNullOrEmpty(GUI.tooltip))
                {var m=Event.current.mousePosition;var r=new Rect(Mathf.Clamp(m.x-90,8,Screen.width-228),Mathf.Clamp(m.y-42,8,Screen.height-48),220,34);HudBox(r);GUI.Label(new Rect(r.x+10,r.y+8,200,22),GUI.tooltip,small);}
            }
            finally{GUI.skin=oldSkin;GUI.contentColor=oldContent;GUI.backgroundColor=oldBackground;}
        }
        void DrawCityStatus()
        {
            var b=StatusBounds;HudBox(b);float cell=(b.width-16)/3;
            string[] labels={localization.Get("ui.design.people"),localization.Get("ui.design.vehicles"),localization.Get("ui.design.funds")};
            string[] values={(session?.Entities.Members.Count??0).ToString("N0"),world.ActiveCount.ToString("N0"),session==null?"—":session.Simulation.Finance.UnlimitedFunds?"∞":session.Simulation.Finance.Money.ToString("N0")};
            for(int i=0;i<3;i++)GUI.Label(new Rect(b.x+8+i*cell,b.y+8,cell,20),labels[i]+"  "+values[i],small);
            if(session!=null)
            {
                SupplyRow(new Rect(b.x+8,b.y+30,b.width-16,18),localization.Get("ui.design.power"),session.City.demand,session.City.PowerSupplied,session.City.power,new Color(.97f,.77f,.35f));
                SupplyRow(new Rect(b.x+8,b.y+53,b.width-16,18),localization.Get("ui.design.water"),session.City.WaterDemand,session.City.WaterSupplied,session.City.water,new Color(.35f,.72f,.98f));
            }
        }
        void SupplyRow(Rect r,string label,int demand,int supplied,int capacity,Color colour)
        {
            GUI.Label(new Rect(r.x,r.y,40,20),label,small);
            float width=Math.Max(12,r.width-270);var bar=new Rect(r.x+44,r.y+5,width,5);Fill(bar,new Color(.13f,.21f,.26f));
            Fill(new Rect(bar.x,bar.y,bar.width*(capacity<=0?0:Mathf.Clamp01((float)supplied/capacity)),bar.height),supplied<demand?new Color(1,.42f,.32f):colour);
            GUI.Label(new Rect(r.x+width+54,r.y,r.width-width-54,20),localization.Get("ui.design.supply",demand,supplied,capacity),small);
        }
        void DrawToolPage()
        {
            var b=ToolPanelBounds;HudBox(b);GUILayout.BeginArea(new Rect(b.x+10,b.y+8,b.width-20,b.height-16));
            GUILayout.BeginHorizontal();GUILayout.Label(localization.Get(PageTitles[(int)toolPage-1]),heading);if(GUILayout.Button(localization.Get("ui.hud.collapse"),GUILayout.Width(56)))toolPage=ToolPage.None;GUILayout.EndHorizontal();
            menuScroll=GUILayout.BeginScrollView(menuScroll);
            switch(toolPage)
            {
                case ToolPage.Roads:DrawRoadTools();break;
                case ToolPage.Zones:DrawZoneTools();break;
                case ToolPage.Utilities:DrawUtilityTools();break;
                case ToolPage.Trade:DrawTradeTools();break;
                case ToolPage.City:DrawObservationTools();break;
                case ToolPage.Options:DrawGameOptions();break;
            }
            GUILayout.Label(ActiveTool,small);GUILayout.Label((building||utilityTool.HasValue)&&!string.IsNullOrEmpty(previewText)?previewText:notice,wrapped);
            GUILayout.EndScrollView();GUILayout.EndArea();
        }
        bool ChoiceButton(string title,Color colour,bool selected)
        {
            var old=GUI.backgroundColor;GUI.backgroundColor=selected?new Color(colour.r*.45f,colour.g*.55f,colour.b*.55f):new Color(.14f,.21f,.27f);
            bool clicked=GUILayout.Button(title,GUILayout.Height(32));GUI.backgroundColor=old;
            if(Event.current.type==EventType.Repaint){var r=GUILayoutUtility.GetLastRect();Fill(new Rect(r.x,r.y,3,r.height),colour);if(selected)Fill(new Rect(r.x+3,r.yMax-2,r.width-3,2),colour);}
            return clicked;
        }
        bool GrowingCity=>session!=null&&session.City.development.enabled;
        void DrawRoadTools()
        {
            if(ChoiceButton(building?localization.Get("ui.hud.end_road"):localization.Get("ui.hud.active.road"),CategoryColours[0],building))
            {bool enable=!building;CancelTool();building=enable;notice=localization.Get(enable?"ui.build.choose_start":"ui.build.cancelled");}
            GUILayout.BeginHorizontal();oneWay=GUILayout.Toggle(oneWay,localization.Get("ui.hud.oneway"));wide=GUILayout.Toggle(wide,localization.Get("ui.hud.wide"));GUILayout.EndHorizontal();
            if(session!=null&&landscape!=null)
            {
                if(ChoiceButton(localization.Get(gridTool.Enabled?"ui.grid.close":"ui.grid.open"),CategoryColours[0],gridTool.Enabled))
                {bool enable=!gridTool.Enabled;CancelTool();gridTool.Enabled=enable;notice=localization.Get(enable?"ui.grid.help":"ui.build.cancelled");}
                if(gridTool.Enabled)gridTool.DrawSettings();
            }
            if(session!=null&&GUILayout.Button(localization.Get("ui.hud.edit_road")))
            {CancelTool();observation.Close();roadEditor.Bind(session);roadEditor.Begin();toolPage=ToolPage.None;}
            GUILayout.Label(localization.Get("ui.hud.road_help"),wrapped);
            if(session!=null&&session.City.utilities.roadConduits)GUILayout.Label(localization.Get("ui.utility.road_conduits"),wrapped);
        }
        void DrawZoneTools()
        {
            if(!GrowingCity){GUILayout.Label(localization.Get("ui.hud.no_zones"));return;}
            foreach(var use in new[]{LandUse.Residential,LandUse.Commercial,LandUse.Industrial,LandUse.Bulldoze})
                if(ChoiceButton(use==LandUse.Residential?localization.Get("ui.hud.residential"):use==LandUse.Commercial?localization.Get("ui.hud.commercial"):use==LandUse.Industrial?localization.Get("ui.hud.industrial"):localization.Get("ui.hud.erase"),use==LandUse.Bulldoze?new Color(.9f,.46f,.42f):HarborCityZoningViews.Colour(use),zoneTool==use))
                {CancelTool();zoneTool=use;notice=localization.Get("ui.hud.zone_help");}
            if(zoneTool!=LandUse.Empty)zoneBoxTool.DrawControls();
        }
        void DrawUtilityTools()
        {
            if(!GrowingCity){GUILayout.Label(localization.Get("ui.hud.no_utilities"));return;}
            if(GUILayout.Button(localization.Get("ui.template.focus")))constructionCamera?.Focus(-660,0);
            foreach(var use in new[]{LandUse.Power,LandUse.Water,LandUse.Sewage,LandUse.Landfill})
                if(ChoiceButton(use==LandUse.Power?localization.Get("ui.hud.power"):use==LandUse.Water?localization.Get("ui.hud.water"):use==LandUse.Sewage?localization.Get("ui.hud.sewage"):localization.Get("ui.waste.facility"),use==LandUse.Power?CategoryColours[2]:use==LandUse.Landfill?CategoryColours[1]:CategoryColours[0],facilityTool==use))
                {CancelTool();facilityTool=use;notice=localization.Get("ui.hud.facility_help");}
            GUILayout.BeginHorizontal();foreach(var kind in new[]{UtilityKind.Electricity,UtilityKind.Water})
                if(GUILayout.Button(kind==UtilityKind.Electricity?localization.Get("ui.hud.electric_line"):localization.Get("ui.hud.pipe"))){CancelTool();utilityTool=kind;notice=localization.Get("ui.build.pipe_start");}
            GUILayout.EndHorizontal();if(GUILayout.Button(localization.Get("ui.hud.rotate")))facilityYaw=(facilityYaw+90)%360;
            GUILayout.Label(localization.Get("ui.hud.supply",session.City.demand,session.City.power,session.City.WaterDemand,session.City.water,session.City.SewageCapacity),wrapped);
            GUILayout.Label(localization.Get("ui.waste.summary",session.Simulation.Waste.Waiting,session.Simulation.Waste.InTransit,session.Simulation.Waste.Stored,session.Simulation.Waste.Processed,session.Simulation.Waste.Vehicles),wrapped);
        }
        void DrawTradeTools()
        {
            if(GrowingCity)
            {
                GUILayout.Label(localization.Get("ui.trade.summary",session.City.trade.rawDelivered,session.City.trade.goodsExported,session.City.trade.cash.ToString("F0")),wrapped);
                if(GUILayout.Button(localization.Get("ui.trade.connect"))){CancelTool();outsideTool=true;notice=localization.Get("ui.trade.choose");}
                if(GUILayout.Button(localization.Get("ui.trade.focus_import")))FocusOutside(OutsideFlow.Import);
                if(GUILayout.Button(localization.Get("ui.trade.focus_export")))FocusOutside(OutsideFlow.Export);
            }
            else if(GUILayout.Button(localization.Get("ui.hud.dispatch")))notice=(session==null?world.Dispatch(origin,destination,1,5):session.Simulation.RequestFreight(origin,destination))==null?localization.Get("ui.hud.no_freight"):localization.Get("ui.hud.freight_sent");
            GUILayout.Label(localization.Get("ui.hud.delivered",world.Delivered),wrapped);
            foreach(var d in world.Depots)GUILayout.Label(localization.Get("ui.hud.depot",d.id,d.stock,d.capacity,d.cash.ToString("F0")),wrapped);
        }
        void DrawObservationTools()
        {
            if(session==null)return;
            if(GUILayout.Button(localization.Get("ui.inspect.open"))){CancelTool();observation.Bind(session);observation.ToggleIssues();toolPage=ToolPage.None;}
            if(GUILayout.Button(localization.Get("ui.finance.open"))){financePanel.IsOpen=true;toolPage=ToolPage.None;}
            GUILayout.Label(localization.Get("ui.hud.network",world.Network.RoadCount,world.Network.JunctionCount),wrapped);
            if(GrowingCity)
            {
                GUILayout.Label(localization.Get("ui.hud.projects",session.City.society.jobEntities.Count,session.City.development.projects.Count),wrapped);
                GUILayout.Label((employmentReadout??(employmentReadout=new HarborCityEmploymentReadout(localization))).Read(session,Time.realtimeSinceStartupAsDouble),wrapped);
            }
            GUILayout.Label(localization.Get("ui.retail.summary",session.City.consumption.ConsumedGoods,session.City.consumption.RetailPayments.ToString("F0")),wrapped);
            GUILayout.Label(localization.Get("ui.time.pending",session.Simulation.PendingSeconds.ToString("F2")),wrapped);
            if(!GrowingCity)GUILayout.Label(localization.Get("ui.hud.factory",session.City.Factory(1)?.raw,session.City.Goods(2)),wrapped);
        }
        void DrawGameOptions()
        {
            if(GUILayout.Button(localization.Get("ui.hud.save"),GUILayout.Height(28)))Save();if(GUILayout.Button(localization.Get("ui.hud.load"),GUILayout.Height(28)))Load();
            if(session!=null)session.Simulation.Finance.UnlimitedFunds=GUILayout.Toggle(session.Simulation.Finance.UnlimitedFunds,localization.Get("ui.finance.unlimited"));
            GUILayout.Label(localization.Get("ui.hud.controls"),wrapped);
            GUILayout.Label(localization.Get("ui.hud.save_path",SavePath),wrapped);
        }
    }
}
