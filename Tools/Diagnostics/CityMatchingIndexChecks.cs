using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using HarborCity;
using HarborCity.RoadCore;

// Compile against frozen/current model sources. No alternate simulator or rules.
static class CityMatchingIndexChecks
{
    static int checks;
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool value,string reason){checks++;if(!value)throw new Exception(reason);}
    static string Hash(CityCoreIndustrySession s)=>Convert.ToHexString(SHA256.HashData(CityCoreSave.Write(s)));
    static object Tag(CityFreightCandidates cache)
    {
        var lanes=(IDictionary)typeof(CityFreightCandidates).GetField("lanes",Private).GetValue(cache);
        foreach(DictionaryEntry pair in lanes){var list=(IList)pair.Value;if(list.Count>0)return list[0];}
        throw new Exception("Fixture needs at least one indexed shop");
    }
    static void Oracle(CityCoreIndustrySession s,RoadAccess source,List<CommercialBuilding> actual)
    {
        var expected=s.Entities.Shops.Where(shop=>{var a=s.Transport.GetAccess(shop.id);return a!=null&&a.valid&&(a.use&AccessUse.Freight)!=0&&s.Transport.Network.CanReach(source,a);}).ToArray();
        Check(actual.SequenceEqual(expected),"Candidate identity/order differs from independent live-shop reachability scan");
    }
    static void Targets(string output)
    {
        var rows=new List<string>{"case,sha256"};var s=CityCoreIndustrySession.Create();
        s.City.Residence(3).housingUnits=2;s.Entities.AddFamily(new Household{id=2,home=3,unit=1,resident=true});
        s.City.GetBuilding(1).level=2;s.City.SyncJobs();
        var p=new CityResident{id=s.City.society.nextCitizenId,age=30,skill=2,canWork=true,location=3};s.Entities.AddResident(1,p);
        int vacancy=s.City.society.jobEntities.First(j=>j.buildingId==1&&j.occupiedCitizenId<0).id;
        string before=Hash(s);
        Check(!s.City.AssignJob(null,vacancy),"Null applicant accepted");
        Check(!s.City.AssignJob(new CityResident{id=p.id,householdId=p.householdId,age=30,skill=2,canWork=true},vacancy),"Imposter identity accepted");
        var family=s.Entities.Family(1);family.resident=false;Check(!s.City.AssignJob(p,vacancy),"Nonresident accepted");family.resident=true;
        p.householdId=2;Check(!s.City.AssignJob(p,vacancy),"Wrong family identity accepted");p.householdId=1;
        family.people.Remove(p);Check(!s.City.AssignJob(p,vacancy),"Missing local membership accepted");family.people.Add(p);
        Check(Hash(s)==before,"Rejected ownership checks changed facts");
        p.age=17;Check(!s.City.AssignJob(p,vacancy),"Minor accepted");p.age=30;
        p.skill=0;Check(!s.City.AssignJob(p,vacancy),"Skill rule changed");p.skill=2;
        p.canWork=false;Check(!s.City.AssignJob(p,vacancy),"Work eligibility changed");p.canWork=true;
        Check(s.City.AssignJob(p,vacancy),"Valid indexed applicant rejected");Check(s.City.AssignJob(p,vacancy),"Idempotent assignment rejected");
        s.Entities.MoveResident(p.id,2);Check(s.City.AssignJob(p,vacancy),"Moved indexed applicant rejected");
        s.City.ReleaseJob(p);Check(s.City.AssignJob(p,vacancy),"Released vacancy cannot be assigned");
        s.City.ReleaseJob(p);s.Entities.RemoveResident(p.id);Check(!s.City.AssignJob(p,vacancy),"Removed resident accepted");s.Validate();rows.Add("membership,"+Hash(s));
        // The fixture constructor assigns four jobs before CityEntities exists.
        Check(s.Entities.Members.Take(4).All(m=>s.City.ResidentJob(m.Person)!=null),"Pre-index construction fallback changed");

        s=CityCoreIndustrySession.Create();var cache=new CityFreightCandidates(s.Entities,s.Transport);var list=new List<CommercialBuilding>();
        Action collect=()=>{cache.Collect(s.Transport.GetAccess(1),list);Oracle(s,s.Transport.GetAccess(1),list);};collect();
        Action<string,Action,bool> change=(name,mutation,structural)=>{
            object tag=Tag(cache);mutation();collect();
#if MATCHING_CANDIDATE
            Check(ReferenceEquals(tag,Tag(cache))!=structural,"Wrong cache invalidation: "+name);
#else
            // The reference deliberately has broad entity invalidation.
            if(structural)Check(!ReferenceEquals(tag,Tag(cache)),"Frozen cache failed required invalidation: "+name);
#endif
            rows.Add(name+","+Hash(s));
        };
        change("family-add",()=>{s.City.Residence(3).housingUnits=2;s.Entities.AddFamily(new Household{id=2,home=3,unit=1,resident=true});},false);
        var newcomer=new CityResident{id=5,age=30,skill=2,canWork=true,location=3};
        change("resident-add",()=>s.Entities.AddResident(1,newcomer),false);
        change("resident-move",()=>s.Entities.MoveResident(5,2),false);
        change("resident-remove",()=>s.Entities.RemoveResident(5),false);
        int home=s.City.nextBuildingId;
        change("unrelated-building",()=>s.Entities.AddBuilding(new ResidentialBuilding{id=home,x=200,z=200}),false);
        int shopId=s.City.nextBuildingId;
        change("shop-add",()=>s.Entities.AddBuilding(new CommercialBuilding{id=shopId,x=20,z=-12}),true);
        change("shop-register",()=>s.Transport.RegisterAccess(shopId,s.Transport.GetAccess(2).lane,15),true);
        var original=s.Entities.Building(2);
        change("shop-replace-reorders",()=>s.Entities.ReplaceBuilding(new CommercialBuilding{id=2,x=original.x,z=original.z,entranceX=original.entranceX,entranceZ=original.entranceZ}),true);
        Check(ReferenceEquals(list.Last(),s.Entities.Building(2)),"Replacement lost canonical shop order/reference");
        int restricted=s.City.nextBuildingId;
        change("resident-only-shop-add",()=>s.Entities.AddBuilding(new CommercialBuilding{id=restricted,x=24,z=-12}),true);
        change("access-permission",()=>s.Transport.RegisterAccess(restricted,s.Transport.GetAccess(2).lane,20,AccessUse.Resident),true);
        Check(!list.Any(shop=>shop.id==restricted),"Resident-only entrance became a freight candidate");
        // Existing candidates do not filter endpoint availability; dispatch owns it.
        change("shop-closed",()=>s.Entities.SetAvailability(2,true,false),false);
        change("shop-reopened",()=>s.Entities.SetAvailability(2,false,false),false);
        change("road-split",()=>s.Transport.SplitRoadAt(2,17),true);
        object stable=Tag(cache);bool rejected=false;try{s.Transport.RegisterAccess(shopId,s.Transport.GetAccess(1).lane,-1);}catch(ArgumentException){rejected=true;}
        Check(rejected,"Invalid access fixture accepted");collect();Check(ReferenceEquals(stable,Tag(cache)),"Failed access registration invalidated/mutated cache");
        var shops=(List<CommercialBuilding>)typeof(CityEntities).GetField("shops",Private).GetValue(s.Entities);shops.Reverse();
        cache.Collect(s.Transport.GetAccess(1),list);bool caught=false;try{Oracle(s,s.Transport.GetAccess(1),list);}catch(Exception e)when(e.Message=="Candidate identity/order differs from independent live-shop reachability scan"){caught=true;}
        Check(caught,"Independent order oracle missed bypassed structural notification");shops.Reverse();collect();
        change("shop-remove",()=>s.Entities.RemoveBuilding(shopId),true);s.Validate();
        var cold=CityCoreSave.Read(CityCoreSave.Write(s));var coldCache=new CityFreightCandidates(cold.Entities,cold.Transport);coldCache.Collect(cold.Transport.GetAccess(1),list);Oracle(cold,cold.Transport.GetAccess(1),list);
        Check(Hash(cold)==Hash(s),"Cold cache rebuild changed complete city facts");
        File.WriteAllLines(output,rows);Console.WriteLine("PASS matching membership/lifecycle/order/access/topology/cold/negative-control; "+checks+" checks");
    }
    static void RebindCheck(string output)
    {
        var list=new List<CommercialBuilding>();var rebind=CityCoreIndustrySession.Create();var rebindCache=new CityFreightCandidates(rebind.Entities,rebind.Transport);
        rebindCache.Collect(rebind.Transport.GetAccess(1),list);Check(list.Count==1,"Rebind fixture lacks initial shop");
        rebind.Transport.RemoveRoad(2);rebindCache.Collect(rebind.Transport.GetAccess(1),list);Check(list.Count==0,"Deleted road left stale shop access");
        Check(rebind.Transport.RebindAccess(2,rebind.Transport.GetAccess(1).lane,25),"Legal shop rebind failed");
        rebindCache.Collect(rebind.Transport.GetAccess(1),list);Oracle(rebind,rebind.Transport.GetAccess(1),list);Check(list.Count==1,"Rebound shop missing from reused network cache");
        File.WriteAllText(output,"PASS same-network access rebind invalidates freight entries; full state "+Hash(rebind));
        Console.WriteLine("PASS same-network road access rebind");
    }
    static void Bench(string save,string output)
    {
        var s=CityCoreSave.Read(File.ReadAllBytes(save));var rows=new List<string>{"part,iterations,milliseconds,gc_bytes,rebuilds,population"};
        var p=s.Entities.Members.Last(m=>m.Person.jobId>=0&&m.Family.resident).Person;int job=p.jobId;bool expected=s.City.AssignJob(p,job);string state=Hash(s);
        for(int i=0;i<1000;i++)s.City.AssignJob(p,job);
        for(int repeat=0;repeat<5;repeat++){
            long bytes=GC.GetAllocatedBytesForCurrentThread(),began=Stopwatch.GetTimestamp();
            for(int i=0;i<10000;i++)if(s.City.AssignJob(p,job)!=expected)throw new Exception("Idempotent assignment changed");
            double elapsed=(Stopwatch.GetTimestamp()-began)*1000.0/Stopwatch.Frequency;bytes=GC.GetAllocatedBytesForCurrentThread()-bytes;rows.Add(string.Join(",","assign-"+repeat,10000,elapsed.ToString("R",CultureInfo.InvariantCulture),bytes,-1,s.City.population));
        }
        Check(Hash(s)==state,"Assignment benchmark changed facts");
        var source=s.Entities.Factories.Select(f=>s.Transport.GetAccess(f.id)).First(a=>a!=null&&a.valid&&(a.use&AccessUse.Freight)!=0);
        var cache=new CityFreightCandidates(s.Entities,s.Transport);var list=new List<CommercialBuilding>();cache.Collect(source,list);Oracle(s,source,list);
        var member=s.Entities.Members.Last(m=>m.Person.tripId==0&&m.Family.resident);int homeFamily=member.Family.id;
        int other=s.Entities.Members.First(m=>m.Family.resident&&m.Family.id!=homeFamily).Family.id;
        foreach(bool mutate in new[]{false,true}){
            int rebuilds=0;double ms=0;long bytes=0;object tag=Tag(cache);
            for(int i=0;i<50;i++){
                long start=GC.GetAllocatedBytesForCurrentThread(),began=Stopwatch.GetTimestamp();
                if(mutate){s.Entities.MoveResident(member.Person.id,other);s.Entities.MoveResident(member.Person.id,homeFamily);}
                cache.Collect(source,list);ms+=(Stopwatch.GetTimestamp()-began)*1000.0/Stopwatch.Frequency;bytes+=GC.GetAllocatedBytesForCurrentThread()-start;
                object next=Tag(cache);if(!ReferenceEquals(next,tag))rebuilds++;tag=next;
            }
            Oracle(s,source,list);rows.Add(string.Join(",",mutate?"freight-unrelated-membership":"freight-unchanged",50,ms.ToString("R",CultureInfo.InvariantCulture),bytes,rebuilds,s.City.population));
        }
        File.WriteAllLines(output,rows);Console.WriteLine("PASS bounded real-save component benchmark; not Player FPS");
    }
    static void Main(string[] args)
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
        if(args[0]=="targets")Targets(args[1]);else if(args[0]=="rebind")RebindCheck(args[1]);else if(args[0]=="bench")Bench(args[1],args[2]);else throw new ArgumentException("targets, rebind or bench");
    }
}
