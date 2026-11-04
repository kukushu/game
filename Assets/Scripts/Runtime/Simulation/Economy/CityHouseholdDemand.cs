using System;
using System.Collections.Generic;
namespace HarborCity
{
    public sealed partial class CityCoreConsumption
    {
        internal event Action<int> ShopDemandChanged;
        readonly Dictionary<int,int> demandHomes=new Dictionary<int,int>();
        readonly Dictionary<int,GoodsAmounts> demand=new Dictionary<int,GoodsAmounts>();
        readonly Dictionary<int,int> supplier=new Dictionary<int,int>();
        readonly Dictionary<int,GoodsAmounts> shopDemand=new Dictionary<int,GoodsAmounts>();
        readonly Dictionary<int,GoodsAmounts> homelessDemand=new Dictionary<int,GoodsAmounts>();
        internal IReadOnlyDictionary<int,GoodsAmounts> UnservedDemand=>homelessDemand;
        internal IReadOnlyDictionary<int,GoodsAmounts> ShopDemand=>shopDemand;
        internal GoodsAmounts DemandAt(int shop)=>shopDemand.TryGetValue(shop,out var n)?n:default;
        static void Add(Dictionary<int,GoodsAmounts> map,int id,GoodsAmounts amount,bool subtract)
        {map.TryGetValue(id,out var old);var next=subtract?old-amount:old+amount;if(next.Total==0)map.Remove(id);else map[id]=next;}
        void AccountDemand(int slot,GoodsAmounts amount,bool subtract)
        {if(supplier.TryGetValue(slot,out int shop)){Add(shopDemand,shop,amount,subtract);ShopDemandChanged?.Invoke(shop);}else Add(homelessDemand,demandHomes.TryGetValue(slot,out int home)?home:families[slot].home,amount,subtract);}
        void AssignSupplier(int slot,CommercialBuilding shop)
        {
            int id=shop?.id??-1;bool before=supplier.TryGetValue(slot,out int old);if(before&&old==id||!before&&id<0)return;
            demand.TryGetValue(slot,out var current);AccountDemand(slot,current,true);if(id<0)supplier.Remove(slot);else supplier[slot]=id;families[slot].goodsSupplier=id;AccountDemand(slot,current,false);
        }
        void UpdateDemand(int slot)
        {
            var h=families[slot];demand.TryGetValue(slot,out var oldDemand);if(demandHomes.TryGetValue(slot,out int oldHome)&&oldHome!=h.home){AccountDemand(slot,oldDemand,true);supplier.Remove(slot);demandHomes[slot]=h.home;AccountDemand(slot,oldDemand,false);}else demandHomes[slot]=h.home;GoodsAmounts incoming=default;
            if(h.shoppingMember!=0){var member=entities.Member(h.shoppingMember);if(member!=null){incoming=member.Person.carriedProducts;var task=world.FindTask(member.Person.tripId);if(task?.purpose==RoadCore.TransportPurpose.Home)incoming+=task.Shipment?.Products??default;}}
            GoodsAmounts next=default;for(int i=0;i<ProductCatalog.Count;i++){var k=(ProductKind)i;next=next.With(k,Math.Max(0,Need(h,k)-incoming[k]));}
            demand.TryGetValue(slot,out var previous);if(previous.Equals(next))return;AccountDemand(slot,previous,true);demand[slot]=next;AccountDemand(slot,next,false);
        }
        internal int RestockTarget(int shop,ProductKind kind)
        {if(!city.development.enabled)return kind==ProductKind.DailyGoods?32:0;var need=DemandAt(shop);if(need.Total<=32)return need[kind];int food=Math.Min(32,need.food);return kind==ProductKind.Food?food:Math.Min(32-food,need.dailyGoods);}
    }
}
