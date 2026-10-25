using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using UnityEngine;
using Unity.Profiling;
using HarborCity.RoadCore;
namespace HarborCity
{
    // Disposable native Player probe. Assertions/serialization stay outside edit frames.
    public sealed class HarborCityDemolitionWindowProbe:MonoBehaviour
    {
        HarborCityRoadCoreGame host;string input,output;
        public void Configure(HarborCityRoadCoreGame h,string path,string result,float rate){host=h;input=path;output=result;host.ScenarioMeasuring=true;StartCoroutine(Run());}
        static string N(double value)=>value.ToString("R",CultureInfo.InvariantCulture);
        IEnumerator Run()
        {
            Directory.CreateDirectory(output);QualitySettings.vSyncCount=0;Application.targetFrameRate=60;host.ScenarioRate(0);yield return null;
            double warm=Time.realtimeSinceStartupAsDouble;while(Time.realtimeSinceStartupAsDouble-warm<8)yield return null;
            var s=host.ScenarioSession;var w=s.Transport;w.DepotLaneRoutingEnabled=true;var args=Environment.GetCommandLineArgs();int ix=Array.IndexOf(args,"-harbor-recovery-road"),road=ix<0?87:int.Parse(args[ix+1]);ix=Array.IndexOf(args,"-harbor-recovery-scenario");string scenario=ix<0?"demolition":args[ix+1];
            if(scenario=="waste-loss"){yield return RunLoss(s);yield break;}
            if(scenario.StartsWith("service-")){bool returning=scenario=="service-return";var actor=w.Vehicles.First(v=>v.Task.purpose==TransportPurpose.Service&&(v.Task.Shipment!=null)==returning&&!w.Network.Get(v.Current).IsConnection);road=w.Network.Get(actor.Current).RoadId;}
            File.WriteAllBytes(Path.Combine(output,"before.hcity"),CityCoreSave.Write(s));
            var taskIds=w.Vehicles.ToDictionary(v=>v.id,v=>v.Task.id);var cargo=w.Vehicles.ToDictionary(v=>v.id,v=>v.Task.Cargo);
            var phases=s.Simulation.Waste.Snapshot().missions.ToDictionary(m=>m.vehicle,m=>m.phase);Vehicle[] pending;
            var frames=new List<string>{"frame_ms,gc_bytes"};double deleteMs,bindingMs,viewMs;var history=new RoadEditHistory(w);
            using(var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame"))
            {
                yield return null;double last=Time.realtimeSinceStartupAsDouble;var timer=Stopwatch.StartNew();history.Execute(()=>s.Construction.RemoveRoad(road));timer.Stop();deleteMs=timer.Elapsed.TotalMilliseconds;
                pending=w.Vehicles.Where(v=>v.status==VehicleStatus.AwaitingRestart).ToArray();timer.Restart();new CityCoreRoadBindings(s.City,w).RefreshIfChanged();timer.Stop();bindingMs=timer.Elapsed.TotalMilliseconds;timer.Restart();host.ScenarioRefreshRoads();timer.Stop();viewMs=timer.Elapsed.TotalMilliseconds;
                for(int i=0;i<12;i++){yield return null;double now=Time.realtimeSinceStartupAsDouble;frames.Add(N((now-last)*1000)+","+(gc.Valid?gc.LastValue:-1));last=now;}
            }
            File.WriteAllLines(Path.Combine(output,"frames.csv"),frames);if(scenario=="demolition-repair"&&!history.Undo())throw new Exception("Failed to restore original road for restart acceptance");s.Validate();var checkpoint=CityCoreSave.Write(s);File.WriteAllBytes(Path.Combine(output,"pending.hcity"),checkpoint);
            var cold=CityCoreSave.Read(checkpoint);cold.Simulation.Growth.Zoning.SetSurface(host.ScenarioSurface);cold.Transport.DepotLaneRoutingEnabled=true;
            int restarted=0,settled=0;var rows=new List<string>{"vehicle,purpose,phase,task,cargo,final_status,final_task"};var seen=new HashSet<int>();
            var timer2=Stopwatch.StartNew();
            for(int i=0;i<200;i++)
            {
                s.Simulation.Advance(.05,1,double.PositiveInfinity);cold.Simulation.Advance(.05,1,double.PositiveInfinity);
                foreach(var v in pending)
                {
                    var current=w.FindVehicle(v.id);if(current!=null&&current.status==VehicleStatus.Active&&seen.Add(v.id)){if(current.Task.id!=taskIds[v.id]||current.Task.Cargo!=cargo[v.id])throw new Exception("Restart replaced task or cargo");restarted++;}
                }
                if(i%20==0){s.Validate();cold.Validate();if(!CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(cold)))throw new Exception("Native hot/cold demolition continuation differs");}
            }
            timer2.Stop();
            foreach(var v in pending){var current=w.FindVehicle(v.id);if(v.Task.purpose==TransportPurpose.Service){if(current!=null)throw new Exception("Displaced service mission was not returned to fleet");settled++;}rows.Add(string.Join(",",v.id,v.Task.purpose,phases.TryGetValue(v.id,out var phase)?phase.ToString():"",taskIds[v.id],cargo[v.id],current==null?"retired":current.status.ToString(),current?.Task.id??0));}
            s.Validate();cold.Validate();if(!CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(cold))||s.Simulation.Waste.BalanceError!=0)throw new Exception("Native final state/conservation differs");
            File.WriteAllBytes(Path.Combine(output,"final.hcity"),CityCoreSave.Write(s));File.WriteAllLines(Path.Combine(output,"vehicles.csv"),rows);
            File.WriteAllText(Path.Combine(output,"summary.csv"),"scenario,road,pending,restarted,settled_services,remaining_pending,delete_ms,bindings_ms,views_ms,paired_200_steps_with_assertions_ms\n"+string.Join(",",scenario,road,pending.Length,restarted,settled,w.Vehicles.Count(v=>v.status==VehicleStatus.AwaitingRestart),N(deleteMs),N(bindingMs),N(viewMs),N(timer2.Elapsed.TotalMilliseconds)));
            File.WriteAllText(Path.Combine(output,"Result.txt"),"PASS native production demolition, same-task restarts, service fleet release, full validation, conservation and 200 hot/cold fixed steps. Scripted paused edit; assertion-heavy continuation is not a performance measurement.\n");Application.Quit(0);
        }
        IEnumerator RunLoss(CityCoreIndustrySession s)
        {
            var waste=s.Simulation.Waste;var w=s.Transport;var pending=waste.Snapshot().missions.Where(m=>w.FindVehicle(m.vehicle)?.status==VehicleStatus.AwaitingRestart).ToArray();
            if(pending.Length!=1)throw new Exception("Loss acceptance fixture requires one actual displaced service mission");
            var m=pending[0];int cargo=w.FindVehicle(m.vehicle).Task.Cargo;var plant=(LandfillBuilding)s.Entities.Building(m.station);var target=s.Entities.Building(m.pickup);int building=target.garbage,free=s.Simulation.Design.Waste.Capacity-plant.stored;
            int amount=m.phase==WastePhase.Pickup?Math.Min(s.Simulation.Design.Waste.Payload,building):cargo;
            int accepted=m.phase==WastePhase.Pickup?(amount<=free?amount:0):Math.Min(amount,free),drop=m.phase==WastePhase.Return?cargo-accepted:0;
            long collected=waste.Collected,lost=waste.Lost,stored=waste.Stored,processed=waste.Processed;var before=CityCoreSave.Write(s);File.WriteAllBytes(Path.Combine(output,"pending.hcity"),before);
            var cold=CityCoreSave.Read(before);cold.Transport.DepotLaneRoutingEnabled=true;cold.Simulation.Growth.Zoning.SetSurface(host.ScenarioSurface);
            s.Simulation.Advance(.05,1,double.PositiveInfinity);cold.Simulation.Advance(.05,1,double.PositiveInfinity);
            if(w.FindVehicle(m.vehicle)!=null||w.FindTask(m.task)!=null||waste.Lost-lost!=drop||waste.Collected-collected!=(m.phase==WastePhase.Pickup?accepted:0)||target.garbage-building!=(m.phase==WastePhase.Pickup?-accepted:0)||waste.Stored+waste.Processed-stored-processed!=accepted||waste.BalanceError!=0)throw new Exception("Native loss/cancellation accounting or retirement failed");
            s.Validate();cold.Validate();var checkpoint=CityCoreSave.Write(s);if(!checkpoint.SequenceEqual(CityCoreSave.Write(cold)))throw new Exception("Pending cold loss settlement differs");
            var after=CityCoreSave.Read(checkpoint);after.Transport.DepotLaneRoutingEnabled=true;after.Simulation.Growth.Zoning.SetSurface(host.ScenarioSurface);
            for(int i=0;i<80;i++){s.Simulation.Advance(.05,1,double.PositiveInfinity);cold.Simulation.Advance(.05,1,double.PositiveInfinity);after.Simulation.Advance(.05,1,double.PositiveInfinity);if(i%20==0){s.Validate();if(!CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(cold))||!CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(after)))throw new Exception("Loss continuation differs after restore");}}
            s.Validate();cold.Validate();after.Validate();File.WriteAllBytes(Path.Combine(output,"final.hcity"),CityCoreSave.Write(s));
            File.WriteAllText(Path.Combine(output,"summary.csv"),"phase,free,cargo,accepted,lost_delta,collected_delta,building_delta,completed_tasks\n"+string.Join(",",m.phase,free,cargo,accepted,drop,m.phase==WastePhase.Pickup?accepted:0,m.phase==WastePhase.Pickup?-accepted:0,1));
            File.WriteAllText(Path.Combine(output,"Result.txt"),"PASS native cancelled/partial/full demolition waste settlement, task retirement, exact lost ledger, conservation and 80 steps of pending/settled cold restoration. Functional fixture, not a performance benchmark.\n");yield return null;Application.Quit(0);
        }
    }
}
