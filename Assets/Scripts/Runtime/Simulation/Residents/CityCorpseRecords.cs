using System;
using System.Linq;
using System.Collections.Generic;

namespace HarborCity
{
    public enum CorpseStage { Waiting,Assigned,InTransit,Buried }
    [Serializable] public sealed class CityCorpse
    {
        public int citizenId,buildingId=-1,cemeteryId=-1,tripId,diedDay;
        public float x,z;
        public CorpseStage stage;
    }
    [Serializable] public sealed class CityDeathcareState
    {
        public int seed=31337,lastCheckDay,uncollectedDays=3;
        public float oldAgeRisk=.02f,severeIllnessRisk=.005f;
        public List<CityCorpse> bodies=new List<CityCorpse>();
    }
    [Serializable] public sealed class CemeteryBuilding : CityBuilding
    {
        public override LandUse Use=>LandUse.Cemetery;
        // Scaled provisional catalogue; exact original values pending comparison.
        public const int Capacity=48,Hearses=2;
    }
    public sealed partial class CityModel
    {
        public CityDeathcareState deathcare=new CityDeathcareState();
        public int LivingMembers(Household h)=>h.people.Count(p=>!p.dead);
        public int BodiesAt(int building)=>deathcare.bodies.Count(b=>b.buildingId==building && (b.stage==CorpseStage.Waiting || b.stage==CorpseStage.Assigned));
    }
}
