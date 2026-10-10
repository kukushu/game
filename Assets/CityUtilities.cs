using System;
using System.Collections.Generic;
using System.Linq;

namespace HarborCity
{
    [Serializable] public sealed class CityUtilityState
    {
        public CityUtilityNetwork electricity=new CityUtilityNetwork(),water=new CityUtilityNetwork();
        public bool Valid()=>electricity!=null && water!=null && electricity.Valid() && water.Valid();
    }
    public sealed class BuildingSupply
    {
        public bool electricity,water,sewage;
        public bool Complete=>electricity && water && sewage;
        public string Problem=>!electricity?"未接通电力或电网容量不足":!water?"未接通供水或管网容量不足":!sewage?"未接通排污或排污容量不足":"水电与排污已接通";
    }
    public sealed partial class CityModel
    {
        public CityUtilityState utilities=new CityUtilityState();
        // Distances/capacities are provisional world-scale parameters. Networks,
        // adjacency and independent pipe coverage follow the CS1 base-game manual.
        public const float ElectricityReach=6,PipeReach=6;
        [NonSerialized] Dictionary<int,BuildingSupply> supply=new Dictionary<int,BuildingSupply>();
        public BuildingSupply Supply(int id)
        {return supply!=null && supply.TryGetValue(id,out var result)?result:new BuildingSupply();}
        public CityUtilityNetwork UtilityNetwork(UtilityKind kind)=>kind==UtilityKind.Electricity?utilities.electricity:utilities.water;
        public bool BuildUtility(UtilityKind kind,RoadNode from,RoadNode to,out string error)
        {
            var plan=UtilityNetwork(kind).Plan(from,to,out error);if(plan==null)return false;
            int cost=(int)Math.Ceiling(CityRoads.Length(from,to)*(kind==UtilityKind.Electricity?15:10));
            if(money<cost){error="资金不足";return false;}
            if(kind==UtilityKind.Electricity)utilities.electricity=plan;else utilities.water=plan;
            money-=cost;Recalculate();Trace("utility.built",kind+"管线建设",new CityLogDetail{amount=cost,x=to.x,z=to.z});return true;
        }
        public bool RemoveUtility(UtilityKind kind,int edge)
        {
            if(money<40 || !UtilityNetwork(kind).Remove(edge))return false;
            money-=40;Recalculate();Trace("utility.removed",kind+"管线拆除",new CityLogDetail{count=edge});return true;
        }
        static float FootprintDistance(CityBuilding a,CityBuilding b)
        {return Math.Max(0,CityRoads.Length(new RoadNode{x=a.x,z=a.z},new RoadNode{x=b.x,z=b.z})-(Math.Max(a.width,a.depth)+Math.Max(b.width,b.depth))/2);}
        static float LineDistance(CityBuilding b,CityUtilityNetwork net,UtilityEdge edge)
        {return Math.Max(0,CityRoads.Distance(new RoadNode{x=b.x,z=b.z},net.Node(edge.a),net.Node(edge.b))-Math.Min(b.width,b.depth)/2);}
        int UtilityLoad(CityBuilding b)=>b.Use==LandUse.Residential?Math.Max(1,society.families.Where(h=>h.resident && h.home==b.id).Sum(h=>LivingMembers(h))):
            b.Use==LandUse.Commercial || b.Use==LandUse.Industrial?Math.Max(1,EmployedAt(b.id)*2):b.Use==LandUse.Power?0:2;
        public void RecalculateUtilities()
        {
            supply=buildings.ToDictionary(b=>b.id,b=>new BuildingSupply());if(!development.enabled)return;
            demand=buildings.Sum(UtilityLoad);
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
            foreach(var b in buildings)
                foreach(var component in powerNet.edges.Where(e=>LineDistance(b,powerNet,e)<=ElectricityReach).Select(e=>components[e.a]).Distinct())
                {if(!touched.ContainsKey(component))touched[component]=new List<int>();touched[component].Add(b.id);}
            foreach(var group in touched.Values)for(int i=1;i<group.Count;i++)
            {adjacency[group[0]].Add(group[i]);adjacency[group[i]].Add(group[0]);}
            var visited=new HashSet<int>();power=0;
            foreach(var b in buildings)
            {
                if(visited.Contains(b.id))continue;var group=new List<int>();var queue=new Queue<int>();queue.Enqueue(b.id);visited.Add(b.id);
                while(queue.Count>0){int id=queue.Dequeue();group.Add(id);foreach(int next in adjacency[id])if(visited.Add(next))queue.Enqueue(next);}
                int capacity=group.Count(id=>GetBuilding(id) is PowerBuilding && BuildingAccess(id))*ServiceCapacity(CityServiceKind.Electricity,160);
                power+=capacity;int load=group.Sum(id=>UtilityLoad(GetBuilding(id)));
                foreach(int id in group)supply[id].electricity=capacity>0 && capacity>=load;
            }
            var waterNet=utilities.water;var pipeComponents=waterNet.Components();water=0;
            var pipeBuildings=new Dictionary<int,List<int>>();var countedSources=new HashSet<int>();
            foreach(var b in buildings)
                foreach(int component in waterNet.edges.Where(e=>LineDistance(b,waterNet,e)<=(b.Use==LandUse.Water || b.Use==LandUse.Sewage?1:PipeReach)).Select(e=>pipeComponents[e.a]).Distinct())
                {if(!pipeBuildings.ContainsKey(component))pipeBuildings[component]=new List<int>();pipeBuildings[component].Add(b.id);}
            foreach(var group in pipeBuildings.Values)
            {
                int fresh=group.Count(id=>GetBuilding(id) is WaterBuilding && supply[id].electricity && BuildingAccess(id))*ServiceCapacity(CityServiceKind.Water,160);
                int drain=group.Count(id=>GetBuilding(id) is SewageBuilding && supply[id].electricity && BuildingAccess(id))*ServiceCapacity(CityServiceKind.Water,160);
                water+=group.Where(id=>GetBuilding(id) is WaterBuilding && supply[id].electricity && BuildingAccess(id) && countedSources.Add(id)).Count()*ServiceCapacity(CityServiceKind.Water,160);int load=group.Where(id=>CityZoningState.ZoneUse(UseOf(id))).Sum(id=>UtilityLoad(GetBuilding(id)));
                foreach(int id in group){supply[id].water|=fresh>0 && fresh>=load;supply[id].sewage|=fresh>0 && fresh>=load && drain>0 && drain>=load;}
            }
        }
    }
}
