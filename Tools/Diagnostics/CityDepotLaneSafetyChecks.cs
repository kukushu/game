using System;
using System.IO;
using System.Linq;
using HarborCity;
static class CityDepotLaneSafetyChecks
{
    static void Main(string[] a)
    {
        var s = CityCoreSave.Read(File.ReadAllBytes(a[0]));
        s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
        for (int i = 0; i < 200; i++)
            s.Simulation.Advance(.05, 1, double.PositiveInfinity);
        s.Transport.DepotLaneRoutingEnabled = true;
        int steps = int.Parse(a[2]);
        for (int i = 0; i < steps; i++)
        {
            if (s.Simulation.Advance(.05, 1, double.PositiveInfinity) != 1)
                throw new Exception("Fixed step lost");
            s.Transport.AssertSafety();
            if (s.Simulation.Waste.BalanceError != 0)
                throw new Exception("Waste ledger changed");
            if (i % 200 == 0)
                s.Validate();
        }
        s.Validate();
        byte[] state = CityCoreSave.Write(s);
        if (!state.SequenceEqual(File.ReadAllBytes(a[1])))
            throw new Exception("Per-step safety observation changed complete final state");
        Console.WriteLine("PASS " + steps + " observed whole steps: vehicle spacing, periodic whole-city validation, per-step waste conservation, exact end-state versus unobserved run. Outside all performance windows.");
    }
}
