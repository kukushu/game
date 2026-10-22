using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarborCity.RoadCore;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Profiling;

namespace HarborCity
{
    // Production entry: CityCore owns simulation; RoadCore owns physical transport.
    public sealed partial class HarborCityRoadCoreGame:MonoBehaviour
    {
        static readonly ProfilerMarker simulationSample=new ProfilerMarker("City.Simulation"),viewsSample=new ProfilerMarker("City.RoadCore.Views");
        static readonly ProfilerMarker uiSample=new ProfilerMarker("City.Views.UI");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            if(FindAnyObjectByType<HarborCityRoadCoreGame>()==null)new GameObject("Harbor City Road Core").AddComponent<HarborCityRoadCoreGame>();
        }
        World world;CityCoreIndustrySession session;Camera cameraView; Material roadMaterial,carMaterial,waterLineMaterial;
        CityLandscape landscape;LandUse zoneTool=LandUse.Empty;int shownBuildings=-1,shownZones=-1,shownNetwork=-1;
        LandUse facilityTool=LandUse.Empty;UtilityKind? utilityTool;float facilityYaw;
        bool outsideTool;
        CityLocalization localization;HarborCityFinancePanel financePanel;HarborCityObservationPanel observation;HarborCityRoadEditor roadEditor;
        readonly List<GameObject> utilityViews=new List<GameObject>();
        HarborCityOutsideViews outsideViews;
        HarborCityEmploymentReadout employmentReadout;
        void FocusOutside(OutsideFlow flow)
        {if(session==null)return;foreach(var port in session.City.trade.connections)if(port.active&&port.flow==flow){constructionCamera?.Focus(port.x,port.z);return;}notice=localization.Get("ui.trade.no_endpoint");}
        HarborCityConstructionCamera constructionCamera;
        HarborCityConstructionPreview constructionPreview;
        HarborCityRoadGridTool gridTool;
        HarborCityZoneBoxTool zoneBoxTool;
        Vector2 menuScroll;string previewText="";Point previewPoint,previewStart;
        bool previewAnchored,previewOneWay,previewWide;int previewVersion=-1,previewMoney;
        Rect MenuBounds=>ToolPanelBounds;
        bool BlocksWorldInput(Vector2 p)=>BlocksHudInput(p)||financePanel.BlocksInput(p)||observation.BlocksInput(p)||roadEditor.BlocksInput(p);
        void ResetGesture(){zoneBoxTool?.Cancel();gridTool?.Cancel();hasStart=false;previewText="";previewVersion=-1;constructionPreview.Hide();}
        void CancelTool(){zoneBoxTool?.Cancel();gridTool?.Cancel();roadEditor.Clear();building=hasStart=outsideTool=false;zoneTool=facilityTool=LandUse.Empty;utilityTool=null;constructionPreview.Hide();previewText="";previewVersion=-1;notice=localization.Get("ui.build.cancelled");}
        Point[] RoadStroke(Point end)
        {
            if(Point.Distance(startPoint,end)<4)throw new CityRuleException("ui.build.too_short");
            var centre=new Point[9];for(int i=0;i<centre.Length;i++){centre[i]=Point.Lerp(startPoint,end,i/8.0);if(landscape!=null){centre[i].y=landscape.Height((float)centre[i].x,(float)centre[i].z);if(centre[i].y<=.15)throw new CityRuleException("ui.build.water");}}
            return centre;
        }
        string RoadError(Exception error)
        {
            if(error is CityRuleException)return localization.Error(error);
            // Existing core validation exceptions have no typed reason yet.
            // Translate known diagnostics here, without coupling physics to UI.
            if(error.Message=="Invalid lane access")return localization.Get("ui.build.access_conflict");
            if(error.Message.StartsWith("New stroke overlaps an existing road",StringComparison.Ordinal))return localization.Get("ui.build.overlap");
            return localization.Get("ui.build.road_error",error.Message);
        }
        void Preview(Point p)
        {
            bool anchored=hasStart;
            if(previewVersion==world.NetworkGeneration&&previewAnchored==anchored&&previewOneWay==oneWay&&previewWide==wide&&previewMoney==(session?.City.money??0)&&Point.Distance(previewPoint,p)<.05&&Point.Distance(previewStart,startPoint)<.05)return;
            previewPoint=p;previewStart=startPoint;previewAnchored=anchored;previewVersion=world.NetworkGeneration;previewOneWay=oneWay;previewWide=wide;previewMoney=session?.City.money??0;
            Point[] points=null;bool valid=true;
            try
            {
                // Cursor geometry is provisional, never a prepared transaction.
                // Final click below still creates and checks the complete plan.
                if(anchored){points=utilityTool.HasValue?new[]{startPoint,p}:RoadStroke(p);previewText=localization.Get(building?"ui.build.preview_unchecked":"ui.build.pipe_end",Point.Distance(startPoint,p).ToString("F1"));}
                else{if(p.y<=.15)throw new CityRuleException("ui.build.water");previewText=localization.Get(utilityTool.HasValue?"ui.build.pipe_start":"ui.build.choose_start");}
            }
            catch(Exception e){valid=false;previewText=building?RoadError(e):localization.Error(e);if(anchored&&points==null)points=new[]{startPoint,p};}
            constructionPreview.Show(anchored?startPoint:p,anchored,points,valid,utilityTool.HasValue ? .28f : wide?4:2,landscape==null?null:landscape.Height,provisional:building&&anchored);
        }
        HarborCityZoningViews zoningViews;Material[] zoneMaterials;
        HarborCityVehicleViews vehicleViews;
        HarborCityRoadViews roadViews;
        HarborCityBuildingViews buildingViews;
        bool hasStart;Point startPoint;bool building,oneWay,wide;int origin,destination;float speed=1;
        long previousTick;int lastSteps;
        CityPlayerStepBudget playerStepBudget=new CityPlayerStepBudget(InteractiveMaximumSteps);
        // Scheduling bounds, not simulation rules. At 60 FPS, 20x needs 6.67
        // complete .05s steps per frame; the old four-step cap prevented it.
        const int InteractiveMaximumSteps=32;
        double interactiveMilliseconds=8;
        double actualRate=double.NaN,rateWallSeconds,rateSimulationSeconds;
        internal CityCoreIndustrySession ScenarioSession=>session;
        bool scenarioSavedMap;
        internal float ScenarioSurface(float x,float z)=>scenarioSavedMap?landscape.Height(x,z):CityScenarioBuilder.Flat(x,z);
        internal int ScenarioSteps=>lastSteps;
        internal float ScenarioCurrentRate=>speed;
        internal int ScenarioVehicleViews=>vehicleViews?.Count??0;
        internal int ScenarioPooledVehicles=>vehicleViews?.Pooled??0;
        internal int ScenarioCreatedVehicleViews=>vehicleViews?.Created??0;
        internal int ScenarioReusedVehicleViews=>vehicleViews?.Reused??0;
        internal int ScenarioBuildingViews=>buildingViews?.Count??0;
        internal int ScenarioZonePlots=>zoningViews?.PlotCount??0;
        internal int ScenarioZoneRenderers=>zoningViews?.RendererCount??0;
        internal int ScenarioRoadViews=>roadViews?.Count??0;
        internal int ScenarioRoadRenderers=>roadViews?.RendererCount??0;
        internal int ScenarioRoadChunkSize=>roadViews?.ChunkSize??0;
        internal int ScenarioRoadCreated=>roadViews?.Created??0;
        internal int ScenarioRoadRemoved=>roadViews?.Removed??0;
        internal void ScenarioRefreshRoads(){if(!ScenarioMeasuring)throw new InvalidOperationException("Diagnostic road refresh outside measurement");RenderRoads();}
        internal int ScenarioUtilityViews=>utilityViews.Count;
        internal bool ScenarioMeasuring;
        internal double ScenarioAdvanceMs,ScenarioVehicleMs,ScenarioStructureMs;
        internal double ScenarioRequestedSeconds;
        internal void ScenarioRate(float rate){playerStepBudget.Reset();speed=rate;previousTick=System.Diagnostics.Stopwatch.GetTimestamp();actualRate=rate==0?0:double.NaN;rateWallSeconds=rateSimulationSeconds=0;}
        internal void ScenarioWorkBudget(int maximumSteps,double milliseconds)
        {
            if(!ScenarioMeasuring||speed!=0||maximumSteps<1||maximumSteps>128||!(milliseconds>=1&&milliseconds<=64))throw new InvalidOperationException("Work-budget overrides require a paused diagnostic window");
            playerStepBudget=new CityPlayerStepBudget(maximumSteps);interactiveMilliseconds=milliseconds;
        }
        static bool LegacyStateAbsent()=>typeof(CityModel).GetField("roads")==null&&typeof(CityModel).GetField("traffic")==null;
        string notice="";
        string SavePath=>Path.Combine(Application.persistentDataPath,session==null?"harbor-road-core-v15.hcrc":"harbor-city-core-v16.hcity");
        static Vector3 V(Point p)=>new Vector3((float)p.x,(float)p.y,(float)p.z);
        void Start()
        {
            Application.targetFrameRate=60;
            var language=Resources.Load<TextAsset>("GameData/Localization/zh-CN");if(language==null)throw new InvalidOperationException("Missing localization resource");localization=new CityLocalization(JsonUtility.FromJson<TranslationData>(language.text));notice=localization.Get("ui.hud.welcome");financePanel=new HarborCityFinancePanel(localization);observation=new HarborCityObservationPanel(localization);observation.Focus=(x,z)=>{if(constructionCamera!=null)constructionCamera.Focus(x,z);else{cameraView.transform.position=new Vector3(x,90,z-65);cameraView.transform.LookAt(new Vector3(x,0,z));}};
            foreach(var c in FindObjectsByType<Camera>(FindObjectsSortMode.None))c.gameObject.SetActive(false);
            cameraView=new GameObject("Road core camera").AddComponent<Camera>();cameraView.transform.position=new Vector3(0,90,-65);cameraView.transform.LookAt(Vector3.zero);cameraView.backgroundColor=new Color(.12f,.18f,.16f);cameraView.farClipPlane=3000;
            var shader=Resources.Load<Shader>("RoadCore");roadMaterial=new Material(shader){color=new Color(.25f,.29f,.32f)};carMaterial=new Material(shader){color=Color.yellow};roadEditor=new HarborCityRoadEditor(localization,roadMaterial);roadEditor.Changed=RenderRoads; // Zones observe NetworkGeneration once in Update; buildings did not move.
            constructionPreview=new HarborCityConstructionPreview(roadMaterial);waterLineMaterial=new Material(roadMaterial){color=new Color(.15f,.65f,1)};
            gridTool=new HarborCityRoadGridTool(roadMaterial,localization);
            zoneBoxTool=new HarborCityZoneBoxTool(roadMaterial,localization);
            var args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"-harbor-core-validate")>=0||Array.IndexOf(args,"-harbor-core-window")>=0||Array.IndexOf(args,"-harbor-city-core-validate")>=0)Application.runInBackground=true;
            if(Array.IndexOf(args,"-harbor-core-validate")>=0){world=new World();int a=world.AddJunction(new Point(-40,0,-15)),b=world.AddJunction(new Point(0,0,-15)),cnode=world.AddJunction(new Point(35,0,20));world.AddRoad(a,b,new[]{world.JunctionPosition(a),new Point(-20,0,-15),world.JunctionPosition(b)});world.AddRoad(b,cnode,new[]{world.JunctionPosition(b),new Point(15,0,-5),world.JunctionPosition(cnode)});origin=world.AddDepot(a,1000);destination=world.AddDepot(cnode,0);}
            else if(Array.IndexOf(args,"-harbor-city-scenario-window")>=0||Array.IndexOf(args,"-harbor-city-inspect")>=0)
            {
                bool inspecting=Array.IndexOf(args,"-harbor-city-inspect")>=0;
                int index=Array.IndexOf(args,inspecting?"-harbor-city-inspect":"-harbor-city-scenario-window");
                session=CityCoreSave.Read(File.ReadAllBytes(args[index+1]));world=session.Transport;
                if(inspecting)session.City.map.ExpandForPlay();
                bool savedMap=Array.IndexOf(args,"-harbor-window-saved-map")>=0;
                scenarioSavedMap=savedMap;
                if(!savedMap)foreach(int id in world.Network.RoadIds){var geometry=world.Network.Centreline(id);for(int i=0;i<geometry.Count;i++)if(Math.Abs(geometry[i].y-2)>1e-6)throw new InvalidOperationException("Flat fixture required; use -harbor-window-saved-map for a player city");}
                landscape=new GameObject("Scenario landscape").AddComponent<CityLandscape>();
                if(savedMap)landscape.Build(session.City.map);else landscape.BuildDiagnosticFlat(2,session.City.map.terrainHalfSize);
                session.Simulation.Growth.Zoning.SetSurface(ScenarioSurface);
                constructionCamera=new HarborCityConstructionCamera(cameraView,landscape,session.City.map.buildHalfSize);
                float mapScale=session.City.map.buildHalfSize/CityMapState.BuildHalfSize;cameraView.transform.position=new Vector3(0,260*mapScale,-185*mapScale);cameraView.transform.LookAt(new Vector3(0,2,0));
                RenderBuildings();RenderZones();RenderUtilities();notice=localization.Get("ui.stage.current");speed=0;Application.runInBackground=true;
                if(!inspecting){var probe=gameObject.AddComponent<HarborCityScenarioWindowProbe>();probe.Configure(this,args[index+1],args[index+2],float.Parse(args[index+3],System.Globalization.CultureInfo.InvariantCulture));}
            }
            else if(args.Any(a=>a.StartsWith("-harbor-",StringComparison.Ordinal)&&a!="-harbor-city-construction-validate"&&a!="-harbor-city-trade-validate"&&a!="-harbor-city-growing-profile")){session=CityCoreIndustrySession.Create();world=session.Transport;origin=1;destination=2;RenderBuildings();}
            else
            {
                // Keep existing original-terrain validation inputs comparable; normal Play starts flat.
                bool originalTerrainCheck=args.Any(a=>a=="-harbor-city-construction-validate"||a=="-harbor-city-trade-validate"||a=="-harbor-city-growing-profile");
                session=originalTerrainCheck?CityCoreIndustrySession.CreateEmpty(HarborCityEconomyAsset.Load()):CityNewTownTemplate.Create(HarborCityEconomyAsset.Load());
                world=session.Transport;BindLandscape();notice=localization.Get(originalTerrainCheck?"ui.stage.current":"ui.template.ready");
                if(!originalTerrainCheck){RenderBuildings();RenderZones();}
            }
            bool playerSession=!args.Any(a=>a.StartsWith("-harbor-",StringComparison.Ordinal))||Array.IndexOf(args,"-harbor-city-inspect")>=0;
            if(session!=null){session.Simulation.Finance.UnlimitedFunds=playerSession;if(playerSession){session.City.utilities.roadConduits=true;session.Simulation.Utilities.Refresh();}}
            RenderRoads();
            previousTick=System.Diagnostics.Stopwatch.GetTimestamp();
            int flag=Array.IndexOf(args,"-harbor-core-validate");if(flag>=0&&flag+1<args.Length)StartCoroutine(ValidateNative(args[flag+1]));
            flag=Array.IndexOf(args,"-harbor-core-window");if(flag>=0&&flag+1<args.Length)StartCoroutine(MeasureWindow(args[flag+1]));
            flag=Array.IndexOf(args,"-harbor-city-core-validate");if(flag>=0&&flag+1<args.Length)StartCoroutine(ValidateIndustryNative(args[flag+1]));
            flag=Array.IndexOf(args,"-harbor-city-window");if(flag>=0&&flag+1<args.Length){Application.runInBackground=true;StartCoroutine(MeasureIntegratedWindow(args[flag+1]));}
            flag=Array.IndexOf(args,"-harbor-city-construction-validate");if(flag>=0&&flag+1<args.Length){Application.runInBackground=true;StartCoroutine(ValidateConstruction(args[flag+1]));}
#if CITY_CORE_ATTRIBUTION
            flag=Array.IndexOf(args,"-harbor-city-growing-profile");if(flag>=0&&flag+1<args.Length){Application.runInBackground=true;StartCoroutine(ProfileGrowingCity(args[flag+1]));}
#endif
            flag=Array.IndexOf(args,"-harbor-city-trade-validate");if(flag>=0&&flag+1<args.Length){Application.runInBackground=true;StartCoroutine(ValidateTrade(args[flag+1]));}
#if CITY_CORE_ATTRIBUTION
            flag=Array.IndexOf(args,"-harbor-city-profile");if(flag>=0&&flag+1<args.Length){Application.runInBackground=true;StartCoroutine(ProfileCity(args[flag+1]));}
#endif
        }
#if CITY_CORE_ATTRIBUTION
        System.Collections.IEnumerator ProfileCity(string path)
        {
            speed=0;Application.targetFrameRate=60;Directory.CreateDirectory(path);session=CityIntegratedScaleFixture.Create(3000);world=session.Transport;RenderRoads();RenderBuildings();for(int i=0;i<20;i++)session.Advance(.05);
            UnityEngine.Profiling.Profiler.logFile=Path.Combine(path,"city.raw");UnityEngine.Profiling.Profiler.maxUsedMemory=512*1024*1024;UnityEngine.Profiling.Profiler.enableBinaryLog=true;UnityEngine.Profiling.Profiler.enabled=true;CityCoreProfile.Enabled=true;
            using(var writer=new StreamWriter(Path.Combine(path,"steps.csv")))
            {writer.WriteLine(CityCoreProfile.Header());for(int i=0;i<200;i++){CityCoreProfile.Reset();int a=GC.CollectionCount(0),b=GC.CollectionCount(1),c=GC.CollectionCount(2);long r=world.RoutePreparations;session.Advance(.05);writer.WriteLine(CityCoreProfile.Row(session.Simulation.Tick,world.ActiveCount,world.RoutePreparations-r,GC.CollectionCount(0)-a,GC.CollectionCount(1)-b,GC.CollectionCount(2)-c));yield return null;}}
            CityCoreProfile.Enabled=false;UnityEngine.Profiling.Profiler.enabled=false;UnityEngine.Profiling.Profiler.enableBinaryLog=false;session.Validate();File.WriteAllText(Path.Combine(path,"ProfileResult.txt"),"PASS native Unity 3000 actual residents, 750 factories/shops, full profiler binary and per-step nested counters. No Deep Profile. Actual frame timings include rendering; counters measure instrumented simulation, not uninstrumented FPS.");Application.Quit(0);
        }
#endif
        void Update()
        {
            observation?.Bind(session);roadEditor?.Bind(session);roadEditor?.Sync();buildingViews?.Select(observation?.Selected??0);
            if(world==null)return;long now=System.Diagnostics.Stopwatch.GetTimestamp();double elapsed=(now-previousTick)/(double)System.Diagnostics.Stopwatch.Frequency,seconds=elapsed*speed;
            if(ScenarioMeasuring)ScenarioRequestedSeconds=seconds;
            double clockBefore=world.Clock;
            using(simulationSample.Auto())
            {
                if(speed==0)lastSteps=0;
                else if(session==null)lastSteps=world.Advance(seconds);
                else
                {
                    int allowance=playerStepBudget.Request(seconds);
                    session.Simulation.RequestTime(seconds);
                    lastSteps=allowance==0?0:session.Simulation.Advance(0,allowance,interactiveMilliseconds);
                    playerStepBudget.Complete(lastSteps);
                }
            }
            previousTick=now;
            // Measure committed simulation time, never requested time or debt.
            // Real elapsed time includes rendering, construction and long frames.
            if(speed==0){actualRate=rateWallSeconds=rateSimulationSeconds=0;}
            else
            {
                rateWallSeconds+=elapsed;rateSimulationSeconds+=world.Clock-clockBefore;
                if(rateWallSeconds>=.5){actualRate=rateSimulationSeconds/rateWallSeconds;rateWallSeconds=rateSimulationSeconds=0;}
            }
            long sampleAt=ScenarioMeasuring?System.Diagnostics.Stopwatch.GetTimestamp():0;
            if(ScenarioMeasuring)ScenarioAdvanceMs=(sampleAt-now)*1000.0/System.Diagnostics.Stopwatch.Frequency;
            if(outsideViews==null)outsideViews=new HarborCityOutsideViews(roadMaterial,localization);outsideViews.Sync(session);
            if(session!=null&&session.City.development.enabled)
            {
                bool changed=shownBuildings!=session.Entities.BuildingRevision;
                if(changed){RenderBuildings();shownBuildings=session.Entities.BuildingRevision;}
                if(changed||shownZones!=session.City.zoning.revision||shownNetwork!=world.NetworkGeneration){RenderZones();shownZones=session.City.zoning.revision;shownNetwork=world.NetworkGeneration;}
            }
            if(ScenarioMeasuring){long end=System.Diagnostics.Stopwatch.GetTimestamp();ScenarioStructureMs=(end-sampleAt)*1000.0/System.Diagnostics.Stopwatch.Frequency;sampleAt=end;}
            using(viewsSample.Auto())
            {
                if(vehicleViews==null)vehicleViews=new HarborCityVehicleViews(carMaterial);vehicleViews.Sync(world);
            }
            if(ScenarioMeasuring)ScenarioVehicleMs=(System.Diagnostics.Stopwatch.GetTimestamp()-sampleAt)*1000.0/System.Diagnostics.Stopwatch.Frequency;
            // A scripted timing run observes normal simulation/views but must not
            // accept incidental clicks or camera keys. Manual inspect is separate.
            if(ScenarioMeasuring)return;
            if(constructionCamera!=null&&(Mouse.current==null||!(BlocksWorldInput(Mouse.current.position.ReadValue()))))constructionCamera.Step(Time.deltaTime);
            if(Keyboard.current!=null)
            {
                var k=Keyboard.current;if(constructionCamera==null){Vector3 move=new Vector3((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),0,(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));cameraView.transform.position+=move*Time.deltaTime*35;}
                if(k.escapeKey.wasPressedThisFrame){CancelTool();toolPage=ToolPage.None;return;}
            }
            var mouse=Mouse.current;if(mouse==null)return;
            bool click=mouse.leftButton.wasPressedThisFrame;
            if(gridTool.Enabled){gridTool.Step(cameraView,landscape,session,BlocksWorldInput(mouse.position.ReadValue()),oneWay,wide,message=>notice=message,RenderRoads);return;}
            if(zoneTool!=LandUse.Empty&&zoneBoxTool.Box&&landscape!=null){zoneBoxTool.Step(cameraView,landscape,session,zoneTool,BlocksWorldInput(mouse.position.ReadValue()),message=>notice=message,RenderZones);return;}
            if(BlocksWorldInput(mouse.position.ReadValue())){constructionPreview.Hide();previewVersion=-1;return;}
            bool inspecting=!roadEditor.IsSelecting&&!building&&zoneTool==LandUse.Empty&&facilityTool==LandUse.Empty&&!utilityTool.HasValue&&!outsideTool;
            if(inspecting){constructionPreview.Hide();if(click){int picked=buildingViews?.Pick(cameraView.ScreenPointToRay(mouse.position.ReadValue()))??0;observation?.Select(picked);}return;}
            var ray=cameraView.ScreenPointToRay(mouse.position.ReadValue());Vector3 hit;
            if(landscape!=null){if(!landscape.Raycast(ray,out hit)){constructionPreview.Hide();if(click)notice=localization.Get("ui.build.no_ground");return;}}
            else{float enter;if(!new Plane(Vector3.up,Vector3.zero).Raycast(ray,out enter)){constructionPreview.Hide();if(click)notice=localization.Get("ui.build.no_ground");return;}hit=ray.GetPoint(enter);}
            var p=new Point(hit.x,hit.y,hit.z);
            if(building||utilityTool.HasValue)Preview(p);else constructionPreview.Hide();
            if(!click)return;
            if(roadEditor.IsSelecting){roadEditor.Pick(p);return;}
            if(outsideTool){try{int id=session.Simulation.Trade.ConnectBorder(p);notice=localization.Get("ui.trade.connected",id);}catch(Exception e){notice=localization.Error(e);}return;}
            if(facilityTool!=LandUse.Empty){try{int id=session.Construction.PlaceFacility(facilityTool,hit.x,hit.z,facilityYaw,session.Simulation.Growth.Zoning);session.Simulation.Utilities.Refresh();notice=session.City.utilities.roadConduits?localization.Get("ui.utility.facility_road",id):"已建市政设施 #"+id+"；供水和排污须接管线，设施需要电力。";RenderBuildings();}catch(Exception e){notice=localization.Error(e);}return;}
            if(zoneTool!=LandUse.Empty){var selection=session.Simulation.Growth.Zoning.Cells().Where(c=>(c.pose.x-hit.x)*(c.pose.x-hit.x)+(c.pose.z-hit.z)*(c.pose.z-hit.z)<=9).ToArray();int changed=session.Simulation.Growth.Zoning.Paint(selection,zoneTool==LandUse.Bulldoze?LandUse.Empty:zoneTool);notice="已修改 "+changed+" 格土地用途；私人建筑由城市时钟逐步生长。";RenderZones();return;}
            if(!hasStart){if(p.y<=.15){notice=localization.Get("ui.build.water");return;}startPoint=p;hasStart=true;previewVersion=-1;Preview(p);notice=localization.Get(utilityTool.HasValue?"ui.build.pipe_end":"ui.build.choose_end");return;}
            if(!utilityTool.HasValue&&Point.Distance(startPoint,p)<4){notice=localization.Get("ui.build.too_short");return;}
            if(utilityTool.HasValue){try{int paid=session.Simulation.Utilities.Build(utilityTool.Value,startPoint,p);hasStart=false;RenderUtilities();notice="已建设管线，花费 "+paid;}catch(Exception e){notice=localization.Error(e);}return;}
            try{var centre=RoadStroke(p);var plan=world.PlanConnectedRoad(centre,oneWay,snap:4,lanes:wide?2:1);int fee=0;if(session!=null)session.Construction.CheckRoadPlacement(plan);if(!(session==null?plan.TryCommit():session.Construction.Commit(plan,out fee)))throw new CityRuleException("road.edit_stale");hasStart=false;constructionPreview.Hide();previewVersion=-1;previewText="";notice=localization.Get("ui.build.success",fee);try{RenderRoads();}catch(Exception viewError){notice+=localization.Get("ui.build.view_error");Debug.LogException(viewError);}}
            catch(Exception e){notice=RoadError(e);}
        }
        System.Collections.IEnumerator MeasureIntegratedWindow(string path)
        {
            Directory.CreateDirectory(path);QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
            using(var output=new StreamWriter(Path.Combine(path,"city-window.csv")))
            {
                output.WriteLine("residents,rate,frames,wall_seconds,simulation_seconds,actual_rate,fps,frame_mean_ms,frame_p95_ms,frame_p99_ms,active_min,active_max,steps,steps_per_frame_max,gc_frame_mean_bytes,gc_counter_valid");
                foreach(int population in new[]{1000,3000})foreach(int rate in new[]{1,3})
                {
                    speed=0;session=CityIntegratedScaleFixture.Create(population);world=session.Transport;RenderBuildings();RenderRoads();for(int i=0;i<20;i++)session.Advance(.05);
                    cameraView.farClipPlane=60000;cameraView.transform.position=new Vector3(population/4*15,25000,0);cameraView.transform.LookAt(new Vector3(population/4*15,0,0));
                    yield return null;previousTick=System.Diagnostics.Stopwatch.GetTimestamp();speed=rate;yield return new WaitForSecondsRealtime(1);
                    var frameTimes=new List<double>();int min=int.MaxValue,max=0,maxSteps=0;long memory=0,startTick=session.Simulation.Tick;double start=Time.realtimeSinceStartupAsDouble,last=start;
                    using(var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame"))
                    {
                        while(Time.realtimeSinceStartupAsDouble-start<12)
                        {
                            yield return null;double now=Time.realtimeSinceStartupAsDouble;frameTimes.Add((now-last)*1000);last=now;min=Math.Min(min,world.ActiveCount);max=Math.Max(max,world.ActiveCount);maxSteps=Math.Max(maxSteps,lastSteps);if(gc.Valid)memory+=gc.LastValue;
                        }
                        speed=0;double elapsed=last-start,sim=(session.Simulation.Tick-startTick)*World.StepSeconds;var sorted=frameTimes.ToArray();Array.Sort(sorted);session.Validate();
                        string row=string.Join(",",population,rate,frameTimes.Count,elapsed.ToString("F6",System.Globalization.CultureInfo.InvariantCulture),sim.ToString("F6",System.Globalization.CultureInfo.InvariantCulture),(sim/elapsed).ToString("F6",System.Globalization.CultureInfo.InvariantCulture),(frameTimes.Count/elapsed).ToString("F3",System.Globalization.CultureInfo.InvariantCulture),frameTimes.Average().ToString("F3",System.Globalization.CultureInfo.InvariantCulture),sorted[(int)((sorted.Length-1)*.95)].ToString("F3",System.Globalization.CultureInfo.InvariantCulture),sorted[(int)((sorted.Length-1)*.99)].ToString("F3",System.Globalization.CultureInfo.InvariantCulture),min,max,session.Simulation.Tick-startTick,maxSteps,memory/frameTimes.Count,gc.Valid);output.WriteLine(row);output.Flush();
                    }
                }
            }
            Application.Quit(0);
        }
        void RenderRoads()
        {
            if(roadViews==null){int chunk=64;var args=Environment.GetCommandLineArgs();int flag=Array.IndexOf(args,"-harbor-road-chunk-size");if(ScenarioMeasuring&&flag>=0)chunk=int.Parse(args[flag+1],System.Globalization.CultureInfo.InvariantCulture);roadViews=new HarborCityRoadViews(roadMaterial,chunk);}
            roadViews.Sync(world,landscape==null?null:landscape.Height);
        }
        void RenderBuildings()
        {
            if(buildingViews==null)buildingViews=new HarborCityBuildingViews(roadMaterial);
            buildingViews.Sync(session?.Entities.Buildings);
        }
        void RenderZones()
        {
            if(session==null||!session.Simulation.Growth.Zoning.Ready){zoningViews?.Dispose();return;}
            if(zoneMaterials==null){zoneMaterials=new Material[4];var uses=new[]{LandUse.Empty,LandUse.Residential,LandUse.Commercial,LandUse.Industrial};for(int i=0;i<4;i++)zoneMaterials[i]=new Material(roadMaterial){color=HarborCityZoningViews.Colour(uses[i])};}
            if(zoningViews==null)zoningViews=new HarborCityZoningViews(zoneMaterials);zoningViews.Sync(session.Simulation.Growth.Zoning.Cells(),landscape==null?null:landscape.Height);
        }
        void RenderUtilities()
        {
            foreach(var view in utilityViews)Destroy(view);utilityViews.Clear();if(session==null)return;
            foreach(var kind in new[]{UtilityKind.Electricity,UtilityKind.Water}){var network=session.City.UtilityNetwork(kind);foreach(var edge in network.edges){var a=network.Node(edge.a);var b=network.Node(edge.b);var go=new GameObject(kind+" line #"+edge.id);var line=go.AddComponent<LineRenderer>();line.sharedMaterial=kind==UtilityKind.Water?waterLineMaterial:carMaterial;line.startWidth=line.endWidth=kind==UtilityKind.Water?.22f:.14f;int samples=Math.Max(2,(int)Math.Ceiling(Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z)))+1);line.positionCount=samples;for(int i=0;i<samples;i++){float t=i/(float)(samples-1),x=Mathf.Lerp(a.x,b.x,t),z=Mathf.Lerp(a.z,b.z,t);line.SetPosition(i,new Vector3(x,(landscape==null?2:landscape.Height(x,z))+(kind==UtilityKind.Water?.34f:1.4f),z));}utilityViews.Add(go);}}
        }
        void OnGUI()
        {
            if(ScenarioMeasuring&&(Event.current.isMouse||Event.current.isKey||Event.current.type==EventType.ScrollWheel))return;
            using(uiSample.Auto())DrawInterface();
        }
        void Save(){try{string tmp=SavePath+".tmp";File.WriteAllBytes(tmp,session==null?SaveCodec.Write(world):CityCoreSave.Write(session));if(File.Exists(SavePath))File.Replace(tmp,SavePath,SavePath+".bak");else File.Move(tmp,SavePath);notice="新格式已保存；旧城市文件未改动";}catch(Exception e){notice=localization.Error(e);}}
        void BindLandscape()
        {
            session.City.map.ExpandForPlay();
            if(landscape==null){var land=new GameObject("City landscape");land.transform.SetParent(transform);landscape=land.AddComponent<CityLandscape>();}
            landscape.Build(session.City.map);
            session.Simulation.Growth.Zoning.SetSurface(landscape.Height);
            constructionCamera=new HarborCityConstructionCamera(cameraView,landscape,session.City.map.buildHalfSize);
            roadViews?.InvalidateSurface();zoningViews?.InvalidateSurface();
        }
        void Load(){try{var bytes=File.ReadAllBytes(SavePath);var replacement=session==null?SaveCodec.Read(bytes):null;var restored=session==null?null:CityCoreSave.Read(bytes);if(restored!=null){restored.Simulation.Finance.UnlimitedFunds=session.Simulation.Finance.UnlimitedFunds;restored.City.utilities.roadConduits=true;restored.Simulation.Utilities.Refresh();}vehicleViews?.Clear();session=restored;world=restored?.Transport??replacement;playerStepBudget.Reset();CancelTool();observation?.Bind(session);roadEditor?.Bind(session);if(session!=null)BindLandscape();shownBuildings=shownZones=shownNetwork=-1;RenderRoads();RenderBuildings();RenderZones();RenderUtilities();notice="新格式已加载";}catch(Exception e){notice="读取失败："+e.Message;}}
        System.Collections.IEnumerator ValidateNative(string path)
        {
            Directory.CreateDirectory(path);speed=0;yield return null;
            try
            {
                var truck=world.Dispatch(origin,destination);if(truck==null)throw new Exception("Dispatch failed");world.Advance(1);Point before=world.Position(truck);world.SplitRoad(1,1);if(Point.Distance(before,world.Position(truck))>1e-8)throw new Exception("Split moved vehicle");RenderRoads();
                var restored=SaveCodec.Read(SaveCodec.Write(world));for(int i=0;i<300;i++){world.Step();restored.Step();world.AssertSafety();restored.AssertSafety();}
                if(Convert.ToBase64String(SaveCodec.Write(world))!=Convert.ToBase64String(SaveCodec.Write(restored)))throw new Exception("Save continuation differs");if(world.Delivered!=1)throw new Exception("No settlement");
                File.WriteAllText(Path.Combine(path,"Result.txt"),"PASS: default new core; actual Unity build/run; split position; binary continuation; safety; freight stock/cash settlement. Old city gameplay is not connected.\n");
            }
            catch(Exception e){File.WriteAllText(Path.Combine(path,"Result.txt"),"FAIL "+e);Application.Quit(1);yield break;}
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(path,"NewCore.png"));yield return null;Application.Quit(0);
        }
        System.Collections.IEnumerator MeasureWindow(string path)
        {
            session=null;RenderBuildings();Directory.CreateDirectory(path);QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
            var rows=new List<string>{"vehicles,scenario,speed,frames,fps,mean_frame_ms,p95_frame_ms,p99_frame_ms,actual_sim_rate,gc_alloc_bytes_per_frame,gc_collections,steps_per_frame_max"};
            foreach(int count in new[]{1000,3000,10000})foreach(bool queued in new[]{false,true})foreach(int multiplier in new[]{1,3})
            {
                speed=0;vehicleViews?.Clear();world=CityRoadCoreFixtures.Create(count,queued);RenderRoads();
                cameraView.transform.position=queued?new Vector3(0,4500,-3000):new Vector3(400,1200,-700);cameraView.farClipPlane=12000;cameraView.transform.LookAt(queued?Vector3.zero:new Vector3(400,0,80));
                speed=multiplier;previousTick=System.Diagnostics.Stopwatch.GetTimestamp();yield return new WaitForSecondsRealtime(3);
                var recorder=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
                var frames=new List<double>();double started=Time.realtimeSinceStartupAsDouble,clock=world.Clock,previous=started;long allocated=0;int collections=GC.CollectionCount(0),maxSteps=0;
                while(Time.realtimeSinceStartupAsDouble-started<6)
                {
                    yield return null;double current=Time.realtimeSinceStartupAsDouble;frames.Add((current-previous)*1000);previous=current;maxSteps=Math.Max(maxSteps,lastSteps);if(recorder.Valid)allocated+=recorder.LastValue;
                    if(world.ActiveCount!=count)throw new InvalidOperationException("Window fixture lost active vehicles");
                }
                double elapsed=Time.realtimeSinceStartupAsDouble-started,rate=(world.Clock-clock)/elapsed;int collected=GC.CollectionCount(0)-collections;string allocation=recorder.Valid?(allocated/frames.Count).ToString():"NA";recorder.Dispose();
                frames.Sort();double sum=0;foreach(double frame in frames)sum+=frame;
                rows.Add(string.Join(",",count,queued?"queued-three-approach":"spaced-twenty-corridors",multiplier,frames.Count,(frames.Count/elapsed).ToString("F3",System.Globalization.CultureInfo.InvariantCulture),(sum/frames.Count).ToString("F3",System.Globalization.CultureInfo.InvariantCulture),frames[(int)((frames.Count-1)*.95)].ToString("F3",System.Globalization.CultureInfo.InvariantCulture),frames[(int)((frames.Count-1)*.99)].ToString("F3",System.Globalization.CultureInfo.InvariantCulture),rate.ToString("F4",System.Globalization.CultureInfo.InvariantCulture),allocation,collected,maxSteps));
                File.WriteAllLines(Path.Combine(path,"window.csv"),rows);notice="窗口采样："+rows[rows.Count-1];
                if(count==1000&&!queued&&multiplier==1){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(path,"Window.png"));yield return null;}
            }
            Application.Quit(0);
        }
        System.Collections.IEnumerator ValidateIndustryNative(string path)
        {
            speed=0;Directory.CreateDirectory(path);yield return null;
            try
            {
                int branch=world.AddJunction(new Point(0,0,25));world.AddRoad(2,branch,new[]{world.JunctionPosition(2),world.JunctionPosition(branch)});if(session.City.Citizens.Any(p=>p.atWork))throw new Exception("Default residents started onsite");for(int wait=0;wait<100&&!world.Vehicles.Any(v=>v.Task.purpose==TransportPurpose.Freight);wait++)session.Advance(.05);var actor=world.Vehicles.First(v=>v.Task.purpose==TransportPurpose.Freight);if(world.RoutePreparations!=2)throw new Exception("Repeated actual departures reran route preparation");if(!world.TryChangeLane(actor.id,1))throw new Exception("Actual industry truck could not start lane change");Point first=world.Position(actor);session.Advance(.5);if(Point.Distance(first,world.Position(actor))<1)throw new Exception("Truck did not physically move");
                Point beforeEdit=world.Position(actor);world.SplitRoadAt(1,12.75);world.SplitRoadAt(2,20.25);var widePlan=world.PlanConnectedRoad(new[]{new Point(-35,0,30),new Point(35,0,30)},lanes:2);if(!widePlan.TryCommit())throw new Exception("Wide road transaction failed");RenderRoads();if(Point.Distance(beforeEdit,world.Position(actor))>1e-8)throw new Exception("Access edit moved truck");
                Point beforeUpgrade=world.Position(actor);if(!world.PlanUpgradeRoad(1,3,14,RoadDirection.Both).TryCommit()||Point.Distance(beforeUpgrade,world.Position(actor))>1e-7)throw new Exception("Actual road upgrade moved freight");RenderRoads();var restored=CityCoreSave.Read(CityCoreSave.Write(session));for(int i=0;i<300;i++){session.Advance(.05);restored.Advance(.05);session.Validate();}
                if(Convert.ToBase64String(CityCoreSave.Write(session))!=Convert.ToBase64String(CityCoreSave.Write(restored)))throw new Exception("Integrated save differs");
                if(session.City.Factory(1).sold==0||session.City.Factory(1).sold!=world.Delivered||session.City.Goods(2)!=world.Delivered||!LegacyStateAbsent())throw new Exception("Business loop/legacy engine invariant failed");
                var repair=CityCoreIndustrySession.Create();repair.Advance(.05);var commuter=repair.Transport.Vehicles.First();Point strandedAt=repair.Transport.Position(commuter);repair.Transport.RemoveRoad(1);repair.Transport.AddRoad(1,2,new[]{repair.Transport.JunctionPosition(1),new Point(-18,0,0),repair.Transport.JunctionPosition(2)},lanes:2);repair.Advance(.05);if(commuter.status!=VehicleStatus.Active||Point.Distance(strandedAt,repair.Transport.Position(commuter))>.7)throw new Exception("Native repaired commute failed / jumped");repair.Advance(6);repair.Validate();
                File.WriteAllText(Path.Combine(path,"IndustryResult.txt"),"PASS actual Unity default city industry including actual resident commuting, real truck lane change and arbitrary split during maneuver: original residents/jobs credit labour, original factory produces, RoadCore trucks move, original shop receives/payments reconcile, exact combined save continues. No old Move or duplicate Depot.\n");
            }
            catch(Exception e){File.WriteAllText(Path.Combine(path,"IndustryResult.txt"),"FAIL "+e);Application.Quit(1);yield break;}
            yield return null;
            var visible=world.Vehicles.FirstOrDefault();Transform rendered=visible==null?null:vehicleViews?.Instance(visible.id);
            if(rendered==null){File.WriteAllText(Path.Combine(path,"IndustryResult.txt"),"FAIL no actual vehicle view");Application.Quit(1);yield break;}
            Vector3 previous=rendered.position;session.Advance(.1);yield return null;
            if(Vector3.Distance(previous,rendered.position)<.5f||Vector3.Distance(rendered.position,V(world.Position(visible))+Vector3.up*.4f)>.001f){File.WriteAllText(Path.Combine(path,"IndustryResult.txt"),"FAIL vehicle view did not track actual movement");Application.Quit(1);yield break;}
            File.AppendAllText(Path.Combine(path,"IndustryResult.txt"),"PASS rendered truck transform follows new-core state across actual Unity frames.\n");
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(path,"Industry.png"));yield return null;Application.Quit(0);
        }
        System.Collections.IEnumerator ValidateConstruction(string path)
        {
            Directory.CreateDirectory(path);speed=0;byte[] trafficStart=null;yield return null;
            building=true;var mouse=Mouse.current;if(mouse==null)mouse=InputSystem.AddDevice<Mouse>();
            foreach(float x in new[]{-30f,30f})
            {
                var screen=cameraView.WorldToScreenPoint(new Vector3(x,landscape.Height(x,-12),-12));var input=new UnityEngine.InputSystem.LowLevel.MouseState{position=new Vector2(screen.x,screen.y)};
                InputSystem.QueueStateEvent(mouse,input.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left,true));yield return null;InputSystem.QueueStateEvent(mouse,input.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left,false));yield return null;
            }
            building=false;
            zoneTool=LandUse.Residential;
            var brushPoint=cameraView.WorldToScreenPoint(new Vector3(-12,landscape.Height(-12,-8),-8));var brushState=new UnityEngine.InputSystem.LowLevel.MouseState{position=new Vector2(brushPoint.x,brushPoint.y)};
            InputSystem.QueueStateEvent(mouse,brushState.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left,true));yield return null;InputSystem.QueueStateEvent(mouse,brushState.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left,false));yield return null;zoneTool=LandUse.Empty;
            try
            {
                if(landscape==null||session.City.buildings.Count!=0||session.Entities.Members.Count!=0)throw new Exception("Default city was not blank / terrain missing");
                if(world.Network.RoadCount!=1||session.City.money>=65000)throw new Exception("Actual two-click terrain road tool/cash failed: "+notice);RenderRoads();
                if(session.City.zoning.designations.Count==0)throw new Exception("Actual terrain zoning brush input failed");
                var zoning=session.Simulation.Growth.Zoning;var plots=zoning.Cells().ToArray();if(plots.Length<30)throw new Exception("Terrain frontage unavailable");zoning.Paint(plots.Where(c=>c.side>0),LandUse.Residential);plots=zoning.Cells().ToArray();zoning.Paint(plots.Where(c=>c.side<0&&c.pose.x<0),LandUse.Commercial);plots=zoning.Cells().ToArray();zoning.Paint(plots.Where(c=>c.side<0&&c.pose.x>=0),LandUse.Industrial);
                session.Advance(6);if(session.City.development.projects.Count!=3)throw new Exception("Zoning did not schedule actual building projects");var restore=CityCoreSave.Read(CityCoreSave.Write(session));restore.Simulation.Growth.Zoning.SetSurface(landscape.Height);
                for(int i=0;i<320;i++){session.Advance(.05);restore.Advance(.05);}session.Validate();restore.Validate();if(!session.City.buildings.Any(b=>b.Use==LandUse.Residential)||!session.City.buildings.Any(b=>b.Use==LandUse.Commercial)||!session.City.buildings.Any(b=>b.Use==LandUse.Industrial))throw new Exception("All three uses did not grow");if(Convert.ToBase64String(CityCoreSave.Write(session))!=Convert.ToBase64String(CityCoreSave.Write(restore)))throw new Exception("Saved construction did not continue identically");if(session.Entities.Members.Count!=0||!LegacyStateAbsent())throw new Exception("Invented population / old runtime network activated");
                foreach(var use in new[]{LandUse.Power,LandUse.Water,LandUse.Sewage})
                {
                    bool placed=false;foreach(var cell in zoning.Cells().Where(c=>c.row==1&&c.available).ToArray())
                    {var lot=CityModel.NewBuilding(use);lot.x=cell.pose.x;lot.z=cell.pose.z;lot.yaw=cell.pose.yaw;if(!zoning.CanPlace(lot)||!zoning.Entrance(lot).HasValue)continue;session.Construction.PlaceFacility(use,lot.x,lot.z,lot.yaw,zoning);placed=true;break;}if(!placed)throw new Exception("Terrain service placement failed: "+use);
                }
                session.Simulation.Utilities.Build(UtilityKind.Electricity,new Point(-29,0,-12),new Point(29,0,-12));session.Simulation.Utilities.Build(UtilityKind.Water,new Point(-29,0,-12),new Point(29,0,-12));
                foreach(var facility in session.City.buildings.Where(b=>b.Use==LandUse.Water||b.Use==LandUse.Sewage))session.Simulation.Utilities.Build(UtilityKind.Water,new Point(facility.x,0,-12),new Point(facility.x,0,facility.z));
                session.Advance(121-world.Clock);if(session.City.population<=0||session.City.Employed<=0)throw new Exception("Serviced private city did not acquire actual residents/jobs");
                int attempts=0;while(world.ActiveCount==0&&attempts++<1200)session.Advance(.05);if(world.ActiveCount==0)throw new Exception("Actual city residents never departed");trafficStart=CityCoreSave.Write(session);restore=CityCoreSave.Read(trafficStart);restore.Simulation.Growth.Zoning.SetSurface(landscape.Height);
                // The old fixed 65s window can end before a full work shift.
                // Observe one complete city day and require the SAME worker to
                // attend and return; do not infer a return from another resident.
                bool attended=false,returned=false;var workersSeen=new HashSet<int>();int cycleSteps=(int)Math.Ceiling(session.City.society.settings.secondsPerDay/SimulationClock.StepSeconds);
                for(int i=0;i<cycleSteps;i++)
                {
                    session.Advance(.05);restore.Advance(.05);
                    foreach(var member in session.Entities.Members)
                    {
                        var person=member.Person;
                        if(person.atWork&&person.workedMinutes>0){attended=true;workersSeen.Add(person.id);}
                        if(workersSeen.Contains(person.id)&&person.canWork&&person.tripId==0&&person.location==member.Family.home&&person.departureDay==session.City.day)returned=true;
                    }
                }
                session.Validate();restore.Validate();if(!attended||!returned)throw new Exception("Actual city commute/work/return loop failed");if(Convert.ToBase64String(CityCoreSave.Write(session))!=Convert.ToBase64String(CityCoreSave.Write(restore)))throw new Exception("Actual city in-flight utility/population continuation differs");
                File.WriteAllBytes(Path.Combine(path,"ConstructionFinal.hcity"),CityCoreSave.Write(session));
                RenderBuildings();RenderZones();RenderUtilities();File.WriteAllText(Path.Combine(path,"ConstructionResult.txt"),"PASS actual Unity blank original terrain; production mouse road/zoning tools and paid construction; private R/C/I growth; electricity, water and sewage facilities/pipes; real migration, job assignment, vehicle commute, attendance and return; unfinished-project and in-flight complete-state save continuation. No old runtime traffic/network. Sustained raw import, consumption, garbage and fiscal management are still pending.\n");
            }
            catch(Exception e){File.WriteAllText(Path.Combine(path,"ConstructionResult.txt"),"FAIL "+e);Application.Quit(1);yield break;}
            QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
            using(var output=new StreamWriter(Path.Combine(path,"playable-window.csv")))
            {
                output.WriteLine("rate,frames,fps,frame_p95_ms,frame_p99_ms,actual_rate,gc_frame_mean_bytes,steps,active_peak,residents,roads,buildings");
                foreach(int rate in new[]{1,3})
                {
                    speed=0;session=CityCoreSave.Read(trafficStart);world=session.Transport;session.Simulation.Growth.Zoning.SetSurface(landscape.Height);RenderRoads();RenderBuildings();RenderZones();RenderUtilities();yield return null;yield return null;speed=rate;previousTick=System.Diagnostics.Stopwatch.GetTimestamp();var recorder=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);var frames=new List<double>(400);double start=Time.realtimeSinceStartupAsDouble,last=start,clock=world.Clock;long allocated=0;int steps=0,peak=world.ActiveCount;
                    while(Time.realtimeSinceStartupAsDouble-start<4){yield return null;double now=Time.realtimeSinceStartupAsDouble;frames.Add((now-last)*1000);last=now;steps+=lastSteps;peak=Math.Max(peak,world.ActiveCount);if(recorder.Valid)allocated+=recorder.LastValue;}
                    double wall=Time.realtimeSinceStartupAsDouble-start;frames.Sort();output.WriteLine(string.Join(",",rate,frames.Count,(frames.Count/wall).ToString("F3",System.Globalization.CultureInfo.InvariantCulture),frames[(int)((frames.Count-1)*.95)].ToString("F3",System.Globalization.CultureInfo.InvariantCulture),frames[(int)((frames.Count-1)*.99)].ToString("F3",System.Globalization.CultureInfo.InvariantCulture),((world.Clock-clock)/wall).ToString("F4",System.Globalization.CultureInfo.InvariantCulture),recorder.Valid?allocated/frames.Count:-1,steps,peak,session.City.population,world.Network.RoadCount,session.City.buildings.Count));recorder.Dispose();
                }
            }
            speed=0;session.Validate();File.AppendAllText(Path.Combine(path,"ConstructionResult.txt"),"PASS 1x/3x real original-terrain growing-city window FPS/time/GC sampling; small city only, not complete sustainable economy or large-city proof.\n");
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(path,"Construction.png"));yield return null;Application.Quit(0);
        }
#if CITY_CORE_ATTRIBUTION
        System.Collections.IEnumerator ProfileGrowingCity(string path)
        {
            Directory.CreateDirectory(path);speed=0;yield return null;
            try{session=CityGrowingGridFixture.Create(landscape.Height,HarborCityEconomyAsset.Load());CityGrowingGridFixture.PlaceWasteFacility(session);world=session.Transport;session.Advance(18);RenderRoads();RenderBuildings();RenderZones();RenderUtilities();}
            catch(Exception e){File.WriteAllText(Path.Combine(path,"ProfileResult.txt"),"FAIL setup "+e);Application.Quit(1);yield break;}
            UnityEngine.Profiling.Profiler.logFile=Path.Combine(path,"growing-city.raw");UnityEngine.Profiling.Profiler.enableBinaryLog=true;UnityEngine.Profiling.Profiler.enabled=true;CityCoreProfile.Enabled=true;
            using(var output=new StreamWriter(Path.Combine(path,"steps.csv")))
            {
                output.WriteLine(CityCoreProfile.Header());
                for(int batch=0;batch<600;batch++)
                {
                    try{for(int i=0;i<20;i++){CityCoreProfile.Reset();int a=GC.CollectionCount(0),b=GC.CollectionCount(1),c=GC.CollectionCount(2);long routes=world.RoutePreparations;session.Advance(.05);output.WriteLine(CityCoreProfile.Row(session.Simulation.Tick,world.ActiveCount,world.RoutePreparations-routes,GC.CollectionCount(0)-a,GC.CollectionCount(1)-b,GC.CollectionCount(2)-c));}}
                    catch(Exception e){File.WriteAllText(Path.Combine(path,"ProfileResult.txt"),"FAIL step "+e);Application.Quit(1);yield break;}
                    yield return null;
                }
            }
            CityCoreProfile.Enabled=false;UnityEngine.Profiling.Profiler.enabled=false;UnityEngine.Profiling.Profiler.enableBinaryLog=false;
            try{session.Validate();RenderBuildings();File.WriteAllText(Path.Combine(path,"ProfileResult.txt"),"PASS original terrain integrated consumer city: "+session.City.population+" people / "+session.City.buildings.Count+" buildings / day "+session.City.day+" / consumed "+session.City.consumption.ConsumedGoods+". 12000 profiled real fixed steps; CSV excludes views/CSV writing, raw includes rendered frames. Instrumented cost is not normal-game FPS.");}
            catch(Exception e){File.WriteAllText(Path.Combine(path,"ProfileResult.txt"),"FAIL final "+e);Application.Quit(1);yield break;}
            Application.Quit(0);
        }
#endif
        System.Collections.IEnumerator ValidateTrade(string path)
        {
            Directory.CreateDirectory(path);speed=0;yield return null;
            try
            {
                session=CityGrowingGridFixture.Create(landscape.Height,HarborCityEconomyAsset.Load());CityGrowingGridFixture.PlaceWasteFacility(session);world=session.Transport;
                cameraView.transform.position=new Vector3(-95,100,-65);cameraView.transform.LookAt(new Vector3(-95,landscape.Height(-95,0),0));RenderRoads();RenderBuildings();RenderZones();RenderUtilities();
                session.Advance(18);if(!world.Tasks.Any(t=>t.Shipment?.kind==CargoKind.RawMaterial))throw new Exception("Native original terrain grid did not dispatch raw materials");
            }
            catch(Exception e){File.WriteAllText(Path.Combine(path,"TradeResult.txt"),"FAIL "+e);Application.Quit(1);yield break;}
            var clone=CityCoreSave.Read(CityCoreSave.Write(session));clone.Simulation.Growth.Zoning.SetSurface(landscape.Height);
            byte[] serviceWindow=null;
            for(int batch=0;batch<300;batch++)
            {
                try{for(int i=0;i<20;i++){session.Advance(.05);clone.Advance(.05);}}
                catch(Exception e){File.WriteAllText(Path.Combine(path,"TradeResult.txt"),"FAIL step "+e);Application.Quit(1);yield break;}
                if(serviceWindow==null&&world.Tasks.Any(t=>t.purpose==TransportPurpose.Service))serviceWindow=CityCoreSave.Write(session);
                yield return null;
            }
            byte[] sample;
            try
            {
                session.Validate();clone.Validate();if(session.Simulation.Waste.Generated<=0||session.Simulation.Waste.Processed<=0||session.Simulation.Waste.BalanceError!=0)throw new Exception("Native waste collection/processing failed");File.WriteAllText(Path.Combine(path,"waste.csv"),"day,generated,collected,waiting,in_transit,stored,processed,service_vehicles,balance_error\n"+string.Join(",",session.City.day,session.Simulation.Waste.Generated,session.Simulation.Waste.Collected,session.Simulation.Waste.Waiting,session.Simulation.Waste.InTransit,session.Simulation.Waste.Stored,session.Simulation.Waste.Processed,session.Simulation.Waste.Vehicles,session.Simulation.Waste.BalanceError));RenderBuildings();int created=buildingViews.Created;var bindings=session.Entities.Buildings.Select(b=>buildingViews.Instance(b.id)).ToArray();var beforeViews=CityCoreSave.Write(session);bool viewCounter=CityCoreProfile.AllocationAvailable;var viewTimer=new System.Diagnostics.Stopwatch();long viewAllocation=GC.GetAllocatedBytesForCurrentThread();viewTimer.Start();for(int i=0;i<20;i++)RenderBuildings();viewTimer.Stop();viewAllocation=viewCounter?GC.GetAllocatedBytesForCurrentThread()-viewAllocation:-1;File.WriteAllText(Path.Combine(path,"building-views.csv"),"buildings,sync_calls,created_before,created_after,sync_mean_ms,total_allocated_bytes\n"+string.Join(",",buildingViews.Count,20,created,buildingViews.Created,(viewTimer.Elapsed.TotalMilliseconds/20).ToString("F6",System.Globalization.CultureInfo.InvariantCulture),viewAllocation));if(buildingViews.Created!=created||!bindings.SequenceEqual(session.Entities.Buildings.Select(b=>buildingViews.Instance(b.id)))||!beforeViews.SequenceEqual(CityCoreSave.Write(session)))throw new Exception("Read-only building views rebuilt objects or changed simulation");if(Convert.ToBase64String(CityCoreSave.Write(session))!=Convert.ToBase64String(CityCoreSave.Write(clone)))throw new Exception("Native trade continuation mismatch");
                int produced=session.Entities.Factories.Sum(f=>f.factory.produced),goods=session.Entities.Factories.Sum(f=>f.Stock)+session.Entities.Shops.Sum(s=>s.Stock)+world.Tasks.Where(t=>t.Shipment?.kind==CargoKind.Goods).Sum(t=>t.Cargo)+session.City.trade.goodsExported+session.City.consumption.ConsumedGoods+session.City.Citizens.Sum(p=>p.carriedGoods);
                if(session.City.population<=0||session.City.Employed<=0||produced<=0||produced!=goods||session.City.consumption.ConsumedGoods<=0)throw new Exception("Native multi-day labour/import/export conservation failed");
                sample=serviceWindow??throw new Exception("Native city never observed an active garbage vehicle");File.WriteAllText(Path.Combine(path,"TradeResult.txt"),"PASS actual original Unity terrain; empty city production construction commands, reachable grid, utilities, private growth, real people/jobs/commuting, paid material trucks and actual wholesale deliveries. Multi-day inventory/market escrow, immutable asset profile and in-flight save continuation. Actual shopping vehicles, paid employer wages, finite commercial accounts, carried home goods and consumption verified. Stable-ID building views retain objects and leave simulation save unchanged. Fiscal and real garbage verification follow.\n");
            }
            catch(Exception e){File.WriteAllText(Path.Combine(path,"TradeResult.txt"),"FAIL final "+e);Application.Quit(1);yield break;}
            QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
            using(var output=new StreamWriter(Path.Combine(path,"trade-window.csv")))
            {
                output.WriteLine("rate,frames,fps,frame_p95_ms,frame_p99_ms,actual_rate,gc_frame_mean_bytes,steps,active_peak,residents,roads,buildings,raw_delivered,goods_exported,service_active_peak");
                foreach(int rate in new[]{1,3})
                {
                    speed=0;session=CityCoreSave.Read(sample);world=session.Transport;session.Simulation.Growth.Zoning.SetSurface(landscape.Height);RenderRoads();RenderBuildings();RenderZones();RenderUtilities();shownBuildings=shownZones=shownNetwork=-1;observation.Bind(session);observation.Close();roadEditor.Bind(session);roadEditor.Begin();int selectedRoad=world.Network.RoadIds.Max();var selectedGeometry=world.Network.Centreline(selectedRoad);roadEditor.Pick(selectedGeometry.Sample(selectedGeometry.Length/2));if(roadEditor.Selected!=selectedRoad)throw new Exception("Native road editor window selection failed");yield return null;yield return null;
                    speed=rate;previousTick=System.Diagnostics.Stopwatch.GetTimestamp();var recorder=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);var frames=new List<double>(400);double start=Time.realtimeSinceStartupAsDouble,last=start,clock=world.Clock;long allocated=0;int steps=0,peak=world.ActiveCount,servicePeak=session.Simulation.Waste.Vehicles;
                    while(Time.realtimeSinceStartupAsDouble-start<4){yield return null;double now=Time.realtimeSinceStartupAsDouble;frames.Add((now-last)*1000);last=now;steps+=lastSteps;peak=Math.Max(peak,world.ActiveCount);servicePeak=Math.Max(servicePeak,session.Simulation.Waste.Vehicles);if(recorder.Valid)allocated+=recorder.LastValue;}
                    double wall=Time.realtimeSinceStartupAsDouble-start;frames.Sort();output.WriteLine(string.Join(",",rate,frames.Count,(frames.Count/wall).ToString("F3",System.Globalization.CultureInfo.InvariantCulture),frames[(int)((frames.Count-1)*.95)].ToString("F3",System.Globalization.CultureInfo.InvariantCulture),frames[(int)((frames.Count-1)*.99)].ToString("F3",System.Globalization.CultureInfo.InvariantCulture),((world.Clock-clock)/wall).ToString("F4",System.Globalization.CultureInfo.InvariantCulture),recorder.Valid?allocated/frames.Count:-1,steps,peak,session.City.population,world.Network.RoadCount,session.City.buildings.Count,session.City.trade.rawDelivered,session.City.trade.goodsExported,servicePeak));recorder.Dispose();
                }
            }
            speed=0;
            try{
                var finance=session.Simulation.Finance;int beforeLoan=finance.Money;finance.Borrow(0);if(finance.Money!=beforeLoan+20000||finance.Debt!=21000)throw new Exception("Native loan credit failed");
                session.Simulation.Utilities.SetBudget(CityServiceKind.Water,50);if(session.City.water!=80||session.City.SewageCapacity!=80)throw new Exception("Native budget did not affect real services");session.Simulation.Utilities.SetBudget(CityServiceKind.Water,100);
                finance.SetTax(LandUse.Residential,10);var saved=CityCoreSave.Read(CityCoreSave.Write(session));saved.Simulation.Growth.Zoning.SetSurface(landscape.Height);for(int i=0;i<200;i++){session.Advance(.05);saved.Advance(.05);}if(!CityCoreSave.Write(session).SequenceEqual(CityCoreSave.Write(saved)))throw new Exception("Native fiscal continuation mismatch");
                int contract=finance.ActiveLoanId(0),owed=finance.Remaining(contract),balance=finance.Money;finance.Repay(contract);if(finance.Money!=balance-owed||finance.Debt!=0)throw new Exception("Native early repayment failed");session.Validate();
                File.WriteAllText(Path.Combine(path,"finance.csv"),"day,treasury,tax_collected,maintenance_paid,public_works,debt\n"+string.Join(",",session.City.day,finance.Money,finance.TaxesCollected,finance.MaintenancePaid,finance.PublicWorks,finance.Debt));
                File.AppendAllText(Path.Combine(path,"TradeResult.txt"),"PASS native fiscal commands: paid construction ledger, real taxes and maintenance, captured loan terms/credit/early payoff, actual water/sewage budget capacity and complete fiscal in-flight save continuation. UI drawn separately; no manual mouse-click acceptance claimed.\n");financePanel.IsOpen=true;if(!financePanel.BlocksInput(Vector2.zero))throw new Exception("Modal finance panel allowed map construction input");
            }catch(Exception e){File.WriteAllText(Path.Combine(path,"TradeResult.txt"),"FAIL native fiscal "+e);Application.Quit(1);yield break;}
            yield return null;yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(path,"Finance.png"));financePanel.IsOpen=false;yield return null;
            try
            {
                RenderBuildings();var home=session.Entities.Buildings.OfType<ResidentialBuilding>().First(b=>session.City.society.families.Any(h=>h.resident&&h.home==b.id));var before=CityCoreSave.Write(session);observation.Bind(session);observation.Select(home.id);observation.SelectFamily(session.City.society.families.First(h=>h.resident&&h.home==home.id).id);
                int picked=buildingViews.Pick(new Ray(new Vector3(home.x,home.groundY+30,home.z),Vector3.down));if(picked!=home.id)throw new Exception("Native building ray selection returned wrong stable ID");
                buildingViews.Select(home.id);var query=new CityCoreObservation(session);var timer=System.Diagnostics.Stopwatch.StartNew();for(int i=0;i<100;i++){query.Read(home.id);query.Issues();}timer.Stop();
                if(!before.SequenceEqual(CityCoreSave.Write(session)))throw new Exception("Inspection changed live simulation");
                File.WriteAllText(Path.Combine(path,"observation.csv"),"selected,residents,buildings,queries,mean_query_pair_ms,save_unchanged\n"+string.Join(",",home.id,session.City.population,session.Entities.Buildings.Count,100,(timer.Elapsed.TotalMilliseconds/100).ToString("F6",System.Globalization.CultureInfo.InvariantCulture),true));
                File.AppendAllText(Path.Combine(path,"TradeResult.txt"),"PASS native inspection: rendered bounds ray maps stable building ID, selected outline, real family/workforce/commuting queries, read-only full save equality; Inspection drawn/queried separately; 1x/3x windows in this batch include selected-road editor. No manual mouse click acceptance claimed.\n");
            }catch(Exception e){File.WriteAllText(Path.Combine(path,"TradeResult.txt"),"FAIL native observation "+e);Application.Quit(1);yield break;}
            observation.Close();Exception roadFault=null;yield return HarborCityRoadEditingValidation.Run(session,roadEditor,landscape.Height,path,e=>roadFault=e);if(roadFault!=null){File.AppendAllText(Path.Combine(path,"TradeResult.txt"),"\nFAIL road editor "+roadFault);Application.Quit(1);yield break;}File.AppendAllText(Path.Combine(path,"TradeResult.txt"),"\n"+File.ReadAllText(Path.Combine(path,"RoadEditingResult.txt")));
            session.Validate();yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(path,"Trade.png"));yield return null;Application.Quit(0);
        }
        void OnDestroy(){if(citySkin!=null)Destroy(citySkin);zoneBoxTool?.Dispose();gridTool?.Dispose();vehicleViews?.Dispose();outsideViews?.Dispose();constructionPreview?.Dispose();roadEditor?.Dispose();buildingViews?.Dispose();roadViews?.Dispose();zoningViews?.Dispose();if(zoneMaterials!=null)foreach(var m in zoneMaterials)Destroy(m);if(roadMaterial!=null)Destroy(roadMaterial);if(carMaterial!=null)Destroy(carMaterial);if(waterLineMaterial!=null)Destroy(waterLineMaterial);foreach(var view in utilityViews)if(view!=null)Destroy(view);}
    }
}
