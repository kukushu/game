using System.Collections.Generic;
using HarborCity.RoadCore;

namespace HarborCity
{
    // Explicit synthetic scale fixtures, not a replacement for measuring an integrated city.
    public static class CityRoadCoreFixtures
    {
        public static World Create(int count,bool queued)
        {
            var world=new World();int groups=queued?3:20;
            double approach=queued?System.Math.Max(3000,(count/groups+1)*1.6+100):System.Math.Max(2000,(count/groups+1)*5+100);
            var starts=new List<int>();var destinations=new List<int>();var routes=new List<List<long>>();
            int merge=queued?world.AddJunction(new Point(0,0,0)):0;
            int end=queued?world.AddJunction(new Point(5,0,0)):0;
            int exit=queued?world.AddRoad(merge,end,new[]{world.JunctionPosition(merge),world.JunctionPosition(end)},true):0;
            int receiver=queued?world.AddDepot(end,0):0;
            for(int g=0;g<groups;g++)
            {
                Point p=queued?(g==0?new Point(-approach,0,0):g==1?new Point(0,0,-approach):new Point(0,0,approach)):new Point(0,0,g*8);
                int a=world.AddJunction(p),b=queued?merge:world.AddJunction(new Point(approach,0,g*8));
                int road=world.AddRoad(a,b,new[]{p,Point.Lerp(p,world.JunctionPosition(b),.5),world.JunctionPosition(b)},true);
                int c=queued?end:world.AddJunction(new Point(approach*2,0,g*8));
                int next=queued?exit:world.AddRoad(b,c,new[]{world.JunctionPosition(b),world.JunctionPosition(c)},true);
                starts.Add(world.AddDepot(a,0));destinations.Add(queued?receiver:world.AddDepot(c,0));
                routes.Add(new List<long>{Network.LaneId(road,false),Network.ConnectionId(Network.LaneId(road,false),Network.LaneId(next,false)),Network.LaneId(next,false)});
            }
            var state=world.Snapshot();state.nextVehicle=count+1;state.nextTask=count+1;if(queued)state.depots.Find(d=>d.id==receiver).capacity=0;
            for(int i=0;i<count;i++)
            {
                int g=i%groups,index=i/groups;var route=routes[g];double length=world.Network.Get(route[0]).Geometry.Length;
                var vehicle=new Vehicle{id=i+1,Task=new TripTask{id=i+1,origin=starts[g],destination=destinations[g],Shipment=new Shipment{remaining=1,unitPrice=5}},Plan=new RoutePlan{segments=new List<long>(route)},Position=new VehiclePosition(route[0],queued?length-3.5-index*1.6:20+index*5)};
                if(queued&&i==0){vehicle.CommitPosition(route[2],world.Network.Get(route[2]).Geometry.Length,2);}state.tasks.Add(vehicle.Task);state.vehicles.Add(vehicle);
            }
            var result=new World(state);result.AssertSafety();return result;
        }
    }
}
