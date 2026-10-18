using System;
using HarborCity.RoadCore;
namespace HarborCity
{
    // Utility networks are separate physical infrastructure, not a second road
    // traffic graph. Reuse the existing component/capacity rules and facts.
    public sealed class CityCoreUtilities
    {
        readonly CityModel city;readonly CityEntities entities;readonly World world;
        int entityVersion=-1,roadVersion=-1,powerVersion=-1,waterVersion=-1;
        CityUtilityNetwork power,water;
        bool dirty=true;
        bool roadConduits;
        internal CityCoreFinance Finance;
        public CityCoreUtilities(CityModel city,CityEntities entities,World world){this.city=city;this.entities=entities;this.world=world;city.RoadUtilitySource=()=>world.Network;}
        public void Invalidate(){dirty=true;}
        public void Refresh()
        {
            if(!city.development.enabled)return;
            if(!dirty&&roadConduits==city.utilities.roadConduits&&entityVersion==entities.Revision&&roadVersion==world.NetworkGeneration&&ReferenceEquals(power,city.utilities.electricity)&&ReferenceEquals(water,city.utilities.water)&&powerVersion==power.revision&&waterVersion==water.revision)return;
#if CITY_CORE_ATTRIBUTION
            using var rebuildObservation=CityCoreProfile.Measure(CityCoreProfile.Part.UtilityRebuild);
#endif
            city.RecalculateUtilities();dirty=false;roadConduits=city.utilities.roadConduits;entityVersion=entities.Revision;roadVersion=world.NetworkGeneration;power=city.utilities.electricity;water=city.utilities.water;powerVersion=power.revision;waterVersion=water.revision;
        }
        public void SetBudget(CityServiceKind kind,int percent)
        {
            entities.StructuralBoundary();if(!city.development.enabled||percent<50||percent>150||(kind!=CityServiceKind.Electricity&&kind!=CityServiceKind.Water))throw new CityRuleException("finance.invalid_budget");
            if(city.ServiceBudget(kind)==percent)return;if(kind==CityServiceKind.Electricity)city.development.electricityBudget=percent;else city.development.waterBudget=percent;Invalidate();Refresh();city.Trace("finance.budget_policy","finance.event.budget",new CityLogDetail{origin=(int)kind,amount=percent});
        }
        public int Build(UtilityKind kind,Point from,Point to)
        {
            entities.StructuralBoundary();var network=city.UtilityNetwork(kind);var candidate=network.Plan(new RoadNode{x=(float)from.x,z=(float)from.z},new RoadNode{x=(float)to.x,z=(float)to.z},out string error,city.map.buildHalfSize);if(candidate==null)throw new InvalidOperationException(error);
            double quoted=Math.Ceiling(Point.Distance(new Point(from.x,0,from.z),new Point(to.x,0,to.z))*(kind==UtilityKind.Electricity?15:10));if(quoted>int.MaxValue)throw new InvalidOperationException("管线建设资金不足");
            int paid=(int)quoted;Finance.CheckSpend(paid);
            if(kind==UtilityKind.Electricity)city.utilities.electricity=candidate;else city.utilities.water=candidate;Finance.SpendApproved(paid);Refresh();return paid;
        }
        public bool Remove(UtilityKind kind,int edge)
        {entities.StructuralBoundary();if(!Finance.UnlimitedFunds&&city.money<40)return false;Finance.CheckSpend(40);if(!city.UtilityNetwork(kind).Remove(edge))return false;Finance.SpendApproved(40);Refresh();return true;}
    }
}
