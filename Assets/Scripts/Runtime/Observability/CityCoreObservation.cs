using System;
using System.Collections.Generic;
using HarborCity.RoadCore;
namespace HarborCity
{
    [Flags] public enum CityObservedProblem {None=0,Road=1,Electricity=2,Water=4,Sewage=8,Garbage=16,Abandoned=32}
    public sealed class ResidentInspection
    {
        public int id,family,age,job,workplace,wage,location,vehicle,task,origin,destination,carriedGoods;
        public bool workforce,atWork,stranded;public double actualCommute;public TransportPurpose purpose;public string name;
    }
    public sealed class FamilyInspection
    {
        public int id,unit,savings,employed,workforce,commuteSamples;public double averageCommute,longestCommute;
        public IReadOnlyList<ResidentInspection> members;
    }
    public sealed class BuildingInspection
    {
        public int id,units,occupied,residents,workforce,employed,jobs,filledJobs,attending,raw,goods,garbage,stored,transportCount,strandedCount;
        public double x,z,cash,workedMinutes;public LandUse use;public CityObservedProblem problems;public long tick;
        public IReadOnlyList<FamilyInspection> families;
    }
    public readonly struct BuildingIssue
    {
        public readonly int id;public readonly CityObservedProblem problems;
        public BuildingIssue(int id,CityObservedProblem problems){this.id=id;this.problems=problems;}
    }
    // Cold, read-only projections; no subscriptions, simulation writes, or saved UI state.
    // Building detail O(P+J+T), explicit problem refresh O(B); never called per vehicle.
    public sealed class CityCoreObservation
    {
        readonly CityCoreIndustrySession session;
        public CityCoreObservation(CityCoreIndustrySession session){this.session=session??throw new ArgumentNullException(nameof(session));}
        CityObservedProblem Problems(CityBuilding b)
        {
            var mask=CityObservedProblem.None;var access=session.Transport.GetAccess(b.id);if(access==null||!access.valid)mask|=CityObservedProblem.Road;
            var supply=session.City.Supply(b.id);if(session.City.development.enabled){if(!supply.electricity)mask|=CityObservedProblem.Electricity;if(!supply.water)mask|=CityObservedProblem.Water;if(!supply.sewage)mask|=CityObservedProblem.Sewage;}
            if(b.garbage>=CityModel.GarbageWarning)mask|=CityObservedProblem.Garbage;if(b.abandoned||b.burned)mask|=CityObservedProblem.Abandoned;return mask;
        }
        public IReadOnlyList<BuildingIssue> Issues()
        {var result=new List<BuildingIssue>();foreach(var b in session.Entities.Buildings){var mask=Problems(b);if(mask!=0)result.Add(new BuildingIssue(b.id,mask));}return result.AsReadOnly();}
        public int Pick(float x,float z)
        {foreach(var b in session.Entities.Buildings)if(b.Contains(x,z))return b.id;return 0;}
        public BuildingInspection Read(int id)
        {
            var b=session.Entities.Building(id);if(b==null)return null;
            var result=new BuildingInspection{id=id,use=b.Use,x=b.x,z=b.z,garbage=b.garbage,problems=Problems(b),tick=session.Simulation.Tick};
            var families=new List<FamilyInspection>();
            foreach(var h in session.City.society.families)if(h.resident&&h.home==id)
            {
                var family=new FamilyInspection{id=h.id,unit=h.unit,savings=h.savings};var members=new List<ResidentInspection>();double sum=0;
                foreach(var p in h.people)if(!p.dead)
                {
                    var job=session.City.ResidentJob(p);var car=p.tripId==0?null:session.Transport.FindTaskVehicle(p.tripId);
                    var person=new ResidentInspection{id=p.id,family=h.id,age=p.age,name=p.name,job=job?.id??-1,workplace=job?.buildingId??-1,wage=job?.wage??0,workforce=p.canWork,atWork=p.atWork,location=p.location,carriedGoods=p.carriedGoods,actualCommute=-1,task=p.tripId};
                    if(p.canWork){family.workforce++;if(job!=null)family.employed++;}
                    if(job!=null&&p.lastCommute>=0&&p.observedHome==h.home&&p.observedWork==job.buildingId){person.actualCommute=p.lastCommute;sum+=p.lastCommute;family.longestCommute=Math.Max(family.longestCommute,p.lastCommute);family.commuteSamples++;}
                    if(car!=null){person.vehicle=car.id;person.origin=car.Task.origin;person.destination=car.Task.destination;person.purpose=car.Task.purpose;person.stranded=car.status==VehicleStatus.Stranded;}
                    members.Add(person);
                }
                family.averageCommute=family.commuteSamples==0?0:sum/family.commuteSamples;family.members=members.AsReadOnly();families.Add(family);result.residents+=members.Count;result.workforce+=family.workforce;result.employed+=family.employed;
            }
            result.families=families.AsReadOnly();result.occupied=families.Count;if(b is ResidentialBuilding home)result.units=home.housingUnits;
            foreach(var job in session.City.society.jobEntities)if(job.buildingId==id){result.jobs++;if(job.occupiedCitizenId>=0)result.filledJobs++;}
            foreach(var m in session.Entities.Members){var p=m.Person;if(!p.dead&&p.atWork&&p.location==id&&p.tripId==0){result.attending++;result.workedMinutes+=p.workedMinutes;}}
            foreach(var task in session.Transport.Tasks)if(task.origin==id||task.destination==id){result.transportCount++;var v=session.Transport.FindTaskVehicle(task.id);if(v?.status==VehicleStatus.Stranded)result.strandedCount++;}
            if(b is IndustrialBuilding factory){result.raw=factory.factory.raw;result.goods=factory.Stock;result.cash=factory.factory.cash;}
            if(b is CommercialBuilding shop){result.goods=shop.Stock;result.cash=shop.Account.Cash;}
            if(b is LandfillBuilding plant)result.stored=plant.stored;
            return result;
        }
    }
}
