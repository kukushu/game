using System;using System.Linq;using HarborCity;using HarborCity.RoadCore;
// Known baseline failure. It intentionally exits nonzero rather than treating
// an invalid parallel-lane correspondence as successful presentation coverage.
static class CityLaneChangeSplitRepro
{
 static void Main(){var s=CityCoreIndustrySession.Create();int branch=s.Transport.AddJunction(new Point(0,0,25));s.Transport.AddRoad(2,branch,new[]{s.Transport.JunctionPosition(2),s.Transport.JunctionPosition(branch)});for(int i=0;i<100&&!s.Transport.Vehicles.Any(v=>v.Task.purpose==TransportPurpose.Freight);i++)s.Advance(.05);var v=s.Transport.Vehicles.First(v=>v.Task.purpose==TransportPurpose.Freight);if(!s.Transport.TryChangeLane(v.id,1))throw new Exception("Fixture failed to begin maneuver");s.Advance(.15);s.Transport.SplitRoadAt(1,12.75);s.Advance(.05);Console.WriteLine("No display consumer; source samples="+s.Transport.Network.Get(v.Current).Geometry.Count+" target samples="+s.Transport.Network.Get(v.changeTarget).Geometry.Count);var p=s.Transport.Position(v);Console.WriteLine("Position available: "+p.x+"/"+p.z);}
}
