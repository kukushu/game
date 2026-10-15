using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarborCity;
using HarborCity.RoadCore;
static class CitySeededNetworkChecks
{
    static byte[] Business(CityCoreIndustrySession s)
    {var snapshot=s.SnapshotBusiness();foreach(var h in snapshot.society.families)foreach(var person in h.people){if(person.observedRevision<0)continue;if(person.observedRevision!=s.Transport.NetworkGeneration)throw new Exception("Unexpected stale observed revision in unchanged fixture");person.observedRevision=0;}using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream)){typeof(CityCoreSave).GetMethod("Put",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{writer,typeof(CityCoreBusinessData),snapshot});writer.Flush();return stream.ToArray();}}
    static byte[] Transport(CityCoreIndustrySession s)
    {var data=s.Transport.Snapshot();data.networkRevision=0;return SaveCodec.Write(new World(data,new SimulationClock(s.Simulation.ClockSnapshot)));}
    static void Main()
    {
        int checks=0;
        foreach(int lanes in new[]{1,2})
        {
            var settings=new CityScenarioSettings{population=60,gridRadius=2,lanesPerDirection=lanes};
            var a=CityScenarioBuilder.Create(settings);settings.seedRoadNetwork=true;var b=CityScenarioBuilder.Create(settings);
            for(int i=0;i<=2400;i++)
            {
                if(i%200==0){a.Validate();b.Validate();if(!Business(a).SequenceEqual(Business(b))||!Transport(a).SequenceEqual(Transport(b))){File.WriteAllBytes("Temp/CityScenarios/seed-before.hcity",CityCoreSave.Write(a));File.WriteAllBytes("Temp/CityScenarios/seed-after.hcity",CityCoreSave.Write(b));File.WriteAllText("Temp/CityScenarios/seed-before-business.json",new System.Web.Script.Serialization.JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Serialize(a.SnapshotBusiness()));File.WriteAllText("Temp/CityScenarios/seed-after-business.json",new System.Web.Script.Serialization.JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Serialize(b.SnapshotBusiness()));throw new Exception("Seeded topology/geometry/finance or actual city continuation differs at "+i+" lanes="+lanes);}checks++;}
                if(i<2400){a.Advance(.05);b.Advance(.05);}
            }
            var saved=CityCoreSave.Write(b);if(!saved.SequenceEqual(CityCoreSave.Write(CityCoreSave.Read(saved))))throw new Exception("Seeded city read/write differs");checks++;
        }
        Console.WriteLine("PASS "+checks+" complete seeded/interactive city comparisons over 4800 steps; only equivalent network/commute-observation revision identities normalized in independent DTOs (validity asserted), no positions/tasks/business ignored");
    }
}
