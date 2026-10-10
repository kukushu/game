using System;
using System.Linq;

namespace HarborCity
{
    [Serializable] public sealed class CityLifecycleState
    {
        // Provisional pacing and boundaries, not confirmed original age units.
        public double daysPerYear=30;
    }
    public sealed partial class CityModel
    {
        public CityLifecycleState lifecycle=new CityLifecycleState();
        public const int WorkingAge=18,RetirementAge=65;
        public void UpdateResidentAges()
        {
            if(!development.enabled)return;
            var transport=new CityTraffic(this);
            foreach(var p in Citizens)
            {
                if(p.lastAgeDay==day)continue;p.lastAgeDay=day;
                int previous=p.age;
                p.ageProgress+=1/lifecycle.daysPerYear;
                int years=(int)Math.Floor(p.ageProgress+1e-10);
                p.ageProgress=Math.Max(0,p.ageProgress-years);p.age=Math.Min(120,p.age+years);
                // Mortality is a separate pending entity workflow. This cap must
                // not be presented as an original-game immortality rule.
                if(p.age==120)p.ageProgress=0;
                if(previous<WorkingAge && p.age>=WorkingAge)p.canWork=true;
                if(p.age>=RetirementAge && p.canWork)
                {p.canWork=false;ReleaseJob(p);transport.ReturnRetiredCommuter(p);}
                if(p.schoolId>=0 && !SchoolMatches(p))transport.EndSchoolEnrollment(p);
                if(previous!=p.age)Trace("citizen.aged","居民年龄推进；年龄单位与速率仍为近似",p,household:p.householdId,citizen:p.id);
            }
        }
        bool ValidResidentAging()
        {
            if(lifecycle==null || double.IsNaN(lifecycle.daysPerYear) || double.IsInfinity(lifecycle.daysPerYear) || lifecycle.daysPerYear<1 || lifecycle.daysPerYear>3650)return false;
            return society.families.SelectMany(h=>h.people).All(p=>!double.IsNaN(p.ageProgress) && !double.IsInfinity(p.ageProgress) && p.ageProgress>=0 && p.ageProgress<1
                && p.lastAgeDay>=0 && p.lastAgeDay<=day);
        }
    }
    public sealed partial class CityTraffic
    {
        public void EndSchoolEnrollment(CityResident p)
        {
            int school=p.schoolId;city.ReleaseSchoolPlace(p);
            var h=city.society.families.Find(f=>f.id==p.householdId);var trip=State.trips.Find(t=>t.id==p.tripId);
            if(h==null)return;
            if(trip?.purpose==TripPurpose.School){RedirectFromCurrent(trip,h.home);p.schoolReturning=true;}
            else if(p.atSchool){p.atSchool=false;p.schoolReturning=true;}
            city.Trace("education.age_departure","离开当前教育阶段，释放学位并从实际位置返家；不自动授予学历",p,household:h.id,citizen:p.id,building:school);
        }
        public void ReturnRetiredCommuter(CityResident p)
        {
            var h=city.society.families.Find(f=>f.id==p.householdId);var trip=State.trips.Find(t=>t.id==p.tripId);
            if(h!=null && trip?.purpose==TripPurpose.Commute)RedirectFromCurrent(trip,h.home);
        }
    }
}
