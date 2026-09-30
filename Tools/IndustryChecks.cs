using System;
using System.Linq;
using HarborCity;

public static class IndustryChecks
{
    static int checks;
    static void Check(bool value,string message) {if(!value) throw new Exception("Industry: "+message); checks++;}
    static CityModel Create(out CityTraffic sim,out CityResident worker,out int factory)
    {
        var c=CityModel.CreateLegacySample(); sim=new CityTraffic(c); c.EnableRoads((x,z)=>1); c.EnableBuildings(); c.EnableHouseholds(); c.EnableResidentTransport();
        foreach(var h in c.society.families) {h.nextReview=10000; foreach(var p in h.people) c.ReleaseJob(p);}
        worker=c.Citizens.First(p=>p.canWork); worker.skill=2;
        var job=c.society.jobEntities.First(j=>c.tiles[j.buildingId]==4); factory=job.buildingId; c.AssignJob(worker,job.id);
        c.society.settings.applicantsPerDay=0; c.traffic.dispatchTimer=-10000; c.traffic.productionTimer=0;
        return c;
    }
    static void Arrive(CityModel c,CityResident p,int id)
    {
        p.atWork=true; p.location=id; p.arrivedDay=c.day; p.departureDay=c.day;
        c.society.dayElapsed=(480+p.id%3*30)/1440f*c.society.settings.secondsPerDay;
    }
    public static void Run()
    {
        var c=Create(out var sim,out var p,out int id); var f=c.Factory(id); f.raw=8;
        sim.Advance(3); Check(c.traffic.stock[id]==0 && f.productiveMinutes==0,"Jobs and raw stock cannot produce without actual attendance");
        f.raw=0; Arrive(c,p,id); sim.Advance(5);
        Check(f.attendanceMinutes>0 && f.productiveMinutes==0 && f.produced==0 && p.earnedWages>0,"Idle attendance earns contractual wages but cannot create goods or bank productive labour");
        f.raw=2; sim.Advance(2);
        Check(f.processing && f.progress>0 && f.produced==0 && f.raw==1,"Starting a batch reserves exactly one real raw unit");
        sim.Advance(3.1f);
        Check(f.produced==1 && c.traffic.stock[id]==1 && f.consumed==f.produced+(f.processing?1:0),"Sixty actual worker minutes complete a product with raw conservation");
        float productive=f.productiveMinutes, progress=f.progress; c.traffic.stock[id]=24; sim.Advance(3);
        Check(f.productiveMinutes==productive && f.progress==progress && f.status=="成品仓满","Full warehouse halts processing without banking idle work");
        c.traffic.stock[id]-=8; int produced=f.produced; sim.Advance(.1f);
        Check(f.produced==produced && f.productiveMinutes>productive,"Shipping frees capacity without instant catch-up production");
        Check(f.noise>0 && f.pollution>0,"Actual production creates environmental activity");
        float noise=f.noise, pollution=f.pollution; p.atWork=false; p.departureDay=c.day; sim.Advance(5);
        Check(f.noise<noise && f.pollution<pollution,"Idle factory emissions fade instead of applying a fixed proximity penalty");

        c=Create(out sim,out p,out id); f=c.Factory(id);
        var import=sim.Dispatch(CityTraffic.Outside,id,TripPurpose.Import,cargoKind:CargoKind.RawMaterial);
        Check(import!=null && f.raw==0 && sim.IncomingAmount(id,CargoKind.RawMaterial)==8,"Imported raw is reserved in transit, never credited at dispatch");
        for(int i=0;i<2000 && import.cargo>0;i++) sim.Advance(.05f);
        Check(import.cargo==0 && f.raw==8 && f.imported==8 && c.traffic.stock[id]==0,"Actual truck arrival delivers raw, not goods");
        f.raw=23; c.traffic.trips.Clear();
        import=sim.Dispatch(CityTraffic.Outside,id,TripPurpose.Import,cargoKind:CargoKind.RawMaterial);
        Check(import!=null && import.cargo==1 && sim.Dispatch(CityTraffic.Outside,id,TripPurpose.Import,cargoKind:CargoKind.RawMaterial)==null,"Inbound reservations prevent raw overflow and duplicate full orders");

        c=Create(out sim,out p,out id); f=c.Factory(id); c.traffic.stock[id]=8;
        var output=sim.Dispatch(id,CityTraffic.Outside,TripPurpose.Export);
        Check(output!=null && c.traffic.stock[id]==0 && output.cargo==8,"Exports remove goods only when a real truck departs");
        for(int i=0;i<2000 && output.cargo>0;i++) sim.Advance(.05f);
        Check(output.cargo==0 && c.traffic.delivered==8,"Export only completes on actual arrival");

        c=Create(out sim,out p,out id); f=c.Factory(id);
        import=sim.Dispatch(CityTraffic.Outside,id,TripPurpose.Import,cargoKind:CargoKind.RawMaterial);
        c.roads.Remove(c.roads.edges.Select(e=>e.id).ToList()); sim.Advance(1);
        Check(f.raw==0 && import.cargo==8 && import.status==TripStatus.Waiting,"Broken logistics cannot teleport raw into factory");
        sim.Advance(121);
        Check(f.raw==0 && !c.traffic.trips.Contains(import) && c.traffic.lostRaw==8,"Failed import is recorded as loss, never delivered");

        c=Create(out sim,out p,out id); f=c.Factory(id); f.raw=3; Arrive(c,p,id);
        c.ProcessFactoryWork(p,30);
        var second=c.Citizens.First(person=>person.canWork && person!=p); second.skill=2;
        c.AssignJob(second,c.society.jobEntities.First(j=>j.buildingId==id && j.occupiedCitizenId<0).id);
        Arrive(c,second,id); c.ProcessFactoryWork(second,30);
        Check(f.produced==1 && f.productiveMinutes==60,"Different workers contribute real minutes to the same batch");
        c.ProcessFactoryWork(p,15); c.money=100000; c.DemolishBuilding(id);
        Check(c.traffic.lostGoods==1 && c.traffic.lostRaw==2 && !f.processing && f.raw==0 && f.discardedBatches==1,"Demolition accounts for stock and unfinished batch loss");

        c=Create(out sim,out p,out id); f=c.Factory(id);
        var home=c.society.families.First(h=>h.people.Contains(p)); int homeId=home.home;
        c.buildings[id].x=c.buildings[homeId].x+6; c.buildings[id].z=c.buildings[homeId].z;
        var clean=c.Evaluate(home,homeId,keepJobs:true); f.noise=1; f.pollution=1; c.buildings[homeId].heavyTraffic=.5f;
        home.noiseSensitivity=home.pollutionSensitivity=home.trafficSensitivity=1;
        var sensitive=c.Evaluate(home,homeId,keepJobs:true);
        home.noiseSensitivity=home.pollutionSensitivity=home.trafficSensitivity=.1f;
        var tolerant=c.Evaluate(home,homeId,keepJobs:true);
        Check(clean.environmentCost==0 && sensitive.environmentCost>tolerant.environmentCost && tolerant.environmentCost>0,"Housing reacts to measured exposure and individual household sensitivity");
        Check(sensitive.wage==tolerant.wage && sensitive.minutes==tolerant.minutes && sensitive.score<tolerant.score,"Environmental preference trades off against the same rent, jobs and commute");

        c=Create(out sim,out p,out id); f=c.Factory(id); c.traffic.stock[id]=7;
        c.version=5; foreach(var b in c.buildings) b.factory=null;
        int jobId=p.jobId; c.EnableHouseholds();
        Check(c.version==6 && c.Factory(id).raw==0 && c.traffic.stock[id]==7 && p.jobId==jobId,"v5 migration preserves goods and jobs without inventing raw");
        c.Factory(id).raw=3; c.EnableHouseholds(); Check(c.Factory(id).raw==3,"Industry migration is idempotent");
        c.traffic.dispatchTimer=0; Check(c.Valid(),"Industrial state and preferences are save-valid");

        c=Create(out sim,out p,out id); c.traffic.dispatchTimer=0;
        for(int i=0;i<2400;i++) sim.Advance(.1f);
        f=c.Factory(id);
        Check(f.imported>0 && f.produced>0 && f.shipped>0,"Automatic scheduling, actual commute, raw imports and goods dispatch complete the production loop");
        Check(f.raw+f.consumed==f.imported && f.consumed==f.produced+(f.processing?1:0),"Natural operation conserves imported raw and processing batches");
        Check(c.Valid(),"Multi-day industrial simulation remains save-valid");
        Console.WriteLine("PASS: "+checks+" industry checks");
    }
}

