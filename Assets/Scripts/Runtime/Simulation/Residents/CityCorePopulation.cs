using System;
using System.Linq;
using System.Collections.Generic;
using HarborCity.RoadCore;
namespace HarborCity
{
    // Baseline private housing migration and nearest reachable vacancy. No
    // personality or speculative lifetime model; lifecycle owner registers facts.
    public sealed class CityCorePopulation
    {
        readonly CityModel city;readonly CityEntities entities;readonly World world;readonly CityCoreUtilities utilities;
        public CityCorePopulation(CityModel city,CityEntities entities,World world,CityCoreUtilities utilities){this.city=city;this.entities=entities;this.world=world;this.utilities=utilities;}
        internal void Step(TimeContext time)
        {
            if(!city.development.enabled)return;
            bool dayDue=city.day>city.society.lastSettledDay;
            if(!dayDue&&time.Start<city.development.nextEmployment)return;
            if(dayDue){Migrate();city.society.lastSettledDay=city.day;}
            if(FindJobs())utilities.Invalidate();city.development.nextEmployment=(float)(time.Start+6);utilities.Refresh();UpdateStatistics();
        }
        void Migrate()
        {
            var occupied=new Dictionary<int,HashSet<int>>();foreach(var h in city.society.families)if(h.resident){if(!occupied.TryGetValue(h.home,out var units))occupied.Add(h.home,units=new HashSet<int>());units.Add(h.unit);}
            int arrivals=0;
            foreach(var home in city.buildings.OfType<ResidentialBuilding>())
            {
                if(home.abandoned||home.burning||home.burned||!city.BuildingAccess(home.id)||!city.Supply(home.id).Complete)continue;
                if(!occupied.TryGetValue(home.id,out var units))occupied.Add(home.id,units=new HashSet<int>());
                for(int unit=0;unit<home.housingUnits;unit++)
                {
                    if(arrivals>=city.society.settings.applicantsPerDay||city.society.families.Count>=10000)return;if(units.Contains(unit))continue;
                    var family=new Household{id=city.society.nextId,home=home.id,unit=unit,resident=true,rent=0,reason="迁入分区住宅；个人寻找实际岗位"};family.savings=450+family.id%7*100;family.openingSavings=family.savings;entities.AddFamily(family);
                    int count=2+family.id%3;for(int slot=0;slot<count;slot++){int age=slot==0?25+family.id%30:slot==1?24+family.id%30:6+(family.id+slot)%12;var person=new CityResident{id=city.society.nextCitizenId,age=age,canWork=age>=18,skill=(family.id+slot)%3,location=home.id,name="居民 "+city.society.nextCitizenId};entities.AddResident(family.id,person);}
                    units.Add(unit);arrivals++;
                }
            }
        }
        bool FindJobs()
        {
            // One bucket per actual workplace; estimate once per home/work pair,
            // not once per job and not in the fixed vehicle loop.
            var vacant=new Dictionary<int,List<CityJob>>();foreach(var job in city.society.jobEntities)if(job.occupiedCitizenId<0&&city.JobFunded(job)&&city.BuildingAccess(job.buildingId)){if(!vacant.TryGetValue(job.buildingId,out var slots))vacant.Add(job.buildingId,slots=new List<CityJob>());slots.Add(job);}
            var distances=new Dictionary<(int,int),double>();bool assigned=false;
            foreach(var member in entities.Members)
            {
                var p=member.Person;var h=member.Family;if(!h.resident||p.dead||!p.canWork||p.jobId>=0||p.tripId!=0||p.atWork)continue;CityJob best=null;double nearest=double.PositiveInfinity;
                foreach(var pair in vacant)
                {
                    CityJob candidate=null;foreach(var job in pair.Value)if(job.occupiedCitizenId<0&&job.requiredSkill<=p.skill&&(candidate==null||job.id<candidate.id))candidate=job;if(candidate==null)continue;
                    var key=(h.home,pair.Key);if(!distances.TryGetValue(key,out double seconds)){seconds=world.EstimateResidentSeconds(h.home,pair.Key);distances.Add(key,seconds);}if(double.IsInfinity(seconds))continue;
                    if(seconds<nearest||seconds==nearest&&(best==null||candidate.id<best.id)){nearest=seconds;best=candidate;}
                }
                if(best!=null)assigned|=city.AssignJob(p,best.id);
            }
            return assigned;
        }
        public void UpdateStatistics(){int population=0;foreach(var m in entities.Members)if(m.Family.resident&&!m.Person.dead)population++;city.population=population;city.jobs=city.society.jobEntities.Count;}
    }
}
