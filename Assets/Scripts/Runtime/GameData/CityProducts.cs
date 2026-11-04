using System;
namespace HarborCity
{
    public enum ProductKind { Food=0, DailyGoods=1 }
    // A value payload: copies cannot alias another owner's mutable inventory.
    // All domain loops use the catalog; the two-slot representation is localized here.
    [Serializable] public struct GoodsAmounts : IEquatable<GoodsAmounts>
    {
        public int food,dailyGoods;
        public int Total=>checked(food+dailyGoods);
        public int this[ProductKind kind] {get {switch(kind){case ProductKind.Food:return food;case ProductKind.DailyGoods:return dailyGoods;default:throw new ArgumentOutOfRangeException(nameof(kind));}}}
        public GoodsAmounts With(ProductKind kind,int amount)
        {if(amount<0)throw new ArgumentOutOfRangeException(nameof(amount));var n=this;switch(kind){case ProductKind.Food:n.food=amount;break;case ProductKind.DailyGoods:n.dailyGoods=amount;break;default:throw new ArgumentOutOfRangeException(nameof(kind));} _=n.Total;return n;}
        public GoodsAmounts Add(ProductKind kind,int amount)=>With(kind,checked(this[kind]+amount));
        public static GoodsAmounts Of(ProductKind kind,int amount)=>default(GoodsAmounts).With(kind,amount);
        public static GoodsAmounts operator +(GoodsAmounts a,GoodsAmounts b)=>new GoodsAmounts{food=checked(a.food+b.food),dailyGoods=checked(a.dailyGoods+b.dailyGoods)};
        public static GoodsAmounts operator -(GoodsAmounts a,GoodsAmounts b)=>a.Add(ProductKind.Food,-b.food).Add(ProductKind.DailyGoods,-b.dailyGoods);
        public bool Equals(GoodsAmounts n)=>food==n.food&&dailyGoods==n.dailyGoods;
        public override bool Equals(object o)=>o is GoodsAmounts n&&Equals(n);
        public override int GetHashCode()=>food*397^dailyGoods;
        public void Validate(){if(food<0||dailyGoods<0)throw new InvalidOperationException("Negative product inventory");_=Total;}
    }
    [Serializable] public sealed class ProductDefinition
    {
        public ProductKind kind;
        public int retailPrice,wholesalePrice;
        public double rawPrice=8,processingCost=2;
        public int labourMinutes,consumptionDays,refillBelowDays,targetDays;
    }
    public sealed class ProductRules
    {
        public ProductKind Kind {get;}
        public int RetailPrice {get;} public int WholesalePrice {get;}
        public double RawPrice {get;} public double ProcessingCost {get;}
        public int LabourMinutes {get;} public int ConsumptionDays {get;} public int RefillBelowDays {get;} public int TargetDays {get;}
        internal ProductRules(ProductDefinition d)
        {
            if(d==null||d.kind<ProductKind.Food||d.kind>ProductKind.DailyGoods||d.retailPrice<1||d.wholesalePrice<1||d.labourMinutes<1||d.consumptionDays<1||d.refillBelowDays<1||d.targetDays<d.refillBelowDays||double.IsNaN(d.rawPrice)||double.IsInfinity(d.rawPrice)||d.rawPrice<0||double.IsNaN(d.processingCost)||double.IsInfinity(d.processingCost)||d.processingCost<0)throw new ArgumentException("Invalid product rules");
            Kind=d.kind;RetailPrice=d.retailPrice;WholesalePrice=d.wholesalePrice;RawPrice=d.rawPrice;ProcessingCost=d.processingCost;LabourMinutes=d.labourMinutes;ConsumptionDays=d.consumptionDays;RefillBelowDays=d.refillBelowDays;TargetDays=d.targetDays;
        }
        internal ProductDefinition Snapshot()=>new ProductDefinition{kind=Kind,retailPrice=RetailPrice,wholesalePrice=WholesalePrice,rawPrice=RawPrice,processingCost=ProcessingCost,labourMinutes=LabourMinutes,consumptionDays=ConsumptionDays,refillBelowDays=RefillBelowDays,targetDays=TargetDays};
    }
    public sealed class ProductCatalog
    {
        public const int Count=2;
        readonly ProductRules[] rules;
        public ProductRules this[ProductKind kind] {get {if((uint)kind>=Count)throw new ArgumentOutOfRangeException(nameof(kind));return rules[(int)kind];}}
        public ProductCatalog(ProductDefinition[] data)
        {if(data==null||data.Length!=Count)throw new ArgumentException("Expected two products");rules=new ProductRules[Count];foreach(var d in data){var r=new ProductRules(d);if(rules[(int)r.Kind]!=null)throw new ArgumentException("Duplicate product");rules[(int)r.Kind]=r;}for(int i=0;i<Count;i++)if(rules[i]==null)throw new ArgumentException("Missing product");}
        internal ProductDefinition[] Snapshot(){var a=new ProductDefinition[Count];for(int i=0;i<Count;i++)a[i]=rules[i].Snapshot();return a;}
        public static ProductDefinition[] Defaults()=>new[]{new ProductDefinition{kind=ProductKind.Food,retailPrice=20,wholesalePrice=15,labourMinutes=15,consumptionDays=1,refillBelowDays=1,targetDays=3},new ProductDefinition{kind=ProductKind.DailyGoods,retailPrice=60,wholesalePrice=45,labourMinutes=60,consumptionDays=7,refillBelowDays=2,targetDays=14}};
    }
}
