# 车辆消失回归修复与更正

## 原因及修复

这是分层显示引入的真实回归。CPU索引和代表数量正确，不代表GPU已绘制：中景车辆、预算不足时的近景替代以及远景车流摘要使用 `Graphics.DrawMeshInstanced`，但共用的 RoadCore Shader 缺少实例化变体与实例ID初始化，不能读取每个实例的物体变换。原有近景截图和CPU覆盖断言没有覆盖这个错误。

`17ea4d5` 添加 `multi_compile_instancing`、`UNITY_VERTEX_INPUT_INSTANCE_ID` 和 `UNITY_SETUP_INSTANCE_ID`。`0b263ed` 将 GraphicsSettings 的 Instancing Variants 设为 **Keep All（值2）**，防止动态创建材质使用的实例化变体被静态场景扫描剔除；这是表现构建设置，不是模拟运行规则。枚举及含义见 [Unity官方源码](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/EditorGraphicsSettings.cs)。构建日志确认 RoadCore 的dx11/dx12顶点变体由剔除后1个变为2个。Keep All也会保留其他Shader实例化变体，不能宣称构建体积或加载成本零增加；本轮未单独测量其增量。

模拟C#、车辆数量/位置、经济、固定步、存档格式均未改。当前玩家Editor保持Play，只刷新Shader即可恢复；现场已看到道路上的黄色车辆重新显示。为了配对测试暂时暂停Editor城市，结束后恢复原50×目标。没有保存覆盖玩家城市。

## 独立像素证据

新增 `Tools/Diagnostics/HarborCityInstancedVehiclePixels.cs`：真实图形设备、256×256 RenderTexture、三辆处于非对称远离原点位置且旋转25度的批量立方体，独立投影检查三个预期位置必须为黄色，空白中心必须为黑色。调用实际运行时批量绘制类，不复制其实现。该工具仅注入临时诊断项目，不编入正式游戏。`Tools/VerifyVehicleViews.ps1` 已接入第二个有图形设备的Player检查，不能使用 `-nographics`；本轮直接执行其Editor和Release像素入口，验证脚本语法，没有重新跑完整旧3000对象生命周期套件。

- 冻结旧Shader：Editor像素测试失败，预期车辆位置实际为黑色，原日志与图片保留。
- 修复Shader：Editor像素测试通过。
- 首轮Release自动剔除、第二轮Strip All候选均失败，未计PASS；这促成最终Keep All设置。测试也改为在图形管线初始化后再执行。
- 最终Keep All **普通Release Player像素测试通过**，不是仅Editor通过。原始目录 `Temp/LayeredPresentation/instancing-fix/{before,after-tools,release-pixels,release-pixels2,release-pixels3}`，构建日志 `build-release{,2,3}.log`。

|旧Shader实际像素|最终Release实际像素|
|---|---|
|![旧版漏绘](InstancingMissing.png)|![三个正确实例](InstancingCorrect.png)|

## 修复后真实性能重新配对

原报告的完整显示性能结论已撤回；旧CSV不删除，作为失败记录保留。本表是修复变体后的新配对，不能用旧版漏绘结果代替。源码 `0b263ed`（Shader/设置已包含）；同一普通Release二进制分别走保留的旧显示路径和分层路径。详情及DLL/输入哈希见 [InstancingFixPlayer.csv](InstancingFixPlayer.csv)。测试机器、1440×900、60FPS上限、200固定步及8秒预热、20秒采样、8ms/32步预算沿用前报告。Editor城市已暂停，机器并未被清成专用测试环境；以下为有限窗口结果，不是多轮置信区间。

|城市/镜头，目标50×|旧显示FPS|修复后分层FPS|旧实际倍率|修复后实际倍率|
|---|---:|---:|---:|---:|
|十万人远景|29.72|54.82|1.49×|2.74×|
|三万人中景|57.14|59.32|6.94×|7.24×|

十万人远景P95 40.30→20.81ms，P99 70.14→53.43ms，最长91.79→86.32ms；车辆同步平均5.26→1.93ms，含区域维护。三万人中景约484辆/帧真正批量绘制；P99 26.14→26.18ms，没有宣称所有分位数都提高。三万人窗口垃圾路线树已预热，518次查询全命中、无建树；十万人沿用667基地容量整组点路由回退，两侧一致、无建树。Release逐帧GC字节仍不可可靠取得，CSV填-1；模拟工作量不同，不能凭GC次数推算单位业务分配改善。

## 状态及边界验证

四个普通窗口各自保存/冷读后继续20固定步完整状态一致。最终Release另跑十万人800固定步，持续平移、缩放、旋转及分辨率切换，显示覆盖/互斥、稳定身份拾取、暂停状态不变及建筑生命周期检查通过；最终Tick14961，完整存档SHA256 `1ed7adf3a41fde9d740b74bec70888da9ce456749d7389d3b541c693a29a889a`，与已有冻结旧显示800步的完整文件一致。该断言窗口不作为正常FPS数据。证据在 `Temp/LayeredPresentation/instancing-fix/fixed-100k-sweep`，像素验证与业务状态验证是两种独立检查。

没有重测/消除原有施工长帧和特定变道中拆路的旧几何缺陷，也未重跑所有镜头的完整性能矩阵，不作百万城市验收声明。玩家未提交修改、CMD、配置和存档文件没有夹带或覆盖。正式分支只快进集成这次修复及证据，main和其他实验不变。
