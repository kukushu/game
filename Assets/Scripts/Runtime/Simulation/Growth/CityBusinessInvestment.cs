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
            var p=m.Person;bool eligible=entities.Member(p.id)==m&&m.Family.resident&&!p.dead&&p.canWork&&p.age>=18&&p.jobId<0&&p.tripId==0&&!p.atWork;
            if(availableWorkers.TryGetValue(p.id,out int old)){if(eligible&&old==p.skill)return;CountSkill(old,-1);availableWorkers.Remove(p.id);}
            if(eligible){availableWorkers.Add(p.id,p.skill);CountSkill(p.skill,1);}
        }
        bool HasWorker(int required){foreach(var pair in skills)if(pair.Key>=required&&pair.Value>0)return true;return false;}
        void Factory(IndustrialBuilding f,bool added)
        {if(added&&f.Stock>0)stockedFactories.Add(f.id);else stockedFactories.Remove(f.id);if(added&&f.externalCapital>0&&!f.factory.firstDelivery)firstDeliveryPending.Add(f.id);else firstDeliveryPending.Remove(f.id);}
        bool Reachable(int source,int destination)
        {var a=world.GetAccess(source);var b=world.GetAccess(destination);return a!=null&&b!=null&&a.valid&&b.valid&&world.Network.CanReach(a,b);}
        bool InvestmentOpportunity(LandUse use,out ProductKind product,out int reserve,out CityBuilding focus)
        {
            product=ProductKind.Food;reserve=0;focus=null;var simulation=entities.Simulation;if(simulation==null||!HasWorker(use==LandUse.Industrial?1:0))return false;
            var market=simulation.Consumption;
            if(use==LandUse.Commercial)
            {
                foreach(var pair in market.UnservedDemand){int amount=pair.Value.Total;var home=entities.Building(pair.Key);if(home==null||!city.BuildingAccess(home.id))continue;if(amount>reserve||amount==reserve&&focus!=null&&home.id<focus.id){reserve=amount;focus=home;}}
                return reserve>0;
            }
            // One best actual procurement location per product; no factory x shop
            // matrix and no per-household inventory copies for investment.
            for(int p=0;p<ProductCatalog.Count;p++)
            {
                var k=(ProductKind)p;bool pending=false;foreach(int id in firstDeliveryPending)if((entities.Building(id) as IndustrialBuilding)?.factory.product==k){pending=true;break;}if(pending)continue;
                int gap=0;CommercialBuilding shop=null;
                foreach(var pair in market.ShopDemand)
                {
                    var candidate=entities.Building(pair.Key) as CommercialBuilding;if(candidate==null||candidate.abandoned||!city.BuildingAccess(candidate.id))continue;
                    int n=market.RestockTarget(candidate.id,k)-candidate.Products[k]-simulation.Shipments.Goods(candidate.id)[k];
                    if(candidate.Account.Cash<simulation.Design.Products[k].WholesalePrice)continue;
                    if(n>gap||n==gap&&shop!=null&&candidate.id<shop.id){gap=n;shop=candidate;}
                }
                if(shop==null||gap<=0)continue;
                foreach(int id in stockedFactories){var f=entities.Building(id) as IndustrialBuilding;if(f!=null&&f.factory.product==k&&Reachable(id,shop.id)){gap-=f.Stock;if(gap<=0)break;}}
                foreach(var project in city.development.projects)gap-=project.supplyReserved[k];
                if(gap>reserve){reserve=gap;product=k;focus=shop;}
            }
            return reserve>0;
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
