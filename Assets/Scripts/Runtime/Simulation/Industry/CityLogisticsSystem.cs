using System;
using System.Collections.Generic;
using HarborCity.RoadCore;
namespace HarborCity
{
    public sealed class CityLogisticsSystem
    {
        readonly CityEntities entities;readonly World transport;readonly CitySimulation simulation;readonly CityCoreIndustryBusiness business;readonly EconomyRules rules;
        readonly SortedSet<int> sources=new SortedSet<int>();readonly HashSet<int> dirtyShops=new HashSet<int>();
        readonly CityRetailIndex[] targets={new CityRetailIndex(),new CityRetailIndex()};
        readonly HashSet<int>[] listed={new HashSet<int>(),new HashSet<int>()};
        readonly List<int> ready=new List<int>();int network=-1;
        public long NextDispatchTick {get;private set;}
        internal CityLogisticsSystem(CityEntities entities,World transport,CitySimulation simulation,long nextTick,CityCoreIndustryBusiness business)
        {
            this.entities=entities;this.transport=transport;this.simulation=simulation;this.business=business;rules=simulation.Design.Economy;NextDispatchTick=nextTick;
            foreach(var f in entities.Factories)if(f.Stock>0)sources.Add(f.id);foreach(var s in entities.Shops)dirtyShops.Add(s.id);
            entities.ProductsChanged+=b=>{if(b is IndustrialBuilding f){if(f.Stock>0)sources.Add(f.id);else sources.Remove(f.id);}else if(b is CommercialBuilding)dirtyShops.Add(b.id);};
            entities.BusinessStructureChanged+=(b,added)=>{if(b is IndustrialBuilding f){if(added&&f.Stock>0)sources.Add(f.id);else sources.Remove(f.id);}if(b is CommercialBuilding s){if(!added)for(int i=0;i<ProductCatalog.Count;i++){targets[i].Remove(s);listed[i].Remove(s.id);}dirtyShops.Add(b.id);}};
            simulation.Consumption.ShopDemandChanged+=id=>dirtyShops.Add(id);
            transport.TaskCommitted+=t=>{if(t.purpose==TransportPurpose.Freight)dirtyShops.Add(t.destination);};
        }
        void RefreshTarget(int id)
        {
            if(!(entities.Building(id) is CommercialBuilding shop))return;var incoming=simulation.Shipments.Goods(id);
            for(int i=0;i<ProductCatalog.Count;i++){var k=(ProductKind)i;bool active=simulation.Consumption.RestockTarget(id,k)>shop.Products[k]+incoming[k]&&(!simulation.DevelopmentEnabled||shop.Account.Cash>=simulation.Design.Products[k].WholesalePrice);
                if(active){if(listed[i].Add(id))targets[i].Add(shop);}else if(listed[i].Remove(id))targets[i].Remove(shop);}
        }
        internal void Step(TimeContext time)
        {
#if CITY_CORE_ATTRIBUTION
            using var observation=CityCoreProfile.Measure(CityCoreProfile.Part.Logistics);
#endif
            if(time.Tick<NextDispatchTick)return;NextDispatchTick=time.Tick+5;
            if(network!=transport.NetworkGeneration){network=transport.NetworkGeneration;foreach(var s in entities.Shops)dirtyShops.Add(s.id);}
            foreach(int id in dirtyShops)RefreshTarget(id);dirtyShops.Clear();ready.Clear();foreach(int id in sources)ready.Add(id);
            foreach(int id in ready)
            {
                var factory=entities.Building(id) as IndustrialBuilding;if(factory==null||factory.Stock<=0||transport.FreightSourceBlocked(id))continue;var kind=factory.factory.product;double price=simulation.Design.Products[kind].WholesalePrice;bool sent=false;
                for(float radius=64;!sent;radius*=2)
                {
                    foreach(var c in targets[(int)kind].Collect(factory.x,factory.z,radius))
                    {var shop=c.shop;var reserved=simulation.Shipments.Goods(shop.id);int missing=simulation.Consumption.RestockTarget(shop.id,kind)-shop.Products[kind]-reserved[kind];int amount=Math.Min(rules.FreightBatch,Math.Min(factory.Stock,Math.Min(missing,32-shop.Stock-reserved.Total)));if(amount<=0||!business.CanFundLocalFreight(shop,amount,price))continue;var task=simulation.RequestFreight(id,shop.id,amount,price);if(task!=null){dirtyShops.Add(shop.id);sent=true;break;}}
                    if(radius>=simulation.MapHalfSize*3)break;
                }
            }
        }
    }
}
