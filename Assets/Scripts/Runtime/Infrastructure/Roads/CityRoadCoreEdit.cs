using System.Collections.Generic;

namespace HarborCity.RoadCore
{
    // Prepared transaction owns candidate geometry and migration data, never live facts.
    // Simulation/dispatch/access changes invalidate the plan; a preview is not a commit.
    public sealed class RoadEditPlan
    {
        internal readonly World owner;
        internal readonly Network source,network;
        internal readonly long epoch;
        internal readonly NetworkData data;
        internal readonly List<World.Remap> remaps;
        internal readonly List<KeyValuePair<Vehicle,Point>> interruptions;
        internal readonly List<RoadAccess> accesses;
        bool consumed;
        public Network Preview=>network;
        internal RoadEditPlan(World owner,NetworkData data,Network network,List<World.Remap> remaps=null,List<KeyValuePair<Vehicle,Point>> interruptions=null,List<RoadAccess> accesses=null)
        {this.owner=owner;source=owner.Network;epoch=owner.EditEpoch;this.data=data;this.network=network;this.remaps=remaps??new List<World.Remap>();this.interruptions=interruptions??new List<KeyValuePair<Vehicle,Point>>();this.accesses=accesses;}
        public bool TryCommit(){if(consumed)return false;if(!owner.TryCommit(this))return false;consumed=true;return true;}
    }
}
