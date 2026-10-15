using System;
using System.Linq;
using HarborCity;

static class CityWasteDiagnosticsChecks
{
    static void Main()
    {
        var normal=CityGrowingGridFixture.Create();CityGrowingGridFixture.PlaceWasteFacility(normal);
        var observed=CityCoreSave.Read(CityCoreSave.Write(normal));observed.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
        var diagnostics=new CityWasteDiagnostics();observed.Simulation.Waste.Diagnostics=diagnostics;
        for(int i=0;i<12000;i++)
        {
            normal.Advance(.05);observed.Advance(.05);
            if(i%200==0&&!CityCoreSave.Write(normal).SequenceEqual(CityCoreSave.Write(observed)))throw new Exception("Waste diagnostics changed city facts");
            if(i==6000){observed=CityCoreSave.Read(CityCoreSave.Write(observed));observed.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);observed.Simulation.Waste.Diagnostics=diagnostics;}
        }
        normal.Validate();observed.Validate();var s=diagnostics.Snapshot();
        if(!CityCoreSave.Write(normal).SequenceEqual(CityCoreSave.Write(observed)))throw new Exception("Waste diagnostics changed continuation");
        if(s.Sum(x=>x.collected)!=observed.Simulation.Waste.Collected||s.Sum(x=>x.processed)!=observed.Simulation.Waste.Processed||s.Sum(x=>x.delivered)!=observed.Simulation.Waste.Collected-observed.Simulation.Waste.InTransit||s.Sum(x=>x.partialLegs)!=0)throw new Exception("Waste measurements missed/replayed work");
        if(s.Sum(x=>x.pickups)<=0||s.Sum(x=>x.outboundDistance)<=0||s.Sum(x=>x.returnDistance)<=0)throw new Exception("No real waste transport measured");
        Console.WriteLine("PASS 12000 paired diagnostics on/off real city steps, in-flight restore and independent ledger/route counters");
    }
}
