using System.Linq;

namespace HarborCity
{
    public sealed partial class CityModel
    {
        // User override: seven game days from abandonment; services must also recover.
        // The outage trigger is provisional; other original causes follow later.
        public const int AbandonedRecoveryDays=7;
        public bool HasBasicServices(int id)=>GetBuilding(id)!=null && !GetBuilding(id).burning && !GetBuilding(id).burned && BuildingAccess(id) && Supply(id).Complete
            && (!waste.enabled || GetBuilding(id).garbage<GarbageWarning);
        public string BuildingConditionLabel(int id)
        {
            var b=GetBuilding(id);if(b==null)return "建筑不存在";
            if(b.burned)return "建筑已烧毁 · 需拆除废墟";
            if(b.burning)return "火灾中 · 损坏 "+b.fireDamage.ToString("F0")+"%";
            if(BodiesAt(id)>0)return (b.abandoned?"已废弃 · ":"")+"等待灵车 · 未收取遗体 "+BodiesAt(id);
            if(b.crime>=CrimeWarning && !b.abandoned && !b.burning)return "犯罪积压 · "+b.crime.ToString("F0");
            string problem=BuildingAccess(id)?Supply(id).Problem:"道路未接通";
            if(development.enabled && waste.enabled && b.garbage>=GarbageWarning)problem="垃圾积压 · "+b.garbage;
            if(b.abandoned)return HasBasicServices(id)?"已废弃 · 至少还需等待 "+System.Math.Max(0,AbandonedRecoveryDays-(day-b.abandonedDay))+" 天":"已废弃 · "+problem;
            return b.outageDays>0?problem+" · 持续 "+b.outageDays+" 天":problem;
        }
    }
}
