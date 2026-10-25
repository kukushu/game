using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using HarborCity;
using HarborCity.RoadCore;

public static class CityDemolitionWasteLossChecks
{
    static int checks;
    static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
    static void Reject(Action action,string why){bool rejected=false;try{action();}catch(InvalidOperationException){rejected=true;}Check(rejected,why);}
    static WasteServiceData Facts(CityCoreIndustrySession s)=>(WasteServiceData)typeof(CityCoreWaste).GetField("state",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(s.Simulation.Waste);
    static CityCoreIndustrySession Read(byte[] bytes){var s=CityCoreSave.Read(bytes);s.Simulation.Growth.Zoning.SetSurface((x,z)=>2);return s;}
    // Fill a fixture's store with explicitly booked prior collections, rather than
    // introducing unaccounted inventory. Exactly one real truck owns this station.
    static CityCoreIndustrySession Pending(byte[] source,bool returning,int free,out int vehicle,out int task)
    {
        var s=Read(source);var m=Facts(s).missions.Single();var v=s.Transport.FindVehicle(m.vehicle);vehicle=v.id;task=v.Task.id;
        Check((m.phase==WastePhase.Return)==returning,"Wrong actual mission phase");s.Transport.RemoveRoad(s.Transport.Network.Get(v.Current).RoadId);
        Check(v.status==VehicleStatus.AwaitingRestart,"Fixture must lose its current surface in a real deletion");
        var plant=(LandfillBuilding)s.Entities.Building(m.station);int delta=s.Simulation.Design.Waste.Capacity-free-plant.stored;Check(delta>=0,"Invalid stock fixture");plant.stored+=delta;s.City.waste.generated+=delta;s.City.waste.collected+=delta;
        s.Validate();return s;
    }
    public static string Run(string output)
    {
        checks=0;var design=new GameDesignData();design.waste.payload=3;design.waste.trucks=1;var growing=CityGrowingGridFixture.Create(design:design);CityGrowingGridFixture.PlaceWasteFacility(growing);byte[] pickup=null,returning=null;
        for(int i=0;i<12000&&(pickup==null||returning==null);i++)
        {
            growing.Advance(.05);
            foreach(var v in growing.Transport.Vehicles.Where(v=>v.Task.purpose==TransportPurpose.Service&&!growing.Transport.Network.Get(v.Current).IsConnection))
            {if(pickup==null&&v.Task.Shipment==null)pickup=CityCoreSave.Write(growing);if(returning==null&&v.Task.Shipment!=null)returning=CityCoreSave.Write(growing);}
        }
        Check(pickup!=null&&returning!=null,"No real pickup/return fixtures");Directory.CreateDirectory(output);
        var rows=new List<string>{"phase,free,original_cargo,accepted,lost_delta,collected_delta,building_delta,remaining_missions"};
        foreach(bool back in new[]{false,true})foreach(int free in new[]{4,3,2,0})
        {
            var s=Pending(back?returning:pickup,back,free,out int id,out int task);var m=Facts(s).missions.Single();var plant=(LandfillBuilding)s.Entities.Building(m.station);var target=s.Entities.Building(m.pickup);var truck=s.Transport.FindVehicle(id);
            int carried=truck.Task.Cargo,amount=back?carried:Math.Min(3,target.garbage),accepted=back?Math.Min(carried,free):amount<=free?amount:0,drop=back?carried-accepted:0;
            long collected=s.City.waste.collected,lost=s.City.waste.lost;int stored=plant.stored,garbage=target.garbage,missions=s.Simulation.Waste.Vehicles;
            var before=CityCoreSave.Write(s);var cold=Read(before);
            File.WriteAllBytes(Path.Combine(output,(back?"return":"pickup")+"-free"+free+".hcity"),before);
            Check(s.Simulation.Waste.RecoverDemolishedVehicles()==1,"Displaced full station retained the truck");
            Check(s.Transport.FindVehicle(id)==null&&s.Transport.FindTask(task)==null&&s.Transport.FindTaskVehicle(task)==null&&s.Simulation.Waste.Vehicles==missions-1,"Vehicle/task/fleet reference leaked");
            Check(truck.Task.Cargo==0&&truck.Task.state==TripState.Completed,"Cargo/task not finalized");
            Check(plant.stored-stored==accepted&&s.City.waste.lost-lost==drop,"Accepted/lost amounts wrong");
            Check(target.garbage-garbage==(back?0:-accepted)&&s.City.waste.collected-collected==(back?0:accepted),"Cancelled pickup or return collected waste twice");
            if(back)Check(accepted+drop==carried,"Original cargo disappeared without a ledger entry");
            s.Validate();Check(s.Simulation.Waste.BalanceError==0,"Loss-aware conservation failed");
            var done=CityCoreSave.Write(s);Check(s.Simulation.Waste.RecoverDemolishedVehicles()==0&&done.SequenceEqual(CityCoreSave.Write(s)),"Repeated handler settled twice");Reject(()=>s.Transport.CompleteDemolishedService(id,task,0),"Duplicate completion was accepted");
            cold.Simulation.Waste.RecoverDemolishedVehicles();Check(done.SequenceEqual(CityCoreSave.Write(cold)),"Pending save/reload settlement differs");var after=Read(done);
            for(int k=0;k<80;k++){s.Advance(.05);cold.Advance(.05);after.Advance(.05);s.Validate();Check(CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(cold))&&CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(after)),"Loss/save/fixed-step continuation differs");}
            rows.Add(string.Join(",",back?"Return":"Pickup",free,carried,accepted,drop,back?0:accepted,back?0:-accepted,missions-1));
        }
        // Existing loss persists and accumulates, never resetting on another incident.
        var accumulated=Pending(returning,true,0,out int actor,out int missionTask);accumulated.City.waste.generated+=5;accumulated.City.waste.collected+=5;accumulated.City.waste.lost=5;accumulated.Validate();int load=accumulated.Transport.FindVehicle(actor).Task.Cargo;accumulated.Simulation.Waste.RecoverDemolishedVehicles();Check(accumulated.Simulation.Waste.Lost==5+load,"Previous loss was overwritten");Read(CityCoreSave.Write(accumulated)).Validate();
        // Recorded loss is allowed; missing stock with no matching loss is not.
        var broken=Read(CityCoreSave.Write(accumulated));var store=broken.Entities.Buildings.OfType<LandfillBuilding>().Single();store.stored--;Reject(broken.Validate,"Unrecorded disappearance passed conservation");
        broken=Read(CityCoreSave.Write(accumulated));broken.City.waste.lost=-1;Reject(broken.Validate,"Negative loss accepted");
        broken=Read(CityCoreSave.Write(accumulated));broken.City.waste.lost=broken.City.waste.collected+1;Reject(broken.Validate,"Loss greater than collected accepted");
        // Corrupt references/states are hard errors before any recovery writes.
        foreach(string fault in new[]{"task","phase","payload","state","vehicle","inventory"})
        {
            broken=Pending(returning,true,0,out actor,out missionTask);var m=Facts(broken).missions.Single();var v=broken.Transport.FindVehicle(actor);var p=(LandfillBuilding)broken.Entities.Building(m.station);
            if(fault=="task")m.task++;
            if(fault=="phase")m.phase=(WastePhase)99;
            if(fault=="payload")v.Task.Shipment.kind=CargoKind.Goods;
            if(fault=="state")v.Task.state=TripState.Completed;
            if(fault=="vehicle")m.vehicle=int.MaxValue;
            if(fault=="inventory")p.stored++;
            var raw=SaveCodec.Write(broken.Transport);long oldLost=broken.City.waste.lost,oldCollected=broken.City.waste.collected;int oldStored=p.stored;
            var city=broken;Reject(()=>city.Simulation.Waste.RecoverDemolishedVehicles(),"Corruption silently forgiven: "+fault);
            Check(raw.SequenceEqual(SaveCodec.Write(broken.Transport))&&p.stored==oldStored&&broken.City.waste.lost==oldLost&&broken.City.waste.collected==oldCollected,"Corrupt mission partially settled: "+fault);
        }
        broken=Pending(returning,true,0,out actor,out missionTask);Facts(broken).missions.Add(Facts(broken).missions.Single());Reject(broken.Validate,"Duplicate mission accepted");var duplicate=broken;Reject(()=>duplicate.Simulation.Waste.RecoverDemolishedVehicles(),"Duplicate mission silently settled");
        // Other stranded reasons and normal fleet reservations are not exceptions.
        broken=Read(returning);var normal=broken.Transport.Vehicles.Single(v=>v.Task.purpose==TransportPurpose.Service);var normalPlant=(LandfillBuilding)broken.Entities.Building(Facts(broken).missions.Single().station);int fill=broken.Simulation.Design.Waste.Capacity-normalPlant.stored;normalPlant.stored+=fill;broken.City.waste.generated+=fill;broken.City.waste.collected+=fill;
        Check(broken.Simulation.Waste.RecoverDemolishedVehicles()==0&&normal.Task.Cargo>0&&broken.Simulation.Waste.Lost==0,"Ordinary truck entered demolition fallback");Reject(broken.Validate,"Normal incoming reservation was disabled");normal.Freeze(broken.Transport.Position(normal));broken.Transport.AssertSafety();Check(broken.Simulation.Waste.RecoverDemolishedVehicles()==0&&normal.Task.Cargo>0&&broken.Simulation.Waste.Lost==0,"Other stranded reason dropped cargo");Reject(broken.Validate,"Other stranded reservation was disabled");
        File.WriteAllLines(Path.Combine(output,"cases.csv"),rows);return "PASS "+checks+" demolition waste loss checks: cancel, partial/full loss, conservation, lifecycle, cold restore and corruption rejection";
    }
}
