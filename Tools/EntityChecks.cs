using System;
using System.Linq;
using System.Collections.Generic;
using HarborCity;

public static class EntityChecks
{
    static int checks;
    static void Check(bool ok,string why) {if(!ok)throw new Exception("Entities: "+why);checks++;}
    static void Reject(Action action,string why)
    {bool rejected=false;try {action();}catch(ArgumentException){rejected=true;}Check(rejected,why);}
    public static void Run()
    {
        var c=TestCity.Create();
        Check(c.Valid(),"Current-world fixture validates with sparse IDs");
        Check(typeof(CityBuilding).IsAbstract && typeof(CityBuilding).GetField("factory")==null && typeof(CityBuilding).GetField("housing")==null,"Base entity contains only common data and geometry");
        Check(typeof(CityModel).GetField("tiles")==null && typeof(CityModel).GetField("levels")==null,"No parallel building type or level arrays");
        Check(typeof(TrafficState).GetFields().All(f=>!f.FieldType.IsArray),"Traffic has no building-indexed arrays");
        foreach(var use in new[]{LandUse.Residential,LandUse.Commercial,LandUse.Industrial,LandUse.Power,LandUse.Water,LandUse.Park})
            Check(CityModel.NewBuilding(use).Use==use,"Explicit concrete entity for "+use);
        var first=c.buildings[0]; int id=first.id;
        Check(id>c.buildings.Count && ReferenceEquals(c.GetBuilding(id),first),"ID is independent of list position");
        c.buildings.Reverse();c.Recalculate();
        Check(ReferenceEquals(c.GetBuilding(id),first) && c.Valid(),"Reordering storage preserves lookup and relationships");
        foreach(var h in c.society.families)
            Check(c.GetBuilding(h.home) is ResidentialBuilding && c.HousingCapacity(h.home)==8 && c.Occupancy(h.home)==1,"Household refers to concrete residence by stable ID");
        Check(c.society.jobEntities.All(j=>c.GetBuilding(j.buildingId) is IGoodsBuilding) && c.Citizens.Where(p=>p.jobId>=0).All(p=>c.GetBuilding(c.Workplace(p))!=null),"Job and resident references resolve concrete workplaces");
        var factory=c.buildings.OfType<IndustrialBuilding>().First();var shop=c.buildings.OfType<CommercialBuilding>().First();
        factory.goodsStock=7;shop.stock=11;factory.factory.raw=3;
        Check(c.Goods(factory.id)==7 && c.Goods(shop.id)==11 && c.Inventory(first.id)==null,"Inventory belongs only to industrial/commercial entities");
        var r=(ResidentialBuilding)first;r.vacantDays=14;r.applications=2;r.interestedFamilies.Add(900);r.heavyTraffic=.3f;
        factory.factory.processing=true;factory.factory.progress=17;factory.factory.consumed=1;factory.factory.raw--;
        var copy=TestCity.RoundTrip(c);
        Check(copy.Valid() && copy.GetBuilding(id) is ResidentialBuilding && copy.GetBuilding(factory.id) is IndustrialBuilding && copy.GetBuilding(shop.id) is CommercialBuilding,"Explicit DTO restores concrete subtypes");
        Check(copy.Residence(id).applications==2 && copy.Residence(id).interestedFamilies.SequenceEqual(r.interestedFamilies) && copy.Residence(id).heavyTraffic==.3f,"Residential payload survives serialization");
        Check(copy.Factory(factory.id).progress==17 && copy.Factory(factory.id).processing && copy.Factory(factory.id).raw==2 && copy.Goods(factory.id)==7 && copy.Goods(shop.id)==11,"Industrial batch and both stock owners survive serialization");
        Check(copy.nextBuildingId==c.nextBuildingId && copy.GetBuilding(id).entranceX==first.entranceX && copy.BuildingAccess(id),"IDs, exact frontage and access survive serialization");
        var data=c.ToSaveData();data.format=7;Reject(()=>data.ToCity(),"Old save formats rejected explicitly");
        data=c.ToSaveData();data.buildings[0].type=LandUse.Empty;Reject(()=>data.ToCity(),"Unknown building discriminator rejected");
        data=c.ToSaveData();data.buildings[0].pose.id=data.buildings[1].pose.id;Reject(()=>data.ToCity(),"Duplicate entity IDs rejected before cache creation");
        data=c.ToSaveData();data.nextBuildingId=1;Reject(()=>data.ToCity(),"Future ID collision rejected");
        data=c.ToSaveData();data.buildings.First(b=>b.type==LandUse.Industrial).industrial=null;Reject(()=>data.ToCity(),"Missing industrial subtype payload rejected");
        data=c.ToSaveData();data.buildings.First(b=>b.type==LandUse.Power).industrial=new[]{new FactoryState()};Reject(()=>data.ToCity(),"Foreign subtype payload rejected");
        data=TestCity.RoundTrip(c).ToSaveData();data.society.families[0].people[0].location=999999;Reject(()=>data.ToCity(),"Missing active resident location rejected");
        data=c.ToSaveData();data.retiredFactoryWages=double.NaN;Reject(()=>data.ToCity(),"Malformed closed payroll rejected");

        c=TestCity.Create();var sim=new CityTraffic(c);sim.Advance(45);
        Check(c.traffic.trips.All(t=>t.origin==-1 || c.GetBuilding(t.origin)!=null) && c.Citizens.Any(p=>p.lastCommute>0),"Real transport resolves sparse building IDs");
        factory=c.buildings.OfType<IndustrialBuilding>().First();double paid=factory.factory.wageCosts,credit=c.society.families.SelectMany(h=>h.people).Sum(p=>p.factoryWageCredit);
        var factoryId=factory.id;c.money=100000;
        Check(c.DemolishBuilding(factoryId) && c.GetBuilding(factoryId)==null && !c.buildings.Any(b=>b.id==factoryId),"Demolition removes entity instead of leaving a slot");
        Check(c.society.jobEntities.All(j=>j.buildingId!=factoryId) && c.traffic.trips.All(t=>t.origin!=factoryId && t.destination!=factoryId && t.home!=factoryId),"Demolition cleans jobs and active trip references");
        Check(c.retiredFactoryWages==paid && Math.Abs(credit-c.society.families.SelectMany(h=>h.people).Sum(p=>p.factoryWageCredit))<.0001,"Closing factory preserves paid resident credits and payroll audit");
        Check(c.Valid(),"Demolished factory state remains valid immediately");
        sim.Advance(90);Check(c.Valid(),"Workers and traffic continue after workplace demolition");
        var h0=c.society.families.First(h=>h.resident);id=h0.home;
        Check(c.DemolishBuilding(id) && !h0.resident && h0.home==-1 && h0.people.All(p=>p.jobId<0 && p.tripId==0),"Housing demolition explicitly displaces its household and cancels its trips");
        Check(c.Valid() && TestCity.RoundTrip(c).Valid(),"Demolished city has no invalid saved references");
        int next=c.nextBuildingId;
        int replacement=TestCity.Build(c,-42.137f,7,LandUse.Residential);
        Check(replacement==next && replacement!=id && c.GetBuilding(id)==null,"Rebuilding uses a fresh monotonic ID");
        copy=TestCity.RoundTrip(c);new CityTraffic(copy).Advance(120);Check(copy.Valid(),"Loaded demolished/rebuilt city resumes normally");
        Console.WriteLine("PASS: "+checks+" entity architecture/save/demolition checks");
    }
}
