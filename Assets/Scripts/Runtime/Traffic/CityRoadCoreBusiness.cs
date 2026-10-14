using System;
using System.Collections.Generic;

namespace HarborCity.RoadCore
{
    // Transport owns the cargo in transit. Business owns endpoints, stock and finance.
    // Depart/Deliver must validate first and commit without fallible observer callbacks.
    public interface ITransportBusiness
    {
        bool Exists(int endpoint);
        int Junction(int endpoint);
        bool Depart(Vehicle vehicle);
        bool Deliver(Vehicle vehicle);
    }
    // Synthetic ledger used by isolated traffic fixtures, never by the integrated city.
    internal sealed class DepotBusiness:ITransportBusiness
    {
        readonly Dictionary<int,Depot> depots;
        public DepotBusiness(Dictionary<int,Depot> depots){this.depots=depots;}
        public bool Exists(int id)=>depots.ContainsKey(id);
        public int Junction(int id)=>depots[id].junction;
        public bool Depart(Vehicle v){var source=depots[v.origin];if(source.stock<v.cargo)return false;source.stock-=v.cargo;return true;}
        public bool Deliver(Vehicle v)
        {
            var destination=depots[v.destination];double payment=v.price*v.cargo;
            if(destination.capacity-destination.stock<v.cargo||destination.cash<payment)return false;
            destination.stock+=v.cargo;destination.cash-=payment;depots[v.origin].cash+=payment;return true;
        }
    }
}
