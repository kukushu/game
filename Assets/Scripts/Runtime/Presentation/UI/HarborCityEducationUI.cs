using System.Linq;
using UnityEngine;

namespace HarborCity
{
    public sealed partial class HarborCityGame
    {
        void DrawSchoolInspector(float h,Color navy)
        {
            var school=city.GetBuilding(inspectedHome) as SchoolBuilding;if(school==null)return;
            Panel(new Rect(24,120,310,h-320),navy);GUILayout.BeginArea(new Rect(38,132,282,h-345));
            GUILayout.BeginHorizontal();GUILayout.Label(CityModel.SchoolName(school.Grade)+" #"+school.id,title);if(GUILayout.Button("关闭",button,GUILayout.Width(60)))inspectedHome=-1;GUILayout.EndHorizontal();
            GUILayout.Label(city.BuildingConditionLabel(school.id),small);
            GUILayout.Label("已入学 "+city.EnrolledAt(school.id)+" / "+city.CapacityAtSchool(school.id)+" · 实际在校 "+city.StudentsAt(school.id),label);
            GUILayout.Label("全城符合本阶段资格 "+city.Citizens.Count(p=>city.SchoolEligible(p,school.Grade)),small);
            GUILayout.Space(10);GUILayout.Label("学位关联真实学生；实际到校并接通服务才累计本阶段学习。患病或缺少服务时暂停上课。",small);
            householdListScroll=GUILayout.BeginScrollView(householdListScroll);
            foreach(var p in city.Citizens.Where(p=>p.schoolId==school.id))
            {var family=city.society.families.First(f=>f.id==p.householdId);GUILayout.Label(p.name+" #"+p.id+" · "+city.ObserveResident(family,p).state+"\n教育 "+p.education+" · 今日学习 "+p.schoolMinutesToday.ToString("F0")+" 分钟\n"+CityModel.SchoolName(school.Grade)+"课程 "+Mathf.Min(100,CityModel.CourseProgress(p,school.Grade)/CityModel.CourseMinutes(school.Grade)*100).ToString("F0")+"%",small);}
            GUILayout.EndScrollView();GUILayout.EndArea();
        }
    }
}
