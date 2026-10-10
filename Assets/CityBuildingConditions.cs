using System.Linq;

namespace HarborCity
{
    public sealed partial class CityModel
    {
        // Manual confirms at least four in-game weeks before reinhabiting.
        // The outage trigger is provisional; other original causes follow later.
        public const int AbandonedRecoveryDays=28;
        public bool HasBasicServices(int id)=>GetBuilding(id)!=null && !GetBuilding(id).burning && !GetBuilding(id).burned && BuildingAccess(id) && Supply(id).Complete
            && (!waste.enabled || GetBuilding(id).garbage<GarbageWarning);
        public string BuildingConditionLabel(int id)
        {
            var b=GetBuilding(id);if(b==null)return "建筑不存在";
            if(BodiesAt(id)>0)return "等待灵车 · 未收取遗体 "+BodiesAt(id);
            if(b.burned)return "建筑已烧毁 · 需拆除废墟";
            if(b.crime>=CrimeWarning && !b.abandoned && !b.burning)return "犯罪积压 · "+b.crime.ToString("F0");
            if(b.burning)return "火灾中 · 损坏 "+b.fireDamage.ToString("F0")+"%";
            string problem=BuildingAccess(id)?Supply(id).Problem:"道路未接通";
            if(development.enabled && waste.enabled && b.garbage>=GarbageWarning)problem="垃圾积压 · "+b.garbage;
            if(b.abandoned)return HasBasicServices(id)?"已废弃 · 至少还需等待 "+System.Math.Max(0,28-(day-b.abandonedDay))+" 天":"已废弃 · "+problem;
            return b.outageDays>0?problem+" · 持续 "+b.outageDays+" 天":problem;
        }
        int UpdateBuildingConditions()
        {
            int left=0;bool changed=false;
            foreach(var b in buildings.Where(b=>CityZoningState.ZoneUse(b.Use)))
            {
                if(b.burned || b.burning)continue;
                bool connected=HasBasicServices(b.id) && !CorpseBacklog(b.id);
                if(connected)b.outageDays=0;else b.outageDays=System.Math.Min(100000,b.outageDays+1);
                if(!b.abandoned && b.outageDays>=development.provisionalOutageDays)
                {
                    b.abandoned=true;b.abandonedDay=day;changed=true;
                    new CityTraffic(this).RemoveBuilding(b.id);
                    foreach(var h in society.families.Where(h=>h.resident && h.home==b.id))
                    {
                        h.resident=false;h.home=h.unit=-1;h.reason="住宅因持续供给或垃圾服务问题而废弃";left++;
                        foreach(var p in h.people.Where(p=>!p.dead)){ReleaseJob(p);p.tripId=0;p.location=-1;p.atWork=false;p.accessNode=-1;p.medicalStage=MedicalStage.None;p.medicalClinicId=-1;p.treatmentMinutes=0;ReleaseSchoolPlace(p);p.atSchool=p.schoolReturning=false;}
                        Trace("household.left",h.reason,h,household:h.id,building:b.id);
                    }
                    Trace("building.abandoned","持续供给/垃圾问题导致废弃；触发天数为待核实近似参数",b,building:b.id);
                }
                else if(b.abandoned && connected && day-b.abandonedDay>=AbandonedRecoveryDays)
                {
                    b.abandoned=false;b.abandonedDay=0;changed=true;
                    Trace("building.reinhabited","供给恢复且已等待至少四个游戏周，建筑重新可用",b,building:b.id);
                }
            }
            if(changed){BuildingsChanged();SyncJobs();}
            return left;
        }
    }
}
