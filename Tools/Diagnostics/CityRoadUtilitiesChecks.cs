using System;
using System.IO;
using System.Linq;
using HarborCity;
using HarborCity.RoadCore;

static class CityRoadUtilitiesChecks
{
    static int count;static void Check(bool value,string why){count++;if(!value)throw new Exception(why);}
    static void CheckSupplyTotals(CityModel city)
    {
        Func<CityBuilding,int> load=building=>building.Use==LandUse.Residential?
            Math.Max(1,city.society.families.Where(h=>h.resident&&h.home==building.id).Sum(h=>city.LivingMembers(h))):
            building.Use==LandUse.Commercial||building.Use==LandUse.Industrial?Math.Max(1,city.EmployedAt(building.id)*2):building.Use==LandUse.Power?0:2;
        Check(city.PowerSupplied==city.buildings.Where(b=>city.Supply(b.id).electricity).Sum(load),"Displayed delivered power differs from actual served building loads");
        Check(city.WaterSupplied==city.buildings.Where(b=>CityZoningState.ZoneUse(b.Use)&&city.Supply(b.id).water).Sum(load),"Displayed delivered water differs from unique served building loads");
        Check(city.PowerSupplied<=city.demand&&city.WaterSupplied<=city.WaterDemand,"Delivered totals exceed actual demand");
    }
    static int Groups(CityRoadUtilityConduits graph)=>graph.network.edges.Select(e=>graph.components[e.a]).Distinct().Count();
    static void CheckNewTownTemplate()
    {
        var empty=CityCoreIndustrySession.CreateEmpty();var s=CityNewTownTemplate.Create();
        Check(s.City.map.Valid()&&s.City.map.buildHalfSize==672&&s.City.map.terrainHalfSize==744,"Player map was not expanded");
        var oldMap=CityMapState.DiagnosticFlat(168);oldMap.ExpandForPlay();
        Check(oldMap.Valid()&&oldMap.buildHalfSize==672,"Existing flat map did not expand");
        var larger=CityMapState.DiagnosticFlat(1024);larger.ExpandForPlay();Check(larger.buildHalfSize==1024,"Existing larger map shrank");
        var hills=new CityMapState();hills.ExpandForPlay();Check(hills.terrainVersion==2&&hills.buildHalfSize==168,"Hill save profile changed");
        var edgePlan=s.Transport.PlanConnectedRoad(new[]{new Point(620,2,620),new Point(650,2,620)},lanes:2);
        s.Construction.CheckRoadPlacement(edgePlan);Check(s.Construction.Commit(edgePlan,out _),"Road in newly available area failed");
        Check(empty.City.buildings.Count==0&&empty.Transport.Network.RoadCount==0,"Template changed the empty-session API or diagnostic fixtures");
        Check(s.City.buildings.Count==CityNewTownTemplate.TotalFacilities&&new[]{LandUse.Power,LandUse.Water,LandUse.Sewage}.All(use=>s.City.buildings.Count(b=>b.Use==use)==CityNewTownTemplate.FacilitiesPerService)&&s.City.buildings.Count(b=>b.Use==LandUse.Landfill)==CityNewTownTemplate.GarbageFacilities,"New-town service counts incorrect");
        Func<CityBuilding,int,bool> edge=(b,side)=>side==0?b.x<=-666:side==1?b.z<=-666:side==2?b.x>=666:b.z>=666;
        Check(s.City.buildings.All(b=>s.Simulation.Growth.Zoning.CanPlace(b,false)&&s.Transport.GetAccess(b.id).valid),"Invalid template terrain, clearance or access");
        for(int side=0;side<4;side++)
        {
            var strip=s.City.buildings.Where(b=>edge(b,side)).ToArray();
            Check(strip.Length==192,"Every perimeter side must contain 192 facilities");
            Check(new[]{LandUse.Power,LandUse.Water,LandUse.Sewage,LandUse.Landfill}.All(use=>strip.Count(b=>b.Use==use)==48),"Side lacks an original service type");
            Check(strip.Min(b=>side%2==0?b.z:b.x)==-640&&strip.Max(b=>side%2==0?b.z:b.x)>640,"Perimeter side does not span the map");
        }
        var central=s.City.buildings.Where(b=>Math.Abs(b.x)<660&&Math.Abs(b.z)<660).ToArray();
        Check(central.Length==CityNewTownTemplate.CentralGarbageFacilities&&central.All(b=>b.Use==LandUse.Landfill),"Central cross must contain garbage facilities only");
        Check(central.Count(b=>b.x>20)==48&&central.Count(b=>b.x< -20)==48&&central.Count(b=>b.z>20)==48&&central.Count(b=>b.z< -20)==48,"Central cross lacks a full arm or banks");
        Check(central.All(b=>Math.Abs(b.x)==7.5f||Math.Abs(b.z)==7.5f),"Central facilities left their road banks");
        Check(s.Transport.Network.RoadIds.All(id=>s.Transport.Network.Settings(id).lanes==2&&s.Transport.Network.Settings(id).direction==RoadDirection.Both),"Template roads must have two lanes in each direction");
        for(int i=0;i<CityNewTownTemplate.OutletsPerSide;i++)for(int side=0;side<4;side++)
        {
            float position=CityNewTownTemplate.OutletPosition(i);
            Check(s.City.buildings.Where(b=>edge(b,side)).All(b=>Math.Abs((side%2==0?b.z:b.x)-position)>=15),"Perimeter outlet facility gap obstructed");
            Check(central.All(b=>Math.Abs((Math.Abs(b.z)==7.5f?b.x:b.z)-position)>=15),"Cross connection corridor obstructed");
            var from=CityNewTownTemplate.Outlet(side,i);
            // Centre outlets already extend to the cross. Attach a real branch
            // there; other outlets must still extend into their clear corridors.
            var to=position==0?(side%2==0?new Point(from.x,2,20):new Point(20,2,from.z)):
                side==0?new Point(-612,2,position):side==1?new Point(position,2,-612):side==2?new Point(612,2,position):new Point(position,2,612);
            var outletCity=CityCoreSave.Read(CityCoreSave.Write(s));outletCity.Simulation.Growth.Zoning.SetSurface((x,z)=>2);
            var extension=outletCity.Transport.PlanConnectedRoad(new[]{from,to},lanes:2);outletCity.Construction.CheckRoadPlacement(extension);
            Check(outletCity.Construction.Commit(extension,out _),"Template outlet could not connect into city");outletCity.Validate();
        }
        for(int i=0;i<s.City.buildings.Count;i++)for(int j=i+1;j<s.City.buildings.Count;j++)Check(!s.City.buildings[i].Overlaps(s.City.buildings[j]),"Template facilities overlap");
        Check(s.City.buildings.All(b=>s.City.Supply(b.id).Complete)&&s.City.power==CityNewTownTemplate.FacilitiesPerService*160&&s.City.water==CityNewTownTemplate.FacilitiesPerService*160&&s.City.SewageCapacity==CityNewTownTemplate.FacilitiesPerService*160,"Template providers not fully functioning at original capacities");
        Check(s.Entities.Members.Count==0&&s.Transport.ActiveCount==0&&s.Transport.Clock==0,"Template advanced time or seeded residents/vehicles");
        Check(s.City.money==empty.City.money&&s.Simulation.Finance.UnlimitedFunds,"Template bypassed the current unlimited-funds policy");
        Check(s.City.utilities.electricity.edges.Count==0&&s.City.utilities.water.edges.Count==0,"Template added duplicate manual networks");CheckSupplyTotals(s.City);
        var bytes=CityCoreSave.Write(s);var restored=CityCoreSave.Read(bytes);restored.Simulation.Growth.Zoning.SetSurface((x,z)=>2);
        Check(bytes.SequenceEqual(CityCoreSave.Write(restored))&&Supplies(s)==Supplies(restored),"Template cold restore differs");
        restored.Simulation.Finance.UnlimitedFunds=s.Simulation.Finance.UnlimitedFunds; // Same runtime preference as the normal Load adapter.
        s.Advance(2);restored.Advance(2);Check(CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(restored)),"Template normal simulation diverges after restore");
        Action<Point[]> road=points=>{var plan=s.Transport.PlanConnectedRoad(points);s.Construction.CheckRoadPlacement(plan);Check(s.Construction.Commit(plan,out _),"Template connection road failed");};
        road(new[]{new Point(-120,2,80),new Point(-80,2,80)});
        int remote=s.Construction.PlaceFacility(LandUse.Water,-100,84.5f,0,s.Simulation.Growth.Zoning);s.Simulation.Utilities.Refresh();
        Check(!s.City.Supply(remote).electricity,"Disconnected remote facility received global power");
        road(new[]{CityNewTownTemplate.Outlet(0,3),new Point(-120,2,240),new Point(-120,2,80)});
        Check(s.City.Supply(remote).Complete,"Connecting one road did not bring actual services to remote facility");
        int link=s.Construction.PickRoad(new Point(-120,2,160));Check(link!=0,"Missing template connection road");s.Construction.RemoveRoad(link);
        Check(!s.City.Supply(remote).electricity,"Removing link left invisible template utility service");
        var before=CityCoreSave.Write(s);bool rejected=false;
        try{var blocked=s.Transport.PlanConnectedRoad(new[]{new Point(-666,2,-640),new Point(-666,2,-610)});s.Construction.CheckRoadPlacement(blocked);}catch(InvalidOperationException){rejected=true;}
        Check(rejected&&before.SequenceEqual(CityCoreSave.Write(s)),"Building-obstructed template road changed live city");s.Validate();
    }
    static string Supplies(CityCoreIndustrySession s)=>string.Join(";",s.Entities.Buildings.Select(b=>b.id+":"+s.City.Supply(b.id).electricity+":"+s.City.Supply(b.id).water+":"+s.City.Supply(b.id).sewage+":"+string.Join(",",s.City.Supply(b.id).pipes.Select(p=>p.id+"/"+p.fresh+"/"+p.drain+"/"+p.load))));
    static void Main(string[] args)
    {
        CheckNewTownTemplate();
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
        CheckSupplyTotals(s.City);
        Check(s.City.utilities.electricity.edges.Count==0&&s.City.utilities.water.edges.Count==0,"Automatic roads wrote duplicate manual networks");
        s.Simulation.Utilities.SetBudget(CityServiceKind.Water,50);Check(s.City.water==80&&s.City.SewageCapacity==80,"Road utilities bypassed service capacity/budget");CheckSupplyTotals(s.City);s.Simulation.Utilities.SetBudget(CityServiceKind.Water,100);
        var bytes=CityCoreSave.Write(s);Check(BitConverter.ToInt32(bytes,4)==17,"New saved rule requires explicit city format 17");var restored=CityCoreSave.Read(bytes);restored.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
        Check(restored.City.utilities.roadConduits&&Supplies(s)==Supplies(restored)&&bytes.SequenceEqual(CityCoreSave.Write(restored)),"Road conduit rule or supply topology lost on cold restore");
        CheckSupplyTotals(restored.City);
        var repaired=CityCoreSave.Read(bytes);repaired.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
        var roads=s.Transport.Network.RoadIds.Select(id=>new {settings=s.Transport.Network.Settings(id),points=s.Transport.Network.Centreline(id).Slice(0,s.Transport.Network.Centreline(id).Length)}).ToArray();
        foreach(var road in roads)repaired.Construction.RemoveRoad(road.settings.id);
        Check(repaired.Transport.Network.RoadCount==0&&repaired.City.power==0&&repaired.City.water==0&&repaired.City.SewageCapacity==0,"Demolishing roads did not immediately disconnect real supplies");
        CheckSupplyTotals(repaired.City);Check(repaired.City.PowerSupplied==0&&repaired.City.WaterSupplied==0,"Disconnected utilities retained stale delivered totals");
        foreach(var road in roads){var rebuild=repaired.Transport.PlanConnectedRoad(road.points,lanes:road.settings.lanes);Check(repaired.Construction.Commit(rebuild,out _),"Road repair failed");}
        repaired.Advance(.05);repaired.Validate();Check(repaired.City.power==160&&repaired.City.water==160&&repaired.City.SewageCapacity==160,"Rebound building accesses did not restore road utilities");
        for(int i=0;i<2500;i++){s.Advance(.05);restored.Advance(.05);}s.Validate();restored.Validate();
        CheckSupplyTotals(s.City);CheckSupplyTotals(restored.City);
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
