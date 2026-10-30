using System;
using HarborCity.RoadCore;
namespace HarborCity
{
    public sealed class CityProductionSystem
    {
        readonly CityModel city;readonly CityEntities entities;
        readonly CityCorePayroll payroll;readonly CityGameDesign design;
        internal CityProductionSystem(CityModel city,CityEntities entities,CityCorePayroll payroll,CityGameDesign design){this.city=city;this.entities=entities;this.payroll=payroll;this.design=design;}
        internal void Step(TimeContext time)
        {
#if CITY_CORE_ATTRIBUTION
            using var observation=HarborCity.RoadCore.CityCoreProfile.Measure(HarborCity.RoadCore.CityCoreProfile.Part.Production);
#endif

            payroll.BeginStep();float minute=city.ResidentMinute,minutes=(float)(time.Duration*1440/city.society.settings.secondsPerDay);
            // Diagnostic callbacks are allowed to observe/mutate fields synchronously.
            // Preserve their existing dynamic Members walk instead of enumerating a
            // set that a callback could change. Normal play has no such callback.
            if(city.logSink!=null){foreach(var member in entities.Members)Produce(member,minute,minutes);}
            else using(var active=entities.WorkingMembers())while(active.MoveNext())Produce(active.Current,minute,minutes);
            city.AdvanceIndustry(minutes);
        }
        void Produce(CityMember member,float minute,float minutes)
        {
                var worker=member.Person;if(!member.Family.resident||!city.CanAttendWork(worker)||!worker.atWork||worker.tripId!=0||worker.arrivedDay!=city.day||city.Workplace(worker)!=worker.location||city.ResidentJob(worker)==null)return;
                float shift=design.Employment.ShiftStart(worker.id,city.development.enabled&&city.GetBuilding(worker.location) is CommercialBuilding),worked=Math.Max(0,Math.Min(minute+minutes,shift+design.Employment.ShiftMinutes)-Math.Max(minute,shift));double credit=worker.wageCredit;worker.workedMinutes+=worked;float paid;
                if(city.development.enabled)paid=payroll.PayAttendance(worker,worked);
                else{city.PayAttendance(worker,worked);int wage=city.ResidentWage(worker);paid=city.Factory(worker.location)==null?worked:wage==0?0:(float)((worker.wageCredit-credit)*480/wage);}
                city.ProcessFactoryWork(worker,Math.Min(worked,paid),worked,city.development.enabled?design.Production:null);
        }
    }
}
