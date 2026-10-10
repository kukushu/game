using System.Linq;
using UnityEngine;

namespace HarborCity
{
    public sealed partial class HarborCityGame
    {
        void DrawCemeteryInspector(float h,Color navy)
        {
            var cemetery=city.GetBuilding(inspectedHome) as CemeteryBuilding;if(cemetery==null)return;
            Panel(new Rect(24,120,310,h-320),navy);GUILayout.BeginArea(new Rect(38,132,282,h-345));
            GUILayout.BeginHorizontal();GUILayout.Label("墓地 #"+cemetery.id,title);if(GUILayout.Button("关闭",button,GUILayout.Width(60)))inspectedHome=-1;GUILayout.EndHorizontal();
            GUILayout.Label(city.BuildingConditionLabel(cemetery.id),small);
            GUILayout.Label("已安葬 "+city.BuriedAt(cemetery.id)+" / "+CemeteryBuilding.Capacity,label);
            GUILayout.Label("占用与在途预留合计 "+city.CemeteryReserved(cemetery.id)+"\n实际出勤灵车 "+city.HearsesAt(cemetery.id)+" / "+city.HearseFleetLimit,small);
            GUILayout.Label("全城未收取 "+city.deathcare.bodies.Count(b=>b.stage==CorpseStage.Waiting || b.stage==CorpseStage.Assigned)+" · 运送中 "+city.deathcare.bodies.Count(b=>b.stage==CorpseStage.InTransit),small);
            householdListScroll=GUILayout.BeginScrollView(householdListScroll);
            foreach(var body in city.deathcare.bodies.Where(b=>b.cemeteryId==cemetery.id))
            {var p=city.AllResidents.First(r=>r.id==body.citizenId);GUILayout.Label(p.name+" #"+p.id+" · "+city.ObserveCorpse(p).state+"\n死亡日期 第 "+body.diedDay+" 天"+(body.tripId>0?" · 灵车 #"+body.tripId:""),small);}
            GUILayout.EndScrollView();GUILayout.EndArea();
        }
    }
}
