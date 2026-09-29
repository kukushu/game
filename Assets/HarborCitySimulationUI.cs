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
        bool InspectHousing(Ray ray)
        {
            city.EnsureResidents();
            inspectedHome=-1;
            inspectedResident=-1; followResident=false;
            commuteFamily=-1;
            if(city.society==null) return false;
            float nearest=float.MaxValue;
            foreach(var pair in visuals)
            {
                if(city.HousingCapacity(pair.Key)==0 || pair.Value==null) continue;
                var collider=pair.Value.GetComponentInChildren<BoxCollider>();
                if(collider!=null && collider.Raycast(ray,out var hit,1000) && hit.distance<nearest)
                {nearest=hit.distance; inspectedHome=pair.Key;}
            }
            // Empty/zoned lots remain selectable even before a building mesh exists.
            if(inspectedHome<0 && landscape.Raycast(ray,out var ground))
            {
                int id=city.PickBuilding(ground.x,ground.z);
                if(city.HousingCapacity(id)>0) inspectedHome=id;
            }
            if(inspectedHome<0) return false;
            inspectedTrip=-1; showSimulation=false; simulationTyping=false; householdListScroll=Vector2.zero;
            if(routeLine!=null) routeLine.positionCount=0;
            notice="已选中住宅 #"+inspectedHome+"，点击住户查看家庭详情。";
            var first=city.society.families.Find(f=>f.resident && f.home==inspectedHome && HasWorkplace(f));
            if(first!=null) ShowWorkplace(first,false);
            return true;
        }
        void DrawHouseholdInspector(float h,Color navy)
        {
            if(selected!=LandUse.Empty || inspectedHome<0) return;
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
            GUILayout.Label("居民 "+occupants.Sum(f=>f.members)+" 人 · 空房 "+(b.housingUnits-occupants.Count)+" 套\n挂牌租金 ¥"+b.askingRent+" / 日 · "+(city.EntityRoadAccess(b.id)?"道路已接通":"道路未接通"),small);
            householdListScroll=GUILayout.BeginScrollView(householdListScroll);
            if(occupants.Count==0) GUILayout.Label("暂时没有住户。\n住房已提供容量，等待家庭选择入住。",small);
            foreach(var f in occupants)
            {
                if(GUILayout.Button("单元 "+(f.unit+1)+" · 家庭 #"+f.id+" · "+f.members+" 人",button))
                {
                    familyIndex=city.society.families.IndexOf(f); familySearch=f.id.ToString();
                    simulationTab=1; simulationScroll=Vector2.zero; showSimulation=true;
                }
                float minutes=city.CommuteMinutes(f.home,f.work);
                GUILayout.Label("日薪 ¥"+city.Wage(f.work)+" · 租金 ¥"+f.rent+"\n储蓄 ¥"+f.savings+" · "+(f.work<0?"待业":"工作 #"+f.work)+"\n通勤："+(minutes<0?"不可达":minutes.ToString("F1")+" 分钟"),small);
                GUILayout.Label("工作目的地："+WorkplaceName(f)+(commuteFamily==f.id?"（已标出）":""),small);
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
            GUILayout.EndHorizontal();
            int newTab=GUILayout.Toolbar(simulationTab,new[]{"总览 / 账目","家庭 / 决策","住房 / 岗位","每日趋势","事件","人口 / 位置"},button);
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
                        " / 工作场所内 "+residents.Sum(h=>h.people.Count(p=>p.atWork && p.tripId==0))+" / 今日已挣工资 ¥"+residents.Sum(h=>h.people.Sum(p=>p.earnedWages)).ToString("F0"),label);
                }
                GUILayout.Label("住房 "+units+" 套 / 空置 "+Math.Max(0,units-residents.Count)+" / 已就业 "+residents.Count(f=>city.CommuteMinutes(f.home,f.work)>=0)+" / 岗位容量 "+city.jobs,label);
                GUILayout.Label("每户 1 名劳动者；通勤车到达才算到岗，工资按实际在岗时间日结。生活与通勤费用流向外部。",small);
                GUILayout.Label("每个道路出行任务都有对应车辆，不限制显示数量。住房选择参考本户最近实际通勤；未尝试的方案按畅通道路估算。偏好每 "+s.settings.reviewDays+" 天评估，租约 "+s.settings.leaseDays+" 天。",small);
                var r=s.history.LastOrDefault();
                if(r==null) GUILayout.Label("尚未日结。可先暂停，再点‘推进 1 天’观察现金流和决策。",small);
                else
                {
                    GUILayout.Label("最近日结：工资 +"+r.wages+"，生活 −"+r.living+"，通勤 −"+r.travel+"，实付租金 −"+r.rent+"，搬迁 −"+r.movingCosts,small);
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
                    GUILayout.Label("家庭 #"+f.id+" · "+f.members+" 人 · "+(f.resident?"本城住户":"城外申请者")+" · 技能 "+f.skill,label);
                    GUILayout.Label("住宅 #"+f.home+" / 单元 "+f.unit+" / 工作 #"+f.work+" / 储蓄 ¥"+f.savings+" / 欠租 ¥"+f.arrears,small);
                    float minutes=city.CommuteMinutes(f.home,f.work);
                    GUILayout.Label("当前通勤 "+(minutes<0?"不可达":minutes.ToString("F1")+" 分钟")+" / 合同租金 "+f.rent+" / 薪资 "+city.Wage(f.work)+" / 下次评估第 "+f.nextReview+" 天",small);
                    GUILayout.Label("偏好 0–1：空间 "+f.spacePreference.ToString("F2")+"  私密 "+f.privacyPreference.ToString("F2")+"  时间 "+f.timePreference.ToString("F2")+"  储蓄 "+f.savingPreference.ToString("F2"),small);
                    GUILayout.Label("最近决定："+f.reason,small);
                    GUILayout.Label("第 "+f.evaluatedDay+" 天评估快照（0 为尚未评估）：正项为空间、私密、结余；负项为时间、变更成本",small);
                    foreach(var o in f.options)
                        GUILayout.Label("房 #"+o.home+" / 工作 #"+o.work+"："+(o.rejection!=""?o.rejection:"总分 "+o.score.ToString("F1")+" = "+o.spaceScore.ToString("F1")+" + "+o.privacyScore.ToString("F1")+" + "+o.moneyScore.ToString("F1")+" − "+o.timeScore.ToString("F1")+" − "+o.changeCost)+"\n租 "+o.rent+" / 工资 "+o.wage+" / 结余 "+o.surplus+" / 通勤 "+o.minutes.ToString("F1"),small);
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
                    else if(city.JobCapacity(i)>0) GUILayout.Label("工作场所 #"+i+" · 已雇 "+city.EmployedAt(i)+" / "+city.JobCapacity(i)+" · 日薪 "+city.Wage(i)+" · 技能要求 "+city.SkillRequired(i),small);
                }
            }
            else if(simulationTab==3)
            {
                GUILayout.Label("天 | 户数 / 空房 | 就业 | 平均通勤 | 租金 / 维护 | 迁入 / 搬家 / 迁出",small);
                foreach(var r in s.history.AsEnumerable().Reverse()) GUILayout.Label(r.day+" | "+r.households+" / "+(r.units-r.households)+" | "+r.employed+" | "+r.averageCommute.ToString("F1")+" | "+r.rent+" / "+r.maintenance+" | "+r.arrived+" / "+r.moved+" / "+r.left,small);
            }
            else if(simulationTab==5) DrawPopulationPanel();
            else foreach(string e in s.events.AsEnumerable().Reverse()) GUILayout.Label(e,small);
            simulationTyping=simulationTab==1 && GUI.GetNameOfFocusedControl()=="FamilySearch"
                || simulationTab==5 && GUI.GetNameOfFocusedControl()=="PopulationSearch";
            GUILayout.EndScrollView(); GUILayout.EndArea();
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
