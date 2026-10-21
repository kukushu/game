# 当前城市达不到50×的分析（2026-10-21）

结论：已在独立Unity Player复现约16×。主要模拟热点是大路网中的实际路径搜索，尤其垃圾车出车与收集后返程；显示侧也有明显固定负担。放宽宿主限制无法让这座城达到50×，反而降低FPS。本轮只完善现有采样入口并分析，没有优化算法或提高正式游戏预算。

## 输入和测试条件

- 基线源码 `1ab13fb65ca8991ec17efc537f99e4208db70cc9`；诊断工具提交 `d135d6b3fd23ca3548aeab3e7cdf505cfd202924`。正式分支 `codex/road-core-20261014`。
- 玩家保存后冻结的输入：`Temp/Speed50Analysis-20261021/input.hcity`，7,541,765字节，SHA256 `2589d2a9ab888aa48998dc84cbfbb2c76534847099a97b28eba4b9789c729be4`。原文件在 `C:/Users/16934/AppData/LocalLow/DefaultCompany/test/harbor-city-core-v16.hcity`，测试后校验值相同。
- 真实输入为331人、220人就业、560座建筑、3562条物理道路，terrainVersion=3；不是旧截图中的3234人或合成三千人城市。道路y范围2至2.0006103515625，旧入口要求精确等于2，第一轮因此拒绝加载，未计入有效结果。
- i5-14600KF / RTX3090 / Windows 11 26200 / Unity6000.6.3f1 / Windows x64 Mono。用户原Editor退出Play，测试逐个运行，1440×900，VSync关闭、目标60FPS，同一全城俯视相机；不代表所有镜头位置或Editor开销。
- 沿用现有ScenarioWindow脚本、真实宿主Update和全部显示对象。按存档地图加载，不修改道路高度；先完整执行200个0.05秒步，再暂停预热显示8秒，之后测量15秒。加载、预热、文件输出、断言和冷恢复均在计时外。
- 所有有效运行的预热完整状态SHA相同：`45ab4bc82663e5c2a8c69958edef15733d5c7e7796805e69b3bca845d6f4b1df`，起始tick141241。原存档自带5973.2282秒待模拟时间，保留不清空，所有倍率均按实际完成步数统计。
- 诊断入口沿用有限资金设置；正常Play临时无限资金开关不入档，不能声称完整复制了该开关。无设施、车辆、居民被删除，无模拟阶段被跳过。暂停组只用于分离显示开销，不作为模拟性能成绩。
- 普通Release组不启用归因。Development组启用原有归因计数和60帧原始Profiler（无Deep Profile），用于定位，不能将其FPS当作普通Player性能。
- baseline/repeat/32ms两组/profile构建时诊断修改尚未提交，内容为上述提交中的采样扩展，唯一区别是直接绑定landscape.Height；最终8ms/128组使用最终提交对应源码，将此绑定封装为ScenarioSurface，以保留旧平地夹具行为。SavedMap=true下两者使用相同高度函数；每组实际Managed DLL SHA保存在summary.csv，不冒称同一二进制。

## 普通Player结果

|配置|实际倍速|FPS|P99帧ms|最长帧ms|
|---|---:|---:|---:|---:|
|8ms / 32步，正式默认|15.72|22.33|130.12|189.09|
|8ms / 32步，复测|15.70|22.39|133.88|179.66|
|8ms / 128步，仅诊断|15.80|22.24|131.85|190.47|
|32ms / 32步，仅诊断|17.95|18.41|168.66|221.00|
|32ms / 128步，仅诊断|19.20|15.47|191.28|331.74|
|暂停，只显示|0|34.02|33.93|53.55|

50×需要每秒1000个完整0.05秒步。默认两轮约每秒314步。只抬步数上限几乎无效；同时加CPU预算只小幅提高吞吐，画面更慢。8ms是完整步结束后的协作检查，不能中断一个重步。默认模拟推进平均28.75ms/帧，车辆同步0.135ms、结构同步0.419ms；这两个同步计时不包含Unity后续渲染。

各组同起点，但固定墙钟时间内推进量不同，终点城市自然不同。以上是吞吐/交互代价对照，不是相同终点业务总CPU的加速比，也没有新旧优化收益声明。15秒有界窗口不足以概括全天最坏情况。

## Profiler证据和调用链

完整15秒归因组执行4000步，Step累计8908.48ms，其中RouteSearch为6470.10ms、514次，约72.6%，平均12.59ms/次。GrowthSearch累计180.36ms、33次；ZoningOccupancy224.34ms、9次。这里的父子累计时间不能相加。

60帧原始调用树可进一步区分寻路来源：

|实际路径搜索所在阶段|次数|累计ms|
|---|---:|---:|
|Settlement → RouteSearch|46|814.65|
|Waste → Admission → RouteLookup → PrepareRoute → RouteSearch|44|436.07|
|ResidentRequests → Dispatch → Admission → RouteLookup → PrepareRoute → RouteSearch|22|89.80|

代码对应：垃圾服务到达 `CityCoreWaste.Arrive` → `World.PrepareServiceReturn` → `AccessRouting.FromLane` → `Network.Route`；返程必须从车辆当前位置开始，分别尝试目标的合法方向。出车经过DispatchRoutes缓存后，未物化路线才调用AccessRouting.Find。`Network.RouteToEither`以堆、cost/parent/settled集合按累计行程成本展开路网；不是每辆车每步重新寻路。当前服务返程没有经过DispatchRoutes的端点路线缓存，缓存能否安全复用必须另做等价性实验，本轮没有绕过这项计算。Settlement中直接RouteSearch的来源与唯一的服务返程调用链相吻合。

这60帧里上述服务两类搜索占路径搜索时间约93%；不能直接把短片段比例推广到全日。搜索实际展开节点数量本轮未计数，不能声称每次访问全城或给出已验证的复杂度改善。

显示保留97,120个道路View（每个非零长度Drivable一个GameObject/MeshRenderer，不等于97,120条物理道路）、约234,841个分区格子/399个分区Renderer。暂停仍只有34FPS。原始Render Thread的DrawBuffersBatchMode累计976.81ms/60帧；主线程一项渲染队列抽取累计234.47ms。说明显示侧有持续负担，但未通过关闭道路等消融去证明各对象的精确占比，GPU计时不可用，不宣称GPU是瓶颈。

原始Profiler本身的FlushMemoryCounters累计638.37ms/60帧，是采样开销，不列为正式游戏热点。Release的每帧GC计数器不可用，CSV为-1；归因组可用的ProfilerRecorder为约502,990B/帧，不能与Release直接比较。60帧树中GrowthSearch累计分配34,676,344B，是值得后续处理的分配热点；它与CPU主热点不是一回事。CityCoreProfile的逐线程分配探测在该Player不支持，相关列也是-1，未伪报0。完整GC停顿时长未测。

## 验证和交付

- 两次Unity Release及Development编译成功，最终版本重新构建并实际运行；未修改原Editor或替换正式启动Player。
- 七组真实窗口检查通过：逐帧步数与tick、请求/完成/欠步平衡、所有活动车辆都有对应View，结束后城市Validate通过。
- 七组结束存档冷恢复后各执行20个完整步，与热世界完整保存逐字节一致；验证在计时外。暂停组预热后完整状态不变。原有101项调度预算检查通过；PowerShell语法检查、git diff --check通过。
- 本轮未改寻路、车辆生成、经济、固定步、存档规则或生产预算，未进行鼠标施工验收，也未宣称已修复或达到50×。
- 共享必要数据：summary.csv（全部配置、实际DLL哈希、FPS/帧/欠步/内存）、frames-baseline.csv（336帧可独立核对）、core-profile.csv（完整窗口分项）、profiler-summary.csv（60帧按线程/调用路径汇总，不可跨层叠加）。
- 本地原始结果、raw、完整hierarchy/threads、截图、日志、结束存档和各Player保留在 `C:/Users/16934/Documents/unity_game/test/Temp/Speed50Analysis-20261021/`。有效组为baseline、repeat8-32、final8-cap128、budget32-cap32、budget32-cap128、paused、profile；早期失败或BuildOnly目录没有性能结论。

复用命令示例（Results必须为新目录）：

```powershell
$env:HARBOR_PLAYER_RELEASE='1'
./Tools/RunCityScenarioWindow.ps1 -Save Temp/Speed50Analysis-20261021/input.hcity -Results Temp/new-speed-window -PlayerPath Temp/new-speed-player/HarborCity.exe -Rates 50 -SharedWarmup -SavedMap -MeasureSeconds 15 -CheckRestore
python Tools/VerifyCityScenarioWindow.py Temp/new-speed-window
```

归因运行另加-CoreProfile -RawProfile -RawProfileSeconds 15 -RawFrameLimit 60，并将HARBOR_PLAYER_RELEASE设为0。预算控制仅诊断参数-BudgetMilliseconds/-MaximumSteps；默认仍8ms/32步。未来首先研究垃圾服务实际寻路的展开/重复工作，再验证道路显示合批收益；需要各自独立性能和完整状态对照，不能用少发车、暂停模拟或隐藏道路替代优化。
