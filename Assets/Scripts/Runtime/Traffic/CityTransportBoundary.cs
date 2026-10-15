namespace HarborCity.RoadCore
{
    internal enum TransportRejection { None,EndpointUnavailable,AccessUnavailable,SourceBlocked,Unreachable,NoRoute,SourceOccupied,JunctionReserved }
    public readonly struct TransportArrival
    {
        public readonly int id,taskId,origin,destination,citizenId,cargo;
        public readonly TransportPurpose purpose;
        public readonly CargoKind kind;
        public readonly double price,departedAt,arrivedAt;
        internal TransportArrival(Vehicle v,double end)
        {var task=v.Task;id=v.id;taskId=task.id;origin=task.origin;destination=task.destination;citizenId=task.citizenId;cargo=task.Cargo;purpose=task.purpose;kind=task.Shipment?.kind??CargoKind.Goods;price=task.Price;departedAt=task.departedAt;arrivedAt=end;}
    }
    internal sealed class ServiceContinuation
    {
        internal readonly World owner;internal readonly long epoch;internal readonly Vehicle vehicle;internal readonly int previousTask;
        internal readonly TripTask task;internal readonly RoutePlan plan;
        internal ServiceContinuation(World owner,long epoch,Vehicle vehicle,int previousTask,TripTask task,RoutePlan plan)
        {this.owner=owner;this.epoch=epoch;this.vehicle=vehicle;this.previousTask=previousTask;this.task=task;this.plan=plan;}
    }
    internal sealed class TransportAdmission
    {
        internal readonly World owner;internal readonly long epoch;internal readonly Vehicle vehicle;
        internal TransportAdmission(World owner,long epoch,Vehicle vehicle){this.owner=owner;this.epoch=epoch;this.vehicle=vehicle;}
    }
}
