using System;
using System.Linq;
using HarborCity;

public static class PedestrianChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Pedestrian: "+why);checks++;}
    static CityModel Fixture(out CityTraffic sim,out CityResident child,out int school,out int node)
    {
        var c=TestCity.Create();c.development.enabled=true;c.waste.enabled=false;c.society.settings.applicantsPerDay=0;
        foreach(var p in c.Citizens)c.ReleaseJob(p);
        var plan=c.roads.Plan(c,TestCity.P(0,-40),TestCity.P(0,40),TestCity.Flat);
        if(!c.CommitRoad(plan))throw new Exception(plan.error);
        UtilitySupplyChecks.ConnectFixture(c);school=TestCity.Build(c,40,-5,LandUse.ElementarySchool);
        child=c.Citizens.First(c.ElementaryEligible);node=c.roads.nodes.First(n=>Math.Abs(n.x)<.01f && Math.Abs(n.z-1.5f)<.01f).id;
        c.society.dayElapsed=40;c.Recalculate();sim=new CityTraffic(c);
        if(!c.EnrollElementary(child.id,school))throw new Exception("Enrollment fixture");return c;
    }
    static void Approach(CityModel c,TrafficTrip t,int node,float distance=.4f)
    {int index=t.route.IndexOf(node);if(index<1 || index==t.route.Count-1)throw new Exception("Actual trip does not cross fixture junction");t.segment=index-1;t.progress=1-distance/(c.roads.EdgeLength(t.Current,t.Next)/3);}
    static bool Inside(CityModel c,TrafficTrip t,int node)
    {int index=t.route.IndexOf(node);if(index<0 || index>t.segment)return false;float distance=t.progress*c.roads.EdgeLength(t.Current,t.Next)/3;for(int i=index;i<t.segment;i++)distance+=c.roads.EdgeLength(t.route[i],t.route[i+1])/3;return distance<.4f;}
    static float Phase(CityModel c,int node,int from,bool green)
    {for(float time=0;time<20;time+=.1f)if((c.roads.PedestrianSignal(node,from,time)==SignalAspect.Green)==green && (c.roads.PedestrianSignal(node,from,time+.5f)==SignalAspect.Green)==green)return time;throw new Exception("Phase fixture");}
    public static void Run()
    {
        var c=Fixture(out var sim,out var p,out int school,out int node);
        c.roads.SetJunctionControl(node,JunctionControl.Signals,out _);var walk=sim.DispatchSchool(p.id);Approach(c,walk,node);
        c.traffic.clock=Phase(c,node,walk.Current,false);sim=new CityTraffic(c);sim.Advance(.3f);
        Check(walk.Next==node && walk.blocked>0 && walk.junctionWaitReason.Contains("行人通行信号") && !p.atSchool,"Real enrolled child waits at red rather than gaining school attendance");
        var observed=c.ObserveResident(c.society.families.First(h=>h.id==p.householdId),p);
        Check(observed.state=="步行等待过街" && observed.reason.Contains("行人通行信号") && observed.destination==school,"Existing resident detail explains the actual crossing wait and retains destination");
        var saved=TestCity.RoundTrip(c);float progress=walk.progress,clock=c.traffic.clock;var savedSim=new CityTraffic(saved);
        Check(saved.traffic.clock==clock && saved.traffic.trips.First(t=>t.id==walk.id).progress==progress,"Loading a waiting pedestrian adds no progress or elapsed time");
        int walkId=walk.id;
        for(int i=0;i<1200 && !p.atSchool;i++){sim.Advance(.05f);savedSim.Advance(.05f);}
        Check(p.atSchool && p.location==school && p.tripId==0 && c.Valid(),"Changing signal phase admits child to the actual school");
        Check(TestCity.Snapshot(c)==TestCity.Snapshot(saved),"Red-light waiting save continues identically to actual attendance");
        walk=sim.DispatchSchool(p.id,true);Check(walk!=null && walk.walking && walk.returning,"Actual pupil starts walking home after school");
        for(int i=0;i<1200 && p.tripId>0;i++)sim.Advance(.05f);
        Check(p.location==c.society.families.First(h=>h.id==p.householdId).home && !p.schoolReturning && c.Valid(),"Return crosses the same signal-controlled junction and finishes at home");
        // Parallel, perpendicular, turning and opposite straight freight; use
        // actual building destinations and reverse task order in every case.
        for(int reverse=0;reverse<2;reverse++)for(int conflict=0;conflict<4;conflict++)
        {
            c=Fixture(out sim,out p,out school,out node);walk=sim.DispatchSchool(p.id);Approach(c,walk,node,.36f);
            var second=c.Citizens.Where(c.ElementaryEligible).Skip(1).First();Check(c.EnrollElementary(second.id,school),"Second real child enrolls");
            var companion=sim.DispatchSchool(second.id);Approach(c,companion,node,.36f);
            int factory=c.buildings.OfType<IndustrialBuilding>().First(b=>b.x<0).id,shop=c.buildings.OfType<CommercialBuilding>().First(b=>b.x>0).id;
            if(conflict==1){c.money+=10000;factory=TestCity.Build(c,5,-20,LandUse.Industrial);shop=TestCity.Build(c,5,20,LandUse.Commercial);}
            if(conflict==2){c.money+=10000;shop=TestCity.Build(c,5,20,LandUse.Commercial);}
            if(conflict==3){factory=c.buildings.OfType<IndustrialBuilding>().First(b=>b.x>0).id;shop=c.buildings.OfType<CommercialBuilding>().First(b=>b.x<0).id;}
            bool crosses=conflict==1 || conflict==2;
            if(!crosses){c.roads.SetJunctionControl(node,JunctionControl.Signals,out _);c.traffic.clock=Phase(c,node,walk.Current,true);}
            c.Inventory(factory).Stock=8;var car=sim.Dispatch(factory,shop,TripPurpose.Delivery);Approach(c,car,node);
            if(reverse==1)c.traffic.trips.Reverse();sim=new CityTraffic(c);sim.Advance(.1f);
            saved=TestCity.RoundTrip(c);savedSim=new CityTraffic(saved);sim.Advance(.5f);savedSim.Advance(.5f);
            Check(TestCity.Snapshot(c)==TestCity.Snapshot(saved),"Loading during admission restores compatible/conflicting movement owners without changing the outcome");
            Check(Inside(c,walk,node) && Inside(c,companion,node) && (crosses?car.Next==node && car.blocked>0:car.segment>=car.route.IndexOf(node) && car.blocked==0),"Real walkers admit parallel freight but protect perpendicular/turning freight independently of task order: "+reverse+" conflict="+conflict+" car="+car.Current+"/"+car.Next+" why="+car.junctionWaitReason);
            int carId=car.id;bool mixed=false;
            for(int i=0;i<1600;i++){sim.Advance(.05f);savedSim.Advance(.05f);mixed|=(Inside(c,walk,node) || Inside(c,companion,node)) && Inside(c,car,node);}
            Check((!crosses || !mixed) && c.Goods(shop)>=8 && !c.traffic.trips.Any(t=>t.id==carId) && c.traffic.failed==0 && c.Valid(),"Real parallel/conflicting cargo delivers and returns without deleting travelers or mixing conflicting occupancy");
            Check(TestCity.Snapshot(c)==TestCity.Snapshot(saved),"In-crossing pedestrians restore their physical occupancy through save/load");
        }
        c=Fixture(out sim,out p,out school,out node);walk=sim.DispatchSchool(p.id);Approach(c,walk,node);int from=walk.Current;
        int incoming=c.roads.edges.First(e=>e.a==from && e.b==node || e.a==node && e.b==from).id;
        Check(c.ChangeRoadDirection(incoming,true,false,out _) && c.ChangeRoadDirection(incoming,true,true,out _),"One-way fixture can reverse a road with only walking traffic");
        c.roads.SetJunctionControl(node,JunctionControl.Signals,out _);
        bool greenSeen=false,redSeen=false;
        for(float t=0;t<12;t+=.1f){greenSeen|=c.roads.PedestrianSignal(node,from,t)==SignalAspect.Green;redSeen|=c.roads.PedestrianSignal(node,from,t)==SignalAspect.Red;}
        Check(greenSeen && redSeen && c.roads.CanTravel(from,node,true) && !c.roads.CanTravel(from,node),"Walking against a one-way road still gets a pedestrian phase instead of permanent red");
        sim=new CityTraffic(c);for(int i=0;i<1200 && !p.atSchool;i++)sim.Advance(.05f);
        Check(p.atSchool && c.Valid(),"Actual counterflow pedestrian completes school journey on one-way road");
        Console.WriteLine("PASS: "+checks+" actual pedestrian/signal/yield/school/one-way/save checks");
    }
}
