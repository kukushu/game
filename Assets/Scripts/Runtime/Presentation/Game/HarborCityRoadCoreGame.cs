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
    // First runnable slice of the new core. Deliberately does not instantiate CityTraffic.
    public sealed class HarborCityRoadCoreGame:MonoBehaviour
    {
        static readonly ProfilerMarker simulationSample=new ProfilerMarker("City.Simulation"),viewsSample=new ProfilerMarker("City.RoadCore.Views");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
#if !CITY_TRAFFIC_BENCHMARK
            // One engine owns the refactor branch. A pre-existing scene component must
            // not launch the legacy city behind this entry; Git retains the full baseline.
            foreach(var old in FindObjectsByType<HarborCityGame>(FindObjectsSortMode.None))old.enabled=false;
            if(FindAnyObjectByType<HarborCityRoadCoreGame>()==null)new GameObject("Harbor City Road Core").AddComponent<HarborCityRoadCoreGame>();
#endif
        }
        World world;CityCoreIndustrySession session;Camera cameraView; Material roadMaterial,carMaterial;
        CityLandscape landscape;LandUse zoneTool=LandUse.Empty;int shownBuildings=-1,shownZones=-1,shownNetwork=-1;
        HarborCityConstructionCamera constructionCamera;
        readonly List<GameObject> zones=new List<GameObject>();Material[] zoneMaterials;
        readonly Dictionary<int,Transform> cars=new Dictionary<int,Transform>();
        readonly HashSet<int> visibleIds=new HashSet<int>();readonly List<int> expiredIds=new List<int>();
        readonly List<GameObject> roads=new List<GameObject>();
        readonly List<GameObject> buildings=new List<GameObject>();
        bool hasStart;Point startPoint;bool building,oneWay,wide;int origin,destination;float speed=1;
        long previousTick;int lastSteps;
        string notice="原工厂劳动→生产→新交通实车运输→原商店库存/支付。居民实际通勤已接入；市政、分区、信号灯和行人尚未接回。";
        string SavePath=>Path.Combine(Application.persistentDataPath,session==null?"harbor-road-core-v13.hcrc":"harbor-city-core-v11.hcity");
        static Vector3 V(Point p)=>new Vector3((float)p.x,(float)p.y,(float)p.z);
        void Start()
        {
            foreach(var c in FindObjectsByType<Camera>(FindObjectsSortMode.None))c.gameObject.SetActive(false);
            cameraView=new GameObject("Road core camera").AddComponent<Camera>();cameraView.transform.position=new Vector3(0,90,-65);cameraView.transform.LookAt(Vector3.zero);cameraView.backgroundColor=new Color(.12f,.18f,.16f);cameraView.farClipPlane=3000;
            var shader=Resources.Load<Shader>("RoadCore");roadMaterial=new Material(shader){color=new Color(.25f,.29f,.32f)};carMaterial=new Material(shader){color=Color.yellow};
            var args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"-harbor-core-validate")>=0||Array.IndexOf(args,"-harbor-core-window")>=0||Array.IndexOf(args,"-harbor-city-core-validate")>=0)Application.runInBackground=true;
            if(Array.IndexOf(args,"-harbor-core-validate")>=0){world=new World();int a=world.AddJunction(new Point(-40,0,-15)),b=world.AddJunction(new Point(0,0,-15)),cnode=world.AddJunction(new Point(35,0,20));world.AddRoad(a,b,new[]{world.JunctionPosition(a),new Point(-20,0,-15),world.JunctionPosition(b)});world.AddRoad(b,cnode,new[]{world.JunctionPosition(b),new Point(15,0,-5),world.JunctionPosition(cnode)});origin=world.AddDepot(a,1000);destination=world.AddDepot(cnode,0);}
            else if(args.Any(a=>a.StartsWith("-harbor-",StringComparison.Ordinal)&&a!="-harbor-city-construction-validate")){session=CityCoreIndustrySession.Create();world=session.Transport;origin=1;destination=2;RenderBuildings();}
            else {session=CityCoreIndustrySession.CreateEmpty();world=session.Transport;var land=new GameObject("City landscape");land.transform.SetParent(transform);landscape=land.AddComponent<CityLandscape>();landscape.Build();session.Simulation.Growth.Zoning.SetSurface(landscape.Height);constructionCamera=new HarborCityConstructionCamera(cameraView,landscape);notice="R3恢复中：空地修路→划区→私人建筑生长。居民迁入、持续供给消费和市政尚未接入。";}
            RenderRoads();
            previousTick=System.Diagnostics.Stopwatch.GetTimestamp();
            int flag=Array.IndexOf(args,"-harbor-core-validate");if(flag>=0&&flag+1<args.Length)StartCoroutine(ValidateNative(args[flag+1]));
            flag=Array.IndexOf(args,"-harbor-core-window");if(flag>=0&&flag+1<args.Length)StartCoroutine(MeasureWindow(args[flag+1]));
            flag=Array.IndexOf(args,"-harbor-city-core-validate");if(flag>=0&&flag+1<args.Length)StartCoroutine(ValidateIndustryNative(args[flag+1]));
            flag=Array.IndexOf(args,"-harbor-city-window");if(flag>=0&&flag+1<args.Length){Application.runInBackground=true;StartCoroutine(MeasureIntegratedWindow(args[flag+1]));}
            flag=Array.IndexOf(args,"-harbor-city-construction-validate");if(flag>=0&&flag+1<args.Length){Application.runInBackground=true;StartCoroutine(ValidateConstruction(args[flag+1]));}
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
            if(world==null)return;long now=System.Diagnostics.Stopwatch.GetTimestamp();double seconds=(now-previousTick)/(double)System.Diagnostics.Stopwatch.Frequency*speed;using(simulationSample.Auto())lastSteps=session==null?world.Advance(seconds):session.Advance(seconds);previousTick=now;
            if(session!=null&&session.City.development.enabled&&(shownBuildings!=session.Entities.BuildingRevision||shownZones!=session.City.zoning.revision||shownNetwork!=world.NetworkGeneration)){RenderBuildings();RenderZones();shownBuildings=session.Entities.BuildingRevision;shownZones=session.City.zoning.revision;shownNetwork=world.NetworkGeneration;}
            using(viewsSample.Auto())
            {
            visibleIds.Clear();foreach(var v in world.Vehicles)
            {
                visibleIds.Add(v.id);
                Transform car;if(!cars.TryGetValue(v.id,out car)){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Freight "+v.id;go.GetComponent<Renderer>().sharedMaterial=carMaterial;go.transform.localScale=new Vector3(.65f,.5f,1);cars.Add(v.id,car=go.transform);}
                car.gameObject.SetActive(v.status!=VehicleStatus.Delivered);car.position=V(world.Position(v))+Vector3.up*.4f;
                if(v.status==VehicleStatus.Active){var d=world.Network.Get(v.Current);var direction=V(d.Geometry.Sample(v.distance+.1))-V(d.Geometry.Sample(Math.Max(0,v.distance-.1)));if(direction.sqrMagnitude>1e-8)car.rotation=Quaternion.LookRotation(direction);}
            }
            expiredIds.Clear();foreach(var pair in cars)if(!visibleIds.Contains(pair.Key))expiredIds.Add(pair.Key);foreach(int id in expiredIds){Destroy(cars[id].gameObject);cars.Remove(id);}
            }
            if(constructionCamera!=null)constructionCamera.Step(Time.deltaTime);
            if(Keyboard.current!=null)
            {
                var k=Keyboard.current;if(constructionCamera==null){Vector3 move=new Vector3((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),0,(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));cameraView.transform.position+=move*Time.deltaTime*35;}
                if(k.escapeKey.wasPressedThisFrame){building=false;hasStart=false;}
            }
            if((!building&&zoneTool==LandUse.Empty)||Mouse.current==null||!Mouse.current.leftButton.wasPressedThisFrame||Mouse.current.position.ReadValue().y>Screen.height-300)return;
            var ray=cameraView.ScreenPointToRay(Mouse.current.position.ReadValue());Vector3 hit;if(landscape!=null){if(!landscape.Raycast(ray,out hit))return;}else{float enter;if(!new Plane(Vector3.up,Vector3.zero).Raycast(ray,out enter))return;hit=ray.GetPoint(enter);}var p=new Point(hit.x,hit.y,hit.z);
            if(zoneTool!=LandUse.Empty){var selection=session.Simulation.Growth.Zoning.Cells().Where(c=>(c.pose.x-hit.x)*(c.pose.x-hit.x)+(c.pose.z-hit.z)*(c.pose.z-hit.z)<=9).ToArray();int changed=session.Simulation.Growth.Zoning.Paint(selection,zoneTool==LandUse.Bulldoze?LandUse.Empty:zoneTool);notice="已修改 "+changed+" 格土地用途；私人建筑由城市时钟逐步生长。";RenderZones();return;}
            if(!hasStart){startPoint=p;hasStart=true;notice="再点击终点；吸附道路内部或端点，同高度交叉自动建路口。Esc 取消。";return;}
            if(Point.Distance(startPoint,p)<4)return;
            try{var centre=new Point[9];for(int i=0;i<centre.Length;i++){centre[i]=Point.Lerp(startPoint,p,i/8.0);if(landscape!=null)centre[i].y=landscape.Height((float)centre[i].x,(float)centre[i].z);}var plan=world.PlanConnectedRoad(centre,oneWay,snap:4,lanes:wide?2:1);int fee=0;if(!(session==null?plan.TryCommit():session.Construction.Commit(plan,out fee)))throw new InvalidOperationException("城市已变化，请重新施工");RenderRoads();notice="已提交道路与引用；建设费用 "+fee;hasStart=false;}
            catch(Exception e){notice=e.Message;}
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
            foreach(var go in roads){Destroy(go.GetComponent<MeshFilter>().sharedMesh);Destroy(go);}roads.Clear();
            foreach(var d in world.Network.Drivables)
            {
                var g=new Geometry(d.Geometry.Slice(d.StartDistance,d.EndDistance));if(g.Length<1e-8)continue;var vertices=new Vector3[g.Count*2];var indices=new int[(g.Count-1)*6];
                for(int i=0;i<g.Count;i++){var t=Network.Tangent(g[Math.Max(0,i-1)],g[Math.Min(g.Count-1,i+1)]);vertices[i*2]=V(Network.Offset(g[i],t,-.5));vertices[i*2+1]=V(Network.Offset(g[i],t,.5));if(i==0)continue;int b=(i-1)*6,a=(i-1)*2;indices[b]=a;indices[b+1]=a+2;indices[b+2]=a+1;indices[b+3]=a+1;indices[b+4]=a+2;indices[b+5]=a+3;}
                var mesh=new Mesh{name="Shared lane geometry "+d.Id};mesh.vertices=vertices;mesh.triangles=indices;mesh.RecalculateNormals();
                var go=new GameObject(mesh.name);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=roadMaterial;roads.Add(go);
            }
        }
        void RenderBuildings()
        {
            foreach(var go in buildings)Destroy(go);buildings.Clear();if(session==null)return;
            foreach(var building in session.City.buildings){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=building.Use+" #"+building.id;go.transform.position=new Vector3(building.x,building.groundY+1.5f,building.z);go.transform.rotation=Quaternion.Euler(0,building.yaw,0);go.transform.localScale=new Vector3(building.width,3,building.depth);go.GetComponent<Renderer>().sharedMaterial=roadMaterial;buildings.Add(go);}
        }
        void RenderZones()
        {
            foreach(var go in zones)Destroy(go);zones.Clear();if(session==null||!session.Simulation.Growth.Zoning.Ready)return;
            if(zoneMaterials==null){zoneMaterials=new Material[4];var colours=new[]{new Color(.35f,.5f,.4f),new Color(.25f,.8f,.4f),new Color(.25f,.65f,1),new Color(1,.7f,.2f)};for(int i=0;i<4;i++)zoneMaterials[i]=new Material(roadMaterial){color=colours[i]};}
            foreach(var cell in session.Simulation.Growth.Zoning.Cells()){if(cell.occupiedBuilding>=0)continue;var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Zone "+cell.key;go.transform.position=new Vector3(cell.pose.x,cell.pose.groundY+.03f,cell.pose.z);go.transform.rotation=Quaternion.Euler(0,cell.pose.yaw,0);go.transform.localScale=new Vector3(cell.pose.width,.04f,cell.pose.depth);go.GetComponent<Renderer>().sharedMaterial=zoneMaterials[cell.use==LandUse.Residential?1:cell.use==LandUse.Commercial?2:cell.use==LandUse.Industrial?3:0];Destroy(go.GetComponent<Collider>());zones.Add(go);}
        }
        void OnGUI()
        {
            if(world==null)return;GUI.Box(new Rect(8,8,Math.Min(Screen.width-16,1100),235),"");GUILayout.BeginArea(new Rect(20,16,Math.Min(Screen.width-40,1080),220));
            GUILayout.Label("HarborCity 城市恢复开发中（尚未恢复完整经营）");GUILayout.Label(notice);
            if(session!=null&&session.City.development.enabled){GUILayout.BeginHorizontal();foreach(var use in new[]{LandUse.Residential,LandUse.Commercial,LandUse.Industrial})if(GUILayout.Button(use==LandUse.Residential?"划住宅区":use==LandUse.Commercial?"划商业区":"划工业区")){zoneTool=use;building=false;hasStart=false;}if(GUILayout.Button("清除分区")){zoneTool=LandUse.Bulldoze;building=false;}if(GUILayout.Button("结束划区"))zoneTool=LandUse.Empty;GUILayout.EndHorizontal();GUILayout.Label("城市资金 "+session.City.money+" · 实际居民 "+session.Entities.Members.Count+" · 真实岗位 "+session.City.society.jobEntities.Count+" · 建设项目 "+session.City.development.projects.Count);}
            GUILayout.Label("活动车辆 "+world.ActiveCount+" · 交付 "+world.Delivered+" · 道路 "+world.Network.RoadCount+" · 拓扑节点 "+world.Network.JunctionCount+" · 模拟秒 "+world.Clock.ToString("F1"));
            GUILayout.BeginHorizontal();if(GUILayout.Button(building?"结束修路":"修路")){building=!building;zoneTool=LandUse.Empty;hasStart=false;}oneWay=GUILayout.Toggle(oneWay,"单行");wide=GUILayout.Toggle(wide,"每方向两车道");if((session==null||!session.City.development.enabled)&&GUILayout.Button("发货 A → B"))notice=(session==null?world.Dispatch(origin,destination,1,5):session.Simulation.RequestFreight(origin,destination))==null?"暂无可用入口/库存/路线":"已发车，库存已扣；到达后才付款";if(GUILayout.Button("1×"))speed=1;if(GUILayout.Button("3×"))speed=3;if(GUILayout.Button("暂停"))speed=0;
            GUILayout.EndHorizontal();GUILayout.BeginHorizontal();
            if(GUILayout.Button("升级第一路")){try{var settings=world.Network.Settings(1);var plan=world.PlanUpgradeRoad(1,wide?2:1,12,settings.direction);int fee;if(!(session==null?plan.TryCommit():session.Construction.Commit(plan,out fee)))throw new InvalidOperationException("城市已变化，请重试");RenderRoads();notice="已升级真实车道/速度与引用";}catch(Exception e){notice=e.Message;}}
            if(GUILayout.Button("第一路改为/反向单行")){try{var settings=world.Network.Settings(1);var direction=settings.direction==RoadDirection.Forward?RoadDirection.Backward:RoadDirection.Forward;var plan=world.PlanUpgradeRoad(1,settings.lanes,settings.speed,direction);int fee;if(!(session==null?plan.TryCommit():session.Construction.Commit(plan,out fee)))throw new InvalidOperationException("城市已变化，请重试");RenderRoads();notice="已改变通行方向；物理道路起终身份不变";}catch(Exception e){notice=e.Message;}}
            if(GUILayout.Button("拆分第一路中点")){try{world.SplitRoadAt(1,world.Network.Centreline(1).Length/2);RenderRoads();notice="已拆分并迁移活动车辆";}catch(Exception e){notice=e.Message;}}
            if(GUILayout.Button("拆除第一路")){try{if(session==null)world.RemoveRoad(1);else session.Construction.RemoveRoad(1);RenderRoads();notice="受影响运输保留为中断，不传送、不丢货";}catch(Exception e){notice=e.Message;}}
            if(GUILayout.Button("保存"))Save();if(GUILayout.Button("读取"))Load();GUILayout.EndHorizontal();
            foreach(var d in world.Depots)GUILayout.Label("仓库 #"+d.id+" 库存 "+d.stock+" / "+d.capacity+" 现金 "+d.cash.ToString("F0"));GUILayout.Label("WASD移动相机；点击两点修路，Esc取消。新存档："+SavePath);GUILayout.EndArea();
            if(session!=null&&!session.City.development.enabled){GUI.Box(new Rect(8,250,620,90),"");GUI.Label(new Rect(20,258,600,22),"工厂：原料 "+session.City.Factory(1)?.raw+" 成品 "+session.City.Goods(1)+" 累计生产 "+session.City.Factory(1)?.produced+" 销售 "+session.City.Factory(1)?.sold+" 现金 "+session.City.Factory(1)?.cash.ToString("F2"));GUI.Label(new Rect(20,285,600,22),"商店库存 "+session.City.Goods(2)+" · 原居民 "+session.City.Citizens.Count()+" · 工厂岗位 "+session.City.JobCapacity(1));}
        }
        void Save(){try{string tmp=SavePath+".tmp";File.WriteAllBytes(tmp,session==null?SaveCodec.Write(world):CityCoreSave.Write(session));if(File.Exists(SavePath))File.Replace(tmp,SavePath,SavePath+".bak");else File.Move(tmp,SavePath);notice="新格式已保存；旧城市文件未改动";}catch(Exception e){notice=e.Message;}}
        void Load(){try{var bytes=File.ReadAllBytes(SavePath);var replacement=session==null?SaveCodec.Read(bytes):null;var restored=session==null?null:CityCoreSave.Read(bytes);foreach(var car in cars.Values)Destroy(car.gameObject);cars.Clear();session=restored;world=restored?.Transport??replacement;if(landscape!=null&&session!=null)session.Simulation.Growth.Zoning.SetSurface(landscape.Height);shownBuildings=shownZones=shownNetwork=-1;RenderRoads();RenderBuildings();RenderZones();notice="新格式已加载";}catch(Exception e){notice="读取失败："+e.Message;}}
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
                speed=0;foreach(var car in cars.Values)Destroy(car.gameObject);cars.Clear();world=CityRoadCoreFixtures.Create(count,queued);RenderRoads();
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
                if(session.City.Factory(1).sold==0||session.City.Factory(1).sold!=world.Delivered||session.City.Goods(2)!=world.Delivered||session.City.roads.edges.Count!=0||session.City.traffic.trips.Count!=0)throw new Exception("Business loop/legacy engine invariant failed");
                var repair=CityCoreIndustrySession.Create();repair.Advance(.05);var commuter=repair.Transport.Vehicles.First();Point strandedAt=repair.Transport.Position(commuter);repair.Transport.RemoveRoad(1);repair.Transport.AddRoad(1,2,new[]{repair.Transport.JunctionPosition(1),new Point(-18,0,0),repair.Transport.JunctionPosition(2)},lanes:2);repair.Advance(.05);if(commuter.status!=VehicleStatus.Active||Point.Distance(strandedAt,repair.Transport.Position(commuter))>.7)throw new Exception("Native repaired commute failed / jumped");repair.Advance(6);repair.Validate();
                File.WriteAllText(Path.Combine(path,"IndustryResult.txt"),"PASS actual Unity default city industry including actual resident commuting, real truck lane change and arbitrary split during maneuver: original residents/jobs credit labour, original factory produces, RoadCore trucks move, original shop receives/payments reconcile, exact combined save continues. No old Move or duplicate Depot.\n");
            }
            catch(Exception e){File.WriteAllText(Path.Combine(path,"IndustryResult.txt"),"FAIL "+e);Application.Quit(1);yield break;}
            yield return null;
            var visible=world.Vehicles.FirstOrDefault();Transform rendered;
            if(visible==null||!cars.TryGetValue(visible.id,out rendered)){File.WriteAllText(Path.Combine(path,"IndustryResult.txt"),"FAIL no actual vehicle view");Application.Quit(1);yield break;}
            Vector3 previous=rendered.position;session.Advance(.1);yield return null;
            if(Vector3.Distance(previous,rendered.position)<.5f||Vector3.Distance(rendered.position,V(world.Position(visible))+Vector3.up*.4f)>.001f){File.WriteAllText(Path.Combine(path,"IndustryResult.txt"),"FAIL vehicle view did not track actual movement");Application.Quit(1);yield break;}
            File.AppendAllText(Path.Combine(path,"IndustryResult.txt"),"PASS rendered truck transform follows new-core state across actual Unity frames.\n");
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(path,"Industry.png"));yield return null;Application.Quit(0);
        }
        System.Collections.IEnumerator ValidateConstruction(string path)
        {
            Directory.CreateDirectory(path);speed=0;yield return null;
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
                for(int i=0;i<320;i++){session.Advance(.05);restore.Advance(.05);}session.Validate();restore.Validate();if(!session.City.buildings.Any(b=>b.Use==LandUse.Residential)||!session.City.buildings.Any(b=>b.Use==LandUse.Commercial)||!session.City.buildings.Any(b=>b.Use==LandUse.Industrial))throw new Exception("All three uses did not grow");if(Convert.ToBase64String(CityCoreSave.Write(session))!=Convert.ToBase64String(CityCoreSave.Write(restore)))throw new Exception("Saved construction did not continue identically");if(session.Entities.Members.Count!=0||session.City.roads.edges.Count!=0||session.City.traffic.trips.Count!=0)throw new Exception("Invented population / old runtime network activated");
                RenderBuildings();RenderZones();File.WriteAllText(Path.Combine(path,"ConstructionResult.txt"),"PASS actual Unity default blank terrain, two mouse clicks through production terrain road tool and paid RoadCore construction, residential/commercial/industrial zoning, timed private building projects via CitySimulation/CityEntities, 3D foundations/entrances, unfinished project save and exact continuation. No old traffic/network, no invented residents. Immigration, supply/consumption/services/economy management are not restored yet.\n");
            }
            catch(Exception e){File.WriteAllText(Path.Combine(path,"ConstructionResult.txt"),"FAIL "+e);Application.Quit(1);yield break;}
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(path,"Construction.png"));yield return null;Application.Quit(0);
        }
        void OnDestroy(){foreach(var go in roads){if(go!=null){var filter=go.GetComponent<MeshFilter>();if(filter!=null)Destroy(filter.sharedMesh);Destroy(go);}}foreach(var go in zones)if(go!=null)Destroy(go);if(zoneMaterials!=null)foreach(var m in zoneMaterials)Destroy(m);if(roadMaterial!=null)Destroy(roadMaterial);if(carMaterial!=null)Destroy(carMaterial);}
    }
}
