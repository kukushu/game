using System;
using System.Collections.Generic;
using System.Linq;
#if CITY_CORE_ATTRIBUTION
using HarborCity.RoadCore;
#endif

namespace HarborCity
{
    [Serializable] public sealed class CityUtilityState
    {
        public bool roadConduits;
        public CityUtilityNetwork electricity=new CityUtilityNetwork(),water=new CityUtilityNetwork();
        public bool Valid(float buildExtent=CityModel.BuildHalfSize)=>electricity!=null && water!=null && electricity.Valid(buildExtent) && water.Valid(buildExtent);
    }
    public sealed class BuildingSupply
    {
        public bool electricity,water,sewage;
        public readonly List<PipeSupply> pipes=new List<PipeSupply>();
        public bool Complete=>electricity && water && sewage;
        public string Problem=>!electricity?"未接通电力或电网容量不足":!water?WaterProblem:!sewage?SewageProblem:"水电与排污已接通";
        string WaterProblem=>pipes.Count==0?"未接通水管":pipes.Any(p=>p.fresh>0)?"管网供水容量不足":"管网没有运行中的供水设施";
        string SewageProblem
        {
            get
            {
                var p=pipes.Where(p=>p.fresh>0).OrderByDescending(p=>p.drain-p.load).FirstOrDefault();
                return p==null?"供水未就绪，无法排污":p.drain==0?"管网没有运行中的排污口":"排污容量不足（需求 "+p.load+" / 可用 "+p.drain+"）";
            }
        }
    }
    // Derived per connected component, shared by covered buildings. Never saved
    // as a second source of truth and never combined across disconnected pipes.
    public sealed class PipeSupply {public int id,load,fresh,drain;public readonly List<int> inactiveDrains=new List<int>();}
    public sealed partial class CityModel
    {
        public CityUtilityState utilities=new CityUtilityState();
        [NonSerialized] internal Func<HarborCity.RoadCore.Network> RoadUtilitySource;
        // Distances/capacities are provisional world-scale parameters. Networks,
        // adjacency and independent pipe coverage follow the CS1 base-game manual.
        public const float ElectricityReach=6,PipeReach=6;
        public int WaterDemand {get;private set;}
        // Read-only totals of the existing allocation, in the same game load
        // units as demand. Capacity (power/water) is not delivered supply.
        public int PowerSupplied {get;private set;}
        public int WaterSupplied {get;private set;}
        public int SewageCapacity {get;private set;}
        public int InstalledSewageCapacity=>buildings.Count(b=>b is SewageBuilding)*ServiceCapacity(CityServiceKind.Water,160);
        public string WaterFacilityDetails(int id)
        {
            var b=GetBuilding(id);if(!(b is SewageBuilding) && !(b is WaterBuilding))return "请选择供水或排污设施";
            var s=Supply(id);bool road=BuildingAccess(id),connected=s.pipes.Count>0,running=road && s.electricity && connected;
            string type=b is SewageBuilding?"排污":"供水";
            return "水管："+(connected?"已接通 · 管网 "+string.Join(", ",s.pipes.Select(p=>"#"+p.id)):"未接通")
                +"\n道路："+(road?"已接通":"未接通")+"\n电力："+(s.electricity?"已供电":"未供电或电网容量不足")
                +"\n运行："+(running?"运行中":!road?"停运 · 缺道路":!s.electricity?"停运 · 缺电力":"停运 · 缺水管")
                +"\n本设施可用"+type+"容量："+(running?ServiceCapacity(CityServiceKind.Water,160):0)+" / "+ServiceCapacity(CityServiceKind.Water,160)
                +string.Concat(s.pipes.Select(p=>"\n\n管网 #"+p.id+" · 用水/排污需求 "+p.load+"\n供水容量 "+p.fresh+" · 排污容量 "+p.drain
                    +(p.inactiveDrains.Count>0?"\n停运排污口："+string.Join(", ",p.inactiveDrains.Select(d=>"#"+d)):"")));
        }
        [NonSerialized] Dictionary<int,BuildingSupply> supply=new Dictionary<int,BuildingSupply>();
        public BuildingSupply Supply(int id)
        {return supply!=null && supply.TryGetValue(id,out var result)?result:new BuildingSupply();}
        public CityUtilityNetwork UtilityNetwork(UtilityKind kind)=>kind==UtilityKind.Electricity?utilities.electricity:utilities.water;
        public bool BuildUtility(UtilityKind kind,RoadNode from,RoadNode to,out string error)
        {
            var plan=UtilityNetwork(kind).Plan(from,to,out error,map.buildHalfSize);if(plan==null)return false;
            int cost=(int)Math.Ceiling(CityPlanarGeometry.Length(from,to)*(kind==UtilityKind.Electricity?15:10));
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
        {return Math.Max(0,CityPlanarGeometry.Length(new RoadNode{x=a.x,z=a.z},new RoadNode{x=b.x,z=b.z})-(Math.Max(a.width,a.depth)+Math.Max(b.width,b.depth))/2);}
        static float LineDistance(CityBuilding b,CityUtilityNetwork net,UtilityEdge edge)
        {return Math.Max(0,CityPlanarGeometry.Distance(new RoadNode{x=b.x,z=b.z},net.Node(edge.a),net.Node(edge.b))-Math.Min(b.width,b.depth)/2);}
        // Provisional load shedding, not a claim about CS1's exact allocation algorithm.
        // Keep essential services operating first; stable IDs avoid random daily flicker.
        // Supply whole buildings only, never spending more than this component provides.
        IEnumerable<int> AllocateUtility(IEnumerable<int> ids,int capacity,Func<int,int> load)
        {
            if(capacity<=0)yield break;
            foreach(int id in ids.OrderBy(id=>GetBuilding(id) is WaterBuilding || GetBuilding(id) is SewageBuilding?0:
                CityZoningState.ZoneUse(UseOf(id))?2:1).ThenBy(id=>id))
            {
                int cost=load(id);if(cost>capacity)continue;
                capacity-=cost;yield return id;
            }
        }
        public void RecalculateUtilities()
        {
            supply=buildings.ToDictionary(b=>b.id,b=>new BuildingSupply());WaterDemand=SewageCapacity=PowerSupplied=WaterSupplied=0;if(!development.enabled)return;
            // One aggregation of actual people/job occupancy per rebuild. Calling
            // Occupancy/EmployedAt from every component was repeated O(B*(P+J)).
            var peopleByHome=new Dictionary<int,int>();foreach(var h in society.families)if(h.resident){peopleByHome.TryGetValue(h.home,out int n);peopleByHome[h.home]=n+LivingMembers(h);}
            var workersByBuilding=new Dictionary<int,int>();foreach(var job in society.jobEntities)if(job.occupiedCitizenId>=0){workersByBuilding.TryGetValue(job.buildingId,out int n);workersByBuilding[job.buildingId]=n+1;}
            var loads=new Dictionary<int,int>();foreach(var b in buildings){peopleByHome.TryGetValue(b.id,out int people);workersByBuilding.TryGetValue(b.id,out int workers);loads.Add(b.id,b.Use==LandUse.Residential?Math.Max(1,people):b.Use==LandUse.Commercial||b.Use==LandUse.Industrial?Math.Max(1,workers*2):b.Use==LandUse.Power?0:2);}
            demand=loads.Values.Sum();
            WaterDemand=buildings.Where(b=>CityZoningState.ZoneUse(b.Use)).Sum(b=>loads[b.id]);
            var layout=SupplyLayout();power=0;
            foreach(var group in layout.powerGroups)
            {
                int capacity=group.Count(id=>GetBuilding(id) is PowerBuilding && BuildingAccess(id))*ServiceCapacity(CityServiceKind.Electricity,160);
                power+=capacity;
                foreach(int id in AllocateUtility(group,capacity,id=>loads[id]))
                {if(!supply[id].electricity)PowerSupplied+=loads[id];supply[id].electricity=true;}
            }
            water=0;var pipeBuildings=layout.pipeGroups;var countedSources=new HashSet<int>();var countedDrains=new HashSet<int>();
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
                foreach(int id in watered)
                {if(!supply[id].water&&CityZoningState.ZoneUse(UseOf(id)))WaterSupplied+=loads[id];supply[id].water=true;}
                foreach(int id in AllocateUtility(watered,drain,id=>CityZoningState.ZoneUse(UseOf(id))?loads[id]:0))supply[id].sewage=true;
            }
        }
    }
}
