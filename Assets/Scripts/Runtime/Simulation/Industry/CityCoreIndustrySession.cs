using System;
using System.Collections.Generic;
using System.Linq;
using HarborCity.RoadCore;

namespace HarborCity
{
    [Serializable] public sealed class CityCoreBusinessData
    {
        public int version=3,money,day,nextBuilding;
        public CityMapState map;
        public CityZoningState zoning;
        public CityDevelopmentState development;
        public double calendarOffset=50;
        public SimulationClockData clock=new SimulationClockData();
        public long nextDispatchTick;
        public bool commuteEnabled=true;
        public int payrollDay;
        public List<BuildingSaveData> buildings;
        public List<BuildingSaveData> retiredBuildings=new List<BuildingSaveData>();
        public HouseholdState society;
        public double retiredFactoryWages;
        public List<IndustrialExposure> residualIndustry;
    }
    // Business boundary, using the original IndustrialBuilding/CommercialBuilding facts.
    public sealed class CityCoreIndustryBusiness
    {
        readonly CityModel city;
        public CityCoreResidents Residents {get;internal set;}
        public CityCoreIndustryBusiness(CityModel city){this.city=city;}
        public bool Exists(int id)=>city.GetBuilding(id)!=null&&!city.GetBuilding(id).abandoned&&!city.GetBuilding(id).burned;
        internal bool Depart(TripTask v)
        {
            if(v.purpose!=TransportPurpose.Freight)return Residents!=null&&Residents.Depart(v);
            if(!(city.GetBuilding(v.origin) is IndustrialBuilding factory)||!(city.GetBuilding(v.destination) is CommercialBuilding shop)||!Exists(v.origin)||!Exists(v.destination)||factory.Stock<v.Cargo||shop.Stock+v.Cargo>32||v.Price!=FactoryState.SalePrice)return false;
            factory.Stock-=v.Cargo;factory.factory.shipped+=v.Cargo;return true;
        }
        internal bool Deliver(TransportArrival v)
        {
            if(v.purpose!=TransportPurpose.Freight)return Residents!=null&&Residents.Arrive(v);
            if(!Exists(v.origin)||!Exists(v.destination))return false;
            var source=city.GetBuilding(v.origin) as IndustrialBuilding;var target=city.GetBuilding(v.destination) as CommercialBuilding;
            if(source==null||target==null||target.Stock+v.cargo>32)return false;
            // Called after physics has committed every vehicle; successful receipt is
            // acknowledged and retired in the same city boundary, never saved mid-step.
            target.Stock+=v.cargo;target.wholesaleCosts+=v.cargo*FactoryState.SalePrice;
            city.RecordFactorySale(v.origin,v.cargo,v.taskId,v.destination,notify:false);return true;
        }
    }
    public sealed class CityCoreIndustrySession
    {
        public CityModel City {get;}
        public World Transport {get;}
        public CityCoreIndustryBusiness Business {get;}
        public CityCoreResidents Residents {get;}
        readonly List<CityBuilding> retired=new List<CityBuilding>();
        public CitySimulation Simulation {get;}
        public CityEntities Entities {get;}
        public CityCoreConstruction Construction {get;}
        public CityCoreIndustrySession(CityModel city,double calendarOffset=50,WorldData transport=null,SimulationClockData clock=null,long nextDispatchTick=0,IEnumerable<CityBuilding> retired=null,bool commuteEnabled=true,int payrollDay=-1)
        {
            City=city;if(retired!=null)this.retired.AddRange(retired);var timeline=new SimulationClock(clock);
            Business=new CityCoreIndustryBusiness(city);Transport=new World(transport??new WorldData{externalBusiness=true},timeline,city.buildings.Where(b=>!b.abandoned&&!b.burned).Select(b=>b.id));Entities=new CityEntities(city,Transport);
            double absolute=calendarOffset+timeline.Seconds;City.day=1+(int)Math.Floor(absolute/City.society.settings.secondsPerDay);City.society.dayElapsed=(float)(absolute%City.society.settings.secondsPerDay);
            Residents=new CityCoreResidents(city,Transport,Entities,commuteEnabled,payrollDay,calendarOffset);Business.Residents=Residents;
            Simulation=new CitySimulation(city,Transport,timeline,Entities,Residents,Business,calendarOffset,nextDispatchTick);
            Entities.Simulation=Simulation;
            Construction=new CityCoreConstruction(city,Entities,Transport);
        }
        public int Advance(double seconds)=>Simulation.Advance(seconds);
        public static CityCoreIndustrySession CreateEmpty()
        {var city=new CityModel();city.development.enabled=true;return new CityCoreIndustrySession(city,calendarOffset:0);}
        public string AccessIssue(int building)
        {var access=Transport.GetAccess(building);return access==null?"缺少道路入口":!access.valid?"入口道路已拆除":(access.use&AccessUse.Freight)==0?"入口不允许货运":"";}
        public void CloseBuilding(int id)
        {
            Entities.StructuralBoundary();
            var building=City.GetBuilding(id);if(building==null)throw new ArgumentException("Missing business building");
            if(building is ResidentialBuilding&&City.society.families.Any(h=>h.resident&&h.home==id))throw new InvalidOperationException("Occupied housing requires an explicit relocation policy; no residents were removed");
            if(building is IndustrialBuilding factory)City.retiredFactoryWages+=factory.factory.wageCosts;
            Entities.RemoveBuilding(id);retired.Add(building);foreach(var member in Entities.Members)if(member.Person.location==id)member.Person.atWork=false;
        }
        public CityCoreBusinessData SnapshotBusiness()=>CityCoreSave.CopyBusiness(new CityCoreBusinessData{map=City.map,zoning=City.zoning,development=City.development,money=City.money,day=City.day,nextBuilding=City.nextBuildingId,calendarOffset=Simulation.CalendarOffset,clock=Simulation.ClockSnapshot,nextDispatchTick=Simulation.Logistics.NextDispatchTick,commuteEnabled=Residents.Enabled,payrollDay=Residents.PayrollDay,society=City.society,buildings=City.buildings.Select(BuildingSaveData.FromBuilding).ToList(),retiredBuildings=retired.Select(BuildingSaveData.FromBuilding).ToList(),retiredFactoryWages=City.retiredFactoryWages,residualIndustry=City.residualIndustry});
        public void Validate()
        {
            if(City.map==null||!City.map.Valid()||City.zoning==null||!City.zoning.Valid()||City.development==null)throw new InvalidOperationException("Invalid map/zoning/development facts");
            Simulation.AssertSaveBoundary();Entities.Validate();Transport.Validate();Transport.AssertSafety();Residents.Validate();if(!City.ValidIndustry())throw new InvalidOperationException("Industrial financial/labour invariants failed");
            foreach(var shop in City.buildings.OfType<CommercialBuilding>())if(shop.Stock<0||shop.Stock>32||shop.wholesaleCosts<0)throw new InvalidOperationException("Commercial inventory invalid");
            foreach(var factory in retired.OfType<IndustrialBuilding>())if(Math.Abs(factory.factory.CashError)>1e-6)throw new InvalidOperationException("Retired account mismatch");
        }
        public static CityCoreIndustrySession Create(bool startAtWork=false)
        {
            var city=new CityModel();city.development.enabled=false;city.nextBuildingId=4;
            var factory=new IndustrialBuilding{id=1,x=-25,z=-7,entranceX=-25,entranceZ=-3};factory.factory.capital=1200;factory.factory.raw=24;factory.factory.imported=24;factory.factory.rawCosts=24*FactoryState.RawPrice;factory.factory.cash=1200-factory.factory.rawCosts;
            city.buildings.Add(factory);city.buildings.Add(new CommercialBuilding{id=2,x=25,z=-7,entranceX=25,entranceZ=-3});city.buildings.Add(new ResidentialBuilding{id=3,x=-30,z=7,entranceX=-30,entranceZ=3,housing=HousingKind.Apartment,housingUnits=1});
            var family=new Household{id=1,home=3,unit=0,resident=true};city.society.families.Add(family);city.society.nextId=2;
            for(int i=1;i<=4;i++)family.people.Add(new CityResident{id=i,householdId=1,name="工人 "+i,age=30,canWork=true,skill=2});city.society.nextCitizenId=5;city.SyncJobs();
            var jobs=city.society.jobEntities.Where(j=>j.buildingId==1).ToList();for(int i=0;i<4;i++){var worker=family.people[i];city.AssignJob(worker,jobs[i].id);worker.atWork=startAtWork;worker.location=startAtWork?1:3;worker.arrivedDay=startAtWork?1:-1;}
            var session=new CityCoreIndustrySession(city,commuteEnabled:!startAtWork);
            int a=session.Transport.AddJunction(new Point(-35,0,0)),b=session.Transport.AddJunction(new Point(0,0,0)),c=session.Transport.AddJunction(new Point(35,0,0));
            int sourceRoad=session.Transport.AddRoad(a,b,new[]{session.Transport.JunctionPosition(a),new Point(-18,0,0),session.Transport.JunctionPosition(b)},lanes:2),goalRoad=session.Transport.AddRoad(b,c,new[]{session.Transport.JunctionPosition(b),new Point(18,0,0),session.Transport.JunctionPosition(c)},lanes:2);
            session.Transport.RegisterAccess(1,Network.LaneId(sourceRoad,false),10,AccessUse.All);session.Transport.RegisterAccess(2,Network.LaneId(goalRoad,false),25,AccessUse.All);session.Transport.RegisterAccess(3,Network.LaneId(sourceRoad,false),5,AccessUse.All);return session;
        }
    }
}
