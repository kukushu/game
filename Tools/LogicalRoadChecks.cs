using System;
using System.Collections.Generic;
using System.Linq;
using HarborCity;
public static class LogicalRoadChecks
{
    static int checks;
    static void Check(bool ok,string why){checks++;if(!ok)throw new Exception("Logical roads: "+why);}
    static CityRoads Graph(float[][] points)
    {
        var r=new CityRoads{nextStroke=10};r.nodes.Add(new RoadNode{id=CityRoads.Entrance,x=-80,z=-80,y=1});
        foreach(var p in points)r.nodes.Add(new RoadNode{id=r.nextNode++,x=p[0],y=p[1],z=p[2]});return r;
    }
    static void Edge(CityRoads r,int a,int b,RoadKind kind=RoadKind.Standard,bool oneWay=false,RoadStructure structure=RoadStructure.Ground,int stroke=1)
    {r.edges.Add(new RoadEdge{id=r.nextEdge++,a=r.nodes[a].id,b=r.nodes[b].id,kind=kind,oneWay=oneWay,structure=structure,stroke=stroke});r.Changed();}
    static void Verify(CityRoads r,bool reachability=true)
    {
        Check(r.Valid(),"Fixture valid");var t=r.GetLogicalTopology();Check(t.IsCurrent(r) && ReferenceEquals(t,r.GetLogicalTopology()),"Hot snapshot reused");
        Check(t.LegacyEdgeCount==r.edges.Count && t.Roads.Sum(x=>x.Geometry.Spans.Count)==r.edges.Count,"Each legacy edge represented exactly once");
        foreach(var e in r.edges)
        {
            Check(t.TryGetEdge(e.id,out var road,out int index) && road.Geometry.Spans[index].EdgeId==e.id,"Edge ID mapping");
            Check(road.ForwardAllowed || road.ReverseAllowed,"Every road has a legal direction");
            foreach(bool reverse in new[]{false,true})
            {
                int a=reverse?e.b:e.a,b=reverse?e.a:e.b;
                bool allowed=!reverse || !e.oneWay;
                Check(t.CanTravel(a,b)==allowed && r.CanTravel(a,b)==allowed && t.CanTravel(a,b,true)==CityRoadTypes.Frontage(e.kind) && r.CanTravel(a,b,true)==CityRoadTypes.Frontage(e.kind),"Independent driving/walking edge oracle");
                Check(t.TryGetTraversal(a,b,out var traversal) && traversal.From.NodeId==a && traversal.To.NodeId==b && traversal.Span.EdgeId==e.id && traversal.VehicleAllowed==allowed,"Directed shared geometry");
                Check(r.Kind(a,b)==e.kind && r.EdgeLength(a,b)==Math.Max(.1f,CityRoads.SpatialLength(r.Node(a),r.Node(b))) && r.SpeedFactor(a,b)==CityRoadTypes.SpeedFactor(e.kind) && r.EdgeWidth(a,b)==CityRoadTypes.Width(e.kind),"Independent metadata/length oracle");
                Check(r.LaneCount(a,b)==(allowed?CityRoadTypes.Lanes(e.kind,e.oneWay):0) && Math.Abs(r.LaneOffset(a,b,1)-CityRoadTypes.LaneOffset(e.kind,e.oneWay,1))<.000001f,"Independent lane oracle "+a+"->"+b+" kind="+e.kind+" oneWay="+e.oneWay+" allowed="+allowed+" count="+r.LaneCount(a,b)+" offset="+r.LaneOffset(a,b,1)+" road="+traversal.OneWay);
                foreach(float f in new[]{0f,.17f,.5f,.999f,1f})
                {
                    Check(t.TryMapLegacy(a,b,f,out var p) && t.TryToLegacy(p,out int from,out int to,out float progress) && from==a && to==b && Math.Abs(progress-f)<.000001f,"Position roundtrip both directions");
                    var sample=road.Geometry.SampleAt(p.Distance);var expected=CityRoads.Lerp(r.Node(a),r.Node(b),f);
                    Check(Math.Abs(sample.X-expected.x)<.00002f && Math.Abs(sample.Y-expected.y)<.00002f && Math.Abs(sample.Z-expected.z)<.00002f,"3D centreline location matches original span");
                }
            }
        }
        foreach(var road in t.Roads)
        {
            double length=road.Geometry.Spans.Sum(s=>(double)CityRoads.SpatialLength(r.Node(s.From),r.Node(s.To)));Check(Math.Abs(length-road.Geometry.Length)<.000001,"3D cumulative length oracle");
            foreach(var p in road.Geometry.Samples)Check(p.X==r.Node(p.NodeId).x && p.Y==r.Node(p.NodeId).y && p.Z==r.Node(p.NodeId).z,"All original curve/height samples retained");
        }
        if(reachability)
        {
            var ends=t.Roads.SelectMany(x=>new[]{x.From,x.To}).Distinct().ToArray();
            foreach(bool walking in new[]{false,true})foreach(int from in ends)
            {
                var old=ReachLegacy(r,from,walking);var derived=ReachLogical(t,from,walking);
                foreach(int to in ends)Check(old.Contains(to)==derived.Contains(to),"Endpoint reachability oracle");
            }
        }
    }
    static HashSet<int> ReachLegacy(CityRoads r,int from,bool walking)
    {var seen=new HashSet<int>{from};var q=new Queue<int>();q.Enqueue(from);while(q.Count>0){int n=q.Dequeue();foreach(int to in r.Neighbors(n))if(r.CanTravel(n,to,walking) && seen.Add(to))q.Enqueue(to);}return seen;}
    static HashSet<int> ReachLogical(CityRoadTopology t,int from,bool walking)
    {var seen=new HashSet<int>{from};var q=new Queue<int>();q.Enqueue(from);while(q.Count>0){int n=q.Dequeue();foreach(var a in t.ArcsFrom(n))if((walking?CityRoadTypes.Frontage(a.Road.Kind):a.VehicleAllowed) && seen.Add(a.To))q.Enqueue(a.To);}return seen;}
    static string Signature(CityRoadTopology t)=>string.Join(";",t.Roads.Select(r=>r.Id+":"+r.From+":"+r.To+":"+r.ForwardAllowed+":"+r.ReverseAllowed+":"+string.Join(",",r.Geometry.Spans.Select(s=>s.EdgeId))));
    public static void Run()
    {
        var empty=TestCity.Empty().roads;Check(empty.GetLogicalTopology().Roads.Count==0 && (empty.GetLogicalTopology().BoundaryAt(CityRoads.Entrance)&LogicalRoadBoundary.Entrance)!=0,"Empty map external endpoint retained");
        var r=Graph(new[]{new[]{0f,1f,0f},new[]{3f,2f,1f},new[]{5f,5f,3f},new[]{7f,2f,5f},new[]{10f,1f,5f}});
        for(int i=1;i<5;i++)Edge(r,i,i+1,structure:i==2?RoadStructure.Elevated:i==3?RoadStructure.Tunnel:RoadStructure.Ground,stroke:i%2+1);
        Verify(r);var t=r.GetLogicalTopology();Check(t.Roads.Count==1 && t.Roads[0].Geometry.Spans.Select(s=>s.Structure).Distinct().Count()==3,"Curve/height/bridge/tunnel/stroke do not invent intersections");
        Check(t.BoundaryAt(r.nodes[2].id)==LogicalRoadBoundary.None && t.PositionsAtNode(r.nodes[2].id).Count==1,"Interior access node maps to road distance");
        string sig=Signature(t);var reordered=r.Copy();reordered.nodes.Reverse();reordered.edges.Reverse();reordered.Changed();Check(Signature(reordered.GetLogicalTopology())==sig,"Input order does not change partition/ID/orientation");
        var originalPoint=t.Roads[0].Geometry.Samples[2];r.nodes[3].y+=.2f;r.Changed();Check(!t.IsCurrent(r) && originalPoint.Y==t.Roads[0].Geometry.Samples[2].Y,"Old snapshot immutable after source edit");
        Check(!r.GetLogicalTopology().TryToLegacy(t.PositionAt(t.Roads[0],1,true),out _,out _,out _),"Foreign/obsolete road handles rejected by new snapshot");
        var copy=r.Copy();Check(!r.GetLogicalTopology().IsCurrent(copy) && !ReferenceEquals(r.GetLogicalTopology(),copy.GetLogicalTopology()),"Same revision different instance cannot share snapshot");
        r.edges[2].kind=RoadKind.FourLane;r.Changed();Verify(r);Check(r.GetLogicalTopology().Roads.Count==3,"Kind/lane/speed change splits corridor");
        r=Graph(new[]{new[]{0f,1f,0f},new[]{3f,1f,0f},new[]{6f,1f,0f},new[]{9f,1f,0f}});Edge(r,1,2,oneWay:true);Edge(r,3,2,oneWay:true);Edge(r,3,4);Verify(r);
        Check((r.GetLogicalTopology().BoundaryAt(r.nodes[2].id)&LogicalRoadBoundary.Direction)!=0,"Head-to-head one-way join remains boundary");
        r=Graph(new[]{new[]{0f,1f,0f},new[]{3f,1f,0f},new[]{6f,1f,0f},new[]{9f,1f,0f}});Edge(r,2,1,oneWay:true);Edge(r,3,2,oneWay:true);Edge(r,4,3,oneWay:true);Verify(r);
        Check(r.GetLogicalTopology().Roads.Count==1 && !r.GetLogicalTopology().Roads[0].ForwardAllowed && r.GetLogicalTopology().Roads[0].ReverseAllowed,"Reverse-oriented one-way corridor");
        r.nodes[2].control=JunctionControl.Uncontrolled;r.Changed();Check(r.GetLogicalTopology().Roads.Count==2,"Explicit control retained as boundary");
        r=Graph(new[]{new[]{-6f,1f,0f},new[]{0f,1f,0f},new[]{6f,1f,0f},new[]{0f,1f,-6f},new[]{0f,1f,6f},new[]{-6f,7f,0f},new[]{0f,7f,0f},new[]{6f,7f,0f}});
        Edge(r,1,2);Edge(r,2,3);Edge(r,4,2);Edge(r,2,5);Edge(r,6,7,structure:RoadStructure.Elevated);Edge(r,7,8,structure:RoadStructure.Elevated);Verify(r);
        Check(r.GetLogicalTopology().Roads.Count==5 && (r.GetLogicalTopology().BoundaryAt(r.nodes[2].id)&LogicalRoadBoundary.Branch)!=0 && r.GetLogicalTopology().BoundaryAt(r.nodes[7].id)==LogicalRoadBoundary.None,"Layered crossing only connects original shared IDs");
        foreach(bool oneWay in new[]{false,true})
        {
            r=Graph(Enumerable.Range(0,24).Select(i=>new[]{(float)Math.Cos(i*Math.PI/12)*10,1f,(float)Math.Sin(i*Math.PI/12)*10}).ToArray());
            for(int i=1;i<=24;i++)Edge(r,i,i%24+1,oneWay:oneWay);Verify(r);t=r.GetLogicalTopology();
            Check(t.Roads.Count==1 && t.Roads[0].From==t.Roads[0].To && (t.BoundaryAt(r.nodes[1].id)&LogicalRoadBoundary.CycleAnchor)!=0,"Closed loop is preserved with deterministic non-junction anchor");
            r.edges.Reverse();r.Changed();Check(Signature(r.GetLogicalTopology())==Signature(t),"Cycle deterministic");
        }
        var random=new Random(812);
        for(int k=0;k<25;k++)
        {
            r=Graph(Enumerable.Range(0,20).Select(i=>new[]{i%5*3f,1f+i%3,i/5*3f}).ToArray());var pairs=new HashSet<(int,int)>();
            for(int i=0;i<35;i++){int a=random.Next(1,21),b=random.Next(1,21);if(a==b || !pairs.Add((Math.Min(a,b),Math.Max(a,b))))continue;Edge(r,a,b,oneWay:random.Next(2)==0);}
            Verify(r);
        }
        ConstructionAndSave();LongChain();Console.WriteLine("PASS: "+checks+" logical-road mapping/geometry/topology/edit/save/oracle checks");
    }
    static void ConstructionAndSave()
    {
        var c=TestCity.Empty();var curve=c.roads.PlanCurve(c,c.roads.Node(CityRoads.Entrance),TestCity.P(-32.5f,21.5f),TestCity.P(-12.5f,1.5f),TestCity.Flat);
        Check(c.CommitRoad(curve),"Actual curve commit");Verify(c.roads);Check(c.roads.GetLogicalTopology().Roads.Count==1 && c.roads.GetLogicalTopology().Roads[0].Geometry.Length>CityRoads.SpatialLength(curve.start,curve.end),"Bezier isn't reduced to straight chord");
        string before=TestCity.Snapshot(c);c.roads.GetLogicalTopology();Check(before==TestCity.Snapshot(c),"Derived graph never enters save");
        var loaded=TestCity.RoundTrip(c);Check(Signature(c.roads.GetLogicalTopology())==Signature(loaded.roads.GetLogicalTopology()),"Native-ID logical partition survives JSON load");
        c=TestCity.Empty();Check(c.CommitRoad(c.roads.Plan(c,c.roads.Node(CityRoads.Entrance),TestCity.P(48,1.5f),TestCity.Flat)),"Split fixture road");
        int factory=TestCity.Build(c,-40,-5,LandUse.Industrial),shop=TestCity.Build(c,40,7,LandUse.Commercial);c.Inventory(factory).Stock=8;
        var sim=new CityTraffic(c);var trip=sim.Dispatch(factory,shop,TripPurpose.Delivery);
        Check(trip!=null,"Real cargo fixture");sim.Advance(.4f);before=TestCity.Snapshot(c);var oldPosition=CityVehicleGeometry.PositionValue(c.roads,trip,trip.segment,trip.progress);var old=c.roads.GetLogicalTopology();
        Check(old.TryMapLegacy(trip.Current,trip.Next,trip.progress,out var pos) && old.TryToLegacy(pos,out _,out _,out float progress) && Math.Abs(progress-trip.progress)<.000001f,"Active trip adapter");
        var afterPosition=CityVehicleGeometry.PositionValue(c.roads,trip,trip.segment,trip.progress);Check(oldPosition.x==afterPosition.x && oldPosition.y==afterPosition.y && oldPosition.z==afterPosition.z && before==TestCity.Snapshot(c),"Lane geometry/trip/waits/cargo untouched by queries");
        var edge=c.roads.edges.First(e=>e.a==trip.Current && e.b==trip.Next || e.b==trip.Current && e.a==trip.Next);var a=c.roads.Node(edge.a);var b=c.roads.Node(edge.b);var middle=CityRoads.Lerp(a,b,.5f);
        var physicalBefore=CityRoads.Lerp(c.roads.Node(trip.Current),c.roads.Node(trip.Next),trip.progress);
        var plan=c.roads.Plan(c,TestCity.P(middle.x,middle.z-12),TestCity.P(middle.x,middle.z+12),TestCity.Flat);
        Check(plan.Valid && c.CommitRoad(plan) && c.traffic.trips.Contains(trip) && trip.cargo==8 && !old.IsCurrent(c.roads),"Mid-road intersection uses existing split migration; loaded vehicle survives: "+plan.error);
        Verify(c.roads);Check(c.roads.GetLogicalTopology().TryMapLegacy(trip.Current,trip.Next,trip.progress,out _),"Split current vehicle maps into rebuilt logical road");
        var physicalAfter=CityRoads.Lerp(c.roads.Node(trip.Current),c.roads.Node(trip.Next),trip.progress);
        Check(CityRoads.SpatialLength(physicalBefore,physicalAfter)<.00002f,"Existing split preserves current centreline world position");
        var restored=TestCity.RoundTrip(c);Check(TestCity.Snapshot(c)==TestCity.Snapshot(restored),"Actual post-split save and loaded trip");
        var untouched=TestCity.RoundTrip(c);var oracle=new CityTraffic(untouched);var live=new CityTraffic(c);
        for(int i=0;i<40;i++){c.roads.GetLogicalTopology();live.Advance(.05f);oracle.Advance(.05f);Check(TestCity.Snapshot(c)==TestCity.Snapshot(untouched),"Post-split query/no-query simulation oracle");}
        old=c.roads.GetLogicalTopology();int branch=c.roads.nodes.First(n=>c.roads.Neighbors(n.id).Count>=3).id;
        Check(c.roads.SetJunctionControl(branch,JunctionControl.Signals,out _) && !old.IsCurrent(c.roads) && (c.roads.GetLogicalTopology().BoundaryAt(branch)&LogicalRoadBoundary.Control)!=0,"Control edit invalidates and preserves role");
        old=c.roads.GetLogicalTopology();Check(c.UpgradeRoad(c.roads.edges.Last().id,RoadKind.Gravel,false,out _,TestCity.Flat) && !old.IsCurrent(c.roads),"Actual upgrade invalidates representation");
        Check(c.UpgradeRoad(c.roads.edges.Last().id,RoadKind.Standard,false,out _,TestCity.Flat),"Restore branch for direction edit");
        old=c.roads.GetLogicalTopology();Check(c.ChangeRoadDirection(c.roads.edges.Last().id,true,false,out _),"Direction edit fixture");Check(!old.IsCurrent(c.roads),"Direction invalidates representation");
        old=c.roads.GetLogicalTopology();Check(c.RemoveRoads(new List<int>{c.roads.edges.Last().id}),"Demolition");Check(!old.IsCurrent(c.roads),"Demolition invalidates representation");Verify(c.roads);
    }
    static void LongChain()
    {
        var r=Graph(Enumerable.Range(0,12000).Select(i=>new[]{-149+i/120*3f,1f,-149+i%120*2.4f}).ToArray());
        // A serpentine large sparse-ID chain, not a capacity claim for vehicle simulation.
        for(int i=1;i<=12000;i++)r.nodes[i].id=1000000000+i;r.nextNode=1000012001;
        for(int row=0;row<100;row++)for(int col=0;col<120;col++)
        {int i=row*120+(row%2==0?col:119-col)+1;int j=col==119?(row+1)*120+(row%2==0?119:0)+1:row*120+(row%2==0?col+1:118-col)+1;if(row<99 || col<119)Edge(r,i,j);}
        Check(r.Valid(),"Large sparse chain valid");var t=r.GetLogicalTopology();Check(t.Roads.Count==1 && t.LegacyEdgeCount==11999 && t.Roads[0].Geometry.Samples.Count==12000,"Long chain iterative compression no recursion/ID truncation");
        var e=r.edges.Last();Check(t.TryMapLegacy(e.a,e.b,.17f,out var p) && t.TryToLegacy(p,out _,out _,out float f) && Math.Abs(f-.17f)<.000001f,"Double prefix retains late-chain progress");
    }
}
