using System;
using HarborCity.RoadCore;
namespace HarborCity
{
    public sealed class CityCoreConstruction
    {
        readonly CityModel city;readonly CityEntities entities;readonly World world;
        internal CityCoreConstruction(CityModel city,CityEntities entities,World world){this.city=city;this.entities=entities;this.world=world;}
        static double RoadValue(Network network){double sum=0;foreach(int id in network.RoadIds)sum+=network.Centreline(id).Length*network.Settings(id).lanes*(40.0/3);return sum;}
        public int Quote(RoadEditPlan plan)
        {if(plan==null||plan.owner!=world||!ReferenceEquals(plan.source,world.Network))throw new InvalidOperationException("道路方案已过期，请重新绘制");double amount=Math.Ceiling(Math.Max(0,RoadValue(plan.Preview)-RoadValue(world.Network)-1e-6));if(amount>int.MaxValue||double.IsNaN(amount)||double.IsInfinity(amount))throw new InvalidOperationException("道路费用超出支持范围");return (int)amount;}
        public bool Commit(RoadEditPlan plan,out int paid)
        {entities.StructuralBoundary();paid=Quote(plan);if(city.money<paid)throw new InvalidOperationException("城市资金不足");if(!plan.TryCommit())return false;city.money-=paid;return true;}
        public void RemoveRoad(int id)
        {entities.StructuralBoundary();if(city.money<40)throw new InvalidOperationException("城市资金不足");world.RemoveRoad(id);city.money-=40;}
        public int PlaceFacility(LandUse use,float x,float z,float yaw,CityCoreZoning zoning)
        {
            entities.StructuralBoundary();if(use!=LandUse.Power&&use!=LandUse.Water&&use!=LandUse.Sewage)throw new ArgumentException("当前批次只恢复电站、供水与排污设施");
            var lot=CityModel.NewBuilding(use);lot.x=x;lot.z=z;lot.yaw=yaw;int cost=CityModel.Cost(use);if(city.money<cost)throw new InvalidOperationException("设施建设资金不足");
            if(!zoning.CanPlace(lot))throw new InvalidOperationException("设施占地、水面或坡度不允许建设");var access=zoning.Entrance(lot);if(!access.HasValue)throw new InvalidOperationException("设施需要地面道路入口");
            lot.id=city.nextBuildingId;world.CheckNewAccess(lot.id,access.Value.drivable,access.Value.distance);entities.AddBuilding(lot);world.RegisterAccess(lot.id,access.Value.drivable,access.Value.distance);city.money-=cost;return lot.id;
        }
    }
}
