using System;
using HarborCity;

public static class TrafficChecks
{
    static int checks;
    static int I(int x, int z) => CityModel.Index(x,z);
    static void Check(bool ok, string message) { if (!ok) throw new Exception("Traffic: " + message); checks++; }
    static CityModel Corridor()
    {
        var c = new CityModel();
        for (int x = 0; x < 36; x++) c.tiles[I(x,18)] = 1;
        c.tiles[I(1,17)] = 2; c.levels[I(1,17)] = 1;
        c.tiles[I(34,19)] = 4; c.levels[I(34,19)] = 1;
        c.tiles[I(30,17)] = 3; c.levels[I(30,17)] = 1;
        c.Recalculate(); return c;
    }
    static CityTraffic Manual(CityModel c)
    {
        var sim = new CityTraffic(c);
        sim.State.dispatchTimer = -10000; sim.State.productionTimer = -10000;
        return sim;
    }
    static void Until(CityTraffic sim, Func<bool> done, float seconds)
    {
        for (int i = 0; i < seconds * 20 && !done(); i++) sim.Advance(.05f);
        Check(done(), "Expected task transition within " + seconds + " seconds");
    }
    public static void Run()
    {
        var c = Corridor(); var sim = Manual(c);
        var trip = sim.Dispatch(I(1,17),I(34,19),TripPurpose.Commute);
        Check(trip != null && trip.route.Count == 34, "Cross-map trip has complete route");
        sim.Advance(0); Check(trip.progress == 0 && sim.State.clock == 0, "Paused traffic does not advance");
        sim.Advance(8); Check(trip.Current % 36 > 8, "Car leaves its initial neighborhood");
        Until(sim, () => trip.status == TripStatus.Visiting, 40);
        Check(sim.State.completed == 1 && trip.Current == I(34,18), "Arrival counted at destination only");
        Until(sim, () => trip.returning && trip.status == TripStatus.Driving, 15);
        Check(trip.destination == I(1,17), "Commute returns to its actual home");
        Until(sim, () => sim.State.trips.Count == 0, 40);
        Check(sim.State.completed == 1, "Return does not double-count outbound completion");

        c = Corridor(); sim = Manual(c); trip = sim.Dispatch(I(1,17),I(34,19),TripPurpose.Commute);
        sim.Advance(3); c.tiles[I(18,18)] = 0;
        sim.Advance(2);
        Check(trip.status == TripStatus.Waiting && sim.State.completed == 0, "Severed route waits without false completion");
        int stopped = trip.Current;
        sim.Advance(2); Check(trip.Current == stopped, "No teleport across disconnected road");
        c.tiles[I(18,18)] = 1;
        Until(sim, () => trip.status == TripStatus.Visiting, 40);

        c = Corridor(); sim = Manual(c);
        for (int x = 16; x <= 20; x++) c.tiles[I(x,20)] = 1;
        c.tiles[I(16,19)] = c.tiles[I(20,19)] = 1;
        trip = sim.Dispatch(I(1,17),I(34,19),TripPurpose.Commute); sim.Advance(2);
        c.tiles[I(18,18)] = 0; sim.Advance(1);
        Check(trip.route.Contains(I(18,20)) && !trip.route.Contains(I(18,18)), "Demolition replans via surviving detour");
        Until(sim, () => trip.status == TripStatus.Visiting, 50);

        c = Corridor(); sim = Manual(c); sim.State.stock[I(34,19)] = 8;
        trip = sim.Dispatch(I(34,19),I(30,17),TripPurpose.Delivery);
        Check(trip != null && sim.State.stock[I(34,19)] == 0 && sim.State.stock[I(30,17)] == 0, "Cargo reserved at dispatch, not delivered early");
        Until(sim, () => trip.status == TripStatus.Visiting, 10);
        Check(sim.State.stock[I(30,17)] == 8 && sim.State.delivered == 8, "Delivery adds actual inventory");
        var shopper = sim.Dispatch(I(1,17),I(30,17),TripPurpose.Shopping);
        Until(sim, () => shopper.status == TripStatus.Visiting, 35);
        Check(sim.State.stock[I(30,17)] == 7 && sim.State.purchases == 1, "Shopping consumes inventory on arrival");

        c = Corridor(); sim = Manual(c); sim.State.stock[I(34,19)] = 8;
        trip = sim.Dispatch(I(34,19),I(30,17),TripPurpose.Delivery);
        c.tiles[I(30,17)] = 0; sim.Advance(.1f);
        Check(sim.State.failed == 1 && sim.State.completed == 0 && sim.State.stock[I(34,19)] == 8, "Deleted destination fails and refunds undelivered cargo");
        c = Corridor(); sim = Manual(c); trip = sim.Dispatch(I(1,17),I(34,19),TripPurpose.Commute);
        c.tiles[trip.Current] = 0; sim.Advance(121);
        Check(sim.State.failed == 1 && sim.State.completed == 0, "Road deleted underneath car times out as failure");

        c = Corridor(); sim = Manual(c);
        var first = sim.Dispatch(I(1,17),I(34,19),TripPurpose.Commute);
        Check(sim.Dispatch(I(1,17),I(34,19),TripPurpose.Commute) == null, "Occupied spawn lane prevents overlap");
        sim.Advance(1);
        var second = sim.Dispatch(I(1,17),I(34,19),TripPurpose.Commute);
        var opposing = sim.Dispatch(I(34,19),I(1,17),TripPurpose.Commute);
        Check(second != null && opposing != null, "Following and opposing traffic can spawn");
        bool separated = true;
        for (int i = 0; i < 400; i++)
        {
            sim.Advance(.05f);
            if (first.status == TripStatus.Driving && second.status == TripStatus.Driving)
                separated &= first.segment + first.progress - second.segment - second.progress >= .47f;
        }
        Check(separated, "Following vehicles maintain minimum gap");
        Until(sim, () => sim.State.completed == 3, 25);

        c = Corridor(); sim = Manual(c);
        c.tiles[I(1,19)] = 3; c.levels[I(1,19)] = 1; sim.State.stock[I(1,19)] = 1;
        trip = sim.Dispatch(I(1,17),I(1,19),TripPurpose.Shopping); sim.Advance(.05f);
        Check(trip.status == TripStatus.Visiting && sim.State.purchases == 1, "Shared access cell still completes a real local trip");

        c = CityModel.Create(); sim = new CityTraffic(c);
        var purposes = new bool[5]; int maxDistance = 0;
        for (int i = 0; i < 4800; i++)
        {
            sim.Advance(.05f);
            foreach (var t in sim.State.trips) { purposes[(int)t.purpose] = true; maxDistance = Math.Max(maxDistance,t.route.Count); }
        }
        Check(Array.TrueForAll(purposes,p => p), "Starter city organically generates commute, shopping, delivery, import and export");
        Check(maxDistance >= 20 && sim.State.completed > 40 && sim.State.purchases > 0 && sim.State.delivered > 0,
            "Sustained city traffic crosses map and fulfills real demands");
        Check(sim.State.trips.Count <= CityTraffic.Capacity && c.Valid(), "Long simulation remains bounded and saveable");
        Console.WriteLine("Traffic stress: completed=" + sim.State.completed + ", failed=" + sim.State.failed + ", purchases=" + sim.State.purchases + ", goods=" + sim.State.delivered);

        var c2 = CityModel.Create(); var a = new CityTraffic(c2);
        var c3 = CityModel.Create(); var b = new CityTraffic(c3);
        for (int i = 0; i < 600; i++) a.Advance(.05f);
        for (int i = 0; i < 100; i++) b.Advance(.3f);
        Check(a.State.completed == b.State.completed && a.State.trips.Count == b.State.trips.Count
            && Math.Abs(a.State.clock - b.State.clock) < .01f, "Fixed-step outcomes independent of render frame/batch size");
        Check(CityTraffic.Valid(null), "Legacy save without traffic accepted");
        a.State.trips[0].progress = float.NaN;
        Check(!CityTraffic.Valid(a.State), "Invalid traffic save rejected");
        Console.WriteLine("PASS: " + checks + " traffic checks");
    }
}
