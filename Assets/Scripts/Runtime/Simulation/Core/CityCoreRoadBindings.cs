using System;
using System.Collections.Generic;
using HarborCity.RoadCore;

namespace HarborCity
{
    // Bridge between original building doors and the road world's unique associations.
    // Geometry lookup, binding recovery and vehicle recovery are cold edit aftermath.
    public sealed class CityCoreRoadBindings
    {
        readonly CityModel city;readonly World world;
        int revision=-1;
        readonly List<SpatialHit> hits=new List<SpatialHit>();
        readonly List<AccessBinding> bindings=new List<AccessBinding>();
        public CityCoreRoadBindings(CityModel city,World world){this.city=city;this.world=world;}
        public void RefreshIfChanged()
        {
            if(revision==world.NetworkGeneration)return;bindings.Clear();
            foreach(var building in city.buildings)
            {
                var access=world.GetAccess(building.id);if(access==null||access.valid)continue;world.Network.Project(new Point(building.entranceX,building.entranceY,building.entranceZ),4,false,hits);
                SpatialHit? chosen=null;foreach(var hit in hits)if(Math.Abs(hit.point.y-building.entranceY)<.5){if(chosen==null)chosen=hit;if(world.Network.Get(hit.drivable).LaneIndex==access.preferredIndex){chosen=hit;break;}}
                if(chosen.HasValue)bindings.Add(new AccessBinding(building.id,chosen.Value.drivable,chosen.Value.distance));
            }
            if(bindings.Count>0)world.RebindAccesses(bindings);
            foreach(var vehicle in world.Vehicles)if(vehicle.status==VehicleStatus.Stranded)world.TryResumeStranded(vehicle.id);revision=world.NetworkGeneration;
        }
    }
}
