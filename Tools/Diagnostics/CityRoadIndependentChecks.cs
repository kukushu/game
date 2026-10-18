using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Reflection;
using HarborCity;
using HarborCity.RoadCore;

// Same executable entry is compiled against frozen/current model sources.
// Complete save hashes, exceptions and continuation must match between builds.
static class CityRoadIndependentChecks
{
    static int checks;static bool baseline;static readonly List<string> rows=new List<string>{"case,outcome,state_sha256"};
    static void Check(bool condition,string reason){checks++;if(!condition)throw new Exception(reason);}
    static byte[] Save(World w)=>SaveCodec.Write(w);
    static void Record(string name,string outcome,World w)
    {using(var h=SHA256.Create())rows.Add(name+","+outcome+","+BitConverter.ToString(h.ComputeHash(Save(w))).Replace("-","").ToLowerInvariant());}
    static void RecordCity(string name,CityCoreIndustrySession s)
    {using(var h=SHA256.Create()){rows.Add(name+",accepted,"+BitConverter.ToString(h.ComputeHash(CityCoreSave.Write(s))).Replace("-","").ToLowerInvariant());rows.Add(name+"_supply,derived,"+BitConverter.ToString(h.ComputeHash(System.Text.Encoding.UTF8.GetBytes(SupplyFacts(s)))).Replace("-","").ToLowerInvariant());}}
    static string SupplyFacts(CityCoreIndustrySession s)=>s.City.power+"/"+s.City.water+"/"+s.City.demand+"/"+s.City.WaterDemand+"/"+s.City.SewageCapacity+";"+string.Join(";",s.Entities.Buildings.Select(b=>{var u=s.City.Supply(b.id);return b.id+":"+u.electricity+":"+u.water+":"+u.sewage+":"+string.Join(",",u.pipes.Select(p=>p.id+"/"+p.load+"/"+p.fresh+"/"+p.drain+"/"+string.Join("-",p.inactiveDrains)));}));
    static Point[] Stroke(double z= -100)
    {var points=new Point[9];for(int i=0;i<9;i++)points[i]=Point.Lerp(new Point(-100,0,z),new Point(-80,0,z),i/8.0);return points;}
    static void Main(string[] args)
    {
        baseline=args[0]=="baseline";string output=args[1];Directory.CreateDirectory(output);
        foreach(bool frozen in new[]{false,true})foreach(bool oneWay in new[]{false,true})foreach(int lanes in new[]{1,2,8})
        {
            var w=CityRoadCoreFixtures.Create(100,false);if(frozen){var v=w.Vehicles[0];v.Freeze(w.Position(v));w.Validate();w.AssertSafety();}
            string name=(frozen?"stranded":"active")+"_"+oneWay+"_"+lanes;var before=Save(w);
            var routes=w.Vehicles.ToDictionary(v=>v.id,v=>v.Plan.segments);var positions=w.Vehicles.ToDictionary(v=>v.id,v=>w.Position(v));
            var plan=w.PlanConnectedRoad(Stroke(),oneWay,lanes:lanes);
            Check(before.SequenceEqual(Save(w)),"Planning changed live facts");
            if(!baseline)Check(plan.remaps.Count==0&&plan.interruptions.Count==0&&plan.accesses==null,"Independent road generated actor/access migrations");
            Check(plan.TryCommit(),"Independent plan stale");
            foreach(var v in w.Vehicles)
            {
                var p=positions[v.id];var q=w.Position(v);Check(p.x==q.x&&p.y==q.y&&p.z==q.z,"Independent edit moved actor");
                Check(routes[v.id].SequenceEqual(v.Route),"Independent edit rerouted actor");if(!baseline)Check(ReferenceEquals(routes[v.id],v.Plan.segments),"Unrelated route was copied/replaced");
            }
            w.Validate();w.AssertSafety();Record(name,"accepted",w);
            var restored=SaveCodec.Read(Save(w));for(int i=0;i<80;i++){w.Advance(.05);restored.Advance(.05);}
            Check(Save(w).SequenceEqual(Save(restored)),"Cold continuation differs");Record(name+"_80steps","accepted",w);
        }
        // Fallbacks and rejection conditions, including exact overlap, snapping,
        // crossing, tiny/duplicate samples, grade separation and malformed input.
        foreach(string name in new[]{"extend","connect","cross","near_road","overlap","tiny","duplicate","nan","elevated","snap_end"})
        {
            var w=CityRoadCoreFixtures.Create(20,false);var before=Save(w);Point[] points;
            switch(name)
            {
                case "extend":points=new[]{new Point(0,0,0),new Point(-20,0,0)};break;
                case "connect":points=new[]{new Point(0,0,0),new Point(0,0,8)};break;
                case "cross":points=new[]{new Point(30,0,-4),new Point(30,0,12)};break;
                case "near_road":points=new[]{new Point(-2,0,-2),new Point(-20,0,-2)};break;
                case "overlap":points=new[]{new Point(20,0,0),new Point(40,0,0)};break;
                case "tiny":points=new[]{new Point(-100,0,-100),new Point(-99.95,0,-100)};break;
                case "duplicate":points=new[]{new Point(-100,0,-100),new Point(-100,0,-100),new Point(-80,0,-100)};break;
                case "nan":points=new[]{new Point(double.NaN,0,-100),new Point(-80,0,-100)};break;
                case "elevated":points=new[]{new Point(30,10,-4),new Point(30,10,12)};break;
                default:points=new[]{new Point(-100,0,-100),new Point(-99,0,-100)};break;
            }
            string outcome="accepted";
            try{var plan=w.PlanConnectedRoad(points,snap:3);Check(plan.TryCommit(),"Fallback stale");w.Validate();w.AssertSafety();}
            catch(Exception error)when(error is ArgumentException||error is InvalidOperationException)
            {outcome=error.GetType().Name;Check(before.SequenceEqual(Save(w)),"Failed road mutated live world: "+name);}
            Record(name,outcome,w);
        }
        var staleWorld=CityRoadCoreFixtures.Create(20,false);var stale=staleWorld.PlanConnectedRoad(Stroke());staleWorld.Advance(.05);var unchanged=Save(staleWorld);
        Check(!stale.TryCommit()&&unchanged.SequenceEqual(Save(staleWorld)),"Simulation did not invalidate plan");Record("stale","rejected",staleWorld);
        UtilityLayoutCases();
        File.WriteAllLines(Path.Combine(output,"independent.csv"),rows);
        File.WriteAllText(Path.Combine(output,"Result.txt"),"PASS "+checks+" isolated-add checks, actor/route identity, frozen actors, old-path fallbacks, failed/stale atomicity and cold continuation. Compare complete state CSV with frozen build; synthetic model checks, not Player performance.\n");
        Console.WriteLine(File.ReadAllText(Path.Combine(output,"Result.txt")));
    }
    static object Layout(CityCoreIndustrySession s)=>typeof(CityModel).GetField("utilityLayout",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(s.City);
    static void UtilityLayoutCases()
    {
        foreach(string mode in new[]{"isolated","manual_cross","dto_change","no_cache","budget"})
        {
            var s=CityGrowingGridFixture.Create();s.City.utilities.roadConduits=true;s.Simulation.Finance.UnlimitedFunds=true;s.Simulation.Utilities.Refresh();
            if(mode=="manual_cross")s.Simulation.Utilities.Build(UtilityKind.Water,new Point(112,2,90),new Point(112,2,110));
            object layout=Layout(s);if(mode=="no_cache")typeof(CityModel).GetField("utilityLayout",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(s.City,null);
            if(mode=="dto_change")s.City.utilities.electricity.nodes[0].x+=.25f; // original signature must still invalidate unversioned DTO changes
            Action<double> add=z=>{var plan=s.Transport.PlanConnectedRoad(new[]{new Point(100,2,z),new Point(124,2,z)},snap:1);s.Construction.CheckRoadPlacement(plan);Check(s.Construction.Commit(plan,out _),"Utility fixture stale");};
            add(100);
            if(!baseline)
            {
                if(mode=="isolated"||mode=="budget")Check(ReferenceEquals(layout,Layout(s)),"Proved unused component rebuilt static layout");
                else Check(!ReferenceEquals(layout,Layout(s)),"Potential utility dependency reused stale layout: "+mode);
            }
            if(mode=="budget"){s.Simulation.Utilities.SetBudget(CityServiceKind.Electricity,150);Check(s.City.power==240,"Static layout reuse cached actual facility capacity");}
            s.Validate();RecordCity("utility_"+mode,s);
            add(124);for(int i=0;i<80;i++)s.Advance(.05);RecordCity("utility_"+mode+"_continuous80",s);
            var bytes=CityCoreSave.Write(s);var restored=CityCoreSave.Read(bytes);restored.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
            for(int i=0;i<80;i++){s.Advance(.05);restored.Advance(.05);}Check(CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(restored))&&SupplyFacts(s)==SupplyFacts(restored),"Retained supply layout changed warm/cold simulation");RecordCity("utility_"+mode+"_restore160",s);
        }
    }
}
