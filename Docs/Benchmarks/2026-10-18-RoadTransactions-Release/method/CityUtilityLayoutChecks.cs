using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using System.Security.Cryptography;using HarborCity;using HarborCity.RoadCore;
static class CityUtilityLayoutChecks
{
    static int checkedBuildings;static readonly List<string> rows=new List<string>{"scenario,buildings,supply_sha256,layout_reused"};
    static readonly FieldInfo layout=typeof(CityModel).GetField("utilityLayout",BindingFlags.NonPublic|BindingFlags.Instance);
    static string Hash(byte[] bytes){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
    static byte[] Supply(CityModel city)
    {
        using(var stream=new MemoryStream())using(var w=new BinaryWriter(stream))
        {
            foreach(string field in new[]{"power","water","demand"})w.Write((int)typeof(CityModel).GetField(field,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).GetValue(city));w.Write(city.WaterDemand);w.Write(city.SewageCapacity);
            foreach(var b in city.buildings){var s=city.Supply(b.id);w.Write(b.id);w.Write(s.electricity);w.Write(s.water);w.Write(s.sewage);w.Write(s.pipes.Count);foreach(var p in s.pipes){w.Write(p.id);w.Write(p.load);w.Write(p.fresh);w.Write(p.drain);w.Write(p.inactiveDrains.Count);foreach(int id in p.inactiveDrains)w.Write(id);}}
            return stream.ToArray();
        }
    }
    static void Compare(CityCoreIndustrySession s,string name,bool? reuse=null)
    {
        var prior=layout.GetValue(s.City);s.City.RecalculateUtilities();bool reused=ReferenceEquals(prior,layout.GetValue(s.City));if(reuse.HasValue&&reuse.Value!=reused)throw new Exception(name+" layout lifetime mismatch");
        var actual=Supply(s.City);var saved=CityCoreSave.Write(s);s.City.FrozenRecalculateUtilities20261018();var expected=Supply(s.City);
        if(!actual.SequenceEqual(expected)||!saved.SequenceEqual(CityCoreSave.Write(s)))throw new Exception(name+" differs from original full utility algorithm");
        checkedBuildings+=s.City.buildings.Count;rows.Add(name+","+s.City.buildings.Count+","+Hash(actual)+","+reused);Console.WriteLine("PASS "+name);
    }
    static void Main(string[] args)
    {
        if(args.Length>2&&args[2]=="orphan")
        {
            var empty=CityCoreIndustrySession.CreateEmpty();var net=empty.City.utilities.electricity;net.nodes.Add(new RoadNode{id=1,x=0,z=0});net.nextNode=2;Compare(empty,"unused_node_initial");net.Node(1);net.nodes[0].id=2;net.nextNode=3;Compare(empty,"unused_node_reidentified",false);File.WriteAllLines(Path.Combine(args[1],"snapshots.csv"),rows);File.WriteAllText(Path.Combine(args[1],"Result.txt"),"PASS unused-node DTO replacement with stale lookup matches frozen cold coverage; complete save validation retained.\n");return;
        }
        var s=CityCoreSave.Read(File.ReadAllBytes(args[0]));s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);string output=args[1];Compare(s,"loaded");Compare(s,"unchanged",true);
        double h=s.City.map.buildHalfSize;var p=s.Transport.PlanConnectedRoad(new[]{new Point(-h+5,2,-h+8),new Point(-h+17,2,-h+8)},snap:1);s.Construction.CheckRoadPlacement(p);if(!s.Construction.Commit(p,out _))throw new Exception("Utility road fixture stale");Compare(s,"road_only",true);
        // Bound the command count, while every snapshot still compares supply for
        // every building. Cover each facility type at two different locations.
        foreach(var provider in s.City.buildings.Where(b=>b is PowerBuilding||b is WaterBuilding||b is SewageBuilding).GroupBy(b=>b.Use).SelectMany(g=>new[]{g.First(),g.Last()}).Distinct())
        {
            bool burning=provider.burning;provider.burning=!burning;Compare(s,"facility_burning_"+provider.id,true);provider.burning=burning;Compare(s,"facility_restored_"+provider.id,true);
            var access=s.Transport.GetAccess(provider.id);if(access!=null&&access.valid){long lane=access.lane;double distance=access.distance;access.valid=false;access.lane=0;access.distance=0;Compare(s,"facility_access_"+provider.id,true);access.valid=true;access.lane=lane;access.distance=distance;Compare(s,"facility_access_restored_"+provider.id,true);}
        }
        int budget=s.City.development.electricityBudget;s.City.development.electricityBudget=50;Compare(s,"budget",true);s.City.development.electricityBudget=budget;Compare(s,"budget_restored",true);
        var family=s.City.society.families.First(f=>f.resident&&f.people.Count<7);var child=new CityResident{id=s.City.society.nextCitizenId,name="Utility oracle child",age=10,canWork=false,location=family.home};s.Entities.AddResident(family.id,child);s.Simulation.Population.UpdateStatistics();Compare(s,"people_load",true);s.Entities.RemoveResident(child.id);s.Simulation.Population.UpdateStatistics();Compare(s,"people_load_restored",true);
        var job=s.City.society.jobEntities.First(j=>j.occupiedCitizenId>=0&&s.Entities.Member(j.occupiedCitizenId).Person.tripId==0&&!s.Entities.Member(j.occupiedCitizenId).Person.atWork);var worker=s.Entities.Member(job.occupiedCitizenId).Person;s.City.ReleaseJob(worker);Compare(s,"employment_load",true);s.City.AssignJob(worker,job.id);Compare(s,"employment_load_restored",true);
        var building=s.City.buildings[0];float x=building.x,w=building.width,d=building.depth;building.x+=.125f;Compare(s,"building_position",false);building.width+=.125f;Compare(s,"building_width",false);building.depth+=.125f;Compare(s,"building_depth",false);building.x=x;building.width=w;building.depth=d;Compare(s,"building_restored",false);
        foreach(var net in new[]{s.City.utilities.electricity,s.City.utilities.water})if(net.nodes.Count>0&&net.edges.Count>0)
        {
            var node=net.nodes[0];float nx=node.x;node.x+=.125f;Compare(s,"node_position",false);node.x=nx;Compare(s,"node_restored",false);
            var edge=net.edges[0];net.edges.RemoveAt(0);Compare(s,"edge_removed",false);net.edges.Insert(0,edge);Compare(s,"edge_restored",false);
        }
        s.City.utilities.water=s.City.utilities.water.Copy();Compare(s,"same_content_new_network",false);s.City.buildings.Reverse();Compare(s,"building_order",false);s.City.buildings.Reverse();Compare(s,"building_order_restored",false);
        var bytes=CityCoreSave.Write(s);var restored=CityCoreSave.Read(bytes);restored.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);Compare(restored,"cold_restore");if(!bytes.SequenceEqual(CityCoreSave.Write(restored)))throw new Exception("Layout cache leaked into save");
        for(int i=0;i<80;i++){s.Advance(.05);restored.Advance(.05);}Compare(s,"continued");Compare(restored,"cold_continued");if(!CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(restored))){
            var options=new System.Text.Json.JsonSerializerOptions{IncludeFields=true};
            File.WriteAllText(Path.Combine(output,"warm-business.json"),System.Text.Json.JsonSerializer.Serialize(s.SnapshotBusiness(),options));File.WriteAllText(Path.Combine(output,"cold-business.json"),System.Text.Json.JsonSerializer.Serialize(restored.SnapshotBusiness(),options));
            File.WriteAllText(Path.Combine(output,"warm-transport.json"),System.Text.Json.JsonSerializer.Serialize(s.Transport.Snapshot(),options));File.WriteAllText(Path.Combine(output,"cold-transport.json"),System.Text.Json.JsonSerializer.Serialize(restored.Transport.Snapshot(),options));
            File.WriteAllLines(Path.Combine(output,"snapshots-before-failure.csv"),rows);throw new Exception("Utility warm/cold continuation differs; complete JSON facts retained");}
        File.WriteAllLines(Path.Combine(output,"snapshots.csv"),rows);File.WriteAllText(Path.Combine(output,"Result.txt"),"PASS "+(rows.Count-1)+" frozen utility rebuild snapshots / "+checkedBuildings+" exact building supplies, all pipe component states and totals; dynamic loads/budget/provider access retained; geometry invalidation, save and 80-step cold continuation.\n");
    }
}
