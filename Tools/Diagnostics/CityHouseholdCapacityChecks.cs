using System;
using System.IO;
using System.Linq;
using HarborCity;
// Capacity/lifecycle unit fixture only: unhoused child records are NOT a functioning city benchmark.
static class CityHouseholdCapacityChecks
{
    static void Require(bool ok,string reason){if(!ok)throw new Exception(reason);}
    static void Main(string[] args)
    {
        var s=CityCoreIndustrySession.CreateEmpty();
        for(int i=0;i<10001;i++)
        {
            var h=new Household{id=s.City.society.nextId,openingSavings=600};s.Entities.AddFamily(h);
            s.Entities.AddResident(h.id,new CityResident{id=s.City.society.nextCitizenId,name="Capacity child",age=10,canWork=false});
        }
        s.Validate();byte[] saved=CityCoreSave.Write(s);var restored=CityCoreSave.Read(saved);restored.Validate();
        Require(saved.SequenceEqual(CityCoreSave.Write(restored)),"Above-old-limit round trip differs");
        Require(restored.Entities.Members.Count==10001&&restored.City.society.families.Count==10001,"Records lost above former ceiling");
        // Continue through the lifecycle owner to the exact named resource boundary.
        for(int i=10001;i<HouseholdState.MaximumFamilies;i++)
        {
            var h=new Household{id=s.City.society.nextId,openingSavings=600};s.Entities.AddFamily(h);
            s.Entities.AddResident(h.id,new CityResident{id=s.City.society.nextCitizenId,name="Capacity child",age=10,canWork=false});
        }
        s.Validate();int next=s.City.society.nextId,revision=s.Entities.Revision;bool rejected=false;
        try{s.Entities.AddFamily(new Household{id=next});}catch(InvalidOperationException){rejected=true;}
        Require(rejected&&s.City.society.nextId==next&&s.Entities.Revision==revision&&s.City.society.families.Count==HouseholdState.MaximumFamilies,"Overflow partially committed");
        Require(s.Entities.Family(next)==null,"Overflow indexed rejected family");s.Validate();
        Console.WriteLine("PASS capacity: 10001 real member/family records round trip; exact 100000-family guard atomic. Synthetic unit fixture, NOT city performance/health acceptance.");
    }
}

