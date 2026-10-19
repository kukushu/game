using System;
using UnityEngine;

namespace HarborCity
{
    public sealed partial class HarborCityRoadCoreGame
    {
        enum ToolPage { None, Roads, Zones, Utilities, Trade, City, Options }
        static readonly string[] PageTitles={"ui.hud.page.roads","ui.hud.page.zones","ui.hud.page.utilities","ui.hud.page.trade","ui.hud.page.city","ui.hud.page.game"};
        ToolPage toolPage;
        GUIStyle hudText,heading,wrapped;
        Rect ToolbarBounds=>new Rect(Math.Max(8,(Screen.width-600)/2),Screen.height-52,Math.Min(600,Screen.width-16),44);
        Rect StatusBounds=>new Rect(Math.Max(8,Screen.width-348),8,Math.Min(340,Screen.width-16),132);
        Rect NoticeBounds=>new Rect(ToolbarBounds.x,ToolbarBounds.y-54,ToolbarBounds.width,48);
        Rect ToolPanelBounds=>toolPage==ToolPage.None?new Rect():new Rect(ToolbarBounds.x,NoticeBounds.y-Math.Min(300,Math.Max(100,Screen.height-254))-8,Math.Min(420,Screen.width-16),Math.Min(300,Math.Max(100,Screen.height-254)));
        bool BlocksHudInput(Vector2 point)
        {
            var p=new Vector2(point.x,Screen.height-point.y);
            return ToolbarBounds.Contains(p)||StatusBounds.Contains(p)||ToolPanelBounds.Contains(p)||NoticeBounds.Contains(p);
        }
        void SelectPage(ToolPage page)
        {
            // Collapsing a panel retains the selected map tool; switching categories cancels it.
            if(toolPage==page){toolPage=ToolPage.None;return;}
            CancelTool();observation.Close();financePanel.IsOpen=false;toolPage=page;menuScroll=Vector2.zero;
        }
        void HudBox(Rect bounds)
        {
            var colour=GUI.color;GUI.color=new Color(.065f,.095f,.12f,.96f);GUI.DrawTexture(bounds,Texture2D.whiteTexture);
            GUI.color=new Color(.2f,.55f,.65f,1);GUI.DrawTexture(new Rect(bounds.x,bounds.y,bounds.width,2),Texture2D.whiteTexture);GUI.color=colour;
        }
        string ActiveTool=>gridTool.Enabled?localization.Get("ui.hud.active.grid"):building?localization.Get("ui.hud.active.road"):utilityTool.HasValue?localization.Get("ui.hud.active.pipe"):facilityTool!=LandUse.Empty?localization.Get("ui.hud.active.facility"):zoneTool!=LandUse.Empty?localization.Get("ui.hud.active.zone"):outsideTool?localization.Get("ui.hud.active.outside"):roadEditor.IsSelecting?localization.Get("ui.hud.active.edit"):localization.Get("ui.hud.active.inspect");
        void DrawInterface()
        {
            if(world==null)
            {
                if(localization==null){var language=Resources.Load<TextAsset>("GameData/Localization/zh-CN");if(language!=null)localization=new CityLocalization(JsonUtility.FromJson<TranslationData>(language.text));}
                if(localization!=null)GUI.Box(new Rect(8,8,Math.Min(Screen.width-16,760),80),localization.Get("ui.build.restart_play"));return;
            }
            if(hudText==null)
            {
                hudText=new GUIStyle(GUI.skin.label){fontSize=14,normal={textColor=Color.white}};
                heading=new GUIStyle(hudText){fontSize=16,fontStyle=FontStyle.Bold};
                wrapped=new GUIStyle(GUI.skin.label){wordWrap=true,normal={textColor=Color.white}};
            }
            if(Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.Escape){CancelTool();toolPage=ToolPage.None;Event.current.Use();}
            var oldContent=GUI.contentColor;GUI.contentColor=Color.white;
            DrawCityStatus();
            HudBox(ToolbarBounds);GUILayout.BeginArea(new Rect(ToolbarBounds.x+6,ToolbarBounds.y+7,ToolbarBounds.width-12,32));GUILayout.BeginHorizontal();
            for(int i=0;i<PageTitles.Length;i++)
            {
                var page=(ToolPage)(i+1);var tint=GUI.backgroundColor;if(toolPage==page)GUI.backgroundColor=new Color(.3f,.8f,1);
                if(GUILayout.Button(localization.Get(PageTitles[i]),GUILayout.Height(28)))SelectPage(page);GUI.backgroundColor=tint;
            }
            GUILayout.EndHorizontal();GUILayout.EndArea();
            HudBox(NoticeBounds);GUILayout.BeginArea(new Rect(NoticeBounds.x+8,NoticeBounds.y+4,NoticeBounds.width-16,NoticeBounds.height-8));
            GUILayout.BeginHorizontal();GUILayout.Label(ActiveTool,heading);if(GUILayout.Button(localization.Get("ui.hud.cancel"),GUILayout.Width(76)))CancelTool();GUILayout.EndHorizontal();
            GUILayout.Label((building||utilityTool.HasValue)&&!string.IsNullOrEmpty(previewText)?previewText:notice,wrapped);GUILayout.EndArea();
            if(toolPage!=ToolPage.None)DrawToolPage();
            outsideViews?.Draw(cameraView,MenuBounds);roadEditor?.Bind(session);roadEditor?.Draw();observation?.Bind(session);observation?.Draw();financePanel?.Draw(session?.Simulation);
            GUI.contentColor=oldContent;
        }
        void DrawCityStatus()
        {
            HudBox(StatusBounds);GUILayout.BeginArea(new Rect(StatusBounds.x+10,StatusBounds.y+8,StatusBounds.width-20,StatusBounds.height-16));
            GUILayout.Label(localization.Get("ui.hud.population",(session?.Entities.Members.Count??0),world.ActiveCount),heading);
            GUILayout.Label(localization.Get("ui.hud.balance_time",(session==null?"—":session.Simulation.Finance.UnlimitedFunds?"∞":session.Simulation.Finance.Money.ToString()),world.Clock.ToString("F1")),hudText);
            GUILayout.BeginHorizontal();
            foreach(float rate in new[]{0f,1f,3f,20f})
            {
                var tint=GUI.backgroundColor;if(speed==rate)GUI.backgroundColor=new Color(.3f,.8f,1);
                if(GUILayout.Button(rate==0?localization.Get("ui.hud.pause"):rate+"×",GUILayout.Height(26)))ScenarioRate(rate);GUI.backgroundColor=tint;
            }
            GUILayout.EndHorizontal();
            GUILayout.Label(localization.Get("ui.hud.rate",speed,double.IsNaN(actualRate)?"…":actualRate.ToString("F1")),wrapped);
            GUILayout.EndArea();
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
            GUILayout.EndScrollView();GUILayout.EndArea();
        }
        bool GrowingCity=>session!=null&&session.City.development.enabled;
        void DrawRoadTools()
        {
            if(GUILayout.Button(building?localization.Get("ui.hud.end_road"):localization.Get("ui.hud.active.road"),GUILayout.Height(28)))
            {bool enable=!building;CancelTool();building=enable;notice=localization.Get(enable?"ui.build.choose_start":"ui.build.cancelled");}
            GUILayout.BeginHorizontal();oneWay=GUILayout.Toggle(oneWay,localization.Get("ui.hud.oneway"));wide=GUILayout.Toggle(wide,localization.Get("ui.hud.wide"));GUILayout.EndHorizontal();
            if(session!=null&&landscape!=null)
            {
                if(GUILayout.Button(localization.Get(gridTool.Enabled?"ui.grid.close":"ui.grid.open")))
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
                if(GUILayout.Button(use==LandUse.Residential?localization.Get("ui.hud.residential"):use==LandUse.Commercial?localization.Get("ui.hud.commercial"):use==LandUse.Industrial?localization.Get("ui.hud.industrial"):localization.Get("ui.hud.erase"),GUILayout.Height(28)))
                {CancelTool();zoneTool=use;notice=localization.Get("ui.hud.zone_help");}
            if(zoneTool!=LandUse.Empty)zoneBoxTool.DrawControls();
        }
        void DrawUtilityTools()
        {
            if(!GrowingCity){GUILayout.Label(localization.Get("ui.hud.no_utilities"));return;}
            foreach(var use in new[]{LandUse.Power,LandUse.Water,LandUse.Sewage,LandUse.Landfill})
                if(GUILayout.Button(use==LandUse.Power?localization.Get("ui.hud.power"):use==LandUse.Water?localization.Get("ui.hud.water"):use==LandUse.Sewage?localization.Get("ui.hud.sewage"):localization.Get("ui.waste.facility"),GUILayout.Height(26)))
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
