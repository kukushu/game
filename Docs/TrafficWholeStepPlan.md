# 交通整步瓶颈归因与整体优化方案

2026-10-12，审阅27b6fd8及本轮工作树。目标是显著降低完整Simulate，而非各自改善几个小函数；20ms/步与10ms量级是研究目标，不能推广为任意城市容量保证。原始42.55ms基准、CityFlow源码比较、局部索引与历史结果继续保留，见 [源码路线](CityFlowTrafficDesign.md)、[Following结果](TrafficFollowingIndex.md)。

**最重要的新结论：共享道路查询层的打包long键严重哈希聚集，主导了多个上层瓶颈。** 没有证据支持把DistanceTo或TurnLane当成本轮主要根因。应先修复所有这些函数共用的查询复杂度，再按新基准重新排序缓存、派生路线和局部索引工作，不能把旧热点预算重复相加。

本轮是归因与受控原型研究：正常游戏仍使用原道路比较器；细分诊断与哈希原型分别由编译开关启用。没有改车辆顺序、出行、时间步、数量、优先级或碰撞规则，没有引入ECS/Jobs/GPU。

## 1. 可开关的细粒度测量

`CITY_TRAFFIC_ATTRIBUTION`只在可分析环境启用，默认关闭。使用静态ProfilerMarker，保留既有父项；无动态名字、不在每个道路长度/HashSet内部比较上添加scope。DistanceTo、IsJunction、TripLane、NodeIndex和MovementInfo的scope统计真实调用；路线节点/连接检查分别采样。按固定步记录循环工作量，诊断HashSet仅用于计数重复节点，不提供任何交通决策。

Hierarchy从Movement → Move展开：

```text
Move.ChooseLane
  ChooseLane.Eligibility
  ChooseLane.LookBack / LookAhead
    Route.DistanceTo
    Static.IsJunction
    Junction.CrossingRadius
  Lanes.TurnLane / Headway / Space / Commit（原节点）
Move.EdgeAdvance → Move.LimitCrossingAdvance
  Route.DistanceTo / Static.IsJunction / Junction.CrossingRadius
  Junction.MovementInfo
    Route.NodeIndex / Route.TripLane
  Crossing.DynamicGate
    Junction.Reserve / PendingWalker / MixedConflict（实际分支）
Move.RouteValidation
  RouteValidation.Nodes
  RouteValidation.Edges
  RouteValidation.Access
```

Static.IsJunction、TripLane等也出现在其他实际父路径中，应保留路径，不能将父子Total相加。普通发布/默认开发构建不执行本轮细分scope及工作量计数。已有诊断仍保留。手动在Unity开发环境添加CITY_TRAFFIC_ATTRIBUTION可展开新节点，或加载下面脚本生成的原生录制；不需要Deep Profile。

同代码采样关/开原生配对：fixed 42.329→44.438ms、1x 42.387→44.536ms、3x 42.247→44.085ms，额外开销约4.35–5.07%；GC相同。说明细分时间含测量开销，不能把44.44当成新的无诊断基线，也不能在每层单独扣掉一个固定百分比。两侧每步任务、9轮状态、既有Calls均相同。记录在Logs/TrafficBenchmark/whole-step-attribution-off/on。

### 已确认的费用（详细采样fixed，900步）

| 节点 | Total ms/步 | Self ms/步 | Calls/步 | GC B/步 |
| --- | ---: | ---: | ---: | ---: |
| ChooseLane | 11.23 | 0.31 | 182.12 | 28101 |
| LookBack | 6.22 | 0.04 | 104.02 | 16329 |
| LookAhead | 3.39 | 0.03 | 94.54 | 11763 |
| LimitCrossingAdvance | 13.46 | 0.16 | 190.93 | 250424 |
| Static.IsJunction（所有路径） | 21.34 | 21.26 | 1703.32 | 68133 |
| Route.DistanceTo（所有路径） | 0.62 | 0.62 | 2081.70 | 0 |
| Route.TripLane（所有路径） | 3.63 | 3.63 | 1625.90 | 0 |
| MovementInfo | 1.25 | 0.05 | 201.31 | 38652 |
| Route.NodeIndex | 0.18 | 0.12 | 808.28 | 103460 |
| RouteValidation.Nodes | 0.12 | 0.12 | 187.13 | 0 |
| RouteValidation.Edges | 5.70 | 5.70 | 182.13 | 0 |
| RouteValidation.Access | 0.04左右 | 同量级 | 182.13 | 872 |
| TurnLane（原节点） | 0.43 | 0.43 | 14.27 | 0 |
| Headway / Space（原节点） | 0.39 / 0.02 | 同Total | 11.45 / 2.60 | 0 |

跨2700步按路径分析，ChooseLane中的IsJunction约9.33ms/步；LimitCrossing中的IsJunction约10.60ms/步，MovementInfo约1.11ms（其TripLane子项约1.01ms），DistanceTo约0.43ms。父Self现在被具体函数解释，不再将路口父项全部归于动态冲突或几何。动态预约、行人和物理冲突继续存在，但不是二十多ms的主因。

每步工作量（fixed平均）：1703次路口分类，只有145个不同节点（约11.7次/节点）；2082次DistanceTo重复累加6075个边；全系统10209次EdgeLength；路线有效性检查3735节点、3548连接；TripLane1626次、向前推演463个连接；Movement记录201个。静态重复确实存在，但调用次数不等于CPU占比，距离累加当前只有0.62ms。

分配来源也有区别：IsJunction使用IEnumerable的邻居枚举带来装箱枚举器（约40B/调用）；RouteNodeIndex的捕获谓词/委托、Movement对象及混合冲突几何产生临时对象。原生归因可见NodeIndex约103KB/步、分类约68KB/步。它们与各父项重叠，不能相加成额外GC；本轮未通过删除记录或改变冲突精度消除它们。

## 2. 共享道路键的复杂度问题

CityRoads.Cache把有向节点对打包为`((long)from << 32) | (uint)to`，作为vehicleConnections、roadLookup、structureLookup的键。**64位编码本身能正确区分方向，错误不是键相等性；问题是默认Int64哈希。** 当前Mono运行时实测将高低32位异或，与 [Mono Int64源码](https://github.com/mono/mono/blob/main/mcs/class/referencesource/mscorlib/system/int64.cs)一致；相邻节点ID大量产生1/3/7等相同哈希，正反向的哈希也一样。该外部源码是实现解释，本机工具与原生A/B才是当前Unity行为的验证，不凭其他运行时推测Unity。

真实存档1320边对应2640个双向查询键，仅74个不同哈希，最多1254键相同。根据同哈希组计算，均匀成功查询仅同哈希碰撞就平均需约396次比较（不同哈希取模落同桶还可能增加探测）。这不是每次真实查询都恰好396次：实际方向集合/频率/插入位置不同，分布统计是结构证据，原生A/B才是实际收益证据。

只保留按序生成的道路端点，1000段/2000键的最大同哈希组1000，10000段/20000键则10000；平均同哈希成功查询代价分别约334/3334。长城市增长时预计O(1)查询退化为长链扫描，进入大量LaneCount、CanTravel、Kind、Width和LaneOffset。IsJunction调用多次LaneCount，TripLane、几何与路由也查询这些表。

因此数据流为：

```text
打包键哈希聚集 → 道路连通/类型/车道查询退化
→ IsJunction / TripLane / RouteValidation / 转向几何 / 寻路重复支付代价
→ 同一固定步中多个看似不同的热点一起变慢
```

最坏代价应考虑O(V × 路线查询量 × 道路碰撞链长度)，不是简单称现有HashSet查询为O(1)。修正键哈希后恢复期望局部查询成本；仍有真正的路线线性遍历和全任务扫描，不能宣称解决所有规模问题。

## 3. 隔离原型与整步实测

CITY_ROAD_KEY_HASH_PROTOTYPE仅为研究开关，给上述三个表使用相同的64位混合哈希比较器；Equals始终比较完整long。保留表、键编码、插入/覆盖规则、方向、结构掩码与原查询代码。它们只被增加和查询，没有用其枚举顺序决定车辆/道路优先级；邻接List、寻路SortedSet及车辆顺序未改。

混合方案在本固定键集合得到2640个不同哈希，在20000键样本也无完全同哈希键；不能保证任意键无碰撞，取模碰撞仍由现有容器正确处理。相比“给每个热点再加缓存”，此方案直接修复所有消费者共享的数据结构复杂度，且不需要改变状态更新语义。

正式原生配对两个运行时源码hash完全相同，只开关比较器；Editor构建源仅额外加入验证入口，不进Player。两侧细分归因关闭、原有Profiler开启，固定存档、同机器、每模式3轮×300步，40步预热。以下P95/P99为每轮分位数的平均：

| 模式 | 完整步均值前→原型 ms | P95前→原型 | P99前→原型 | 均值下降 |
| --- | ---: | ---: | ---: | ---: |
| fixed | 42.711 → 7.449 | 56.470 → 10.373 | 104.035 → 13.714 | 82.56% |
| 1x | 42.633 → 10.199 | 54.646 → 15.079 | 104.285 → 17.862 | 76.08% |
| 3x | 43.097 → 7.520 | 56.127 → 10.469 | 104.857 → 13.780 | 82.55% |

各模式节奏/帧分组不同，按实测分别报告，不把fixed的7.45推广到所有模式。GC仍约807955B/步，两侧相同：本轮没有减少任务/分配，改善来自查找成本。采样开销相同，尚未测关闭所有Profiler的发布版。

fixed的主要模块变化：

| 模块 | 前→原型 ms/步 | 贡献解释 |
| --- | ---: | --- |
| ChooseLane | 10.72 → 0.69 | 约10.03ms，主要共享道路分类/车道查询 |
| RouteValidation | 5.56 → 0.24 | 约5.32ms，尚未缓存路线结果，仍检查相同连接 |
| EdgeAdvance | 19.57 → 2.14 | 约17.43ms，覆盖下面子模块，不再另加其收益 |
| ↳ LimitCrossingAdvance | 12.81 → 1.37 | 分类/车道与几何消费者一起改善；动态检查保留 |
| ↳ Following | 2.44 → 0.14 | 局部索引没改，路线车道与后续道路查询变快 |
| ResidentsAndIndustry | 4.23 → 2.52 | 共享路网查询也影响居民/生产环节 |
| Services | 1.26 → 1.25 | 基本不变，不能归功于哈希方案 |

3x实际推进1.159→2.9997模拟秒/真实秒，900步分别用38.817→15.001秒；后者平均每帧一步，Advance约7.52ms/帧。1x实际0.9961→1.0007。**这是无城市渲染/UI的Development Player；不是用户实城FPS，也不是大规模容量验收。**

2700个步序任务数、9个原生最终完整状态、所有既有模块Calls一致。没有删车、跳步或调精度。340步逐步完整JSON和累积器对照相同；正常与原型全量模型回归、10000键相等/方向/删除检查通过。原型+细分诊断的独立Unity Player通过建造、道路改动、实际换道/行人/预约/运输/原生存读及26日续跑。隐藏Player的GUI覆盖限制仍保留。

原生验收第一次研究构建遗漏既有1440×900窗口配置，一条高速/匝道鼠标输入检查失败（模型、交通与26日续跑已通过）。只将研究构建参数对齐既有验证Player后，第二次全套输入/模型/交通验收通过；未改测试阈值或交通实现，初次Failure保留在归档，最终使用V2结果。

## 4. 优化路线与20/10ms预算

1. **最优先：将已验证的道路键分布修正转为正式实现。** 本轮默认未启用，提交的是可审阅研究原型。实测已经在当前场景证明20ms目标有空间，fixed/3x进入10ms以下、1x约10.20ms；这比先缓存几个旧热点更有价值。正式启用前补充真实Unity现城与更大路网/任务规模、施工冷启动验收；保持比较器控制组与状态对照。无需多阶段更新或交通规则更改。
2. **以新基准重排整个步的预算。** 原型Movement约3.48ms、居民/工业2.52ms、服务1.25ms、调度0.20ms，不把旧21ms分类和5.7ms路线预算继续当作可叠加收益。当前RouteValidation仅0.24ms，理想全部消除也只有此上限；服务与居民成了更重要的整步部分，需分层测量再选问题。
3. **道路静态派生层与路线视图。** 后续按道路实例/revision准备精确edgeUnits、方向/车道/合流信息与节点分类；路线版本化后派生路口/合流索引，避免各消费者扫描所有几何采样点。先保持既有float计算顺序与完整失效；不建立另一份道路真相。当前收益上限不大，但可控制规模增长；记录内存、构建及施工冷缓存成本。
4. **局部交通邻域与任务生命周期。** 逐项把真正的O(V²)查询（Space的双向/入向、Headway短边、ExitClear、行人邻域及ID查找）限定到局部车道/节点，保持双占用和顺序。现城收益较小不代表大规模可忽略；在固定局部密度和高度集中拥堵两种场景测增长曲线。现有Following桶复用，不机械代入其他条件。
5. **阶段化/并行另行设计确认。** 先降低算法和共享查询成本，再判断瓶颈是否值得并行。当前单线程逐车即时提交使晚处理车看到早处理车的新位置、换道占用和预约；CityFlow的意向/Buffer阶段会改变可见状态。若未来计划并行，需明确读快照、稳定提交rank、预约串行仲裁、生命周期归并及旧语义兼容策略，先给出方案并等待确认。

固定步0.05模拟秒，要持续3x，纯模拟平均成本理论上需小于16.67ms/步；20ms是中间目标，不足以单靠CPU维持3x，更不包含渲染/UI。10ms量级留下余量，但长期大城市仍须验证。用户观察到的卡顿帧还可能有GC、冷缓存、服务事件、渲染或编辑器开销，不用本原型结果承诺全城无尖峰。

## 5. RouteValidation独立失效设计（尚未实施）

目前原函数每步检查剩余节点活跃、每个有向连接可通行、最终节点仍属于目的地入口。原型仍完整执行，证明原先5.7ms大部分是单次连接查询退化，并非只由路线长度导致。但更长路线/大量任务下O(VL)仍有必要研究。

建议先缓存**成功验证**：键包含道路实例/revision、路线修改generation、目的地/步行模式、目的地入口代数（初版可用buildingRevision粗失效）及validatedFromSegment。路线与入口不变、车辆只向前推进时，成功的剩余完整路径意味着更短后缀成功。回退到更早segment须重验。失败不简单永久缓存，车辆沿完整边推进后可越过坏前缀，失效范围也会变；不能减少原重试频率。

必须覆盖的写入：CityTraffic返程替换route、当前位置Redirect、Replan的RemoveRange/AddRange；CityRoads.ApplySplits的InsertRange与segment重定位；垃圾车满载返场改route/destination；居民/学校/医疗/服务重定向；出生/销毁、读档；拆建/升级/反向/桥隧/方向与节点坐标；建筑拆除/入口变化/城外入口。原数据公开List可同长度原位变化，只检查引用/Count不够，不能每步hash全路径来假装消除了O(L)。应先集中合法路线变更入口、维护非序列化generation，读档清空派生结果。

岗位、居民病情和目的地建筑存在性仍由Move.Prepare实时验证，不能被路线缓存跳过。道路revision已有粗失效可先保证正确，未来拆分几何/连接/控制版本须单独证明；不为减少重建而漏掉宽度、方向或入口变动。

距离优化同样谨慎：原DistanceTo以Ahead/Behind为初值逐个float累加。前缀和相减、double替代、后向逆序累加都可能改变边界。可先缓存完全相同的edgeUnits、复用相同segment/progress/generation下的查询、仅沿实际路口索引查询；前向递推若保持原加法顺序可逐点证明，后向不能直接类推。更改数值顺序需另行决策与阈值测试，本轮未改。

## 6. 重复运行与证据

```powershell
./Tools/RunTrafficBenchmark.ps1 -OutputDirectory Logs/TrafficBenchmark/new-attribution-off
./Tools/RunTrafficBenchmark.ps1 -OutputDirectory Logs/TrafficBenchmark/new-attribution-on -DetailedAttribution
./Tools/RunTrafficBenchmark.ps1 -OutputDirectory Logs/TrafficBenchmark/new-key-control
./Tools/RunTrafficBenchmark.ps1 -OutputDirectory Logs/TrafficBenchmark/new-key-prototype -RoadKeyHashPrototype
./Tools/CompareTrafficBenchmark.ps1 -Before Logs/TrafficBenchmark/new-key-control -After Logs/TrafficBenchmark/new-key-prototype
./Tools/MeasureRoadKeyDistribution.ps1
./Tools/Verify.ps1
./Tools/Verify.ps1 -RoadKeyHashPrototype
./Tools/CompareTrafficIndexReplay.ps1 -RoadKeyHashPrototype
```

不可覆盖既有结果；两侧road radius/following索引保持开启。attribution.csv逐步计数、模块与分位数、参数/源hash/控制开关、回归/重放/原生结果归档于Docs/Benchmarks/2026-10-12-WholeStep。完整调用路径和每轮raw在本机Logs/TrafficBenchmark/whole-step-attribution-*与road-key-plan-*。基准协议、环境和限制沿用 [TrafficBenchmark.md](TrafficBenchmark.md)。
