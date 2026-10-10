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
                File.WriteAllText(Path.Combine(output,"Conditions.txt"),"PASS: outage, actual household/job release, native abandoned-state save, 28-day minimum and real migration recovery");
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
                int roadEdges=inputCity.roads.edges.Count;
                yield return ClickWorld(inputGame,device,new Vector2(28,1.5f));
                yield return ClickWorld(inputGame,device,new Vector2(28,20));
                if(inputCity.roads.edges.Count<=roadEdges)
                {ok=false;File.WriteAllText(Path.Combine(output,"Failure.txt"),"Actual road world-input path failed");}
                else File.AppendAllText(Path.Combine(output,"Result.txt"),"; road world-input PASS");
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
            Application.Quit(ok?0:1);
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
            for(int n=0;n<6000 && body.stage!=CorpseStage.InTransit;n++)sim.Advance(.05f);
            if(body.stage!=CorpseStage.InTransit || city.BuriedAt(cemetery)!=0 || city.BodiesAt(home)!=0)throw new InvalidOperationException("Native hearse did not actually pick up the specific corpse");
            city=JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(city.ToSaveData())).ToCity();body=city.Corpse(citizen);sim=new CityTraffic(city);
            if(!city.Valid() || body.stage!=CorpseStage.InTransit || city.AllResidents.First(r=>r.id==citizen).jobId>=0)throw new InvalidOperationException("Native loaded hearse JSON failed");
            File.WriteAllText(Path.Combine(output,"DeathcareSceneCity.json"),JsonUtility.ToJson(city.ToSaveData(),true));
            for(int n=0;n<6000 && body.stage!=CorpseStage.Buried;n++)sim.Advance(.05f);
            if(!city.Valid() || body.stage!=CorpseStage.Buried || city.BuriedAt(cemetery)!=1 || city.HearsesAt(cemetery)!=0 || city.Citizens.Any(r=>r.id==citizen))throw new InvalidOperationException("Native actual burial, fleet release or living-only population failed");
            File.WriteAllText(Path.Combine(output,"Deathcare.txt"),"PASS: controlled death after actual return, living population and concrete job release, preserved history and original body, cemetery construction, actual hearse pickup, native loaded JSON, actual burial and finite fleet release; citizen="+citizen+"; buried="+city.BuriedAt(cemetery));
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
