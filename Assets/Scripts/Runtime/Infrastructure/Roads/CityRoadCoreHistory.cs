using System;
using System.Collections.Generic;

namespace HarborCity.RoadCore
{
    internal sealed class RoadEditState
    {public NetworkData network;public List<RoadAccess> accesses;}
    // Tool history contains only road facts. Clock, actors and business never rewind.
    public sealed class RoadEditHistory
    {
        readonly World world;readonly List<RoadEditState> previous=new List<RoadEditState>();
        public int Count=>previous.Count;
        public RoadEditHistory(World world){this.world=world;}
        public void Execute(Action edit)
        {var state=world.CaptureRoadEdit();int revision=world.NetworkGeneration;edit();if(world.NetworkGeneration==revision)return;if(previous.Count==8)previous.RemoveAt(0);previous.Add(state);}
        public bool Undo()
        {if(previous.Count==0)return false;var plan=world.PlanRestoreRoadEdit(previous[previous.Count-1]);if(!plan.TryCommit())return false;previous.RemoveAt(previous.Count-1);return true;}
    }
}
