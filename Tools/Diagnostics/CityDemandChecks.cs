using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using HarborCity;
using HarborCity.RoadCore;
static class CityDemandChecks
{
    static int checks;
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    static int[] Reference(CityModel c)=>new[]{Math.Clamp(70-c.buildings.OfType<ResidentialBuilding>().Sum(b=>c.HousingCapacity(b.id)-c.Occupancy(b.id))*12+c.society.jobEntities.Count(j=>j.occupiedCitizenId<0)*2-c.Unemployed*4,0,100),Math.Clamp(30+c.population*3-c.buildings.OfType<CommercialBuilding>().Sum(b=>c.JobCapacity(b.id))*6,0,100),Math.Clamp(40+c.Unemployed*8-c.society.jobEntities.Count(j=>j.occupiedCitizenId<0)*8,0,100)};
    static void Compare(CityModel c){var expected=Reference(c);c.DevelopmentDemand();Check(expected.SequenceEqual(new[]{c.residentialDemand,c.commercialDemand,c.industrialDemand}),"Demand changed");}
    static void SmallCases()
    {
        // Unsaturated demand values expose count errors that a large city's
        // clamped zero/100 readings could hide. Deliberately exercise read filters.
        for(int seed=0;seed<120;seed++)
        {
            var c=new CityModel();c.buildings.Clear();c.society.families.Clear();c.society.jobEntities.Clear();c.population=seed%20;
            c.buildings.Add(new ResidentialBuilding{id=1,housingUnits=seed%5,abandoned=seed%7==0});
            c.buildings.Add(new ResidentialBuilding{id=2,housingUnits=2});
            c.buildings.Add(new CommercialBuilding{id=3,abandoned=seed%3==0});
            c.buildings.Add(new IndustrialBuilding{id=4});
            for(int j=0;j<seed%10;j++)c.society.jobEntities.Add(new CityJob{id=j+1,buildingId=j%3==0?3:4,occupiedCitizenId=j%2==0?j+1:-1});
            for(int h=0;h<seed%8;h++)
            {var f=new Household{id=h+1,home=h%3==0?99:h%2+1,unit=(h+seed)%5-1,resident=h%3!=1};f.people.Add(new CityResident{id=h+1,householdId=h+1,canWork=h%2==0,dead=h%5==0,jobId=h+1});c.society.families.Add(f);}
            Compare(c);
        }
        Console.WriteLine("PASS "+checks+" unsaturated/filter demand cases");
    }
    static void Main(string[] args)
    {
        SmallCases();if(args.Length==1&&args[0]=="--small")return;
        var bytes=File.ReadAllBytes(args[0]);var s=CityCoreSave.Read(bytes);s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);var c=s.City;
        Directory.CreateDirectory(args[1]);Compare(c);
        using(var report=new StreamWriter(Path.Combine(args[1],"demand.csv"))){report.WriteLine("query,iteration,ms,allocated_bytes");
            for(int i=0;i<8;i++){long allocation=GC.GetAllocatedBytesForCurrentThread();var timer=Stopwatch.StartNew();c.DevelopmentDemand();timer.Stop();report.WriteLine("actual,"+i+","+timer.Elapsed.TotalMilliseconds.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+(GC.GetAllocatedBytesForCurrentThread()-allocation));}}
        Check(bytes.SequenceEqual(CityCoreSave.Write(s)),"Read-only demand changed saved facts");
        // Keep original population/jobs/household membership; perturb legitimate
        // vacancy filters on a separate loaded object, never the source save.
        var edge=CityCoreSave.Read(bytes);var homes=edge.City.buildings.OfType<ResidentialBuilding>().Take(4).ToArray();var households=edge.City.society.families.Take(8).ToArray();
        Compare(edge.City);homes[0].abandoned=true;Compare(edge.City);homes[1].housingUnits=0;Compare(edge.City);
        households[0].resident=false;households[1].unit=-1;households[2].unit=int.MaxValue;Compare(edge.City);
        homes[0].abandoned=false;homes[1].housingUnits=2;households[0].resident=true;Compare(edge.City);
        for(int i=0;i<240;i++)s.Advance(.05);s.Validate();File.WriteAllBytes(Path.Combine(args[1],"continued.hcity"),CityCoreSave.Write(s));
        var restored=CityCoreSave.Read(CityCoreSave.Write(s));for(int i=0;i<20;i++){s.Advance(.05);restored.Advance(.05);}s.Validate();restored.Validate();Check(CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(restored)),"Continuation restore changed full city");
        Console.WriteLine("PASS "+checks+" demand assertions, 260 full-city steps, in-flight cold restore; output="+args[1]);
    }
}
