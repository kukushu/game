using System;
using System.Linq;
using HarborCity;

public static class JunctionGeometryCacheChecks
{
    static int checks;
    static void Check(bool condition,string reason){if(!condition)throw new Exception("Junction radius cache: "+reason);checks++;}
    static void Equivalent(CityRoads roads)
    {
        foreach(var n in roads.nodes)
            Check(CityJunctions.CrossingRadius(roads,n.id)==CityJunctions.CalculateCrossingRadius(roads,n.id),"Cached radius must equal unchanged algorithm");
    }
    public static void Run()
    {
        var city=JunctionChecks.Crossroads();var roads=city.roads;Equivalent(roads);
        int center=roads.nodes.First(n=>Math.Abs(n.x)<.01f && Math.Abs(n.z-1.5f)<.01f).id;float initial=CityJunctions.CrossingRadius(roads,center);
        int count=roads.junctionRadii.Count;
        Check(CityJunctions.CrossingRadius(roads,center)==initial && roads.junctionRadii.Count==count,"Repeat lookup reuses entry");
        var allocation=(Func<long>)Delegate.CreateDelegate(typeof(Func<long>),typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread"));
        long before=allocation();for(int i=0;i<1000;i++)CityJunctions.CrossingRadius(roads,center);
        Check(allocation()==before,"Warm radius lookup allocates zero managed bytes");
        foreach(var e in roads.edges)e.kind=RoadKind.FourLane;roads.Changed();Equivalent(roads);
        Check(CityJunctions.CrossingRadius(roads,center)>initial,"Width upgrade invalidates radius");
        foreach(var e in roads.edges){e.kind=RoadKind.Standard;e.oneWay=true;e.structure=RoadStructure.Elevated;}roads.Changed();Equivalent(roads);
        var arm=roads.Neighbors(center)[0];roads.Node(arm).z+=4;roads.Changed();Equivalent(roads);
        roads.Node(center).y+=3;roads.Changed();Equivalent(roads);
        roads.Node(center).control=JunctionControl.Signals;roads.Changed();Equivalent(roads);
        var copy=TestCity.RoundTrip(city);Check(copy.roads.junctionRadii==null,"Derived cache is absent from save");Equivalent(copy.roads);
        var other=JunctionChecks.Crossroads();foreach(var e in other.roads.edges)e.kind=RoadKind.FourLane;other.roads.Changed();Equivalent(other.roads);
        Check(!ReferenceEquals(roads.junctionRadii,other.roads.junctionRadii),"Identical node IDs in different cities cannot share a cache");
        var remove=roads.edges.Where(e=>e.a==center || e.b==center).Select(e=>e.id).ToList();roads.Remove(remove);Equivalent(roads);
        Check(CityJunctions.CrossingRadius(roads,int.MaxValue)==CityJunctions.CalculateCrossingRadius(roads,int.MaxValue) && !roads.junctionRadii.ContainsKey(int.MaxValue),"Missing node remains equivalent and is not retained");
        Console.WriteLine("PASS: "+checks+" junction radius cache/oracle/invalidation checks");
    }
}
