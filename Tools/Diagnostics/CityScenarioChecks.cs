using System;
using System.Linq;
using HarborCity;

static class CityScenarioChecks
{
    static int assertions;
    static void Check(bool value,string message){assertions++;if(!value)throw new Exception(message);}
    static void Main()
    {
        foreach(var bad in new[]{new CityScenarioSettings{population=0},new CityScenarioSettings{layout="unknown"},new CityScenarioSettings{gridRadius=100},new CityScenarioSettings{warmupDays=30},new CityScenarioSettings{openingPublicGrant=-1}})
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
        Console.WriteLine("PASS "+assertions+" scenario construction checks; actual city semantics remain covered by the long runner and existing city/core regressions");
    }
}
