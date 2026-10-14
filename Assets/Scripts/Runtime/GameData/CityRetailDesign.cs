using System;
namespace HarborCity
{
    [Serializable] public sealed class RetailDefinition
    {
        public int unitPrice=60,itemsPerVisit=3,shoppingStartMinute=1020;
        public double initialShopCash=2000,shoppingIntervalSeconds=1,visitSeconds=2;
        public float initialSearchRadius=64;
    }
    public sealed class RetailRules
    {
        public int UnitPrice {get;}
        public int ItemsPerVisit {get;}
        public int ShoppingStartMinute {get;}
        public double InitialShopCash {get;}
        public double VisitSeconds {get;}
        public long ShoppingIntervalTicks {get;}
        public float InitialSearchRadius {get;}
        internal RetailRules(RetailDefinition d)
        {
            if(d==null||d.unitPrice<1||d.unitPrice>1000000||d.itemsPerVisit<1||d.itemsPerVisit>32||d.shoppingStartMinute<0||d.shoppingStartMinute>=1440||!Finite(d.initialShopCash)||d.initialShopCash<0||!Finite(d.visitSeconds)||d.visitSeconds<0||d.visitSeconds>3600||!Finite(d.shoppingIntervalSeconds)||d.shoppingIntervalSeconds<=0||d.shoppingIntervalSeconds>3600)throw new ArgumentException("Invalid retail design data");
            UnitPrice=d.unitPrice;ItemsPerVisit=d.itemsPerVisit;ShoppingStartMinute=d.shoppingStartMinute;InitialShopCash=d.initialShopCash;VisitSeconds=d.visitSeconds;ShoppingIntervalTicks=Math.Max(1,(long)Math.Ceiling(d.shoppingIntervalSeconds/RoadCore.SimulationClock.StepSeconds-1e-9));
            if(d.initialSearchRadius<=0||float.IsNaN(d.initialSearchRadius)||float.IsInfinity(d.initialSearchRadius)||d.initialSearchRadius>1024)throw new ArgumentException("Invalid retail search radius");InitialSearchRadius=d.initialSearchRadius;
        }
        static bool Finite(double n)=>!double.IsNaN(n)&&!double.IsInfinity(n);
        internal RetailDefinition Snapshot()=>new RetailDefinition{unitPrice=UnitPrice,itemsPerVisit=ItemsPerVisit,shoppingStartMinute=ShoppingStartMinute,initialShopCash=InitialShopCash,shoppingIntervalSeconds=ShoppingIntervalTicks*RoadCore.SimulationClock.StepSeconds,visitSeconds=VisitSeconds,initialSearchRadius=InitialSearchRadius};
    }
    [Serializable] public sealed class ProductionDefinition {public double initialFactoryCash=1200,unitProcessingCost=2;public float labourMinutesPerItem=60;}
    public sealed class ProductionRules
    {
        public double InitialFactoryCash {get;}
        public double UnitProcessingCost {get;}
        public float LabourMinutesPerItem {get;}
        internal ProductionRules(ProductionDefinition d)
        {if(d==null||d.initialFactoryCash<0||double.IsNaN(d.initialFactoryCash)||double.IsInfinity(d.initialFactoryCash)||d.unitProcessingCost<0||double.IsNaN(d.unitProcessingCost)||double.IsInfinity(d.unitProcessingCost)||d.labourMinutesPerItem<=0||float.IsNaN(d.labourMinutesPerItem)||float.IsInfinity(d.labourMinutesPerItem))throw new ArgumentException("Invalid production design data");InitialFactoryCash=d.initialFactoryCash;UnitProcessingCost=d.unitProcessingCost;LabourMinutesPerItem=d.labourMinutesPerItem;}
        internal ProductionDefinition Snapshot()=>new ProductionDefinition{initialFactoryCash=InitialFactoryCash,unitProcessingCost=UnitProcessingCost,labourMinutesPerItem=LabourMinutesPerItem};
    }
}
