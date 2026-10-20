using System;
using System.Collections.Generic;
using System.Linq;

namespace HarborCity
{
    [Serializable] public sealed class CityJob
    {
        public int id, buildingId, wage, requiredSkill;
        public int occupiedCitizenId=-1;
    }
    public sealed partial class CityModel
    {
        public CityJob Job(int id)
        {
#if CITY_CORE_ATTRIBUTION
            using var observation=HarborCity.RoadCore.CityCoreProfile.Measure(HarborCity.RoadCore.CityCoreProfile.Part.Job);
#endif
            if(entities!=null)return entities.Job(id);
            return FindJobRecord(id);
        }
        // Session construction can query records before the entity index is attached.
        // Keep that predicate out of the indexed path so it does not allocate.
        CityJob FindJobRecord(int id)=>society?.jobEntities?.Find(j=>j.id==id);
        public CityJob ResidentJob(CityResident p)
        {
            var job=p==null?null:Job(p.jobId);
            return job!=null && job.occupiedCitizenId==p.id?job:null;
        }
        public int Workplace(CityResident p) => ResidentJob(p)?.buildingId ?? -1;
        public int ResidentWage(CityResident p) => ResidentJob(p)?.wage ?? 0;
        public IEnumerable<CityResident> Citizens => society.families.Where(h=>h.resident).SelectMany(h=>h.people).Where(p=>!p.dead);
        public int Employed => Citizens.Count(p=>p.canWork && ResidentJob(p)!=null);
        public int Unemployed => Citizens.Count(p=>p.canWork && ResidentJob(p)==null);
        int BuildingJobSlots(int id) => GetBuilding(id)!=null && !GetBuilding(id).abandoned && (GetBuilding(id) is CommercialBuilding || GetBuilding(id) is IndustrialBuilding)?GetBuilding(id).level*(entities?.Employment.SlotsPerLevel??4):0;
        public void ReleaseJob(CityResident p)
        {
            var job=ResidentJob(p); if(job!=null) job.occupiedCitizenId=-1;
            if(job!=null) Trace("job.released","居民释放岗位",job,household:p.householdId,citizen:p.id,job:job.id,building:job.buildingId);
            p.jobId=-1; p.requestedAt=-1;
        }
        public bool AssignJob(CityResident p,int jobId)
        {
            if(p==null || !society.families.Any(h=>h.resident && h.id==p.householdId && h.people.Contains(p))) return false;
            var job=Job(jobId);
            if(!JobFunded(job) || !p.canWork || p.age<18 || p.skill<job.requiredSkill ||
                (job.occupiedCitizenId!=-1 && job.occupiedCitizenId!=p.id)) return false;
            if(p.jobId==jobId) return true;
            if(GetBuilding(job.buildingId)?.burning==true) return false;
            // Normal choices cannot change employment halfway through a trip/shift.
            if(p.tripId>0 || p.atWork) return false;
            ReleaseJob(p); p.jobId=job.id; job.occupiedCitizenId=p.id;
            Trace("job.assigned","居民占用岗位",job,household:p.householdId,citizen:p.id,job:job.id,building:job.buildingId); return true;
        }
        public void SyncJobs()
        {
            if(society?.jobEntities==null) return;
            var people=society.families.SelectMany(h=>h.people).ToDictionary(p=>p.id);
            foreach(var group in society.jobEntities.GroupBy(j=>j.buildingId).ToList())
            {
                int excess=group.Count()-BuildingJobSlots(group.Key);
                foreach(var job in group.OrderBy(j=>j.occupiedCitizenId>=0).ThenByDescending(j=>j.id).Take(Math.Max(0,excess)).ToList())
                {
                    if(people.TryGetValue(job.occupiedCitizenId,out var p)) ReleaseJob(p);
                    society.jobEntities.Remove(job);
                    Trace("job.removed","建筑岗位容量减少或建筑已拆除",job,job:job.id,building:job.buildingId);
                }
            }
            var counts=society.jobEntities.GroupBy(j=>j.buildingId).ToDictionary(g=>g.Key,g=>g.Count());
            foreach(var building in buildings)
            {
                int i=building.id;
                counts.TryGetValue(i,out int existing);
                for(int n=existing;n<BuildingJobSlots(i);n++)
                {
                    var job=new CityJob {id=society.nextJobId++,buildingId=i,wage=entities==null?95+(i%5)*12+(UseOf(i)==LandUse.Industrial?20:0):entities.Employment.Wage(i,UseOf(i)==LandUse.Industrial),requiredSkill=UseOf(i)==LandUse.Industrial?1:0};
                    society.jobEntities.Add(job); Trace("job.created","建筑提供岗位",job,job:job.id,building:i);
                }
            }
            entities?.RebuildJobs();
        }
    }
}
