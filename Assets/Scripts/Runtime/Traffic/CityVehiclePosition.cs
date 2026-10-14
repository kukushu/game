using System;
namespace HarborCity.RoadCore
{
    // Sole physical anchor. Distance measures the vehicle centre from the start
    // of the oriented shared geometry, including when Drivable is a connection.
    public sealed class VehiclePosition
    {
        public long Drivable {get;private set;}
        public double Distance {get;private set;}
        public bool IsFrozen {get;private set;}
        public Point FrozenPoint {get;private set;}
        public VehiclePosition(long drivable,double distance)
        {if(drivable==0||distance<0||double.IsNaN(distance)||double.IsInfinity(distance))throw new ArgumentException("Invalid physical vehicle position");Drivable=drivable;Distance=distance;IsFrozen=false;FrozenPoint=default;}
        internal void Commit(long drivable,double distance,bool preserveFrozen)
        {
            if(drivable==0||distance<0||double.IsNaN(distance)||double.IsInfinity(distance))throw new ArgumentException("Invalid physical vehicle position");
            Drivable=drivable;Distance=distance;if(IsFrozen&&!preserveFrozen){IsFrozen=false;FrozenPoint=default;}
        }
        internal void Freeze(Point point){IsFrozen=true;FrozenPoint=point;}
        internal VehiclePosition Copy(){var copy=new VehiclePosition(Drivable,Distance);if(IsFrozen)copy.Freeze(FrozenPoint);return copy;}
    }
}
