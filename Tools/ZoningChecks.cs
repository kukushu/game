using System;
using System.Linq;
using System.Collections.Generic;
using HarborCity;

public static class ZoningChecks
{
    static int checks;
    static float Flat(float x,float z)=>1;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Zoning: "+why);checks++;}
    static void Build(CityModel c,RoadNode a,RoadNode b)
    {var p=c.roads.Plan(c,a,b,Flat);Check(p.Valid && c.CommitRoad(p),"Road fixture: "+p.error);}
    public static void Run()
    {
        var c=TestCity.Empty();Check(c.ZoningCells(Flat).Count==0 && c.zoning.Valid(),"Empty city has no free-floating developable grid");
        Build(c,c.roads.Node(CityRoads.Entrance),TestCity.P(30,1.5f));
        var cells=c.ZoningCells(Flat);Check(cells.Count>100 && cells.Select(x=>x.row).Distinct().Count()==4 && cells.Select(x=>x.side).Distinct().Count()==2,"Basic road supplies four aligned rows on both sides");
        Check(cells.Select(x=>x.key).Distinct().Count()==cells.Count && cells.All(x=>ZoneCell.ValidKey(x.key)),"Cell identities are unique, finite world poses");
        int money=c.money,nextBuilding=c.nextBuildingId;
        var selected=cells.Where(x=>x.pose.x<-25 && x.side==1).ToList();
        Check(c.PaintZones(selected,LandUse.Residential)==selected.Count && c.money==money && c.nextBuildingId==nextBuilding && c.buildings.Count==0,"Planning neither charges construction fees nor creates building entities");
        Check(c.PaintZones(selected,LandUse.Residential)==0,"Holding brush over same cells is idempotent");
        Check(c.PaintZones(selected.Take(3),LandUse.Empty)==3 && selected.Take(3).All(x=>x.use==LandUse.Empty),"Dezoning clears planning only");
        var painted=c.zoning.designations.Select(d=>d.key).ToHashSet();var ids=c.zoning.designations.Select(d=>d.id).ToList();
        var copy=TestCity.RoundTrip(c);Check(copy.Valid() && copy.zoning.designations.Select(d=>d.id).SequenceEqual(ids) && copy.ZoningCells(Flat).Where(x=>x.use==LandUse.Residential).Select(x=>x.key).ToHashSet().SetEquals(painted),"Save round trip restores exact painted land and stable designation IDs");
        var before=cells.Where(x=>x.pose.x<-25).Select(x=>x.key).ToHashSet();
        Build(c,TestCity.P(0,1.5f),TestCity.P(0,25));
        var split=c.ZoningCells(Flat);Check(before.IsSubsetOf(split.Select(x=>x.key).ToHashSet()) && painted.IsSubsetOf(split.Where(x=>x.use==LandUse.Residential).Select(x=>x.key).ToHashSet()),"Splitting a straight road for an intersection preserves distant geometry and zoning");
        Check(split.All(x=>!c.roads.edges.Any(e=>x.pose.HitsRoad(c.roads.Node(e.a),c.roads.Node(e.b)))),"Junction mouths and road surfaces are excluded");
        bool overlaps=false;for(int i=0;i<split.Count;i++) for(int j=i+1;j<split.Count;j++) if(split[i].pose.Overlaps(split[j].pose)) overlaps=true;
        Check(!overlaps,"Competing street grids have no duplicated developable land");
        int home=TestCity.Build(c,-42.137f,7,LandUse.Residential);var occupied=c.ZoningCells(Flat).Where(x=>x.occupiedBuilding==home).ToList();
        Check(occupied.Count>0 && occupied.All(x=>!x.available) && c.GetBuilding(home)!=null,"Actual independent building footprint masks its zoning cells");
        c.PaintZones(occupied,LandUse.Industrial);Check(c.GetBuilding(home).Use==LandUse.Residential && c.GetBuilding(home).id==home,"Rezoning cannot silently replace or demolish an existing building");
        var edgeIds=c.roads.edges.Select(e=>e.id).ToList();Check(c.RemoveRoads(edgeIds,0) && c.ZoningCells(Flat).Count==0 && c.zoning.designations.Count>0 && c.GetBuilding(home)!=null,"Removing roads eliminates developable cells without erasing entities or archived planning intent");
        Check(TestCity.RoundTrip(c).Valid(),"Detached land intent remains valid and saveable");
        copy=TestCity.RoundTrip(c);copy.zoning.mapId="other-map";Check(!copy.Valid(),"Incompatible map identity rejected");
        copy=TestCity.RoundTrip(c);copy.zoning.designations.Add(copy.zoning.designations[0]);Check(!copy.Valid(),"Duplicated designation ID and geometry rejected");
        copy=TestCity.RoundTrip(c);copy.zoning.designations[0].key="NaN:1:0";Check(!copy.Valid(),"Nonfinite saved geometry rejected");
        c=TestCity.Empty();Build(c,c.roads.Node(CityRoads.Entrance),TestCity.P(-15,25));
        Check(c.ZoningCells(Flat).Count>0 && c.ZoningCells(Flat).Any(x=>Math.Abs(x.pose.yaw%90)>1),"Diagonal zoning follows local road direction");
        Check(c.ZoningCells((x,z)=>0).Count==0,"Underwater planning land is unavailable");
        c=TestCity.Empty();var curve=c.roads.PlanCurve(c,c.roads.Node(CityRoads.Entrance),TestCity.P(-32.5f,21.5f),TestCity.P(-12.5f,1.5f),Flat);
        Check(curve.Valid && c.CommitRoad(curve) && c.ZoningCells(Flat).Count>0,"Sampled curved road produces usable aligned planning land");
        Console.WriteLine("PASS: "+checks+" road zoning/occupancy/save checks");
    }
}
