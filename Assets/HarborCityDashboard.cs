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
        bool dashboardPreviewOnly=false;
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
            DashboardStyles();PrepareCharts(s);var l=s.live;var t=s.today;
            Rect header=Block(40);
            var overall=l.unfundedFactories>0 || s.findings.Any(f=>FindingVisualTone(f,s)==DashboardTone.Critical)?DashboardTone.Critical:s.findings.Any(f=>FindingVisualTone(f,s)==DashboardTone.Attention)?DashboardTone.Attention:DashboardTone.Neutral;
            StatusBadge(new Rect(header.x,header.y+5,110,27),overall==DashboardTone.Critical?"存在资金警示":overall==DashboardTone.Attention?"有事项需关注":"当前观测",overall);
            Ink(new Rect(header.x+124,header.y+6,header.width-124,25),"D"+l.day+"  "+((int)l.minute/60)+":"+((int)l.minute%60).ToString("00")+"     今日生产 "+t.produced+" / 实售 "+t.sold,dashHeading);
            Rect row=Block(110);
            MetricCard(CardCell(row,0,4),"人口",l.population.ToString(),sparkPopulation.Delta(),spark:sparkPopulation);
            MetricCard(CardCell(row,1,4),"就业率",l.workforce>0?(100.0*l.employed/l.workforce).ToString("0")+"%":"—",l.employed+" / "+l.workforce+" 劳动者",l.unemployed>0?DashboardTone.Attention:DashboardTone.Neutral,amount:l.employed,capacity:l.workforce);
            MetricCard(CardCell(row,2,4),"空置住房",l.vacantUnits.ToString(),l.vacantUnits+" / "+l.units+" 套",amount:l.vacantUnits,capacity:l.units);
            MetricCard(CardCell(row,3,4),"平均实际通勤",s.currentDay.commuteSamplesAfter>0?l.averageCommute.ToString("0.#")+" min":"—",s.currentDay.commuteSamplesAfter+" 个实际样本",spark:sparkCommute);
            GUILayout.Space(8);row=Block(110);
            MetricCard(CardCell(row,0,4),"财政",Money(l.treasury),sparkTreasury.Delta(),l.treasury<0?DashboardTone.Critical:DashboardTone.Neutral,sparkTreasury);
            MetricCard(CardCell(row,1,4),"居民在途",l.residentsInTransit.ToString(),"受阻 "+l.blockedResidents+" · 在岗 "+l.atWork,l.blockedResidents>0?DashboardTone.Attention:DashboardTone.Neutral,amount:l.blockedResidents,capacity:l.residentsInTransit);
            MetricCard(CardCell(row,2,4),"货运在途",l.freightInTransit.ToString(),"受阻 "+l.blockedFreight,l.blockedFreight>0?DashboardTone.Attention:DashboardTone.Neutral,amount:l.blockedFreight,capacity:l.freightInTransit);
            int abnormal=s.businesses.Count(b=>b.industrial && (b.raw==0 || b.goods>=FactoryState.GoodsCapacity || b.cash<=0));
            MetricCard(CardCell(row,3,4),"库存 / 资金异常工厂",abnormal.ToString(),"缺货商业 "+l.emptyCommercial,overall);
            SectionCard("需要关注",s.findings.Count+" 项 · 点击查看证据");
            if(s.findings.Count==0)StatusBadge(Block(30),"未观测到明显异常",DashboardTone.Neutral);
            int shown=showAllFindings?s.findings.Count:Math.Min(3,s.findings.Count);
            for(int i=0;i<shown;i++)FindingCard(s.findings[i],s);
            if(s.findings.Count>3 && GUILayout.Button(showAllFindings?"收起关注项":"展开其余 "+(s.findings.Count-3)+" 项",button))showAllFindings=!showAllFindings;
            SectionCard("交通与工业 · 当前状态");
            GUILayout.BeginHorizontal();GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            MiniBar("居民受阻",l.blockedResidents,Math.Max(1,l.residentsInTransit),DashboardTone.Attention);
            MiniBar("货运受阻",l.blockedFreight,Math.Max(1,l.freightInTransit),DashboardTone.Attention);
            FieldRow("严重迟到 / 今日",t.late+" 人");GUILayout.EndVertical();GUILayout.Space(18);GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            FieldRow("原料为空",l.rawEmptyFactories+" 家",l.rawEmptyFactories>0?DashboardTone.Attention:DashboardTone.Neutral);
            FieldRow("成品仓满",l.stockFullFactories+" 家",l.stockFullFactories>0?DashboardTone.Attention:DashboardTone.Neutral);
            FieldRow("现金耗尽",l.unfundedFactories+" 家",l.unfundedFactories>0?DashboardTone.Critical:DashboardTone.Neutral);
            GUILayout.EndVertical();GUILayout.EndHorizontal();
            SectionCard("Today · 实际流量",Coverage(s.currentDay));
            double flowMax=Math.Max(1,Math.Max(Math.Max(t.produced,t.sold),Math.Max(t.shipped,t.imported)));
            MiniBar("生产",t.produced,flowMax,DashboardTone.Neutral);
            MiniBar("实售",t.sold,flowMax,DashboardTone.Positive);
            MiniBar("运出",t.shipped,flowMax,DashboardTone.Neutral);
            MiniBar("原料入库",t.imported,flowMax,DashboardTone.Neutral);
            Text("同一相对尺度 · 运出不等于实售；今日截至当前，不能直接与昨日全天比较。");
            SectionCard("劳动情况", "工人分钟 · 非墙钟时间");
            double labourMax=Math.Max(1,Math.Max(t.attendance,Math.Max(t.productive,Math.Max(t.rawBlocked,Math.Max(t.stockBlocked,t.fundsBlocked)))));
            MiniBar("实际在岗",t.attendance,labourMax,DashboardTone.Neutral);
            MiniBar("有效加工",t.productive,labourMax,DashboardTone.Positive);
            MiniBar("缺料受阻",t.rawBlocked,labourMax,DashboardTone.Attention);
            MiniBar("仓满受阻",t.stockBlocked,labourMax,DashboardTone.Attention);
            MiniBar("资金受阻",t.fundsBlocked,labourMax,DashboardTone.Critical);
            SectionCard("今日资金与城市变动");
            FieldRow("工厂收入",Money(t.revenue));FieldRow("工厂实付工资",Money(t.employerWages));FieldRow("外部岗位已挣未日结",Money(t.externalAccrued));
            FieldRow("原料 / 加工支出",Money(t.rawCost)+" / "+Money(t.productionCost));
            FieldRow("迁入 / 搬家 / 迁出",t.arrived+" / "+t.moved+" / "+t.left+" 户");
            FieldRow("人口 / 观察起点",s.currentDay.populationBefore+" → "+l.population);
            FieldRow("就业 / 观察起点",s.currentDay.employedBefore+" → "+l.employed);
            if(GUILayout.Button("查看完整历史趋势 →",button)) {simulationTab=3;simulationScroll=Vector2.zero;}
        }
        void DrawBusinessSummary(BusinessAnalysisState b)
        {
            DashboardStyles();Rect badge=Block(28);StatusBadge(badge,b.status+(b.connected?" · 道路已接通":" · 道路未接通"),DashboardPresentation.StateTone(b.status));
            if(b.industrial)
            {
                Rect cards=Block(110);
                MetricCard(CardCell(cards,0,2),"当前现金",Money(b.cash),"当前状态",b.cash<=0?DashboardTone.Critical:DashboardTone.Neutral);
                MetricCard(CardCell(cards,1,2),"今日经营盈余",b.today==null?"—":Money(b.today.profit),"包含预付采购",b.today?.profit<0?DashboardTone.Attention:DashboardTone.Neutral);
                ProgressMetric("原料",b.raw,FactoryState.RawCapacity,b.raw==0?DashboardTone.Attention:DashboardTone.Neutral,"在途 "+b.incomingRaw);
                ProgressMetric("成品",b.goods,FactoryState.GoodsCapacity,b.goods>=FactoryState.GoodsCapacity?DashboardTone.Attention:DashboardTone.Neutral,"待交付 "+b.outgoingGoods);
                ProgressMetric("加工",b.progress,FactoryState.LabourPerItem,suffix:"工人分钟");
            }
            else ProgressMetric("商品库存",b.goods,32,b.goods==0?DashboardTone.Attention:DashboardTone.Neutral,"在途补货 "+b.incomingGoods);
            ProgressMetric("岗位 / 已雇",b.employees.Count,b.slots);
            ProgressMetric("当前实际在岗",b.attending,b.employees.Count);
            FieldRow("资金可用岗位",b.fundedJobs.ToString());
            if(!b.industrial) {Text("商业沿用简化外部资金；当前没有独立现金或利润账。");return;}
            var d=b.today;if(d==null)return;
            SectionCard("今日产销与出勤");
            double max=Math.Max(1,Math.Max(d.produced,Math.Max(d.sold,d.shipped)));
            MiniBar("生产",d.produced,max,DashboardTone.Neutral);MiniBar("实售",d.sold,max,DashboardTone.Positive);MiniBar("运出",d.shipped,max,DashboardTone.Neutral);
            ProgressMetric("今日观测到岗",d.attended,d.assigned,suffix:"迟到 "+d.late);
            FieldRow("在岗 / 有效劳动",d.attendance.ToString("0.#")+" / "+d.productive.ToString("0.#")+" 工人分钟");
            SectionCard("今日经营收支","当日差量");
            double cashMax=Math.Max(1,Math.Max(d.revenue,Math.Max(d.rawCost,Math.Max(d.wageCost,d.productionCost))));
            MiniBar("收入",d.revenue,cashMax,DashboardTone.Positive,"+"+Money(d.revenue));
            MiniBar("原料支出",d.rawCost,cashMax,DashboardTone.Attention,"−"+Money(d.rawCost));
            MiniBar("实付工资",d.wageCost,cashMax,DashboardTone.Attention,"−"+Money(d.wageCost));
            MiniBar("加工成本",d.productionCost,cashMax,DashboardTone.Attention,"−"+Money(d.productionCost));
            SectionCard("主要瓶颈");
            StatusBadge(Block(28),d.mainBottleneck,d.causes.Count==0?DashboardTone.Neutral:DashboardPresentation.FindingTone(d.causes[0]));
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
                SectionCard("家庭财务与住房");
                FieldRow("当前住房 / 单元","#"+h.home+" / "+(h.unit+1));
                FieldRow("储蓄",Money(h.savings));FieldRow("租金 / 欠租",Money(h.rent)+" / "+Money(h.arrears));
                FieldRow("合同收入",Money(h.expectedIncome));FieldRow("最近日结工资",Money(h.income));
                FieldRow("成员通勤合计",Minutes(h.commute));FieldRow("下次评估","D"+h.reviewDay);
                SectionCard("成员 · 点击查看居民");
                foreach(var p in h.members)EntityButton(p.name+" #"+p.id+"  |  工作场所 #"+p.work+"  |  "+p.activity,AnalysisEntityKind.Resident,p.id);
                Heading("最近实际决策");var d=h.decision;
                if(d==null)Text("本观察会话尚未捕获决策；存档已有原因："+h.reason);
                else
                {
                    Text("D"+d.decisionDay+" · "+d.action+" · "+(d.committed?"已提交":"未变更")+"\n"+d.reason);
                    if(d.evaluated)
                    {
                        bool comparable=DashboardPresentation.ComparableDecision(d);
                        FieldRow("住所变化",(d.fromHome<0?"城外":"#"+d.fromHome)+" → #"+d.toHome);
                        FieldRow("候选通勤",(d.fromHome<0?"无当前住所":Minutes(d.currentCommute))+" → "+Minutes(d.proposedCommute));
                        FieldRow("当前 / 候选评分",(comparable?d.currentScore.ToString("F1"):"无有效当前方案")+" / "+d.proposedScore.ToString("F1"));
                        if(comparable)FieldRow("评分改善 / 所需",(d.proposedScore-d.currentScore).ToString("+0.#;-0.#;0")+" / "+d.improvementRequired);
                        if(!string.IsNullOrEmpty(d.currentRejection))FieldRow("当前方案拒绝",d.currentRejection);
                        if(!string.IsNullOrEmpty(d.proposedRejection))FieldRow("候选方案拒绝",d.proposedRejection);
                    }
                    foreach(var factor in d.factors)Text(factor);foreach(var rejection in d.rejections)Text(rejection);
                }
                Heading("D"+h.evaluatedDay+" 已评估候选 / 拒绝原因");
                foreach(var o in h.candidates)Text("住宅 #"+o.home+" · 岗位 "+string.Join(",",o.jobIds)+" · "+(string.IsNullOrEmpty(o.rejection)?"评分 "+o.score.ToString("F1"):o.rejection)+"\n通勤 "+Minutes(o.minutes)+" · 工资 "+o.wage+" · 租金 "+o.rent+" · 结余 "+o.surplus+"\n空间 "+o.spaceScore.ToString("F1")+" / 私密 "+o.privacyScore.ToString("F1")+" / 预算 "+o.moneyScore.ToString("F1")+" / 时间扣分 "+o.timeScore.ToString("F1")+" / 变更 "+o.changeCost+" / 环境 "+o.environmentCost.ToString("F1"));
                if(h.home>=0 && GUILayout.Button("定位住宅",button))LocateAnalysisBuilding(h.home);
            }
            else if(analysisEntityKind==AnalysisEntityKind.Resident)
            {
                var p=s.Resident(analysisEntityId);if(p==null) {Text("居民已离开当前实体列表。");return;}
                SectionCard(p.name+" #"+p.id+" · "+p.age+" 岁");
                FieldRow("活动状态",p.activity);FieldRow("当前位置",p.location);FieldRow("目的地",p.destination);
                SectionCard("工作与实际出勤");
                FieldRow("工作场所 / 岗位","#"+p.work+" / #"+p.jobId);FieldRow("合同工资",Money(p.wage));
                FieldRow("今日在岗",Minutes(p.workedMinutes));FieldRow("待入家庭工资",Money(p.pendingWages));
                FieldRow("最近实际通勤",Minutes(p.commute));FieldRow("最近迟到",Minutes(p.late),p.late>=15?DashboardTone.Attention:DashboardTone.Neutral);
                Text(p.reason);
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
            DashboardStyles();
            foreach(var b in s.businesses)
            {
                Rect r=Block(75);Fill(r,dashSurface);var tone=DashboardPresentation.StateTone(b.status);
                Ink(new Rect(r.x+10,r.y+6,r.width*.45f,23),(b.industrial?"工厂":"商业")+" #"+b.id,dashHeading);
                StatusBadge(new Rect(r.x+r.width*.48f,r.y+7,r.width*.32f,22),b.status,tone);
                Ink(new Rect(r.x+10,r.y+35,r.width-100,20),"库存 "+b.goods+" · 实际在岗 "+b.attending+" / "+b.employees.Count+" · 今日实售 "+(b.today?.sold.ToString()??"—"),dashCaption);
                ProgressBar(new Rect(r.x+10,r.y+62,r.width-100,5),b.goods,b.industrial?FactoryState.GoodsCapacity:32,ToneColor(tone));
                if(GUI.Button(new Rect(r.xMax-75,r.y+34,65,29),"详情 →",button))OpenAnalysisEntity(b.industrial?AnalysisEntityKind.Factory:AnalysisEntityKind.Commercial,b.id);
                GUILayout.Space(6);
            }
            Heading("住房");foreach(var r in s.housing)EntityButton("住宅 #"+r.id+" · 入住 "+r.occupied+" / "+r.units+" · 挂牌租金 "+r.rent,AnalysisEntityKind.Residence,r.id);
        }
        void DrawHistoryRow(CityHistorySample d)
        {Text("D"+d.day+(d.partial?"（部分日）":"")+" · 人口 "+d.population+" · 就业 "+d.employed+" · 通勤 "+Minutes(d.averageCommute)+"\n财政 "+Money(d.treasury)+" · 日结工资 "+d.wages+" · 工厂产量 / 实售 "+(d.industryKnown?d.produced+" / "+d.sold:"未观测")+" · 迁入 / 搬家 / 迁出 "+d.arrived+" / "+d.moved+" / "+d.left);}
        void DrawAnalysisHistory(CityAnalysisState s)
        {
            PrepareCharts(s);SectionCard("History · 已结束日趋势",s.history.Count+" / 180 日");
            LineChart("人口 / 就业",chartPopulation,"人口（蓝）",chartEmployment,"就业（绿）","人");
            LineChart("财政",chartTreasury,"日结财政",unit:"¥");
            LineChart("平均实际通勤",chartCommute,"实际通勤均值",unit:"分钟",note:"使用日账目均值；0 可能表示当日无实际通勤样本，样本群体可能变化");
            LineChart("工厂生产 / 实售",chartProduction,"生产（蓝）",chartSales,"实售（绿）","件", "未观测的工业历史留空；橙点为部分日，不与完整日连线");
            if(GUILayout.Button(showHistoryRows?"收起日账目明细":"展开日账目明细",button))showHistoryRows=!showHistoryRows;
            if(showHistoryRows)foreach(var d in s.history.AsEnumerable().Reverse())DrawHistoryRow(d);
        }
        void DrawAnalysisTimeline(CityAnalysisState s)
        {
            Text("最近 "+s.timeline.Count+" / 750 条关键变化；工资结算、发车、加工步骤保留在可选 Trace 中。");
            GUI.SetNextControlName("AnalysisSearch");timelineSearch=GUILayout.TextField(timelineSearch);
            foreach(var e in s.timeline.AsEnumerable().Reverse().Where(e=>(e.message+" "+e.code).IndexOf(timelineSearch,StringComparison.OrdinalIgnoreCase)>=0).Take(150))
            {
                DashboardStyles();Rect r=Block(56);var tone=TimelineVisualTone(e);
                Ink(new Rect(r.x,r.y+5,75,19),"D"+e.day,dashNote);
                Ink(new Rect(r.x,r.y+25,75,22),((int)e.minute/60)+":"+((int)e.minute%60).ToString("00"),dashCaption);
                Fill(new Rect(r.x+82,r.y,2,r.height),dashTrack);Fill(new Rect(r.x+78,r.y+20,10,10),ToneColor(tone));
                Ink(new Rect(r.x+102,r.y+14,Math.Max(1,r.width-178),26),e.message,dashCaption,tooltip:e.message);
                if(GUI.Button(new Rect(r.xMax-68,r.y+12,62,30),"查看",button))OpenAnalysisEntity(e.entityKind,e.entityId>=0?e.entityId:0);
            }
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
        {if(dashboardPreviewOnly)return;var b=city.GetBuilding(id);if(b==null)return;selected=LandUse.Empty;inspectedTrip=inspectedResident=-1;followResident=false;commuteFamily=-1;focus=new Vector3(b.x,0,b.z);zoom=24;showSimulation=false;inspectedHome=id;UpdateCamera();}
        void LocateAnalysisResident(int id,bool follow)
        {
            if(dashboardPreviewOnly)return;
            var h=city.society.families.Find(h=>h.people.Exists(p=>p.id==id));var p=h?.people.Find(p=>p.id==id);
            if(p!=null)SelectPopulationResident(new PopulationRow {family=h,person=p,activity=city.ObserveResident(h,p)},follow);
        }
    }
}
