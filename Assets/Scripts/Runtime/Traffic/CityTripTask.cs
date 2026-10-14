using System;
using System.Collections.Generic;
namespace HarborCity.RoadCore
{
    // Task store owns these facts. Vehicle holds a resolved reference to its task,
    // not copies of cargo, citizen, endpoints or accounting values.
    [Serializable] public sealed class Shipment
    {
        public CargoKind kind {get;internal set;}
        public int remaining {get;internal set;}
        public double unitPrice {get;internal set;}
        internal Shipment Copy()=>new Shipment{kind=kind,remaining=remaining,unitPrice=unitPrice};
    }
    public enum TripState { InTransit,Completed }
    [Serializable] public sealed class TripTask
    {
        public int id {get;internal set;}
        public int origin {get;internal set;}
        public int destination {get;internal set;}
        public TransportPurpose purpose {get;internal set;}
        public int citizenId {get;internal set;}
        public double departedAt {get;internal set;}
        public TripState state {get;internal set;}
        public Shipment Shipment {get;internal set;}
        public int Cargo=>Shipment?.remaining??0;
        public double Price=>Shipment?.unitPrice??0;
        internal TripTask Copy()=>new TripTask{id=id,origin=origin,destination=destination,purpose=purpose,citizenId=citizenId,departedAt=departedAt,state=state,Shipment=Shipment?.Copy()};
    }
    // Navigation owns the path and terminal station. Geometry belongs to Network.
    // Route progress is still on Vehicle in this first bounded R2 batch.
    public sealed class RoutePlan
    {
        internal List<long> segments=new List<long>();
        public double EndDistance {get;internal set;}=-1;
        IReadOnlyList<long> view;List<long> owner;
        public IReadOnlyList<long> Segments {get{if(!ReferenceEquals(owner,segments)){owner=segments;view=segments.AsReadOnly();}return view;}}
        internal RoutePlan Copy()=>new RoutePlan{segments=new List<long>(segments),EndDistance=EndDistance};
    }
}
