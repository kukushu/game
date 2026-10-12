# 交通模拟性能研究与分阶段改造方案

更新：2026-10-12。代码核实基线：`f81e775`，本地工作树；项目仓库 https://github.com/kukushu/game 。本轮仅研究、读取既有录制与保存文档，不修改交通运行规则、时间调度、依赖包或存档。

本文长期维护算法、证据和决策；[TrafficProfiling.md](TrafficProfiling.md) 保存标记树与历史采样；[PerformanceDiagnostics.md](PerformanceDiagnostics.md) 保存此前优化对照。文中“建议”不表示已经实现，性能目标不是性能保证。

## 实施更新（2026-10-12）

最新整步归因与受控原型研究见 [TrafficWholeStepPlan.md](TrafficWholeStepPlan.md)：DistanceTo仅约0.62ms，IsJunction约21.34ms，路线检查主要是连接查询5.70ms。确认打包long键默认哈希大量聚集，当前2640键仅74个不同哈希；修正分布的隔离原型fixed完整步42.711→7.449ms、3x实际约3.00，状态/任务/Calls及逐步重放一致。**原型默认关闭、尚未正式启用**，不是现城FPS结论。先把该共享复杂度问题作为正式优化候选，再按新成本评估派生路线、验证缓存与局部邻域；不沿用旧热点数值做叠加承诺。

CityFlow深入源码对照及首批有向边跟车索引已完成，见 [源码/阶段依赖/设计区别](CityFlowTrafficDesign.md) 和 [实现/配对结果](TrafficFollowingIndex.md)。只改变Following候选来源，不改变车辆依次提交、车距、换道、预约或固定步；Following约减少23%，完整步约减少2%，GC基本不变，3x仍未达标。随后优先继续归因ChooseLane/LimitCrossingAdvance的大Self及有效路线验证，不同时改所有模块，不直接采用CityFlow双缓冲或并行。旧单文件录制缺帧试验已排除，正式分轮数据完整归档。

已完成固定存档原生基准和仅CrossingRadius缓存的首轮优化：每侧2700步配对、状态与任务数量一致，完整固定步均值约50.6→43.6ms（下降约14%），GC约1.027→0.808MB/步（下降21.37%）。详见 [基准协议](TrafficBenchmark.md) 与 [首轮结果](TrafficRadiusCache.md)。历史研究和假设保留在下文；其中关于没有半径缓存/尚未建立基准的核实条目是f81e775时点，已被本次实现更新，不能视为最新未完成项。路线验证和全局查询等仍存在，3x实际推进仅约1.147模拟秒/真实秒，目标尚未达到。

## 1. 当前判断

当前卡顿的直接证据是主线程交通模拟耗时和分配，不足以将整座城市的全部瓶颈归因于车辆渲染，也不足以排除其他模拟、展示和编辑器开销。几百居民不等于几百车；`State.trips` 还包括步行、服务、访问/停留等交通任务。必须分别记录总任务、驾驶机动车、行人、停留任务数量。

问题由三层叠加：单步含重复道路/路口/路线计算，局部交通查询扫描全城任务，一帧又执行多个固定步。现有数据不能单独证明复杂度曲线，但代码已确认存在规模增长风险。直接迁移 ECS/GPU 或将 0.05 秒改为 0.25 秒，均不是本轮结论。

建议先做保持结果的静态几何缓存和有效路线验证复用，再引入增量局部索引；调度预算、决策降频与并行属于独立设计批次。先降低单步成本，也测量时间放大链条，不能以丢弃任务/步数掩盖卡顿。

## 2. 证据来源与历史数据

### 2.1 历史记录

用户最初截图：Movement 334.20 ms / 7 Calls / Self 325.92 ms / GC 约 3.6 MB，平均约 47.74 ms/步。最初请求描述三倍速，研究草稿称一倍速；缺少对应运行时速度记录，本文不替它确认倍速。Calls 只能证明发生了 7 次调用。

第一次细分录制：120 个真实城市 3× 帧，均 20 步，任务 118–339；中位代表帧 Movement 986.514 ms，尖峰 1624.236 ms。同期有其他构建负载和用户操作，不是严格 A/B。

最终细分录制：120 帧中 20 个有效模拟帧，18 帧为 20 步，另两帧为 1/5 步。帧 5876：165 任务、20 步、817.973 ms、GC 10142264 B；帧 5868：194 任务、20 步、994.489 ms、GC 12453776 B。原始 CSV 在 `Logs/TrafficProfilerVerified`。没有模拟的帧不能代表正常运行性能。

帧 5876 的重点节点如下；同名节点跨父路径合并，Total 包含子项，不能将父子 Total 相加。

| 节点 | Total ms | Self ms | Calls | GC B |
| --- | ---: | ---: | ---: | ---: |
| Movement | 817.973 | 0.379 | 20 | 10142264 |
| Move | 808.934 | 0.637 | 3274 | 9758336 |
| Move.EdgeAdvance | 458.730 | 22.890 | 3321 | 6682912 |
| Move.LimitCrossingAdvance | 295.201 | 211.737 | 3321 | 4603052 |
| Move.ChooseLane | 231.344 | 167.956 | 3116 | 1955488 |
| Junction.CrossingRadius | 130.384 | 124.778 | 4340 | 3719232 |
| Move.RouteValidation | 111.063 | 111.062 | 3216 | 1760 |
| Move.Following | 50.240 | 50.240 | 2154 | 0 |
| Junction.ConflictPath | 46.470 | 45.799 | 522 | 1256976 |

“800 ms”是一帧所有任务、多次固定步的累计值，不是一辆车移动一次的时间。

### 2.2 本轮读取较新的原生录制

读取文件：`ProfilerCaptures/test_2026-10-11_23-41-42.data`，571779620 B，SHA256 `4FD15C1B27C7D96B8930B46B90316F034F83360BFCEF060A57DA23DF357F9235`。

使用隔离 Unity 6000.6.3f1 编辑器的 `ProfilerDriver.LoadProfile` 加载既有文件，通过现有 Hierarchy 导出工具读取主线程数据；没有接管或推进当前城市。加载日志：`Temp/TrafficResearchRawCapture.log`，成功退出。CSV 保存于 `Logs/TrafficResearch/2026-10-12/frames.csv` 和 `samples.csv`，不会覆盖上批 Verified 数据。这是重新分析既有录制，不是重新录制或复测优化效果。

文件包含 45 帧，Movement Calls 为 6（15 帧）或 7（30 帧）。帧 0 FixedSteps=13、Movement Calls=7，存在录制边界/计数器对应异常，原因待核实；排除其数值一致性分析。其余 44 帧两者一致，任务 244–341。

| 代表帧 | 任务 | 步数 / Calls | Movement ms | Self ms | GC B | ms/步 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 有效帧排序中位代表帧 30 | 318 | 7 | 476.677 | 0.230（约） | 4580908 | 68.097 |
| 最耗时帧 9 | 337 | 7 | 557.462 | 0.240（约） | 5127544 | 79.637 |

44 帧的每帧 ms/步算术平均约 71.279 ms，范围 55.543–79.637 ms。它不是全录制按调用数加权的平均。没有健康普通帧；“中位”指常见卡顿帧，非流畅性能。

| 帧 30 重点节点 | Total ms | Self ms | Calls | GC B |
| --- | ---: | ---: | ---: | ---: |
| Move.ChooseLane | 168.86 | 123.79 | 2066 | 1137248 |
| Move.LimitCrossingAdvance | 163.14 | 132.98 | 2232 | 2128748 |
| Move.RouteValidation | 56.48 | 56.47 | 2101 | 7744 |
| Junction.CrossingRadius | 54.69 | 53.46 | 2211 | 1721808 |
| Move.Following | 46.43 | 46.43 | 1821 | 0 |

CSV 不包含实际倍速、deltaTime、unscaledDeltaTime、实时 maximumDeltaTime、CPU 型号或该帧提交 SHA，因此不根据 6/7 Calls 单独断言一倍速。数据也不能与旧批不同任务、道路、机器负载的帧作严格优劣比较。

## 3. 最新代码核实：三类结论

### 3.1 代码已确认，仍然存在

| 路径/函数 | 实际行为与规模因素 | 结论边界 |
| --- | --- | --- |
| HarborCityGame.Update → AdvanceSociety | 输入 `Time.deltaTime * speed`；HarborCitySimulationUI 直接调用 traffic.Advance | 是自定义循环，不是 Unity FixedUpdate 驱动交通 |
| CityTraffic.Advance / Simulate | 累积 remainder，每 0.05 秒调用 Simulate；每步 ToArray 按原顺序处理全部快照任务 | 没有 CPU 时间预算；先前标记未改变次数 |
| CityTraffic.RouteValid | 非提前返回任务每步检查剩余节点、连接、目的地接入 | O(L) 遍历仍重复；有效结果没有按版本复用 |
| CityTraffic.Move.Following | 每个近邻路线边均遍历 State.trips，再筛方向、状态和车道 | 每次约 O(kN)，同一步多车调用可达 O(N²k)；早退/步行/访问分支影响实际量 |
| CityLanes.LaneHeadway / LaneSpace / EntryLane | 局部距离内的每个边扫描所有任务 | 并非每车每步都执行，但调用时仍是全局扫描；短边增加 k |
| TryChooseLane | 有步行、换道、冷却、车道数早退；其后重复 DistanceTo / IsJunction / CrossingRadius | 高 Self 不能全部归因于车道空间扫描；实际 Headway/Space 比父项小得多 |
| CityTraffic.IsJunction / DistanceTo | 前者遍历邻接、部分双入口组合；后者从当前位置逐边求距离 | 同一次前后搜索重复求和可能约 O(k²)；占 Self 比例尚无单独 Marker 证明 |
| CityJunctions.CrossingRadius | 每次遍历 d 个邻接和入口×出口组合，Corner 构造 RoadNode | 约 O(d²) 静态重复计算；新 RoadNode 自带 stopFrom List 分配；没有半径缓存 |
| CityJunctions.Conflicts / Path | 按入/出节点与车道创建临时 trip、route、两组 17 点路径，再两两线段测距 | 路径采样不缓存；最多 16×16 段比较是固定采样成本，不应称全城 O(N²) |
| ReleaseJunctions / Reserve | 预约按节点组织，但反复 List.Find 查任务 ID | 约预约数×N 的查找成本；不是所有预约都互相全局比较 |
| ExitClear / WalkingEntryClear / PendingWalker | 出口边或节点附近检查仍扫描全城任务，部分查询还查路线节点/距离 | 动态占用不可永久缓存；具体成本依赖候选与路线长度 |
| CityTrafficControls.ControlAllows / HighwayGapAllows | 扫描交通任务，比较排队先后、信号、出口、冲突；高速分支查剩余路线 | 原草稿遗漏这两处，局部索引设计必须一并覆盖 |

用 N 表示全部任务，L 表示剩余路线长度，k 表示实际距离窗口中的边数，d 表示节点度。城市的道路拆分密度与路口数量同样影响成本；不能只用 N 推算性能，不能声称所有函数统一 O(N²)。

### 3.2 最近提交已经处理或部分处理

- `2da41d9` 已将车辆展示几何计算改为值类型路径；`PositionValue` 避免临时图实体。但 `CityJunctions.Path` 仍调用兼容接口 `Position` 返回 RoadNode，行人 Corner 仍分配，不能称所有几何无 GC。
- 道路节点、邻接、方向通行、道路种类/速度、结构查询已有派生索引。`CanTravel` 不是每次线性扫描全部道路，不再建议重复建立同一套道路字典。
- `AccessBuilding` 已按 roads 对象/revision 缓存入口；GetBuilding 有 ID 字典。城外入口仍创建 List，删除/不存在分支仍可能分配；有效路线重复扫描没有解决。
- 单起点、单目标 FindPath 已按 `(start,goal,walking)` 和 revision 缓存，容量 512，返回独立可变 List。不能将缓存命中称为无 GC；多端点仍走计算路径，未缓存。SignalGroups 也已有 revision 缓存，动态信号相位仍每次求值。
- Following 的自身道路几何已移到候选车辆循环外，展示集合与组件状态写入减少；State.trips 全局遍历仍存在。
- `f81e775` 完成细分 Marker、任务/步数计数器和 CSV 导出，提供证据，不是新增交通优化。现有性能测试保护缓存隔离/失效与值几何分配，不是 3000 辆扩展性基准。

### 3.3 待测量的假设

1. 真实运行时 deltaTime 是否持续被最大值截断；是否因单步昂贵而每帧一直执行 6/7 或 20 步。项目设置确认，但运行时 Time API 和模拟/墙钟差值尚未配对录制。
2. ChooseLane / LimitCrossingAdvance 的剩余 Self 是否主要来自 IsJunction、DistanceTo、WalkingLength、MixedConflicts。代码给出候选原因，不等于全部成本已有归属。
3. 实际候选扫描次数随 N、局部密度、短边长度如何增长；目前没有同网络同需求的 100→3000 曲线。
4. 细分 Marker 的完整开销。空 scope 微测不是完整城市 A/B；尤其高 Calls 和 GC 时间不可直接归因于 marker。
5. 路口几何缓存的可获收益、内存上限与冷启动尖峰；本轮没有实现缓存，不给倍数保证。
6. 当前代码没有视觉前后模拟帧插值；AnimateTraffic 使用当前进度派生坐标。降低运动频率前必须验证平滑、时间一致性，不能把现有曲线位置计算当成时间插值。

## 4. 调度放大的正确解释

`ProjectSettings/TimeManager.asset`：Maximum Allowed Timestep=0.33333334，Unity Fixed Timestep=0.02，TimeScale=1。交通自身固定步为 0.05，不使用这个 0.02。代码未找到运行时重设 maximumDeltaTime 的逻辑，但仍需读取运行时值确认。

根据自定义循环，预计步数为 `floor((oldRemainder + deltaTime * speed + epsilon) / 0.05)`。若 deltaTime 被截到约 0.333，speed=1 时常为 6/7；speed=3 时约 20。这与录制相符，尚不是完整因果实测。Advance 结束 remainder 通常不足一交通步；不能说它无限积压并补算所有真实经过的时间。

Unity 的 deltaTime 上限会使游戏时间少于真实时间，它不自动补回被截掉的墙钟时间。这个机制解释“帧中步数增加到上限”和“模拟落后”，二者应分别观察。[Unity 时间变化文档](https://docs.unity.com/zh-cn/engine/6000.6/manual/scripting/managing-time-and-frame-rate/time-handling-variations)

本轮较新录制平均约 71 ms/步，仅 Movement 按 20 步/模拟秒就需要约 1.42 秒 CPU；维持三倍速则约 4.28 秒 CPU/真实秒，未包含其他工作。它是测量下的容量估算，不是固定步开销永远恒定的证明。历史 818/20≈40.9 ms/步，也已经吃掉一倍速大部分主线程预算。

下一次诊断应同帧记录 speed、Time.timeScale、deltaTime、unscaledDeltaTime、maximumDeltaTime、固定步数、步前后 State.clock、墙钟增量、任务分类及 Advance 总耗时；统计请求/实际模拟时间和墙钟时间。采集到预分配缓冲，结束后写 CSV，避免逐车日志。

预算候选：保留所有固定步的持久欠账、帧 CPU 时间上限、低速追赶/明确模拟速度降档提示；如果请求时间持续大于处理能力，欠账会无界增长，必须说明过载行为。需要同步审查居民、服务、工业、财政是否共用模拟时钟，不能只限交通而让经济继续正常速度，也不能静默丢步。预算策略和玩法取舍应单独说明并确认后实施。

## 5. 优先架构方案：先保持串行结果

### 5.1 静态几何派生缓存

先在每个 CityRoads 实例范围缓存 IsJunction 与 CrossingRadius，按对象身份和 revision 失效，结果不进存档。第一版使用现有粗粒度 revision 即可：开/关信号也会 Changed，可能多清缓存，但正确性优先；不要一开始就重构道路版本体系。

第二个小批次缓存冲突路径和按需冲突结果。路径键必须包含 node、entry、exit、inLane、outLane；车辆和行人通行分开。冲突键由两个完整 movement 键组成。当前 Conflicts 是静态采样几何，不包含动态车辆进度，因而可复用，但必须保留退化路径返回保守冲突、采样点/精度/阈值及浮点运算结果。对称规范化须先验证原实现对称，不直接假设。

优先懒构建、有限容量、旧 revision 整体抛弃；避免每次改路预计算所有路口的全部 movement 配对，后者为 O(M²) 内存/冷启动成本。新拓扑还要正确 RestoreJunctions，缓存失效不能清除实体交通任务或改变物理预约释放。

可缓存几何、车道静态偏移、行人过街关系和信号分组。不可永久缓存信号相位、排队时间、出口占用、行人位置、预约所有者、当前动态车距。先用现有算法做 oracle，逐项比较，缓存只是复用。

### 5.2 有效路线验证复用

为每个 trip 维护非存档派生验证记录：roads 实例/revision、建筑/接入版本、destination、walking、路线身份及变更代次。道路、建筑增删/入口变更、目的地重定向、换 route、返程、读档重建均失效；路线原地 RemoveRange/AddRange 不能仅靠 List 引用发现变化。审核所有 mutation 调用点，建立显式标脏入口，再省掉稳定路线的 O(L) 扫描。

第一版只缓存有效结果，保留当前边即时检查和无效路线重试/重新寻路的原顺序与节奏。不要把 invalid 结果永久缓存导致恢复道路后一直等待。buildingRevision 现存可复用，但要审计直接修改入口坐标的路径；版本号不是自动完整的事实源。

### 5.3 局部索引分两步，暂不强制排序

第一步建立 ID→trip 字典，保留 State.trips 和快照顺序，派发/移除/失败/到达/读档全部同步维护。字典不进存档。先替换 Find 等价查询，验证快照中已移除对象的现有处理语义，不能顺便改变它。

第二步用 `(fromNode,toNode,lane)` 的有向车道桶；换道期间一个任务可占两桶，访问中任务不得占驾驶桶。初版复用 List 桶、只筛局部候选并保留原顺序；不急于按进度排序，因为顺序保留、易验证比最优理论查询更重要。对 Following 这类最小距离归约可再验证候选顺序无关。

每步开头可重建桶 O(N)，但仅此不够：Move 顺序修改状态后要立即更新桶，保持后车能看到已移动前车，与当前“边读边写”的模拟一致。车辆跨短边多次、换道开启/结束、current/next/lane/status/route 改变、任务生成/删除、快照后的新任务全部覆盖。shadow 对照检查每次候选和最终决策，旧全扫描为 oracle。

随后按节点/驶入方向维护驾驶者与行人的附近集合，覆盖 PendingWalker、WalkingEntryClear、ControlAllows、HighwayGapAllows；不能只索引 Next，因为短边下相关节点可能跨多个边，行人会在路口两侧持续占用。用现有函数的距离窗口定义邻域，避免按视觉距离替代规则。候选需保留既有队列时间/ID优先关系，不依赖 Dictionary 迭代顺序。

这些查询从“每车扫 N”趋向“每车扫局部 K”，但拥堵在同一桶时 K 仍很大，不承诺全场景 O(N)。最后才比较有序数组、带位置句柄的列表、分段桶等方案；排序/搬移也有成本。路网规则车道查询先于通用 BVH：它天然区分桥隧、方向与车道；BVH 可用于跨路线的空间查询，先证明需求再加。

### 5.4 决策频率、渲染和并行后置

路线有效验证复用属于保持结果的优化；受阻换道降频属于行为变化。前车距离、出口空间、动态行人和路口安全仍在原运动步检查。可先缓存静态目标车道，再实验错峰受阻换道，但两者需不同验收。

现有串行预约和经济事件不适合直接并行写。先提取只读道路快照、局部候选准备、独立路径构建等纯函数，测量打包/同步/归并开销，再评估 Jobs/Burst。若改为所有车统一读旧状态并提交，等价性不再成立，视为新交通模型，需先解释和确认。GPU 与混合中观暂为远期，不纳入当前实施承诺。

## 6. 外部资料核实与可迁移边界

查阅日：2026-10-12。外部实现不等于 HarborCity 设计，也不把二代、MOD 机制混进一代规则。以下用概述，不复制代码。

| 来源 | 已核实内容 | 对本项目的边界 |
| --- | --- | --- |
| [CS1 开发团队 Deep Dive（2015）](https://www.gamedeveloper.com/design/game-design-deep-dive-traffic-systems-in-i-cities-skylines-i-) | 节点/段、出发寻路与路径变化时重算、提前选道、约 4 次/秒移动模拟与前后模拟帧平滑、较低频复杂决策、活动实体限制 | 这是开发团队文章，不是全部版本的源码规范；没有披露本项目需要的缓存/桶结构。频率不能机械移植；本文不据此减少任务或采用传送消堵 |
| [How Traffic Works in Cities: Skylines](https://www.gamedeveloper.com/design/how-traffic-works-in-cities-skylines) | 非开发团队设计分析 | 作为背景链接；具体技术事实以开发团队原文和可核查实现为准，不由这篇推导原作内部算法 |
| [CS2 Traffic AI 官方说明](https://www.paradoxinteractive.com/games/cities-skylines-ii/features/traffic-ai) | 更积极换道、附近代理动态判断、异常重寻路与多核利用 | 不把二代行为作为一代复刻目标；宣传性性能表述不是我们的基准 |
| [CS2 Code Modding 官方说明](https://www.paradoxinteractive.com/games/cities-skylines-ii/modding/dev-diary-3-code-modding) | 使用 ECS/Burst；部分计算可显著加速且实现不当可能更慢 | 所称 30–40 倍针对部分计算，不代表整城或本项目会获得相同收益 |
| [SUMO Basic Definition](https://sumo.dlr.de/docs/Simulation/Basic_Definition.html) | 区分运动 step-length 与 action-step-length；决策周期与反应时间不匹配可能碰撞 | 支持研究分频，不证明当前改步长后仍安全，也不能照搬其积分方法 |
| [SUMO Road Networks](https://sumo.dlr.de/docs/Networks/SUMO_Road_Networks.html)、[Intersections](https://sumo.dlr.de/docs/Simulation/Intersections.html) | foe/response 描述连接冲突与优先关系 | 静态冲突表可借鉴；本项目动态预约、公平和行人规则不能用静态表替代 |
| [SUMO Meso](https://sumo.dlr.de/docs/Simulation/Meso.html) | 提供中观交通表示，与微观行为有差异 | 未来远距离交通研究，不替换当前可见车辆和真实运输结果 |
| [CityFlow 源仓库](https://github.com/cityflow-project/CityFlow) | 查看 roadnet.h/cpp 和 engine.cpp：Drivable 拥有车辆集合、Lane 有 Segment 索引；按 drivable 更新前车；任务阶段与屏障协调多线程；换道含 shadow 处理 | 借鉴局部组织和同步边界，不复制其车辆模型；它的分阶段提交与当前串行语义不同 |
| [OSMTrafficSim](https://github.com/maajor/OSMTrafficSim) | 源码 VehicleSystem 使用 NativeArray、BVH 感知、依赖链和 Burst MoveJob；BVHConstructor 编排构建任务 | README 的 25k车/10k行人/30FPS 是作者特定环境自述；源码中有随机换道/选段和边界重生，与真实目的地货运不等价，不能作本项目容量保证 |
| [Unity DOTS 官方示例](https://github.com/Unity-Technologies/EntityComponentSystemSamples) | 官方学习入口包含 Entities/Jobs/Burst 相关示例 | 本轮仅核查仓库入口，未执行示例性能测试；不用引入整套 ECS 作为前置条件 |
| [Unity 6000.6 时间变化](https://docs.unity.com/zh-cn/engine/6000.6/manual/scripting/managing-time-and-frame-rate/time-handling-variations) | deltaTime 上限、unscaled 时间与受限游戏时间分离 | 自定义交通循环仍须独立审计，不直接套物理 FixedUpdate 的步数规则 |

源码参考固定版本：CityFlow `81ee0f47659ca66177a71f81676691c58ee89184`，OSMTrafficSim `8d7fc0bac15f1920bcb9dba1786a87443eba3ba8`。

- CityFlow：[roadnet.h](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/roadnet/roadnet.h)、[roadnet.cpp](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/roadnet/roadnet.cpp)、[engine.cpp](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/engine/engine.cpp)。重点为 threadUpdateLeaderAndGap、nextStep 与 Drivable/Lane/Segment；链表不是本项目必须采用的容器。
- OSMTrafficSim：[VehicleSystem.cs](https://github.com/maajor/OSMTrafficSim/blob/8d7fc0bac15f1920bcb9dba1786a87443eba3ba8/Assets/Code/Traffic/VehicleSystem.cs)、[BVHConstructor.cs](https://github.com/maajor/OSMTrafficSim/blob/8d7fc0bac15f1920bcb9dba1786a87443eba3ba8/Assets/Code/BVH/BVHConstructor.cs)。这里只审阅关键系统，不代表已审计全部项目。

## 7. 可执行阶段与回退

每批单独 Git 提交、保存基线数据；先在副本重放，再真实 Unity 运行。不同批次不混入玩法或频率改变。

| 阶段 | 瓶颈与优先理由 | 改善类别/范围 | 正确性检查 | 性能测量 | 回退 |
| --- | --- | --- | --- | --- | --- |
| 0 基准与时间链核实 | 新录制仍有大量 Self；必须有相同输入和任务规模 | 先补时间/分类/候选计数，建立固定步重放和规模场景，不改变调度 | 相同存档、输入、随机状态、步数的结果一致；计数边界单测 | 1×/3×普通与尖峰，ms/步、GC/步、请求/实际模拟秒/墙钟；关闭细分 A/B | 诊断开关可撤，不影响存档/运行 |
| 1a 路口分类/半径缓存 | 高频静态、已测分配明显、变更小 | 重复计算/GC，现有 revision 失效 | 所有路口半径/分类与原函数逐项相等，桥隧/宽窄/偏斜/拆分/改路/读档 | 冷/热缓存、命中/失效、总 Movement 与 Radius，不只看节点变小 | 关闭缓存调用原函数；缓存不存档 |
| 1b 路径/冲突复用 | Conflicts 重建临时对象且结果静态 | 重复几何/GC，有限容量懒缓存 | 每个 movement 配对 oracle、退化保守结果、行人及换道边界、转弯视觉共用规则 | Path/Geometry、冷启动尖峰、内存随 junction/movement 增长 | 独立开关与提交，不改预约规则 |
| 2 有效路线验证缓存 | RouteValidation 持续高耗时、已有道路入口缓存可复用 | 重复 O(L) 扫描；保留重算时机 | 拆路/反向/升级/入口变化/目的地移除/返程/原地路线编辑/读档；有效和失效转变正确 | 验证次数/命中率、稳定帧与连续施工尖峰、长路线规模 | 失配回原验证，派生记录清空即可 |
| 3a ID 索引 | Find 反复扫描全任务，易单独验证 | 查询复杂度/闭包分配 | 所有生命周期字典与列表一致、ID不重复、读档重建、移除快照对象行为保持 | Find calls、Release/Reserve 的 Self、索引维护时间 | 查询切回列表，保留实体列表和顺序 |
| 3b 有向车道/路口局部桶 | 全局扫描是扩展性的主要结构问题，但一致性风险较高 | 候选缩减，随后按需排序/分段 | shadow 比较候选/决策与逐步状态，双车道、短边多跳、行人、合流、拥堵、公平、货物守恒 | 每次候选数、N/K/k曲线、桶维护/查询耗时、最拥堵桶，100→3000 | 查询开关切回扫描；索引非真相源，不进存档 |
| 4 决策/渲染/调度独立设计 | 先证明单步已缩短，再解决频率与过载取舍 | 调度/决策频率/平滑，可能改变行为，先说明并确认 | 驾驶反应/通行量/队列/事故/任务时间，全部系统共用时钟；不得静默丢任务/步数 | 同模拟秒总耗时、墙钟落后、响应时间与流量，不能只比 FPS | 保留原0.05串行模式，参数/模型批次独立 |
| 5 数据批处理与 Jobs/Burst | 仍达不到规模目标且证据支持并行时才进入 | 纯函数并行、数据布局；暂不 GPU/全面 ECS | 串行 oracle、依赖/竞态、确定性、生命周期与存档；二阶段语义另审 | 含数据拷贝、同步、归并的端到端收益，1/多线程对照 | 保留串行入口，不绑定存档到 ECS 数据 |

如果任一阶段目标节点下降但总单步时间/分配不降，暂停扩展，检查成本转移、维护开销、内存膨胀或采样影响，不以“增加缓存/字典”作为完成标准。

## 8. 规模、正确性与性能验收

规模阶梯建议为 100、300、1000、3000 活动机动车，另记录/混合行人和服务任务；3000 是目标场景，不是已支持的声明。不得复制重叠车辆制造数量、减少真车、切换装饰对象或把访问任务全算车辆。测试应有真实合理派发与入口空间，记录需求拒绝/等待数，不让场景在测试期间自然消失成小样本。

至少两类扩展曲线：相似局部密度、扩大路网的分散车流；同路网逐渐增压的拥堵车流。覆盖无灯/信号/停车牌、四车道换道、匝道合流、偏斜路口、桥隧上下重叠、短边密集、跨多个边、行人混行和施工。记录路边数/路口数、路线长度分布、桶最大密度，否则 N 的增长不可比较。

先固定种子、城市快照、输入序列与精确交通步数，充分预热后录制至少 1000 步；重复 3 轮，保留冷启动/施工尖峰单独统计。现有 PerformanceProbe 会连续推进城市，交通规模会变且包含服务/居民，它可作副本合法性诊断，但不能独自证明固定 N 的性能曲线。应新增固定场景重放基准，不把旧脚本的平均当成最新 FPS。

报告：Movement/完整 Advance 的 p50/p95/p99/max ms/步、GC B/步、GC暂停；每帧 Calls/任务分类；每模拟秒 CPU 和 wall time；缓存命中/失效/内存；候选与几何调用数。CPU/GPU、Editor/Development/Release、细分标记开关分开。稳定场景与发生事件的分配分开，不强求所有派发/寻路事件零分配。

容量估算：若保持 0.05 秒步长，3×请求约60交通步/真实秒。60FPS下平均约一步/帧，整帧16.67ms需同时容纳模拟、展示、UI与渲染。建议初始工程目标为指定参考机器3000活动机动车时 Movement p95≤8ms/步，热稳态移动查询尽量0B分配；这是待用基准验证的目标，尚未达成，也不自动保证整帧60FPS。确定机器后记录CPU、内存、构建方式与同城负载，再修订预算；不得用修改速度或任务量“达标”。

保持算法的批次要比较每步 route/segment/progress/lane/换道状态、预约、等待原因、blocked、clock/remainder、完成/失败任务以及居民/货物/垃圾守恒。优先要求原精度下结果一致，浮点容差必须有明确原因，不能用大容差隐藏碰撞或死锁。包含原生JSON存读后重放、长时间运行与现有交通/行人/合流/服务/货运/存档回归；实际游玩验证车辆不停顿/瞬移、道路改动和交通反馈，测试通过不等于复刻完成。

## 9. 本轮交付与下一批边界

已完成最新本地代码核实、外部第一手资料/关键源码核对、较新原生录制读取、研究和阶段方案保存。没有修改模型，所以本轮没有重新跑整套玩法回归或宣称优化收益；此前通过记录见 TrafficProfiling.md，隔离Unity本次加载/导出已验证成功。

本地 Packages 的编辑器插件更新与 ProfilerCaptures 原始文件均为既有工作树内容，本次保留、不纳入文档提交。原始录制约572MB，CSV为派生分析记录，不直接将大文件加入Git。

下一批推荐阶段0配对基准和阶段1a最小缓存试验，各自独立提交；此方案尚未实施。居民人格/人生/复杂家庭经济、美术扩展、全系统重写均不在本研究范围。每次实施后在本文补充提交SHA、数据、失效覆盖及结论，将假设更新为已证实或否定；不覆盖历史证据。
