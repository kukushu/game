using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using HarborCity.RoadCore;

public static class RoadCoreChecks
{
    static int checks;
    static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
    static void Reject(Action action,string message){bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}Check(rejected,message);}
    public static World Fixture(int count,bool congested)
    {
        return HarborCity.CityRoadCoreFixtures.Create(count,congested);
    }
    static void Tests()
    {
        var w=new World();int a=w.AddJunction(new Point(0,0,0)),b=w.AddJunction(new Point(30,3,0)),c=w.AddJunction(new Point(50,3,20));
        w.AddRoad(a,b,new[]{w.JunctionPosition(a),new Point(15,1.5,3),w.JunctionPosition(b)},false);w.AddRoad(b,c,new[]{w.JunctionPosition(b),w.JunctionPosition(c)},true);
        Check(w.Network.JunctionCount==3&&w.Network.RoadCount==2,"Samples became topology nodes");Check(w.Network.Route(c,a)==null,"One-way reversal allowed");Check(w.Network.Route(a,c).Count==3,"Route expanded geometry");
        int d1=w.AddDepot(a,10),d2=w.AddDepot(c,0);var v=w.Dispatch(d1,d2);Check(v!=null&&w.GetDepot(d1).stock==9,"Dispatch stock");Check(w.Dispatch(d1,d2)==null,"Overlapping spawn");w.Advance(.31);Check(Math.Abs(w.Clock-.3)<1e-9,"Dropped/extra steps");
        Point before=w.Position(v);int j=w.SplitRoad(1,1);Check(Point.Distance(before,w.Position(v))<1e-8,"Split position jumped");Check(v.route.Count==5,"Split route missing link");Check(w.Network.JunctionCount==4,"Split junction missing");
        var clone=SaveCodec.Read(SaveCodec.Write(w));
        for(int i=0;i<180;i++){w.Step();clone.Step();w.AssertSafety();Check(Convert.ToBase64String(SaveCodec.Write(w))==Convert.ToBase64String(SaveCodec.Write(clone)),"Loaded continuation differs");}
        Check(w.Delivered==1&&v.cargo==0&&w.GetDepot(d2).stock==1,"Cargo delivery");Check(w.GetDepot(d1).cash==100005&&w.GetDepot(d2).cash==99995,"Cash conservation");for(int i=0;i<50;i++)w.Step();Check(w.Delivered==1,"Double settlement");
        var reverse=w.Dispatch(d2,d1);Check(reverse==null,"Destination one-way should be unreachable");
        var uninterrupted=new World();int x=uninterrupted.AddJunction(new Point(0,0,0)),y=uninterrupted.AddJunction(new Point(40,0,0));uninterrupted.AddRoad(x,y,new[]{uninterrupted.JunctionPosition(x),new Point(20,0,0),uninterrupted.JunctionPosition(y)});int da=uninterrupted.AddDepot(x),db=uninterrupted.AddDepot(y);var back=uninterrupted.Dispatch(db,da);uninterrupted.Advance(.5);before=uninterrupted.Position(back);uninterrupted.SplitRoad(1,1);Check(Point.Distance(before,uninterrupted.Position(back))<1e-8,"Reverse split jumped");uninterrupted.RemoveRoad(1);Check(back.status==VehicleStatus.Stranded&&back.cargo==1&&Point.Distance(before,uninterrupted.Position(back))<1e-8,"Demolition lost cargo/position");var restored=SaveCodec.Read(SaveCodec.Write(uninterrupted));Check(restored.ActiveCount==0,"Stranded save resumed blindly");
        Reject(()=>new World(new WorldData{coreFormat=34}),"Old save accepted");var invalid=w.Snapshot();invalid.network.nextRoad=1;Reject(()=>new World(invalid),"Allocator reuse accepted");
        // Ownership: loading must not retain writable aliases to caller DTOs.
        var isolatedState=w.Snapshot();var isolated=new World(isolatedState);isolatedState.network.roads.Clear();Check(isolated.Network.RoadCount==3,"External DTO changed live road facts");
        bool oldRejected=false;try{SaveCodec.Read(System.Text.Encoding.UTF8.GetBytes("{\"format\":34}"));}catch(InvalidDataException){oldRejected=true;}Check(oldRejected,"Old JSON accepted by codec");
        byte[] corrupted=SaveCodec.Write(w);corrupted[50]^=1;bool corruptRejected=false;try{SaveCodec.Read(corrupted);}catch(InvalidDataException){corruptRejected=true;}Check(corruptRejected,"Corrupted state accepted");
        var grid=new World();var nodes=new int[4,4];for(int row=0;row<4;row++)for(int col=0;col<4;col++)nodes[row,col]=grid.AddJunction(new Point(col*20,row*.5,row*20));
        for(int row=0;row<4;row++)for(int col=0;col<4;col++)foreach(var direction in new[]{new[]{1,0},new[]{0,1}}){int gridX=col+direction[0],gridY=row+direction[1];if(gridX<4&&gridY<4){int source=nodes[row,col],goal=nodes[gridY,gridX];grid.AddRoad(source,goal,new[]{grid.JunctionPosition(source),grid.JunctionPosition(goal)});}}
        for(int row=0;row<4;row++)for(int col=0;col<4;col++)for(int r=0;r<4;r++)for(int gridCol=0;gridCol<4;gridCol++){var route=grid.Network.Route(nodes[row,col],nodes[r,gridCol]);Check(route!=null,"Heap lost reachable direction");if(route.Count>0)grid.Network.ValidateRoute(route);}
        var dense=new World();int denseA=dense.AddJunction(new Point(0,0,0)),denseB=dense.AddJunction(new Point(100,0,0));var samples=new Point[1001];for(int i=0;i<samples.Length;i++)samples[i]=new Point(i*.1,0,0);dense.AddRoad(denseA,denseB,samples);Check(dense.Network.Route(denseA,denseB).Count==1&&dense.Network.JunctionCount==2,"Sampling density expanded topology/route");
        // Split after halfway, both directions, on curved/elevated geometry, and on a connection.
        foreach(bool backwards in new[]{false,true})foreach(double delay in new[]{.5,1.9,2.8})
        {
            var edit=new World();int e1=edit.AddJunction(new Point(0,0,0)),e2=edit.AddJunction(new Point(30,1,0)),e3=edit.AddJunction(new Point(40,1,10));edit.AddRoad(e1,e2,new[]{edit.JunctionPosition(e1),new Point(15,.5,2),edit.JunctionPosition(e2)});edit.AddRoad(e2,e3,new[]{edit.JunctionPosition(e2),edit.JunctionPosition(e3)});int first=edit.AddDepot(e1),last=edit.AddDepot(e3);var truck=edit.Dispatch(backwards?last:first,backwards?first:last);edit.Advance(delay);Point pose=edit.Position(truck);double length=edit.Network.Get(Network.LaneId(1,backwards)).Geometry.Length;edit.SplitRoad(1,1);Check(Point.Distance(pose,edit.Position(truck))<1e-7,"Edit changed curved/connection position");double combined=edit.Network.Get(Network.LaneId(1,backwards)).Geometry.Length+edit.Network.Get(Network.LaneId(3,backwards)).Geometry.Length;Check(Math.Abs(length-combined)<1e-8,"Split lane changed metric");var continued=SaveCodec.Read(SaveCodec.Write(edit));for(int i=0;i<100;i++){edit.Step();continued.Step();edit.AssertSafety();}Check(Convert.ToBase64String(SaveCodec.Write(edit))==Convert.ToBase64String(SaveCodec.Write(continued)),"Edited saved state diverged");Check(edit.Delivered==1,"Edit stalled delivery");
        }
        foreach(int roadId in new[]{1,2}){var removal=new World();int p=removal.AddJunction(new Point(0,0,0)),q=removal.AddJunction(new Point(30,0,0));removal.AddRoad(p,q,new[]{removal.JunctionPosition(p),removal.JunctionPosition(q)});int pa=removal.AddDepot(p),qa=removal.AddDepot(q);var actor=removal.Dispatch(pa,qa);removal.Advance(.5);if(roadId==1){Point pose=removal.Position(actor);removal.RemoveRoad(1);Check(Point.Distance(pose,removal.Position(actor))<1e-8&&actor.cargo==1,"Current road removal lost actor");}}
        // Sparse IDs and packed positive/reverse/link keys retain full equality with mixed hashing.
        foreach(int id in new[]{1,2,65535,25000000}){long forward=Network.LaneId(id,false),backward=Network.LaneId(id,true);Check(forward!=backward,"Direction identity collapsed");Check(Network.ConnectionId(forward,backward)!=Network.ConnectionId(backward,forward),"Connection direction collapsed");}
        Check(Network.LaneId(1,false,1)!=Network.LaneId(1,false,0),"Future lane identity collapsed");Reject(()=>Network.LaneId(Network.MaximumRoadId+1,false),"Out-of-range identity allowed");
        var past=new World();int pastA=past.AddJunction(new Point(0,0,0)),pastB=past.AddJunction(new Point(30,0,0)),pastC=past.AddJunction(new Point(60,0,0));past.AddRoad(pastA,pastB,new[]{past.JunctionPosition(pastA),past.JunctionPosition(pastB)},true);past.AddRoad(pastB,pastC,new[]{past.JunctionPosition(pastB),past.JunctionPosition(pastC)},true);var passed=past.Dispatch(past.AddDepot(pastA),past.AddDepot(pastC,0));past.Advance(2.7);Point passedPosition=past.Position(passed);past.RemoveRoad(1);Check(passed.status==VehicleStatus.Active&&Point.Distance(passedPosition,past.Position(passed))<1e-8,"Removing traversed road changed current task");var loadedPast=SaveCodec.Read(SaveCodec.Write(past));loadedPast.Advance(4);Check(loadedPast.Delivered==1,"Traversed road removal broke saved delivery");
        var construction=new World();int left=construction.AddJunction(new Point(0,0,0)),right=construction.AddJunction(new Point(30,0,0));construction.AddRoad(left,right,new[]{construction.JunctionPosition(left),new Point(15,0,0),construction.JunctionPosition(right)});int leftDepot=construction.AddDepot(left),rightDepot=construction.AddDepot(right);var constructionState=construction.Snapshot();constructionState.nextVehicle=3;constructionState.vehicles.Add(new Vehicle{id=1,origin=leftDepot,destination=rightDepot,route=new List<long>{Network.LaneId(1,false)},distance=14});constructionState.vehicles.Add(new Vehicle{id=2,origin=rightDepot,destination=leftDepot,route=new List<long>{Network.LaneId(1,true)},distance=14});construction=new World(constructionState);int splitNode=construction.SplitRoad(1,1);Check(!construction.Network.IsConflictJunction(splitNode),"Pure split introduced junction conflict");int north=construction.AddJunction(new Point(15,0,20));string beforeEdit=Convert.ToBase64String(SaveCodec.Write(construction));bool delayed=false;try{construction.AddRoad(splitNode,north,new[]{construction.JunctionPosition(splitNode),construction.JunctionPosition(north)});}catch(InvalidOperationException){delayed=true;}Check(delayed&&beforeEdit==Convert.ToBase64String(SaveCodec.Write(construction)),"Unsafe construction partially committed");construction.Advance(.5);construction.AddRoad(splitNode,north,new[]{construction.JunctionPosition(splitNode),construction.JunctionPosition(north)});construction.AssertSafety();Check(construction.ActiveCount==2,"Safe deferred construction lost vehicles");
        foreach(bool congested in new[]{false,true}){var fleet=Fixture(1000,congested);for(int i=0;i<300;i++){fleet.Step();fleet.AssertSafety();}Check(fleet.ActiveCount==1000,"Benchmark lost active vehicles");}
        // Competing approaches: whole-junction mutex must prevent simultaneous connection occupancy.
        var crossing=new World();int center=crossing.AddJunction(new Point(0,0,0)),west=crossing.AddJunction(new Point(-30,0,0)),south=crossing.AddJunction(new Point(0,0,-30)),east=crossing.AddJunction(new Point(30,0,0));
        crossing.AddRoad(west,center,new[]{crossing.JunctionPosition(west),crossing.JunctionPosition(center)},true);crossing.AddRoad(south,center,new[]{crossing.JunctionPosition(south),crossing.JunctionPosition(center)},true);crossing.AddRoad(center,east,new[]{crossing.JunctionPosition(center),crossing.JunctionPosition(east)},true);int westD=crossing.AddDepot(west),southD=crossing.AddDepot(south),eastD=crossing.AddDepot(east,0);crossing.Dispatch(westD,eastD);crossing.Dispatch(southD,eastD);for(int i=0;i<200;i++){crossing.Step();crossing.AssertSafety();}Check(crossing.Delivered==2,"Intersection deadlock");
        Console.WriteLine("PASS "+checks+" core checks; direction, topology, safety, split/reverse/removal, freight/cash, binary continuation. Lane change/pedestrian/full city not yet connected.");
    }
    static void Benchmark(string path)
    {
        var result=new List<string>{"scenario,active,steps,mean_ms,p95_ms,p99_ms,allocated_bytes_per_step,simulated_seconds,wall_seconds"};
        foreach(int count in new[]{1000,3000})foreach(bool congested in new[]{false,true})
        {
            var w=Fixture(count,congested);for(int i=0;i<50;i++)w.Step();w.AssertSafety();var times=new double[300];var sw=new Stopwatch();long gc=GC.GetAllocatedBytesForCurrentThread();var total=Stopwatch.StartNew();
            for(int i=0;i<times.Length;i++){sw.Restart();w.Step();sw.Stop();times[i]=sw.Elapsed.TotalMilliseconds;if(w.ActiveCount!=count)throw new Exception("Fixture ceased to have requested active population");}
            total.Stop();gc=GC.GetAllocatedBytesForCurrentThread()-gc;w.AssertSafety();double mean=0;foreach(double t in times)mean+=t;Array.Sort(times);
            string row=string.Join(",",congested?"queued-three-approach-merge":"spaced-twenty-corridors",count,300,(mean/300).ToString("F5",CultureInfo.InvariantCulture),times[284].ToString("F5",CultureInfo.InvariantCulture),times[296].ToString("F5",CultureInfo.InvariantCulture),(gc/300.0).ToString("F1",CultureInfo.InvariantCulture),15,total.Elapsed.TotalSeconds.ToString("F5",CultureInfo.InvariantCulture));result.Add(row);Console.WriteLine(row);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));File.WriteAllLines(path,result);
        var edits=new List<string>{"active,operation,rounds,mean_ms,p95_ms,p99_ms,allocated_bytes_per_commit"};
        foreach(int count in new[]{1000,3000})
        {
            byte[] seed=SaveCodec.Write(Fixture(count,false));var times=new double[12];long allocated=0;
            for(int round=0;round<times.Length;round++)
            {
                var world=SaveCodec.Read(seed);long before=GC.GetAllocatedBytesForCurrentThread();var timer=Stopwatch.StartNew();world.SplitRoad(1,1);timer.Stop();allocated+=GC.GetAllocatedBytesForCurrentThread()-before;times[round]=timer.Elapsed.TotalMilliseconds;world.AssertSafety();Check(world.ActiveCount==count,"Edit lost active vehicle");
            }
            double sum=0;foreach(double time in times)sum+=time;Array.Sort(times);edits.Add(string.Join(",",count,"split-shared-road-and-migrate",times.Length,(sum/times.Length).ToString("F5",CultureInfo.InvariantCulture),times[11].ToString("F5",CultureInfo.InvariantCulture),times[11].ToString("F5",CultureInfo.InvariantCulture),allocated/times.Length));
        }
        File.WriteAllLines(Path.ChangeExtension(path,"edits.csv"),edits);
    }
    public static void Main(string[] args){Tests();if(args.Length>0)Benchmark(args[0]);}
}
