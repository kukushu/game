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
        const float ResidenceInspectorWidth=414;
        int residenceFamily=-1,residenceSnapshotHome=-1;
        CityAnalysisState residenceSnapshot;
        ResidencePresentation residenceView;
        bool residenceJumpToDetail;
        
        bool InspectBuilding(Ray ray)
        {
            inspectedHome=-1;
            residenceFamily=-1;
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
            notice="已选中"+names[(int)city.UseOf(inspectedHome)]+" #"+inspectedHome+(city.UseOf(inspectedHome)==LandUse.Cemetery?"，查看实际遗体、预留位置和灵车。":city.UseOf(inspectedHome)==LandUse.PoliceStation?"，查看实际治安和警车出勤。":city.UseOf(inspectedHome)==LandUse.FireHouse?"，查看实际火灾和消防车出勤。":city.GetBuilding(inspectedHome) is SchoolBuilding?"，查看真实学生、学位和学习情况。":city.UseOf(inspectedHome)==LandUse.Clinic?"，查看真实患者、预约和救护车。":city.UseOf(inspectedHome)==LandUse.Residential?"，点击住户查看家庭详情。":"，查看经营、库存和员工情况。");
            return true;
        }
        bool InspectableBuilding(int id) => city.GetBuilding(id)!=null && ((int)city.UseOf(id)>=2 && (int)city.UseOf(id)<=4 || city.UseOf(id)==LandUse.Landfill || city.UseOf(id)==LandUse.Clinic || city.GetBuilding(id) is SchoolBuilding || city.UseOf(id)==LandUse.FireHouse || city.UseOf(id)==LandUse.PoliceStation || city.UseOf(id)==LandUse.Cemetery);
        bool InspectingBusiness => selected==LandUse.Empty && (city.UseOf(inspectedHome)==LandUse.Commercial || city.UseOf(inspectedHome)==LandUse.Industrial);
        LineRenderer businessMarker;
        void UpdateBusinessMarker()
        {
            if(!InspectingBusiness && !InspectingLandfill && city.UseOf(inspectedHome)!=LandUse.Clinic && !(city.GetBuilding(inspectedHome) is SchoolBuilding) && city.UseOf(inspectedHome)!=LandUse.FireHouse && city.UseOf(inspectedHome)!=LandUse.PoliceStation && city.UseOf(inspectedHome)!=LandUse.Cemetery)
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
            if(city.development.enabled)GUILayout.Label("等级 "+city.GetBuilding(business.id).level+" · 土地价值 "+city.LandValue(business.id).ToString("F0")+"\n"+city.BuildingUpgradeReason(business.id),small);
            if(GUILayout.Button("查看经营原因 / 员工 / 货运",button))OpenAnalysisEntity(business.industrial?AnalysisEntityKind.Factory:AnalysisEntityKind.Commercial,business.id);
            GUILayout.EndScrollView();GUILayout.EndArea();
        }
        void DrawHouseholdInspector(float h,Color navy)
        {
            if(selected!=LandUse.Empty || inspectedHome<0)return;
            if(city.UseOf(inspectedHome)==LandUse.Clinic){DrawClinicInspector(h,navy);return;}
            if(city.UseOf(inspectedHome)==LandUse.Cemetery){DrawCemeteryInspector(h,navy);return;}
            if(city.GetBuilding(inspectedHome) is SchoolBuilding){DrawSchoolInspector(h,navy);return;}
            if(city.UseOf(inspectedHome)==LandUse.PoliceStation){DrawPoliceInspector(h,navy);return;}
            if(city.UseOf(inspectedHome)==LandUse.FireHouse){DrawFireInspector(h,navy);return;}
            if(InspectingLandfill){DrawLandfillInspector(h,navy);return;}
            if(InspectingBusiness) {DrawBusinessInspector(h,navy);return;}
            if(showSimulation)return;
            var state=city.analysis?.State;var housing=state?.housing.Find(r=>r.id==inspectedHome);
            if(housing==null)return;
            Panel(new Rect(24,120,ResidenceInspectorWidth,h-320),navy);
            GUILayout.BeginArea(new Rect(38,132,ResidenceInspectorWidth-28,h-345));
            DrawResidenceContents(state,housing);
            GUILayout.EndArea();
        }
        void DrawResidenceContents(CityAnalysisState state,HousingAnalysisState housing)
        {
            DashboardStyles();
            if(residenceSnapshotHome!=housing.id) {residenceFamily=-1;householdListScroll=Vector2.zero;}
            if(!ReferenceEquals(residenceSnapshot,state) || residenceSnapshotHome!=housing.id)
            {residenceSnapshot=state;residenceSnapshotHome=housing.id;residenceView=new ResidencePresentation(state,housing.id);}
            if(residenceView.Selected(residenceFamily)==null)residenceFamily=-1;
            GUILayout.BeginHorizontal();GUILayout.Label("住宅 #"+housing.id+" / "+(city.development.enabled?"低密度住宅 L"+city.GetBuilding(housing.id).level:housing.kind==HousingKind.Villa?"别墅":"公寓"),dashHeading);
            if(GUILayout.Button("关闭",button,GUILayout.Width(60)))inspectedHome=-1;
            GUILayout.EndHorizontal();
            Rect road=Block(24);StatusBadge(new Rect(road.x,road.y,110,21),housing.connected?"道路已接通":"道路未接通",housing.connected?DashboardTone.Positive:DashboardTone.Critical);
            Ink(new Rect(road.x+120,road.y,road.width-120,23),"空置 "+Math.Max(0,housing.units-housing.occupied)+" 户 · 失业劳动者 "+residenceView.unemployed+" 人",dashNote,residenceView.unemployed>0?dashAmber:dashBlue);
            if(city.development.enabled) {Rect utility=Block(24);StatusBadge(utility,city.BuildingConditionLabel(housing.id),city.GetBuilding(housing.id).abandoned?DashboardTone.Critical:city.HasBasicServices(housing.id)?DashboardTone.Positive:DashboardTone.Attention);}
            Rect row=Block(85);
            ResidenceMetric(CardCell(row,0,3),"已入住 / 单元",housing.occupied+" / "+housing.units,"空置 "+Math.Max(0,housing.units-housing.occupied)+" 户",residenceView.OccupancyTone,housing.occupied,housing.units);
            ResidenceMetric(CardCell(row,1,3),"居民总数",housing.population.ToString(),"当前住户",DashboardTone.Neutral);
            ResidenceMetric(CardCell(row,2,3),"挂牌日租金",Money(housing.rent),"每单元 / 日",DashboardTone.Neutral);
            GUILayout.Space(6);row=Block(85);
            ResidenceMetric(CardCell(row,0,3),"总日租金",Money(residenceView.contractRent),"合同应收，非实收",DashboardTone.Neutral);
            ResidenceMetric(CardCell(row,1,3),"就业 / 劳动力",residenceView.employed+" / "+residenceView.workforce,"劳动者 "+residenceView.workforce+" 人",residenceView.unemployed>0?DashboardTone.Attention:DashboardTone.Neutral,residenceView.employed,residenceView.workforce);
            var commute=residenceView.actualCommute;
            ResidenceMetric(CardCell(row,2,3),"实际通勤 · 平均 / 最长",commute.samples==0?"—":commute.Average.ToString("0.#")+" / "+commute.longest.ToString("0.#"),commute.samples+" 人样本 · 分钟",commute.Long?DashboardTone.Attention:DashboardTone.Neutral);
            GUILayout.Space(6);
            if(residenceJumpToDetail && Event.current.type==EventType.Layout) {householdListScroll.y=39+residenceView.families.Count*160;residenceJumpToDetail=false;}
            householdListScroll=GUILayout.BeginScrollView(householdListScroll);
            SectionCard("家庭列表 · "+residenceView.families.Count+" 户");
            foreach(var family in residenceView.families)DrawResidenceFamilyCard(family);
            if(residenceView.families.Count==0)Text("暂时没有住户，等待家庭选择入住。");
            var chosen=residenceView.Selected(residenceFamily);
            if(chosen==null) {GUILayout.Space(8);Text("选择一个家庭查看成员；默认不展开成员。");}
            else DrawResidenceFamilyDetail(chosen);
            GUILayout.EndScrollView();
        }
        void ResidenceMetric(Rect r,string caption,string value,string note,DashboardTone tone,double amount=0,double capacity=0)
        {
            Fill(r,dashSurface);Fill(new Rect(r.x,r.y,3,r.height),ToneColor(tone));
            Ink(new Rect(r.x+8,r.y+5,r.width-14,19),caption,dashNote);
            int size=dashValue.fontSize;dashValue.fontSize=21;
            float width=dashValue.CalcSize(new GUIContent(value)).x;if(width>r.width-14)dashValue.fontSize=Math.Max(13,(int)(21*(r.width-14)/width));
            Ink(new Rect(r.x+8,r.y+25,r.width-14,30),value,dashValue,ToneColor(tone),value);dashValue.fontSize=size;
            Ink(new Rect(r.x+8,r.y+56,r.width-14,21),note,dashNote,tooltip:note);
            if(capacity>0)ProgressBar(new Rect(r.x+8,r.y+79,r.width-16,3),amount,capacity,ToneColor(tone));
        }
        void DrawResidenceFamilyCard(HouseholdAnalysisState family)
        {
            int unemployed=ResidencePresentation.Unemployed(family);
            var commute=CommutePresentation.For(family);
            bool longCommute=commute.Long,chosen=residenceFamily==family.id;
            Rect r=Block(153);Fill(r,dashSurface);Fill(new Rect(r.x,r.y,3,r.height),chosen?dashGreen:unemployed>0||longCommute?dashAmber:dashBlue);
            Ink(new Rect(r.x+10,r.y+7,r.width-(unemployed>0?115:20),22),"家庭 #"+family.id+" · 单元 "+(family.unit+1)+" · "+family.members.Count+" 人",dashHeading,chosen?dashGreen:dashBlue);
            float half=(r.width-24)/2;
            ResidenceCardField(r,half,0,0,"储蓄",Money(family.savings));
            ResidenceCardField(r,half,1,0,"合同日薪",Money(family.expectedIncome));
            ResidenceCardField(r,half,0,1,"日房租",Money(family.rent));
            ResidenceCardField(r,half,1,1,"已就业",family.employed+" 人");
            Ink(new Rect(r.x+10,r.y+81,r.width-20,21),commute.Summary+(longCommute?" · 最长≥120分":""),dashCaption,longCommute?dashAmber:dashBlue);
            if(unemployed>0)StatusBadge(new Rect(r.x+r.width-95,r.y+7,85,21),"失业 "+unemployed+" 人",DashboardTone.Attention);
            float y=r.y+115;
            if(GUI.Button(new Rect(r.x+10,y,100,26),chosen?"已选中":"查看详情",button)) {residenceFamily=family.id;residenceJumpToDetail=true;}
            GUILayout.Space(7);
        }
        void ResidenceCardField(Rect card,float half,int column,int line,string name,string value)
        {
            float x=card.x+10+column*(half+4),y=card.y+34+line*23;
            Ink(new Rect(x,y,58,21),name,dashNote);Ink(new Rect(x+60,y,half-60,21),value,dashCaption,tooltip:value);
        }
        void DrawResidenceFamilyDetail(HouseholdAnalysisState family)
        {
            SectionCard("选中家庭 #"+family.id+" · 成员详情");
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("返回家庭列表",button)) {residenceFamily=-1;householdListScroll=Vector2.zero;}
            if(GUILayout.Button("决策 / 候选方案",button))OpenAnalysisEntity(AnalysisEntityKind.Household,family.id);
            GUILayout.EndHorizontal();
            FieldRow("最近日结工资",Money(family.income));
            FieldRow("实际通勤",CommutePresentation.For(family).Summary);
            FieldRow("家庭通勤成本合计",family.commute.ToString("0.#")+" 方案分钟");
            if(family.arrears>0)FieldRow("租金欠款",Money(family.arrears),DashboardTone.Attention);
            foreach(var p in family.members)
            {
                Rect r=Block(p.jobId>0?132:101);Fill(r,dashSurface);
                bool unemployed=p.canWork && p.jobId<=0;
                Ink(new Rect(r.x+10,r.y+6,r.width-125,23),p.name+" #"+p.id,dashHeading);
                StatusBadge(new Rect(r.x+r.width-108,r.y+7,98,21),unemployed?"失业":p.activity,unemployed?DashboardTone.Attention:p.atWork?DashboardTone.Positive:DashboardTone.Neutral);
                Ink(new Rect(r.x+10,r.y+35,r.width-20,21),"工作地点："+(p.jobId<=0?"无岗位":p.work<0?"城外":"建筑 #"+p.work),dashCaption);
                Ink(new Rect(r.x+10,r.y+60,r.width-120,21),"最近实到通勤："+(p.hasCommuteObservation?p.commute.ToString("0.#")+" 分钟":"暂无样本"),dashNote);
                if(GUI.Button(new Rect(r.x+r.width-98,r.y+62,88,26),"居民详情",button))OpenAnalysisEntity(AnalysisEntityKind.Resident,p.id);
                if(p.jobId>0)
                {
                    bool enabled=GUI.enabled;GUI.enabled=enabled && p.work>=0 && !dashboardPreviewOnly;
                    if(GUI.Button(new Rect(r.x+10,r.y+96,100,26),"显示路线",button))ShowResidentWorkplace(p.id,false);
                    if(GUI.Button(new Rect(r.x+118,r.y+96,100,26),"定位工作",button))ShowResidentWorkplace(p.id,true);
                    GUI.enabled=enabled;
                }
                GUILayout.Space(6);
            }
        }
        void AdvanceSociety(float seconds) {traffic.Advance(seconds);}
        void DrawHousing(ResidentialBuilding b,Transform parent)
        {
            bool villa=b.housing==HousingKind.Villa;
            float growth=city.development.enabled?b.level-1:0;
            float height=(villa?1.7f:4.8f)+growth*.3f;
            float width=Mathf.Min(b.width-.5f,villa?3.7f+growth*.15f:2.4f), depth=Mathf.Min(b.depth-.5f,b.depth>3?4.2f:2.3f);
            var selection=parent.gameObject.AddComponent<BoxCollider>();
            selection.center=new Vector3(0,height/2+.15f,0);
            selection.size=new Vector3(width+.2f,height+.6f,depth+.2f);
            Box(villa?"Villa":"Apartments",new Vector3(0,height/2+.1f,0),new Vector3(width,height,depth),Color.Lerp(new Color(.83f,.82f,.73f),new Color(.92f,.9f,.84f),growth/4),parent);
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
                var csv=new System.Text.StringBuilder("day,households,population,units,employed,commute,rent,tax,maintenance,wages,moved,arrived,left,loanPayment,treasuryError,householdError\n");
                foreach(var r in city.society.history)
                    csv.AppendLine(string.Join(",",new[]{r.day.ToString(),r.households.ToString(),r.population.ToString(),r.units.ToString(),r.employed.ToString(),
                        r.averageCommute.ToString("F2",System.Globalization.CultureInfo.InvariantCulture),r.rent.ToString(),r.tax.ToString(),r.maintenance.ToString(),r.wages.ToString(),r.moved.ToString(),r.arrived.ToString(),r.left.ToString(),
                        r.loanPayment.ToString(),(r.closingTreasury-r.openingTreasury-r.rent-r.tax+r.maintenance+r.loanPayment).ToString(),(r.closingSavings-r.openingSavings-r.wages+r.living+r.travel+r.rent+r.movingCosts).ToString()}));
                File.WriteAllText(Path.ChangeExtension(path,"csv"),csv.ToString(),new System.Text.UTF8Encoding(true));
            }
            catch(Exception e) {notice="导出失败："+e.Message;}
        }
    }
}
