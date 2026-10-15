using System;

namespace HarborCity
{
    [Serializable]
    public sealed class EconomyDefinition
    {
        public double rawUnitPrice=8,goodsWholesalePrice=45;
        public double outsideOpeningCash=100000;
        public int freightBatch=8,rawReorderTarget=24,exportThreshold=16,maxGateTrucks=4;
        public double tradeIntervalSeconds=1;
    }

    [Serializable]
    public sealed class GameDesignData
    {
        public int schemaVersion=3,contentVersion=3;
        public string profile="core-economy";
        public EconomyDefinition economy=new EconomyDefinition();
        public EmploymentDefinition employment=new EmploymentDefinition();
        public RetailDefinition retail=new RetailDefinition();
        public ProductionDefinition production=new ProductionDefinition();
        public FinanceDefinition finance=new FinanceDefinition();
    }

    // Authoring DTOs never become mutable live rules. The running city owns this
    // immutable snapshot and saves it, so editing an asset cannot reinterpret a
    // purchase or a future arrival in an already saved city.
    public sealed class EconomyRules
    {
        public double RawUnitPrice {get;}
        public double GoodsWholesalePrice {get;}
        public double OutsideOpeningCash {get;}
        public int FreightBatch {get;}
        public int RawReorderTarget {get;}
        public int ExportThreshold {get;}
        public int MaxGateTrucks {get;}
        public long TradeIntervalTicks {get;}
        internal EconomyRules(EconomyDefinition definition)
        {
            if(definition==null||!Positive(definition.rawUnitPrice)||!Positive(definition.goodsWholesalePrice)||!Positive(definition.outsideOpeningCash)||definition.freightBatch<1||definition.freightBatch>FactoryState.RawCapacity||definition.rawReorderTarget<1||definition.rawReorderTarget>FactoryState.RawCapacity||definition.exportThreshold<definition.freightBatch||definition.exportThreshold>FactoryState.GoodsCapacity||definition.maxGateTrucks<1||definition.maxGateTrucks>10000||!Positive(definition.tradeIntervalSeconds)||definition.tradeIntervalSeconds>3600)
                throw new ArgumentException("Invalid economy design data");
            RawUnitPrice=definition.rawUnitPrice;GoodsWholesalePrice=definition.goodsWholesalePrice;OutsideOpeningCash=definition.outsideOpeningCash;
            FreightBatch=definition.freightBatch;RawReorderTarget=definition.rawReorderTarget;ExportThreshold=definition.exportThreshold;MaxGateTrucks=definition.maxGateTrucks;
            TradeIntervalTicks=Math.Max(1,(long)Math.Ceiling(definition.tradeIntervalSeconds/RoadCore.SimulationClock.StepSeconds-1e-9));
        }
        static bool Positive(double n)=>n>0&&!double.IsNaN(n)&&!double.IsInfinity(n);
        internal EconomyDefinition Snapshot()=>new EconomyDefinition{rawUnitPrice=RawUnitPrice,goodsWholesalePrice=GoodsWholesalePrice,outsideOpeningCash=OutsideOpeningCash,freightBatch=FreightBatch,rawReorderTarget=RawReorderTarget,exportThreshold=ExportThreshold,maxGateTrucks=MaxGateTrucks,tradeIntervalSeconds=TradeIntervalTicks*RoadCore.SimulationClock.StepSeconds};
    }

    public sealed class CityGameDesign
    {
        readonly int contentVersion;
        readonly string profile;
        public EconomyRules Economy {get;}
        public EmploymentRules Employment {get;}
        public RetailRules Retail {get;}
        public ProductionRules Production {get;}
        public FinanceRules Finance {get;}
        public CityGameDesign(GameDesignData definition)
        {
            if(definition==null||definition.schemaVersion!=3||definition.contentVersion<1||string.IsNullOrWhiteSpace(definition.profile))throw new ArgumentException("Unsupported design profile");
            contentVersion=definition.contentVersion;profile=definition.profile;Economy=new EconomyRules(definition.economy);
            Employment=new EmploymentRules(definition.employment);Retail=new RetailRules(definition.retail);Production=new ProductionRules(definition.production);Finance=new FinanceRules(definition.finance);
        }
        public GameDesignData Snapshot()=>new GameDesignData{schemaVersion=3,contentVersion=contentVersion,profile=profile,economy=Economy.Snapshot(),employment=Employment.Snapshot(),retail=Retail.Snapshot(),production=Production.Snapshot(),finance=Finance.Snapshot()};
    }

    public sealed class CityRuleException:InvalidOperationException
    {
        public string Key {get;}
        public object[] Arguments {get;}
        public CityRuleException(string key,params object[] arguments):base(key){Key=key;Arguments=arguments;}
    }
}
