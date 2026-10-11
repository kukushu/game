using System.Collections.Generic;

namespace HarborCity
{
    public enum BuildingProblemKind { Electricity,Water,Sewage,Garbage,Road,Fire,Abandoned,Bodies,Crime,Processing }
    public sealed class BuildingProblem
    {
        public int buildingId,severity;
        public BuildingProblemKind kind;
        public string detail;
    }
    public sealed partial class CityModel
    {
        // Presentation derived from the same states as simulation, never another outage counter.
        public List<BuildingProblem> BuildingProblems()
        {
            var result=new List<BuildingProblem>();
            foreach(var b in buildings)
            {
                if(b is ParkBuilding)continue;
                bool zoned=CityZoningState.ZoneUse(b.Use);
                int severity=zoned && b.outageDays>=development.provisionalOutageDays-1?2:1;
                if(b.abandoned || b.burned)severity=3;
                if(b.burned){AddProblem(result,b,BuildingProblemKind.Abandoned,3,"烧毁废墟，需要拆除");continue;}
                if(b.burning)AddProblem(result,b,BuildingProblemKind.Fire,3,"火灾中，损坏 "+b.fireDamage.ToString("F0")+"%");
                if(b.abandoned)AddProblem(result,b,BuildingProblemKind.Abandoned,3,"已废弃；恢复服务且废弃满 "+AbandonedRecoveryDays+" 天后重新可用");
                if(!BuildingAccess(b.id))AddProblem(result,b,BuildingProblemKind.Road,severity,"道路未接通城外连接");
                if(development.enabled)
                {
                    var supply=Supply(b.id);
                    if(!(b is PowerBuilding) && !supply.electricity)AddProblem(result,b,BuildingProblemKind.Electricity,severity,"电力未接通或所在电网容量不足");
                    bool needsPipes=!(b is PowerBuilding) && !(b is WaterBuilding) && !(b is SewageBuilding);
                    if(needsPipes && !supply.water)AddProblem(result,b,BuildingProblemKind.Water,severity,"供水未接通或所在管网供水不足");
                    if(needsPipes && !supply.sewage)AddProblem(result,b,BuildingProblemKind.Sewage,severity,"排污未接通或所在管网排污不足");
                    if(waste.enabled && b.garbage>=GarbageWarning)AddProblem(result,b,BuildingProblemKind.Garbage,severity,"垃圾积压 "+b.garbage+"，警戒值 "+GarbageWarning);
                }
                if(BodiesAt(b.id)>0)AddProblem(result,b,BuildingProblemKind.Bodies,CorpseBacklog(b.id)?severity:1,"等待收取遗体 "+BodiesAt(b.id));
                if(b.crime>=CrimeWarning)AddProblem(result,b,BuildingProblemKind.Crime,1,"犯罪积压 "+b.crime.ToString("F0"));
                if(b is LandfillBuilding depot && depot.stored>=LandfillBuilding.Capacity)
                    AddProblem(result,b,BuildingProblemKind.Processing,2,"垃圾处理场暂存已满，收运受限");
            }
            return result;
        }
        static void AddProblem(List<BuildingProblem> list,CityBuilding b,BuildingProblemKind kind,int severity,string detail)
            =>list.Add(new BuildingProblem{buildingId=b.id,kind=kind,severity=severity,detail=detail});
    }
}
