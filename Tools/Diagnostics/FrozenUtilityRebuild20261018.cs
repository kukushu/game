// Frozen original full utility rebuild from 0f13682e81c181c7c7f11bb13e3d87af655cf031; test-only.
namespace HarborCity { using System;using System.Collections.Generic;using System.Linq; public sealed partial class CityModel {
        internal void FrozenRecalculateUtilities20261018()
        {
            supply=buildings.ToDictionary(b=>b.id,b=>new BuildingSupply());WaterDemand=SewageCapacity=0;if(!development.enabled)return;
            // One aggregation of actual people/job occupancy per rebuild. Calling
            // Occupancy/EmployedAt from every component was repeated O(B*(P+J)).
            var peopleByHome=new Dictionary<int,int>();foreach(var h in society.families)if(h.resident){peopleByHome.TryGetValue(h.home,out int n);peopleByHome[h.home]=n+LivingMembers(h);}
            var workersByBuilding=new Dictionary<int,int>();foreach(var job in society.jobEntities)if(job.occupiedCitizenId>=0){workersByBuilding.TryGetValue(job.buildingId,out int n);workersByBuilding[job.buildingId]=n+1;}
            var loads=new Dictionary<int,int>();foreach(var b in buildings){peopleByHome.TryGetValue(b.id,out int people);workersByBuilding.TryGetValue(b.id,out int workers);loads.Add(b.id,b.Use==LandUse.Residential?Math.Max(1,people):b.Use==LandUse.Commercial||b.Use==LandUse.Industrial?Math.Max(1,workers*2):b.Use==LandUse.Power?0:2);}
            demand=loads.Values.Sum();
            WaterDemand=buildings.Where(b=>CityZoningState.ZoneUse(b.Use)).Sum(b=>loads[b.id]);
            var powerNet=utilities.electricity;var components=powerNet.Components();
            var adjacency=buildings.ToDictionary(b=>b.id,b=>new List<int>());
            // Maximum validated footprint is 8.9; 16-unit buckets plus adjacent
            // buckets cover every pair within the 6-unit transfer radius.
            var buckets=new Dictionary<(int,int),List<CityBuilding>>();
            foreach(var b in buildings)
            {var key=((int)Math.Floor(b.x/16),(int)Math.Floor(b.z/16));if(!buckets.ContainsKey(key))buckets[key]=new List<CityBuilding>();buckets[key].Add(b);}
            foreach(var b in buildings)
            {
                int x=(int)Math.Floor(b.x/16),z=(int)Math.Floor(b.z/16);
                for(int dx=-1;dx<=1;dx++)for(int dz=-1;dz<=1;dz++)
                    if(buckets.TryGetValue((x+dx,z+dz),out var neighbours))foreach(var other in neighbours)
                        if(other.id>b.id && FootprintDistance(b,other)<=ElectricityReach)
                        {adjacency[b.id].Add(other.id);adjacency[other.id].Add(b.id);}
            }
            var touched=new Dictionary<int,List<int>>();
            {
#if CITY_CORE_ATTRIBUTION
            using var powerCoverageObservation=CityCoreProfile.Measure(CityCoreProfile.Part.UtilityPowerCoverage);
#endif
            var coverage=new CityUtilityCoverage(powerNet);var candidates=new List<UtilityEdge>();var covered=new HashSet<int>();
            foreach(var b in buildings)
            {
                coverage.Query(b,ElectricityReach,candidates);covered.Clear();
                foreach(var edge in candidates)if(LineDistance(b,powerNet,edge)<=ElectricityReach)
                {int component=components[edge.a];if(!covered.Add(component))continue;if(!touched.ContainsKey(component))touched[component]=new List<int>();touched[component].Add(b.id);}
            }
            }
            foreach(var group in touched.Values)for(int i=1;i<group.Count;i++)
            {adjacency[group[0]].Add(group[i]);adjacency[group[i]].Add(group[0]);}
            var visited=new HashSet<int>();power=0;
            foreach(var b in buildings)
            {
                if(visited.Contains(b.id))continue;var group=new List<int>();var queue=new Queue<int>();queue.Enqueue(b.id);visited.Add(b.id);
                while(queue.Count>0){int id=queue.Dequeue();group.Add(id);foreach(int next in adjacency[id])if(visited.Add(next))queue.Enqueue(next);}
                int capacity=group.Count(id=>GetBuilding(id) is PowerBuilding && BuildingAccess(id))*ServiceCapacity(CityServiceKind.Electricity,160);
                power+=capacity;
                foreach(int id in AllocateUtility(group,capacity,id=>loads[id]))supply[id].electricity=true;
            }
            var waterNet=utilities.water;var pipeComponents=waterNet.Components();water=0;
            var pipeBuildings=new Dictionary<int,List<int>>();var countedSources=new HashSet<int>();var countedDrains=new HashSet<int>();
            {
#if CITY_CORE_ATTRIBUTION
            using var waterCoverageObservation=CityCoreProfile.Measure(CityCoreProfile.Part.UtilityWaterCoverage);
#endif
            var coverage=new CityUtilityCoverage(waterNet);var candidates=new List<UtilityEdge>();var covered=new HashSet<int>();
            foreach(var b in buildings)
            {
                float reach=b.Use==LandUse.Water||b.Use==LandUse.Sewage?1:PipeReach;coverage.Query(b,reach,candidates);covered.Clear();
                foreach(var edge in candidates)if(LineDistance(b,waterNet,edge)<=reach)
                {int component=pipeComponents[edge.a];if(!covered.Add(component))continue;if(!pipeBuildings.ContainsKey(component))pipeBuildings[component]=new List<int>();pipeBuildings[component].Add(b.id);}
            }
            }
            foreach(var pair in pipeBuildings)
            {
                var group=pair.Value;
                int fresh=group.Count(id=>GetBuilding(id) is WaterBuilding && supply[id].electricity && BuildingAccess(id))*ServiceCapacity(CityServiceKind.Water,160);
                int drain=group.Count(id=>GetBuilding(id) is SewageBuilding && supply[id].electricity && BuildingAccess(id))*ServiceCapacity(CityServiceKind.Water,160);
                water+=group.Where(id=>GetBuilding(id) is WaterBuilding && supply[id].electricity && BuildingAccess(id) && countedSources.Add(id)).Count()*ServiceCapacity(CityServiceKind.Water,160);int load=group.Where(id=>CityZoningState.ZoneUse(UseOf(id))).Sum(id=>loads[id]);
                SewageCapacity+=group.Count(id=>GetBuilding(id) is SewageBuilding && supply[id].electricity && BuildingAccess(id) && countedDrains.Add(id))*ServiceCapacity(CityServiceKind.Water,160);
                var status=new PipeSupply{id=pair.Key,load=load,fresh=fresh,drain=drain};
                status.inactiveDrains.AddRange(group.Where(id=>GetBuilding(id) is SewageBuilding && (!supply[id].electricity || !BuildingAccess(id))));
                foreach(int id in group)supply[id].pipes.Add(status);
                var watered=AllocateUtility(group,fresh,id=>CityZoningState.ZoneUse(UseOf(id))?loads[id]:0).ToList();
                foreach(int id in watered)supply[id].water=true;
                foreach(int id in AllocateUtility(watered,drain,id=>CityZoningState.ZoneUse(UseOf(id))?loads[id]:0))supply[id].sewage=true;
            }
        }
}}
