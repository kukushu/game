using System;
using System.Linq;
using HarborCity;
using HarborCity.RoadCore;
public static class CityProfileChecks
{
    public static void Main()
    {
        var normal=CityGrowingGridFixture.Create();var profiled=CityCoreSave.Read(CityCoreSave.Write(normal));profiled.Simulation.Growth.Zoning.SetSurface((x,z)=>2);CityCoreProfile.Reset();
        for(int i=0;i<6000;i++){
            CityCoreProfile.Enabled=false;normal.Advance(.05);CityCoreProfile.Enabled=true;profiled.Advance(.05);
            if(i%200==0&&!CityCoreSave.Write(normal).SequenceEqual(CityCoreSave.Write(profiled)))throw new Exception("Profiling changed live city state");
        }
        CityCoreProfile.Enabled=false;normal.Validate();profiled.Validate();
        if(!CityCoreSave.Write(normal).SequenceEqual(CityCoreSave.Write(profiled)))throw new Exception("Profiling changed final state");
        foreach(var part in new[]{CityCoreProfile.Part.Step,CityCoreProfile.Part.Growth,CityCoreProfile.Part.Utilities,CityCoreProfile.Part.Population,CityCoreProfile.Part.Consumption,CityCoreProfile.Part.Trade,CityCoreProfile.Part.Payroll,CityCoreProfile.Part.Finance,CityCoreProfile.Part.FinanceAccrual})if(CityCoreProfile.Calls[(int)part]!=6000)throw new Exception("Unbalanced/missing phase "+part);
        if(CityCoreProfile.Calls[(int)CityCoreProfile.Part.Waste]!=12000)throw new Exception("Waste generation/processing phase missing");
        if(CityCoreProfile.AllocationAvailable&&CityCoreProfile.Bytes[(int)CityCoreProfile.Part.Growth]<=0)throw new Exception("Supported counter missed growth allocations");
        Console.WriteLine("PASS 6000 paired profiling on/off real city steps; all new phase call counts, full-save state and allocation availability");
    }
}
