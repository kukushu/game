# HarborCity 交接给 Codex：项目现状、开发约束与性能工作全记录

交接日期：2026-10-17。本文不依赖 Work 对话历史。主要产品代码与证据基线：`codex/road-core-20261014` 的 `08a89a4`（占用索引代码为 `1c374c4`）。本文及资源清单随后作为独立文档提交；查看 `git log -1` 获取交接提交本身，避免在正文编造自引用SHA。

## 0. 交接状态：现在必须停下来

用户最新命令是**暂停所有性能分析、优化、规模扩展及新代码改造，完成交接后等待**。过去“连续运行4～6小时”“直到用量耗尽”“自动进入下一阶段”等授权不应被新会话当作当前任务。本文的运行命令和下一步建议仅供未来获得恢复指令后使用；交接过程没有执行它们。

交接前工作区干净，开发分支与远程一致；上一轮有效改动已提交、推送。本次仅新增交接文档和资源清单、更新 AGENTS 阅读入口。检查未发现 HarborCity / Unity / mono 测试进程。两个 dotnet 进程分别是 VS Code 的 UnityCodeModel 与 ProjectSystem.BuildHost，不是城市仿真，不能误杀。用户存档、原始Profiler、失败现场、stash及历史工作区均未删除。

**新会话建议先读：**

1. 本文及仓库根 `AGENTS.md`，以最新指令覆盖旧里程碑。
2. [最新批次与经验收尾](../Architecture/PerformanceReview20261017.md)。
3. [交互式架构图](../Architecture/HarborCityArchitecture.html)：全局数据所有权、核心类关系、模拟流程；页面源码固定至 `1c374c4`，是审查定位基线，不代表页面中的历史提案全都已经实现。
4. [可玩性实际验收](../Architecture/R3Usability.md)、[产品路线](../ProductRoadmap.md)、[CS1进度](../CS1ImplementationProgress.md)。后两者有历史内容，当前状态必须再对照源码，不能因为文档写过就认定功能已接新链。
5. 如恢复性能工作，再按本文链接读各配对报告，不从零重做已证实的归因。

## 1. 用户长期要求与工作方式

### 产品目标和范围

- 优先接近《都市：天际线》**一代原版**的建设经营体验：修路、沿路分区、建筑生长、居民就业通勤、工业商业、服务财政、可保存继续经营。暂不开发原创人格、人生故事、复杂家庭金融。画风、美术提升暂后置，功能性显示与交互必须可用。
- 曾讨论 TM:PE、修路MOD与DLC，但不能由此把各代、DLC、MOD规则混入当前原版基准。原作不确定规则标为近似或待核实，查可靠资料；不要用自创规则静默替代。
- 用户明确例外：垃圾处理设施应持续处理运回垃圾（焚烧式意图），而不是永远填满；曾要求废弃恢复由30天改7天。检查当前新链是否落实，不要因重构默默丢掉要求。
- 空白地图起步；玩家沿路划用途，由建筑生长，而非把政府逐栋建住宅收租作为不可改变约束。居民、岗位、车辆、库存必须真实关联，不允许装饰车或虚假人口冒充规模。
- 当前三万人真实体验尚未达到稳定3×。五万、十万人是独立规模目标；过去后台可运行不代表完整城市经营健康或Unity流畅。

### 架构与数据约束

- 高内聚、低耦合。道路管拓扑及共享几何，交通管物理运输，人口管活动，经济管生产交易，市政管供给，表现只读显示。不要让 CityModel、World 或宿主继续成为万能对象，也不要仅为消除 partial 或美化类图大重写。
- 模拟与表现分离；Unity Update 可以调度，但 GameObject、Transform、Mesh、动画、摄像机可见性和插值不能成为模拟事实。UI/工具经命令修改，不从OnGUI或观察查询推进模拟。
- 统一0.05秒固定步；完整执行每步，所有未处理时间留在SimulationClock。当前正常宿主每帧最多4步/8ms合作预算，单步不可中断，因此仍能产生长帧。暂停时不能调用Advance(0)清偿旧债。不能静默丢步或把实际1.45×叫3×达标。
- 每种事实一个所有者。职位、居民、家庭、车辆、任务、Shipment、库存、余额、合同不能有多份可独立写的真相。派生索引/缓存要说明失效和重建；跨域扣钱、发货、任务创建、到达必须预检/提交/失败一致，不重复交付。
- 游戏设计数据按领域强类型组织，位置见 `Assets/Resources/GameData`；领域读取不可变 `CityGameDesign`，保存冻结配置版本和规则。新可调价格/工资/生产/消费/服务参数不散落硬编码，也不建一个万能全球数据类。
- 玩家可见文字用稳定本地化Key，`Localization/zh-CN.json` 与 `en.json` 参数保持一致。旧文案随功能迁移，不因为集中数据暂停产品做全量重写。
- 用户已明确允许**不兼容旧版本存档**，不必开发迁移器或保留完整旧引擎回退；遇不支持格式应明确拒绝。动态施工的一致性和本阶段新存档可靠性仍是硬要求，不能借“不兼容”瞬移车、丢货、改入口。
- 新系统不应永久依附旧短RoadEdge包装层，更不允许接回旧CityTraffic作为第二运行交通。已经有价值的算法、哈希、安全约束应保留。
- 目前单线程模拟；不要未归因就引入多线程、ECS、Jobs、Burst或GPU。

### 实际开发与沟通

- 一批处理一个可验证问题，真实调用链接入后及时删被替代层。分批提交并推送当前开发分支，不每小步等确认；重大新经济/玩法决策、不可逆操作需用户决定。
- 每批报告修改模块与必要性、唯一事实/事务、模拟与视图边界、真实复杂度/分配、回归与性能、风险、提交SHA。HTML重要关系图需跟随核心变化。
- 实测→归因→局部方案→配对→提交；不能用编译通过代替游玩，不以类数量或代码量衡量成果。守恒通过不等于经营健康。
- 持续工作期间定期说明看到了什么、假设是什么、下一步验证什么，避免长时间黑盒。用户乐于帮助点界面或观察，但不要反复索取已有授权。
- 用户允许修改测试地图；不代表允许删除其个人存档或重要采样。真实鼠标验收、程序化Unity集成、后台模型回归要分别报告。程序调用Construction不能声称鼠标交互已验。
- Windows防火墙偶尔挡住本地Player，需人工关闭；不用联网也能本地测，不能假设弹窗已点。不要尝试自动操作安全中心。C盘曾不足，长测试前先看资源。

## 2. Git与工作区现状

仓库本机：`C:/Users/16934/Documents/unity_game/test`；远程：<https://github.com/kukushu/game>。

| 项目 | 交接检查值 | 注意 |
|---|---|---|
| 当前分支 | codex/road-core-20261014 | 继续在此开发；不擅自merge到main |
| 文档前HEAD | 08a89a4 | 已推送origin同名分支 |
| 最新正式优化 | 1c374c4 | 占用索引复用、失效与测试 |
| main / baseline-road-core | 110c4ea | 历史基线，**不是**当前完整开发状态 |
| backup-before-profiler-cleanup | 9cff941 | 历史备份，不删除、不复活旧引擎 |
| 旧工作区 | C:/Users/16934/.codex/worktrees/city-scenarios-stage2/test，detached facea1d | 历史阶段，不作为最新源；未修改或清理 |

本地还有3份stash：一个恢复垃圾诊断的recovery，两个737ee35时期WIP。它们不是“当前漏交的修改”。不要自动apply/drop；用户曾误切历史分支再apply而产生modify/delete冲突。先确认分支/源码/真实差异再处理，避免把旧内容覆盖新实现。

没有强推，没有改main。之前Profiler大文件曾阻塞push，仓库已有清理历史记录 `Docs/GitHistoryCleanup.md`。不要再次提交Temp、完整保存、Player、Library或大Profiler。必要证据仅轻量CSV/JSON/文字与有价值截图。

## 3. 当前架构：代码阅读导航与唯一事实

代码分为 `Assets/Scripts/Runtime/{Infrastructure,Traffic,Simulation,GameData,Observability,Presentation}`，诊断在 `Assets/Scripts/Diagnostics`，离线宿主和测试在 `Tools`。`Docs/CodeLayout.md`有目录说明。

| 模块/关键结构 | 实际责任与位置 | 不能做什么 |
|---|---|---|
| CitySimulation / SimulationClock | Simulation/Core/CitySimulation.cs、CitySimulationClock.cs；唯一tick、秒数、欠步、领域执行顺序和到达协调 | 不把所有业务规则塞回调度器；渲染不能改时间 |
| CityEntities | Simulation/Core/CityEntities.cs；建筑/家庭/居民/岗位/外部端点结构生命周期，索引引用原对象 | public旧列表不是支持的写入API，结构变化须健康步边界 |
| CityModel | 城市保存/领域事实历史根，仍有partial与旧功能代码 | 不能因文件还在就认定旧系统参与当前模拟；新增不继续膨胀 |
| RoadCore道路/Network | Infrastructure/Roads/CityRoadCoreNetwork.cs及同目录几何/施工/保存；道路有稳定身份，几何采样不等于寻路节点，车道/连接从路网派生 | 不让Mesh拥有共享几何；不永久展开回旧短边Move |
| Lane / LaneConnection / JunctionPort | 实际通行区间和真实端口曲线；用于位置、连接冲突、渲染 | 不把路口连接仅当视觉曲线或忽略车身占用 |
| World | Traffic/CityRoadCoreWorld.cs；道路与交通状态、准入、物理移动、到达缓冲 | 不拥有工厂采购/工资/消费/库存规则；仍承担部分施工与准入协调，是局部边界风险 |
| VehiclePosition | Traffic/CityVehiclePosition.cs；唯一物理Drivable、沿行驶方向纵向车身中心距离、冻结点 | Current不能反由RoutePlan推导；Transform不写回 |
| Vehicle / RoutePlan | World内Vehicle引用Position、Task、Plan；CityTripTask.cs内RoutePlan持未来有序drivable与终点距离，cursor是导航锚点 | 任务ID不是车辆ID；路线不是第二份物理位置 |
| TripTask / Shipment | Traffic/CityTripTask.cs；WorldData.tasks拥有独立任务；Shipment独占在途货量/种类/交易单价 | 不将货物复制在车与工厂两处；反向Task→Vehicle索引可重建 |
| RoadAccess / DispatchRoutes | 建筑门口到合法有向车道/纵向距离关联；有限LRU蓝图/标量报价 | 不缓存瞬时交通当静态自由流；不共享可编辑路线List |
| 居民/家庭/CityJob | Simulation/Residents；Household持成员住房储蓄偏好，Citizen持技能岗位工资实际通勤；岗位是真实实体 | 不以家庭数量近似就业，宏观人口/就业从实体汇总 |
| 生产 / Logistics / Trade / Payroll / Consumption | Simulation/Industry、Economy及对应领域模块；真实到岗劳动、库存加工、原料进口/成品运输、付薪零售 | 交通不直接修改企业财务；不得凭空加库存补钱掩盖经营问题 |
| Utilities / Waste / Finance / Growth | Simulation/Services、Economy、Growth；真实供给管线、垃圾任务/处理、税维护贷款、沿路用途生长 | 配置不是运行余额；分区网格是土地规划派生，不复活旧tile建筑库 |
| Construction | Simulation/Core/CityCoreConstruction.cs与Infrastructure道路Plan；预检、合法方案、成功提交扣费、入口/车位迁移 | 预览只读，失败完整事实不变；端口覆盖在途车不可安全迁移时拒绝 |
| Presentation | HarborCityRoadCoreGame组合宿主、BuildingViews/VehicleViews/ZoningViews/OutsideViews、UI观察工具 | GameObject与材质是视图自有/借用资源，不得驱动人口和业务 |

### 一步执行顺序（以源码为准）

`Advance`先申请真实时间，健康边界刷新道路绑定→Growth→Utilities→Population；随后居民出行请求→维护计提→实际劳动生产→消费→成品物流→外部贸易→垃圾调度→World.MoveStep（占用、换道、意图/预约、物理提交）→到达业务确认/ACK/退休；完整步提交时钟后垃圾日产生、工资日结、财政日结。失败进入Fault，不能半步继续装作正常。

路由Network是静态路网的派生查询，编辑后替换；RouteScratch单线程同步借用，不进存档，结果列表独立。车道占用/影子/预约由World维护；本批仅在indexDirty时重建，派车成功在干净桶中有序插入。端点实际变化、物理变化、退休、道路编辑/恢复都要维护失效。不能新增一个“读查询”意外刷新世界事实。

当前新链保存由CityCoreSave/CityRoadCoreSave明确写完整领域、时钟与在途状态，业务version8/核心format15（城市外层16；后续改动以实际写入器守卫为准）。不要把旧CitySaveData的历史版本兼容分支套到新格式。配置快照随城市保存；缓存和Transform不保存。

## 4. 这一轮发现的瓶颈与有效改动

**以下是分批独立测量，基线、窗口、运行时不同，百分比不能相加或连乘成总体承诺。** 原始细节和环境以各报告为准；所有列为已提交正式默认实现，除表中明确标历史者。

| 提交 | 根因、改动 | 已测收益与限制 | 报告 |
|---|---|---|---|
| 262adb0（早期历史） | 打包long道路键默认XOR哈希严重冲突；复用混合哈希比较器，相等性不变 | 当时旧完整步约42.8→7.7ms，约82%；**旧城旧引擎历史，不是今天30k成绩** | Docs/RoadHashProduction.md |
| 7502947 / 1c9fb15 | 路线缓存容量到顶全清；垃圾车堵源方向仍反复准备 | 单项LRU淘汰、只在全部合法可达方向真堵时便宜拒绝；3000人约0.795→0.719ms，非30k成绩 | ScaleChallengeResults |
| 941b602 | 30k工作组超过4096导致每步缓存抖动 | 每实际查询居民的自由流秒数派生缓存，按地址/岗位/Network/Access失效；解决每步重复，但冷峰仍在 | ScaleChallengeResults |
| 16d34ea | Unity无限while补全部欠步，使单帧执行近千步 | 每帧4步/8ms合作预算保留所有债务；改善响应，**非计算提速**，一个长步仍阻塞 | CityWindow30000 |
| c0de4e6 | Growth需求住宅×家庭、商业×岗位重复扫描 | 局部线性汇总，原公式/筛选及240步全状态一致 | CityWindow30000 |
| 156737e | 每次路由重建距离/前驱/访问/堆工作空间 | 单线程RouteScratch复用，冷峰分配454→9MB；Mono1.709→1.507秒，原生1.270→0.880秒，计算仍贵 | CityWindow30000 |
| 14650d8 | 仅求自由流秒数仍生成完整路径蓝图，四次搜索 | 多源/多目标最小秒数报价，按需生成实际路线；4801归因峰1498→357ms，但普通整日仅18.439→18.198ms（约1.31%） | CommuteOptimization20261017 |
| 559a5df | 90733独立分区Renderer，暂停也仅23FPS | 保留全部顶点/三角形、按空间与材质合为256批，像素0差；暂停23.30→59.99FPS，1×19.94→38.04FPS | ZoningViewBatching20261017 |
| 0444e58 | 每出发方向分别搜索两个到达方向，总4次 | 规范双目标搜索4→2，稳定同分与原路径保持；整日ABBA17.767→15.335ms，约13.69%，分配约-2.12%；6751局部1343→394ms | TwoGoalRouting20261017 |
| 9443d0c | 施工/分区反复采样同静态道路投影 | 每Network缓存段裁剪端点；整日单对约16.022→15.498ms，冷峰1957→1458ms；**冷分配+29.46MB、整日分配+6.06%** | SpatialProjectionCache20261017 |
| 6027a0d | 集中返家产生约1989个新车辆显示对象 | 退役视图供同帧新ID复用，最多4096闲置，不限制真实活动车显示；6751Views34.394→12.899ms，组件创建5967→0；普通FPS没有可靠显著收益 | VehicleViewPooling20261017 |
| 1c374c4 | 派车维护索引后MoveStep无条件重建同状态 | 仅脏时重建并补端点失效，ABBA均15.773→13.914ms，约11.8%，分配214906→166681B/步；P99及1.5秒冷峰未解决 | PerformanceReview20261017 |

更早已验证的岗位安全几何下界、分区占用桶、管线DDA覆盖、车道SCC可达性也需保留：`737ee35`、`facea1d`、`43eafc1`、`b7c73c2`。不能因看到旧类就重复开发；SCC只是可达性，不是最短路，跨分量缓存也不是所有查询O(1)。

### 11524次miss的根本结论

不是每步错误清空缓存，也不是住址/岗位/路网/入口无故变版。集中出行时11524个个人估计均首次查询，对应3511组住宅/工作；大量跨居民共享已经由原LRU命中完成。3495次蓝图准备键都不同，只34条曾经驱逐后重查；实际准入839次远少于估计。不能把11524当独立Dijkstra次数。解决办法是报价与构造路径分离、共享静态搜索，不是盲扩LRU、再加一层个人/家庭缓存。

### 长帧不是一个问题

- tick4441：恢复后冷分区/施工合法性、空间查询及几何构造，原约1.9秒/204MB，缓存后约1.5秒仍在；不是通勤缓存问题。
- tick4801：首次集中工作估计与实际派车，标量报价已大幅降低估计成本；保留全部实际出勤计算。
- tick6751：真实返家，4866次准入、2226新蓝图，绝大多数实际发车；新增提前源占用预检仅可能避开9/2226条新路线（约0.4%），不能解决主要峰。
- 真实raw帧6751旧显示版本主线程314.8ms、模拟261.2ms、PrepareRoute237.2ms/2226次；GC约3ms不能解释237ms路线计算。
- 车辆视图池后同类帧约298.7ms，Views降至12.9ms，但模拟约265ms，因此表现优化不能代替集中出行优化。

## 5. 失败或未推广实验：不要重新启用

| 实验/误区 | 证据与处理 |
|---|---|
| 居民派车前普遍双方向占用预检 | 早期虽同状态，但Admission Self/BFS大增，撤回；返家观察也仅9条必要新路线可避。服务派车已有独立有效预检，不能混为一谈 |
| 盲增4096 LRU | P0显示主要是必要的首次查询，不构成有效方案，未推广 |
| SAT轴临时数组去除 | 50万矩形判断相同，热调用3.2MB→0B；整日15.388→15.632ms，分配仅约-1%；冷步只减少5.18MB，不是130MB来源。产品CityBuildings已恢复原实现 |
| 空间段跨桶去重stamp | 9600查询/194860有序命中及40步全状态相同，但查询632与正式639ms接近，无可靠增益；仅Temp原型 |
| 居民局部Job查询合并 | ABBA原16.1791、新16.2795ms，分配相同，完整状态相同；没有可靠整城收益，未进产品 |
| 关闭分区绘制 | 仅归因消融，虽23→60FPS但删画面不是优化；正式用保留完整几何的合批替代 |
| 诊断误用/失败 | 一次“暂停”被GUI输入改速，实际推进1208步，已判无效；后续测试flag锁输入并记录actual_rate。部分Recorder0不受支持，改记-1。整段raw2GB导出超内存失败，改为60关键帧有界采样，原件保留 |

实验文件均在Temp，没有运行时第二引擎。源码覆盖工具参数只用于配对隔离，默认正常源码。应保留负结果，避免下次见“零分配”再走同样弯路。

## 6. 当前真实三万人窗口性能

最新有效窗口：`Temp/CityScenarios/window30k-index-reuse-flow`。相同threshold输入，Unity Development Mono Player，1440×900，VSync0、目标60，8秒预热+60秒采样，未录raw。单线程正常宿主，所有欠步保留。

| 请求速度 | FPS | 实际模拟秒/真实秒 | P95帧 | P99帧 | 最大帧 | 分配B/帧 | 实际执行步 |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1× | 42.24 | 0.99971× | 35.26ms | 43.46ms | 206.88ms | 133444 | 1200 |
| 3× | 29.01 | 1.45053× | 42.35ms | 77.93ms | 206.92ms | 236419 | 1741 |

1×测量tick4566→5766，3×4633→6374；3×欠步继续增长，绝不是3倍达标。两个窗口暖/冷恢复20步完整状态相同，显示车辆数逐帧等于真实活动车。不同3×实现推进到不同tick，不能将窗口FPS增长直接当同工作量模拟提速。

同输入正常后台整日2400步：原两次均值15.9044/15.6407，新13.7633/14.0653ms；P99约59～60ms，最大1496～1519ms，全部终态SHA相同。后台数据不含完整Unity表现，不能拿13.9ms推算流畅FPS。

最后一次 `window30k-index-scope-raw`：编译并启动成功、Player退出0，但缺summary/有效raw报告，监督脚本退出1，导出没有执行。新的原生Occupancy.Clear/Actors/Sort节点**编译通过但实际Profiler展示尚未验收**。停止指令后没有追查。现有正常窗口验证针对正式算法；不要把这次缺报告当成功。

## 7. 当前未解决事项（按体验优先）

1. **1×仍有可见约207ms长帧，3×仅约1.45×实际速度。** 集中实际路线和冷分区都是已证实热点；恢复后若优化，仍应单问题、同存档配对，不再无限扩独立交通测试。
2. **冷恢复/动态建设尖峰。** tick4441约1.5秒，分区几何、合法性、字符串Key和候选构造有大分配；本次并未消除。显示合批目前结构变化仍整批替换，也非完全增量。
3. **真实派车成本与每步常态成本。** 多条必要新路线仍要搜索，暖跑占用排序约1ms/约52KB仍存在；细分归因自身开销大，不能据采样版Self直接决定新架构。
4. **大量道路/管线和车辆视图。** 该城22572道路、16441管线、全部实车仍逐个同步。车辆池只降低出生峰，不是GPU实例化，也没证明常驻内存降低。相机移动/建设视图尖峰还需专门验收。
5. **经营健康不达标。** 垃圾运输吞吐、企业现金自锁、货运距离/日长不匹配，不能以守恒通过宣称城市健康。新的经济规则必须用户决策；当前不得补钱/加库存/暗改日长掩盖。
6. **可靠性/诊断。** 历史曾有Unity原生退出崩溃，近期未复现不代表根因修复；末次raw报告缺失未查。GPU、部分计数器不可用要报未知；不要自动归因GC。
7. **产品完整度与扩展性。** 新链信号/行人/完整公共交通、高级管理等仍有未接项，旧源码存在不等于恢复；详见路线和架构图。新的五万/十万人窗口未验收，不应先扩大人口掩盖三万人体验。

十万人背景证据：100006真实居民、8334工厂、活动车峰27317；原12货车都最终交付，非永久死锁。单进口4在途名额，自由流151～191秒，而一天120秒。多入口同城1/4/12口一日原料48/88/169（含共同carry-in32），仍按端点顺序选，不是最近；生产22/33/46，到店0/0/6，消费0。某些店现金自锁，但不是所有店；有货晚到且有现金店尚未验次日销售。布局收益不称算法收益。该批没有允许凭空补贴；见ImportEndpointExperiment、LogisticsAttribution100000。未来继续本地匹配前先重新核对最新实现与用户授权，不重复已做实验。

## 8. 测试环境与证据规则

- Windows 11（记录为NT10.0.26200），i5-14600KF，约32GB内存；Unity `6000.6.3f1 (45d8eee7de74)`。实际每轮环境在environment.json，GPU耗时本轮不可用，不填0。
- 主要配对后台宿主为Unity自带Mono；大规模有显式.NET64。x86 Mono曾在约1.5GB恢复副本OOM，不能称模型上限。Mono与.NET长期浮点状态曾不同，不能跨运行时当等价配对。
- i5混合核心调度有噪声；后续主要后台ABBA固定逻辑CPU0（AffinityMask1），正式Unity窗口不绑核。每轮使用同保存、同runtime、相同固定步和业务，不并发Player、raw导出或其他大型测试。
- `CITY_CORE_ATTRIBUTION`是额外归因构建，不是普通速度；正常基准必须-NoAttribution。高频Job等作用域会显著增加开销。Profiler原始录制与普通FPS分开。
- 峰值报告必须含平均/P95/P99/max、GC分配/可用性、实际步数与倍率、真实活动数。Unity非分代GC三个计数不相加，GC.Alloc不是GC暂停；跨线程等待不加进主线程预算。
- 债务核对：请求时间=已完成步×0.05+剩余欠步（含初始）；暂停0步且全保存不变。按相同tick比较业务，墙钟分数欠步仅可在独立比较DTO归一化，不能改活状态或原保存。
- 全状态相同仍不足以证明所有玩法正确，要保留方向/间距/换道/路口/施工/库存资金守恒断言。新版模型有意变规则要明确，不强求旧浮点序列，但纯优化继续严格对照。

## 9. 固定三万人输入、复现工具及命令（现在不要执行）

### 必须保留的固定输入

`Temp/CityScenarios/challenge30000-traffic10000/threshold.hcity`

- 38095644B，SHA256 `e78c47daaffcb989ffc57eb0048575c3b6aa6dc66386dd6689c9c8eacd5db7d8`。
- tick4406，30000居民、7806建筑、10887真实活动车；这是已运行到繁忙阈值的保存，不是仅初始化城市，也不是纯交通夹具。
- 同输入2400步正常终态SHA：`2596b3c6f2c80fdca74941df38049ae8d519eeb5ae95a7e0b9f344e2a9a59f27`；本机可核对 `reuse-index-v2-day-b2/final.hcity`。冷/暖/集中出发/实际合法分路的观察对照终态为 `f2386908d5501481c597b54c8340a1a8ecfe07fdc7cf45473046ae8ca64789d3`。
- 输入本身被Temp忽略，**GitHub clone并不会带来它**。换电脑先请用户复制该文件及需要的原始证据，不重新随机生成后声称同基线。当前本机使用Codex可直接读取。

### 后续手动选择运行的例子

以下仅为操作手册。本次交接没有运行，也没有安排自动任务。所有结果目录必须全新，替换`handoff-example-*`后缀避免覆盖。

```powershell
# 先核实路径、版本、当前分支；不要盲目stash apply
Set-Location C:/Users/16934/Documents/unity_game/test
git status --short
git branch --show-current
Get-FileHash Temp/CityScenarios/challenge30000-traffic10000/threshold.hcity -Algorithm SHA256

# 正常后台整日2400步，禁止并发其他性能测试
& ./Tools/MeasureCitySteps.ps1 `
  -Save Temp/CityScenarios/challenge30000-traffic10000/threshold.hcity `
  -Results Temp/CityScenarios/handoff-example-day `
  -Steps 2400 -TimeoutSeconds 300 -Runtime Mono -NoAttribution -AffinityMask 1

# 冷恢复、预热、集中出行和实际合法分路归因；不是普通性能
& ./Tools/AttributeCommuteQueries.ps1 `
  -Save Temp/CityScenarios/challenge30000-traffic10000/threshold.hcity `
  -Results Temp/CityScenarios/handoff-example-commute -TimeoutSeconds 300

# 实际窗口1×/3×，正常重新构建，8秒预热+60秒记录并校验恢复
& ./Tools/RunCityScenarioWindow.ps1 `
  -Save Temp/CityScenarios/challenge30000-traffic10000/threshold.hcity `
  -Results Temp/CityScenarios/handoff-example-window `
  -PlayerPath Temp/ManualCityPlayer/HarborCity.exe -Rates @(1,3) `
  -CheckRestore -TimeoutSeconds 180 -MaxMemoryGB 8
python Tools/VerifyCityScenarioWindow.py Temp/CityScenarios/handoff-example-window

# 只在需要时采关键帧，raw有明显自身开销；勿先录整段几个GB
& ./Tools/RunCityScenarioWindow.ps1 `
  -Save Temp/CityScenarios/challenge30000-traffic10000/threshold.hcity `
  -Results Temp/CityScenarios/handoff-example-raw `
  -PlayerPath Temp/ManualCityPlayer/HarborCity.exe -Rates @(3) `
  -RawProfile -RawProfileSeconds 120 -RawStartTick 6748 -RawFrameLimit 60 `
  -CheckRestore -TimeoutSeconds 240 -MaxMemoryGB 8
& ./Tools/ExportCityWindowProfile.ps1 `
  -Capture Temp/CityScenarios/handoff-example-raw/3x -TimeoutSeconds 180 -MaxMemoryGB 6
```

最后raw示例需有有效summary、window.raw且监督器成功再导出；末次新Marker短采样正是在这点失败，不要“退出0就继续”。工具不保证单步低于时间预算，安全终止时保留现场，禁止放宽守卫无限等。

### 回归工具和已知覆盖

- `VerifyRoadCore.ps1 -Results Temp/RoadCore/<fresh>.csv`：当前3310核心检查（原3218+92占用复用）；方向、换道、间距、路口、动态编辑、货运/现金、恢复。
- `VerifyCityCore.ps1 -Results Temp/RoadCore/<fresh>.csv -Attribution`：城市23461、岗位10000、采样开关6000；完整城市钱货/真实工作链。不要把脚本名误解成原生窗口验收。
- `VerifySimulationBudget.ps1`：80项预算、欠步、恢复与失败；先看参数再使用。
- `VerifyTravelTimeQuotes.ps1`：12168项报价/实际路线及同分、方向、高程、单行/编辑检查；`VerifyRouteWorkspace.ps1`：8473项旧工作空间对照。
- `VerifyZoneViewBatches.ps1`：275206项显示几何；`VerifyVehicleViews.ps1`：15079项真实Unity生命周期。动态集成覆盖不是鼠标验收。
- `VerifyCityWindowState.ps1 -Before <save> -After <save>`：同tick业务/交通及仅比较副本分数欠步处理。使用AlignTicks前先理解其推进工作，不可对不同tick直接说状态相同。
- `RunBoundedCityScenario.ps1`：现成成熟城综合长测监督器（默认600s/120s停滞/8GiB），`RunCityScenario.ps1`是底层；大规模优先有界版本。发生OOM/停滞先保留现场缩小问题，不反复构造100k。

生成器在 `Assets/Scripts/Diagnostics/Runtime/CityScenarioBuilder.cs`，用正常CitySimulation；配置`Tools/Scenarios/mature-avenues-30000.json`（seed7919，gridRadius10，streetSpacing48，8户公寓、每方向2车道、显式测试地图半径504、合法seedRoadNetwork）。代码构造合法成熟城后才运行正常业务。配置生成器本身不保证精确重建上面threshold快照；需要相同存档就用保存原件。

### Profiler展开

`HarborCityRoadCoreGame.Update → City.Simulation → City.Simulation.Step`下分别看ResidentRequests（PrepareCost/PrepareRoute）、GrowthBoundary、Waste、Production、Logistics、ExternalTrade、Settlement。

交通：`City.RoadCore.Step → Occupancy / IntentAndReservations / CommitAndSettlement`。本批Occupancy实际在Reindex处，可能位于派车准备而不在MoveStep下；下含Clear/Actors/Sort（最后原生节点展示未验）。表现看Update下Views，以及渲染队列；Render Thread独立看。`threads.csv`为完整帧时长，hierarchy有Self阈值会缺一些根节点，不能当没有该帧。

## 10. 结果、临时资源与本地依赖

### 已同步GitHub的轻量证据

`Docs/Benchmarks/2026-10-17-{Commute,TwoGoalRoutes,ZoningViews,SpatialCache,ReturnPeak,VehicleViews,OccupancyReuse}`及相应Architecture报告。最新OccupancyReuse中有paired-steps、1x/3x窗口summary、clock-balance、恢复结果、资源、冷暖/分路日志和无收益Job合并ABBA。没有提交完整保存和逐帧大CSV。

完整原始结果在 `Temp/CityScenarios/`：

- `reuse-index-day-a1`、`reuse-index-day-b1`、`reuse-index-v2-day-b2`、`reuse-index-day-a2`：整日ABBA与最终保存。
- `resident-local-day-{a1,b1,b2,a2}`：无收益Job合并，不能推广。
- `warm30k-{normal-control,hit-occupancy-fine,reuse-index-fine}`：细分观察，额外作用域影响明显，不当普通性能。
- `index-reuse-commute-edit`：冷暖/集中出行/实际道路分割对照。
- `window30k-index-reuse-flow`：最新有效原生1×/3×、逐帧、截图、恢复、资源。
- `window30k-return-peak-raw`、`window30k-pool-return-raw`：真实返家长帧raw及导出。
- `window30k-index-scope-raw`：末次缺报告，明确失败，不删除。
- 其他`window30k-*`、`commute-*`、`return-peak-*`是历史控制/失败/消融，先看environment与资源退出状态，不拿目录名推断通过。

临时原型：`CityBuildings.axis-prototype.cs`、`CityRoadCoreSpatial.dedup-prototype.cs`、`CityCoreResidents.local-job-prototype.cs`等在Temp；冻结参照和工具运行副本同样不在Git。没有证明收益的源码不要复制进Assets。

### 只有本机有的资源

见[本机资源清单](LocalResources20261017.json)，交接只盘点文件和输入Hash，没有运行模拟。

- 30k固定保存约38MB、50k final约63MB、100k final约123MB；50k路径 `challenge50000-connectivity-after/final.hcity`，100k路径 `challenge100000-dotnet64/final.hcity`。这些是不同运行时/阶段的证据，不能直接拼配对或说Unity已验。
- `Temp/ManualCityPlayer`完整目录才是可运行Player，仅复制exe无数据目录不能运行。exe壳时间不证明业务程序集最新；重新构建看Managed/Assembly-CSharp.dll及environment哈希。末次普通源码已编译，但短采样报告失败。
- `Temp/RoadCoreUnity`是可重建Unity验证副本，不能在里面改代码后忘记主源码；`Temp/TrafficBenchmarkProject`是更早验证副本。`Temp/CodeLayoutNativeValidation`本次盘点不存在。
- `ProfilerCaptures`及Temp的大.raw/.data、截图、日志、场景保存未上传。不要为了push删除它们；若要清缓存须用户授权并核实路径，保留证据。
- 旧独立工作区和3个stash只有本机有，见Git章节，不是漏交功能。
- Unity安装固定 `C:/Program Files/Unity/Hub/Editor/6000.6.3f1`，Mono/csc来自其MonoBleedingEdge；部分工具依赖PowerShell7、Python3、Node、Chrome/Playwright，文档验证脚本有本机Playwright默认路径。换电脑需配置，不是模型错误。
- 原来粘贴的任务书在用户`.codex/attachments`，不在GitHub；核心长期约束已在本文和AGENTS内化，无需新会话依赖聊天附件。

## 11. 经验、风险与新会话建议

### 应保留的经验

1. **消除不必要工作优先于加缓存。** 分清需求报价与实际导航、首次唯一查询与失效、静态几何与动态占用；不要因Dictionary存在就假定快。
2. **数据流往往比局部函数更值钱。** 合并有共同搜索状态的目标、复用仍有效索引，完整步收益比微小getter合并更清楚。
3. **失效协议就是正确性。** Endpoint移除需置脏、新Network换Scratch、Access版本失效，不能靠下一次全扫碰巧修好。缓存不得成为第二真相。
4. **测量工具也可能错。** 锁输入、核actual_rate/步债/真实车数、记录源码和二进制Hash、检查报告是否真的生成；未知是-1，不是0；保留失败。
5. **局部快与全局快分开。** SAT零分配无整城收益，视图池大幅降低出生帧Views但普通FPS未显著提升，静态缓存以更多内存换时间。不要挑最漂亮的指标。
6. **完整事实对照与有意义断言同时需要。** 纯优化原城市保存一致；新模型要明确规则/安全/守恒/施工恢复。不能删失败断言、改精度、减车跳步制造性能。
7. **不要无限交通引擎化。** 用户最终要完整可经营城市。基础足够后，应在真实城市和真实交互检验模块，而非扩越来越复杂但独立的单路Demo。
8. **交接优于聊天记忆。** 新结论更新AGENTS/专题报告/HTML事实图；旧提案明确标历史，失败实验也记录。简短进度更新让用户能参与判断。

### 后续恢复授权后最值得做的三件事

1. 用当前同一30k保存确认要解决的是哪类体验：冷加载/建设1.5秒峰还是暖跑集中派车/3×算力；**复用已有证据，不先重跑所有规模**。一次一个问题，普通配对和raw归因分离。
2. 按已证实热点提出局部方案：冷分区几何/候选/Key分配，或集中实际路线与占用排序；不要直接扩大LRU、加居民预检或全面并行。保留完整状态、施工、在途恢复验收。
3. 同时把完整可玩城验收保持在路线前面：真实操作、服务吞吐和经营状态要持续可解释。经济自锁/日长属于产品规则，不未经用户决定就“优化”成有钱有货。

**现在不是上述任务的开始。交接提交并推送后停止，等待用户下一步指令。**

## 12. 给新Codex会话的交接入口

可以直接告诉新会话：

> 请先读仓库根AGENTS.md与Docs/Handover/HarborCity交接给Codex-2026-10-17.md，核对当前分支与工作区，不依赖旧Work聊天。现在处于暂停状态，只确认理解并报告风险，不自动启动性能分析、构建、仿真或优化。后续由我另行指定任务。

交接收尾只进行了只读Git/进程/已有文档检查、输入文件Hash与本机资源盘点、静态链接/SHA核对和文档提交。没有新跑回归、Unity、Profiler或规模实验；此前通过的测试均为既有证据，不冒充本次重新验收。
