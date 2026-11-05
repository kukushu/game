using System;
using System.Collections.Generic;
namespace HarborCity
{
    public enum CityFeedbackKind {FoodEmpty,DailyEmpty,Cash,Road,Electricity,Water,Sewage,Garbage,Abandoned,FoodLow,DailyLow}
    [Serializable] public sealed class CityFeedbackSummary
    {
        public long tick,householdFood;
        public int households;
        public int foodEmpty,foodLow,dailyEmpty,dailyLow,cashShops,cashFactories,infrastructure;
        public bool Critical=>foodEmpty+dailyEmpty+cashShops+cashFactories+infrastructure>0;
        public int Count=>foodEmpty+foodLow+dailyEmpty+dailyLow+cashShops+cashFactories+infrastructure;
    }
    [Serializable] public sealed class CityFeedbackEntry
    {
        public CityFeedbackKind kind;public int building,family,affected,severity;
        public bool hasProduct;public ProductKind product;public string reason;public long observedTick;public int stock,replenishmentNeed;public double cash;
        public string Key=>"ui.feedback.kind."+kind.ToString().ToLowerInvariant();
    }
    // One derived flag record per family/building, no duplicate inventories.
    // Callbacks do not query or execute business. Flush only at a complete boundary.
    public sealed class CityPlayerFeedback:IDisposable
    {
        readonly CityCoreIndustrySession session;readonly CityCoreObservation query;
        readonly HashSet<int> dirtyFamilies=new HashSet<int>(),dirtyBuildings=new HashSet<int>();
        readonly Dictionary<int,int> residentFood=new Dictionary<int,int>();
        readonly Dictionary<int,int> familyFlags=new Dictionary<int,int>();
        readonly Dictionary<int,int> familyHomes=new Dictionary<int,int>();
        readonly Dictionary<int,CityObservedProblem> buildingFlags=new Dictionary<int,CityObservedProblem>();
        readonly Dictionary<int,LandUse> emptyAccounts=new Dictionary<int,LandUse>();
        readonly List<CityFeedbackEntry> entries=new List<CityFeedbackEntry>();
        readonly List<int> pending=new List<int>();bool allBuildings=true,entriesDirty=true,disposed,development;
        int network=-1,revision=-1;
        public CityFeedbackSummary Summary {get;}=new CityFeedbackSummary();
        public CityPlayerFeedback(CityCoreIndustrySession session,CityCoreObservation query)
        {
            this.session=session;this.query=query;development=session.City.development.enabled;
            foreach(var h in session.City.society.families)dirtyFamilies.Add(h.id);
            session.Entities.ShoppingFamilyFactsChanged+=Family;
            session.Entities.MemberStructureChanged+=Member;
            session.Entities.ResidentStateChanged+=Resident;
            session.Entities.BusinessStructureChanged+=Structure;
            session.Entities.BusinessFundingBoundaryChanged+=Business;
            session.Simulation.Utilities.SuppliesRebuilt+=Supplies;
            session.Simulation.Waste.WaitingChanged+=Business;
        }
        void Family(Household h){dirtyFamilies.Add(h.id);}
        void Member(CityMember m,bool added){Family(m.Family);}
        void Resident(CityMember m,CityResidentStateChange reason)
        {if(reason==CityResidentStateChange.Death||reason==CityResidentStateChange.Membership||reason==CityResidentStateChange.Residence||reason==CityResidentStateChange.Home)Family(m.Family);}
        void Structure(CityBuilding b,bool added){Business(b.id);}
        void Business(int id){dirtyBuildings.Add(id);}
        void Supplies(){allBuildings=true;}
        void FamilyTotals(int mask,int sign)
        {if((mask&1)!=0)Summary.foodEmpty+=sign;if((mask&2)!=0)Summary.foodLow+=sign;if((mask&4)!=0)Summary.dailyEmpty+=sign;if((mask&8)!=0)Summary.dailyLow+=sign;}
        void ReadFamily(int id)
        {
            int next=0;var h=session.Entities.Family(id);bool occupied=false;
            if(h!=null&&h.resident&&h.home>=0)foreach(var person in h.people)if(!person.dead){occupied=true;break;}
            bool wasOccupied=residentFood.TryGetValue(id,out int previousFood);
            int food=occupied?h.goods.food:0;
            Summary.householdFood+=(long)food-previousFood;
            if(occupied)residentFood[id]=food;
            else if(wasOccupied)residentFood.Remove(id);
            Summary.households=residentFood.Count;
            if(session.City.development.enabled&&h!=null&&h.resident&&h.goodsInitialized&&h.consumptionPeople>0)
            {
                if(h.goods.food==0)next|=1;else if(session.Simulation.Consumption.Need(h,ProductKind.Food)>0)next|=2;
                if(h.goods.dailyGoods==0)next|=4;else if(session.Simulation.Consumption.Need(h,ProductKind.DailyGoods)>0)next|=8;
            }
            familyFlags.TryGetValue(id,out int old);familyHomes.TryGetValue(id,out int home);
            if(old!=next||next!=0&&home!=h.home){entriesDirty=true;FamilyTotals(old,-1);FamilyTotals(next,1);}
            if(next==0){familyFlags.Remove(id);familyHomes.Remove(id);}else{familyFlags[id]=next;familyHomes[id]=h.home;}
        }
        void ReadBuilding(int id)
        {
            var b=session.Entities.Building(id);var next=b==null?CityObservedProblem.None:query.Problems(b);
            buildingFlags.TryGetValue(id,out var old);
            if(old!=next){entriesDirty=true;if(old!=0)Summary.infrastructure--;if(next!=0)Summary.infrastructure++;}
            if(next==0)buildingFlags.Remove(id);else buildingFlags[id]=next;
            bool has=emptyAccounts.TryGetValue(id,out var previous);
            bool empty=session.City.development.enabled&&b!=null&&!b.abandoned&&!b.burned&&(b is IndustrialBuilding f&&f.factory.cash<=0||b is CommercialBuilding s&&s.Account.Cash<=0);
            if(has&&(!empty||previous!=b.Use)){if(previous==LandUse.Commercial)Summary.cashShops--;else Summary.cashFactories--;emptyAccounts.Remove(id);entriesDirty=true;}
            if(empty&&!emptyAccounts.ContainsKey(id)){emptyAccounts.Add(id,b.Use);if(b.Use==LandUse.Commercial)Summary.cashShops++;else Summary.cashFactories++;entriesDirty=true;}
        }
        public void Flush()
        {
            if(disposed||session.Simulation.IsStepping||session.Simulation.IsApplyingBoundary)return;
            if(development!=session.City.development.enabled){development=session.City.development.enabled;foreach(var h in session.City.society.families)dirtyFamilies.Add(h.id);allBuildings=true;}
            if(network!=session.Transport.NetworkGeneration||revision!=session.Entities.BuildingRevision){allBuildings=true;network=session.Transport.NetworkGeneration;revision=session.Entities.BuildingRevision;}
            if(allBuildings){foreach(var b in session.Entities.Buildings)dirtyBuildings.Add(b.id);foreach(int id in buildingFlags.Keys)dirtyBuildings.Add(id);foreach(int id in emptyAccounts.Keys)dirtyBuildings.Add(id);allBuildings=false;}
            pending.Clear();foreach(int id in dirtyFamilies)pending.Add(id);dirtyFamilies.Clear();foreach(int id in pending)ReadFamily(id);
            pending.Clear();foreach(int id in dirtyBuildings)pending.Add(id);dirtyBuildings.Clear();foreach(int id in pending)ReadBuilding(id);
            Summary.tick=session.Simulation.Tick;
        }
        public IReadOnlyList<CityFeedbackEntry> Entries()
        {
            Flush();if(!entriesDirty){RefreshEvidence();return entries;}
            entries.Clear();var grouped=new Dictionary<long,CityFeedbackEntry>();
            foreach(var p in familyFlags)
            {
                int home=familyHomes[p.Key];if(home<0)continue;
                for(int bit=0;bit<4;bit++)if((p.Value&(1<<bit))!=0)
                {
                    var kind=bit==0?CityFeedbackKind.FoodEmpty:bit==1?CityFeedbackKind.FoodLow:bit==2?CityFeedbackKind.DailyEmpty:CityFeedbackKind.DailyLow;
                    long key=((long)home<<8)+(int)kind;
                    if(grouped.TryGetValue(key,out var entry)){entry.affected++;entry.family=Math.Min(entry.family,p.Key);}
                    else{entry=new CityFeedbackEntry{kind=kind,building=home,family=p.Key,affected=1,severity=bit==0||bit==2?2:1};grouped.Add(key,entry);entries.Add(entry);}
                }
            }
            foreach(var p in emptyAccounts)entries.Add(new CityFeedbackEntry{kind=CityFeedbackKind.Cash,building=p.Key,affected=1,severity=2});
            foreach(var p in buildingFlags)foreach(CityObservedProblem flag in Enum.GetValues(typeof(CityObservedProblem)))if(flag!=0&&(p.Value&flag)!=0)entries.Add(new CityFeedbackEntry{kind=(CityFeedbackKind)Enum.Parse(typeof(CityFeedbackKind),flag.ToString()),building=p.Key,affected=1,severity=2});
            entries.Sort((a,b)=>{int c=b.severity.CompareTo(a.severity);if(c==0)c=a.kind.CompareTo(b.kind);if(c==0)c=a.building.CompareTo(b.building);return c!=0?c:a.family.CompareTo(b.family);});
            entriesDirty=false;RefreshEvidence();return entries;
        }
        void RefreshEvidence()
        {
            foreach(var entry in entries)
            {
                entry.observedTick=Summary.tick;
                entry.reason=entry.Key;
                if(entry.family>0)
                {
                    entry.hasProduct=true;entry.product=entry.kind==CityFeedbackKind.FoodEmpty||entry.kind==CityFeedbackKind.FoodLow?ProductKind.Food:ProductKind.DailyGoods;
                    var h=session.Entities.Family(entry.family);if(h!=null){entry.stock=h.goods[entry.product];entry.replenishmentNeed=session.Simulation.Consumption.Need(h,entry.product);}
                }
                else if(entry.kind==CityFeedbackKind.Cash){var b=session.Entities.Building(entry.building);entry.cash=b is CommercialBuilding shop?shop.Account.Cash:(b as IndustrialBuilding)?.factory.cash??0;}
            }
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;
            session.Entities.ShoppingFamilyFactsChanged-=Family;session.Entities.MemberStructureChanged-=Member;session.Entities.ResidentStateChanged-=Resident;session.Entities.BusinessStructureChanged-=Structure;session.Entities.BusinessFundingBoundaryChanged-=Business;
            session.Simulation.Utilities.SuppliesRebuilt-=Supplies;session.Simulation.Waste.WaitingChanged-=Business;
            entries.Clear();residentFood.Clear();Summary.households=0;Summary.householdFood=0;familyFlags.Clear();familyHomes.Clear();buildingFlags.Clear();emptyAccounts.Clear();dirtyFamilies.Clear();dirtyBuildings.Clear();
        }
    }
}
