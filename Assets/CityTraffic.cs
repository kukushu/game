using System;
using System.Collections.Generic;
using System.Linq;

namespace HarborCity
{
    public enum TripPurpose { Commute, Delivery, Import, Export, GarbagePickup, GarbageTransfer, Shopping, Healthcare, School, FireResponse, PoliceResponse, Deathcare }
    public enum TripStatus { Driving, Waiting, Visiting }
    public enum CargoKind { Goods, RawMaterial, Waste }

    [Serializable]
    public sealed class TrafficTrip
    {
        public int id, origin, destination, home, segment, cargo;
        public int householdId;
        public int residentId;
        public float departedAt;
        public TripPurpose purpose;
        public CargoKind cargoKind;
        public TripStatus status;
        public bool returning;
        public bool walking,medicalPickup;
        public int clinicId=-1;
        public int corpseId;
        public List<int> route = new List<int>();
        public float progress, delay, blocked;
        public int Current => route.Count == 0 ? -1 : route[Math.Min(segment, route.Count - 1)];
        public int Next => segment + 1 < route.Count ? route[segment + 1] : Current;
    }

    [Serializable]
    public sealed class TrafficState
    {
        public int nextId = 1, scheduler, completed, failed, delivered;
        public int lostGoods, lostRaw,consumedGoods;
        public float clock, dispatchTimer, remainder;
        public List<TrafficTrip> trips = new List<TrafficTrip>();
    }

    // Actual resident and freight trips, separate from Unity rendering.
    public sealed partial class CityTraffic
    {
        public const int Capacity = 48;
        public const int TaskCapacity = 100048;
        public const int Outside = -1;
        const float Step = .05f, Gap = .48f;
        const double FixedStepSeconds=.05;
        readonly CityModel city;
        readonly Dictionary<int, int> junctions = new Dictionary<int, int>();
        public TrafficState State => city.traffic;
        static int Entrance => CityRoads.Entrance;

        public CityTraffic(CityModel city)
        {
            this.city = city;
            if(city.traffic==null) city.traffic=new TrafficState();
            // Restore junction ownership from the saved physical positions, departures first.
            foreach (var t in State.trips)
                if (!t.walking && t.status == TripStatus.Driving && Behind(t) < .4f && !DepartedQueue(t)) Reserve(t.Current,t.id);
            foreach (var t in State.trips)
                if (!t.walking && t.status == TripStatus.Driving && Ahead(t) <= .35f) Reserve(t.Next,t.id);
        }

        static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n) && n >= 0;
        public static bool Valid(CityModel city)
        {
            var s=city.traffic; var roads=city.roads;
            if(s==null || s.trips==null || s.trips.Count>TaskCapacity || !Finite(s.clock) || !Finite(s.dispatchTimer) || !Finite(s.remainder)
                || s.remainder>Step+.001f || s.nextId<1 || s.nextId==int.MaxValue || s.scheduler<0 || s.completed<0 || s.failed<0 || s.delivered<0 || s.lostGoods<0 || s.lostRaw<0 || s.consumedGoods<0) return false;
            var ids=new HashSet<int>();
            foreach(var t in s.trips)
            {
                if(t==null || t.id<=0 || t.id>=s.nextId || !ids.Add(t.id) || !ValidEndpoint(t.origin) || !ValidEndpoint(t.destination) || !ValidEndpoint(t.home)
                    || t.route==null || t.route.Count==0 || t.route.Count>20000 || t.segment<0 || t.segment>=t.route.Count || !Finite(t.progress) || t.progress>=1
                    || t.residentId<0 || !Finite(t.departedAt) || !Finite(t.delay) || !Finite(t.blocked) || t.cargo<0 || t.cargo>8
                    || (int)t.purpose<0 || (int)t.purpose>11 || (int)t.status<0 || (int)t.status>2 || (int)t.cargoKind<0 || (int)t.cargoKind>2
                    || t.cargoKind==CargoKind.RawMaterial && (t.purpose!=TripPurpose.Import || !t.returning && t.origin!=Outside)) return false;
                if((t.purpose==TripPurpose.GarbagePickup || t.purpose==TripPurpose.GarbageTransfer)!=(t.cargoKind==CargoKind.Waste) || t.cargoKind==CargoKind.Waste && t.residentId!=0)return false;
                if(t.purpose==TripPurpose.Shopping && (t.residentId<=0 || t.cargoKind!=CargoKind.Goods || t.cargo>1))return false;
                if(t.purpose==TripPurpose.FireResponse && (t.residentId!=0 || t.cargo!=0 || !(city.GetBuilding(t.home) is FireHouseBuilding)))return false;
                if(t.purpose!=TripPurpose.Deathcare && t.corpseId!=0)return false;
                if(t.purpose==TripPurpose.Deathcare && (t.corpseId<=0 || t.residentId!=0 || t.cargo!=0 || !(city.GetBuilding(t.home) is CemeteryBuilding)))return false;
                if(t.purpose==TripPurpose.PoliceResponse && (t.residentId!=0 || t.cargo!=0 || !(city.GetBuilding(t.home) is PoliceStationBuilding)))return false;
                if(t.purpose==TripPurpose.Healthcare)
                {if(t.residentId<=0 || t.cargo!=0 || t.cargoKind!=CargoKind.Goods || !(city.GetBuilding(t.clinicId) is ClinicBuilding) || t.medicalPickup && (t.walking || t.returning))return false;}
                else if(t.purpose==TripPurpose.School)
                {if(!t.walking || t.medicalPickup || t.clinicId!=-1 || t.residentId<=0 || t.cargo!=0 || t.cargoKind!=CargoKind.Goods || !t.returning && !(city.GetBuilding(t.destination) is SchoolBuilding))return false;}
                else if(t.walking || t.medicalPickup || t.clinicId!=-1)return false;
                if(t.route.Any(n=>roads.Node(n)==null)) return false;
            }
            return true;
            bool ValidEndpoint(int id)=>id==Outside || city.GetBuilding(id)!=null;
        }
        bool Road(int i)=>city.roads.Active(i);
        bool Linked(int a,int b)=>a==b || city.roads.Linked(a,b);
        float Units(int a,int b)=>a==b?1:city.roads.EdgeLength(a,b)/3;
        float Ahead(TrafficTrip t)=>(1-t.progress)*Units(t.Current,t.Next);
        float Behind(TrafficTrip t)=>t.progress*Units(t.Current,t.Next);
        // Existing node locks approximate the whole intersection. A vehicle
        // already on its exit edge must not retain that global lock forever
        // while waiting for downstream traffic; the one-second grace is provisional.
        static bool DepartedQueue(TrafficTrip t)=>t.progress>0 && t.blocked>=1;
        bool Building(int id)=>city.GetBuilding(id)!=null;
        bool Endpoint(int i)=>i==Outside?Road(Entrance):Building(i);
        IEnumerable<int> Neighbors(int i)=>city.roads.Neighbors(i);
        List<int> Access(int endpoint)=>endpoint==Outside || Building(endpoint)?city.AccessBuilding(endpoint):new List<int>();
        public List<int> FindRoute(int origin,int destination)=>Search(Access(origin),Access(destination));
        List<int> Search(List<int> starts,List<int> goals)=>city.roads.FindPath(starts,goals);

        public TrafficTrip Dispatch(int origin, int destination, TripPurpose purpose, int residentId=0,CargoKind cargoKind=CargoKind.Goods)
        {
            if(purpose==TripPurpose.Shopping)return DispatchShopping(origin,destination,residentId);
            if(purpose>=TripPurpose.GarbagePickup || cargoKind==CargoKind.Waste)return null;
            if(purpose==TripPurpose.Commute && residentId<=0) return null;
            if(purpose==TripPurpose.Commute)
            {
                if(city.GetBuilding(destination)?.burning==true)return null;
                var person=city.Citizens.FirstOrDefault(p=>p.id==residentId);
                if(person!=null && destination==city.Workplace(person) && (person.sick || city.GetBuilding(destination)?.burning==true))return null;
            }
            if(city.GetBuilding(destination)?.abandoned==true || residentId==0 && city.GetBuilding(origin)?.abandoned==true)return null;
            if(purpose>=TripPurpose.Delivery && (origin!=Outside && city.Inventory(origin)==null || destination!=Outside && cargoKind==CargoKind.Goods && !(city.GetBuilding(destination) is CommercialBuilding))) return null;
            int background=0; foreach(var existing in State.trips) if(existing.residentId==0) background++;
            if (State.trips.Count >= TaskCapacity || (residentId==0 && background>=Capacity) || origin == destination || !Endpoint(origin) || !Endpoint(destination)) return null;
            int cargo = purpose == TripPurpose.Delivery || purpose == TripPurpose.Import || purpose == TripPurpose.Export ? 8 : 0;
            if(cargoKind==CargoKind.RawMaterial && (origin!=Outside || purpose!=TripPurpose.Import || city.Factory(destination)==null)) return null;
            if(cargo>0 && destination!=Outside)
            {
                int available=cargoKind==CargoKind.RawMaterial?FactoryState.RawCapacity-city.Factory(destination).raw-IncomingAmount(destination,cargoKind):32-city.Inventory(destination).Stock-IncomingAmount(destination,cargoKind);
                cargo=Math.Min(cargo,available); if(cargo<=0) return null;
            }
            if (cargo > 0 && origin != Outside && city.Inventory(origin).Stock < cargo) return null;
            var route = FindRoute(origin, destination);
            if (route.Count == 0 || !CanEnter(route, null)) return null;
            var buyer=cargoKind==CargoKind.RawMaterial?city.Factory(destination):null;
            if(buyer!=null && !city.development.enabled)
            {
                cargo=Math.Min(cargo,(int)Math.Min(8,Math.Floor(buyer.cash/FactoryState.RawPrice)));
                if(cargo<=0) return null;
                double cost=cargo*FactoryState.RawPrice;
                buyer.cash-=cost; buyer.rawCosts+=cost;
                city.Trace("factory.raw_paid","原料货车发车时支付货款；运输损失不退款",buyer,building:destination);
            }
            var trip = new TrafficTrip { id = State.nextId++, origin = origin, destination = destination, home = origin,
                purpose = purpose, cargo = cargo, cargoKind=cargoKind, route = route, residentId=residentId, departedAt=State.clock };
            if (cargo > 0 && origin != Outside) city.Inventory(origin).Stock -= cargo;
            if(cargo>0 && origin!=Outside && city.Factory(origin)!=null) city.Factory(origin).shipped+=cargo;
            State.trips.Add(trip);
            if(residentId==0) city.Trace("trip.started","货运发车："+purpose,trip,trip:trip.id);
            return trip;
        }

        public int IncomingAmount(int destination,CargoKind kind)
        {
            int amount=0; foreach(var t in State.trips) if(!t.returning && t.destination==destination && t.cargoKind==kind) amount+=t.cargo;
            return amount;
        }
        bool IncomingCargo(int destination)
        {
            foreach (var t in State.trips) if (t.destination == destination && !t.returning && t.residentId==0 && t.cargoKind==CargoKind.Goods && t.cargo > 0) return true;
            return false;
        }
        bool BusyHome(int home, TripPurpose purpose)
        {
            foreach (var t in State.trips) if (t.home == home && t.purpose == purpose) return true;
            return false;
        }
        bool CanEnter(List<int> route, TrafficTrip self)
        {
            foreach (var other in State.trips)
            {
                if (other == self || other.walking || other.status == TripStatus.Visiting) continue;
                if (other.Current == route[0] && (route.Count == 1 || other.Next == route[1]) && Behind(other) < Gap) return false;
                if (other.Next == route[0] && Ahead(other) < Gap) return false;
            }
            return true;
        }

        public void Advance(float seconds)
        {
            if (seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            // Large fast-forward calls must not repeatedly subtract tiny float
            // steps from a large float: that loses simulation time over days.
            double remaining=(double)State.remainder+seconds;State.remainder=0;
            while (remaining + .000001 >= FixedStepSeconds)
            {
                remaining-=FixedStepSeconds;
                Simulate();
            }
            State.remainder=(float)Math.Max(0,remaining);
        }

        void Simulate()
        {
            State.clock += Step;
            // A junction reservation covers the approach and departure, then releases.
            var release = new List<int>();
            foreach (var pair in junctions)
            {
                var owner = State.trips.Find(t => t.id == pair.Value);
                if (owner == null || owner.status != TripStatus.Driving
                    || !(owner.Next == pair.Key && Ahead(owner) <= .36f || owner.Current == pair.Key && Behind(owner) < .4f && !DepartedQueue(owner))) release.Add(pair.Key);
            }
            foreach (int node in release) junctions.Remove(node);
            foreach (var t in State.trips.ToArray()) Move(t);
            AdvanceHealthcare(Step*1440/city.society.settings.secondsPerDay);
            AdvanceSchools(Step*1440/city.society.settings.secondsPerDay);
            city.AdvanceFires(Step*1440/city.society.settings.secondsPerDay);
            city.AdvancePolice(Step*1440/city.society.settings.secondsPerDay);
            city.ObserveAnalysisTraffic();
            AdvanceResidents();
            State.dispatchTimer += Step;
            if (State.dispatchTimer >= .6f)
            {
                State.dispatchTimer -= .6f;
                Schedule();
            }
        }

        void Schedule()
        {
            if (State.trips.Count >= TaskCapacity) return;
            ScheduleFire();SchedulePolice();ScheduleDeathcare();
            if(ScheduleWaste())return;
            // Rotate sources so adding a remote district does not starve its trips.
            var sources=city.buildings.OrderBy(b=>b.id).ToList();
            for(int k=0;k<sources.Count;k++)
            {
                State.scheduler%=sources.Count;
                var building=sources[State.scheduler]; State.scheduler=(State.scheduler+1)%sources.Count;
                int i=building.id;if(building.abandoned || !Building(i) || !city.EntityRoadAccess(i)) continue;
                var use=building.Use;
                if (use == LandUse.Commercial && city.Inventory(i).Stock < 8 && !IncomingCargo(i))
                {
                    foreach(var supplier in city.buildings.OfType<IndustrialBuilding>())
                        if(Dispatch(supplier.id,i,TripPurpose.Delivery)!=null) return;
                    if (Dispatch(Outside, i, TripPurpose.Import) != null) return;
                }
                else if (use == LandUse.Industrial)
                {
                    var factory=city.Factory(i);
                    if(factory!=null && city.EmployedAt(i)>0 && factory.raw+IncomingAmount(i,CargoKind.RawMaterial)<8)
                        if(Dispatch(Outside,i,TripPurpose.Import,cargoKind:CargoKind.RawMaterial)!=null) return;
                    if(city.Inventory(i).Stock>=20 && !BusyHome(i,TripPurpose.Export) && Dispatch(i,Outside,TripPurpose.Export)!=null) return;
                }
            }
        }

        bool IsJunction(int node)
        {
            int count = 0;
            foreach (int n in Neighbors(node)) if (Road(n)) count++;
            return count > 2;
        }
        bool Reserve(int node, int id)
        {
            if (!IsJunction(node)) return true;
            if (junctions.TryGetValue(node, out int owner)) return owner == id;
            junctions[node] = id; return true;
        }
        bool RouteValid(TrafficTrip t)
        {
            for (int i = t.segment; i < t.route.Count; i++) if (!Road(t.route[i])) return false;
            for (int i = t.segment; i < t.route.Count-1; i++) if (!Linked(t.route[i],t.route[i+1])) return false;
            return Access(t.destination).Contains(t.route[t.route.Count - 1]);
        }
        void Wait(TrafficTrip t)
        {
            t.status = TripStatus.Waiting; t.blocked += Step;
            if (t.blocked >= 120 && t.residentId==0 && t.cargoKind!=CargoKind.Waste && t.purpose!=TripPurpose.FireResponse && t.purpose!=TripPurpose.PoliceResponse && t.purpose!=TripPurpose.Deathcare) Fail(t);
        }
        public void RemoveBuilding(int id)
        {
            foreach(var t in State.trips.ToArray())
            {
                var h=t.residentId>0?TripFamily(t):null;
                if(t.origin!=id && t.destination!=id && t.home!=id && h?.home!=id) continue;
                if(t.residentId==0)
                {
                    // Abandonment and fire leave the building entity in place.
                    // Its specific corpse still needs the assigned hearse;
                    // demolition guards prevent deleting a body or cemetery.
                    if(t.purpose==TripPurpose.Deathcare && city.GetBuilding(id)!=null)continue;
                    if((t.purpose==TripPurpose.FireResponse || t.purpose==TripPurpose.PoliceResponse) && t.home!=id){if(t.origin==id)t.origin=Outside;RedirectFromCurrent(t,t.home);continue;}
                    if(t.cargoKind==CargoKind.Waste && t.home!=id && t.cargo>0)
                    {
                        if(t.origin==id)t.origin=Outside;
                        if(t.destination==id)RedirectFromCurrent(t,t.home);
                        continue;
                    }
                    Fail(t);continue;
                }
                var p=h?.people.Find(c=>c.id==t.residentId);
                if(h==null || h.home==id)
                {if(p!=null) {p.tripId=0;p.atWork=false;p.location=-1;} State.lostGoods+=t.cargo;t.cargo=0;State.trips.Remove(t);continue;}
                if(t.origin==id) t.origin=Outside;
                t.home=h.home;RedirectFromCurrent(t,h.home);
                if(t.purpose==TripPurpose.School && p!=null)p.schoolReturning=true;
            }
        }
        void RedirectFromCurrent(TrafficTrip t,int destination)
        {
            // A stopped passenger or loaded truck leaves the actual access node.
            // Moving vehicles retain their intact current edge until replanning.
            if(t.status==TripStatus.Visiting)
            {int current=t.Current;t.route=new List<int>{current};t.segment=0;t.progress=0;t.delay=0;t.status=TripStatus.Driving;}
            t.destination=destination;t.returning=true;
        }
        bool HoldAtServiceTarget(TrafficTrip t)
        {
            if(Road(t.Current) && Access(t.destination).Contains(t.Current))return true;
            // Road construction can split the former entrance edge and choose a
            // different access node. Resume from the actual stopped position.
            int current=t.Current;t.route=new List<int>{current};t.segment=0;t.progress=0;t.delay=0;t.status=TripStatus.Driving;
            return false;
        }
        void Fail(TrafficTrip t)
        {
            city.Trace("trip.failed","任务失败；核对未送达货物的退回或损失",t,household:t.householdId,citizen:t.residentId,trip:t.id,level:"warning");
            // Undelivered goods are returned to the supplier, never credited to the buyer.
            if(t.cargo>0)
            {
                int restored=t.residentId==0 && t.cargoKind==CargoKind.Goods && city.GetBuilding(t.origin) is IGoodsBuilding?Math.Min(t.cargo,Math.Max(0,24-city.Inventory(t.origin).Stock)):0;
                if(restored>0) city.Inventory(t.origin).Stock+=restored;
                int lost=t.cargo-restored;
                if(t.cargoKind==CargoKind.Waste)city.waste.lost+=lost;else if(t.cargoKind==CargoKind.RawMaterial) State.lostRaw+=lost; else State.lostGoods+=lost;
                city.Trace("cargo.failed","运输失败；退回或明确记为损失",new CityLogDetail {origin=t.origin,destination=t.destination,amount=restored,count=lost,reason=t.cargoKind.ToString()},trip:t.id,level:"warning");
            }
            if(t.residentId>0){var person=TripFamily(t)?.people.Find(p=>p.id==t.residentId);if(person!=null)person.tripId=0;}
            State.failed++; State.trips.Remove(t);
        }

        void Move(TrafficTrip t)
        {
            if(HoldFireEngine(t) || HoldPoliceCar(t) || HoldHearse(t))return;
            if(t.residentId>0 && !ValidateResidentTrip(t)) return;
            if(t.cargoKind==CargoKind.Waste && t.status==TripStatus.Driving && t.delay>0)
            {t.delay=Math.Max(0,t.delay-Step);return;}
            if(t.cargoKind==CargoKind.Waste)CollectAlongRoute(t);
            // Losing road access is temporary; only a demolished building invalidates an existing task.
            if (t.destination!=Outside && !Building(t.destination) || t.home!=Outside && !Building(t.home)) { Fail(t); return; }
            if (t.status == TripStatus.Visiting)
            {
                t.delay = Math.Max(0, t.delay - Step);
                if (t.delay > 0) return;
                if (!t.returning)
                {
                    t.returning = true;
                    t.origin = t.destination; t.destination = t.home;
                }
                var back = FindRoute(t.origin, t.destination);
                if (back.Count == 0 || !CanEnter(back, t)) { t.blocked += Step; if (t.blocked >= 120 && t.cargoKind!=CargoKind.Waste && t.residentId==0 && t.purpose!=TripPurpose.FireResponse && t.purpose!=TripPurpose.PoliceResponse && t.purpose!=TripPurpose.Deathcare) Fail(t); return; }
                t.route = back; t.segment = 0; t.progress = t.blocked = 0; t.status = TripStatus.Driving;
                city.Trace("trip.returning","目的地停留结束，开始返程",t,trip:t.id);
            }
            if (!RouteValid(t))
            {
                // Finish an intact edge before replanning; don't teleport to a distant road.
                if (!Road(t.Current) || (t.progress > 0 && (!Road(t.Next) || !Linked(t.Current,t.Next)))) { Wait(t); return; }
                if (t.progress == 0)
                {
                    t.delay = Math.Max(0, t.delay - Step);
                    if (t.delay > 0) { Wait(t); return; }
                    var route = Search(new List<int> { t.Current }, Access(t.destination));
                    if (route.Count == 0) { t.delay = 1; Wait(t); return; }
                    t.route = route; t.segment = 0;
                }
            }
            t.status = TripStatus.Driving;
            if (t.segment == t.route.Count - 1) { Arrive(t); return; }
            float units = Units(t.Current,t.Next);
            // Every vehicle in a household city uses the same simulation clock and road speed.
            // One world unit per game minute = 480 road units per game day (3 world units per road unit).
            float velocity=(t.walking?120f:480f)/city.society.settings.secondsPerDay;
            float advance = Step * velocity / units;
            float proposed = Math.Min(1, t.progress + advance);
            foreach (var other in State.trips)
            {
                if (t.walking || other.walking || other == t || other.status == TripStatus.Visiting) continue;
                // Splitting a road can create edges shorter than one car's headway.
                float offset = -Behind(t);
                for (int s = t.segment; s < t.route.Count-1 && offset < Gap + units; s++)
                {
                    if (other.Current == t.route[s] && other.Next == t.route[s+1])
                    {
                        float distance = offset + Behind(other);
                        if (distance > .0001f) proposed = Math.Min(proposed,t.progress+(distance-Gap)/units);
                    }
                    offset += Units(t.route[s],t.route[s+1]);
                }
            }
            // A car leaving an intersection must be allowed to clear it. Trying
            // to reacquire its departure lock can create a circular wait when
            // downstream traffic backs up into another nearby intersection.
            if (!t.walking && (1-proposed)*units <= .35f && !Reserve(t.Next, t.id)) proposed = Math.Min(proposed, Math.Max(0,1-.36f/units));
            // A split edge can leave progress one float below 1. Do not classify
            // the final representable increment as blocked: it must reach the next
            // segment and release its junction reservation.
            if (proposed <= t.progress) { t.blocked += Step; if (t.blocked >= 120 && t.residentId==0 && t.cargoKind!=CargoKind.Waste && t.purpose!=TripPurpose.FireResponse && t.purpose!=TripPurpose.PoliceResponse && t.purpose!=TripPurpose.Deathcare) Fail(t); return; }
            t.progress = proposed; t.blocked = 0;
            if (t.progress >= 1) { t.segment++; t.progress = 0; }
        }

        void Arrive(TrafficTrip t)
        {
            if(t.purpose==TripPurpose.FireResponse){ArriveFire(t);return;}
            if(t.purpose==TripPurpose.PoliceResponse){ArrivePolice(t);return;}
            if(t.purpose==TripPurpose.Deathcare){ArriveHearse(t);return;}
            if(t.residentId>0) { ArriveResident(t); return; }
            if(t.cargoKind==CargoKind.Waste){ArriveWaste(t);return;}
            if (t.returning) {city.Trace("trip.finished","返程完成",t,trip:t.id); State.trips.Remove(t); return; }
            city.Trace("trip.arrived","抵达目的地，交付货物或开始停留",t,trip:t.id);
            if (t.cargo > 0)
            {
                int delivered=t.cargo;
                if(t.cargoKind==CargoKind.RawMaterial)
                {
                    var factory=city.Factory(t.destination);
                    if(factory==null) {Fail(t); return;}
                    int added=Math.Min(t.cargo,FactoryState.RawCapacity-factory.raw);
                    factory.raw+=added; factory.imported+=added; State.lostRaw+=t.cargo-added;
                    delivered=added;
                    city.Trace("factory.raw_arrived","原料货车实际到厂入库",factory,building:t.destination,trip:t.id);
                }
                else if(t.destination!=Outside)
                {
                    int added=Math.Min(t.cargo,32-city.Inventory(t.destination).Stock); city.Inventory(t.destination).Stock+=added; State.lostGoods+=t.cargo-added;
                    delivered=added;
                }
                if(t.cargoKind==CargoKind.Goods && !t.returning) city.RecordFactorySale(t.origin,delivered,t.id,t.destination);
                State.delivered += delivered; t.cargo = 0;
            }
            State.completed++;
            t.status = TripStatus.Visiting;
            t.delay = 2;
            t.blocked = 0;
        }
    }
}
