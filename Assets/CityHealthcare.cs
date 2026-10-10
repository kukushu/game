using System;
using System.Linq;
using System.Collections.Generic;

namespace HarborCity
{
    public enum MedicalStage { None,Assigned,Treatment,Returning }
    [Serializable] public sealed class ClinicBuilding : CityBuilding
    {
        public override LandUse Use=>LandUse.Clinic;
        // Scaled first implementation; original capacities/costs/speed unverified.
        public const int PatientCapacity=8,Ambulances=2;
    }
    public sealed partial class CityModel
    {
        public int ClinicCapacity=>Math.Max(1,ServiceCapacity(CityServiceKind.Healthcare,ClinicBuilding.PatientCapacity));
        public int ClinicFleetLimit=>Math.Max(1,ServiceCapacity(CityServiceKind.Healthcare,ClinicBuilding.Ambulances));
        public int ClinicReservations(int id)=>Citizens.Count(p=>p.medicalClinicId==id && p.sick && p.medicalStage!=MedicalStage.Returning);
        public int ClinicPatients(int id)=>Citizens.Count(p=>p.medicalClinicId==id && p.medicalStage==MedicalStage.Treatment);
        public int ClinicAmbulances(int id)=>traffic.trips.Count(t=>t.purpose==TripPurpose.Healthcare && t.clinicId==id && !t.walking);
        bool ValidHealthcare()
        {
            foreach(var h in society.families)foreach(var p in h.people??new List<CityResident>())
            {
                if((int)p.medicalStage<0 || (int)p.medicalStage>3 || float.IsNaN(p.treatmentMinutes) || float.IsInfinity(p.treatmentMinutes) || p.treatmentMinutes<0 || p.treatmentMinutes>1000000)return false;
                if(p.medicalStage==MedicalStage.None)
                {if(p.medicalClinicId!=-1 || p.treatmentMinutes!=0 || traffic.trips.Any(t=>t.id==p.tripId && t.purpose==TripPurpose.Healthcare))return false;continue;}
                if(!development.enabled || !h.resident || p.atWork || !(GetBuilding(p.medicalClinicId) is ClinicBuilding))return false;
                if(p.medicalStage==MedicalStage.Assigned && (!p.sick || p.tripId==0))return false;
                if(p.medicalStage==MedicalStage.Treatment && (!p.sick || p.tripId!=0 || p.location!=p.medicalClinicId))return false;
                if(p.tripId>0 && !traffic.trips.Any(t=>t.id==p.tripId && t.purpose==TripPurpose.Healthcare && t.clinicId==p.medicalClinicId && t.returning==(p.medicalStage==MedicalStage.Returning)))return false;
            }
            // Lower budgets limit NEW reservations/dispatches. Existing patients
            // and ambulances keep their state instead of disappearing on a slider.
            foreach(var clinic in buildings.OfType<ClinicBuilding>())if(ClinicReservations(clinic.id)>ClinicBuilding.PatientCapacity*3/2 || ClinicAmbulances(clinic.id)>ClinicBuilding.Ambulances*3/2)return false;
            return true;
        }
    }
    public sealed partial class CityTraffic
    {
        public TrafficTrip DispatchHealthcare(int citizen)
        {
            var h=city.society.families.FirstOrDefault(f=>f.resident && f.people.Any(p=>p.id==citizen));var person=h?.people.Find(p=>p.id==citizen);
            if(!city.development.enabled || person==null || !person.sick || person.atWork || person.tripId!=0 || person.medicalStage!=MedicalStage.None || person.location!=h.home || State.trips.Count>=TaskCapacity || State.nextId==int.MaxValue)return null;
            foreach(var clinic in city.buildings.OfType<ClinicBuilding>().Where(b=>city.HasBasicServices(b.id) && city.ClinicReservations(b.id)<city.ClinicCapacity)
                .OrderBy(b=>city.TravelMinutes(h.home,b.id)).ThenBy(b=>b.id))
            {
                var walk=FindRoute(h.home,clinic.id);if(walk.Count==0)continue;
                var pickup=FindRoute(clinic.id,h.home);
                bool ambulance=city.ClinicAmbulances(clinic.id)<city.ClinicFleetLimit && pickup.Count>0 && CanEnter(pickup,null);
                var trip=new TrafficTrip{id=State.nextId++,origin=ambulance?clinic.id:h.home,destination=ambulance?h.home:clinic.id,
                    home=h.home,residentId=citizen,householdId=h.id,purpose=TripPurpose.Healthcare,clinicId=clinic.id,medicalPickup=ambulance,walking=!ambulance,
                    route=ambulance?pickup:walk,departedAt=State.clock};
                State.trips.Add(trip);person.tripId=trip.id;person.medicalClinicId=clinic.id;person.medicalStage=MedicalStage.Assigned;person.treatmentMinutes=0;
                city.Trace("healthcare.dispatched",ambulance?"救护车从诊所实际出发接人；居民仍在家":"暂无可发救护车，居民实际步行就诊",trip,household:h.id,citizen:citizen,building:clinic.id,trip:trip.id);return trip;
            }
            return null;
        }
        bool ValidateHealthcareTrip(TrafficTrip trip)
        {
            var h=TripFamily(trip);var person=h?.people.Find(p=>p.id==trip.residentId);
            if(h==null || !h.resident || person==null)
            {if(person!=null){person.tripId=0;person.medicalStage=MedicalStage.None;person.medicalClinicId=-1;person.treatmentMinutes=0;}State.trips.Remove(trip);return false;}
            int destination=trip.returning || trip.medicalPickup?h.home:trip.clinicId;
            if(trip.destination!=destination)
            {bool returning=trip.returning;RedirectFromCurrent(trip,destination);trip.returning=returning;}
            trip.home=h.home;return true;
        }
        void ArriveHealthcare(TrafficTrip trip)
        {
            var h=TripFamily(trip);var person=h?.people.Find(p=>p.id==trip.residentId);if(person==null){State.trips.Remove(trip);return;}
            if(trip.medicalPickup)
            {
                person.location=h.home;person.accessNode=trip.Current;trip.medicalPickup=false;
                RedirectFromCurrent(trip,trip.clinicId);trip.returning=false;
                city.Trace("healthcare.picked_up","救护车实际到达住处接到居民，开始送往诊所",trip,household:h.id,citizen:person.id,building:h.home,trip:trip.id);return;
            }
            person.tripId=0;person.location=trip.destination;person.accessNode=trip.Current;person.atWork=false;
            if(trip.returning)
            {person.medicalStage=MedicalStage.None;person.medicalClinicId=-1;person.treatmentMinutes=0;}
            else person.medicalStage=MedicalStage.Treatment;
            city.Trace(trip.returning?"healthcare.home":"healthcare.admitted",trip.returning?"居民就医后实际回家":"居民实际到达诊所，开始占用治疗位置",trip,household:h.id,citizen:person.id,building:trip.destination,trip:trip.id);
            State.completed++;State.trips.Remove(trip);
        }
        void AdvanceHealthcare(float minutes)
        {
            if(!city.development.enabled)return;
            foreach(var h in city.society.families.Where(h=>h.resident))foreach(var p in h.people.Where(p=>!p.dead))
            {
                if(p.medicalStage==MedicalStage.Treatment && city.HasBasicServices(p.medicalClinicId))
                {
                    p.treatmentMinutes+=minutes;p.health=Math.Min(100,p.health+minutes*.12f);
                    if(p.health>=80)
                    {p.sick=false;p.medicalStage=MedicalStage.Returning;city.Trace("healthcare.treated","实际治疗后恢复；等待步行返家，尚未到家",p,household:h.id,citizen:p.id,building:p.medicalClinicId);}
                }
                if(p.tripId!=0 || p.atWork || State.clock<p.retryAt)continue;
                if(p.medicalStage==MedicalStage.Returning)
                {
                    p.retryAt=State.clock+.25f;var route=FindRoute(p.location,h.home);if(route.Count==0 || State.trips.Count>=TaskCapacity || State.nextId==int.MaxValue)continue;
                    var trip=new TrafficTrip{id=State.nextId++,origin=p.location,destination=h.home,home=h.home,householdId=h.id,residentId=p.id,
                        purpose=TripPurpose.Healthcare,clinicId=p.medicalClinicId,walking=true,returning=true,route=route,departedAt=State.clock};
                    State.trips.Add(trip);p.tripId=trip.id;
                    city.Trace("healthcare.returning","居民从治疗地点实际步行返家",trip,household:h.id,citizen:p.id,trip:trip.id);
                }
                else if(p.medicalStage==MedicalStage.None && p.sick && p.location==h.home)
                {p.retryAt=State.clock+.25f;DispatchHealthcare(p.id);}
            }
        }
    }
}
