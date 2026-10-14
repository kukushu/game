using System;
using System.Collections.Generic;
using HarborCity.RoadCore;

namespace HarborCity
{
    [Serializable] public sealed class ConsumptionData {public long nextTick;public int consumedGoods;public double retailPayments;}
    public sealed class CityConsumptionState
    {
        public long NextTick {get;internal set;}
        public int ConsumedGoods {get;internal set;}
        public double RetailPayments {get;internal set;}
        public CityConsumptionState(){}
        public CityConsumptionState(ConsumptionData d){if(d==null)throw new ArgumentNullException(nameof(d));NextTick=d.nextTick;ConsumedGoods=d.consumedGoods;RetailPayments=d.retailPayments;}
        public ConsumptionData Snapshot()=>new ConsumptionData{nextTick=NextTick,consumedGoods=ConsumedGoods,retailPayments=RetailPayments};
    }
    public sealed class CityCoreConsumption
    {
        readonly CityModel city;readonly CityEntities entities;readonly World world;readonly CitySimulation simulation;readonly RetailRules rules;
        readonly CityRetailIndex index=new CityRetailIndex();
        readonly CityCorePayroll payroll;
        readonly Dictionary<int,double> wholesaleReserved=new Dictionary<int,double>();
        internal CityCoreConsumption(CityModel city,CityEntities entities,World world,CitySimulation simulation)
        {this.city=city;this.entities=entities;this.world=world;this.simulation=simulation;rules=simulation.Design.Retail;payroll=simulation.Payroll;}
        internal void Step(TimeContext time)
        {
            if(!city.development.enabled||time.Tick<city.consumption.NextTick)return;city.consumption.NextTick=time.Tick+rules.ShoppingIntervalTicks;
            if(city.ResidentMinute<rules.ShoppingStartMinute)return;index.Begin();for(int i=0;i<entities.Shops.Count;i++){var shop=entities.Shops[i];if(shop.Stock>0&&Open(shop))index.Add(shop);}if(index.Count==0)return;
            foreach(var family in city.society.families)
            {
                if(!family.resident||family.shoppingDay==city.day||family.savings<rules.UnitPrice)continue;
                CityResident person=null;foreach(var p in family.people)if(!p.dead&&!p.sick&&p.age>=18&&!p.atWork&&p.tripId==0&&p.location==family.home&&p.carriedGoods==0&&world.Clock>=p.retryAt){person=p;break;}if(person==null)continue;
                var home=entities.Building(family.home);if(home==null)continue;CommercialBuilding best=null;
                for(float radius=rules.InitialSearchRadius;best==null;radius*=2)
                {
                    foreach(var candidate in index.Collect(home.x,home.z,radius))if(candidate.shop.Stock>0&&world.CanReachResident(home.id,candidate.shop.id)){best=candidate.shop;break;}
                    if(radius>=city.map.buildHalfSize*3)break;
                }
                if(best==null)continue;person.retryAt=(float)(world.Clock+.5);simulation.RequestShopping(person.id,family.home,best.id);
            }
        }
        bool Open(CommercialBuilding shop)=>!shop.abandoned&&!shop.burning&&!shop.burned&&city.BuildingAccess(shop.id)&&city.Supply(shop.id).Complete&&payroll.WorkersAt(shop.id)>0;
        internal bool CanDepart(TripTask task,CityMember member)
        {
            var p=member.Person;var h=member.Family;
            if(!city.development.enabled||p.age<18||p.sick||p.carriedGoods!=0||h.shoppingDay==city.day||h.savings<rules.UnitPrice||task.origin!=h.home||!(entities.Building(task.destination) is CommercialBuilding shop)||shop.Stock<=0||!Open(shop))return false;
            return true;
        }
        internal void BeginVisit(TransportArrival arrival,CityMember member)
        {
            var person=member.Person;var family=member.Family;
            var shop=entities.Building(arrival.destination) as CommercialBuilding;
            if(shop!=null){shop.customers++;if(shop.retailDay!=city.day){shop.retailDay=city.day;shop.retailSoldToday=0;}
                if(Open(shop)){int amount=Math.Min(rules.ItemsPerVisit,Math.Min(shop.Stock,family.savings/rules.UnitPrice));int payment=amount*rules.UnitPrice;shop.Stock-=amount;shop.retailSold+=amount;shop.retailSoldToday+=amount;shop.Account.Cash+=payment;shop.Account.RetailRevenue+=payment;family.savings-=payment;family.retailPaid+=payment;person.carriedGoods=amount;city.consumption.RetailPayments+=payment;}}
            person.visitUntil=arrival.arrivedAt+rules.VisitSeconds;person.retryAt=(float)person.visitUntil;
        }
        internal void ConsumeAtHome(int cargo){city.consumption.ConsumedGoods+=cargo;}
        public void Validate()
        {
            var state=city.consumption;if(city.society.retiredWagesPaid<0||state.NextTick<0||state.ConsumedGoods<0||state.RetailPayments<0||double.IsNaN(state.RetailPayments)||double.IsInfinity(state.RetailPayments))throw new InvalidOperationException("Invalid consumption facts");
            long produced=0,goods=state.ConsumedGoods+city.trade.goodsExported;
            double wages=0,paid=city.society.retiredWagesPaid,payments=0,retail=0;
            foreach(var factory in entities.Factories){wages+=factory.factory.wageCosts;produced+=factory.factory.produced;goods+=factory.Stock;}
            foreach(var b in entities.RetiredBuildings){if(b is IndustrialBuilding f){wages+=f.factory.wageCosts;produced+=f.factory.produced;goods+=f.Stock;}else if(b is CommercialBuilding s){goods+=s.Stock;wages+=s.Account.WageCosts;retail+=s.Account.RetailRevenue;s.Account.Validate();}}
            wholesaleReserved.Clear();foreach(var task in world.Tasks){if(task.Shipment?.kind==CargoKind.Goods)goods+=task.Cargo;if(task.purpose==TransportPurpose.Freight&&task.Shipment?.kind==CargoKind.Goods){wholesaleReserved.TryGetValue(task.destination,out double n);wholesaleReserved[task.destination]=n+task.Cargo*task.Price;}}
            foreach(var shop in entities.Shops){goods+=shop.Stock;shop.Account.Validate();wages+=shop.Account.WageCosts;retail+=shop.Account.RetailRevenue;wholesaleReserved.TryGetValue(shop.id,out double reserved);if(city.development.enabled&&Math.Abs(shop.Account.WholesaleFunded-shop.wholesaleCosts-reserved)>1e-6)throw new InvalidOperationException("Wholesale payment escrow mismatch");}
            foreach(var family in city.society.families){if(family.retailPaid<0||family.openingSavings<0||family.incomeReceived<0)throw new InvalidOperationException("Invalid household money ledger");payments+=family.retailPaid;if(city.development.enabled&&family.savings!=(long)family.openingSavings+family.incomeReceived-family.retailPaid)throw new InvalidOperationException("Household income/retail ledger mismatch");foreach(var p in family.people){if(p.carriedGoods<0||p.carriedGoods>rules.ItemsPerVisit||p.tripId!=0&&p.carriedGoods!=0||double.IsNaN(p.visitUntil)||double.IsInfinity(p.visitUntil)||p.visitUntil<0)throw new InvalidOperationException("Invalid carried goods/visit");goods+=p.carriedGoods;paid+=p.wagesPaid+p.wageCredit;}}
            if(city.development.enabled&&produced!=goods)throw new InvalidOperationException("Produced goods lost or duplicated across stock/shipment/pocket/consumption/export");
            if(city.development.enabled&&(Math.Abs(wages-paid)>1e-5||Math.Abs(retail-payments)>1e-6||Math.Abs(payments-state.RetailPayments)>1e-6))throw new InvalidOperationException("Employment/retail money lost or duplicated");
        }
    }
}
