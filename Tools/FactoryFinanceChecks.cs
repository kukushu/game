using System;
using System.Linq;
using System.Web.Script.Serialization;
using HarborCity;

public static class FactoryFinanceChecks
{
    static int checks;
    static void Check(bool value,string reason) {if(!value) throw new Exception("Factory finance: "+reason); checks++;}
    static CityModel Create(out CityTraffic sim,out CityResident worker,out int id)
    {
        var c=CityModel.CreateLegacySample(); sim=new CityTraffic(c); c.EnableRoads((x,z)=>1); c.EnableBuildings(); c.EnableHouseholds(); c.EnableResidentTransport();
        foreach(var h in c.society.families) {h.nextReview=10000; foreach(var p in h.people) c.ReleaseJob(p);}
        worker=c.Citizens.First(p=>p.canWork); worker.skill=2;
        var job=c.society.jobEntities.First(j=>c.tiles[j.buildingId]==4); id=job.buildingId; c.AssignJob(worker,job.id);
        c.society.settings.applicantsPerDay=0; c.traffic.dispatchTimer=-10000;
        return c;
    }
    static void Cash(FactoryState f,double cash) {f.cash=cash; f.capital=cash-f.salesRevenue+f.rawCosts+f.wageCosts+f.productionCosts;}
    static void AtWork(CityModel c,CityResident p,int id)
    {
        p.atWork=true; p.location=id; p.arrivedDay=p.departureDay=c.day;
        c.society.dayElapsed=(480+p.id%3*30)/1440f*c.society.settings.secondsPerDay;
    }
    static void Deliver(CityTraffic sim,TrafficTrip trip)
    {for(int i=0;i<2500 && trip.cargo>0;i++) sim.Advance(.05f);}
    static void Ledger(CityModel c)
    {
        Check(c.buildings.Where(b=>b.factory!=null).All(b=>Math.Abs(b.factory.CashError)<.0001),"Every factory cash reconciles with capital, sales and actual costs");
        Check(Math.Abs(c.buildings.Where(b=>b.factory!=null).Sum(b=>b.factory.wageCosts)-c.society.families.SelectMany(h=>h.people).Sum(p=>p.factoryWageCredit+p.totalFactoryWagesPaid))<.0001,"Employer payroll equals resident credits plus transferred savings");
    }
    // JavaScriptSerializer emits doubles with fewer than round-trip digits. Compare
    // financial continuation to a micro-unit while keeping IDs, inventory and dates exact.
    static object Normalise(object value)
    {
        if(value is double) return Math.Round((double)value,6);
        if(value is decimal) return Math.Round((decimal)value,6);
        if(value is System.Collections.Generic.Dictionary<string,object>)
            return ((System.Collections.Generic.Dictionary<string,object>)value).ToDictionary(p=>p.Key,p=>Normalise(p.Value));
        if(value is System.Collections.ArrayList) return ((System.Collections.ArrayList)value).Cast<object>().Select(Normalise).ToArray();
        if(value is object[]) return ((object[])value).Select(Normalise).ToArray();
        return value;
    }
    static string Snapshot(JavaScriptSerializer json,object value) => json.Serialize(Normalise(json.DeserializeObject(json.Serialize(value))));
    public static void Run()
    {
        var c=Create(out var sim,out var p,out int id); var f=c.Factory(id);
        double opening=f.cash;
        var raw=sim.Dispatch(CityTraffic.Outside,id,TripPurpose.Import,cargoKind:CargoKind.RawMaterial);
        Check(raw!=null && raw.cargo==8 && f.raw==0 && f.cash==opening-64 && f.rawCosts==64,"Only successful dispatch pays for raw; inventory awaits arrival");
        Deliver(sim,raw); Check(f.raw==8 && f.cash==opening-64,"Arrival cannot charge raw a second time");
        c.traffic.trips.Clear(); c.traffic.stock[id]=8;
        var sale=sim.Dispatch(id,CityTraffic.Outside,TripPurpose.Export);
        Check(sale!=null && f.salesRevenue==0 && f.cash==opening-64,"Goods dispatch is not revenue");
        Deliver(sim,sale);
        Check(f.sold==8 && f.salesRevenue==360 && f.cash==opening-64+360,"Actual exports generate exactly delivered quantity times fixed price");
        sim.Advance(12); Check(f.salesRevenue==360,"Unload, return and task removal cannot duplicate sales"); Ledger(c);

        c=Create(out sim,out p,out id); f=c.Factory(id); c.traffic.stock[id]=8;
        int shop=c.buildings.First(b=>c.tiles[b.id]==3).id;
        sale=sim.Dispatch(id,shop,TripPurpose.Delivery); Deliver(sim,sale);
        Check(sale.cargo==0 && c.traffic.stock[shop]>=8 && f.salesRevenue==360,"Commercial delivery pays only on actual receipt"); Ledger(c);

        c=Create(out sim,out p,out id); f=c.Factory(id); c.traffic.stock[id]=8;
        shop=c.buildings.First(b=>c.tiles[b.id]==3).id;
        sale=sim.Dispatch(id,shop,TripPurpose.Delivery); c.traffic.stock[shop]=31; Deliver(sim,sale);
        Check(f.sold==1 && f.salesRevenue==45 && c.traffic.lostGoods==7,"Rejected overflow earns no revenue; only accepted units are paid"); Ledger(c);

        c=Create(out sim,out p,out id); f=c.Factory(id); Cash(f,17);
        raw=sim.Dispatch(CityTraffic.Outside,id,TripPurpose.Import,cargoKind:CargoKind.RawMaterial);
        Check(raw!=null && raw.cargo==2 && f.cash==1 && f.rawCosts==16,"Low cash limits purchased quantity instead of creating debt");
        Check(sim.Dispatch(CityTraffic.Outside,id,TripPurpose.Import,cargoKind:CargoKind.RawMaterial)==null && f.cash==1,"Unaffordable order neither dispatches nor charges");
        c.roads.Remove(c.roads.edges.Select(e=>e.id).ToList()); sim.Advance(122);
        Check(f.raw==0 && f.rawCosts==16 && f.salesRevenue==0 && c.traffic.lostRaw==2,"Lost prepaid imports remain a real cost and never become inventory"); Ledger(c);

        c=Create(out sim,out p,out id); f=c.Factory(id); c.traffic.stock[id]=8;
        sale=sim.Dispatch(id,CityTraffic.Outside,TripPurpose.Export);
        c.roads.Remove(c.roads.edges.Select(e=>e.id).ToList()); sim.Advance(122);
        Check(f.salesRevenue==0 && f.cash==FactoryState.StartingCash && c.traffic.stock[id]==8,"Failed export restores goods without paying the factory"); Ledger(c);
        Check(sim.Dispatch(CityTraffic.Outside,id,TripPurpose.Import,cargoKind:CargoKind.RawMaterial)==null && f.rawCosts==0,"Impossible route does not charge an undispatched raw order");

        c=Create(out sim,out p,out id); f=c.Factory(id); Cash(f,1.25); AtWork(c,p,id);
        c.PayAttendance(p,480);
        Check(f.cash==0 && f.wageCosts==1.25 && p.factoryWageCredit==1.25 && p.earnedWages==0,"Worker credit is limited to actual employer cash");
        double credit=p.factoryWageCredit; c.PayAttendance(p,480);
        Check(p.factoryWageCredit==credit && f.unpaidWages>0,"Cashless idle attendance cannot mint wages");
        c.HouseholdTick();
        Check(p.totalFactoryWagesPaid==1 && p.factoryWageCredit==.25 && c.society.history.Last().factoryWages==1,"Daily pay preserves fractional employer-funded credit"); Ledger(c);

        c=Create(out sim,out p,out id); f=c.Factory(id); Cash(f,2); AtWork(c,p,id); sim.Advance(5);
        Check(f.cash==0 && f.produced==0 && f.wageCosts<=2 && (p.tripId>0 || !p.atWork),"Raw-starved employer spends only finite cash, then sends worker home");
        for(int i=0;i<3000 && p.tripId>0;i++) sim.Advance(.05f);
        Check(p.jobId==-1 && !p.atWork,"Cashless employer releases employment after actual return");
        var job=c.society.jobEntities.First(j=>j.buildingId==id);
        Check(!c.AssignJob(p,job.id) && c.ExpectedJobWage(job)==0,"Unfunded physical slots are not usable wage-paying vacancies");
        credit=p.factoryWageCredit; sim.Advance(20);
        Check(p.factoryWageCredit==credit && f.salesRevenue==0,"Stopped factory cannot continue normal pay");
        c.traffic.stock[id]=8; sale=sim.Dispatch(id,CityTraffic.Outside,TripPurpose.Export); Deliver(sim,sale);
        Check(f.cash==360 && c.JobFunded(job) && c.AssignJob(p,job.id),"Selling remaining goods restores liquidity and permits rehiring without subsidies"); Ledger(c);

        c=Create(out sim,out p,out id); f=c.Factory(id); Cash(f,8); f.raw=2; c.traffic.stock[id]=24; AtWork(c,p,id);
        sim.Advance(8);
        Check(f.cash==0 && f.productionCosts==0 && f.produced==0 && f.salesRevenue==0,"Unsold full warehouse stops production while finite idle payroll exhausts cash"); Ledger(c);

        c=Create(out sim,out p,out id); f=c.Factory(id); Cash(f,1); f.raw=1; AtWork(c,p,id); c.ProcessFactoryWork(p,60);
        Check(f.raw==1 && !f.processing && f.productionCosts==0,"Insufficient processing funds do not consume raw or start a batch");
        Cash(f,10); c.ProcessFactoryWork(p,60);
        Check(f.produced==1 && f.productionCosts==2 && f.cash==8,"A real batch pays processing cost exactly once"); Ledger(c);

        c=Create(out sim,out p,out id); f=c.Factory(id); c.traffic.dispatchTimer=0;
        for(int i=0;i<3600;i++) sim.Advance(.1f);
        Check(f.salesRevenue>0 && f.rawCosts>0 && f.wageCosts>0 && f.productionCosts>0 && f.cash>0,"Natural multi-day operation funds imports, labour and processing with actual sales"); Ledger(c);
        Check(c.Valid(),"Natural financed industry remains save-valid");

        var json=new JavaScriptSerializer {MaxJsonLength=int.MaxValue};
        var copy=json.Deserialize<CityModel>(json.Serialize(c)); var resumed=new CityTraffic(copy);
        sim.Advance(5); resumed.Advance(5);
        Check(copy.Valid() && Snapshot(json,c.society)==Snapshot(json,copy.society) && Snapshot(json,c.traffic)==Snapshot(json,copy.traffic) && Snapshot(json,c.buildings)==Snapshot(json,copy.buildings),"Save continuation preserves accounts, fractional wages and in-transit transactions");

        c=Create(out sim,out p,out id); AtWork(c,p,id); c.PayAttendance(p,60); f=c.Factory(id);
        credit=p.factoryWageCredit; opening=f.cash; c.money=100000; c.DemolishBuilding(id);
        Check(p.factoryWageCredit==credit && f.cash==opening && p.jobId==-1,"Demolition retains already-paid resident credit and freezes the closed factory ledger");
        c.HouseholdTick(); Ledger(c);

        c=Create(out sim,out p,out id); f=c.Factory(id); c.version=6;
        foreach(var b in c.buildings.Where(b=>b.factory!=null)) {b.factory.financeInitialized=false; b.factory.cash=b.factory.capital=0;}
        f.raw=3; f.processing=true; f.progress=17; f.consumed=1; c.EnableHouseholds();
        Check(c.version==7 && f.raw==3 && f.progress==17 && f.cash==FactoryState.StartingCash,"v6 migration preserves materials and batches with explicit once-only opening capital");
        Cash(f,0); c.EnableHouseholds(); c.Recalculate();
        Check(f.cash==0,"Migration and recalculation never refill an exhausted account"); Ledger(c);

        c=Create(out sim,out p,out id);
        var lot=c.RoadsideLots().First(b=>c.CanBuild(b,(x,z)=>1,out _)); int treasury=c.money;
        int built=c.PlaceBuilding(lot,LandUse.Industrial,(x,z)=>1,out _);
        Check(built>=0 && c.money==treasury-CityModel.Cost(LandUse.Industrial)-FactoryState.StartingCash && c.Factory(built).capitalFromTreasury,"New factory capital is transferred from construction budget, not generated");
        c.Factory(built).cash=-1; Check(!c.Valid(),"Malformed negative or imbalanced financial saves are rejected");
        Console.WriteLine("PASS: "+checks+" factory finance checks");
    }
}
