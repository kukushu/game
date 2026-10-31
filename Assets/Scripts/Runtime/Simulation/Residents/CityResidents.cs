using System;
using System.Collections.Generic;

namespace HarborCity
{
    // Explicit saved facts may use guarded setters without changing their wire name/type.
    [AttributeUsage(AttributeTargets.Property)] internal sealed class CitySavedFactAttribute : Attribute {}
    internal enum CityResidentGuard { Trip, Retry, Visit }
    internal enum CityResidentStateChange { Ability, Sickness, Death, Location, Attendance, Home, Residence, Membership, HousingAvailability, TripDay, CommuteObservation }
    [Serializable] public sealed class CityResident
    {
        public int id, age;
        public double ageProgress;
        public int lastAgeDay;
        public string name;
        bool capable,deceased,ill;int currentLocation=-1;
        [CitySavedFact] public bool canWork {get=>capable;set{if(capable==value)return;capable=value;EntityOwner?.StateChanged(this,CityResidentStateChange.Ability);}}
        [CitySavedFact] public bool dead {get=>deceased;set{if(deceased==value)return;deceased=value;EntityOwner?.StateChanged(this,CityResidentStateChange.Death);}}
        [CitySavedFact] public bool sick {get=>ill;set{if(ill==value)return;ill=value;EntityOwner?.StateChanged(this,CityResidentStateChange.Sickness);}}
        [CitySavedFact] public int location {get=>currentLocation;set{if(currentLocation==value)return;currentLocation=value;EntityOwner?.StateChanged(this,CityResidentStateChange.Location);}}
        public int householdId, skill, wagePaid;
        int departedDay=-1,reachedDay=-1,seenHome=-1,seenWork=-1,seenRevision=-1;float commute=-1;
        [CitySavedFact] public int departureDay {get=>departedDay;set{if(departedDay==value)return;departedDay=value;EntityOwner?.StateChanged(this,CityResidentStateChange.TripDay);}}
        [CitySavedFact] public int arrivedDay {get=>reachedDay;set{if(reachedDay==value)return;reachedDay=value;EntityOwner?.StateChanged(this,CityResidentStateChange.TripDay);}}
        [CitySavedFact] public int observedHome {get=>seenHome;set{if(seenHome==value)return;seenHome=value;EntityOwner?.StateChanged(this,CityResidentStateChange.CommuteObservation);}}
        [CitySavedFact] public int observedWork {get=>seenWork;set{if(seenWork==value)return;seenWork=value;EntityOwner?.StateChanged(this,CityResidentStateChange.CommuteObservation);}}
        [CitySavedFact] public int observedRevision {get=>seenRevision;set{if(seenRevision==value)return;seenRevision=value;EntityOwner?.StateChanged(this,CityResidentStateChange.CommuteObservation);}}
        // Preserve float bits and operation order. Only invalidate derived plans;
        // arrival is still settled by the original transaction before evaluation.
        [CitySavedFact] public float lastCommute {get=>commute;set{commute=value;EntityOwner?.StateChanged(this,CityResidentStateChange.CommuteObservation);}}
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
        public MedicalStage medicalStage;
        public int medicalClinicId=-1;
        public float treatmentMinutes;
        public int education,schoolId=-1,schoolDepartureDay=-1,schoolArrivedDay=-1;
        public bool atSchool,schoolReturning;
        public float studyMinutes,schoolMinutesToday,highSchoolStudyMinutes,universityStudyMinutes;
        public double wageCredit;
        public int carriedGoods;
        public int wagesPaid;
        public float earnedWages, workedMinutes, lastDelay, requestedAt=-1;
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
