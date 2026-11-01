using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Diagnostics;
using HarborCity;
using HarborCity.RoadCore;
static class ShoppingReplayTrace
{
    internal static BinaryWriter Output;
    internal static void Record(long tick,int family,int person,int target){if(Output==null)return;Output.Write(tick);Output.Write(family);Output.Write(person);Output.Write(target);}
}
static class CityShoppingScheduleChecks
{
    static int checks;
    static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
    static CityResident Pick(Household h,CityCoreIndustrySession s)
    {if(!h.resident||h.shoppingDay==s.City.day||h.savings<s.Simulation.Design.Retail.UnitPrice)return null;return h.people.FirstOrDefault(p=>!p.dead&&!p.sick&&p.age>=18&&!p.atWork&&p.tripId==0&&p.location==h.home&&p.carriedGoods==0&&s.Transport.Clock>=p.retryAt);}
#if CITY_HOUSEHOLD_SHOPPING_SCHEDULE
    static List<int> Pass(CityCoreIndustrySession s,bool indexed,int step)
    {
        var result=new List<int>();Action<Household> visit=h=>{if(Pick(h,s)==null)return;result.Add(h.id);if(h.id%13==0){int at=(h.id*17+step)%s.City.society.families.Count;var other=s.City.society.families[at];other.savings=900;other.shoppingDay=-1;foreach(var p in other.people){p.tripId=0;p.location=other.home;p.sick=false;p.retryAt=0;}}};
        if(indexed){foreach(var h in s.Simulation.Consumption.Schedule.Begin())visit(h);}else foreach(var h in s.City.society.families)visit(h);return result;
    }
    static void Unit()
    {
        var a=CityCoreIndustrySession.Create();var b=CityCoreIndustrySession.Create();
        foreach(var s in new[]{a,b})for(int i=0;i<180;i++){var h=new Household{id=s.City.society.nextId,home=3,resident=true,savings=900};s.Entities.AddFamily(h);s.Entities.AddResident(h.id,new CityResident{id=s.City.society.nextCitizenId,age=20,location=3});}
        var random=new Random(110118);for(int pass=0;pass<400;pass++)
        {
            for(int k=0;k<24;k++){int at=random.Next(a.City.society.families.Count),kind=random.Next(11);foreach(var s in new[]{a,b}){var h=s.City.society.families[at];var p=h.people[0];switch(kind){case 0:h.savings=h.savings==0?900:0;break;case 1:h.shoppingDay=h.shoppingDay==s.City.day?-1:s.City.day;break;case 2:h.resident=!h.resident;break;case 3:p.sick=!p.sick;break;case 4:p.dead=!p.dead;break;case 5:p.atWork=!p.atWork;break;case 6:p.tripId=p.tripId==0?123:0;break;case 7:p.location=p.location==h.home?1:h.home;break;case 8:p.retryAt=pass%3==0?float.NaN:pass%3==1?float.PositiveInfinity:0;break;case 9:p.age=p.age==17?25:17;break;case 10:p.carriedGoods=p.carriedGoods==0?1:0;break;}}}
            var actual=Pass(a,true,pass);var expected=Pass(b,false,pass);Check(actual.SequenceEqual(expected),"Family order/eligibility mismatch pass "+pass);a.Simulation.Consumption.Schedule.AssertOracle();
        }
        var t=CityCoreIndustrySession.Create();var f=t.Entities.Members[0].Family;foreach(var p in f.people)p.dead=true;var who=f.people[0];who.dead=false;who.age=20;who.location=f.home;who.atWork=false;who.tripId=0;who.carriedGoods=0;f.resident=true;f.shoppingDay=-1;f.savings=900;who.retryAt=.05f;
        Check(!t.Simulation.Consumption.Schedule.Begin().GetEnumerator().MoveNext(),"Future retry woke early");t.Advance(.05);Check(!t.Simulation.Consumption.Schedule.Begin().GetEnumerator().MoveNext(),"Float retry .05f rounded to double .05");t.Advance(.05);t.Simulation.Consumption.Schedule.AssertOracle();Check(t.Simulation.Consumption.Schedule.Begin().GetEnumerator().MoveNext(),"Retry wake missed");
        f.savings=0;t.Simulation.Consumption.Schedule.Begin();typeof(Household).GetField("balance",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(f,900);bool caught=false;try{t.Simulation.Consumption.Schedule.AssertOracle();}catch(InvalidOperationException){caught=true;}Check(caught,"Missing funds notification not detected by independent oracle");
        var life=CityCoreIndustrySession.Create();life.Simulation.Consumption.Schedule.Begin();var added=new Household{id=life.City.society.nextId,home=3,resident=true,savings=900};life.Entities.AddFamily(added);life.Entities.AddResident(added.id,new CityResident{id=life.City.society.nextCitizenId,age=21,location=3});life.Simulation.Consumption.Schedule.AssertOracle();
        var moved=life.Entities.Members[0].Person;life.Entities.MoveResident(moved.id,added.id);life.Simulation.Consumption.Schedule.AssertOracle();Check(Pass(life,true,1).SequenceEqual(Pass(life,false,1)),"Membership order mismatch");
        int remove=added.people.Last().id;life.Entities.RemoveResident(remove);life.Simulation.Consumption.Schedule.AssertOracle();added.home=1;life.Simulation.Consumption.Schedule.AssertOracle();added.home=3;life.Simulation.Consumption.Schedule.AssertOracle();
        Console.WriteLine("PASS "+checks+" shopping index/original-loop checks; 400 multiword callbacks; exact retry boundary; missing-notification negative control");
    }
#endif
    static void Main(string[] args)
    {
        if(args[0]=="unit"){
#if CITY_HOUSEHOLD_SHOPPING_SCHEDULE
            Unit();return;
#else
            throw new Exception("Unit requires candidate compile flag");
#endif
        }
        string folder=args[2];Directory.CreateDirectory(folder);var input=File.ReadAllBytes(args[1]);var s=CityCoreSave.Read(input);s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);s.Transport.DepotLaneRoutingEnabled=true;int steps=int.Parse(args[3]);
        bool transitions=args.Length>4&&bool.Parse(args[4]);
        int shopId=transitions?s.Entities.Shops.First(x=>!x.abandoned&&!x.burned).id:0;
        int roadId=transitions?s.Transport.Network.RoadIds.First():0;
        var timings=new List<string>{"tick,elapsed_ms,allocated_bytes"};
        var states=new List<string>{"tick,sha256"};ShoppingReplayTrace.Output=new BinaryWriter(File.Create(Path.Combine(folder,"requests.bin")));
        try{for(int i=0;i<steps;i++){
            if(transitions){
                if(i==100)s.Entities.SetAvailability(shopId,true,false);
                if(i==200)s.Entities.SetAvailability(shopId,false,false);
                if(i==300){var before=CityCoreSave.Write(s);bool rejected=false;try{s.Construction.UpgradeRoad(int.MaxValue,2,RoadDirection.Both);}catch(CityRuleException){rejected=true;}Check(rejected&&before.SequenceEqual(CityCoreSave.Write(s)),"Failed road transaction changed state");}
                if(i==400){
                    bool removed=false;int attempts=0;
                    foreach(int id in new[]{roadId}.Concat(s.Transport.Network.RoadIds.OrderByDescending(x=>x)).Distinct().Take(8)){
                        var before=CityCoreSave.Write(s);attempts++;
                        try{s.Construction.RemoveRoad(id);removed=true;Console.WriteLine("Road removed "+id+" attempt "+attempts);break;}
                        catch(InvalidOperationException e) when(e.Message=="施工节点安全区内有冲突车辆，请等待它们驶离后重试；道路和任务未修改。"){
                            Check(before.SequenceEqual(CityCoreSave.Write(s)),"Reservation-rejected demolition changed full state");Console.WriteLine("Reservation rejection retained full state road "+id);
                        }
                    }
                    Check(removed,"No safe demolition in bounded transition fixture; not a scheduling failure");
                }
            }
            long allocated=GC.GetAllocatedBytesForCurrentThread();var timer=Stopwatch.StartNew();if(s.Advance(.05)!=1)throw new Exception("Lost fixed step");timer.Stop();timings.Add(string.Join(",",s.Simulation.Tick,timer.Elapsed.TotalMilliseconds.ToString("R",System.Globalization.CultureInfo.InvariantCulture),GC.GetAllocatedBytesForCurrentThread()-allocated));if(i%400==399||i==steps-1){
#if CITY_HOUSEHOLD_SHOPPING_SCHEDULE
            s.Simulation.Consumption.Schedule.AssertOracle();
#endif
            byte[] bytes=CityCoreSave.Write(s);using(var sha=SHA256.Create())states.Add(s.Simulation.Tick+","+BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-",""));if(i==steps-1)File.WriteAllBytes(Path.Combine(folder,"final.hcity"),bytes);Console.WriteLine("State tick "+s.Simulation.Tick);}}
        }finally{ShoppingReplayTrace.Output.Dispose();ShoppingReplayTrace.Output=null;}
        File.WriteAllLines(Path.Combine(folder,"step-times.csv"),timings);File.WriteAllLines(Path.Combine(folder,"states.csv"),states);var hot=s;var cold=CityCoreSave.Read(File.ReadAllBytes(Path.Combine(folder,"final.hcity")));cold.Transport.DepotLaneRoutingEnabled=true;cold.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);for(int i=0;i<80;i++){hot.Advance(.05);cold.Advance(.05);}Check(CityCoreSave.Write(hot).SequenceEqual(CityCoreSave.Write(cold)),"Cold scheduler changed full state");Check(input.SequenceEqual(File.ReadAllBytes(args[1])),"Input modified");Console.WriteLine("PASS full-state checkpoints, cold80 and input protection (cross-build comparison performed by controller)");
    }
}
