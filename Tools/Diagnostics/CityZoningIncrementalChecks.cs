using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Security.Cryptography;using HarborCity;using HarborCity.RoadCore;

// Frozen 8273ffa algorithm and production zoning see the same single city/world.
// This is a test oracle, never compiled into a product Player.
static class CityZoningIncrementalChecks
{
    static int snapshots,cellsChecked;static readonly List<string> rows=new List<string>{"scenario,cells,state_sha256"};
    static string Hash(byte[] bytes){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
    static bool Bits(float a,float b)=>BitConverter.SingleToInt32Bits(a)==BitConverter.SingleToInt32Bits(b);
    static bool Same(ZoneCell a,ZoneCell b)=>a.key==b.key&&a.block==b.block&&a.column==b.column&&a.row==b.row&&a.side==b.side&&a.use==b.use&&a.available==b.available&&a.occupiedBuilding==b.occupiedBuilding&&Bits(a.pose.x,b.pose.x)&&Bits(a.pose.z,b.pose.z)&&Bits(a.pose.yaw,b.pose.yaw)&&Bits(a.pose.width,b.pose.width)&&Bits(a.pose.depth,b.pose.depth)&&Bits(a.pose.groundY,b.pose.groundY);
    static void Compare(CityCoreIndustrySession s,FrozenCoreZoning20261018 oracle,string label)
    {
        var before=CityCoreSave.Write(s);var actual=s.Simulation.Growth.Zoning.Cells();var expected=oracle.Cells();
        if(actual.Count!=expected.Count)throw new Exception(label+" cell count "+actual.Count+" != "+expected.Count);
        for(int i=0;i<actual.Count;i++)if(!Same(actual[i],expected[i]))throw new Exception(label+" ordered cell differs at "+i+" actual "+actual[i].key+" "+actual[i].block+" expected "+expected[i].key+" "+expected[i].block);
        if(!before.SequenceEqual(CityCoreSave.Write(s)))throw new Exception(label+" read-only zoning mutated facts");
        snapshots++;cellsChecked+=actual.Count;rows.Add(label+","+actual.Count+","+Hash(before));Console.WriteLine("PASS "+label+" "+actual.Count);
    }
    static int Add(CityCoreIndustrySession s,double x,double z,double X,double Z)
    {var points=new Point[9];for(int i=0;i<9;i++)points[i]=Point.Lerp(new Point(x,2,z),new Point(X,2,Z),i/8.0);var p=s.Transport.PlanConnectedRoad(points,false,snap:1,lanes:1);s.Construction.CheckRoadPlacement(p);if(!s.Construction.Commit(p,out _))throw new Exception("Fixture stale plan");return s.Transport.Network.RoadIds.Max();}
    static FrozenCoreZoning20261018 Bind(CityCoreIndustrySession s,Func<float,float,float> sampler)
    {s.Simulation.Growth.Zoning.SetSurface(sampler);var oracle=new FrozenCoreZoning20261018(s.City,s.Entities,s.Transport);oracle.SetSurface(sampler);return oracle;}
    static void Attempt(CityCoreIndustrySession s,Action command,string label)
    {var before=CityCoreSave.Write(s);try{command();}catch(InvalidOperationException){if(!before.SequenceEqual(CityCoreSave.Write(s)))throw new Exception(label+" failed edit changed facts");Console.WriteLine("REJECTED unchanged "+label);}}
    static void Busy(string save)
    {
        var s=CityCoreSave.Read(File.ReadAllBytes(save));var oracle=Bind(s,CityScenarioBuilder.Flat);double h=s.City.map.buildHalfSize;
        Compare(s,oracle,"busy_initial");var stable=s.Simulation.Growth.Zoning.Cells().Where(c=>c.pose.x>0).ToArray();
        int a=Add(s,-h+5,-h+8,-h+17,-h+8);Compare(s,oracle,"busy_add");
        int b=Add(s,-h+5,-h+32,-h+17,-h+32);Compare(s,oracle,"busy_second");
        int ordinary=Add(s,-h+5,-h+56,-h+17,-h+56);Compare(s,oracle,"busy_third");
        Add(s,-h+11,-h+8,-h+11,-h+32);Compare(s,oracle,"busy_connect_split");
        Attempt(s,()=>s.Construction.UpgradeRoad(ordinary,2,RoadDirection.Both),"busy_widen");Compare(s,oracle,"busy_widen");
        Attempt(s,()=>s.Construction.UpgradeRoad(ordinary,2,RoadDirection.Backward),"busy_backward");Compare(s,oracle,"busy_backward");
        Attempt(s,()=>s.Construction.UpgradeRoad(ordinary,2,RoadDirection.Forward),"busy_forward");Compare(s,oracle,"busy_forward");
        Attempt(s,()=>s.Construction.RemoveRoad(ordinary),"busy_remove");Compare(s,oracle,"busy_remove");
        for(int i=0;i<4;i++){int id=Add(s,-h+5,-h+80+i*24,-h+17,-h+80+i*24);Compare(s,oracle,"busy_continuous_"+i);Attempt(s,()=>s.Construction.RemoveRoad(id),"busy_remove_"+i);Compare(s,oracle,"busy_remove_"+i);}
        var before=CityCoreSave.Write(s);var uncommitted=s.Transport.PlanConnectedRoad(new[]{new Point(-h+5,2,-h+64),new Point(-h+17,2,-h+64)},false,snap:1);
        Compare(s,oracle,"busy_cancelled_preparation");if(!before.SequenceEqual(CityCoreSave.Write(s)))throw new Exception("Cancelled prepare changed save");
        var restored=CityCoreSave.Read(before);var restoredOracle=Bind(restored,CityScenarioBuilder.Flat);Compare(restored,restoredOracle,"busy_loaded");
        for(int i=0;i<160;i++){s.Advance(.05);restored.Advance(.05);}Compare(s,oracle,"busy_after160");Compare(restored,restoredOracle,"busy_loaded_after160");
        if(!CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(restored)))throw new Exception("Warm/cold in-flight continuation differs");s.Validate();restored.Validate();
    }
    static void Fixtures()
    {
        var s=CityCoreIndustrySession.CreateEmpty();var oracle=Bind(s,CityScenarioBuilder.Flat);Compare(s,oracle,"empty");
        int first=Add(s,-70,-64,70,-64);Compare(s,oracle,"cross_chunk");
        Add(s,-64,-100,-64,80);Compare(s,oracle,"split_boundary");
        Add(s,-35,-95,45,65);Compare(s,oracle,"angled_competition");
        Add(s,-80,32,80,32);Compare(s,oracle,"complex_crossing");
        s.Construction.UpgradeRoad(first,2,RoadDirection.Both);Compare(s,oracle,"fixture_widen");
        var cells=s.Simulation.Growth.Zoning.Cells();foreach(var use in new[]{LandUse.Residential,LandUse.Commercial,LandUse.Industrial}){s.Simulation.Growth.Zoning.Paint(cells.Where(c=>c.pose.x<(int)use*10).Take(24),use);Compare(s,oracle,"paint_"+use);}
        s.Construction.RemoveRoad(first);Compare(s,oracle,"fixture_remove");
        Func<float,float,float> slope=(x,z)=>2+(float)Math.Sin(x*.03)*.2f;oracle.SetSurface(slope);s.Simulation.Growth.Zoning.SetSurface(slope);Compare(s,oracle,"surface_invalidation");
        s.City.map=CityMapState.DiagnosticFlat(180);Compare(s,oracle,"extent_invalidation");
        bool fail=false;Func<float,float,float> flakey=(x,z)=>fail?throw new InvalidOperationException("intentional terrain failure"):2;
        oracle=Bind(s,flakey);Compare(s,oracle,"failure_surface_initial");Add(s,80,70,110,100);var failureBefore=CityCoreSave.Write(s);fail=true;bool threw=false;
        try{s.Simulation.Growth.Zoning.Cells();}catch(InvalidOperationException){threw=true;}if(!threw||!failureBefore.SequenceEqual(CityCoreSave.Write(s)))throw new Exception("Derived terrain failure changed facts");fail=false;Compare(s,oracle,"failure_recovery");
        var growing=CityGrowingGridFixture.Create();var growthOracle=Bind(growing,CityScenarioBuilder.Flat);Compare(growing,growthOracle,"growth_initial");
        for(int i=0;i<4800;i++){growing.Advance(.05);if(i%120==0)Compare(growing,growthOracle,"growth_"+i);}Compare(growing,growthOracle,"growth_final");growing.Validate();
    }
    static void Stress()
    {
        var s=CityCoreIndustrySession.CreateEmpty();var oracle=Bind(s,CityScenarioBuilder.Flat);var random=new Random(40721);int accepted=0,rejected=0;
        for(int i=0;i<30;i++)
        {
            var before=CityCoreSave.Write(s);double a=-60+i/2*8,b=-65+random.NextDouble()*4,c=65-random.NextDouble()*4;
            try{if(i%2==0)Add(s,b,a,c,a+random.NextDouble()*3);else Add(s,a,b,a+random.NextDouble()*3,c);accepted++;}
            catch(ArgumentException){rejected++;if(!before.SequenceEqual(CityCoreSave.Write(s)))throw new Exception("Rejected stress geometry changed facts");}
            catch(InvalidOperationException){rejected++;if(!before.SequenceEqual(CityCoreSave.Write(s)))throw new Exception("Rejected stress transaction changed facts");}
            Compare(s,oracle,"dense_edit_"+i);
        }
        if(accepted<20)throw new Exception("Stress fixture did not exercise enough accepted edits");
        foreach(int id in s.Transport.Network.RoadIds.OrderByDescending(id=>id).Take(12).ToArray())
        {
            s.Construction.UpgradeRoad(id,1,RoadDirection.Backward);Compare(s,oracle,"dense_reverse_"+id);
            s.Construction.RemoveRoad(id);Compare(s,oracle,"dense_delete_"+id);
        }
        Console.WriteLine("Dense stress accepted="+accepted+" rejected="+rejected);s.Validate();
    }
    static void Locality(string save)
    {
        var s=CityCoreSave.Read(File.ReadAllBytes(save));var oracle=Bind(s,CityScenarioBuilder.Flat);Compare(s,oracle,"locality_initial");
        var zoning=s.Simulation.Growth.Zoning;var remote=zoning.Cells().Where(c=>c.pose.x>0).ToArray();double h=s.City.map.buildHalfSize;Add(s,-h+5,-h+8,-h+17,-h+8);Compare(s,oracle,"locality_added");
        var after=zoning.Cells().ToDictionary(c=>c.key);if(remote.Any(c=>!after.TryGetValue(c.key,out var actual)||!ReferenceEquals(c,actual)))throw new Exception("Unchanged remote cells were regenerated");
        if(zoning.LastGeometryFull||zoning.LastGeometryRoads>=s.Transport.Network.RoadCount/4)throw new Exception("Isolated local edit recomputed global candidates");
        Console.WriteLine("PASS remote reused="+remote.Length+" geometry roads="+zoning.LastGeometryRoads+" resolved roads="+zoning.LastResolvedRoads);
    }
    static void OccupiedEdits()
    {
        // Reuse the existing production-growth fixture pattern: paint three
        // uses and let normal timed construction create projects/buildings.
        var s=CityCoreIndustrySession.CreateEmpty();Bind(s,CityScenarioBuilder.Flat);Add(s,-40,0,40,0);var zoning=s.Simulation.Growth.Zoning;
        zoning.Paint(zoning.Cells().Where(c=>c.side>0).ToArray(),LandUse.Residential);zoning.Paint(zoning.Cells().Where(c=>c.side<0&&c.pose.x<0).ToArray(),LandUse.Commercial);zoning.Paint(zoning.Cells().Where(c=>c.side<0&&c.pose.x>=0).ToArray(),LandUse.Industrial);
        s.Advance(6);if(s.City.development.projects.Count!=3)throw new Exception("Mixed normal projects fixture missing");var pending=CityCoreSave.Write(s);s.Advance(16);
        if(!new[]{LandUse.Residential,LandUse.Commercial,LandUse.Industrial}.All(use=>s.Entities.Buildings.Any(b=>b.Use==use)))throw new Exception("Normal growth did not create all three actual building uses");var built=CityCoreSave.Write(s);
        foreach(var checkpoint in new[]{pending,built})foreach(string operation in new[]{"split","widen","reverse","delete","add"})
        {
            var clone=CityCoreSave.Read(checkpoint);var oracle=Bind(clone,CityScenarioBuilder.Flat);string label=(ReferenceEquals(checkpoint,pending)?"pending_":"built_")+operation;Compare(clone,oracle,label+"_initial");var before=CityCoreSave.Write(clone);
            try
            {
                if(operation=="split")clone.Transport.SplitRoadAt(1,33.7);
                if(operation=="widen")clone.Construction.UpgradeRoad(1,2,RoadDirection.Both);
                if(operation=="reverse")clone.Construction.UpgradeRoad(1,1,RoadDirection.Backward);
                if(operation=="delete")clone.Construction.RemoveRoad(1);
                if(operation=="add")Add(clone,-40,30,40,30);
            }
            catch(InvalidOperationException){if(!before.SequenceEqual(CityCoreSave.Write(clone)))throw new Exception(label+" rejected transaction changed facts");Console.WriteLine("REJECTED unchanged "+label);}
            catch(ArgumentException){if(!before.SequenceEqual(CityCoreSave.Write(clone)))throw new Exception(label+" rejected geometry changed facts");Console.WriteLine("REJECTED unchanged "+label);}
            Compare(clone,oracle,label+"_edited");clone.Advance(16);Compare(clone,oracle,label+"_continued");clone.Validate();
        }
    }
    static void Main(string[] args){Directory.CreateDirectory(args[1]);if(args.Length>2&&args[2]=="stress")Stress();else if(args.Length>2&&args[2]=="locality")Locality(args[0]);else if(args.Length>2&&args[2]=="occupied")OccupiedEdits();else{Busy(args[0]);Fixtures();}File.WriteAllLines(Path.Combine(args[1],"snapshots.csv"),rows);File.WriteAllText(Path.Combine(args[1],"Result.txt"),"PASS "+snapshots+" ordered zoning snapshots / "+cellsChecked+" exact cells including key/block/column/row/side/use/availability/owner/float geometry; frozen 8273ffa oracle. Scope is the explicitly selected full, dense, locality or occupied edit suite; see stdout and snapshots.csv.\n");}
}
