using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace HarborCity
{
    public static class CityTrafficChecks
    {
        [MenuItem("Harbor/Validate traffic save")]
        public static void Validate()
        {
            var c=CityBuildingChecks.Fixture();var f=c.buildings.OfType<IndustrialBuilding>().Single();var shop=c.buildings.OfType<CommercialBuilding>().Single();
            c.society.settings.applicantsPerDay=0;f.goodsStock=8;
            var sim=new CityTraffic(c);var trip=sim.Dispatch(f.id,shop.id,TripPurpose.Delivery);CityBuildingChecks.Check(trip!=null,"Real freight dispatch");
            sim.Advance(.1f);var copy=CityBuildingChecks.Copy(c);var resumed=new CityTraffic(copy);
            sim.Advance(30);resumed.Advance(30);
            CityBuildingChecks.Check(c.Valid() && copy.Valid() && CityBuildingChecks.Equivalent(c.ToSaveData(),copy.ToSaveData()),"Freight native JSON continuation");
            CityBuildingChecks.Check(c.Factory(f.id).salesRevenue==8*FactoryState.SalePrice && c.Goods(shop.id)>=8,"Actual delivery credits seller and buyer inventory");
            CityBuildingChecks.Check(c.DemolishBuilding(f.id) && c.GetBuilding(f.id)==null && c.Valid() && CityBuildingChecks.Copy(c).Valid(),"Factory demolition and payroll closure");
            CityBuildingChecks.Result("CityTrafficChecks","PASS: native freight JSON, deterministic continuation, goods delivery/sale and factory demolition");
        }
    }
}
