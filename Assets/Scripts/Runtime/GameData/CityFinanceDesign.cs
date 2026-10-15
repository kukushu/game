using System;
using System.Linq;
namespace HarborCity
{
    [Serializable] public sealed class LoanDefinition
    {
        public int id,principal,interestPercent,days;
        public static LoanDefinition[] Defaults()=>new[]{new LoanDefinition{id=0,principal=20000,interestPercent=5,days=364},new LoanDefinition{id=1,principal=60000,interestPercent=10,days=1820},new LoanDefinition{id=2,principal=200000,interestPercent=15,days=3640}};
    }
    [Serializable] public sealed class FinanceDefinition
    {
        public int startingTreasury=65000;
        public int defaultResidentialTax=9,defaultCommercialTax=9,defaultIndustrialTax=9,defaultElectricityBudget=100,defaultWaterBudget=100;
        public int residentialAssessmentPerPerson=18,commercialAssessmentPerWorker=160,industrialAssessmentPerWorker=180;
        public int electricityMaintenance=90,waterMaintenance=65,sewageMaintenance=65;
        public double roadMaintenancePerLaneUnit=.32/1.5/7,utilityMaintenancePerUnit=1.0/30;
        public LoanDefinition[] loans=LoanDefinition.Defaults();
    }
    public sealed class LoanTerms
    {
        public int Id {get;} public int Principal {get;} public int InterestPercent {get;} public int Days {get;} public int Total {get;}
        internal LoanTerms(LoanDefinition d){if(d==null||d.id<0||d.id>2||d.principal<1||d.principal>10000000||d.interestPercent<0||d.interestPercent>100||d.days<1||d.days>100000)throw new ArgumentException("Invalid loan design");Id=d.id;Principal=d.principal;InterestPercent=d.interestPercent;Days=d.days;Total=checked(Principal+(int)((long)Principal*InterestPercent/100));}
        internal LoanDefinition Snapshot()=>new LoanDefinition{id=Id,principal=Principal,interestPercent=InterestPercent,days=Days};
    }
    public sealed class FinanceRules
    {
        readonly FinanceDefinition d;readonly LoanTerms[] loans;
        public System.Collections.Generic.IReadOnlyList<LoanTerms> Loans {get;}
        public int StartingTreasury=>d.startingTreasury;
        public int DefaultResidentialTax=>d.defaultResidentialTax;public int DefaultCommercialTax=>d.defaultCommercialTax;public int DefaultIndustrialTax=>d.defaultIndustrialTax;public int DefaultElectricityBudget=>d.defaultElectricityBudget;public int DefaultWaterBudget=>d.defaultWaterBudget;
        public int ResidentialBase=>d.residentialAssessmentPerPerson;public int CommercialBase=>d.commercialAssessmentPerWorker;public int IndustrialBase=>d.industrialAssessmentPerWorker;
        public double RoadRate=>d.roadMaintenancePerLaneUnit;public double UtilityRate=>d.utilityMaintenancePerUnit;
        internal FinanceRules(FinanceDefinition data)
        {
            if(data==null||data.startingTreasury<0||new[]{data.defaultResidentialTax,data.defaultCommercialTax,data.defaultIndustrialTax}.Any(n=>n<0||n>29)||new[]{data.defaultElectricityBudget,data.defaultWaterBudget}.Any(n=>n<50||n>150)||new[]{data.residentialAssessmentPerPerson,data.commercialAssessmentPerWorker,data.industrialAssessmentPerWorker,data.electricityMaintenance,data.waterMaintenance,data.sewageMaintenance}.Any(n=>n<0||n>1000000)||!ValidRate(data.roadMaintenancePerLaneUnit)||!ValidRate(data.utilityMaintenancePerUnit)||data.loans==null||data.loans.Length!=3)throw new ArgumentException("Invalid fiscal design");
            loans=data.loans.Select(x=>new LoanTerms(x)).OrderBy(x=>x.Id).ToArray();if(loans.Where((x,i)=>x.Id!=i).Any())throw new ArgumentException("Loan quote IDs must be 0,1,2");Loans=Array.AsReadOnly(loans);
            d=new FinanceDefinition{startingTreasury=data.startingTreasury,defaultResidentialTax=data.defaultResidentialTax,defaultCommercialTax=data.defaultCommercialTax,defaultIndustrialTax=data.defaultIndustrialTax,defaultElectricityBudget=data.defaultElectricityBudget,defaultWaterBudget=data.defaultWaterBudget,residentialAssessmentPerPerson=data.residentialAssessmentPerPerson,commercialAssessmentPerWorker=data.commercialAssessmentPerWorker,industrialAssessmentPerWorker=data.industrialAssessmentPerWorker,electricityMaintenance=data.electricityMaintenance,waterMaintenance=data.waterMaintenance,sewageMaintenance=data.sewageMaintenance,roadMaintenancePerLaneUnit=data.roadMaintenancePerLaneUnit,utilityMaintenancePerUnit=data.utilityMaintenancePerUnit,loans=loans.Select(x=>x.Snapshot()).ToArray()};
        }
        static bool ValidRate(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x)&&x>=0&&x<=1000000;
        public int FacilityMaintenance(LandUse use)=>use==LandUse.Power?d.electricityMaintenance:use==LandUse.Water?d.waterMaintenance:use==LandUse.Sewage?d.sewageMaintenance:0;
        internal FinanceDefinition Snapshot()=>new FinanceDefinition{startingTreasury=d.startingTreasury,defaultResidentialTax=d.defaultResidentialTax,defaultCommercialTax=d.defaultCommercialTax,defaultIndustrialTax=d.defaultIndustrialTax,defaultElectricityBudget=d.defaultElectricityBudget,defaultWaterBudget=d.defaultWaterBudget,residentialAssessmentPerPerson=d.residentialAssessmentPerPerson,commercialAssessmentPerWorker=d.commercialAssessmentPerWorker,industrialAssessmentPerWorker=d.industrialAssessmentPerWorker,electricityMaintenance=d.electricityMaintenance,waterMaintenance=d.waterMaintenance,sewageMaintenance=d.sewageMaintenance,roadMaintenancePerLaneUnit=RoadRate,utilityMaintenancePerUnit=UtilityRate,loans=loans.Select(x=>x.Snapshot()).ToArray()};
    }
}
