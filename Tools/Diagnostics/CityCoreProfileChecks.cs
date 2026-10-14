using System;
using System.IO;
using HarborCity;
using HarborCity.RoadCore;
public static class CityCoreProfileChecks
{
    public static void Main(string[] args)
    {
        var s=CityIntegratedScaleFixture.Create(3000);for(int i=0;i<20;i++)s.Advance(.05);CityCoreProfile.Enabled=true;
        using(var output=new StreamWriter(args[0])){output.WriteLine(CityCoreProfile.Header());for(int i=0;i<200;i++){CityCoreProfile.Reset();int a=GC.CollectionCount(0),b=GC.CollectionCount(1),c=GC.CollectionCount(2);long r=s.Transport.RoutePreparations;s.Advance(.05);output.WriteLine(CityCoreProfile.Row(s.Simulation.Tick,s.Transport.ActiveCount,s.Transport.RoutePreparations-r,GC.CollectionCount(0)-a,GC.CollectionCount(1)-b,GC.CollectionCount(2)-c));}}
        CityCoreProfile.Enabled=false;s.Validate();Console.WriteLine("PASS real 3000-resident attribution; profile observes no decisions. "+args[0]);
    }
}
