using System;
using System.Collections.Generic;
using HarborCity.RoadCore;

namespace HarborCity
{
    public sealed class CityMember
    {public Household Family {get;internal set;}public CityResident Person {get;}internal CityMember(Household family,CityResident person){Family=family;Person=person;}}
    // Indexes reference the original facts, never copy people/inventories. Structural
    // changes go through this owner; public legacy lists are not a supported city API.
    public sealed class CityEntities
    {
        readonly CityModel city;readonly World transport;
        readonly Dictionary<int,CityBuilding> buildings=new Dictionary<int,CityBuilding>();
        readonly Dictionary<int,CityMember> people=new Dictionary<int,CityMember>();
        readonly Dictionary<int,Household> families=new Dictionary<int,Household>();
        readonly Dictionary<int,CityJob> jobs=new Dictionary<int,CityJob>();
        readonly List<CityMember> members=new List<CityMember>();
        readonly List<IndustrialBuilding> factories=new List<IndustrialBuilding>();
        readonly List<CommercialBuilding> shops=new List<CommercialBuilding>();
        public IReadOnlyList<CityMember> Members {get;}
        public IReadOnlyList<IndustrialBuilding> Factories {get;}
        public IReadOnlyList<CommercialBuilding> Shops {get;}
        public int Revision {get;private set;}
        public int BuildingRevision {get;private set;}
        internal CitySimulation Simulation;
        internal void StructuralBoundary()
        {if(Simulation!=null&&(Simulation.IsStepping||Simulation.Fault!=null))throw new InvalidOperationException("Structural edits require a healthy city step boundary");}
        internal CityEntities(CityModel city,World transport)
        {
            this.city=city;this.transport=transport;Members=members.AsReadOnly();Factories=factories.AsReadOnly();Shops=shops.AsReadOnly();
            foreach(var b in city.buildings){buildings.Add(b.id,b);IndexType(b);transport.SetEndpointAvailable(b.id,Available(b));}
            foreach(var h in city.society.families){families.Add(h.id,h);foreach(var p in h.people){var m=new CityMember(h,p);people.Add(p.id,m);members.Add(m);}}
            RebuildJobs();city.entities=this;
        }
        static bool Available(CityBuilding b)=>!b.abandoned&&!b.burned;
        void IndexType(CityBuilding b){if(b is IndustrialBuilding f)factories.Add(f);if(b is CommercialBuilding s)shops.Add(s);}
        void UnindexType(CityBuilding b){if(b is IndustrialBuilding f)factories.Remove(f);if(b is CommercialBuilding s)shops.Remove(s);}
        internal void RebuildJobs(){jobs.Clear();foreach(var j in city.society.jobEntities)jobs.Add(j.id,j);}
        public CityBuilding Building(int id)=>buildings.TryGetValue(id,out var b)?b:null;
        public CityJob Job(int id)
        {
            return jobs.TryGetValue(id,out var j)?j:null;
        }
        public CityMember Member(int id)=>people.TryGetValue(id,out var m)?m:null;
        public Household Family(int id)=>families.TryGetValue(id,out var h)?h:null;
        public void AddBuilding(CityBuilding b)
        {StructuralBoundary();if(b==null||b.id<city.nextBuildingId||b.id>=int.MaxValue||buildings.ContainsKey(b.id))throw new ArgumentException("Building ID must be new and monotonic");city.buildings.Add(b);buildings.Add(b.id,b);IndexType(b);city.nextBuildingId=b.id+1;transport.SetEndpointAvailable(b.id,Available(b));Revision++;BuildingRevision++;city.SyncJobs();}
        public void ReplaceBuilding(CityBuilding b)
        {StructuralBoundary();var old=b==null?null:Building(b.id);if(old==null||b.Use!=old.Use||old is IndustrialBuilding)throw new ArgumentException("Replacement preserves identity/use; factory accounts need explicit retirement, not replacement");if(old is CommercialBuilding shop&&b is CommercialBuilding next&&(shop.Stock!=next.Stock||shop.wholesaleCosts!=next.wholesaleCosts))throw new ArgumentException("Replacement cannot discard inventory/finance");int at=city.buildings.IndexOf(old);city.buildings[at]=b;buildings[b.id]=b;UnindexType(old);IndexType(b);transport.SetEndpointAvailable(b.id,Available(b));Revision++;BuildingRevision++;city.SyncJobs();transport.RefreshEndpoints();}
        internal CityBuilding RemoveBuilding(int id)
        {StructuralBoundary();var b=Building(id);if(b==null)throw new ArgumentException("Missing building");city.buildings.Remove(b);buildings.Remove(id);UnindexType(b);transport.SetEndpointAvailable(id,false);Revision++;BuildingRevision++;city.SyncJobs();transport.RefreshEndpoints();return b;}
        public void SetAvailability(int id,bool abandoned,bool burned)
        {StructuralBoundary();var b=Building(id);if(b==null)throw new ArgumentException("Missing building");b.abandoned=abandoned;b.burned=burned;transport.SetEndpointAvailable(id,Available(b));Revision++;BuildingRevision++;city.SyncJobs();transport.RefreshEndpoints();}
        public void AddFamily(Household h)
        {StructuralBoundary();if(h==null||h.id<city.society.nextId||h.id>=int.MaxValue||families.ContainsKey(h.id)||h.people.Count!=0)throw new ArgumentException("Add empty new family first");families.Add(h.id,h);city.society.families.Add(h);city.society.nextId=h.id+1;Revision++;}
        public void AddResident(int familyId,CityResident p)
        {StructuralBoundary();var h=Family(familyId);if(h==null||p==null||p.id<city.society.nextCitizenId||p.id>=int.MaxValue||people.ContainsKey(p.id)||p.tripId!=0||p.jobId>=0)throw new ArgumentException("Invalid new resident");p.householdId=h.id;h.people.Add(p);var m=new CityMember(h,p);members.Add(m);people.Add(p.id,m);city.society.nextCitizenId=p.id+1;Revision++;}
        public void MoveResident(int citizen,int familyId)
        {StructuralBoundary();var m=Member(citizen);var h=Family(familyId);if(m==null||h==null||m.Person.tripId!=0)throw new InvalidOperationException("Cannot move absent/in-transit resident");if(m.Family==h)return;m.Family.people.Remove(m.Person);h.people.Add(m.Person);m.Family=h;m.Person.householdId=h.id;Revision++;}
        public void RemoveResident(int citizen)
        {StructuralBoundary();var m=Member(citizen);if(m==null||m.Person.tripId!=0||m.Person.factoryWageCredit!=0||m.Person.earnedWages!=0)throw new InvalidOperationException("Cannot remove an in-transit resident or discard unclaimed wages");city.ReleaseJob(m.Person);m.Family.people.Remove(m.Person);members.Remove(m);people.Remove(citizen);Revision++;}
        public void Validate()
        {
            if(buildings.Count!=city.buildings.Count||jobs.Count!=city.society.jobEntities.Count||families.Count!=city.society.families.Count)throw new InvalidOperationException("Structural edit bypassed CityEntities");
            foreach(var b in city.buildings)if(!ReferenceEquals(Building(b.id),b)||transport.EndpointExists(b.id)!=Available(b))throw new InvalidOperationException("Stale building identity/availability");
            int count=0;foreach(var h in city.society.families){if(!ReferenceEquals(Family(h.id),h))throw new InvalidOperationException("Stale family");foreach(var p in h.people){count++;var m=Member(p.id);if(m==null||m.Family!=h||m.Person!=p||p.householdId!=h.id)throw new InvalidOperationException("Stale membership");}}
            if(count!=people.Count)throw new InvalidOperationException("Stale person index");foreach(var m in members)if(m.Person.jobId>=0&&(Job(m.Person.jobId)==null||Job(m.Person.jobId).occupiedCitizenId!=m.Person.id))throw new InvalidOperationException("Dangling personal job");foreach(var j in city.society.jobEntities)if(!ReferenceEquals(Job(j.id),j)||Building(j.buildingId)==null||j.occupiedCitizenId>=0&&(Member(j.occupiedCitizenId)==null||Member(j.occupiedCitizenId).Person.jobId!=j.id))throw new InvalidOperationException("Invalid job ownership");
        }
    }
}
