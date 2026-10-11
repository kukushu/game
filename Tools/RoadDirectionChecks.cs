using System;
using System.Linq;
using System.Collections.Generic;
using HarborCity;
public static class RoadDirectionChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Road direction: "+why);checks++;}
    static void Build(CityModel c,RoadNode a,RoadNode b,bool oneWay=true)
    {var p=c.roads.Plan(c,a,b,TestCity.Flat,oneWay);if(!p.Valid || !c.CommitRoad(p))throw new Exception(p.error);}
    static int Node(CityModel c,RoadNode p)=>c.roads.nodes.First(n=>CityRoads.Length(n,p)<.01f).id;
    public static CityModel Loop()
    {
        var c=TestCity.Empty();var a=TestCity.P(-45,1.5f);var b=TestCity.P(30,1.5f);var d=TestCity.P(30,25);var e=TestCity.P(-45,25);
        Build(c,c.roads.Node(CityRoads.Entrance),a,false);Build(c,a,b);Build(c,b,d);Build(c,d,e);Build(c,e,a);return c;
    }
    public static void Run()
    {
        var c=TestCity.Empty();Build(c,c.roads.Node(CityRoads.Entrance),TestCity.P(30,1.5f));int end=Node(c,TestCity.P(30,1.5f));
        var start=new List<int>{CityRoads.Entrance};var finish=new List<int>{end};
        Check(c.roads.FindPath(start,finish).Count>0 && c.roads.FindPath(finish,start).Count==0,"Vehicle paths respect construction direction");
        Check(c.roads.FindPath(finish,start,true).Count>0 && c.roads.Connected(end),"Walkers remain bidirectional and physical roadside access is retained");
        var copy=TestCity.RoundTrip(c);Check(copy.Valid() && copy.roads.edges.All(e=>e.oneWay) && copy.roads.FindPath(finish,start).Count==0,"Direction and stable graph survive save");
        int id=c.roads.edges[0].id;Check(c.ChangeRoadDirection(id,true,true,out _) && c.roads.FindPath(start,finish).Count==0 && c.roads.FindPath(finish,start).Count>0,"Reverse changes actual vehicle reachability");
        Check(c.ChangeRoadDirection(id,false,false,out _) && c.roads.FindPath(start,finish).Count>0 && c.roads.FindPath(finish,start).Count>0,"Conversion restores both directions");
        Check(!c.ChangeRoadDirection(id,false,false,out _) && !c.ChangeRoadDirection(id,true,true,out _) && c.Valid(),"No-op and reverse of two-way are rejected");
        c=TestCity.Empty();Build(c,c.roads.Node(CityRoads.Entrance),TestCity.P(30,1.5f));var old=c.roads.edges[8];var mid=CityRoads.Lerp(c.roads.Node(old.a),c.roads.Node(old.b),.5f);
        Build(c,mid,TestCity.P(mid.x,-20),false);end=Node(c,TestCity.P(30,1.5f));finish=new List<int>{end};
        Check(c.roads.edges.Where(e=>e.stroke==1).All(e=>e.oneWay) && c.roads.FindPath(finish,start).Count==0 && c.Valid(),"Intersection splitting preserves original one-way orientation");
        c=TestCity.Empty();var curve=c.roads.PlanCurve(c,c.roads.Node(CityRoads.Entrance),TestCity.P(-30,1.5f),TestCity.P(-10,22),TestCity.Flat,true);
        Check(curve.Valid && c.CommitRoad(curve) && c.roads.edges.All(e=>e.oneWay) && c.Valid(),"Curved spans share actual one-way rules");
        c=Loop();int origin=TestCity.Build(c,-35,7,LandUse.Industrial),destination=TestCity.Build(c,20,7,LandUse.Commercial);
        c.Inventory(origin).Stock=8;var sim=new CityTraffic(c);var t=sim.Dispatch(origin,destination,TripPurpose.Delivery);
        Check(t!=null && sim.FindRoute(destination,origin).Count>t.route.Count,"Return freight takes the loop instead of driving against traffic");
        sim.Advance(.05f);var position=CityRoads.Lerp(c.roads.Node(t.Current),c.roads.Node(t.Next),t.progress);id=c.roads.edges.First(e=>e.a==t.Current && e.b==t.Next).id;
        Check(!c.ChangeRoadDirection(id,true,true,out _) && CityRoads.Length(position,CityRoads.Lerp(c.roads.Node(t.Current),c.roads.Node(t.Next),t.progress))<.001f,"Occupied road edit cannot teleport or erase a real vehicle");
        c=TestCity.RoundTrip(c);sim=new CityTraffic(c);int trip=t.id;
        for(int n=0;n<1000 && c.Goods(destination)<8;n++)sim.Advance(.05f);
        Check(c.Goods(destination)>=8 && c.traffic.trips.Any(x=>x.id==trip) && c.Valid(),"Saved real cargo actually arrives along directed roads");
        for(int n=0;n<3000 && c.traffic.trips.Any(x=>x.id==trip);n++)sim.Advance(.05f);
        Check(c.traffic.trips.All(x=>x.id!=trip) && c.traffic.completed>0 && c.Valid(),"Real freight completes directed return trip");
        c=Loop();var zones=c.ZoningCells(TestCity.Flat);Check(zones.Any(z=>z.available),"One-way roads still produce zoning land");
        var dto=c.ToSaveData();dto.format=24;var migrated=dto.ToCity();Check(migrated.Valid() && migrated.money==c.money,"Previous save envelope preserves entity state");
        c=TestCity.Create();c.development.enabled=true;UtilitySupplyChecks.ConnectFixture(c);c.waste.enabled=false;c.fire.chancePerBuildingDay=0;c.society.settings.applicantsPerDay=0;c.money+=10000;
        int school=TestCity.Build(c,40,-5,LandUse.ElementarySchool);var h=c.society.families.First();var child=h.people.First(p=>p.age==10);
        id=c.roads.edges.First(e=>e.stroke==1).id;Check(c.ChangeRoadDirection(id,true,false,out _) && c.EnrollSchool(child.id,school),"School enrollment uses walking reachability on one-way streets");
        Check(c.TravelMinutes(school,h.home)<0 && c.TravelMinutes(school,h.home,true)>0,"Walking and driving travel caches remain distinct");
        c.society.dayElapsed=480f/1440*c.society.settings.secondsPerDay;sim=new CityTraffic(c);
        for(int n=0;n<2000 && !child.atSchool;n++)sim.Advance(.05f);
        Check(child.atSchool && c.Valid(),"A specific pupil actually walks to school on the directed road");
        c.society.dayElapsed=960f/1440*c.society.settings.secondsPerDay;sim.Advance(.1f);
        Check(child.tripId>0 && c.traffic.trips.First(t2=>t2.id==child.tripId).walking,"School return starts against the vehicle direction as a real walking trip");
        int citizen=child.id;c=TestCity.RoundTrip(c);child=c.Citizens.First(p=>p.id==citizen);sim=new CityTraffic(c);
        for(int n=0;n<2000 && child.tripId>0;n++)sim.Advance(.05f);
        Check(child.tripId==0 && child.location==h.home && c.Valid(),"Saved pupil actually returns home without violating vehicle restrictions");
        Console.WriteLine("PASS: "+checks+" directed roads/walking/curve/split/real freight/save checks");
    }
}
