using System;
using System.Linq;
using HarborCity;

static class CityScenarioChecks
{
    static int assertions;
    static void Check(bool value,string message){assertions++;if(!value)throw new Exception(message);}
    static void Main(string[] args)
    {
        foreach(var bad in new[]{new CityScenarioSettings{householdsPerResidentialBuilding=0},new CityScenarioSettings{householdsPerResidentialBuilding=9},new CityScenarioSettings{population=0},new CityScenarioSettings{layout="unknown"},new CityScenarioSettings{gridRadius=100},new CityScenarioSettings{warmupDays=30},new CityScenarioSettings{openingPublicGrant=-1}})
        {bool rejected=false;try{bad.Validate();}catch(ArgumentException){rejected=true;}Check(rejected,"Invalid scenario accepted");}
        var settings=new CityScenarioSettings();var s=CityScenarioBuilder.Create(settings);
        Check(s.City.development.enabled,"Normal city systems disabled");Check(s.Entities.Members.Count==500,"Not 500 real residents");
        Check(s.Entities.Members.Count(m=>m.Person.canWork)==334,"Incorrect real workforce");
        Check(s.City.society.jobEntities.Count==336,"Incorrect real job count");
        Check(s.Entities.Members.All(m=>!m.Person.atWork&&m.Person.tripId==0&&m.Person.jobId==-1&&m.Person.location==m.Family.home),"Invented initial attendance, trip or employment");
        Check(s.Entities.Buildings.OfType<IndustrialBuilding>().All(f=>f.Stock==0&&f.factory.raw==0&&f.factory.produced==0&&f.factory.capital==s.Simulation.Design.Production.InitialFactoryCash),"Fake initial production");
        Check(s.Entities.Buildings.OfType<CommercialBuilding>().All(b=>b.Stock==0&&b.Account.Cash==s.Simulation.Design.Retail.InitialShopCash),"Fake initial sales");
        Check(s.Entities.Buildings.All(b=>s.City.Supply(b.id).Complete),"Disconnected initial services");s.Validate();
        byte[] original=CityCoreSave.Write(s);Check(original.SequenceEqual(CityCoreSave.Write(CityScenarioBuilder.Create(settings))),"Seed is not deterministic");
        settings.seed++;Check(!original.SequenceEqual(CityCoreSave.Write(CityScenarioBuilder.Create(settings))),"Seed does not change real city");
        Check(s.City.society.families.Sum(h=>h.people.Count)==500,"Cached population hides missing people");
        if(args.Contains("--scale"))
        {
            var large=CityScenarioBuilder.Create(new CityScenarioSettings{population=3000,householdsPerResidentialBuilding=8,openingPublicGrant=3000000});
            Check(large.Entities.Members.Count==3000,"Scale silently reduced people");
            Check(large.City.society.families.Count==1000,"Scale silently reduced households");
            Check(large.Entities.Buildings.OfType<ResidentialBuilding>().Count()==125,"Expected actual eight-unit apartments");
            Check(large.City.society.families.GroupBy(f=>new{f.home,f.unit}).All(g=>g.Count()==1),"Families share a housing unit");
            Check(large.Entities.Members.Count(m=>m.Person.canWork)==2000&&large.City.society.jobEntities.Count==2000,"Wrong actual workers/jobs");
            Check(large.Entities.Buildings.All(b=>large.City.Supply(b.id).Complete),"Larger fixture utility load not covered");
            byte[] scale=CityCoreSave.Write(large);var restored=CityCoreSave.Read(scale);restored.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
            Check(scale.SequenceEqual(CityCoreSave.Write(restored)),"Scale save/read differs");large.Advance(.05);restored.Advance(.05);large.Validate();restored.Validate();
            Check(CityCoreSave.Write(large).SequenceEqual(CityCoreSave.Write(restored)),"Cold scale restored job assignment/state differs");
        }
        Console.WriteLine("PASS "+assertions+" scenario construction checks; actual city semantics remain covered by the long runner and existing city/core regressions");
    }
}
