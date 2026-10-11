using System;
using System.Linq;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace HarborCity
{
    // Opt-in unattended player smoke test; never runs during normal play.
    public sealed class HarborCityStandaloneValidation : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-harbor-validate");
            if(index<0 || index+1>=args.Length) return;
            var run=new GameObject("Standalone validation").AddComponent<HarborCityStandaloneValidation>();run.output=Path.GetFullPath(args[index+1]);
            Application.runInBackground=true;
        }
        string output;
        public static void ConnectSupplies(CityModel city,Func<float,float,float> height)
        {
            var end=new RoadNode{x=-68,z=1.5f,y=height(-68,1.5f)};
            var road=city.roads.Plan(city,city.roads.Node(CityRoads.Entrance),end,height);
            if(!city.CommitRoad(road))throw new InvalidOperationException("Drain road: "+road.error);
            var drain=city.RoadsidePreview(-64,-5,LandUse.Sewage,HousingKind.Villa,out string error);
            if(city.PlaceBuilding(drain,LandUse.Sewage,height,out error)<0)throw new InvalidOperationException(error);
            var landfill=city.RoadsidePreview(-34,-5,LandUse.Landfill,HousingKind.Villa,out error);
            if(city.PlaceBuilding(landfill,LandUse.Landfill,height,out error)<0)throw new InvalidOperationException(error);
            if(!city.BuildUtility(UtilityKind.Water,new RoadNode{x=-64,z=0},new RoadNode{x=28,z=0},out error)
                || !city.BuildUtility(UtilityKind.Electricity,new RoadNode{x=-64,z=0},new RoadNode{x=28,z=0},out error))throw new InvalidOperationException(error);
        }
        IEnumerator Start()
        {
            for(int n=0;n<12;n++) yield return null;
            bool ok=true;
            try
            {
                Directory.CreateDirectory(output);var game=FindAnyObjectByType<HarborCityGame>();
                const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;var type=typeof(HarborCityGame);
                var city=(CityModel)type.GetField("city",flags).GetValue(game);var land=(CityLandscape)type.GetField("landscape",flags).GetValue(game);
                CheckMapExpansion(land,output);
                var end=new RoadNode{x=28,z=1.5f,y=land.Height(28,1.5f)};var road=city.roads.Plan(city,city.roads.Node(CityRoads.Entrance),end,land.Height);
                if(!city.CommitRoad(road)) throw new InvalidOperationException(road.error??"Road failed");
                var cells=city.ZoningCells(land.Height);city.PaintZones(System.Linq.Enumerable.Where(cells,c=>c.pose.x<-22),LandUse.Residential);
                city.PaintZones(System.Linq.Enumerable.Where(cells,c=>c.pose.x>=-22 && c.pose.x<2),LandUse.Commercial);
                city.PaintZones(System.Linq.Enumerable.Where(cells,c=>c.pose.x>=2),LandUse.Industrial);
                foreach(var service in new[]{LandUse.Power,LandUse.Water})
                {
                    var lot=city.RoadsidePreview(service==LandUse.Power?-47:-40,-5,service,HousingKind.Apartment,out string error);
                    if(city.PlaceBuilding(lot,service,land.Height,out error)<0) throw new InvalidOperationException(error);
                }
                ConnectSupplies(city,land.Height);
                var traffic=(CityTraffic)type.GetField("traffic",flags).GetValue(game);traffic.Advance(600);
                if(!city.Valid() || city.population<=0 || city.Employed<=0) throw new InvalidOperationException("Standalone city did not develop valid residents and jobs");
                if(city.traffic.consumedGoods<=0 || !System.Linq.Enumerable.Any(System.Linq.Enumerable.OfType<CommercialBuilding>(city.buildings),b=>b.retailSold>0))
                    throw new InvalidOperationException("Standalone residents did not actually shop and bring goods home");
                string json=JsonUtility.ToJson(city.ToSaveData(),true);var restored=JsonUtility.FromJson<CitySaveData>(json).ToCity();
                if(!restored.Valid() || restored.population!=city.population) throw new InvalidOperationException("Player save round trip failed");
                var healthProbe=JsonUtility.FromJson<CitySaveData>(json).ToCity();
                var patient=healthProbe.Citizens.First();patient.health=35;patient.sick=true;int patientJob=patient.jobId;
                healthProbe=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(healthProbe.ToSaveData())).ToCity();
                patient=healthProbe.Citizens.First(p=>p.id==patient.id);
                if(!patient.sick || patient.health!=35 || patient.jobId!=patientJob || healthProbe.CanAttendWork(patient) || healthProbe.SickResidents!=1)
                    throw new InvalidOperationException("Native personal health state did not round-trip");
                File.WriteAllText(Path.Combine(output,"Health.txt"),"PASS: native personal health, illness, retained Job and attendance gate");
                CheckHealthcare(city,land.Height,output);
                CheckEducation(land.Height,output);
                CheckResidentIdentity(city,output);
                CheckLoans(city,output);
                CheckUtilityAllocation(city,output);
                CheckRoadDirection(output);
                CheckJunctions(output);
                CheckRoadTypes(output);
                CheckLanes(game,output);
                CheckWideRoads(game,output);
                CheckLaneChoice(game,output);
                CheckVehicleTurns(game,output);
                CheckHighways(game,output);
                CheckHighwayMerging(output);
                CheckTrafficControls(game,output);
                CheckPedestrians(game,output);
                CheckPedestrians(game,output,true);
                CheckPedestrians(game,output,false,.5f);
                CheckPedestrians(game,output,false,-.5f);
                CheckElevatedTraffic(game,output);
                CheckDeathcare(land.Height,output);
                CheckAging(output);
                CheckHigherEducation(land.Height,output);
                CheckFire(land.Height,output);
                CheckPolice(land.Height,output);
                CheckServiceRoadChanges(output);
                int standardExpense=restored.upkeep;restored.SetServiceBudget(CityServiceKind.Electricity,150);restored.SetServiceBudget(CityServiceKind.Water,150);restored.SetServiceBudget(CityServiceKind.Garbage,50);
                restored=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(restored.ToSaveData())).ToCity();
                if(restored.power!=240 || restored.water!=240 || restored.GarbageFleetLimit!=1 || restored.upkeep<=standardExpense)
                    throw new InvalidOperationException("Native service budget capacity, fleet or expense did not survive save");
                File.WriteAllText(Path.Combine(output,"Budgets.txt"),"PASS: native budget JSON, actual connected supply, finite dispatch allowance and maintenance response");
                restored.SetServiceBudget(CityServiceKind.Electricity,100);restored.SetServiceBudget(CityServiceKind.Water,100);restored.SetServiceBudget(CityServiceKind.Garbage,100);
                restored.developmentHeight=land.Height;new CityTraffic(restored).Advance(2401);
                if(!restored.Valid() || restored.population<=0 || restored.Employed<=0 || restored.WasteBalanceError!=0 || restored.day<25)
                    throw new InvalidOperationException("Long native city lost population, employment or material validity");
                File.WriteAllText(Path.Combine(output,"LongRun.txt"),"PASS: native continued operation; day="+restored.day+"; population="+restored.population+"; employed="+restored.Employed+"; consumed="+restored.traffic.consumedGoods+"; wasteBalanceError="+restored.WasteBalanceError);
                restored.waste.enabled=false;var restorePipes=restored.utilities.water.Copy();restored.utilities.water=new CityUtilityNetwork();restored.Recalculate();
                for(int n=0;n<restored.development.provisionalOutageDays;n++)restored.Tick();
                if(restored.population!=0 || restored.jobs!=0 || !restored.Valid())throw new InvalidOperationException("Player outage did not release real residents and jobs");
                restored=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(restored.ToSaveData())).ToCity();
                restored.utilities.water=restorePipes;restored.Recalculate();
                for(int n=0;n<CityModel.AbandonedRecoveryDays;n++)restored.Tick();
                if(restored.population<=0 || restored.jobs<=0 || !restored.Valid())throw new InvalidOperationException("Player abandoned-city recovery failed");
                File.WriteAllText(Path.Combine(output,"Conditions.txt"),"PASS: outage, actual household/job release, native abandoned-state save, seven-day minimum and real migration recovery");
                File.WriteAllText(Path.Combine(output,"City.json"),json);
                type.GetField("validationSavePath",flags).SetValue(game,Path.Combine(output,"harbor-city.json"));
                type.GetMethod("Save",flags).Invoke(game,null);type.GetMethod("Save",flags).Invoke(game,null);
                if(!File.Exists(Path.Combine(output,"harbor-city.json.bak"))) throw new InvalidOperationException("Atomic replacement did not retain backup");
                int treasury=city.money;city.money++;
                type.GetMethod("Load",flags).Invoke(game,null);
                city=(CityModel)type.GetField("city",flags).GetValue(game);
                if(city.money!=treasury || !city.Valid()) throw new InvalidOperationException("Actual game save/load failed");
                File.WriteAllText(Path.Combine(output,"harbor-city.json"),"invalid json");
                var retained=city;type.GetMethod("Load",flags).Invoke(game,null);
                if(!ReferenceEquals(retained,type.GetField("city",flags).GetValue(game))) throw new InvalidOperationException("Bad save destroyed current city");
                type.GetMethod("Save",flags).Invoke(game,null);
                if(!JsonUtility.FromJson<CitySaveData>(File.ReadAllText(Path.Combine(output,"harbor-city.json.bak"))).ToCity().Valid())
                    throw new InvalidOperationException("Repairing bad primary save destroyed valid backup");
                File.WriteAllText(Path.Combine(output,"Result.txt"),"PASS: standalone growth, real citizens/jobs/trips/shopping and native JSON; population="+city.population+"; employed="+city.Employed+"; jobs="+city.jobs+"; buildings="+city.buildings.Count+"; income="+city.income+"; trafficCompleted="+city.traffic.completed+"; consumedGoods="+city.traffic.consumedGoods+"; wasteBalanceError="+city.WasteBalanceError);
                type.GetField("speed",flags).SetValue(game,0f);type.GetField("selected",flags).SetValue(game,LandUse.Residential);
                type.GetField("focus",flags).SetValue(game,new Vector3(-10,0,1.5f));type.GetField("zoom",flags).SetValue(game,48f);
                type.GetMethod("Rebuild",flags).Invoke(game,null);type.GetMethod("RefreshDevelopmentView",flags).Invoke(game,null);
                type.GetMethod("RefreshZoningOverlay",flags).Invoke(game,null);type.GetMethod("UpdateCamera",flags).Invoke(game,null);
            }
            catch(Exception ex) {ok=false;Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"Failure.txt"),ex.ToString());}
            if(ok)
            {
                // Exercise the real world-space tool input in the isolated player.
                // This does not validate IMGUI button interaction or original-game parity.
                var inputGame=FindAnyObjectByType<HarborCityGame>();
                const BindingFlags inputFlags=BindingFlags.Instance|BindingFlags.NonPublic;
                var inputType=typeof(HarborCityGame);
                inputType.GetField("selected",inputFlags).SetValue(inputGame,LandUse.Water);
                inputType.GetField("waterTool",inputFlags).SetValue(inputGame,1);
                var inputCity=(CityModel)inputType.GetField("city",inputFlags).GetValue(inputGame);
                int before=inputCity.utilities.water.edges.Count;
                var inputCam=(Camera)inputType.GetField("cam",inputFlags).GetValue(inputGame);
                var inputLand=(CityLandscape)inputType.GetField("landscape",inputFlags).GetValue(inputGame);
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                var device=InputSystem.AddDevice<Mouse>("ValidationMouse");
                foreach(var point in new[]{new Vector2(-4,20),new Vector2(16,20)})
                {
                    var screen=inputCam.WorldToScreenPoint(new Vector3(point.x,inputLand.Height(point.x,point.y),point.y));
                    File.AppendAllText(Path.Combine(output,"Input.txt"),"screen="+screen+"; blocked="+inputType.GetMethod("OverUI",inputFlags).Invoke(inputGame,new object[]{new Vector2(screen.x,screen.y)})+Environment.NewLine);
                    InputSystem.QueueDeltaStateEvent(device.position,new Vector2(screen.x,screen.y));
                    for(int n=0;n<2;n++)yield return null;
                    InputSystem.QueueStateEvent(device,new MouseState{position=new Vector2(screen.x,screen.y)}.WithButton(MouseButton.Left));yield return null;yield return null;
                    File.AppendAllText(Path.Combine(output,"Input.txt"),"current="+Mouse.current.name+"; left="+device.leftButton.isPressed+"; start="+(inputType.GetField("utilityStart",inputFlags).GetValue(inputGame)!=null)+"; edges="+inputCity.utilities.water.edges.Count+Environment.NewLine);
                    InputSystem.QueueStateEvent(device,new MouseState{position=new Vector2(screen.x,screen.y)});yield return null;yield return null;
                }
                if(inputCity.utilities.water.edges.Count!=before+1)
                {ok=false;File.WriteAllText(Path.Combine(output,"Failure.txt"),"Actual pipe world-input path failed");}
                else File.AppendAllText(Path.Combine(output,"Result.txt"),"; pipe world-input PASS");
                inputType.GetField("selected",inputFlags).SetValue(inputGame,LandUse.Road);
                inputType.GetField("roadOneWay",inputFlags).SetValue(inputGame,true);
                int roadEdges=inputCity.roads.edges.Count;
                yield return ClickWorld(inputGame,device,new Vector2(28,1.5f));
                yield return ClickWorld(inputGame,device,new Vector2(28,20));
                if(inputCity.roads.edges.Count<=roadEdges)
                {ok=false;File.WriteAllText(Path.Combine(output,"Failure.txt"),"Actual road world-input path failed");}
                else File.AppendAllText(Path.Combine(output,"Result.txt"),"; road world-input PASS");
                var directed=inputCity.roads.edges.Skip(roadEdges).ToList();
                if(directed.Count==0 || directed.Any(e=>!e.oneWay)){ok=false;File.WriteAllText(Path.Combine(output,"Failure.txt"),"One-way construction input failed");}
                inputType.GetField("roadOperation",inputFlags).SetValue(inputGame,2);
                yield return ClickWorld(inputGame,device,new Vector2(28,12));
                if(directed.Any(e=>inputCity.roads.Node(e.a).z<inputCity.roads.Node(e.b).z)){ok=false;File.WriteAllText(Path.Combine(output,"Failure.txt"),"One-way reverse world-input failed");}
                inputType.GetField("roadOperation",inputFlags).SetValue(inputGame,1);
                inputType.GetField("roadOneWay",inputFlags).SetValue(inputGame,false);
                yield return ClickWorld(inputGame,device,new Vector2(28,12));
                if(directed.Any(e=>e.oneWay)){ok=false;File.WriteAllText(Path.Combine(output,"Failure.txt"),"Two-way conversion world-input failed");}
                else File.AppendAllText(Path.Combine(output,"Result.txt"),"; directed construction/reverse/conversion world-input PASS");
                inputType.GetField("roadKind",inputFlags).SetValue(inputGame,RoadKind.Gravel);
                yield return ClickWorld(inputGame,device,new Vector2(28,12));
                if(directed.Any(e=>e.kind!=RoadKind.Gravel)){ok=false;File.WriteAllText(Path.Combine(output,"Failure.txt"),"Gravel conversion world-input failed");}
                int pavingFunds=inputCity.money;
                inputCity.PreviewRoadUpgrade(directed[0].id,RoadKind.Standard,false,out int pavingCost,out _);
                inputType.GetField("roadKind",inputFlags).SetValue(inputGame,RoadKind.Standard);
                yield return ClickWorld(inputGame,device,new Vector2(28,12));
                if(directed.Any(e=>e.kind!=RoadKind.Standard) || pavingCost<=0 || inputCity.money!=pavingFunds-pavingCost){ok=false;File.WriteAllText(Path.Combine(output,"Failure.txt"),"Paid paving world-input failed");}
                else File.AppendAllText(Path.Combine(output,"Result.txt"),"; gravel/paid paving world-input PASS");
                inputType.GetField("roadOperation",inputFlags).SetValue(inputGame,0);
                inputType.GetField("roadKind",inputFlags).SetValue(inputGame,RoadKind.FourLane);int wideStart=inputCity.roads.edges.Count;
                // Use a genuine legal terrain site; a wider cross-slope may reject
                // the narrow-road fixture's hillside. Do not relax slope rules.
                Vector2 wideFrom=default,wideTo=default;bool wideSite=false;
                // Keep the continuous-curve corridor (28,20 -> 10,16 -> -8,8)
                // clear. A now-legal westbound wide branch would occupy it and
                // correctly fail the later curve's junction-angle validation.
                var candidates=new[]{new[]{new Vector2(0,-20),new Vector2(20,-20)},new[]{new Vector2(-20,-20),new Vector2(0,-20)},new[]{new Vector2(28,20),new Vector2(28,40)}};
                foreach(var pair in candidates)
                {
                    var from=inputCity.roads.Snap(pair[0].x,pair[0].y,inputLand.Height);var to=inputCity.roads.Snap(pair[1].x,pair[1].y,inputLand.Height);
                    if(!inputCity.roads.Plan(inputCity,from,to,inputLand.Height,false,RoadKind.FourLane).Valid)continue;
                    wideFrom=new Vector2(from.x,from.z);wideTo=new Vector2(to.x,to.z);wideSite=true;break;
                }
                if(!wideSite){ok=false;File.WriteAllText(Path.Combine(output,"Failure.txt"),"No legal wide-road terrain site for world-input validation");}
                else {yield return ClickWorld(inputGame,device,wideFrom);yield return ClickWorld(inputGame,device,wideTo);}
                var wide=inputCity.roads.edges.Skip(wideStart).ToList();
                if(wide.Count==0 || wide.Any(e=>e.kind!=RoadKind.FourLane || inputCity.roads.LaneCount(e.a,e.b)!=2 || inputCity.roads.LaneCount(e.b,e.a)!=2))
                {ok=false;File.WriteAllText(Path.Combine(output,"Failure.txt"),"Wide construction world-input failed: "+inputType.GetField("notice",inputFlags).GetValue(inputGame));}
                else
                {
                    inputType.GetField("roadOperation",inputFlags).SetValue(inputGame,1);inputType.GetField("roadKind",inputFlags).SetValue(inputGame,RoadKind.Standard);
                    yield return ClickWorld(inputGame,device,(wideFrom+wideTo)/2);
                    int wideningFunds=inputCity.money;bool preview=inputCity.PreviewRoadUpgrade(wide[0].id,RoadKind.FourLane,false,out int wideningCost,out _,inputLand.Height);
                    inputType.GetField("roadKind",inputFlags).SetValue(inputGame,RoadKind.FourLane);
                    yield return ClickWorld(inputGame,device,(wideFrom+wideTo)/2);
                    if(!preview || wideningCost<=0 || wide.Any(e=>e.kind!=RoadKind.FourLane) || inputCity.money!=wideningFunds-wideningCost)
                    {ok=false;File.WriteAllText(Path.Combine(output,"Failure.txt"),"Paid widening world-input failed: "+inputType.GetField("notice",inputFlags).GetValue(inputGame));}
                    else File.AppendAllText(Path.Combine(output,"Result.txt"),"; wide construction/paid widening world-input PASS");
                }
                inputType.GetField("roadOperation",inputFlags).SetValue(inputGame,0);inputType.GetField("roadKind",inputFlags).SetValue(inputGame,RoadKind.Standard);
                yield return CheckHighwayInput(inputGame,device,output);
                yield return CheckExpandedMapInput(inputGame,device,output);
                inputType.GetField("roadOperation",inputFlags).SetValue(inputGame,0);inputType.GetField("roadKind",inputFlags).SetValue(inputGame,RoadKind.Standard);inputType.GetField("roadOneWay",inputFlags).SetValue(inputGame,false);
                inputType.GetField("selected",inputFlags).SetValue(inputGame,LandUse.Residential);
                inputType.GetField("zoneTool",inputFlags).SetValue(inputGame,0);
                inputType.GetField("zoneBrush",inputFlags).SetValue(inputGame,0);
                var targetCell=System.Linq.Enumerable.FirstOrDefault(inputCity.ZoningCells(inputLand.Height),c=>c.available && c.use==LandUse.Empty && c.pose.x>28 && c.pose.z>8 && c.pose.z<16);
                if(targetCell==null){ok=false;File.WriteAllText(Path.Combine(output,"Failure.txt"),"New road has no development frontage");}
                else
                {
                    yield return ClickWorld(inputGame,device,new Vector2(targetCell.pose.x,targetCell.pose.z));
                    var painted=inputCity.PickZone(targetCell.pose.x,targetCell.pose.z,inputLand.Height);
                    if(painted==null || painted.use!=LandUse.Residential){ok=false;File.WriteAllText(Path.Combine(output,"Failure.txt"),"Actual zoning world-input path failed");}
                else File.AppendAllText(Path.Combine(output,"Result.txt"),"; zoning world-input PASS");
                }
                inputType.GetField("selected",inputFlags).SetValue(inputGame,LandUse.Road);
                inputType.GetField("roadMode",inputFlags).SetValue(inputGame,2);
                inputType.GetMethod("ResetRoad",inputFlags).Invoke(inputGame,null);
                int beforeCurves=inputCity.roads.nextStroke;
                yield return ClickWorld(inputGame,device,new Vector2(28,20));
                yield return ClickWorld(inputGame,device,new Vector2(20,20));
                yield return ClickWorld(inputGame,device,new Vector2(10,16));
                yield return ClickWorld(inputGame,device,new Vector2(-8,8));
                if(inputCity.roads.nextStroke!=beforeCurves+2)
                {ok=false;File.WriteAllText(Path.Combine(output,"Failure.txt"),"Actual continuous curve world-input failed: "+inputType.GetField("notice",inputFlags).GetValue(inputGame));}
                else File.AppendAllText(Path.Combine(output,"Result.txt"),"; continuous curve world-input PASS");
                yield return CheckTrafficControlInput(inputGame,device);
                yield return CheckWideConnectionInput(inputGame,device);
                if(File.Exists(Path.Combine(output,"Failure.txt")))ok=false;
                inputType.GetField("selected",inputFlags).SetValue(inputGame,LandUse.Water);
                for(int n=0;n<12;n++) yield return null;
                try {
                // Hidden windows can stop presenting the swapchain. Validate the
                // player renderer independently instead of accepting a black capture.
                var game=FindAnyObjectByType<HarborCityGame>();
                var camera=(Camera)typeof(HarborCityGame).GetField("cam",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game);
                var target=new RenderTexture(1440,900,24);target.Create();
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=target});
                RenderTexture.active=target;var image=new Texture2D(1440,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(output,"StandaloneCity.png"),image.EncodeToPNG());
                string inputJson=JsonUtility.ToJson(inputCity.ToSaveData(),true);
                var inputReload=JsonUtility.FromJson<CitySaveData>(inputJson).ToCity();
                if(inputReload.utilities.water.edges.Count!=inputCity.utilities.water.edges.Count || inputReload.roads.edges.Count!=inputCity.roads.edges.Count)throw new InvalidOperationException("World-input result did not survive reload");
                File.WriteAllText(Path.Combine(output,"WorldInputCity.json"),inputJson);
                RenderTexture.active=null;Destroy(image);target.Release();Destroy(target);
                } catch(Exception ex) {ok=false;File.WriteAllText(Path.Combine(output,"Failure.txt"),ex.ToString());}
                if(ok)
                {
                    inputType.GetField("selected",inputFlags).SetValue(inputGame,LandUse.Empty);
                    inputType.GetMethod("RefreshUtilityView",inputFlags).Invoke(inputGame,null);
                    var keys=InputSystem.AddDevice<Keyboard>("ValidationKeys");
                    InputSystem.QueueStateEvent(keys,new KeyboardState(Key.F2));yield return null;yield return null;
                    InputSystem.QueueStateEvent(keys,new KeyboardState());
                    for(int n=0;n<12;n++)yield return null;
                    try
                    {
                        var overlay=(GameObject)inputType.GetField("infoOverlay",inputFlags).GetValue(inputGame);
                        var mesh=(Mesh)inputType.GetField("infoMesh",inputFlags).GetValue(inputGame);
                        if(!(bool)inputType.GetField("showInfoViews",inputFlags).GetValue(inputGame) || overlay==null || !overlay.activeSelf || mesh.vertexCount!=inputCity.buildings.Count*4)
                            throw new InvalidOperationException("Native F2 did not show all actual building indicators");
                        var target=new RenderTexture(1440,900,24);target.Create();
                        UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(inputCam,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=target});
                        RenderTexture.active=target;var image=new Texture2D(1440,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();
                        File.WriteAllBytes(Path.Combine(output,"InfoView.png"),image.EncodeToPNG());RenderTexture.active=null;Destroy(image);target.Release();Destroy(target);
                        File.AppendAllText(Path.Combine(output,"Result.txt"),"; F2 building info world-input PASS");
                    }
                    catch(Exception ex){ok=false;File.WriteAllText(Path.Combine(output,"Failure.txt"),ex.ToString());}
                }
                if(ok)
                {
                    var medicalCity=JsonUtility.FromJson<CitySaveData>(File.ReadAllText(Path.Combine(output,"FireSceneCity.json"))).ToCity();
                    inputType.GetField("city",inputFlags).SetValue(inputGame,medicalCity);
                    inputType.GetField("traffic",inputFlags).SetValue(inputGame,new CityTraffic(medicalCity));
                    inputType.GetField("showInfoViews",inputFlags).SetValue(inputGame,false);
                    inputType.GetField("inspectedHome",inputFlags).SetValue(inputGame,medicalCity.buildings.OfType<FireHouseBuilding>().Single().id);
                    inputType.GetField("focus",inputFlags).SetValue(inputGame,new Vector3(25,0,1.5f));
                    inputType.GetField("zoom",inputFlags).SetValue(inputGame,30f);
                    inputType.GetMethod("Rebuild",inputFlags).Invoke(inputGame,null);inputType.GetMethod("UpdateCamera",inputFlags).Invoke(inputGame,null);
                    for(int n=0;n<12;n++)yield return null;
                    try
                    {
                        var target=new RenderTexture(1440,900,24);target.Create();
                        UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(inputCam,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=target});
                        RenderTexture.active=target;var image=new Texture2D(1440,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();
                        File.WriteAllBytes(Path.Combine(output,"Services.png"),image.EncodeToPNG());RenderTexture.active=null;Destroy(image);target.Release();Destroy(target);
                        var fireViews=(System.Collections.Generic.Dictionary<int,Transform>)inputType.GetField("flameViews",inputFlags).GetValue(inputGame);
                        var vehicles=(System.Collections.Generic.Dictionary<int,Transform>)inputType.GetField("trafficViews",inputFlags).GetValue(inputGame);
                        var engine=medicalCity.traffic.trips.Single(t=>t.purpose==TripPurpose.FireResponse && t.status==TripStatus.Visiting);
                        if(fireViews.Count!=medicalCity.ActiveFires || !vehicles.ContainsKey(engine.id) || !vehicles[engine.id].gameObject.activeSelf)throw new InvalidOperationException("Native fire or on-scene engine is missing from the scene");
                        File.AppendAllText(Path.Combine(output,"Result.txt"),"; school, clinic, fire and on-scene engine native render PASS");
                    }
                    catch(Exception ex){ok=false;File.WriteAllText(Path.Combine(output,"Failure.txt"),ex.ToString());}
                }
            }
            if(ok)
            {
                var inputGame=FindAnyObjectByType<HarborCityGame>();var inputType=typeof(HarborCityGame);
                const BindingFlags inputFlags=BindingFlags.Instance|BindingFlags.NonPublic;
                var inputCam=(Camera)inputType.GetField("cam",inputFlags).GetValue(inputGame);
                var policeCity=JsonUtility.FromJson<CitySaveData>(File.ReadAllText(Path.Combine(output,"PoliceSceneCity.json"))).ToCity();
                inputType.GetField("city",inputFlags).SetValue(inputGame,policeCity);inputType.GetField("traffic",inputFlags).SetValue(inputGame,new CityTraffic(policeCity));
                inputType.GetField("inspectedHome",inputFlags).SetValue(inputGame,policeCity.buildings.OfType<PoliceStationBuilding>().Single().id);
                var response=policeCity.traffic.trips.First(t=>t.purpose==TripPurpose.PoliceResponse && t.status==TripStatus.Visiting);
                var sceneTarget=policeCity.GetBuilding(response.destination);
                inputType.GetField("focus",inputFlags).SetValue(inputGame,new Vector3(sceneTarget.x,0,sceneTarget.z));
                inputType.GetField("zoom",inputFlags).SetValue(inputGame,40f);
                inputType.GetMethod("Rebuild",inputFlags).Invoke(inputGame,null);inputType.GetMethod("UpdateCamera",inputFlags).Invoke(inputGame,null);
                for(int n=0;n<12;n++)yield return null;
                try
                {
                    var vehicles=(System.Collections.Generic.Dictionary<int,Transform>)inputType.GetField("trafficViews",inputFlags).GetValue(inputGame);
                    if(!vehicles.ContainsKey(response.id) || !vehicles[response.id].gameObject.activeSelf)throw new InvalidOperationException("Native on-scene police car missing");
                    var homes=policeCity.buildings.OfType<ResidentialBuilding>().Where(b=>b.level>1).ToList();
                    var models=(System.Collections.Generic.Dictionary<int,GameObject>)inputType.GetField("visuals",inputFlags).GetValue(inputGame);
                    if(homes.Count==0 || homes.Any(b=>!models.ContainsKey(b.id) || !models[b.id].GetComponentsInChildren<Transform>().Any(t=>t.name=="Villa" && Mathf.Abs(t.localScale.y-(1.7f+.3f*(b.level-1)))<.001f)))
                        throw new InvalidOperationException("Native actually upgraded housing did not render its saved level");
                    var target=new RenderTexture(1440,900,24);target.Create();
                    UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(inputCam,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=target});
                    RenderTexture.active=target;var image=new Texture2D(1440,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();
                    File.WriteAllBytes(Path.Combine(output,"PoliceScene.png"),image.EncodeToPNG());RenderTexture.active=null;Destroy(image);target.Release();Destroy(target);
                    File.AppendAllText(Path.Combine(output,"Result.txt"),"; police on-scene and upgraded housing native render PASS");
                }
                catch(Exception ex){ok=false;File.WriteAllText(Path.Combine(output,"Failure.txt"),ex.ToString());}
            }
            if(ok)
            {
                var game=FindAnyObjectByType<HarborCityGame>();var type=typeof(HarborCityGame);const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
                var educationCity=JsonUtility.FromJson<CitySaveData>(File.ReadAllText(Path.Combine(output,"HigherEducationCity.json"))).ToCity();
                type.GetField("city",flags).SetValue(game,educationCity);type.GetField("traffic",flags).SetValue(game,new CityTraffic(educationCity));
                type.GetField("inspectedHome",flags).SetValue(game,educationCity.buildings.OfType<UniversityBuilding>().Single().id);
                type.GetField("focus",flags).SetValue(game,new Vector3(38,0,1.5f));type.GetField("zoom",flags).SetValue(game,30f);
                type.GetMethod("Rebuild",flags).Invoke(game,null);type.GetMethod("UpdateCamera",flags).Invoke(game,null);
                for(int n=0;n<12;n++)yield return null;
                try
                {
                    var models=(System.Collections.Generic.Dictionary<int,GameObject>)type.GetField("visuals",flags).GetValue(game);
                    foreach(var school in educationCity.buildings.OfType<SchoolBuilding>())
                        if(!models.ContainsKey(school.id) || !models[school.id].GetComponentsInChildren<Transform>().Any(t=>t.name=="School building" && Mathf.Abs(t.localScale.y-(school.Grade==3?3.2f:school.Grade==2?2.6f:2))<.001f))throw new InvalidOperationException("Native school stage model missing");
                    var cam=(Camera)type.GetField("cam",flags).GetValue(game);var target=new RenderTexture(1440,900,24);target.Create();
                    UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(cam,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=target});
                    RenderTexture.active=target;var image=new Texture2D(1440,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();
                    File.WriteAllBytes(Path.Combine(output,"EducationStages.png"),image.EncodeToPNG());RenderTexture.active=null;Destroy(image);target.Release();Destroy(target);
                    File.AppendAllText(Path.Combine(output,"Result.txt"),"; high-school/university native scene and inspector PASS");
                }
                catch(Exception ex){ok=false;File.WriteAllText(Path.Combine(output,"Failure.txt"),ex.ToString());}
            }
            if(ok)
            {
                var game=FindAnyObjectByType<HarborCityGame>();var type=typeof(HarborCityGame);const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
                var deathCity=JsonUtility.FromJson<CitySaveData>(File.ReadAllText(Path.Combine(output,"DeathcareSceneCity.json"))).ToCity();
                var cemetery=deathCity.buildings.OfType<CemeteryBuilding>().Single();var hearse=deathCity.traffic.trips.Single(t=>t.purpose==TripPurpose.Deathcare);
                type.GetField("city",flags).SetValue(game,deathCity);type.GetField("traffic",flags).SetValue(game,new CityTraffic(deathCity));
                type.GetField("inspectedHome",flags).SetValue(game,cemetery.id);type.GetField("focus",flags).SetValue(game,new Vector3(24,0,1.5f));type.GetField("zoom",flags).SetValue(game,40f);
                type.GetMethod("Rebuild",flags).Invoke(game,null);type.GetMethod("UpdateCamera",flags).Invoke(game,null);
                for(int n=0;n<12;n++)yield return null;
                try
                {
                    var models=(System.Collections.Generic.Dictionary<int,GameObject>)type.GetField("visuals",flags).GetValue(game);
                    var vehicles=(System.Collections.Generic.Dictionary<int,Transform>)type.GetField("trafficViews",flags).GetValue(game);
                    if(!models.ContainsKey(cemetery.id) || !models[cemetery.id].GetComponentsInChildren<Transform>().Any(t=>t.name=="Cemetery chapel") || !vehicles.ContainsKey(hearse.id) || !vehicles[hearse.id].gameObject.activeSelf)throw new InvalidOperationException("Native cemetery or specific loaded hearse missing from scene");
                    var cam=(Camera)type.GetField("cam",flags).GetValue(game);var target=new RenderTexture(1440,900,24);target.Create();
                    UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(cam,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=target});
                    RenderTexture.active=target;var image=new Texture2D(1440,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();
                    File.WriteAllBytes(Path.Combine(output,"DeathcareScene.png"),image.EncodeToPNG());RenderTexture.active=null;Destroy(image);target.Release();Destroy(target);
                    File.AppendAllText(Path.Combine(output,"Result.txt"),"; cemetery and specific loaded hearse native scene PASS");
                }
                catch(Exception ex){ok=false;File.WriteAllText(Path.Combine(output,"Failure.txt"),ex.ToString());}
            }
            if(ok)
            {
                var elevationGame=FindAnyObjectByType<HarborCityGame>();var elevationMouse=Mouse.current;
                yield return CheckElevationInput(elevationGame,elevationMouse);
                if(File.Exists(Path.Combine(output,"Failure.txt")))ok=false;
            }
            if(ok)
            {
                yield return CheckProblemPresentation(FindAnyObjectByType<HarborCityGame>());
                if(File.Exists(Path.Combine(output,"Failure.txt")))ok=false;
            }
            Application.Quit(ok?0:1);
        }

        IEnumerator CheckProblemPresentation(HarborCityGame game)
        {
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;var type=typeof(HarborCityGame);
            var c=JsonUtility.FromJson<CitySaveData>(File.ReadAllText(Path.Combine(output,"City.json"))).ToCity();
            var depot=c.buildings.OfType<LandfillBuilding>().First();
            depot.stored+=100;c.waste.generated+=100;long before=c.waste.processed;
            c.AdvanceWasteProcessing(12);
            try
            {
                var copy=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(c.ToSaveData())).ToCity();
                if(c.waste.processed-before!=25 || copy.WasteBalanceError!=0 || copy.waste.processed!=c.waste.processed
                    || Math.Abs(((LandfillBuilding)copy.GetBuilding(depot.id)).processingRemainder-depot.processingRemainder)>.0001f)
                    throw new InvalidOperationException("Native treatment stock/progress/processed JSON failed");
                File.WriteAllText(Path.Combine(output,"WasteProcessing.txt"),"PASS: physical inventory processed at actual game-day rate, native progress/cumulative disposal JSON and conserved waste");
            }
            catch(Exception ex){File.WriteAllText(Path.Combine(output,"Failure.txt"),ex.ToString());yield break;}
            var homes=c.buildings.OfType<ResidentialBuilding>().Where(b=>!b.abandoned && !b.burning && !b.burned).Take(2).ToList();
            c.utilities.water=new CityUtilityNetwork();c.Recalculate();
            foreach(var home in homes){home.outageDays=2;home.garbage+=32;c.waste.generated+=32;}
            type.GetField("city",flags).SetValue(game,c);type.GetField("traffic",flags).SetValue(game,new CityTraffic(c));
            type.GetField("speed",flags).SetValue(game,0f);type.GetField("selected",flags).SetValue(game,LandUse.Empty);
            type.GetField("inspectedHome",flags).SetValue(game,-1);type.GetField("showProblems",flags).SetValue(game,true);
            type.GetField("showInfoViews",flags).SetValue(game,false);type.GetField("showSimulation",flags).SetValue(game,false);
            type.GetField("showEconomy",flags).SetValue(game,false);type.GetField("nextProblemRefresh",flags).SetValue(game,0f);
            type.GetField("help",flags).SetValue(game,false);type.GetField("followResident",flags).SetValue(game,false);
            type.GetMethod("SetUndergroundView",flags).Invoke(game,new object[]{false});
            type.GetField("focus",flags).SetValue(game,new Vector3(homes[0].x,0,homes[0].z));type.GetField("zoom",flags).SetValue(game,25f);
            type.GetMethod("Rebuild",flags).Invoke(game,null);type.GetMethod("UpdateCamera",flags).Invoke(game,null);
            for(int i=0;i<15;i++)yield return null;
            try
            {
                var rects=(System.Collections.Generic.List<Rect>)type.GetField("problemHitRects",flags).GetValue(game);
                if(c.BuildingProblems().Count(p=>p.buildingId==homes[0].id && p.severity==2)<3)
                    throw new InvalidOperationException("Native multi-problem warning badges missing; rects="+rects.Count+"; snapshot="+((System.Collections.Generic.List<BuildingProblem>)type.GetField("problemSnapshot",flags).GetValue(game)).Count+"; underground="+type.GetField("undergroundView",flags).GetValue(game)+"; help="+type.GetField("help",flags).GetValue(game)+"; focus="+type.GetField("focus",flags).GetValue(game)+"; screen="+Screen.width+"x"+Screen.height+"; problems="+string.Join(",",c.BuildingProblems().Where(p=>p.buildingId==homes[0].id).Select(p=>p.kind+":"+p.severity)));
                // A hidden Windows player can simulate without issuing IMGUI repaint events.
                // Never call that a passed visual/input check; interactive Unity covers it separately.
                if(rects.Count>0)
                {
                    float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);var rect=rects[0];
                    if(!(bool)type.GetMethod("OverUI",flags).Invoke(game,new object[]{new Vector2(rect.center.x*scale,Screen.height-rect.center.y*scale)}))
                        throw new InvalidOperationException("Warning badge input leaks into world construction");
                }
                else File.WriteAllText(Path.Combine(output,"ProblemGUIUnverified.txt"),"Hidden player produced no IMGUI hit rectangles. Visuals, hover and button input require the separate interactive Unity check.");
                var text=(string)type.GetMethod("ProblemTooltip",flags).Invoke(game,new object[]{c.BuildingProblems().Where(p=>p.buildingId==homes[0].id)});
                if(!text.Contains("再 1 次") || !text.Contains("垃圾积压") || !text.Contains("排污"))throw new InvalidOperationException("Native warning tooltip countdown/detail missing");
            }
            catch(Exception ex){File.WriteAllText(Path.Combine(output,"Failure.txt"),ex.ToString());yield break;}
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"BuildingProblems.png"));
            yield return null;yield return null;
            try
            {
                type.GetMethod("LocateProblem",flags).Invoke(game,new object[]{BuildingProblemKind.Garbage});
                int first=(int)type.GetField("inspectedHome",flags).GetValue(game);
                type.GetMethod("LocateProblem",flags).Invoke(game,new object[]{BuildingProblemKind.Garbage});
                int second=(int)type.GetField("inspectedHome",flags).GetValue(game);
                var focus=(Vector3)type.GetField("focus",flags).GetValue(game);var building=c.GetBuilding(second);
                if(first==second || first<0 || building==null || Mathf.Abs(focus.x-building.x)>.01f || Mathf.Abs(focus.z-building.z)>.01f)
                    throw new InvalidOperationException("Native problem navigation does not cycle actual buildings");
                File.WriteAllText(Path.Combine(output,"BuildingProblems.txt"),"PASS: native multiple problem states, severity/countdown/tooltip text and actual camera/selection/marker navigation cycling. Hidden-window GUI, button click and real mouse hover are verified separately in interactive Unity, not inferred from these checks.");
            }
            catch(Exception ex){File.WriteAllText(Path.Combine(output,"Failure.txt"),ex.ToString());}
            for(int i=0;i<8;i++)yield return null;
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"ProblemNavigation.png"));
            yield return null;yield return null;
        }

        static void CheckElevatedTraffic(HarborCityGame game,string output)
        {
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;var type=typeof(HarborCityGame);
            var originalCity=type.GetField("city",flags).GetValue(game);var originalTraffic=type.GetField("traffic",flags).GetValue(game);
            try
            {
                foreach(float level in new[]{7f,-5f})
                {
                    Func<float,float,float> flat=(x,z)=>1;var c=CityModel.Create();c.money=200000;c.development.enabled=false;c.SetEntranceHeight(flat);
                    RoadNode P(float x,float y)=>new RoadNode{x=x,z=1.5f,y=y};
                    var points=new[]{c.roads.Node(CityRoads.Entrance),P(-30,1),P(-8,level),P(8,level),P(30,1),P(45,1)};
                    for(int i=1;i<points.Length;i++){var plan=c.roads.Plan(c,points[i-1],points[i],flat,engineered:points[i-1].y!=1 || points[i].y!=1);if(!c.CommitRoad(plan))throw new InvalidOperationException(plan.error);}
                    int Build(float x,LandUse use)=>c.PlaceBuilding(c.RoadsidePreview(x,7,use,HousingKind.Apartment,out _),use,flat,out _);
                    int factory=Build(-42,LandUse.Industrial),shop=Build(40,LandUse.Commercial);c.Inventory(factory).Stock=8;c.society.settings.applicantsPerDay=0;c.traffic.dispatchTimer=-10000;
                    var sim=new CityTraffic(c);var t=sim.Dispatch(factory,shop,TripPurpose.Delivery);if(t==null)throw new InvalidOperationException("Native bridge/tunnel trip missing");
                    int deck=t.route.FindIndex(id=>Math.Abs(c.roads.Node(id).y-level)<.01f);t.segment=deck;t.progress=.5f;
                    type.GetField("city",flags).SetValue(game,c);type.GetField("traffic",flags).SetValue(game,sim);
                    type.GetMethod("SetUndergroundView",flags).Invoke(game,new object[]{false});type.GetMethod("AnimateTraffic",flags).Invoke(game,null);
                    var views=(System.Collections.Generic.Dictionary<int,Transform>)type.GetField("trafficViews",flags).GetValue(game);
                    if(views[t.id].gameObject.activeSelf!=(level>1) || Math.Abs(((Vector3)type.GetMethod("TrafficPoint",flags).Invoke(game,new object[]{t,t.segment,t.progress})).y-(level+.32f))>.01f)throw new InvalidOperationException("Vehicle deck height or tunnel visibility failed");
                    type.GetMethod("SetUndergroundView",flags).Invoke(game,new object[]{true});type.GetMethod("AnimateTraffic",flags).Invoke(game,null);
                    if(!views[t.id].gameObject.activeSelf || Math.Abs(views[t.id].position.y-(level+.32f))>.01f)throw new InvalidOperationException("Underground vehicle render failed");
                    c.traffic.dispatchTimer=0;c=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(c.ToSaveData())).ToCity();c.traffic.dispatchTimer=-10000;sim=new CityTraffic(c);sim.Advance(40);c.traffic.dispatchTimer=0;
                    if(c.Goods(shop)!=8 || c.traffic.lostGoods!=0 || !c.Valid())throw new InvalidOperationException("Native bridge/tunnel freight save continuation failed");
                    File.WriteAllText(Path.Combine(output,level>1?"ElevatedCity.json":"TunnelCity.json"),JsonUtility.ToJson(c.ToSaveData(),true));
                }
                File.WriteAllText(Path.Combine(output,"ElevatedTraffic.txt"),"PASS: real bridge/tunnel delivery, actual vehicle height, normal/underground visibility, native JSON and cargo arrival/return");
            }
            finally{type.GetField("city",flags).SetValue(game,originalCity);type.GetField("traffic",flags).SetValue(game,originalTraffic);type.GetMethod("SetUndergroundView",flags).Invoke(game,new object[]{false});type.GetMethod("AnimateTraffic",flags).Invoke(game,null);}
        }
        IEnumerator CheckElevationInput(HarborCityGame game,Mouse mouse)
        {
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;var type=typeof(HarborCityGame);var originalCity=type.GetField("city",flags).GetValue(game);var originalTraffic=type.GetField("traffic",flags).GetValue(game);
            var land=(CityLandscape)type.GetField("landscape",flags).GetValue(game);var c=CityModel.Create();c.money=200000;c.development.enabled=false;c.SetEntranceHeight(land.Height);
            var cross=c.roads.Plan(c,new RoadNode{x=-20,z=-8,y=land.Height(-20,-8)},new RoadNode{x=35,z=-8,y=land.Height(35,-8)},land.Height);if(!c.CommitRoad(cross)){File.WriteAllText(Path.Combine(output,"Failure.txt"),cross.error);yield break;}
            var keys=InputSystem.AddDevice<Keyboard>("ElevationKeys");
            try
            {
                type.GetField("city",flags).SetValue(game,c);type.GetField("traffic",flags).SetValue(game,new CityTraffic(c));type.GetField("selected",flags).SetValue(game,LandUse.Road);type.GetField("roadMode",flags).SetValue(game,0);type.GetField("roadOperation",flags).SetValue(game,0);type.GetField("roadKind",flags).SetValue(game,RoadKind.Standard);type.GetField("roadOneWay",flags).SetValue(game,false);
                type.GetField("focus",flags).SetValue(game,new Vector3(8,2,2));type.GetField("zoom",flags).SetValue(game,65f);type.GetMethod("UpdateCamera",flags).Invoke(game,null);
                foreach(int sign in new[]{1,-1})
                {
                    type.GetMethod("ResetRoad",flags).Invoke(game,null);type.GetMethod("SetRoadElevation",flags).Invoke(game,new object[]{0f});float x=sign>0?6:22;
                    yield return ClickWorld(game,mouse,new Vector2(x,-20));
                    for(int i=0;i<2;i++){InputSystem.QueueStateEvent(keys,new KeyboardState(sign>0?Key.PageUp:Key.PageDown));yield return null;yield return null;InputSystem.QueueStateEvent(keys,new KeyboardState());yield return null;yield return null;}
                    if((float)type.GetField("roadElevation",flags).GetValue(game)!=sign*6){File.WriteAllText(Path.Combine(output,"Failure.txt"),"Elevation keyboard failed");yield break;}
                    yield return ClickWorld(game,mouse,new Vector2(x,2));
                    yield return ClickWorld(game,mouse,new Vector2(x,14));
                    InputSystem.QueueStateEvent(keys,new KeyboardState(Key.Home));yield return null;yield return null;InputSystem.QueueStateEvent(keys,new KeyboardState());yield return null;yield return null;
                    var exit=sign>0?new Vector2(x,36):new Vector2(-10,14);
                    yield return ClickWorld(game,mouse,exit);
                    if(!c.roads.edges.Any(e=>e.structure==(sign>0?RoadStructure.Elevated:RoadStructure.Tunnel)) || !c.roads.nodes.Any(n=>CityRoads.Length(n,new RoadNode{x=exit.x,z=exit.y})<.05f && Math.Abs(n.y-land.Height(exit.x,exit.y))<.05f))
                    {File.WriteAllText(Path.Combine(output,"Failure.txt"),"Elevation world-input construction or exit ramp failed: "+type.GetField("notice",flags).GetValue(game));yield break;}
                }
                if(c.roads.nodes.Any(n=>Math.Abs(n.z+8)<.1f && c.roads.Neighbors(n.id).Count>2)){File.WriteAllText(Path.Combine(output,"Failure.txt"),"Elevation world-input made false at-grade connection");yield break;}
                File.WriteAllText(Path.Combine(output,"ElevationInputCity.json"),JsonUtility.ToJson(c.ToSaveData(),true));File.WriteAllText(Path.Combine(output,"ElevationInput.txt"),"PASS: actual mouse/keyboard ground start, PgUp/PgDn, raised/buried straight decks, Home and ramps; no false cross connection");
                type.GetField("selected",flags).SetValue(game,LandUse.Bulldoze);
                type.GetMethod("SetRoadElevation",flags).Invoke(game,new object[]{0f});
                foreach(var structure in new[]{RoadStructure.Tunnel,RoadStructure.Elevated,RoadStructure.Ground})
                {
                    type.GetMethod("SetUndergroundView",flags).Invoke(game,new object[]{structure==RoadStructure.Tunnel});
                    var edge=c.roads.edges.First(e=>e.structure==structure && Math.Abs(c.roads.Node(e.a).y-c.roads.Node(e.b).y)<1);
                    var a=c.roads.Node(edge.a);var b=c.roads.Node(edge.b);var point=new Vector3((a.x+b.x)/2,(a.y+b.y)/2+.09f,(a.z+b.z)/2);
                    var camera=(Camera)type.GetField("cam",flags).GetValue(game);
                    var screen=camera.WorldToScreenPoint(point);var position=new Vector2(screen.x,screen.y);
                    if((bool)type.GetMethod("OverUI",flags).Invoke(game,new object[]{position}))
                    {File.WriteAllText(Path.Combine(output,"Failure.txt"),"Demolition input fixture is behind a panel: "+structure);yield break;}
                    int other=c.roads.edges.Count(e=>e.structure!=structure);
                    InputSystem.QueueStateEvent(mouse,new MouseState{position=position});yield return null;yield return null;
                    InputSystem.QueueStateEvent(mouse,new MouseState{position=position}.WithButton(MouseButton.Left));yield return null;yield return null;
                    InputSystem.QueueStateEvent(mouse,new MouseState{position=position});yield return null;yield return null;
                    if(c.roads.edges.Any(e=>e.id==edge.id) || c.roads.edges.Count(e=>e.structure!=structure)!=other)
                    {File.WriteAllText(Path.Combine(output,"Failure.txt"),"Actual visible-road demolition failed or removed another level: "+structure+"; "+type.GetField("notice",flags).GetValue(game));yield break;}
                }
                if(!JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(c.ToSaveData())).ToCity().Valid())
                {File.WriteAllText(Path.Combine(output,"Failure.txt"),"Native demolition save invalid");yield break;}
                File.WriteAllText(Path.Combine(output,"DemolitionInput.txt"),"PASS: actual oblique-camera mouse clicks remove tunnel, elevated and surface roads with construction elevation at zero; other levels preserved and native JSON remains valid");
            }
            finally{InputSystem.RemoveDevice(keys);type.GetField("city",flags).SetValue(game,originalCity);type.GetField("traffic",flags).SetValue(game,originalTraffic);type.GetMethod("ResetRoad",flags).Invoke(game,null);type.GetMethod("SetRoadElevation",flags).Invoke(game,new object[]{0f});}
        }

        static void CheckLanes(HarborCityGame game,string output)
        {
            Func<float,float,float> flat=(x,z)=>1;var c=CityModel.Create();c.development.enabled=false;c.SetEntranceHeight(flat);
            var a=new RoadNode{x=-45,z=1.5f,y=1};var b=new RoadNode{x=30,z=1.5f,y=1};var d=new RoadNode{x=30,z=25,y=1};var e=new RoadNode{x=-45,z=25,y=1};
            var points=new[]{c.roads.Node(CityRoads.Entrance),a,b,d,e,a};
            for(int n=1;n<points.Length;n++)if(!c.CommitRoad(c.roads.Plan(c,points[n-1],points[n],flat,n>1)))throw new InvalidOperationException("Native lane loop construction failed");
            int origin=c.PlaceBuilding(c.RoadsidePreview(-35,7,LandUse.Industrial,HousingKind.Apartment,out _),LandUse.Industrial,flat,out _);c.Inventory(origin).Stock=16;
            var sim=new CityTraffic(c);var first=sim.Dispatch(origin,CityTraffic.Outside,TripPurpose.Export);var second=sim.Dispatch(origin,CityTraffic.Outside,TripPurpose.Export);
            if(first==null || second==null || first.lane==second.lane)throw new InvalidOperationException("Native two-lane departure failed");
            sim.Advance(.25f);c=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(c.ToSaveData())).ToCity();sim=new CityTraffic(c);
            first=c.traffic.trips.First(t=>t.id==first.id);second=c.traffic.trips.First(t=>t.id==second.id);
            if(first.lane==second.lane || !c.Valid())throw new InvalidOperationException("Native lane save failed");
            const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;var type=typeof(HarborCityGame);
            var originalCity=type.GetField("city",flags).GetValue(game);var originalTraffic=type.GetField("traffic",flags).GetValue(game);
            try
            {
                type.GetField("city",flags).SetValue(game,c);type.GetField("traffic",flags).SetValue(game,sim);type.GetMethod("AnimateTraffic",flags).Invoke(game,null);
                var views=(System.Collections.Generic.Dictionary<int,Transform>)type.GetField("trafficViews",flags).GetValue(game);
                Vector3 p=views[first.id].position,q=views[second.id].position;p.y=q.y=0;
                if(!views[first.id].gameObject.activeSelf || !views[second.id].gameObject.activeSelf || Vector3.Distance(p,q)<1.05f)throw new InvalidOperationException("Native separate lane vehicle transforms failed");
            }
            finally
            {
                type.GetField("city",flags).SetValue(game,originalCity);type.GetField("traffic",flags).SetValue(game,originalTraffic);type.GetMethod("AnimateTraffic",flags).Invoke(game,null);
            }
            int firstId=first.id,secondId=second.id;sim.Advance(100);
            if(!c.Valid() || c.traffic.failed>0 || c.traffic.lostGoods>0 || c.traffic.completed<2 || c.traffic.trips.Any(t=>t.id==firstId || t.id==secondId))throw new InvalidOperationException("Native two-lane freight merge/return failed");
            File.WriteAllText(Path.Combine(output,"Lanes.txt"),"PASS: two simultaneous real exports, separate lane vehicle transforms, native lane JSON, one-lane merge and both return trips without lost tasks/cargo");
        }
        static void CheckLaneChoice(HarborCityGame game,string output)
        {
            Func<float,float,float> flat=(x,z)=>1;var c=CityModel.Create();c.development.enabled=false;c.SetEntranceHeight(flat);
            if(!c.CommitRoad(c.roads.Plan(c,c.roads.Node(CityRoads.Entrance),new RoadNode{x=45,z=1.5f,y=1},flat,false,RoadKind.FourLane)))throw new InvalidOperationException("Native lane change road failed");
            int origin=c.PlaceBuilding(c.RoadsidePreview(35,7,LandUse.Industrial,HousingKind.Apartment,out _),LandUse.Industrial,flat,out _);c.Inventory(origin).Stock=16;
            var sim=new CityTraffic(c);var t=sim.Dispatch(origin,CityTraffic.Outside,TripPurpose.Export);var lead=sim.Dispatch(origin,CityTraffic.Outside,TripPurpose.Export);
            foreach(var item in new[]{t,lead})
            {
                float x=item==t?10:8;
                item.segment=item.route.FindIndex(n=>c.roads.Node(n).x>=x && item.route.IndexOf(n)+1<item.route.Count && c.roads.Node(item.route[item.route.IndexOf(n)+1]).x<x);
                var a=c.roads.Node(item.Current);var b=c.roads.Node(item.Next);item.progress=(a.x-x)/(a.x-b.x);item.lane=item.previousLane=0;
            }
            sim.Advance(.1f);
            if(t.lane!=1 || t.laneChangeRemaining<=0 || !c.Valid())throw new InvalidOperationException("Native real export did not safely change lane");
            int id=t.id;var restored=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(c.ToSaveData())).ToCity();var resumed=new CityTraffic(restored);
            if(restored.traffic.trips.First(p=>p.id==id).laneChangeRemaining!=t.laneChangeRemaining)throw new InvalidOperationException("Native lateral progress save failed");
            const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;var type=typeof(HarborCityGame);
            var originalCity=type.GetField("city",flags).GetValue(game);var originalTraffic=type.GetField("traffic",flags).GetValue(game);
            try
            {
                type.GetField("city",flags).SetValue(game,c);type.GetField("traffic",flags).SetValue(game,sim);
                float previous=1.5f+.55f;
                for(int n=0;n<8;n++)
                {
                    type.GetMethod("AnimateTraffic",flags).Invoke(game,null);
                    var views=(System.Collections.Generic.Dictionary<int,Transform>)type.GetField("trafficViews",flags).GetValue(game);
                    float z=views[id].position.z;
                    if(!views[id].gameObject.activeSelf || z<previous-.001f || z-previous>.5f || z>1.5f+1.65f+.001f)throw new InvalidOperationException("Native actual vehicle lane animation jumped or reversed");
                    previous=z;sim.Advance(.05f);resumed.Advance(.05f);
                }
                var stopped=new TrafficTrip{route=new System.Collections.Generic.List<int>{CityRoads.Entrance}};
                var stopPoint=(Vector3)type.GetMethod("TrafficPoint",flags).Invoke(game,new object[]{stopped,0,0f});
                if(float.IsNaN(stopPoint.x+stopPoint.z))throw new InvalidOperationException("Native single-node stopped vehicle rendering failed");
            }
            finally
            {type.GetField("city",flags).SetValue(game,originalCity);type.GetField("traffic",flags).SetValue(game,originalTraffic);type.GetMethod("AnimateTraffic",flags).Invoke(game,null);}
            sim.Advance(15);resumed.Advance(15);
            if(!c.Valid() || c.traffic.failed!=0 || c.traffic.lostGoods!=0 || c.traffic.completed<2 || c.traffic.trips.Any(p=>p.id==id)
                || JsonUtility.ToJson(c.ToSaveData())!=JsonUtility.ToJson(restored.ToSaveData()))throw new InvalidOperationException("Native lane change freight return/save continuation failed");
            File.WriteAllText(Path.Combine(output,"LaneChoice.txt"),"PASS: real blocked export safely changes lane; actual vehicle Transform follows continuous lateral movement; native in-progress lane JSON resumes identically; both exports and physical returns complete without lost cargo. No GUI or original-game equivalence claim.");
        }
        static void CheckVehicleTurns(HarborCityGame game,string output)
        {
            Func<float,float,float> flat=(x,z)=>1;var c=CityModel.Create();c.development.enabled=false;c.SetEntranceHeight(flat);
            if(!c.CommitRoad(c.roads.Plan(c,c.roads.Node(CityRoads.Entrance),new RoadNode{x=45,z=1.5f,y=1},flat,false,RoadKind.FourLane))
                || !c.CommitRoad(c.roads.Plan(c,new RoadNode{x=0,z=-30,y=1},new RoadNode{x=0,z=30,y=1},flat,false,RoadKind.FourLane)))throw new InvalidOperationException("Native turn fixture construction failed");
            int end=c.roads.nodes.OrderBy(n=>CityRoads.Length(n,new RoadNode{x=0,z=-30})).First().id;
            var trip=new TrafficTrip{route=c.roads.FindPath(new System.Collections.Generic.List<int>{CityRoads.Entrance},new System.Collections.Generic.List<int>{end}),lane=1,previousLane=1};
            const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;var type=typeof(HarborCityGame);
            var original=type.GetField("city",flags).GetValue(game);Vector3? previous=null;
            try
            {
                type.GetField("city",flags).SetValue(game,c);
                for(int s=0;s<trip.route.Count-1;s++)
                {
                    trip.segment=s;
                    for(int k=0;k<=40;k++)
                    {
                        var position=(Vector3)type.GetMethod("TrafficPoint",flags).Invoke(game,new object[]{trip,s,k/40f});
                        var expected=CityVehicleGeometry.Position(c.roads,trip,s,k/40f);
                        if(Mathf.Abs(position.x-expected.x)+Mathf.Abs(position.z-expected.z)>.001f || float.IsNaN(position.y))throw new InvalidOperationException("Native renderer differs from shared vehicle turn geometry");
                        if(previous.HasValue && (position.x<previous.Value.x-.001f || position.z>previous.Value.z+.001f))throw new InvalidOperationException("Native right turn folds inward");
                        previous=position;
                    }
                }
                var parked=(Vector3)type.GetMethod("TrafficPoint",flags).Invoke(game,new object[]{trip,trip.route.Count-1,0f});
                var destination=c.roads.Node(end);
                if(Vector2.Distance(new Vector2(parked.x,parked.z),new Vector2(destination.x,destination.z))>2)throw new InvalidOperationException("Native arrived vehicle moved back to preceding segment");
            }
            finally{type.GetField("city",flags).SetValue(game,original);}
            File.WriteAllText(Path.Combine(output,"VehicleTurns.txt"),"PASS: native TrafficPoint used by vehicles and selected routes follows the shared four-lane right-turn geometry without folding inward, including graph-node transitions. This is geometry verification, not a GUI or original-game equivalence claim.");
        }
        IEnumerator CheckExpandedMapInput(HarborCityGame game,Mouse mouse,string output)
        {
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;var type=typeof(HarborCityGame);
            var savedCity=type.GetField("city",flags).GetValue(game);var savedTraffic=type.GetField("traffic",flags).GetValue(game);
            var focus=type.GetField("focus",flags).GetValue(game);var zoom=type.GetField("zoom",flags).GetValue(game);
            var yaw=type.GetField("yaw",flags).GetValue(game);var pitch=type.GetField("pitch",flags).GetValue(game);
            var land=(CityLandscape)type.GetField("landscape",flags).GetValue(game);var cam=(Camera)type.GetField("cam",flags).GetValue(game);
            var c=CityModel.Create();c.money=1000000;c.development.enabled=false;c.SetEntranceHeight(land.Height);
            try
            {
                type.GetField("focus",flags).SetValue(game,Vector3.zero);type.GetField("zoom",flags).SetValue(game,230f);type.GetField("pitch",flags).SetValue(game,20f);type.GetField("yaw",flags).SetValue(game,0f);
                type.GetMethod("UpdateCamera",flags).Invoke(game,null);
                if(game.transform.Find("Sea").localScale.x<CityMapState.TerrainHalfSize*8){File.WriteAllText(Path.Combine(output,"Failure.txt"),"Sea backdrop too small for expanded camera");yield break;}
                foreach(float x in new[]{-168f,168f})foreach(float z in new[]{-168f,168f})
                {
                    var v=cam.WorldToViewportPoint(new Vector3(x,land.Height(x,z),z));
                    if(v.x<0 || v.x>1 || v.y<0 || v.y>1 || v.z<cam.nearClipPlane || v.z>cam.farClipPlane){File.WriteAllText(Path.Combine(output,"Failure.txt"),"Expanded map clipped at widest zoom/lowest pitch");yield break;}
                }
                if(!c.CommitRoad(c.roads.Plan(c,c.roads.Node(CityRoads.Entrance),new RoadNode{x=115,z=1.5f,y=land.Height(115,1.5f)},land.Height))){File.WriteAllText(Path.Combine(output,"Failure.txt"),"Expanded input approach road");yield break;}
                type.GetField("city",flags).SetValue(game,c);type.GetField("traffic",flags).SetValue(game,new CityTraffic(c));
                type.GetField("focus",flags).SetValue(game,new Vector3(135,0,0));type.GetField("zoom",flags).SetValue(game,40f);type.GetField("pitch",flags).SetValue(game,60f);type.GetMethod("UpdateCamera",flags).Invoke(game,null);
                type.GetField("selected",flags).SetValue(game,LandUse.Road);type.GetField("roadKind",flags).SetValue(game,RoadKind.Standard);type.GetField("roadOneWay",flags).SetValue(game,false);
                type.GetField("roadOperation",flags).SetValue(game,0);type.GetField("roadMode",flags).SetValue(game,0);type.GetMethod("ResetRoad",flags).Invoke(game,null);
                yield return ClickWorld(game,mouse,new Vector2(115,1.5f));yield return ClickWorld(game,mouse,new Vector2(156,1.5f));
                if(!c.roads.nodes.Any(n=>n.x>150) || !c.Valid()){File.WriteAllText(Path.Combine(output,"Failure.txt"),"Actual expanded land road input failed: "+type.GetField("notice",flags).GetValue(game));yield break;}
                type.GetField("selected",flags).SetValue(game,LandUse.Water);type.GetField("waterTool",flags).SetValue(game,1);type.GetField("utilityStart",flags).SetValue(game,null);
                yield return ClickWorld(game,mouse,new Vector2(116,0));yield return ClickWorld(game,mouse,new Vector2(154,0));
                if(c.utilities.water.edges.Count!=1){File.WriteAllText(Path.Combine(output,"Failure.txt"),"Actual expanded pipe input failed");yield break;}
                var boundary=(LineRenderer)type.GetField("buildBoundary",flags).GetValue(game);
                if(boundary==null || boundary.positionCount!=768 || !boundary.loop || !boundary.enabled){File.WriteAllText(Path.Combine(output,"Failure.txt"),"Expanded construction boundary missing");yield break;}
                for(int i=0;i<boundary.positionCount;i++)
                {
                    var p=boundary.GetPosition(i);
                    if(Mathf.Abs(Mathf.Max(Mathf.Abs(p.x),Mathf.Abs(p.z))-168)>.001f || Mathf.Abs(p.y-(Mathf.Max(0,land.Height(p.x,p.z))+.16f))>.001f){File.WriteAllText(Path.Combine(output,"Failure.txt"),"Construction boundary not aligned with terrain/legal bounds");yield break;}
                }
                type.GetField("selected",flags).SetValue(game,LandUse.Empty);yield return null;yield return null;
                if(boundary.enabled){File.WriteAllText(Path.Combine(output,"Failure.txt"),"Boundary did not hide outside construction");yield break;}
                File.WriteAllText(Path.Combine(output,"MapExpansionInput.txt"),"PASS: full 336-square bounds visible at widest zoom/lowest pitch; real world-input road and pipe on x115-156 land; 768 boundary vertices aligned to terrain/bounds; hidden in selection mode");
            }
            finally
            {
                type.GetField("city",flags).SetValue(game,savedCity);type.GetField("traffic",flags).SetValue(game,savedTraffic);
                type.GetField("focus",flags).SetValue(game,focus);type.GetField("zoom",flags).SetValue(game,zoom);type.GetField("yaw",flags).SetValue(game,yaw);type.GetField("pitch",flags).SetValue(game,pitch);
                type.GetField("selected",flags).SetValue(game,LandUse.Road);type.GetField("utilityStart",flags).SetValue(game,null);type.GetMethod("ResetRoad",flags).Invoke(game,null);type.GetMethod("UpdateCamera",flags).Invoke(game,null);
            }
        }

        static void CheckMapExpansion(CityLandscape land,string output)
        {
            if(land.Terrain.terrainData.heightmapResolution!=1025 || land.Terrain.terrainData.size.x!=480)
                throw new InvalidOperationException("Expanded native terrain dimensions/resolution");
            // Compare against the original Terrain interpolation, not just its analytical height.
            var old=new TerrainData{heightmapResolution=513,size=new Vector3(240,64,240)};
            try
            {
                var heights=new float[513,513];
                for(int z=0;z<513;z++)for(int x=0;x<513;x++)heights[z,x]=Mathf.Clamp01((CityLandscape.LegacyElevation(x/512f*240-120,z/512f*240-120)+8)/64);
                old.SetHeights(0,0,heights);
                // Exercise the same stale Terrain replacement used by live script reloads.
                land.Terrain.terrainData=old;land.Terrain.GetComponent<TerrainCollider>().terrainData=old;
                land.Build();
                if(land.Terrain.terrainData.heightmapResolution!=1025 || land.Terrain.terrainData.size.x!=480)
                    throw new InvalidOperationException("Retained legacy Terrain did not expand");
                for(float z=-84;z<=84;z+=7)for(float x=-84;x<=84;x+=7)
                    if(Mathf.Abs(land.Height(x,z)-(old.GetInterpolatedHeight((x+120)/240,(z+120)/240)-8))>.002f)
                        throw new InvalidOperationException("Legacy build-area terrain moved at "+x+","+z);
            }
            finally{Destroy(old);}
            Physics.SyncTransforms();int rays=0;
            foreach(var p in new[]{new Vector2(150,0),new Vector2(-150,0),new Vector2(0,150),new Vector2(0,-150),new Vector2(160,50),new Vector2(-160,-50)})
            {
                if(land.Height(p.x,p.y)<=.15f || !land.Raycast(new Ray(new Vector3(p.x,200,p.y),Vector3.down),out var hit)
                    || Mathf.Abs(hit.y-land.Height(p.x,p.y))>.04f)throw new InvalidOperationException("Expanded land picking "+p);
                rays++;
            }
            var c=CityModel.Create();c.money=1000000;c.development.enabled=false;c.SetEntranceHeight(land.Height);
            var plan=c.roads.Plan(c,c.roads.Node(CityRoads.Entrance),new RoadNode{x=156,z=1.5f,y=land.Height(156,1.5f)},land.Height);
            if(!c.CommitRoad(plan))throw new InvalidOperationException("Real-terrain expansion route: "+plan.error);
            var preview=c.RoadsidePreview(145,7,LandUse.Commercial,HousingKind.Apartment,out string error);
            int shop=c.PlaceBuilding(preview,LandUse.Commercial,land.Height,out error);
            if(shop<0 || !c.BuildUtility(UtilityKind.Water,new RoadNode{x=0,z=0},new RoadNode{x=155,z=0},out error)
                || !c.BuildUtility(UtilityKind.Electricity,new RoadNode{x=0,z=0},new RoadNode{x=155,z=0},out error))throw new InvalidOperationException("Expanded building/utilities: "+error);
            c.PaintZones(c.ZoningCells(land.Height).Where(z=>z.pose.x>110),LandUse.Residential);
            var sim=new CityTraffic(c);if(sim.Dispatch(CityTraffic.Outside,shop,TripPurpose.Import)==null)throw new InvalidOperationException("Expanded dispatch");
            var saved=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(c.ToSaveData())).ToCity();var resumed=new CityTraffic(saved);
            sim.Advance(180);resumed.Advance(180);
            if(!c.Valid() || c.Goods(shop)<8 || c.traffic.completed==0 || c.traffic.failed!=0
                || JsonUtility.ToJson(c.ToSaveData())!=JsonUtility.ToJson(saved.ToSaveData()))throw new InvalidOperationException("Expanded native transport/save continuation");
            var legacy=c.ToSaveData();legacy.format=33;legacy.map=null;
            if(legacy.ToCity().GetBuilding(shop).x!=c.GetBuilding(shop).x)throw new InvalidOperationException("Legacy map migration moved entities");
            File.WriteAllText(Path.Combine(output,"MapExpansion.txt"),"PASS: 480 terrain/336 build area; 625 old terrain interpolated heights within 2 mm; "+rays+" expanded native collider picks; real terrain road, stable-ID shop, zoning, pipes/power; loaded import delivered/returned; native format34 deterministic continuation and format33 migration");
        }

        static CityModel HighwayMergeFixture(out int factory,out int shop)
        {
            Func<float,float,float> flat=(x,z)=>1;var c=CityModel.Create();c.money=1000000;c.development.enabled=false;c.SetEntranceHeight(flat);
            void Road(float ax,float az,float bx,float bz,RoadKind kind)
            {
                var p=c.roads.Plan(c,c.roads.Snap(ax,az,flat),c.roads.Snap(bx,bz,flat),flat,CityRoadTypes.LimitedAccess(kind),kind);
                if(!c.CommitRoad(p))throw new InvalidOperationException("Native merge fixture: "+p.error);
            }
            Road(-52.5f,1.5f,-45,1.5f,RoadKind.Standard);Road(-45,1.5f,40,1.5f,RoadKind.Highway);
            Road(40,1.5f,40,25,RoadKind.Ramp);Road(40,25,-45,25,RoadKind.Standard);Road(-45,25,-45,1.5f,RoadKind.Ramp);
            Road(-45,1.5f,-45,-25,RoadKind.Standard);Road(-45,-25,40,-25,RoadKind.Standard);Road(-10,-25,0,1.5f,RoadKind.Ramp);
            int Build(float x,float z,LandUse use)
            {var p=c.RoadsidePreview(x,z,use,HousingKind.Apartment,out string error);int id=c.PlaceBuilding(p,use,flat,out error);if(id<0)throw new InvalidOperationException(error);return id;}
            factory=Build(-12,-31,LandUse.Industrial);shop=Build(30,31,LandUse.Commercial);return c;
        }
        static void CheckHighwayMerging(string output)
        {
            for(int order=0;order<3;order++)
            {
                var c=HighwayMergeFixture(out int factory,out int shop);c.Inventory(factory).Stock=8;int node=c.roads.nodes.First(n=>Math.Abs(n.x)<.75f && Math.Abs(n.z-1.5f)<.01f).id;
                var sim=new CityTraffic(c);var main=sim.Dispatch(CityTraffic.Outside,shop,TripPurpose.Import);var ramp=sim.Dispatch(factory,shop,TripPurpose.Delivery);
                foreach(var t in new[]{main,ramp})
                {
                    if(t==null || !t.route.Contains(node))throw new InvalidOperationException("Native merge route");
                    t.segment=t.route.IndexOf(node)-1;t.progress=1-.34f/(c.roads.EdgeLength(t.Current,t.Next)/3);t.lane=t.previousLane=t.laneChangeFrom=t==main?2:0;
                    t.junctionWaitNode=node;t.junctionQueuedAt=t==main?9:1;
                }
                c.traffic.clock=10;if(order==1)c.traffic.trips.Reverse();
                if(order==2)c.roads.ToggleStop(node,main.route[main.route.IndexOf(node)-1],out _);
                sim=new CityTraffic(c);sim.Advance(.1f);
                if(order==2?ramp.segment<ramp.route.IndexOf(node) || main.Next!=node || main.blocked<=0
                    :main.segment<main.route.IndexOf(node) || ramp.Next!=node || ramp.blocked<=0)throw new InvalidOperationException("Native highway mainline/stop priority");
                var saved=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(c.ToSaveData())).ToCity();var resumed=new CityTraffic(saved);
                sim.Advance(100);resumed.Advance(100);
                if(c.Goods(shop)!=16 || c.traffic.trips.Count!=0 || c.traffic.failed!=0 || c.traffic.lostGoods!=0 || !c.Valid()
                    || JsonUtility.ToJson(c.ToSaveData())!=JsonUtility.ToJson(saved.ToSaveData()))throw new InvalidOperationException("Native merge freight/save continuation");
            }
            {
                var c=HighwayMergeFixture(out int factory,out int shop);Func<float,float,float> flat=(x,z)=>1;
                int Build(float x,float z,LandUse use)
                {var p=c.RoadsidePreview(x,z,use,HousingKind.Apartment,out string error);int id=c.PlaceBuilding(p,use,flat,out error);if(id<0)throw new InvalidOperationException(error);return id;}
                var factories=new[]{factory,Build(-30,-31,LandUse.Industrial),Build(10,-31,LandUse.Industrial)};foreach(int id in factories)c.Inventory(id).Stock=24;
                var shops=new[]{shop,Build(-35,31,LandUse.Commercial),Build(-20,31,LandUse.Commercial),Build(-5,31,LandUse.Commercial),Build(10,31,LandUse.Commercial)};
                var sim=new CityTraffic(c);int imports=0,deliveries=0;
                for(int i=0;i<6000;i++)
                {
                    if(imports<12 && sim.Dispatch(CityTraffic.Outside,shops[imports%5],TripPurpose.Import)!=null)imports++;
                    if(deliveries<8 && sim.Dispatch(factories[deliveries/3],shops[(12+deliveries)%5],TripPurpose.Delivery)!=null)deliveries++;
                    sim.Advance(.05f);
                }
                if(imports!=12 || deliveries!=8 || shops.Sum(c.Goods)!=160 || c.traffic.trips.Count!=0 || c.traffic.failed!=0 || c.traffic.lostGoods!=0 || !c.Valid())throw new InvalidOperationException("Native twenty-vehicle merge flow");
            }
            File.WriteAllText(Path.Combine(output,"HighwayMerging.txt"),"PASS: actual loaded mainline/ramp vehicles honor physical priority in either list order; native yielding JSON resumes identically; twenty naturally dispatched cargos deliver 160 goods and physically return without loss/deadlock. This does not claim original-game algorithm or heavy-city saturation parity.");
        }

        static void CheckHighways(HarborCityGame game,string output)
        {
            Func<float,float,float> flat=(x,z)=>1;var c=CityModel.Create();c.development.enabled=false;c.SetEntranceHeight(flat);
            var a=new RoadNode{x=-45,z=1.5f,y=1};var b=new RoadNode{x=40,z=1.5f,y=1};var d=new RoadNode{x=40,z=25,y=1};var e=new RoadNode{x=-45,z=25,y=1};
            var points=new[]{c.roads.Node(CityRoads.Entrance),a,b,d,e,a};var kinds=new[]{RoadKind.Standard,RoadKind.Highway,RoadKind.Ramp,RoadKind.Standard,RoadKind.Ramp};
            for(int i=0;i<kinds.Length;i++)if(!c.CommitRoad(c.roads.Plan(c,points[i],points[i+1],flat,CityRoadTypes.LimitedAccess(kinds[i]),kinds[i])))throw new InvalidOperationException("Native highway/ramp loop failed");
            var trips=new System.Collections.Generic.List<TrafficTrip>();var sim=new CityTraffic(c);var shops=new System.Collections.Generic.List<int>();
            foreach(float x in new[]{25f,5f,-20f})
            {
                int shop=c.PlaceBuilding(c.RoadsidePreview(x,31,LandUse.Commercial,HousingKind.Apartment,out _),LandUse.Commercial,flat,out _);shops.Add(shop);
                var trip=sim.Dispatch(CityTraffic.Outside,shop,TripPurpose.Import);if(trip==null)throw new InvalidOperationException("Native highway import dispatch failed");
                int segment=trip.route.FindIndex(n=>c.roads.Node(n).x<=-30 && trip.route.IndexOf(n)+1<trip.route.Count && c.roads.Node(trip.route[trip.route.IndexOf(n)+1]).x>-30);
                trip.segment=segment;var p=c.roads.Node(trip.Current);var q=c.roads.Node(trip.Next);trip.progress=(-30-p.x)/(q.x-p.x);trip.lane=trip.previousLane=trip.laneChangeFrom=trips.Count;trips.Add(trip);
            }
            sim=new CityTraffic(c);sim.Advance(.1f);var saved=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(c.ToSaveData())).ToCity();var resumed=new CityTraffic(saved);
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;var type=typeof(HarborCityGame);var originalCity=type.GetField("city",flags).GetValue(game);var originalTraffic=type.GetField("traffic",flags).GetValue(game);
            try
            {
                type.GetField("city",flags).SetValue(game,c);type.GetField("traffic",flags).SetValue(game,sim);type.GetMethod("AnimateTraffic",flags).Invoke(game,null);
                var views=(System.Collections.Generic.Dictionary<int,Transform>)type.GetField("trafficViews",flags).GetValue(game);
                for(int i=1;i<trips.Count;i++)if(Mathf.Abs(views[trips[i].id].position.z-views[trips[i-1].id].position.z)<1.09f)throw new InvalidOperationException("Native highway third-lane transforms overlap");
                File.WriteAllText(Path.Combine(output,"HighwayCity.json"),JsonUtility.ToJson(c.ToSaveData(),true));
            }
            finally{type.GetField("city",flags).SetValue(game,originalCity);type.GetField("traffic",flags).SetValue(game,originalTraffic);type.GetMethod("AnimateTraffic",flags).Invoke(game,null);}
            sim.Advance(80);resumed.Advance(80);
            if(!c.Valid() || c.traffic.failed!=0 || c.traffic.lostGoods!=0 || shops.Any(id=>c.Goods(id)<8) || trips.Any(t=>c.traffic.trips.Any(p=>p.id==t.id)) || JsonUtility.ToJson(c.ToSaveData())!=JsonUtility.ToJson(saved.ToSaveData()))throw new InvalidOperationException("Native highway imports/ramps/return/save failed");
            File.WriteAllText(Path.Combine(output,"Highways.txt"),"PASS: native three separate highway vehicle Transforms; real imports use off-ramp, deliver to ordinary-street shops, return via on-ramp; format 33 JSON resumes identically. No GUI or exact original-game claim.");
        }
        IEnumerator CheckHighwayInput(HarborCityGame game,Mouse mouse,string output)
        {
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;var type=typeof(HarborCityGame);var originalCity=type.GetField("city",flags).GetValue(game);var originalTraffic=type.GetField("traffic",flags).GetValue(game);
            var focus=type.GetField("focus",flags).GetValue(game);var zoom=type.GetField("zoom",flags).GetValue(game);var land=(CityLandscape)type.GetField("landscape",flags).GetValue(game);
            var c=CityModel.Create();c.money=200000;c.development.enabled=false;c.SetEntranceHeight(land.Height);
            try
            {
                type.GetField("city",flags).SetValue(game,c);type.GetField("traffic",flags).SetValue(game,new CityTraffic(c));type.GetField("selected",flags).SetValue(game,LandUse.Road);
                type.GetField("roadMode",flags).SetValue(game,0);type.GetField("roadOperation",flags).SetValue(game,0);type.GetField("roadKind",flags).SetValue(game,RoadKind.Highway);type.GetField("roadOneWay",flags).SetValue(game,true);
                type.GetField("focus",flags).SetValue(game,new Vector3(8,2,2));type.GetField("zoom",flags).SetValue(game,65f);type.GetMethod("UpdateCamera",flags).Invoke(game,null);type.GetMethod("ResetRoad",flags).Invoke(game,null);
                Vector2 start=new Vector2(0,-20),end=new Vector2(20,-20);var a=c.roads.Snap(start.x,start.y,land.Height);var b=c.roads.Snap(end.x,end.y,land.Height);
                var plan=c.roads.Plan(c,a,b,land.Height,true,RoadKind.Highway);int money=c.money;
                if(!plan.Valid){File.WriteAllText(Path.Combine(output,"Failure.txt"),"Highway world-input fixture rejected: "+plan.error);yield break;}
                yield return ClickWorld(game,mouse,start);yield return ClickWorld(game,mouse,end);
                if(c.roads.edges.Count==0 || c.roads.edges.Any(edge=>edge.kind!=RoadKind.Highway || !edge.oneWay) || c.money!=money-plan.cost){File.WriteAllText(Path.Combine(output,"Failure.txt"),"Highway construction world-input failed: "+type.GetField("notice",flags).GetValue(game));yield break;}
                type.GetMethod("ResetRoad",flags).Invoke(game,null);type.GetField("roadKind",flags).SetValue(game,RoadKind.Ramp);
                Vector2 exit=new Vector2(20,15);a=c.roads.Snap(end.x,end.y,land.Height);b=c.roads.Snap(exit.x,exit.y,land.Height);plan=c.roads.Plan(c,a,b,land.Height,true,RoadKind.Ramp);money=c.money;
                if(!plan.Valid){File.WriteAllText(Path.Combine(output,"Failure.txt"),"Ramp world-input fixture rejected: "+plan.error);yield break;}
                yield return ClickWorld(game,mouse,end);yield return ClickWorld(game,mouse,exit);
                if(!c.roads.edges.Any(edge=>edge.kind==RoadKind.Ramp) || c.money!=money-plan.cost || c.ZoningCells(land.Height).Count!=0 || !c.Valid()){File.WriteAllText(Path.Combine(output,"Failure.txt"),"Ramp construction/frontage world-input failed: "+type.GetField("notice",flags).GetValue(game));yield break;}
                type.GetMethod("ResetRoad",flags).Invoke(game,null);type.GetField("roadOperation",flags).SetValue(game,2);
                var edge=c.roads.edges.First(r=>r.kind==RoadKind.Highway);int from=edge.a,to=edge.b;
                yield return ClickWorld(game,mouse,(start+end)/2);
                if(edge.a!=to || edge.b!=from){File.WriteAllText(Path.Combine(output,"Failure.txt"),"Highway reverse world-input failed");yield break;}
                type.GetField("roadOperation",flags).SetValue(game,1);type.GetField("roadKind",flags).SetValue(game,RoadKind.Standard);type.GetField("roadOneWay",flags).SetValue(game,false);
                yield return ClickWorld(game,mouse,(start+end)/2);
                if(edge.kind!=RoadKind.Standard || edge.oneWay || !c.Valid()){File.WriteAllText(Path.Combine(output,"Failure.txt"),"Highway downgrade world-input failed");yield break;}
                File.WriteAllText(Path.Combine(output,"HighwayInput.txt"),"PASS: actual Unity mouse road-tool construction of highway and connected ramp on terrain, correct charges/no zoning, reverse and downgrade; UI toolbar clicks require separate interactive check.");
            }
            finally
            {
                type.GetField("city",flags).SetValue(game,originalCity);type.GetField("traffic",flags).SetValue(game,originalTraffic);type.GetField("focus",flags).SetValue(game,focus);type.GetField("zoom",flags).SetValue(game,zoom);
                type.GetMethod("UpdateCamera",flags).Invoke(game,null);type.GetMethod("ResetRoad",flags).Invoke(game,null);type.GetField("roadOperation",flags).SetValue(game,0);type.GetField("roadKind",flags).SetValue(game,RoadKind.Standard);type.GetField("roadOneWay",flags).SetValue(game,false);
            }
        }
        static void CheckWideRoads(HarborCityGame game,string output)
        {
            Func<float,float,float> flat=(x,z)=>1;var c=CityModel.Create();c.development.enabled=false;c.SetEntranceHeight(flat);
            if(!c.CommitRoad(c.roads.Plan(c,c.roads.Node(CityRoads.Entrance),new RoadNode{x=40,z=1.5f,y=1},flat)))throw new InvalidOperationException("Native wide road initial construction failed");
            int edge=c.roads.edges[0].id,money=c.money;var ids=c.roads.edges.Select(e=>e.id).ToArray();
            c.PaintZones(c.ZoningCells(flat).Where(z=>z.pose.x<0),LandUse.Residential);
            if(!c.PreviewRoadUpgrade(edge,RoadKind.FourLane,false,out int cost,out _,flat) || !c.UpgradeRoad(edge,RoadKind.FourLane,false,out _,flat) || c.money!=money-cost || !ids.SequenceEqual(c.roads.edges.Select(e=>e.id)) || !c.ZoningCells(flat).Any(z=>z.use==LandUse.Residential))throw new InvalidOperationException("Native paid widening/zoning failed");
            int origin=c.PlaceBuilding(c.RoadsidePreview(-35,7,LandUse.Industrial,HousingKind.Apartment,out _),LandUse.Industrial,flat,out _);if(origin<0)throw new InvalidOperationException("Native wide building access failed");c.Inventory(origin).Stock=16;
            var sim=new CityTraffic(c);var first=sim.Dispatch(origin,CityTraffic.Outside,TripPurpose.Export);var second=sim.Dispatch(origin,CityTraffic.Outside,TripPurpose.Export);
            if(first==null || second==null || first.lane==second.lane)throw new InvalidOperationException("Native wide freight departure failed");
            sim.Advance(.25f);c=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(c.ToSaveData())).ToCity();sim=new CityTraffic(c);
            first=c.traffic.trips.First(t=>t.id==first.id);second=c.traffic.trips.First(t=>t.id==second.id);
            const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;var type=typeof(HarborCityGame);
            var originalCity=type.GetField("city",flags).GetValue(game);var originalTraffic=type.GetField("traffic",flags).GetValue(game);
            try
            {
                type.GetField("city",flags).SetValue(game,c);type.GetField("traffic",flags).SetValue(game,sim);type.GetMethod("AnimateTraffic",flags).Invoke(game,null);
                var views=(System.Collections.Generic.Dictionary<int,Transform>)type.GetField("trafficViews",flags).GetValue(game);
                Vector3 a=views[first.id].position,b=views[second.id].position;a.y=b.y=0;
                if(Vector3.Distance(a,b)<1.05f)throw new InvalidOperationException("Native wide lane placement failed");
            }
            finally
            {
                type.GetField("city",flags).SetValue(game,originalCity);type.GetField("traffic",flags).SetValue(game,originalTraffic);type.GetMethod("AnimateTraffic",flags).Invoke(game,null);
            }
            int ai=first.id,bi=second.id;sim.Advance(100);
            if(!c.Valid() || c.traffic.failed!=0 || c.traffic.lostGoods!=0 || c.traffic.completed<2 || c.traffic.trips.Any(t=>t.id==ai || t.id==bi))throw new InvalidOperationException("Native wide freight/return failed");
            File.WriteAllText(Path.Combine(output,"WideRoads.txt"),"PASS: paid whole-street widening, stable IDs, retained roadside land use, actual parallel exports, separate vehicle transforms, native profile/lane JSON and complete returns without lost goods");
        }
        static CityModel ControlCity()
        {
            Func<float,float,float> flat=(x,z)=>1;var c=CityModel.Create();c.development.enabled=false;c.SetEntranceHeight(flat);
            foreach(var points in new[]{new[]{c.roads.Node(CityRoads.Entrance),new RoadNode{x=48,z=1.5f,y=1}},new[]{new RoadNode{x=0,z=-40,y=1},new RoadNode{x=0,z=40,y=1}}})
                if(!c.CommitRoad(c.roads.Plan(c,points[0],points[1],flat)))throw new InvalidOperationException("Native controlled crossroads failed");
            return c;
        }
        static void CheckTrafficControls(HarborCityGame game,string output)
        {
            Func<float,float,float> flat=(x,z)=>1;var c=ControlCity();int node=c.roads.nodes.First(p=>Math.Abs(p.x)<.01f && Math.Abs(p.z-1.5f)<.01f).id;
            int w=c.roads.Neighbors(node).First(p=>c.roads.Node(p).x<-.01f),n=c.roads.Neighbors(node).First(p=>c.roads.Node(p).z>1.51f);
            c.roads.SetJunctionControl(node,JunctionControl.Signals,out _);
            int west=c.PlaceBuilding(c.RoadsidePreview(-35,7,LandUse.Industrial,HousingKind.Apartment,out _),LandUse.Industrial,flat,out _),east=c.PlaceBuilding(c.RoadsidePreview(35,7,LandUse.Commercial,HousingKind.Apartment,out _),LandUse.Commercial,flat,out _);
            int north=c.PlaceBuilding(c.RoadsidePreview(5,30,LandUse.Industrial,HousingKind.Apartment,out _),LandUse.Industrial,flat,out _),south=c.PlaceBuilding(c.RoadsidePreview(5,-30,LandUse.Commercial,HousingKind.Apartment,out _),LandUse.Commercial,flat,out _);
            c.Inventory(west).Stock=c.Inventory(north).Stock=8;var sim=new CityTraffic(c);var a=sim.Dispatch(west,east,TripPurpose.Delivery);var b=sim.Dispatch(north,south,TripPurpose.Delivery);
            foreach(var t in new[]{a,b}){if(t==null)throw new InvalidOperationException("Native controlled freight failed");t.segment=t.route.IndexOf(node)-1;t.progress=1-.4f/(c.roads.EdgeLength(t.Current,t.Next)/3);}
            for(int i=0;i<200;i++){float time=i*.1f;if(c.roads.Signal(node,w,time)==SignalAspect.Green && c.roads.Signal(node,w,time+.7f)==SignalAspect.Green){c.traffic.clock=time;break;}}
            sim=new CityTraffic(c);sim.Advance(.15f);
            if(a.segment<a.route.IndexOf(node) || b.Next!=node || b.blocked<=0 || b.junctionWaitReason!="等待信号灯")throw new InvalidOperationException("Native actual red/green control failed");
            var saved=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(c.ToSaveData())).ToCity();var savedSim=new CityTraffic(saved);
            if(!saved.Valid() || saved.traffic.trips.First(t=>t.id==b.id).junctionWaitNode!=node)throw new InvalidOperationException("Native queued control JSON failed");
            const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;var type=typeof(HarborCityGame);var originalCity=type.GetField("city",flags).GetValue(game);var originalTraffic=type.GetField("traffic",flags).GetValue(game);
            try
            {
                type.GetField("city",flags).SetValue(game,c);type.GetField("traffic",flags).SetValue(game,sim);type.GetMethod("AnimateTraffic",flags).Invoke(game,null);
                var views=(System.Collections.Generic.Dictionary<(int node,int from),Renderer>)type.GetField("trafficControlViews",flags).GetValue(game);
                if(!views.ContainsKey((node,w)) || !views.ContainsKey((node,n)) || views[(node,w)].sharedMaterial.color.g<.8f || views[(node,n)].sharedMaterial.color.r<.8f)throw new InvalidOperationException("Native actual signal marker phases failed");
                c.roads.ToggleStop(node,w,out _);type.GetMethod("AnimateTraffic",flags).Invoke(game,null);
                if(views.Count!=1 || !views[(node,w)].name.StartsWith("Stop sign"))throw new InvalidOperationException("Native stop marker control change failed");
                c.roads.SetJunctionControl(node,JunctionControl.Signals,out _);
            }
            finally{type.GetField("city",flags).SetValue(game,originalCity);type.GetField("traffic",flags).SetValue(game,originalTraffic);type.GetMethod("AnimateTraffic",flags).Invoke(game,null);}
            sim.Advance(80);savedSim.Advance(80);
            if(!c.Valid() || !saved.Valid() || c.Goods(east)<8 || c.Goods(south)<8 || saved.Goods(east)<8 || saved.Goods(south)<8 || c.traffic.failed!=0 || saved.traffic.failed!=0 || c.traffic.trips.Any(t=>t.id==a.id || t.id==b.id))throw new InvalidOperationException("Native saved signals failed actual delivery/return");
            c=ControlCity();node=c.roads.nodes.First(p=>Math.Abs(p.x)<.01f && Math.Abs(p.z-1.5f)<.01f).id;w=c.roads.Neighbors(node).First(p=>c.roads.Node(p).x<-.01f);c.roads.ToggleStop(node,w,out _);
            west=c.PlaceBuilding(c.RoadsidePreview(-35,7,LandUse.Industrial,HousingKind.Apartment,out _),LandUse.Industrial,flat,out _);east=c.PlaceBuilding(c.RoadsidePreview(35,7,LandUse.Commercial,HousingKind.Apartment,out _),LandUse.Commercial,flat,out _);
            c.Inventory(west).Stock=8;sim=new CityTraffic(c);a=sim.Dispatch(west,east,TripPurpose.Delivery);a.segment=a.route.IndexOf(node)-1;a.progress=1-.4f/(c.roads.EdgeLength(a.Current,a.Next)/3);sim=new CityTraffic(c);sim.Advance(.2f);
            if(a.Next!=node || a.junctionStopTime<=0)throw new InvalidOperationException("Native actual stop duration failed");
            saved=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(c.ToSaveData())).ToCity();float stopTime=saved.traffic.trips.First(t=>t.id==a.id).junctionStopTime;savedSim=new CityTraffic(saved);
            if(saved.traffic.trips.First(t=>t.id==a.id).junctionStopTime!=stopTime)throw new InvalidOperationException("Native load advanced stop time");
            sim.Advance(80);savedSim.Advance(80);
            if(c.Goods(east)<8 || saved.Goods(east)<8 || c.traffic.failed!=0 || saved.traffic.failed!=0 || !c.Valid() || !saved.Valid() || c.traffic.trips.Any(t=>t.id==a.id) || saved.traffic.trips.Any(t=>t.id==a.id))throw new InvalidOperationException("Native saved stopping cargo delivery/return failed");
            File.WriteAllText(Path.Combine(output,"TrafficControls.txt"),"PASS: native actual cargo red waits/green crossing, retained queued arrival in JSON, red/green signal and stop marker renderers, saved phase continuation, actual stopping duration retained on load, deliveries and complete return trips without failure");
        }
        IEnumerator CheckTrafficControlInput(HarborCityGame game,Mouse mouse)
        {
            const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;var type=typeof(HarborCityGame);var originalCity=type.GetField("city",flags).GetValue(game);var originalTraffic=type.GetField("traffic",flags).GetValue(game);
            var c=ControlCity();int node=c.roads.nodes.First(p=>Math.Abs(p.x)<.01f && Math.Abs(p.z-1.5f)<.01f).id;int w=c.roads.Neighbors(node).First(p=>c.roads.Node(p).x<-.01f);
            try
            {
                type.GetField("city",flags).SetValue(game,c);type.GetField("traffic",flags).SetValue(game,new CityTraffic(c));type.GetField("selected",flags).SetValue(game,LandUse.Road);type.GetField("roadOperation",flags).SetValue(game,3);type.GetField("junctionTool",flags).SetValue(game,0);
                yield return ClickWorld(game,mouse,new Vector2(0,1.5f));
                if(!c.roads.HasSignals(node)){File.WriteAllText(Path.Combine(output,"Failure.txt"),"Signal on world-input failed");yield break;}
                yield return ClickWorld(game,mouse,new Vector2(0,1.5f));
                if(c.roads.HasSignals(node)){File.WriteAllText(Path.Combine(output,"Failure.txt"),"Signal off world-input failed");yield break;}
                type.GetField("junctionTool",flags).SetValue(game,1);yield return ClickWorld(game,mouse,new Vector2(-2,1.5f));
                if(!c.roads.Node(node).stopFrom.Contains(w)){File.WriteAllText(Path.Combine(output,"Failure.txt"),"Stop sign world-input failed");yield break;}
                yield return ClickWorld(game,mouse,new Vector2(-2,1.5f));
                type.GetField("junctionTool",flags).SetValue(game,2);yield return ClickWorld(game,mouse,new Vector2(0,1.5f));
                if(c.roads.Node(node).stopFrom.Count!=0 || c.roads.Node(node).control!=JunctionControl.Automatic){File.WriteAllText(Path.Combine(output,"Failure.txt"),"Stop removal/default world-input failed");yield break;}
                File.AppendAllText(Path.Combine(output,"Result.txt"),"; signal on/off, stop placement/removal and default world-input PASS (isolated controlled crossroads)");
            }
            finally{type.GetField("city",flags).SetValue(game,originalCity);type.GetField("traffic",flags).SetValue(game,originalTraffic);type.GetField("roadOperation",flags).SetValue(game,0);type.GetMethod("AnimateTraffic",flags).Invoke(game,null);}
        }
        static void CheckPedestrians(HarborCityGame game,string output,bool wide=false,float skew=0)
        {
            Func<float,float,float> flat=(x,z)=>1;var c=CityModel.Create();c.money=150000;c.development.enabled=false;c.SetEntranceHeight(flat);
            string prefix=skew>0?"PositiveSkew":skew<0?"NegativeSkew":"";
            foreach(var ends in new[]{new[]{c.roads.Node(CityRoads.Entrance),new RoadNode{x=48,z=1.5f,y=1}},new[]{new RoadNode{x=-41.5f*skew,z=-40,y=1},new RoadNode{x=38.5f*skew,z=40,y=1}},new[]{c.roads.Node(CityRoads.Entrance),new RoadNode{x=-68,z=1.5f,y=1}}})
                if(!c.CommitRoad(c.roads.Plan(c,ends[0],ends[1],flat,false,wide?RoadKind.FourLane:RoadKind.Standard)))throw new InvalidOperationException("Native pedestrian road fixture");
            int Build(float x,float z,LandUse use)
            {int id=c.PlaceBuilding(c.RoadsidePreview(x,z,use,HousingKind.Apartment,out _),use,flat,out string error);if(id<0)throw new InvalidOperationException(error);return id;}
            float side=wide?-8:-5;
            int home=Build(-30,wide?9:7,LandUse.Residential),school=Build(40,side,LandUse.ElementarySchool);
            Build(-46,side,LandUse.Power);Build(-38,side,LandUse.Water);Build(-64,side,LandUse.Sewage);
            foreach(var kind in new[]{UtilityKind.Water,UtilityKind.Electricity})
                if(!c.BuildUtility(kind,new RoadNode{x=-64,z=wide?-2.5f:0},new RoadNode{x=38,z=wide?-2.5f:0},out _))throw new InvalidOperationException("Native pedestrian utilities");
            if(wide)foreach(var facility in c.buildings.Where(b=>b.Use==LandUse.Water || b.Use==LandUse.Sewage))
                if(!c.BuildUtility(UtilityKind.Water,new RoadNode{x=facility.x,z=-2.5f},new RoadNode{x=facility.x,z=facility.z},out _))throw new InvalidOperationException("Native wide source pipe connection");
            var housing=(ResidentialBuilding)c.GetBuilding(home);
            var h=new Household{id=c.society.nextId++,home=home,unit=0,resident=true,rent=housing.askingRent,leaseEnd=31,nextReview=4,savings=1000,spacePreference=.5f,privacyPreference=.5f,timePreference=.5f,savingPreference=.5f,noiseSensitivity=.5f,pollutionSensitivity=.5f,trafficSensitivity=.5f};
            var child=new CityResident{id=c.society.nextCitizenId++,householdId=h.id,name="过街验证居民",age=10,location=home};h.people.Add(child);c.society.families.Add(h);
            c.development.enabled=true;c.waste.enabled=false;c.society.settings.applicantsPerDay=0;c.society.dayElapsed=40;c.Recalculate();
            if(!c.EnrollElementary(child.id,school))throw new InvalidOperationException("Native pedestrian enrollment");
            int node=c.roads.nodes.First(n=>Math.Abs(n.x)<.01f && Math.Abs(n.z-1.5f)<.01f).id;c.roads.SetJunctionControl(node,JunctionControl.Signals,out _);
            var sim=new CityTraffic(c);var walk=sim.DispatchSchool(child.id);
            void Approach(TrafficTrip t)
            {
                int index=t.route.IndexOf(node);if(index<1 || index==t.route.Count-1)throw new InvalidOperationException("Native pedestrian route");
                float distance=CityJunctions.CrossingRadius(c.roads,node)+.06f;t.segment=index-1;
                while(t.segment>0 && c.roads.EdgeLength(t.Current,t.Next)/3<distance){distance-=c.roads.EdgeLength(t.Current,t.Next)/3;t.segment--;}
                t.progress=1-distance/(c.roads.EdgeLength(t.Current,t.Next)/3);if(t.progress<0)throw new InvalidOperationException("Native crossing approach too short");
            }
            int From(TrafficTrip t)=>t.route[t.route.IndexOf(node)-1];
            Approach(walk);while(c.roads.PedestrianSignal(node,From(walk),c.traffic.clock)==SignalAspect.Green || c.roads.PedestrianSignal(node,From(walk),c.traffic.clock+.5f)==SignalAspect.Green)c.traffic.clock+=.1f;
            sim=new CityTraffic(c);sim.Advance(.3f);
            if(walk.segment>=walk.route.IndexOf(node) || walk.blocked<=0 || child.atSchool)throw new InvalidOperationException("Native pedestrian red wait");
            int id=walk.id;c=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(c.ToSaveData())).ToCity();sim=new CityTraffic(c);walk=c.traffic.trips.First(t=>t.id==id);child=c.Citizens.First(p=>p.id==child.id);
            while(c.roads.PedestrianSignal(node,From(walk),c.traffic.clock)!=SignalAspect.Green || c.roads.PedestrianSignal(node,From(walk),c.traffic.clock+.7f)!=SignalAspect.Green)c.traffic.clock+=.1f;
            int factory=Build(-20,side,LandUse.Industrial),shop=Build(20,side,LandUse.Commercial);c.Inventory(factory).Stock=8;var truck=sim.Dispatch(factory,shop,TripPurpose.Delivery);Approach(truck);sim.Advance(.6f);
            if(walk.blocked>0 || truck.segment<truck.route.IndexOf(node) || truck.blocked>0)throw new InvalidOperationException("Native parallel freight pedestrian concurrency");
            c.money+=10000;int crossingFactory=Build(skew==0?(wide?8:5):-21.5f*skew+4,-20,LandUse.Industrial),crossingShop=Build(skew==0?(wide?8:5):18.5f*skew+4,20,LandUse.Commercial);
            c.Inventory(crossingFactory).Stock=8;var crossing=sim.Dispatch(crossingFactory,crossingShop,TripPurpose.Delivery);Approach(crossing);
            c.roads.SetJunctionControl(node,JunctionControl.Uncontrolled,out _);sim.Advance(.1f);
            if(crossing.segment>=crossing.route.IndexOf(node) || crossing.blocked<=0 || !crossing.junctionWaitReason.Contains("行人"))throw new InvalidOperationException("Native conflicting freight pedestrian yield");
            const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;var type=typeof(HarborCityGame);var originalCity=type.GetField("city",flags).GetValue(game);var originalTraffic=type.GetField("traffic",flags).GetValue(game);
            try
            {
                type.GetField("city",flags).SetValue(game,c);type.GetField("traffic",flags).SetValue(game,sim);type.GetMethod("RefreshRoadView",flags).Invoke(game,null);type.GetMethod("AnimateTraffic",flags).Invoke(game,null);
                var views=(System.Collections.Generic.Dictionary<int,Transform>)type.GetField("trafficViews",flags).GetValue(game);
                if(!views[id].gameObject.activeSelf || !views[id].name.StartsWith("Pedestrian ") || views[id].localScale.x>=.3f || !views[truck.id].gameObject.activeSelf)throw new InvalidOperationException("Native real pedestrian/truck render");
                var walkingPoint=CityJunctions.WalkingPosition(c.roads,walk,walk.segment,walk.progress);
                if(Math.Abs(views[id].position.x-walkingPoint.x)>.001f || Math.Abs(views[id].position.z-walkingPoint.z)>.001f)throw new InvalidOperationException("Native walker render disagrees with simulated sidewalk path");
                var roadView=(CityRoadView)type.GetField("roadView",flags).GetValue(game);
                var painted=roadView.GetComponentsInChildren<MeshFilter>().First(f=>f.name=="Road markings").sharedMesh.vertices;
                foreach(int arm in c.roads.Neighbors(node))
                {
                    var crosswalk=CityJunctions.Crosswalk(c.roads,node,arm);
                    if(crosswalk==null || painted.Count(v=>CityRoads.Distance(new RoadNode{x=v.x,z=v.z},crosswalk[0],crosswalk[1])<.31f)<16)throw new InvalidOperationException("Native crosswalk paint missing at physical crossing");
                }
                File.WriteAllText(Path.Combine(output,prefix+(wide?"WidePedestrianCrossingCity.json":"PedestrianCrossingCity.json")),JsonUtility.ToJson(c.ToSaveData(),true));
            }
            finally{type.GetField("city",flags).SetValue(game,originalCity);type.GetField("traffic",flags).SetValue(game,originalTraffic);type.GetMethod("RefreshRoadView",flags).Invoke(game,null);type.GetMethod("AnimateTraffic",flags).Invoke(game,null);}
            // Check attendance within the actual school day. Fast-forwarding 90
            // seconds would correctly include dismissal and the child's return.
            c=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(c.ToSaveData())).ToCity();sim=new CityTraffic(c);child=c.Citizens.First(p=>p.id==child.id);int truckId=truck.id;sim.Advance(30);
            if(!child.atSchool || c.Goods(shop)<8 || c.Goods(crossingShop)<8 || c.traffic.trips.Any(t=>t.id==truckId || t.id==crossing.id) || c.traffic.failed!=0 || c.traffic.lostGoods!=0 || !c.Valid())throw new InvalidOperationException("Native crossing attendance/cargo/save continuation");
            var back=sim.DispatchSchool(child.id,true);if(back==null)throw new InvalidOperationException("Native pedestrian real return");sim.Advance(30);
            if(child.location!=home || child.schoolReturning || !c.Valid())throw new InvalidOperationException("Native pedestrian return home");
            File.WriteAllText(Path.Combine(output,prefix+(wide?"WidePedestrians.txt":"Pedestrians.txt")),"PASS: real pupil red wait outside width-based crossing boundary, parallel freight passes alongside walker, crossing freight yields to actual crossing, actual sidewalk render and all four crosswalk stripe meshes, native JSON during crossing, both cargo deliveries/returns and actual school attendance/walk home");
        }
        IEnumerator CheckWideConnectionInput(HarborCityGame game,Mouse mouse)
        {
            const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;var type=typeof(HarborCityGame);var originalCity=type.GetField("city",flags).GetValue(game);var originalTraffic=type.GetField("traffic",flags).GetValue(game);
            var land=(CityLandscape)type.GetField("landscape",flags).GetValue(game);var c=CityModel.Create();c.development.enabled=false;c.SetEntranceHeight(land.Height);
            var street=c.roads.Plan(c,c.roads.Node(CityRoads.Entrance),new RoadNode{x=40,z=1.5f,y=land.Height(40,1.5f)},land.Height,false,RoadKind.FourLane);
            if(!c.CommitRoad(street)){File.WriteAllText(Path.Combine(output,"Failure.txt"),"Wide junction native initial road failed: "+street.error);yield break;}
            try
            {
                type.GetField("city",flags).SetValue(game,c);type.GetField("traffic",flags).SetValue(game,new CityTraffic(c));type.GetField("selected",flags).SetValue(game,LandUse.Road);type.GetField("roadOperation",flags).SetValue(game,0);type.GetField("roadMode",flags).SetValue(game,0);type.GetField("roadKind",flags).SetValue(game,RoadKind.FourLane);type.GetField("roadOneWay",flags).SetValue(game,false);type.GetMethod("ResetRoad",flags).Invoke(game,null);type.GetMethod("RefreshRoadView",flags).Invoke(game,null);
                foreach(var pair in new[]{new[]{new Vector2(0,1.5f),new Vector2(0,18)},new[]{new Vector2(0,-22),new Vector2(0,-10)},new[]{new Vector2(0,-10),new Vector2(0,1.5f)}})
                {
                    type.GetMethod("ResetRoad",flags).Invoke(game,null);int stroke=c.roads.nextStroke;yield return ClickWorld(game,mouse,pair[0]);yield return ClickWorld(game,mouse,pair[1]);
                    if(c.roads.nextStroke!=stroke+1){File.WriteAllText(Path.Combine(output,"Failure.txt"),"Wide junction world-input connection failed: "+type.GetField("notice",flags).GetValue(game));yield break;}
                }
                int center=c.roads.nodes.First(n=>Math.Abs(n.x)<.01f && Math.Abs(n.z-1.5f)<.01f).id;
                if(c.roads.Neighbors(center).Count!=4 || !c.roads.HasSignals(center) || !c.Valid()){File.WriteAllText(Path.Combine(output,"Failure.txt"),"Wide junction actual four-arm topology/control failed");yield break;}
                c=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(c.ToSaveData())).ToCity();
                int factory=c.PlaceBuilding(c.RoadsidePreview(5,-18,LandUse.Industrial,HousingKind.Apartment,out _),LandUse.Industrial,land.Height,out _),shop=c.PlaceBuilding(c.RoadsidePreview(5,13,LandUse.Commercial,HousingKind.Apartment,out _),LandUse.Commercial,land.Height,out _);
                if(factory<0 || shop<0){File.WriteAllText(Path.Combine(output,"Failure.txt"),"Wide junction real cargo building fixture failed");yield break;}
                c.Inventory(factory).Stock=8;var sim=new CityTraffic(c);var trip=sim.Dispatch(factory,shop,TripPurpose.Delivery);
                if(trip==null || !trip.route.Contains(center)){File.WriteAllText(Path.Combine(output,"Failure.txt"),"Wide junction real freight route failed");yield break;}
                int id=trip.id;sim.Advance(.3f);c=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(c.ToSaveData())).ToCity();sim=new CityTraffic(c);sim.Advance(80);
                if(c.Goods(shop)<8 || c.traffic.failed!=0 || c.traffic.lostGoods!=0 || c.traffic.trips.Any(t=>t.id==id) || !c.Valid()){File.WriteAllText(Path.Combine(output,"Failure.txt"),"Wide junction saved actual cargo delivery/return failed");yield break;}
                File.WriteAllText(Path.Combine(output,"WideConnections.txt"),"PASS: actual world-input four-lane T connection, separate south stub joined to form four-arm crossroads, native JSON, default signals, real cross-junction cargo delivery and return without task loss");
                File.AppendAllText(Path.Combine(output,"Result.txt"),"; four-lane T/stub/crossroads world-input and real freight PASS");
            }
            finally{type.GetField("city",flags).SetValue(game,originalCity);type.GetField("traffic",flags).SetValue(game,originalTraffic);type.GetField("roadKind",flags).SetValue(game,RoadKind.Standard);type.GetMethod("ResetRoad",flags).Invoke(game,null);type.GetMethod("RefreshRoadView",flags).Invoke(game,null);type.GetMethod("AnimateTraffic",flags).Invoke(game,null);}
        }
        static void CheckRoadTypes(string output)
        {
            Func<float,float,float> flat=(x,z)=>1;var c=CityModel.Create();c.development.enabled=false;c.SetEntranceHeight(flat);
            var plan=c.roads.Plan(c,c.roads.Node(CityRoads.Entrance),new RoadNode{x=45,z=1.5f,y=1},flat,false,RoadKind.Gravel);
            if(!c.CommitRoad(plan))throw new InvalidOperationException("Native gravel build failed");
            int origin=c.PlaceBuilding(c.RoadsidePreview(-35,7,LandUse.Industrial,HousingKind.Apartment,out _),LandUse.Industrial,flat,out _);
            int destination=c.PlaceBuilding(c.RoadsidePreview(35,7,LandUse.Commercial,HousingKind.Apartment,out _),LandUse.Commercial,flat,out _);
            c.Inventory(origin).Stock=8;var sim=new CityTraffic(c);var t=sim.Dispatch(origin,destination,TripPurpose.Delivery);if(t==null)throw new InvalidOperationException("Native gravel freight failed");
            sim.Advance(.05f);c=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(c.ToSaveData())).ToCity();t=c.traffic.trips.First(p=>p.id==t.id);
            float before=t.progress;int funds=c.money;int upkeep=c.RoadMaintenance;
            if(!c.PreviewRoadUpgrade(c.roads.edges[0].id,RoadKind.Standard,false,out int cost,out _) || !c.UpgradeRoad(c.roads.edges[0].id,RoadKind.Standard,false,out _) || c.money!=funds-cost || t.progress!=before || c.RoadMaintenance<=upkeep)throw new InvalidOperationException("Native moving gravel upgrade failed");
            c=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(c.ToSaveData())).ToCity();sim=new CityTraffic(c);sim.Advance(60);
            if(!c.Valid() || c.Goods(destination)<8 || c.traffic.failed>0 || c.traffic.trips.Any(p=>p.id==t.id) || c.roads.edges.Any(e=>e.kind!=RoadKind.Standard))throw new InvalidOperationException("Native upgraded freight save/delivery/return failed");
            File.WriteAllText(Path.Combine(output,"RoadTypes.txt"),"PASS: gravel construction, real moving freight, native JSON, paid paving with upkeep/speed changes, preserved position, cargo delivery and return");
        }
        static void CheckJunctions(string output)
        {
            Func<float,float,float> flat=(x,z)=>1;var c=CityModel.Create();c.development.enabled=false;c.SetEntranceHeight(flat);
            foreach(var points in new[]{new[]{c.roads.Node(CityRoads.Entrance),new RoadNode{x=48,z=1.5f,y=1}},new[]{new RoadNode{x=0,z=-40,y=1},new RoadNode{x=0,z=40,y=1}}})
                if(!c.CommitRoad(c.roads.Plan(c,points[0],points[1],flat)))throw new InvalidOperationException("Native crossroads construction failed");
            int west=c.PlaceBuilding(c.RoadsidePreview(-35,7,LandUse.Industrial,HousingKind.Apartment,out _),LandUse.Industrial,flat,out _);
            int eastShop=c.PlaceBuilding(c.RoadsidePreview(35,7,LandUse.Commercial,HousingKind.Apartment,out _),LandUse.Commercial,flat,out _);
            int east=c.PlaceBuilding(c.RoadsidePreview(35,-5,LandUse.Industrial,HousingKind.Apartment,out _),LandUse.Industrial,flat,out _);
            int westShop=c.PlaceBuilding(c.RoadsidePreview(-35,-5,LandUse.Commercial,HousingKind.Apartment,out _),LandUse.Commercial,flat,out _);
            int node=c.roads.nodes.First(n=>Math.Abs(n.x)<.01f && Math.Abs(n.z-1.5f)<.01f).id;
            c.Inventory(west).Stock=8;c.Inventory(east).Stock=8;var sim=new CityTraffic(c);
            var trips=new[]{sim.Dispatch(west,eastShop,TripPurpose.Delivery),sim.Dispatch(east,westShop,TripPurpose.Delivery)};
            foreach(var t in trips){if(t==null)throw new InvalidOperationException("Native junction dispatch failed");t.segment=t.route.IndexOf(node)-1;t.progress=1-.4f/(CityRoads.Length(c.roads.Node(t.Current),c.roads.Node(t.Next))/3);}
            sim=new CityTraffic(c);sim.Advance(.15f);
            if(trips.Any(t=>t.Current!=node || t.blocked>0))throw new InvalidOperationException("Native opposite freight did not share junction");
            c=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(c.ToSaveData())).ToCity();sim=new CityTraffic(c);sim.Advance(60);
            if(!c.Valid() || c.traffic.failed>0 || c.Goods(eastShop)<8 || c.Goods(westShop)<8 || c.traffic.trips.Any(t=>trips.Any(p=>p.id==t.id)))throw new InvalidOperationException("Native shared junction save/delivery/return failed");
            File.WriteAllText(Path.Combine(output,"Junctions.txt"),"PASS: simultaneous opposite real freight, native JSON in-junction save, both cargo deliveries and returns without task loss");
        }
        static void CheckRoadDirection(string output)
        {
            Func<float,float,float> flat=(x,z)=>1;var c=CityModel.Create();c.development.enabled=false;c.SetEntranceHeight(flat);
            var a=new RoadNode{x=-45,z=1.5f,y=1};var b=new RoadNode{x=30,z=1.5f,y=1};var d=new RoadNode{x=30,z=25,y=1};var e=new RoadNode{x=-45,z=25,y=1};
            var points=new[]{c.roads.Node(CityRoads.Entrance),a,b,d,e,a};
            for(int n=1;n<points.Length;n++)if(!c.CommitRoad(c.roads.Plan(c,points[n-1],points[n],flat,n>1)))throw new InvalidOperationException("Native directed loop construction failed");
            int origin=c.PlaceBuilding(c.RoadsidePreview(-35,7,LandUse.Industrial,HousingKind.Apartment,out _),LandUse.Industrial,flat,out _);
            int destination=c.PlaceBuilding(c.RoadsidePreview(20,7,LandUse.Commercial,HousingKind.Apartment,out _),LandUse.Commercial,flat,out _);
            c.Inventory(origin).Stock=8;var sim=new CityTraffic(c);var trip=sim.Dispatch(origin,destination,TripPurpose.Delivery);
            if(trip==null || sim.FindRoute(destination,origin).Count<=trip.route.Count)throw new InvalidOperationException("Native vehicle did not respect one-way return detour");
            sim.Advance(.05f);int id=trip.id;c=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(c.ToSaveData())).ToCity();sim=new CityTraffic(c);
            for(int n=0;n<4000 && c.traffic.trips.Any(t=>t.id==id);n++)sim.Advance(.05f);
            if(c.traffic.trips.Any(t=>t.id==id) || c.Goods(destination)<8 || !c.Valid())throw new InvalidOperationException("Native saved one-way freight did not deliver and return");
            File.WriteAllText(Path.Combine(output,"RoadDirection.txt"),"PASS: directed loop, real freight detour/delivery/return and native JSON continuation");
        }
        static void CheckUtilityAllocation(CityModel source,string output)
        {
            var probe=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(source.ToSaveData())).ToCity();
            var families=probe.society.families.Where(h=>h.resident).ToArray();int n=0;
            if(families.Length==0)throw new InvalidOperationException("Utility allocation fixture needs resident households");
            while(probe.demand<=probe.power)
            {
                var h=families[n++%families.Length];h.people.Add(new CityResident{id=probe.society.nextCitizenId++,householdId=h.id,name="供给测试居民",age=10,location=h.home});probe.Recalculate();
            }
            int supplied=probe.buildings.Count(b=>probe.Supply(b.id).electricity);
            if(supplied==0 || supplied==probe.buildings.Count || probe.water==0 || probe.SewageCapacity==0
                || probe.buildings.Where(b=>b is WaterBuilding || b is SewageBuilding).Any(b=>!probe.Supply(b.id).electricity))
                throw new InvalidOperationException("Native slight overload cut off the entire city or water facilities");
            string ids=string.Join(",",probe.buildings.Where(b=>probe.Supply(b.id).electricity).Select(b=>b.id).OrderBy(id=>id));
            probe=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(probe.ToSaveData())).ToCity();
            if(!probe.Valid() || ids!=string.Join(",",probe.buildings.Where(b=>probe.Supply(b.id).electricity).Select(b=>b.id).OrderBy(id=>id)))
                throw new InvalidOperationException("Native load shedding changed after save reload");
            File.WriteAllText(Path.Combine(output,"UtilityAllocation.txt"),"PASS: native electricity demand="+probe.demand+" / capacity="+probe.power+"; powered="+supplied+" / buildings="+probe.buildings.Count+"; water="+probe.water+"; sewage="+probe.SewageCapacity+"; stable native save reload. No GUI claim.");
        }
        static void CheckLoans(CityModel source,string output)
        {
            var city=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(source.ToSaveData())).ToCity();
            int money=city.money;
            foreach(var offer in CityModel.LoanOffers)if(!city.TakeLoan(offer.id,out string reason))throw new InvalidOperationException(reason);
            if(city.money!=money+280000 || city.LoanDebt!=317000 || !city.Valid())throw new InvalidOperationException("Loan principal or debt mismatch");
            city.Tick();var d=city.society.history.Last();
            if(d.loanPayment<=0 || d.closingTreasury!=d.openingTreasury+d.tax-d.maintenance-d.loanPayment)throw new InvalidOperationException("Loan ledger mismatch");
            var saved=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(city.ToSaveData())).ToCity();
            city.Tick();saved.Tick();
            if(saved.money!=city.money || saved.LoanDebt!=city.LoanDebt || !saved.Valid())throw new InvalidOperationException("Loan native save continuation mismatch");
            if(!saved.RepayLoanEarly(saved.ActiveLoan(0).id,out _) || saved.ActiveLoan(0)!=null || !saved.Valid())throw new InvalidOperationException("Loan early repayment failed");
            var old=source.ToSaveData();old.format=23;old.loans=null;var migrated=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(old)).ToCity();
            if(migrated.money!=source.money || migrated.population!=source.population || migrated.LoanDebt!=0 || !migrated.Valid())throw new InvalidOperationException("Loan legacy migration failed");
            File.WriteAllText(Path.Combine(output,"Loans.txt"),"PASS: three actual loans, treasury ledger, native JSON continuation, early repayment and format-23 migration; original city unchanged");
        }
        static void CheckHealthcare(CityModel source,Func<float,float,float> height,string output)
        {
            var city=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(source.ToSaveData())).ToCity();city.waste.enabled=false;city.developmentHeight=null;city.society.settings.applicantsPerDay=0;
            var east=new RoadNode{x=48,z=1.5f,y=height(48,1.5f)};
            var extend=city.roads.Plan(city,new RoadNode{x=28,z=1.5f,y=height(28,1.5f)},east,height);
            if(!city.CommitRoad(extend))throw new InvalidOperationException("Medical fixture road: "+extend.error);
            foreach(var kind in new[]{UtilityKind.Electricity,UtilityKind.Water})
                if(!city.BuildUtility(kind,new RoadNode{x=28,z=0},new RoadNode{x=48,z=0},out string utilityError))throw new InvalidOperationException(utilityError);
            var lot=city.RoadsidePreview(40,-5,LandUse.Clinic,HousingKind.Villa,out string error);
            int clinic=city.PlaceBuilding(lot,LandUse.Clinic,height,out error);if(clinic<0 || !city.HasBasicServices(clinic))throw new InvalidOperationException("Medical fixture clinic: "+error);
            city.SetServiceBudget(CityServiceKind.Healthcare,150);
            city=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(city.ToSaveData())).ToCity();
            if(city.ClinicCapacity!=12 || city.ClinicFleetLimit!=3 || city.development.healthcareBudget!=150)throw new InvalidOperationException("Medical native budget save failed");
            city.SetServiceBudget(CityServiceKind.Healthcare,100);
            var person=city.Citizens.First(p=>p.tripId==0 && !p.atWork && p.medicalStage==MedicalStage.None && p.location==city.society.families.First(h=>h.id==p.householdId).home);
            int citizen=person.id;person.sick=true;person.health=30;foreach(var p in city.Citizens.Where(p=>p.id!=citizen))city.ReleaseJob(p);
            var traffic=new CityTraffic(city);var trip=traffic.DispatchHealthcare(citizen);
            if(trip==null || !trip.medicalPickup || city.ClinicPatients(clinic)!=0)throw new InvalidOperationException("Medical native pickup dispatch failed");
            for(int n=0;n<4000 && person.medicalStage!=MedicalStage.Treatment;n++)traffic.Advance(.05f);
            if(person.medicalStage!=MedicalStage.Treatment || person.location!=clinic)throw new InvalidOperationException("Medical native admission failed");
            city=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(city.ToSaveData())).ToCity();person=city.Citizens.First(p=>p.id==citizen);traffic=new CityTraffic(city);
            if(person.medicalStage!=MedicalStage.Treatment || city.ClinicPatients(clinic)!=1)throw new InvalidOperationException("Medical native patient save failed");
            for(int n=0;n<6000 && person.medicalStage!=MedicalStage.None;n++)traffic.Advance(.05f);
            var home=city.society.families.First(h=>h.id==person.householdId).home;
            if(!city.Valid() || person.sick || person.medicalStage!=MedicalStage.None || person.location!=home || person.medicalClinicId!=-1)throw new InvalidOperationException("Medical native treatment or actual return failed");
            File.WriteAllText(Path.Combine(output,"Healthcare.txt"),"PASS: real clinic, ambulance pickup and delivery, native patient save, treatment and walking home; citizen="+citizen+"; health="+person.health.ToString("F1"));
            File.WriteAllText(Path.Combine(output,"HealthcareCity.json"),JsonUtility.ToJson(city.ToSaveData(),true));
        }

        static void CheckEducation(Func<float,float,float> height,string output)
        {
            var city=JsonUtility.FromJson<CitySaveData>(File.ReadAllText(Path.Combine(output,"HealthcareCity.json"))).ToCity();
            var lot=city.RoadsidePreview(40,5,LandUse.ElementarySchool,HousingKind.Villa,out string error);
            int school=city.PlaceBuilding(lot,LandUse.ElementarySchool,height,out error);if(school<0 || !city.HasBasicServices(school))throw new InvalidOperationException("Native school placement: "+error);
            city.SetServiceBudget(CityServiceKind.Education,150);city=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(city.ToSaveData())).ToCity();
            if(city.SchoolCapacity!=36 || city.development.educationBudget!=150)throw new InvalidOperationException("Native education budget save failed");
            city.SetServiceBudget(CityServiceKind.Education,100);
            var parkLot=city.RoadsidePreview(-60,5,LandUse.Park,HousingKind.Villa,out error);
            if(city.PlaceBuilding(parkLot,LandUse.Park,height,out error)<0)throw new InvalidOperationException("Native local recreation: "+error);
            var pupil=city.Citizens.First(p=>city.ElementaryEligible(p) && p.tripId==0 && p.medicalStage==MedicalStage.None && !p.sick);
            int citizen=pupil.id;city.society.dayElapsed=40;var traffic=new CityTraffic(city);
            if(!city.EnrollElementary(citizen,school) || traffic.DispatchSchool(citizen)==null || city.StudentsAt(school)!=0)throw new InvalidOperationException("Native actual school dispatch failed");
            for(int n=0;n<6000 && !pupil.atSchool;n++)traffic.Advance(.05f);
            if(!pupil.atSchool || pupil.location!=school || pupil.studyMinutes<=0)throw new InvalidOperationException("Native actual school admission failed");
            city=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(city.ToSaveData())).ToCity();pupil=city.Citizens.First(p=>p.id==citizen);
            if(!pupil.atSchool || pupil.schoolId!=school)throw new InvalidOperationException("Native classroom save failed");
            traffic=new CityTraffic(city);traffic.Advance(2201);
            city=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(city.ToSaveData())).ToCity();pupil=city.Citizens.First(p=>p.id==citizen);
            if(!city.Valid() || pupil.education!=1 || pupil.studyMinutes<CityModel.ElementaryStudyMinutes || pupil.schoolId!=school || pupil.jobId>=0)throw new InvalidOperationException("Native repeated actual lessons or graduation failed");
            File.WriteAllText(Path.Combine(output,"Education.txt"),"PASS: real elementary places, walking, classroom native JSON, repeated actual lessons, personal graduation and education budget; citizen="+citizen+"; day="+city.day+"; studied="+pupil.studyMinutes.ToString("F0")+"; enrolled="+city.EnrolledAt(school));
            File.WriteAllText(Path.Combine(output,"EducationCity.json"),JsonUtility.ToJson(city.ToSaveData(),true));
            int home=city.society.families.First(h=>h.id==pupil.householdId).home;
            if(city.GetBuilding(home).level<2 || city.Residence(home).housingUnits<2)throw new InvalidOperationException("Native actual education and local services did not develop housing: "+city.BuildingUpgradeReason(home)+"; value="+city.LandValue(home));
            File.WriteAllText(Path.Combine(output,"BuildingGrowth.txt"),"PASS: actual repeated school lessons, local recreation and supplied service improve stable housing; home="+home+"; level="+city.GetBuilding(home).level+"; units="+city.HousingCapacity(home)+"; landValue="+city.LandValue(home).ToString("F1"));
        }
        static void CheckDeathcare(Func<float,float,float> height,string output)
        {
            var city=JsonUtility.FromJson<CitySaveData>(File.ReadAllText(Path.Combine(output,"EducationCity.json"))).ToCity();
            city.fire.chancePerBuildingDay=0;city.society.settings.applicantsPerDay=0;city.money+=CityModel.Cost(LandUse.Cemetery);
            var lot=city.RoadsidePreview(34,-5,LandUse.Cemetery,HousingKind.Villa,out string error);int cemetery=city.PlaceBuilding(lot,LandUse.Cemetery,height,out error);
            if(cemetery<0 || !city.HasBasicServices(cemetery))throw new InvalidOperationException("Native cemetery construction: "+error);
            var p=city.Citizens.First(r=>r.canWork && r.jobId>=0 && r.medicalStage==MedicalStage.None && !r.sick);int citizen=p.id;var sim=new CityTraffic(city);
            for(int n=0;n<6000 && (p.tripId!=0 || p.atWork);n++)sim.Advance(.05f);
            if(p.tripId!=0 || p.atWork)throw new InvalidOperationException("Native deathcare resident did not physically return before controlled incident");
            int population=city.population,job=p.jobId;int home=city.society.families.First(h=>h.id==p.householdId).home;
            if(!city.DieResident(citizen) || city.population!=population-1 || city.Job(job).occupiedCitizenId>=0 || city.Corpse(citizen).buildingId!=home || !city.Valid())throw new InvalidOperationException("Native death did not preserve real body and release individual employment");
            var body=city.Corpse(citizen);sim.DispatchHearse(cemetery,citizen);
            for(int n=0;n<6000 && body.stage==CorpseStage.Waiting;n++)sim.Advance(.05f);
            if(body.stage!=CorpseStage.Assigned)throw new InvalidOperationException("Native hearse assignment missing before blocked pickup");
            var blocked=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(city.ToSaveData())).ToCity();var originalRoads=blocked.roads.Copy();
            blocked.roads.edges.Clear();blocked.roads.Changed();for(int n=0;n<6;n++)blocked.Tick();
            if(!blocked.GetBuilding(home).abandoned || blocked.Corpse(citizen).stage!=CorpseStage.Assigned || blocked.HearsesAt(cemetery)!=1 || !blocked.Valid())throw new InvalidOperationException("Native abandonment discarded assigned hearse or corpse");
            blocked=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(blocked.ToSaveData())).ToCity();blocked.roads=originalRoads;var blockedTraffic=new CityTraffic(blocked);
            for(int n=0;n<6000 && blocked.Corpse(citizen).stage!=CorpseStage.Buried;n++)blockedTraffic.Advance(.05f);
            if(blocked.Corpse(citizen).stage!=CorpseStage.Buried || !blocked.Valid())throw new InvalidOperationException("Native saved abandoned-building pickup could not resume");
            for(int n=0;n<6000 && body.stage!=CorpseStage.InTransit;n++)sim.Advance(.05f);
            if(body.stage!=CorpseStage.InTransit || city.BuriedAt(cemetery)!=0 || city.BodiesAt(home)!=0)throw new InvalidOperationException("Native hearse did not actually pick up the specific corpse");
            city=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(city.ToSaveData())).ToCity();body=city.Corpse(citizen);sim=new CityTraffic(city);
            if(!city.Valid() || body.stage!=CorpseStage.InTransit || city.AllResidents.First(r=>r.id==citizen).jobId>=0)throw new InvalidOperationException("Native loaded hearse JSON failed");
            File.WriteAllText(Path.Combine(output,"DeathcareSceneCity.json"),JsonUtility.ToJson(city.ToSaveData(),true));
            for(int n=0;n<6000 && body.stage!=CorpseStage.Buried;n++)sim.Advance(.05f);
            if(!city.Valid() || body.stage!=CorpseStage.Buried || city.BuriedAt(cemetery)!=1 || city.HearsesAt(cemetery)!=0 || city.Citizens.Any(r=>r.id==citizen))throw new InvalidOperationException("Native actual burial, fleet release or living-only population failed");
            File.WriteAllText(Path.Combine(output,"Deathcare.txt"),"PASS: controlled death after actual return, living population and concrete job release, preserved history and original body, cemetery construction, actual hearse pickup, native loaded JSON, actual burial and finite fleet release, abandoned-building assigned hearse retained and native saved pickup resumed; citizen="+citizen+"; buried="+city.BuriedAt(cemetery));
        }
        static void CheckResidentIdentity(CityModel source,string output)
        {
            var city=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(source.ToSaveData())).ToCity();
            var people=city.society.families.SelectMany(h=>h.people).ToList();
            var original=people.ToDictionary(p=>p.id,p=>p.householdId);int next=city.society.nextCitizenId;
            if(people.Count==0 || original.Count!=people.Count || people.Any(p=>p.id>=next) || !city.Valid())throw new InvalidOperationException("Native resident identity or counter failed");
            // Reserve a number without fabricating a birth; even unused reservations must not be reused.
            int reserved=city.AllocateResidentId();
            city=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(city.ToSaveData())).ToCity();
            if(reserved!=next || city.society.nextCitizenId!=next+1 || original.Any(pair=>!city.society.families.Any(h=>h.id==pair.Value && h.people.Any(p=>p.id==pair.Key))))throw new InvalidOperationException("Native saved identity was reused or reconstructed from family");
            new CityTraffic(city).Advance(.25f);
            if(!city.Valid())throw new InvalidOperationException("Native actual transport after identity reload failed");
            File.WriteAllText(Path.Combine(output,"ResidentIdentity.txt"),"PASS: real city residents, independent unique IDs, persisted next identity, unused reservation never reused, unchanged family/job/transport associations; nextCitizenId="+city.society.nextCitizenId);
        }
        static void CheckHigherEducation(Func<float,float,float> height,string output)
        {
            var city=JsonUtility.FromJson<CitySaveData>(File.ReadAllText(Path.Combine(output,"EducationCity.json"))).ToCity();
            city.fire.chancePerBuildingDay=0;city.society.settings.applicantsPerDay=0;city.money+=100000;
            // Age boundaries only are controlled; all three qualifications are earned through real lessons.
            var p=city.Citizens.First(r=>r.education==1 && r.schoolId>=0 && !r.sick && r.medicalStage==MedicalStage.None);
            int citizen=p.id;p.age=13;p.ageProgress=.99;p.lastAgeDay=city.day-1;city.UpdateResidentAges();var sim=new CityTraffic(city);
            for(int n=0;n<6000 && (p.schoolReturning || p.tripId!=0);n++)sim.Advance(.05f);
            int home=city.society.families.First(h=>h.id==p.householdId).home;
            if(p.location!=home || p.schoolId>=0)throw new InvalidOperationException("Native high-school applicant did not physically return");
            var lot=city.RoadsidePreview(34,5,LandUse.HighSchool,HousingKind.Villa,out string error);
            int school=city.PlaceBuilding(lot,LandUse.HighSchool,height,out error);
            if(school<0 || !city.HasBasicServices(school))throw new InvalidOperationException("Native high-school placement: "+error);
            city.society.dayElapsed=40;
            if(!city.EnrollSchool(citizen,school) || sim.DispatchSchool(citizen)==null)throw new InvalidOperationException("Native high-school enrollment failed");
            city=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(city.ToSaveData())).ToCity();p=city.Citizens.First(r=>r.id==citizen);sim=new CityTraffic(city);sim.Advance(1600);
            if(!city.Valid() || p.education!=2 || p.highSchoolStudyMinutes<CityModel.CourseMinutes(2) || p.universityStudyMinutes!=0)throw new InvalidOperationException("Native high-school repeated actual lessons failed");
            p.age=17;p.ageProgress=.99;p.lastAgeDay=city.day-1;city.UpdateResidentAges();
            for(int n=0;n<6000 && (p.schoolReturning || p.tripId!=0);n++)sim.Advance(.05f);
            if(p.location!=home || p.schoolId>=0)throw new InvalidOperationException("Native university applicant did not physically return");
            lot=city.RoadsidePreview(46,5,LandUse.University,HousingKind.Villa,out error);int university=city.PlaceBuilding(lot,LandUse.University,height,out error);
            if(university<0 || !city.HasBasicServices(university))throw new InvalidOperationException("Native university placement: "+error);
            city.society.dayElapsed=40;
            if(!city.EnrollSchool(citizen,university) || p.canWork || p.jobId>=0 || sim.DispatchSchool(citizen)==null)throw new InvalidOperationException("Native university actual enrollment failed");
            city=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(city.ToSaveData())).ToCity();p=city.Citizens.First(r=>r.id==citizen);sim=new CityTraffic(city);sim.Advance(2600);
            if(!city.Valid() || p.education!=3 || p.universityStudyMinutes<CityModel.CourseMinutes(3) || p.schoolId>=0 || !p.canWork)throw new InvalidOperationException("Native university actual graduation or qualification release failed");
            File.WriteAllText(Path.Combine(output,"HigherEducation.txt"),"PASS: actual prior elementary qualification, high-school and university construction, separate repeated actual lessons, saved real walking and full-time student, individual graduation; citizen="+citizen+"; elementary="+p.studyMinutes.ToString("F0")+"; highSchool="+p.highSchoolStudyMinutes.ToString("F0")+"; university="+p.universityStudyMinutes.ToString("F0"));
            File.WriteAllText(Path.Combine(output,"HigherEducationCity.json"),JsonUtility.ToJson(city.ToSaveData(),true));
        }
        static void CheckAging(string output)
        {
            var city=JsonUtility.FromJson<CitySaveData>(File.ReadAllText(Path.Combine(output,"EducationCity.json"))).ToCity();
            // Birthday boundaries are controlled fixture inputs, not claimed original pacing.
            city.fire.chancePerBuildingDay=0;city.society.settings.applicantsPerDay=0;
            var pupil=city.Citizens.First(p=>p.schoolId>=0 && p.tripId==0 && !p.sick && p.medicalStage==MedicalStage.None);
            pupil.age=13;pupil.ageProgress=.99;pupil.lastAgeDay=city.day-1;city.society.dayElapsed=40;
            var sim=new CityTraffic(city);
            if(!pupil.atSchool && sim.DispatchSchool(pupil.id)==null)throw new InvalidOperationException("Native aging pupil dispatch failed");
            for(int n=0;n<6000 && !pupil.atSchool;n++)sim.Advance(.05f);
            if(!pupil.atSchool)throw new InvalidOperationException("Native aging pupil arrival failed");
            int id=pupil.id,location=pupil.location;float studied=pupil.studyMinutes;int education=pupil.education;
            city.UpdateResidentAges();
            if(pupil.age!=14 || pupil.schoolId>=0 || pupil.atSchool || !pupil.schoolReturning || pupil.location!=location || pupil.education!=education || pupil.studyMinutes!=studied)throw new InvalidOperationException("Native aging school departure changed physical state or earned education");
            city=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(city.ToSaveData())).ToCity();pupil=city.Citizens.First(p=>p.id==id);sim=new CityTraffic(city);
            for(int n=0;n<6000 && (pupil.schoolReturning || pupil.tripId!=0);n++)sim.Advance(.05f);
            int home=city.society.families.First(h=>h.id==pupil.householdId).home;
            if(!city.Valid() || pupil.schoolReturning || pupil.tripId!=0 || pupil.location!=home)throw new InvalidOperationException("Native older pupil actual return failed");
            var worker=city.Citizens.First(p=>p.canWork && p.jobId>=0 && !p.sick && p.medicalStage==MedicalStage.None);
            for(int n=0;n<6000 && (worker.tripId!=0 || worker.atWork);n++)sim.Advance(.05f);
            if(worker.tripId!=0 || worker.atWork)throw new InvalidOperationException("Native aging worker did not physically return before fixture commute");
            var family=city.society.families.First(h=>h.id==worker.householdId);
            var trip=sim.Dispatch(family.home,city.Workplace(worker),TripPurpose.Commute,worker.id);
            if(trip==null)throw new InvalidOperationException("Native retirement commute dispatch failed");
            trip.householdId=family.id;trip.home=family.home;worker.tripId=trip.id;worker.departureDay=city.day;sim.Advance(.25f);
            int node=trip.Current;float progress=trip.progress;id=worker.id;worker.age=64;worker.ageProgress=.99;worker.lastAgeDay=city.day-1;
            city.UpdateResidentAges();
            if(worker.canWork || worker.jobId>=0 || !trip.returning || trip.Current!=node || trip.progress!=progress)throw new InvalidOperationException("Native retirement did not retain and redirect actual car");
            city=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(city.ToSaveData())).ToCity();worker=city.Citizens.First(p=>p.id==id);sim=new CityTraffic(city);
            for(int n=0;n<6000 && worker.tripId!=0;n++)sim.Advance(.05f);
            home=city.society.families.First(h=>h.id==worker.householdId).home;
            if(!city.Valid() || worker.tripId!=0 || worker.atWork || worker.location!=home || worker.canWork)throw new InvalidOperationException("Native retired worker actual return failed");
            File.WriteAllText(Path.Combine(output,"Aging.txt"),"PASS: controlled birthdays, actual school arrival and age departure, earned education preserved, native JSON return, concrete job release and same-car retirement return; daysPerYear="+city.lifecycle.daysPerYear);
        }
        static void CheckFire(Func<float,float,float> height,string output)
        {
            var city=JsonUtility.FromJson<CitySaveData>(File.ReadAllText(Path.Combine(output,"EducationCity.json"))).ToCity();
            // Controlled incident fixture only; normal integration retains natural fires.
            city.fire.chancePerBuildingDay=0;city.money+=CityModel.Cost(LandUse.FireHouse);
            var lot=city.RoadsidePreview(34,-5,LandUse.FireHouse,HousingKind.Villa,out string error);
            int station=city.PlaceBuilding(lot,LandUse.FireHouse,height,out error);
            if(station<0 || !city.HasBasicServices(station))throw new InvalidOperationException("Native fire station: "+error);
            int target=city.buildings.OfType<IndustrialBuilding>().First(b=>!b.abandoned).id;
            city.IgniteBuilding(target);var sim=new CityTraffic(city);var engine=sim.DispatchFire(station,target);
            if(engine==null)throw new InvalidOperationException("Native fire dispatch failed");
            for(int n=0;n<5000 && engine.status!=TripStatus.Visiting;n++)sim.Advance(.05f);
            if(engine.status!=TripStatus.Visiting || !city.GetBuilding(target).burning)throw new InvalidOperationException("Native engine did not arrive at the fire");
            city.SetServiceBudget(CityServiceKind.Fire,150);
            city=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(city.ToSaveData())).ToCity();sim=new CityTraffic(city);
            if(!city.Valid() || city.FireFleetLimit!=3 || !city.GetBuilding(target).burning)throw new InvalidOperationException("Native fire response save failed");
            File.WriteAllText(Path.Combine(output,"FireSceneCity.json"),JsonUtility.ToJson(city.ToSaveData(),true));
            for(int n=0;n<5000 && city.FireEnginesAt(station)>0;n++)sim.Advance(.05f);
            if(city.GetBuilding(target).burning || city.GetBuilding(target).burned || city.FireEnginesAt(station)>0 || !city.Valid())throw new InvalidOperationException("Native actual suppression and return failed");
            city.IgniteBuilding(target);city.GetBuilding(target).fireDamage=99;city.AdvanceFires(20);
            city=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(city.ToSaveData())).ToCity();
            if(!city.Valid() || !city.GetBuilding(target).burned || city.society.jobEntities.Any(j=>j.buildingId==target))throw new InvalidOperationException("Native ruin or actual job release failed");
            File.WriteAllText(Path.Combine(output,"Fire.txt"),"PASS: actual station, dispatch, on-scene native JSON, budget, suppression, physical return, ruin and Job removal; target="+target);
        }
        static void CheckServiceRoadChanges(string output)
        {
            foreach(bool fire in new[]{false,true})
            {
                Func<float,float,float> flat=(x,z)=>1;
                var city=CityModel.Create();city.development.enabled=false;city.SetEntranceHeight(flat);
                if(!city.CommitRoad(city.roads.Plan(city,city.roads.Node(CityRoads.Entrance),new RoadNode{x=48,z=1.5f,y=1},flat))
                    || !city.CommitRoad(city.roads.Plan(city,city.roads.Node(CityRoads.Entrance),new RoadNode{x=-68,z=1.5f,y=1},flat)))throw new InvalidOperationException("Native road-response fixture access failed");
                int Place(float x,float z,LandUse use)
                {
                    var lot=city.RoadsidePreview(x,z,use,HousingKind.Apartment,out string error);
                    int id=city.PlaceBuilding(lot,use,(px,pz)=>px< -78?0:1,out error);if(id<0)throw new InvalidOperationException(error);return id;
                }
                int target=Place(-42.137f,7,LandUse.Residential);Place(-46,-5,LandUse.Power);Place(-38,-5,LandUse.Water);Place(-64,-5,LandUse.Sewage);
                int station=Place(40,-5,fire?LandUse.FireHouse:LandUse.PoliceStation);
                city.development.enabled=true;city.waste.enabled=false;city.fire.chancePerBuildingDay=0;city.society.settings.applicantsPerDay=0;
                foreach(var kind in new[]{UtilityKind.Electricity,UtilityKind.Water})if(!city.BuildUtility(kind,new RoadNode{x=-64,z=0},new RoadNode{x=48,z=0},out string error))throw new InvalidOperationException(error);
                if(fire)city.IgniteBuilding(target);else city.GetBuilding(target).crime=90;
                var sim=new CityTraffic(city);var trip=fire?sim.DispatchFire(station,target):sim.DispatchPolice(station,target);
                if(trip==null)throw new InvalidOperationException("Native road-response dispatch failed");
                for(int n=0;n<6000 && trip.status!=TripStatus.Visiting;n++)sim.Advance(.05f);
                if(trip.status!=TripStatus.Visiting)throw new InvalidOperationException("Native road-response arrival failed");
                int previous=trip.Current;var b=city.GetBuilding(target);
                var road=city.roads.Plan(city,new RoadNode{x=b.entranceX,z=1.5f,y=1},new RoadNode{x=b.entranceX,z=-10,y=1},flat);
                if(!city.CommitRoad(road) || city.AccessBuilding(target).Contains(previous))throw new InvalidOperationException("Native junction did not change access: "+road.error);
                sim.Advance(.05f);
                if(trip.Current!=previous || trip.status==TripStatus.Visiting || trip.returning)throw new InvalidOperationException("Native stopped response teleported or failed to resume");
                int tripId=trip.id;city=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(city.ToSaveData())).ToCity();sim=new CityTraffic(city);trip=city.traffic.trips.Single(t=>t.id==tripId);
                for(int n=0;n<6000 && trip.status!=TripStatus.Visiting;n++)sim.Advance(.05f);
                if(trip.status!=TripStatus.Visiting || trip.Current==previous || !city.AccessBuilding(target).Contains(trip.Current))throw new InvalidOperationException("Native response did not reach updated access");
                for(int n=0;n<6000 && city.traffic.trips.Contains(trip);n++)sim.Advance(.05f);
                if(!city.Valid() || city.traffic.trips.Contains(trip) || (fire?city.GetBuilding(target).burning || city.GetBuilding(target).burned:city.GetBuilding(target).crime!=0))throw new InvalidOperationException("Native updated response did not complete and return");
            }
            File.WriteAllText(Path.Combine(output,"ServiceRoadChanges.txt"),"PASS: actual fire/police arrival, junction construction, changed entrance, physical reapproach, native midway JSON and return");
        }
        static void CheckPolice(Func<float,float,float> height,string output)
        {
            var city=JsonUtility.FromJson<CitySaveData>(File.ReadAllText(Path.Combine(output,"EducationCity.json"))).ToCity();
            city.fire.chancePerBuildingDay=0;city.money+=CityModel.Cost(LandUse.PoliceStation);
            var lot=city.RoadsidePreview(34,5,LandUse.PoliceStation,HousingKind.Villa,out string error);
            int station=city.PlaceBuilding(lot,LandUse.PoliceStation,height,out error);
            if(station<0 || !city.HasBasicServices(station))throw new InvalidOperationException("Native police station: "+error);
            int target=city.buildings.OfType<IndustrialBuilding>().First(b=>!b.abandoned).id;city.GetBuilding(target).crime=60;
            var sim=new CityTraffic(city);var car=sim.DispatchPolice(station,target);
            if(car==null)throw new InvalidOperationException("Native police dispatch failed");
            for(int n=0;n<6000 && car.status!=TripStatus.Visiting;n++)sim.Advance(.05f);
            if(car.status!=TripStatus.Visiting || city.GetBuilding(target).crime<=0)throw new InvalidOperationException("Native police arrival failed");
            city.SetServiceBudget(CityServiceKind.Police,150);city=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(city.ToSaveData())).ToCity();
            if(!city.Valid() || city.PoliceFleetLimit!=3 || !city.traffic.trips.Any(t=>t.id==car.id && t.status==TripStatus.Visiting))throw new InvalidOperationException("Native police scene save failed");
            File.WriteAllText(Path.Combine(output,"PoliceSceneCity.json"),JsonUtility.ToJson(city.ToSaveData(),true));
            sim=new CityTraffic(city);int response=car.id;
            for(int n=0;n<6000 && city.traffic.trips.Any(t=>t.id==response);n++)sim.Advance(.05f);
            if(!city.Valid() || city.GetBuilding(target).crime!=0 || city.traffic.trips.Any(t=>t.id==response))throw new InvalidOperationException("Native police response or actual return failed");
            File.WriteAllText(Path.Combine(output,"Police.txt"),"PASS: actual police station, finite dispatch, on-scene native JSON, budget, building crime reduction and physical return; target="+target);
        }
        IEnumerator ClickWorld(HarborCityGame game,Mouse mouse,Vector2 point)
        {
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;var type=typeof(HarborCityGame);
            var camera=(Camera)type.GetField("cam",flags).GetValue(game);var land=(CityLandscape)type.GetField("landscape",flags).GetValue(game);
            var screen=camera.WorldToScreenPoint(new Vector3(point.x,land.Height(point.x,point.y),point.y));var position=new Vector2(screen.x,screen.y);
            File.AppendAllText(Path.Combine(output,"Input.txt"),"world="+point+"; screen="+position+"; blocked="+type.GetMethod("OverUI",flags).Invoke(game,new object[]{position})+Environment.NewLine);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=position});yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=position}.WithButton(MouseButton.Left));yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=position});yield return null;yield return null;
        }
    }
}
