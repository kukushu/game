using System;
using System.Collections.Generic;
using HarborCity.RoadCore;
namespace HarborCity
{
    public enum WastePhase { Pickup,Return }
    [Serializable] public sealed class WasteMission
    {
        public int vehicle,task,station,pickup;
        public WastePhase phase;
        internal WasteMission Copy()=>new WasteMission{vehicle=vehicle,task=task,station=station,pickup=pickup,phase=phase};
    }
    [Serializable] public sealed class WasteServiceData
    {
        public CityWasteState totals=new CityWasteState();
        public int lastGenerationDay;
        public long nextDispatchTick;
        public List<WasteMission> missions=new List<WasteMission>();
    }
    // Owns service missions and generation/disposal accounting. Shipment alone owns
    // in-transit payload; buildings own waiting/stored waste. Views own none of these.
    public sealed class CityCoreWaste
    {
        readonly CityModel city;readonly CityEntities entities;readonly World world;readonly WasteRules rules;
        readonly WasteServiceData state;
        readonly Dictionary<int,WasteMission> byVehicle=new Dictionary<int,WasteMission>();
        readonly List<LandfillBuilding> plants=new List<LandfillBuilding>();int buildingRevision=-1;
        readonly List<CityBuilding> candidates=new List<CityBuilding>();
        readonly HashSet<int> reservedTargets=new HashSet<int>();
        readonly Dictionary<int,int> fleets=new Dictionary<int,int>(),reserved=new Dictionary<int,int>();
        readonly Dictionary<int,double> work=new Dictionary<int,double>();
        readonly Dictionary<int,int> counts=new Dictionary<int,int>();
        static readonly Comparison<CityBuilding> Priority=(a,b)=>{int order=b.garbage.CompareTo(a.garbage);return order!=0?order:a.id.CompareTo(b.id);};
        public long Generated=>state.totals.generated;public long Collected=>state.totals.collected;public long Processed=>state.totals.processed;
        public int Vehicles=>byVehicle.Count;
        public CityWasteDiagnostics Diagnostics {get;set;}
        public long Waiting=>Generated-Collected; // Derived from the conserved transfer ledger, not a second editable stock.
        long WaitingInBuildings {get{long sum=0;foreach(var b in entities.Buildings)sum+=b.garbage;foreach(var b in entities.RetiredBuildings)sum+=b.garbage;return sum;}}
        public long Stored {get{RefreshPlants();long sum=0;foreach(var b in plants)sum+=b.stored;foreach(var b in entities.RetiredBuildings)if(b is LandfillBuilding p)sum+=p.stored;return sum;}}
        public long InTransit {get{long sum=0;foreach(var mission in state.missions)sum+=world.FindTask(mission.task)?.Cargo??0;return sum;}}
        public long BalanceError=>Generated-WaitingInBuildings-Stored-InTransit-Processed;
        internal CityCoreWaste(CityModel city,CityEntities entities,World world,WasteRules rules,WasteServiceData saved=null)
        {
            this.city=city;this.entities=entities;this.world=world;this.rules=rules;
            state=saved??new WasteServiceData{lastGenerationDay=city.day};state=Snapshot();city.waste=state.totals;
            foreach(var mission in state.missions)byVehicle.Add(mission.vehicle,mission);
        }
        public WasteServiceData Snapshot()
        {
            var t=state.totals;var copy=new WasteServiceData{lastGenerationDay=state.lastGenerationDay,nextDispatchTick=state.nextDispatchTick,totals=new CityWasteState{enabled=t.enabled,generated=t.generated,collected=t.collected,processed=t.processed,transferred=t.transferred,lost=t.lost}};
            foreach(var m in state.missions)copy.missions.Add(m.Copy());return copy;
        }
        void RefreshPlants(){if(buildingRevision==entities.BuildingRevision)return;plants.Clear();foreach(var b in entities.Buildings)if(b is LandfillBuilding p)plants.Add(p);buildingRevision=entities.BuildingRevision;}
        bool Running(LandfillBuilding b)=>!b.abandoned&&!b.burning&&!b.burned&&!b.emptying&&entities.RoadAccess(b.id)&&city.Supply(b.id).Complete;
        internal void GenerateDay()
        {
#if CITY_CORE_ATTRIBUTION
            using var observation=CityCoreProfile.Measure(CityCoreProfile.Part.Waste);
#endif
            if(!city.development.enabled||!state.totals.enabled||city.day<=state.lastGenerationDay)return;
            // Payroll has not reset actual attendance. One daily aggregation, O(P+B).
            work.Clear();foreach(var m in entities.Members)if(!m.Person.dead&&city.Workplace(m.Person)>0){int id=city.Workplace(m.Person);work.TryGetValue(id,out double minutes);work[id]=minutes+m.Person.workedMinutes;}
            counts.Clear();foreach(var h in city.society.families)if(h.resident){int count=0;foreach(var p in h.people)if(!p.dead)count++;counts.TryGetValue(h.home,out int n);counts[h.home]=n+count;}
            foreach(var b in entities.Buildings)
            {
                int amount=0;if(b is ResidentialBuilding){counts.TryGetValue(b.id,out int count);amount=count*rules.ResidentialRate;}
                else if(b is IndustrialBuilding||b is CommercialBuilding){work.TryGetValue(b.id,out double minutes);amount=(int)Math.Min(int.MaxValue,Math.Floor(minutes/rules.WorkMinutesPerUnit));}
                amount=Math.Min(amount,100000-b.garbage);b.garbage+=amount;state.totals.generated+=amount;
            }
            state.lastGenerationDay=city.day;
        }
        internal void Step(TimeContext time)
        {
#if CITY_CORE_ATTRIBUTION
            using var observation=CityCoreProfile.Measure(CityCoreProfile.Part.Waste);
#endif
            if(!city.development.enabled||!state.totals.enabled)return;RefreshPlants();
            foreach(var plant in plants)
            {
                Diagnostics?.Plant(plant.id,plant.stored,Running(plant),time.Duration);
                if(plant.stored<=0||!Running(plant))continue;
                double progress=plant.processingRemainder+time.Duration*rules.ProcessingPerDay/city.society.settings.secondsPerDay;
                int amount=(int)Math.Min(plant.stored,Math.Floor(progress));plant.processingRemainder=(float)(progress-amount);plant.stored-=amount;state.totals.processed+=amount;if(plant.stored==0)plant.processingRemainder=0;
                Diagnostics?.Process(plant.id,amount);
            }
            if(time.Tick<state.nextDispatchTick)return;state.nextDispatchTick=time.Tick+rules.DispatchTicks;if(plants.Count==0)return;
            // Periodic city scan, never per vehicle/physics subquery. Fleet and incoming
            // reservations are derived from missions, not independently saved counters.
            reservedTargets.Clear();fleets.Clear();reserved.Clear();
            foreach(var m in state.missions){reservedTargets.Add(m.pickup);fleets.TryGetValue(m.station,out int count);fleets[m.station]=count+1;reserved.TryGetValue(m.station,out int payload);reserved[m.station]=payload+(m.phase==WastePhase.Pickup?rules.Payload:world.FindTask(m.task).Cargo);}
            candidates.Clear();foreach(var b in entities.Buildings)if(b.garbage>0&&!reservedTargets.Contains(b.id)&&entities.RoadAccess(b.id))candidates.Add(b);candidates.Sort(Priority);
            foreach(var plant in plants)
            {
                fleets.TryGetValue(plant.id,out int count);reserved.TryGetValue(plant.id,out int incoming);
                var measured=Diagnostics?.Dispatch(plant.id);if(measured!=null)measured.dispatchChecks++;
                if(count>=rules.Trucks){if(measured!=null)measured.fleetFull++;continue;}
                if(plant.stored+incoming+rules.Payload>rules.Capacity){if(measured!=null)measured.storageFull++;continue;}
                if(!Running(plant)){if(measured!=null)measured.unavailable++;continue;}
                bool tried=false,admitted=false;
                foreach(var target in candidates)
                {
                    if(target.id==plant.id||reservedTargets.Contains(target.id))continue;
                    tried=true;if(measured!=null)measured.admissionAttempts++;
                    var admission=world.PrepareTransport(plant.id,target.id,0,0,TransportPurpose.Service,0,out var rejected);if(admission==null){if(measured!=null)measured.rejected[(int)rejected]++;continue;}
                    world.CheckAdmission(admission);var v=world.CommitAdmission(admission);var mission=new WasteMission{vehicle=v.id,task=v.Task.id,station=plant.id,pickup=target.id};state.missions.Add(mission);byVehicle.Add(v.id,mission);reservedTargets.Add(target.id);admitted=true;
                    if(measured!=null){measured.dispatched++;Diagnostics.Start(world,mission,v,count+1,state.missions.Count);}break;
                }
                if(measured!=null&&!admitted){if(tried)measured.allCandidatesRejected++;else measured.noCandidates++;}
            }
        }
        // Arrival has already passed physics checks. Returning route admission must
        // succeed before garbage leaves the building; an inaccessible return waits.
        internal void Arrive(TransportArrival arrival)
        {
            if(!byVehicle.TryGetValue(arrival.id,out var m)||m.task!=arrival.taskId)throw new InvalidOperationException("Missing waste mission");
            var plant=entities.Building(m.station) as LandfillBuilding;
            var oldTask=Diagnostics==null?null:world.FindTask(m.task);
            if(m.phase==WastePhase.Pickup)
            {
                var pickup=entities.Building(m.pickup);int amount=Math.Min(rules.Payload,pickup.garbage);
                var next=world.PrepareServiceReturn(arrival,m.station,amount);if(next==null){var blocked=Diagnostics?.Dispatch(m.station);if(blocked!=null)blocked.returnContinuationRejected++;return;}
                world.CheckContinuation(next);pickup.garbage-=amount;state.totals.collected+=amount;world.CommitContinuation(next);m.phase=WastePhase.Return;m.task=next.task.id;
                if(Diagnostics!=null){Diagnostics.Complete(m.station,oldTask,arrival.arrivedAt,amount,false);Diagnostics.Start(world,m,world.FindVehicle(m.vehicle),0,state.missions.Count);}
            }
            else
            {
                if(plant.stored+arrival.cargo>rules.Capacity){var blocked=Diagnostics?.Dispatch(m.station);if(blocked!=null)blocked.returnStorageBlocked++;return;}plant.stored+=arrival.cargo;world.AcknowledgeArrival(arrival.id);state.missions.Remove(m);byVehicle.Remove(m.vehicle);
                Diagnostics?.Complete(m.station,oldTask,arrival.arrivedAt,arrival.cargo,true);
            }
        }
        public void Validate()
        {
            RefreshPlants();var t=state.totals;if(!ReferenceEquals(city.waste,t)||t==null||!t.Valid()||t.lost!=0||t.transferred!=0||state.lastGenerationDay<1||state.lastGenerationDay>city.day||state.nextDispatchTick<0||t.collected>t.generated||t.collected!=Stored+InTransit+Processed||BalanceError!=0)throw new InvalidOperationException("Waste conservation/schedule failed");
            var ids=new HashSet<int>();var tasks=new HashSet<int>();var targets=new HashSet<int>();var fleetCounts=new Dictionary<int,int>();var reservations=new Dictionary<int,int>();
            foreach(var m in state.missions)
            {
                var v=world.FindVehicle(m.vehicle);var task=world.FindTask(m.task);
                fleetCounts.TryGetValue(m.station,out int fleet);fleetCounts[m.station]=fleet+1;reservations.TryGetValue(m.station,out int incoming);reservations[m.station]=incoming+(m.phase==WastePhase.Pickup?rules.Payload:task?.Cargo??0);
                if(m.station==m.pickup||!ids.Add(m.vehicle)||!tasks.Add(m.task)||!targets.Add(m.pickup)||v==null||v.Task!=task||task.purpose!=TransportPurpose.Service||!(entities.Building(m.station) is LandfillBuilding)||entities.Building(m.pickup)==null||m.phase<WastePhase.Pickup||m.phase>WastePhase.Return||task.origin!=(m.phase==WastePhase.Pickup?m.station:m.pickup)||task.destination!=(m.phase==WastePhase.Pickup?m.pickup:m.station)||m.phase==WastePhase.Pickup&&task.Shipment!=null||m.phase==WastePhase.Return&&(task.Shipment==null||task.Shipment.kind!=CargoKind.Waste||task.Cargo<1||task.Cargo>rules.Payload))throw new InvalidOperationException("Waste mission/task mismatch");
            }
            foreach(var p in plants){fleetCounts.TryGetValue(p.id,out int n);reservations.TryGetValue(p.id,out int incoming);if(n>rules.Trucks||p.stored+incoming>rules.Capacity)throw new InvalidOperationException("Waste fleet/incoming capacity overbooked");}
            foreach(var v in world.Vehicles)if(v.Task.purpose==TransportPurpose.Service&&!ids.Contains(v.id))throw new InvalidOperationException("Unowned service vehicle");
            foreach(var b in entities.Buildings)if(b.garbage<0||b.garbage>100000||b is LandfillBuilding p&&(p.stored<0||p.stored>rules.Capacity||p.processingRemainder<0||p.processingRemainder>=1||float.IsNaN(p.processingRemainder)))throw new InvalidOperationException("Invalid waste inventory");
        }
    }
}
