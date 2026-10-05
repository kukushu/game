using System;
using System.Linq;
using UnityEngine;
namespace HarborCity
{
    public sealed partial class HarborCityGame
    {
        AnalysisEntityKind analysisEntityKind;
        int analysisEntityId=-1;
        string analysisSearch="",timelineSearch="";
        int analysisResidentPage;
        void OpenAnalysisEntity(AnalysisEntityKind kind,int id)
        {
            analysisEntityKind=kind;analysisEntityId=id;showSimulation=true;simulationTyping=false;simulationScroll=Vector2.zero;
            simulationTab=kind==AnalysisEntityKind.Household?1:kind==AnalysisEntityKind.Resident?5:kind==AnalysisEntityKind.Factory || kind==AnalysisEntityKind.Commercial?2:0;
        }
        void Text(string value) {GUILayout.Label(value,small);}
        void Heading(string value) {GUILayout.Space(8);GUILayout.Label(value,label);}
        string Money(double value)=>"¥"+value.ToString("F2");
        string Minutes(double value)=>value<0?"不可达 / 未观测":value.ToString("F1")+" 分钟";
        string Coverage(CityAnalysisReport r)=>r==null?"未观测":(r.inProgress?"今日截至当前":"已结束日")+(r.partial?" · 部分日，观察始于 "+Minutes(r.fromMinute):"");
        void EntityButton(string text,AnalysisEntityKind kind,int id)
        {if(GUILayout.Button(text,button))OpenAnalysisEntity(kind,kind==AnalysisEntityKind.City?0:id);}
        void DrawDashboard(CityAnalysisState s)
        {
            var l=s.live;var t=s.today;
            Heading("需要关注 / Findings");
            if(s.findings.Count==0)Text("暂未观测到需要关注的异常或已提交变动。");
            foreach(var f in s.findings)
            {
                EntityButton(f.title,f.entityKind,f.entityId>=0?f.entityId:0);
                Text(f.evidence);
            }
            Heading("Live State · D"+l.day+" "+((int)l.minute/60)+":"+((int)l.minute%60).ToString("00"));
            Text("人口 "+l.population+" · 家庭 "+l.households+" · 城外申请 "+l.applicants+" 户\n就业 "+l.employed+" / 劳动者 "+l.workforce+" · 失业 "+l.unemployed+" · 岗位 "+l.jobs+"（资金可用 "+l.fundedJobs+"）\n住房空置 "+l.vacantUnits+" / "+l.units+" · 财政 "+Money(l.treasury));
            Text("当前在途居民 "+l.residentsInTransit+"（阻塞 "+l.blockedResidents+"）· 在岗 "+l.atWork+"\n货运 "+l.freightInTransit+"（阻塞 "+l.blockedFreight+"）\n原料为空工厂 "+l.rawEmptyFactories+" · 仓满 "+l.stockFullFactories+" · 现金耗尽 "+l.unfundedFactories+" · 缺货商业 "+l.emptyCommercial);
            Heading("Today / Current Day · "+Coverage(s.currentDay));
            Text("观察起点 → 当前：人口 "+s.currentDay.populationBefore+" → "+l.population+" · 就业 "+s.currentDay.employedBefore+" → "+l.employed+"\n实际通勤均值 "+Minutes(s.currentDay.commuteBefore)+" → "+Minutes(s.currentDay.commuteAfter)+"（样本 "+s.currentDay.commuteSamplesBefore+" → "+s.currentDay.commuteSamplesAfter+"，群体可能变化）");
            Text("生产 "+t.produced+" · 实售 "+t.sold+" · 运出 "+t.shipped+" · 原料实际入库 "+t.imported+"\n工厂收入 "+Money(t.revenue)+" · 原料支出 "+Money(t.rawCost)+" · 加工成本 "+Money(t.productionCost)+"\n工厂实付工资 "+Money(t.employerWages)+" · 外部岗位已挣未日结 "+Money(t.externalAccrued));
            Text("实际在岗 "+Minutes(t.attendance)+" · 有效加工 "+Minutes(t.productive)+"\n受阻劳动：缺料 "+Minutes(t.rawBlocked)+" / 仓满 "+Minutes(t.stockBlocked)+" / 资金 "+Minutes(t.fundsBlocked)+"\n以上是工人分钟，多人同时受阻会相加。\n迁入 "+t.arrived+" / 搬家 "+t.moved+" / 迁出 "+t.left+" · 严重迟到 "+t.late+" 人 · 长时间受阻货运 "+t.longWaitingFreight+" 个");
            Heading("History · 最近已结束日");
            foreach(var d in s.history.AsEnumerable().Reverse().Take(3))DrawHistoryRow(d);
            if(s.history.Count==0)Text("尚无日结历史。");
            Text("今日累计与昨日全天覆盖范围不同；不会将两者直接解释为产量下降。");
        }
        void DrawBusinessSummary(BusinessAnalysisState b)
        {
            Text("状态："+b.status+" · "+(b.connected?"道路已接通":"道路未接通")+"\n已雇 "+b.employees.Count+" / 岗位 "+b.slots+"（资金可用 "+b.fundedJobs+"）· 实际在岗 "+b.attending);
            if(!b.industrial) {Text("商品 "+b.goods+" / 32 · 在途补货 "+b.incomingGoods+"\n商业沿用简化外部资金，无独立现金利润账。分析只显示已有真实数据。");return;}
            Text("现金 "+Money(b.cash)+" · 原料 "+b.raw+" / 24（在途 "+b.incomingRaw+"）\n成品 "+b.goods+" / 24（待交付 "+b.outgoingGoods+"）\n加工进度 "+b.progress.ToString("F1")+" / 60 工人分钟");
            var d=b.today;if(d==null)return;
            Text("今日产量 "+d.produced+" · 实售 "+d.sold+" · 运出 "+d.shipped+"\n今日到岗 "+d.attended+" / 已分配 "+d.assigned+" · 迟到 "+d.late+"\n在岗 "+Minutes(d.attendance)+" · 有效劳动 "+Minutes(d.productive)+"\n收入 "+Money(d.revenue)+" · 原料 "+Money(d.rawCost)+"\n实付工资 "+Money(d.wageCost)+" · 加工 "+Money(d.productionCost)+"\n今日经营盈余 "+Money(d.profit)+"\n主要瓶颈："+d.mainBottleneck);
        }
        void DrawAnalysisDetail(CityAnalysisState s)
        {
            if(GUILayout.Button("← 返回列表 / Dashboard",button)) {analysisEntityId=-1;return;}
            if(analysisEntityKind==AnalysisEntityKind.Factory || analysisEntityKind==AnalysisEntityKind.Commercial)
            {
                var b=s.Business(analysisEntityId);
                if(b==null) {Text("该工作场所已移除。历史中的事件仍保留。");return;}
                Heading((b.industrial?"工厂":"商业")+" #"+b.id);Text(Coverage(s.currentDay));DrawBusinessSummary(b);
                if(b.today!=null)
                {
                    Heading("原因与实际证据");
                    foreach(var c in b.today.causes)Text(c.title+"："+c.evidence);
                    foreach(var e in b.today.effects)Text("后续影响："+e);
                    foreach(var e in b.today.relatedObservations)Text("相关观察："+e);
                    Text("受阻劳动：缺料 "+Minutes(b.today.rawBlocked)+" / 仓满 "+Minutes(b.today.stockBlocked)+" / 资金 "+Minutes(b.today.fundsBlocked)+"（工人分钟）");
                }
                if(b.yesterday!=null)Text("昨日全天产量 "+b.yesterday.produced+" · 实售 "+b.yesterday.sold+" · 盈余 "+Money(b.yesterday.profit));
                if(b.industrial)Text("累计收入 "+Money(b.totalRevenue)+" · 原料支出 "+Money(b.totalRawCosts)+" · 工资 "+Money(b.totalWages)+" · 加工 "+Money(b.totalProductionCosts)+"\n期初投入 "+Money(b.capital)+" · 资金核对差额 "+b.cashError.ToString("F6")+"\n当前噪声 "+b.noise.ToString("F2")+" · 污染 "+b.pollution.ToString("F2"));
                Heading("员工");foreach(int id in b.employees) {var p=s.Resident(id);EntityButton((p?.name??"居民")+" #"+id+" · "+p?.activity,AnalysisEntityKind.Resident,id);}
                Heading("相关货运");foreach(int id in b.trips) {var t=s.Trip(id);EntityButton("货车 #"+id+" · "+t?.purpose+" · "+t?.status,AnalysisEntityKind.Trip,id);}
                if(GUILayout.Button("定位建筑",button))LocateAnalysisBuilding(b.id);
            }
            else if(analysisEntityKind==AnalysisEntityKind.Household)
            {
                var h=s.Household(analysisEntityId);if(h==null) {Text("该家庭已不在运行时实体列表中。");return;}
                Heading("家庭 #"+h.id+" · "+(h.resident?"本城住户":"城外申请者"));
                Text("住宅 #"+h.home+" / 单元 "+(h.unit+1)+" · 租金 "+Money(h.rent)+"\n储蓄 "+Money(h.savings)+" · 欠租 "+Money(h.arrears)+"\n最近日结实收工资 "+Money(h.income)+" · 合同工资合计 "+Money(h.expectedIncome)+"\n成员通勤合计 "+Minutes(h.commute)+" · 下次评估 D"+h.reviewDay);
                foreach(var p in h.members)EntityButton(p.name+" #"+p.id+" · 工作场所 #"+p.work+" · "+p.activity,AnalysisEntityKind.Resident,p.id);
                Heading("最近实际决策");var d=h.decision;
                if(d==null)Text("本观察会话尚未捕获决策；存档已有原因："+h.reason);
                else
                {
                    Text("D"+d.decisionDay+" · "+d.action+" · "+(d.committed?"已提交":"未变更")+"\n"+d.reason);
                    if(d.evaluated)Text("住宅 #"+d.fromHome+" → #"+d.toHome+"\n候选通勤 "+Minutes(d.currentCommute)+" → "+Minutes(d.proposedCommute)+"\n综合评分 "+d.currentScore.ToString("F1")+" → "+d.proposedScore.ToString("F1")+" · 所需改善 "+d.improvementRequired);
                    foreach(var factor in d.factors)Text(factor);foreach(var rejection in d.rejections)Text(rejection);
                }
                Heading("D"+h.evaluatedDay+" 已评估候选 / 拒绝原因");
                foreach(var o in h.candidates)Text("住宅 #"+o.home+" · 岗位 "+string.Join(",",o.jobIds)+" · "+(string.IsNullOrEmpty(o.rejection)?"评分 "+o.score.ToString("F1"):o.rejection)+"\n通勤 "+Minutes(o.minutes)+" · 工资 "+o.wage+" · 租金 "+o.rent+" · 结余 "+o.surplus+"\n空间 "+o.spaceScore.ToString("F1")+" / 私密 "+o.privacyScore.ToString("F1")+" / 预算 "+o.moneyScore.ToString("F1")+" / 时间扣分 "+o.timeScore.ToString("F1")+" / 变更 "+o.changeCost+" / 环境 "+o.environmentCost.ToString("F1"));
                if(h.home>=0 && GUILayout.Button("定位住宅",button))LocateAnalysisBuilding(h.home);
            }
            else if(analysisEntityKind==AnalysisEntityKind.Resident)
            {
                var p=s.Resident(analysisEntityId);if(p==null) {Text("居民已离开当前实体列表。");return;}
                Heading(p.name+" #"+p.id+" · "+p.age+" 岁");Text(p.activity+"\n"+p.reason+"\n当前位置："+p.location+"\n目的地："+p.destination);
                Text("工作场所 #"+p.work+" · 岗位 #"+p.jobId+" · 合同工资 "+Money(p.wage)+"\n今日在岗 "+Minutes(p.workedMinutes)+" · 待入家庭工资 "+Money(p.pendingWages)+"\n最近实际通勤 "+Minutes(p.commute)+" · 最近迟到 "+Minutes(p.late));
                EntityButton("家庭 #"+p.householdId,AnalysisEntityKind.Household,p.householdId);
                if(p.work>=0)EntityButton("工作场所 #"+p.work,s.Business(p.work)?.industrial==true?AnalysisEntityKind.Factory:AnalysisEntityKind.Commercial,p.work);
                if(p.tripId>0)EntityButton("当前车辆 #"+p.tripId,AnalysisEntityKind.Trip,p.tripId);
                GUI.enabled=p.located;
                if(GUILayout.Button("定位居民",button))LocateAnalysisResident(p.id,false);
                if(GUILayout.Button("跟随居民",button))LocateAnalysisResident(p.id,true);
                GUI.enabled=true;
            }
            else if(analysisEntityKind==AnalysisEntityKind.Trip)
            {
                var t=s.Trip(analysisEntityId);if(t==null) {Text("该运输任务已结束；关键事件保留发生时的证据。");return;}
                Heading("车辆 / 任务 #"+t.id);Text(t.purpose+" · "+t.status+"\n"+t.from+" → "+t.to+"\n载货 "+t.cargo+" · 连续受阻 "+Minutes(t.blockedMinutes));
                if(t.residentId>0)EntityButton("居民 #"+t.residentId,AnalysisEntityKind.Resident,t.residentId);
                if(GUILayout.Button("在地图查看车辆",button)) {selected=LandUse.Empty;showSimulation=false;inspectedHome=-1;inspectedResident=-1;followResident=false;commuteFamily=-1;inspectedTrip=t.id;}
            }
            else if(analysisEntityKind==AnalysisEntityKind.Residence)
            {
                var r=s.housing.Find(x=>x.id==analysisEntityId);if(r==null) {Text("住宅已移除。");return;}
                Heading("住宅 #"+r.id);Text("入住 "+r.occupied+" / "+r.units+" · 挂牌租金 "+r.rent+" · 申请 "+r.applications);
                foreach(var h in s.households.Where(h=>h.resident && h.home==r.id))EntityButton("家庭 #"+h.id,AnalysisEntityKind.Household,h.id);
            }
            else
            {
                Heading("城市级异常 · 真实证据与相关实体");
                foreach(var finding in s.findings.Where(f=>f.entityKind==AnalysisEntityKind.City))Text(finding.title+"："+finding.evidence);
                Heading("本日严重迟到 / 货运受阻的关键记录");
                foreach(var e in s.timeline.Where(e=>e.day==s.live.day && (e.code=="resident.late" || e.code=="freight.blocked")))EntityButton(e.message,e.entityKind,e.entityId);
                if(GUILayout.Button("查看家庭候选与就业原因",button)) {analysisEntityId=-1;simulationTab=1;}
                if(GUILayout.Button("查看居民",button)) {analysisEntityId=-1;simulationTab=5;}
            }
        }
        void DrawAnalysisFamilies(CityAnalysisState s)
        {
            SearchAnalysis();foreach(var h in s.households.Where(h=>Matches("家庭 #"+h.id+" 住宅 #"+h.home+" "+h.reason)))
                EntityButton("家庭 #"+h.id+" · 住宅 #"+h.home+" · "+h.reason,AnalysisEntityKind.Household,h.id);
        }
        void DrawAnalysisBusinesses(CityAnalysisState s)
        {
            foreach(var b in s.businesses)EntityButton((b.industrial?"工厂":"商业")+" #"+b.id+" · "+b.status+" · 今日生产 "+(b.today?.produced.ToString()??"—"),b.industrial?AnalysisEntityKind.Factory:AnalysisEntityKind.Commercial,b.id);
            Heading("住房");foreach(var r in s.housing)EntityButton("住宅 #"+r.id+" · 入住 "+r.occupied+" / "+r.units+" · 挂牌租金 "+r.rent,AnalysisEntityKind.Residence,r.id);
        }
        void DrawHistoryRow(CityHistorySample d)
        {Text("D"+d.day+(d.partial?"（部分日）":"")+" · 人口 "+d.population+" · 就业 "+d.employed+" · 通勤 "+Minutes(d.averageCommute)+"\n财政 "+Money(d.treasury)+" · 日结工资 "+d.wages+" · 工厂产量 / 实售 "+(d.industryKnown?d.produced+" / "+d.sold:"未观测")+" · 迁入 / 搬家 / 迁出 "+d.arrived+" / "+d.moved+" / "+d.left);}
        void DrawAnalysisHistory(CityAnalysisState s)
        {Text("最近 180 个已结束日；读档恢复的旧账目没有工业观察数据时标为未观测。");foreach(var d in s.history.AsEnumerable().Reverse())DrawHistoryRow(d);}
        void DrawAnalysisTimeline(CityAnalysisState s)
        {
            Text("最近 "+s.timeline.Count+" / 750 条关键变化；工资结算、发车、加工步骤保留在可选 Trace 中。");
            GUI.SetNextControlName("AnalysisSearch");timelineSearch=GUILayout.TextField(timelineSearch);
            foreach(var e in s.timeline.AsEnumerable().Reverse().Where(e=>(e.message+" "+e.code).IndexOf(timelineSearch,StringComparison.OrdinalIgnoreCase)>=0).Take(150))
                EntityButton("D"+e.day+" "+((int)e.minute/60)+":"+((int)e.minute%60).ToString("00")+" · "+e.message,e.entityKind,e.entityId);
        }
        void SearchAnalysis() {GUI.SetNextControlName("AnalysisSearch");analysisSearch=GUILayout.TextField(analysisSearch);}
        bool Matches(string text)=>text.IndexOf(analysisSearch,StringComparison.OrdinalIgnoreCase)>=0;
        void DrawAnalysisResidents(CityAnalysisState s)
        {
            SearchAnalysis();var rows=s.residents.Where(p=>p.resident && Matches(p.name+" #"+p.id+" 家庭 #"+p.householdId+" "+p.location+" "+p.activity)).ToList();
            int pages=Math.Max(1,(rows.Count+29)/30);analysisResidentPage=Mathf.Clamp(analysisResidentPage,0,pages-1);
            GUILayout.BeginHorizontal();if(GUILayout.Button("上一页",button))analysisResidentPage=Math.Max(0,analysisResidentPage-1);
            Text((analysisResidentPage+1)+" / "+pages+" · 匹配 "+rows.Count+" 人");if(GUILayout.Button("下一页",button))analysisResidentPage=Math.Min(pages-1,analysisResidentPage+1);GUILayout.EndHorizontal();
            foreach(var p in rows.Skip(analysisResidentPage*30).Take(30))EntityButton(p.name+" #"+p.id+" · "+p.activity+" · "+p.location,AnalysisEntityKind.Resident,p.id);
        }
        void DrawAnalysisDebug(CityAnalysisState s)
        {
            Heading("Debug Trace · "+(simulationLog==null?"关闭（默认）":"正在写盘"));
            Text("完整原始事件用于深入排查。关闭时分析、日报、关键时间线继续在内存运行。\n不会自动写 events/readable/Analysis Markdown；正常存档格式保持不变。");
            if(GUILayout.Button(simulationLog==null?"主动开启完整 Debug Trace":"停止完整 Debug Trace",button)) {if(simulationLog==null)StartDebugTrace();else StopDebugTrace();}
            if(simulationLog!=null) {Text(simulationLog.DirectoryPath);if(GUILayout.Button("导出当前 Trace",button))ExportSimulationLog();}
            if(GUILayout.Button("导出模拟存档快照",button))ExportSimulation();
            Heading("最近日结核对");foreach(var d in s.history.AsEnumerable().Reverse().Take(5))Text("D"+d.day+" 财政差额 "+d.treasuryError+" · 家庭储蓄差额 "+d.savingsError+"（应为 0）");
        }
        void LocateAnalysisBuilding(int id)
        {var b=city.GetBuilding(id);if(b==null)return;selected=LandUse.Empty;inspectedTrip=inspectedResident=-1;followResident=false;commuteFamily=-1;focus=new Vector3(b.x,0,b.z);zoom=24;showSimulation=false;inspectedHome=id;UpdateCamera();}
        void LocateAnalysisResident(int id,bool follow)
        {
            var h=city.society.families.Find(h=>h.people.Exists(p=>p.id==id));var p=h?.people.Find(p=>p.id==id);
            if(p!=null)SelectPopulationResident(new PopulationRow {family=h,person=p,activity=city.ObserveResident(h,p)},follow);
        }
    }
}
