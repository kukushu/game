# 当前玩家城市卡顿诊断（2026-10-24）

**当前主要卡在垃圾车去程、返程的重复全网 Dijkstra，不是道路渲染。** 正常 Play 默认没有启用此前独立的垃圾路线树实验。原生现场捕获、同一真实存档的普通 Player 和分项 Profiler 均支持这个结论。本轮没有修改或优化正式游戏逻辑。

## 现场与条件

源码/诊断基点 `cdc26e5c604765cf35e6f6275ce5b2cdd1fe248b`，当前分支 `codex/experiment-road-view-batching-20261022`。原始用户存档 `C:/Users/16934/AppData/LocalLow/DefaultCompany/test/harbor-city-core-v16.hcity`，复制至 `Temp/CurrentCityDiagnosis-20261024/input.hcity`；两者 SHA256 均为 `388a7ab4039586428ecde106f13c5918f7ab574fcfb9f2b30620fc3db913845e`，测试后原档没有变化。

当前编辑器观察到目标 50×、实际约 5～10×、约 1,150 人，车辆数量随运行变化。当前画面已有合批道路，不是此前 97,120 Renderer 的旧二进制。独立 Player 共同测量起点为 1,143 人、966 建筑、518 活动车；63,604 逻辑 Drivable 仅对应 **415 个道路 Renderer**，分区约 247,245 格 / 588 Renderer。

真实现场调用现有 `CityTrafficProfilerCapture.Capture`，运行 50×捕获 118 个有效帧，再暂停游戏捕获 120 帧；Game PlayerLoop 平均 **24.60→2.79ms**，运行最长 402.06ms。PlayerLoop 不是完整 Editor 帧，不据此计算编辑器 FPS。现场采样包含垃圾派车 PrepareRoute 单帧累计 390.13ms、外贸 PrepareRoute 累计 65.48ms、Settlement 24.84ms。

独立测试使用该存档的原地图，200 个相同固定预热步、8秒暂停显示预热，20秒正常宿主窗口；i5-14600KF / RTX3090 / Win11 / Unity6000.6.3f1 / Windows x64 Mono / 1440×900 / 全城诊断相机 / VSync0 / target60。编辑器临时暂停，避免同时运行模拟。**独立全城相机并非复制用户当前近景视角，存档冷恢复也会清空不保存的 OD 缓存；两组数据用途不同。** 预热之后仍不断产生新路线请求，不能称普通 OD 缓存已全城预建。本轮不包含加载、保存与校验耗时。

普通 Release 复用 `933898619dd83c0bb938f0f9dd75d366132d6218` 的最终 Player（之后 cdc26e5 仅文档）；Development 分项 Player 从当前基点新构建，仅增加 `CITY_CORE_ATTRIBUTION` 编译标记。二进制校验值见 [player-summary.csv](player-summary.csv)。8ms / 32步预算及 0.05秒固定步均未改变。

## 实际画面卡顿：普通 Release

| 目标速度 | FPS | 实际倍率 | P95帧 ms | P99帧 ms | 最长帧 ms |
|---|---:|---:|---:|---:|---:|
| 暂停 | 60.04 | 0 | 16.83 | 17.15 | 17.82 |
| 1× | 47.66 | 0.92× | 21.62 | 40.86 | **3189.43** |
| 50× | **12.55** | **3.60×** | 71.90 | **1786.39** | **3074.78** |

1×也会偶发长停顿，平均 FPS 不能代表顺畅。50×窗口中，约 0.73、3.07、2.42、1.79、1.57、0.99、1.05、1.57秒等长帧分布于整个20秒，并非仅第一帧冷启动；后半窗口仍有秒级停顿。3.0748秒帧的宿主 Advance 为 **3.0669秒**，道路/结构同步仅 **0.0009ms**。见 [spikes.csv](spikes.csv)、[frames.csv](frames.csv)。离线重放的缓存状态、时点和相机不同，不把3.60×冒充用户现场即时倍率。

## 哪些任务、哪些函数在卡

正常配置 Development 的完整20秒：1,351固定步，Step累计 **18,179.33ms**。下表为包含子调用的计时，**重叠项不能相加**，尤其 RouteSearch 是其他业务任务的子调用。

| 任务 / 函数链 | 累计 ms | 占Step | 含义 |
|---|---:|---:|---|
| `Network.RouteToEither`，RouteSearch | **15673.04** | **86.21%** | 4,638次实际全网搜索，平均3.38ms/次；一次业务批次可能调用多次 |
| `CityCoreWaste.Step → World.PrepareTransport → DispatchRoutes.Get → AccessRouting.Find → Network.RouteToEither` | **13707.69** | **75.40%** | 垃圾场依优先级选择目标、准备去程路线；含极便宜的每日垃圾生成检查 |
| `CitySimulation.Step` 到达结算，垃圾分支 `Waste.Arrive → PrepareServiceReturn → AccessRouting.FromLane → Network.Route` | **1546.37** | **8.51%** | 返程搜索及到达确认/卸载；不是纯经济付款耗时 |
| `CityLogisticsSystem.Step → RequestFreight → Dispatch → PrepareTransport` | **1262.74** | **6.95%** | 工厂给商店送货，反复筛选/尝试派发 |
| `CityCoreResidents.AdvanceRequests` | 768.30 | 4.23% | 居民通勤、购物请求与就业查询 |
| `World.MoveStep` | 507.96 | 2.79% | 车辆意图、移动、预约等；部分占用重建也可能被其他调用触发 |
| `CityCoreGrowth.Step` | 258.44 | 1.42% | 建筑增长，其中GrowthSearch 195.13ms / 11次 |
| 分区占用更新 `CityCoreZoning` | 48.45 | 0.27% | 2次，当前不是主要秒级卡顿来源 |
| 生产 / 外贸 / 公共设施刷新 | 60.36 / 30.12 / 10.70 | 0.33 / 0.17 / 0.06% | 正常业务仍在运行，没有删业务换取数字 |

原始 Profiler 进一步验证：119个完整帧中，垃圾派车下的 RouteSearch 单帧累计最高 **2915.46ms**，对应 Waste 总计 **2920.91ms**、Simulation.Step **2931.23ms**。这些是同帧多次调用的汇总，**不是一次 Dijkstra 本身耗时2.9秒**。结算下的 RouteSearch 同帧最高25.37ms。垃圾路线搜索能够直接解释本次秒级停顿。

## 为什么任务不多仍然这么忙

一次垃圾调度会遍历可派车基地与待收集目标。旧去程按基地/建筑对查询 OD 缓存，遇到未缓存目标仍搜索全城；返程 `AccessRouting.FromLane` 从实际车道位置搜索，没有用建筑对缓存。双向入口还可能引出多次搜索。道路网络大时，一批看起来只是“派几辆垃圾车”的业务可连续执行大量搜索。

此外，完整窗口出现 **429,142次 Admission、428,923次 RouteLookup、427,607次 CitySimulation.Dispatch**，OD命中425,867次、没有淘汰；所以不是每次尝试都在跑 Dijkstra，也没有证据说本窗是4096容量抖动。119帧的货运子链中，**150,041次 PrepareTransport**累计分配约 **109.12MB**。代码先准备交通任务，再由业务 Depart 决定能否发货，存在失败尝试产生临时对象的可能；本次没有统计每一种拒绝原因，**资金不足/占用等具体失败原因尚不能定论**。

Development 全20秒 GC recorder 总分配约 **373.50MB**，各代 collection count 都为4；RouteSearch CPU主因不等于GC主因。短 raw 中捕获的 GC.Collect 为2.61ms，不能解释2.9秒垃圾搜索，也不代表完整GC暂停总量。CoreProfile 本机线程分配探针不可用，`core-modules.csv` 的字节为-1；内存归因使用 Unity Profiler GC.Alloc。Release GC recorder不可用，同样标-1，不造数。

## 渲染是否仍是主因

暂停可稳定60FPS。Development暂停 Render Thread `DrawBuffersBatchMode` 平均 **0.407ms/帧**，运行约 **0.323ms/帧**；Main Thread `ExecuteRenderQueueJob` 约 **0.029ms/帧**。车辆显示同步整窗平均约 **0.311ms/帧**，结构同步0.085ms/帧。

渲染线程 `Semaphore.WaitForSignal` 在长帧中等到约2.93秒，是等待主线程提交的表现，**不能将它当成GPU渲染道路耗时2.93秒**。GPU计时不可用，没有宣称完成GPU内部逐Pass测量。本城当前主要矛盾已在CPU业务循环；暂停达60也不能证明所有视角、任意画质都没有GPU瓶颈。

## 单独启用垃圾树的诊断对照

只在独立 Development Player 临时设置 `DepotJunctionRoutingEnabled=true`，测量前预建96基地192树（约710ms、36,380,160字节树数组，均在计时外）。共同预热城市保存哈希均为 `14f413d92731273eeca87c62685394e004f1da4f77e2b720d7e5ca515f79d400`，相同输入与预算。正常编辑器开关没有改动。

| 相同Development宿主20秒 | 默认关闭 | 预热后临时开启 |
|---|---:|---:|
| FPS / 实际倍率 | 15.14 / 3.37× | **50.70 / 12.50×** |
| P99 / 最长帧 ms | 1512.19 / 2939.87 | **74.68 / 586.42** |
| 实际固定步 | 1351 | 5000 |
| 每步平均Step ms | 13.46 | **2.45** |
| 每步垃圾阶段 ms | 10.146 | **0.0117** |
| 每步结算 ms | 1.145 | **0.0118** |

测量期5,117次树查询全部命中，0建树/失效/淘汰/回退。总全网搜索仍有8,367次，因为执行了更多居民/货运/外贸工作；不能拿整个窗口搜索次数变多否定垃圾搜索的消除，也不能将该对照当正式 Release 优化成绩。不同路线及更多固定步导致后续状态不同，本轮没有进行新一轮全状态等价验收，不据此自动启用实验。

开启后的剩余分项为：货运Logistics39.71%、车辆Movement20.83%、Residents19.62%、Trade8.84%、Growth6.61%；其中搜索23.03%嵌套在各任务中。即使垃圾搜索改善，仍有约586ms长帧和大量分配，不能承诺切开关即可稳定50×。

## 为什么8ms预算没有挡住秒级卡顿

`CitySimulation.Advance` 在**完整固定步结束之后**才检查耗时。一步内部若连续处理一批垃圾路线，就能阻塞数秒，8ms只是合作式停止后续步的预算，不是能打断当前函数的硬实时上限。32步上限也不是当前主因：正常50×最多只完成10步/帧，分项版本最多7步，远未触顶。当前默认每步平均13.46ms，仅搜索就占绝大部分；50×需要每真实秒执行1000固定步，现有工作量自然无法达到。

## 下一步顺序与数据范围

1. 优先完成垃圾路线树进入正常Play的独立验收，重点确认路线合法性、绕路及服务效率，再决定是否启用；本轮只做诊断，没有修改默认开关。
2. 追踪货运高频失败/重复派发的具体原因，优先消除无效任务准备及路线复制；保留原优先级、安全及支付规则。
3. 进一步针对居民就业重复查询、车辆占用清空/排序和增长搜索的长帧做专项分析。不是本轮顺带优化。
4. 保留道路合批成果；当前没有证据支持再次把道路Renderer作为第一优先级。

六个 Player 窗口的账时、实际固定步、车辆显示数、暂停状态保存一致性及原生验证均通过；原档哈希不变，Assets源代码无修改，用户未提交文件保留。没有本轮算法改动，因此没有宣称新的施工/存档兼容性全面回归。编辑器恢复为可响应Play，**游戏保留暂停**，方便用户检查。

完整41分项（含本窗未发生的项目）、调用次数和Self时间见 [core-modules.csv](core-modules.csv)；295条已捕获线程/调用路径见 [function-summary.csv](function-summary.csv)；现场见 [editor-50x.csv](editor-50x.csv)、[editor-paused.csv](editor-paused.csv)。**这不是每个C#方法的自动全量Deep Profile**：现有业务分项和Unity marker以外的方法无法逐个独立归因；导出还过滤Self<0.2ms的非业务小scope。未出现的函数不代表永远无开销。

原始Profiler、日志、Player与存档在本地 `Temp/CurrentCityDiagnosis-20261024/`，三个raw的完整路径、SHA256和导出范围见 [captures.csv](captures.csv)。仅提交简报与汇总CSV，不建立新的归档框架。诊断报告最终提交以本文件Git历史为准。
