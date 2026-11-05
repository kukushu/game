using System;
using System.Collections.Generic;
using HarborCity.RoadCore;
using HarborCity.Diagnostics;
namespace HarborCity
{
    [Serializable] public sealed class ConsumptionData
    {public long nextTick;public GoodsTotals consumed,initial,lost,bought,unmet;public double retailPayments;}
    public sealed class CityConsumptionState
    {
        public long NextTick {get;internal set;}
        public GoodsTotals Consumed {get;internal set;} public GoodsTotals Initial {get;internal set;} public GoodsTotals Lost {get;internal set;} public GoodsTotals Bought {get;internal set;} public GoodsTotals Unmet {get;internal set;}
        public long ConsumedGoods=>Consumed.Total;
        public double RetailPayments {get;internal set;}
        public CityConsumptionState(){}
        public CityConsumptionState(ConsumptionData d){if(d==null)throw new ArgumentNullException(nameof(d));NextTick=d.nextTick;Consumed=d.consumed;Initial=d.initial;Lost=d.lost;Bought=d.bought;Unmet=d.unmet;RetailPayments=d.retailPayments;}
        public ConsumptionData Snapshot()=>new ConsumptionData{nextTick=NextTick,consumed=Consumed,initial=Initial,lost=Lost,bought=Bought,unmet=Unmet,retailPayments=RetailPayments};
    }
    // Owns derived deadlines and demand, never a second inventory or account.
    public sealed partial class CityCoreConsumption
    {
        readonly CityModel city;readonly CityEntities entities;readonly World world;readonly CitySimulation simulation;readonly RetailRules rules;readonly ProductCatalog products;
        readonly CityRetailIndex index=new CityRetailIndex();
        readonly CityRetailIndex[] saleIndex={new CityRetailIndex(),new CityRetailIndex()};
        readonly HashSet<int>[] saleListed={new HashSet<int>(),new HashSet<int>()};readonly CityCorePayroll payroll;
        readonly List<Household> families=new List<Household>();readonly Dictionary<int,int> slots=new Dictionary<int,int>();
        readonly CityWakeQueue consumption=new CityWakeQueue(16),shopping=new CityWakeQueue(16);
        readonly HashSet<int> dirty=new HashSet<int>(),dirtyStores=new HashSet<int>(),noStore=new HashSet<int>();readonly List<int> buffer=new List<int>(),due=new List<int>();
        readonly HashSet<int> supplyBlocked=new HashSet<int>();
        readonly HashSet<int> layoutChanges=new HashSet<int>();
        readonly HashSet<int>[] productWaiters={new HashSet<int>(),new HashSet<int>()};
        readonly List<CommercialBuilding>[] reopened={new List<CommercialBuilding>(),new List<CommercialBuilding>()};
        readonly Dictionary<int,HashSet<int>> waiting=new Dictionary<int,HashSet<int>>();readonly Dictionary<int,int> familyStore=new Dictionary<int,int>();
        readonly Dictionary<int,GoodsAmounts> storeStock=new Dictionary<int,GoodsAmounts>();readonly Dictionary<int,bool> storeOpen=new Dictionary<int,bool>();
        readonly Dictionary<int,double> wholesaleReserved=new Dictionary<int,double>();
        int slotCapacity=16;int network=-1,buildings=-1;long tick;bool enabled;
        internal bool ReferencePolling; // Test-only oracle; same decisions, different candidate enumeration.
        internal long FamilyVisits,ConsumptionVisits,Notifications,StoreUpdates,SearchCandidates;
        internal static bool DemandSchedulingCompiled=>true;
        public int PendingConsumption=>consumption.Count;public int PendingShopping=>shopping.Count;
        public long QueueBytes=>consumption.LogicalBytes+shopping.LogicalBytes;
        internal CityCoreConsumption(CityModel city,CityEntities entities,World world,CitySimulation simulation)
        {
            this.city=city;this.entities=entities;this.world=world;this.simulation=simulation;rules=simulation.Design.Retail;products=simulation.Design.Products;payroll=simulation.Payroll;tick=simulation.Tick;
            foreach(var h in city.society.families)Register(h);
            foreach(var shop in entities.Shops){index.Add(shop);dirtyStores.Add(shop.id);}
            simulation.Utilities.SuppliesRebuilt+=()=>{foreach(int id in supplyBlocked)dirtyStores.Add(id);};
            world.EndpointAvailabilityChanged+=(id,revision)=>{if(entities.Building(id) is CommercialBuilding)dirtyStores.Add(id);};
            entities.ShoppingFamilyFactsChanged+=FamilyChanged;
            entities.MemberStructureChanged+=(m,added)=>Mark(m.Family,true);
            entities.ResidentStateChanged+=(m,why)=>{Mark(m.Family,why==CityResidentStateChange.Death||why==CityResidentStateChange.Membership);int work=city.Workplace(m.Person);if(work>=0)dirtyStores.Add(work);};
            entities.RequestGuardsChanged+=(m,g)=>Mark(m.Family,false);
            entities.FundingEligibilityChanged+=m=>{int work=city.Workplace(m.Person);if(work>=0)dirtyStores.Add(work);};
            entities.ProductsChanged+=b=>{if(b is CommercialBuilding)dirtyStores.Add(b.id);};
            entities.BusinessStructureChanged+=(b,added)=>{if(b is CommercialBuilding shop){if(added)index.Add(shop);else{index.Remove(shop);for(int n=0;n<ProductCatalog.Count;n++){saleIndex[n].Remove(shop);saleListed[n].Remove(shop.id);}}dirtyStores.Add(b.id);layoutChanges.Add(b.id);}foreach(int i in noStore)shopping.Schedule(i,tick);};
        }
        int Register(Household h)
        {if(slots.TryGetValue(h.id,out int at))return at;at=families.Count;families.Add(h);slots.Add(h.id,at);if(families.Count>slotCapacity){slotCapacity=checked(slotCapacity*2);consumption.EnsureCapacity(slotCapacity);shopping.EnsureCapacity(slotCapacity);}dirty.Add(at);if(h.goodsSupplier>=0)supplier[at]=h.goodsSupplier;UpdateDemand(at);return at;}
        void FamilyChanged(Household h)=>Mark(h,true);
        void Mark(Household h,bool count)
        {Notifications++;int i=Register(h);if(count)dirty.Add(i);shopping.Schedule(i,tick);}
        int People(Household h){if(!h.resident)return 0;int n=0;foreach(var p in h.people)if(!p.dead)n++;return n;}
        double Denominator(ProductKind p)=>city.society.settings.secondsPerDay/SimulationClock.StepSeconds*products[p].ConsumptionDays;
        void Initialize(Household h)
        {
            if(h.goodsInitialized)return;h.goodsInitialized=true;h.consumptionEpoch=tick;h.consumptionPeople=People(h);
            for(int i=0;i<ProductCatalog.Count;i++){var k=(ProductKind)i;int n=(int)Math.Ceiling(h.consumptionPeople*(double)products[k].TargetDays/products[k].ConsumptionDays);h.goods=h.goods.Add(k,n);h.broughtGoods=h.broughtGoods.Add(k,n);city.consumption.Initial=city.consumption.Initial.Add(k,n);}
        }
        // Absolute epoch arithmetic makes sparse settlement identical to per-tick reference.
        double Exact(Household h,ProductKind p)=>Math.Max(0,tick-h.consumptionEpoch)*(double)h.consumptionPeople/Denominator(p)+(p==ProductKind.Food?h.foodFraction:h.dailyFraction);
        void Settle(int slot,bool membership)
        {
            ConsumptionVisits++;var h=families[slot];Initialize(h);
            for(int i=0;i<ProductCatalog.Count;i++)
            {
                var k=(ProductKind)i;long total=(long)Math.Floor(Exact(h,k)+1e-10),before=k==ProductKind.Food?h.foodUsedAtEpoch:h.dailyUsedAtEpoch;long n=total-before;
                if(n<0||n>int.MaxValue)throw new InvalidOperationException("Consumption epoch exceeds supported quantity");
                if(n>0){int consumed=Math.Min((int)n,h.goods[k]);h.goods=h.goods.Add(k,-consumed);h.consumedProducts=h.consumedProducts.Add(k,consumed);h.unmetProducts=h.unmetProducts.Add(k,(int)n-consumed);city.consumption.Unmet=city.consumption.Unmet.Add(k,(int)n-consumed);city.consumption.Consumed=city.consumption.Consumed.Add(k,consumed);shopping.Schedule(slot,tick);}
                if(k==ProductKind.Food)h.foodUsedAtEpoch=total;else h.dailyUsedAtEpoch=total;
            }
            if(membership){int n=People(h);if(n!=h.consumptionPeople){h.foodFraction=Exact(h,ProductKind.Food)-h.foodUsedAtEpoch;h.dailyFraction=Exact(h,ProductKind.DailyGoods)-h.dailyUsedAtEpoch;h.consumptionPeople=n;h.consumptionEpoch=tick;h.foodUsedAtEpoch=h.dailyUsedAtEpoch=0;}}
            ScheduleConsumption(slot);UpdateDemand(slot);
        }
        void ScheduleConsumption(int i)
        {
            var h=families[i];if(h.consumptionPeople==0){consumption.Cancel(i);return;}long next=long.MaxValue;
            for(int p=0;p<ProductCatalog.Count;p++){var k=(ProductKind)p;double fraction=k==ProductKind.Food?h.foodFraction:h.dailyFraction;long used=k==ProductKind.Food?h.foodUsedAtEpoch:h.dailyUsedAtEpoch;double deadline=h.consumptionEpoch+(used+1-fraction)*Denominator(k)/h.consumptionPeople;long dueTick=Math.Max(tick+1,(long)Math.Ceiling(deadline-1e-9));next=Math.Min(next,dueTick);}consumption.Schedule(i,next);
        }
        internal void Step(TimeContext time)
        {
            tick=time.Tick;if(!city.development.enabled){enabled=false;return;}
            if(!enabled){enabled=true;for(int i=0;i<families.Count;i++){dirty.Add(i);shopping.Schedule(i,tick);}}
            if(network!=world.NetworkGeneration){for(int n=0;n<ProductCatalog.Count;n++)foreach(int i in productWaiters[n])shopping.Schedule(i,tick);}
            if(network!=world.NetworkGeneration||buildings!=entities.BuildingRevision)
            {network=world.NetworkGeneration;buildings=entities.BuildingRevision;foreach(var id in waiting.Keys){dirtyStores.Add(id);}foreach(int i in noStore)shopping.Schedule(i,tick);}
            buffer.Clear();foreach(int i in dirty)buffer.Add(i);dirty.Clear();buffer.Sort();foreach(int i in buffer)Settle(i,true);
            if(ReferencePolling){for(int i=0;i<families.Count;i++)Settle(i,false);}else{consumption.DrainDue(tick,due);foreach(int i in due)Settle(i,false);}
            if(tick<city.consumption.NextTick)return;FlushStores();city.consumption.NextTick=tick+rules.ShoppingIntervalTicks;
            shopping.DrainDue(tick,due);
            if(ReferencePolling){due.Clear();for(int i=0;i<families.Count;i++)due.Add(i);}
            if(due.Count==0)return;using(var batch=world.Network.BeginGoalBatch())foreach(int i in due)Request(i);
        }
        void FlushStores()
        {
            for(int n=0;n<ProductCatalog.Count;n++)reopened[n].Clear();
            foreach(int id in dirtyStores)
            {
                if(!(entities.Building(id) is CommercialBuilding shop))continue;StoreUpdates++;storeStock.TryGetValue(id,out var old);storeOpen.TryGetValue(id,out bool wasOpen);bool open=Open(shop);var stock=shop.Products;if(stock.Total>0&&!city.Supply(id).Complete)supplyBlocked.Add(id);else supplyBlocked.Remove(id);
                for(int n=0;n<ProductCatalog.Count;n++){var k=(ProductKind)n;bool selling=open&&stock[k]>0;if(selling){if(saleListed[n].Add(id))saleIndex[n].Add(shop);}else if(saleListed[n].Remove(id))saleIndex[n].Remove(shop);if(selling&&(!wasOpen||old[k]==0))reopened[n].Add(shop);}
                storeStock[id]=stock;storeOpen[id]=open;
            }
            dirtyStores.Clear();
            if(layoutChanges.Count>0){for(int n=0;n<ProductCatalog.Count;n++)foreach(int i in productWaiters[n]){var h=families[i];var home=entities.Building(h.home);var old=entities.Building(h.goodsSupplier);foreach(int changed in layoutChanges){var candidate=entities.Building(changed);if(h.goodsSupplier==changed||home!=null&&candidate!=null&&(old==null||Squared(home,candidate)<=Squared(home,old))){shopping.Schedule(i,tick);break;}}}layoutChanges.Clear();}
            for(int n=0;n<ProductCatalog.Count;n++)if(reopened[n].Count>0)foreach(int i in productWaiters[n]){var h=families[i];foreach(var shop in reopened[n])if(world.CanReachResident(h.home,shop.id)){shopping.Schedule(i,tick);break;}}
        }
        static double Squared(CityBuilding a,CityBuilding b){double x=a.x-b.x,z=a.z-b.z;return x*x+z*z;}
        void Unwait(int i)
        {for(int n=0;n<ProductCatalog.Count;n++)productWaiters[n].Remove(i);noStore.Remove(i);if(familyStore.TryGetValue(i,out int id)){if(waiting.TryGetValue(id,out var set))set.Remove(i);familyStore.Remove(i);}}
        void Wait(int i,CommercialBuilding shop)
        {productWaiters[Need(families[i],ProductKind.Food)>0?0:1].Add(i);if(shop==null){noStore.Add(i);return;}storeOpen[shop.id]=Open(shop);storeStock[shop.id]=shop.Products;if(shop.Stock>0&&!city.Supply(shop.id).Complete)supplyBlocked.Add(shop.id);if(!waiting.TryGetValue(shop.id,out var set))waiting.Add(shop.id,set=new HashSet<int>());set.Add(i);familyStore[i]=shop.id;}
        internal int Need(Household h,ProductKind k)
        {
            if(!h.resident||h.consumptionPeople==0)return 0;var r=products[k];if((long)h.goods[k]*r.ConsumptionDays>=(long)h.consumptionPeople*r.RefillBelowDays)return 0;
            return Math.Max(0,(int)Math.Ceiling(h.consumptionPeople*(double)r.TargetDays/r.ConsumptionDays)-h.goods[k]);
        }
        public double DaysAvailable(Household h,ProductKind k)=>h.consumptionPeople==0?double.PositiveInfinity:h.goods[k]*(double)products[k].ConsumptionDays/h.consumptionPeople;
        void Request(int i)
        {
            FamilyVisits++;var h=families[i];Unwait(i);int food=Need(h,ProductKind.Food),daily=Need(h,ProductKind.DailyGoods);
            if(!h.resident||food+daily==0){h.goodsProblem="";return;}
            if(h.shoppingMember!=0){h.goodsProblem="goods.status.returning";return;}
            int minimum=products[food>0?ProductKind.Food:ProductKind.DailyGoods].RetailPrice;if(h.savings<minimum){h.goodsProblem="goods.status.no_money";return;}
            CityResident person=null;double retry=double.PositiveInfinity;
            foreach(var p in h.people)if(!p.dead&&!p.sick&&p.age>=18&&!p.atWork&&p.tripId==0&&p.location==h.home&&p.carriedGoods==0){retry=Math.Min(retry,p.retryAt);if(world.Clock>=p.retryAt){person=p;break;}}
            if(person==null){h.goodsProblem="goods.status.no_member";if(!double.IsInfinity(retry))shopping.Schedule(i,Math.Max(tick+1,(long)Math.Ceiling(retry/SimulationClock.StepSeconds)));return;}
            var home=entities.Building(h.home);if(home==null){h.goodsProblem="goods.status.no_home";return;}
            CommercialBuilding best=null,reachable=null;bool route=false;var kind=food>0?ProductKind.Food:ProductKind.DailyGoods;
            var available=saleIndex[(int)kind];
            if(available.Count>0)for(float radius=rules.InitialSearchRadius;best==null;radius*=2)
            {
                foreach(var c in available.Collect(home.x,home.z,radius))
                {SearchCandidates++;var shop=c.shop;if(shop.Products[kind]<=0||!Open(shop)||!world.CanReachResident(home.id,shop.id))continue;best=shop;break;}
                if(radius>=city.map.buildHalfSize*3)break;
            }
            reachable=best;
            if(reachable==null&&index.Count>0)for(float radius=rules.InitialSearchRadius;reachable==null;radius*=2)
            {foreach(var c in index.Collect(home.x,home.z,radius)){SearchCandidates++;if(city.BuildingAccess(c.shop.id)&&world.CanReachResident(home.id,c.shop.id)){reachable=c.shop;break;}}if(radius>=city.map.buildHalfSize*3)break;}
            route=reachable!=null;
            AssignSupplier(i,best??reachable);if(best==null){h.goodsProblem=route?"goods.status.no_stock":"goods.status.no_store";Wait(i,reachable);return;}
            person.retryAt=(float)(world.Clock+.5);var vehicle=simulation.RequestShopping(person.id,h.home,best.id);
            h.goodsProblem=vehicle==null?"goods.status.blocked":"goods.status.shopping";if(vehicle==null)shopping.Schedule(i,tick+rules.ShoppingIntervalTicks);
        }
        bool Open(CommercialBuilding s)=>!s.abandoned&&!s.burning&&!s.burned&&city.BuildingAccess(s.id)&&city.Supply(s.id).Complete&&payroll.WorkersAt(s.id)>0;
        internal bool CanDepart(TripTask task,CityMember m)
        {var p=m.Person;var h=m.Family;return city.development.enabled&&h.shoppingMember==0&&p.age>=18&&!p.sick&&p.carriedGoods==0&&task.origin==h.home&&entities.Building(task.destination) is CommercialBuilding shop&&shop.Stock>0&&Open(shop)&&h.savings>=products[Need(h,ProductKind.Food)>0?ProductKind.Food:ProductKind.DailyGoods].RetailPrice;}
        internal void BeginVisit(TransportArrival a,CityMember m)
        {
            var h=m.Family;var person=m.Person;var shop=entities.Building(a.destination) as CommercialBuilding;GoodsAmounts bought=default;int payment=0,capacity=32;
            if(shop!=null){shop.customers++;if(shop.retailDay!=city.day){shop.retailDay=city.day;shop.retailSoldToday=0;}
                if(Open(shop))for(int n=0;n<ProductCatalog.Count;n++)
                {var k=(ProductKind)n;int price=products[k].RetailPrice,want=Need(h,k),budget=h.savings-payment;if(k!=ProductKind.Food)budget=Math.Max(0,budget-Math.Max(0,Need(h,ProductKind.Food)-bought.food)*products[ProductKind.Food].RetailPrice);int count=Math.Min(capacity,Math.Min(want,Math.Min(shop.Products[k],budget/price)));bought=bought.Add(k,count);payment=checked(payment+count*price);capacity-=count;}
                shop.Products-=bought;shop.retailSold+=bought.Total;shop.retailSoldToday+=bought.Total;shop.Account.Cash+=payment;shop.Account.RetailRevenue+=payment;h.savings-=payment;h.retailPaid+=payment;person.carriedProducts=bought;city.consumption.Bought+=bought;city.consumption.RetailPayments+=payment;UpdateDemand(slots[h.id]);}
            person.visitUntil=a.arrivedAt+rules.VisitSeconds;person.retryAt=(float)person.visitUntil;
        }
        internal event Action<int,double> ShoppingReturned;
        internal void ReturnHome(CityMember m,GoodsAmounts goods,double arrivedAt)
        {
            var family=m.Family;
            family.goods+=goods;
            bool finishedShopping=family.shoppingMember==m.Person.id;
            if(finishedShopping)family.shoppingMember=0;
            UpdateDemand(slots[family.id]);
            Mark(family,false);
            if(finishedShopping)ShoppingReturned?.Invoke(family.id,arrivedAt);
        }
        public void Validate()
        {
            var state=city.consumption;state.Consumed.Validate();state.Initial.Validate();state.Lost.Validate();state.Bought.Validate();state.Unmet.Validate();if(state.NextTick<0||state.RetailPayments<0||double.IsNaN(state.RetailPayments)||double.IsInfinity(state.RetailPayments))throw new InvalidOperationException("Invalid consumption state");
            long[] unmet=new long[ProductCatalog.Count],made=new long[ProductCatalog.Count],held=new long[ProductCatalog.Count],initial=new long[ProductCatalog.Count],consumed=new long[ProductCatalog.Count];double wages=0,paid=city.society.retiredWagesPaid,retail=0,payments=0;wholesaleReserved.Clear();
            foreach(var b in entities.Buildings)CountBuilding(b,made,held,ref wages,ref retail);
            foreach(var b in entities.RetiredBuildings)CountBuilding(b,made,held,ref wages,ref retail);
            foreach(var task in world.Tasks)if(task.Shipment?.kind==CargoKind.Goods){for(int n=0;n<ProductCatalog.Count;n++)held[n]+=task.Shipment.Products[(ProductKind)n];if(task.purpose==TransportPurpose.Freight){wholesaleReserved.TryGetValue(task.destination,out double r);wholesaleReserved[task.destination]=r+task.Cargo*task.Price;}}
            foreach(var shop in entities.Shops){wholesaleReserved.TryGetValue(shop.id,out double reserved);if(city.development.enabled&&Math.Abs(shop.Account.WholesaleFunded-shop.wholesaleCosts-reserved)>1e-6)throw new InvalidOperationException("Wholesale escrow mismatch");}
            foreach(var h in city.society.families)
            {
                h.goods.Validate();h.broughtGoods.Validate();h.consumedProducts.Validate();h.unmetProducts.Validate();
                if(h.savings<0||h.retailPaid<0||h.incomeReceived<0||h.taxPaid<0||h.openingSavings<0)throw new InvalidOperationException("Invalid household ledger");
                payments+=h.retailPaid;if(city.development.enabled&&h.savings!=(long)h.openingSavings+h.incomeReceived-h.retailPaid-h.taxPaid)throw new InvalidOperationException("Household money mismatch");
                for(int n=0;n<ProductCatalog.Count;n++){var k=(ProductKind)n;held[n]+=h.goods[k];initial[n]+=h.broughtGoods[k];consumed[n]+=h.consumedProducts[k];unmet[n]+=h.unmetProducts[k];}
                if(h.goodsInitialized&&(h.consumptionEpoch<0||h.consumptionEpoch>simulation.Tick||h.consumptionPeople<0||h.foodUsedAtEpoch<0||h.dailyUsedAtEpoch<0||h.foodFraction<0||h.foodFraction>=1.000000001||h.dailyFraction<0||h.dailyFraction>=1.000000001||double.IsNaN(h.foodFraction)||double.IsNaN(h.dailyFraction)))throw new InvalidOperationException("Invalid consumption epoch");
                if(h.shoppingMember!=0&&!h.people.Exists(p=>p.id==h.shoppingMember))throw new InvalidOperationException("Orphan household shopping cycle");
                foreach(var p in h.people){p.carriedProducts.Validate();if(p.carriedGoods>32||p.tripId!=0&&p.carriedGoods!=0||p.visitUntil<0||double.IsNaN(p.visitUntil)||double.IsInfinity(p.visitUntil))throw new InvalidOperationException("Invalid shopping payload");for(int n=0;n<ProductCatalog.Count;n++)held[n]+=p.carriedProducts[(ProductKind)n];paid+=p.wagesPaid+p.wageCredit;}
            }
            if(city.development.enabled)
            {
                for(int n=0;n<ProductCatalog.Count;n++){var k=(ProductKind)n;if(unmet[n]!=state.Unmet[k]||initial[n]!=state.Initial[k]||consumed[n]!=state.Consumed[k]||made[n]+initial[n]!=held[n]+state.Consumed[k]+state.Lost[k]+city.trade.ExportedProducts[k])throw new InvalidOperationException("Product conservation mismatch: "+k+" made="+made[n]+" initial="+initial[n]+" held="+held[n]+" consumed="+state.Consumed[k]);}
                double purchased=0;for(int n=0;n<ProductCatalog.Count;n++)purchased+=state.Bought[(ProductKind)n]*products[(ProductKind)n].RetailPrice;if(Math.Abs(purchased-state.RetailPayments)>1e-6)throw new InvalidOperationException("Product retail quantities/payment mismatch");
                if(Math.Abs(wages-paid)>1e-5||Math.Abs(retail-payments)>1e-6||Math.Abs(payments-state.RetailPayments)>1e-6)throw new InvalidOperationException("Employment/retail money mismatch");
            }
        }
        void CountBuilding(CityBuilding b,long[] made,long[] held,ref double wages,ref double retail)
        {
            if(b is IGoodsBuilding inventory){inventory.Products.Validate();for(int n=0;n<ProductCatalog.Count;n++)held[n]+=inventory.Products[(ProductKind)n];}
            if(b is IndustrialBuilding factory){var k=factory.factory.product;if((uint)k>=ProductCatalog.Count||factory.Products[k]!=factory.Stock)throw new InvalidOperationException("Factory product mismatch");made[(int)k]+=factory.factory.produced;wages+=factory.factory.wageCosts;}
            if(b is CommercialBuilding shop){shop.Account.Validate();wages+=shop.Account.WageCosts;retail+=shop.Account.RetailRevenue;}
        }
    }
}
