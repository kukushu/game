using System.Linq;
using UnityEngine;

namespace HarborCity
{
    public sealed partial class HarborCityGame
    {
        void DrawPoliceInspector(float h,Color navy)
        {
            int id=inspectedHome;Panel(new Rect(24,120,310,h-320),navy);GUILayout.BeginArea(new Rect(38,132,282,h-345));
            GUILayout.BeginHorizontal();GUILayout.Label("警局 #"+id,title);if(GUILayout.Button("关闭",button,GUILayout.Width(60)))inspectedHome=-1;GUILayout.EndHorizontal();
            GUILayout.Label(city.BuildingConditionLabel(id),small);GUILayout.Label("出勤警车 "+city.PoliceCarsAt(id)+" / "+city.PoliceFleetLimit,label);
            GUILayout.Label("治安待处理建筑 "+city.CrimeBuildings+"\n平均犯罪积压 "+city.AverageCrime.ToString("F1"),label);
            GUILayout.Label("警车实际到达建筑后处理；拥堵、断路和设施停供会延误服务。",small);
            foreach(var t in city.traffic.trips.Where(t=>t.purpose==TripPurpose.PoliceResponse && t.home==id))GUILayout.Label("警车 #"+t.id+" · "+(t.returning?"返程":t.status==TripStatus.Visiting?"现场处理":"出勤途中")+"\n目的地 "+EndpointName(t.destination),small);
            GUILayout.EndArea();
        }
    }
}
