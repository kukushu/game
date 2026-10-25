using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using HarborCity.RoadCore;
static class CityFromLaneChecks
{
    static int checks,queries;static BinaryWriter output;
    static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
    static RoadAccess Other(Network n,RoadAccess a)
    {var d=n.Get(a.lane);long id=Network.LaneId(d.RoadId,!d.Reverse,d.LaneIndex);return n.Contains(id)?new RoadAccess{endpoint=a.endpoint,lane=id,distance=d.Geometry.MapReverseDistance(n.Get(id).Geometry,a.distance),use=a.use}:null;}
    static double Cost(Network n,List<long> r,double start,double end)
    {if(r==null)return double.PositiveInfinity;double c=0;for(int i=0;i<r.Count;i++){var d=n.Get(r[i]);c+=((i==r.Count-1?end:d.EndDistance)-(i==0?start:d.StartDistance))/d.Speed;}return c;}
    // Frozen original FromLane including strict cost comparison and goal order.
    static List<long> Old(Network n,long lane,double distance,RoadAccess target,bool both,out double end)
    {var source=new RoadAccess{lane=lane,distance=distance};var r=n.Route(source,target);end=target.distance;double cost=Cost(n,r,distance,end);if(both){var opposite=Other(n,target);if(opposite!=null){var next=n.Route(source,opposite);if(Cost(n,next,distance,opposite.distance)<cost){r=next;end=opposite.distance;}}}return r;}
    static void Pair(Network n,long lane,double distance,RoadAccess target,bool both)
    {
        queries++;var expected=Old(n,lane,distance,target,both,out double a);var actual=AccessRouting.FromLane(n,lane,distance,target,both,out double b);
        if(output!=null){output.Write(actual!=null);output.Write(b);if(actual!=null){output.Write(actual.Count);foreach(long id in actual)output.Write(id);output.Write(Cost(n,actual,distance,b));}}
        string detail="source="+lane+"/"+distance+" target="+target.lane+"/"+target.distance+" both="+both;
        Check((expected==null)==(actual==null),"Reachability changed "+detail);Check(BitConverter.DoubleToInt64Bits(a)==BitConverter.DoubleToInt64Bits(b),"Destination side/distance changed "+detail);
        if(actual!=null){Check(expected.SequenceEqual(actual),"Exact route/tie order changed "+detail+" old="+string.Join("/",expected)+" new="+string.Join("/",actual));n.ValidateRoute(actual);Check(BitConverter.DoubleToInt64Bits(Cost(n,expected,distance,a))==BitConverter.DoubleToInt64Bits(Cost(n,actual,distance,b)),"Cost bits changed");}
    }
    static Network Grid(int seed)
    {
        var data=new NetworkData();var random=new Random(seed);
        for(int z=0;z<4;z++)for(int x=0;x<4;x++)data.junctions.Add(new Junction{id=data.nextJunction++,position=new Point(x*25,2,z*25)});
        for(int z=0;z<4;z++)for(int x=0;x<4;x++)foreach(int next in new[]{x<3?1:0,z<3?4:0})
        {
            if(next==0)continue;int from=1+x+4*z,to=from+next;var a=data.junctions[from-1].position;var b=data.junctions[to-1].position;
            data.roads.Add(new PhysicalRoad{id=data.nextRoad++,from=from,to=to,direction=seed==0?RoadDirection.Both:(RoadDirection)random.Next(3),lanes=seed==0?1:random.Next(1,3),speed=seed==0?12:8+random.NextDouble()*12,points=new[]{a,Point.Lerp(a,b,.5),b}});
        }
        int p=data.nextJunction++,q=data.nextJunction++;data.junctions.Add(new Junction{id=p,position=new Point(200,2,0)});data.junctions.Add(new Junction{id=q,position=new Point(230,2,0)});data.roads.Add(new PhysicalRoad{id=data.nextRoad++,from=p,to=q,direction=RoadDirection.Both,lanes=2,speed=12,points=new[]{new Point(200,2,0),new Point(230,2,0)}});
        return new Network(data);
    }
    static void Small(Network n)
    {
        var lanes=n.Drivables.Where(d=>!d.IsConnection).OrderBy(d=>d.Id).Take(8).ToArray();
        foreach(var a in lanes)foreach(var b in lanes)foreach(double x in new[]{0.0,.5,1.0})foreach(double y in new[]{0.0,.3,1.0})foreach(bool both in new[]{false,true})Pair(n,a.Id,a.StartDistance+x*a.TravelLength,new RoadAccess{lane=b.Id,distance=b.StartDistance+y*b.TravelLength},both);
    }
    static void Main(string[] args)
    {
        if(args.Length>0)output=new BinaryWriter(File.Create(args[0]));
        foreach(int seed in new[]{0,7,41})
        {
            var n=Grid(seed);var lanes=n.Drivables.Where(d=>!d.IsConnection).OrderBy(d=>d.Id).ToArray();
            foreach(var source in lanes.Take(12))foreach(var goal in lanes.Where((d,i)=>i%3==0))foreach(double x in new[]{0.0,.5,1.0})foreach(double y in new[]{0.0,.5,1.0})foreach(bool both in new[]{false,true})Pair(n,source.Id,source.StartDistance+x*source.TravelLength,new RoadAccess{lane=goal.Id,distance=goal.StartDistance+y*goal.TravelLength},both);
            var random=new Random(seed+991);for(int i=0;i<12000;i++){var a=lanes[random.Next(lanes.Length)];var b=lanes[random.Next(lanes.Length)];Pair(n,a.Id,a.StartDistance+random.NextDouble()*a.TravelLength,new RoadAccess{lane=b.Id,distance=b.StartDistance+random.NextDouble()*b.TravelLength},i%2==0);}
            var first=lanes[0];Pair(n,first.Id,first.StartDistance,new RoadAccess{lane=first.Id,distance=first.EndDistance,valid=false},true);Pair(n,first.Id,first.StartDistance-1,new RoadAccess{lane=first.Id,distance=first.EndDistance},true);
        }
        // Two collinear roads provide zero-length connections and terminal ties.
        var w=new World();int j=w.AddJunction(new Point()),k=w.AddJunction(new Point(20,0,0)),l=w.AddJunction(new Point(40,0,0));w.AddRoad(j,k,new[]{w.JunctionPosition(j),w.JunctionPosition(k)});w.AddRoad(k,l,new[]{w.JunctionPosition(k),w.JunctionPosition(l)});
        foreach(var a in w.Network.Drivables.Where(d=>!d.IsConnection))foreach(var b in w.Network.Drivables.Where(d=>!d.IsConnection))foreach(double s in new[]{a.StartDistance,a.EndDistance})foreach(double e in new[]{b.StartDistance,b.EndDistance})Pair(w.Network,a.Id,s,new RoadAccess{lane=b.Id,distance=e},true);
        var original=w.Network;Small(original);w.SplitRoadAt(1,10);Small(w.Network);
        if(!w.PlanUpgradeRoad(2,2,18,RoadDirection.Forward).TryCommit())throw new Exception("Warm-cache upgrade rejected");Small(w.Network);
        if(!w.PlanUpgradeRoad(2,2,22,RoadDirection.Forward).TryCommit())throw new Exception("Warm-cache speed edit rejected");Small(w.Network);
        w.RemoveRoad(2);Small(w.Network);Small(original);Small(SaveCodec.Read(SaveCodec.Write(w)).Network);
        output?.Dispose();Console.WriteLine("PASS "+checks+" checks / "+queries+" exact old FromLane queries: route, terminal bits, cost bits, one-way, multilane, disconnected, zero connection, ties and boundaries");
    }
}
