using System;
using System.Collections.Generic;

namespace HarborCity
{
    // Explicit saved facts may use guarded setters without changing their wire name/type.
    [AttributeUsage(AttributeTargets.Property)] internal sealed class CitySavedFactAttribute : Attribute {}
    internal enum CityResidentGuard { Trip, Retry, Visit }
    [Serializable] public sealed class CityResident
    {
        public int id, age;
        public double ageProgress;
        public int lastAgeDay;
        public string name;
        public bool canWork,dead;
        public int householdId, skill, wagePaid;
        public int departureDay=-1, arrivedDay=-1, location=-1, observedHome=-1, observedWork=-1, observedRevision=-1;
        bool working;
        internal CityEntities EntityOwner;
        int trip,assignedJob=-1;float retry;double visit;
        [CitySavedFact] public int jobId {get=>assignedJob;set{assignedJob=value;EntityOwner?.ResidentJobChanged(this);}}
        // Store first, invalidate only. Never evaluate requests inside a partial
        // departure/arrival transaction. Floating writes retain their exact bits.
        [CitySavedFact] public int tripId {get=>trip;set{trip=value;EntityOwner?.RequestGuardChanged(this,CityResidentGuard.Trip);}}
        [CitySavedFact] public float retryAt {get=>retry;set{retry=value;EntityOwner?.RequestGuardChanged(this,CityResidentGuard.Retry);}}
        [CitySavedFact] public double visitUntil {get=>visit;set{visit=value;EntityOwner?.RequestGuardChanged(this,CityResidentGuard.Visit);}}
        [CitySavedFact] public bool atWork
        {
            get=>working;
            set {if(working==value)return;working=value;EntityOwner?.WorkAttendanceChanged(this);}
        }
        public float health=100;
        public bool sick;
        public MedicalStage medicalStage;
        public int medicalClinicId=-1;
        public float treatmentMinutes;
        public int education,schoolId=-1,schoolDepartureDay=-1,schoolArrivedDay=-1;
        public bool atSchool,schoolReturning;
        public float studyMinutes,schoolMinutesToday,highSchoolStudyMinutes,universityStudyMinutes;
        public double wageCredit;
        public int carriedGoods;
        public int wagesPaid;
        public float earnedWages, workedMinutes, lastCommute=-1, lastDelay, requestedAt=-1;
        public int accessNode=-1;
    }
    public sealed class ResidentActivity
    {
        public string state, reason;
        public int building=-1, destination=-1;
        public float x,z, progress;
        public bool located, travelling;
        public List<RoadNode> route=new List<RoadNode>();
    }
    public sealed partial class CityModel
    {
        public float ResidentMinute => society.dayElapsed/society.settings.secondsPerDay*1440;
    }
}
