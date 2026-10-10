using System;
using System.Linq;
using System.Collections.Generic;

namespace HarborCity
{
    [Serializable] public abstract class SchoolBuilding : CityBuilding
    {
        public abstract int Grade {get;}
        public abstract int BaseSeats {get;}
    }
    [Serializable] public sealed class HighSchoolBuilding : SchoolBuilding
    {
        public override LandUse Use=>LandUse.HighSchool;
        public override int Grade=>2;
        public override int BaseSeats=>24;
    }
    [Serializable] public sealed class UniversityBuilding : SchoolBuilding
    {
        public override LandUse Use=>LandUse.University;
        public override int Grade=>3;
        public override int BaseSeats=>36;
    }
    [Serializable] public sealed class ElementarySchoolBuilding : SchoolBuilding
    {
        public override LandUse Use=>LandUse.ElementarySchool;
        public const int Seats=24; // Provisional scaled capacity, not CS1's asset value.
        public override int Grade=>1;
        public override int BaseSeats=>Seats;
    }
    public sealed partial class CityModel
    {
        // The manual confirms children attend elementary school; numeric age
        // boundaries, hours and graduation duration still need original comparison.
        public const float ElementaryStudyMinutes=7200;
        public bool ElementaryEligible(CityResident p)=>p!=null && !p.dead && p.age>=6 && p.age<14 && !p.canWork;
        public bool HighSchoolEligible(CityResident p)=>p!=null && !p.dead && p.age>=14 && p.age<18 && p.education>=1 && !p.canWork;
        public bool UniversityEligible(CityResident p)=>p!=null && !p.dead && p.age>=WorkingAge && p.age<RetirementAge && p.education==2 && p.jobId<0;
        public bool SchoolEligible(CityResident p,int grade)=>grade==1?ElementaryEligible(p):grade==2?HighSchoolEligible(p):grade==3 && UniversityEligible(p);
        public bool SchoolEligible(CityResident p)=>SchoolEligible(p,1) || SchoolEligible(p,2) || SchoolEligible(p,3);
        public bool SchoolMatches(CityResident p)=>GetBuilding(p?.schoolId??-1) is SchoolBuilding s && SchoolEligible(p,s.Grade);
        public static float CourseMinutes(int grade)=>grade==1?ElementaryStudyMinutes:grade==2?5760:9600;
        public static float CourseProgress(CityResident p,int grade)=>grade==1?p.studyMinutes:grade==2?p.highSchoolStudyMinutes:p.universityStudyMinutes;
        public static string SchoolName(int grade)=>grade==1?"小学":grade==2?"高中":"大学";
        public int CapacityAtSchool(int id)=>GetBuilding(id) is SchoolBuilding s?Math.Max(1,ServiceCapacity(CityServiceKind.Education,s.BaseSeats)):0;
        public int SchoolCapacity=>Math.Max(1,ServiceCapacity(CityServiceKind.Education,ElementarySchoolBuilding.Seats));
        public int EnrolledAt(int id)=>Citizens.Count(p=>p.schoolId==id);
        public int StudentsAt(int id)=>Citizens.Count(p=>p.schoolId==id && p.atSchool);
        public void ReleaseSchoolPlace(CityResident p)
        {
            if(GetBuilding(p.schoolId) is UniversityBuilding)p.canWork=p.age>=WorkingAge && p.age<RetirementAge;
            p.schoolId=-1;
        }
        public int ElementaryDemand=>Citizens.Count(ElementaryEligible);
        public int ElementaryEnrolled=>Citizens.Count(p=>GetBuilding(p.schoolId) is ElementarySchoolBuilding);
        public bool EnrollElementary(int citizen,int school)
            =>GetBuilding(school) is ElementarySchoolBuilding && EnrollSchool(citizen,school);
        public bool EnrollSchool(int citizen,int school)
        {
            var person=Citizens.FirstOrDefault(p=>p.id==citizen);
            var facility=GetBuilding(school) as SchoolBuilding;
            if(!development.enabled || facility==null || !SchoolEligible(person,facility.Grade) || person.schoolReturning || person.tripId!=0 || person.atSchool || person.atWork || person.sick || person.medicalStage!=MedicalStage.None || !HasBasicServices(school))return false;
            if(person.schoolId==school)return true;
            if(person.schoolId>=0)return false;
            var family=society.families.First(h=>h.id==person.householdId);
            if(!family.resident || person.location!=family.home || EnrolledAt(school)>=CapacityAtSchool(school) || TravelMinutes(family.home,school)<0)return false;
            person.schoolId=school;if(facility.Grade==3)person.canWork=false;
            Trace("education.enrolled","居民占用"+SchoolName(facility.Grade)+"学位；尚未到校",person,household:family.id,citizen:person.id,building:school);return true;
        }
        public void PrepareSchoolEnrollment()
        {
            if(!development.enabled)return;
            foreach(var p in Citizens.Where(p=>p.schoolId<0 && SchoolEligible(p)))
            {
                var h=society.families.First(f=>f.id==p.householdId);
                foreach(var s in buildings.OfType<SchoolBuilding>().OrderBy(b=>TravelMinutes(h.home,b.id)).ThenBy(b=>b.id))if(EnrollSchool(p.id,s.id))break;
            }
        }
        bool ValidEducation()
        {
            foreach(var h in society.families)foreach(var p in h.people??new List<CityResident>())
            {
                if(p.education<0 || p.education>3 || p.schoolId< -1 || p.schoolDepartureDay< -1 || p.schoolDepartureDay>day || p.schoolArrivedDay< -1 || p.schoolArrivedDay>day
                    || float.IsNaN(p.studyMinutes) || float.IsInfinity(p.studyMinutes) || p.studyMinutes<0 || p.studyMinutes>1000000
                    || float.IsNaN(p.schoolMinutesToday) || float.IsInfinity(p.schoolMinutesToday) || p.schoolMinutesToday<0 || p.schoolMinutesToday>1441)return false;
                foreach(float progress in new[]{p.highSchoolStudyMinutes,p.universityStudyMinutes})if(float.IsNaN(progress) || float.IsInfinity(progress) || progress<0 || progress>1000000)return false;
                if(p.schoolId>=0 && (!development.enabled || !h.resident || !SchoolMatches(p) || p.jobId>=0 || GetBuilding(p.schoolId) is UniversityBuilding && p.canWork))return false;
                if(p.atSchool && (p.schoolId<0 || p.location!=p.schoolId || p.tripId!=0 || p.atWork || p.schoolReturning || p.medicalStage!=MedicalStage.None))return false;
                if(p.schoolReturning && (!h.resident || p.atWork || p.atSchool || p.medicalStage!=MedicalStage.None))return false;
                if(p.schoolReturning && p.tripId==0 && GetBuilding(p.location)==null && roads.Node(p.accessNode)==null)return false;
                var trip=traffic.trips.Find(t=>t.id==p.tripId);
                if(trip?.purpose==TripPurpose.School && (!trip.returning && !SchoolMatches(p) || p.atWork || p.atSchool || p.medicalStage!=MedicalStage.None
                    || trip.returning!=p.schoolReturning || !trip.returning && trip.destination!=p.schoolId || trip.returning && trip.destination!=h.home))return false;
                if(p.schoolReturning && p.tripId>0 && trip?.purpose!=TripPurpose.School)return false;
            }
            foreach(var school in buildings.OfType<SchoolBuilding>())if(EnrolledAt(school.id)>school.BaseSeats*3/2)return false;
            return true;
        }
    }
    public sealed partial class CityTraffic
    {
        public TrafficTrip DispatchSchool(int citizen,bool returning=false)
        {
            var h=city.society.families.FirstOrDefault(f=>f.resident && f.people.Any(p=>p.id==citizen));var p=h?.people.Find(r=>r.id==citizen);
            if(!city.development.enabled || p==null || !returning && !city.SchoolMatches(p) || p.tripId!=0 || p.atWork || p.medicalStage!=MedicalStage.None || State.trips.Count>=TaskCapacity || State.nextId==int.MaxValue)return null;
            if(!returning && (p.sick || p.atSchool || p.schoolReturning || !city.HasBasicServices(p.schoolId) || p.location!=h.home))return null;
            if(returning && !p.atSchool && !p.schoolReturning)return null;
            int origin=returning?p.location:h.home,destination=returning?h.home:p.schoolId;
            var route=Endpoint(origin)?FindRoute(origin,destination):Road(p.accessNode)?Search(new List<int>{p.accessNode},Access(destination)):new List<int>();
            if(route.Count==0)return null;
            var trip=new TrafficTrip{id=State.nextId++,origin=Endpoint(origin)?origin:Outside,destination=destination,home=h.home,householdId=h.id,residentId=p.id,
                purpose=TripPurpose.School,walking=true,returning=returning,route=route,departedAt=State.clock};
            State.trips.Add(trip);p.tripId=trip.id;p.atSchool=false;p.schoolReturning=returning;if(!returning)p.schoolDepartureDay=city.day;
            city.Trace("education.trip_started",returning?"儿童从实际位置步行回家":"儿童实际步行上学",trip,household:h.id,citizen:p.id,building:p.schoolId,trip:trip.id);return trip;
        }
        bool ValidateSchoolTrip(TrafficTrip trip)
        {
            var h=TripFamily(trip);var p=h?.people.Find(r=>r.id==trip.residentId);
            if(h==null || !h.resident || p==null)
            {if(p!=null){p.tripId=0;city.ReleaseSchoolPlace(p);p.atSchool=p.schoolReturning=false;}State.trips.Remove(trip);return false;}
            if(!trip.returning && (!city.SchoolMatches(p) || p.sick || !city.HasBasicServices(p.schoolId)))
            {RedirectFromCurrent(trip,h.home);p.schoolReturning=true;}
            if(trip.returning && trip.destination!=h.home)RedirectFromCurrent(trip,h.home);
            trip.home=h.home;return true;
        }
        void ArriveSchool(TrafficTrip trip)
        {
            var h=TripFamily(trip);var p=h?.people.Find(r=>r.id==trip.residentId);
            if(p!=null)
            {p.tripId=0;p.location=trip.destination;p.accessNode=trip.Current;p.atWork=false;p.atSchool=!trip.returning;p.schoolReturning=false;if(!trip.returning)p.schoolArrivedDay=city.day;}
            city.Trace("education.arrived",trip.returning?"儿童实际回家":"儿童实际到校；现在才可以累计学习",trip,household:trip.householdId,citizen:trip.residentId,building:trip.destination,trip:trip.id);
            State.completed++;State.trips.Remove(trip);
        }
        void AdvanceSchools(float minutes)
        {
            if(!city.development.enabled)return;
            float minute=city.ResidentMinute;
            foreach(var h in city.society.families.Where(h=>h.resident))foreach(var p in h.people.Where(p=>city.SchoolEligible(p) || p.atSchool || p.schoolReturning))
            {
                if(city.SchoolMatches(p) && p.atSchool && !p.sick && p.schoolArrivedDay==city.day && city.HasBasicServices(p.schoolId))
                {
                    int grade=((SchoolBuilding)city.GetBuilding(p.schoolId)).Grade;
                    float studied=Math.Max(0,Math.Min(minute+minutes,960)-Math.Max(minute,480));
                    if(grade==1)p.studyMinutes=Math.Min(1000000,p.studyMinutes+studied);
                    else if(grade==2)p.highSchoolStudyMinutes=Math.Min(1000000,p.highSchoolStudyMinutes+studied);
                    else p.universityStudyMinutes=Math.Min(1000000,p.universityStudyMinutes+studied);
                    p.schoolMinutesToday+=studied;
                    if(p.education<grade && CityModel.CourseProgress(p,grade)>=CityModel.CourseMinutes(grade))
                    {p.education=grade;p.skill=Math.Max(p.skill,grade);city.Trace("education.graduated","个人实际学习累计达到"+CityModel.SchoolName(grade)+"课程要求",p,household:h.id,citizen:p.id,building:p.schoolId);
                        if(grade==3)EndSchoolEnrollment(p);}
                }
                if(p.tripId!=0 || p.medicalStage!=MedicalStage.None || State.clock<p.retryAt)continue;
                bool returning=p.schoolReturning || p.atSchool && (p.sick || minute>=960 || p.schoolArrivedDay<city.day || !city.HasBasicServices(p.schoolId));
                if(returning){p.retryAt=State.clock+.25f;DispatchSchool(p.id,true);continue;}
                if(!city.SchoolEligible(p) || p.sick || p.atSchool)continue;
                if(p.schoolId<0)
                {
                    p.retryAt=State.clock+.25f;
                    foreach(var school in city.buildings.OfType<SchoolBuilding>().OrderBy(b=>city.TravelMinutes(h.home,b.id)).ThenBy(b=>b.id))if(city.EnrollSchool(p.id,school.id))break;
                }
                if(p.schoolId<0 || p.schoolDepartureDay==city.day)continue;
                float estimate=city.TravelMinutes(h.home,p.schoolId)*4;
                if(estimate>=0 && minute>=Math.Max(0,480-estimate) && minute<960){p.retryAt=State.clock+.25f;DispatchSchool(p.id);}
            }
        }
    }
}
