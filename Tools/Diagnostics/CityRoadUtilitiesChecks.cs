using System;
using System.IO;
using System.Linq;
using HarborCity;
using HarborCity.RoadCore;

static class CityRoadUtilitiesChecks
{
    static int count;static void Check(bool value,string why){count++;if(!value)throw new Exception(why);}
    static int Groups(CityRoadUtilityConduits graph)=>graph.network.edges.Select(e=>graph.components[e.a]).Distinct().Count();
    static string Supplies(CityCoreIndustrySession s)=>string.Join(";",s.Entities.Buildings.Select(b=>b.id+":"+s.City.Supply(b.id).electricity+":"+s.City.Supply(b.id).water+":"+s.City.Supply(b.id).sewage+":"+string.Join(",",s.City.Supply(b.id).pipes.Select(p=>p.id+"/"+p.fresh+"/"+p.drain+"/"+p.load))));
    static void Main(string[] args)
    {
        var w=new World();int a=w.AddJunction(new Point(-30,2,0)),b=w.AddJunction(new Point(30,2,0));w.AddRoad(a,b,new[]{w.JunctionPosition(a),w.JunctionPosition(b)},true);
        int c=w.AddJunction(new Point(0,8,-30)),d=w.AddJunction(new Point(0,8,30));w.AddRoad(c,d,new[]{w.JunctionPosition(c),w.JunctionPosition(d)});
        var empty=new CityUtilityNetwork();var graph=new CityRoadUtilityConduits(w.Network,empty);
        Check(Groups(graph)==2&&w.Network.Route(b,a)==null,"Conduits joined grade-separated crossing or changed one-way traffic");
        var wire=empty.Plan(new RoadNode{x=-10,z=-10},new RoadNode{x=-10,z=10},out _);var before=SaveCodec.Write(w);graph=new CityRoadUtilityConduits(w.Network,wire);
        Check(Groups(graph)==2&&graph.network.edges.Count==3&&before.SequenceEqual(SaveCodec.Write(w))&&wire.edges.Count==1,"Manual interior crossing not joined / phantom connector / source mutation");
        var parallel=empty.Plan(new RoadNode{x=-20,z=1},new RoadNode{x=-10,z=1},out _);Check(Groups(new CityRoadUtilityConduits(w.Network,parallel))==3,"Separate nearby pipe falsely connected");
        var connector=w.PlanConnectedRoad(new[]{new Point(30,2,0),new Point(30,8,30),new Point(0,8,30)},snap:.1);Check(connector.TryCommit()&&Groups(new CityRoadUtilityConduits(w.Network,empty))==1,"Connected-road conduit did not join");
        int joined=w.Network.RoadIds.Max();w.RemoveRoad(joined);Check(Groups(new CityRoadUtilityConduits(w.Network,empty))==2,"Road removal left invisible utility link");
        Check(w.PlanUpgradeRoad(1,2,12,RoadDirection.Backward).TryCommit()&&Groups(new CityRoadUtilityConduits(w.Network,empty))==2,"Widening/reversing cut conduits or joined disconnected roads");
        w.SplitRoadAt(1,20);Check(Groups(new CityRoadUtilityConduits(w.Network,empty))==2,"Road split broke conduit connectivity");

        var s=CityGrowingGridFixture.Create();s.City.utilities.electricity=new CityUtilityNetwork();s.City.utilities.water=new CityUtilityNetwork();s.City.utilities.roadConduits=true;s.Simulation.Utilities.Refresh();
        Check(s.City.buildings.All(building=>s.City.Supply(building.id).Complete),"Roadside power/water/sewage facilities did not automatically connect");
        Check(s.City.utilities.electricity.edges.Count==0&&s.City.utilities.water.edges.Count==0,"Automatic roads wrote duplicate manual networks");
        s.Simulation.Utilities.SetBudget(CityServiceKind.Water,50);Check(s.City.water==80&&s.City.SewageCapacity==80,"Road utilities bypassed service capacity/budget");s.Simulation.Utilities.SetBudget(CityServiceKind.Water,100);
        var bytes=CityCoreSave.Write(s);Check(BitConverter.ToInt32(bytes,4)==17,"New saved rule requires explicit city format 17");var restored=CityCoreSave.Read(bytes);restored.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
        Check(restored.City.utilities.roadConduits&&Supplies(s)==Supplies(restored)&&bytes.SequenceEqual(CityCoreSave.Write(restored)),"Road conduit rule or supply topology lost on cold restore");
        var repaired=CityCoreSave.Read(bytes);repaired.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
        var roads=s.Transport.Network.RoadIds.Select(id=>new {settings=s.Transport.Network.Settings(id),points=s.Transport.Network.Centreline(id).Slice(0,s.Transport.Network.Centreline(id).Length)}).ToArray();
        foreach(var road in roads)repaired.Construction.RemoveRoad(road.settings.id);
        Check(repaired.Transport.Network.RoadCount==0&&repaired.City.power==0&&repaired.City.water==0&&repaired.City.SewageCapacity==0,"Demolishing roads did not immediately disconnect real supplies");
        foreach(var road in roads){var rebuild=repaired.Transport.PlanConnectedRoad(road.points,lanes:road.settings.lanes);Check(repaired.Construction.Commit(rebuild,out _),"Road repair failed");}
        repaired.Advance(.05);repaired.Validate();Check(repaired.City.power==160&&repaired.City.water==160&&repaired.City.SewageCapacity==160,"Rebound building accesses did not restore road utilities");
        for(int i=0;i<2500;i++){s.Advance(.05);restored.Advance(.05);}s.Validate();restored.Validate();
        Check(s.Entities.Members.Count>0&&s.City.buildings.Any(building=>building.Use==LandUse.Residential),"Road-only serviced city failed to grow real residents: buildings="+s.City.buildings.Count+" projects="+s.City.development.projects.Count+" population="+s.Entities.Members.Count+" supplies="+Supplies(s));
        Check(CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(restored))&&Supplies(s)==Supplies(restored),"Road utility complete simulation/save continuation differs");
        int power=s.City.buildings.Single(building=>building.Use==LandUse.Power).id,water=s.City.buildings.Single(building=>building.Use==LandUse.Water).id,sewage=s.City.buildings.Single(building=>building.Use==LandUse.Sewage).id;
        s.Entities.SetAvailability(power,true,false);s.Simulation.Utilities.Refresh();Check(s.City.power==0&&s.City.water==0&&s.City.SewageCapacity==0,"Road conduits created power without a working plant");
        s.Entities.SetAvailability(power,false,false);s.Simulation.Utilities.Refresh();Check(s.City.power==160&&s.City.water==160,"Power plant recovery did not restore real supplies");
        s.Entities.SetAvailability(water,true,false);s.Simulation.Utilities.Refresh();Check(s.City.water==0,"Roads created water without a working source");s.Entities.SetAvailability(water,false,false);s.Simulation.Utilities.Refresh();
        s.Entities.SetAvailability(sewage,true,false);s.Simulation.Utilities.Refresh();Check(s.City.SewageCapacity==0&&s.City.buildings.OfType<ResidentialBuilding>().All(building=>!s.City.Supply(building.id).sewage),"Roads disposed sewage without a working outlet");s.Entities.SetAvailability(sewage,false,false);s.Simulation.Utilities.Refresh();
        before=CityCoreSave.Write(s);bool refused=false;try{s.Transport.PlanConnectedRoad(new[]{new Point(-130,2,0),new Point(-110,2,0)});}catch(InvalidOperationException){refused=true;}
        Check(refused&&before.SequenceEqual(CityCoreSave.Write(s)),"Failed road construction changed conduit rule/manual facts/city");
        s.City.utilities.roadConduits=false;s.Simulation.Utilities.Refresh();Check(s.City.water==0&&s.City.SewageCapacity==0,"Disabled road conduits still supplied empty pipe network");
        if(args.Length>0)
        {
            var old=File.ReadAllBytes(args[0]);Check(BitConverter.ToInt32(old,4)==16,"Expected existing original v16 city");var legacy=CityCoreSave.Read(old);legacy.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
            Check(!legacy.City.utilities.roadConduits,"Legacy city silently changed utility rules");var upgraded=CityCoreSave.Write(legacy);var repeat=CityCoreSave.Read(upgraded);repeat.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
            Check(upgraded.SequenceEqual(CityCoreSave.Write(repeat)),"Existing v16 city failed v17 save round trip");
            legacy.City.utilities.roadConduits=true;legacy.Simulation.Utilities.Refresh();legacy.Validate();upgraded=CityCoreSave.Write(legacy);repeat=CityCoreSave.Read(upgraded);repeat.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
            Check(upgraded.SequenceEqual(CityCoreSave.Write(repeat))&&Supplies(legacy)==Supplies(repeat),"Existing busy city road utility activation failed complete cold restore");
        }
        Console.WriteLine("PASS "+count+" real road utility topology, manual connections, capacities, growth, removal and save checks");
    }
}
