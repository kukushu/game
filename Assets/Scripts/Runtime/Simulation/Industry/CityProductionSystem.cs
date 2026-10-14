using System;
using HarborCity.RoadCore;
namespace HarborCity
{
    public sealed class CityProductionSystem
    {
        readonly CityModel city;readonly CityEntities entities;
        internal CityProductionSystem(CityModel city,CityEntities entities){this.city=city;this.entities=entities;}
        internal void Step(TimeContext time)
        {
#if CITY_CORE_ATTRIBUTION
            using var observation=HarborCity.RoadCore.CityCoreProfile.Measure(HarborCity.RoadCore.CityCoreProfile.Part.Production);
#endif

            float minute=city.ResidentMinute,minutes=(float)(time.Duration*1440/city.society.settings.secondsPerDay);
            foreach(var member in entities.Members)
            {
                var worker=member.Person;if(!member.Family.resident||!(city.GetBuilding(worker.location) is IndustrialBuilding)||!city.CanAttendWork(worker)||!worker.atWork||worker.tripId!=0||worker.arrivedDay!=city.day||city.Workplace(worker)!=worker.location||city.ResidentJob(worker)==null)continue;
                float shift=480+worker.id%3*30,worked=Math.Max(0,Math.Min(minute+minutes,shift+480)-Math.Max(minute,shift));double credit=worker.factoryWageCredit;worker.workedMinutes+=worked;city.PayAttendance(worker,worked);int wage=city.ResidentWage(worker);float paid=wage==0?0:(float)((worker.factoryWageCredit-credit)*480/wage);city.ProcessFactoryWork(worker,Math.Min(worked,paid),worked);
            }
            city.AdvanceIndustry(minutes);
        }
    }
}
