using System;
using System.Linq;
using System.Collections.Generic;
using HarborCity.RoadCore;
static class CityAccessRebindChecks
{
    static int checks;
    static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
    // Frozen old operation; Snapshot.network is a detached equivalent geometry
    // copy (the private CopyNetwork is unavailable to the test). Not a timing oracle.
    static bool Old(World w,IReadOnlyList<AccessBinding> bindings)
    {
        if(bindings==null||bindings.Count==0)return false;var replacements=new Dictionary<int,RoadAccess>();
        foreach(var b in bindings){var old=w.GetAccess(b.endpoint);var d=w.Network.Get(b.lane);if(old==null||old.valid)return false;if(d.IsConnection||b.distance<d.StartDistance||b.distance>d.EndDistance||double.IsNaN(b.distance)||double.IsInfinity(b.distance))throw new ArgumentException("Invalid replacement access");replacements.Add(b.endpoint,new RoadAccess{endpoint=b.endpoint,preferredIndex=d.LaneIndex,lane=b.lane,distance=b.distance,use=old.use});}
        var proposed=new List<RoadAccess>();foreach(var a in w.Snapshot().accesses)proposed.Add(replacements.TryGetValue(a.endpoint,out var next)?next:a.Copy());var n=w.Snapshot().network;return new RoadEditPlan(w,n,new Network(n,w.Network),accesses:proposed).TryCommit();
    }
    static World Fixture()
    {
        var w=new World();int a=w.AddJunction(new Point()),b=w.AddJunction(new Point(120,0,0)),c=w.AddJunction(new Point(120,0,120)),d=w.AddJunction(new Point(0,0,120));
        w.AddRoad(a,b,new[]{w.JunctionPosition(a),w.JunctionPosition(b)},lanes:2);w.AddRoad(b,c,new[]{w.JunctionPosition(b),w.JunctionPosition(c)},lanes:2);w.AddRoad(c,d,new[]{w.JunctionPosition(c),w.JunctionPosition(d)},lanes:2);w.AddRoad(d,a,new[]{w.JunctionPosition(d),w.JunctionPosition(a)},lanes:2);
        int from=w.AddDepot(a),to=w.AddDepot(c,0),station=w.AddDepot(b),other=w.AddDepot(d);
        w.RegisterAccess(from,Network.LaneId(1,false),20);w.RegisterAccess(to,Network.LaneId(2,false),40);w.RegisterAccess(station,Network.LaneId(3,false),50,AccessUse.Freight);w.RegisterAccess(other,Network.LaneId(4,false),50,AccessUse.Resident);
        Check(w.Dispatch(from,to)!=null,"Actual traffic fixture");w.Advance(.5);var data=w.Snapshot();foreach(var access in data.accesses)if(access.endpoint>=3){access.valid=false;access.lane=0;access.distance=0;}return new World(data);
    }
    static void Warm(World w,bool junction)
    {
        w.FreightTravelSeconds(1,2);w.DepotLaneRoutingEnabled=!junction;w.DepotJunctionRoutingEnabled=junction;
        w.JunctionRoutes.SetDepots(new[]{1,3},w.GetAccess,()=>w.AccessRevision);
        Check((w.ReplayDepotQuery(1,w.GetAccess(1),w.GetAccess(2),false).route!=null)==(w.GetAccess(1).valid&&w.GetAccess(2).valid),"Warm valid route / invalid-access failure");
    }
    static string Attempt(Func<bool> f){try{return f().ToString();}catch(Exception e){return e.GetType().FullName;}}
    static void Pair(World initial,IReadOnlyList<AccessBinding> bindings,bool success,bool junction,string label)
    {
        var bytes=SaveCodec.Write(initial);var old=SaveCodec.Read(bytes);var next=SaveCodec.Read(bytes);Warm(old,junction);Warm(next,junction);var network=next.Network;
        var before=SaveCodec.Write(next);long epoch=next.EditEpoch,access=next.AccessRevision;int generation=next.NetworkGeneration;int cached=next.CachedRoutes;
        string a=Attempt(()=>Old(old,bindings)),b=Attempt(()=>next.RebindAccesses(bindings));Check(a==b,label+" outcome/exception differs");Check(ReferenceEquals(next.Network,network),label+" unnecessarily replaced Network");Check(SaveCodec.Write(old).SequenceEqual(SaveCodec.Write(next)),label+" full state differs");
        if(success)
        {
            Check(b=="True"&&next.EditEpoch==epoch+1&&next.NetworkGeneration==generation+1&&next.AccessRevision==access,label+" original invalidation/version semantics changed");Check(next.CachedRoutes==0&&next.DepotRoutes.Count==0&&next.JunctionRoutes.Count==0,label+" old caches retained");
            foreach(int depot in new[]{1,3}){var source=next.GetAccess(depot);if(!source.valid)continue;var actual=next.ReplayDepotQuery(depot,source,next.GetAccess(2),false);var expected=old.ReplayDepotQuery(depot,old.GetAccess(depot),old.GetAccess(2),false);Check((actual.route==null)==(expected.route==null)&&(actual.route==null||actual.route.SequenceEqual(expected.route)&&actual.start==expected.start&&actual.end==expected.end),label+" depot/pin route differs after rebind");}
            var cold=SaveCodec.Read(SaveCodec.Write(next));cold.DepotLaneRoutingEnabled=!junction;cold.DepotJunctionRoutingEnabled=junction;
            for(int i=0;i<80;i++){old.Advance(.05);next.Advance(.05);cold.Advance(.05);old.AssertSafety();next.AssertSafety();Check(SaveCodec.Write(old).SequenceEqual(SaveCodec.Write(next))&&SaveCodec.Write(next).SequenceEqual(SaveCodec.Write(cold)),label+" cold/old continuation differs");}
        }
        else Check(before.SequenceEqual(SaveCodec.Write(next))&&epoch==next.EditEpoch&&generation==next.NetworkGeneration&&cached==next.CachedRoutes,label+" rejected/no-op call partially committed");
        Console.WriteLine("PASS "+label);
    }
    static void Main()
    {
        var w=Fixture();long lane=Network.LaneId(3,false);double end=w.Network.Get(lane).EndDistance;var valid=new[]{new AccessBinding(3,lane,end),new AccessBinding(4,Network.LaneId(4,true,1),20)};
        foreach(bool junction in new[]{false,true})Pair(w,valid,true,junction,junction?"junction trees and terminal pin":"lane trees and permissions");
        Pair(w,null,false,false,"null");Pair(w,new AccessBinding[0],false,false,"empty");Pair(w,new[]{new AccessBinding(1,lane,20)},false,false,"already valid");Pair(w,new[]{new AccessBinding(99,lane,20)},false,false,"missing endpoint");
        Pair(w,new[]{new AccessBinding(3,lane,double.NaN)},false,false,"invalid position");Pair(w,new[]{new AccessBinding(3,lane,end+1)},false,false,"out of bounds");Pair(w,new[]{valid[0],valid[0]},false,false,"duplicate");Pair(w,new[]{new AccessBinding(3,Network.ConnectionId(Network.LaneId(1,false),Network.LaneId(2,false)),0)},false,false,"connection as access");
        var changing=Fixture();var car=changing.Vehicles.First();Check(changing.TryChangeLane(car.id,1),"Fixture lane change");changing.Advance(.1);Pair(changing,valid,true,false,"in-flight lane change");
        var atJunction=Fixture();for(int i=0;i<1000&&!atJunction.Network.Get(atJunction.Vehicles.First().Current).IsConnection;i++)atJunction.Advance(.05);Check(atJunction.Network.Get(atJunction.Vehicles.First().Current).IsConnection,"Reach connection");Pair(atJunction,valid,true,false,"junction reservation");
        var stranded=Fixture();stranded.RemoveRoad(2);Pair(stranded,valid,true,false,"future route stranded");var displaced=Fixture();displaced.RemoveRoad(1);Pair(displaced,valid,true,false,"demolition displaced");
        var corrupt=Fixture();corrupt.Vehicles.First().Task.Shipment.remaining=-1;var raw=SaveCodec.Write(corrupt);var original=corrupt.Network;long epoch=corrupt.EditEpoch;bool refused=false;try{corrupt.RebindAccesses(valid);}catch(ArgumentException){refused=true;}Check(refused&&raw.SequenceEqual(SaveCodec.Write(corrupt))&&ReferenceEquals(original,corrupt.Network)&&corrupt.EditEpoch==epoch&&!corrupt.GetAccess(3).valid,"Full validator removed or failed candidate mutated live state");
        w=Fixture();var stale=w.PlanRoad(1,3,new[]{w.JunctionPosition(1),w.JunctionPosition(3)});Check(w.RebindAccesses(valid)&&!stale.TryCommit(),"Same Network identity allowed stale plan commit");
        var retained=w.Network;double length=retained.Centreline(3).Length;w.SplitRoadAt(3,60);Check(!ReferenceEquals(retained,w.Network)&&retained.Centreline(3).Length==length&&w.Network.Centreline(3).Length<length,"Later edit mutated the borrowed immutable Network");SaveCodec.Read(SaveCodec.Write(w)).AssertSafety();Check(w.GetAccess(3).valid,"Later split lost the rebound entrance");
        w=Fixture();var admission=w.PrepareTransport(1,2,1,0,TransportPurpose.Freight,0);Check(admission!=null,"Prepared admission fixture");w.RebindAccesses(valid);raw=SaveCodec.Write(w);refused=false;try{w.CommitAdmission(admission);}catch(InvalidOperationException){refused=true;}Check(refused&&raw.SequenceEqual(SaveCodec.Write(w)),"Same Network identity allowed stale transport admission");
        Console.WriteLine("PASS "+checks+" access rebind checks: frozen old/full-state, caches, epochs, safety, rollback and cold restore");
    }
}
