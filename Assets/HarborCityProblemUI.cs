using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HarborCity
{
    public sealed partial class HarborCityGame
    {
        static readonly string[] problemNames={"缺电","缺水","排污不足","垃圾积压","道路断开","火灾","废弃","等待灵车","犯罪积压","处理场满"};
        static readonly string[] problemGlyphs={"电","水","污","垃","路","火","废","遗","罪","满"};
        List<BuildingProblem> problemSnapshot=new List<BuildingProblem>();
        readonly Dictionary<int,int> previousProblemSeverity=new Dictionary<int,int>();
        readonly Dictionary<BuildingProblemKind,int> problemNext=new Dictionary<BuildingProblemKind,int>();
        readonly List<Rect> problemHitRects=new List<Rect>();
        float nextProblemRefresh;
        bool problemInitialized,showProblems;
        string problemNotification="";
        float problemNotificationUntil;
        Rect problemListRect,problemSummaryRect;
        GUIStyle problemBadgeStyle;

        void RefreshBuildingProblems()
        {
            if(Time.unscaledTime<nextProblemRefresh)return;
            nextProblemRefresh=Time.unscaledTime+.5f;
            problemSnapshot=city.BuildingProblems();
            var current=problemSnapshot.GroupBy(p=>p.buildingId).ToDictionary(g=>g.Key,g=>g.Max(p=>p.severity));
            int raised=current.Count(p=>!previousProblemSeverity.TryGetValue(p.Key,out int old) || p.Value>old);
            if(raised>0 && problemInitialized)
            {problemNotification=raised+" 栋建筑出现新问题或情况恶化";problemNotificationUntil=Time.unscaledTime+7;}
            previousProblemSeverity.Clear();foreach(var pair in current)previousProblemSeverity[pair.Key]=pair.Value;
            problemInitialized=true;
        }
        bool OverProblemUI(Vector2 point)=>problemSummaryRect.Contains(point) || problemHitRects.Any(r=>r.Contains(point)) || showProblems && problemListRect.Contains(point);
        void LocateProblem(BuildingProblemKind kind)
        {
            var ids=problemSnapshot.Where(p=>p.kind==kind).Select(p=>p.buildingId).Distinct().OrderBy(id=>id).ToList();
            if(ids.Count==0)return;
            problemNext.TryGetValue(kind,out int previous);
            int id=ids.FirstOrDefault(n=>n>previous);if(id==0)id=ids[0];problemNext[kind]=id;
            selected=LandUse.Empty;inspectedTrip=-1;inspectedHome=id;residenceFamily=-1;inspectedResident=-1;
            residenceSnapshotHome=-1;householdListScroll=Vector2.zero;
            LocateTrafficEndpoint(id,false);
            notice=problemNames[(int)kind]+"：已定位建筑 #"+id+"；再次点击可查看下一栋。";
        }
        string ProblemTooltip(IEnumerable<BuildingProblem> entries)
        {
            var groups=entries.GroupBy(p=>p.buildingId).ToList();
            string text=groups.Count>1?"附近 "+groups.Count+" 栋建筑存在问题\n":"";
            foreach(var group in groups.Take(4))
            {
                var b=city.GetBuilding(group.Key);if(b==null)continue;
                text+=names[(int)b.Use]+" #"+b.id+"\n"+string.Join("\n",group.Select(p=>"· "+p.detail))+"\n";
                if(!b.abandoned && !b.burning && CityZoningState.ZoneUse(b.Use))
                {
                    bool counts=!city.HasBasicServices(b.id) || city.CorpseBacklog(b.id);
                    text+="已记录问题 "+b.outageDays+" 天"+(counts?"；若未解决，再 "+Mathf.Max(0,city.development.provisionalOutageDays-b.outageDays)+" 次每日检查后废弃":"；此提示当前不触发废弃")+"\n";
                }
            }
            if(groups.Count>4)text+="更多建筑请从城市问题列表定位。";
            return text.TrimEnd();
        }
        void DrawBuildingProblems(float scale,float w,float h,Color navy)
        {
            problemHitRects.Clear();
            if(!help && !showSimulation && !undergroundView)
            {
                var visible=new List<KeyValuePair<Vector2,List<BuildingProblem>>>();
                foreach(var group in problemSnapshot.GroupBy(p=>p.buildingId))
                {
                    if(!visuals.TryGetValue(group.Key,out var view) || view==null)continue;
                    var bounds=view.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).ToArray();
                    float top=bounds.Length>0?bounds.Max(b=>b.max.y):view.transform.position.y+3;
                    var screen=cam.WorldToScreenPoint(new Vector3(view.transform.position.x,top+1,view.transform.position.z));
                    if(screen.z<=0)continue;
                    var pos=new Vector2(screen.x/scale,(Screen.height-screen.y)/scale);
                    if(pos.x<350 || pos.x>w-290 || pos.y<115 || pos.y>h-190)continue;
                    visible.Add(new KeyValuePair<Vector2,List<BuildingProblem>>(pos,group.ToList()));
                }
                bool combine=zoom>=40 || visible.Count>35;
                var clusters=visible.GroupBy(p=>combine?new Vector2(Mathf.Floor(p.Key.x/64),Mathf.Floor(p.Key.y/48)):p.Key);
                string hover=null;
                foreach(var cluster in clusters)
                {
                    var entries=cluster.SelectMany(p=>p.Value).ToList();
                    float x=cluster.Average(p=>p.Key.x),y=cluster.Average(p=>p.Key.y);
                    var kinds=entries.Select(p=>p.kind).Distinct().ToList();
                    int count=entries.Select(p=>p.buildingId).Distinct().Count();
                    string caption=count>1?"! "+count:string.Join(" ",kinds.Select(k=>problemGlyphs[(int)k]));
                    float width=count>1?48:Mathf.Max(28,kinds.Count*24);
                    var rect=new Rect(x-width/2,y-25,width,25);
                    problemHitRects.Add(rect);
                    int severity=entries.Max(p=>p.severity);
                    Panel(rect,severity>=3?new Color(.75f,.16f,.17f,.96f):severity==2?new Color(.8f,.32f,.1f,.96f):new Color(.58f,.43f,.09f,.96f));
                    if(problemBadgeStyle==null)problemBadgeStyle=new GUIStyle(label){alignment=TextAnchor.MiddleCenter,fontSize=15};
                    GUI.Label(rect,caption,problemBadgeStyle);
                    if(rect.Contains(Event.current.mousePosition))hover=ProblemTooltip(entries);
                }
                if(hover!=null)
                {
                    float height=small.CalcHeight(new GUIContent(hover),330)+18;
                    var tip=new Rect(Mathf.Clamp(Event.current.mousePosition.x+14,350,Mathf.Max(350,w-650)),Mathf.Clamp(Event.current.mousePosition.y+16,115,Mathf.Max(115,h-195-height)),350,height);
                    Panel(tip,navy);GUI.Label(new Rect(tip.x+10,tip.y+8,330,height-16),hover,small);
                }
            }
        }
        void DrawProblemSummary(float w,Color navy)
        {
            int count=problemSnapshot.Select(p=>p.buildingId).Distinct().Count();
            problemSummaryRect=new Rect(w-254,544,210,28);
            if(GUI.Button(problemSummaryRect,"建筑问题 "+count+" 栋 · "+(showProblems?"收起":"查看"),button))showProblems=!showProblems;
            if(Time.unscaledTime<problemNotificationUntil)
                GUI.Label(new Rect(w-580,579,535,24),problemNotification,small);
            problemListRect=new Rect(w-574,120,286,70+problemSnapshot.Select(p=>p.kind).Distinct().Count()*30);
            if(!showProblems)return;
            Panel(problemListRect,navy);
            GUI.Label(new Rect(problemListRect.x+12,132,262,26),"城市问题 · 点击依次定位",label);
            float y=166;
            foreach(var group in problemSnapshot.GroupBy(p=>p.kind).OrderBy(g=>g.Key))
            {
                if(GUI.Button(new Rect(problemListRect.x+12,y,262,26),problemNames[(int)group.Key]+" · "+group.Select(p=>p.buildingId).Distinct().Count()+" 栋",button))LocateProblem(group.Key);
                y+=30;
            }
            if(count==0)GUI.Label(new Rect(problemListRect.x+12,y,262,24),"当前没有建筑问题",small);
        }
    }
}
