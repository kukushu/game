# 城市模拟百万级架构升级计划

基线：`codex/road-core-20261014`，实际 HEAD `a3f7cea`（2026-10-30重新确认）。独立实验：`codex/city-scale-architecture-20261030`。本轮明确授权正式Unity引擎研究，取代Python实验的旧工作范围；不改main，不覆盖玩家档或主工作区四个未提交文件。

## 当前结论与证据边界

阶段0已有源代码复杂度与30k真实Player基线；阶段1缓存/搜索工作区假设未获稳定收益、未集成；阶段2已有保守影子原型和失败负控，阶段3—7尚未验收。不能据此声称支持十万/百万人。本文是持续工程的阶段状态，不把规划当实现。

真实输入保持原文件：`test/Temp/CityScenarios/regenerated30000-20261017/city.hcity`，SHA256 `3F49BD1A02879BB686C24D5517902CC581C657220CE2B955107D89B6FA870B4E`。只读实数核对见 [census](../Performance/ScaleArchitecture-20261030/baseline-census.csv)：30000活居民、10000家庭、7806建筑、20000岗位且全占用、5230活动车、200座垃圾站、595垃圾任务。初始实际在岗0不代表无就业：居民处于真实出行/尚未到班状态，不能用岗位数代替出勤人数。

新编译 Unity6000.6.3f1 Release Player、1440×900、原档地图及原显示，2400真实步+8秒显示预热、随后1200完整0.05秒步。最长tick8201=51.6438ms，Waste39.0675ms，其中RouteSearch37.4381ms/109次。完整校验、20步冷热完全一致、进程退出0。平均11.1486ms、P95 13.8854/P99 20.675ms是带归因埋点且脚本每帧单步的**诊断**，不是正常Player FPS/50×倍率，也不能与历史粗计时Residents直接相比；Unity线程分配计数不可用，保留-1。见[阶段汇总](../Performance/ScaleArchitecture-20261030/baseline-fixed-summary.csv)、[长步](../Performance/ScaleArchitecture-20261030/baseline-long-steps.csv)、[校验](../Performance/ScaleArchitecture-20261030/baseline-validation.txt)。

历史普通Player基线继续保留于 `Docs/Performance/GrowthCandidates-20261027/Report.md`：优化后50×目标两轮约50.57FPS/2.560实际倍率，最长约62.47ms；这不是本轮重新测得的正常窗口成绩。原始新Player/日志/完整存档仅留本地 `city-scale-architecture-20261030/Temp/ResidentScheduling/`，不上传大文件。

## 复杂度与更新频率

N=存储的居民成员数（包括某些不可行动记录），B=建筑数，J=岗位数，V=实际车辆数，E=实际业务事件数。H=家庭、W=有空岗位工作地、S=商店、F=工厂、P=垃圾站、C=分区格、R/L=道路图节点/边；R/L不与业务事件E混称。时间表读取规则定义，不能假设所有系统均0.05秒扫全城。

| 路径/当前代码 | 触发 | 当前主要工作量 | 已存在的优化/真正需要的计算 | 候选改法及风险 |
|---|---|---|---|---|
| `Core/CitySimulation.Advance/Step` | 每0.05秒；原债务/8ms32步宿主预算 | 多阶段调用，成本由以下项相加 | 增长/水电/人口边界→居民→财政累计→实际劳动→购物→物流→外贸→垃圾→车辆→到达→提交日切 | 保持阶段及float边界；不能把漏步叫提速 |
| `Residents/CityCoreResidents.AdvanceRequests` | 每步 | O(N)+真正派发/路径 | 每人一次权威岗位读取；部分在家纯资格早退；通勤报价缓存；仍读健康/岗位/资金 | 时间轮/堆+精确失效，仅影子先行。必须保持Members顺序、0.25秒retry、float retryAt与minute首次满足步 |
| `Industry/CityProductionSystem.Step` | 每步 | O(N)+工业环境O(B) | 只对实际到岗/可工作/原班次重叠者付款并生产；`staffed.Clear`后本步积累供柜台判断 | 按实际在岗集合有序执行原公式；到岗/离岗/岗位/健康/缺钱完整触发。不能把每天发工资替代逐步劳动 |
| `Industry/CityIndustry.AdvanceIndustry` | 每步 | O(B+残余环境记录) | 工厂状态、stepProductive、噪声污染与旧住宅历史衰减；float乘法次序有意义 | 先跳过数学恒零且无状态变化对象；按需积分若改变舍入必须另列近似，不混合等价优化 |
| `Residents/CityCorePopulation.Step/FindJobs` | 到日切或6模拟秒 | 预建空缺O(J)；每求职者检查工作地桶内岗位，总上界O(N求职×J+候选排序+路径查询) | 已有按真实工作地分桶、home/work报价复用、距离下界排序/截断；不是仍逐岗位都寻路 | 待就业集合、按技能/工资/原ID有序空缺索引、局部失效。保留最近距离和并列最小job.id，不能改成随机岗位 |
| `Core/CityEntities` | 结构/关系变化 | building/person/family/job等O(1)索引；成员删除List.Remove O(N)冷路径 | 原实体唯一事实；Members视图、工厂/商店子集、建筑/实体revision | 增加 building→成员/岗位/住户反向引用；不能另存可编辑员工/容量事实。结构与失效日志一并维护 |
| `Residents/CityHouseholds` 查询 | 按调用 | Occupancy O(H)、JobCapacity/EmployedAt O(J) | GetBuilding/Job已有字典，不重复开发 | 增量计数或读反向索引；先查热调用频率，不能凭函数有遍历就称每步瓶颈 |
| `Services/CityCoreWaste.GenerateDay` | 日切一次 | O(N+H+B) | 真出勤与住户汇总后生成；工资尚未重置出勤 | 可在劳动/住户变化累计，仍在同一天边界按原公式取整入垃圾账 |
| `Services/CityCoreWaste.Step` | 每步处理P；每2秒派发 | O(P)处理，派发重建任务预留/扫B+候选排序O(BlogB)+各站逐候选准入/路径 | 站列表按BuildingRevision缓存；任务/预留从真实missions派生；按垃圾降序、ID升序；每站本轮至多一派发 | 第一优先测精确路径缓存/批次复用；随后脏候选。有200站而树路由容量128时走既有点路由，不能暗换路线或推迟任务 |
| `Economy/CityCoreConsumption.Step` | 每1秒 | 扫可营业商店/家庭；每需要购物家庭空间查询+可达判断 | 已有商店空间索引，按半径扩大；库存、实际staffed、服务和预算即时复核 | 需求家庭/营业店活动集合；不能用昨日开放状态掩盖本步欠薪/停产 |
| `Industry/CityLogisticsSystem.Step` | 每5tick=0.25秒 | 扫任务与工厂；工厂遍历候选商店，最坏O(V+F×S)+路径 | 已有CityFreightCandidates与真实到货/库存/支付 | 带需求供应链活动队列与反向任务；任务排序、价格与实际运输不变 |
| `Economy/CityCorePayroll` | 劳动时记信用；日切发放 | 日切O(N+H)，本步staffed按实际付款形成 | 不预设工资；欠薪与生产、家庭收入真实关联 | 变化工资信用集合，仍按原成员和家庭顺序日切支付，防跨户舍入与透支 |
| `Economy/CityCoreFinance.Accrue/SettleDay` | 每步累计；日结 | **Accrue通常O(1)**；日结O(J+N+B+贷款) | CurrentMaintenanceRate已有道路/建筑/水电/预算版本缓存；变化时才重算维护报价 | 不重复优化已缓存的维护扫描；税/费用按实际主体日结，现金守恒与边界原子性不变 |
| `Services/CityCoreUtilities.Refresh` | 每步检查版本 | 常态O(1)，失效时完整供给计算 | 已有dirty/revision及网络身份缓存 | 优先改善准确失效来源；供给容量分配若全局相关不能固定半径截断 |
| `Growth/CityCoreGrowth` | 每6秒 | 一次O(C)候选分组+实际候选几何 | a3f已复用原序候选，旧30轮空扫不是当前代码 | 仍有全格读取及统计；先测当前分支，不重做已合入优化 |
| `Traffic/World.MoveStep/occupancy` | 每步 | 活动车V的真实推进/约束；部分仍O(V) | 身体/影子与脏桶增量，稳定ID移动索引已有；临时路口预约仍完整维护 | 不把真实物理计算O(V)误判为闲置居民扫描；实际改道/安全仍需复核。AssertSafety可完整Reindex，会掩盖增量错，须先oracle |
| `RoadCoreNetwork/DispatchRoutes` | 真实路径请求 | 缓存命中近常量，未命中图搜索；当前已有A*与lane-pair共享 | Network不可变身份、入口变化失效；OD 4096和lane-pair 4096/8MiB两层受限缓存；站树128上限 | 相同查询/不可变邻接精确复用；改变启发或等成本父节点会影响完整状态，不能直接替换 |
| `Core/CityCoreSave` | 保存/冷加载 | O(N+H+B+J+V+路图+历史)，反射具体DTO、整包字节数组 | 业务schema/version17严格字段与类型；索引读后重建 | 先测内存峰值与读写分别，不把校验删掉；数据布局/协议变更独立兼容方案 |
| `Presentation/HarborCityRoadCoreGame` | 每渲染帧 | 车辆视图O(V)，建筑按revision重建；路/分区显示有派生缓存 | 模拟Clock不由相机裁剪决定；UI真实读数不能写经济事实 | 只读表现快照/可见绘制可优化，镜头不改变居民/车辆是否真实存在 |

## 容量检查与10万准备

- 现有成熟场景生成器明确限制population≤100000、gridRadius≤24，户内生成2成人+1儿童、实际住房/岗位/生产与服务。不能仅把population字段写100万就当真实负载。
- 当前 `HouseholdState.MaximumFamilies=100000`，不是100000人口。常见3人家庭意味着约30万人即逼近此限制；精确容量还受家庭/岗位/建筑及人物校验共同约束，不能直接由乘法宣称支持。
- 地图常规旧界168、平地正常扩展672，诊断平地上限2048；生成器要求streetSpacing×gridRadius≤buildHalfSize−24。30k原档半界456。已有十万配置16×48=768，半界792，均在既有边界内；没有删除限制。
- 保存每个受控集合计数上限1000000（含边界），全包字节数组/反射和多个完整事务副本会造成远大于稳态的峰值。32位整数实体ID不得复用/溢出；车辆/任务ID达到int.MaxValue显式拒绝。具体原始字段不变。
- 30k原档37,120,578字节；.NET64冷读诊断约758ms、写约845ms、保留托管差约45.85MB。这些不是Unity驻留内存或运行FPS；序列化新包37,120,592字节，读取与校验可用，不声称输入逐字节原样序列化。Player终态另做完整冷热检查。
- 复用已有 `Tools/Scenarios/mature-grid-100000.json`：100000人、radius16、半界792、实际公共初始资金100M、仍10天业务与2天预热。已完成100k真实实体构建及有界部分运行，尚未完成十天成熟经济/Unity Player验收，不能算规模架构达标。使用原有 `RunBoundedCityScenario.ps1 -Runtime DotNet64 -TimeoutSeconds ... -MaxMemoryGB ...` 有界生成，必须记录住房/实际岗位/供应/车流/守恒后才进入规模矩阵。初始公共资金是显式夹具条件，不是性能优化创造的收入。

## 分阶段路线与集成门槛

| 阶段 | 下一项工作 | 预期复杂度方向 | 硬门槛/风险 |
|---|---|---|---|
| 0 | 本清单、30k最新诊断、100k配置 | 拒绝用人口数字替代实际负载 | 已有100k部分运行和正常窗口新基线，仍缺100k成熟档和长期内存 |
| 1 | 先测不改路线的精确缓存容量/污染假设；如失败保留反例 | 减少重复A*，不改变E与调度频率 | 旧新完整保存/20+冷热、Player配对重复收益；不可靠不集成 |
| 2 | 先建立所有写路径→精确失效映射，再做影子时间唤醒 | 维护O(变化/到期事件)，原轮询仍权威 | 已有保守影子及负控，未启用实际稀疏执行。所有真实请求必须被候选覆盖，按Members序一致；资金、健康、跨日、读档/路网不能遗漏 |
| 3 | 仅在影子充分覆盖后替代部分安全类别 | O(E到期+保守检查类别)，闲置居民不再每步查Job | 没捕获的事件保守poll；队列维护/峰内存/GC均入成本；不能晚一个float边界tick |
| 4 | 已集成首项在岗位图（2ca4e6c），随后研究恒零环境安全跳过 | 劳动O(N在岗)+变更维护；减少闲置B | 原付款/生产/舍入顺序、staffed供给同；非等价积分单独方案 |
| 5 | 求职/空缺、住户/雇员、需求商店/货运/垃圾反向索引 | O(E变更+有需要的候选) | 保留原最近/并列/成员顺序，不把结构缓存当第二事实 |
| 6 | 测布局、ID生命周期、反射/副本峰值 | 仅热点局部紧凑数组/bitset，非全盘ECS | 存档协议不顺改；校验、重建成本和冷/热继续均验证 |
| 7 | 30k/100k/300k/1m真实业务矩阵 | 量化每活跃对象及每事件成本 | 30万/百万当前未验；跨区域、混合交通和并行先独立设计，不擅自替换规则 |

## 调度器设计约束（尚非实现/验收）

优先评估数组索引的分桶/时间轮+远期最小堆：一个居民至多一个有效唤醒键，generation用于过期，无每tick事件对象。先估计4个int/long数组×N的具体内存，再选择，不能用抽象框架隐藏维护成本。时间轴唯一事实仍是Clock及原居民字段。

同tick先收集候选，再按当前Members的有效序号执行原流程，不能ID排序。读档从权威成员、trip/retry/visit/day、岗位与雇主状态重建，不保存第二时间事实。按原float minute/retryAt/visitUntil条件计算首次合法tick，不能粗略ceil后晚唤醒；分离参数计算和实际发车原复核。

失效表至少需覆盖：CityEntities增删/移户/岗位变动；工资付款将雇主资金从有到无；消费/到货/财政使企业资金恢复；CanAttendWork依赖的疾病/死亡/资格写入；工作/购物车辆真实到达、购物停留结束；日切；World.NetworkGeneration/AccessRevision与施工恢复。当前部分public业务字段仍可直接写，必须找到并约束真实写路径，不能只监听Entity.Revision就称完整。

影子首次只记录候选、实际RequestCommute和顺序，不改retry/岗位/车辆/经济。先用正常、返家、跨日、失败重试、缺钱恢复、疾病、迁居、入口/路网改变、冷热恢复的同初态测试证明；未经证明的类别保守检查，影子命中不等于已省CPU。

资源与测量限制：原玩家Unity Editor及其导入worker保留，不终止用户进程。本轮计时期间不并行运行自己的编译器或其他测试Player；这一控制不等于独占操作系统。配对需重复、退出超时与采样通过分别记录。隐藏脚本Player的固定步数据只解释CPU，尚不提供正常可见窗口FPS。

本轮进展与所有实验边界见 [首轮交付](../Performance/ScaleArchitecture-20261030/Progress.md)，精确写入口契约见 [失效表](ResidentWakeInvalidation.md)。100k仅906个预热后无画面模型步，不能与Unity Player成绩混用；30万与百万城市未验证。
