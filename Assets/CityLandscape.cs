using System.Collections.Generic;
using UnityEngine;

namespace HarborCity
{
    // Fixed landscape seed/shape: older city saves keep exactly the same lot coordinates.
    public sealed class CityLandscape : MonoBehaviour
    {
        const int Resolution = 513;
        const float HalfSize = 120f;
        const float BaseHeight = -8f;
        const float HeightRange = 64f;
        Terrain terrain;
        TerrainData data;
        TerrainCollider ground;
        readonly List<Object> ownedAssets = new List<Object>();
        public Terrain Terrain => terrain;
        public const float SeaLevel = 0f;

        static float Hill(float x, float z, float cx, float cz, float radius, float height)
        {
            float dx = (x - cx) / radius, dz = (z - cz) / radius;
            return height * Mathf.Exp(-(dx * dx + dz * dz));
        }

        static float Elevation(float x, float z)
        {
            float radius = Mathf.Sqrt(x * x + z * z);
            float angle = Mathf.Atan2(z, x);
            float shoreline = 91 + 8 * Mathf.Sin(angle * 3 + .8f) + 5 * Mathf.Cos(angle * 5);
            float coast = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(shoreline - 15, shoreline + 10, radius));
            float hills = Hill(x,z,-37,38,20,18) + Hill(x,z,39,46,19,25)
                + Hill(x,z,-39,-39,22,13) + Hill(x,z,57,-26,24,12)
                + Hill(x,z,-62,60,24,17);
            float centre = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(28, 54, radius));
            float noise = (Mathf.PerlinNoise(x * .035f + 41, z * .035f + 73) - .5f) * 2;
            float land = 2.1f + hills * centre + noise * (.25f + centre * 1.8f);
            // Keep the west entrance as an accessible valley through the hills.
            float entranceValley = x < 0 ? Mathf.Exp(-Mathf.Pow((z - 1.5f) / 7f, 2)) : 0;
            land = Mathf.Lerp(land, 2.1f, entranceValley * .9f);
            return Mathf.Lerp(land, -7f, coast);
        }


        public void Build()
        {
            // 如果 terrain 已经创建过，就直接返回，避免重复生成
            if (terrain != null) return;


            // 从 Resources/HarborCity/Terrain 加载一个 Material
            // 这里加载的是 Terrain 最终用于渲染的材质
            var material = Resources.Load<Material>("HarborCity/Terrain");

            // 如果没找到材质，直接报错
            if (material == null)
                throw new System.InvalidOperationException(
                    "Missing HarborCity Terrain material."
                );


            // 创建 TerrainData
            //
            // TerrainData 可以理解为：
            // “这块地形的数据本体”
            //
            // 它里面存：
            // 1. 高度图
            // 2. Terrain Layer
            // 3. 地表纹理混合权重
            // 4. 地形尺寸
            data = new TerrainData
            {
                name = "Harbor Island v1",

                // 高度图分辨率
                // 这里是 513 × 513 个高度采样点
                heightmapResolution = Resolution,

                // 地形的世界尺寸：
                // x = 240
                // y = 64
                // z = 240
                //
                // HalfSize = 120
                // HeightRange = 64
                size = new Vector3(
                    HalfSize * 2,
                    HeightRange,
                    HalfSize * 2
                ),

                // 地表纹理权重图分辨率
                // 即后面控制“哪里是草、哪里是岩石、哪里是沙”的图
                alphamapResolution = 256,

                // 远距离使用的 base map 分辨率
                baseMapResolution = 512
            };

            // 记录起来，OnDestroy 时手动 Destroy
            ownedAssets.Add(data);


            // 创建高度图数组
            //
            // 513 × 513
            // 每个元素是一个 0~1 的高度值
            var heights = new float[Resolution, Resolution];


            // 遍历高度图上的每一个点
            for (int z = 0; z < Resolution; z++)
            {
                for (int x = 0; x < Resolution; x++)
                {
                    // 把 heightmap 下标 x
                    // 转换成世界坐标 wx
                    //
                    // x = 0       -> wx = -120
                    // x = 256     -> wx ≈ 0
                    // x = 512     -> wx = +120
                    float wx =
                        x / (float)(Resolution - 1)
                        * HalfSize * 2
                        - HalfSize;


                    // z 方向同理
                    float wz =
                        z / (float)(Resolution - 1)
                        * HalfSize * 2
                        - HalfSize;


                    // Elevation(wx, wz)
                    // 计算这个世界坐标上的真实地形高度
                    //
                    // 例如可能得到：
                    // -7m
                    // 2m
                    // 15m
                    // 25m
                    //
                    // 但 Unity Terrain 的高度图要求是 0~1
                    //
                    // 所以这里把真实高度归一化：
                    //
                    // normalizedHeight =
                    //     (真实高度 - 最低高度) / 总高度范围
                    //
                    // BaseHeight = -8
                    // HeightRange = 64
                    //
                    // 最后 Clamp01 保证范围一定在 0~1
                    heights[z, x] = Mathf.Clamp01(
                        (Elevation(wx, wz) - BaseHeight)
                        / HeightRange
                    );
                }
            }


            // 把生成好的高度图交给 Unity TerrainData
            //
            // 从 heightmap 的 (0,0) 开始写入整个 heights
            data.SetHeights(0, 0, heights);



            // 定义四种地表基础颜色
            //
            // 大概对应：
            // 0：普通草地
            // 1：高地/深色植被
            // 2：岩石
            // 3：沙滩
            var colors = new[]
            {
                new Color(.39f,.54f,.32f),
                new Color(.30f,.43f,.28f),
                new Color(.49f,.50f,.45f),
                new Color(.75f,.70f,.52f)
            };


            // 创建 4 个 TerrainLayer
            //
            // TerrainLayer 就是 Terrain 可以混合使用的
            // “地表材质层”
            var layers = new TerrainLayer[colors.Length];


            for (int i = 0; i < layers.Length; i++)
            {
                // 这里没有使用真正的美术贴图，
                // 而是运行时临时生成一张 64x64 的纹理
                //
                // 所以注释里才写：
                // procedural placeholders
                var texture = new Texture2D(
                    64,
                    64,
                    TextureFormat.RGBA32,
                    true
                )
                {
                    name = "Ground layer " + i,

                    // 纹理平铺重复
                    wrapMode = TextureWrapMode.Repeat
                };


                // 为这张 64×64 纹理准备像素数组
                var pixels = new Color[64 * 64];


                for (int z = 0; z < 64; z++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        // 用 PerlinNoise 产生一点明暗变化
                        //
                        // 避免整块地面完全是纯色，
                        // 看起来太死板
                        float shade =
                            .92f
                            + .16f
                            * Mathf.PerlinNoise(
                                x * .19f + i * 13,
                                z * .19f + 7
                            );


                        // 基础颜色 × 随机明暗
                        pixels[z * 64 + x] =
                            new Color(
                                colors[i].r * shade,
                                colors[i].g * shade,
                                colors[i].b * shade,
                                1
                            );
                    }
                }


                // 把 pixels 写入 Texture2D
                texture.SetPixels(pixels);

                // 真正上传/应用纹理数据
                //
                // 第一个 true：
                // 生成 mipmap
                //
                // 第二个 true：
                // 之后不再保留 CPU 可读副本
                texture.Apply(true, true);


                // 记录起来，后面销毁
                ownedAssets.Add(texture);


                // 创建一个 TerrainLayer
                layers[i] = new TerrainLayer
                {
                    name = "Harbor surface " + i,

                    // 这个 Layer 使用刚才生成的纹理
                    diffuseTexture = texture,

                    // 纹理每 12 × 12 世界单位重复一次
                    tileSize = new Vector2(12, 12),

                    // 地面非常粗糙
                    smoothness = .05f,

                    // smoothness 不从贴图取，直接使用常量
                    smoothnessSource =
                        TerrainLayerSmoothnessSource.ConstantOnly,

                    // 非金属材质
                    metallic = 0
                };


                // 同样记录起来，方便销毁
                ownedAssets.Add(layers[i]);
            }


            // 告诉 TerrainData：
            // 这块 Terrain 有这四层地表材质
            data.terrainLayers = layers;



            // alphamapResolution = 256
            int size = data.alphamapResolution;


            // 创建地表权重图
            //
            // 维度：
            // 256 × 256 × 4
            //
            // 最后一维的 4：
            // 表示四个 TerrainLayer 的混合权重
            var weights = new float[size, size, 4];


            // 遍历地表权重图上的每一个点
            for (int z = 0; z < size; z++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 转成 0~1 的归一化坐标
                    float u = x / (float)(size - 1);
                    float v = z / (float)(size - 1);


                    // 根据 u,v 查询当前地形高度
                    //
                    // GetInterpolatedHeight 返回相对 Terrain 原点的高度
                    // 再 + BaseHeight 得到真实世界高度
                    float h =
                        data.GetInterpolatedHeight(u, v)
                        + BaseHeight;


                    // 计算“沙滩权重”
                    //
                    // 越靠近低海拔，sand 越高
                    float sand =
                        1
                        - Mathf.SmoothStep(
                            0,
                            1,
                            Mathf.InverseLerp(
                                .5f,
                                2.2f,
                                h
                            )
                        );


                    // 计算“岩石权重”
                    //
                    // 两种情况下更容易是岩石：
                    //
                    // 1. 坡度很陡
                    // 2. 海拔很高
                    float rock =
                        Mathf.Max(
                            Mathf.SmoothStep(
                                0,
                                1,
                                Mathf.InverseLerp(
                                    27,
                                    43,
                                    data.GetSteepness(u, v)
                                )
                            ),

                            Mathf.SmoothStep(
                                0,
                                1,
                                Mathf.InverseLerp(
                                    20,
                                    27,
                                    h
                                )
                            )
                        )
                        * (1 - sand);


                    // 计算“高地植被权重”
                    //
                    // 大约海拔 6~15 之间逐渐增加
                    //
                    // 同时排除已经属于 sand / rock 的部分
                    float upland =
                        Mathf.SmoothStep(
                            0,
                            1,
                            Mathf.InverseLerp(
                                6,
                                15,
                                h
                            )
                        )
                        * (1 - sand - rock);


                    // 剩余部分作为普通草地
                    weights[z, x, 0] =
                        1 - sand - rock - upland;

                    // 高地草地
                    weights[z, x, 1] = upland;

                    // 岩石
                    weights[z, x, 2] = rock;

                    // 沙滩
                    weights[z, x, 3] = sand;
                }
            }


            // 把刚才算出来的纹理混合权重交给 TerrainData
            data.SetAlphamaps(0, 0, weights);



            // 真正创建 Unity Terrain GameObject
            //
            // 这一步非常关键：
            // 前面只是准备 TerrainData
            //
            // 到这里才真正创建：
            //
            // GameObject
            // ├── Terrain
            // └── TerrainCollider
            var obj =
                UnityEngine.Terrain.CreateTerrainGameObject(data);


            // 给它改名
            obj.name = "Harbor Terrain";


            // 挂到当前 CityLandscape 所在 GameObject 下面
            obj.transform.SetParent(transform, false);


            // Terrain 默认以左下角为原点
            //
            // 因为希望整个地图中心在世界坐标 (0,0,0)
            // 所以把 Terrain 左下角放到：
            //
            // (-120, -8, -120)
            //
            // 地形尺寸又是：
            // 240 × 64 × 240
            //
            // 因此 x/z 最终正好覆盖：
            // -120 ~ +120
            obj.transform.localPosition =
                new Vector3(
                    -HalfSize,
                    BaseHeight,
                    -HalfSize
                );


            // 获取刚创建出来的 Terrain 组件
            terrain = obj.GetComponent<Terrain>();

            // 获取 TerrainCollider
            //
            // 之后鼠标射线检测地面就靠它
            ground = obj.GetComponent<TerrainCollider>();


            // 使用之前 Resources.Load 得到的 Terrain 材质
            terrain.materialTemplate = material;


            // 开启实例化渲染优化
            terrain.drawInstanced = true;


            // Terrain 网格允许的高度图近似误差
            //
            // 越小：
            // 地形细节越精确
            // 但渲染开销越高
            terrain.heightmapPixelError = 2;


            // 超过一定距离后使用较低精度的 basemap
            terrain.basemapDistance = 300;


            // 强制刷新 Terrain
            terrain.Flush();


            // 告诉 Physics 系统：
            // Transform / Collider 已经发生变化，
            // 现在立刻同步
            Physics.SyncTransforms();
        }


        // All placement and camera height queries now use Unity Terrain's height data.
        public float Height(float x, float z)
        {
            Vector3 origin = terrain.transform.position;
            return data.GetInterpolatedHeight(Mathf.Clamp01((x - origin.x) / data.size.x),
                Mathf.Clamp01((z - origin.z) / data.size.z)) + origin.y;
        }

        public bool Raycast(Ray ray, out Vector3 point)
        {
            if (ground.Raycast(ray,out var hit,1000)) { point = hit.point; return true; }
            point = default; return false;
        }

        public void LotRange(Vector3 centre, out float low, out float high)
        {
            low = float.PositiveInfinity; high = float.NegativeInfinity;
            for (int z = 0; z <= 6; z++) for (int x = 0; x <= 6; x++)
            {
                float h = Height(centre.x - 1.5f + x * .5f,centre.z - 1.5f + z * .5f);
                low = Mathf.Min(low,h); high = Mathf.Max(high,h);
            }
        }

        public GameObject Surface(string objectName, Transform parent, Vector3 centre, float width, float depth, float lift, Material material, float yaw = 0)
        {
            const int segments = 6;
            var vertices = new Vector3[(segments + 1) * (segments + 1)];
            var triangles = new int[segments * segments * 6];
            for (int z = 0; z <= segments; z++) for (int x = 0; x <= segments; x++)
            {
                Vector3 offset = Quaternion.Euler(0,yaw,0) * new Vector3((x / (float)segments - .5f) * width,0,(z / (float)segments - .5f) * depth);
                float px = centre.x + offset.x;
                float pz = centre.z + offset.z;
                vertices[z * (segments + 1) + x] = parent.InverseTransformPoint(new Vector3(px,Height(px,pz) + lift,pz));
            }
            int t = 0;
            for (int z = 0; z < segments; z++) for (int x = 0; x < segments; x++)
            {
                int a = z * (segments + 1) + x, b = a + segments + 1;
                triangles[t++] = a; triangles[t++] = b; triangles[t++] = a + 1;
                triangles[t++] = a + 1; triangles[t++] = b; triangles[t++] = b + 1;
            }
            var mesh = new Mesh { name = objectName, vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var obj = new GameObject(objectName); obj.transform.SetParent(parent,false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial = material;
            obj.AddComponent<CityGeneratedMesh>(); return obj;
        }

        void OnDestroy()
        {
            if (ground != null) ground.terrainData = null;
            if (terrain != null) terrain.terrainData = null;
            foreach (var asset in ownedAssets) if (asset != null) Destroy(asset);
        }
    }

    public sealed class CityGeneratedMesh : MonoBehaviour
    {
        void OnDestroy()
        {
            var filter = GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null) Destroy(filter.sharedMesh);
        }
    }
}
