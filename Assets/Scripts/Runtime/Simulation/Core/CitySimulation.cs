using System;
using HarborCity.RoadCore;
#if UNITY_5_3_OR_NEWER
using Unity.Profiling;
#endif
namespace HarborCity
{
    public sealed class CitySimulation
    {
        readonly CityModel city;readonly World transport;readonly SimulationClock clock;
        readonly CityCoreResidents residents;readonly CityCoreIndustryBusiness business;readonly CityCoreRoadBindings bindings;
        readonly CityProductionSystem production;
        public CityLogisticsSystem Logistics {get;}
        public CityCoreGrowth Growth {get;}
        public CityCoreUtilities Utilities {get;}
        public CityCorePopulation Population {get;}
        public SimulationClockData ClockSnapshot=>clock.Snapshot();
        public long Tick=>clock.Tick;public double Seconds=>clock.Seconds;
        public double CalendarOffset {get;}
        public bool IsStepping {get;private set;}
        public bool IsApplyingBoundary {get;private set;}
        public Exception Fault {get;private set;}
#if UNITY_5_3_OR_NEWER
        static readonly ProfilerMarker stepSample=new ProfilerMarker("City.Simulation.Step"),growthSample=new ProfilerMarker("City.Simulation.GrowthBoundary"),utilitySample=new ProfilerMarker("City.Simulation.UtilitiesBoundary"),populationSample=new ProfilerMarker("City.Simulation.PopulationBoundary"),requestsSample=new ProfilerMarker("City.Simulation.ResidentRequests"),productionSample=new ProfilerMarker("City.Simulation.Production"),logisticsSample=new ProfilerMarker("City.Simulation.Logistics"),settleSample=new ProfilerMarker("City.Simulation.Settlement");
#endif
        internal CitySimulation(CityModel city,World transport,SimulationClock clock,CityEntities entities,CityCoreResidents residents,CityCoreIndustryBusiness business,double offset,long nextDispatch)
        {if(double.IsNaN(offset)||double.IsInfinity(offset)||offset<0||city.society.settings.secondsPerDay<1)throw new ArgumentException("Invalid calendar");this.city=city;this.transport=transport;this.clock=clock;this.residents=residents;this.business=business;CalendarOffset=offset;bindings=new CityCoreRoadBindings(city,transport);production=new CityProductionSystem(city,entities);Logistics=new CityLogisticsSystem(entities,transport,this,nextDispatch);Growth=new CityCoreGrowth(city,entities,transport);Utilities=new CityCoreUtilities(city,entities,transport);Population=new CityCorePopulation(city,entities,transport,Utilities);residents.RequestCommute=RequestCommute;SyncCalendar(clock.Seconds);Utilities.Refresh();}
        void SyncCalendar(double seconds)
        {double absolute=CalendarOffset+seconds,daySeconds=city.society.settings.secondsPerDay;city.day=1+(int)Math.Floor(absolute/daySeconds);city.society.dayElapsed=(float)(absolute%daySeconds);}
        public int Advance(double seconds)
        {
            if(Fault!=null)throw new InvalidOperationException("Simulation faulted; reload a validated save",Fault);if(IsStepping||IsApplyingBoundary)throw new InvalidOperationException("Reentrant simulation advance");clock.Request(seconds);int steps=0;
            while(clock.HasStep)
            {
#if CITY_CORE_ATTRIBUTION
                using var observation=CityCoreProfile.Measure(CityCoreProfile.Part.Step);
#endif
#if UNITY_5_3_OR_NEWER
                using(stepSample.Auto())
#endif
                {try{IsApplyingBoundary=true;SyncCalendar(clock.Seconds);bindings.RefreshIfChanged();
#if UNITY_5_3_OR_NEWER
                    using(growthSample.Auto())
#endif
                    Growth.Step(clock.Context);
#if UNITY_5_3_OR_NEWER
                    using(utilitySample.Auto())
#endif
                    Utilities.Refresh();
#if UNITY_5_3_OR_NEWER
                    using(populationSample.Auto())
#endif
                    if(Growth.Zoning.Ready)Population.Step(clock.Context);IsApplyingBoundary=false;IsStepping=true;Step(clock.Context);clock.Commit(true);SyncCalendar(clock.Seconds);residents.SettlePayroll();steps++;}catch(Exception e){Fault=e;throw;}finally{IsStepping=false;IsApplyingBoundary=false;}}
            }
            return steps;
        }
        void Step(TimeContext time)
        {
            {
                SyncCalendar(time.Start);bindings.RefreshIfChanged();
#if UNITY_5_3_OR_NEWER
                using(requestsSample.Auto())
#endif
                residents.AdvanceRequests();
#if UNITY_5_3_OR_NEWER
                using(productionSample.Auto())
#endif
                production.Step(time);
#if UNITY_5_3_OR_NEWER
                using(logisticsSample.Auto())
#endif
                Logistics.Step(time);
                transport.MoveStep(time);
#if UNITY_5_3_OR_NEWER
                using(settleSample.Auto())
#endif
                {
#if CITY_CORE_ATTRIBUTION
                    using var settlementObservation=CityCoreProfile.Measure(CityCoreProfile.Part.Settlement);
#endif
                    foreach(var arrival in transport.Arrivals)if(business.Deliver(arrival))transport.AcknowledgeArrival(arrival.id);transport.RetireCompleted();}
            }
        }
        public Vehicle RequestFreight(int origin,int destination,int cargo=1,double price=FactoryState.SalePrice)
        =>Dispatch(origin,destination,cargo,price,TransportPurpose.Freight,0);
        public Vehicle RequestCommute(int citizen,int origin,int destination,bool returning=false)
        =>Dispatch(origin,destination,0,0,returning?TransportPurpose.Home:TransportPurpose.Work,citizen);
        Vehicle Dispatch(int origin,int destination,int cargo,double price,TransportPurpose purpose,int citizen)
        {
#if CITY_CORE_ATTRIBUTION
            using var observation=HarborCity.RoadCore.CityCoreProfile.Measure(HarborCity.RoadCore.CityCoreProfile.Part.Dispatch);
#endif
if(Fault!=null)throw new InvalidOperationException("Cannot dispatch into a faulted simulation",Fault);var admission=transport.PrepareTransport(origin,destination,cargo,price,purpose,citizen);if(admission==null||!business.Depart(admission.vehicle.Task))return null;return transport.CommitAdmission(admission);}
        internal void AssertSaveBoundary(){if(IsStepping||IsApplyingBoundary||Fault!=null)throw new InvalidOperationException("Cannot save a partial/faulted simulation step");}
    }
}
