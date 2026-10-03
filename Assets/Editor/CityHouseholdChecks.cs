using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HarborCity
{
    public static class CityHouseholdChecks
    {
        [Serializable] sealed class LogPayloadCheck {public CityLogDetail data;}
        [MenuItem("Harbor/Validate household simulation")]
        public static void Validate()
        {
            var empty=CityModel.Create(); new CityTraffic(empty); empty.EnableRoads((x,z)=>1);
            empty.EnableBuildings(); empty.EnableHouseholds(); empty.EnableResidentTransport();
            empty=JsonUtility.FromJson<CityModel>(JsonUtility.ToJson(empty));
            if(!empty.Valid() || empty.population!=0 || empty.society.families.Count!=0 || empty.roads.edges.Count!=0 || empty.tiles.Any(t=>t!=0))
                throw new Exception("Empty new-game save round-trip failed");
            using(var journal=new CitySimulationLog("Temp/UnityLogChecks",o=>JsonUtility.ToJson(o)))
            {
                empty.logSink=journal.Write; journal.Snapshot("baseline",empty);
                empty.Trace("test.unity_json","中文\"引号\"\n换行",new CityLogDetail {reason="嵌套数据",amount=42},citizen:10);
                string folder=journal.Export("Temp/UnityLogExports",empty);
                string line=File.ReadAllLines(Path.Combine(folder,"events-0001.jsonl")).Single();
                var record=JsonUtility.FromJson<CityLogEvent>(line);
                if(record.seq!=1 || record.citizenId!=10 || record.message!="中文\"引号\"\n换行" || JsonUtility.FromJson<LogPayloadCheck>(line).data.amount!=42
                    || !JsonUtility.FromJson<CityModel>(File.ReadAllText(Path.Combine(folder,"baseline.json"))).Valid())
                    throw new Exception("Structured log Unity JSON/export failed");
                empty.logSink=null;
                var analysis=JsonUtility.FromJson<CityAnalysisReport>(File.ReadAllText(Path.Combine(folder,"Analysis","in-progress.json")));
                if(analysis==null || !analysis.inProgress || analysis.day!=empty.day || analysis.factories==null || analysis.households==null)
                    throw new Exception("Derived analysis Unity JSON/export failed");
            }
            var c=CityModel.CreateLegacySample(); new CityTraffic(c);
            c=JsonUtility.FromJson<CityModel>(JsonUtility.ToJson(c));
            if(!c.Valid()) throw new Exception("Legacy JSON validation failed");
            c.EnableRoads((x,z)=>1); c.EnableBuildings(); c.EnableHouseholds();
            for(int i=0;i<12;i++) c.Tick(); c.society.dayElapsed=42.5f;
            var restored=JsonUtility.FromJson<CityModel>(JsonUtility.ToJson(c));
            if(!restored.Valid() || restored.society.dayElapsed!=42.5f) throw new Exception("Household save round-trip failed");
            if(!c.society.families.SelectMany(h=>h.people).Select(p=>p.id+":"+p.name).SequenceEqual(restored.society.families.SelectMany(h=>h.people).Select(p=>p.id+":"+p.name)))
                throw new Exception("Resident identities changed on reload");
            var family=restored.society.families.First(h=>h.people.Any(p=>restored.Workplace(p)>=0));
            var before=restored.ObserveResident(family,family.people[0]);
            var again=JsonUtility.FromJson<CityModel>(JsonUtility.ToJson(restored));
            var same=again.society.families.First(h=>h.id==family.id); var after=again.ObserveResident(same,same.people[0]);
            if(before.state!=after.state || before.x!=after.x || before.z!=after.z) throw new Exception("Resident activity changed on reload");
            for(int i=0;i<20;i++) {c.Tick(); restored.Tick();}
            if(JsonUtility.ToJson(c.society)!=JsonUtility.ToJson(restored.society) || c.money!=restored.money)
                throw new Exception("Restored household decisions diverged");
            var game=UnityEngine.Object.FindAnyObjectByType<HarborCityGame>();
            if(game!=null)
            {
                var live=(CityModel)typeof(HarborCityGame).GetField("city",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(game);
                if(!live.Valid() || live.version!=7) throw new Exception("Live household model invalid");
                var snapshot=JsonUtility.FromJson<CityModel>(JsonUtility.ToJson(live)); snapshot.Tick();
                if(!snapshot.Valid()) throw new Exception("Live snapshot failed daily settlement");
                foreach(var day in snapshot.society.history)
                    if(day.closingTreasury!=day.openingTreasury+day.rent-day.maintenance
                        || day.closingSavings!=day.openingSavings+day.wages-day.living-day.travel-day.rent-day.movingCosts)
                        throw new Exception("Live ledger imbalance");
            }
            var transport=CityModel.CreateLegacySample(); var driver=new CityTraffic(transport);
            transport.EnableRoads((x,z)=>1); transport.EnableBuildings(); transport.EnableHouseholds(); transport.EnableResidentTransport();
            driver.Advance(37);
            // Exercise the actual Unity serializer with a v4 journey already in progress.
            var legacy=JsonUtility.FromJson<CityModel>(JsonUtility.ToJson(transport));
            foreach(var h in legacy.society.families)
            {
                var original=h.people[0]; h.members=h.people.Count; h.work=legacy.Workplace(original); h.skill=original.skill;
                foreach(var p in h.people.Skip(1))
                {
                    legacy.traffic.trips.RemoveAll(t=>t.residentId==p.id);
                    p.tripId=0; p.atWork=false; p.earnedWages=p.workedMinutes=0;
                }
            }
            legacy.version=4; legacy.society.jobEntities.Clear();
            foreach(var p in legacy.society.families.SelectMany(h=>h.people)) p.jobId=-1;
            string oldTrips=JsonUtility.ToJson(legacy.traffic);
            var oldPeople=legacy.society.families.Select(h=>h.people[0].id+":"+h.people[0].earnedWages+":"+h.people[0].tripId).ToArray();
            legacy=JsonUtility.FromJson<CityModel>(JsonUtility.ToJson(legacy)); legacy.EnableHouseholds();
            if(!legacy.Valid() || oldTrips!=JsonUtility.ToJson(legacy.traffic) || !oldPeople.SequenceEqual(legacy.society.families.Select(h=>h.people[0].id+":"+h.people[0].earnedWages+":"+h.people[0].tripId)))
                throw new Exception("v4 job migration changed journeys, earnings or identities");
            new CityTraffic(legacy).Advance(2);
            if(!legacy.Valid()) throw new Exception("Migrated v4 journeys cannot continue");
            var transportCopy=JsonUtility.FromJson<CityModel>(JsonUtility.ToJson(transport));
            if(!transportCopy.Valid()) throw new Exception("Resident transport save invalid");
            var resumed=new CityTraffic(transportCopy);
            driver.Advance(2); resumed.Advance(2);
            if(JsonUtility.ToJson(transport.society)!=JsonUtility.ToJson(transportCopy.society) || JsonUtility.ToJson(transport.traffic)!=JsonUtility.ToJson(transportCopy.traffic)
                || transport.buildings.Where((b,i)=>JsonUtility.ToJson(b)!=JsonUtility.ToJson(transportCopy.buildings[i])).Any())
            {
                File.WriteAllText("Temp/IndustryExpected.json",JsonUtility.ToJson(transport,true));
                File.WriteAllText("Temp/IndustryActual.json",JsonUtility.ToJson(transportCopy,true));
                throw new Exception("Resident transport diverged after reload; snapshots written to Temp/IndustryExpected.json and IndustryActual.json");
            }
            var industrial=transport.buildings.First(b=>transport.tiles[b.id]==4).factory;
            industrial.raw=3; industrial.processing=true; industrial.progress=17; industrial.consumed=industrial.produced+1;
            industrial.noise=.3f; industrial.pollution=.8f;
            transportCopy=JsonUtility.FromJson<CityModel>(JsonUtility.ToJson(transport));
            resumed=new CityTraffic(transportCopy); driver.Advance(2); resumed.Advance(2);
            if(!transportCopy.Valid() || JsonUtility.ToJson(transport)!=JsonUtility.ToJson(transportCopy)) throw new Exception("Partial industrial batch or environmental history diverged after reload");
            foreach(var b in transport.buildings.Where(b=>b.factory!=null))
                if(Math.Abs(b.factory.CashError)>.0001) throw new Exception("Factory cash ledger diverged after native JSON reload");
            double employerPay=transport.buildings.Where(b=>b.factory!=null).Sum(b=>b.factory.wageCosts);
            double workerPay=transport.society.families.SelectMany(h=>h.people).Sum(p=>p.factoryWageCredit+p.totalFactoryWagesPaid);
            if(Math.Abs(employerPay-workerPay)>.0001) throw new Exception("Native JSON factory payroll transfer is not conserved");
            var v6=JsonUtility.FromJson<CityModel>(JsonUtility.ToJson(transport)); v6.version=6;
            foreach(var b in v6.buildings.Where(b=>b.factory!=null))
            {
                var f=b.factory; f.financeInitialized=false; f.cash=f.capital=f.salesRevenue=f.rawCosts=f.wageCosts=f.productionCosts=f.unpaidWages=0; f.sold=0;
            }
            foreach(var p in v6.society.families.SelectMany(h=>h.people)) {p.factoryWageCredit=0; p.totalFactoryWagesPaid=0;}
            string v6Trips=JsonUtility.ToJson(v6.traffic);
            v6=JsonUtility.FromJson<CityModel>(JsonUtility.ToJson(v6)); v6.EnableHouseholds();
            if(!v6.Valid() || v6.version!=7 || JsonUtility.ToJson(v6.traffic)!=v6Trips
                || v6.buildings.Any(b=>b.factory!=null && b.factory.cash!=FactoryState.StartingCash)) throw new Exception("v6 finance migration changed journeys or initial funding");
            v6.EnableHouseholds();
            if(v6.buildings.Any(b=>b.factory!=null && b.factory.cash!=FactoryState.StartingCash)) throw new Exception("Finance migration is not idempotent");
            var v5=JsonUtility.FromJson<CityModel>(JsonUtility.ToJson(transport)); v5.version=5;
            foreach(var b in v5.buildings) b.factory=null;
            foreach(var p in v5.society.families.SelectMany(h=>h.people)) {p.factoryWageCredit=0; p.totalFactoryWagesPaid=0;}
            string preservedTraffic=JsonUtility.ToJson(v5.traffic);
            v5=JsonUtility.FromJson<CityModel>(JsonUtility.ToJson(v5)); v5.EnableHouseholds();
            if(!v5.Valid() || v5.version!=7 || preservedTraffic!=JsonUtility.ToJson(v5.traffic) || v5.buildings.Any(b=>b.factory!=null && b.factory.raw!=0))
                throw new Exception("v5 industry migration changed existing stock or journeys");
            string result="PASS: v7 JSON round-trip, partial-day time, deterministic 20-day continuation, resident transport save continuation, v4 job/journey migration. Live city ledger: "+(game!=null?"passed":"skipped (not in Play mode)");
            Directory.CreateDirectory("Temp"); File.WriteAllText("Temp/CityHouseholdChecks.txt",result); Debug.Log(result);
        }
    }
}
