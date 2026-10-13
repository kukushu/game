using System;
using System.Collections.Generic;
using System.IO;
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
        World world; Camera cameraView; Material roadMaterial,carMaterial;
        readonly Dictionary<int,Transform> cars=new Dictionary<int,Transform>();
        readonly HashSet<int> visibleIds=new HashSet<int>();readonly List<int> expiredIds=new List<int>();
        readonly List<GameObject> roads=new List<GameObject>();
        int startJunction;Point startPoint;bool building,oneWay;int origin,destination;float speed=1;
        long previousTick;int lastSteps;
        string notice="重构第一批：道路/仓库货运闭环。居民、市政、分区、换道、行人、桥隧工具尚未接回。";
        string SavePath=>Path.Combine(Application.persistentDataPath,"harbor-road-core-v1.hcrc");
        static Vector3 V(Point p)=>new Vector3((float)p.x,(float)p.y,(float)p.z);
        void Start()
        {
            foreach(var c in FindObjectsByType<Camera>(FindObjectsSortMode.None))c.gameObject.SetActive(false);
            cameraView=new GameObject("Road core camera").AddComponent<Camera>();cameraView.transform.position=new Vector3(0,90,-65);cameraView.transform.LookAt(Vector3.zero);cameraView.backgroundColor=new Color(.12f,.18f,.16f);cameraView.farClipPlane=3000;
            var shader=Resources.Load<Shader>("RoadCore");roadMaterial=new Material(shader){color=new Color(.25f,.29f,.32f)};carMaterial=new Material(shader){color=Color.yellow};
            world=new World();int a=world.AddJunction(new Point(-40,0,-15)),b=world.AddJunction(new Point(0,0,-15)),cnode=world.AddJunction(new Point(35,0,20));
            world.AddRoad(a,b,new[]{world.JunctionPosition(a),new Point(-20,0,-15),world.JunctionPosition(b)});
            world.AddRoad(b,cnode,new[]{world.JunctionPosition(b),new Point(15,0,-5),world.JunctionPosition(cnode)});
            origin=world.AddDepot(a,1000);destination=world.AddDepot(cnode,0);RenderRoads();
            previousTick=System.Diagnostics.Stopwatch.GetTimestamp();
            var args=Environment.GetCommandLineArgs();int flag=Array.IndexOf(args,"-harbor-core-validate");if(flag>=0&&flag+1<args.Length)StartCoroutine(ValidateNative(args[flag+1]));
            flag=Array.IndexOf(args,"-harbor-core-window");if(flag>=0&&flag+1<args.Length)StartCoroutine(MeasureWindow(args[flag+1]));
        }
        void Update()
        {
            if(world==null)return;long now=System.Diagnostics.Stopwatch.GetTimestamp();using(simulationSample.Auto())lastSteps=world.Advance((now-previousTick)/(double)System.Diagnostics.Stopwatch.Frequency*speed);previousTick=now;
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
                if(k.escapeKey.wasPressedThisFrame){building=false;startJunction=0;}
            }
            if(!building||Mouse.current==null||!Mouse.current.leftButton.wasPressedThisFrame||Mouse.current.position.ReadValue().y>Screen.height-200)return;
            var ray=cameraView.ScreenPointToRay(Mouse.current.position.ReadValue());float enter;if(!new Plane(Vector3.up,Vector3.zero).Raycast(ray,out enter))return;var hit=ray.GetPoint(enter);var p=new Point(hit.x,0,hit.z);
            int snap=world.SnapJunction(p,4);
            if(startJunction==0){startJunction=snap!=0?snap:world.AddJunction(p);startPoint=world.JunctionPosition(startJunction);notice="再点击终点（4单位内吸附端点）；Esc 取消。";return;}
            if(snap==startJunction||Point.Distance(startPoint,p)<4)return;
            int end=snap!=0?snap:world.AddJunction(p);var q=world.JunctionPosition(end);
            try{world.AddRoad(startJunction,end,new[]{startPoint,Point.Lerp(startPoint,q,.5),q},oneWay);RenderRoads();notice="已建路：几何中点不进入寻路拓扑。当前工具仅吸附端点；内部交叉需要显式拆分。";startJunction=0;}
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
        void OnGUI()
        {
            if(world==null)return;GUI.Box(new Rect(8,8,Math.Min(Screen.width-16,1100),195),"");GUILayout.BeginArea(new Rect(20,16,Math.Min(Screen.width-40,1080),180));
            GUILayout.Label("HarborCity 道路新内核 · 第一批运行入口（非完整城市版本）");GUILayout.Label(notice);
            GUILayout.Label("活动车辆 "+world.ActiveCount+" · 交付 "+world.Delivered+" · 道路 "+world.Network.RoadCount+" · 拓扑节点 "+world.Network.JunctionCount+" · 模拟秒 "+world.Clock.ToString("F1"));
            GUILayout.BeginHorizontal();if(GUILayout.Button(building?"结束修路":"修路")){building=!building;startJunction=0;}oneWay=GUILayout.Toggle(oneWay,"单行");if(GUILayout.Button("发货 A → B"))notice=world.Dispatch(origin,destination)==null?"暂无可用入口/库存/路线":"已发车，库存已扣；到达后才付款";if(GUILayout.Button("1×"))speed=1;if(GUILayout.Button("3×"))speed=3;if(GUILayout.Button("暂停"))speed=0;
            if(GUILayout.Button("拆分第一路中点")){try{world.SplitRoad(1,1);RenderRoads();notice="已拆分并迁移活动车辆";}catch(Exception e){notice=e.Message;}}
            if(GUILayout.Button("拆除第一路")){try{world.RemoveRoad(1);RenderRoads();notice="受影响运输保留为中断，不传送、不丢货";}catch(Exception e){notice=e.Message;}}
            if(GUILayout.Button("保存"))Save();if(GUILayout.Button("读取"))Load();GUILayout.EndHorizontal();
            foreach(var d in world.Depots)GUILayout.Label("仓库 #"+d.id+" 库存 "+d.stock+" / "+d.capacity+" 现金 "+d.cash.ToString("F0"));GUILayout.Label("WASD移动相机；点击两点修路，Esc取消。新存档："+SavePath);GUILayout.EndArea();
        }
        void Save(){try{string tmp=SavePath+".tmp";File.WriteAllBytes(tmp,SaveCodec.Write(world));if(File.Exists(SavePath))File.Replace(tmp,SavePath,SavePath+".bak");else File.Move(tmp,SavePath);notice="新格式已保存；旧城市文件未改动";}catch(Exception e){notice=e.Message;}}
        void Load(){try{var replacement=SaveCodec.Read(File.ReadAllBytes(SavePath));foreach(var car in cars.Values)Destroy(car.gameObject);cars.Clear();world=replacement;RenderRoads();notice="新格式已加载";}catch(Exception e){notice="读取失败："+e.Message;}}
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
            Directory.CreateDirectory(path);QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
            var rows=new List<string>{"vehicles,scenario,speed,frames,fps,mean_frame_ms,p95_frame_ms,p99_frame_ms,actual_sim_rate,gc_alloc_bytes_per_frame,gc_collections,steps_per_frame_max"};
            foreach(int count in new[]{1000,3000})foreach(bool queued in new[]{false,true})foreach(int multiplier in new[]{1,3})
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
        void OnDestroy(){foreach(var go in roads){if(go!=null){var filter=go.GetComponent<MeshFilter>();if(filter!=null)Destroy(filter.sharedMesh);Destroy(go);}}if(roadMaterial!=null)Destroy(roadMaterial);if(carMaterial!=null)Destroy(carMaterial);}
    }
}
