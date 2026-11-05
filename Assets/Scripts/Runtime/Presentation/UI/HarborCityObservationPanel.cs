using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;
namespace HarborCity
{
    // Presentation owns selection/scroll/throttling only. Snapshots contain no live facts.
    public sealed class HarborCityObservationPanel
    {
        static readonly ProfilerMarker refreshSample=new ProfilerMarker("City.Views.Observation.Refresh");
        readonly CityLocalization text;CityCoreIndustrySession session;CityCoreObservation query;
        BuildingInspection shown;CityPlayerFeedback feedback;IReadOnlyList<CityFeedbackEntry> feedbackEntries;
        readonly int[] groupCounts=new int[Enum.GetValues(typeof(CityFeedbackKind)).Length];int group=-1;
        double feedbackDeadline;public string FeedbackError {get;private set;}="";
        public Action RecordScene;
        public int SelectedFamily=>family;
        public CityFeedbackSummary Summary {get{PollFeedback();return feedback?.Summary;}}
        int selected,family,issuePage;double refreshDeadline;long refreshed=-1;int entityRevision=-1,roadRevision=-1;Vector2 scroll;bool showIssues;
        int locatedTask,locatedVehicle;
        string locatedResident;
        public Action<float,float> Focus;
        public bool TryGetLocatedVehicle(out Vector3 position,out string resident)
        {
            position=default;
            resident=locatedResident;
            if(selected==0||locatedTask==0||session==null)return false;
            var vehicle=session.Transport.FindTaskVehicle(locatedTask);
            if(vehicle==null||vehicle.id!=locatedVehicle||vehicle.status==RoadCore.VehicleStatus.Delivered||vehicle.status==RoadCore.VehicleStatus.AwaitingRestart)return false;
            var point=session.Transport.Position(vehicle);
            position=new Vector3((float)point.x,(float)point.y+.4f,(float)point.z);
            return true;
        }
        public int Selected=>selected;
        public float Top=96;
        Rect Bounds=>new Rect(Math.Max(8,Screen.width-380),Top,Math.Min(372,Screen.width-16),Math.Max(60,Screen.height-Top-56));
        public HarborCityObservationPanel(CityLocalization text){this.text=text;}
        public bool BlocksInput(Vector2 screen)=> (selected!=0||showIssues)&&Bounds.Contains(new Vector2(screen.x,Screen.height-screen.y));
        public void Bind(CityCoreIndustrySession current)
        {if(ReferenceEquals(session,current))return;feedback?.Dispose();session=current;query=current==null?null:new CityCoreObservation(current);feedback=current==null?null:new CityPlayerFeedback(current,query);selected=family=issuePage=locatedTask=0;group=-1;showIssues=false;scroll=Vector2.zero;shown=null;feedbackEntries=null;refreshed=-1;entityRevision=roadRevision=-1;feedbackDeadline=0;FeedbackError="";}
        public void Dispose(){feedback?.Dispose();feedback=null;session=null;query=null;}
        public void PollFeedback(bool force=false)
        {
            if(feedback==null||!force&&Time.realtimeSinceStartupAsDouble<feedbackDeadline)return;
            feedbackDeadline=Time.realtimeSinceStartupAsDouble+.5;
            try{feedback.Flush();if(showIssues||force){feedbackEntries=feedback.Entries();Array.Clear(groupCounts,0,groupCounts.Length);foreach(var e in feedbackEntries)groupCounts[(int)e.kind]+=e.affected;}FeedbackError="";}catch(Exception e){FeedbackError=e.ToString();}
        }
        public CityFeedbackEntry[] CaptureEntries(){PollFeedback(true);return feedbackEntries==null?new CityFeedbackEntry[0]:new List<CityFeedbackEntry>(feedbackEntries).ToArray();}
        public BuildingInspection CaptureSelected(){return selected==0?null:query?.Read(selected);}
        public void Close(){selected=family=0;showIssues=false;}
        public void Select(int id){locatedTask=0;selected=id;family=0;refreshed=-1;}
        public void SelectFamily(int id){family=id;}
        public void Pick(float x,float z){if(query!=null)Select(query.Pick(x,z));}
        public void ToggleIssues(){showIssues=!showIssues;selected=family=issuePage=locatedTask=0;group=-1;refreshed=-1;feedbackDeadline=0;}
        void Refresh()
        {
            if(query==null||selected==0&&!showIssues)return;long tick=session.Simulation.Tick;
            if(refreshed>=0&&tick-refreshed<10&&Time.realtimeSinceStartupAsDouble<refreshDeadline&&entityRevision==session.Entities.Revision&&roadRevision==session.Transport.NetworkGeneration)return;
            using var sample=refreshSample.Auto();
            try{shown=selected==0?null:query.Read(selected);if(selected!=0&&shown==null)selected=family=0;}catch(Exception e){shown=null;FeedbackError=e.ToString();}
            refreshDeadline=Time.realtimeSinceStartupAsDouble+.5;refreshed=tick;entityRevision=session.Entities.Revision;roadRevision=session.Transport.NetworkGeneration;
        }
        string Yes(bool value)=>text.Get(value?"ui.inspect.yes":"ui.inspect.no");
        string Problems(CityObservedProblem flags)
        {if(flags==0)return text.Get("ui.feedback.basic_clear");var parts=new System.Collections.Generic.List<string>();foreach(CityObservedProblem p in Enum.GetValues(typeof(CityObservedProblem)))if(p!=0&&(flags&p)!=0)parts.Add(text.Get("ui.inspect.problem."+p.ToString().ToLowerInvariant()));return string.Join(" / ",parts);}
        string Location(int id)
        {var b=session.Entities.Building(id);return b==null?text.Get("ui.feedback.unknown_location"):text.Get("ui.feedback.location",text.Get("ui.inspect.use."+b.Use.ToString().ToLowerInvariant()),b.x.ToString("F0"),b.z.ToString("F0"));}
        void Locate(int id,int household=0)
        {Select(id);family=household;showIssues=false;scroll=Vector2.zero;var b=session.Entities.Building(id);if(b!=null)Focus?.Invoke(b.x,b.z);}
        void DrawIssues()
        {
            GUILayout.Label(text.Get("ui.feedback.issues_title"));
            if(!string.IsNullOrEmpty(FeedbackError)){GUILayout.Label(text.Get("ui.feedback.unavailable"));return;}
            if(feedbackEntries==null||feedbackEntries.Count==0){GUILayout.Label(text.Get("ui.feedback.no_issues"));return;}
            if(group<0)
            {
                for(int i=0;i<groupCounts.Length;i++)if(groupCounts[i]>0)
                {var kind=(CityFeedbackKind)i;var colour=GUI.contentColor;GUI.contentColor=i>=(int)CityFeedbackKind.FoodLow?new Color(1,.8f,.35f):new Color(1,.5f,.45f);if(GUILayout.Button(text.Get("ui.feedback.kind."+kind.ToString().ToLowerInvariant(),groupCounts[i]))){group=i;issuePage=0;scroll=Vector2.zero;}GUI.contentColor=colour;}
                return;
            }
            if(GUILayout.Button(text.Get("ui.feedback.back_groups"))){group=-1;return;}
            GUILayout.Label(text.Get("ui.feedback.kind."+((CityFeedbackKind)group).ToString().ToLowerInvariant(),groupCounts[group]));
            int total=0;foreach(var e in feedbackEntries)if((int)e.kind==group)total++;
            int pages=Math.Max(1,(total+19)/20);issuePage=Math.Min(issuePage,pages-1);
            GUILayout.BeginHorizontal();if(GUILayout.Button("←"))issuePage=Math.Max(0,issuePage-1);GUILayout.Label(text.Get("ui.inspect.page",issuePage+1,pages));if(GUILayout.Button("→"))issuePage=Math.Min(pages-1,issuePage+1);GUILayout.EndHorizontal();
            int at=0;foreach(var e in feedbackEntries)if((int)e.kind==group)
            {if(at++<issuePage*20||at>issuePage*20+20)continue;var b=session.Entities.Building(e.building);if(b==null)continue;string label=Location(b.id);if(e.family>0)label+=" · "+text.Get("ui.feedback.households",e.affected);if(GUILayout.Button(label))Locate(e.building,e.family);}
        }
        public void Draw()
        {
            PollFeedback();Refresh();if(query==null||selected==0&&!showIssues)return;
            var previous=GUI.color;var content=GUI.contentColor;GUI.contentColor=Color.white;GUI.color=new Color(.07f,.1f,.12f,1);GUI.DrawTexture(Bounds,Texture2D.whiteTexture);GUI.color=previous;
            GUILayout.BeginArea(Bounds,GUI.skin.box);if(GUILayout.Button(text.Get("ui.inspect.close"))){selected=family=0;showIssues=false;GUILayout.EndArea();GUI.contentColor=content;return;}
            if(GUILayout.Button(text.Get("ui.feedback.capture")))RecordScene?.Invoke();
            scroll=GUILayout.BeginScrollView(scroll);
            if(showIssues)DrawIssues();
            if(shown!=null&&shown.id==selected)
            {
                if(GUILayout.Button(text.Get("ui.feedback.back_groups"))){selected=family=0;showIssues=true;group=-1;feedbackDeadline=0;}
                GUILayout.Label(Location(shown.id));GUILayout.Label(Problems(shown.problems));
                GUILayout.Label(text.Get("ui.inspect.garbage",shown.garbage,shown.stored));GUILayout.Label(text.Get("ui.inspect.jobs",shown.filledJobs,shown.jobs,shown.attending));GUILayout.Label(text.Get("ui.inspect.transport",shown.transportCount,shown.strandedCount));
                if(shown.use==LandUse.Industrial||shown.use==LandUse.Commercial)GUILayout.Label(text.Get("ui.inspect.inventory",shown.raw,shown.goods,shown.cash.ToString("F0"),shown.workedMinutes.ToString("F1")));
                if(shown.use==LandUse.Industrial||shown.use==LandUse.Commercial){GUILayout.Label(text.Get("ui.goods.inventory",shown.products.food,shown.products.dailyGoods,shown.externalCapital.ToString("F0")));if(shown.use==LandUse.Industrial)GUILayout.Label(text.Get("ui.goods.output",text.Get(shown.product==ProductKind.Food?"goods.food":"goods.daily")));}
                if(shown.use==LandUse.Industrial||shown.use==LandUse.Commercial)
                {
                    GUILayout.Label(text.Get(shown.businessReason));
                    GUILayout.Label(shown.paidWorkers<0?text.Get("ui.feedback.reason.pending"):text.Get("ui.feedback.staff",shown.attending,shown.paidWorkers));
                    GUILayout.Label(text.Get("ui.feedback.cash",shown.cash.ToString("F2")));
                    GUILayout.Label(text.Get("ui.feedback.ledger",shown.capital.ToString("F2"),shown.revenue.ToString("F2"),shown.wages.ToString("F2"),shown.purchases.ToString("F2"),shown.processing.ToString("F2"),shown.taxes.ToString("F2")));
                    if(shown.use==LandUse.Industrial)GUILayout.Label(text.Get("ui.feedback.raw_incoming",shown.rawIncoming));else GUILayout.Label(text.Get("ui.feedback.goods_incoming",shown.incoming.food,shown.incoming.dailyGoods));
                    GUILayout.Label(text.Get("ui.feedback.advice.business"));
                }
                if(shown.units>0){GUILayout.Label(text.Get("ui.inspect.housing",shown.occupied,shown.units,shown.residents));GUILayout.Label(text.Get("ui.inspect.workforce",shown.employed,shown.workforce));}
                foreach(var h in shown.families)
                {
                    if(GUILayout.Button(text.Get("ui.feedback.family",h.unit+1,h.members.Count)))family=family==h.id?0:h.id;
                    if(h.id!=family)continue;
                    GUILayout.Label(text.Get("ui.inspect.savings",h.savings));GUILayout.Label(h.stockKnown?text.Get("ui.goods.household",h.products.food,h.foodDays.ToString("F1"),h.products.dailyGoods,h.dailyDays.ToString("F1")):text.Get("ui.feedback.reason.preparing"));
                    GUILayout.Label(text.Get("ui.feedback.need",h.needFood,h.needDaily));GUILayout.Label(text.Get("ui.feedback.adults",h.healthyAdults,h.availableAdults,h.workingAdults,h.travellingAdults));
                    if(!string.IsNullOrEmpty(h.goodsProblem))GUILayout.Label(text.Get(h.goodsProblem));
                    if(h.supplier>0&&session.Entities.Building(h.supplier)!=null&&GUILayout.Button(text.Get("ui.feedback.locate_supplier")))Locate(h.supplier);
                    GUILayout.Label(text.Get("ui.feedback.advice.family"));
                    GUILayout.Label(h.commuteSamples==0?text.Get("ui.inspect.no_commute"):text.Get("ui.inspect.commute",h.averageCommute.ToString("F1"),h.longestCommute.ToString("F1"),h.commuteSamples));
                    foreach(var p in h.members)
                    {
                        GUILayout.Label(text.Get("ui.feedback.person",p.name,p.age,Yes(p.workforce)));
                        GUILayout.Label(p.job<0?text.Get(p.workforce?"ui.inspect.unemployed":"ui.inspect.not_workforce"):text.Get("ui.feedback.work",Location(p.workplace),p.wage,Yes(p.atWork)));
                        if(p.workplace>0&&GUILayout.Button(text.Get("ui.inspect.locate_work"))){Locate(p.workplace);}
                        GUILayout.Label(p.vehicle>0?text.Get("ui.feedback.journey",Location(p.origin),Location(p.destination),text.Get("ui.inspect.trip."+p.purpose.ToString().ToLowerInvariant()),Yes(p.stranded)):text.Get("ui.feedback.person_location",Location(p.location),p.carriedGoods));
                        if(p.vehicle>0&&GUILayout.Button(text.Get("ui.feedback.locate_vehicle")))
                        {
                            var vehicle=session.Transport.FindTaskVehicle(p.task);
                            if(vehicle!=null&&vehicle.id==p.vehicle)
                            {
                                locatedTask=p.task;
                                locatedVehicle=vehicle.id;
                                locatedResident=p.name;
                                var position=session.Transport.Position(vehicle);
                                Focus?.Invoke((float)position.x,(float)position.z);
                            }
                            refreshed=-1;
                        }
                    }
                }
            }
            GUILayout.EndScrollView();GUILayout.EndArea();GUI.contentColor=content;
        }
    }
}
