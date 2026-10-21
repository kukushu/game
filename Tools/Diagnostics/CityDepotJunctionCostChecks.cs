using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HarborCity;
using HarborCity.RoadCore;
static class CityDepotJunctionCostChecks
{
    static int checks;static double maxError;
    static double Cost(Network n,List<long> path,double start,double end)=>new DispatchBlueprint(n,path,start,end).seconds;
    static void Equal(double expected,double actual,int terms,string label)
    {
        checks++;if(double.IsPositiveInfinity(expected)&&double.IsPositiveInfinity(actual))return;
        double error=Math.Abs(expected-actual),bound=8*(terms+4)*2.2204460492503131e-16*Math.Max(1,Math.Abs(expected));
        if(double.IsNaN(error)||double.IsInfinity(error)||error>bound)throw new Exception(label+" expected="+expected+" actual="+actual+" error="+error+" bound="+bound);
        maxError=Math.Max(maxError,error);
    }
    static void Verify(Network n,RoadAccess anchor,DepotJunctionRouting cache,int samples,List<string> rows)
    {
        cache.StateCost(n,anchor.endpoint,anchor,0,false);int size=cache.Nodes;
        for(int i=0;i<Math.Min(samples,size);i++)
        {
            int state=i*size/Math.Min(samples,size);var members=cache.Members(state).ToArray();
            foreach(bool back in new[]{false,true})
            {
                double expected=double.PositiveInfinity;int terms=0;
                if(back)
                {
                    bool set=false;foreach(long lane in members){var r=AccessRouting.FromLane(n,lane,n.Get(lane).EndDistance,anchor,true,out double e);double cost=Cost(n,r,n.Get(lane).EndDistance,e);if(!set){expected=cost;set=true;}else Equal(expected,cost,r?.Count??1,"Merged reverse member mismatch");terms=Math.Max(terms,r?.Count??1);}
                }
                else foreach(long lane in members)foreach(var source in new[]{anchor,AccessRouting.AtDirection(n,anchor,!n.Get(anchor.lane).Reverse)})
                {
                    if(source==null)continue;var goal=new RoadAccess{lane=lane,distance=n.Get(lane).EndDistance};var r=n.Route(source,goal);double cost=Cost(n,r,source.distance,goal.distance);if(cost<expected)expected=cost;terms=Math.Max(terms,r?.Count??1);
                }
                double actual=cache.StateCost(n,anchor.endpoint,anchor,state,back);Equal(expected,actual,terms,"State shortest time");
                var path=cache.StatePath(n,anchor.endpoint,anchor,state,back,out double start,out double end);
                if(path!=null){n.ValidateRoute(path);Equal(actual,Cost(n,path,start,end),path.Count,"Restored state cost");}
                else if(!double.IsPositiveInfinity(actual))throw new Exception("Finite label missing path");
                rows.Add(string.Join(",",anchor.endpoint,state,back,members.Length,expected,actual,Math.Abs(expected-actual),path?.Count??0));
            }
        }
    }
    static void Main(string[] args)
    {
        var rows=new List<string>{"depot,state,returning,members,reference_seconds,tree_seconds,absolute_error,segments"};
        var w=new World();var ids=new int[4,3];for(int z=0;z<3;z++)for(int x=0;x<4;x++)ids[x,z]=w.AddJunction(new Point(x*37,2,z*31));
        for(int z=0;z<3;z++)for(int x=0;x<4;x++)
        {
            if(x<3)w.AddRoad(ids[x,z],ids[x+1,z],new[]{w.JunctionPosition(ids[x,z]),w.JunctionPosition(ids[x+1,z])},speed:5+x*7+z,lanes:2);
            if(z<2)w.AddRoad(ids[x,z],ids[x,z+1],new[]{w.JunctionPosition(ids[x,z]),w.JunctionPosition(ids[x,z+1])},oneWay:x%2==1,speed:3+x+z*12,lanes:2);
        }
        var lanes=w.Network.Drivables.Where(x=>!x.IsConnection).ToArray();var cache=new DepotJunctionRouting();int id=1;
        foreach(var lane in lanes.Where((x,i)=>i%7==0))foreach(double fraction in new[]{0.0,.37,1.0})
        {var anchor=new RoadAccess{endpoint=id++,lane=lane.Id,distance=lane.StartDistance+fraction*lane.TravelLength};Verify(w.Network,anchor,cache,int.MaxValue,rows);}
        if(cache.Nodes<=cache.TopologyOnlyNodes)throw new Exception("Fixture must expose unequal-cost topological classes");
        if(args.Length>0)
        {
            var city=CityCoreSave.Read(File.ReadAllBytes(args[0]));var real=city.Transport.Network;var c=new DepotJunctionRouting();
            foreach(var plant in city.City.buildings.OfType<LandfillBuilding>().Where((x,i)=>i%23==0).Take(5))Verify(real,city.Transport.GetAccess(plant.id),c,24,rows);
            Console.WriteLine("REAL topology-only="+c.TopologyOnlyNodes+" weighted="+c.Nodes+" lanes="+c.Lanes);
        }
        if(args.Length>1)File.WriteAllLines(args[1],rows);
        Console.WriteLine("PASS "+checks+" independent weighted-state costs/restored paths; mixed speeds, restricted turns, both source curbs, exact boundary terminals; max error="+maxError);
    }
}
