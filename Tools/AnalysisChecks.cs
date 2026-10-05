using System;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using HarborCity;

public static class AnalysisChecks
{
    static int checks;
    static readonly JavaScriptSerializer json=new JavaScriptSerializer {MaxJsonLength=int.MaxValue};
    static void Check(bool value,string reason) {if(!value) throw new Exception("Analysis: "+reason); checks++;}
    static string Serialize(object value)
    {
        if(value is CityModel) return json.Serialize(((CityModel)value).ToSaveData());
        if(value is CityLogEvent)
            return json.Serialize(value.GetType().GetFields().Where(f=>!f.IsNotSerialized).ToDictionary(f=>f.Name,f=>f.GetValue(value)));
        return json.Serialize(value);
    }
    static CityModel Create(out CityTraffic traffic,out CityResident person,out int factory)
    {
        var c=TestCity.Create(); traffic=new CityTraffic(c);   
        foreach(var h in c.society.families) {h.nextReview=10000; foreach(var p in h.people) c.ReleaseJob(p);}
        person=c.Citizens.First(p=>p.canWork); person.skill=2;
        var job=c.society.jobEntities.First(j=>(int)c.UseOf(j.buildingId)==4); factory=job.buildingId; c.AssignJob(person,job.id);
        c.society.settings.applicantsPerDay=0; c.traffic.dispatchTimer=-10000;
        return c;
    }
    static CityDailyAnalysis Observe(CityModel c) {var a=new CityDailyAnalysis(c); c.analysis=a; return a;}
    static void Work(CityModel c,CityResident p,int id,float minute,float duration,float paid=-1)
    {
        p.atWork=true; p.location=id; p.arrivedDay=p.departureDay=c.day;
        c.society.dayElapsed=minute/1440*c.society.settings.secondsPerDay;
        c.PayAttendance(p,duration); c.ProcessFactoryWork(p,paid<0?duration:paid,duration);
        c.AdvanceIndustry(duration);
    }
    static void Close(CityModel c) {c.society.dayElapsed=0; c.Tick();}
    static FactoryDailySummary Factory(CityAnalysisReport r,int id) => r.factories.Single(f=>f.id==id);
    public static void Run()
    {
        var c=Create(out var sim,out var p,out int id); var f=c.Factory(id); f.raw=1;
        var analysis=Observe(c);
        Work(c,p,id,480,60); Work(c,p,id,600,180);
        p.lastCommute=27; p.lastDelay=20;
        c.Trace("citizen.attendance","actual arrival",p,citizen:p.id,building:id);
        c.Trace("citizen.attendance","duplicate observation",p,citizen:p.id,building:id);
        Close(c); var report=analysis.Latest; var factory=Factory(report,id);
        Check(report.day==1 && report.settlementDay==2 && !report.partial,"Completed day is separate from legacy next-day settlement numbering");
        Check(factory.produced==1 && factory.attendance==240 && factory.productive==60 && factory.rawBlocked==180,"Daily counters match actual cumulative changes and blocked labour");
        Check(factory.firstRawBlock>=599 && factory.firstRawBlock<=601 && factory.mainBottleneck=="原料不足","Raw bottleneck is based on observed work, with first observed time");
        Check(factory.assigned==1 && factory.attended==1,"New hires during next-day settlement do not contaminate the completed day's staff denominator");
        Check(report.lateResidents==1 && report.commuteSamples==1 && report.todayArrivalCommute==27,"Actual late arrivals and commute samples are deduplicated by citizen");
        Check(factory.wageCost>0 && factory.profit==factory.revenue-factory.rawCost-factory.wageCost-factory.productionCost,"Daily finance derives from real employer counters");
        Check(report.attention.Count<=5 && report.attention.Any(a=>a.code=="raw"),"City attention is bounded and includes evidenced bottlenecks");
        Work(c,p,id,480,30); Close(c); factory=Factory(analysis.Latest,id);
        Check(factory.produced==0 && factory.attendance==30 && factory.rawBlocked==30,"Next day resets evidence and subtracts the previous closing baseline");

        c=Create(out sim,out p,out id); f=c.Factory(id); f.raw=1; analysis=Observe(c);
        Work(c,p,id,480,60); Close(c); factory=Factory(analysis.Latest,id);
        Check(factory.raw==0 && factory.produced==1 && !factory.causes.Any(a=>a.code=="raw"),"Empty end inventory alone is not evidence of production lost to raw shortage");
        Check(factory.mainBottleneck=="未观测到明显生产阻断","A productive factory does not inherit a diagnosis from its end status string");

        c=Create(out sim,out p,out id); f=c.Factory(id); f.raw=1; f.cash=f.capital=c.ResidentWage(p)/8.0+FactoryState.ProcessingCost; analysis=Observe(c);
        Work(c,p,id,480,60); c.roads.Remove(c.roads.edges.Select(e=>e.id).ToList()); Close(c); factory=Factory(analysis.Latest,id);
        Check(factory.cash==0 && factory.productive==60 && !factory.causes.Any(a=>a.code=="cash" || a.code=="road"),"End-only cash exhaustion and disconnected roads do not invent earlier productive losses");
        Check(factory.relatedObservations.Any(s=>s.Contains("不代表全天断路")),"Current road state stays separate from observed daily causes");

        c=Create(out sim,out p,out id); f=c.Factory(id); f.raw=2; c.Inventory(id).Stock=24; analysis=Observe(c);
        Work(c,p,id,480,120); Close(c); factory=Factory(analysis.Latest,id);
        Check(factory.stockBlocked==120 && factory.rawBlocked==0 && factory.mainBottleneck=="成品仓满","Full goods capacity is distinguished from raw shortages");
        Check(factory.produced==0 && factory.wageCost>0 && factory.profit<0,"Idle paid labour and real cash loss are visible without inventing output");

        c=Create(out sim,out p,out id); f=c.Factory(id); f.cash=f.capital=0; f.raw=1; analysis=Observe(c);
        Work(c,p,id,480,60,0); Close(c); factory=Factory(analysis.Latest,id);
        Check(factory.fundsBlocked==60 && factory.rawBlocked==0 && factory.wageCost==0 && factory.unpaidWages>0,"Unpaid attendance is diagnosed as finance, not lack of raw");
        Check(factory.effects.Any(s=>s.Contains("不能进入居民工资余额")),"Wage effects require observed unpaid attendance");

        c=Create(out sim,out p,out id); analysis=Observe(c);
        var raw=sim.Dispatch(CityTraffic.Outside,id,TripPurpose.Import,cargoKind:CargoKind.RawMaterial);
        raw.status=TripStatus.Waiting; raw.blocked=3; analysis.ObserveTraffic(); analysis.ObserveTraffic();
        Work(c,p,id,480,60); raw.blocked=0; raw.status=TripStatus.Driving; analysis.ObserveTraffic();
        Close(c); factory=Factory(analysis.Latest,id);
        Check(analysis.Latest.longWaitingFreight==1 && factory.inputWaiting==1 && factory.incomingRaw==8,"Waiting tasks are deduplicated and remembered after recovery; in-transit inventory remains separate");
        Check(factory.causes.Any(a=>a.code=="input_transport" && a.evidence.Contains("尚未证明")),"Transport and shortages are not presented as a fully proven temporal causal link");

        c=Create(out sim,out p,out id); f=c.Factory(id); f.raw=5;
        Work(c,p,id,480,60); // Historical production before observation starts.
        c.society.dayElapsed=720f/1440*c.society.settings.secondsPerDay; analysis=Observe(c);
        Work(c,p,id,780,60); Close(c); report=analysis.Latest; factory=Factory(report,id);
        Check(report.partial && Math.Abs(report.fromMinute-720)<.01f && factory.produced==1 && factory.imported==0,"A mid-day baseline excludes historical totals and labels partial coverage");
        string state=Serialize(c), snapshot=Serialize(analysis.Current()); analysis.Current();
        Check(state==Serialize(c) && snapshot==Serialize(analysis.Current()),"Repeated partial snapshots are deterministic and never change simulation state");

        c=Create(out sim,out p,out id); analysis=Observe(c); f=c.Factory(id); f.raw=1;
        int shop=c.buildings.First(b=>(int)c.UseOf(b.id)==3).id;
        Work(c,p,id,480,60); c.Inventory(shop).Stock=0; analysis.ObserveSale(id,shop); Close(c);
        factory=Factory(analysis.Latest,id);
        Check(factory.relatedObservations.Count==1 && factory.relatedObservations[0].Contains("不能据此确认"),"A related commercial shortage is explicitly not attributed to one supplier");

        c=Create(out sim,out p,out id); analysis=Observe(c);
        var lot=c.RoadsidePreview(40,6,LandUse.Industrial,HousingKind.Apartment,out _);
        int built=c.PlaceBuilding(lot,LandUse.Industrial,(x,z)=>1,out _); Close(c);
        factory=Factory(analysis.Latest,built);
        Check(factory.capitalInflow==FactoryState.StartingCash && factory.revenue==0 && factory.cashBefore==0,"New factory capital is separated from sales in the daily balance");

        c=Create(out sim,out p,out id); var h=c.society.families.First(family=>family.people.Contains(p));
        foreach(var family in c.society.families) if(family!=h) family.resident=false;
        int destination=c.buildings.First(b=>b.id!=h.home && c.HousingCapacity(b.id)>0 && c.EntityRoadAccess(b.id)).id;
        c.Residence(destination).housing=HousingKind.Villa; c.Residence(destination).housingUnits=1; c.Residence(destination).askingRent=12;
        h.privacyPreference=1; h.timePreference=h.spacePreference=h.savingPreference=0; h.nextReview=2; c.society.settings.improvement=0;
        analysis=Observe(c); Close(c);
        var decision=analysis.Latest.households.Single(d=>d.id==h.id);
        Check(decision.evaluated && decision.committed && decision.action=="搬家" && decision.fromHome!=decision.toHome,"A real committed household move retains before/after identities");
        Check(decision.proposedJobs.Count==h.people.Count && decision.actualJobs.SequenceEqual(decision.citizenIds.Select(cid=>h.people.Find(person=>person.id==cid).jobId)),"Proposed and actual jobs are matched by citizen identity, not differing iteration order");
        Check(decision.factors.Any(s=>s.Contains("私密")) && decision.reason.Contains("评分"),"Decision explanations use actual utility components and committed outcome");

        c=Create(out sim,out p,out id); analysis=Observe(c); Work(c,p,id,480,1); h=c.society.families.First(family=>family.people.Contains(p)); h.nextReview=2; Close(c);
        decision=analysis.Latest.households.Single(d=>d.id==h.id);
        Check(!decision.evaluated && !decision.committed && decision.action=="评估延期","Busy residents get an actual deferral, not fabricated candidate scores");

        c=Create(out sim,out p,out id); analysis=Observe(c); h=c.society.families.First(family=>family.people.Contains(p));
        int initialRent=c.Residence(h.home).askingRent; c.Residence(h.home).askingRent+=3; Close(c);
        Check(analysis.Latest.housingChanges.Any(s=>s.Contains("挂牌租金 "+initialRent+" → "+(initialRent+3))),"Advertised rent changes are measured from a copied baseline");

        var a=Create(out var first,out var pa,out int fa); var b=Create(out var second,out var pb,out int fb);
        a.traffic.dispatchTimer=b.traffic.dispatchTimer=0; analysis=Observe(a);
        first.Advance(240.1f); second.Advance(240.1f);
        Check(analysis.Failure==null && Serialize(a)==Serialize(b),"Analysis-enabled real transport and economy match the same simulation without analysis");
        var compare=Create(out second,out pb,out fb); compare.traffic.dispatchTimer=0; var repeat=Observe(compare); second.Advance(240.1f);
        Check(Serialize(analysis.Latest)==Serialize(repeat.Latest),"The same simulation produces byte-equivalent structured analysis with no LLM or wall clock");

        string root="Temp/AnalysisChecks-"+Guid.NewGuid().ToString("N");
        c=Create(out sim,out p,out id); f=c.Factory(id); f.raw=1;
        using(var log=new CitySimulationLog(root,Serialize))
        {
            c.logSink=log.Write; log.Snapshot("baseline",c);
            Work(c,p,id,480,60); Work(c,p,id,600,120); Close(c);
            string export=log.Export(Path.Combine(root,"Exports"),c);
            string folder=Path.Combine(export,"Analysis");
            Check(log.Failure==null && log.AnalysisFailure==null && File.Exists(Path.Combine(folder,"latest-city.md")),"Derived reports export beside the unchanged raw journal");
            Check(new[]{"city","factories","households"}.All(kind=>File.Exists(Path.Combine(folder,"day-000001-"+kind+".md"))),"All three human reports are generated on actual daily settlement");
            var restored=json.Deserialize<CityAnalysisReport>(File.ReadAllText(Path.Combine(folder,"day-000001.json")));
            Check(Factory(restored,id).rawBlocked==120 && restored.day==1,"Structured report round-trip retains daily evidence");
            Check(File.Exists(Path.Combine(folder,"in-progress.json")) && json.Deserialize<CityAnalysisReport>(File.ReadAllText(Path.Combine(folder,"in-progress.json"))).inProgress,"Export includes an explicitly unfinished current-day snapshot");
            int lines=File.ReadAllLines(Directory.GetFiles(export,"events-*.jsonl").First()).Length;
            c.Trace("test.audit","raw audit continues"); log.Flush();
            Check(File.ReadAllLines(Directory.GetFiles(export,"events-*.jsonl").First()).Length==lines,"Exported audit and analysis remain immutable while simulation continues");
            string sample=Path.Combine("Temp","CityAnalysisSample"); Directory.CreateDirectory(sample);
            foreach(string file in Directory.GetFiles(folder)) File.Copy(file,Path.Combine(sample,Path.GetFileName(file)),true);
        }
        Check(c.analysis==null,"Closing a journal detaches its session observer");

        c=Create(out sim,out p,out id);
        using(var log=new CitySimulationLog(root,Serialize))
        {
            c.logSink=log.Write; log.Snapshot("baseline",c);
            File.WriteAllText(Path.Combine(log.DirectoryPath,"Analysis"),"block derived directory to simulate write failure");
            Close(c); c.Trace("test.audit","raw survives analysis failure"); log.Flush();
            Check(log.AnalysisFailure!=null && log.Failure==null,"Derived output failure is isolated from the raw journal");
            string faultExport=log.Export(Path.Combine(root,"FaultExports"),c);
            Check(Directory.GetFiles(faultExport,"events-*.jsonl").SelectMany(File.ReadAllLines).Any(line=>line.Contains("raw survives analysis failure")),"Raw auditing continues after a derived writer failure");
        }
        c=Create(out sim,out p,out id);f=c.Factory(id);f.raw=1;analysis=Observe(c);
        Work(c,p,id,480,60);double wages=f.wageCosts;
        c.money=100000;Check(c.DemolishBuilding(id),"Factory demolition fixture");Close(c);
        factory=Factory(analysis.Latest,id);
        Check(!factory.active && factory.state=="已拆除" && factory.produced==1 && factory.wageCost==wages && factory.goods==0,"Demolished factory retains real daily output and payroll without an entity shell");
        Check(analysis.Failure==null && c.GetBuilding(id)==null,"Daily observer handles physically removed factories");
        Console.WriteLine("PASS: "+checks+" deterministic city analysis checks");
    }
}
