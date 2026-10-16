using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using HarborCity;
using HarborCity.RoadCore;

// Headless host only. All world changes still run through the normal simulation.
static class CityScenarioRun
{
    static readonly JavaScriptSerializer Json=new JavaScriptSerializer{MaxJsonLength=int.MaxValue};
    static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
    static void Write(string path,object data)=>File.WriteAllText(path,Json.Serialize(data));
    static string Hash(byte[] data){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(data)).Replace("-","").ToLowerInvariant();}
    static object Distribution(IEnumerable<double> values)
    {
        var a=values.OrderBy(x=>x).ToArray();if(a.Length==0)return null;
        return new{count=a.Length,mean=a.Average(),p95=a[(int)Math.Ceiling(a.Length*.95)-1],p99=a[(int)Math.Ceiling(a.Length*.99)-1],max=a[a.Length-1]};
    }
    static object State(CityCoreIndustrySession s)
    {
        var c=s.City;var people=s.Entities.Members.Select(m=>m.Person).ToArray();var factories=s.Entities.Buildings.OfType<IndustrialBuilding>().ToArray();var shops=s.Entities.Buildings.OfType<CommercialBuilding>().ToArray();var tasks=s.Transport.Tasks;
        double now=s.Simulation.Seconds;long issued=s.Transport.Snapshot().nextTask-1;
        return new{tick=s.Simulation.Tick,day=c.day,population=people.Length,households=c.society.families.Count,buildings=s.Entities.Buildings.Count,roads=s.Transport.Network.RoadCount,junctions=s.Transport.Network.JunctionCount,
            workforce=people.Count(p=>p.canWork&&!p.dead),employed=people.Count(p=>p.jobId>0),atWork=people.Count(p=>p.atWork),residentTrips=people.Count(p=>p.tripId!=0),
            active=s.Transport.ActiveCount,stranded=s.Transport.Vehicles.Count(v=>v.status==VehicleStatus.Stranded),tasks=tasks.Count,
            issuedTasks=issued,retiredAcknowledgedTasks=issued-tasks.Count,completedPendingAck=tasks.Count(t=>t.state==TripState.Completed),tasksOlderThanDay=tasks.Count(t=>now-t.departedAt>c.society.settings.secondsPerDay),
            oldestTaskSeconds=tasks.Count==0?0:tasks.Max(t=>now-t.departedAt),tasksByPurpose=tasks.GroupBy(t=>t.purpose).ToDictionary(g=>g.Key.ToString(),g=>g.Count()),
            waitingToDepart=(object)null,failedHistorical=(object)null, // No authoritative historical counters exist; never report fabricated zeroes.
            produced=factories.Sum(f=>(long)f.factory.produced),rawStock=factories.Sum(f=>(long)f.factory.raw),factoryGoods=factories.Sum(f=>(long)f.Stock),shopGoods=shops.Sum(b=>(long)b.Stock),
            shipped=factories.Sum(f=>(long)f.factory.shipped),rawDispatched=c.trade.rawDispatched,rawDelivered=c.trade.rawDelivered,exports=c.trade.goodsExported,consumed=c.consumption.ConsumedGoods,
            treasury=c.money,companyCash=factories.Sum(f=>f.factory.cash)+shops.Sum(b=>b.Account.Cash),householdSavings=c.society.families.Sum(h=>(long)h.savings),
            wages=factories.Sum(f=>f.factory.wageCosts)+shops.Sum(b=>b.Account.WageCosts),tax=s.Simulation.Finance.TaxesCollected,maintenance=s.Simulation.Finance.MaintenancePaid,
            powerDemand=c.demand,powerCapacity=c.power,waterDemand=c.WaterDemand,waterCapacity=c.water,sewageCapacity=c.SewageCapacity,unsupplied=s.Entities.Buildings.Count(b=>!c.Supply(b.id).Complete),
            wasteGenerated=s.Simulation.Waste.Generated,wasteWaiting=s.Simulation.Waste.Waiting,wasteInTransit=s.Simulation.Waste.InTransit,wasteStored=s.Simulation.Waste.Stored,wasteProcessed=s.Simulation.Waste.Processed,wasteBalanceError=s.Simulation.Waste.BalanceError};
    }
    static CityCoreIndustrySession Restore(byte[] bytes)
    {var s=CityCoreSave.Read(bytes);s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);s.Validate();return s;}
    static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
    sealed class BuildObservation
    {
        readonly string output;readonly Stopwatch timer=Stopwatch.StartNew();readonly long allocated=GC.GetAllocatedBytesForCurrentThread();
        public CityCoreIndustrySession Latest;public string Phase;
        public BuildObservation(string output){this.output=output;}
        public void Observe(string phase,CityCoreIndustrySession session)
        {
            Latest=session;Phase=phase;
            File.AppendAllText(Path.Combine(output,"build-timeline.jsonl"),Json.Serialize(new{phase,ms=timer.Elapsed.TotalMilliseconds,allocated=GC.GetAllocatedBytesForCurrentThread()-allocated,roads=session.Transport.Network.RoadCount,buildings=session.Entities.Buildings.Count,residents=session.Entities.Members.Count})+Environment.NewLine);
            Console.WriteLine("Build "+phase+" buildings "+session.Entities.Buildings.Count+" residents "+session.Entities.Members.Count);
        }
    }
    static int Main(string[] args)
    {
        string output=args.Length>1?args[1]:"Temp/CityScenarios/latest";Directory.CreateDirectory(output);CityCoreIndustrySession s=null;CityWasteDiagnostics wasteDiagnostic=null;var wall=Stopwatch.StartNew();var construction=new BuildObservation(output);
        try
        {
            var settings=Json.Deserialize<CityScenarioSettings>(File.ReadAllText(args[0]));settings.Validate();
            Write(Path.Combine(output,"settings.json"),settings);
            bool profile=args.Length>2&&args[2]=="profile";
            wasteDiagnostic=args.Length>5&&args[5]=="waste"?new CityWasteDiagnostics():null;
            var design=Json.Deserialize<GameDesignData>(File.ReadAllText(args[4]));
            bool loading=args[3]!="-";
            long initAt=Stopwatch.GetTimestamp(),initBytes=GC.GetAllocatedBytesForCurrentThread();
            s=loading?Restore(File.ReadAllBytes(args[3])):CityScenarioBuilder.Create(settings,design,construction.Observe);
            Write(Path.Combine(output,"initialization.json"),new{loading,ms=(Stopwatch.GetTimestamp()-initAt)*1000.0/Stopwatch.Frequency,allocated=GC.GetAllocatedBytesForCurrentThread()-initBytes});
            Require(s.Entities.Members.Count==settings.population,"Loaded population does not match declared scenario");
            Write(Path.Combine(output,"effective-design.json"),s.Simulation.Design.Snapshot());
            byte[] initial=CityCoreSave.Write(s);File.WriteAllBytes(Path.Combine(output,"initial.hcity"),initial);
            if(!loading){var repeated=CityCoreSave.Write(CityScenarioBuilder.Create(settings,design,construction.Observe));Require(initial.SequenceEqual(repeated),"Same seed/settings did not reproduce initial city");}
            // All timed runs start from the same saved facts and cold derived caches,
            // whether constructed just now or supplied by --Load.
            s=Restore(initial);
            s.Simulation.Waste.Diagnostics=wasteDiagnostic;
            long initialCollected=s.Simulation.Waste.Collected,initialProcessed=s.Simulation.Waste.Processed,initialInTransit=s.Simulation.Waste.InTransit;
            var initialState=State(s);Write(Path.Combine(output,"initial-state.json"),initialState);
            int steps=checked((int)Math.Round(settings.days*s.City.society.settings.secondsPerDay/SimulationClock.StepSeconds));
            int warmup=(int)Math.Round(settings.warmupDays*s.City.society.settings.secondsPerDay/SimulationClock.StepSeconds);
            var samples=new double[steps];var bytesPerStep=new long[steps];var daily=new List<double>();var steady=new List<double>();var postRestore=new List<double>();var warnings=new HashSet<string>();
            long initialTick=s.Simulation.Tick,routePreparations=0;int peak=0,day=s.City.day,g0=GC.CollectionCount(0),g1=GC.CollectionCount(1),g2=GC.CollectionCount(2);double validationMs=0,restoreMs=0;bool replayChecked=false,everWorking=false;string replayHash=null;
            var observedPurposes=new HashSet<TransportPurpose>();
            var stepCollections=new long[3];
            var previousTicks=new long[CityCoreProfile.Ticks.Length];var previousCalls=new long[previousTicks.Length];var previousBytes=new long[previousTicks.Length];
            CityCoreProfile.Reset();CityCoreProfile.Enabled=profile;
            using(var timeline=new StreamWriter(Path.Combine(output,"timeline.jsonl")))
            using(var spikes=new StreamWriter(Path.Combine(output,"profile-spikes.jsonl")))
            using(var csv=new StreamWriter(Path.Combine(output,"steps.csv")))
            {
                long flushAt=Stopwatch.GetTimestamp(),progressAt=flushAt;
                csv.WriteLine("tick,step_ms,allocated_bytes,active,day_boundary,phase,gc0_host_cumulative,gc1_host_cumulative,gc2_host_cumulative");
                for(int i=0;i<steps;i++)
                {
                    int cg0=GC.CollectionCount(0),cg1=GC.CollectionCount(1),cg2=GC.CollectionCount(2);
                    long routesBefore=s.Transport.RoutePreparations,before=GC.GetAllocatedBytesForCurrentThread(),start=Stopwatch.GetTimestamp();int advanced=s.Advance(SimulationClock.StepSeconds);long end=Stopwatch.GetTimestamp();long allocated=GC.GetAllocatedBytesForCurrentThread()-before;routePreparations+=s.Transport.RoutePreparations-routesBefore;
                    stepCollections[0]+=GC.CollectionCount(0)-cg0;stepCollections[1]+=GC.CollectionCount(1)-cg1;stepCollections[2]+=GC.CollectionCount(2)-cg2;
                    Require(advanced==1&&s.Simulation.Tick==initialTick+i+1,"Fixed step skipped or repeated");
                    double ms=(end-start)*1000.0/Stopwatch.Frequency;samples[i]=ms;bytesPerStep[i]=CityCoreProfile.AllocationAvailable?allocated:-1;peak=Math.Max(peak,s.Transport.ActiveCount);
                    bool boundary=s.City.day!=day,restoring=i>steps/2&&i<=steps/2+2400;
                    if(boundary)daily.Add(ms);if(restoring)postRestore.Add(ms);else if(i>=warmup&&!boundary)steady.Add(ms);
                    string phase=i<warmup?"warmup":restoring?"post_restore":boundary?"day_boundary":"steady";
                    csv.WriteLine(string.Join(",",s.Simulation.Tick,ms.ToString("F6",Inv),bytesPerStep[i],s.Transport.ActiveCount,boundary?1:0,phase,GC.CollectionCount(0)-g0,GC.CollectionCount(1)-g1,GC.CollectionCount(2)-g2));
                    // Real progress must reach disk even when fewer than 200 steps
                    // take minutes. Diagnostics stay outside measured model work.
                    long observedAt=Stopwatch.GetTimestamp();
                    if(observedAt-flushAt>=Stopwatch.Frequency){csv.Flush();spikes.Flush();flushAt=observedAt;}
                    if(observedAt-progressAt>=10L*Stopwatch.Frequency){Console.WriteLine("Progress tick "+s.Simulation.Tick+" active "+s.Transport.ActiveCount);progressAt=observedAt;}
                    if(profile)
                    {
                        if(ms>=10)spikes.WriteLine(Json.Serialize(new{tick=s.Simulation.Tick,ms,allocated,phase,parts=Enumerable.Range(0,previousTicks.Length).Select(k=>new{name=((CityCoreProfile.Part)k).ToString(),ms=(CityCoreProfile.Ticks[k]-previousTicks[k])*1000.0/Stopwatch.Frequency,calls=CityCoreProfile.Calls[k]-previousCalls[k],bytes=CityCoreProfile.AllocationAvailable?CityCoreProfile.Bytes[k]-previousBytes[k]:-1}).ToArray()}));
                        Array.Copy(CityCoreProfile.Ticks,previousTicks,previousTicks.Length);Array.Copy(CityCoreProfile.Calls,previousCalls,previousTicks.Length);Array.Copy(CityCoreProfile.Bytes,previousBytes,previousTicks.Length);
                    }
                    if(boundary)
                    {
                        long v=Stopwatch.GetTimestamp();s.Validate();validationMs+=(Stopwatch.GetTimestamp()-v)*1000.0/Stopwatch.Frequency;
                        day=s.City.day;Console.WriteLine("Day "+day+" population "+s.Entities.Members.Count+" active "+s.Transport.ActiveCount+" consumed "+s.City.consumption.ConsumedGoods);
                    }
                    if(boundary||i%200==0)
                    {
                        timeline.WriteLine(Json.Serialize(State(s)));timeline.Flush();
                        foreach(var t in s.Transport.Tasks)observedPurposes.Add(t.purpose);everWorking|=s.Entities.Members.Any(m=>m.Person.atWork);
                        if(s.Transport.Vehicles.Any(v=>v.status==VehicleStatus.Stranded))warnings.Add("Stranded vehicles observed; inspect timeline and save");
                        if(s.Transport.Tasks.Any(t=>s.Simulation.Seconds-t.departedAt>s.City.society.settings.secondsPerDay))warnings.Add("Tasks lasting over one game day observed; congestion/lifecycle diagnosis required");
                        if(s.Simulation.Waste.Waiting>settings.population*5)warnings.Add("Waste backlog exceeds five residential population-days; collection capacity/routing needs diagnosis, not a conservation failure");
                        if(s.City.money<0)warnings.Add("Public treasury negative; economic outcome, not automatically a state corruption");
                    }
                    if(i==steps/2)
                    {
                        // Fork a real in-flight checkpoint. Replay is outside timed steps and attribution.
                        long v=Stopwatch.GetTimestamp();CityCoreProfile.Enabled=false;s.Simulation.Waste.Diagnostics=null;
                        byte[] checkpoint=CityCoreSave.Write(s);File.WriteAllBytes(Path.Combine(output,"inflight.hcity"),checkpoint);Require(s.Transport.ActiveCount>0,"Checkpoint contains no active vehicles");
                        var a=Restore(checkpoint);var b=Restore(checkpoint);Require(CityCoreSave.Write(a).SequenceEqual(checkpoint),"Read/write checkpoint changed state");
                        for(int j=0;j<2400;j++){a.Advance(.05);b.Advance(.05);}a.Validate();b.Validate();byte[] result=CityCoreSave.Write(a);Require(result.SequenceEqual(CityCoreSave.Write(b)),"Restored continuations differ");
                        // Also compare a live continuation, including derived cache state, with restored state.
                        var live=s;for(int j=0;j<2400;j++)live.Advance(.05);live.Validate();Require(result.SequenceEqual(CityCoreSave.Write(live)),"Live and restored continuations differ");
                        s=Restore(checkpoint);s.Simulation.Waste.Diagnostics=wasteDiagnostic;replayHash=Hash(result);replayChecked=true;restoreMs+=(Stopwatch.GetTimestamp()-v)*1000.0/Stopwatch.Frequency;CityCoreProfile.Enabled=profile;
                    }
                }
            }
            s.Validate();byte[] final=CityCoreSave.Write(s);File.WriteAllBytes(Path.Combine(output,"final.hcity"),final);Restore(final);
            Require(s.City.trade.rawDelivered>0&&s.City.consumption.ConsumedGoods>0&&s.Simulation.Waste.Processed>0&&s.Simulation.Finance.TaxesCollected>0&&s.Entities.Buildings.OfType<IndustrialBuilding>().Sum(f=>(long)f.factory.produced)>0,"Required real city activity absent; not a complete scenario pass");
            Require(everWorking&&new[]{TransportPurpose.Work,TransportPurpose.Home,TransportPurpose.Freight,TransportPurpose.Shopping,TransportPurpose.Service}.All(p=>observedPurposes.Contains(p)),"Not all required real city trip purposes/attendance observed");
            if(wasteDiagnostic!=null)
            {
                var stations=wasteDiagnostic.Snapshot();
                Require(stations.Sum(t=>t.collected)==s.Simulation.Waste.Collected-initialCollected&&stations.Sum(t=>t.processed)==s.Simulation.Waste.Processed-initialProcessed,"Waste measurement counted replay work or missed real activity");
                Require(stations.Sum(t=>t.delivered)==stations.Sum(t=>t.collected)+initialInTransit-s.Simulation.Waste.InTransit,"Waste arrival measurement disagrees with actual shipments");
                Write(Path.Combine(output,"waste-diagnostics.json"),new{peakActive=wasteDiagnostic.PeakActive,simulationSeconds=steps*.05,stations});
            }
            if(profile){File.WriteAllLines(Path.Combine(output,"attribution.csv"),new[]{CityCoreProfile.Header(),CityCoreProfile.Row(s.Simulation.Tick,s.Transport.ActiveCount,routePreparations,GC.CollectionCount(0)-g0,GC.CollectionCount(1)-g1,GC.CollectionCount(2)-g2)});}
            // GC collection counters include diagnostic/replay work; step allocation/time exclude it.
            long workingSet=Process.GetCurrentProcess().WorkingSet64;
            Write(Path.Combine(output,"report.json"),new{status="passed-invariants",mode="B seeded mature city; no proof of natural growth",rendering="headless; FPS and Unity 1x/3x unverified",profile,steps,initialTick,finalTick=s.Simulation.Tick,
                allStepMs=Distribution(samples),warmupStepMs=Distribution(samples.Take(warmup)),stableNonDayBoundaryMs=Distribution(steady),postRestoreStepMs=Distribution(postRestore),dayBoundaryMs=Distribution(daily),
                stepAllocatedBytes=CityCoreProfile.AllocationAvailable?Distribution(bytesPerStep.Select(x=>(double)x)):null,peakActiveVehicles=peak,simulationSeconds=steps*.05,
                timedSimulationMs=samples.Sum(),hostWallSeconds=wall.Elapsed.TotalSeconds,validationMs,restoreMs,
                stepCollections,gcCollectionsIncludingDiagnostics=new[]{GC.CollectionCount(0)-g0,GC.CollectionCount(1)-g1,GC.CollectionCount(2)-g2},managedMemoryBytes=GC.GetTotalMemory(false),workingSetBytes=workingSet>0?(long?)workingSet:null,
                replayChecked,replayHash,everWorking,observedPurposes=observedPurposes.Select(p=>p.ToString()).OrderBy(p=>p).ToArray(),initialHash=Hash(initial),finalHash=Hash(final),initial=initialState,final=State(s),operationalWarnings=warnings.OrderBy(w=>w).ToArray(),gcPauseMs=(object)null,
                limitations=new[]{"No Unity rendering/FPS measurement in this run","Retired task count derived from monotonic task allocator minus current store; historical failures unavailable","No explicit departure queue; waiting count unavailable","Passing invariants does not prove economically sustainable city","This run covers only the declared population/configuration; Mode A natural growth, other seeds/layouts and larger populations are not verified by this run"}});
            return 0;
        }
        catch(Exception e)
        {
            Write(Path.Combine(output,"failure.json"),new{status="failed",exception=e.ToString(),wallSeconds=wall.Elapsed.TotalSeconds,constructionPhase=construction.Phase});
            if(s!=null||construction.Latest!=null)try{File.WriteAllBytes(Path.Combine(output,"failure.hcity"),CityCoreSave.Write(s??construction.Latest));}catch(Exception save){File.WriteAllText(Path.Combine(output,"failure-save-error.txt"),save.ToString());}
            Console.Error.WriteLine(e);return 1;
        }
        finally{CityCoreProfile.Enabled=false;}
    }
}
