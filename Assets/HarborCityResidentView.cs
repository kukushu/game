using UnityEngine;
using System.Collections.Generic;

namespace HarborCity
{
    public sealed partial class HarborCityGame
    {
        int inspectedResident=-1;
        bool followResident;
        LineRenderer residentMarker, residentRoute;
        string populationSearch="";
        int populationFilter, populationPage;
        sealed class PopulationRow
        {
            public Household family;
            public CityResident person;
            public ResidentActivity activity;
            public string location, destination;
            public int category;
        }
        void OpenPopulationPanel()
        {
            city.EnsureResidents(); showSimulation=true; simulationTab=5;
            simulationScroll=Vector2.zero; simulationTyping=false;
        }
        string PopulationPlace(int id) => id<0 || id>=city.tiles.Length?"无":city.tiles[id]==0?"已拆建筑 #"+id:EndpointName(id);
        void SelectPopulationResident(PopulationRow row,bool follow)
        {
            selected=LandUse.Empty; inspectedHome=row.family.home; inspectedResident=row.person.id;
            inspectedTrip=-1; commuteFamily=-1; followResident=follow;
            showSimulation=false; simulationTyping=false; householdListScroll=Vector2.zero;
            focus=new Vector3(row.activity.x,0,row.activity.z); zoom=24; UpdateCamera(); UpdateResidentView();
            notice="已选中 "+row.person.name+"（居民 #"+row.person.id+"），青色标记显示当前位置。";
        }
        void DrawPopulationPanel()
        {
            city.EnsureResidents();
            var rows=new List<PopulationRow>(); var counts=new int[5];
            foreach(var family in city.society.families)
            {
                if(!family.resident) continue;
                foreach(var person in family.people)
                {
                    var a=city.ObserveResident(family,person);
                    int category=!a.located || a.state.Contains("等待") || a.state.Contains("排队") || a.state.Contains("中断")?4:a.travelling?3:person.atWork?2:1;
                    counts[category]++;
                    string location=a.building>=0?PopulationPlace(a.building):a.located?"道路上（"+a.x.ToString("F1")+", "+a.z.ToString("F1")+"）":"暂无可定位位置";
                    if(a.travelling && person.tripId>0) location="车辆 #"+person.tripId+" · "+location;
                    rows.Add(new PopulationRow{family=family,person=person,activity=a,location=location,destination=PopulationPlace(a.destination),category=category});
                }
            }
            GUILayout.Label("本城 "+rows.Count+" 人 · 在家 "+counts[1]+" · 工作场所 "+counts[2]+" · 行驶中 "+counts[3]+" · 等待 / 异常 "+counts[4],label);
            GUILayout.Label("显示全部本城居民，位置随模拟更新；城外申请者不计入人口。暂停后可逐一核对。",small);
            GUILayout.BeginHorizontal(); GUILayout.Label("搜索",small,GUILayout.Width(40));
            GUI.SetNextControlName("PopulationSearch");
            string search=GUILayout.TextField(populationSearch,GUILayout.MinWidth(150));
            if(search!=populationSearch) {populationSearch=search; populationPage=0;}
            if(GUILayout.Button("清空",button,GUILayout.Width(65))) {populationSearch=""; populationPage=0; GUI.FocusControl(null);}
            GUILayout.EndHorizontal();
            GUILayout.Label("可搜索姓名、居民编号、家庭编号、建筑编号或车辆编号",small);
            int filter=GUILayout.Toolbar(populationFilter,new[]{"全部","在家","工作场所","行驶中","等待 / 异常"},button);
            if(filter!=populationFilter) {populationFilter=filter; populationPage=0;}
            string query=populationSearch.Trim();
            rows.RemoveAll(row=>populationFilter!=0 && row.category!=populationFilter || query.Length>0 &&
                (row.person.name+" 居民 #"+row.person.id+" 家庭 #"+row.family.id+" "+row.location+" "+row.destination).IndexOf(query,System.StringComparison.OrdinalIgnoreCase)<0);
            const int perPage=30;
            int pages=Mathf.Max(1,(rows.Count+perPage-1)/perPage); populationPage=Mathf.Clamp(populationPage,0,pages-1);
            GUILayout.BeginHorizontal();
            GUI.enabled=populationPage>0; if(GUILayout.Button("上一页",button)) populationPage--;
            GUI.enabled=true; GUILayout.Label("匹配 "+rows.Count+" 人 · "+(populationPage+1)+" / "+pages+" 页",small);
            GUI.enabled=populationPage+1<pages; if(GUILayout.Button("下一页",button)) populationPage++;
            GUI.enabled=true; GUILayout.EndHorizontal();
            if(rows.Count==0) GUILayout.Label("没有匹配的居民。",small);
            for(int i=populationPage*perPage;i<Mathf.Min(rows.Count,(populationPage+1)*perPage);i++)
            {
                var row=rows[i];
                GUILayout.BeginHorizontal();
                GUILayout.Label(row.person.name+" #"+row.person.id+" · "+row.person.age+" 岁 · 家庭 #"+row.family.id,small);
                GUI.enabled=row.activity.located;
                if(GUILayout.Button("定位",button,GUILayout.Width(60))) SelectPopulationResident(row,false);
                if(GUILayout.Button("跟随",button,GUILayout.Width(60))) SelectPopulationResident(row,true);
                GUI.enabled=true; GUILayout.EndHorizontal();
                GUILayout.Label(row.activity.state+" · 当前位置："+row.location+"\n目的地："+row.destination,small);
                GUILayout.Space(8);
            }
        }
        void DrawResidents(Household h)
        {
            GUILayout.Label("家庭成员（点击查看个人）",small);
            if(h.people==null) return;
            foreach(var person in h.people)
            {
                if(GUILayout.Button(person.name+" · "+person.age+" 岁 · "+(person.worker?"劳动者":"家庭成员"),button))
                {inspectedResident=person.id; followResident=false; commuteFamily=-1;}
                if(inspectedResident!=person.id) continue;
                var a=city.ObserveResident(h,person); int minute=(int)city.ResidentMinute;
                GUILayout.Label("居民 #"+person.id+" · "+minute/60+":"+(minute%60).ToString("00")+"\n状态："+a.state+
                    "\n当前位置："+(a.building>=0?names[city.tiles[a.building]]+" #"+a.building:a.located?"道路上":"城外 / 无住所")+
                    "\n目的地："+(a.destination>=0?names[city.tiles[a.destination]]+" #"+a.destination:"暂无出行")+
                    "\n工作："+(person.worker?WorkplaceName(h):"未安排")+
                    "\n日薪：¥"+(person.worker?city.Wage(h.work):0)+"（进入家庭共同预算）\n"+a.reason,small);
                if(a.travelling) GUILayout.Label("行程进度："+(a.progress*100).ToString("F0")+"%",small);
                GUILayout.BeginHorizontal();
                GUI.enabled=a.located;
                if(GUILayout.Button("定位此人",button)) {focus=new Vector3(a.x,0,a.z); zoom=24; UpdateCamera();}
                if(GUILayout.Button(followResident?"停止跟随":"跟随此人",button)) followResident=!followResident;
                GUI.enabled=true;
                GUILayout.EndHorizontal();
                GUILayout.Label("青色标记跟随实际通勤车辆；到达后才进入建筑。工资按实际在岗时间计算。",small);
            }
        }
        void UpdateResidentView()
        {
            var h=city.society?.families.Find(f=>f.resident && f.home==inspectedHome && f.people!=null && f.people.Exists(p=>p.id==inspectedResident));
            if(selected!=LandUse.Empty || h==null)
            {
                if(residentMarker!=null) residentMarker.positionCount=0;
                if(residentRoute!=null) residentRoute.positionCount=0;
                followResident=false; return;
            }
            var person=h.people.Find(p=>p.id==inspectedResident); var a=city.ObserveResident(h,person);
            if(residentMarker==null) residentMarker=CommuteRenderer("Resident location",Color.cyan,.2f);
            if(residentRoute==null) residentRoute=CommuteRenderer("Resident journey",Color.cyan,.12f);
            if(!a.located) {residentMarker.positionCount=residentRoute.positionCount=0; followResident=false; return;}
            Vector3 p=a.building>=0?BuildingMarker(a.building):new Vector3(a.x,landscape.Height(a.x,a.z)+.6f,a.z);
            var actualTrip=traffic.State.trips.Find(t=>t.id==person.tripId && t.residentId==person.id);
            if(actualTrip!=null && a.located) p=TrafficPoint(actualTrip,actualTrip.segment,actualTrip.progress)+Vector3.up*.4f;
            residentMarker.positionCount=5;
            residentMarker.SetPositions(new[]{p+Vector3.left*.6f,p+Vector3.forward*.6f,p+Vector3.right*.6f,p+Vector3.back*.6f,p+Vector3.left*.6f});
            residentRoute.positionCount=a.route.Count;
            for(int i=0;i<a.route.Count;i++) {var n=a.route[i]; residentRoute.SetPosition(i,new Vector3(n.x,Mathf.Max(n.y,landscape.Height(n.x,n.z))+.35f,n.z));}
            if(followResident && !showSimulation && !help && !draggingView)
            {focus=new Vector3(a.x,0,a.z); UpdateCamera();}
        }
    }
}
