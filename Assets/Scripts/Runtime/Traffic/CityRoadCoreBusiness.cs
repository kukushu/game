using System;
using System.Collections.Generic;

namespace HarborCity.RoadCore
{
    internal interface IJunctionEndpointProvider {int Junction(int endpoint);}
    // Synthetic ledger used by isolated traffic fixtures, never by the integrated city.
    internal sealed class DepotBusiness:IJunctionEndpointProvider
    {
        readonly Dictionary<int,Depot> depots;
        public DepotBusiness(Dictionary<int,Depot> depots){this.depots=depots;}
        public bool Exists(int id)=>depots.ContainsKey(id);
        public int Junction(int id)=>depots[id].junction;
        public bool Depart(TripTask v){if(v.purpose!=TransportPurpose.Freight||v.Shipment?.kind!=CargoKind.Goods)return false;var source=depots[v.origin];if(source.stock<v.Cargo)return false;source.stock-=v.Cargo;return true;}
        public bool Deliver(TransportArrival v)
        {
            var destination=depots[v.destination];double payment=v.price*v.cargo;
            if(destination.capacity-destination.stock<v.cargo||destination.cash<payment)return false;
            destination.stock+=v.cargo;destination.cash-=payment;depots[v.origin].cash+=payment;return true;
        }
    }
}
