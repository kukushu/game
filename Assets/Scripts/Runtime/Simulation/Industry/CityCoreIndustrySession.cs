using System;
using System.Collections.Generic;
using System.Linq;
using HarborCity.RoadCore;

namespace HarborCity
{
    [Serializable] public sealed class CityCoreEndpoint {public int building,junction;}
    [Serializable] public sealed class CityCoreBusinessData
    {
        public int version=1,money,day,nextBuilding;
        public double calendarOffset=50,dispatchAt;
        public List<BuildingSaveData> buildings;
        public List<BuildingSaveData> retiredBuildings=new List<BuildingSaveData>();
        public HouseholdState society;
        public List<CityCoreEndpoint> endpoints=new List<CityCoreEndpoint>();
        public List<int> receipts=new List<int>();
        public double retiredFactoryWages;
        public List<IndustrialExposure> residualIndustry;
    }
    // Business boundary, using the original IndustrialBuilding/CommercialBuilding facts.
    public sealed class CityCoreIndustryBusiness:ITransportBusiness
    {
        readonly CityModel city;
        readonly Dictionary<int,int> endpoints=new Dictionary<int,int>();
        readonly HashSet<int> receipts=new HashSet<int>();
        public CityCoreIndustryBusiness(CityModel city,IEnumerable<CityCoreEndpoint> entrances,IEnumerable<int> completed=null)
        {this.city=city;foreach(var access in entrances)endpoints.Add(access.building,access.junction);if(completed!=null)foreach(int id in completed)if(!receipts.Add(id))throw new ArgumentException("Duplicate receipt");}
        public IEnumerable<int> Receipts {get {foreach(int id in receipts)yield return id;}}
        public bool Exists(int id)=>endpoints.ContainsKey(id)&&city.GetBuilding(id)!=null&&!city.GetBuilding(id).abandoned&&!city.GetBuilding(id).burned;
        public int Junction(int id)=>endpoints[id];
        public bool Depart(Vehicle v)
        {
            if(!(city.GetBuilding(v.origin) is IndustrialBuilding factory)||!(city.GetBuilding(v.destination) is CommercialBuilding shop)||!Exists(v.origin)||!Exists(v.destination)||factory.Stock<v.cargo||shop.Stock+v.cargo>32||v.price!=FactoryState.SalePrice)return false;
            factory.Stock-=v.cargo;factory.factory.shipped+=v.cargo;return true;
        }
        public bool Deliver(Vehicle v)
        {
            if(receipts.Contains(v.id))return true;
            if(!Exists(v.origin)||!Exists(v.destination))return false;
            var source=city.GetBuilding(v.origin) as IndustrialBuilding;var target=city.GetBuilding(v.destination) as CommercialBuilding;
            if(source==null||target==null||target.Stock+v.cargo>32)return false;
            // All prerequisites are checked before committing. No log/observer can throw
            // between stock receipt and financial receipt. The saved ID guards retries.
            target.Stock+=v.cargo;target.wholesaleCosts+=v.cargo*FactoryState.SalePrice;
            city.RecordFactorySale(v.origin,v.cargo,v.id,v.destination,notify:false);receipts.Add(v.id);return true;
        }
    }
    public sealed class CityCoreIndustrySession
    {
        public CityModel City {get;}
        public World Transport {get;}
        public CityCoreIndustryBusiness Business {get;}
        readonly List<CityCoreEndpoint> endpoints;
        readonly List<CityBuilding> retired=new List<CityBuilding>();
        readonly Action before,after;
        readonly double calendarOffset;
        double dispatchAt;
        public CityCoreIndustrySession(CityModel city,List<CityCoreEndpoint> endpoints,double calendarOffset=50,WorldData transport=null,IEnumerable<int> receipts=null,double dispatchAt=0,IEnumerable<CityBuilding> retired=null)
        {
            City=city;this.endpoints=endpoints.Select(e=>new CityCoreEndpoint{building=e.building,junction=e.junction}).ToList();this.calendarOffset=calendarOffset;this.dispatchAt=dispatchAt;
            if(retired!=null)this.retired.AddRange(retired);
            Business=new CityCoreIndustryBusiness(city,this.endpoints,receipts);Transport=transport==null?new World(Business):new World(transport,Business);
            before=BeforeStep;after=SyncCalendar;SyncCalendar();
        }
        public int Advance(double seconds)=>Transport.Advance(seconds,before,after);
        void SyncCalendar()
        {
            double absolute=calendarOffset+Transport.Clock,seconds=City.society.settings.secondsPerDay;
            City.day=1+(int)Math.Floor(absolute/seconds);City.society.dayElapsed=(float)(absolute%seconds);
        }
        void BeforeStep()
        {
            SyncCalendar();float minute=City.ResidentMinute,minutes=(float)(World.StepSeconds*1440/City.society.settings.secondsPerDay);
            foreach(var household in City.society.families)
            {
                if(!household.resident)continue;
                foreach(var worker in household.people)
                {
                    if(!(City.GetBuilding(worker.location) is IndustrialBuilding)||!City.CanAttendWork(worker)||!worker.atWork||worker.tripId!=0||worker.arrivedDay!=City.day||City.Workplace(worker)!=worker.location||City.ResidentJob(worker)==null)continue;
                    float shift=480+worker.id%3*30,worked=Math.Max(0,Math.Min(minute+minutes,shift+480)-Math.Max(minute,shift));
                    double credit=worker.factoryWageCredit;worker.workedMinutes+=worked;City.PayAttendance(worker,worked);
                    float paid=City.ResidentWage(worker)==0?0:(float)((worker.factoryWageCredit-credit)*480/City.ResidentWage(worker));
                    City.ProcessFactoryWork(worker,Math.Min(worked,paid),worked);
                }
            }
            City.AdvanceIndustry(minutes);
            if(Transport.Clock+1e-9<dispatchAt)return;dispatchAt=Transport.Clock+.25;
            var incoming=new Dictionary<int,int>();foreach(var task in Transport.Vehicles){int count;incoming.TryGetValue(task.destination,out count);incoming[task.destination]=count+task.cargo;}
            foreach(var factory in City.buildings.OfType<IndustrialBuilding>())foreach(var shop in City.buildings.OfType<CommercialBuilding>())
            {
                int reserved;incoming.TryGetValue(shop.id,out reserved);int amount=Math.Min(8,Math.Min(factory.Stock,32-shop.Stock-reserved));
                if(amount<=0)continue;var task=Transport.Dispatch(factory.id,shop.id,amount,FactoryState.SalePrice);if(task!=null)incoming[shop.id]=reserved+amount;
            }
        }
        public void CloseBuilding(int id)
        {
            var building=City.GetBuilding(id);if(building==null)throw new ArgumentException("Missing business building");
            if(building is IndustrialBuilding factory)City.retiredFactoryWages+=factory.factory.wageCosts;
            City.buildings.Remove(building);retired.Add(building);City.SyncJobs();Transport.RefreshEndpoints();
        }
        public CityCoreBusinessData SnapshotBusiness()=>CityCoreSave.CopyBusiness(new CityCoreBusinessData{money=City.money,day=City.day,nextBuilding=City.nextBuildingId,calendarOffset=calendarOffset,dispatchAt=dispatchAt,society=City.society,buildings=City.buildings.Select(BuildingSaveData.FromBuilding).ToList(),retiredBuildings=retired.Select(BuildingSaveData.FromBuilding).ToList(),endpoints=endpoints.Select(e=>new CityCoreEndpoint{building=e.building,junction=e.junction}).ToList(),receipts=Business.Receipts.OrderBy(id=>id).ToList(),retiredFactoryWages=City.retiredFactoryWages,residualIndustry=City.residualIndustry});
        public void Validate()
        {
            Transport.Validate();Transport.AssertSafety();if(!City.ValidIndustry())throw new InvalidOperationException("Industrial financial/labour invariants failed");
            var receipts=new HashSet<int>(Business.Receipts);foreach(var task in Transport.Vehicles)if(task.cargo>0&&receipts.Contains(task.id))throw new InvalidOperationException("Settled receipt still has in-transit cargo");
            foreach(var shop in City.buildings.OfType<CommercialBuilding>())if(shop.Stock<0||shop.Stock>32||shop.wholesaleCosts<0)throw new InvalidOperationException("Commercial inventory invalid");
            foreach(var factory in retired.OfType<IndustrialBuilding>())if(Math.Abs(factory.factory.CashError)>1e-6)throw new InvalidOperationException("Retired account mismatch");
        }
        public static CityCoreIndustrySession Create()
        {
            var city=new CityModel();city.development.enabled=false;city.nextBuildingId=4;
            var factory=new IndustrialBuilding{id=1,x=-35,z=-7,entranceX=-35,entranceZ=0};factory.factory.capital=1200;factory.factory.raw=24;factory.factory.imported=24;factory.factory.rawCosts=24*FactoryState.RawPrice;factory.factory.cash=1200-factory.factory.rawCosts;
            city.buildings.Add(factory);city.buildings.Add(new CommercialBuilding{id=2,x=35,z=-7,entranceX=35,entranceZ=0});city.buildings.Add(new ResidentialBuilding{id=3,x=-30,z=7,housing=HousingKind.Apartment,housingUnits=1});
            var family=new Household{id=1,home=3,unit=0,resident=true};city.society.families.Add(family);city.society.nextId=2;
            for(int i=1;i<=4;i++)family.people.Add(new CityResident{id=i,householdId=1,name="工人 "+i,age=30,canWork=true,skill=2});city.society.nextCitizenId=5;city.SyncJobs();
            var jobs=city.society.jobEntities.Where(j=>j.buildingId==1).ToList();for(int i=0;i<4;i++){var worker=family.people[i];city.AssignJob(worker,jobs[i].id);worker.atWork=true;worker.location=1;worker.arrivedDay=1;}
            var addresses=new List<CityCoreEndpoint>();var session=new CityCoreIndustrySession(city,addresses);
            // Bind after constructing the network; no legacy RoadNode/Edge is created.
            int a=session.Transport.AddJunction(new Point(-35,0,0)),b=session.Transport.AddJunction(new Point(0,0,0)),c=session.Transport.AddJunction(new Point(35,0,0));
            session.Transport.AddRoad(a,b,new[]{session.Transport.JunctionPosition(a),new Point(-18,0,0),session.Transport.JunctionPosition(b)});session.Transport.AddRoad(b,c,new[]{session.Transport.JunctionPosition(b),new Point(18,0,0),session.Transport.JunctionPosition(c)});
            var state=session.Transport.Snapshot();return new CityCoreIndustrySession(city,new List<CityCoreEndpoint>{new CityCoreEndpoint{building=1,junction=a},new CityCoreEndpoint{building=2,junction=c}},50,state);
        }
    }
}
