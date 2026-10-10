using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HarborCity
{
    [Serializable] public sealed class AnalysisFinding
    {
        public string code, title, evidence;
        public double severity;
        public AnalysisEntityKind entityKind;
        public int entityId=-1;
    }
    [Serializable] public sealed class FactoryDailySummary
    {
        public int id, produced, sold, shipped, imported, rawBefore, raw, goodsBefore, goods, incomingRaw, outgoingGoods;
        public int assigned, attended, late, slots, inputWaiting, outputWaiting;
        public bool active, roadConnected;
        public string state, mainBottleneck;
        public double attendance, productive, rawBlocked, stockBlocked, fundsBlocked;
        public double cashBefore, cash, capitalInflow, revenue, rawCost, wageCost, productionCost, profit, unpaidWages;
        public float firstRawBlock=-1, firstStockBlock=-1, firstFundsBlock=-1, noiseBefore, noise, pollutionBefore, pollution;
        public List<AnalysisFinding> causes=new List<AnalysisFinding>();
        public List<string> effects=new List<string>(), relatedObservations=new List<string>();
    }
    [Serializable] public sealed class HouseholdDecisionSummary
    {
        public int id, decisionDay, fromHome=-1, toHome=-1, savingsBefore, savingsAfter, paidWages, unpaidRent;
        public bool evaluated, committed;
        public string action="待核对提交结果", reason, currentRejection, proposedRejection;
        public float currentScore, proposedScore, improvementRequired;
        public int currentWage, proposedWage, currentRent, proposedRent;
        public float currentCommute, proposedCommute;
        public List<int> citizenIds=new List<int>(), beforeJobs=new List<int>(), proposedJobs=new List<int>(), actualJobs=new List<int>();
        public List<string> factors=new List<string>(), rejections=new List<string>();
    }
    [Serializable] public sealed class CityAnalysisReport
    {
        public int schema=1, day, settlementDay;
        public bool partial, inProgress;
        public float fromMinute, toMinute;
        public int populationBefore, population, employedBefore, employed, workforce, unemployed, fundedJobs;
        public int arrived, moved, left, wages, factoryWages, externalWages, unpaidRent;
        public int lateResidents, commuteSamples, pendingCommuters, longWaitingFreight, vacantHomes;
        public int commuteSamplesBefore, commuteSamplesAfter;
        public float commuteBefore, commuteAfter, todayArrivalCommute;
        public List<string> housingChanges=new List<string>(), commercialShortages=new List<string>();
        public List<AnalysisFinding> attention=new List<AnalysisFinding>();
        public List<FactoryDailySummary> factories=new List<FactoryDailySummary>();
        public List<HouseholdDecisionSummary> households=new List<HouseholdDecisionSummary>();
    }

    // A session-scoped observer: no RNG, economic changes, or fabricated historical data.
    // It copies baselines and captures actual work/transport evidence while simulation runs.
    public sealed partial class CityDailyAnalysis
    {
        sealed class FactoryBaseline
        {
            public bool active;
            public int raw, goods, produced, sold, shipped, imported;
            public double attendance, productive, cash, capital, revenue, rawCost, wages, productionCost, unpaid;
            public float noise, pollution;
        }
        sealed class FactoryEvidence
        {
            public double rawBlocked, stockBlocked, fundsBlocked;
            public float firstRaw=-1, firstStock=-1, firstFunds=-1;
            public HashSet<int> assigned=new HashSet<int>(), attended=new HashSet<int>(), late=new HashSet<int>();
            public HashSet<int> inputWaiting=new HashSet<int>(), outputWaiting=new HashSet<int>();
            public HashSet<int> deliveredShops=new HashSet<int>();
        }
        readonly CityModel city;
        Dictionary<int,FactoryBaseline> baseline=new Dictionary<int,FactoryBaseline>();
        Dictionary<int,FactoryEvidence> evidence=new Dictionary<int,FactoryEvidence>();
        Dictionary<int,int> savings=new Dictionary<int,int>(), rents=new Dictionary<int,int>(), homes=new Dictionary<int,int>();
        Dictionary<int,HouseholdDecisionSummary> decisions=new Dictionary<int,HouseholdDecisionSummary>();
        readonly Dictionary<int,FactoryDailySummary> demolished=new Dictionary<int,FactoryDailySummary>();
        HashSet<int> lateResidents=new HashSet<int>(), longFreight=new HashSet<int>(), arrivals=new HashSet<int>();
        int startDay, populationBefore, employedBefore, commuteSamplesBefore;
        float startMinute, commuteBefore;
        double arrivalCommute;
        bool settling;
        public CityAnalysisReport Latest {get; private set;}
        public string Failure {get; private set;}
        public CityDailyAnalysis(CityModel model)
        {city=model; BeginDay(); InitializeRuntimeHistory();}
        public void Fail(Exception error) {if(Failure==null) Failure=error.Message;}
        FactoryEvidence Evidence(int id)
        {
            if(!evidence.TryGetValue(id,out var result)) {result=new FactoryEvidence(); evidence.Add(id,result);}
            return result;
        }
        FactoryBaseline Capture(int id)
        {
            var f=city.Factory(id);
            return new FactoryBaseline {active=city.UseOf(id)==LandUse.Industrial,raw=f.raw,goods=city.Inventory(id).Stock,produced=f.produced,sold=f.sold,shipped=f.shipped,imported=f.imported,
                attendance=f.attendanceMinutes,productive=f.productiveMinutes,cash=f.cash,capital=f.capital,revenue=f.salesRevenue,rawCost=f.rawCosts,wages=f.wageCosts,
                productionCost=f.productionCosts,unpaid=f.unpaidWages,noise=f.noise,pollution=f.pollution};
        }
        void BeginDay()
        {
            startDay=city.day; startMinute=city.ResidentMinute;
            ResetRuntimeDay();
            populationBefore=city.Citizens.Count(); employedBefore=city.Employed; commuteBefore=city.AverageCommute;
            commuteSamplesBefore=CommuteObservations();
            demolished.Clear(); baseline.Clear(); evidence.Clear(); decisions.Clear(); rents.Clear(); savings.Clear(); homes.Clear(); settling=false;
            lateResidents.Clear(); longFreight.Clear(); arrivals.Clear(); arrivalCommute=0;
            foreach(var b in city.buildings)
            {
                if(b is IndustrialBuilding) baseline[b.id]=Capture(b.id);
                if(city.HousingCapacity(b.id)>0) rents[b.id]=city.Residence(b.id).askingRent;
            }
            foreach(var h in city.society.families) {savings[h.id]=h.savings; homes[h.id]=h.home;}
            foreach(var job in city.society.jobEntities)
                if(job.occupiedCitizenId>=0 && city.Factory(job.buildingId)!=null) Evidence(job.buildingId).assigned.Add(job.occupiedCitizenId);
            // Mid-day recording may see workers already inside. Count only observed attendance;
            // never invent an earlier arrival or late duration for them.
            foreach(var p in city.Citizens.Where(p=>p.atWork && p.tripId==0 && p.arrivedDay==city.day))
                if(city.Factory(p.location)!=null) Evidence(p.location).attended.Add(p.id);
        }
        public void ObserveLabour(int factory,int citizen,float attendance,float productive,float rawLost,float stockLost,float fundsLost)
        {
            if(Failure!=null) return;
            var e=Evidence(factory); e.assigned.Add(citizen); e.attended.Add(citizen);
            e.rawBlocked+=rawLost; e.stockBlocked+=stockLost; e.fundsBlocked+=fundsLost;
            float minute=city.ResidentMinute;
            if(rawLost>0 && e.firstRaw<0) e.firstRaw=minute;
            if(stockLost>0 && e.firstStock<0) e.firstStock=minute;
            if(fundsLost>0 && e.firstFunds<0) e.firstFunds=minute;
        }
        public void ObserveSale(int factory,int buyer)
        {if(Failure==null && buyer>=0 && city.UseOf(buyer)==LandUse.Commercial) Evidence(factory).deliveredShops.Add(buyer);}
        public void PrepareSettlement() {settling=true;}
        public void ObserveTraffic()
        {
            if(Failure!=null) return;
            // Threshold is in game minutes, independent of simulation day length/speed.
            ObserveRuntimeCommercial();
            foreach(var t in city.traffic.trips)
            {
                ObserveRuntimeTraffic(t);
                if(t.residentId>0 || t.cargoKind==CargoKind.Waste || t.purpose<TripPurpose.Delivery || t.status==TripStatus.Visiting
                    || t.blocked*1440/city.society.settings.secondsPerDay<30) continue;
                longFreight.Add(t.id);
                if(!t.returning && t.cargoKind==CargoKind.RawMaterial && city.Factory(t.destination)!=null) Evidence(t.destination).inputWaiting.Add(t.id);
                if(!t.returning && t.cargoKind==CargoKind.Goods && city.Factory(t.origin)!=null) Evidence(t.origin).outputWaiting.Add(t.id);
            }
        }
        static HouseholdOption Current(CityLogDetail d) => d?.currentOption;
        public void ObserveEvent(CityLogEvent evt)
        {
            if(Failure!=null) return;
            ObserveRuntimeEvent(evt);
            if(evt.type=="factory.demolished")
            {
                var f=Build(true,null).factories.Find(f=>f.id==evt.buildingId);
                if(f!=null) {f.active=false;f.state="已拆除";f.mainBottleneck="已拆除";f.raw=f.goods=0;demolished[f.id]=f;}
            }
            if(evt.type=="building.created" && city.Factory(evt.buildingId)!=null)
                baseline[evt.buildingId]=new FactoryBaseline {active=false}; // New capital is not sales revenue.
            if(evt.type=="household.created" && evt.data is Household born) {savings[born.id]=born.savings; homes[born.id]=born.home;}
            if(!settling && evt.type=="job.assigned" && evt.data is CityJob job && city.Factory(job.buildingId)!=null) Evidence(job.buildingId).assigned.Add(evt.citizenId);
            if(evt.type=="citizen.attendance" && evt.data is CityResident person)
            {
                if(arrivals.Add(person.id)) arrivalCommute+=person.lastCommute;
                if(person.lastDelay>=15) lateResidents.Add(person.id);
                if(city.Factory(person.location)!=null)
                {
                    var f=Evidence(person.location); f.attended.Add(person.id); f.assigned.Add(person.id);
                    if(person.lastDelay>=15) f.late.Add(person.id);
                }
            }
            if(evt.type=="trip.failed") ObserveTraffic();
            if(evt.type=="household.decision" && evt.data is CityLogDetail detail)
            {
                var h=city.society.families.Find(f=>f.id==evt.householdId);
                var a=Current(detail); var b=detail.selectedOption;
                var result=NewDecision(h,evt.day); result.evaluated=true;
                if(a!=null && b!=null)
                {
                    result.currentScore=a.score; result.proposedScore=b.score; result.currentRejection=a.rejection; result.proposedRejection=b.rejection;
                    result.currentWage=a.wage; result.proposedWage=b.wage; result.currentRent=a.rent; result.proposedRent=b.rent;
                    result.currentCommute=a.minutes; result.proposedCommute=b.minutes;
                    result.citizenIds=new List<int>(a.citizenIds); result.beforeJobs=new List<int>(a.jobIds); result.proposedJobs=new List<int>(b.jobIds);
                    AddFactor(result,"空间得分",b.spaceScore-a.spaceScore);
                    AddFactor(result,"私密得分",b.privacyScore-a.privacyScore);
                    AddFactor(result,"预算结余得分",b.moneyScore-a.moneyScore);
                    AddFactor(result,"通勤扣分的改善",a.timeScore-b.timeScore);
                    AddFactor(result,"环境扣分的改善",a.environmentCost-b.environmentCost);
                    AddFactor(result,"变更成本的改善",a.changeCost-b.changeCost);
                }
                foreach(var group in (detail.options??new List<HouseholdOption>()).Where(o=>!string.IsNullOrEmpty(o.rejection)).GroupBy(o=>o.rejection).OrderByDescending(g=>g.Count()).ThenBy(g=>g.Key,StringComparer.Ordinal))
                    result.rejections.Add(group.Key+"（"+group.Count()+" 个候选）");
                decisions[h.id]=result;
            }
            if(evt.type=="household.unchanged" || evt.type=="household.arrived" || evt.type=="household.moved" || evt.type=="household.jobs_changed" || evt.type=="household.left" || evt.type=="household.deferred")
            {
                var h=city.society.families.Find(f=>f.id==evt.householdId); if(h==null) return;
                if(!decisions.TryGetValue(h.id,out var result)) {result=NewDecision(h,evt.day); decisions[h.id]=result;}
                result.action=evt.type=="household.moved"?"搬家":evt.type=="household.arrived"?"迁入":evt.type=="household.jobs_changed"?"换工作":evt.type=="household.left"?"迁出":evt.type=="household.deferred"?"评估延期":"维持现状";
                result.committed=evt.type=="household.moved" || evt.type=="household.arrived" || evt.type=="household.jobs_changed" || evt.type=="household.left";
                result.reason=evt.message; result.toHome=h.home; result.actualJobs=result.citizenIds.Select(id=>h.people.Find(p=>p.id==id)?.jobId??-1).ToList();
                result.savingsAfter=h.savings; result.paidWages=h.wagePaid; result.unpaidRent=h.arrears;
            }
            if(evt.type=="city.day" && evt.data is HouseholdDay ledger)
            {
                var report=Build(false,ledger); Latest=report;
                CompleteRuntimeDay(report,ledger);
                BeginDay();
            }
        }
        HouseholdDecisionSummary NewDecision(Household h,int day)
        {
            return new HouseholdDecisionSummary {id=h.id,decisionDay=day,fromHome=homes.TryGetValue(h.id,out int oldHome)?oldHome:h.home,toHome=h.home,
                savingsBefore=savings.TryGetValue(h.id,out int before)?before:h.savings,savingsAfter=h.savings,paidWages=h.wagePaid,unpaidRent=h.arrears,
                improvementRequired=city.society.settings.improvement,citizenIds=h.people.Select(p=>p.id).ToList(),beforeJobs=h.people.Select(p=>p.jobId).ToList(),actualJobs=h.people.Select(p=>p.jobId).ToList()};
        }
        static void AddFactor(HouseholdDecisionSummary r,string name,float delta)
        {if(Math.Abs(delta)>.01f) r.factors.Add(name+" "+(delta>0?"+":"")+F(delta)+" 分");}
        public CityAnalysisReport Current() => Build(true,null);
        int Incoming(int id,CargoKind kind) => city.traffic.trips.Where(t=>!t.returning && t.destination==id && t.cargoKind==kind).Sum(t=>t.cargo);
        int CommuteObservations() => city.Citizens.Count(p=>city.ResidentJob(p)!=null && p.lastCommute>=0 && p.observedWork==city.Workplace(p)
            && city.society.families.Any(h=>h.id==p.householdId && h.home==p.observedHome));
        CityAnalysisReport Build(bool inProgress,HouseholdDay ledger)
        {
            var report=new CityAnalysisReport {day=startDay,settlementDay=ledger?.day??0,inProgress=inProgress,partial=startMinute>.01f,
                fromMinute=startMinute,toMinute=inProgress?city.ResidentMinute:1440,populationBefore=populationBefore,population=city.Citizens.Count(),
                employedBefore=employedBefore,employed=city.Employed,workforce=city.Citizens.Count(p=>p.canWork),unemployed=city.Unemployed,
                fundedJobs=city.society.jobEntities.Count(city.JobFunded),commuteBefore=commuteBefore,commuteAfter=city.AverageCommute,
                commuteSamplesBefore=commuteSamplesBefore,commuteSamplesAfter=CommuteObservations(),
                lateResidents=lateResidents.Count,commuteSamples=arrivals.Count,todayArrivalCommute=arrivals.Count==0?0:(float)(arrivalCommute/arrivals.Count),longWaitingFreight=longFreight.Count,
                pendingCommuters=city.traffic.trips.Count(t=>t.residentId>0 && t.purpose==TripPurpose.Commute && !t.returning),arrived=runtimeArrived,moved=runtimeMoved,left=runtimeLeft,
                wages=ledger?.wages??0,factoryWages=ledger?.factoryWages??0,externalWages=ledger?.externalWages??0,unpaidRent=ledger?.unpaidRent??0};
            foreach(var b in city.buildings.OrderBy(b=>b.id))
            {
                if(city.HousingCapacity(b.id)>0)
                {
                    int vacant=city.HousingCapacity(b.id)-city.Occupancy(b.id);
                    report.vacantHomes+=vacant;
                    if(vacant>0) report.housingChanges.Add("住宅 #"+b.id+" 空置 "+vacant+" / "+city.HousingCapacity(b.id)+" 户");
                    if(rents.TryGetValue(b.id,out int rent) && rent!=city.Residence(b.id).askingRent)
                    {
                        string change="住宅 #"+b.id+" 挂牌租金 "+rent+" → "+city.Residence(b.id).askingRent+"（空置 / 实际意向规则；已有租约另计）";
                        report.housingChanges.Add(change);
                        report.attention.Add(new AnalysisFinding {entityKind=AnalysisEntityKind.Residence,entityId=b.id,code="rent_changed",title="住宅 #"+b.id+" 挂牌租金变化",evidence=change,severity=Math.Abs(city.Residence(b.id).askingRent-rent)*5});
                    }
                }
                if(city.UseOf(b.id)==LandUse.Commercial && city.Inventory(b.id).Stock==0)
                    report.commercialShortages.Add("商业 #"+b.id+" 当前缺货；在途商品 "+Incoming(b.id,CargoKind.Goods));
                if(!(b is IndustrialBuilding)) continue;
                var end=Capture(b.id); if(!baseline.TryGetValue(b.id,out var begin)) begin=new FactoryBaseline();
                var observed=Evidence(b.id);
                if(!end.active && !begin.active && end.produced==begin.produced && end.wages==begin.wages) continue;
                var f=new FactoryDailySummary {id=b.id,active=end.active,roadConnected=end.active && city.EntityRoadAccess(b.id),state=end.active?city.Factory(b.id).status:"已拆除",
                    produced=end.produced-begin.produced,sold=end.sold-begin.sold,shipped=end.shipped-begin.shipped,imported=end.imported-begin.imported,
                    rawBefore=begin.raw,raw=end.raw,goodsBefore=begin.goods,goods=end.goods,incomingRaw=Incoming(b.id,CargoKind.RawMaterial),
                    outgoingGoods=city.traffic.trips.Where(t=>!t.returning && t.origin==b.id && t.cargoKind==CargoKind.Goods).Sum(t=>t.cargo),
                    assigned=observed.assigned.Count,attended=observed.attended.Count,late=observed.late.Count,slots=city.JobCapacity(b.id),inputWaiting=observed.inputWaiting.Count,outputWaiting=observed.outputWaiting.Count,
                    attendance=end.attendance-begin.attendance,productive=end.productive-begin.productive,rawBlocked=observed.rawBlocked,stockBlocked=observed.stockBlocked,fundsBlocked=observed.fundsBlocked,
                    firstRawBlock=observed.firstRaw,firstStockBlock=observed.firstStock,firstFundsBlock=observed.firstFunds,
                    cashBefore=begin.cash,cash=end.cash,capitalInflow=end.capital-begin.capital,revenue=end.revenue-begin.revenue,rawCost=end.rawCost-begin.rawCost,wageCost=end.wages-begin.wages,
                    productionCost=end.productionCost-begin.productionCost,unpaidWages=end.unpaid-begin.unpaid,noiseBefore=begin.noise,noise=end.noise,pollutionBefore=begin.pollution,pollution=end.pollution};
                f.profit=f.revenue-f.rawCost-f.wageCost-f.productionCost;
                ExplainFactory(f);
                foreach(int shop in observed.deliveredShops.OrderBy(id=>id))
                    if(city.UseOf(shop)==LandUse.Commercial && city.Inventory(shop).Stock==0) f.relatedObservations.Add("本日曾向商业 #"+shop+" 实际供货，该店当前仍缺货；不能据此确认本厂是唯一原因。");
                report.factories.Add(f);
            }
            report.factories.AddRange(demolished.Values.Where(f=>city.GetBuilding(f.id)==null));
            report.households=decisions.Values.OrderBy(d=>d.id).Select(CopyDecision).ToList();
            foreach(var d in report.households)
            {
                var h=city.society.families.Find(f=>f.id==d.id);
                if(h!=null) {d.savingsAfter=h.savings; d.paidWages=h.wagePaid; d.unpaidRent=h.arrears;}
            }
            foreach(var f in report.factories)
                foreach(var cause in f.causes) report.attention.Add(new AnalysisFinding {entityKind=AnalysisEntityKind.Factory,entityId=f.id,code=cause.code,title="工厂 #"+f.id+"："+cause.title,evidence=cause.evidence,severity=cause.severity});
            if(report.unemployed>0) report.attention.Add(new AnalysisFinding {code="unemployment",title="就业不足",evidence="劳动成员 "+report.workforce+" 人，失业 "+report.unemployed+" 人；岗位是否可达、技能匹配和资金可用须结合家庭候选摘要核对。",severity=report.unemployed*30});
            if(report.lateResidents>0) report.attention.Add(new AnalysisFinding {code="late",title="实际通勤迟到",evidence="实到样本中 "+report.lateResidents+" 人迟到至少 15 游戏分钟；未到岗者另计。",severity=report.lateResidents*20});
            if(report.longWaitingFreight>0) report.attention.Add(new AnalysisFinding {code="freight",title="货运长时间等待",evidence=report.longWaitingFreight+" 个不同任务曾连续等待至少 30 游戏分钟，包含后来恢复或失败的任务。",severity=report.longWaitingFreight*20});
            if(report.population!=report.populationBefore) report.attention.Add(new AnalysisFinding {code="population",title="人口变化",evidence=report.populationBefore+" → "+report.population+"；本次日结迁入 "+report.arrived+" 户、迁出 "+report.left+" 户。",severity=Math.Abs(report.population-report.populationBefore)*10});
            if(report.unpaidRent>0) report.attention.Add(new AnalysisFinding {code="rent",title="家庭租金支付不足",evidence="本次结算当期未付租金 ¥"+report.unpaidRent+"；查看家庭实付工资、储蓄和候选预算。",severity=report.unpaidRent});
            report.attention=report.attention.OrderByDescending(a=>a.severity).ThenBy(a=>a.title,StringComparer.Ordinal).Take(5).ToList();
            return report;
        }
        void ExplainFactory(FactoryDailySummary f)
        {
            if(!f.active) {f.mainBottleneck="已拆除"; return;}
            if(f.rawBlocked>.01) f.causes.Add(new AnalysisFinding {code="raw",title="原料不足",severity=f.rawBlocked,evidence="真实在岗期间有 "+F(f.rawBlocked)+" 工人分钟因没有可用原料而无法加工；首次观测约 "+Time(f.firstRawBlock)+"。原料末库存 "+f.raw+"，在途 "+f.incomingRaw+"。"});
            if(f.stockBlocked>.01) f.causes.Add(new AnalysisFinding {code="stock",title="成品仓满",severity=f.stockBlocked,evidence="真实在岗期间有 "+F(f.stockBlocked)+" 工人分钟被成品容量阻断；首次观测约 "+Time(f.firstStockBlock)+"。本日实售 "+f.sold+"，装车运出 "+f.shipped+"（运出不是收入）。"});
            if(!city.development.enabled && (f.fundsBlocked>.01 || f.unpaidWages>.01 || f.cash<=0 && f.cashBefore<=0 && f.revenue==0 && f.capitalInflow==0))
                f.causes.Add(new AnalysisFinding {code="cash",title="经营资金不足",severity=f.cash<=0 && f.attendance<=0?Math.Max(1,f.assigned*480+1):Math.Max(1,f.fundsBlocked),evidence="资金不足阻断 "+F(f.fundsBlocked)+" 工人分钟；现金 "+F(f.cashBefore)+" → "+F(f.cash)+"；未付实际出勤 ¥"+F(f.unpaidWages)+"。"});
            if(f.attended==0) f.causes.Add(new AnalysisFinding {code="workers",title="没有观测到工人到岗",severity=Math.Max(1,f.assigned*480),evidence="记录区间内观测到岗 0 / 曾分配员工 "+f.assigned+" 人；没有实际劳动不能生产。不能仅据此区分空缺、未出发和途中未到达。"});
            if(f.late>0) f.causes.Add(new AnalysisFinding {code="late",title="工人实际迟到",severity=f.late*15,evidence=f.late+" 名工人实际到达时较原计划班次迟到至少 15 游戏分钟；跨日到达不等于今日损失相同劳动时间，不能据人数估算减产。"});
            if(!f.roadConnected) f.relatedObservations.Add("日末建筑未连通城外入口；这是当前状态，不代表全天断路，也不能单凭它解释今日减产。");
            if(f.inputWaiting>0) f.causes.Add(new AnalysisFinding {code="input_transport",title="原料运输等待",severity=f.inputWaiting*30,evidence=f.inputWaiting+" 个原料任务曾连续等待至少 30 游戏分钟，交付前不能入库；是否与全部缺料时段重合尚未证明。"});
            if(f.outputWaiting>0) f.causes.Add(new AnalysisFinding {code="output_transport",title="商品输出等待",severity=f.outputWaiting*30,evidence=f.outputWaiting+" 个商品任务曾长时间等待；未实际交付的商品尚不能带来收入。"});
            f.causes=f.causes.OrderByDescending(a=>a.severity).ThenBy(a=>a.code,StringComparer.Ordinal).ToList();
            f.mainBottleneck=f.causes.Count>0?f.causes[0].title:f.produced>0?"未观测到明显生产阻断":"证据不足，不能判断低产原因";
            if(f.rawBlocked+f.stockBlocked+f.fundsBlocked>.01) f.effects.Add("观测到 "+F(f.rawBlocked+f.stockBlocked+f.fundsBlocked)+" 工人分钟未转化为加工劳动；其中付薪闲置会消耗经营现金。");
            if(f.unpaidWages>.01) f.effects.Add("本厂有 ¥"+F(f.unpaidWages)+" 实际出勤工资未支付，不能进入居民工资余额。");
            if(!city.development.enabled && f.cash<=0) f.effects.Add("资金已耗尽，岗位不能继续招聘，现有劳动者按真实道路返家；已有货物实际售出才可恢复现金。");
            if(f.profit<-.01) f.effects.Add("本日经营收支差额 ¥"+F(f.profit)+"；包含当日预付采购，不是按销售成本匹配的会计利润。");
            if(Math.Abs(f.noise-f.noiseBefore)>.01 || Math.Abs(f.pollution-f.pollutionBefore)>.01)
                f.effects.Add("活动暴露变化：噪声 "+F(f.noiseBefore)+" → "+F(f.noise)+"，污染 "+F(f.pollutionBefore)+" → "+F(f.pollution)+"；由实际加工活动和既有衰减规则更新。");
        }
        static string F(double value) => value.ToString("0.##",CultureInfo.InvariantCulture);
        static string Time(float minute) => minute<0?"未记录":((int)minute/60).ToString("00")+":"+((int)minute%60).ToString("00");

    }
    public sealed partial class CityModel
    {
        [NonSerialized] public CityDailyAnalysis analysis;
        internal void ObserveAnalysisLabour(int id,int citizen,float attendance,float productive,float rawLost,float stockLost,float fundsLost)
        {if(analysis==null) return; try {analysis.ObserveLabour(id,citizen,attendance,productive,rawLost,stockLost,fundsLost);} catch(Exception ex) {analysis.Fail(ex);}}
        internal void ObserveAnalysisTraffic()
        {if(analysis==null) return; try {analysis.ObserveTraffic();} catch(Exception ex) {analysis.Fail(ex);}}
        internal void ObserveAnalysisSale(int id,int buyer)
        {if(analysis==null) return; try {analysis.ObserveSale(id,buyer);} catch(Exception ex) {analysis.Fail(ex);}}
    }
}

