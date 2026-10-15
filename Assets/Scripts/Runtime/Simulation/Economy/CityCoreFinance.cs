using System;
using System.Collections.Generic;
using System.Linq;
using HarborCity.RoadCore;
namespace HarborCity
{
    [Serializable] public sealed class CoreLoanData {public int nextId;public List<CityLoan> contracts;}
    [Serializable] public sealed class TreasuryData
    {
        public double maintenanceAccrued;
        public int openingMoney,lastSettledDay,lastTax,lastMaintenance,lastLoanPayment;
        public long publicWorks,taxCollected,maintenancePaid,loanIssued,loanRepaid,developmentGrants,uncollectedAssessment;
    }
    // This ledger accounts for changes to CityModel.money; it is not a second balance.
    public sealed class CityCoreFinance
    {
        readonly CityModel city;readonly CityEntities entities;readonly World world;readonly FinanceRules rules;
        int roadVersion=-1,buildingVersion=-1,powerVersion=-1,waterVersion=-1,powerBudget=-1,waterBudget=-1,cachedRate;CityUtilityNetwork cachedPower,cachedWater;
        readonly TreasuryData state;readonly Dictionary<int,int> workers=new Dictionary<int,int>();readonly List<Charge> charges=new List<Charge>();
        struct Charge {public Household family;public CityBuilding business;public int paid;}
        public int Money=>city.money;public int LastTax=>state.lastTax;public int LastMaintenance=>state.lastMaintenance;public int LastLoanPayment=>state.lastLoanPayment;
        public double MaintenanceAccrued=>state.maintenanceAccrued;
        public int MaintenanceRate=>CurrentMaintenanceRate();
        public long PublicWorks=>state.publicWorks;public long TaxesCollected=>state.taxCollected;public long MaintenancePaid=>state.maintenancePaid;public long UncollectedAssessment=>state.uncollectedAssessment;
        public int Tax(LandUse use)=>use==LandUse.Residential?city.development.residentialTax:use==LandUse.Commercial?city.development.commercialTax:city.development.industrialTax;
        public int Budget(CityServiceKind kind)=>city.ServiceBudget(kind);
        public int ActiveLoanId(int offer)=>ActiveLoan(offer)?.id??0;
        public int Remaining(int id){foreach(var l in city.loans.contracts)if(l.id==id)return Remaining(l);return 0;}
        public IReadOnlyList<LoanTerms> Offers=>rules.Loans;
        public long Debt {get{long sum=0;foreach(var loan in city.loans.contracts)sum+=Remaining(loan);return sum;}}
        internal CityCoreFinance(CityModel city,CityEntities entities,World world,FinanceRules rules,TreasuryData saved)
        {this.city=city;this.entities=entities;this.world=world;this.rules=rules;state=saved??new TreasuryData{openingMoney=city.money,lastSettledDay=city.day};state=Snapshot();city.income=state.lastTax;city.upkeep=state.lastMaintenance;}
        public TreasuryData Snapshot()=>new TreasuryData{maintenanceAccrued=state.maintenanceAccrued,openingMoney=state.openingMoney,lastSettledDay=state.lastSettledDay,lastTax=state.lastTax,lastMaintenance=state.lastMaintenance,lastLoanPayment=state.lastLoanPayment,publicWorks=state.publicWorks,taxCollected=state.taxCollected,maintenancePaid=state.maintenancePaid,loanIssued=state.loanIssued,loanRepaid=state.loanRepaid,developmentGrants=state.developmentGrants,uncollectedAssessment=state.uncollectedAssessment};
        internal void CheckSpend(int amount){entities.StructuralBoundary();if(amount<0||city.money<amount)throw new CityRuleException("finance.insufficient_funds");if(state.publicWorks>long.MaxValue-amount)throw new InvalidOperationException("Public works ledger overflow");}
        internal void SpendApproved(int amount){city.money-=amount;state.publicWorks+=amount;}
        public void GrantDevelopmentFunds(int amount=100000){entities.StructuralBoundary();if(amount<1||(long)city.money+amount>int.MaxValue||state.developmentGrants>long.MaxValue-amount)throw new CityRuleException("finance.balance_limit");city.money+=amount;state.developmentGrants+=amount;city.Trace("finance.grant","finance.event.grant",new CityLogDetail{amount=amount});}
        public void SetTax(LandUse use,int percent)
        {
            entities.StructuralBoundary();if(!city.development.enabled||percent<0||percent>29)throw new CityRuleException("finance.invalid_tax");
            if(use==LandUse.Residential)city.development.residentialTax=percent;else if(use==LandUse.Commercial)city.development.commercialTax=percent;else if(use==LandUse.Industrial)city.development.industrialTax=percent;else throw new CityRuleException("finance.invalid_tax");city.Trace("finance.tax_policy","finance.event.tax",new CityLogDetail{origin=(int)use,amount=percent});
        }
        internal CityLoan ActiveLoan(int offer){foreach(var loan in city.loans.contracts)if(loan.offerId==offer&&Remaining(loan)>0)return loan;return null;}
        public int Remaining(CityLoan loan)=>loan.total-loan.paid;
        public void Borrow(int offer)
        {
            entities.StructuralBoundary();if(!city.development.enabled||offer<0||offer>=rules.Loans.Count)throw new CityRuleException("finance.invalid_loan");if(ActiveLoan(offer)!=null)throw new CityRuleException("finance.loan_active");
            var quote=rules.Loans[offer];if(city.loans.contracts.Count>=10000||city.loans.nextId>=int.MaxValue||(long)city.money+quote.Principal>int.MaxValue)throw new CityRuleException("finance.balance_limit");
            var loan=new CityLoan{id=city.loans.nextId,offerId=offer,takenDay=city.day,principal=quote.Principal,total=quote.Total,days=quote.Days};city.loans.contracts.Add(loan);city.loans.nextId++;city.money+=quote.Principal;state.loanIssued+=quote.Principal;city.Trace("finance.borrow","finance.event.borrow",new CityLoan{id=loan.id,offerId=offer,takenDay=loan.takenDay,principal=loan.principal,total=loan.total,days=loan.days});
        }
        public void Repay(int id)
        {
            entities.StructuralBoundary();CityLoan target=null;foreach(var l in city.loans.contracts)if(l.id==id){target=l;break;}if(target==null||Remaining(target)<=0)throw new CityRuleException("finance.invalid_loan");int amount=Remaining(target);if(city.money<amount)throw new CityRuleException("finance.insufficient_funds");city.money-=amount;target.paid+=amount;state.loanRepaid+=amount;city.Trace("finance.repay","finance.event.repay",new CityLogDetail{origin=id,amount=amount});
        }
        static int Scheduled(CityLoan loan,int day)=>(int)((long)loan.total*Math.Min(loan.days,Math.Max(0,day-loan.takenDay))/loan.days);
        public int QuoteMaintenance()
        {
            double roads=0;foreach(int id in world.Network.RoadIds)roads+=world.Network.Centreline(id).Length*world.Network.Settings(id).lanes*rules.RoadRate;
            long total=(long)Math.Ceiling(roads);foreach(var b in entities.Buildings){var kind=b.Use==LandUse.Power?CityServiceKind.Electricity:CityServiceKind.Water;total+=b.Use==LandUse.Landfill?rules.FacilityMaintenance(b.Use):city.ServiceExpense(kind,rules.FacilityMaintenance(b.Use));}
            foreach(var kind in new[]{UtilityKind.Electricity,UtilityKind.Water}){var net=city.UtilityNetwork(kind);foreach(var edge in net.edges)total+=(long)Math.Ceiling(CityRoads.Length(net.Node(edge.a),net.Node(edge.b))*rules.UtilityRate);}
            if(total>int.MaxValue)throw new InvalidOperationException("Maintenance exceeds supported treasury range");return (int)total;
        }
        int CurrentMaintenanceRate()
        {
            var power=city.utilities.electricity;var water=city.utilities.water;int pb=city.development.electricityBudget,wb=city.development.waterBudget;
            if(roadVersion!=world.NetworkGeneration||buildingVersion!=entities.BuildingRevision||!ReferenceEquals(cachedPower,power)||!ReferenceEquals(cachedWater,water)||powerVersion!=power.revision||waterVersion!=water.revision||powerBudget!=pb||waterBudget!=wb){cachedRate=QuoteMaintenance();roadVersion=world.NetworkGeneration;buildingVersion=entities.BuildingRevision;cachedPower=power;cachedWater=water;powerVersion=power.revision;waterVersion=water.revision;powerBudget=pb;waterBudget=wb;}
            return cachedRate;
        }
        internal void Accrue(TimeContext time)
        {
#if CITY_CORE_ATTRIBUTION
            using var observation=CityCoreProfile.Measure(CityCoreProfile.Part.FinanceAccrual);
#endif
            if(city.development.enabled)state.maintenanceAccrued+=CurrentMaintenanceRate()*time.Duration/city.society.settings.secondsPerDay;
        }
        internal void SettleDay()
        {
            if(!city.development.enabled||city.day<=state.lastSettledDay)return;
            charges.Clear();workers.Clear();foreach(var job in city.society.jobEntities)if(job.occupiedCitizenId>=0){workers.TryGetValue(job.buildingId,out int n);workers[job.buildingId]=n+1;}
            long assessed=0,collected=0;foreach(var family in city.society.families)if(family.resident){int people=0;foreach(var p in family.people)if(!p.dead)people++;long due=(long)people*rules.ResidentialBase*city.development.residentialTax/100;int paid=(int)Math.Min(due,family.savings);assessed+=due;collected+=paid;charges.Add(new Charge{family=family,paid=paid});}
            foreach(var b in entities.Buildings)if(b is IndustrialBuilding||b is CommercialBuilding){workers.TryGetValue(b.id,out int n);long due=(long)n*(b is IndustrialBuilding?rules.IndustrialBase:rules.CommercialBase)*(b is IndustrialBuilding?city.development.industrialTax:city.development.commercialTax)/100;double cash=b is IndustrialBuilding f?f.factory.cash:((CommercialBuilding)b).Account.Cash;int paid=(int)Math.Min(due,Math.Min(int.MaxValue,Math.Floor(cash)));assessed+=due;collected+=paid;charges.Add(new Charge{business=b,paid=paid});}
            if(double.IsNaN(state.maintenanceAccrued)||double.IsInfinity(state.maintenanceAccrued)||state.maintenanceAccrued<0||state.maintenanceAccrued>int.MaxValue)throw new InvalidOperationException("Invalid accrued maintenance");int maintenance=(int)Math.Floor(state.maintenanceAccrued+1e-8);long loanPayment=0;foreach(var loan in city.loans.contracts)loanPayment+=Math.Max(0,Scheduled(loan,city.day)-loan.paid);
            long closing=(long)city.money+collected-maintenance-loanPayment;if(closing<int.MinValue||closing>int.MaxValue||collected>int.MaxValue||loanPayment>int.MaxValue)throw new InvalidOperationException("Fiscal settlement exceeds supported balance range");
            checked{_ = state.taxCollected+collected;_ = state.maintenancePaid+maintenance;_ = state.loanRepaid+loanPayment;_ = state.uncollectedAssessment+assessed-collected;}
            foreach(var charge in charges)if(charge.family!=null&&charge.family.taxPaid>long.MaxValue-charge.paid)throw new InvalidOperationException("Taxpayer ledger overflow");
            foreach(var charge in charges){if(charge.family!=null){charge.family.savings-=charge.paid;charge.family.taxPaid+=charge.paid;}else if(charge.business is IndustrialBuilding f){f.factory.cash-=charge.paid;f.factory.taxCosts+=charge.paid;}else{var a=((CommercialBuilding)charge.business).Account;a.Cash-=charge.paid;a.TaxCosts+=charge.paid;}}
            foreach(var loan in city.loans.contracts)loan.paid+=Math.Max(0,Scheduled(loan,city.day)-loan.paid);
            city.money=(int)closing;state.maintenanceAccrued=Math.Max(0,state.maintenanceAccrued-maintenance);state.taxCollected+=collected;state.maintenancePaid+=maintenance;state.loanRepaid+=loanPayment;state.uncollectedAssessment+=assessed-collected;state.lastTax=(int)collected;state.lastMaintenance=maintenance;state.lastLoanPayment=(int)loanPayment;state.lastSettledDay=city.day;city.loans.lastSettledDay=city.day;city.income=state.lastTax;city.upkeep=maintenance;if(city.logSink!=null||city.analysis!=null)city.Trace("finance.day","finance.event.day",Snapshot());
        }
        static long WholeTax(double amount)
        {if(double.IsNaN(amount)||double.IsInfinity(amount)||amount<0||amount!=Math.Truncate(amount))throw new InvalidOperationException("Tax counters must be nonnegative whole currency");return checked((long)amount);}
        public void Validate()
        {
            if(city.loans==null||city.loans.contracts==null||city.loans.nextId<1||city.loans.contracts.Count>10000||new[]{city.development.residentialTax,city.development.commercialTax,city.development.industrialTax}.Any(n=>n<0||n>29)||new[]{city.development.electricityBudget,city.development.waterBudget}.Any(n=>n<50||n>150))throw new InvalidOperationException("Invalid fiscal policy/contract collection");
            if(double.IsNaN(state.maintenanceAccrued)||double.IsInfinity(state.maintenanceAccrued)||state.maintenanceAccrued<0||state.maintenanceAccrued>int.MaxValue)throw new InvalidOperationException("Invalid accrued maintenance");
            long expected=checked((long)state.openingMoney+state.developmentGrants+state.taxCollected+state.loanIssued-state.publicWorks-state.maintenancePaid-state.loanRepaid);if(expected!=city.money)throw new InvalidOperationException("Treasury ledger mismatch; write through finance commands");
            if(state.lastTax<0||state.lastTax>state.taxCollected||state.lastMaintenance<0||state.lastMaintenance>state.maintenancePaid||state.lastLoanPayment<0||state.lastLoanPayment>state.loanRepaid||state.lastSettledDay<1||state.lastSettledDay>city.day||state.publicWorks<0||state.taxCollected<0||state.maintenancePaid<0||state.loanIssued<0||state.loanRepaid<0||state.developmentGrants<0||state.uncollectedAssessment<0)throw new InvalidOperationException("Invalid fiscal ledger");
            var ids=new HashSet<int>();var active=new HashSet<int>();long issued=0,repaid=0;foreach(var loan in city.loans.contracts){if(loan.id<1||loan.id>=city.loans.nextId||!ids.Add(loan.id)||loan.offerId<0||loan.offerId>2||loan.principal<=0||loan.total!=rules.Loans[loan.offerId].Total||loan.principal!=rules.Loans[loan.offerId].Principal||loan.days!=rules.Loans[loan.offerId].Days||loan.days<1||loan.takenDay<1||loan.takenDay>city.day||loan.paid<0||loan.paid>loan.total||loan.paid<Scheduled(loan,state.lastSettledDay)||Remaining(loan)>0&&!active.Add(loan.offerId))throw new InvalidOperationException("Invalid saved loan contract");issued+=loan.principal;repaid+=loan.paid;}if(issued!=state.loanIssued||repaid!=state.loanRepaid)throw new InvalidOperationException("Loan cash ledger mismatch");
            long tax=0;foreach(var h in city.society.families){if(h.savings<0||h.taxPaid<0)throw new InvalidOperationException("Negative household funds/tax counter");tax=checked(tax+h.taxPaid);}foreach(var b in entities.Buildings)tax=checked(tax+WholeTax(b is IndustrialBuilding f?f.factory.taxCosts:b is CommercialBuilding s?s.Account.TaxCosts:0));foreach(var b in entities.RetiredBuildings)tax=checked(tax+WholeTax(b is IndustrialBuilding f?f.factory.taxCosts:b is CommercialBuilding s?s.Account.TaxCosts:0));if(tax!=state.taxCollected)throw new InvalidOperationException("Taxpayer/treasury transfer mismatch");
        }
    }
}
