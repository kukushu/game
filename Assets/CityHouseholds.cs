using System;
using System.Collections.Generic;
using System.Linq;

namespace HarborCity
{
    public enum HousingKind { Apartment, Villa }
    [Serializable] public sealed class Household
    {
        public int id, home=-1, unit=-1, savings=600;
        public int rent, leaseEnd, lastMove=-100, nextReview, hardship, arrears;
        public bool resident;
        public float spacePreference, privacyPreference, timePreference, savingPreference;
        public float noiseSensitivity, pollutionSensitivity, trafficSensitivity;
        public int wagePaid, rentPaid, livingPaid, travelPaid;
        public int evaluatedDay,shoppingDay=-1;
        public string reason="等待住房和工作";
        public List<HouseholdOption> options=new List<HouseholdOption>();
        public List<CityResident> people=new List<CityResident>();
    }
    [Serializable] public sealed class HouseholdOption
    {
        public int home, rent, wage, surplus;
        public List<int> citizenIds=new List<int>(), jobIds=new List<int>();
        public float minutes, spaceScore, privacyScore, moneyScore, timeScore, changeCost, score;
        public float noise, pollution, heavyTraffic, environmentCost;
        public string rejection="";
    }
    [Serializable] public sealed class HouseholdDay
    {
        public int unemployed;
        public int tax;
        public int loanPayment;
        public int factoryWages, externalWages;
        public int day, households, population, units, employed, wages, rent, living, travel, maintenance, moved, arrived, left;
        public int openingTreasury, closingTreasury, openingSavings, closingSavings, movingCosts, unpaidRent;
        public float averageCommute;
    }
    [Serializable] public sealed class HouseholdSettings
    {
        public float secondsPerDay=120;
        public int reviewDays=7, leaseDays=30, minimumStay=7, moveCost=80, applicantsPerDay=2;
        public int apartmentCost=6000, villaCost=2800, apartmentMaintenance=24, villaMaintenance=12;
        public int apartmentRent=24, villaRent=55;
        public float improvement=8;
    }
    [Serializable] public sealed class HouseholdState
    {
        public int nextId=1, seed=19237, lastSettledDay;
        public float dayElapsed;
        public int nextJobId=1;
        public int nextCitizenId;
        public List<CityJob> jobEntities=new List<CityJob>();
        public HouseholdSettings settings=new HouseholdSettings();
        public List<Household> families=new List<Household>();
        public List<HouseholdDay> history=new List<HouseholdDay>();
        public List<string> events=new List<string>();
    }

    public sealed partial class CityModel
    {
        public HouseholdState society=new HouseholdState {lastSettledDay=1,nextCitizenId=10};
        [NonSerialized] CityRoads commuteRoads;
        [NonSerialized] int commuteRevision=-1;
        [NonSerialized] Dictionary<long,float> commuteCache=new Dictionary<long,float>();
        public int HousingCapacity(int id) => Residence(id)==null || Residence(id).abandoned?0:Residence(id).housingUnits;
        public int Occupancy(int id) => HousingCapacity(id)==0?0:society.families.Count(h=>h.resident && h.home==id && h.unit>=0 && h.unit<HousingCapacity(id));
        public int JobCapacity(int id) => society.jobEntities.Count(j=>j.buildingId==id);
        public int EmployedAt(int id) => society.jobEntities.Count(j=>j.buildingId==id && j.occupiedCitizenId>=0);
        public int Wage(int id) => society.jobEntities.Where(j=>j.buildingId==id).Select(j=>j.wage).FirstOrDefault();
        public int SkillRequired(int id) => society.jobEntities.Where(j=>j.buildingId==id).Select(j=>j.requiredSkill).FirstOrDefault();
        public float CommuteMinutes(int home,int work)
        {
            if(Residence(home)==null || GetBuilding(work)==null || JobCapacity(work)==0) return -1;
            return TravelMinutes(home,work);
        }
        public float TravelMinutes(int home,int work)
        {
            if(GetBuilding(home)==null || GetBuilding(work)==null)return -1;
            if(commuteCache==null) commuteCache=new Dictionary<long,float>();
            if(commuteRoads!=roads || commuteRevision!=roads.revision)
            { commuteCache.Clear(); commuteRoads=roads; commuteRevision=roads.revision; }
            long key=((long)home<<32)|(uint)work;
            if(commuteCache.TryGetValue(key,out float value)) return value;
            var path=roads.FindPath(AccessBuilding(home),AccessBuilding(work));
            value=-1;
            if(path.Count>0)
            {
                // Prototype map scale: one world unit represents one commute minute.
                value=CityRoads.Length(new RoadNode{x=GetBuilding(home).entranceX,z=GetBuilding(home).entranceZ},roads.Node(path[0]))
                    +CityRoads.Length(new RoadNode{x=GetBuilding(work).entranceX,z=GetBuilding(work).entranceZ},roads.Node(path[path.Count-1]));
                for(int i=1;i<path.Count;i++) value+=roads.EdgeLength(path[i-1],path[i]);
            }
            commuteCache[key]=value; return value;
        }
        float NextPreference()
        {
            society.seed=(int)(((long)society.seed*48271)%2147483647);
            return .2f+.8f*(society.seed/2147483647f);
        }
        Household NewHousehold()
        {
            var h=new Household {id=society.nextId++,spacePreference=NextPreference(),privacyPreference=NextPreference(),
                timePreference=NextPreference(),savingPreference=NextPreference()};
            int memberCount=2+h.id%3; h.savings=450+h.id%7*100;
            InitializeResidents(h,memberCount); InitializeEnvironmentPreferences(h);
            society.families.Add(h); Trace("household.created","生成城外申请家庭",h,household:h.id); return h;
        }
        void Event(string message)
        {
            society.events.Add("第 "+day+" 天 · "+message);
            if(society.events.Count>120) society.events.RemoveAt(0);
        }
        
        public HouseholdOption Evaluate(Household h,int home,bool ignoreHousingCapacity=false,bool keepJobs=false)
        {
            var o=new HouseholdOption {home=home,score=-10000};
            if(HousingCapacity(home)==0) {o.rejection="没有住房"; return o;}
            var b=Residence(home);
            o.rent=h.resident && h.home==home && day<h.leaseEnd ? h.rent : b.askingRent;
            var used=new HashSet<int>(); bool unreachable=false; int changes=0;
            foreach(var p in h.people.OrderByDescending(p=>p.skill).ThenBy(p=>p.id))
            {
                bool keep=keepJobs || p.tripId>0 || p.atWork;
                var existing=ResidentJob(p); CityJob best=keep?existing:null; float bestScore=float.MinValue;
                if(!keep && p.canWork)
                    foreach(var job in society.jobEntities)
                    {
                        if(!JobFunded(job) || used.Contains(job.id) || p.skill<job.requiredSkill || job.occupiedCitizenId>=0 && job.occupiedCitizenId!=p.id) continue;
                        float minutes=ExpectedCommute(p,home,job.buildingId); if(minutes<0) continue;
                        float score=ExpectedJobWage(job)-minutes*(.3f+h.timePreference*.7f)-(p.jobId!=job.id?5:0);
                        if(score>bestScore) {best=job; bestScore=score;}
                    }
                o.citizenIds.Add(p.id); o.jobIds.Add(best?.id ?? -1);
                if((best?.id ?? -1)!=p.jobId) changes++;
                if(best==null) continue;
                used.Add(best.id); o.wage+=ExpectedJobWage(best);
                float travel=ExpectedCommute(p,home,best.buildingId);
                if(travel<0) unreachable=true; else o.minutes+=travel;
            }
            if(!ignoreHousingCapacity && h.home!=home && Occupancy(home)>=HousingCapacity(home)) o.rejection="住房已满";
            else if(!EntityRoadAccess(home)) o.rejection="住宅道路未连通城外入口";
            else if(unreachable) o.rejection="成员工作地点不可达";
            else if(o.jobIds.All(id=>id<0) && h.people.Any(p=>p.canWork)) o.rejection="没有可达且技能匹配的空缺岗位";
            int travelCost=(int)Math.Ceiling(o.minutes*.3f);
            int members=Math.Max(1,LivingMembers(h));
            o.surplus=o.wage-o.rent-members*9-travelCost;
            if(o.rejection=="" && (o.surplus<0 || h.home!=home && h.savings<society.settings.moveCost)) o.rejection="收入或搬迁储蓄不足";
            float area=b.housing==HousingKind.Villa?110:55;
            o.spaceScore=h.spacePreference*25*(float)Math.Log(1+area/members/15);
            float privacy=b.housing==HousingKind.Villa?1:.35f-.12f*Occupancy(home)/Math.Max(1,HousingCapacity(home));
            o.privacyScore=h.privacyPreference*35*privacy;
            o.moneyScore=(.25f+h.savingPreference)*20*(float)Math.Log(1+Math.Max(0,o.surplus)/25f);
            o.timeScore=h.timePreference*o.minutes*.7f;
            o.changeCost=(h.resident && home!=h.home?10:0)+(h.resident?changes*5:0);
            var environment=EnvironmentAt(home);
            o.noise=environment.noise; o.pollution=environment.pollution; o.heavyTraffic=environment.heavyTraffic;
            o.environmentCost=o.noise*h.noiseSensitivity*18+o.pollution*h.pollutionSensitivity*30+o.heavyTraffic*h.trafficSensitivity*12;
            if(o.rejection=="") o.score=o.spaceScore+o.privacyScore+o.moneyScore-o.timeScore-o.changeCost-o.environmentCost;
            return o;
        }
        void Review(Household h,HouseholdDay report)
        {
            if(h.resident && HousingCapacity(h.home)>0
                && h.people!=null && h.people.Exists(p=>p.tripId>0 || p.atWork))
            { h.nextReview=day+1; h.reason="正在出行或尚未回家，延后住房和工作变更"; Trace("household.deferred",h.reason,household:h.id); return; }
            h.evaluatedDay=day;
            var current=Evaluate(h,h.home,keepJobs:true); var best=current;
            var options=new List<HouseholdOption> {current};
            // Small prototype: inspect all buildings. Cached road queries avoid repeated Dijkstra.
            foreach(var residence in buildings.OfType<ResidentialBuilding>())
            {
                int home=residence.id;
                if(home!=h.home && h.resident && current.rejection=="" && day-h.lastMove<society.settings.minimumStay) continue;
                var option=Evaluate(h,home); options.Add(option);
                if(home!=h.home)
                {
                    var demandOption=Evaluate(h,home,true);
                    if(demandOption.rejection=="" && demandOption.score>current.score+society.settings.improvement)
                    {
                        var building=Residence(home);
                        if(building.interestedFamilies==null) building.interestedFamilies=new List<int>();
                        if(!building.interestedFamilies.Contains(h.id)) building.interestedFamilies.Add(h.id);
                        building.applications=building.interestedFamilies.Count;
                    }
                }
                if(option.rejection=="" && option.score>best.score) best=option;
            }
            h.options=options.OrderBy(o=>o.rejection!="").ThenByDescending(o=>o.score).Take(8).ToList();
            if(!h.options.Contains(current)) h.options.Add(current);
            h.nextReview=day+society.settings.reviewDays;
            Trace("household.decision","评估住房与全体成员岗位",new CityLogDetail {previous=h.home,destination=best.home,options=options,currentOption=current,selectedOption=best},household:h.id);
            if(best.rejection!="" || best==current || current.rejection=="" && best.score<current.score+society.settings.improvement)
            {
                string reasons=string.Join("；",options.Where(o=>o.home>=0 && o.rejection!="").Select(o=>o.rejection).Distinct());
                h.reason=current.rejection!="" ? "暂未找到可行替代："+(reasons.Length>0?reasons:current.rejection) : "维持现状：替代方案改善不足（门槛 "+society.settings.improvement+"）";
                Trace("household.unchanged",h.reason,h,household:h.id,level:current.rejection!=""?"warning":"info");
                if(day>=h.leaseEnd && HousingCapacity(h.home)>0)
                {h.rent=Residence(h.home).askingRent; h.leaseEnd=day+society.settings.leaseDays;}
                return;
            }
            bool entering=!h.resident, moving=h.home!=best.home;
            if(entering && h.people!=null) foreach(var p in h.people) {p.tripId=0; p.atWork=false; p.requestedAt=-1; p.departureDay=-1;}
            // Sequential commit with daily rotating order: capacity checked again, no double booking.
            int unit=h.unit;
            if(moving)
            {
                unit=0;
                while(society.families.Any(f=>f.resident && f.home==best.home && f.unit==unit)) unit++;
                if(unit>=HousingCapacity(best.home)) return;
            }
            if(!ApplyJobPlan(h,best)) {h.nextReview=day+1; return;}
            if(moving)
            {
                h.savings-=society.settings.moveCost; report.movingCosts+=society.settings.moveCost;
                h.rent=best.rent; h.leaseEnd=day+society.settings.leaseDays; h.lastMove=day;
            }
            string before="住宅 #"+h.home;
            h.home=best.home; h.unit=unit; h.resident=true; h.hardship=0;
            if(day>=h.leaseEnd) {h.rent=best.rent; h.leaseEnd=day+society.settings.leaseDays;}
            h.reason=(entering?"迁入":moving?"搬家":"换工作")+"："+before+" → 住宅 #"+h.home+" / 家庭岗位 "+string.Join(",",best.jobIds)
                +"；评分 "+current.score.ToString("F1")+" → "+best.score.ToString("F1")+"，通勤 "+best.minutes.ToString("F1")+" 分钟";
            if(entering) report.arrived++; else if(moving) report.moved++;
            Event("家庭 #"+h.id+" "+h.reason);
            Trace(entering?"household.arrived":moving?"household.moved":"household.jobs_changed",h.reason,h,household:h.id,building:h.home);
        }
        public void RecalculateHouseholds()
        {
            SyncJobs();
            population=jobs=income=upkeep=power=water=demand=0;
            foreach(var e in roads.edges) upkeep+=(int)Math.Ceiling(roads.EdgeLength(e.a,e.b)/3);
            foreach(var building in buildings)
            {
                int i=building.id;bool access=EntityRoadAccess(i);
                if(UseOf(i)==LandUse.Power) {upkeep+=ServiceExpense(CityServiceKind.Electricity,90); if(access) power+=160;}
                if(UseOf(i)==LandUse.Water) {upkeep+=ServiceExpense(CityServiceKind.Water,65); if(access) water+=160;}
                if(UseOf(i)==LandUse.Park) upkeep+=8;
                if(UseOf(i)==LandUse.Sewage) upkeep+=ServiceExpense(CityServiceKind.Water,65);
                if(UseOf(i)==LandUse.Landfill)upkeep+=ServiceExpense(CityServiceKind.Garbage,23);
                if(UseOf(i)==LandUse.Clinic)upkeep+=ServiceExpense(CityServiceKind.Healthcare,80);
                if(GetBuilding(i) is SchoolBuilding school)upkeep+=ServiceExpense(CityServiceKind.Education,school.Grade==1?60:school.Grade==2?100:160);
                if(UseOf(i)==LandUse.FireHouse)upkeep+=ServiceExpense(CityServiceKind.Fire,80);
                if(UseOf(i)==LandUse.PoliceStation)upkeep+=ServiceExpense(CityServiceKind.Police,80);
                if(UseOf(i)==LandUse.Cemetery)upkeep+=ServiceExpense(CityServiceKind.Healthcare,60);
                if(!development.enabled && UseOf(i)==LandUse.Residential) upkeep+=Residence(i).housing==HousingKind.Villa?society.settings.villaMaintenance:society.settings.apartmentMaintenance;
            }
            foreach(var h in society.families) if(h.resident) {population+=LivingMembers(h); demand++;}
            jobs=society.jobEntities.Count(JobFunded);
            income=society.history.Count>0?(development.enabled?society.history[society.history.Count-1].tax:society.history[society.history.Count-1].rent):0;
            if(development.enabled) {upkeep+=utilities.electricity.edges.Sum(e=>(int)Math.Ceiling(CityRoads.Length(utilities.electricity.Node(e.a),utilities.electricity.Node(e.b))/30))
                +utilities.water.edges.Sum(e=>(int)Math.Ceiling(CityRoads.Length(utilities.water.Node(e.a),utilities.water.Node(e.b))/30));RecalculateUtilities();DevelopmentDemand();}
            happiness=society.families.Any(h=>h.resident)?(int)society.families.Where(h=>h.resident).Average(h=>Math.Clamp(85-h.hardship*5-(int)HouseholdCommute(h)/3,10,100)):70;
        }
        public void HouseholdTick()
        {
            if(development.enabled) {BaselineTick();return;}
            if(analysis!=null) try {analysis.PrepareSettlement();} catch(Exception ex) {analysis.Fail(ex);}
            day++; RecalculateHouseholds();
            if(day%7==1) foreach(var b in buildings.OfType<ResidentialBuilding>()) {b.applications=0; b.interestedFamilies=new List<int>();}
            var r=new HouseholdDay {day=day,openingTreasury=money,openingSavings=society.families.Sum(h=>h.savings),maintenance=upkeep};
            foreach(var h in society.families)
            {
                h.wagePaid=h.rentPaid=h.livingPaid=h.travelPaid=0;
                if(!h.resident) continue;
                h.wagePaid=0;
                foreach(var person in h.people)
                {
                    int factoryPay=(int)Math.Floor(person.factoryWageCredit);
                    person.factoryWageCredit-=factoryPay;
                    person.totalFactoryWagesPaid+=factoryPay; r.factoryWages+=factoryPay;
                    r.externalWages+=(int)Math.Round(person.earnedWages);
                    person.wagePaid=(int)Math.Round(person.earnedWages)+factoryPay; h.wagePaid+=person.wagePaid;
                    Trace("citizen.pay","日末个人实际出勤与入账",new CityLogDetail {minutes=person.workedMinutes,earned=person.earnedWages+factoryPay,amount=person.wagePaid,reason="工厂实付 "+factoryPay+"；其余为商业工资"},
                        household:h.id,citizen:person.id,job:person.jobId,building:Workplace(person));
                }
                h.savings+=h.wagePaid; r.wages+=h.wagePaid;
                foreach(var person in h.people) {person.earnedWages=0; person.workedMinutes=0;}
                h.livingPaid=Math.Min(h.savings,h.people.Count*9); h.savings-=h.livingPaid; r.living+=h.livingPaid;
                h.travelPaid=Math.Min(h.savings,(int)Math.Ceiling(HouseholdCommute(h)*.3f)); h.savings-=h.travelPaid; r.travel+=h.travelPaid;
                int due=HousingCapacity(h.home)>0?h.rent+h.arrears:0;
                h.rentPaid=Math.Min(h.savings,due); h.savings-=h.rentPaid; r.rent+=h.rentPaid;
                if(HousingCapacity(h.home)>0) h.arrears=due-h.rentPaid;
                r.unpaidRent+=Math.Max(0,h.rent-h.rentPaid);
                bool trouble=!h.people.Any(p=>ResidentJob(p)!=null && CommuteMinutes(h.home,Workplace(p))>=0) || HousingCapacity(h.home)==0 || h.rentPaid<due || h.livingPaid<h.people.Count*9;
                h.hardship=trouble?h.hardship+1:0;
                if(trouble || day>=h.leaseEnd) h.nextReview=day;
                Trace("household.settlement","家庭工资、租金、生活费、通勤费和储蓄结算",h,household:h.id,building:h.home);
            }
            // Finite outside applicant pool; their starting savings are tracked as an external inflow.
            int supplied=0;
            while(society.families.Count(h=>!h.resident)<12 && supplied<society.settings.applicantsPerDay)
            {var h=NewHousehold(); r.openingSavings+=h.savings; supplied++;}
            var order=society.families.OrderBy(h=>(h.id+day)%Math.Max(1,society.nextId)).ToList();
            foreach(var h in order)
            {
                if(day>=h.nextReview) Review(h,r);
                if(h.resident && h.hardship>=30)
                {
                    h.resident=false; h.home=h.unit=-1;
                    foreach(var p in h.people) {ReleaseJob(p); if(traffic!=null) traffic.trips.RemoveAll(t=>t.residentId==p.id); p.tripId=0; p.atWork=false;} h.reason="迁出：连续 30 天无法维持居住和就业";
                    h.nextReview=day+30; r.left++; Event("家庭 #"+h.id+" "+h.reason);
                    Trace("household.left",h.reason,h,household:h.id,level:"warning");
                }
            }
            // Vacancy adjusts only advertised rent; existing leases remain locked.
            if(day%7==0) foreach(var b in buildings.OfType<ResidentialBuilding>())
            {
                int i=b.id; int occupied=Occupancy(i), previousRent=b.askingRent;
                b.vacantDays=occupied<HousingCapacity(i)?b.vacantDays+7:0;
                if(b.vacantDays>=14) b.askingRent=Math.Max(12,b.askingRent-1);
                // Raise only when a real unmatched applicant can afford and prefers this full building.
                else if(occupied==HousingCapacity(i) && b.applications>0)
                    b.askingRent=Math.Min(b.housing==HousingKind.Villa?90:45,b.askingRent+1);
                if(b.askingRent!=previousRent) Trace("housing.rent_changed","挂牌租金随空置或真实申请需求调整",new CityLogDetail {previous=previousRent,amount=b.askingRent,count=occupied},building:i);
            }
            money+=r.rent-r.maintenance; society.lastSettledDay=day;
            r.households=society.families.Count(h=>h.resident); r.population=society.families.Where(h=>h.resident).Sum(h=>LivingMembers(h));
            r.units=buildings.OfType<ResidentialBuilding>().Sum(b=>b.housingUnits); r.employed=Employed; r.unemployed=Unemployed;
            r.averageCommute=AverageCommute;
            r.closingTreasury=money; r.closingSavings=society.families.Sum(h=>h.savings);
            society.history.Add(r); if(society.history.Count>180) society.history.RemoveAt(0);
            Trace("city.day","每日城市统计与资金核对",r);
            foreach(var b in buildings.OfType<IndustrialBuilding>())
                Trace("factory.day","工厂累计生产、出勤、库存及环境记录",new FactoryReport {state=b.factory,goods=Inventory(b.id).Stock},building:b.id);
            if(r.closingTreasury!=r.openingTreasury+r.rent-r.maintenance || r.closingSavings!=r.openingSavings+r.wages-r.rent-r.living-r.travel-r.movingCosts)
                Trace("invariant.cash","日结资金不守恒",r,level:"error");
            RecalculateHouseholds();
        }
        public bool ValidHouseholds()
        {
            if(society==null || society.settings==null || society.families==null || society.history==null || society.events==null
                || society.nextId<1 || society.seed<=0 || society.seed>=2147483647 || society.lastSettledDay!=day
                || float.IsNaN(society.dayElapsed) || float.IsInfinity(society.dayElapsed) || society.dayElapsed<0
                || float.IsNaN(society.settings.secondsPerDay) || society.settings.secondsPerDay<1 || society.settings.secondsPerDay>3600
                || society.dayElapsed>=society.settings.secondsPerDay || society.settings.reviewDays<1 || society.settings.leaseDays<1
                || society.settings.minimumStay<0 || society.settings.moveCost<0 || society.settings.applicantsPerDay<0 || society.settings.applicantsPerDay>100
                || society.settings.apartmentCost<0 || society.settings.villaCost<0 || society.settings.apartmentMaintenance<0 || society.settings.villaMaintenance<0
                || society.settings.apartmentRent<0 || society.settings.villaRent<0 || float.IsNaN(society.settings.improvement) || society.settings.improvement<0 || society.settings.improvement>1000
                || society.families.Count>10000 || society.history.Count>180 || society.events.Count>120) return false;
            var ids=new HashSet<int>(); var units=new HashSet<string>();
            foreach(var h in society.families)
            {
                if(h==null || h.shoppingDay< -1 || h.shoppingDay>day || h.id<1 || h.id>=society.nextId || !ids.Add(h.id) || h.savings<0 || h.rent<0 || h.arrears<0 || h.options==null) return false;
                if(h.people!=null && h.people.Count>0)
                {
                    for(int p=0;p<h.people.Count;p++)
                    {
                        if(h.people[p]==null || h.people[p].age<0 || h.people[p].age>120
                            || string.IsNullOrEmpty(h.people[p].name)) return false;
                        var person=h.people[p];
                        if(person.location < -1 || person.location>=0 && GetBuilding(person.location)==null
                            || person.observedHome < -1 || person.observedHome>=0 && Residence(person.observedHome)==null
                            || person.observedWork < -1 || person.observedWork>=0 && GetBuilding(person.observedWork)==null) return false;
                        foreach(float value in new[]{person.health,person.earnedWages,person.workedMinutes,person.lastCommute,person.lastDelay,person.retryAt,person.requestedAt})
                            if(float.IsNaN(value) || float.IsInfinity(value)) return false;
                        if(person.health<0 || person.health>100 || person.tripId<0 || person.earnedWages<0 || person.workedMinutes<0 || person.totalFactoryWagesPaid<0 || !FinanceNumber(person.factoryWageCredit)) return false;
                        if(person.tripId>0 && (traffic==null || !traffic.trips.Exists(t=>t.id==person.tripId && t.residentId==person.id && t.householdId==h.id))) return false;
                    }
                }
                foreach(float p in new[]{h.spacePreference,h.privacyPreference,h.timePreference,h.savingPreference})
                    if(float.IsNaN(p) || p<0 || p>1) return false;
                if(h.home < -1 || h.home>=0 && Residence(h.home)==null || h.resident && Residence(h.home)==null) return false;
                // Demolition can leave temporarily displaced households pending their next review.
                if(h.resident && HousingCapacity(h.home)>0 && (h.unit<0 || h.unit>=HousingCapacity(h.home) || !units.Add(h.home+":"+h.unit))) return false;
            }
            if(traffic!=null)
                foreach(var trip in traffic.trips) if(trip.residentId>0 && !society.families.Any(h=>h.id==trip.householdId && h.people!=null && h.people.Any(p=>p.id==trip.residentId && p.tripId==trip.id))) return false;
            return ValidJobs() && ValidIndustry() && ValidHealthcare() && ValidEducation();
        }
    }
}
