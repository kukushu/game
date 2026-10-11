using System.Linq;
namespace HarborCity
{
    public sealed partial class CityModel
    {
        // Direction-only editing leaves geometry, zoning keys and entity IDs intact.
        // Until displacement is implemented, occupied vehicle edges cannot be flipped.
        public bool ChangeRoadDirection(int edgeId,bool oneWay,bool reverse,out string reason)
        {
            var edge=roads.edges.Find(e=>e.id==edgeId);
            if(edge==null || reverse && !edge.oneWay){reason="请选择已有单行道路来反向。";return false;}
            var run=roads.Run(edgeId);var affected=roads.edges.Where(e=>run.Contains(e.id)).ToList();
            if(affected.Any(e=>!CityRoadTypes.Valid(e.kind,oneWay))){reason="当前土路和四车道道路不支持单行，请先选择普通铺装道路。";return false;}
            if(!reverse && affected.All(e=>e.oneWay==oneWay)){reason="该路段已经是所选道路类型。";return false;}
            if(traffic.trips.Any(t=>!t.walking && t.status!=TripStatus.Visiting && t.Current!=t.Next && affected.Any(e=>e.a==t.Current && e.b==t.Next || e.b==t.Current && e.a==t.Next)))
            {reason="路段上有车辆，暂不能改变方向；请等车辆驶离。";return false;}
            foreach(var e in affected){e.oneWay=oneWay;if(reverse){int old=e.a;e.a=e.b;e.b=old;}}
            roads.Changed();Recalculate();reason=reverse?"单行方向已反转。":"道路已改为"+(oneWay?"单行；方向沿原施工起点至终点。":"双向。");
            Trace("road.direction_changed",reason,new CityLogDetail{ids=run});return true;
        }
    }
}
