using System;
using System.Linq;
using System.Reflection;
using HarborCity;

public static class ResidentIdentityChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Resident identity: "+why);checks++;}
    static Household Applicant(CityModel c)=>(Household)typeof(CityModel).GetMethod("NewHousehold",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,null);
    public static void Run()
    {
        var c=TestCity.Empty();c.development.enabled=true;var a=Applicant(c);var b=Applicant(c);Applicant(c);
        int[] original=c.society.families.SelectMany(h=>h.people).Select(p=>p.id).ToArray();
        Check(original.Distinct().Count()==original.Length && original.All(id=>id<c.society.nextCitizenId) && c.Valid(),"Applicants obtain globally independent identities even before arrival");
        var expand=typeof(CityModel).GetMethod("InitializeResidents",BindingFlags.Instance|BindingFlags.NonPublic);expand.Invoke(c,new object[]{a,16});
        Check(a.people.Count==16 && c.society.families.SelectMany(h=>h.people).Select(p=>p.id).Distinct().Count()==c.society.families.Sum(h=>h.people.Count) && c.Valid(),"A family larger than ten members cannot collide with other families");
        Check(original.All(id=>c.society.families.SelectMany(h=>h.people).Any(p=>p.id==id)),"Adding members leaves every pre-existing identity intact");
        int next=c.society.nextCitizenId;var saved=TestCity.RoundTrip(c);var newer=Applicant(saved);
        Check(newer.people.First().id==next && newer.people.All(p=>!c.society.families.SelectMany(h=>h.people).Any(old=>old.id==p.id)) && saved.Valid(),"Saving and loading continues the counter without reconstructing it from household numbers");
        int reserved=saved.AllocateResidentId();int later=saved.AllocateResidentId();var resumed=TestCity.RoundTrip(saved);
        Check(later==reserved+1 && resumed.AllocateResidentId()==later+1 && resumed.Valid(),"Consumed identities remain consumed across reload even when no active person uses them");
        resumed.society.families.Reverse();var last=Applicant(resumed);
        Check(last.people.Select(p=>p.id).All(id=>id>later) && resumed.Valid(),"Family ordering does not determine or reset personal identity");
        saved=TestCity.RoundTrip(c);saved.society.nextCitizenId=saved.society.families.SelectMany(h=>h.people).Max(p=>p.id);
        Check(!saved.Valid(),"Counter rollback into an existing identity is rejected");
        saved=TestCity.RoundTrip(c);saved.society.nextCitizenId=0;Check(!saved.Valid(),"Missing or zero identity state is rejected");
        saved=TestCity.RoundTrip(c);saved.society.families[1].people[0].id=saved.society.families[0].people[0].id;
        Check(!saved.Valid(),"Duplicate identity in different families is rejected");
        saved=TestCity.RoundTrip(c);saved.society.nextCitizenId=int.MaxValue-1;bool exhausted=false;
        try{saved.AllocateResidentId();}catch(InvalidOperationException){exhausted=true;}
        Check(exhausted && saved.society.nextCitizenId==int.MaxValue-1,"Exhaustion cannot overflow or reuse a previous number");
        var live=TestCity.Create();int[] workerIds=live.Citizens.Where(p=>live.ResidentJob(p)!=null).Select(p=>p.id).ToArray();
        var liveSaved=TestCity.RoundTrip(live);Check(workerIds.All(id=>liveSaved.Citizens.Any(p=>p.id==id && liveSaved.ResidentJob(p)!=null)) && liveSaved.Valid(),"Existing job occupants remain linked to stable people after save");
        Console.WriteLine("PASS: "+checks+" independent resident identity/large families/counter/save/contract checks");
    }
}
