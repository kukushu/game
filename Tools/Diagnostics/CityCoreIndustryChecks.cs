using System;
using System.Linq;
using HarborCity;
using HarborCity.RoadCore;

public static class CityCoreIndustryChecks
{
    static int checks;
    static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
    public static void Main(string[] args)
    {
        ReachabilityTests();
        FreightCandidateTests();
        var session=CityCoreIndustrySession.Create(startAtWork:true);var city=session.City;var factory=city.Factory(1);double opening=factory.cash;
        var snapshot=session.SnapshotBusiness();snapshot.buildings[0].industrial[0].cash=0;snapshot.society.families[0].people[0].location=-1;Check(factory.cash==opening&&city.Citizens.First().location==1,"Business snapshot leaked live state");
        Check(city.roads.edges.Count==0&&city.traffic.trips.Count==0,"Legacy engine/network used");Check(session.Transport.Snapshot().depots.Count==0,"Duplicate city inventory");
        Check(session.Transport.Network.JunctionCount==3&&session.Transport.GetAccess(1).distance==10&&session.Transport.GetAccess(2).distance==25,"Industry still uses per-building junctions");
        session.Advance(2);Check(factory.produced>0&&factory.productiveMinutes>0,"No real labour production");Check(session.Transport.ActiveCount>0&&factory.shipped>0,"No business dispatch");Check(city.Goods(2)==0&&factory.sold==0,"Goods teleported / early revenue");var actor=session.Transport.Vehicles.First();Point pose=session.Transport.Position(actor);session.Advance(.5);Check(Point.Distance(pose,session.Transport.Position(actor))>1,"No actual new-core movement");
        var restored=CityCoreSave.Read(CityCoreSave.Write(session));for(int i=0;i<220;i++){session.Advance(.05);restored.Advance(.05);session.Validate();Check(Convert.ToBase64String(CityCoreSave.Write(session))==Convert.ToBase64String(CityCoreSave.Write(restored)),"City/traffic saved continuation differs");}
        Check(factory.sold==session.Transport.Delivered&&factory.sold==city.Goods(2),"Inventory/delivery ledger mismatch");Check(factory.salesRevenue==city.GetBuilding(2).AsShop().wholesaleCosts,"Buyer/seller money mismatch");Check(Math.Abs(factory.CashError)<1e-6&&Math.Abs(factory.cash-(opening+factory.salesRevenue-factory.wageCosts-factory.productionCosts))<1e-8,"Financial conservation");Check(factory.produced==city.Goods(1)+city.Goods(2)+session.Transport.Vehicles.Sum(v=>v.cargo),"Goods conservation");
        var blocked=CityCoreIndustrySession.Create(startAtWork:true);blocked.City.Inventory(2).Stock=31;blocked.Advance(2);var waiting=blocked.Transport.Vehicles.First();blocked.City.Inventory(2).Stock=32;blocked.Advance(9);Check(waiting.cargo>0&&waiting.status==VehicleStatus.Active&&blocked.Transport.Delivered==0,"Full target dropped cargo");blocked.City.Inventory(2).Stock=31;blocked.Advance(.1);Check(blocked.Transport.Delivered==1&&waiting.cargo==0,"Unloading did not resume");double sales=blocked.City.Factory(1).salesRevenue;blocked.Advance(.1);Check(blocked.City.Factory(1).salesRevenue==sales,"Duplicate payment");
        var disconnected=CityCoreIndustrySession.Create(startAtWork:true);disconnected.Transport.RemoveRoad(2);disconnected.Advance(2);Check(disconnected.Transport.ActiveCount==0&&disconnected.City.Goods(1)>0&&disconnected.City.Factory(1).salesRevenue==0,"Unreachable request debited stock");
        var interrupted=CityCoreIndustrySession.Create(startAtWork:true);interrupted.Advance(2);var task=interrupted.Transport.Vehicles.First();int cargo=task.cargo;Point before=interrupted.Transport.Position(task);interrupted.Transport.RemoveRoad(1);interrupted.Advance(.05);Check(task.status==VehicleStatus.Stranded&&task.cargo==cargo&&Point.Distance(before,interrupted.Transport.Position(task))<1e-8,"Road interruption lost cargo/position");var loaded=CityCoreSave.Read(CityCoreSave.Write(interrupted));Check(loaded.Transport.Vehicles.Sum(v=>v.cargo)==interrupted.Transport.Vehicles.Sum(v=>v.cargo),"Interrupted saved cargo lost");
        var empty=CityCoreIndustrySession.Create(startAtWork:true);foreach(var worker in empty.City.Citizens)worker.atWork=false;empty.Advance(3);Check(empty.City.Factory(1).produced==0&&empty.Transport.ActiveCount==0,"Jobs alone produced goods");
        var gone=CityCoreIndustrySession.Create(startAtWork:true);gone.Advance(2);var goneTask=gone.Transport.Vehicles.First();gone.CloseBuilding(2);gone.Advance(.05);Check(goneTask.status==VehicleStatus.Stranded&&goneTask.cargo>0,"Deleted destination lost task");CityCoreSave.Read(CityCoreSave.Write(gone)).Validate();
        var closed=CityCoreIndustrySession.Create(startAtWork:true);closed.Advance(2);int carrying=closed.Transport.Vehicles.Sum(v=>v.cargo);double companyCash=closed.City.Factory(1).cash;closed.CloseBuilding(1);Check(closed.Transport.ActiveCount==0&&closed.Transport.Vehicles.Sum(v=>v.cargo)==carrying,"Closed source lost cargo");var closedLoad=CityCoreSave.Read(CityCoreSave.Write(closed));closedLoad.Advance(.1);Check(closedLoad.SnapshotBusiness().retiredBuildings[0].industrial[0].cash==companyCash,"Closed business finance lost on load");closedLoad.Validate();
        var editing=CityCoreIndustrySession.Create(startAtWork:true);editing.Advance(2);var moving=editing.Transport.Vehicles.First();Point exact=editing.Transport.Position(moving);editing.Transport.SplitRoadAt(1,12.75);editing.Transport.SplitRoadAt(2,20.25);Check(Point.Distance(exact,editing.Transport.Position(moving))<1e-8,"Industrial access edits moved vehicle");var editingLoad=CityCoreSave.Read(CityCoreSave.Write(editing));for(int i=0;i<240;i++){editing.Advance(.05);editingLoad.Advance(.05);}editing.Validate();Check(editing.Transport.Delivered>0&&Convert.ToBase64String(CityCoreSave.Write(editing))==Convert.ToBase64String(CityCoreSave.Write(editingLoad)),"Industry split-access delivery/save failed");
        ResidentTests();R1Tests();
        Console.WriteLine("PASS "+checks+" integrated industry/resident checks; original labour/inventory/finance, real RoadCore commuting, exact new city save, interruption and full unloading.");
        if(args.Length>0)
        {
            var tested=CityCoreIndustrySession.Create(startAtWork:true);for(int i=0;i<50;i++)tested.Advance(.05);var samples=new double[300];int min=int.MaxValue,max=0;long allocated=GC.GetAllocatedBytesForCurrentThread();var clock=System.Diagnostics.Stopwatch.StartNew();var timer=new System.Diagnostics.Stopwatch();
            for(int i=0;i<samples.Length;i++){timer.Restart();tested.Advance(.05);timer.Stop();samples[i]=timer.Elapsed.TotalMilliseconds;min=Math.Min(min,tested.Transport.ActiveCount);max=Math.Max(max,tested.Transport.ActiveCount);}clock.Stop();allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;tested.Validate();double sum=samples.Sum();Array.Sort(samples);
            string row=string.Join(",",min,max,tested.Transport.Network.RoadCount,tested.Transport.Network.JunctionCount,4,300,(sum/300).ToString("F6",System.Globalization.CultureInfo.InvariantCulture),samples[284].ToString("F6",System.Globalization.CultureInfo.InvariantCulture),samples[296].ToString("F6",System.Globalization.CultureInfo.InvariantCulture),allocated/300,15,clock.Elapsed.TotalSeconds.ToString("F6",System.Globalization.CultureInfo.InvariantCulture));
            System.IO.File.WriteAllLines(args[0],new[]{"active_min,active_max,roads,junctions,workers,steps,mean_ms,p95_ms,p99_ms,allocated_per_step,simulation_seconds,wall_seconds",row});Console.WriteLine(row);
        }
    }
    static void ResidentTests()
    {
        var session=CityCoreIndustrySession.Create();Check(session.City.Citizens.All(p=>!p.atWork&&p.location==3),"Default workers invented onsite attendance");session.Advance(.05);Check(session.Transport.ActiveCount==1&&session.Transport.Vehicles[0].purpose==TransportPurpose.Work&&session.City.Factory(1).produced==0,"Departure did not use real empty passenger vehicle / produced before arrival");var person=session.City.Citizens.First(p=>p.tripId>0);var task=session.Transport.FindVehicle(person.tripId);var observed=session.Residents.Observe(person.id);Check(observed.travelling&&observed.destination==1&&observed.x< -25,"Resident observation did not use real vehicle");Check(session.Simulation.RequestCommute(person.id,3,1)==null,"Resident got two simultaneous cars");
        session.Transport.SplitRoadAt(1,5);var restore=CityCoreSave.Read(CityCoreSave.Write(session));for(int i=0;i<220;i++){session.Advance(.05);restore.Advance(.05);session.Validate();Check(Convert.ToBase64String(CityCoreSave.Write(session))==Convert.ToBase64String(CityCoreSave.Write(restore)),"Resident saved in motion diverged");}Check(session.City.Citizens.All(p=>p.atWork&&p.lastCommute>0&&p.workedMinutes>0)&&session.City.Factory(1).sold>0,"Actual commuting did not cause labour/production/delivery");
        while(!session.Transport.Vehicles.Any(v=>v.purpose==TransportPurpose.Home)&&session.Transport.Clock<40)session.Advance(.05);var returning=session.Transport.Vehicles.First(v=>v.purpose==TransportPurpose.Home);double x=session.Transport.Position(returning).x;session.Advance(.05);Check(session.Transport.Position(returning).x<x&&returning.cargo==0,"Return trip did not actually travel west / carries invented goods");var returnLoad=CityCoreSave.Read(CityCoreSave.Write(session));for(int i=0;i<180;i++){session.Advance(.05);returnLoad.Advance(.05);}Check(Convert.ToBase64String(CityCoreSave.Write(session))==Convert.ToBase64String(CityCoreSave.Write(returnLoad)),"Return save differs");Check(session.City.Citizens.All(p=>!p.atWork&&p.tripId==0&&p.location==3),"Workers never returned home");
        int savings=session.City.society.families[0].savings;session.Advance(40);session.Validate();Check(session.City.day==2&&session.City.Citizens.Sum(p=>p.totalFactoryWagesPaid)>0&&session.City.society.families[0].savings>savings,"Actual employer wages did not reach household at day boundary");
        var blocked=CityCoreIndustrySession.Create();blocked.Transport.RemoveRoad(1);blocked.Advance(3);Check(blocked.Transport.ActiveCount==0&&blocked.City.Factory(1).produced==0&&blocked.City.Citizens.All(p=>p.location==3&&!p.atWork),"Broken commute still produced labour");
        var interrupted=CityCoreIndustrySession.Create();interrupted.Advance(.05);var rider=interrupted.City.Citizens.First(p=>p.tripId>0);var car=interrupted.Transport.FindVehicle(rider.tripId);Point before=interrupted.Transport.Position(car);interrupted.Transport.RemoveRoad(1);interrupted.Advance(.05);interrupted.Validate();Check(car.status==VehicleStatus.Stranded&&rider.tripId==car.id&&rider.location==-1&&!rider.atWork&&Point.Distance(before,interrupted.Transport.Position(car))<1e-7,"Cut road lost/teleported commuting citizen");CityCoreSave.Read(CityCoreSave.Write(interrupted)).Validate();
        interrupted.Transport.AddRoad(1,2,new[]{interrupted.Transport.JunctionPosition(1),new Point(-18,0,0),interrupted.Transport.JunctionPosition(2)},lanes:2);interrupted.Advance(.05);Check(car.status==VehicleStatus.Active&&car.citizenId==rider.id&&rider.tripId==car.id&&Point.Distance(before,interrupted.Transport.Position(car))<.7,"Repaired road did not recover same citizen/car without jump");interrupted.Advance(6);interrupted.Validate();Check(rider.atWork&&interrupted.City.Factory(1).produced>0,"Recovered commute did not resume actual labour");
    }
    static CommercialBuilding AsShop(this CityBuilding b)=>(CommercialBuilding)b;
    static void ReachabilityTests()
    {
        // Compare the structural filter with actual routing, including same-lane
        // upstream targets that require a cycle and lane restrictions at cuts.
        foreach(var direction in new[]{RoadDirection.Both,RoadDirection.Forward,RoadDirection.Backward})
        {
            var d=new NetworkData{nextJunction=5,nextRoad=5};
            d.junctions.AddRange(new[]{new Junction{id=1,position=new Point(0,0,0)},new Junction{id=2,position=new Point(20,0,0)},new Junction{id=3,position=new Point(20,0,20)},new Junction{id=4,position=new Point(0,0,20)}});
            for(int i=0;i<4;i++)d.roads.Add(new PhysicalRoad{id=i+1,from=i+1,to=(i+1)%4+1,lanes=2,direction=direction,points=new[]{d.junctions[i].position,d.junctions[(i+1)%4].position}});
            var n=new Network(d);var lanes=n.Drivables.Where(x=>!x.IsConnection).ToArray();
            foreach(var a in lanes)foreach(var b in lanes)foreach(double start in new[]{2.0,18.0})foreach(double end in new[]{2.0,18.0})
            {
                var s=new RoadAccess{lane=a.Id,distance=start,valid=true};var t=new RoadAccess{lane=b.Id,distance=end,valid=true};
                Check(n.CanReach(s,t)==(n.Route(s,t)!=null),"Directed lane reachability disagrees with real routing");
            }
        }
        var city=CityCoreIndustrySession.Create();var old=city.Transport.Network;var source=city.Transport.GetAccess(1);var goal=city.Transport.GetAccess(2);
        Check(old.CanReach(source,goal),"Freight path incorrectly rejected");city.Transport.RemoveRoad(2);Check(!city.Transport.Network.CanReach(source,goal),"Construction reused stale reachability");
    }
    static void FreightCandidateTests()
    {
        var s=CityIntegratedScaleFixture.Create(100);var index=new CityFreightCandidates(s.Entities,s.Transport);var result=new System.Collections.Generic.List<CommercialBuilding>();
        Action verify=()=>
        {
            foreach(var f in s.Entities.Factories)
            {
                var source=s.Transport.GetAccess(f.id);index.Collect(source,result);
                var expected=s.Entities.Shops.Where(shop=>s.Transport.Network.CanReach(source,s.Transport.GetAccess(shop.id))).ToArray();
                Check(result.SequenceEqual(expected),"Local freight candidate index changed reachable shop ordering");
            }
        };
        verify();s.Transport.SplitRoadAt(1,380);verify();s.Transport.RemoveRoad(2);verify();
        var load=CityCoreSave.Read(CityCoreSave.Write(s));var restoredIndex=new CityFreightCandidates(load.Entities,load.Transport);
        foreach(var f in load.Entities.Factories){restoredIndex.Collect(load.Transport.GetAccess(f.id),result);Check(result.SequenceEqual(load.Entities.Shops.Where(shop=>load.Transport.Network.CanReach(load.Transport.GetAccess(f.id),load.Transport.GetAccess(shop.id)))),"Loaded freight index wrong");}
    }
    static void R1Tests()
    {
        var s=CityCoreIndustrySession.Create();Check(s.Advance(.023)==0&&s.Simulation.Tick==0,"Partial step advanced time");var clone=CityCoreSave.Read(CityCoreSave.Write(s));Check(clone.Advance(.027)==1&&s.Advance(.027)==1&&s.Simulation.Tick==1&&s.Transport.Clock==.05,"Saved debt / single tick differs");Check(Convert.ToBase64String(CityCoreSave.Write(s))==Convert.ToBase64String(CityCoreSave.Write(clone)),"Saved debt changed complete state");long tick=s.Simulation.Tick;s.Advance(0);Check(tick==s.Simulation.Tick,"Pause advanced clock");bool rejected=false;try{s.Transport.Advance(.1);}catch(InvalidOperationException){rejected=true;}Check(rejected&&s.Simulation.Tick==tick,"Transport advanced city clock independently");rejected=false;try{s.Transport.Step();}catch(InvalidOperationException){rejected=true;}Check(rejected&&s.Simulation.Tick==tick,"Direct city physics step accepted");
        var one=CityCoreIndustrySession.Create();var three=CityCoreIndustrySession.Create();for(int i=0;i<1200;i++)one.Advance(.05);for(int i=0;i<400;i++)three.Advance(.15);Check(one.Simulation.Tick==1200&&three.Simulation.Tick==1200&&one.Transport.Clock==60,"Rate requests lost/duplicated ticks");Check(Convert.ToBase64String(CityCoreSave.Write(one))==Convert.ToBase64String(CityCoreSave.Write(three)),"1x/3x request grouping changed state");
        var indexed=CityCoreIndustrySession.Create();var original=indexed.City.GetBuilding(2);var replacement=new CommercialBuilding{id=2,x=original.x,z=original.z,entranceX=original.entranceX,entranceZ=original.entranceZ};indexed.Entities.ReplaceBuilding(replacement);Check(ReferenceEquals(indexed.City.GetBuilding(2),replacement)&&indexed.Entities.Shops[0]==replacement,"Same-count building replacement stale");indexed.Entities.AddFamily(new Household{id=2,home=3,unit=1,resident=true});indexed.City.Residence(3).housingUnits=2;indexed.City.Factory(1).raw=24;indexed.City.GetBuilding(1).level=2;indexed.City.SyncJobs();var p=new CityResident{id=5,age=30,canWork=true,skill=2,location=3,name="新增工人"};indexed.Entities.AddResident(1,p);var vacant=indexed.City.society.jobEntities.First(j=>j.occupiedCitizenId<0&&j.buildingId==1);indexed.City.AssignJob(p,vacant.id);indexed.Entities.MoveResident(5,2);Check(indexed.Entities.Member(5).Family.id==2&&p.householdId==2&&!indexed.City.society.families[0].people.Contains(p),"Membership move stale");
        indexed.Advance(7);Check(p.atWork&&p.workedMinutes>0&&indexed.Residents.Observe(5).building==1,"New registered resident did not actually commute/work");indexed.Validate();var saved=CityCoreSave.Read(CityCoreSave.Write(indexed));for(int i=0;i<100;i++){indexed.Advance(.05);saved.Advance(.05);}Check(Convert.ToBase64String(CityCoreSave.Write(indexed))==Convert.ToBase64String(CityCoreSave.Write(saved)),"Lifecycle save differs");
        var removal=CityCoreIndustrySession.Create();var idle=new CityResident{id=5,name="待移除",age=20,canWork=true,location=3};removal.Entities.AddResident(1,idle);removal.City.GetBuilding(1).level=2;removal.City.SyncJobs();var job=removal.City.society.jobEntities.First(j=>j.occupiedCitizenId<0&&j.buildingId==1);removal.City.AssignJob(idle,job.id);removal.Entities.RemoveResident(5);Check(removal.Entities.Member(5)==null&&job.occupiedCitizenId==-1,"Removal left person/job references");removal.Advance(.05);var rider=removal.City.Citizens.First(x=>x.tripId>0);int before=removal.Entities.Members.Count;rejected=false;try{removal.Entities.RemoveResident(rider.id);}catch(InvalidOperationException){rejected=true;}Check(rejected&&removal.Entities.Members.Count==before&&rider.tripId>0,"Removed an in-transit person");removal.Validate();
        var unsafeCity=CityCoreIndustrySession.Create();unsafeCity.City.buildings[1]=new CommercialBuilding{id=2};rejected=false;try{unsafeCity.Validate();}catch(InvalidOperationException){rejected=true;}Check(rejected,"Bypassed same-count mutation was silently valid");
        var closing=CityCoreIndustrySession.Create();closing.Advance(.05);var traveler=closing.Transport.Vehicles[0];closing.Entities.SetAvailability(1,true,false);Check(traveler.status==VehicleStatus.Stranded&&closing.City.Job(closing.City.Citizens.First(p2=>p2.tripId>0).jobId)==null,"Availability did not release jobs/interrupt real trip");closing.Validate();
    }
}
