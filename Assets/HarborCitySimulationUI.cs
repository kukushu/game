using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace HarborCity
{
    public sealed partial class HarborCityGame
    {
        bool showSimulation;
        int simulationTab;
        Vector2 simulationScroll;
        
        bool simulationTyping;
        int inspectedHome=-1;
        Vector2 householdListScroll;
        
        bool InspectBuilding(Ray ray)
        {
            inspectedHome=-1;
            inspectedResident=-1; followResident=false;
            commuteFamily=-1;
            if(city.society==null) return false;
            float nearest=float.MaxValue;
            foreach(var pair in visuals)
            {
                if(!InspectableBuilding(pair.Key) || pair.Value==null) continue;
                var collider=pair.Value.GetComponentInChildren<BoxCollider>();
                if(collider!=null && collider.Raycast(ray,out var hit,1000) && hit.distance<nearest)
                {nearest=hit.distance; inspectedHome=pair.Key;}
            }
            // Empty/zoned lots remain selectable even before a building mesh exists.
            if(inspectedHome<0 && landscape.Raycast(ray,out var ground))
            {
                int id=city.PickBuilding(ground.x,ground.z);
                if(InspectableBuilding(id)) inspectedHome=id;
            }
            if(inspectedHome<0) return false;
            inspectedTrip=-1; showSimulation=false; simulationTyping=false; householdListScroll=Vector2.zero;
            if(routeLine!=null) routeLine.positionCount=0;
            notice="已选中"+names[(int)city.UseOf(inspectedHome)]+" #"+inspectedHome+(city.UseOf(inspectedHome)==LandUse.Residential?"，点击住户查看家庭详情。":"，查看经营、库存和员工情况。");
            var first=city.society.families.Find(f=>f.resident && f.home==inspectedHome && HasWorkplace(f));
            if(first!=null) ShowWorkplace(first,false);
            return true;
        }
        bool InspectableBuilding(int id) => city.GetBuilding(id)!=null && (int)city.UseOf(id)>=2 && (int)city.UseOf(id)<=4;
        bool InspectingBusiness => selected==LandUse.Empty && InspectableBuilding(inspectedHome) && (int)city.UseOf(inspectedHome)>=3;
        LineRenderer businessMarker;
        void UpdateBusinessMarker()
        {
            if(!InspectingBusiness)
            {if(businessMarker!=null) businessMarker.positionCount=0; return;}
            if(businessMarker==null) businessMarker=CommuteRenderer("Selected business",new Color(1,.72f,.15f),.2f);
            MarkBuilding(businessMarker,inspectedHome);
        }
        void DrawBusinessInspector(float h,Color navy)
        {
            var state=city.analysis?.State;var business=state?.Business(inspectedHome);
            if(business==null)return;
            Panel(new Rect(24,120,310,h-320),navy);
            GUILayout.BeginArea(new Rect(38,132,282,h-345));
            GUILayout.BeginHorizontal();GUILayout.Label((business.industrial?"工厂":"商业")+" #"+business.id,title);
            if(GUILayout.Button("关闭",button,GUILayout.Width(60)))inspectedHome=-1;
            GUILayout.EndHorizontal();
            householdListScroll=GUILayout.BeginScrollView(householdListScroll);
            DrawBusinessSummary(business);
            if(GUILayout.Button("查看经营原因 / 员工 / 货运",button))OpenAnalysisEntity(business.industrial?AnalysisEntityKind.Factory:AnalysisEntityKind.Commercial,business.id);
            GUILayout.EndScrollView();GUILayout.EndArea();
        }
        void DrawHouseholdInspector(float h,Color navy)
        {
            if(selected!=LandUse.Empty || inspectedHome<0)return;
            if(InspectingBusiness) {DrawBusinessInspector(h,navy);return;}
            var state=city.analysis?.State;var housing=state?.housing.Find(r=>r.id==inspectedHome);
            if(housing==null)return;
            Panel(new Rect(24,120,310,h-320),navy);
            GUILayout.BeginArea(new Rect(38,132,282,h-345));
            GUILayout.BeginHorizontal();GUILayout.Label("住宅 #"+housing.id,title);
            if(GUILayout.Button("关闭",button,GUILayout.Width(60)))inspectedHome=-1;
            GUILayout.EndHorizontal();
            Text((housing.kind==HousingKind.Villa?"小别墅":"公寓")+" · 入住 "+housing.occupied+" / "+housing.units+" 户\n居民 "+housing.population+" 人 · 挂牌租金 "+housing.rent+"\n"+(housing.connected?"道路已接通":"道路未接通"));
            householdListScroll=GUILayout.BeginScrollView(householdListScroll);
            foreach(var family in state.households.Where(f=>f.resident && f.home==housing.id).OrderBy(f=>f.unit))
            {
                EntityButton("单元 "+(family.unit+1)+" · 家庭 #"+family.id+" · "+family.members.Count+" 人",AnalysisEntityKind.Household,family.id);
                Text("合同日薪 "+Money(family.expectedIncome)+" · 租金 "+Money(family.rent)+"\n储蓄 "+Money(family.savings)+" · 就业 "+family.employed+"\n成员通勤合计 "+Minutes(family.commute));
                if(family.employed>0)
                {
                    GUILayout.BeginHorizontal();
                    if(GUILayout.Button("显示路线",button))ShowWorkplace(city.society.families.Find(f=>f.id==family.id),false);
                    if(GUILayout.Button("定位工作",button))ShowWorkplace(city.society.families.Find(f=>f.id==family.id),true);
                    GUILayout.EndHorizontal();
                }
                foreach(var p in family.members)EntityButton(p.name+" #"+p.id+" · "+p.activity,AnalysisEntityKind.Resident,p.id);
                GUILayout.Space(8);
            }
            if(housing.occupied==0)Text("暂时没有住户，等待家庭选择入住。");
            GUILayout.EndScrollView();GUILayout.EndArea();
        }
        void AdvanceSociety(float seconds) {traffic.Advance(seconds);}
        void DrawHousing(ResidentialBuilding b,Transform parent)
        {
            bool villa=b.housing==HousingKind.Villa;
            float height=villa?1.7f:4.8f;
            float width=villa?3.7f:2.4f, depth=b.depth>3?4.2f:2.3f;
            var selection=parent.gameObject.AddComponent<BoxCollider>();
            selection.center=new Vector3(0,height/2+.15f,0);
            selection.size=new Vector3(width+.2f,height+.6f,depth+.2f);
            Box(villa?"Villa":"Apartments",new Vector3(0,height/2+.1f,0),new Vector3(width,height,depth),new Color(.83f,.82f,.73f),parent);
            Box("Roof",new Vector3(0,height+.22f,0),new Vector3(width+.2f,.25f,depth+.2f),villa?new Color(.52f,.3f,.24f):palette[2],parent);
            for(float y=.7f;y<height;y+=1.1f) Box("Windows",new Vector3(0,y,-depth/2-.02f),new Vector3(width*.75f,.4f,.03f),new Color(.24f,.38f,.43f),parent);
        }
        void ToggleSimulationPanel()
        {
            showSimulation=!showSimulation;
            simulationTyping=false;
            if(showSimulation) {simulationTab=0;analysisEntityId=-1;simulationScroll=Vector2.zero;RefreshAnalysis();}
        }
        void DrawSimulationPanel(float w,float h,Color navy)
        {
            if(city.society==null)return;
            if(!help)DrawHouseholdInspector(h,navy);
            if(GUI.Button(new Rect(w-274,448,250,34),showSimulation?"F3  关闭城市 Dashboard":"F3  打开城市 Dashboard",button))ToggleSimulationPanel();
            if(selected==LandUse.Residential)
            {
                Panel(new Rect(24,380,310,119),navy);
                if(GUI.Button(new Rect(40,388,135,32),"公寓 · 8 户",button))housingChoice=HousingKind.Apartment;
                if(GUI.Button(new Rect(181,388,135,32),"别墅 · 1 户",button))housingChoice=HousingKind.Villa;
                var config=city.society.settings;
                GUI.Label(new Rect(40,426,275,65),(housingChoice==HousingKind.Villa?"别墅：¥"+config.villaCost+" / 维护 "+config.villaMaintenance:"公寓：¥"+config.apartmentCost+" / 维护 "+config.apartmentMaintenance)+" 每日\n空置也维护；租金按实际入住结算。",small);
            }
            if(!showSimulation) {simulationTyping=false;return;}
            Rect rect=new Rect(340,110,w-640,h-305);Panel(rect,navy);
            GUILayout.BeginArea(new Rect(rect.x+14,rect.y+8,rect.width-28,rect.height-16));
            GUILayout.Label("F3 · 城市运行 Dashboard",title);
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("暂停",button))speed=0;
            if(GUILayout.Button("推进 1 天",button)) {speed=0;AdvanceSociety(city.society.settings.secondsPerDay-city.society.dayElapsed);RefreshAnalysis();}
            if(GUILayout.Button("20×",button))speed=20;
            if(GUILayout.Button("导出分析 JSON",button))ExportAnalysis();
            GUILayout.EndHorizontal();
            int tab=GUILayout.Toolbar(simulationTab,new[]{"Dashboard","家庭","经营","History","时间线","居民","Debug"},button);
            if(tab!=simulationTab) {simulationTab=tab;analysisEntityId=-1;simulationScroll=Vector2.zero;}
            simulationScroll=GUILayout.BeginScrollView(simulationScroll);
            var state=city.analysis?.State;
            if(city.analysis?.Failure!=null)GUILayout.Label("分析失败："+city.analysis.Failure,small);
            if(state==null)GUILayout.Label("分析状态尚未准备完成。",small);
            else if(analysisEntityId>=0)DrawAnalysisDetail(state);
            else if(simulationTab==0)DrawDashboard(state);
            else if(simulationTab==1)DrawAnalysisFamilies(state);
            else if(simulationTab==2)DrawAnalysisBusinesses(state);
            else if(simulationTab==3)DrawAnalysisHistory(state);
            else if(simulationTab==4)DrawAnalysisTimeline(state);
            else if(simulationTab==5)DrawAnalysisResidents(state);
            else DrawAnalysisDebug(state);
            simulationTyping=GUI.GetNameOfFocusedControl()=="AnalysisSearch";
            GUILayout.EndScrollView();GUILayout.EndArea();
        }
        void ExportSimulation()
        {
            try
            {
                string directory=Path.Combine(Application.persistentDataPath,"SimulationReports"); Directory.CreateDirectory(directory);
                string path=Path.Combine(directory,"city-day-"+city.day+"-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".json");
                File.WriteAllText(path,JsonUtility.ToJson(city.ToSaveData(),true)); notice="模拟快照已导出："+path;
                var csv=new System.Text.StringBuilder("day,households,population,units,employed,commute,rent,maintenance,wages,moved,arrived,left,treasuryError,householdError\n");
                foreach(var r in city.society.history)
                    csv.AppendLine(string.Join(",",new[]{r.day.ToString(),r.households.ToString(),r.population.ToString(),r.units.ToString(),r.employed.ToString(),
                        r.averageCommute.ToString("F2",System.Globalization.CultureInfo.InvariantCulture),r.rent.ToString(),r.maintenance.ToString(),r.wages.ToString(),r.moved.ToString(),r.arrived.ToString(),r.left.ToString(),
                        (r.closingTreasury-r.openingTreasury-r.rent+r.maintenance).ToString(),(r.closingSavings-r.openingSavings-r.wages+r.living+r.travel+r.rent+r.movingCosts).ToString()}));
                File.WriteAllText(Path.ChangeExtension(path,"csv"),csv.ToString(),new System.Text.UTF8Encoding(true));
            }
            catch(Exception e) {notice="导出失败："+e.Message;}
        }
    }
}
