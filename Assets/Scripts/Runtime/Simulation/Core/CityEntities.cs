using System;
using System.Collections.Generic;
using HarborCity.RoadCore;

namespace HarborCity
{
    public sealed class CityMember
    {public Household Family {get;internal set;}public CityResident Person {get;}internal long Ordinal {get;set;}internal CityMember(Household family,CityResident person,long ordinal){Family=family;Person=person;Ordinal=ordinal;}}
    // Sparse ordered words: a tree node represents up to 64 attendance bits, not
    // one worker. Slots reference the original members; empty words are released.
    internal sealed class CityAttendanceIndex
    {
        internal sealed class Word
        {internal readonly long Key;internal readonly CityMember[] Slots=new CityMember[64];internal ulong Bits;internal int Live;internal Word(long key){Key=key;}}
        readonly Dictionary<long,Word> words=new Dictionary<long,Word>();
        readonly SortedSet<Word> active=new SortedSet<Word>(Comparer<Word>.Create((a,b)=>a.Key.CompareTo(b.Key)));
        internal int Count {get;private set;}
        internal void Add(CityMember member)
        {
            long key=member.Ordinal>>6;if(!words.TryGetValue(key,out var word)){word=new Word(key);words.Add(key,word);}
            int bit=(int)(member.Ordinal&63);if(word.Slots[bit]!=null)throw new InvalidOperationException("Duplicate attendance ordinal");
            word.Slots[bit]=member;word.Live++;Set(member,member.Person.atWork);
        }
        internal void Set(CityMember member,bool value)
        {
            var word=words[member.Ordinal>>6];int bit=(int)(member.Ordinal&63);if(word.Slots[bit]!=member)throw new InvalidOperationException("Attendance slot ownership mismatch");
            ulong mask=1UL<<bit;bool before=(word.Bits&mask)!=0;if(before==value)return;
            if(value){if(word.Bits==0)active.Add(word);word.Bits|=mask;Count++;}
            else{word.Bits&=~mask;Count--;if(word.Bits==0)active.Remove(word);}
        }
        internal bool Contains(CityMember member)=>words.TryGetValue(member.Ordinal>>6,out var word)&&word.Slots[(int)(member.Ordinal&63)]==member&&(word.Bits&(1UL<<(int)(member.Ordinal&63)))!=0;
        internal void Remove(CityMember member)
        {Set(member,false);var word=words[member.Ordinal>>6];word.Slots[(int)(member.Ordinal&63)]=null;if(--word.Live==0)words.Remove(word.Key);}
        internal void Clear(){active.Clear();words.Clear();Count=0;}
        internal Enumerator GetEnumerator()=>new Enumerator(active);
        internal struct Enumerator:IDisposable
        {
            static readonly int[] offsets={0,1,48,2,57,49,28,3,61,58,50,42,38,29,17,4,62,55,59,36,53,51,43,22,45,39,33,30,24,18,12,5,63,47,56,27,60,41,37,16,54,35,52,21,44,32,23,11,46,26,40,15,34,20,31,10,25,14,19,9,13,8,7,6};
            SortedSet<Word>.Enumerator words;Word word;ulong remaining;bool hasWords;
            internal Enumerator(SortedSet<Word> active){hasWords=active.Count!=0;words=hasWords?active.GetEnumerator():default(SortedSet<Word>.Enumerator);word=null;remaining=0;Current=null;}
            internal CityMember Current {get;private set;}
            internal bool MoveNext()
            {
                if(!hasWords)return false;
                while(remaining==0){if(!words.MoveNext()){Current=null;return false;}word=words.Current;remaining=word.Bits;}
                ulong low=unchecked(remaining&(0UL-remaining));int offset=offsets[unchecked((int)((low*0x03f79d71b4cb0a89UL)>>58))];
                remaining&=remaining-1;Current=word.Slots[offset];return true;
            }
            public void Dispose(){if(hasWords)words.Dispose();}
        }
    }
    // Indexes reference the original facts, never copy people/inventories. Structural
    // changes go through this owner; public legacy lists are not a supported city API.
    public sealed class CityEntities
    {
        readonly CityModel city;readonly World transport;
        readonly Dictionary<int,CityBuilding> buildings=new Dictionary<int,CityBuilding>();
        readonly Dictionary<int,CityMember> people=new Dictionary<int,CityMember>();
        readonly Dictionary<int,Household> families=new Dictionary<int,Household>();
        readonly Dictionary<int,CityJob> jobs=new Dictionary<int,CityJob>();
        readonly Dictionary<int,CityOutsideConnection> connections=new Dictionary<int,CityOutsideConnection>();
        readonly List<CityMember> members=new List<CityMember>();
        readonly CityAttendanceIndex workingMembers=new CityAttendanceIndex();
        long nextMemberOrdinal;
        internal CityAttendanceIndex.Enumerator WorkingMembers()=>workingMembers.GetEnumerator();
        internal void WorkAttendanceChanged(CityResident p)
        {
            var member=Member(p.id);if(member==null||member.Person!=p)throw new InvalidOperationException("Attendance changed outside resident ownership");
            workingMembers.Set(member,p.atWork);
        }
        void BindAttendance(CityMember member)
        {member.Person.WorkOwner=this;workingMembers.Add(member);}
        readonly List<IndustrialBuilding> factories=new List<IndustrialBuilding>();
        readonly List<CommercialBuilding> shops=new List<CommercialBuilding>();
        readonly List<CityBuilding> retired=new List<CityBuilding>();
        public IReadOnlyList<CityBuilding> Buildings {get;}
        public IReadOnlyList<CityBuilding> RetiredBuildings {get;}
        public IReadOnlyList<CityMember> Members {get;}
        public IReadOnlyList<IndustrialBuilding> Factories {get;}
        public IReadOnlyList<CommercialBuilding> Shops {get;}
        internal int RoadRevision=>transport.NetworkGeneration;
        public int Revision {get;private set;}
        public int BuildingRevision {get;private set;}
        internal CitySimulation Simulation;
        public EmploymentRules Employment {get;}
        public ProductionRules Production {get;}
        internal void StructuralBoundary()
        {if(Simulation!=null&&(Simulation.IsStepping||Simulation.Fault!=null))throw new InvalidOperationException("Structural edits require a healthy city step boundary");}
        internal CityEntities(CityModel city,World transport,EmploymentRules employment=null,ProductionRules production=null,IEnumerable<CityBuilding> archived=null)
        {
            Employment=employment??new EmploymentRules(new EmploymentDefinition());
            Production=production??new ProductionRules(new ProductionDefinition());
            if(archived!=null)retired.AddRange(archived);RetiredBuildings=retired.AsReadOnly();
            this.city=city;this.transport=transport;Buildings=city.buildings.AsReadOnly();Members=members.AsReadOnly();Factories=factories.AsReadOnly();Shops=shops.AsReadOnly();
            foreach(var b in city.buildings){buildings.Add(b.id,b);IndexType(b);transport.SetEndpointAvailable(b.id,Available(b));}
            foreach(var c in city.trade.connections){if(buildings.ContainsKey(c.id))throw new ArgumentException("Endpoint identity collision");connections.Add(c.id,c);transport.SetEndpointAvailable(c.id,c.active);}
            foreach(var h in city.society.families){families.Add(h.id,h);foreach(var p in h.people){var m=new CityMember(h,p,nextMemberOrdinal++);people.Add(p.id,m);members.Add(m);BindAttendance(m);}}
            RebuildJobs();city.entities=this;
        }
        static bool Available(CityBuilding b)=>!b.abandoned&&!b.burned;
        void IndexType(CityBuilding b){if(b is IndustrialBuilding f)factories.Add(f);if(b is CommercialBuilding s)shops.Add(s);}
        void UnindexType(CityBuilding b){if(b is IndustrialBuilding f)factories.Remove(f);if(b is CommercialBuilding s)shops.Remove(s);}
        internal void RebuildJobs(){jobs.Clear();foreach(var j in city.society.jobEntities)jobs.Add(j.id,j);}
        public CityBuilding Building(int id)=>buildings.TryGetValue(id,out var b)?b:null;
        public CityOutsideConnection Connection(int id)=>connections.TryGetValue(id,out var c)?c:null;
        public void AddConnection(CityOutsideConnection connection)
        {
            StructuralBoundary();if(connection==null||connection.id<city.nextBuildingId||connection.id>=int.MaxValue||connections.ContainsKey(connection.id)||buildings.ContainsKey(connection.id))throw new ArgumentException("Outside endpoint ID must be new and monotonic");
            connections.Add(connection.id,connection);city.trade.AddConnection(connection);city.nextBuildingId=connection.id+1;transport.SetEndpointAvailable(connection.id,connection.active);Revision++;
        }
        internal void CheckNewConnections(IReadOnlyList<CityOutsideConnection> proposed)
        {
            StructuralBoundary();long next=city.nextBuildingId;foreach(var c in proposed){if(c==null||c.id<next||c.id>=int.MaxValue||Building(c.id)!=null||Connection(c.id)!=null||c.flow<OutsideFlow.Import||c.flow>OutsideFlow.Export||float.IsNaN(c.x)||float.IsNaN(c.y)||float.IsNaN(c.z)||float.IsInfinity(c.x)||float.IsInfinity(c.y)||float.IsInfinity(c.z))throw new ArgumentException("Invalid outside endpoint batch");next=(long)c.id+1;}
        }
        public void SetConnectionActive(int id,bool active)
        {StructuralBoundary();var c=Connection(id);if(c==null)throw new ArgumentException("Missing outside connection");c.active=active;transport.SetEndpointAvailable(id,active);Revision++;transport.RefreshEndpoints();}
        internal bool RoadAccess(int id){var b=Building(id);var access=transport.GetAccess(id);return b!=null&&Available(b)&&!b.burning&&access!=null&&access.valid;}
        public CityJob Job(int id)
        {
            return jobs.TryGetValue(id,out var j)?j:null;
        }
        public CityMember Member(int id)=>people.TryGetValue(id,out var m)?m:null;
        public Household Family(int id)=>families.TryGetValue(id,out var h)?h:null;
        public void AddBuilding(CityBuilding b)
        {StructuralBoundary();if(b==null||b.id<city.nextBuildingId||b.id>=int.MaxValue||buildings.ContainsKey(b.id))throw new ArgumentException("Building ID must be new and monotonic");city.buildings.Add(b);buildings.Add(b.id,b);IndexType(b);city.nextBuildingId=b.id+1;transport.SetEndpointAvailable(b.id,Available(b));Revision++;BuildingRevision++;city.SyncJobs();}
        public void ReplaceBuilding(CityBuilding b)
        {StructuralBoundary();var old=b==null?null:Building(b.id);if(old==null||b.Use!=old.Use||old is IndustrialBuilding)throw new ArgumentException("Replacement preserves identity/use; factory accounts need explicit retirement, not replacement");if(old.garbage!=b.garbage||old is LandfillBuilding landfill&&(!(b is LandfillBuilding nextPlant)||landfill.stored!=nextPlant.stored||landfill.processingRemainder!=nextPlant.processingRemainder))throw new ArgumentException("Replacement cannot discard waste inventory");if(old is CommercialBuilding shop&&b is CommercialBuilding next){if(shop.Stock!=next.Stock||shop.wholesaleCosts!=next.wholesaleCosts)throw new ArgumentException("Replacement cannot discard inventory/finance");next.Account=shop.Account;}int at=city.buildings.IndexOf(old);city.buildings[at]=b;buildings[b.id]=b;UnindexType(old);IndexType(b);transport.SetEndpointAvailable(b.id,Available(b));Revision++;BuildingRevision++;city.SyncJobs();transport.RefreshEndpoints();}
        internal CityBuilding RemoveBuilding(int id)
        {StructuralBoundary();var b=Building(id);if(b==null)throw new ArgumentException("Missing building");city.buildings.Remove(b);buildings.Remove(id);UnindexType(b);retired.Add(b);transport.SetEndpointAvailable(id,false);Revision++;BuildingRevision++;city.SyncJobs();transport.RefreshEndpoints();return b;}
        public void SetAvailability(int id,bool abandoned,bool burned)
        {StructuralBoundary();var b=Building(id);if(b==null)throw new ArgumentException("Missing building");b.abandoned=abandoned;b.burned=burned;transport.SetEndpointAvailable(id,Available(b));Revision++;BuildingRevision++;city.SyncJobs();transport.RefreshEndpoints();}
        public void AddFamily(Household h)
        {StructuralBoundary();if(families.Count>=HouseholdState.MaximumFamilies)throw new InvalidOperationException("Household technical capacity reached");if(h==null||h.id<city.society.nextId||h.id>=int.MaxValue||families.ContainsKey(h.id)||h.people.Count!=0)throw new ArgumentException("Add empty new family first");families.Add(h.id,h);city.society.families.Add(h);city.society.nextId=h.id+1;Revision++;}
        public void AddResident(int familyId,CityResident p)
        {StructuralBoundary();var h=Family(familyId);if(h==null||p==null||p.id<city.society.nextCitizenId||p.id>=int.MaxValue||people.ContainsKey(p.id)||p.tripId!=0||p.jobId>=0)throw new ArgumentException("Invalid new resident");p.householdId=h.id;h.people.Add(p);var m=new CityMember(h,p,nextMemberOrdinal++);members.Add(m);people.Add(p.id,m);BindAttendance(m);city.society.nextCitizenId=p.id+1;Revision++;}
        public void MoveResident(int citizen,int familyId)
        {StructuralBoundary();var m=Member(citizen);var h=Family(familyId);if(m==null||h==null||m.Person.tripId!=0)throw new InvalidOperationException("Cannot move absent/in-transit resident");if(m.Family==h)return;m.Family.people.Remove(m.Person);h.people.Add(m.Person);m.Family=h;m.Person.householdId=h.id;Revision++;}
        public void RemoveResident(int citizen)
        {StructuralBoundary();var m=Member(citizen);if(m==null||m.Person.tripId!=0||m.Person.carriedGoods!=0||m.Person.wageCredit!=0||m.Person.earnedWages!=0)throw new InvalidOperationException("Cannot remove an in-transit resident or discard unclaimed wages");city.ReleaseJob(m.Person);city.society.retiredWagesPaid+=m.Person.wagesPaid;m.Family.people.Remove(m.Person);if(m.Family.people.Count==0){m.Family.resident=false;m.Family.home=m.Family.unit=-1;}workingMembers.Remove(m);m.Person.WorkOwner=null;members.Remove(m);people.Remove(citizen);CompactAttendanceOrdinals();Revision++;}
        void CompactAttendanceOrdinals()
        {
            // Bound holes from many generations of removals. This cold structural
            // rebuild is amortized, keeps Members order, and owns no business facts.
            if(nextMemberOrdinal<=2L*members.Count+1024)return;
            workingMembers.Clear();nextMemberOrdinal=0;
            foreach(var member in members){member.Ordinal=nextMemberOrdinal++;workingMembers.Add(member);}
        }
        public void Validate()
        {
            int workingCount=0;long previousOrdinal=-1;
            foreach(var m in members){if(m.Ordinal<=previousOrdinal||m.Person.WorkOwner!=this||workingMembers.Contains(m)!=m.Person.atWork)throw new InvalidOperationException("Stale attendance index");previousOrdinal=m.Ordinal;if(m.Person.atWork)workingCount++;}
            if(workingMembers.Count!=workingCount)throw new InvalidOperationException("Dangling attendance index");
            var archivedIds=new HashSet<int>();foreach(var b in retired)if(b.id<=0||b.id>=city.nextBuildingId||!archivedIds.Add(b.id)||Building(b.id)!=null||Connection(b.id)!=null||transport.EndpointExists(b.id))throw new InvalidOperationException("Invalid retired building identity");
            if(connections.Count!=city.trade.connections.Count)throw new InvalidOperationException("Outside lifecycle bypassed");
            foreach(var c in city.trade.connections)if(c.id<=0||c.id>=city.nextBuildingId||c.flow<OutsideFlow.Import||c.flow>OutsideFlow.Export||Building(c.id)!=null||!ReferenceEquals(Connection(c.id),c)||transport.EndpointExists(c.id)!=c.active||float.IsNaN(c.x)||float.IsNaN(c.y)||float.IsNaN(c.z)||float.IsInfinity(c.x)||float.IsInfinity(c.y)||float.IsInfinity(c.z))throw new InvalidOperationException("Invalid outside identity/availability");
            if(buildings.Count!=city.buildings.Count||jobs.Count!=city.society.jobEntities.Count||families.Count!=city.society.families.Count)throw new InvalidOperationException("Structural edit bypassed CityEntities");
            foreach(var b in city.buildings)if(!ReferenceEquals(Building(b.id),b)||transport.EndpointExists(b.id)!=Available(b))throw new InvalidOperationException("Stale building identity/availability");
            int count=0;foreach(var h in city.society.families){if(!ReferenceEquals(Family(h.id),h))throw new InvalidOperationException("Stale family");foreach(var p in h.people){count++;var m=Member(p.id);if(m==null||m.Family!=h||m.Person!=p||p.householdId!=h.id)throw new InvalidOperationException("Stale membership");}}
            if(count!=people.Count)throw new InvalidOperationException("Stale person index");foreach(var m in members)if(m.Person.jobId>=0&&(Job(m.Person.jobId)==null||Job(m.Person.jobId).occupiedCitizenId!=m.Person.id))throw new InvalidOperationException("Dangling personal job");foreach(var j in city.society.jobEntities)if(!ReferenceEquals(Job(j.id),j)||Building(j.buildingId)==null||j.occupiedCitizenId>=0&&(Member(j.occupiedCitizenId)==null||Member(j.occupiedCitizenId).Person.jobId!=j.id))throw new InvalidOperationException("Invalid job ownership");
        }
    }
}
