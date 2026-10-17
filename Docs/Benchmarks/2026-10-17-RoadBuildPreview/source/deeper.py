from pathlib import Path
p=Path('Temp/RoadCoreUnity/Assets/Scripts/Runtime/Traffic/CityRoadCoreReservations.cs')
s=p.read_text(encoding='utf-8-sig')
s=s.replace('public ConnectionConflicts(Network network)\n        {','public ConnectionConflicts(Network network)\n        {using var roadProbeScope=new global::HarborCity.RoadBuildProbeScope("ConnectionConflicts");',1)
needle='            foreach(var bucket in local.Values)for(int i=0;i<bucket.Count;i++)for(int j=i+1;j<bucket.Count;j++)if(Overlap(protectedPaths[bucket[i].Id],protectedPaths[bucket[j].Id])){conflicts[bucket[i].Id].Add(bucket[j].Id);conflicts[bucket[j].Id].Add(bucket[i].Id);}'
assert needle in s
s=s.replace(needle,'            using(var pairsScope=new global::HarborCity.RoadBuildProbeScope("ConflictPairs")){'+needle.strip()+'}')
p.write_text(s,encoding='utf-8')
print('Added constructor and pair-testing markers only in isolated source')
