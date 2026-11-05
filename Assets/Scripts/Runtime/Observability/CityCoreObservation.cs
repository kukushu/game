using System;
using System.Collections.Generic;
using HarborCity.RoadCore;
namespace HarborCity
{
    [Flags] public enum CityObservedProblem {None=0,Road=1,Electricity=2,Water=4,Sewage=8,Garbage=16,Abandoned=32}
    [Serializable] public sealed class ResidentInspection
    {
        public int id,family,age,job,workplace,wage,location,vehicle,task,origin,destination,carriedGoods;
        public bool workforce,atWork,stranded;public double actualCommute;public TransportPurpose purpose;public string name;
    }
    [Serializable] public sealed class FamilyInspection
    {
        public GoodsAmounts products;public double foodDays,dailyDays,shoppingMinutes;public string goodsProblem;
        public int id,unit,savings,employed,workforce,commuteSamples;public double averageCommute,longestCommute;
        [NonSerialized] public IReadOnlyList<ResidentInspection> members;
        public bool stockKnown;public int needFood,needDaily,healthyAdults,availableAdults,workingAdults,travellingAdults,supplier,shoppingMember;
    }
    [Serializable] public sealed class BuildingInspection
    {
        public GoodsAmounts products;public ProductKind product;public double externalCapital;
        public int id,units,occupied,residents,workforce,employed,jobs,filledJobs,attending,raw,goods,garbage,stored,transportCount,strandedCount;
        public double x,z,cash,workedMinutes;public LandUse use;public CityObservedProblem problems;public long tick;
        [NonSerialized] public IReadOnlyList<FamilyInspection> families;
        public double capital,revenue,wages,purchases,processing,taxes;
        public int paidWorkers=-1,rawIncoming;public GoodsAmounts incoming;
        public string businessReason;
    }
    public readonly struct BuildingIssue
    {
        public readonly int id;public readonly CityObservedProblem problems;
        public BuildingIssue(int id,CityObservedProblem problems){this.id=id;this.problems=problems;}
    }
    // Read-only projections with optional session-local shopping observations.
    // Detail reads selected-home members and indexed staff plus jobs/tasks; explicit Issues is O(B).
    public sealed class CityCoreObservation : IDisposable
    {
        readonly CityCoreIndustrySession session;readonly long boundTick;
        readonly List<Household> homeFamilies=new List<Household>();readonly List<CityMember> staff=new List<CityMember>();
        public bool PayrollKnown=>session.Simulation.Tick>boundTick;
        public CityCoreObservation(CityCoreIndustrySession session){this.session=session??throw new ArgumentNullException(nameof(session));boundTick=session.Simulation.Tick;}
        CityShoppingTime shoppingTime;
        internal void ObserveShoppingTime()=>shoppingTime??=new CityShoppingTime(session);
        public void Dispose(){shoppingTime?.Dispose();shoppingTime=null;}
        internal CityObservedProblem Problems(CityBuilding b)
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
            var result=new BuildingInspection{externalCapital=b.externalCapital,products=(b as IGoodsBuilding)?.Products??default,product=(b as IndustrialBuilding)?.factory.product??ProductKind.Food,id=id,use=b.Use,x=b.x,z=b.z,garbage=b.garbage,problems=Problems(b),tick=session.Simulation.Tick};
            var families=new List<FamilyInspection>();
            session.Entities.CollectHomeFamilies(id,homeFamilies);
            foreach(var h in homeFamilies)if(h.resident)
            {
                bool known=h.goodsInitialized&&h.consumptionPeople>0;
                var family=new FamilyInspection{id=h.id,unit=h.unit,savings=h.savings,products=h.goods,stockKnown=known,foodDays=known?session.Simulation.Consumption.DaysAvailable(h,ProductKind.Food):0,dailyDays=known?session.Simulation.Consumption.DaysAvailable(h,ProductKind.DailyGoods):0,needFood=session.Simulation.Consumption.Need(h,ProductKind.Food),needDaily=session.Simulation.Consumption.Need(h,ProductKind.DailyGoods),supplier=h.goodsSupplier,shoppingMember=h.shoppingMember,shoppingMinutes=shoppingTime?.TodayMinutes(h.id)??0};var members=new List<ResidentInspection>();double sum=0;
                foreach(var p in h.people)if(!p.dead)
                {
                    var job=session.City.ResidentJob(p);var car=p.tripId==0?null:session.Transport.FindTaskVehicle(p.tripId);
                    var person=new ResidentInspection{id=p.id,family=h.id,age=p.age,name=p.name,job=job?.id??-1,workplace=job?.buildingId??-1,wage=job?.wage??0,workforce=p.canWork,atWork=p.atWork,location=p.location,carriedGoods=p.carriedGoods,actualCommute=-1,task=p.tripId};
                    if(p.canWork){family.workforce++;if(job!=null)family.employed++;}
                    if(p.age>=18&&!p.sick){family.healthyAdults++;if(p.atWork)family.workingAdults++;if(p.tripId!=0)family.travellingAdults++;if(!p.atWork&&p.tripId==0&&p.location==h.home&&p.carriedGoods==0&&session.Transport.Clock>=p.retryAt)family.availableAdults++;}
                    if(job!=null&&p.lastCommute>=0&&p.observedHome==h.home&&p.observedWork==job.buildingId){person.actualCommute=p.lastCommute;sum+=p.lastCommute;family.longestCommute=Math.Max(family.longestCommute,p.lastCommute);family.commuteSamples++;}
                    if(car!=null){person.vehicle=car.id;person.origin=car.Task.origin;person.destination=car.Task.destination;person.purpose=car.Task.purpose;person.stranded=car.status==VehicleStatus.Stranded||car.status==VehicleStatus.AwaitingRestart;}
                    members.Add(person);
                }
                family.averageCommute=family.commuteSamples==0?0:sum/family.commuteSamples;family.members=members.AsReadOnly();family.goodsProblem=FamilyReason(h,family);families.Add(family);result.residents+=members.Count;result.workforce+=family.workforce;result.employed+=family.employed;
            }
            result.families=families.AsReadOnly();result.occupied=families.Count;if(b is ResidentialBuilding home)result.units=home.housingUnits;
            foreach(var job in session.City.society.jobEntities)if(job.buildingId==id){result.jobs++;if(job.occupiedCitizenId>=0)result.filledJobs++;}
            session.Entities.CollectEmployees(id,staff);foreach(var m in staff){var p=m.Person;if(!p.dead&&p.atWork&&p.location==id&&p.tripId==0){result.attending++;result.workedMinutes+=p.workedMinutes;}}
            foreach(var task in session.Transport.Tasks)if(task.origin==id||task.destination==id){result.transportCount++;var v=session.Transport.FindTaskVehicle(task.id);if(v?.status==VehicleStatus.Stranded||v?.status==VehicleStatus.AwaitingRestart)result.strandedCount++;}
            if(b is IndustrialBuilding factory){var a=factory.factory;result.raw=a.raw;result.goods=factory.Stock;result.cash=a.cash;result.capital=a.capital;result.revenue=a.salesRevenue;result.wages=a.wageCosts;result.purchases=a.rawCosts;result.processing=a.productionCosts;result.taxes=a.taxCosts;result.rawIncoming=session.Simulation.Shipments.Raw(id);}
            if(b is CommercialBuilding shop){var a=shop.Account;result.goods=shop.Stock;result.cash=a.Cash;result.capital=a.Capital;result.revenue=a.RetailRevenue;result.wages=a.WageCosts;result.purchases=a.WholesaleFunded;result.taxes=a.TaxCosts;result.incoming=session.Simulation.Shipments.Goods(id);}
            if(b is IndustrialBuilding||b is CommercialBuilding){result.paidWorkers=PayrollKnown?session.Simulation.Payroll.WorkersAt(id):-1;result.businessReason=BusinessReason(b,result);}
            if(b is LandfillBuilding plant)result.stored=plant.stored;
            return result;
        }
        string BusinessReason(CityBuilding b,BuildingInspection data)
        {
            if(b.abandoned||b.burned)return "ui.feedback.reason.retired";
            if((data.problems&CityObservedProblem.Road)!=0)return "ui.feedback.reason.road";
            if((data.problems&(CityObservedProblem.Electricity|CityObservedProblem.Water|CityObservedProblem.Sewage))!=0)return "ui.feedback.reason.supply";
            if(data.cash<=0&&session.City.development.enabled)return "ui.feedback.reason.cash";
            if(!PayrollKnown)return "ui.feedback.reason.pending";
            if(data.paidWorkers==0)return "ui.feedback.reason.staff";
            if(b is IndustrialBuilding){if(data.goods>=FactoryState.GoodsCapacity)return "ui.feedback.reason.full";if(data.raw==0)return data.rawIncoming>0?"ui.feedback.reason.raw_incoming":"ui.feedback.reason.raw";}
            return "ui.feedback.reason.operating";
        }
        string FamilyReason(Household h,FamilyInspection data)
        {
            if(!data.stockKnown)return "ui.feedback.reason.preparing";
            if(data.needFood+data.needDaily==0)return "ui.feedback.reason.enough";
            if(h.shoppingMember!=0)
            {
                var p=session.Entities.Member(h.shoppingMember)?.Person;var car=p==null?null:session.Transport.FindTaskVehicle(p.tripId);
                if(car!=null&&(car.status==VehicleStatus.Stranded||car.status==VehicleStatus.AwaitingRestart))return "ui.feedback.reason.transport";
                if(car?.Task.purpose==TransportPurpose.Home&&car.Task.Shipment?.Products.Total>0)return "ui.feedback.reason.returning";
                return p!=null&&p.carriedGoods>0?"ui.feedback.reason.purchased":"ui.feedback.reason.shopping";
            }
            var kind=data.needFood>0?ProductKind.Food:ProductKind.DailyGoods;
            if(h.savings<session.Simulation.Design.Products[kind].RetailPrice)return "ui.feedback.reason.money";
            if(data.availableAdults==0)return "ui.feedback.reason.member";
            if(!session.City.BuildingAccess(h.home))return "ui.feedback.reason.road";
            if(!(session.Entities.Building(h.goodsSupplier) is CommercialBuilding shop))return "ui.feedback.reason.unassigned";
            if(!session.City.BuildingAccess(shop.id))return "ui.feedback.reason.road";
            if(!session.City.Supply(shop.id).Complete)return "ui.feedback.reason.supply";
            if(shop.Account.Cash<=0)return "ui.feedback.reason.supplier_cash";
            if(!PayrollKnown)return "ui.feedback.reason.pending";
            if(session.Simulation.Payroll.WorkersAt(shop.id)==0)return "ui.feedback.reason.supplier_staff";
            if(shop.Products[kind]<=0)return "ui.feedback.reason.supplier_stock";
            return "ui.feedback.reason.waiting";
        }
    }

    // One interval per household. Split at city midnight and count only time
    // observed in this session, including unfinished trips and visits.
    sealed class CityShoppingTime : IDisposable
    {
        sealed class Entry
        {
            internal int Shopper,Day;
            internal double StartedAt,CompletedSeconds;
        }
        readonly CityCoreIndustrySession session;
        readonly Dictionary<int,Entry> entries=new Dictionary<int,Entry>();
        double DaySeconds=>session.City.society.settings.secondsPerDay;
        double Offset=>session.Simulation.CalendarOffset;

        internal CityShoppingTime(CityCoreIndustrySession session)
        {
            this.session=session;
            foreach(var family in session.City.society.families)
                if(family.shoppingMember!=0)Begin(family.id,family.shoppingMember,session.Transport.Clock);
            session.Transport.TaskCommitted+=Departed;
            session.Simulation.Consumption.ShoppingReturned+=Finished;
            session.Entities.MemberStructureChanged+=MemberChanged;
        }
        void Begin(int family,int shopper,double time)
        {
            if(!entries.TryGetValue(family,out var entry))entries.Add(family,entry=new Entry());
            entry.Shopper=shopper;
            entry.StartedAt=time;
        }
        void Departed(TripTask task)
        {
            if(task.purpose!=TransportPurpose.Shopping)return;
            var member=session.Entities.Member(task.citizenId);
            if(member!=null)Begin(member.Family.id,task.citizenId,task.departedAt);
        }
        void MemberChanged(CityMember member,bool added)
        {
            if(!added&&entries.TryGetValue(member.Family.id,out var entry)&&entry.Shopper==member.Person.id)
                Finished(member.Family.id,session.Transport.Clock);
        }
        int DayAt(double seconds)=>1+(int)Math.Floor((Offset+seconds)/DaySeconds);
        double SecondsToday(double startedAt,double now,int day)
            =>Math.Max(0,now-Math.Max(startedAt,(day-1)*DaySeconds-Offset));
        void Finished(int family,double time)
        {
            if(!entries.TryGetValue(family,out var entry)||entry.Shopper==0)return;
            int day=DayAt(time);
            if(entry.Day!=day){entry.Day=day;entry.CompletedSeconds=0;}
            entry.CompletedSeconds+=SecondsToday(entry.StartedAt,time,day);
            entry.Shopper=0;
        }
        internal double TodayMinutes(int family)
        {
            if(!entries.TryGetValue(family,out var entry))return 0;
            double now=session.Transport.Clock;
            int day=DayAt(now);
            double seconds=entry.Day==day?entry.CompletedSeconds:0;
            if(entry.Shopper!=0)seconds+=SecondsToday(entry.StartedAt,now,day);
            return seconds*1440/DaySeconds;
        }
        public void Dispose()
        {
            session.Transport.TaskCommitted-=Departed;
            session.Simulation.Consumption.ShoppingReturned-=Finished;
            session.Entities.MemberStructureChanged-=MemberChanged;
        }
    }

}
