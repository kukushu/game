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
            internal HarborCity.RoadCore.Network roads;
            internal readonly List<List<int>> powerGroups=new List<List<int>>();
            internal readonly Dictionary<int,List<int>> pipeGroups=new Dictionary<int,List<int>>();
        }
        [NonSerialized] UtilityLayout utilityLayout;
        // The layout owns only static building/component membership. An isolated
        // road with no possible building coverage or manual-conduit contact adds
        // an unused component; existing group IDs/order and membership are exact.
        // Refresh STILL recalculates all current loads, budgets and capacities.
        internal bool RetainIndependentRoadSupplyLayout(HarborCity.RoadCore.Network before,HarborCity.RoadCore.Network after,int road)
        {
            var layout=utilityLayout;
            if(!utilities.roadConduits||layout==null||!ReferenceEquals(layout.roads,before)||!ReferenceEquals(layout.power,utilities.electricity)||!ReferenceEquals(layout.water,utilities.water))return false;
            var line=after.Centreline(road);double minX=double.PositiveInfinity,minZ=minX,maxX=double.NegativeInfinity,maxZ=maxX;
            for(int i=0;i<line.Count;i++)
            {
                var p=line[i];if(Math.Abs(p.x)>1000000||Math.Abs(p.z)>1000000)return false; // bound float projection roundoff below conservative 1m guard
                minX=Math.Min(minX,p.x);maxX=Math.Max(maxX,p.x);minZ=Math.Min(minZ,p.z);maxZ=Math.Max(maxZ,p.z);
            }
            foreach(var b in buildings)
            {
                double reach=Math.Max(ElectricityReach,PipeReach)+Math.Max(b.width,b.depth)/2+1;
                if(b.x+reach>=minX&&b.x-reach<=maxX&&b.z+reach>=minZ&&b.z-reach<=maxZ)return false;
            }
            foreach(var net in new[]{utilities.electricity,utilities.water})foreach(var edge in net.edges)
            {
                var a=net.Node(edge.a);var b=net.Node(edge.b);if(a==null||b==null)return false;
                // Covers crossing, touching, collinear and grade-separated manual
                // pipes: the original utility projection connects in XZ, not Y.
                if(Math.Max(a.x,b.x)+1>=minX&&Math.Min(a.x,b.x)-1<=maxX&&Math.Max(a.z,b.z)+1>=minZ&&Math.Min(a.z,b.z)-1<=maxZ)return false;
            }
            // SupplyLayout subsequently checks the complete original content key.
            // DTO edits, changed footprints/manual data or flags still rebuild.
            layout.roads=after;return true;
        }
        IEnumerable<long> UtilityLayoutKey()
        {
            yield return utilities.roadConduits?1:0;
            yield return buildings.Count;
            foreach(var b in buildings){yield return b.id;yield return (int)b.Use;yield return BitConverter.SingleToInt32Bits(b.x);yield return BitConverter.SingleToInt32Bits(b.z);yield return BitConverter.SingleToInt32Bits(b.width);yield return BitConverter.SingleToInt32Bits(b.depth);}
            foreach(var net in new[]{utilities.electricity,utilities.water})
            {
                yield return net.nodes.Count;yield return net.edges.Count;
                // An unused node may have been replaced in the public DTO before
                // the network's lookup was refreshed. Cold coverage never reads
                // unused endpoints, so its signature must tolerate that case too.
                foreach(var n in net.nodes){var resolved=net.Node(n.id)??n;yield return n.id;yield return BitConverter.SingleToInt32Bits(resolved.x);yield return BitConverter.SingleToInt32Bits(resolved.y);yield return BitConverter.SingleToInt32Bits(resolved.z);}
                foreach(var e in net.edges){yield return e.id;yield return e.a;yield return e.b;}
            }
        }
        UtilityLayout SupplyLayout()
        {
            var roads=utilities.roadConduits?RoadUtilitySource?.Invoke():null;var old=utilityLayout;
            if(old!=null&&ReferenceEquals(old.roads,roads)&&ReferenceEquals(old.power,utilities.electricity)&&ReferenceEquals(old.water,utilities.water))
            {
                int at=0;bool same=true;foreach(long value in UtilityLayoutKey()){if(at>=old.key.Length||old.key[at++]!=value){same=false;break;}}
                if(same&&at==old.key.Length)return old;
            }
            var layout=new UtilityLayout{power=utilities.electricity,water=utilities.water,roads=roads};
            var roadPower=roads==null?null:new CityRoadUtilityConduits(roads,utilities.electricity);
            var powerNet=roadPower?.network??utilities.electricity;var components=roadPower?.components??powerNet.Components();
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
            var roadWater=roads==null?null:new CityRoadUtilityConduits(roads,utilities.water);
            var waterNet=roadWater?.network??utilities.water;var pipeComponents=roadWater?.components??waterNet.Components();
            var pipeBuildings=layout.pipeGroups;
            {
#if CITY_CORE_ATTRIBUTION
            using var waterCoverageObservation=CityCoreProfile.Measure(CityCoreProfile.Part.UtilityWaterCoverage);
#endif
            var coverage=new CityUtilityCoverage(waterNet);var candidates=new List<UtilityEdge>();var covered=new HashSet<int>();
            foreach(var b in buildings)
            {
                bool facility=b.Use==LandUse.Water||b.Use==LandUse.Sewage;
                float reach=facility&&roadWater==null?1:PipeReach;coverage.Query(b,reach,candidates);covered.Clear();
                // One automatic service drop per source/outlet: a plant beside two
                // disconnected roads must not duplicate its capacity into both systems.
                int roadComponent=-1;float bestRoadDistance=float.PositiveInfinity;
                if(facility&&roadWater!=null)foreach(var edge in candidates)if(edge.id>=roadWater.firstRoadEdge)
                {float distance=LineDistance(b,waterNet,edge);if(distance<=PipeReach&&distance<bestRoadDistance){bestRoadDistance=distance;roadComponent=pipeComponents[edge.a];}}
                foreach(var edge in candidates)if((!facility||roadWater==null||edge.id<roadWater.firstRoadEdge||pipeComponents[edge.a]==roadComponent)&&LineDistance(b,waterNet,edge)<=(facility&&(roadWater==null||edge.id<roadWater.firstRoadEdge)?1:PipeReach))
                {int component=pipeComponents[edge.a];if(!covered.Add(component))continue;if(!pipeBuildings.ContainsKey(component))pipeBuildings[component]=new List<int>();pipeBuildings[component].Add(b.id);}
            }
            }
            layout.key=UtilityLayoutKey().ToArray();utilityLayout=layout;return layout;
        }
    }
}
