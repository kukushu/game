using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HarborCity
{
    [Serializable] public sealed class HarborFeedbackBuildInfo
    {
        public bool playtest,dirty;public string label,commit,branch,sourceFingerprint,builtUtc;
    }
    [Serializable] sealed class HarborFeedbackView {public bool flatFixture;public string cityHash;}
    [Serializable] sealed class HarborFeedbackCapture
    {
        public int schema=1;public string utc,unity,simulationFault,observationError,saveError,screenshotError;
        public string cityOrigin;public int cityDay,population,households,buildings;public double cityClock;
        public HarborFeedbackBuildInfo build;public long tick;public double pendingSeconds;public float previousRate;
        public int selectedBuilding,selectedFamily;public bool paused,flatFixture,saveWritten,screenshotWritten;
        public CityFeedbackSummary summary;public CityFeedbackEntry[] issues;
        public BuildingInspection building;public FamilyInspection[] families;public ResidentInspection[] residents;
    }
    public sealed partial class HarborCityRoadCoreGame
    {
        HarborFeedbackBuildInfo feedbackBuild;bool feedbackCapturing,feedbackFlatFixture;
        string feedbackCityOrigin="normal-new-town";
        int feedbackPendingCase=-1;string feedbackLastCapture="",feedbackCaptureResult="",feedbackFault="";
        string FeedbackCaseRoot=>Path.Combine(Application.dataPath,"StreamingAssets","FeedbackCases");
        HarborFeedbackBuildInfo FeedbackBuild
        {
            get
            {
                if(feedbackBuild==null){var resource=Resources.Load<TextAsset>("FeedbackBuildInfo");feedbackBuild=resource==null?new HarborFeedbackBuildInfo{label="Editor"}:JsonUtility.FromJson<HarborFeedbackBuildInfo>(resource.text);}
                return feedbackBuild??(feedbackBuild=new HarborFeedbackBuildInfo{label="Editor"});
            }
        }
        Rect FeedbackBounds
        {
            get{float left=StatusBounds.xMax+8,width=TimeBounds.x-left-8;return width>=120?new Rect(left,8,width,78):new Rect(8,90,Math.Min(408,Screen.width-16),30);}
        }
        void OpenFeedbackIssues()
        {CancelTool();financePanel.IsOpen=false;toolPage=ToolPage.None;observation.Bind(session);observation.ToggleIssues();}
        void DrawFeedbackBar()
        {
            if(session==null)return;var summary=observation.Summary;var r=FeedbackBounds;
            HudBox(r);var previous=GUI.contentColor;
            bool unavailable=!string.IsNullOrEmpty(observation.FeedbackError);
            GUI.contentColor=unavailable?Muted:summary!=null&&summary.Critical?new Color(1,.55f,.48f):summary!=null&&summary.Count>0?new Color(1,.82f,.38f):Muted;
            if(GUI.Button(r,new GUIContent("",localization.Get("ui.feedback.bar_help")),GUIStyle.none))OpenFeedbackIssues();
            if(unavailable||summary==null)GUI.Label(new Rect(r.x+8,r.y+5,r.width-16,r.height-8),localization.Get("ui.feedback.unavailable"),wrapped);
            else if(r.height<40)GUI.Label(new Rect(r.x+8,r.y+6,r.width-16,20),localization.Get("ui.feedback.bar_short",summary.foodEmpty,summary.dailyEmpty,summary.cashShops+summary.cashFactories),small);
            else if(r.width<420)
            {
                GUI.Label(new Rect(r.x+8,r.y+5,r.width-16,18),localization.Get("ui.feedback.title"),small);
                GUI.Label(new Rect(r.x+8,r.y+23,r.width-16,17),localization.Get("ui.feedback.food_short",summary.foodEmpty,summary.foodLow),small);
                GUI.Label(new Rect(r.x+8,r.y+40,r.width-16,17),localization.Get("ui.feedback.daily_short",summary.dailyEmpty,summary.dailyLow),small);
                GUI.Label(new Rect(r.x+8,r.y+57,r.width-16,17),localization.Get("ui.feedback.business_short",summary.cashShops+summary.cashFactories,summary.infrastructure),small);
            }
            else
            {
                GUI.Label(new Rect(r.x+10,r.y+6,r.width-20,20),localization.Get("ui.feedback.title"),heading);
                GUI.Label(new Rect(r.x+10,r.y+31,(r.width-20)/2,20),localization.Get("ui.feedback.food_short",summary.foodEmpty,summary.foodLow),small);
                GUI.Label(new Rect(r.center.x,r.y+31,(r.width-20)/2,20),localization.Get("ui.feedback.daily_short",summary.dailyEmpty,summary.dailyLow),small);
                GUI.Label(new Rect(r.x+10,r.y+53,r.width-20,20),summary.Count==0?localization.Get("ui.feedback.no_issues_short"):localization.Get("ui.feedback.business_long",summary.cashShops,summary.cashFactories,summary.infrastructure),small);
            }
            GUI.contentColor=previous;
        }
        void BeginFeedbackCapture()
        {
            if(feedbackCapturing||session==null)return;
            float previous=speed;ScenarioRate(0);feedbackCapturing=true;feedbackLastCapture="";feedbackCaptureResult="";
            StartCoroutine(CaptureFeedback(previous));
        }
        static string FeedbackSaveHash(byte[] bytes)
        {using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        void WriteFeedbackSaveView(byte[] bytes)
        {if(FeedbackBuild.playtest)File.WriteAllText(SavePath+".view.json",JsonUtility.ToJson(new HarborFeedbackView{flatFixture=feedbackFlatFixture,cityHash=FeedbackSaveHash(bytes)},true));}
        bool ReadFeedbackSaveView(byte[] bytes)
        {
            if(!FeedbackBuild.playtest)return false;
            if(!File.Exists(SavePath+".view.json"))throw new InvalidDataException(localization.Get("ui.feedback.save_view_missing"));
            var view=JsonUtility.FromJson<HarborFeedbackView>(File.ReadAllText(SavePath+".view.json"));
            if(view==null||view.cityHash!=FeedbackSaveHash(bytes))throw new InvalidDataException(localization.Get("ui.feedback.save_view_mismatch"));
            return view.flatFixture;
        }
        IEnumerator CaptureFeedback(float previousRate)
        {
            string directory=Path.Combine(Application.persistentDataPath,"IssueSnapshots",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+"-"+Guid.NewGuid().ToString("N").Substring(0,8));
            var capture=new HarborFeedbackCapture{cityOrigin=feedbackCityOrigin,cityDay=session.City.day,population=session.City.population,households=session.City.society.families.Count,buildings=session.Entities.Buildings.Count,cityClock=session.Transport.Clock,utc=DateTime.UtcNow.ToString("o"),unity=Application.unityVersion,build=FeedbackBuild,tick=session.Simulation.Tick,pendingSeconds=session.Simulation.PendingSeconds,previousRate=previousRate,paused=true,flatFixture=feedbackFlatFixture,selectedBuilding=observation.Selected,selectedFamily=observation.SelectedFamily,simulationFault=session.Simulation.Fault?.ToString()??feedbackFault};
            bool created=true;
            try{Directory.CreateDirectory(directory);}catch(Exception e){created=false;feedbackCaptureResult=localization.Get("ui.feedback.capture_failed",e.Message);}
            if(created)
            {
                try
                {
                    capture.issues=observation.CaptureEntries();capture.summary=observation.Summary;capture.observationError=observation.FeedbackError;
                    capture.building=observation.CaptureSelected();var families=new List<FamilyInspection>();var people=new List<ResidentInspection>();
                    if(capture.building!=null)foreach(var family in capture.building.families){families.Add(family);foreach(var person in family.members)people.Add(person);}
                    capture.families=families.ToArray();capture.residents=people.ToArray();
                }catch(Exception e){capture.observationError=e.ToString();}
                // Save's existing boundary/fault validation remains authoritative.
                try{byte[] bytes=CityCoreSave.Write(session);File.WriteAllBytes(Path.Combine(directory,"city.hcity"),bytes);capture.saveWritten=true;}catch(Exception e){capture.saveError=e.ToString();}
                try{File.WriteAllText(Path.Combine(directory,"scene.json"),JsonUtility.ToJson(capture,true));}catch(Exception e){feedbackCaptureResult=localization.Get("ui.feedback.capture_failed",e.Message);}
                yield return new WaitForEndOfFrame();
                Texture2D screenshot=null;
                try{screenshot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(directory,"window.png"),screenshot.EncodeToPNG());capture.screenshotWritten=true;}catch(Exception e){capture.screenshotError=e.ToString();}finally{if(screenshot!=null)Destroy(screenshot);}
                bool metadataWritten=true;try{File.WriteAllText(Path.Combine(directory,"scene.json"),JsonUtility.ToJson(capture,true));}catch(Exception e){metadataWritten=false;feedbackCaptureResult=localization.Get("ui.feedback.capture_failed",e.Message);}
                feedbackLastCapture=directory;
                if(metadataWritten)feedbackCaptureResult=localization.Get(!capture.saveWritten?"ui.feedback.capture_no_save":capture.screenshotWritten?"ui.feedback.capture_ok":"ui.feedback.capture_partial",directory);
            }
            feedbackCapturing=false;notice=feedbackCaptureResult;
        }
        void BindFeedbackLandscape(bool flat)
        {
            feedbackFlatFixture=flat;
            if(!flat){BindLandscape();return;}
            if(landscape==null){var land=new GameObject("Feedback case landscape");land.transform.SetParent(transform);landscape=land.AddComponent<CityLandscape>();}
            landscape.BuildDiagnosticFlat(2,session.City.map.terrainHalfSize);session.Simulation.Growth.Zoning.SetSurface((x,z)=>2);
            constructionCamera=new HarborCityConstructionCamera(cameraView,landscape,session.City.map.buildHalfSize);roadViews?.InvalidateSurface();zoningViews?.InvalidateSurface();
        }
        void LoadFeedbackCase(int index)
        {
            try
            {
                string path=Path.Combine(FeedbackCaseRoot,index==0?"shop-cash.hcity":"supply-stopped.hcity");
                var restored=CityCoreSave.Read(File.ReadAllBytes(path));bool unlimited=session.Simulation.Finance.UnlimitedFunds;
                ScenarioRate(0);CancelTool();vehicleViews?.Clear();session=restored;world=restored.Transport;feedbackFault="";
                session.Simulation.Finance.UnlimitedFunds=unlimited;world.DepotLaneRoutingEnabled=true;BindFeedbackLandscape(true);
                observation.Bind(session);roadEditor.Bind(session);shownBuildings=shownZones=shownNetwork=-1;RenderRoads();RenderBuildings();RenderZones();RenderUtilities();
                constructionCamera.Focus(-95,0);feedbackPendingCase=-1;feedbackCityOrigin=Path.GetFileName(path);notice=localization.Get("ui.feedback.case_loaded");
            }catch(Exception e){notice=localization.Get("ui.feedback.case_failed",e.Message);}
        }
        void DrawFeedbackOptions()
        {
            GUILayout.Label(localization.Get("ui.feedback.version",FeedbackBuild.label??"Editor"),wrapped);
            if(GUILayout.Button(localization.Get("ui.feedback.capture")))BeginFeedbackCapture();
            if(feedbackCapturing)GUILayout.Label(localization.Get("ui.feedback.capturing"),wrapped);
            if(!string.IsNullOrEmpty(feedbackCaptureResult))GUILayout.Label(feedbackCaptureResult,wrapped);
            if(!string.IsNullOrEmpty(feedbackLastCapture))
            {
                if(GUILayout.Button(localization.Get("ui.feedback.open_folder")))Application.OpenURL(new Uri(feedbackLastCapture+Path.DirectorySeparatorChar).AbsoluteUri);
                if(GUILayout.Button(localization.Get("ui.feedback.copy_path")))GUIUtility.systemCopyBuffer=feedbackLastCapture;
            }
            if(!FeedbackBuild.playtest)return;
            GUILayout.Label(localization.Get("ui.feedback.cases_help"),wrapped);
            if(feedbackPendingCase<0)
            {
                if(GUILayout.Button(localization.Get("ui.feedback.case_cash")))feedbackPendingCase=0;
                if(GUILayout.Button(localization.Get("ui.feedback.case_supply")))feedbackPendingCase=1;
            }
            else
            {
                GUILayout.Label(localization.Get("ui.feedback.case_confirm"),wrapped);
                if(GUILayout.Button(localization.Get("ui.feedback.case_save_load"))&&Save())LoadFeedbackCase(feedbackPendingCase);
                if(GUILayout.Button(localization.Get("ui.feedback.case_load")))LoadFeedbackCase(feedbackPendingCase);
                if(GUILayout.Button(localization.Get("ui.feedback.cancel")))feedbackPendingCase=-1;
            }
        }
    }
}
