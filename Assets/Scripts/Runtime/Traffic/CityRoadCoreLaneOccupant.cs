using System.Collections.Generic;

namespace HarborCity.RoadCore
{
    // Distance is a derived index entry, never saved or independently advanced.
    // A lane-changing actor contributes its center to both source and target lanes.
    internal sealed class LaneOccupant
    {
        public readonly Vehicle vehicle;
        public double distance;
        public readonly bool shadow;
        public LaneOccupant(Vehicle vehicle,double distance,bool shadow=false){this.vehicle=vehicle;this.distance=distance;this.shadow=shadow;}
    }
    internal sealed class LaneOccupantOrder:IComparer<LaneOccupant>
    {
        public static readonly LaneOccupantOrder Instance=new LaneOccupantOrder();
        public int Compare(LaneOccupant a,LaneOccupant b){int c=b.distance.CompareTo(a.distance);return c!=0?c:a.vehicle.id.CompareTo(b.vehicle.id);}
    }
}
