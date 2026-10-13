using System;
using System.Linq;

namespace HarborCity
{
    public sealed partial class CityModel
    {
        // Provisional scales, not confirmed CS1 formulae. Illness is personal;
        // no population-wide health value controls attendance or migration.
        public const float SickHealthThreshold=40;
        public int SickResidents => Citizens.Count(p=>p.sick);
        public float AverageHealth => Citizens.Select(p=>p.health).DefaultIfEmpty(100).Average();
        public bool CanAttendWork(CityResident p)=>p!=null && p.canWork && !p.sick;

        public void UpdateResidentHealth()
        {
            if(!development.enabled)return;
            foreach(var h in society.families.Where(h=>h.resident))foreach(var p in h.people.Where(p=>!p.dead))
            {
                // Daily exposure sample at the resident's actual position. This is
                // not a continuous exposure model or a copy of the original formula.
                var activity=ObserveResident(h,p);
                if(!activity.located)continue;
                var environment=EnvironmentAt(activity.x,activity.z);
                var building=GetBuilding(activity.building);
                float garbage=building==null?0:Math.Clamp(building.garbage/(float)GarbageWarning,0,1);
                bool freshWater=building==null || Supply(building.id).water && Supply(building.id).sewage;
                float pressure=12*Math.Clamp(environment.pollution,0,1)+4*Math.Clamp(environment.noise,0,1)+8*garbage+(freshWater?0:6)+2*(building?.crime??0)/100;
                p.health=Math.Clamp(p.health+(!p.sick?2:0)-pressure,0,100);
                if(!p.sick && p.health<=SickHealthThreshold)
                {
                    p.sick=true;
                    Trace("citizen.sick","居民患病；暂时不能实际工作，保留岗位合同",p,household:h.id,citizen:p.id,job:p.jobId,building:activity.building,level:"warning");
                }
            }
        }
    }
}
