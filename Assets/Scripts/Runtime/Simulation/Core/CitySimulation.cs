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
        public CityCoreTrade Trade {get;}
        public CityGameDesign Design {get;}
        public CityCorePayroll Payroll {get;}
        public CityCoreConsumption Consumption {get;}
        public CityCoreFinance Finance {get;}
        public CityCoreWaste Waste {get;}
        public SimulationClockData ClockSnapshot=>clock.Snapshot();
        public long Tick=>clock.Tick;public double Seconds=>clock.Seconds;
        public double CalendarOffset {get;}
        public bool IsStepping {get;private set;}
        public bool IsApplyingBoundary {get;private set;}
        public Exception Fault {get;private set;}
#if UNITY_5_3_OR_NEWER
        static readonly ProfilerMarker wasteSample=new ProfilerMarker("City.Simulation.Waste"),wasteGenerationSample=new ProfilerMarker("City.Simulation.WasteGenerationBoundary");
        static readonly ProfilerMarker stepSample=new ProfilerMarker("City.Simulation.Step"),growthSample=new ProfilerMarker("City.Simulation.GrowthBoundary"),utilitySample=new ProfilerMarker("City.Simulation.UtilitiesBoundary"),populationSample=new ProfilerMarker("City.Simulation.PopulationBoundary"),requestsSample=new ProfilerMarker("City.Simulation.ResidentRequests"),productionSample=new ProfilerMarker("City.Simulation.Production"),consumptionSample=new ProfilerMarker("City.Simulation.Consumption"),payrollSample=new ProfilerMarker("City.Simulation.PayrollBoundary"),financeSample=new ProfilerMarker("City.Simulation.FinanceBoundary"),financeAccrualSample=new ProfilerMarker("City.Simulation.FinanceAccrual"),logisticsSample=new ProfilerMarker("City.Simulation.Logistics"),tradeSample=new ProfilerMarker("City.Simulation.ExternalTrade"),settleSample=new ProfilerMarker("City.Simulation.Settlement");
#endif
        internal CitySimulation(CityModel city,World transport,SimulationClock clock,CityEntities entities,CityCoreResidents residents,CityCoreIndustryBusiness business,double offset,long nextDispatch,CityGameDesign design=null,int payrollDay=-1,TreasuryData treasury=null,WasteServiceData waste=null)
        {if(double.IsNaN(offset)||double.IsInfinity(offset)||offset<0||city.society.settings.secondsPerDay<1)throw new ArgumentException("Invalid calendar");Design=design??new CityGameDesign(new GameDesignData());this.city=city;this.transport=transport;this.clock=clock;this.residents=residents;this.business=business;CalendarOffset=offset;bindings=new CityCoreRoadBindings(city,transport);Payroll=new CityCorePayroll(city,entities,Design.Employment,payrollDay);production=new CityProductionSystem(city,entities,Payroll,Design);Logistics=new CityLogisticsSystem(entities,transport,this,nextDispatch);Growth=new CityCoreGrowth(city,entities,transport,Design.Production,Design.Retail);Utilities=new CityCoreUtilities(city,entities,transport);Finance=new CityCoreFinance(city,entities,transport,Design.Finance,treasury);Waste=new CityCoreWaste(city,entities,transport,Design.Waste,waste);Utilities.Finance=Finance;Population=new CityCorePopulation(city,entities,transport,Utilities);Consumption=new CityCoreConsumption(city,entities,transport,this);residents.Consumption=Consumption;residents.RequestCommute=RequestCommute;SyncCalendar(clock.Seconds);Trade=new CityCoreTrade(city,entities,transport,this);Utilities.Refresh();}
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
#if CITY_CORE_ATTRIBUTION
                    using(CityCoreProfile.Measure(CityCoreProfile.Part.Growth))
#endif
                    Growth.Step(clock.Context);
#if UNITY_5_3_OR_NEWER
                    using(utilitySample.Auto())
#endif
#if CITY_CORE_ATTRIBUTION
                    using(CityCoreProfile.Measure(CityCoreProfile.Part.Utilities))
#endif
                    Utilities.Refresh();
#if UNITY_5_3_OR_NEWER
                    using(populationSample.Auto())
#endif
#if CITY_CORE_ATTRIBUTION
                    using(CityCoreProfile.Measure(CityCoreProfile.Part.Population))
#endif
                    if(Growth.Zoning.Ready)Population.Step(clock.Context);IsApplyingBoundary=false;IsStepping=true;Step(clock.Context);clock.Commit(true);SyncCalendar(clock.Seconds);
#if UNITY_5_3_OR_NEWER
                    using(wasteGenerationSample.Auto())
#endif
                    Waste.GenerateDay();
#if UNITY_5_3_OR_NEWER
                    using(payrollSample.Auto())
#endif
#if CITY_CORE_ATTRIBUTION
                    using(CityCoreProfile.Measure(CityCoreProfile.Part.Payroll))
#endif
                    Payroll.SettleDay();
#if UNITY_5_3_OR_NEWER
                    using(financeSample.Auto())
#endif
#if CITY_CORE_ATTRIBUTION
                    using(CityCoreProfile.Measure(CityCoreProfile.Part.Finance))
#endif
                    Finance.SettleDay();steps++;}catch(Exception e){Fault=e;throw;}finally{IsStepping=false;IsApplyingBoundary=false;}}
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
                using(financeAccrualSample.Auto())
#endif
                Finance.Accrue(time);
#if UNITY_5_3_OR_NEWER
                using(productionSample.Auto())
#endif
                production.Step(time);
#if UNITY_5_3_OR_NEWER
                using(consumptionSample.Auto())
#endif
#if CITY_CORE_ATTRIBUTION
                    using(CityCoreProfile.Measure(CityCoreProfile.Part.Consumption))
#endif
                    Consumption.Step(time);
#if UNITY_5_3_OR_NEWER
                using(logisticsSample.Auto())
#endif
                Logistics.Step(time);
#if UNITY_5_3_OR_NEWER
                using(tradeSample.Auto())
#endif
#if CITY_CORE_ATTRIBUTION
                    using(CityCoreProfile.Measure(CityCoreProfile.Part.Trade))
#endif
                    Trade.Step(time);
#if UNITY_5_3_OR_NEWER
                using(wasteSample.Auto())
#endif
                Waste.Step(time);
                transport.MoveStep(time);
#if UNITY_5_3_OR_NEWER
                using(settleSample.Auto())
#endif
                {
#if CITY_CORE_ATTRIBUTION
                    using var settlementObservation=CityCoreProfile.Measure(CityCoreProfile.Part.Settlement);
#endif
                    foreach(var arrival in transport.Arrivals){transport.CheckArrival(arrival);if(arrival.purpose==TransportPurpose.Service)Waste.Arrive(arrival);else if(business.Deliver(arrival))transport.AcknowledgeArrival(arrival.id);}transport.RetireCompleted();}
            }
        }
        public Vehicle RequestFreight(int origin,int destination,int cargo=1,double? price=null)
        =>Dispatch(origin,destination,cargo,price??Design.Economy.GoodsWholesalePrice,TransportPurpose.Freight,0);
        public Vehicle RequestMaterial(int origin,int destination,int amount,CargoKind kind,double price)
        =>Dispatch(origin,destination,amount,price,TransportPurpose.Freight,0,kind);
        public Vehicle RequestCommute(int citizen,int origin,int destination,bool returning=false)
        =>Dispatch(origin,destination,returning?residents.CarriedGoods(citizen):0,0,returning?TransportPurpose.Home:TransportPurpose.Work,citizen);
        public Vehicle RequestShopping(int citizen,int origin,int destination)
        =>Dispatch(origin,destination,0,0,TransportPurpose.Shopping,citizen);
        Vehicle Dispatch(int origin,int destination,int cargo,double price,TransportPurpose purpose,int citizen,CargoKind kind=CargoKind.Goods)
        {
#if CITY_CORE_ATTRIBUTION
            using var observation=HarborCity.RoadCore.CityCoreProfile.Measure(HarborCity.RoadCore.CityCoreProfile.Part.Dispatch);
#endif
if(Fault!=null)throw new InvalidOperationException("Cannot dispatch into a faulted simulation",Fault);var admission=transport.PrepareTransport(origin,destination,cargo,price,purpose,citizen);if(admission==null)return null;transport.CheckAdmission(admission);if(admission.vehicle.Task.Shipment!=null)admission.vehicle.Task.Shipment.kind=kind;if(!business.Depart(admission.vehicle.Task))return null;return transport.CommitAdmission(admission);}
        internal void AssertSaveBoundary(){if(IsStepping||IsApplyingBoundary||Fault!=null)throw new InvalidOperationException("Cannot save a partial/faulted simulation step");}
    }
}
