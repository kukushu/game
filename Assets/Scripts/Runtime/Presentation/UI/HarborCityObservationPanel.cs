using System;
using UnityEngine;
using Unity.Profiling;
namespace HarborCity
{
    // Presentation owns selection/scroll/throttling only. Snapshots contain no live facts.
    public sealed class HarborCityObservationPanel
    {
        static readonly ProfilerMarker refreshSample=new ProfilerMarker("City.Views.Observation.Refresh");
        readonly CityLocalization text;CityCoreIndustrySession session;CityCoreObservation query;
        BuildingInspection shown;System.Collections.Generic.IReadOnlyList<BuildingIssue> issues;
        int selected,family,issuePage;double refreshDeadline;long refreshed=-1;int entityRevision=-1,roadRevision=-1;Vector2 scroll;bool showIssues;
        public Action<float,float> Focus;
        public int Selected=>selected;
        Rect Bounds=>new Rect(Math.Max(8,Screen.width-380),152,Math.Min(372,Screen.width-16),Math.Max(60,Screen.height-312));
        public HarborCityObservationPanel(CityLocalization text){this.text=text;}
        public bool BlocksInput(Vector2 screen)=> (selected!=0||showIssues)&&Bounds.Contains(new Vector2(screen.x,Screen.height-screen.y));
        public void Bind(CityCoreIndustrySession current)
        {if(ReferenceEquals(session,current))return;session=current;query=current==null?null:new CityCoreObservation(current);selected=family=issuePage=0;showIssues=false;scroll=Vector2.zero;shown=null;issues=null;refreshed=-1;entityRevision=roadRevision=-1;}
        public void Close(){selected=family=0;showIssues=false;}
        public void Select(int id){selected=id;family=0;refreshed=-1;}
        public void SelectFamily(int id){family=id;}
        public void Pick(float x,float z){if(query!=null)Select(query.Pick(x,z));}
        public void ToggleIssues(){showIssues=!showIssues;refreshed=-1;}
        void Refresh()
        {
            if(query==null||selected==0&&!showIssues)return;long tick=session.Simulation.Tick;
            if(refreshed>=0&&tick-refreshed<10&&Time.realtimeSinceStartupAsDouble<refreshDeadline&&entityRevision==session.Entities.Revision&&roadRevision==session.Transport.NetworkGeneration)return;
            using var sample=refreshSample.Auto();
            shown=selected==0?null:query.Read(selected);if(selected!=0&&shown==null)selected=family=0;
            if(showIssues)issues=query.Issues();refreshDeadline=Time.realtimeSinceStartupAsDouble+.5;refreshed=tick;entityRevision=session.Entities.Revision;roadRevision=session.Transport.NetworkGeneration;
        }
        string Yes(bool value)=>text.Get(value?"ui.inspect.yes":"ui.inspect.no");
        string Problems(CityObservedProblem flags)
        {if(flags==0)return text.Get("ui.inspect.healthy");var parts=new System.Collections.Generic.List<string>();foreach(CityObservedProblem p in Enum.GetValues(typeof(CityObservedProblem)))if(p!=0&&(flags&p)!=0)parts.Add(text.Get("ui.inspect.problem."+p.ToString().ToLowerInvariant()));return string.Join(" / ",parts);}
        public void Draw()
        {
            Refresh();if(query==null||selected==0&&!showIssues)return;
            var previous=GUI.color;var content=GUI.contentColor;GUI.contentColor=Color.white;GUI.color=new Color(.07f,.1f,.12f,1);GUI.DrawTexture(Bounds,Texture2D.whiteTexture);GUI.color=previous;
            GUILayout.BeginArea(Bounds,GUI.skin.box);if(GUILayout.Button(text.Get("ui.inspect.close"))){selected=family=0;showIssues=false;GUILayout.EndArea();GUI.contentColor=content;return;}
            scroll=GUILayout.BeginScrollView(scroll);
            if(showIssues&&issues!=null)
            {
                GUILayout.Label(text.Get("ui.inspect.issues",issues.Count));int pages=Math.Max(1,(issues.Count+19)/20);issuePage=Math.Min(issuePage,pages-1);
                GUILayout.BeginHorizontal();if(GUILayout.Button("←"))issuePage=Math.Max(0,issuePage-1);GUILayout.Label(text.Get("ui.inspect.page",issuePage+1,pages));if(GUILayout.Button("→"))issuePage=Math.Min(pages-1,issuePage+1);GUILayout.EndHorizontal();
                for(int i=issuePage*20;i<Math.Min(issues.Count,(issuePage+1)*20);i++){var issue=issues[i];if(GUILayout.Button("#"+issue.id+" "+Problems(issue.problems))){Select(issue.id);var b=query.Read(issue.id);if(b!=null)Focus?.Invoke((float)b.x,(float)b.z);}}
            }
            if(shown!=null&&shown.id==selected)
            {
                GUILayout.Label(text.Get("ui.inspect.building",selected,text.Get("ui.inspect.use."+shown.use.ToString().ToLowerInvariant()),shown.tick));GUILayout.Label(Problems(shown.problems));
                GUILayout.Label(text.Get("ui.inspect.garbage",shown.garbage,shown.stored));GUILayout.Label(text.Get("ui.inspect.jobs",shown.filledJobs,shown.jobs,shown.attending));GUILayout.Label(text.Get("ui.inspect.transport",shown.transportCount,shown.strandedCount));
                if(shown.use==LandUse.Industrial||shown.use==LandUse.Commercial)GUILayout.Label(text.Get("ui.inspect.inventory",shown.raw,shown.goods,shown.cash.ToString("F0"),shown.workedMinutes.ToString("F1")));
                if(shown.units>0){GUILayout.Label(text.Get("ui.inspect.housing",shown.occupied,shown.units,shown.residents));GUILayout.Label(text.Get("ui.inspect.workforce",shown.employed,shown.workforce));}
                foreach(var h in shown.families)
                {
                    if(GUILayout.Button(text.Get("ui.inspect.family",h.id,h.unit+1,h.members.Count)))family=family==h.id?0:h.id;
                    if(h.id!=family)continue;
                    GUILayout.Label(text.Get("ui.inspect.savings",h.savings));GUILayout.Label(h.commuteSamples==0?text.Get("ui.inspect.no_commute"):text.Get("ui.inspect.commute",h.averageCommute.ToString("F1"),h.longestCommute.ToString("F1"),h.commuteSamples));
                    foreach(var p in h.members)
                    {
                        GUILayout.Label(text.Get("ui.inspect.person",p.id,p.name,p.age,Yes(p.workforce)));
                        GUILayout.Label(p.job<0?text.Get(p.workforce?"ui.inspect.unemployed":"ui.inspect.not_workforce"):text.Get("ui.inspect.work",p.job,p.workplace,p.wage,Yes(p.atWork)));
                        if(p.workplace>0&&GUILayout.Button(text.Get("ui.inspect.locate_work"))){var b=query.Read(p.workplace);if(b!=null){Select(b.id);Focus?.Invoke((float)b.x,(float)b.z);}}
                        GUILayout.Label(p.vehicle>0?text.Get("ui.inspect.journey",p.vehicle,p.task,p.origin,p.destination,text.Get("ui.inspect.trip."+p.purpose.ToString().ToLowerInvariant()),Yes(p.stranded)):text.Get("ui.inspect.location",p.location,p.carriedGoods));
                    }
                }
            }
            GUILayout.EndScrollView();GUILayout.EndArea();GUI.contentColor=content;
        }
    }
}
