using System;
using System.Collections.Generic;
using System.Linq;
using HarborCity;
using HarborCity.RoadCore;
// Independent graph traversal oracle; no reliance on the candidate's SCC/cache internals.
static class CityReachabilityComponentChecks
{
    static long checks;
    static void Require(bool value){checks++;if(!value)throw new Exception("Reachability/cycle differs from directed connection traversal");}
    static NetworkData Grid(int seed,bool acyclic)
    {
        var random=new Random(seed);var data=new NetworkData();const int side=6;
        for(int z=0;z<side;z++)for(int x=0;x<side;x++)data.junctions.Add(new Junction{id=data.nextJunction++,position=new Point(x*48,seed%2*12,z*48)});
        void Add(int a,int b)
        {
            if(!acyclic&&random.Next(4)==0)return;
            data.roads.Add(new PhysicalRoad{id=data.nextRoad++,from=a,to=b,lanes=acyclic?2:1+random.Next(2),direction=acyclic?RoadDirection.Forward:(RoadDirection)random.Next(3),points=new[]{data.junctions[a-1].position,data.junctions[b-1].position}});
        }
        for(int z=0;z<side;z++)for(int x=0;x<side;x++){int a=z*side+x+1;if(x+1<side)Add(a,a+1);if(z+1<side)Add(a,a+side);}
        return data;
    }
    static void Compare(Network network)
    {
        var lanes=network.Drivables.Where(d=>!d.IsConnection).ToArray();var next=new Dictionary<long,List<long>>(CityRoadKeyComparer.Instance);
        foreach(var d in network.Drivables.OfType<LaneConnection>()){if(!next.TryGetValue(d.Incoming.Id,out var exits))next.Add(d.Incoming.Id,exits=new List<long>());exits.Add(d.Outgoing.Id);}
        // Repeat enough distinct sources to exercise the bounded cross-component cache's eviction.
        for(int pass=0;pass<2;pass++)foreach(var source in lanes)
        {
            var reached=new HashSet<long>(CityRoadKeyComparer.Instance);var queue=new Queue<long>();
            if(next.TryGetValue(source.Id,out var first))foreach(long child in first)if(reached.Add(child))queue.Enqueue(child);
            while(queue.Count>0){long at=queue.Dequeue();if(next.TryGetValue(at,out var edges))foreach(long child in edges)if(reached.Add(child))queue.Enqueue(child);}
            Require(network.HasLaneCycle(source.Id)==reached.Contains(source.Id));
            double low=source.StartDistance,high=source.EndDistance;
            foreach(var target in lanes)
            {
                Require(network.CanReach(source.Id,low,target.Id,target.EndDistance)==(source.Id==target.Id||reached.Contains(target.Id)));
                Require(network.CanReach(source.Id,high,target.Id,target.StartDistance)==reached.Contains(target.Id));
            }
        }
    }
    static void Main()
    {
        Compare(new Network(new NetworkData()));Compare(new Network(Grid(0,true)));
        for(int seed=1;seed<=24;seed++)
        {
            var data=Grid(seed,false);Compare(new Network(data));
            data.roads.RemoveAt(data.roads.Count/2);Compare(new Network(data));
            data.roads[0].direction=data.roads[0].direction==RoadDirection.Forward?RoadDirection.Backward:RoadDirection.Forward;Compare(new Network(data));
        }
        Console.WriteLine("PASS "+checks+" directed lane reachability/cycle oracle checks; multiple SCCs, DAG/cache eviction, one/two lanes, disconnected roads, heights and rebuilt networks.");
    }
}
