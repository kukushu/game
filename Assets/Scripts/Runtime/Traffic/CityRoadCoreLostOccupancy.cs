using System.Collections.Generic;

namespace HarborCity.RoadCore
{
    internal sealed class LostOccupancy
    {
        internal readonly struct Placement
        {
            public readonly long lane;public readonly LaneOccupant occupant;
            public Placement(long lane,LaneOccupant occupant){this.lane=lane;this.occupant=occupant;}
        }
        Network network;
        readonly Dictionary<int,List<Placement>> placements=new Dictionary<int,List<Placement>>();
        readonly List<SpatialHit> scratch=new List<SpatialHit>();
        public void Reset(Network current){network=current;placements.Clear();scratch.Clear();}
        public List<Placement> Get(Network current,Vehicle v)
        {
            if(!ReferenceEquals(current,network))Reset(current);List<Placement> result;if(placements.TryGetValue(v.id,out result))return result;
            current.Project(v.strandedPosition,.9,true,scratch);result=new List<Placement>();foreach(var hit in scratch)result.Add(new Placement(hit.drivable,new LaneOccupant(v,hit.distance,true)));placements.Add(v.id,result);return result;
        }
        public void Forget(int id)=>placements.Remove(id);
    }
}
