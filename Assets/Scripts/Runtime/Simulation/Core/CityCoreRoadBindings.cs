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
            foreach(var connection in city.trade.connections)
            {
                var access=world.GetAccess(connection.id);if(access==null||access.valid)continue;
                world.Network.Project(new Point(connection.x,connection.y,connection.z),4,false,hits);
                foreach(var hit in hits)
                {
                    var d=world.Network.Get(hit.drivable);var tangent=d.Geometry.DirectionAt(hit.distance);bool xBorder=Math.Abs(connection.x)>Math.Abs(connection.z);double inward=xBorder?-Math.Sign(connection.x):-Math.Sign(connection.z);double dot=(xBorder?tangent.x:tangent.z)*inward;
                    if(Math.Abs(hit.point.y-connection.y)<.5&&Math.Abs(dot)>.2&&(dot>0)==(connection.flow==OutsideFlow.Import)){bindings.Add(new AccessBinding(connection.id,hit.drivable,hit.distance));break;}
                }
            }
            if(bindings.Count>0)world.RebindAccesses(bindings);
            foreach(var vehicle in world.Vehicles)if(vehicle.status==VehicleStatus.Stranded)world.TryResumeStranded(vehicle.id);revision=world.NetworkGeneration;
        }
    }
}
