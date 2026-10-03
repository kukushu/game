using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace HarborCity
{
    public sealed partial class HarborCityGame : MonoBehaviour
    {
        const float Cell = 3f;
        const float MinZoom = 10f;
        const float MaxZoom = 155f;
        const float ZoomPerStep = .85f;
        CityModel city;
        readonly Dictionary<int, GameObject> visuals = new Dictionary<int, GameObject>();
        readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        readonly List<Transform> cars = new List<Transform>();
        readonly Dictionary<int, Transform> trafficViews = new Dictionary<int, Transform>();
        CityTraffic traffic;
        LineRenderer routeLine;
        int inspectedTrip = -1;
        readonly string[] names = { "选择", "道路", "住宅", "商业", "工业", "电站", "水塔", "公园", "拆除" };
        readonly Color[] palette = { new Color(.27f,.43f,.32f), new Color(.19f,.23f,.27f), new Color(.37f,.76f,.52f),
            new Color(.32f,.64f,.83f), new Color(.89f,.68f,.32f), new Color(.96f,.65f,.3f), new Color(.37f,.77f,.83f),
            new Color(.26f,.62f,.39f), new Color(.93f,.4f,.38f) };
        Camera cam;
        CityLandscape landscape;
        int cursorCell = -1;
        Transform world, cursor;
        Vector3 focus = new Vector3(0, 0, 0);
        float yaw = -32, pitch = 52, zoom = 48, timer, speed = 1;
        const float MinPitch = 20f;
        const float MaxPitch = 80f;
        int hoverX = -1, hoverZ = -1, lastPaint = -1;
        LandUse selected = LandUse.Road;
        string notice = "空白城市：从地图西侧的浅色连接点修路，再建设住房和工作场所。";
        GUIStyle label, title, small, button, number;
        Font font;
        bool help;
        bool draggingView;
        Vector2 previousDragPointer;
        bool rightGesture, rotatingView;
        Vector2 rightPressPointer, previousRotatePointer;
        const float RotationDegreesPerPixel = .25f;
        const float RotationDragThreshold = 5f;
        string SavePath => Path.Combine(Application.persistentDataPath, "harbor-city.json");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            if (FindAnyObjectByType<HarborCityGame>() == null) new GameObject("Harbor City").AddComponent<HarborCityGame>();
        }


        void Start()
        {
            // 1. 创建城市的数据模型
            // 这里一般会初始化：地图格子、资金、人口、建筑状态之类的数据
            city = CityModel.Create();

            // 2. 创建交通系统，并把刚才的 city 数据交给它
            // 交通系统后面会根据道路、建筑等信息生成车辆和路线
            traffic = new CityTraffic(city);


            // 3. 找到场景里现有的所有 Camera，并全部关闭
            // 因为这个游戏不打算使用 Unity 场景里原本的 Main Camera，
            // 而是下面自己创建一台相机
            foreach (var c in FindObjectsByType<Camera>())
                c.gameObject.SetActive(false);


            // 4. 代码动态创建一台新的相机
            // new GameObject("City Camera")：创建一个 GameObject
            // AddComponent<Camera>()：给它挂一个 Camera 组件
            cam = new GameObject("City Camera").AddComponent<Camera>();

            // 把它标记为 MainCamera
            cam.tag = "MainCamera";

            // 使用正交相机，而不是透视相机
            // 类似很多城市建设游戏那种“模型不会近大远小得特别明显”的视角
            cam.orthographic = true;

            // 相机最近和最远能看到多远
            cam.nearClipPlane = .1f;
            cam.farClipPlane = 500;

            // 背景使用纯色
            cam.clearFlags = CameraClearFlags.SolidColor;

            // 设置天空/背景颜色
            cam.backgroundColor = new Color(.66f, .79f, .81f);

            // 给相机挂 AudioListener
            // 可以理解成“玩家的耳朵”，用来接收场景中的声音
            cam.gameObject.AddComponent<AudioListener>();


            // 5. 找场景里有没有灯光
            var sun = FindAnyObjectByType<Light>();

            // 如果没有，就自己创建一个叫 Sun 的 GameObject，并挂 Light 组件
            if (sun == null)
                sun = new GameObject("Sun").AddComponent<Light>();

            // 设置成 Directional Light
            // 这种灯常用来模拟太阳光
            sun.type = LightType.Directional;

            // 设置光照强度
            sun.intensity = 1.8f;

            // 设置太阳照射方向
            sun.transform.rotation = Quaternion.Euler(50, -35, 0);

            // 开启柔和阴影
            sun.shadows = LightShadows.Soft;


            // 6. 设置整个场景的环境光
            // Flat 表示使用一个统一的环境光颜色
            RenderSettings.ambientMode = AmbientMode.Flat;

            // 设置环境光颜色
            RenderSettings.ambientLight = new Color(.66f, .73f, .79f);


            // 7. 创建一个叫 "City lots" 的 GameObject
            // 后面道路、住宅、商业、工业等地块，大概率都会挂在它下面
            world = new GameObject("City lots").transform;


            // 8. 创建一个 Landscape GameObject
            var terrainObject = new GameObject("Landscape");

            // 把 Landscape 挂到当前 HarborCityGame 这个 GameObject 下面
            terrainObject.transform.SetParent(transform, false);

            // 给 Landscape 添加 CityLandscape 组件
            landscape = terrainObject.AddComponent<CityLandscape>();

            // 调用 CityLandscape.Build()
            // 真正开始生成地形
            landscape.Build();


            // 9. 创建一个巨大的扁平 Cube，当作海面
            //
            // 位置：
            // y = -0.18，稍微低于陆地
            //
            // 尺寸：
            // 700 x 0.3 x 700
            //
            // 颜色：
            // 蓝绿色
            Box(
                "Sea",
                new Vector3(0, -.18f, 0),
                new Vector3(700, .3f, 700),
                new Color(.24f, .53f, .63f),
                transform
            );


            // 10. 创建一个固定随机数生成器
            // 14 是随机种子
            //
            // 好处是：
            // 每次运行时生成的树的位置都一样，
            // 不会每次启动游戏地形都变
            var random = new System.Random(14);


            // 11. 尝试随机生成 700 棵树
            for (int i = 0; i < 700; i++)
            {
                // 随机生成 x、z 坐标
                // 范围大约是 -95 ~ 95
                float x = (float)random.NextDouble() * 190 - 95;
                float z = (float)random.NextDouble() * 190 - 95;


                // 如果位置在城市中心区域，就跳过
                //
                // 也就是说：
                // 城市核心区域大约是 -56 ~ 56，
                // 树主要生成在城市外围
                if (Mathf.Abs(x) < 56 && Mathf.Abs(z) < 56)
                    continue;


                // 查询这个 x,z 坐标对应的地形高度
                float height = landscape.Height(x, z);


                // 太低的不种，可能接近海面
                //
                // 太高的不种，可能在山顶
                if (height < 1.5f || height > 23)
                    continue;


                // 真正生成树
                //
                // y 坐标直接使用地形高度，
                // 所以树会贴在地面上
                //
                // 最后一个参数是随机大小
                Tree(
                    new Vector3(x, height, z),
                    transform,
                    .9f + (float)random.NextDouble()
                );
            }


            // 12. 创建一个“放置预览块”
            //
            // 就是鼠标移动到地图上时，
            // 提示你“当前准备在这里放道路/住宅”的那个方块
            cursor = Box(
                "Placement",
                Vector3.zero,
                new Vector3(2.9f, .08f, 2.9f),
                palette[2],
                transform
            ).transform;


            // 13. 根据当前 city 数据，
            // 把所有已有地块重新画出来
            //
            // 里面最终会调用 DrawLot()
            Rebuild();


            // 14. 创建/准备交通车辆对应的 GameObject
            EnsureTrafficViews();


            // 15. 从系统字体中创建 Unity 字体
            //
            // 优先尝试：
            // Microsoft YaHei
            // SimHei
            // Arial
            //
            // 用于显示中文 UI
            font = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei", "SimHei", "Arial" },
                18
            );


            // 16. 根据当前 yaw、pitch、zoom、focus
            // 更新相机的位置和角度
            UpdateCamera();
            StartSimulationLog("新开局");
        }

        void OnEnable()
        {
            // Unity can reload scripts while a city is running. Rebind transient controllers.
            if (city == null || landscape == null) return;
            city.Recalculate();
            Rebuild();
            traffic = new CityTraffic(city);
            EnsureTrafficViews();
            StartSimulationLog("脚本重载或组件重新启用");
        }

        void EnsureTrafficViews()
        {
            if (cars.Count == 0)
                foreach (Transform child in transform)
                    if (child.name == "Traffic" || child.name.StartsWith("Vehicle ")) cars.Add(child);
            foreach (var car in cars) car.gameObject.SetActive(false);
            trafficViews.Clear();
            if (routeLine != null) return;
            routeLine = new GameObject("Selected vehicle route").AddComponent<LineRenderer>();
            routeLine.transform.SetParent(transform, false);
            routeLine.sharedMaterial = Mat(new Color(1,.85f,.18f));
            routeLine.startWidth = routeLine.endWidth = .16f;
            routeLine.shadowCastingMode = ShadowCastingMode.Off;
            routeLine.receiveShadows = false; routeLine.positionCount = 0;
        }

        Vector3 Position(int x, int z)
        {
            float px = (x - 17.5f) * Cell, pz = (z - 17.5f) * Cell;
            return new Vector3(px,landscape.Height(px,pz),pz);
        }

        Material Mat(Color color)
        {
            if (materials.TryGetValue(color, out var material)) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.color = color; material.SetFloat("_Smoothness", .12f);
            materials[color] = material; return material;
        }

        GameObject Box(string objectName, Vector3 position, Vector3 scale, Color color, Transform parent)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = objectName; obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position; obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = Mat(color);
            Destroy(obj.GetComponent<Collider>()); return obj;
        }

        void Tree(Vector3 position, Transform parent, float size = 1)
        {
            Box("Trunk", position + Vector3.up * .45f * size, new Vector3(.16f,.9f,.16f) * size, new Color(.38f,.29f,.2f), parent);
            var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = "Tree crown"; crown.transform.SetParent(parent, false);
            crown.transform.localPosition = position + Vector3.up * 1.25f * size;
            crown.transform.localScale = new Vector3(1.2f,1.7f,1.2f) * size;
            crown.GetComponent<Renderer>().sharedMaterial = Mat(new Color(.2f,.42f,.29f));
            Destroy(crown.GetComponent<Collider>());
        }

        void DrawLot(int i)
        {
            if (visuals.TryGetValue(i, out var old)) { old.SetActive(false); Destroy(old); visuals.Remove(i); }
            var use = (LandUse)city.tiles[i];
            if (use == LandUse.Empty) return;
            var root = new GameObject(names[(int)use]); root.transform.SetParent(world, false);
            var pose = city.buildings[i];
            root.transform.position = new Vector3(pose.x,landscape.Height(pose.x,pose.z),pose.z);
            root.transform.rotation = Quaternion.Euler(0,pose.yaw,0);
            visuals[i] = root;
            Transform t = root.transform;
            landscape.Surface("Lot",t,t.position,pose.width,pose.depth,.06f,Mat(palette[(int)use]),pose.yaw);
            if (use == LandUse.Road)
            {
                bool horizontal = city.Get(i % 36 - 1, i / 36) == use || city.Get(i % 36 + 1, i / 36) == use;
                landscape.Surface("Lane marking",t,t.position,horizontal ? .85f : .07f,horizontal ? .07f : .85f,.09f,Mat(new Color(.82f,.8f,.63f)));
                return;
            }
            if (use >= LandUse.Residential && use <= LandUse.Industrial && city.levels[i] == 0) return;
            BuildingHeight(pose,out float low,out float high);
            // A level foundation keeps buildings upright while the ground remains sloped.
            Vector3 basePosition = t.position; basePosition.y = high + .1f;
            var foundation = new GameObject("Level building base").transform;
            foundation.SetParent(t,false); foundation.position = basePosition; t = foundation;
            float depth = high - low + .2f;
            Box("Foundation",new Vector3(0,-depth / 2,0),new Vector3(pose.width-.05f,depth,pose.depth-.05f),new Color(.48f,.48f,.42f),t);
            if (use == LandUse.Park)
            {
                Box("Path", new Vector3(0,.1f,0), new Vector3(.5f,.06f,2.8f), new Color(.77f,.74f,.61f), t);
                Tree(new Vector3(-.8f,0,-.6f), t); Tree(new Vector3(.8f,0,.7f), t, .8f); return;
            }
            if (use == LandUse.Power)
            {
                Box("Power station", new Vector3(0,.65f,0), new Vector3(2.4f,1.2f,2.2f), new Color(.65f,.66f,.62f), t);
                for (int s = -1; s <= 1; s += 2) Box("Stack", new Vector3(s * .65f,2,.5f), new Vector3(.45f,3,.45f), new Color(.8f,.48f,.3f), t);
                return;
            }
            if (use == LandUse.Water)
            {
                for (int s = -1; s <= 1; s += 2) Box("Tower leg", new Vector3(s * .7f,1.4f,0), new Vector3(.16f,2.8f,.16f), new Color(.75f,.79f,.76f), t);
                Box("Water tank", new Vector3(0,2.9f,0), new Vector3(2.1f,1.3f,2.1f), palette[6], t); return;
            }
            if(use==LandUse.Residential && city.society!=null)
            {
                DrawHousing(pose,t); return;
            }
            int level = city.levels[i];
            if (level == 0) return;
            float height = use == LandUse.Industrial ? 1.3f + level * .5f : 1.2f + level * 1.35f + (i % 3) * .3f;
            var selection=t.gameObject.AddComponent<BoxCollider>();
            selection.center=new Vector3(0,height/2+.15f,0);
            selection.size=new Vector3(2.3f,height+.4f,2.3f);
            Color facade = use == LandUse.Residential ? new Color(.86f,.84f,.74f) : use == LandUse.Commercial ? new Color(.6f,.75f,.79f) : new Color(.72f,.65f,.5f);
            Box("Building", new Vector3(0,height / 2 + .1f,0), new Vector3(2.15f,height,2.15f), facade, t);
            Box("Roof", new Vector3(0,height + .16f,0), new Vector3(2.3f,.18f,2.3f), use == LandUse.Residential ? new Color(.36f,.45f,.46f) : palette[(int)use], t);
            for (float y = .8f; y < height; y += .95f)
            {
                Box("Windows", new Vector3(0,y,-1.081f), new Vector3(1.65f,.36f,.025f), new Color(.24f,.38f,.43f), t);
                Box("Windows", new Vector3(1.081f,y,0), new Vector3(.025f,.36f,1.65f), new Color(.24f,.38f,.43f), t);
            }
        }

        void Rebuild()
        {
            InitializeRoads();
            city.EnableBuildings();
            city.EnableHouseholds();
            city.EnableResidentTransport();
            foreach (Transform child in world) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            visuals.Clear();
            zoningRoads = null;
            for (int i = 0; i < city.tiles.Length; i++) DrawLot(i);
            RefreshRoadView();
        }

        void UpdateCamera()
        {
            if (landscape != null) focus.y = Mathf.Max(0,landscape.Height(focus.x,focus.z));
            cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0);
            // Keep the lower edge of the orthographic view above ground at low angles.
            float pitchRadians = pitch * Mathf.Deg2Rad;
            float distance = Mathf.Max(130f, (zoom * Mathf.Cos(pitchRadians) + 12f) / Mathf.Sin(pitchRadians));
            cam.transform.position = focus - cam.transform.forward * distance;
            cam.orthographicSize = zoom;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard != null && !simulationTyping)
            {
                if(keyboard.f3Key.wasPressedThisFrame) showSimulation=!showSimulation;
                Vector3 move = Vector3.zero;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.z++;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.z--;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x++;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x--;
                focus += Quaternion.Euler(0,yaw,0) * move * (zoom * .65f * Time.deltaTime);
                focus.x = Mathf.Clamp(focus.x,-CityModel.BuildHalfSize,CityModel.BuildHalfSize); focus.z = Mathf.Clamp(focus.z,-CityModel.BuildHalfSize,CityModel.BuildHalfSize);
                if (keyboard.qKey.isPressed) yaw -= Time.deltaTime * 55;
                if (keyboard.eKey.isPressed) yaw += Time.deltaTime * 55;
                if (keyboard.spaceKey.wasPressedThisFrame) speed = speed == 0 ? 1 : 0;
                if (keyboard.escapeKey.wasPressedThisFrame && !CancelRoad()) selected = LandUse.Empty;
                if (keyboard.zKey.wasPressedThisFrame && (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)) UndoRoad();
                if (keyboard.digit0Key.wasPressedThisFrame) selected = LandUse.Empty;
                for (int n = 1; n <= 8; n++) if (keyboard[(Key)((int)Key.Digit1 + n - 1)].wasPressedThisFrame) selected = (LandUse)n;
            }
            if (mouse != null)
            {
                Vector2 mp = mouse.position.ReadValue();
                bool overUI = OverUI(mp);
                UpdateCamera();
                if (mouse.middleButton.wasPressedThisFrame && !overUI && cam.pixelRect.Contains(mp))
                {
                    draggingView = true;
                    rightGesture = rotatingView = false;
                    previousDragPointer = mp;
                }
                if (!mouse.middleButton.isPressed || !Application.isFocused) draggingView = false;
                if (draggingView)
                {
                    PanView(previousDragPointer, mp);
                    previousDragPointer = mp;
                    lastPaint = -1;
                }
                RotateWithMouse(mouse, mp, overUI);
                if (!overUI) ZoomAtPointer(mp, mouse.scroll.ReadValue().y);
                var ray = cam.ScreenPointToRay(mp);
                hoverX = hoverZ = -1;
                if (!overUI && !draggingView && !mouse.rightButton.isPressed
                    && landscape.Raycast(ray, out Vector3 hit))
                {
                    hoverX = Mathf.FloorToInt((hit.x + 54) / Cell); hoverZ = Mathf.FloorToInt((hit.z + 54) / Cell);
                }
                bool inside = CityModel.Inside(hoverX,hoverZ);
                bool roadHandled = HandleRoadInput(mouse,ray,overUI || draggingView || mouse.rightButton.isPressed);
                if (selected == LandUse.Empty && mouse.leftButton.wasPressedThisFrame && !overUI
                    && !draggingView && !mouse.rightButton.isPressed && !InspectBuilding(ray)) InspectTraffic(mp);
                HandleBuildingInput(mouse,ray,overUI || draggingView || mouse.rightButton.isPressed || roadHandled);

            }
            AdvanceSociety(Time.deltaTime * speed);
            UpdateSimulationLog();
            AnimateTraffic();
            UpdateCommuteView();
            UpdateResidentView();
            UpdateBusinessMarker();
            UpdateTrafficEndpointMarker();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) { draggingView = rightGesture = rotatingView = false; lastPaint = -1; }
        }

        void RotateWithMouse(Mouse mouse, Vector2 pointer, bool overUI)
        {
            if (mouse.rightButton.wasPressedThisFrame && !overUI && !draggingView
                && Application.isFocused && cam.pixelRect.Contains(pointer))
            {
                rightGesture = true;
                rotatingView = false;
                rightPressPointer = previousRotatePointer = pointer;
            }
            if (!rightGesture) return;
            lastPaint = -1;
            if (draggingView || !Application.isFocused)
            {
                rightGesture = rotatingView = false;
                return;
            }

            // Distinguish a click from a drag so rotating preserves the selected tool.
            if ((pointer - rightPressPointer).sqrMagnitude >= RotationDragThreshold * RotationDragThreshold)
                rotatingView = true;
            if (rotatingView)
            {
                yaw = Mathf.Repeat(yaw + (pointer.x - previousRotatePointer.x) * RotationDegreesPerPixel, 360f);
                pitch = Mathf.Clamp(pitch - (pointer.y - previousRotatePointer.y) * RotationDegreesPerPixel, MinPitch, MaxPitch);
                UpdateCamera();
            }
            previousRotatePointer = pointer;
            if (mouse.rightButton.wasReleasedThisFrame)
            {
                if (!rotatingView && !CancelRoad()) selected = LandUse.Empty;
                rightGesture = rotatingView = false;
            }
            else if (!mouse.rightButton.isPressed) rightGesture = rotatingView = false;
        }

        void PanView(Vector2 from, Vector2 to)
        {
            // Drag on a fixed plane through the picked terrain point to avoid jumping on hills.
            var fromRay = cam.ScreenPointToRay(from);
            Vector3 anchor = landscape.Raycast(fromRay,out Vector3 hit) ? hit : focus;
            var ground = new Plane(Vector3.up, anchor);
            var toRay = cam.ScreenPointToRay(to);
            if (!ground.Raycast(fromRay, out float fromDistance) || !ground.Raycast(toRay, out float toDistance)) return;
            focus += fromRay.GetPoint(fromDistance) - toRay.GetPoint(toDistance);
            focus.x = Mathf.Clamp(focus.x, -CityModel.BuildHalfSize, CityModel.BuildHalfSize);
            focus.z = Mathf.Clamp(focus.z, -CityModel.BuildHalfSize, CityModel.BuildHalfSize);
            UpdateCamera();
        }

        void ZoomAtPointer(Vector2 pointer, float scroll)
        {
            // Input System 1.20 defaults to one unit per notch, not Windows' native 120.
            if (InputSystem.settings.scrollDeltaBehavior == InputSettings.ScrollDeltaBehavior.KeepPlatformSpecificInputRange
                && (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer))
                scroll /= 120f;
            if (Mathf.Approximately(scroll, 0f)) return;

            var beforeRay = cam.ScreenPointToRay(pointer);
            Vector3 anchor = landscape.Raycast(beforeRay,out Vector3 hit) ? hit : focus;
            var ground = new Plane(Vector3.up, anchor);
            bool anchored = ground.Raycast(beforeRay, out float beforeDistance);
            zoom = Mathf.Clamp(zoom * Mathf.Pow(ZoomPerStep, scroll), MinZoom, MaxZoom);
            UpdateCamera();
            var afterRay = cam.ScreenPointToRay(pointer);
            if (anchored && ground.Raycast(afterRay, out float afterDistance))
            {
                focus += beforeRay.GetPoint(beforeDistance) - afterRay.GetPoint(afterDistance);
                focus.x = Mathf.Clamp(focus.x, -CityModel.BuildHalfSize, CityModel.BuildHalfSize);
                focus.z = Mathf.Clamp(focus.z, -CityModel.BuildHalfSize, CityModel.BuildHalfSize);
                UpdateCamera();
            }
        }

        void Paint(int x, int z)
        {
            if (selected != LandUse.Bulldoze && city.Get(x,z) == selected) return;
            if (selected != LandUse.Bulldoze)
            {
                landscape.LotRange(Position(x,z),out float low,out float high);
                if (low <= CityLandscape.SeaLevel + .15f) { notice = "水面不能直接建设，请选择陆地。"; return; }
                float maxRise = selected == LandUse.Road ? 2.1f : 1.5f;
                if (high - low > maxRise) { notice = "坡度太陡，请沿缓坡绕行或换一处地块。"; return; }
            }
            if (city.Place(x,z,selected,out string error))
            {
                DrawLot(CityModel.Index(x,z));
                for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++)
                    if (Math.Abs(dx) + Math.Abs(dz) == 1 && CityModel.Inside(x + dx,z + dz)
                        && city.Get(x + dx,z + dz) == LandUse.Road) DrawLot(CityModel.Index(x + dx,z + dz));
                notice = selected == LandUse.Bulldoze ? "已拆除。" : names[(int)selected] + "已规划；建筑需要连接西侧入口的道路和水电。";
            }
            else if (error.Length > 0) notice = error;
        }

        void AnimateTraffic()
        {
            var activeIds = new HashSet<int>();
            foreach(var trip in traffic.State.trips) if(trip.status!=TripStatus.Visiting) activeIds.Add(trip.id);
            var expired = new List<int>();
            foreach (var pair in trafficViews)
                if (!activeIds.Contains(pair.Key)) { pair.Value.gameObject.SetActive(false); expired.Add(pair.Key); }
            foreach (int id in expired) trafficViews.Remove(id);
            var assigned = new HashSet<Transform>(trafficViews.Values);
            var available = new Queue<Transform>();
            foreach(var model in cars) if(!assigned.Contains(model)) available.Enqueue(model);
            foreach (var trip in traffic.State.trips)
            {
                if(trip.status==TripStatus.Visiting) continue;
                if (!trafficViews.TryGetValue(trip.id, out var car))
                {
                    if(available.Count>0) car=available.Dequeue();
                    else
                    {
                        car=Box("Traffic",Vector3.zero,new Vector3(.5f,.35f,.85f),Color.white,transform).transform;
                        cars.Add(car);
                    }
                    trafficViews[trip.id] = car;
                }
                bool visible = trip.status != TripStatus.Visiting && (city.roads != null
                    ? city.roads.Active(trip.Current) && (trip.progress == 0 || city.roads.Linked(trip.Current,trip.Next))
                    : city.tiles[trip.Current] == (int)LandUse.Road);
                car.gameObject.SetActive(visible);
                if (!visible) continue;
                bool freight = trip.purpose >= TripPurpose.Delivery;
                car.localScale = freight ? new Vector3(.65f,.5f,1.15f) : new Vector3(.5f,.35f,.85f);
                Color color = trip.id == inspectedTrip ? new Color(1,.85f,.18f) : freight ? palette[4]
                    : trip.purpose == TripPurpose.Commute ? Color.white : palette[3];
                car.GetComponent<Renderer>().sharedMaterial = Mat(color);
                car.name = "Vehicle " + trip.id + " " + Purpose(trip);
                car.position = TrafficPoint(trip,trip.segment,trip.progress);
                if (trip.Current != trip.Next)
                {
                    Vector3 forward = TrafficPoint(trip,trip.segment,Mathf.Min(1,trip.progress + .02f)) - car.position;
                    if (forward.sqrMagnitude > .000001f) car.rotation = Quaternion.LookRotation(forward);
                }
            }
            var inspected = traffic.State.trips.Find(t => t.id == inspectedTrip);
            routeLine.positionCount = 0;
            if (inspected != null && inspected.status != TripStatus.Visiting)
            {
                var points = new List<Vector3>();
                for (int segment = inspected.segment; segment < inspected.route.Count - 1; segment++)
                    for (int k = 0; k <= 8; k++)
                    {
                        float fraction = segment == inspected.segment ? Mathf.Lerp(inspected.progress,1,k / 8f) : k / 8f;
                        points.Add(TrafficPoint(inspected,segment,fraction) - Vector3.up * .09f);
                    }
                routeLine.positionCount = points.Count; routeLine.SetPositions(points.ToArray());
            }
        }

        Vector3 TrafficPoint(TrafficTrip trip, int segment, float fraction)
        {
            int a = trip.route[segment], b = trip.route[Mathf.Min(segment + 1,trip.route.Count - 1)];
            Vector3 start = RoadPosition(a), end = RoadPosition(b);
            Vector3 direction = end - start; direction.y = 0; direction.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up,direction);
            Vector3 startRight = right, endRight = right;
            if (segment > 0)
            {
                int previous = trip.route[segment - 1];
                Vector3 incoming = start - RoadPosition(previous); incoming.y = 0;
                startRight = (right + Vector3.Cross(Vector3.up,incoming.normalized)) * .5f;
            }
            if (segment + 2 < trip.route.Count)
            {
                int next = trip.route[segment + 2];
                Vector3 outgoing = RoadPosition(next) - end; outgoing.y = 0;
                endRight = (right + Vector3.Cross(Vector3.up,outgoing.normalized)) * .5f;
            }
            Vector3 point = Vector3.Lerp(start + startRight * .55f,end + endRight * .55f,fraction);
            point.y = (city.roads == null ? landscape.Height(point.x,point.z) : CityRoadView.SurfaceHeight(city.roads,landscape,point,a,b)) + .32f;
            return point;
        }

        void InspectTraffic(Vector2 pointer)
        {
            inspectedTrip = -1;
            float closest = 18 * 18;
            foreach (var pair in trafficViews)
            {
                if (!pair.Value.gameObject.activeSelf) continue;
                Vector3 screen = cam.WorldToScreenPoint(pair.Value.position);
                float distance = ((Vector2)screen - pointer).sqrMagnitude;
                if (screen.z > 0 && distance < closest) { closest = distance; inspectedTrip = pair.Key; }
            }
        }

        string Purpose(TrafficTrip trip)
        {
            string[] purposes = { "通勤", "购物", "配送商品", "进口商品", "出口商品" };
            if(trip.cargoKind==CargoKind.RawMaterial) return trip.returning?"进口原料 · 返程":"进口原料";
            return trip.returning ? purposes[(int)trip.purpose] + " · 返程" : purposes[(int)trip.purpose];
        }
        string EndpointName(int i) => i == CityTraffic.Outside ? "西侧城外入口" : names[city.tiles[i]] + " #" + i;

        void LocateTrafficEndpoint(int id,bool origin)
        {
            Vector3 destination;
            if(id==CityTraffic.Outside)
            {
                var entrance=city.roads?.Node(CityRoads.Entrance);
                if(entrance==null) {notice="城外入口目前不可定位。"; return;}
                destination=new Vector3(entrance.x,0,entrance.z);
            }
            else
            {
                if(id<0 || city.buildings==null || id>=city.buildings.Count)
                {notice="该地点目前不可定位。"; return;}
                var building=city.buildings[id];
                destination=new Vector3(building.x,0,building.z);
            }
            followResident=false; showSimulation=false;
            locatedTrafficEndpoint=id; locatedTrafficTrip=inspectedTrip; locatedTrafficOrigin=origin;
            focus=destination; zoom=24; UpdateCamera();
            UpdateTrafficEndpointMarker();
            notice="已定位："+EndpointName(id)+(id>=0 && city.tiles[id]==0?"（建筑已拆除，显示原址）":"");
        }

        bool OverUI(Vector2 position)
        {
            float scale = Mathf.Min(Screen.width / 1440f, Screen.height / 900f);
            float x = position.x / scale, y = (Screen.height - position.y) / scale;
            float width = Screen.width / scale, height = Screen.height / scale;
            return showSimulation && new Rect(340,110,width-640,height-305).Contains(new Vector2(x,y)) || y < 108 || y > height - 185 || (x > width - 290 && y < 490)
                || (help && x < 485 && y < 470) || (!help && x < 345 && y > 115 && y < (selected==LandUse.Empty && inspectedHome>=0?height-200:selected==LandUse.Residential?505:385));
        }

        void Styles()
        {
            if (label != null) return;
            label = new GUIStyle(GUI.skin.label) { font = font, fontSize = 17, normal = { textColor = new Color(.88f,.93f,.94f) } };
            small = new GUIStyle(label) { fontSize = 14, wordWrap = true };
            title = new GUIStyle(label) { fontSize = 26, fontStyle = FontStyle.Bold };
            number = new GUIStyle(title) { fontSize = 23 };
            button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 16, alignment = TextAnchor.MiddleCenter };
        }

        void Panel(Rect rect, Color color)
        {
            GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = Color.white;
        }

        void Stat(float x, string caption, string value)
        {
            GUI.Label(new Rect(x,22,170,23),caption,small);
            GUI.Label(new Rect(x,47,175,35),value,number);
        }

        void OnGUI()
        {
            if (city == null || traffic == null) return;
            Styles();
            float scale = Mathf.Min(Screen.width / 1440f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale,scale,1));
            float w = Screen.width / scale, h = Screen.height / scale;
            Color navy = new Color(.055f,.105f,.14f,.96f);
            DrawCommuteLabels(scale,navy);
            DrawTrafficEndpointLabel(scale,navy);
            Panel(new Rect(0,0,w,100),navy);
            Panel(new Rect(24,26,5,47),palette[2]);
            GUI.Label(new Rect(43,20,225,37),"湾岸 / HARBOR",title);
            GUI.Label(new Rect(45,58,210,25),"城市建造 · 自由沙盒",small);
            Stat(290,"城市资金", "¥ " + city.money.ToString("N0"));
            GUI.Label(new Rect(485,22,170,23),"人口 · 点击查看",small);
            if(GUI.Button(new Rect(485,47,175,35),city.population.ToString("N0"),number)) OpenPopulationPanel();
            Stat(655,"就业岗位",city.jobs.ToString("N0"));
            Stat(825,"上日租金减当前维护",(city.income >= city.upkeep ? "+ " : "− ") + Math.Abs(city.income - city.upkeep));
            Stat(1020,"幸福度",city.happiness + "%");
            GUI.Label(new Rect(w - 170,23,160,28),"第 " + city.day + " 天",label);
            int minute=city.society==null?0:Mathf.Clamp(Mathf.FloorToInt(city.ResidentMinute),0,1439);
            GUI.Label(new Rect(w - 170,55,160,28),(minute/60).ToString("00")+":"+(minute%60).ToString("00")
                + (speed==0?" · 暂停":" · "+speed.ToString("0.#")+"×"),label);

            float rx = w - 274;
            Panel(new Rect(rx,120,250,312),navy);
            GUI.Label(new Rect(rx + 20,138,220,30),"城市概览",title);
            Meter(rx + 20,185,"电力",city.demand,city.power,palette[5]);
            Meter(rx + 20,238,"供水",city.demand,city.water,palette[6]);
            GUI.Label(new Rect(rx + 20,293,225,26),"上日实收租金  +" + city.income + " / 天",label);
            GUI.Label(new Rect(rx + 20,322,225,26),"维护  −" + city.upkeep + " / 天",label);
            GUI.Label(new Rect(rx + 20,365,212,60),"交通任务 " + traffic.State.trips.Count + "  /  完成 " + traffic.State.completed
                + "\n送货 " + traffic.State.delivered + "  /  购物 " + traffic.State.purchases + "  /  失败 " + traffic.State.failed,small);

            if (!help && selected == LandUse.Road) RoadToolPanel(navy);
            if (!help && selected != LandUse.Road && !(selected==LandUse.Empty && inspectedHome>=0))
            {
                Panel(new Rect(24,120,310,258),navy);
                if (GUI.Button(new Rect(40,134,278,32),"0  查看建筑 / 车辆",button)) selected = LandUse.Empty;
                var trip = traffic.State.trips.Find(t => t.id == inspectedTrip);
                if (trip == null)
                    GUI.Label(new Rect(40,180,278,180),"按 0 或 Esc 退出建造。点击住宅查看住户，点击工厂或商业查看经营，点击车辆查看路线。\n\n白色：通勤   蓝色：购物\n橙色：配送 / 进出口\n\n黄色线显示所选车辆的剩余路线。",small);
                else
                {
                    string status = trip.status == TripStatus.Visiting ? "已到达 / 停留" : trip.status == TripStatus.Waiting ? "等待道路恢复"
                        : trip.blocked > .1f ? "排队让行" : "行驶中";
                    GUI.Label(new Rect(40,180,278,158),"车辆 #" + trip.id + "  " + Purpose(trip) + "\n从：" + EndpointName(trip.origin)
                        + "\n到：" + EndpointName(trip.destination) + "\n状态：" + status + "\n剩余路段：" + (trip.route.Count - 1 - trip.segment)
                        + (trip.blocked>.1f?"\n连续等待："+trip.blocked.ToString("F1")+" 秒":"")
                        + (trip.residentId>0?"\n乘员：居民 #"+trip.residentId+" / 家庭 #"+trip.householdId:"")
                        + (trip.cargo > 0 ? "   载货：" + trip.cargo : "")
                        + (trip.destination >= 0 && city.tiles[trip.destination] == 3 ? "\n目的地库存：" + traffic.State.stock[trip.destination] : ""),small);
                    if(GUI.Button(new Rect(40,342,135,30),"定位出发地",button)) LocateTrafficEndpoint(trip.origin,true);
                    if(GUI.Button(new Rect(183,342,135,30),"定位目的地",button)) LocateTrafficEndpoint(trip.destination,false);
                }
            }

            Panel(new Rect(24,h - 136,w - 48,108),navy);
            for (int n = 1; n <= 8; n++)
            {
                Rect r = new Rect(40 + (n - 1) * 111,h - 120,101,74);
                Panel(r,selected == (LandUse)n ? new Color(.23f,.39f,.43f) : new Color(.1f,.17f,.21f));
                Panel(new Rect(r.x,r.y,101,4),palette[n]);
                if (GUI.Button(r,n + "  " + names[n] + (n == 1 ? "\n¥ 100 / 3米" : n==2 ? "\n选择住宅类型" : "\n¥ " + (CityModel.Cost((LandUse)n)+(n==4?FactoryState.StartingCash:0))),button)) selected = (LandUse)n;
            }
            if (GUI.Button(new Rect(955,h - 118,76,31),speed == 0 ? "继续" : "暂停",button)) speed = speed == 0 ? 1 : 0;
            if (GUI.Button(new Rect(1039,h - 118,76,31),speed == 3 ? "3×" : "1×",button)) speed = speed == 3 ? 1 : 3;
            if (GUI.Button(new Rect(1123,h - 118,76,31),"保存",button)) Save();
            if (GUI.Button(new Rect(1207,h - 118,76,31),"读取",button)) Load();
            if (GUI.Button(new Rect(1291,h - 118,85,31),"帮助",button)) help = !help;
            GUI.Label(new Rect(955,h - 77,410,42),"中键平移 · 右键左右旋转 / 上下俯仰\n滚轮缩放 · WASD 移动 · Q/E 旋转",small);
            Panel(new Rect(24,h - 181,w - 48,33),new Color(.055f,.105f,.14f,.85f));
            GUI.Label(new Rect(38,h - 177,w - 80,29),notice,small);

            DrawSimulationPanel(w,h,navy);
            if (help)
            {
                Panel(new Rect(24,120,450,337),navy);
                GUI.Label(new Rect(44,140,410,40),"从一条道路开始",title);
                GUI.Label(new Rect(44,190,400,250),"1   道路须连接地图西侧的入口。\n\n2   在路边拖拽划分住宅、商业和工业区。\n\n3   临路电站与水塔提供全城容量；不足时扩建。\n\n4   平衡人口与就业，建公园提升幸福度。\n\n按 1 点击起点、终点修路；2–8 切换工具。\n右键拖动旋转，中键拖动平移。\n右键 / Esc 撤回道路起点或退出；空格暂停。\n存档保存在本机，读取会替换当前进度。",small);
            }
        }

        void Meter(float x, float y, string name, int used, int capacity, Color color)
        {
            GUI.Label(new Rect(x,y,215,25),name + "  " + used + " / " + capacity,small);
            Panel(new Rect(x,y + 28,210,6),new Color(.2f,.27f,.29f));
            Panel(new Rect(x,y + 28,210 * Mathf.Clamp01(capacity == 0 ? 1 : (float)used / capacity),6),used > capacity || capacity == 0 ? palette[8] : color);
        }

        void Save()
        {
            try
            {
                string temp = SavePath + ".tmp";
                File.WriteAllText(temp,JsonUtility.ToJson(city,true));
                if (File.Exists(SavePath)) File.Copy(SavePath,SavePath + ".bak",true);
                File.Copy(temp,SavePath,true); File.Delete(temp);
                city.Trace("save.success","城市存档保存成功");
                notice = "城市已保存。第 " + city.day + " 天 / " + city.population + " 人";
            }
            catch (Exception e) { notice = "保存失败：" + e.Message; }
        }

        void Load()
        {
            try
            {
                if (!File.Exists(SavePath)) { notice = "还没有存档，请先保存城市。"; return; }
                var loaded = JsonUtility.FromJson<CityModel>(File.ReadAllText(SavePath));
                if (loaded == null || !loaded.Valid()) { notice = "存档格式无效，当前城市已保留。"; return; }
                loaded.EnableRoads(landscape.Height);
                loaded.EnableBuildings();
                loaded.EnableHouseholds();
                StopSimulationLog();
                city = loaded; city.Recalculate(); traffic = new CityTraffic(city); inspectedTrip = -1; inspectedHome=-1; commuteFamily=-1; roadUndo.Clear();
                foreach (var car in cars) car.gameObject.SetActive(false);
                trafficViews.Clear(); timer = 0; Rebuild(); AnimateTraffic(); notice = "已读取第 " + city.day + " 天的城市。"; StartSimulationLog("读取存档");
            }
            catch (Exception e) { notice = "读取失败：" + e.Message; }
        }

        void OnDestroy()
        {
            foreach (var material in materials.Values) if (material != null) Destroy(material);
            if (font != null) Destroy(font);
        }
    }
}
