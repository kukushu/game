using System;
using System.Linq;
using HarborCity;

public static class WasteChecks
{
    static int checks;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Waste: "+why);checks++;}
    static CityModel Fixture(out int depot,out int home)
    {
        var c=TestCity.Create();c.development.enabled=true;UtilitySupplyChecks.ConnectFixture(c);c.society.settings.applicantsPerDay=0;
        depot=TestCity.Build(c,-33,-7,LandUse.Landfill);home=c.buildings.OfType<ResidentialBuilding>().First().id;
        return c;
    }
    public static void Run()
    {
        var c=Fixture(out int depot,out int home);c.GetBuilding(home).garbage=16;c.waste.generated=16;
        var sim=new CityTraffic(c);var trip=sim.DispatchWaste(depot,home);
        Check(trip!=null && trip.cargo==0 && c.GetBuilding(home).garbage==16 && ((LandfillBuilding)c.GetBuilding(depot)).stored==0,"Departure reserves a physical truck but does not teleport waste");
        sim.Advance(8);
        Check(c.GetBuilding(home).garbage<16 && c.waste.collected>0,"Actual arrival removes building waste");
        Check(c.WasteBalanceError==0 && c.traffic.trips.Count(t=>t.cargoKind==CargoKind.Waste && t.home==depot)<=LandfillBuilding.Trucks,"Fleet is finite and material remains in buildings/vehicles/storage");
        sim.Advance(60);
        Check(((LandfillBuilding)c.GetBuilding(depot)).stored==16 && c.GetBuilding(home).garbage==0 && c.GarbageInTransit==0,"Waste enters landfill only after real return journeys");
        Check(c.Valid() && TestCity.RoundTrip(c).WasteBalanceError==0,"Truck and landfill subtype save reliably");
        Check(!c.DemolishBuilding(depot),"Nonempty landfill cannot be bulldozed");
        int receiver=TestCity.Build(c,34,-7,LandUse.Landfill);c.SetLandfillEmptying(depot,true);
        Check(sim.DispatchWaste(depot,home)==null,"Emptying landfill stops collection");
        trip=sim.DispatchWaste(depot,receiver,true);
        Check(trip!=null && trip.cargo>0 && ((LandfillBuilding)c.GetBuilding(depot)).stored<16 && ((LandfillBuilding)c.GetBuilding(receiver)).stored==0,"Transfer takes existing stored waste into an actual truck");
        var saved=TestCity.RoundTrip(c);Check(saved.WasteBalanceError==0 && saved.traffic.trips.Any(t=>t.cargoKind==CargoKind.Waste && t.cargo>0),"Loaded transfer saves without duplicating inventory");
        sim.Advance(40);
        Check(((LandfillBuilding)c.GetBuilding(depot)).stored==0 && ((LandfillBuilding)c.GetBuilding(receiver)).stored==16 && c.waste.transferred==16 && c.WasteBalanceError==0,"Emptying moves stock to another landfill through roads");
        Check(c.DemolishBuilding(depot),"Emptied depot can be demolished after trucks return");
        c=Fixture(out depot,out home);var landfill=(LandfillBuilding)c.GetBuilding(depot);landfill.stored=LandfillBuilding.Capacity-1;c.GetBuilding(home).garbage=3;c.waste.generated=landfill.stored+3;
        sim=new CityTraffic(c);sim.DispatchWaste(depot,home);sim.Advance(60);
        Check(landfill.stored==LandfillBuilding.Capacity && c.GetBuilding(home).garbage==2 && c.WasteBalanceError==0,"Full storage limits physical collection without destroying excess building waste");
        Check(sim.DispatchWaste(depot,home)==null,"Full landfill cannot send another pickup truck");
        c=Fixture(out depot,out home);c.Tick();Check(c.GarbageInBuildings==c.population && c.WasteBalanceError==0,"Resident garbage derives from actual residents, without imaginary commercial labour");
        var damaged=TestCity.RoundTrip(c);((LandfillBuilding)damaged.GetBuilding(depot)).stored=-1;Check(!damaged.Valid(),"Malformed negative facility inventory rejected");
        c=Fixture(out depot,out home);c.GetBuilding(home).garbage=8;c.waste.generated=8;sim=new CityTraffic(c);trip=sim.DispatchWaste(depot,home);
        for(int n=0;n<50 && trip.cargo==0;n++)sim.Advance(.2f);
        Check(trip.cargo>0,"Truck is physically loaded before closure scenario");
        c.waste.enabled=false; // Isolate this shipment from new generation/dispatch.
        c.RemoveRoads(c.roads.edges.Select(e=>e.id).ToList());sim.Advance(140);
        Check(c.traffic.trips.Contains(trip) && trip.cargo>0 && c.waste.lost==0 && TestCity.RoundTrip(c).WasteBalanceError==0,"Road interruption cannot despawn loaded garbage after the freight timeout");
        c.analysis=new CityDailyAnalysis(c);c.analysis.ObserveTraffic();var wasteAnalysis=c.analysis.Refresh(true);
        Check(wasteAnalysis.currentDay.longWaitingFreight==0 && wasteAnalysis.timeline.Any(e=>e.code=="garbage.blocked"),"Blocked garbage is reported as sanitation transport rather than industrial goods shortage");
        c.CommitRoad(c.roads.Plan(c,c.roads.Node(CityRoads.Entrance),TestCity.P(48,1.5f),TestCity.Flat));sim.Advance(60);
        Check(!c.traffic.trips.Contains(trip) && ((LandfillBuilding)c.GetBuilding(depot)).stored==8 && c.WasteBalanceError==0,"Restoring roads resumes the same held load to actual storage");
        c=Fixture(out depot,out home);var near=c.buildings.OfType<ResidentialBuilding>().Where(b=>b.x>-33).OrderBy(b=>b.x).First();var far=c.buildings.OfType<ResidentialBuilding>().OrderByDescending(b=>b.x).First();
        near.garbage=3;far.garbage=5;c.waste.generated=8;sim=new CityTraffic(c);trip=sim.DispatchWaste(depot,far.id);c.waste.enabled=false;
        for(int n=0;n<50 && trip.cargo==0;n++)sim.Advance(.1f);
        Check(trip.cargo==3 && near.garbage==0 && far.garbage==5,"Truck collects an intermediate building only after reaching its road access");
        sim.Advance(60);
        Check(far.garbage==0 && ((LandfillBuilding)c.GetBuilding(depot)).stored==8 && c.waste.collected==8,"One physical trip collects multiple buildings and returns when loaded");
        Check(!c.traffic.trips.Contains(trip) && c.WasteBalanceError==0 && TestCity.RoundTrip(c).Valid(),"Along-route collection and retargeting preserve material and save validity");
        c=Fixture(out depot,out home);c.GetBuilding(home).garbage=8;c.waste.generated=8;sim=new CityTraffic(c);trip=sim.DispatchWaste(depot,home);c.waste.enabled=false;
        for(int n=0;n<100 && trip.cargo==0;n++)sim.Advance(.1f);
        Check(trip.cargo==8 && c.DemolishBuilding(home) && c.traffic.trips.Contains(trip) && c.waste.lost==0,"Demolishing a collected building retains loaded waste on its real truck");
        sim.Advance(60);Check(((LandfillBuilding)c.GetBuilding(depot)).stored==8 && c.WasteBalanceError==0 && TestCity.RoundTrip(c).Valid(),"Retained garbage actually returns to its depot after source demolition");
        Console.WriteLine("PASS: "+checks+" real garbage/fleet/transfer/capacity/save checks");
    }
}
