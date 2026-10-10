using System.Linq;
using UnityEngine;

namespace HarborCity
{
    public sealed partial class HarborCityGame
    {
        void DrawClinicInspector(float h,Color navy)
        {
            var clinic=city.GetBuilding(inspectedHome) as ClinicBuilding;if(clinic==null)return;
            Panel(new Rect(24,120,310,h-320),navy);GUILayout.BeginArea(new Rect(38,132,282,h-345));
            GUILayout.BeginHorizontal();GUILayout.Label("诊所 #"+clinic.id,title);if(GUILayout.Button("关闭",button,GUILayout.Width(60)))inspectedHome=-1;GUILayout.EndHorizontal();
            GUILayout.Label(city.BuildingAccess(clinic.id)?city.Supply(clinic.id).Problem:"道路未接通",small);
            GUILayout.Label("治疗中 "+city.ClinicPatients(clinic.id)+" · 已预约 "+city.ClinicReservations(clinic.id)+" / "+city.ClinicCapacity,label);
            GUILayout.Label("出勤救护车 "+city.ClinicAmbulances(clinic.id)+" / "+city.ClinicFleetLimit,label);
            GUILayout.Label("全城患病 "+city.SickResidents+" 人 · 平均健康 "+city.AverageHealth.ToString("F0"),small);
            GUILayout.Space(10);GUILayout.Label("实际到院并接通服务才治疗；暂无救护车时尝试步行。道路或供给中断会延误接送与治疗。",small);
            householdListScroll=GUILayout.BeginScrollView(householdListScroll);
            foreach(var p in city.Citizens.Where(p=>p.medicalClinicId==clinic.id))
            {var family=city.society.families.First(f=>f.id==p.householdId);GUILayout.Label(p.name+" #"+p.id+" · "+city.ObserveResident(family,p).state+"\n健康 "+p.health.ToString("F0")+" · 已治疗 "+p.treatmentMinutes.ToString("F0")+" 分钟",small);}
            GUILayout.EndScrollView();GUILayout.EndArea();
        }
    }
}
