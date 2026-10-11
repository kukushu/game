using System;
using System.Linq;
namespace HarborCity
{
    public enum RoadKind { Standard, Gravel, FourLane }
    // Provisional CS1 road parameters; unit/time conversion and upgrade refunds
    // remain provisional until verified against a specific vanilla build.
    public static class CityRoadTypes
    {
        public const float CellLength=1.5f;
        public static bool Valid(RoadKind kind,bool oneWay)=>Enum.IsDefined(typeof(RoadKind),kind) && !(kind!=RoadKind.Standard && oneWay);
        public static string Name(RoadKind kind)=>kind==RoadKind.Gravel?"双向土路":kind==RoadKind.FourLane?"四车道双向道路":"普通铺装道路";
        public static float Width(RoadKind kind)=>kind==RoadKind.FourLane?5.4f:CityRoads.Width;
        public static int Lanes(RoadKind kind,bool oneWay)=>kind==RoadKind.FourLane || oneWay?2:1;
        public static float SpeedFactor(RoadKind kind)=>kind==RoadKind.Gravel?.75f:kind==RoadKind.FourLane?1.5f:1;
        public static int CostPerCell(RoadKind kind)=>kind==RoadKind.Gravel?20:kind==RoadKind.FourLane?60:40;
        public static double UpkeepPerCellWeek(RoadKind kind)=>kind==RoadKind.Gravel?.19:kind==RoadKind.FourLane?.8:.32;
        public static int ConstructionCost(float length,RoadKind kind)=>(int)Math.Ceiling(length/CellLength*CostPerCell(kind));
    }
    public sealed partial class CityModel
    {
        public int RoadMaintenance=>(int)Math.Ceiling(roads.edges.Sum(e=>roads.EdgeLength(e.a,e.b)/CityRoadTypes.CellLength*CityRoadTypes.UpkeepPerCellWeek(e.kind)*CityRoads.StructureMultiplier(e.structure))/7);
        public bool PreviewRoadUpgrade(int edgeId,RoadKind kind,bool oneWay,out int cost,out string reason,Func<float,float,float> height=null)
        {
            cost=0;
            if(!CityRoadTypes.Valid(kind,oneWay)){reason="当前土路和四车道道路只支持双向。";return false;}
            var run=roads.Run(edgeId);var affected=roads.edges.Where(e=>run.Contains(e.id)).ToList();
            if(affected.Count==0){reason="请选择已有道路。";return false;}
            if(affected.All(e=>e.kind==kind && e.oneWay==oneWay)){reason="该路段已经是所选类型。";return false;}
            bool widthChanged=affected.Any(e=>CityRoadTypes.Width(e.kind)!=CityRoadTypes.Width(kind));
            if((widthChanged || affected.Any(e=>e.oneWay!=oneWay)) && traffic.trips.Any(t=>t.status!=TripStatus.Visiting && (!t.walking || widthChanged) && affected.Any(e=>Uses(t,e))))
            {reason="路段上有车辆或行人，暂不能改变宽度/方向；请等其驶离。";return false;}
            if(widthChanged)
            {
                if(height==null){reason="宽度升级需要地形校验。";return false;}
                foreach(var e in affected)
                    if(CityRoadTypes.Width(kind)>CityRoadTypes.Width(e.kind) && !RoadWidthClear(roads.Node(e.a),roads.Node(e.b),kind,height,affected.Select(x=>x.id).ToList(),out reason,e.structure!=RoadStructure.Ground))return false;
            }
            double amount=affected.Sum(e=>roads.EdgeLength(e.a,e.b)/CityRoadTypes.CellLength*Math.Max(0,CityRoadTypes.CostPerCell(kind)-CityRoadTypes.CostPerCell(e.kind))*CityRoads.StructureMultiplier(e.structure));
            cost=(int)Math.Ceiling(amount);
            if(money<cost){reason="升级资金不足，需 ¥ "+cost+"。";return false;}
            reason="改为"+CityRoadTypes.Name(kind)+(oneWay?" · 单行":" · 双向")+"，费用 ¥ "+cost+"。";return true;
            bool Uses(TrafficTrip t,RoadEdge e)
            {
                if(e.a==t.Current && e.b==t.Next || e.b==t.Current && e.a==t.Next)return true;
                if(t.segment<=0)return false;
                int previous=t.route[t.segment-1];return e.a==previous && e.b==t.Current || e.b==previous && e.a==t.Current;
            }
        }
        public bool UpgradeRoad(int edgeId,RoadKind kind,bool oneWay,out string reason,Func<float,float,float> height=null)
        {
            if(!PreviewRoadUpgrade(edgeId,kind,oneWay,out int cost,out reason,height))return false;
            var ids=roads.Run(edgeId);bool widthChanged=roads.edges.Any(e=>ids.Contains(e.id) && CityRoadTypes.Width(e.kind)!=CityRoadTypes.Width(kind));
            var oldZones=widthChanged?ZoningCells(height).Where(c=>c.use!=LandUse.Empty).ToList():null;
            foreach(var e in roads.edges.Where(e=>ids.Contains(e.id))){e.kind=kind;e.oneWay=oneWay;}
            money-=cost;roads.Changed();Recalculate();
            if(widthChanged)RemapRoadZones(oldZones,height);
            Trace("road.upgraded",reason,new CityLogDetail{ids=ids,amount=cost});return true;
        }
    }
}
