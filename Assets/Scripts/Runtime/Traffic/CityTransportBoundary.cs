namespace HarborCity.RoadCore
{
    public readonly struct TransportArrival
    {
        public readonly int id,origin,destination,citizenId,cargo;
        public readonly TransportPurpose purpose;
        public readonly double price,departedAt,arrivedAt;
        internal TransportArrival(Vehicle v,double end)
        {id=v.id;origin=v.origin;destination=v.destination;citizenId=v.citizenId;cargo=v.cargo;purpose=v.purpose;price=v.price;departedAt=v.departedAt;arrivedAt=end;}
    }
    internal sealed class TransportAdmission
    {
        internal readonly World owner;internal readonly long epoch;internal readonly Vehicle vehicle;
        internal TransportAdmission(World owner,long epoch,Vehicle vehicle){this.owner=owner;this.epoch=epoch;this.vehicle=vehicle;}
    }
}
