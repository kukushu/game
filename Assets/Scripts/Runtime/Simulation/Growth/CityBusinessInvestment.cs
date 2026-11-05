using System;
using System.Collections.Generic;
using HarborCity.RoadCore;
namespace HarborCity
{
    public sealed partial class CityCoreGrowth
    {
        readonly Dictionary<int,int> availableWorkers=new Dictionary<int,int>();
        readonly SortedDictionary<int,int> skills=new SortedDictionary<int,int>();
        readonly HashSet<int> firstDeliveryPending=new HashSet<int>(),stockedFactories=new HashSet<int>();
        readonly List<(CommercialBuilding shop,int gap)> procurement=new List<(CommercialBuilding,int)>();
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
        {if(added&&f.Stock>0)stockedFactories.Add(f.id);else stockedFactories.Remove(f.id);if(added&&f.externalCapital>0&&!f.factory.firstDelivery)firstDeliveryPending.Add(f.id);else firstDeliveryPending.Remove(f.id);}
        bool Reachable(int source,int destination)
        {var a=world.GetAccess(source);var b=world.GetAccess(destination);return a!=null&&b!=null&&a.valid&&b.valid&&world.Network.CanReach(a,b);}
        internal bool InvestmentOpportunity(LandUse use,out ProductKind product,out int reserve,out CityBuilding focus)
        {
            product=ProductKind.Food;reserve=0;focus=null;var simulation=entities.Simulation;if(simulation==null||!HasWorker(use==LandUse.Industrial?1:0))return false;
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
                var k=(ProductKind)p;bool pending=false;foreach(int id in firstDeliveryPending)if((entities.Building(id) as IndustrialBuilding)?.factory.product==k){pending=true;break;}if(pending)continue;
                procurement.Clear();
                foreach(var pair in market.ShopDemand)
                {
                    var candidate=entities.Building(pair.Key) as CommercialBuilding;if(candidate==null||candidate.abandoned||!city.BuildingAccess(candidate.id))continue;
                    int n=market.RestockTarget(candidate.id,k)-candidate.Products[k]-simulation.Shipments.Goods(candidate.id)[k];
                    if(candidate.Account.Cash<simulation.Design.Products[k].WholesalePrice)continue;
                    if(n>0)procurement.Add((candidate,n));
                }
                procurement.Sort((a,b)=>{int c=b.gap.CompareTo(a.gap);return c!=0?c:a.shop.id.CompareTo(b.shop.id);});
                foreach(var entry in procurement)
                {
                    if(entry.gap<reserve)break;int gap=entry.gap;var shop=entry.shop;
                    foreach(int id in stockedFactories){var f=entities.Building(id) as IndustrialBuilding;if(f!=null&&!f.abandoned&&!f.burned&&f.factory.product==k&&Reachable(id,shop.id)){gap-=f.Stock;if(gap<=0)break;}}
                    foreach(var project in city.development.projects)gap-=project.supplyReserved[k];
                    if(gap>reserve||gap>0&&gap==reserve&&product==k&&focus!=null&&shop.id<focus.id){reserve=gap;product=k;focus=shop;}
                }
            }
            return reserve>0;
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
        static void CheckCapital(CityBuilding b){if(double.IsNaN(b.externalCapital)||double.IsInfinity(b.externalCapital)||b.externalCapital<0||b.externalCapital>0&&!(b is IndustrialBuilding)&&!(b is CommercialBuilding))throw new InvalidOperationException("Invalid external startup capital");}
        internal void ValidateInvestment()
        {
            double sum=0;foreach(var b in entities.Buildings){CheckCapital(b);sum+=b.externalCapital;}foreach(var b in entities.RetiredBuildings){CheckCapital(b);sum+=b.externalCapital;}
            if(sum!=city.development.externalInvestment||double.IsNaN(sum)||double.IsInfinity(sum)||sum<0)throw new InvalidOperationException("External business investment ledger mismatch");
            foreach(var p in city.development.projects)p.supplyReserved.Validate();
        }
    }
}
