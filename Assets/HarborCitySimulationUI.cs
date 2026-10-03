using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace HarborCity
{
    public sealed partial class HarborCityGame
    {
        bool showSimulation=true;
        int simulationTab, familyIndex;
        Vector2 simulationScroll;
        string familySearch="";
        bool simulationTyping;
        int inspectedHome=-1;
        Vector2 householdListScroll;
        int analysisView, analysisFactory=-1;
        bool InspectBuilding(Ray ray)
        {
            city.EnsureResidents();
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
            notice="已选中"+names[city.tiles[inspectedHome]]+" #"+inspectedHome+(city.tiles[inspectedHome]==2?"，点击住户查看家庭详情。":"，查看经营、库存和员工情况。");
            var first=city.society.families.Find(f=>f.resident && f.home==inspectedHome && HasWorkplace(f));
            if(first!=null) ShowWorkplace(first,false);
            return true;
        }
        bool InspectableBuilding(int id) => id>=0 && id<city.tiles.Length && city.tiles[id]>=2 && city.tiles[id]<=4;
        bool InspectingBusiness => selected==LandUse.Empty && InspectableBuilding(inspectedHome) && city.tiles[inspectedHome]>=3;
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
            int id=inspectedHome; bool industrial=city.tiles[id]==4;
            var f=city.Factory(id);
            var workers=city.Citizens.Where(p=>city.Workplace(p)==id).OrderBy(p=>p.id).ToList();
            var freight=traffic.State.trips.Where(t=>t.residentId==0 && t.purpose>=TripPurpose.Delivery && (t.home==id || t.origin==id || t.destination==id)).ToList();
            int goods=traffic.State.stock[id], inboundGoods=traffic.IncomingAmount(id,CargoKind.Goods);
            Panel(new Rect(24,120,310,h-320),navy);
            GUILayout.BeginArea(new Rect(38,132,282,h-345));
            GUILayout.BeginHorizontal();
            GUILayout.Label((industrial?"工厂":"商业")+" #"+id,title);
            bool close=GUILayout.Button("关闭",button,GUILayout.Width(60));
            GUILayout.EndHorizontal();
            if(close) inspectedHome=-1;
            GUILayout.Label(city.EntityRoadAccess(id)?"道路已接通":"道路未接通",small);
            householdListScroll=GUILayout.BeginScrollView(householdListScroll);
            if(industrial && f!=null)
            {
                GUILayout.Label("经营状态："+f.status,label);
                GUILayout.Label("现金 ¥"+f.cash.ToString("F2")+"\n累计经营盈余 ¥"+f.Profit.ToString("F2"),label);
                GUILayout.Label("累计销售收入 ¥"+f.salesRevenue.ToString("F2")+"\n原料支出 ¥"+f.rawCosts.ToString("F2")+"\n实付工资 ¥"+f.wageCosts.ToString("F2")+"\n加工成本 ¥"+f.productionCosts.ToString("F2"),small);
                GUILayout.Label("期初投入 ¥"+f.capital.ToString("F2")+(f.capitalFromTreasury?"（建设预算）":"（旧厂迁移）")+"\n未付出勤 ¥"+f.unpaidWages.ToString("F2")+"\n资金核对差额 "+f.CashError.ToString("F6"),small);
                GUILayout.Space(8);
                GUILayout.Label("生产与库存",label);
                GUILayout.Label("原料 "+f.raw+" / "+FactoryState.RawCapacity+" · 在途原料 "+traffic.IncomingAmount(id,CargoKind.RawMaterial)+"\n成品 "+goods+" / "+FactoryState.GoodsCapacity+"\n加工进度 "+f.progress.ToString("F1")+" / "+FactoryState.LabourPerItem+" 工人分钟\n累计产量 "+f.produced+" · 实售 "+f.sold+" 件",small);
                GUILayout.Label("累计在岗 "+f.attendanceMinutes.ToString("F0")+" / 加工 "+f.productiveMinutes.ToString("F0")+" 分钟\n噪声 "+f.noise.ToString("F2")+" / 污染 "+f.pollution.ToString("F2"),small);
                if(f.cash<=0) GUILayout.Label("资金耗尽：停薪并安排返家，等待实际售出库存恢复资金。",small);
                var daily=city.analysis?.Latest?.factories.Find(report=>report.id==id);
                if(daily!=null)
                {
                    GUILayout.Space(8);
                    GUILayout.Label("最近日报 Day "+city.analysis.Latest.day+"："+daily.mainBottleneck,small);
                    if(GUILayout.Button("查看本厂原因与日报",button))
                    {showSimulation=true; simulationTab=6; analysisView=1; analysisFactory=id; simulationScroll=Vector2.zero;}
                }
            }
            else
            {
                string state=!city.EntityRoadAccess(id)?"道路中断":goods==0?(inboundGoods>0?"缺货，等待补货到达":"缺货，等待配送或进口"):workers.Count==0?"有库存，暂无员工":"有库存";
                GUILayout.Label("经营状态："+state,label);
                GUILayout.Label("商品库存 "+goods+" / 32\n在途补货 "+inboundGoods+" 件",label);
                GUILayout.Label("库存低于 8 时安排配送或进口，到货后才入库。",small);
                GUILayout.Label("商业采购和工资使用简化外部资金，当前尚无独立现金、成本和利润账目。",small);
            }
            GUILayout.Space(8);
            GUILayout.Label("岗位与员工",label);
            GUILayout.Label("已雇 "+workers.Count+" / "+city.JobCapacity(id)+" · 资金可用岗位 "+city.society.jobEntities.Count(j=>j.buildingId==id && city.JobFunded(j))+"\n实际在岗 "+workers.Count(p=>p.atWork && p.tripId==0 && p.location==id)+" 人\n合同日薪 ¥"+city.Wage(id)+" · 技能要求 "+city.SkillRequired(id),small);
            if(workers.Count==0) GUILayout.Label("暂无员工。",small);
            foreach(var p in workers)
            {
                var family=city.society.families.Find(household=>household.id==p.householdId);
                var activity=city.ObserveResident(family,p);
                GUILayout.Label(p.name+" #"+p.id+" · "+activity.state+"\n今日在岗 "+p.workedMinutes.ToString("F1")+" 分钟 · 家庭 #"+p.householdId,small);
                GUI.enabled=activity.located;
                if(GUILayout.Button("查看居民："+p.name,button))
                    SelectPopulationResident(new PopulationRow {family=family,person=p,activity=activity},false);
                GUI.enabled=true;
            }
            GUILayout.Space(8);
            GUILayout.Label("相关货运",label);
            int shipping=freight.Where(t=>!t.returning && t.origin==id && t.cargoKind==CargoKind.Goods).Sum(t=>t.cargo);
            GUILayout.Label("待交付商品 "+shipping+" 件 · 相关货车 "+freight.Count+" 辆",small);
            if(freight.Count==0) GUILayout.Label("当前没有相关货运任务。",small);
            foreach(var t in freight)
            {
                string status=t.status==TripStatus.Visiting?"装卸停留":t.status==TripStatus.Waiting?"等待道路恢复":t.blocked>.1f?"排队":"行驶中";
                if(GUILayout.Button("货车 #"+t.id+" · "+Purpose(t),button))
                {inspectedHome=-1; inspectedResident=-1; followResident=false; commuteFamily=-1; inspectedTrip=t.id;}
                GUILayout.Label(status+" · 载货 "+t.cargo+"\n"+(t.origin==CityTraffic.Outside?"西侧城外入口":PopulationPlace(t.origin))+" → "+(t.destination==CityTraffic.Outside?"西侧城外入口":PopulationPlace(t.destination)),small);
            }
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
        void DrawHouseholdInspector(float h,Color navy)
        {
            if(selected!=LandUse.Empty || inspectedHome<0) return;
            if(InspectingBusiness) {DrawBusinessInspector(h,navy); return;}
            if(city.HousingCapacity(inspectedHome)==0) {inspectedHome=-1; return;}
            var b=city.buildings[inspectedHome];
            var occupants=city.society.families.Where(f=>f.resident && f.home==inspectedHome).OrderBy(f=>f.unit).ToList();
            Panel(new Rect(24,120,310,h-320),navy);
            GUILayout.BeginArea(new Rect(38,132,282,h-345));
            GUILayout.BeginHorizontal();
            GUILayout.Label("住宅 #"+inspectedHome,title);
            if(GUILayout.Button("关闭",button,GUILayout.Width(60))) inspectedHome=-1;
            GUILayout.EndHorizontal();
            GUILayout.Label((b.housing==HousingKind.Villa?"小别墅":"公寓")+" · 入住 "+occupants.Count+" / "+b.housingUnits+" 户",label);
            GUILayout.Label("居民 "+occupants.Sum(f=>f.people.Count)+" 人 · 空房 "+(b.housingUnits-occupants.Count)+" 套\n挂牌租金 ¥"+b.askingRent+" / 日 · "+(city.EntityRoadAccess(b.id)?"道路已接通":"道路未接通"),small);
            householdListScroll=GUILayout.BeginScrollView(householdListScroll);
            if(occupants.Count==0) GUILayout.Label("暂时没有住户。\n住房已提供容量，等待家庭选择入住。",small);
            foreach(var f in occupants)
            {
                if(GUILayout.Button("单元 "+(f.unit+1)+" · 家庭 #"+f.id+" · "+f.people.Count+" 人",button))
                {
                    familyIndex=city.society.families.IndexOf(f); familySearch=f.id.ToString();
                    simulationTab=1; simulationScroll=Vector2.zero; showSimulation=true;
                }
                float minutes=city.HouseholdCommute(f);
                GUILayout.Label("日薪 ¥"+city.HouseholdSalary(f)+" · 租金 ¥"+f.rent+"\n储蓄 ¥"+f.savings+" · "+"就业成员 "+f.people.Count(p=>city.ResidentJob(p)!=null)+"\n通勤："+(minutes<0?"不可达":minutes.ToString("F1")+" 分钟"),small);
                GUILayout.Label("工作目的地："+string.Join("、",f.people.Where(p=>city.ResidentJob(p)!=null).Select(p=>p.name+" → "+WorkplaceName(p)))+(commuteFamily==f.id?"（已标出）":""),small);
                if(HasWorkplace(f))
                {
                    GUILayout.BeginHorizontal();
                    if(GUILayout.Button("显示路线",button)) ShowWorkplace(f,false);
                    if(GUILayout.Button("定位工作",button)) ShowWorkplace(f,true);
                    GUILayout.EndHorizontal();
                    if(minutes<0) GUILayout.Label("工作地点仍在，但目前没有可用通勤连接。",small);
                }
                DrawResidents(f);
                GUILayout.Space(8);
            }
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
        void AdvanceSociety(float seconds)
        {
            if(city.society==null || seconds<=0) return;
            if(!city.society.transportEnabled) city.EnableResidentTransport();
            if(city.society.transportEnabled) {traffic.Advance(seconds); return;}
            city.society.dayElapsed+=seconds;
            while(city.society.dayElapsed>=city.society.settings.secondsPerDay)
            {
                city.society.dayElapsed-=city.society.settings.secondsPerDay;
                city.Tick();
            }
            traffic.Advance(seconds);
        }
        void DrawHousing(CityBuilding b,Transform parent)
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
        void DrawSimulationPanel(float w,float h,Color navy)
        {
            if(city.society==null) return;
            if(!help) DrawHouseholdInspector(h,navy);
            if(GUI.Button(new Rect(w-274,448,250,34),showSimulation?"F3  关闭模拟观察台":"F3  打开模拟观察台",button)) showSimulation=!showSimulation;
            if(selected==LandUse.Residential)
            {
                Panel(new Rect(24,380,310,119),navy);
                if(GUI.Button(new Rect(40,388,135,32),"公寓 · 8 户",button)) housingChoice=HousingKind.Apartment;
                if(GUI.Button(new Rect(181,388,135,32),"别墅 · 1 户",button)) housingChoice=HousingKind.Villa;
                var config=city.society.settings;
                GUI.Label(new Rect(40,426,275,65),(housingChoice==HousingKind.Villa?"别墅：¥"+config.villaCost+" / 维护 "+config.villaMaintenance:"公寓：¥"+config.apartmentCost+" / 维护 "+config.apartmentMaintenance)+" 每日\n空置也维护；租金按实际入住结算。",small);
            }
            if(!showSimulation) {simulationTyping=false; return;}
            Rect rect=new Rect(340,110,w-640,h-305); Panel(rect,navy);
            GUILayout.BeginArea(new Rect(rect.x+14,rect.y+8,rect.width-28,rect.height-16));
            GUILayout.Label("模拟观察台 · 家庭选择实验",title);
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("暂停",button)) speed=0;
            if(GUILayout.Button("推进 1 天",button)) {speed=0; AdvanceSociety(city.society.settings.secondsPerDay-city.society.dayElapsed);}
            if(GUILayout.Button("20× 观察",button)) speed=20;
            if(GUILayout.Button("导出快照",button)) ExportSimulation();
            if(GUILayout.Button("导出日志",button)) ExportSimulationLog();
            GUILayout.EndHorizontal();
            int newTab=GUILayout.Toolbar(simulationTab,new[]{"总览 / 账目","家庭 / 决策","住房 / 岗位","每日趋势","事件","人口 / 位置","城市分析"},button);
            if(newTab!=simulationTab) {simulationTab=newTab; simulationScroll=Vector2.zero;}
            simulationScroll=GUILayout.BeginScrollView(simulationScroll);
            var s=city.society; var residents=s.families.Where(f=>f.resident).ToList();
            if(simulationTab==0)
            {
                int units=Enumerable.Range(0,city.tiles.Length).Sum(city.HousingCapacity);
                GUILayout.Label("第 "+city.day+" 天 · 日内进度 "+(100*s.dayElapsed/s.settings.secondsPerDay).ToString("F0")+"% · 1× 每天 "+s.settings.secondsPerDay+" 秒",small);
                GUILayout.Label("家庭 "+residents.Count+" / 居民 "+city.population+" / 城外等待 "+s.families.Count(f=>!f.resident),label);
                if(s.transportEnabled)
                {
                    var commuters=traffic.State.trips.Where(t=>t.residentId>0).ToList();
                    GUILayout.Label("实际通勤任务 "+commuters.Count+" / 排队或断路 "+commuters.Count(t=>t.blocked>.1f || t.status==TripStatus.Waiting)+
                        " / 工作场所内 "+residents.Sum(h=>h.people.Count(p=>p.atWork && p.tripId==0))+" / 待入账工资 ¥"+residents.Sum(h=>h.people.Sum(p=>p.earnedWages+p.factoryWageCredit)).ToString("F0"),label);
                }
                GUILayout.Label("住房 "+units+" 套 / 空置 "+Math.Max(0,units-residents.Count)+" / 已就业 "+city.Employed+" / 岗位容量 "+city.jobs,label);
                GUILayout.Label("通勤车到达才算到岗；工厂工资由有限现金支付，耗尽后停薪并返家求职。商业工资、生活与通勤费用仍使用简化外部资金。",small);
                GUILayout.Label("每个道路出行任务都有对应车辆，不限制显示数量。住房选择参考本户最近实际通勤；未尝试的方案按畅通道路估算。偏好每 "+s.settings.reviewDays+" 天评估，租约 "+s.settings.leaseDays+" 天。",small);
                var r=s.history.LastOrDefault();
                if(r==null) GUILayout.Label("尚未日结。可先暂停，再点‘推进 1 天’观察现金流和决策。",small);
                else
                {
                    GUILayout.Label("最近日结：工资 +"+r.wages+"，生活 −"+r.living+"，通勤 −"+r.travel+"，实付租金 −"+r.rent+"，搬迁 −"+r.movingCosts,small);
                    GUILayout.Label("其中工厂实付工资 "+r.factoryWages+" / 商业与旧版已挣工资 "+r.externalWages,small);
                    GUILayout.Label("财政："+r.openingTreasury+" + 实收 "+r.rent+" − 维护 "+r.maintenance+" = "+r.closingTreasury,small);
                    int treasuryError=r.closingTreasury-(r.openingTreasury+r.rent-r.maintenance);
                    int householdError=r.closingSavings-(r.openingSavings+r.wages-r.living-r.travel-r.rent-r.movingCosts);
                    GUILayout.Label("核对差额：财政 "+treasuryError+" / 家庭 "+householdError+"（均应为 0）",label);
                    GUILayout.Label("新欠租 "+r.unpaidRent+"；迁入 "+r.arrived+" / 搬家 "+r.moved+" / 迁出 "+r.left+"。家庭期初储蓄包含当日外部申请者携入资金。",small);
                }
                GUILayout.Label("建议实验：记录一户 → 修通捷径 → 推进数天 → 比较通勤与候选评分。住房变满不立即涨旧租约。",small);
            }
            else if(simulationTab==1)
            {
                if(s.families.Count>0)
                {
                    GUILayout.BeginHorizontal();
                    if(GUILayout.Button("上一户",button)) familyIndex--;
                    if(GUILayout.Button("下一户",button)) familyIndex++;
                    if(GUILayout.Button("下一户有变动",button))
                    {for(int n=1;n<=s.families.Count;n++) {int candidate=(familyIndex+n+s.families.Count)%s.families.Count; if(s.families[candidate].lastMove>=city.day-7) {familyIndex=candidate; break;}}}
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("家庭编号",small,GUILayout.Width(75)); GUI.SetNextControlName("FamilySearch"); familySearch=GUILayout.TextField(familySearch,GUILayout.Width(100));
                    if(GUILayout.Button("查找",button,GUILayout.Width(75)) && int.TryParse(familySearch,out int requested))
                    {int found=s.families.FindIndex(f=>f.id==requested); if(found>=0) familyIndex=found;}
                    GUILayout.EndHorizontal();
                    familyIndex=(familyIndex%s.families.Count+s.families.Count)%s.families.Count;
                    var f=s.families[familyIndex];
                    if(f.home>=0 && GUILayout.Button("定位这户的住宅",button))
                    {var home=city.buildings[f.home]; focus=new Vector3(home.x,0,home.z); zoom=24; showSimulation=false; UpdateCamera();}
                    GUILayout.Label("家庭 #"+f.id+" · "+f.people.Count+" 人 · "+(f.resident?"本城住户":"城外申请者")+" · 技能 "+string.Join(" / ",f.people.Select(p=>p.skill)),label);
                    GUILayout.Label("住宅 #"+f.home+" / 单元 "+f.unit+" / 就业成员 "+f.people.Count(p=>city.ResidentJob(p)!=null)+" / 储蓄 ¥"+f.savings+" / 欠租 ¥"+f.arrears,small);
                    float minutes=city.HouseholdCommute(f);
                    GUILayout.Label("成员通勤合计 "+(minutes<0?"不可达":minutes.ToString("F1")+" 分钟")+" / 合同租金 "+f.rent+" / 薪资 "+city.HouseholdSalary(f)+" / 下次评估第 "+f.nextReview+" 天",small);
                    GUILayout.Label("偏好 0–1：空间 "+f.spacePreference.ToString("F2")+"  私密 "+f.privacyPreference.ToString("F2")+"  时间 "+f.timePreference.ToString("F2")+"  储蓄 "+f.savingPreference.ToString("F2"),small);
                    GUILayout.Label("最近决定："+f.reason,small);
                    GUILayout.Label("环境敏感度：噪声 "+f.noiseSensitivity.ToString("F2")+" / 污染 "+f.pollutionSensitivity.ToString("F2")+" / 货车 "+f.trafficSensitivity.ToString("F2"),small);
                    GUILayout.Label("第 "+f.evaluatedDay+" 天评估快照（0 为尚未评估）：正项为空间、私密、结余；负项为时间、变更成本",small);
                    foreach(var o in f.options)
                        GUILayout.Label("房 #"+o.home+" / 成员岗位 "+(o.jobIds==null?"旧记录":string.Join(",",o.jobIds))+"："+(o.rejection!=""?o.rejection:"总分 "+o.score.ToString("F1")+" = "+o.spaceScore.ToString("F1")+" + "+o.privacyScore.ToString("F1")+" + "+o.moneyScore.ToString("F1")+" − "+o.timeScore.ToString("F1")+" − "+o.changeCost+" − 环境 "+o.environmentCost.ToString("F1"))+"\n租 "+o.rent+" / 工资 "+o.wage+" / 结余 "+o.surplus+" / 通勤 "+o.minutes.ToString("F1")+" / 噪声、污染、货车 "+o.noise.ToString("F2")+" / "+o.pollution.ToString("F2")+" / "+o.heavyTraffic.ToString("F2"),small);
                }
            }
            else if(simulationTab==2)
            {
                for(int i=0;i<city.tiles.Length;i++)
                {
                    if(city.HousingCapacity(i)>0)
                    {
                        var b=city.buildings[i]; int actual=residents.Where(f=>f.home==i).Sum(f=>f.rentPaid);
                        GUILayout.Label("住宅 #"+i+" "+(b.housing==HousingKind.Villa?"别墅":b.housing==HousingKind.Legacy?"旧城公寓":"公寓")+" · "+city.Occupancy(i)+" / "+b.housingUnits+" 户 · 挂牌 "+b.askingRent+" · 上日实收 "+actual+" · 本周意向 "+b.applications+" · "+(city.EntityRoadAccess(i)?"临路":"断路"),small);
                    }
                    else if(city.JobCapacity(i)>0) GUILayout.Label("工作场所 #"+i+" · 已雇 "+city.EmployedAt(i)+" / "+city.JobCapacity(i)+" · 有资金的岗位 "+s.jobEntities.Count(j=>j.buildingId==i && city.JobFunded(j))+" · 合同日薪 "+city.Wage(i)+" · 技能要求 "+city.SkillRequired(i),small);
                    var factory=city.Factory(i);
                    if(factory!=null)
                    {
                        GUILayout.Label("工厂 #"+i+" · "+factory.status+" · 原料 "+factory.raw+" / 24 · 成品 "+city.traffic.stock[i]+" / 24 · 加工进度 "+factory.progress.ToString("F1")+" / 60 工人分钟",small);
                        GUILayout.Label("累计：在岗 "+factory.attendanceMinutes.ToString("F0")+" 分钟 / 有效加工 "+factory.productiveMinutes.ToString("F0")+" 分钟 / 产量 "+factory.produced+" 件 · 噪声 "+factory.noise.ToString("F2")+" / 污染 "+factory.pollution.ToString("F2"),small);
                        GUILayout.Label("现金 ¥"+factory.cash.ToString("F2")+" · 已售 "+factory.sold+" 件 / 收入 ¥"+factory.salesRevenue.ToString("F2")+" · 期初投入 ¥"+factory.capital.ToString("F2"),small);
                        GUILayout.Label("累计支出：原料 ¥"+factory.rawCosts.ToString("F2")+" / 实付工资 ¥"+factory.wageCosts.ToString("F2")+" / 加工 ¥"+factory.productionCosts.ToString("F2")+" · 盈余 ¥"+factory.Profit.ToString("F2")+" · 未付出勤 ¥"+factory.unpaidWages.ToString("F2")+" · 资金核对 "+factory.CashError.ToString("F6"),small);
                    }
                }
            }
            else if(simulationTab==3)
            {
                GUILayout.Label("天 | 户数 / 空房 | 就业 | 平均通勤 | 租金 / 维护 | 迁入 / 搬家 / 迁出",small);
                foreach(var r in s.history.AsEnumerable().Reverse()) GUILayout.Label(r.day+" | "+r.households+" / "+(r.units-r.households)+" | "+r.employed+" | "+r.averageCommute.ToString("F1")+" | "+r.rent+" / "+r.maintenance+" | "+r.arrived+" / "+r.moved+" / "+r.left,small);
            }
            else if(simulationTab==5) DrawPopulationPanel();
            else if(simulationTab==6) DrawAnalysisPanel();
            else foreach(string e in s.events.AsEnumerable().Reverse()) GUILayout.Label(e,small);
            simulationTyping=simulationTab==1 && GUI.GetNameOfFocusedControl()=="FamilySearch"
                || simulationTab==5 && GUI.GetNameOfFocusedControl()=="PopulationSearch";
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
        void DrawAnalysisPanel()
        {
            var report=city.analysis?.Latest;
            if(city.analysis?.Failure!=null) {GUILayout.Label("分析报告不可用："+city.analysis.Failure,small); return;}
            if(report==null)
            {GUILayout.Label("尚无本会话已结束的日报。可暂停后推进一天生成日报；导出日志也会生成当前未结束日的分析快照。中途开始记录会明确标为部分日。",small); return;}
            GUILayout.Label("最近结束：Day "+report.day+" · 当前模拟 Day "+city.day+"；完整报告随日志导出到 Analysis 文件夹。",small);
            int view=GUILayout.Toolbar(analysisView,new[]{"城市日报","工厂日报","家庭决策摘要"},button);
            if(view!=analysisView) {analysisView=view; analysisFactory=-1;}
            if(analysisView==1 && analysisFactory>=0 && GUILayout.Button("显示全部工厂",button)) analysisFactory=-1;
            string markdown=analysisView==0?CityDailyAnalysis.RenderCity(report):analysisView==1?CityDailyAnalysis.RenderFactories(report,analysisFactory):CityDailyAnalysis.RenderHouseholds(report);
            string plain=string.Join("\n",markdown.Split('\n').Select(line=>line.StartsWith("#",StringComparison.Ordinal)?line.TrimStart('#',' '):line.Replace("**","")));
            GUILayout.Label(plain,small);
        }
        void ExportSimulation()
        {
            try
            {
                string directory=Path.Combine(Application.persistentDataPath,"SimulationReports"); Directory.CreateDirectory(directory);
                string path=Path.Combine(directory,"city-day-"+city.day+"-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".json");
                File.WriteAllText(path,JsonUtility.ToJson(city,true)); notice="模拟快照已导出："+path;
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
