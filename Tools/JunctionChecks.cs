using System;
using System.Linq;
using HarborCity;
public static class JunctionChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Junction: "+why);checks++;}
    public static CityModel Crossroads()
    {
        var c=TestCity.Empty();
        foreach(var p in new[]{new[]{c.roads.Node(CityRoads.Entrance),TestCity.P(48,1.5f)},new[]{TestCity.P(0,-40),TestCity.P(0,40)}})
        {var plan=c.roads.Plan(c,p[0],p[1],TestCity.Flat);if(!c.CommitRoad(plan))throw new Exception(plan.error);}
        return c;
    }
    static int Center(CityModel c)=>c.roads.nodes.First(n=>Math.Abs(n.x)<.01f && Math.Abs(n.z-1.5f)<.01f).id;
    static int Neighbor(CityModel c,int node,float x,float z)=>c.roads.Neighbors(node).OrderBy(n=>CityRoads.Length(c.roads.Node(n),TestCity.P(x,z))).First();
    static void Approach(CityModel c,TrafficTrip t,int node)
    {
        int i=t.route.IndexOf(node);if(i<1 || i==t.route.Count-1)throw new Exception("Fixture does not cross junction");
        t.segment=i-1;t.progress=1-.4f/(CityRoads.Length(c.roads.Node(t.Current),c.roads.Node(t.Next))/3);t.blocked=0;
    }
    static bool InJunction(CityModel c,TrafficTrip t,int node)=>t.Current==node && t.progress*CityRoads.Length(c.roads.Node(t.Current),c.roads.Node(t.Next))/3<.4f;
    public static void Run()
    {
        var c=Crossroads();int node=Center(c),w=Neighbor(c,node,-10,1.5f),e=Neighbor(c,node,10,1.5f),n=Neighbor(c,node,0,10),s=Neighbor(c,node,0,-10);
        Check(!CityJunctions.Conflicts(c.roads,node,w,e,e,w),"Opposing straight traffic can share a junction");
        Check(CityJunctions.Conflicts(c.roads,node,w,e,n,s),"Crossing straight traffic conflicts");
        Check(!CityJunctions.Conflicts(c.roads,node,w,s,e,n),"Opposing right turns can pass together");
        Check(CityJunctions.Conflicts(c.roads,node,w,e,e,s),"Left turn across oncoming straight traffic conflicts");
        Check(CityJunctions.Conflicts(c.roads,node,w,e,n,e),"Merging into one outgoing lane conflicts");
        Check(CityJunctions.Conflicts(c.roads,node,w,e,w,n),"One incoming queue cannot overtake at a junction");
        Check(CityJunctions.Conflicts(c.roads,node,-1,e,w,n),"Unknown entry is conservative");
        int[] arms={w,e,n,s};bool symmetric=true;
        foreach(int a in arms)foreach(int b in arms.Where(x=>x!=a))foreach(int d in arms)foreach(int f in arms.Where(x=>x!=d))
            symmetric &= CityJunctions.Conflicts(c.roads,node,a,b,d,f)==CityJunctions.Conflicts(c.roads,node,d,f,a,b);
        Check(symmetric,"Conflict rules are independent of vehicle ordering");
        int west=TestCity.Build(c,-35,7,LandUse.Industrial),eastShop=TestCity.Build(c,35,7,LandUse.Commercial);
        int east=TestCity.Build(c,35,-5,LandUse.Industrial),westShop=TestCity.Build(c,-35,-5,LandUse.Commercial);
        c.Inventory(west).Stock=8;c.Inventory(east).Stock=8;var sim=new CityTraffic(c);
        var aTrip=sim.Dispatch(west,eastShop,TripPurpose.Delivery);var bTrip=sim.Dispatch(east,westShop,TripPurpose.Delivery);
        Approach(c,aTrip,node);Approach(c,bTrip,node);sim=new CityTraffic(c);sim.Advance(.15f);
        Check(aTrip.Current==node && bTrip.Current==node && aTrip.blocked==0 && bTrip.blocked==0,"Two real opposite freight trips enter together");
        var saved=TestCity.RoundTrip(c);var savedSim=new CityTraffic(saved);int aId=aTrip.id,bId=bTrip.id;
        for(int i=0;i<1200;i++){sim.Advance(.05f);savedSim.Advance(.05f);}
        Check(c.Goods(eastShop)>=8 && c.Goods(westShop)>=8 && c.traffic.trips.All(t=>t.id!=aId && t.id!=bId) && c.traffic.failed==0 && c.Valid(),"Concurrent freight delivers both cargos and returns without failed tasks");
        Check(TestCity.Snapshot(saved)==TestCity.Snapshot(c),"Saving inside a shared junction reconstructs identical continuation");
        c=Crossroads();node=Center(c);west=TestCity.Build(c,-35,7,LandUse.Industrial);eastShop=TestCity.Build(c,35,7,LandUse.Commercial);
        int north=TestCity.Build(c,5,30,LandUse.Industrial),southShop=TestCity.Build(c,5,-30,LandUse.Commercial);
        c.Inventory(west).Stock=8;c.Inventory(north).Stock=8;sim=new CityTraffic(c);
        aTrip=sim.Dispatch(west,eastShop,TripPurpose.Delivery);bTrip=sim.Dispatch(north,southShop,TripPurpose.Delivery);
        Approach(c,aTrip,node);Approach(c,bTrip,node);sim=new CityTraffic(c);bool overlap=false,waited=false;
        for(int i=0;i<1200;i++)
        {
            sim.Advance(.05f);overlap |= InJunction(c,aTrip,node) && InJunction(c,bTrip,node);waited |= aTrip.blocked>0 || bTrip.blocked>0;
        }
        Check(waited && !overlap,"Crossing freight yields without simultaneous conflicting departures");
        Check(c.Goods(eastShop)>=8 && c.Goods(southShop)>=8 && c.traffic.failed==0 && c.Valid(),"Both crossing deliveries eventually finish and preserve cargo");
        c=Crossroads();node=Center(c);west=TestCity.Build(c,-35,7,LandUse.Industrial);eastShop=TestCity.Build(c,35,7,LandUse.Commercial);
        c.Inventory(west).Stock=8;sim=new CityTraffic(c);aTrip=sim.Dispatch(west,eastShop,TripPurpose.Delivery);Approach(c,aTrip,node);
        c.Inventory(west).Stock=8;bTrip=sim.Dispatch(west,eastShop,TripPurpose.Delivery);
        bTrip.segment=bTrip.route.IndexOf(node);bTrip.progress=.1f;
        sim=new CityTraffic(c);sim.Advance(.1f);
        Check(aTrip.Next==node && (1-aTrip.progress)*CityRoads.Length(c.roads.Node(aTrip.Current),c.roads.Node(aTrip.Next))/3>=.35f,"A queued exit holds the next truck outside the junction");
        for(int i=0;i<1200;i++)sim.Advance(.05f);
        Check(c.Goods(eastShop)>=16 && c.traffic.failed==0 && c.Valid(),"Clearing the exit resumes queued real cargo without deletion");
        Console.WriteLine("PASS: "+checks+" junction conflicts/concurrent real freight/yielding/save checks");
    }
}
