using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using HarborCity;
public static class ObservabilityChecks
{
    static int checks;
    static readonly JavaScriptSerializer json=new JavaScriptSerializer {MaxJsonLength=int.MaxValue};
    static void Check(bool yes,string why) {if(!yes)throw new Exception("Observability: "+why);checks++;}
    static CityModel City() {var c=TestCity.Create();new CityTraffic(c);return c;}
    static CityDailyAnalysis Attach(CityModel c) {return c.analysis=new CityDailyAnalysis(c);}
    static string Serialize(object value)
    {
        if(value is CityModel model)return json.Serialize(model.ToSaveData());
        if(value is CityLogEvent)return json.Serialize(value.GetType().GetFields().Where(f=>!f.IsNotSerialized).ToDictionary(f=>f.Name,f=>f.GetValue(value)));
        return json.Serialize(value);
    }
    public static void Run()
    {
        var c=City();foreach(var shopBuilding in c.buildings.OfType<CommercialBuilding>())shopBuilding.stock=4;var a=Attach(c);var s=a.Refresh(true);
        Check(c.logSink==null && s.currentDay.inProgress && s.live.day==1,"Default observer works from day one without a disk writer");
        Check(s.live.population==c.population && s.live.employed==c.Employed && s.live.units==c.buildings.OfType<ResidentialBuilding>().Sum(b=>b.housingUnits),"Live summary matches authoritative simulation state");
        Check(s.residents.Count(p=>p.resident)==c.population && s.households.All(h=>h.members.All(p=>p.householdId==h.id)),"Detail entities retain stable IDs and household membership");
        Check(s.today.produced==0 && s.today.employerWages==0 && s.timeline.Count==0,"Opening cumulative totals and initial states do not fabricate today's activity");
        int id=c.buildings.OfType<IndustrialBuilding>().First().id;var f=c.Factory(id);int shop=c.buildings.OfType<CommercialBuilding>().First().id;
        f.produced+=2;f.sold++;f.salesRevenue+=14;f.rawCosts+=4;f.wageCosts+=3;f.productionCosts+=1;
        a.ObserveLabour(id,11,120,30,90,0,0);s=a.Refresh(true);
        Check(s.today.produced==2 && s.today.sold==1 && s.today.revenue==14 && s.today.employerWages==3,"Today comes from real cumulative deltas, including employer payments");
        Check(s.Business(id).today.rawBlocked==90 && s.findings.Any(x=>x.entityKind==AnalysisEntityKind.Factory && x.entityId==id && x.code=="raw"),"Raw labour evidence produces a drillable deterministic factory finding");
        string immutable=Serialize(s);f.produced++;a.Refresh(true);
        Check(Serialize(s)==immutable,"Published snapshot remains detached as simulation continues");
        var person=c.Citizens.First();person.lastDelay=20;person.lastCommute=33;
        c.Trace("citizen.attendance","arrival",person,citizen:person.id);c.Trace("citizen.attendance","duplicate",person,citizen:person.id);
        var trip=new CityTraffic(c).Dispatch(CityTraffic.Outside,id,TripPurpose.Import,cargoKind:CargoKind.RawMaterial);
        trip.status=TripStatus.Waiting;trip.blocked=c.society.settings.secondsPerDay*31/1440;
        a.ObserveTraffic();a.ObserveTraffic();s=a.Refresh(true);
        Check(s.today.late==1 && s.timeline.Count(e=>e.code=="resident.late")==1,"Severe lateness is one resident/day event, not each repeated observation");
        Check(s.timeline.Count(e=>e.code=="freight.blocked")==1 && s.today.longWaitingFreight==1 && s.Trip(trip.id).blockedMinutes>=30,"Long freight blockage produces one episode event and real task detail");
        trip.blocked=0;trip.status=TripStatus.Driving;a.ObserveTraffic();trip.blocked=4;trip.status=TripStatus.Waiting;a.ObserveTraffic();
        Check(a.Refresh(true).timeline.Count(e=>e.code=="freight.blocked")==2 && a.State.today.longWaitingFreight==1,"New blockage episode gets a new key event; daily task count remains unique");
        c.Inventory(shop).Stock=0;a.ObserveTraffic();c.Inventory(shop).Stock=4;a.ObserveTraffic();s=a.Refresh(true);
        Check(s.timeline.Count(e=>e.code=="commercial.empty")==1 && s.timeline.Count(e=>e.code=="commercial.restocked")==1,"Commercial transitions reflect actual inventory, not repetitive empty polling");
        c.society.dayElapsed=c.society.settings.secondsPerDay*.5f;f.status="缺料";c.Trace("factory.status","shortage",f,building:id);f.status="生产中";c.Trace("factory.status","recover",f,building:id);
        Check(a.Refresh(true).timeline.Any(e=>e.entityId==id && e.message.Contains("缺料 → 生产中")),"Factory recovery retains before/after state with entity target");
        int visible=a.State.timeline.Count;for(int i=0;i<2000;i++)c.Trace("citizen.pay","fine payroll",person,citizen:person.id);
        Check(a.Refresh(true).timeline.Count==visible,"Fine payroll steps never flood the human timeline");
        for(int i=0;i<900;i++)c.Trace("household.moved","bounded event "+i,household:1);
        s=a.Refresh(true);
        Check(s.timeline.Count==750 && s.timeline.First().message=="bounded event 150" && s.timeline.Last().message=="bounded event 899","Real key-event stream keeps only 750 most recent entries in order");
        Check(s.today.moved==900,"Current-day migrations count observed commits before settlement");
        string exported=Serialize(s);var restored=json.Deserialize<CityAnalysisState>(exported);
        Check(restored.timeline.Count==750 && restored.Business(id).today.rawBlocked==90 && restored.Resident(person.id).householdId==person.householdId,"Structured export round trip retains entities, evidence and bounded history");
        c=City();a=Attach(c);id=c.buildings.OfType<IndustrialBuilding>().First().id;
        var jobs=c.society.jobEntities.Where(j=>j.buildingId==id).ToList();
        foreach(var job in jobs)c.Trace("job.removed","remove",job,building:id,job:job.id);
        s=a.Refresh(true);
        Check(s.timeline.Count(e=>e.code=="job.removed")==1 && s.timeline.First(e=>e.code=="job.removed").affectedCount==jobs.Count,"Simultaneous invalidation is coalesced by workplace, preserving affected job count");
        for(int i=0;i<181;i++)c.Trace("city.day","synthetic ledger for bounds",new HouseholdDay {day=i+2,population=i});
        s=a.Refresh(true);
        Check(s.history.Count==180 && s.reports.Count==180 && s.history.First().population==1 && s.history.Last().population==180,"Both report and city-history rings remain bounded at 180 days");
        c=City();c.society.history.Add(new HouseholdDay {day=7,population=18,employed=12});c.society.dayElapsed=c.society.settings.secondsPerDay*.5f;a=Attach(c);s=a.Refresh(true);
        Check(s.currentDay.partial && Math.Abs(s.currentDay.fromMinute-720)<.01 && s.history.Count==1 && !s.history[0].industryKnown,"Load baseline marks partial day and never fabricates past industrial measurements");
        string before=TestCity.Snapshot(c);a.Refresh(true);a.Current();a.Refresh(true);
        Check(before==TestCity.Snapshot(c),"Repeated paused querying does not mutate any saved simulation field");
        c=City();a=Attach(c);var household=c.society.families.First();c.money=100000;c.DemolishBuilding(household.home);s=a.Refresh(true);
        Check(s.today.left>0 && s.timeline.Any(e=>e.code=="household.left"),"Non-settlement departure remains visible in Today's actual event counts");
        string root=Path.Combine("Temp","ObservabilityChecks-"+Guid.NewGuid().ToString("N"));
        c=City();a=Attach(c);int baselineProduction=c.buildings.OfType<IndustrialBuilding>().Sum(b=>b.factory.produced);
        var reference=City();var observed=City();var second=Attach(observed);
        using(var log=new CitySimulationLog(root,Serialize))
        {
            c.logSink=log.Write;log.Snapshot("baseline",c);c.Trace("session.start","debug only");
            for(int n=0;n<8;n++)
            {
                new CityTraffic(c).Advance(60);new CityTraffic(reference).Advance(60);new CityTraffic(observed).Advance(60);
                a.Refresh(true);second.Refresh(true);
                Check(TestCity.Snapshot(c)==TestCity.Snapshot(reference) && TestCity.Snapshot(c)==TestCity.Snapshot(observed),"Observer and optional disk trace leave all simulation state and deterministic execution identical (step "+n+")");
            }
            c.Trace("trip.state","trace-only poll");c.Trace("network.audit","trace-only audit");
            Check(Serialize(a.Refresh(true))==Serialize(second.Refresh(true)),"Trace-only events cannot change human analysis, versions, findings or timeline");
            Check(ReferenceEquals(c.analysis,a) && a.State.history.Count>0,"Writer baseline snapshot neither creates nor replaces the independent observer");
            log.Flush();
            Check(Directory.GetFiles(log.DirectoryPath,"events-*.jsonl").Length>0 && Directory.GetFiles(log.DirectoryPath,"readable-*.log").Length>0,"Explicit Debug Trace retains both original audit formats");
            Check(!Directory.Exists(Path.Combine(log.DirectoryPath,"Analysis")),"Trace never renders Markdown analysis or creates Analysis folder");
        }
        c.logSink=null;
        Check(ReferenceEquals(c.analysis,a),"Disposing a trace writer never detaches runtime analysis");
        var oldReport=a.Latest;var oldJson=Serialize(oldReport);new CityTraffic(c).Advance(12);a.Refresh(true);
        Check(Serialize(oldReport)==oldJson && a.Failure==null,"Completed reports remain stable after subsequent current-day refreshes");
        Console.WriteLine("PASS: "+checks+" runtime observability/dashboard checks");
    }
}
