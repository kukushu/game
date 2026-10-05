using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace HarborCity
{
    public static class CityObservabilityChecks
    {
        [MenuItem("Harbor/Validate runtime observability and native analysis JSON")]
        public static void Validate()
        {
            var c=CityBuildingChecks.Fixture();new CityTraffic(c);
            var reference=CityBuildingChecks.Copy(c);new CityTraffic(reference);
            c.analysis=new CityDailyAnalysis(c);
            var first=c.analysis.Refresh(true);
            CityBuildingChecks.Check(c.logSink==null && first.currentDay.inProgress,"Runtime observation needs no writer");
            var firstJson=JsonUtility.ToJson(first);
            for(int i=0;i<8;i++)
            {
                new CityTraffic(c).Advance(60);new CityTraffic(reference).Advance(60);c.analysis.Refresh(true);
                CityBuildingChecks.Check(CityBuildingChecks.Equivalent(c.ToSaveData(),reference.ToSaveData()),"Analysis changed native simulation/save state");
            }
            CityBuildingChecks.Check(JsonUtility.ToJson(first)==firstJson,"Later observation mutated a previously returned native snapshot");
            var s=c.analysis.State;
            CityBuildingChecks.Check(s.history.Count>0 && s.reports.Count>0 && c.analysis.Failure==null,"Runtime analysis closes real daily reports");
            var restored=JsonUtility.FromJson<CityAnalysisState>(JsonUtility.ToJson(s));
            CityBuildingChecks.Check(restored.live.population==s.live.population && restored.today.produced==s.today.produced && restored.residents.Count==s.residents.Count && restored.reports.Count==s.reports.Count,"Native analysis JSON lost metrics or entities");
            foreach(var b in s.businesses)
            {
                var round=restored.Business(b.id);
                CityBuildingChecks.Check(round.industrial==b.industrial && round.hasTodayReport==b.hasTodayReport && round.hasYesterdayReport==b.hasYesterdayReport,"Optional analysis reports must not become fabricated JsonUtility default objects");
                if(b.industrial)CityBuildingChecks.Check(CityBuildingChecks.Equivalent(round.today,b.today),"Native JSON lost factory daily evidence");
            }
            foreach(var h in s.households)
            {
                var round=restored.Household(h.id);
                CityBuildingChecks.Check(round.hasDecision==h.hasDecision,"Optional household decision presence is explicit");
                CityBuildingChecks.Check(round.members.Select(p=>p.id).SequenceEqual(h.members.Select(p=>p.id)),"Native JSON lost household membership IDs");
            }
            CityBuildingChecks.Check(CityModel.SaveFormat==8 && !JsonUtility.ToJson(c.ToSaveData()).Contains("timeline"),"Observation must not modify gameplay save schema");
            CityBuildingChecks.Result("ObservabilityNative","PASS: runtime analysis/native JSON/save isolation");
        }
    }
}
