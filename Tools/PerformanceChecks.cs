using System;
using System.Collections.Generic;
using System.Linq;
using HarborCity;

public static class PerformanceChecks
{
    static int count;
    static void Check(bool valid,string message){if(!valid)throw new Exception(message);count++;}
    public static void Run()
    {
        var c=TestCity.Empty();
        var plan=c.roads.Plan(c,c.roads.Node(CityRoads.Entrance),TestCity.P(20,1.5f),TestCity.Flat);
        Check(c.CommitRoad(plan),"Performance fixture construction");
        int from=CityRoads.Entrance,to=c.roads.nodes.Last().id;
        List<int> Path(int a,int b,bool walking=false)=>c.roads.FindPath(new List<int>{a},new List<int>{b},walking);
        var expected=Path(from,to);var edited=Path(from,to);edited.Clear();
        Check(Path(from,to).SequenceEqual(expected),"Mutable returned route must not corrupt cache");
        Check(Path(to,from).Count>0,"Warm reverse route");
        foreach(var edge in c.roads.edges)edge.oneWay=true;c.roads.Changed();
        Check(Path(to,from).Count==0 && Path(from,to).Count>0,"Direction edit invalidates cached routes");
        Check(Path(to,from,true).Count>0,"Walking and vehicle cache entries remain separate");
        foreach(var a in c.roads.nodes)foreach(var b in c.roads.nodes)Path(a.id,b.id);
        Check(Path(from,to).SequenceEqual(expected),"Bounded cache eviction preserves route");
        var serializer=new System.Web.Script.Serialization.JavaScriptSerializer{MaxJsonLength=int.MaxValue};
        var copy=serializer.Deserialize<CitySaveData>(serializer.Serialize(c.ToSaveData())).ToCity();
        Check(copy.roads.FindPath(new List<int>{to},new List<int>{from}).Count==0,"Derived route cache is not save state");
        var first=c.roads.edges[0];
        Check(c.roads.Ground(first.a,first.b) && c.roads.Ground(first.b,first.a),"Ground structure lookup both directions");
        first.structure=RoadStructure.Tunnel;c.roads.Changed();
        Check(!c.roads.Ground(first.a,first.b) && c.roads.Underground(first.b,first.a),"Structure edit invalidates lookup");
        var trip=new TrafficTrip{route=expected,segment=1,lane=0};
        for(int i=0;i<=20;i++)
        {
            float fraction=i/20f;var p=CityVehicleGeometry.PositionValue(c.roads,trip,trip.segment,fraction);
            var n=CityVehicleGeometry.Position(c.roads,trip,trip.segment,fraction);
            Check(p.x==n.x && p.y==n.y && p.z==n.z,"Value geometry agrees with road-node compatibility API");
        }
        var allocated=(Func<long>)Delegate.CreateDelegate(typeof(Func<long>),typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread"));
        CityVehicleGeometry.PositionValue(c.roads,trip,1,.8f);long before=allocated();
        for(int i=0;i<1000;i++)CityVehicleGeometry.PositionValue(c.roads,trip,1,.8f);
        Check(allocated()==before,"Repeated vehicle value geometry must not allocate managed objects");
        c.roads.edges.Clear();c.roads.Changed();
        Check(Path(from,to).Count==0 && !c.roads.Underground(first.a,first.b),"Road removal invalidates derived caches");
        Console.WriteLine("PASS: "+count+" performance cache/geometry/invalidation checks");
    }
}
