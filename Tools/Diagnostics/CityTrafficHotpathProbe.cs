using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using HarborCity;
using HarborCity.RoadCore;

static class CityTrafficHotpathProbe
{
    static string Hash(byte[] data){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(data)).Replace("-","");}
    static string Row(params object[] values)=>string.Join(",",values.Select(v=>Convert.ToString(v,CultureInfo.InvariantCulture)));
    static Dictionary<string,long> Counters(World w)
    {
        var result=new Dictionary<string,long>();
        foreach(var f in typeof(World).GetFields(BindingFlags.Instance|BindingFlags.NonPublic).Where(f=>f.Name.StartsWith("Census")))result[f.Name]=(long)f.GetValue(w);
        foreach(var f in typeof(LaneChanges).GetFields(BindingFlags.Static|BindingFlags.NonPublic).Where(f=>f.Name.StartsWith("Census")))result[f.Name]=(long)f.GetValue(null);
        return result;
    }
    static void Mark(List<string> states,string name,CityCoreIndustrySession s)
    {
        // Compare exact incremental order before full validation can repair it.
        s.Transport.AssertIncrementalIndex();s.Validate();states.Add(Row(name,s.Simulation.Tick,s.Transport.ActiveCount,Hash(CityCoreSave.Write(s))));
    }
    static void SmallCases(string output)
    {
        // Independent total-order oracle: projected shadows and exact distance
        // ties must keep the same vehicle-ID arbitration as the original sort.
        var random=new Random(7919);
        for(int test=0;test<200;test++)
        {
            var bucket=new List<LaneOccupant>();for(int i=0;i<40;i++)bucket.Add(new LaneOccupant(new Vehicle{id=i+1},random.Next(8),i%2==0));
            var expected=bucket.OrderByDescending(x=>x.distance).ThenBy(x=>x.vehicle.id).ToArray();
            LaneOccupantOrder.SortIfNeeded(bucket);if(!bucket.SequenceEqual(expected))throw new Exception("Occupant sort differs from independent distance/ID/shadow oracle");
            if(LaneOccupantOrder.SortIfNeeded(bucket))throw new Exception("Ordered bucket unexpectedly sorted");
        }
        var states=new List<string>{"case,tick,active,sha256"};
        var gaps=new List<string>{"neighbor,route_checks,route_segments,state_sha256"};
        foreach(int neighbor in new[]{-1,1})
        {
            var world=new World();var roads=new List<int>();int a=world.AddJunction(new Point());int first=a,relief=0;
            for(int i=0;i<40;i++){int b=world.AddJunction(new Point((i+1)*100,0,0));roads.Add(world.AddRoad(a,b,new[]{world.JunctionPosition(a),world.JunctionPosition(b)},true,lanes:2));a=b;if(i==38)relief=b;}
            // Ordinary two-leg junctions preserve lane identity. A legal late
            // junction allows rejoining the destination lane after a long walk.
            int side=world.AddJunction(new Point(3900,0,30));world.AddRoad(relief,side,new[]{world.JunctionPosition(relief),world.JunctionPosition(side)});
            int source=world.AddDepot(first),goal=world.AddDepot(a,0);world.RegisterAccess(source,Network.LaneId(roads[0],false),30);world.RegisterAccess(goal,Network.LaneId(roads[39],false),50);
            var actor=world.Dispatch(source,goal);long target=Network.LaneId(roads[0],false,1);double d=world.Network.Get(actor.Current).Geometry.MapDistance(world.Network.Get(target).Geometry,actor.distance);
            var route=new List<long>();if(!LaneChanges.Route(world.Network,actor,target,route))throw new Exception("Long-route gap fixture invalid");
            var data=world.Snapshot();data.nextVehicle=data.nextTask=3;var blocker=new Vehicle{id=2,Task=new TripTask{id=2,origin=source,destination=goal,Shipment=new Shipment{remaining=1,unitPrice=5}},Plan=new RoutePlan{segments=route,EndDistance=50},Position=new VehiclePosition(target,d+neighbor)};data.tasks.Add(blocker.Task);data.vehicles.Add(blocker);world=new World(data);
            byte[] before=SaveCodec.Write(world);var counters=Counters(world);if(world.TryChangeLane(1,1))throw new Exception("Unsafe front/rear gap accepted");if(!before.SequenceEqual(SaveCodec.Write(world)))throw new Exception("Rejected gap changed physical/task state");var after=Counters(world);
            gaps.Add(Row(neighbor,after.TryGetValue("CensusRouteChecks",out long checks)?checks-counters["CensusRouteChecks"]:-1,after.TryGetValue("CensusRouteSegments",out long segments)?segments-counters["CensusRouteSegments"]:-1,Hash(SaveCodec.Write(world))));
        }
        File.WriteAllLines(Path.Combine(output,"gap-census.csv"),gaps);
        foreach(bool edit in new[]{false,true})
        {
            var city=CityCoreIndustrySession.Create();
            for(int i=0;i<1200;i++)
            {
                if(edit&&i==40)city.Transport.SplitRoadAt(1,17);
                if(edit&&i==100)city.Transport.RemoveRoad(2);
                if(edit&&i==140)city.Transport.AddRoad(2,3,new[]{city.Transport.JunctionPosition(2),new Point(18,0,0),city.Transport.JunctionPosition(3)},lanes:2);
                city.Advance(.05);if(i%40==39)Mark(states,edit?"edits":"commute",city);
            }
            var cold=CityCoreSave.Read(CityCoreSave.Write(city));
            for(int i=0;i<80;i++){city.Advance(.05);cold.Advance(.05);if(!CityCoreSave.Write(city).SequenceEqual(CityCoreSave.Write(cold)))throw new Exception("Cold city continuation differs");}
            Mark(states,edit?"edits-cold":"commute-cold",city);
        }
        File.WriteAllLines(Path.Combine(output,"small-states.csv"),states);
    }
    static void Main(string[] args)
    {
        string input=args[0],output=args[1];int warm=int.Parse(args[2]),steps=int.Parse(args[3]);bool verify=args.Length>4&&args[4]=="verify";
        if(Directory.Exists(output))throw new Exception("Fresh result directory required");Directory.CreateDirectory(output);
        if(verify)SmallCases(output);
        var city=CityCoreSave.Read(File.ReadAllBytes(input));city.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);city.Transport.DepotLaneRoutingEnabled=true;
        for(int i=0;i<warm;i++)city.Advance(.05);
        var states=new List<string>{"case,tick,active,sha256"};File.WriteAllBytes(Path.Combine(output,"start.hcity"),CityCoreSave.Write(city));
        var before=Counters(city.Transport);var samples=new double[steps];var timer=new Stopwatch();long memory=GC.GetTotalMemory(true),allocated=GC.GetAllocatedBytesForCurrentThread();
        int gc0=GC.CollectionCount(0),gc1=GC.CollectionCount(1),gc2=GC.CollectionCount(2);
        for(int i=0;i<steps;i++)
        {
            timer.Restart();city.Advance(.05);timer.Stop();samples[i]=timer.Elapsed.TotalMilliseconds;
            if(verify&&i%200==199)Mark(states,"city",city);
        }
        long bytes=GC.GetAllocatedBytesForCurrentThread()-allocated;int end0=GC.CollectionCount(0),end1=GC.CollectionCount(1),end2=GC.CollectionCount(2);var after=Counters(city.Transport);
        File.WriteAllLines(Path.Combine(output,"census.csv"),new[]{"metric,start,end,delta"}.Concat(after.Select(p=>Row(p.Key,before[p.Key],p.Value,p.Value-before[p.Key]))));
        Mark(states,"end",city);byte[] final=CityCoreSave.Write(city);File.WriteAllBytes(Path.Combine(output,"final.hcity"),final);
        var coldCity=CityCoreSave.Read(final);coldCity.Transport.DepotLaneRoutingEnabled=true;coldCity.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
        for(int i=0;i<20;i++){city.Advance(.05);coldCity.Advance(.05);}city.Transport.AssertIncrementalIndex();coldCity.Transport.AssertIncrementalIndex();city.Validate();coldCity.Validate();if(!CityCoreSave.Write(city).SequenceEqual(CityCoreSave.Write(coldCity)))throw new Exception("Cold city continuation differs");
        File.WriteAllLines(Path.Combine(output,"states.csv"),states);File.WriteAllLines(Path.Combine(output,"steps.csv"),new[]{"index,ms"}.Concat(samples.Select((v,i)=>Row(i,v))));
        Array.Sort(samples);File.WriteAllText(Path.Combine(output,"summary.csv"),"runtime,steps,mean_ms,p95_ms,p99_ms,max_ms,allocated_bytes,retained_before_steps,gc0,gc1,gc2,population,active,final_sha256\n"+Row(verify?"DotNet64-oracles-not-performance":"DotNet64-model-not-Player",steps,samples.Average(),samples[(int)Math.Ceiling(steps*.95)-1],samples[(int)Math.Ceiling(steps*.99)-1],samples[steps-1],bytes,memory,end0-gc0,end1-gc1,end2-gc2,city.City.population,city.Transport.ActiveCount,Hash(final)));
        Console.WriteLine("PASS whole-city index, ledgers, safety and cold20; population="+city.City.population+" active="+city.Transport.ActiveCount+" output="+output);
    }
}
