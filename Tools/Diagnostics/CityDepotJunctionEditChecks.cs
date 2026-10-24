using System;
using System.IO;
using System.Linq;
using HarborCity;
using HarborCity.RoadCore;
static class CityDepotJunctionEditChecks
{
    static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
    static string Vehicles(World w)=>string.Join("\n",w.Vehicles.Select(v=>string.Join(",",v.id,v.Current,v.distance.ToString("R"),v.Task.id,v.changeTarget,v.changeElapsed.ToString("R"),string.Join(";",v.Plan.segments.Skip(v.cursor)),v.Plan.EndDistance.ToString("R"))));
    static void Main(string[] args)
    {
        var s=CityCoreSave.Read(File.ReadAllBytes(args[0]));s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);var w=s.Transport;w.DepotJunctionRoutingEnabled=true;
        Check(w.ActiveCount>0,"Requires in-flight vehicles");
        var depot=s.City.buildings.OfType<LandfillBuilding>().First(x=>w.GetAccess(x.id)?.valid==true);
        var target=s.City.buildings.Last(x=>!(x is LandfillBuilding)&&w.GetAccess(x.id)?.valid==true);
        Action lookup=()=>Check(w.JunctionRoutes.Outbound(w.Network,depot.id,w.GetAccess(depot.id),w.GetAccess(target.id),out var r,out _,out _)&&r!=null,"Edit cache query failed");
        lookup();long invalidations=w.JunctionRoutes.Invalidations;
        var v=w.Vehicles.First(x=>!w.Network.Get(x.Current).IsConnection);var road=w.Network.Settings(w.Network.Get(v.Current).RoadId);
        string before=Vehicles(w);byte[] initial=CityCoreSave.Write(s);bool refused=false;
        try{Check(w.PlanUpgradeRoad(road.id,road.lanes,road.speed+.5,road.direction).TryCommit(),"In-flight edit stale");}
        catch(InvalidOperationException ex) when(ex.Message.Contains("施工节点安全区")){refused=true;Check(initial.SequenceEqual(CityCoreSave.Write(s)),"Safety-rejected edit changed complete city");}
        Check(before==Vehicles(w),"Speed edit moved vehicles/tasks/remaining routes");lookup();
        if(!refused)Check(w.JunctionRoutes.Invalidations>invalidations,"In-flight edit kept stale graph");w.AssertSafety();s.Validate();
        byte[] saved=CityCoreSave.Write(s);var cold=CityCoreSave.Read(saved);cold.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);cold.Transport.DepotJunctionRoutingEnabled=true;
        for(int i=0;i<100;i++){Check(s.Simulation.Advance(.05,1,double.PositiveInfinity)==1&&cold.Simulation.Advance(.05,1,double.PositiveInfinity)==1,"Lost edit continuation step");w.AssertSafety();cold.Transport.AssertSafety();Check(s.Simulation.Waste.BalanceError==0&&cold.Simulation.Waste.BalanceError==0,"Edited waste imbalance");}
        s.Validate();cold.Validate();Check(CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(cold)),"Edited warm/cold state mismatch");
        Controlled();
        Console.WriteLine("PASS busy real city edit "+(refused?"safely refused, whole state unchanged":"accepted without moving vehicles")+"; 100 hot/cold city steps and waste balance; controlled in-flight add/split/speed edits also passed.");
    }
    static void Controlled()
    {
        var w=new World();int a=w.AddJunction(new Point(0,2,0)),b=w.AddJunction(new Point(100,2,0)),c=w.AddJunction(new Point(200,2,0));
        int r1=w.AddRoad(a,b,new[]{w.JunctionPosition(a),w.JunctionPosition(b)},lanes:2),r2=w.AddRoad(b,c,new[]{w.JunctionPosition(b),w.JunctionPosition(c)},lanes:2);
        int home=w.AddDepot(a),away=w.AddDepot(c);w.RegisterAccess(home,Network.LaneId(r1,false),10);w.RegisterAccess(away,Network.LaneId(r2,false),80);
        var vehicle=w.Dispatch(home,away);Check(vehicle!=null,"Controlled real vehicle not dispatched");w.Advance(3);Check(w.ActiveCount==1,"Controlled traffic not in flight");
        Action lookup=()=>Check(w.JunctionRoutes.Outbound(w.Network,home,w.GetAccess(home),w.GetAccess(away),out var route,out _,out _)&&route!=null,"Controlled lookup failed");
        lookup();long epoch=w.JunctionRoutes.Invalidations;string before=Vehicles(w);
        Check(w.PlanUpgradeRoad(r1,2,13,RoadDirection.Both).TryCommit(),"Safe controlled speed edit refused");lookup();Check(w.JunctionRoutes.Invalidations>epoch,"Controlled stale speed cache");Check(before==Vehicles(w),"Controlled edit moved vehicle");
        int d=w.AddJunction(new Point(0,2,100)),e=w.AddJunction(new Point(50,2,100));int extra=w.AddRoad(d,e,new[]{w.JunctionPosition(d),w.JunctionPosition(e)},lanes:2);lookup();w.SplitRoadAt(extra,25);lookup();Check(before==Vehicles(w),"Distant add/split moved vehicle");
        w.Validate();w.AssertSafety();var cold=SaveCodec.Read(SaveCodec.Write(w));
        for(int i=0;i<400;i++){w.Advance(.05);cold.Advance(.05);w.AssertSafety();cold.AssertSafety();}
        w.Validate();cold.Validate();Check(SaveCodec.Write(w).SequenceEqual(SaveCodec.Write(cold)),"Controlled edited save continuation differs");Check(w.Delivered==1,"Edited real vehicle did not arrive");
        var state=SaveCodec.Write(w);bool rejected=false;try{w.PlanUpgradeRoad(r1,0,13,RoadDirection.Both).TryCommit();}catch(ArgumentException){rejected=true;}Check(rejected&&state.SequenceEqual(SaveCodec.Write(w)),"Rejected invalid transaction changed world");
    }
}
