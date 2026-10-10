using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HarborCity
{
    public enum AnalysisEntityKind { City, Factory, Commercial, Household, Resident, Trip, Residence }

    // Bounded observation history. It is never part of the gameplay save.
    public sealed class AnalysisRingBuffer<T> : IEnumerable<T>
    {
        readonly T[] items;
        int first;
        public int Count {get; private set;}
        public int Capacity=>items.Length;
        public AnalysisRingBuffer(int capacity) {if(capacity<1) throw new ArgumentOutOfRangeException(nameof(capacity));items=new T[capacity];}
        public void Add(T item)
        {
            if(Count<items.Length) {items[(first+Count)%items.Length]=item;Count++;}
            else {items[first]=item;first=(first+1)%items.Length;}
        }
        public IEnumerator<T> GetEnumerator() {for(int i=0;i<Count;i++)yield return items[(first+i)%items.Length];}
        IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
    }
    [Serializable] public sealed class CityKeyEvent
    {
        public long sequence;
        public int day,entityId,affectedCount=1;
        public float minute;
        public AnalysisEntityKind entityKind;
        public string code,message;
        public CityKeyEvent Copy()=>(CityKeyEvent)MemberwiseClone();
    }
    [Serializable] public sealed class CityLiveState
    {
        public int day,population,households,applicants,employed,unemployed,workforce,jobs,fundedJobs;
        public int units,vacantUnits,residentsInTransit,freightInTransit,blockedResidents,blockedFreight,atWork;
        public int rawEmptyFactories,stockFullFactories,unfundedFactories,emptyCommercial,treasury;
        public int loanDebt,loanPaymentNextDay;
        public float minute,clock,averageCommute;
        public double pendingWages;
    }
    [Serializable] public sealed class CityTodayState
    {
        public int produced,sold,shipped,imported,arrived,moved,left,late,longWaitingFreight;
        public double attendance,productive,rawBlocked,stockBlocked,fundsBlocked,employerWages,externalAccrued,earnedWages;
        public double revenue,rawCost,productionCost;
    }
    [Serializable] public sealed class CityHistorySample
    {
        public int day,population,households,employed,unemployed,treasury,wages,rent,tax,maintenance,arrived,moved,left;
        public int loanPayment;
        public int produced,sold;
        public float averageCommute;
        public bool industryKnown,partial;
        public int treasuryError,savingsError;
    }
    [Serializable] public sealed class ResidentAnalysisState
    {
        public int id,householdId,age,jobId,work,home,tripId,wage;
        public bool canWork,atWork,resident,located,travelling,hasCommuteObservation;
        public string name,activity,reason,location,destination;
        public float x,z,workedMinutes,commute,late,progress;
        public double pendingWages;
    }
    [Serializable] public sealed class HouseholdAnalysisState
    {
        public int id,home,unit,rent,savings,arrears,income,expectedIncome,reviewDay,evaluatedDay,employed;
        public bool resident,hasDecision;
        public float commute;
        public string reason;
        public HouseholdDecisionSummary decision;
        public List<HouseholdOption> candidates=new List<HouseholdOption>();
        public List<ResidentAnalysisState> members=new List<ResidentAnalysisState>();
    }
    [Serializable] public sealed class BusinessAnalysisState
    {
        public int id,goods,raw,incomingGoods,incomingRaw,outgoingGoods,slots,fundedJobs,attending,wage,skill;
        public bool industrial,connected,processing,hasTodayReport,hasYesterdayReport;
        public string status;
        public float x,z,progress,noise,pollution;
        public double cash,capital,totalRevenue,totalRawCosts,totalWages,totalProductionCosts,profit,cashError;
        public FactoryDailySummary today,yesterday;
        public List<int> employees=new List<int>(),trips=new List<int>();
    }
    [Serializable] public sealed class HousingAnalysisState
    {
        public int id,units,occupied,rent,applications,population;
        public bool connected;
        public HousingKind kind;
    }
    [Serializable] public sealed class TripAnalysisState
    {
        public int id,origin,destination,residentId,householdId,cargo;
        public bool returning;
        public string purpose,status,from,to;
        public double blockedMinutes;
    }
    [Serializable] public sealed class CityAnalysisState
    {
        public int schema=1;
        public long version;
        public string failure;
        public CityLiveState live;
        public CityTodayState today;
        public CityAnalysisReport currentDay,lastDay;
        public List<AnalysisFinding> findings=new List<AnalysisFinding>();
        public List<CityHistorySample> history=new List<CityHistorySample>();
        public List<CityAnalysisReport> reports=new List<CityAnalysisReport>();
        public List<CityKeyEvent> timeline=new List<CityKeyEvent>();
        public List<BusinessAnalysisState> businesses=new List<BusinessAnalysisState>();
        public List<HousingAnalysisState> housing=new List<HousingAnalysisState>();
        public List<HouseholdAnalysisState> households=new List<HouseholdAnalysisState>();
        public List<ResidentAnalysisState> residents=new List<ResidentAnalysisState>();
        public List<TripAnalysisState> trips=new List<TripAnalysisState>();
        public BusinessAnalysisState Business(int id)=>businesses.Find(b=>b.id==id);
        public HouseholdAnalysisState Household(int id)=>households.Find(h=>h.id==id);
        public ResidentAnalysisState Resident(int id)=>residents.Find(p=>p.id==id);
        public TripAnalysisState Trip(int id)=>trips.Find(t=>t.id==id);
    }

    public sealed partial class CityDailyAnalysis
    {
        AnalysisRingBuffer<CityKeyEvent> keyEvents=new AnalysisRingBuffer<CityKeyEvent>(750);
        AnalysisRingBuffer<CityHistorySample> history=new AnalysisRingBuffer<CityHistorySample>(180);
        AnalysisRingBuffer<CityAnalysisReport> completedReports=new AnalysisRingBuffer<CityAnalysisReport>(180);
        readonly Dictionary<int,string> factoryStates=new Dictionary<int,string>();
        readonly Dictionary<int,bool> commercialEmpty=new Dictionary<int,bool>();
        readonly Dictionary<int,HouseholdDecisionSummary> latestDecisions=new Dictionary<int,HouseholdDecisionSummary>();
        readonly Dictionary<int,double> externalBaseline=new Dictionary<int,double>();
        readonly HashSet<int> lateTimeline=new HashSet<int>(),waitingEpisodes=new HashSet<int>();
        int runtimeArrived,runtimeMoved,runtimeLeft;
        long eventSequence,runtimeVersion;
        public CityAnalysisState State {get; private set;}

        void InitializeRuntimeHistory()
        {
            foreach(var d in city.society.history) history.Add(HistorySample(d,null));
            foreach(var b in city.buildings.OfType<IndustrialBuilding>()) factoryStates[b.id]=b.factory.status;
            foreach(var b in city.buildings.OfType<CommercialBuilding>()) commercialEmpty[b.id]=b.stock==0;
        }
        void ResetRuntimeDay()
        {
            runtimeArrived=runtimeMoved=runtimeLeft=0;lateTimeline.Clear();externalBaseline.Clear();runtimeVersion++;
            foreach(var p in city.society.families.SelectMany(h=>h.people))externalBaseline[p.id]=p.earnedWages;
        }
        static CityHistorySample HistorySample(HouseholdDay d,CityAnalysisReport report)
        {
            return new CityHistorySample {day=d.day-1,population=d.population,households=d.households,employed=d.employed,unemployed=d.unemployed,
                treasury=d.closingTreasury,wages=d.wages,rent=d.rent,tax=d.tax,maintenance=d.maintenance,loanPayment=d.loanPayment,arrived=d.arrived,moved=d.moved,left=d.left,
                averageCommute=d.averageCommute,industryKnown=report!=null,partial=report?.partial??false,
                produced=report?.factories.Sum(f=>f.produced)??0,sold=report?.factories.Sum(f=>f.sold)??0,
                treasuryError=d.closingTreasury-d.openingTreasury-d.tax-d.rent+d.maintenance+d.loanPayment,
                savingsError=d.closingSavings-d.openingSavings-d.wages+d.rent+d.living+d.travel+d.movingCosts};
        }
        void CompleteRuntimeDay(CityAnalysisReport report,HouseholdDay ledger)
        {
            history.Add(HistorySample(ledger,report));completedReports.Add(report);
            foreach(var d in report.households)latestDecisions[d.id]=CopyDecision(d);
        }
        void Key(string code,string message,AnalysisEntityKind kind,int id)
        {
            keyEvents.Add(new CityKeyEvent {sequence=++eventSequence,day=startDay,minute=settling?1440:city.ResidentMinute,
                code=code,message=message,entityKind=kind,entityId=id});runtimeVersion++;
        }
        void ObserveRuntimeEvent(CityLogEvent e)
        {
            if(!e.type.StartsWith("session.",StringComparison.Ordinal) && e.type!="trip.state" && e.type!="trip.resume" && e.type!="network.audit" && e.type!="building.access")runtimeVersion++;
            if(e.type=="household.arrived")runtimeArrived++;
            if(e.type=="household.moved")runtimeMoved++;
            if(e.type=="household.left")runtimeLeft++;
            if(e.type=="household.arrived" || e.type=="household.moved" || e.type=="household.left" || e.type=="household.jobs_changed")
                Key(e.type,e.message,AnalysisEntityKind.Household,e.householdId);
            if(e.type=="job.removed" && e.data is CityJob job)
            {
                var last=keyEvents.LastOrDefault();
                float minute=settling?1440:city.ResidentMinute;
                if(last!=null && last.code==e.type && last.entityId==job.buildingId && last.day==startDay && last.minute==minute)
                {last.affectedCount++;last.message="工作场所 #"+job.buildingId+"："+last.affectedCount+" 个岗位失效";}
                else Key(e.type,"工作场所 #"+job.buildingId+"：1 个岗位失效",city.Factory(job.buildingId)!=null?AnalysisEntityKind.Factory:AnalysisEntityKind.Commercial,job.buildingId);
            }
            if(e.type=="citizen.attendance" && e.data is CityResident p && p.lastDelay>=15 && lateTimeline.Add(p.id))
                Key("resident.late","居民 #"+p.id+" 实际迟到 "+F(p.lastDelay)+" 游戏分钟",AnalysisEntityKind.Resident,p.id);
            if(e.type=="factory.status" && e.data is FactoryState f)
            {
                factoryStates.TryGetValue(e.buildingId,out string before);
                // Routine off-shift '缺工' changes do not flood the human timeline.
                if(f.status=="缺工" && (city.ResidentMinute<600 || city.ResidentMinute>=1020))return;
                if(before!=f.status)
                {
                    factoryStates[e.buildingId]=f.status;
                    Key(e.type,"工厂 #"+e.buildingId+"："+(before??"新厂")+" → "+f.status,AnalysisEntityKind.Factory,e.buildingId);
                }
            }
            if(e.type=="factory.demolished") {factoryStates.Remove(e.buildingId);Key(e.type,"工厂 #"+e.buildingId+" 已拆除，库存计损失，已付工资保留",AnalysisEntityKind.Factory,e.buildingId);}
            if(e.type=="invariant.cash")Key(e.type,e.message,AnalysisEntityKind.City,-1);
        }
        void ObserveRuntimeTraffic(TrafficTrip t)
        {
            bool blocked=t.residentId==0 && t.purpose>=TripPurpose.Delivery && t.status!=TripStatus.Visiting
                && t.blocked*1440/city.society.settings.secondsPerDay>=30;
            if(!blocked) {waitingEpisodes.Remove(t.id);return;}
            if(waitingEpisodes.Add(t.id))
                Key(t.cargoKind==CargoKind.Waste?"garbage.blocked":"freight.blocked",(t.cargoKind==CargoKind.Waste?"垃圾车 #":"货运 #")+t.id+" 连续受阻至少 30 游戏分钟（"+t.origin+" → "+t.destination+"）",AnalysisEntityKind.Trip,t.id);
        }
        void ObserveRuntimeCommercial()
        {
            foreach(var b in city.buildings.OfType<CommercialBuilding>())
            {
                bool empty=b.stock==0;
                if(commercialEmpty.TryGetValue(b.id,out bool before) && before!=empty)
                    Key(empty?"commercial.empty":"commercial.restocked","商业 #"+b.id+(empty?" 库存耗尽，等待补货":" 补货实际到达，库存恢复"),AnalysisEntityKind.Commercial,b.id);
                commercialEmpty[b.id]=empty;
            }
        }
        static HouseholdDecisionSummary CopyDecision(HouseholdDecisionSummary d)
        {
            return new HouseholdDecisionSummary {id=d.id,decisionDay=d.decisionDay,fromHome=d.fromHome,toHome=d.toHome,savingsBefore=d.savingsBefore,savingsAfter=d.savingsAfter,
                paidWages=d.paidWages,unpaidRent=d.unpaidRent,evaluated=d.evaluated,committed=d.committed,action=d.action,reason=d.reason,
                currentRejection=d.currentRejection,proposedRejection=d.proposedRejection,currentScore=d.currentScore,proposedScore=d.proposedScore,improvementRequired=d.improvementRequired,
                currentWage=d.currentWage,proposedWage=d.proposedWage,currentRent=d.currentRent,proposedRent=d.proposedRent,currentCommute=d.currentCommute,proposedCommute=d.proposedCommute,
                citizenIds=new List<int>(d.citizenIds),beforeJobs=new List<int>(d.beforeJobs),proposedJobs=new List<int>(d.proposedJobs),actualJobs=new List<int>(d.actualJobs),
                factors=new List<string>(d.factors),rejections=new List<string>(d.rejections)};
        }
        static HouseholdOption CopyOption(HouseholdOption o)
        {
            return new HouseholdOption {home=o.home,rent=o.rent,wage=o.wage,surplus=o.surplus,minutes=o.minutes,spaceScore=o.spaceScore,privacyScore=o.privacyScore,
                moneyScore=o.moneyScore,timeScore=o.timeScore,changeCost=o.changeCost,score=o.score,noise=o.noise,pollution=o.pollution,heavyTraffic=o.heavyTraffic,environmentCost=o.environmentCost,
                rejection=o.rejection,citizenIds=new List<int>(o.citizenIds),jobIds=new List<int>(o.jobIds)};
        }
        string Place(int id)=>id<0?"城外 / 无目的地":city.GetBuilding(id)==null?"已移除 #"+id:city.UseOf(id)+" #"+id;

        // A detached presentation snapshot, refreshed by the controller, never by
        // simulation transactions. UI formats fields; it does not aggregate entities.
        public CityAnalysisState Refresh(bool force=false)
        {
            if(!force && State!=null && State.version==runtimeVersion && State.live.clock==city.traffic.clock)return State;
            var report=Current();var s=new CityAnalysisState {version=runtimeVersion,failure=Failure,currentDay=report,lastDay=Latest,
                history=history.ToList(),reports=completedReports.ToList(),timeline=keyEvents.Select(e=>e.Copy()).ToList()};
            var live=new CityLiveState {day=city.day,minute=city.ResidentMinute,clock=city.traffic.clock,treasury=city.money,loanDebt=city.LoanDebt,loanPaymentNextDay=city.LoanPaymentNextDay,
                population=city.Citizens.Count(),households=city.society.families.Count(h=>h.resident),applicants=city.society.families.Count(h=>!h.resident),
                employed=city.Employed,unemployed=city.Unemployed,workforce=city.Citizens.Count(p=>p.canWork),jobs=city.society.jobEntities.Count,
                fundedJobs=city.society.jobEntities.Count(city.JobFunded),averageCommute=city.AverageCommute};s.live=live;
            var activeTrips=city.traffic.trips;
            live.residentsInTransit=activeTrips.Count(t=>t.residentId>0);live.freightInTransit=activeTrips.Count(t=>t.residentId==0);
            live.blockedResidents=activeTrips.Count(t=>t.residentId>0 && (t.blocked>.1f || t.status==TripStatus.Waiting));
            live.blockedFreight=activeTrips.Count(t=>t.residentId==0 && (t.blocked>.1f || t.status==TripStatus.Waiting));
            foreach(var t in activeTrips)s.trips.Add(new TripAnalysisState {id=t.id,origin=t.origin,destination=t.destination,residentId=t.residentId,householdId=t.householdId,
                cargo=t.cargo,returning=t.returning,from=Place(t.origin),to=Place(t.destination),
                purpose=t.cargoKind==CargoKind.Waste?(t.purpose==TripPurpose.GarbageTransfer?"转运垃圾":"收集垃圾"):t.purpose==TripPurpose.Shopping?"购物":t.residentId>0?"通勤":t.cargoKind==CargoKind.RawMaterial?"进口原料":t.purpose==TripPurpose.Export?"出口商品":t.purpose==TripPurpose.Import?"进口商品":"配送商品",
                status=t.status==TripStatus.Visiting?"装卸停留":t.status==TripStatus.Waiting?"等待道路恢复":t.blocked>.1f?"排队":"行驶中",
                blockedMinutes=t.blocked*1440/city.society.settings.secondsPerDay});
            waitingEpisodes.RemoveWhere(id=>!activeTrips.Any(t=>t.id==id));
            foreach(int id in commercialEmpty.Keys.Where(id=>city.GetBuilding(id)==null).ToList())commercialEmpty.Remove(id);
            foreach(var h in city.society.families)
            {
                var hs=new HouseholdAnalysisState {id=h.id,home=h.home,unit=h.unit,rent=h.rent,savings=h.savings,arrears=h.arrears,resident=h.resident,
                    income=h.wagePaid,expectedIncome=city.HouseholdSalary(h),commute=city.HouseholdCommute(h),reviewDay=h.nextReview,evaluatedDay=h.evaluatedDay,reason=h.reason,employed=h.people.Count(p=>city.ResidentJob(p)!=null),
                    candidates=h.options.Select(CopyOption).ToList()};
                var decision=report.households.Find(d=>d.id==h.id);
                if(decision==null)latestDecisions.TryGetValue(h.id,out decision);
                hs.decision=decision==null?null:CopyDecision(decision);
                hs.hasDecision=hs.decision!=null;
                foreach(var p in h.people.Where(p=>!p.dead))
                {
                    var a=city.ObserveResident(h,p);int work=city.Workplace(p);
                    var ps=new ResidentAnalysisState {id=p.id,householdId=h.id,name=p.name,age=p.age,canWork=p.canWork,resident=h.resident,home=h.home,work=work,
                        jobId=p.jobId,tripId=p.tripId,wage=city.ResidentWage(p),atWork=p.atWork,activity=a.state,reason=a.reason,located=a.located,travelling=a.travelling,
                        x=a.x,z=a.z,progress=a.progress,location=a.travelling?"道路上 / 车辆 #"+p.tripId:Place(a.building),destination=Place(a.destination),
                        workedMinutes=p.workedMinutes,pendingWages=p.earnedWages+p.factoryWageCredit,commute=p.lastCommute,late=p.lastDelay,
                        hasCommuteObservation=h.resident && p.canWork && city.ResidentJob(p)!=null && p.lastCommute>=0 && p.observedHome==h.home && p.observedWork==work};
                    hs.members.Add(ps);s.residents.Add(ps);
                    if(h.resident) {live.pendingWages+=ps.pendingWages;if(p.atWork && p.tripId==0)live.atWork++;}
                }
                s.households.Add(hs);
            }
            foreach(var b in city.buildings)
            {
                if(b is ResidentialBuilding r)
                {
                    int occupied=city.Occupancy(r.id);live.units+=r.housingUnits;live.vacantUnits+=r.housingUnits-occupied;
                    s.housing.Add(new HousingAnalysisState {id=r.id,kind=r.housing,units=r.housingUnits,occupied=occupied,rent=r.askingRent,applications=r.applications,connected=city.BuildingAccess(r.id),population=s.households.Where(h=>h.resident && h.home==r.id).Sum(h=>h.members.Count)});
                }
                if(!(b is IGoodsBuilding))continue;
                var f=city.Factory(b.id);var workers=s.residents.Where(p=>p.resident && p.work==b.id).ToList();
                var business=new BusinessAnalysisState {id=b.id,industrial=f!=null,connected=city.BuildingAccess(b.id),goods=city.Goods(b.id),x=b.x,z=b.z,
                    incomingGoods=Incoming(b.id,CargoKind.Goods),incomingRaw=Incoming(b.id,CargoKind.RawMaterial),slots=city.JobCapacity(b.id),
                    fundedJobs=city.society.jobEntities.Count(j=>j.buildingId==b.id && city.JobFunded(j)),employees=workers.Select(p=>p.id).ToList(),
                    attending=workers.Count(p=>p.atWork && p.tripId==0),wage=city.Wage(b.id),skill=city.SkillRequired(b.id),
                    trips=activeTrips.Where(t=>t.residentId==0 && (t.origin==b.id || t.destination==b.id || t.home==b.id)).Select(t=>t.id).ToList(),
                    outgoingGoods=activeTrips.Where(t=>!t.returning && t.origin==b.id && t.cargoKind==CargoKind.Goods).Sum(t=>t.cargo)};
                if(f!=null)
                {
                    business.status=f.status;business.raw=f.raw;business.cash=f.cash;business.capital=f.capital;business.progress=f.progress;business.processing=f.processing;
                    business.noise=f.noise;business.pollution=f.pollution;business.totalRevenue=f.salesRevenue;business.totalRawCosts=f.rawCosts;
                    business.totalWages=f.wageCosts;business.totalProductionCosts=f.productionCosts;business.profit=f.Profit;business.cashError=f.CashError;
                    business.today=report.factories.Find(item=>item.id==b.id);business.yesterday=Latest?.factories.Find(item=>item.id==b.id);
                    business.hasTodayReport=business.today!=null;business.hasYesterdayReport=business.yesterday!=null;
                    if(f.raw==0)live.rawEmptyFactories++;if(business.goods>=FactoryState.GoodsCapacity)live.stockFullFactories++;if(!city.development.enabled && f.cash<=0)live.unfundedFactories++;
                }
                else {business.status=!business.connected?"道路中断":business.goods==0?"缺货，等待补货":"有库存";if(business.goods==0)live.emptyCommercial++;}
                s.businesses.Add(business);
            }
            double external=city.society.families.SelectMany(h=>h.people).Sum(p=>Math.Max(0,p.earnedWages-(externalBaseline.TryGetValue(p.id,out double start)?start:0)));
            s.today=new CityTodayState {produced=report.factories.Sum(f=>f.produced),sold=report.factories.Sum(f=>f.sold),shipped=report.factories.Sum(f=>f.shipped),
                imported=report.factories.Sum(f=>f.imported),arrived=report.arrived,moved=report.moved,left=report.left,late=report.lateResidents,longWaitingFreight=report.longWaitingFreight,
                attendance=report.factories.Sum(f=>f.attendance),productive=report.factories.Sum(f=>f.productive),rawBlocked=report.factories.Sum(f=>f.rawBlocked),
                stockBlocked=report.factories.Sum(f=>f.stockBlocked),fundsBlocked=report.factories.Sum(f=>f.fundsBlocked),employerWages=report.factories.Sum(f=>f.wageCost),
                externalAccrued=external,earnedWages=report.factories.Sum(f=>f.wageCost)+external,revenue=report.factories.Sum(f=>f.revenue),
                rawCost=report.factories.Sum(f=>f.rawCost),productionCost=report.factories.Sum(f=>f.productionCost)};
            BuildRuntimeFindings(s);
            State=s;return s;
        }
        void BuildRuntimeFindings(CityAnalysisState s)
        {
            s.findings.AddRange(s.currentDay.attention.Where(a=>a.code!="workers" || s.live.minute>=600));
            if(s.lastDay!=null)
                foreach(var f in s.lastDay.attention.Where(f=>f.entityKind==AnalysisEntityKind.Factory || f.entityKind==AnalysisEntityKind.Residence))
                    if(!s.findings.Any(now=>now.entityKind==f.entityKind && now.entityId==f.entityId && now.code==f.code))
                        s.findings.Add(new AnalysisFinding {entityKind=f.entityKind,entityId=f.entityId,code=f.code,title="昨日 D"+s.lastDay.day+" · "+f.title,
                            evidence="已结束的"+(s.lastDay.partial?"部分日":"完整日")+"证据："+f.evidence,severity=f.severity});
            foreach(var b in s.businesses)
            {
                if(!b.industrial && b.goods==0)
                    s.findings.Add(new AnalysisFinding {entityKind=AnalysisEntityKind.Commercial,entityId=b.id,code="commercial_empty",title="商业 #"+b.id+" 库存为 0",
                        evidence="当前缺货；在途补货 "+b.incomingGoods+" 件；"+(b.connected?"等待实际交付。":"当前道路未接通。"),severity=30});
                if(b.industrial && b.today!=null)
                {
                    var cause=b.today.causes.FirstOrDefault(c=>c.code!="workers" || s.live.minute>=600);
                    if(cause!=null && !s.findings.Any(a=>a.entityKind==AnalysisEntityKind.Factory && a.entityId==b.id && a.code==cause.code))
                        s.findings.Add(new AnalysisFinding {entityKind=AnalysisEntityKind.Factory,entityId=b.id,code=cause.code,title="工厂 #"+b.id+"："+cause.title,evidence=cause.evidence,severity=cause.severity});
                }
            }
            foreach(var d in s.currentDay.households.Concat(s.lastDay?.households??new List<HouseholdDecisionSummary>()).Where(d=>d.committed).GroupBy(d=>d.id).Select(g=>g.First()))
                s.findings.Add(new AnalysisFinding {entityKind=AnalysisEntityKind.Household,entityId=d.id,code="household_change",title="家庭 #"+d.id+" "+d.action,
                    evidence="动作日 D"+d.decisionDay+"；住宅 #"+d.fromHome+" → #"+d.toHome+"；"+(d.evaluated?"候选通勤 "+F(d.currentCommute)+" → "+F(d.proposedCommute)+" 分钟；":"未评估候选；")+d.reason,
                    severity=Math.Max(10,Math.Abs(d.proposedScore-d.currentScore))});
            s.findings=s.findings.OrderByDescending(a=>a.severity).ThenBy(a=>a.code,StringComparer.Ordinal).ThenBy(a=>a.entityId).Take(12).ToList();
        }
    }
}
