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
        [MenuItem("Harbor/Validate household simulation")]
        public static void Validate()
        {
            var empty=CityModel.Create(); new CityTraffic(empty); empty.EnableRoads((x,z)=>1);
            empty.EnableBuildings(); empty.EnableHouseholds(); empty.EnableResidentTransport();
            empty=JsonUtility.FromJson<CityModel>(JsonUtility.ToJson(empty));
            if(!empty.Valid() || empty.population!=0 || empty.society.families.Count!=0 || empty.roads.edges.Count!=0 || empty.tiles.Any(t=>t!=0))
                throw new Exception("Empty new-game save round-trip failed");
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
                if(!live.Valid() || live.version!=5) throw new Exception("Live household model invalid");
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
            if(JsonUtility.ToJson(transport.society)!=JsonUtility.ToJson(transportCopy.society) || JsonUtility.ToJson(transport.traffic)!=JsonUtility.ToJson(transportCopy.traffic))
                throw new Exception("Resident transport diverged after reload");
            string result="PASS: v5 JSON round-trip, partial-day time, deterministic 20-day continuation, resident transport save continuation, v4 job/journey migration. Live city ledger: "+(game!=null?"passed":"skipped (not in Play mode)");
            Directory.CreateDirectory("Temp"); File.WriteAllText("Temp/CityHouseholdChecks.txt",result); Debug.Log(result);
        }
    }
}
