using System;
using HarborCity.RoadCore;
namespace HarborCity
{
    public sealed class CityCoreConstruction
    {
        readonly CityModel city;readonly CityEntities entities;readonly World world;readonly CityCoreFinance finance;readonly WasteRules waste;
        readonly CityCoreUtilities utilities;
        internal CityCoreConstruction(CityModel city,CityEntities entities,World world,CityCoreFinance finance,WasteRules waste,CityCoreUtilities utilities){this.city=city;this.entities=entities;this.world=world;this.finance=finance;this.waste=waste;this.utilities=utilities;}
        static double RoadValue(Network network){double sum=0;foreach(int id in network.RoadIds)sum+=network.Centreline(id).Length*network.Settings(id).lanes*(40.0/3);return sum;}
        public int Quote(RoadEditPlan plan)
        {if(plan==null||plan.owner!=world||!ReferenceEquals(plan.source,world.Network))throw new InvalidOperationException("道路方案已过期，请重新绘制");double amount=Math.Ceiling(Math.Max(0,RoadValue(plan.Preview)-RoadValue(world.Network)-1e-6));if(amount>int.MaxValue||double.IsNaN(amount)||double.IsInfinity(amount))throw new InvalidOperationException("道路费用超出支持范围");return (int)amount;}
        public bool Commit(RoadEditPlan plan,out int paid)
        {entities.StructuralBoundary();paid=Quote(plan);finance.CheckSpend(paid);if(!plan.TryCommit())return false;finance.SpendApproved(paid);if(city.utilities.roadConduits){if(plan.independentRoad!=0)city.RetainIndependentRoadSupplyLayout(plan.source,plan.Preview,plan.independentRoad);utilities.Refresh();}return true;}
        public void RemoveRoad(int id)
        {entities.StructuralBoundary();finance.CheckSpend(40);world.RemoveRoad(id);finance.SpendApproved(40);if(city.utilities.roadConduits)utilities.Refresh();}
        // UI sends a stable road ID, never holds a prepared plan across frames.
        // Build and validate the candidate at this healthy boundary, then pay once.
        public int UpgradeRoad(int id,int lanes,RoadDirection direction)
        {
            entities.StructuralBoundary();if(!world.Network.ContainsRoad(id))throw new CityRuleException("road.edit_missing");
            var settings=world.Network.Settings(id);if(settings.lanes==lanes&&settings.direction==direction)return 0;var plan=world.PlanUpgradeRoad(id,lanes,settings.speed,direction);
            if(lanes>settings.lanes)foreach(var b in entities.Buildings)if(CityRoadClearance.Intersects(plan.Preview,b,selectionHits))throw new CityRuleException("road.widen_obstructed",b.id);
            if(!Commit(plan,out int paid))throw new CityRuleException("road.edit_stale");return paid;
        }
        public int PickRoad(Point point,double radius=2.5)
        {
            world.Network.Project(point,radius,false,selectionHits);
            foreach(var hit in selectionHits){var lane=world.Network.Get(hit.drivable);if(!lane.IsConnection)return lane.RoadId;}return 0;
        }
        readonly System.Collections.Generic.List<SpatialHit> selectionHits=new System.Collections.Generic.List<SpatialHit>();
        // Cold, read-only placement preflight shared by the cursor and the final
        // click. A preview never owns or commits live road/cash state.
        public int CheckRoadPlacement(RoadEditPlan plan)
        {
            int price=Quote(plan);finance.CheckSpend(price);
            foreach(var b in entities.Buildings)if(CityRoadClearance.Intersects(plan.Preview,b,selectionHits))throw new CityRuleException("road.build_obstructed",b.id);
            foreach(var project in city.development.projects){var b=project.building.ToBuilding();if(CityRoadClearance.Intersects(plan.Preview,b,selectionHits))throw new CityRuleException("road.build_obstructed",b.id);}
            return price;
        }
        public int PlaceFacility(LandUse use,float x,float z,float yaw,CityCoreZoning zoning)
        {
            entities.StructuralBoundary();if(use!=LandUse.Power&&use!=LandUse.Water&&use!=LandUse.Sewage&&use!=LandUse.Landfill)throw new ArgumentException("Unsupported facility");
            var lot=CityModel.NewBuilding(use);lot.x=x;lot.z=z;lot.yaw=yaw;int cost=use==LandUse.Landfill?waste.ConstructionPrice:CityModel.Cost(use);finance.CheckSpend(cost);
            if(!zoning.CanPlace(lot))throw new InvalidOperationException("设施占地、水面或坡度不允许建设");var access=zoning.Entrance(lot);if(!access.HasValue)throw new InvalidOperationException("设施需要地面道路入口");
            lot.id=city.nextBuildingId;world.CheckNewAccess(lot.id,access.Value.drivable,access.Value.distance);entities.AddBuilding(lot);world.RegisterAccess(lot.id,access.Value.drivable,access.Value.distance);finance.SpendApproved(cost);return lot.id;
        }
    }
}
