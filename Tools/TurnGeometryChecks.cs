using System;
using System.Collections.Generic;
using System.Linq;
using HarborCity;
public static class TurnGeometryChecks
{
    static int checks;
    static void Check(bool ok,string reason){if(!ok)throw new Exception("Turn geometry: "+reason);checks++;}
    public static void Run()
    {
        foreach(var kind in new[]{RoadKind.Standard,RoadKind.FourLane})
        foreach(int sign in new[]{-1,1})
        {
            var c=TestCity.Empty();
            Check(c.CommitRoad(c.roads.Plan(c,c.roads.Node(CityRoads.Entrance),TestCity.P(45,1.5f),TestCity.Flat,false,kind)),"Build approach");
            Check(c.CommitRoad(c.roads.Plan(c,TestCity.P(0,-30),TestCity.P(0,30),TestCity.Flat,false,kind)),"Build junction");
            int end=c.roads.nodes.OrderBy(n=>CityRoads.Length(n,TestCity.P(0,sign*30))).First().id;
            var route=c.roads.FindPath(new List<int>{CityRoads.Entrance},new List<int>{end});
            int lane=kind==RoadKind.FourLane?1:0;
            var t=new TrafficTrip{route=route,lane=lane,previousLane=lane};
            RoadNode previous=null;bool continuous=true,monotonic=true,finite=true;
            for(int s=0;s<route.Count-1;s++)
            {
                t.segment=s;
                for(int k=0;k<=40;k++)
                {
                    var p=CityVehicleGeometry.Position(c.roads,t,s,k/40f);
                    finite &= !float.IsNaN(p.x+p.z) && !float.IsInfinity(p.x+p.z);
                    if(previous!=null){monotonic &= p.x>=previous.x-.001f && sign*(p.z-previous.z)>=-.001f;continuous &= CityRoads.Length(previous,p)<.3f;}
                    previous=p;
                }
                if(s+1<route.Count-1)
                {
                    var a=CityVehicleGeometry.Position(c.roads,t,s,1);t.segment=s+1;
                    var b=CityVehicleGeometry.Position(c.roads,t,s+1,0);
                    Check(CityRoads.Length(a,b)<.001f,"Crossing graph node does not teleport between incoming/outgoing lanes");
                }
            }
            Check(monotonic,"Turn never folds inward or reverses along either axis");
            Check(continuous && finite,"Vehicle and route samples remain continuous and finite");
            var parked=CityVehicleGeometry.Position(c.roads,t,route.Count-1,0);
            Check(CityRoads.Length(parked,c.roads.Node(end))<2,"Arrived vehicle remains at destination instead of returning to preceding segment");
            var saved=TestCity.RoundTrip(c);Check(saved.Valid(),"Road geometry does not change saved entity state");
        }
        Console.WriteLine("Turn geometry checks passed: "+checks);
    }
}
