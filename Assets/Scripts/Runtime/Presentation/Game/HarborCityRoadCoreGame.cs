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
        readonly Dictionary<int,Transform> cars=new Dictionary<int,Transform>();
        readonly HashSet<int> visibleIds=new HashSet<int>();readonly List<int> expiredIds=new List<int>();
        readonly List<GameObject> roads=new List<GameObject>();
        readonly List<GameObject> buildings=new List<GameObject>();
        bool hasStart;Point startPoint;bool building,oneWay,wide;int origin,destination;float speed=1;
        long previousTick;int lastSteps;
        string notice="原工厂劳动→生产→新交通实车运输→原商店库存/支付。居民实际通勤已接入；市政、分区、信号灯和行人尚未接回。";
        string SavePath=>Path.Combine(Application.persistentDataPath,session==null?"harbor-road-core-v7.hcrc":"harbor-city-core-v4.hcity");
        static Vector3 V(Point p)=>new Vector3((float)p.x,(float)p.y,(float)p.z);
        void Start()
        {
            foreach(var c in FindObjectsByType<Camera>(FindObjectsSortMode.None))c.gameObject.SetActive(false);
            cameraView=new GameObject("Road core camera").AddComponent<Camera>();cameraView.transform.position=new Vector3(0,90,-65);cameraView.transform.LookAt(Vector3.zero);cameraView.backgroundColor=new Color(.12f,.18f,.16f);cameraView.farClipPlane=3000;
            var shader=Resources.Load<Shader>("RoadCore");roadMaterial=new Material(shader){color=new Color(.25f,.29f,.32f)};carMaterial=new Material(shader){color=Color.yellow};
            var args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"-harbor-core-validate")>=0||Array.IndexOf(args,"-harbor-core-window")>=0||Array.IndexOf(args,"-harbor-city-core-validate")>=0)Application.runInBackground=true;
            if(Array.IndexOf(args,"-harbor-core-validate")>=0){world=new World();int a=world.AddJunction(new Point(-40,0,-15)),b=world.AddJunction(new Point(0,0,-15)),cnode=world.AddJunction(new Point(35,0,20));world.AddRoad(a,b,new[]{world.JunctionPosition(a),new Point(-20,0,-15),world.JunctionPosition(b)});world.AddRoad(b,cnode,new[]{world.JunctionPosition(b),new Point(15,0,-5),world.JunctionPosition(cnode)});origin=world.AddDepot(a,1000);destination=world.AddDepot(cnode,0);}
            else {session=CityCoreIndustrySession.Create();world=session.Transport;origin=1;destination=2;RenderBuildings();}
            RenderRoads();
            previousTick=System.Diagnostics.Stopwatch.GetTimestamp();
            int flag=Array.IndexOf(args,"-harbor-core-validate");if(flag>=0&&flag+1<args.Length)StartCoroutine(ValidateNative(args[flag+1]));
            flag=Array.IndexOf(args,"-harbor-core-window");if(flag>=0&&flag+1<args.Length)StartCoroutine(MeasureWindow(args[flag+1]));
            flag=Array.IndexOf(args,"-harbor-city-core-validate");if(flag>=0&&flag+1<args.Length)StartCoroutine(ValidateIndustryNative(args[flag+1]));
        }
        void Update()
        {
            if(world==null)return;long now=System.Diagnostics.Stopwatch.GetTimestamp();double seconds=(now-previousTick)/(double)System.Diagnostics.Stopwatch.Frequency*speed;using(simulationSample.Auto())lastSteps=session==null?world.Advance(seconds):session.Advance(seconds);previousTick=now;
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
            if(Keyboard.current!=null)
            {
                var k=Keyboard.current;Vector3 move=new Vector3((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),0,(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));cameraView.transform.position+=move*Time.deltaTime*35;
                if(k.escapeKey.wasPressedThisFrame){building=false;hasStart=false;}
            }
            if(!building||Mouse.current==null||!Mouse.current.leftButton.wasPressedThisFrame||Mouse.current.position.ReadValue().y>Screen.height-200)return;
            var ray=cameraView.ScreenPointToRay(Mouse.current.position.ReadValue());float enter;if(!new Plane(Vector3.up,Vector3.zero).Raycast(ray,out enter))return;var hit=ray.GetPoint(enter);var p=new Point(hit.x,0,hit.z);
            if(!hasStart){startPoint=p;hasStart=true;notice="再点击终点；吸附道路内部或端点，同高度交叉自动建路口。Esc 取消。";return;}
            if(Point.Distance(startPoint,p)<4)return;
            try{var plan=world.PlanConnectedRoad(new[]{startPoint,Point.Lerp(startPoint,p,.5),p},oneWay,snap:4,lanes:wide?2:1);if(!plan.TryCommit())throw new InvalidOperationException("城市已变化，请重新施工");RenderRoads();notice="已原子提交道路、路口和车辆入口迁移。";hasStart=false;}
            catch(Exception e){notice=e.Message;}
        }
        void RenderRoads()
        {
            foreach(var go in roads){Destroy(go.GetComponent<MeshFilter>().sharedMesh);Destroy(go);}roads.Clear();
            foreach(var d in world.Network.Drivables)
            {
                var g=d.Geometry;if(g.Length<1e-8)continue;var vertices=new Vector3[g.Count*2];var indices=new int[(g.Count-1)*6];
                for(int i=0;i<g.Count;i++){var t=Network.Tangent(g[Math.Max(0,i-1)],g[Math.Min(g.Count-1,i+1)]);vertices[i*2]=V(Network.Offset(g[i],t,-.5));vertices[i*2+1]=V(Network.Offset(g[i],t,.5));if(i==0)continue;int b=(i-1)*6,a=(i-1)*2;indices[b]=a;indices[b+1]=a+2;indices[b+2]=a+1;indices[b+3]=a+1;indices[b+4]=a+2;indices[b+5]=a+3;}
                var mesh=new Mesh{name="Shared lane geometry "+d.Id};mesh.vertices=vertices;mesh.triangles=indices;mesh.RecalculateNormals();
                var go=new GameObject(mesh.name);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=roadMaterial;roads.Add(go);
            }
        }
        void RenderBuildings()
        {
            foreach(var go in buildings)Destroy(go);buildings.Clear();if(session==null)return;
            foreach(var building in session.City.buildings){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=building.Use+" #"+building.id;go.transform.position=new Vector3(building.x,1.5f,building.z);go.transform.localScale=new Vector3(5,3,5);go.GetComponent<Renderer>().sharedMaterial=roadMaterial;buildings.Add(go);}
        }
        void OnGUI()
        {
            if(world==null)return;GUI.Box(new Rect(8,8,Math.Min(Screen.width-16,1100),195),"");GUILayout.BeginArea(new Rect(20,16,Math.Min(Screen.width-40,1080),180));
            GUILayout.Label("HarborCity 原工业/商业接入新交通（完整城市仍在迁移）");GUILayout.Label(notice);
            GUILayout.Label("活动车辆 "+world.ActiveCount+" · 交付 "+world.Delivered+" · 道路 "+world.Network.RoadCount+" · 拓扑节点 "+world.Network.JunctionCount+" · 模拟秒 "+world.Clock.ToString("F1"));
            GUILayout.BeginHorizontal();if(GUILayout.Button(building?"结束修路":"修路")){building=!building;hasStart=false;}oneWay=GUILayout.Toggle(oneWay,"单行");wide=GUILayout.Toggle(wide,"每方向两车道");if(GUILayout.Button("发货 A → B"))notice=world.Dispatch(origin,destination,1,session==null?5:FactoryState.SalePrice)==null?"暂无可用入口/库存/路线":"已发车，库存已扣；到达后才付款";if(GUILayout.Button("1×"))speed=1;if(GUILayout.Button("3×"))speed=3;if(GUILayout.Button("暂停"))speed=0;
            if(GUILayout.Button("拆分第一路中点")){try{world.SplitRoad(1,1);RenderRoads();notice="已拆分并迁移活动车辆";}catch(Exception e){notice=e.Message;}}
            if(GUILayout.Button("拆除第一路")){try{world.RemoveRoad(1);RenderRoads();notice="受影响运输保留为中断，不传送、不丢货";}catch(Exception e){notice=e.Message;}}
            if(GUILayout.Button("保存"))Save();if(GUILayout.Button("读取"))Load();GUILayout.EndHorizontal();
            foreach(var d in world.Depots)GUILayout.Label("仓库 #"+d.id+" 库存 "+d.stock+" / "+d.capacity+" 现金 "+d.cash.ToString("F0"));GUILayout.Label("WASD移动相机；点击两点修路，Esc取消。新存档："+SavePath);GUILayout.EndArea();
            if(session!=null){GUI.Box(new Rect(8,210,620,90),"");GUI.Label(new Rect(20,218,600,22),"工厂：原料 "+session.City.Factory(1)?.raw+" 成品 "+session.City.Goods(1)+" 累计生产 "+session.City.Factory(1)?.produced+" 销售 "+session.City.Factory(1)?.sold+" 现金 "+session.City.Factory(1)?.cash.ToString("F2"));GUI.Label(new Rect(20,245,600,22),"商店库存 "+session.City.Goods(2)+" · 原居民 "+session.City.Citizens.Count()+" · 工厂岗位 "+session.City.JobCapacity(1));}
        }
        void Save(){try{string tmp=SavePath+".tmp";File.WriteAllBytes(tmp,session==null?SaveCodec.Write(world):CityCoreSave.Write(session));if(File.Exists(SavePath))File.Replace(tmp,SavePath,SavePath+".bak");else File.Move(tmp,SavePath);notice="新格式已保存；旧城市文件未改动";}catch(Exception e){notice=e.Message;}}
        void Load(){try{var bytes=File.ReadAllBytes(SavePath);var replacement=session==null?SaveCodec.Read(bytes):null;var restored=session==null?null:CityCoreSave.Read(bytes);foreach(var car in cars.Values)Destroy(car.gameObject);cars.Clear();session=restored;world=restored?.Transport??replacement;RenderRoads();RenderBuildings();notice="新格式已加载";}catch(Exception e){notice="读取失败："+e.Message;}}
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
                int branch=world.AddJunction(new Point(0,0,25));world.AddRoad(2,branch,new[]{world.JunctionPosition(2),world.JunctionPosition(branch)});if(session.City.Citizens.Any(p=>p.atWork))throw new Exception("Default residents started onsite");for(int wait=0;wait<100&&!world.Vehicles.Any(v=>v.purpose==TransportPurpose.Freight);wait++)session.Advance(.05);var actor=world.Vehicles.First(v=>v.purpose==TransportPurpose.Freight);if(!world.TryChangeLane(actor.id,1))throw new Exception("Actual industry truck could not start lane change");Point first=world.Position(actor);session.Advance(.5);if(Point.Distance(first,world.Position(actor))<1)throw new Exception("Truck did not physically move");
                Point beforeEdit=world.Position(actor);world.SplitRoadAt(1,12.75);world.SplitRoadAt(2,20.25);var widePlan=world.PlanConnectedRoad(new[]{new Point(-35,0,30),new Point(35,0,30)},lanes:2);if(!widePlan.TryCommit())throw new Exception("Wide road transaction failed");RenderRoads();if(Point.Distance(beforeEdit,world.Position(actor))>1e-8)throw new Exception("Access edit moved truck");
                var restored=CityCoreSave.Read(CityCoreSave.Write(session));for(int i=0;i<300;i++){session.Advance(.05);restored.Advance(.05);session.Validate();}
                if(Convert.ToBase64String(CityCoreSave.Write(session))!=Convert.ToBase64String(CityCoreSave.Write(restored)))throw new Exception("Integrated save differs");
                if(session.City.Factory(1).sold==0||session.City.Factory(1).sold!=world.Delivered||session.City.Goods(2)!=world.Delivered||session.City.roads.edges.Count!=0||session.City.traffic.trips.Count!=0)throw new Exception("Business loop/legacy engine invariant failed");
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
        void OnDestroy(){foreach(var go in roads){if(go!=null){var filter=go.GetComponent<MeshFilter>();if(filter!=null)Destroy(filter.sharedMesh);Destroy(go);}}if(roadMaterial!=null)Destroy(roadMaterial);if(carMaterial!=null)Destroy(carMaterial);}
    }
}
