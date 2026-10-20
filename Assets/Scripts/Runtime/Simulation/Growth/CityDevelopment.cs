using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_5_3_OR_NEWER
using Unity.Profiling;
#endif

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
        public float nextEmployment;
        public List<DevelopmentProject> projects=new List<DevelopmentProject>();
        [NonSerialized] public int revision;
    }
    public sealed partial class CityModel
    {
        public CityDevelopmentState development=new CityDevelopmentState();
        [NonSerialized] public int residentialDemand,commercialDemand,industrialDemand;
        // Placeholder asset footprints, not the original game's spawn probabilities.
        internal static readonly (int width,int depth)[] GrowthFootprints={(2,2),(3,2),(2,3),(4,3),(3,4),(4,4),(1,2),(1,1),(2,1),(3,3)};
#if UNITY_5_3_OR_NEWER
        static readonly ProfilerMarker demandSample=new ProfilerMarker("City.Growth.Demand"),housingDemandSample=new ProfilerMarker("City.Growth.Demand.Housing"),jobsDemandSample=new ProfilerMarker("City.Growth.Demand.Jobs"),workersDemandSample=new ProfilerMarker("City.Growth.Demand.Workers"),commerceDemandSample=new ProfilerMarker("City.Growth.Demand.Commerce");
#endif
        public void DevelopmentDemand()
        {
#if UNITY_5_3_OR_NEWER
            using var demandScope=demandSample.Auto();
#endif
            int vacant,vacantJobs,unemployed,commercialCapacity;
#if UNITY_5_3_OR_NEWER
            using(housingDemandSample.Auto())
#endif
            vacant=DevelopmentHousingVacancies();
#if UNITY_5_3_OR_NEWER
            using(jobsDemandSample.Auto())
#endif
            vacantJobs=society.jobEntities.Count(j=>j.occupiedCitizenId<0);
#if UNITY_5_3_OR_NEWER
            using(workersDemandSample.Auto())
#endif
            unemployed=Unemployed;
#if UNITY_5_3_OR_NEWER
            using(commerceDemandSample.Auto())
#endif
            commercialCapacity=DevelopmentCommercialCapacity();
            // Observable demand estimates, not a claim to CS1's undisclosed formula.
            residentialDemand=Math.Clamp(70-vacant*12+vacantJobs*2-unemployed*4,0,100);
            commercialDemand=Math.Clamp(30+population*3-commercialCapacity*6,0,100);
            industrialDemand=Math.Clamp(40+unemployed*8-vacantJobs*8,0,100);
        }
        // Query-local derived counts only: no second mutable occupancy truth or
        // invalidation protocol. Preserve Occupancy's resident/unit/abandoned filters.
        int DevelopmentHousingVacancies()
        {
            var occupied=new Dictionary<int,int>();
            foreach(var family in society.families)
            {
                if(!family.resident||family.unit<0)continue;
                var home=Residence(family.home);
                if(home==null||home.abandoned||family.unit>=home.housingUnits)continue;
                occupied.TryGetValue(home.id,out int count);occupied[home.id]=count+1;
            }
            int vacant=0;
            foreach(var building in buildings)
                if(building is ResidentialBuilding home)
                {occupied.TryGetValue(home.id,out int count);int capacity=home.abandoned?0:home.housingUnits;vacant=checked(vacant+(capacity-count));}
            return vacant;
        }
        int DevelopmentCommercialCapacity()
        {
            // JobCapacity includes every existing job, occupied or not, even in
            // abandoned shops. Do not replace it with theoretical building slots.
            int capacity=0;
            foreach(var job in society.jobEntities)
                if(GetBuilding(job.buildingId) is CommercialBuilding)capacity=checked(capacity+1);
            return capacity;
        }
    }
}
