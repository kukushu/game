using System;
using System.Collections.Generic;
using HarborCity.RoadCore;
namespace HarborCity
{
    public sealed class CityLogisticsSystem
    {
        readonly CityEntities entities;readonly World transport;readonly CitySimulation simulation;
        readonly Dictionary<int,int> incoming=new Dictionary<int,int>();
        public long NextDispatchTick {get;private set;}
        internal CityLogisticsSystem(CityEntities entities,World transport,CitySimulation simulation,long nextTick)
        {this.entities=entities;this.transport=transport;this.simulation=simulation;NextDispatchTick=nextTick;}
        internal void Step(TimeContext time)
        {
            if(time.Tick<NextDispatchTick)return;NextDispatchTick=time.Tick+5;incoming.Clear();
            foreach(var task in transport.Vehicles){incoming.TryGetValue(task.destination,out int count);incoming[task.destination]=count+task.cargo;}
            // Still O(V+F*S), explicitly measured; typed views remove the old F*B filtering.
            foreach(var factory in entities.Factories)foreach(var shop in entities.Shops)
            {incoming.TryGetValue(shop.id,out int reserved);int amount=Math.Min(8,Math.Min(factory.Stock,32-shop.Stock-reserved));if(amount<=0)continue;var task=simulation.RequestFreight(factory.id,shop.id,amount,FactoryState.SalePrice);if(task!=null)incoming[shop.id]=reserved+amount;}
        }
    }
}
