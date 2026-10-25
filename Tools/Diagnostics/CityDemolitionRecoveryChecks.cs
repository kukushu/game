using System;
using System.IO;
using System.Linq;
using HarborCity;
using HarborCity.RoadCore;

public static class CityDemolitionRecoveryChecks
{
    static int checks;
    static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
    static void Same(World a,World b,string why)=>Check(SaveCodec.Write(a).SequenceEqual(SaveCodec.Write(b)),why);
    static World Freight(int count,out int road,out int origin,out int goal,int lanes=1)
    {
        var w=new World();int a=w.AddJunction(new Point()),b=w.AddJunction(new Point(80,0,0)),c=w.AddJunction(new Point(2080,0,0)),d=w.AddJunction(new Point(2160,0,0)),e=w.AddJunction(new Point(1080,0,300));
        w.AddRoad(a,b,new[]{w.JunctionPosition(a),w.JunctionPosition(b)},lanes:lanes);road=w.AddRoad(b,c,new[]{w.JunctionPosition(b),w.JunctionPosition(c)},lanes:lanes);w.AddRoad(c,d,new[]{w.JunctionPosition(c),w.JunctionPosition(d)},lanes:lanes);
        w.AddRoad(b,e,new[]{w.JunctionPosition(b),w.JunctionPosition(e)},lanes:lanes);w.AddRoad(e,c,new[]{w.JunctionPosition(e),w.JunctionPosition(c)},lanes:lanes);
        origin=w.AddDepot(a,100);goal=w.AddDepot(d,0);
        for(int i=0;i<count;i++){Check(w.Dispatch(origin,goal,2,7)!=null,"Fixture departure");w.Advance(.3);}w.Advance(10);
        int deletedRoad=road;Check(w.Vehicles.All(v=>w.Network.Get(v.Current).RoadId==deletedRoad),"Fixture actors must actually be on deleted pavement");return w;
    }
    static void FreightTests()
    {
        int road,origin,goal;var w=Freight(5,out road,out origin,out goal);var tasks=w.Vehicles.ToDictionary(v=>v.id,v=>v.Task);var positions=w.Vehicles.ToDictionary(v=>v.id,v=>w.Position(v));int stock=w.GetDepot(origin).stock;double cash=w.GetDepot(origin).cash;
        w.RemoveRoad(road);Check(w.Vehicles.All(v=>v.status==VehicleStatus.AwaitingRestart),"Only demolished current surfaces should be queued");Check(w.ActiveCount==0,"Queued tasks count as road traffic");
        foreach(var v in w.Vehicles)Check(ReferenceEquals(v.Task,tasks[v.id])&&v.Task.Cargo==2&&Point.Distance(positions[v.id],w.Position(v))<1e-8,"Demolition changed task/cargo before restart");
        w.AssertSafety();var cold=SaveCodec.Read(SaveCodec.Write(w));Check(cold.Vehicles.All(v=>v.status==VehicleStatus.AwaitingRestart),"Cold restore lost pending reason");int restarted=0;
        for(int i=0;i<180;i++){w.Advance(.05);cold.Advance(.05);w.Validate();w.AssertSafety();Same(w,cold,"Queued/hot/cold restart order differs");restarted=Math.Max(restarted,w.ActiveCount);}
        Check(restarted==5&&w.Vehicles.All(v=>v.status==VehicleStatus.Active),"Shared source did not safely release every original task");Check(w.GetDepot(origin).stock==stock&&w.GetDepot(origin).cash==cash,"Restart debited stock or paid again");
        foreach(var v in w.Vehicles)Check(ReferenceEquals(v.Task,tasks[v.id])&&v.Task.departedAt==tasks[v.id].departedAt&&v.Task.Cargo==2,"Restart replaced business task");
        w.Advance(300);cold.Advance(300);Same(w,cold,"Restarted delivery cold continuation differs");Check(w.Delivered==10&&w.GetDepot(goal).stock==10&&w.GetDepot(origin).stock==90&&w.GetDepot(goal).cash==99930&&w.GetDepot(origin).cash==100070,"Freight did not deliver/pay exactly once");
        // A future broken route retains a real physical surface: original in-place recovery only.
        w=Freight(1,out road,out origin,out goal);var car=w.Vehicles.Single();var pose=w.Position(car);w.RemoveRoad(3);Check(car.status==VehicleStatus.Stranded&&!w.HasDemolishedVehicles,"Future-only interruption incorrectly queued");w.Advance(1);Check(car.status==VehicleStatus.Stranded&&Point.Distance(pose,w.Position(car))<1e-8,"Other stranded cause was relocated");
        // A rejected demolition must not leave queued actors or partial state.
        w=Freight(2,out road,out origin,out goal);w.Vehicles.Last().Task.Shipment.remaining=-1;var before=SaveCodec.Write(w);bool failed=false;try{w.RemoveRoad(road);}catch(ArgumentException){failed=true;}Check(failed&&before.SequenceEqual(SaveCodec.Write(w)),"Failed demolition partially queued tasks");
        w=Freight(1,out road,out origin,out goal,2);car=w.Vehicles.Single();Check(w.TryChangeLane(car.id,1-w.Network.Get(car.Current).LaneIndex),"Moving-lane deletion fixture");w.Advance(.1);w.RemoveRoad(road);Check(car.status==VehicleStatus.AwaitingRestart&&car.changeTarget==0&&car.changeElapsed==0,"Removed lane retained its changing occupancy");w.Advance(2);w.Validate();w.AssertSafety();Check(car.status==VehicleStatus.Active&&car.Task.Cargo==2,"Lane-changing task failed normal safe restart");
        // Removing an incident road can remove the connection under the actor too.
        w=Freight(1,out road,out origin,out goal);car=w.Vehicles.Single();for(int i=0;i<5000&&!w.Network.Get(car.Current).IsConnection;i++)w.Advance(.05);Check(w.Network.Get(car.Current).IsConnection,"Connection deletion fixture");w.RemoveRoad(road);Check(car.status==VehicleStatus.AwaitingRestart,"Removed current connection did not enter demolition queue");w.Advance(2);w.Validate();w.AssertSafety();Check(car.status==VehicleStatus.Active,"Removed connection task failed restart");
        Console.WriteLine("PASS freight/shared-source/unchanged stranded/atomic demolition");
    }
    static void ResidentTests()
    {
        var s=CityCoreIndustrySession.Create();s.Advance(.05);var p=s.City.Citizens.First(c=>c.tripId>0);var v=s.Transport.FindTaskVehicle(p.tripId);var task=v.Task;int id=v.id;
        s.Transport.RemoveRoad(1);Check(v.status==VehicleStatus.AwaitingRestart&&p.tripId==task.id&&p.location==-1&&!p.atWork,"Resident task detached during demolition");s.Advance(1);Check(v.status==VehicleStatus.AwaitingRestart,"Invalid origin incorrectly restarted");
        var cold=CityCoreSave.Read(CityCoreSave.Write(s));int rebuilt=s.Transport.AddRoad(1,2,new[]{s.Transport.JunctionPosition(1),new Point(-18,0,0),s.Transport.JunctionPosition(2)},lanes:2);cold.Transport.AddRoad(1,2,new[]{cold.Transport.JunctionPosition(1),new Point(-18,0,0),cold.Transport.JunctionPosition(2)},lanes:2);
        for(int i=0;i<80;i++){s.Advance(.05);cold.Advance(.05);s.Validate();Check(CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(cold)),"Resident retry/cold continuation differs");}
        Check(s.Transport.FindVehicle(id)==null&&p.tripId==0&&p.atWork&&p.location==1,"Resident did not complete original commute after rebind");Check(task.citizenId==p.id&&task.state==TripState.Completed,"Resident task/citizen identity changed");
        // Endpoint closure is deliberately outside the demolition exception.
        s=CityCoreIndustrySession.Create();s.Advance(.05);v=s.Transport.Vehicles.First();s.Entities.SetAvailability(1,true,false);s.Advance(.05);Check(v.status==VehicleStatus.Stranded,"Closed workplace incorrectly re-dispatched");
        Console.WriteLine("PASS resident same task, unavailable origin, rebind, closure isolation");
    }
    static void WasteTests(string output)
    {
        var rules=new GameDesignData();rules.waste.payload=3;rules.waste.trucks=2;var s=CityGrowingGridFixture.Create(design:rules);CityGrowingGridFixture.PlaceWasteFacility(s);byte[] pickup=null,returning=null;
        for(int i=0;i<12000&&(pickup==null||returning==null);i++)
        {
            s.Advance(.05);
            foreach(var v in s.Transport.Vehicles.Where(v=>v.Task.purpose==TransportPurpose.Service&&!s.Transport.Network.Get(v.Current).IsConnection))
            {if(pickup==null&&v.Task.Shipment==null)pickup=CityCoreSave.Write(s);if(returning==null&&v.Task.Shipment!=null)returning=CityCoreSave.Write(s);}
        }
        Check(pickup!=null&&returning!=null,"Actual growing city did not generate both service phases");
        foreach(var bytes in new[]{pickup,returning})
        {
            s=CityCoreSave.Read(bytes);s.Simulation.Growth.Zoning.SetSurface((x,z)=>2);bool loaded=ReferenceEquals(bytes,returning);var v=s.Transport.Vehicles.First(v=>v.Task.purpose==TransportPurpose.Service&&(v.Task.Shipment!=null)==loaded&&!s.Transport.Network.Get(v.Current).IsConnection);
            int id=v.id,road=s.Transport.Network.Get(v.Current).RoadId;var missions=s.Simulation.Waste.Snapshot().missions.ToDictionary(m=>m.vehicle);long waiting=s.Simulation.Waste.Waiting,stored=s.Simulation.Waste.Stored,transit=s.Simulation.Waste.InTransit,collected=s.Simulation.Waste.Collected;int expectedCollected=0,expectedStored=0;
            s.Transport.RemoveRoad(road);Check(v.status==VehicleStatus.AwaitingRestart,"Service on demolished road was not queued");
            foreach(var actor in s.Transport.Vehicles.Where(c=>c.status==VehicleStatus.AwaitingRestart&&c.Task.purpose==TransportPurpose.Service)){var m=missions[actor.id];int amount=m.phase==WastePhase.Pickup?Math.Min(3,s.Entities.Building(m.pickup).garbage):actor.Task.Cargo;expectedStored+=amount;if(m.phase==WastePhase.Pickup)expectedCollected+=amount;}
            var cold=CityCoreSave.Read(CityCoreSave.Write(s));cold.Simulation.Growth.Zoning.SetSurface((x,z)=>2);
            int n=s.Simulation.Waste.RecoverDemolishedVehicles();Check(n>0&&s.Transport.FindVehicle(id)==null,"Service not returned to fleet");Check(s.Simulation.Waste.Collected==collected+expectedCollected&&s.Simulation.Waste.Waiting==waiting-expectedCollected&&s.Simulation.Waste.Stored==stored+expectedStored&&s.Simulation.Waste.BalanceError==0,"Phase settlement broke waste conservation");Check(s.Simulation.Waste.RecoverDemolishedVehicles()==0,"Service settled twice");
            cold.Simulation.Waste.RecoverDemolishedVehicles();s.Validate();cold.Validate();Check(CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(cold)),"Cold garbage settlement differs");
            for(int i=0;i<100;i++){s.Advance(.05);cold.Advance(.05);s.Validate();Check(s.Simulation.Waste.BalanceError==0&&CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(cold)),"Waste continuing simulation/cold state differs");}
            Console.WriteLine("PASS actual waste "+(loaded?"return":"pickup")+" direct settlement, fleet, conservation, save/continue");
        }
        if(output!=null){Directory.CreateDirectory(output);File.WriteAllBytes(Path.Combine(output,"waste-pickup.hcity"),pickup);File.WriteAllBytes(Path.Combine(output,"waste-return.hcity"),returning);}
    }
    public static string Run(string output=null){checks=0;FreightTests();ResidentTests();WasteTests(output);return "PASS "+checks+" demolition recovery checks";}
    public static void Main(string[] args){Console.WriteLine(Run(args.Length>0?args[0]:null));Console.WriteLine(CityDemolitionWasteLossChecks.Run(Path.Combine(args.Length>0?args[0]:"Temp/DemolitionRecovery-20261025/checks","loss")));}
}
