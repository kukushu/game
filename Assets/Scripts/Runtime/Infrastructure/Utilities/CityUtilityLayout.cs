using System;using System.Collections.Generic;using System.Linq;
#if CITY_CORE_ATTRIBUTION
using HarborCity.RoadCore;
#endif
namespace HarborCity
{
    public sealed partial class CityModel
    {
        // Static supply geometry only, never supply/load/capacity or money facts.
        // Content comparison also handles public DTO edits without revision bumps.
        sealed class UtilityLayout
        {
            internal CityUtilityNetwork power,water;internal long[] key;
            internal readonly List<List<int>> powerGroups=new List<List<int>>();
            internal readonly Dictionary<int,List<int>> pipeGroups=new Dictionary<int,List<int>>();
        }
        [NonSerialized] UtilityLayout utilityLayout;
        IEnumerable<long> UtilityLayoutKey()
        {
            yield return buildings.Count;
            foreach(var b in buildings){yield return b.id;yield return (int)b.Use;yield return BitConverter.SingleToInt32Bits(b.x);yield return BitConverter.SingleToInt32Bits(b.z);yield return BitConverter.SingleToInt32Bits(b.width);yield return BitConverter.SingleToInt32Bits(b.depth);}
            foreach(var net in new[]{utilities.electricity,utilities.water})
            {
                yield return net.nodes.Count;yield return net.edges.Count;
                foreach(var n in net.nodes){var resolved=net.Node(n.id);yield return n.id;yield return BitConverter.SingleToInt32Bits(resolved.x);yield return BitConverter.SingleToInt32Bits(resolved.y);yield return BitConverter.SingleToInt32Bits(resolved.z);}
                foreach(var e in net.edges){yield return e.id;yield return e.a;yield return e.b;}
            }
        }
        UtilityLayout SupplyLayout()
        {
            var old=utilityLayout;
            if(old!=null&&ReferenceEquals(old.power,utilities.electricity)&&ReferenceEquals(old.water,utilities.water))
            {
                int at=0;bool same=true;foreach(long value in UtilityLayoutKey()){if(at>=old.key.Length||old.key[at++]!=value){same=false;break;}}
                if(same&&at==old.key.Length)return old;
            }
            var layout=new UtilityLayout{power=utilities.electricity,water=utilities.water};
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
            var visited=new HashSet<int>();
            foreach(var b in buildings)
            {
                if(visited.Contains(b.id))continue;var group=new List<int>();var queue=new Queue<int>();queue.Enqueue(b.id);visited.Add(b.id);
                while(queue.Count>0){int id=queue.Dequeue();group.Add(id);foreach(int next in adjacency[id])if(visited.Add(next))queue.Enqueue(next);}
                layout.powerGroups.Add(group);
            }
            var waterNet=utilities.water;var pipeComponents=waterNet.Components();
            var pipeBuildings=layout.pipeGroups;
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
            layout.key=UtilityLayoutKey().ToArray();utilityLayout=layout;return layout;
        }
    }
}
