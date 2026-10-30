using System;
namespace HarborCity
{
    [Serializable] public sealed class ShopAccountData
    {public double cash,capital,retailRevenue,wageCosts,wholesaleFunded,unpaidWages,taxCosts;}
    // Shop inventory stays on the building. This object owns only its account;
    // wholesaleFunded - wholesaleCosts is paid cargo still in transport.
    public sealed class CityShopAccount
    {
        double balance;
        internal CityEntities FundingOwner;internal int FundingBuilding=-1;
        public double Cash {get=>balance;internal set{bool funded=balance>0;balance=value;if(funded!=(value>0))FundingOwner?.MarkFundingDirty(FundingBuilding,this);}}
        public double Capital {get;internal set;}
        public double RetailRevenue {get;internal set;}
        public double WageCosts {get;internal set;}
        public double WholesaleFunded {get;internal set;}
        public double UnpaidWages {get;internal set;}
        public double TaxCosts {get;internal set;}
        public double CashError=>Cash-(Capital+RetailRevenue-WageCosts-WholesaleFunded-TaxCosts);
        public CityShopAccount(){}
        public CityShopAccount(ShopAccountData d)
        {if(d==null)throw new ArgumentNullException(nameof(d));Cash=d.cash;Capital=d.capital;RetailRevenue=d.retailRevenue;WageCosts=d.wageCosts;WholesaleFunded=d.wholesaleFunded;UnpaidWages=d.unpaidWages;TaxCosts=d.taxCosts;}
        public ShopAccountData Snapshot()=>new ShopAccountData{cash=Cash,capital=Capital,retailRevenue=RetailRevenue,wageCosts=WageCosts,wholesaleFunded=WholesaleFunded,unpaidWages=UnpaidWages,taxCosts=TaxCosts};
        public void Validate()
        {foreach(double n in new[]{Cash,Capital,RetailRevenue,WageCosts,WholesaleFunded,UnpaidWages,TaxCosts})if(double.IsNaN(n)||double.IsInfinity(n)||n<0)throw new InvalidOperationException("Invalid shop balance");if(Math.Abs(CashError)>1e-6)throw new InvalidOperationException("Shop account does not balance");}
    }
}
