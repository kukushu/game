using System;
using System.Collections.Generic;
using HarborCity.RoadCore;

namespace HarborCity
{
    // Original citizens, household membership and job assignments remain the facts.
    // This component schedules/settles their real RoadCore vehicles, never old Move.
    public sealed class CityCoreResidents
    {
        sealed class Member {public Household family;public CityResident person;}
        readonly CityModel city;readonly World transport;
        readonly double calendarOffset;
        readonly Dictionary<int,Member> members=new Dictionary<int,Member>();
        public bool Enabled {get;}
        public int PayrollDay {get;private set;}
        public CityCoreResidents(CityModel city,World transport,bool enabled=true,int payrollDay=-1,double calendarOffset=50)
        {
            this.city=city;this.transport=transport;this.calendarOffset=calendarOffset;Enabled=enabled;PayrollDay=payrollDay<0?city.day:payrollDay;
            foreach(var household in city.society.families)foreach(var person in household.people)members.Add(person.id,new Member{family=household,person=person});
        }
        public bool Depart(Vehicle v)
        {
            Member member;if(!members.TryGetValue(v.citizenId,out member))return false;var p=member.person;var h=member.family;
            if(!h.resident||p.dead||p.tripId!=0||p.location!=v.origin||v.cargo!=0||v.price!=0)return false;
            if(v.purpose==TransportPurpose.Work&&(p.atWork||v.origin!=h.home||v.destination!=city.Workplace(p)||!city.CanAttendWork(p)||!city.JobFunded(city.ResidentJob(p))))return false;
            if(v.purpose==TransportPurpose.Home&&v.destination!=h.home)return false;
            if(v.purpose!=TransportPurpose.Home&&v.purpose!=TransportPurpose.Work)return false;
            p.tripId=v.id;p.location=-1;p.atWork=false;if(v.purpose==TransportPurpose.Work)p.departureDay=city.day;return true;
        }
        public bool Arrive(Vehicle v)
        {
            Member member;if(!members.TryGetValue(v.citizenId,out member))return false;var p=member.person;var h=member.family;
            if(p.dead||!h.resident||p.tripId!=v.id)return false;
            p.tripId=0;p.location=v.destination;p.atWork=v.purpose==TransportPurpose.Work&&city.Workplace(p)==v.destination&&city.CanAttendWork(p)&&city.JobFunded(city.ResidentJob(p));
            if(p.atWork){double seconds=city.society.settings.secondsPerDay,absolute=calendarOffset+transport.Clock+World.StepSeconds;p.arrivedDay=1+(int)Math.Floor(absolute/seconds);p.lastCommute=(float)Math.Max(0,(transport.Clock+World.StepSeconds-v.departedAt)*1440/seconds);p.lastDelay=(float)Math.Max(0,(p.arrivedDay-p.departureDay)*1440+absolute%seconds*1440/seconds-(480+p.id%3*30));p.observedHome=h.home;p.observedWork=v.destination;p.observedRevision=transport.NetworkGeneration;}
            return true;
        }
        public void AdvanceRequests()
        {
            if(!Enabled)return;
            if(city.day>PayrollDay){foreach(var family in city.society.families)family.wagePaid=0;foreach(var member in members.Values){var p=member.person;int paid=city.TransferFactoryPay(p);p.wagePaid=paid+(int)Math.Round(p.earnedWages);member.family.savings+=p.wagePaid;member.family.wagePaid+=p.wagePaid;p.earnedWages=0;p.workedMinutes=0;}PayrollDay=city.day;}
            float minute=city.ResidentMinute;
            foreach(var member in members.Values)
            {
                var p=member.person;var h=member.family;if(!h.resident||p.dead||p.tripId!=0||transport.Clock<p.retryAt)continue;float shift=480+p.id%3*30,end=shift+480;
                bool returning=p.location!=h.home&&(!p.atWork||p.sick||city.Workplace(p)!=p.location||p.arrivedDay<city.day||minute>=end||!city.JobFunded(city.ResidentJob(p)));
                bool leaving=!p.atWork&&p.location==h.home&&p.departureDay!=city.day&&city.CanAttendWork(p)&&city.JobFunded(city.ResidentJob(p))&&minute>=Math.Max(0,shift-Math.Max(0,p.lastCommute))&&minute<end;
                if(!returning&&!leaving)continue;p.retryAt=(float)(transport.Clock+.25);transport.DispatchResident(p.id,returning?p.location:h.home,returning?h.home:city.Workplace(p),returning);
            }
        }
        public ResidentActivity Observe(int citizen)
        {
            Member member;if(!members.TryGetValue(citizen,out member))throw new ArgumentException("Unknown citizen");var p=member.person;var a=new ResidentActivity();
            if(p.tripId!=0){var task=transport.FindVehicle(p.tripId);if(task==null)throw new InvalidOperationException("Resident transport reference missing");var point=transport.Position(task);a.located=true;a.travelling=true;a.x=(float)point.x;a.z=(float)point.z;a.destination=task.destination;a.state=task.status==VehicleStatus.Stranded?"道路中断，仍在车辆中":task.purpose==TransportPurpose.Home?"回家途中":"上班途中";a.reason="实际车辆 #"+task.id;return a;}
            var building=city.GetBuilding(p.location);a.building=p.location;a.located=building!=null;a.state=p.atWork?"实际在岗":"在家 / 等待出发";if(building!=null){a.x=building.x;a.z=building.z;}return a;
        }
        public void Validate()
        {
            if(PayrollDay<1||PayrollDay>city.day)throw new InvalidOperationException("Invalid payroll settlement day");
            var owned=new HashSet<int>();foreach(var task in transport.Vehicles)if(task.purpose!=TransportPurpose.Freight){Member member;if(!members.TryGetValue(task.citizenId,out member)||!owned.Add(task.citizenId)||member.person.tripId!=task.id||member.person.atWork||member.person.location!=-1)throw new InvalidOperationException("Resident/vehicle ownership mismatch");}
            foreach(var member in members.Values){var p=member.person;if(p.tripId!=0){var task=transport.FindVehicle(p.tripId);if(task==null||task.citizenId!=p.id)throw new InvalidOperationException("Dangling resident trip");}}
        }
    }
}
