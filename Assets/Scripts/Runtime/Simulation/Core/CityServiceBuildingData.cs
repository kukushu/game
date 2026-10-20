using System;
namespace HarborCity
{
    // Building catalogue and resident/save metadata only. No service dispatcher
    // or second simulation is implemented in these data declarations.
    public enum MedicalStage { None,Assigned,Treatment,Returning }
    [Serializable] public sealed class ClinicBuilding : CityBuilding
    {
        public override LandUse Use=>LandUse.Clinic;
        // Scaled first implementation; original capacities/costs/speed unverified.
        public const int PatientCapacity=8,Ambulances=2;
    }
    [Serializable] public sealed class FireHouseBuilding : CityBuilding
    {
        public override LandUse Use=>LandUse.FireHouse;
        public const int Engines=2;
    }
    [Serializable] public sealed class PoliceStationBuilding : CityBuilding
    {
        public override LandUse Use=>LandUse.PoliceStation;
        public const int Cars=2;
    }
    public sealed partial class CityModel
    {
        public const float CrimeWarning=25;
    }
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
}
