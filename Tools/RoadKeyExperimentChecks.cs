using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using HarborCity;
public static class RoadKeyExperimentChecks
{
    static int checks;
    static void Check(bool value,string message){checks++;if(!value)throw new Exception("Road key: "+message);}
    static long Key(int a,int b)=>((long)a<<32)|(uint)b;
    public static void Run()
    {
        var reference=new Dictionary<long,int>();var proposed=new Dictionary<long,int>(CityRoadKeyComparer.Instance);
        var keys=new List<long>();var random=new Random(61);
        for(int i=0;i<10000;i++)
        {
            int a=random.Next(),b=i%2==0?a+1:random.Next();long key=((long)a<<32)|(uint)b;
            keys.Add(key);reference[key]=proposed[key]=i;
        }
        foreach(long key in keys)if(reference[key]!=proposed[key] || !CityRoadKeyComparer.Instance.Equals(key,key))throw new Exception("Road key equality/value changed");
        long x=((long)1<<32)|2,y=((long)2<<32)|1;
        if(x.GetHashCode()!=y.GetHashCode() || CityRoadKeyComparer.Instance.Equals(x,y))throw new Exception("Reversed key must collide in default hash but remain distinct in equality");
        proposed[x]=1;proposed[y]=2;if(proposed[x]!=1 || proposed[y]!=2)throw new Exception("Direction lost");
        proposed.Remove(x);if(proposed.ContainsKey(x) || proposed[y]!=2)throw new Exception("Key removal affected reverse direction");
        foreach(int a in new[]{int.MinValue,-1,0,1,65535,65536,1000000000,int.MaxValue})
        foreach(int b in new[]{int.MinValue,-1,0,1,65535,65536,1000000000,int.MaxValue})
        {long k=Key(a,b);proposed[k]=a;Check(proposed[k]==a && CityRoadKeyComparer.Instance.Equals(k,Key(a,b)),"Full signed/unsigned bit patterns");if(a!=b)Check(!CityRoadKeyComparer.Instance.Equals(k,Key(b,a)),"Direction preserves all bits");}
        var roads=new CityRoads{nextNode=1000020000};
        const int width=100,height=200;
        for(int z=0;z<height;z++)for(int a=0;a<width;a++)
            roads.nodes.Add(new RoadNode{id=z==0 && a==0?CityRoads.Entrance:1000000000+z*width+a,x=-149+a*3,z=-149+z*1.5f,y=1});
        for(int z=0;z<height;z++)for(int a=0;a<width;a++)
        {
            int i=z*width+a;
            if(a+1<width)Add(i,i+1);
            if(z+1<height)Add(i,i+width);
        }
        roads.Changed();Check(roads.Valid(),"20000 sparse-ID nodes / 39700 edges validate");
        foreach(var e in roads.edges.Where((e,i)=>i%37==0))
        {Check(roads.CanTravel(e.a,e.b) && roads.CanTravel(e.b,e.a)==!e.oneWay,"Large graph direction");Check(roads.Kind(e.a,e.b)==e.kind && roads.LaneCount(e.a,e.b)==CityRoadTypes.Lanes(e.kind,e.oneWay),"Large graph lane/type lookup");}
        var first=roads.edges[0];int from=first.a,to=first.b;
        first.oneWay=!first.oneWay;roads.Changed();Check(roads.CanTravel(to,from)==!first.oneWay,"Changed direction rebuilds derived lookup");
        roads.edges.Remove(first);roads.Changed();Check(!roads.CanTravel(from,to) && roads.LaneCount(from,to)==0,"Demolition clears old lookup");
        roads.edges.Add(first);first.kind=RoadKind.FourLane;first.oneWay=false;roads.Changed();Check(roads.CanTravel(to,from) && roads.LaneCount(from,to)==2,"Rebuild/widen updates lookup");
        var json=new JavaScriptSerializer{MaxJsonLength=int.MaxValue};var loaded=json.Deserialize<CityRoads>(json.Serialize(roads));
        Check(loaded.Valid() && loaded.CanTravel(from,to) && loaded.Kind(from,to)==RoadKind.FourLane,"Sparse large road save restores and rebuilds lookups");
        // Legacy caches remain for construction; new traffic queries need not create them.
        loaded.Node(from);
        var cache=typeof(CityRoads).GetField("roadLookup",System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(loaded) as Dictionary<long,RoadEdge>;
#if CITY_ROAD_KEY_HASH_DISABLED
        Check(cache.Comparer.Equals(EqualityComparer<long>.Default),"Explicit fallback uses original comparer");
#else
        Check(ReferenceEquals(cache.Comparer,CityRoadKeyComparer.Instance),"Normal build uses optimized comparer by default");
#endif
        Console.WriteLine("PASS: 10000 key cases + "+checks+" edge-ID/direction/dynamic/save/large-network/default-comparer checks");
        void Add(int a,int b){bool one=roads.nextEdge%3==0;roads.edges.Add(new RoadEdge{id=roads.nextEdge++,a=roads.nodes[a].id,b=roads.nodes[b].id,kind=one?RoadKind.Highway:RoadKind.Standard,oneWay=one});}
    }
}
