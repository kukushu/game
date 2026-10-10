using System;
using System.Collections.Generic;
using System.Linq;

namespace HarborCity
{
    [Serializable] public sealed class DevelopmentProject
    {
        public int id;
        public float started,completeAt;
        public BuildingSaveData building;
        public List<string> cells=new List<string>();
    }
    [Serializable] public sealed class CityDevelopmentState
    {
        // Separate rulesets prevent the prototype's government rent economy from
        // silently combining with privately grown zoning. Rates/formulae are provisional.
        public bool enabled;
        // Trigger delay is an exposed provisional parameter, not a verified CS1 constant.
        public int provisionalOutageDays=3;
        public int nextProjectId=1,residentialTax=9,commercialTax=9,industrialTax=9;
        public int electricityBudget=100,waterBudget=100,garbageBudget=100,healthcareBudget=100,educationBudget=100,fireBudget=100,policeBudget=100;
        public float nextGrowth;
        public List<DevelopmentProject> projects=new List<DevelopmentProject>();
        [NonSerialized] public int revision;
    }
    public sealed partial class CityModel
    {
        public CityDevelopmentState development=new CityDevelopmentState();
        [NonSerialized] public Func<float,float,float> developmentHeight;
        [NonSerialized] public int residentialDemand,commercialDemand,industrialDemand;
        // Placeholder asset footprints, not the original game's spawn probabilities.
        static readonly (int width,int depth)[] growthFootprints={(2,2),(3,2),(2,3),(4,3),(3,4),(4,4),(1,2),(1,1),(2,1),(3,3)};
        public void DevelopmentDemand()
        {
            int vacant=buildings.OfType<ResidentialBuilding>().Sum(b=>HousingCapacity(b.id)-Occupancy(b.id));
            int vacantJobs=society.jobEntities.Count(j=>j.occupiedCitizenId<0);
            // Observable demand estimates, not a claim to CS1's undisclosed formula.
            residentialDemand=Math.Clamp(70-vacant*12+vacantJobs*2-Unemployed*4,0,100);
            commercialDemand=Math.Clamp(30+population*3-buildings.OfType<CommercialBuilding>().Sum(b=>JobCapacity(b.id))*6,0,100);
            industrialDemand=Math.Clamp(40+Unemployed*8-vacantJobs*8,0,100);
        }
        public void AdvanceDevelopment(Func<float,float,float> height)
        {
            if(!development.enabled) return;
            foreach(var project in development.projects.Where(p=>traffic.clock>=p.completeAt).ToList())
            {
                var lot=project.building.ToBuilding();
                if(buildings.Count>=100000 || nextBuildingId==int.MaxValue) {CancelProject(project,"建筑数量已达上限");continue;}
                if(!CanBuild(lot,height,out string error,project.id)) {CancelProject(project,error);continue;}
                var cells=ZoningCells(height);
                if(project.cells.Any(key=>!cells.Any(c=>c.key==key && c.use==lot.Use))) {CancelProject(project,"道路或土地用途改变");continue;}
                lot.id=nextBuildingId++;
                if(lot is ResidentialBuilding home) {home.housing=HousingKind.Villa;home.housingUnits=1;home.askingRent=0;}
                buildings.Add(lot);development.projects.Remove(project);development.revision++;BuildingsChanged();Recalculate();
                Trace("building.grown","分区私人建筑建设完成",new CityLogDetail{x=lot.x,z=lot.z,reason=lot.Use.ToString()},building:lot.id);
            }
            if(traffic.clock<development.nextGrowth) return;
            if(development.nextProjectId==int.MaxValue) return;
            development.nextGrowth=traffic.clock+6;
            Recalculate();DevelopmentDemand();
            var candidates=ZoningCells(height);
            foreach(var use in new[]{LandUse.Residential,LandUse.Commercial,LandUse.Industrial})
            {
                int need=use==LandUse.Residential?residentialDemand:use==LandUse.Commercial?commercialDemand:industrialDemand;
                if(need<=0 || development.projects.Any(p=>p.building.type==use)) continue;
                bool planned=false;
                for(int sizeIndex=0;sizeIndex<growthFootprints.Length && !planned;sizeIndex++)
                {
                    var size=growthFootprints[(development.nextProjectId-1+sizeIndex)%growthFootprints.Length];
                    foreach(var first in candidates.Where(c=>c.row==0 && c.available && c.use==use))
                    {
                        var footprint=candidates.Where(c=>c.block==first.block && c.side==first.side && c.row<size.depth && c.column>=first.column && c.column<first.column+size.width).ToList();
                        if(footprint.Count!=size.width*size.depth || footprint.Any(c=>!c.available || c.use!=use)
                            || footprint.Any(c=>development.projects.Any(p=>p.cells.Contains(c.key)))) continue;
                        var lot=NewBuilding(use);lot.x=footprint.Average(c=>c.pose.x);lot.z=footprint.Average(c=>c.pose.z);lot.yaw=first.pose.yaw;
                        lot.width=CityZoningState.CellSize*size.width-.1f;lot.depth=CityZoningState.CellSize*size.depth-.1f;
                        // Curved planning cells can diverge. A rigid building must cover
                        // only its own selected cells, never unzoned gaps or neighbours.
                        if(footprint.Any(c=>!lot.Contains(c.pose.x,c.pose.z)) || candidates.Any(c=>c.pose.Overlaps(lot) && !footprint.Contains(c)))continue;
                        var entrance=lot.Point(0,-lot.depth/2);lot.entranceX=entrance.x;lot.entranceZ=entrance.z;
                        var edge=roads.Pick(entrance.x,entrance.z,CityRoads.Width/2+.45f);
                        if(edge==null || !roads.Connected(edge.a) || !CanBuild(lot,height,out _) || development.projects.Any(p=>lot.Overlaps(p.building.ToBuilding()))) continue;
                        var project=new DevelopmentProject{id=development.nextProjectId++,started=traffic.clock,completeAt=traffic.clock+12,building=BuildingSaveData.FromBuilding(lot),cells=footprint.Select(c=>c.key).ToList()};
                        development.projects.Add(project);development.revision++;Trace("building.construction","分区私人建筑开始施工",project);planned=true;break;
                    }
                }
            }
        }
        void CancelProject(DevelopmentProject project,string reason)
        {development.projects.Remove(project);development.revision++;Trace("building.construction_cancelled",reason,project);}
        public bool ValidDevelopment()
        {
            if(development==null || development.projects==null || development.projects.Count>1000 || development.nextProjectId<1 || development.provisionalOutageDays<1 || development.provisionalOutageDays>30
                || !development.enabled && development.projects.Count>0
                || !FiniteDevelopment(development.nextGrowth) || development.nextGrowth<0
                || new[]{development.residentialTax,development.commercialTax,development.industrialTax}.Any(t=>t<0 || t>29)
                || new[]{development.electricityBudget,development.waterBudget,development.garbageBudget,development.healthcareBudget,development.educationBudget,development.fireBudget,development.policeBudget}.Any(b=>b<50 || b>150)) return false;
            var ids=new HashSet<int>();var keys=new HashSet<string>();
            try
            {
                foreach(var p in development.projects)
                {
                    if(p==null || p.id<1 || p.id>=development.nextProjectId || !ids.Add(p.id) || p.building==null || !CityZoningState.ZoneUse(p.building.type)
                        || !FiniteDevelopment(p.started) || !FiniteDevelopment(p.completeAt) || p.started<0 || p.completeAt<=p.started
                        || p.cells==null || p.cells.Count<1 || p.cells.Count>16 || p.cells.Any(k=>!ZoneCell.ValidKey(k) || !keys.Add(k))) return false;
                    var b=p.building.ToBuilding();if(b.upgradeDays!=0 || b.lastUpgradeDay!=0 || b.crime!=0 || b.burning || b.burned || b.fireDamage!=0 || b.fireIntensity!=0 || b.garbage!=0 || b.outageDays!=0 || b.abandoned || b.abandonedDay!=0 || b.level!=1 || b.id!=0 || !FiniteDevelopment(b.x) || !FiniteDevelopment(b.z) || !FiniteDevelopment(b.yaw)
                        || !FiniteDevelopment(b.entranceX) || !FiniteDevelopment(b.entranceZ)
                        || !ValidGrowthDimension(b.width) || !ValidGrowthDimension(b.depth) || p.cells.Count!=GrowthCells(b.width)*GrowthCells(b.depth) || Math.Abs(b.x)>BuildHalfSize || Math.Abs(b.z)>BuildHalfSize
                        || Math.Abs(b.entranceX)>BuildHalfSize || Math.Abs(b.entranceZ)>BuildHalfSize || b.yaw<0 || b.yaw>=360) return false;
                    if(b is ResidentialBuilding home && (home.housingUnits!=0 || home.askingRent!=0 || home.interestedFamilies==null)) return false;
                    if(b is IGoodsBuilding goods && goods.Stock!=0) return false;
                    if(b is CommercialBuilding shop && (shop.customers!=0 || shop.retailSold!=0 || shop.retailSoldToday!=0 || shop.retailDay!=-1))return false;
                    if(b is IndustrialBuilding factory)
                        foreach(var field in typeof(FactoryState).GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public))
                        {
                            var value=field.GetValue(factory.factory);
                            if(value is double d && d!=0 || value is float f && f!=0 || value is int n && n!=0 || value is bool active && active) return false;
                        }
                }
            } catch(ArgumentException) {return false;}
            return true;
        }
        static bool FiniteDevelopment(float v)=>!float.IsNaN(v) && !float.IsInfinity(v);
        static int GrowthCells(float dimension)=>(int)Math.Round((dimension+.1f)/CityZoningState.CellSize);
        static bool ValidGrowthDimension(float dimension)=>FiniteDevelopment(dimension) && GrowthCells(dimension)>=1 && GrowthCells(dimension)<=4
            && Math.Abs(dimension-(GrowthCells(dimension)*CityZoningState.CellSize-.1f))<.0001f;

        void BaselineTick()
        {
            if(analysis!=null) try {analysis.PrepareSettlement();} catch(Exception ex) {analysis.Fail(ex);}
            day++;UpdateResidentAges();PrepareSchoolEnrollment();Recalculate();UpdateResidentHealth();UpdateMortality();GenerateGarbage();
            GenerateFires();UpdateCrime();
            foreach(var person in Citizens)person.schoolMinutesToday=0;
            var r=new HouseholdDay{day=day,openingTreasury=money,openingSavings=society.families.Sum(h=>h.savings),maintenance=upkeep};
            foreach(var h in society.families.Where(h=>h.resident || h.people.Any(p=>p.earnedWages>0)))
            {
                h.wagePaid=h.rentPaid=h.livingPaid=h.travelPaid=0;
                foreach(var p in h.people)
                {p.wagePaid=(int)Math.Round(p.earnedWages);h.wagePaid+=p.wagePaid;p.earnedWages=0;p.workedMinutes=0;}
                h.savings+=h.wagePaid;r.wages+=h.wagePaid;r.externalWages+=h.wagePaid;
            }
            r.left+=UpdateBuildingConditions();UpdateBuildingGrowth();Recalculate();
            // Migration is no longer gated by a joint rent/savings/work choice.
            int arrivals=0;
            foreach(var home in buildings.OfType<ResidentialBuilding>().Where(b=>!b.abandoned && !b.burning && !b.burned && BuildingAccess(b.id)))
            {
                if(!Supply(home.id).Complete) continue;
                while(Occupancy(home.id)<home.housingUnits && arrivals<society.settings.applicantsPerDay && society.families.Count<10000)
                {
                    var h=society.families.FirstOrDefault(f=>!f.resident && LivingMembers(f)>0);
                    if(h==null) {h=NewHousehold();r.openingSavings+=h.savings;}
                    // Count new household savings as an outside inflow in the ledger.
                    h.home=home.id;h.unit=Enumerable.Range(0,home.housingUnits).First(unit=>!society.families.Any(f=>f.resident && f.home==home.id && f.unit==unit));
                    h.resident=true;h.rent=h.arrears=0;h.reason="迁入分区生长的住宅；工作由个人寻找";
                    foreach(var p in h.people.Where(p=>!p.dead)) {p.location=home.id;p.atWork=false;p.tripId=0;}
                    arrivals++;r.arrived++;Trace("household.arrived",h.reason,h,household:h.id,building:home.id);
                }
            }
            SyncJobs();
            foreach(var h in society.families.Where(f=>f.resident)) foreach(var p in h.people.Where(p=>p.canWork && p.jobId<0 && p.tripId==0 && !p.atWork && !p.atSchool && !p.schoolReturning && p.schoolId<0))
            {
                var job=society.jobEntities.Where(j=>j.occupiedCitizenId<0 && j.requiredSkill<=p.skill && BuildingAccess(j.buildingId) && GetBuilding(j.buildingId)?.burning!=true)
                    .Select(j=>new{job=j,time=CommuteMinutes(h.home,j.buildingId)}).Where(x=>x.time>=0).OrderBy(x=>x.time).ThenBy(x=>x.job.id).FirstOrDefault();
                if(job!=null) AssignJob(p,job.job.id);
            }
            Recalculate();
            // Provisional assessed taxable activity. Exact CS1 tax bases remain to be verified.
            r.tax=(int)Math.Floor(population*18*development.residentialTax/100.0)
                +(int)Math.Floor(buildings.OfType<CommercialBuilding>().Sum(b=>EmployedAt(b.id))*160*development.commercialTax/100.0)
                +(int)Math.Floor(buildings.OfType<IndustrialBuilding>().Sum(b=>EmployedAt(b.id))*180*development.industrialTax/100.0);
            money+=r.tax-r.maintenance;society.lastSettledDay=day;
            r.population=population;r.households=society.families.Count(h=>h.resident);r.units=buildings.OfType<ResidentialBuilding>().Sum(b=>HousingCapacity(b.id));
            r.employed=Employed;r.unemployed=Unemployed;r.averageCommute=AverageCommute;r.closingTreasury=money;r.closingSavings=society.families.Sum(h=>h.savings);
            society.history.Add(r);if(society.history.Count>180) society.history.RemoveAt(0);Trace("city.day","分区城市税收与公共维护结算（税基暂为近似）",r);Recalculate();
        }
    }
}
