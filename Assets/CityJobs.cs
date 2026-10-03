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
        public CityJob Job(int id) => society?.jobEntities?.Find(j=>j.id==id);
        public CityJob ResidentJob(CityResident p)
        {
            var job=p==null?null:Job(p.jobId);
            return job!=null && job.occupiedCitizenId==p.id?job:null;
        }
        public int Workplace(CityResident p) => ResidentJob(p)?.buildingId ?? -1;
        public int ResidentWage(CityResident p) => ResidentJob(p)?.wage ?? 0;
        public IEnumerable<CityResident> Citizens => society.families.Where(h=>h.resident).SelectMany(h=>h.people);
        public int Employed => Citizens.Count(p=>p.canWork && ResidentJob(p)!=null);
        public int Unemployed => Citizens.Count(p=>p.canWork && ResidentJob(p)==null);
        public int HouseholdSalary(Household h) => h.people.Sum(p=>ExpectedJobWage(ResidentJob(p)));
        public float HouseholdCommute(Household h) => h.people.Where(p=>ResidentJob(p)!=null)
            .Sum(p=>Math.Max(0,ExpectedCommute(p,h.home,Workplace(p))));
        // No observation is invented for a worker who has never reached this workplace.
        public float AverageCommute => Citizens.Where(p=>ResidentJob(p)!=null && p.lastCommute>=0 && p.observedWork==Workplace(p)
            && society.families.Any(h=>h.id==p.householdId && h.home==p.observedHome))
            .Select(p=>p.lastCommute).DefaultIfEmpty(0).Average();
        int BuildingJobSlots(int id) => id>=0 && id<tiles.Length && (tiles[id]==3 || tiles[id]==4)?Math.Max(1,levels[id])*4:0;
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
            for(int i=0;i<tiles.Length;i++)
            {
                counts.TryGetValue(i,out int existing);
                for(int n=existing;n<BuildingJobSlots(i);n++)
                {
                    var job=new CityJob {id=society.nextJobId++,buildingId=i,wage=95+(i%5)*12+(tiles[i]==4?20:0),requiredSkill=tiles[i]==4?1:0};
                    society.jobEntities.Add(job); Trace("job.created","建筑提供岗位",job,job:job.id,building:i);
                }
            }
        }
        public void EnableJobs(bool newCity=false)
        {
            if(version>=5) return;
            EnsureResidents(); society.jobEntities=new List<CityJob>(); society.nextJobId=1;
            foreach(var h in society.families)
                for(int i=0;i<h.people.Count;i++)
                {
                    var p=h.people[i]; p.householdId=h.id; p.canWork=p.age>=18;
                    p.skill=i==0?h.skill:(h.id+i)%3; p.jobId=-1;
                }
            SyncJobs();
            foreach(var h in society.families.Where(h=>h.resident))
            {
                // v4 has one employer. Restore it to its original worker without resetting journeys or earnings.
                var p=h.people.Find(person=>person.worker) ?? h.people.FirstOrDefault();
                if(p==null || !p.canWork) continue;
                var job=society.jobEntities.Find(j=>j.buildingId==h.work && j.occupiedCitizenId<0 && j.requiredSkill<=p.skill);
                if(job!=null) {p.jobId=job.id; job.occupiedCitizenId=p.id;}
            }
            version=5;
            if(newCity)
                foreach(var h in society.families.Where(h=>h.resident)) ApplyJobPlan(h,Evaluate(h,h.home));
        }
        bool ApplyJobPlan(Household h,HouseholdOption option)
        {
            var reserved=new HashSet<int>();
            for(int i=0;i<option.citizenIds.Count;i++)
            {
                var p=h.people.Find(c=>c.id==option.citizenIds[i]); int id=option.jobIds[i];
                if(p==null || (p.tripId>0 || p.atWork) && p.jobId!=id) return false;
                if(id<0) continue;
                var j=Job(id);
                if(!JobFunded(j) || !p.canWork || p.age<18 || p.skill<j.requiredSkill || !reserved.Add(id) || j.occupiedCitizenId>=0 && j.occupiedCitizenId!=p.id) return false;
            }
            for(int i=0;i<option.citizenIds.Count;i++)
            {
                var p=h.people.Find(c=>c.id==option.citizenIds[i]);
                // This transaction also serves outside applicants immediately before housing is committed.
                int id=option.jobIds[i];
                if(p.jobId==id) continue;
                ReleaseJob(p);
                if(id>=0) {p.jobId=id; Job(id).occupiedCitizenId=p.id; Trace("job.assigned","家庭决策分配个人岗位",Job(id),household:h.id,citizen:p.id,job:id,building:Workplace(p));}
            }
            return true;
        }
        public bool ValidJobs()
        {
            if(society.jobEntities==null || society.nextJobId<1) return false;
            var citizens=new Dictionary<int,CityResident>(); var residentIds=new HashSet<int>();
            foreach(var h in society.families)
            {
                if(h.people==null || h.people.Count==0) return false;
                foreach(var p in h.people)
                {
                    if(p==null || p.id<=0 || citizens.ContainsKey(p.id) || p.householdId!=h.id || p.skill<0 || p.canWork && p.age<18 || p.jobId < -1) return false;
                    citizens.Add(p.id,p); if(h.resident) residentIds.Add(p.id);
                }
            }
            var ids=new HashSet<int>(); var employees=new HashSet<int>(); var jobMap=new Dictionary<int,CityJob>();
            foreach(var j in society.jobEntities)
            {
                if(j==null || j.id<1 || j.id>=society.nextJobId || !ids.Add(j.id) || BuildingJobSlots(j.buildingId)==0 || j.wage<0 || j.requiredSkill<0 || j.occupiedCitizenId < -1) return false;
                jobMap.Add(j.id,j);
                if(j.occupiedCitizenId<0) continue;
                if(!citizens.TryGetValue(j.occupiedCitizenId,out var p) || !residentIds.Contains(p.id) || !employees.Add(p.id)
                    || !p.canWork || p.skill<j.requiredSkill || p.jobId!=j.id) return false;
            }
            foreach(var p in citizens.Values)
                if(p.jobId>=0 && (!jobMap.TryGetValue(p.jobId,out var j) || j.occupiedCitizenId!=p.id)) return false;
            var counts=society.jobEntities.GroupBy(j=>j.buildingId).ToDictionary(g=>g.Key,g=>g.Count());
            for(int i=0;i<tiles.Length;i++) {counts.TryGetValue(i,out int count); if(count!=BuildingJobSlots(i)) return false;}
            return true;
        }
    }
}
