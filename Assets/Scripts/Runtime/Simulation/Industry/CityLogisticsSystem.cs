using System;
using System.Collections.Generic;
using HarborCity.RoadCore;
namespace HarborCity
{
    public sealed class CityLogisticsSystem
    {
        readonly CityEntities entities;readonly World transport;readonly CitySimulation simulation;
        readonly Dictionary<int,int> incoming=new Dictionary<int,int>();
        readonly CityFreightCandidates candidates;
        readonly List<CommercialBuilding> shops=new List<CommercialBuilding>();
        public long NextDispatchTick {get;private set;}
        internal CityLogisticsSystem(CityEntities entities,World transport,CitySimulation simulation,long nextTick)
        {this.entities=entities;this.transport=transport;this.simulation=simulation;NextDispatchTick=nextTick;candidates=new CityFreightCandidates(entities,transport);}
        internal void Step(TimeContext time)
        {
#if CITY_CORE_ATTRIBUTION
            using var observation=HarborCity.RoadCore.CityCoreProfile.Measure(HarborCity.RoadCore.CityCoreProfile.Part.Logistics);
#endif

            if(time.Tick<NextDispatchTick)return;NextDispatchTick=time.Tick+5;incoming.Clear();
            foreach(var task in transport.Vehicles){incoming.TryGetValue(task.destination,out int count);incoming[task.destination]=count+task.cargo;}
            // Local source backpressure: no destination can bypass the same blocked
            // station. Preserve shop order; unreachable destinations still try later shops.
            // Query directed lane/station ranges; preserve canonical shop ordering.
            foreach(var factory in entities.Factories)
            {
                if(factory.Stock<=0||transport.FreightSourceBlocked(factory.id))continue;
                candidates.Collect(transport.GetAccess(factory.id),shops);
                foreach(var shop in shops)
                {incoming.TryGetValue(shop.id,out int reserved);int amount=Math.Min(8,Math.Min(factory.Stock,32-shop.Stock-reserved));if(amount<=0)continue;var task=simulation.RequestFreight(factory.id,shop.id,amount,FactoryState.SalePrice);if(task!=null){incoming[shop.id]=reserved+amount;break;}}
            }
        }
    }
}
