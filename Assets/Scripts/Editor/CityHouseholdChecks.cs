using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace HarborCity
{
    public static class CityHouseholdChecks
    {
        [MenuItem("Harbor/Validate household simulation")]
        public static void Validate()
        {
            var c=CityBuildingChecks.Fixture();var sim=new CityTraffic(c);sim.Advance(360.1f);
            CityBuildingChecks.Check(c.Valid() && c.society.families.Any(h=>h.resident) && c.Citizens.Any(p=>p.lastCommute>0),"Actual household move-in and travel");
            var copy=CityBuildingChecks.Copy(c);
            CityBuildingChecks.Check(CityBuildingChecks.Equivalent(copy.ToSaveData(),c.ToSaveData()),"Native JSON saved society, roads and traffic");
            var resumed=new CityTraffic(copy);sim.Advance(240);resumed.Advance(240);
            CityBuildingChecks.Check(copy.Valid() && c.Valid() && CityBuildingChecks.Equivalent(copy.ToSaveData(),c.ToSaveData()),"Deterministic native save continuation");
            foreach(var r in c.society.history) CityBuildingChecks.Check(r.closingSavings==r.openingSavings+r.wages-r.rent-r.living-r.travel-r.movingCosts && r.closingTreasury==r.openingTreasury+r.rent-r.maintenance,"Daily ledgers");
            var home=c.society.families.First(h=>h.resident).home;CityBuildingChecks.Check(c.DemolishBuilding(home) && c.Valid() && CityBuildingChecks.Copy(c).Valid(),"Home demolition and native save");
            CityBuildingChecks.Result("CityHouseholdChecks","PASS: actual move-in, jobs, commute, native JSON, deterministic continuation, daily ledgers and home demolition");
        }
    }
}

