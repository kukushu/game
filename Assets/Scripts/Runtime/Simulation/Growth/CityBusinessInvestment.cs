using System;
using System.Collections.Generic;
using HarborCity.RoadCore;
namespace HarborCity
{
    public sealed partial class CityCoreGrowth
    {
        readonly Dictionary<int,int> availableWorkers=new Dictionary<int,int>();
        readonly SortedDictionary<int,int> skills=new SortedDictionary<int,int>();
        readonly HashSet<int> firstDeliveryPending=new HashSet<int>();
        readonly List<(CommercialBuilding shop,int gap)> procurement=new List<(CommercialBuilding,int)>();
        readonly Dictionary<int,int> plannedSupply=new Dictionary<int,int>();
        readonly List<CityMember> forecastWorkers=new List<CityMember>();
        void BindInvestment()
        {
            foreach(var m in entities.Members)Worker(m);foreach(var f in entities.Factories)Factory(f,true);
            entities.MemberStructureChanged+=(m,added)=>Worker(m);entities.JobAssignmentsChanged+=Worker;
            entities.ResidentStateChanged+=(m,r)=>Worker(m);entities.RequestGuardsChanged+=(m,g)=>{if(g==CityResidentGuard.Trip)Worker(m);};
            entities.ProductsChanged+=b=>{if(b is IndustrialBuilding f)Factory(f,true);};
            entities.BusinessStructureChanged+=(b,added)=>{if(b is IndustrialBuilding f)Factory(f,added);};
        }
        void CountSkill(int skill,int delta){skills.TryGetValue(skill,out int n);n+=delta;if(n==0)skills.Remove(skill);else skills[skill]=n;}
        void Worker(CityMember m)
        {
            var p=m.Person;bool eligible=entities.Member(p.id)==m&&m.Family.resident&&!p.dead&&!p.sick&&p.canWork&&p.age>=18&&p.jobId<0&&p.tripId==0&&!p.atWork;
            if(availableWorkers.TryGetValue(p.id,out int old)){if(eligible&&old==p.skill)return;CountSkill(old,-1);availableWorkers.Remove(p.id);}
            if(eligible){availableWorkers.Add(p.id,p.skill);CountSkill(p.skill,1);}
        }
        internal bool HasWorker(int required){foreach(var pair in skills)if(pair.Key>=required&&pair.Value>0)return true;return false;}
        void Factory(IndustrialBuilding f,bool added)
        {if(added&&f.externalCapital>0&&!f.factory.firstDelivery)firstDeliveryPending.Add(f.id);else firstDeliveryPending.Remove(f.id);}
        bool Reachable(int source,int destination)
        {var a=world.GetAccess(source);var b=world.GetAccess(destination);return a!=null&&b!=null&&a.valid&&b.valid&&world.Network.CanReach(a,b);}
        internal bool InvestmentOpportunity(LandUse use,out ProductKind product,out int reserve,out CityBuilding focus)
            =>InvestmentOpportunity(use,out product,out reserve,out focus,out _);
        internal bool InvestmentOpportunity(LandUse use,out ProductKind product,out int reserve,out CityBuilding focus,out bool exportProject)
        {
            exportProject=false;product=ProductKind.Food;reserve=0;focus=null;var simulation=entities.Simulation;if(simulation==null)return false;
            // A funded business may advertise jobs before qualified migration.
            // Requiring both a job before migration and an unemployed local worker
            // before investment otherwise deadlocks the first specialist factory.
            if(!HasWorker(use==LandUse.Industrial?1:0)&&city.DevelopmentHousingVacancies()==0)return false;
            var market=simulation.Consumption;
            if(use==LandUse.Commercial)
            {
                foreach(var pair in market.UnservedDemand){int amount=pair.Value.Total;var home=entities.Building(pair.Key);if(home==null||!city.BuildingAccess(home.id)||market.FindReachableSupplier(home)!=null)continue;if(amount>reserve||amount==reserve&&focus!=null&&home.id<focus.id){reserve=amount;focus=home;}}
                return reserve>0;
            }
            // Search actual gaps in descending upper-bound order. A covered
            // largest store must not hide an uncovered independent region.
            // Reuse query scratch; do not maintain a factory/store matrix.
            for(int p=0;p<ProductCatalog.Count;p++)
            {
                var k=(ProductKind)p;bool pending=false;
                foreach(int id in firstDeliveryPending)
                {
                    var f=entities.Building(id) as IndustrialBuilding;if(f==null||f.factory.product!=k)continue;
                    bool canStaff=city.EmployedAt(id)>0&&f.factory.cash>0||f.factory.cash>=simulation.Design.Employment.Wage(id,true);
                    if(canStaff||f.Stock>0||simulation.Shipments.Trucks(id)>0){pending=true;break;}
                }
                if(pending)continue;
                procurement.Clear();
                foreach(var pair in market.ShopDemand)
                {
                    var candidate=entities.Building(pair.Key) as CommercialBuilding;if(candidate==null||candidate.abandoned||!city.BuildingAccess(candidate.id))continue;
                    int n=market.RestockTarget(candidate.id,k)-candidate.Products[k]-simulation.Shipments.Goods(candidate.id)[k];
                    if(candidate.Account.Cash<simulation.Design.Products[k].WholesalePrice)continue;
                    if(n>0)procurement.Add((candidate,n));
                }
                procurement.Sort((a,b)=>{int c=b.gap.CompareTo(a.gap);return c!=0?c:a.shop.id.CompareTo(b.shop.id);});
                plannedSupply.Clear();
                foreach(var factory in entities.Factories)
                    if(!factory.abandoned&&!factory.burned&&factory.factory.product==k)
                        plannedSupply[factory.id]=factory.Stock+NearTermProduction(factory);
                foreach(var entry in procurement)
                {
                    if(entry.gap<reserve)break;int gap=entry.gap;var shop=entry.shop;
                    // Allocate each unit only once across shops in stable order.
                    foreach(var factory in entities.Factories)
                        if(plannedSupply.TryGetValue(factory.id,out int available)&&available>0&&Reachable(factory.id,shop.id))
                        {int covered=Math.Min(gap,available);plannedSupply[factory.id]=available-covered;gap-=covered;if(gap<=0)break;}
                    foreach(var project in city.development.projects)gap-=project.supplyReserved[k];
                    if(gap>reserve||gap>0&&gap==reserve&&product==k&&focus!=null&&shop.id<focus.id){reserve=gap;product=k;focus=shop;}
                }
            }
            if(reserve>0)return true;
            // External demand is a configured daily order book, not an infinite
            // incentive to create factories. Existing staffed capacity gets credit
            // before a new project can reserve any remaining order volume.
            bool hasExport=false;foreach(var port in city.trade.connections)if(port.active&&port.flow==OutsideFlow.Export&&world.GetAccess(port.id)?.valid==true){hasExport=true;break;}
            if(!hasExport)return false;
            for(int p=0;p<ProductCatalog.Count;p++)
            {
                var kind=(ProductKind)p;var rule=simulation.Design.Products[kind];
                double wage=simulation.Design.Employment.Wage(city.nextBuildingId,true);
                double cost=simulation.Design.Economy.RawUnitPrice+rule.ProcessingCost+wage*rule.LabourMinutes/simulation.Design.Employment.ShiftMinutes;
                if(rule.WholesalePrice<=cost)continue;
                int gap=simulation.Design.Economy.DailyExportOrders[kind];
                foreach(var factory in entities.Factories)
                {
                    if(factory.factory.product!=kind||factory.factory.cash<=0||factory.abandoned||factory.burned)continue;
                    bool reachable=false;foreach(var port in city.trade.connections)
                        if(port.active&&port.flow==OutsideFlow.Export&&Reachable(factory.id,port.id)){reachable=true;break;}
                    if(reachable)gap-=(int)Math.Floor(factory.level*simulation.Design.Employment.SlotsPerLevel*(double)simulation.Design.Employment.ShiftMinutes*city.FactoryProcessingRate(factory.id)/rule.LabourMinutes);
                }
                foreach(var project in city.development.projects)gap-=project.supplyReserved[kind];
                if(gap>reserve){reserve=gap;product=kind;exportProject=true;}
            }
            return reserve>0;
        }
        internal int NearTermProduction(IndustrialBuilding factory)
        {
            var simulation=entities.Simulation;var state=factory.factory;
            if(state.cash<=0||factory.abandoned||factory.burning||factory.burned||!city.BuildingAccess(factory.id)||!city.Supply(factory.id).Complete)return 0;
            var product=simulation.Design.Products[state.product];
            int workers=0;double wages=0;entities.CollectEmployees(factory.id,forecastWorkers);
            foreach(var member in forecastWorkers)
                if(member.Family.resident&&city.CanAttendWork(member.Person))
                {workers++;wages+=city.ResidentWage(member.Person);}
            if(workers==0)return 0;
            // A one-shift upper bound, limited to material already owned and cash
            // already available. This forecast neither creates goods nor pays wages.
            int material=state.raw+simulation.Shipments.Raw(factory.id)+(state.processing?1:0);
            double labour=simulation.Design.Employment.ShiftMinutes*workers*city.FactoryProcessingRate(factory.id);
            double unitCost=product.ProcessingCost+wages*product.LabourMinutes/labour;
            int affordable=unitCost>0?(int)Math.Min(material,Math.Floor(state.cash/unitCost)):material;
            return Math.Min(FactoryState.GoodsCapacity-factory.Stock,Math.Min(affordable,(int)Math.Floor(labour/product.LabourMinutes)));
        }
        bool CanImportAt(SpatialHit entrance)
        {
            foreach(var port in city.trade.connections)
            {
                if(!port.active||port.flow!=OutsideFlow.Import)continue;
                var access=world.GetAccess(port.id);
                if(access!=null&&access.valid&&world.Network.CanReach(access.lane,access.distance,entrance.drivable,entrance.distance))return true;
            }
            return false;
        }
        bool CanExportAt(SpatialHit entrance)
        {
            foreach(var port in city.trade.connections)
            {
                if(!port.active||port.flow!=OutsideFlow.Export)continue;var access=world.GetAccess(port.id);
                if(access!=null&&access.valid&&world.Network.CanReach(entrance.drivable,entrance.distance,access.lane,access.distance))return true;
            }
            return false;
        }
        static void CheckCapital(CityBuilding b){if(double.IsNaN(b.externalCapital)||double.IsInfinity(b.externalCapital)||b.externalCapital<0||b.externalCapital>0&&!(b is IndustrialBuilding)&&!(b is CommercialBuilding))throw new InvalidOperationException("Invalid external startup capital");}
        internal void ValidateInvestment()
        {
            double sum=0;foreach(var b in entities.Buildings){CheckCapital(b);sum+=b.externalCapital;}foreach(var b in entities.RetiredBuildings){CheckCapital(b);sum+=b.externalCapital;}
            if(sum!=city.development.externalInvestment||double.IsNaN(sum)||double.IsInfinity(sum)||sum<0)throw new InvalidOperationException("External business investment ledger mismatch");
            foreach(var p in city.development.projects)p.supplyReserved.Validate();
        }
    }
}
