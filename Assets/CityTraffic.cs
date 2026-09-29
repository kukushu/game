using System;
using System.Collections.Generic;

namespace HarborCity
{
    public enum TripPurpose { Commute, Shopping, Delivery, Import, Export }
    public enum TripStatus { Driving, Waiting, Visiting }

    [Serializable]
    public sealed class TrafficTrip
    {
        public int id, origin, destination, home, segment, cargo;
        public int householdId;
        public int residentId;
        public float departedAt;
        public TripPurpose purpose;
        public TripStatus status;
        public bool returning;
        public List<int> route = new List<int>();
        public float progress, delay, blocked;
        public int Current => route.Count == 0 ? -1 : route[Math.Min(segment, route.Count - 1)];
        public int Next => segment + 1 < route.Count ? route[segment + 1] : Current;
    }

    [Serializable]
    public sealed class TrafficState
    {
        public int nextId = 1, scheduler, completed, failed, delivered, purchases;
        public float clock, dispatchTimer, productionTimer, remainder;
        public List<TrafficTrip> trips = new List<TrafficTrip>();
        public int[] stock = new int[CityModel.Size * CityModel.Size];
        public float[] nextCommute = new float[CityModel.Size * CityModel.Size];
        public float[] nextShopping = new float[CityModel.Size * CityModel.Size];
    }

    // Pure simulation: representative building trips, separate from Unity rendering.
    public sealed partial class CityTraffic
    {
        public const int Capacity = 48;
        public const int TaskCapacity = 100048;
        public const int Outside = -1;
        const float Step = .05f, Gap = .48f;
        readonly CityModel city;
        readonly Dictionary<int, int> junctions = new Dictionary<int, int>();
        public TrafficState State => city.traffic;
        static int Entrance => CityModel.Index(0, CityModel.Size / 2);

        public CityTraffic(CityModel city)
        {
            this.city = city;
            if (city.traffic == null) city.traffic = new TrafficState {
                stock=new int[city.tiles.Length], nextCommute=new float[city.tiles.Length], nextShopping=new float[city.tiles.Length] };
            // Restore junction ownership from the saved physical positions, departures first.
            foreach (var t in State.trips)
                if (t.status == TripStatus.Driving && Behind(t) < .4f) Reserve(t.Current,t.id);
            foreach (var t in State.trips)
                if (t.status == TripStatus.Driving && Ahead(t) <= .35f) Reserve(t.Next,t.id);
        }

        static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n) && n >= 0;
        static bool Lot(int n, int count) => n >= Outside && n < count;
        public static bool Valid(TrafficState s, CityRoads roads = null, int count = CityModel.Size * CityModel.Size)
        {
            if (s == null) return true; // Additive migration of the original city saves.
            if (s.trips == null || s.trips.Count > TaskCapacity || s.stock == null || s.stock.Length != count
                || s.nextCommute == null || s.nextCommute.Length != count || s.nextShopping == null || s.nextShopping.Length != count
                || !Finite(s.clock) || !Finite(s.dispatchTimer) || !Finite(s.productionTimer) || !Finite(s.remainder)
                || s.remainder > Step + .001f || s.nextId < 1 || s.nextId == int.MaxValue || s.scheduler < 0 || s.scheduler >= count
                || s.completed < 0 || s.failed < 0 || s.delivered < 0 || s.purchases < 0) return false;
            var ids = new HashSet<int>();
            foreach (int n in s.stock) if (n < 0 || n > 32) return false;
            foreach (float n in s.nextCommute) if (!Finite(n)) return false;
            foreach (float n in s.nextShopping) if (!Finite(n)) return false;
            foreach (var t in s.trips)
            {
                if (t == null || t.id <= 0 || t.id >= s.nextId || !ids.Add(t.id) || !Lot(t.origin,count) || !Lot(t.destination,count)
                    || !Lot(t.home,count) || t.route == null || t.route.Count == 0 || t.route.Count > (roads == null ? count : 20000)
                    || t.segment < 0 || t.segment >= t.route.Count || !Finite(t.progress) || t.progress >= 1 || t.residentId<0 || !Finite(t.departedAt)
                    || !Finite(t.delay) || !Finite(t.blocked) || t.cargo < 0 || t.cargo > 8
                    || (int)t.purpose < 0 || (int)t.purpose > 4 || (int)t.status < 0 || (int)t.status > 2) return false;
                for (int i = 0; i < t.route.Count; i++)
                    if (roads != null ? roads.Node(t.route[i]) == null
                        : t.route[i] < 0 || t.route[i] >= count || (i > 0 && Distance(t.route[i - 1], t.route[i]) != 1)) return false;
            }
            return true;
        }

        static int Distance(int a, int b) => Math.Abs(a % CityModel.Size - b % CityModel.Size) + Math.Abs(a / CityModel.Size - b / CityModel.Size);
        bool Road(int i) => city.roads != null ? city.roads.Active(i) : i >= 0 && i < city.tiles.Length && city.tiles[i] == (int)LandUse.Road;
        bool Linked(int a,int b) => a == b || (city.roads != null ? city.roads.Linked(a,b) : Road(a) && Road(b) && Distance(a,b)==1);
        float Units(int a,int b) => city.roads == null || a == b ? 1 : city.roads.EdgeLength(a,b) / 3;
        float Ahead(TrafficTrip t) => (1-t.progress)*Units(t.Current,t.Next);
        float Behind(TrafficTrip t) => t.progress*Units(t.Current,t.Next);
        bool Building(int i) => i >= 0 && i < city.tiles.Length && city.levels[i] > 0
            && city.tiles[i] >= (int)LandUse.Residential && city.tiles[i] <= (int)LandUse.Industrial;
        bool Endpoint(int i) => i == Outside ? Road(Entrance) : Building(i);
        IEnumerable<int> Neighbors(int i)
        {
            if (city.roads != null) { foreach (int n in city.roads.Neighbors(i)) yield return n; yield break; }
            int x = i % CityModel.Size, z = i / CityModel.Size;
            if (x > 0) yield return i - 1;
            if (x + 1 < CityModel.Size) yield return i + 1;
            if (z > 0) yield return i - CityModel.Size;
            if (z + 1 < CityModel.Size) yield return i + CityModel.Size;
        }
        List<int> Access(int endpoint)
        {
            if (city.version >= 3) return endpoint == Outside || Building(endpoint) ? city.AccessBuilding(endpoint) : new List<int>();
            if (city.roads != null) return endpoint == Outside || Building(endpoint) ? city.roads.Access(endpoint) : new List<int>();
            var result = new List<int>();
            if (endpoint == Outside) { if (Road(Entrance)) result.Add(Entrance); }
            else if (Building(endpoint)) foreach (int n in Neighbors(endpoint)) if (Road(n)) result.Add(n);
            return result;
        }

        // Uniform road costs: multi-source BFS finds the shortest legal grid route,
        // including buildings with several entrances. No diagonal or cross-row edges.
        public List<int> FindRoute(int origin, int destination) => Search(Access(origin), Access(destination));
        List<int> Search(List<int> starts, List<int> goals)
        {
            if (city.roads != null) return city.roads.FindPath(starts,goals);
            var result = new List<int>();
            if (starts.Count == 0 || goals.Count == 0) return result;
            var previous = new int[city.tiles.Length];
            for (int i = 0; i < previous.Length; i++) previous[i] = -2;
            var queue = new Queue<int>();
            foreach (int start in starts) if (Road(start)) { previous[start] = -1; queue.Enqueue(start); }
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                if (goals.Contains(current))
                {
                    for (int i = current; i >= 0; i = previous[i]) result.Add(i);
                    result.Reverse(); return result;
                }
                foreach (int n in Neighbors(current))
                    if (Road(n) && previous[n] == -2) { previous[n] = current; queue.Enqueue(n); }
            }
            return result;
        }

        public TrafficTrip Dispatch(int origin, int destination, TripPurpose purpose, int residentId=0)
        {
            int background=0; foreach(var existing in State.trips) if(existing.residentId==0) background++;
            if (State.trips.Count >= TaskCapacity || (residentId==0 && background>=Capacity) || origin == destination || !Endpoint(origin) || !Endpoint(destination)) return null;
            int cargo = purpose == TripPurpose.Delivery || purpose == TripPurpose.Import || purpose == TripPurpose.Export ? 8 : 0;
            if (cargo > 0 && origin != Outside && State.stock[origin] < cargo) return null;
            if (purpose == TripPurpose.Shopping && State.stock[destination] <= ReservedShopping(destination)) return null;
            var route = FindRoute(origin, destination);
            if (route.Count == 0 || !CanEnter(route, null)) return null;
            var trip = new TrafficTrip { id = State.nextId++, origin = origin, destination = destination, home = origin,
                purpose = purpose, cargo = cargo, route = route, residentId=residentId, departedAt=State.clock };
            if (cargo > 0 && origin != Outside) State.stock[origin] -= cargo;
            State.trips.Add(trip);
            return trip;
        }

        int ReservedShopping(int destination)
        {
            int n = 0;
            foreach (var t in State.trips) if (!t.returning && t.purpose == TripPurpose.Shopping && t.destination == destination && t.status != TripStatus.Visiting) n++;
            return n;
        }
        bool IncomingCargo(int destination)
        {
            foreach (var t in State.trips) if (t.destination == destination && t.cargo > 0) return true;
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
                if (other == self || other.status == TripStatus.Visiting) continue;
                if (other.Current == route[0] && (route.Count == 1 || other.Next == route[1]) && Behind(other) < Gap) return false;
                if (other.Next == route[0] && Ahead(other) < Gap) return false;
            }
            return true;
        }

        public void Advance(float seconds)
        {
            if (seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            State.remainder += seconds;
            while (State.remainder + .000001f >= Step)
            {
                State.remainder = Math.Max(0, State.remainder - Step);
                Simulate();
            }
        }

        void Simulate()
        {
            State.clock += Step;
            State.productionTimer += Step;
            if (State.productionTimer >= 2)
            {
                State.productionTimer -= 2;
                for (int i = 0; i < city.tiles.Length; i++)
                {
                    if (!Building(i)) State.stock[i] = 0;
                    else if (city.tiles[i] == (int)LandUse.Industrial && city.EntityRoadAccess(i))
                        State.stock[i] = Math.Min(24, State.stock[i] + city.levels[i]);
                }
            }
            // A junction reservation covers the approach and departure, then releases.
            var release = new List<int>();
            foreach (var pair in junctions)
            {
                var owner = State.trips.Find(t => t.id == pair.Value);
                if (owner == null || owner.status != TripStatus.Driving
                    || !(owner.Next == pair.Key && Ahead(owner) <= .36f || owner.Current == pair.Key && Behind(owner) < .4f)) release.Add(pair.Key);
            }
            foreach (int node in release) junctions.Remove(node);
            foreach (var t in State.trips.ToArray()) Move(t);
            if(city.society!=null && city.society.transportEnabled) AdvanceResidents();
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
            // Rotate sources so adding a remote district does not starve its trips.
            for (int k = 0; k < city.tiles.Length; k++)
            {
                int i = State.scheduler;
                State.scheduler = (State.scheduler + 1) % city.tiles.Length;
                if (!Building(i) || !city.EntityRoadAccess(i)) continue;
                var use = (LandUse)city.tiles[i];
                if (use == LandUse.Residential)
                {
                    if(city.society!=null)
                    {
                        // Household cities dispatch through real citizen jobs in AdvanceResidents.
                        continue;
                    }
                    if (State.clock >= State.nextCommute[i] && !BusyHome(i, TripPurpose.Commute))
                    {
                        // Stable preferred workplace per building, with reachable alternatives.
                        for (int j = 0; j < city.tiles.Length; j++)
                        {
                            int target = (i * 37 + j) % city.tiles.Length;
                            if (Building(target) && (city.tiles[target] == 3 || city.tiles[target] == 4)
                                && Dispatch(i, target, TripPurpose.Commute) != null)
                            { State.nextCommute[i] = State.clock + 45; return; }
                        }
                    }
                    if (State.clock >= State.nextShopping[i] && !BusyHome(i, TripPurpose.Shopping))
                        for (int j = 0; j < city.tiles.Length; j++)
                        {
                            int target = (i * 13 + j) % city.tiles.Length;
                            if (city.tiles[target] == 3 && Building(target) && Dispatch(i, target, TripPurpose.Shopping) != null)
                            { State.nextShopping[i] = State.clock + 30; return; }
                        }
                }
                else if (use == LandUse.Commercial && State.stock[i] < 8 && !IncomingCargo(i))
                {
                    for (int j = 0; j < city.tiles.Length; j++)
                        if (city.tiles[j] == 4 && Building(j) && Dispatch(j, i, TripPurpose.Delivery) != null) return;
                    if (Dispatch(Outside, i, TripPurpose.Import) != null) return;
                }
                else if (use == LandUse.Industrial && State.stock[i] >= 20 && !BusyHome(i, TripPurpose.Export))
                    if (Dispatch(i, Outside, TripPurpose.Export) != null) return;
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
            if (t.blocked >= 120 && t.residentId==0) Fail(t);
        }
        void Fail(TrafficTrip t)
        {
            // Undelivered goods are returned to the supplier, never credited to the buyer.
            if (t.cargo > 0 && Building(t.origin)) State.stock[t.origin] = Math.Min(24, State.stock[t.origin] + t.cargo);
            State.failed++; State.trips.Remove(t);
        }

        void Move(TrafficTrip t)
        {
            if(t.residentId>0 && !ValidateResidentTrip(t)) return;
            if (!Endpoint(t.destination) || !Endpoint(t.home)) { Fail(t); return; }
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
                if (back.Count == 0 || !CanEnter(back, t)) { t.blocked += Step; if (t.blocked >= 120) Fail(t); return; }
                t.route = back; t.segment = 0; t.progress = t.blocked = 0; t.status = TripStatus.Driving;
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
            float velocity=t.residentId>0 ? 480f/city.society.settings.secondsPerDay : t.purpose >= TripPurpose.Delivery ? .9f : 1.2f;
            float advance = Step * velocity / units;
            float proposed = Math.Min(1, t.progress + advance);
            foreach (var other in State.trips)
            {
                if (other == t || other.status == TripStatus.Visiting) continue;
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
            if (Behind(t) < .4f && !Reserve(t.Current, t.id)) proposed = t.progress;
            if ((1-proposed)*units <= .35f && !Reserve(t.Next, t.id)) proposed = Math.Min(proposed, Math.Max(0,1-.36f/units));
            // A split edge can leave progress one float below 1. Do not classify
            // the final representable increment as blocked: it must reach the next
            // segment and release its junction reservation.
            if (proposed <= t.progress) { t.blocked += Step; if (t.blocked >= 120 && t.residentId==0) Fail(t); return; }
            t.progress = proposed; t.blocked = 0;
            if (t.progress >= 1) { t.segment++; t.progress = 0; }
        }

        void Arrive(TrafficTrip t)
        {
            if(t.residentId>0) { ArriveResident(t); return; }
            if (t.returning) { State.trips.Remove(t); return; }
            if (t.cargo > 0)
            {
                if (t.destination != Outside) State.stock[t.destination] = Math.Min(32, State.stock[t.destination] + t.cargo);
                State.delivered += t.cargo; t.cargo = 0;
            }
            if (t.purpose == TripPurpose.Shopping)
            {
                if (State.stock[t.destination] <= 0) { Fail(t); return; }
                State.stock[t.destination]--; State.purchases++;
            }
            State.completed++;
            t.status = TripStatus.Visiting;
            t.delay = t.purpose == TripPurpose.Commute ? 10 : t.purpose == TripPurpose.Shopping ? 4 : 2;
            t.blocked = 0;
        }
    }
}
