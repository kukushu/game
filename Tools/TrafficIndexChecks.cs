using System;
using System.Collections.Generic;
using System.Linq;
using HarborCity;

public static class TrafficIndexChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Traffic index: "+why);checks++;}
    static TrafficTrip Trip(int id,int from,int to)=>new TrafficTrip{id=id,route=new List<int>{from,to},progress=(id%17)/20f,lane=id%2};
    // Same pointwise lane/status/distance predicates as Following, independent of candidate order.
    static float Limit(IEnumerable<TrafficTrip> candidates,TrafficTrip self,int from,int to,int lane,int sourceLane,float proposed)
    {
        foreach(var other in candidates)
        {
            if(other.walking || other==self || other.status==TripStatus.Visiting)continue;
            bool Occupies(int l)=>other.lane==l || other.laneChangeRemaining>0 && other.laneChangeFrom==l;
            if(other.Current==from && other.Next==to && (Occupies(lane) || self.laneChangeRemaining>0 && Occupies(sourceLane)))
            {float distance=other.progress-self.progress;if(distance>.0001f)proposed=Math.Min(proposed,self.progress+distance-.12f);}
        }
        return proposed;
    }
    public static void Run()
    {
        var roads=new CityRoads();var trips=new List<TrafficTrip>{Trip(1,1,2),Trip(2,1,2),Trip(3,2,1),Trip(4,2,3)};
        var index=new DirectedTrafficIndex();index.Begin(trips,roads);
        Check(index.OnEdge(1,2).Count==2 && index.OnEdge(2,1).Count==1,"Directions are separate");
        var moving=trips[0];moving.segment=1;moving.route.Add(3);index.Refresh(moving);
        Check(!index.OnEdge(1,2).Contains(moving) && index.OnEdge(2,3).Contains(moving),"Crossed edge visible to the next car");
        moving.laneChangeFrom=0;moving.lane=1;moving.laneChangeRemaining=.2f;index.Refresh(moving);
        Check(index.OnEdge(2,3).Count==2 && index.OnEdge(2,3).Single(t=>t==moving).laneChangeFrom==0,"Dual lane occupancy stays on the real object, without duplicate entities");
        trips.Remove(moving);index.Remove(moving);index.Refresh(moving);
        Check(!index.OnEdge(2,3).Contains(moving),"Removed snapshot object cannot return through Refresh");
        var born=Trip(10,2,3);trips.Add(born);index.Add(born);
        Check(index.OnEdge(2,3).Contains(born),"New trip immediately participates in local queries");
        index.End();trips.Clear();trips.Add(Trip(20,8,9));roads.Changed();index.Begin(trips,roads);
        Check(index.OnEdge(2,3).Count==0 && index.OnEdge(8,9).Count==1,"Outside-step changes/road revision rebuild from actual state");
        index.End();index.Begin(trips,new CityRoads());
        Check(index.OnEdge(8,9).Count==1,"A different road instance with the same revision rebuilds its own index");
        index.End();index.Begin(trips,roads);
        var allocated=(Func<long>)Delegate.CreateDelegate(typeof(Func<long>),typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread"));
        for(int i=0;i<5;i++){index.End();index.Begin(trips,roads);index.Refresh(trips[0]);index.OnEdge(8,9);}
        long before=allocated();for(int i=0;i<100;i++){index.End();index.Begin(trips,roads);index.Refresh(trips[0]);index.OnEdge(8,9);}
        Check(allocated()==before,"Warm build/sync/query reuse buffers without managed allocations");index.End();
        foreach(int n in new[]{100,1000,3000})
        {
            trips=new List<TrafficTrip>();
            for(int i=0;i<n;i++)
            {
                var t=Trip(i+1,i/10,i/10+1);t.walking=i%19==0;t.status=i%13==0?TripStatus.Visiting:TripStatus.Driving;
                t.laneChangeFrom=1-t.lane;t.laneChangeRemaining=i%7==0?.2f:0;trips.Add(t);
            }
            index.Begin(trips,roads);long local=0;
            foreach(var t in trips)
            {
                var candidates=index.OnEdge(t.Current,t.Next);local+=candidates.Count;
                Check(Limit(trips,t,t.Current,t.Next,t.lane,t.laneChangeFrom,.95f)==Limit(candidates,t,t.Current,t.Next,t.lane,t.laneChangeFrom,.95f),"Local Following equals full scan at "+n);
            }
            Check(local==n*10L,"Ten tasks per directed edge keep candidate count proportional to N");
            Console.WriteLine("Candidate-only scale: N="+n+", full="+(n*(long)n)+", local="+local+" (not a full-city FPS benchmark)");index.End();
        }
        trips=new List<TrafficTrip>();for(int i=0;i<100;i++)trips.Add(Trip(i+1,1,2));
        index.Begin(trips,roads);
        Check(index.OnEdge(1,2).Count==100,"Concentrated traffic keeps all candidates; no precision loss or task cap");
        index.End();
        Console.WriteLine("PASS: "+checks+" directed traffic index/oracle/lifecycle checks");
    }
}
