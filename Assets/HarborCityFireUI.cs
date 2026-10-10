using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HarborCity
{
    public sealed partial class HarborCityGame
    {
        readonly Dictionary<int,Transform> flameViews=new Dictionary<int,Transform>();
        void RefreshFireView()
        {
            foreach(int id in flameViews.Keys.ToList())if(city.GetBuilding(id)?.burning!=true){if(flameViews[id]!=null)Destroy(flameViews[id].gameObject);flameViews.Remove(id);}
            foreach(var b in city.buildings.Where(b=>b.burning))
            {
                if(!flameViews.TryGetValue(b.id,out var view) || view==null)
                {view=Box("Active fire #"+b.id,new Vector3(b.x,landscape.Height(b.x,b.z)+2,b.z),new Vector3(b.width*.3f,4,b.depth*.3f),new Color(1,.4f,.08f),world).transform;flameViews[b.id]=view;}
                view.localScale=new Vector3(b.width*.3f,3+b.fireIntensity,b.depth*.3f);
            }
        }
        void DrawFireInspector(float h,Color navy)
        {
            int id=inspectedHome;Panel(new Rect(24,120,310,h-320),navy);GUILayout.BeginArea(new Rect(38,132,282,h-345));
            GUILayout.BeginHorizontal();GUILayout.Label("消防站 #"+id,title);if(GUILayout.Button("关闭",button,GUILayout.Width(60)))inspectedHome=-1;GUILayout.EndHorizontal();
            GUILayout.Label(city.BuildingConditionLabel(id),small);GUILayout.Label("出勤消防车 "+city.FireEnginesAt(id)+" / "+city.FireFleetLimit,label);
            GUILayout.Label("全城火灾 "+city.ActiveFires+" · 烧毁建筑 "+city.BurnedBuildings,label);
            GUILayout.Label("车辆实际到场后才处置。道路拥堵、中断或消防站服务不足都会延误救援。",small);
            foreach(var trip in city.traffic.trips.Where(t=>t.purpose==TripPurpose.FireResponse && t.home==id))GUILayout.Label("消防车 #"+trip.id+" · "+(trip.returning?"返程":trip.status==TripStatus.Visiting?"到场处置":"救援途中")+"\n目的地 "+EndpointName(trip.destination),small);
            GUILayout.EndArea();
        }
    }
}
