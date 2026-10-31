# 百万级引擎执行清单

执行分支 `codex/million-scale-execution-20261031`；正式基线 `93d97f2004b3893d3b76bf765e972e21405d19fa`；历史实验只读参考 `53a9e8c`，不整支合并。规范以本分支新版AGENTS为准。状态仅 TODO / IN_PROGRESS / DONE / BLOCKED / SKIPPED；同时至多一个主要IN_PROGRESS。

恢复顺序：读本文件与[进度](Progress.md)→核对Git/工作树/未提交文件→按依赖推进。ID简写依赖001即MS-001。`S/`代表Assets/Scripts/Runtime/Simulation/，居民/经济/工业等目录见下表真实文件；`Tools/`相对仓库根。所有未来预期都是假设，验收前不算成果。

已有成果：稀疏在岗正式2ca4e6c，完整固定步约−3.4%/实际倍率约+5.5%，未改善FPS/长尾；报告[正式证据](../../Performance/SparseAttendance-20261030/Report.md)。旧队列/影子仅Tools，漏通知负控仍失败；垃圾缓存扩容与工作区改动无可靠整体收益，标记历史SKIPPED假设，不再重做。100k初态及5706部分模型步不是长期/Player验收。固定实验资料从53a读取，不能假定正式分支已存在。

现在：MS-005复查修正通过（MS005-Review.md）；MS-006通知/成本验收完成，MS-007通知与成员顺序持久化验收通过；MS-008纯时间计算已验证；MS-009失效通知及状态对照通过；MS-010完整影子与补充事实通知验收通过；MS-011实验基础守卫已实际接管并通过模型对照，正常构建仍轮询；MS-012已完成选定班次/状态计划的实际接管与模型对照；MS-013完成原生Player配对与集成判定，实验默认不开、不合正式；MS-014已完成生产分项与活动计数调查，MS-015首候选正确但无可靠整体收益，已撤回，目标未完成回TODO；MS-020按已满足依赖和38ms峰值证据优先调查。每项代码提交同步状态；SHA在后续记录中回填。完整事件调度尚未完成；已验证类别的Player吞吐收益及FPS取舍见MS013。

|ID|状态|依赖|目标/理由|真实文件与调用链（S/下，Tools另注）|问题/预期复杂度|拟用结构/算法|完成定义（正确性及性能）|实际提交/测试/报告|
|---|---|---|---|---|---|---|---|---|
|MS-001|DONE|—|建立可恢复台账与保护基线；不重复已有实验|AGENTS.md；TODO/Progress；git worktree/ref|一次调查 O(文件/引用)；锁定93d/53a与用户修改哈希|独立分支、依赖DAG、既有成果索引|台账字段完整、至多一项IN_PROGRESS；受保护文件不变|48552f5；基线93d97f2/53a9e8c，保护快照留本地Temp|
|MS-002|DONE|001|审计居民守卫写路径，确定最小通知范围|Residents/CityResidents.cs、CityCoreResidents.Depart/Arrive/AdvanceRequests；Economy/CityCoreConsumption|每步O(N)；当前直接提前retryAt会漏通知|写入矩阵；先只接tripId/retryAt/visitUntil必要守卫|列出初始化/冷读/出发/到达/购物/结构及直接写入；不推断全事件已覆盖|[写入契约](ResidentWakeContracts.md)，无ref/out或字段反射依赖|
|MS-003|DONE|002|受控守卫写通知，消除已证实漏唤醒|Residents/CityResidents.cs → Core/CityEntities.cs；CityCoreSave显式事实|直接写O(1)变O(1)+有关监听；不广播全城|复用实体Owner和CitySavedFact；保留所有浮点位模式|冻结93d保存/完整状态一致；直接提前/延后、无效值、增删/冷读通知均测；暂不替换轮询|[MS003结果](MS003-GuardNotifications.md)，冻结状态/字节与核心测试通过|
|MS-004|DONE|003|复用索引堆并让影子接真实守卫/结构通知|旧53a Tools/Diagnostics/CityWakeQueue.cs、ResidentWakeShadow.cs；Tools/Diagnostics/PrepareResidentWakeShadow.py；Entities生命周期|队列O(logN)，影子仍O(N)；维护成本单列|按当前Members ordinal，去重/取消/冷构建；不重造旧队列|原负控转为被公共setter覆盖；故意漏通知的负控仍能检错；真实请求无遗漏/乱序|[MS004结果](MS004-WakeShadow.md)，6920帧零遗漏，正负控通过|
|MS-005|DONE|004|为岗位分配/释放建立工作地→成员关系|Residents/CityJobs.AssignJob/ReleaseJob/SyncJobs；CityEntities|由查全员变为受影响员工集合|派生反向索引，原成员序、换户与删除处理|岗位所有权oracle；失业/重雇/关闭/冷热全状态一致；记录维护/内存|[MS005结果](MS005-EmploymentIndex.md)；状态同、维护分配同、约+2.41MB；无稳定整体提速；[复查修正](MS005-Review.md)消除无变化重绑重复分配|
|MS-006|DONE|005|捕获雇主现金耗尽与恢复的精确失效|Economy/CityCorePayroll.PayAttendance；Industry/CityIndustry；Consumption/Trade/Finance事务|资金变化只触及有关员工，避免O(N)广播|提交后门槛变动通知；权威现金不复制|付款失败无事件副作用；有限资金、到货销售/税费恢复、请求顺序/完整状态同|[MS006报告](MS006-FundingNotifications.md)；9108实际通知，状态同；模型CPU约+6.04%、内存+0.438MiB，未集成|
|MS-007|DONE|004|健康、工作资格和住址生命周期通知|Residents/CityResidents/CityHouseholds/CityJobs；Core/CityEntities.MoveResident/Add/Remove|关系变化按实际成员失效|审计受支持写入口与Owner；未接类别保守poll|疾病/死亡/工作能力、搬家/住宅失效、脱离Owner、冷重建严格对照|[MS007阶段证据](MS007-ResidentState.md)；88断言/126点同；[86145e2修复冷序](MS007-MemberOrder.md)，格式18；原FAIL反例保留，当前全部通过|
|MS-008|DONE|005,006,007|计划首次合法上班/下班Tick|CityCoreResidents.AdvanceRequests/EstimateCommute；GameData/CityEmploymentDesign；CitySimulationClock|未到时刻O(N)重复判断→到期/变化事件|float minute/retry及double阈值搜索；不可达等待保守策略|所有边界不晚一tick；同tick按Members，原0.25重试；原报价/lastCommute语义同|[MS008证据](MS008-ResidentTime.md)；17335断言、210取整反例、实际128+128样本；仅计算未调度|
|MS-009|DONE|008|日切与路网/入口变化唤醒|CitySimulation.SyncCalendar；Core/CityCoreRoadBindings；RoadCore.NetworkGeneration/AccessRevision|局部依赖失效，真正全局事件可O(N)|关联等待集合/版本失效；不固定半径截断|新增/删除/拆分/方向/入口与不可达恢复、跨日和时间规则对照|d647607；[MS009报告](MS009-PlanInvalidation.md)，60定向/139完整状态点及208167请求同；保留全局安全回退|
|MS-010|DONE|009|完整影子验收与开销统计|ResidentWakeShadow及居民完整场景回放；CityCoreSave|影子不省正式CPU；量化维护/峰值而非只数候选|冻结原请求序列及完整状态，负控、跨日、冷热|30k+定向资金/病亡/搬家/购物/道路/失败恢复都无遗漏、顺序及状态同|3e86bb0；[MS010验收](MS010-FullShadow.md)，152状态点/482141请求摘要同、7181阶段995818请求0漏；诊断额外约2.1ms/6.38MiB，未接管执行|
|MS-011|DONE|010|先接管可靠守卫类别的实际执行|CityCoreResidents.AdvanceRequests；CityEntities；CitySimulation|O(到期+保守候选)，不是全员poll后再过滤|明确实验开关；原oracle路径保留；未接类别原检查|同存档同固定步完整状态/事件序一致；包含队列维护的粗计时先证实收益|5c5370d/43a9e10/b23cf85；[MS011](MS011-GuardExecution.md)，152状态点/请求字节同，模型均值约−5～8%；P99未改善，默认关闭待Player验收|
|MS-012|DONE|011|逐类启用班次及状态唤醒，减少未到时刻检查|CityCoreResidents；工作地/家庭反向索引|进一步O(N等待)→O(到期/真实变化)|按类别推进，每类单独对照与回退|不得略过实时准入/支付；所有任务、工资、经济及后续模拟同|e7d57ba；[MS012](MS012-TemporalExecution.md)，三方152状态点及482141请求同；模型相对MS011约−18.7%，额外2.72MiB，Player见MS013/默认关闭|
|MS-013|DONE|012|居民调度真实Player配对、回归及选择性集成|Tools/RunResidentSchedulingDiagnosis/RunCityScenarioWindow；居民与保存测试|完整步/真实倍率/长尾/GC含维护成本|同30k二进制条件A/B/B/A，细归因和普通构建分离|完整状态同且整体重复收益才集成；不改8ms/32步/.05规则，不强合失败方案|[原生Player验收](MS013-PlayerAcceptance.md)：50×目标实际3.103→3.661×，FPS46.86→45.77；不默认启用、不合正式|
|MS-014|DONE|001|拆解AdvanceIndustry工厂/环境/状态成本|Industry/CityProductionSystem.Step→CityIndustry.AdvanceIndustry；CityCorePayroll|在岗循环已O(A)，企业环境仍O(B)|低开销分项与周期/活动统计，不重复做在岗集合|同城预热完整步归因；确认恒零、持续变化及状态依赖后再选择实现|已有在岗正式2ca4e6c/报告，不能重复计成果；[实测分项与活动计数](MS014-ProductionAudit.md)：Workers0.312/Industry0.275ms每步；六原生状态/冷读一致；细埋点污染已校准|
|MS-015|TODO|014|精确恒零/无活动企业稀疏化|CityIndustry.AdvanceIndustry；残余环境、企业状态写口|只省不产生任何变化的高频工作|派生活动集合；原每步float运算顺序不变|冻结全状态、资金/原料/产量/污染状态与冷热同；Player整体收益|[第一候选未采用](MS015-ZeroWorkExperiment.md)：位/状态同，完整步6.97269→6.98402ms，撤回104caa1改动；稀疏化目标未完成|
|MS-016|TODO|005|量化并减少求职与岗位候选重复工作|Residents/CityCorePopulation.FindJobs；CityJobs；CityHouseholds|求职者×工作地/岗位与家庭查找|按技能/空缺/原job.id排序的派生候选，不改评分|原最近/同分序与完整岗位选择同，30k/100k冷/热分列；整体有收益|—|
|MS-017|TODO|007|住房→住户/空置单元关系与失效|CityHouseholds.Occupancy；CityCorePopulation；CityEntities|重复扫家庭→受影响住宅查询|基于原家庭/单元事实的可重建索引|迁入迁出/换户/拆房/冷恢复、住房分配及资金同；先确认热调用|—|
|MS-018|TODO|006,007|按需购物家庭及营业店候选|Economy/CityCoreConsumption.Step/BeginVisit；CityCorePayroll|每秒扫家庭/店→需求/变化集合|保留原家庭/店选择序与营业资金实时核对|缺钱/补货/付薪/购物到达/失败重试/冷热同；测完整步|—|
|MS-019|TODO|006|货运供需与在途预留关系|Industry/CityLogisticsSystem.Step；CityFreightCandidates；World任务生命周期|周期O(V+F*S)→实际供需及相关任务|派生反向任务/库存需求索引；原匹配次序|支付、货物、预留和失败原子性不变；真实Player重复收益|—|
|MS-020|IN_PROGRESS|001|统计垃圾批次查询重复及失败，再选共享方案|Services/CityCoreWaste.Step→World服务路由|集中搜索尖峰；4096→16k、工作区试验已无收益|低开销按站点/目标/成功与展开统计；不重做扩容|同一输入/固定步；证明共享机会后另增实现任务，不改积压优先级/出车规则|旧失败证据53a Progress；[MS013峰值](MS013-PlayerAcceptance.md)：200基地超过128容量，整组回退，单帧普通搜索38.29ms；后续统计复用，不盲目扩容；[首轮查询统计](MS020-RoutingAudit.md)：269服务查询/122实际基地，需补端点及拒绝原因，未改算法|
|MS-021|TODO|001|从已有100k初态继续多日验证并归因经营异常|Tools/RunBoundedCityScenario；CityScenarioRun；100k已有initial.hcity|避免再构建两遍；区分冷峰、稳定CPU与消费0|复用只读初态；有界多日、日切Validate与在途checkpoint|真实就业/到岗/资金/生产/货运/消费/服务；守恒与冷热，失败如实，不为通过改平衡|已有5706步/906稳定步，仅部分模型结果|
|MS-022|TODO|013,015,021|100k真实UnityPlayer验收|RunCityScenarioWindow；现有100k城市|加载与稳态、FPS和模拟倍率分开|有界Player矩阵、固定相机与预热、原微观负载|至少可靠1×实际推进、可交互；FPS/长尾/欠步/内存与保存；50×独立报告|—|
|MS-023|TODO|013,015,021|按热数据实测布局与分配候选|CityResidents/CityEntities/World；CityCoreSave|对象布局/索引内存先证据后局部SoA/槽位|稳定ID与可压缩槽位分离；不全面ECS|真实维护/GC/峰内存、完整状态与保存，不能只测数组内核|—|
|MS-024|TODO|021,023|审查家庭、地图、生成与序列化容量|CityHouseholds.MaximumFamilies；CityScenarioBuilder；CityCoreSave/SaveCodec|当前生成上限100k，集合/ID/峰值约束独立|逐项容量边界/写前拒绝/数据布局证明，不只取消上限|边界、拒绝原子性、真实规模场景；当前保存/读取正确，旧档不强适配|—|
|MS-025|TODO|022|大规模交通真实需求和单车成本普查|Traffic/World.MoveStep/occupancy；路由；居民/货运/市政任务|O(V)真实物理与长路径成本需分离|按实际任务起终/时间/车道密度统计；沿用已验证索引|同负载下推进、堵塞、预约、编辑、在途保存；不靠减少车提高成绩|—|
|MS-026|TODO|025|如需混合交通先提交独立语义方案|World车辆/Shipment/到达；Network|微观交通达到实测上限才研究分层|流量/排队/真实任务身份守恒、宏微边界原型|若出行时间或规则改变须用户设计决定；误差/安全/守恒通过才允许实现集成|设计审批门槛；尚未提出改规则|
|MS-027|TODO|013,019,025|局部变化与区域依赖方案|CitySimulation；Network；经济物流关系|局部事件避免全城广播，允许必要跨区传播|真实空间+经济联系分区，单线程先证实边界|跨区通勤/结算、道路封闭、全局政策不遗漏；不固定小半径假定因果|—|
|MS-028|TODO|027|独立评估多速率/并行的必要性与确定性|CitySimulation.Clock/Step；World；经济结算|只有仍存在CPU瓶颈才研究并行|所有权/阶段屏障/确定性提交；Jobs/Burst仅候选|时间精度/玩法变化先设计审批；同状态或明确业务误差标准，不暗跳步|—|
|MS-029|TODO|022,024,025|300k真实城市有界模型及Player矩阵|CityScenarioBuilder/RunBoundedCityScenario/RunCityScenarioWindow|不是人口字段或队列容量测试|真实住房岗位企业服务路网与实际活动，多日/冷热|至少1×与合理交互，守恒/峰内存/长帧，报告硬件实际边界|—|
|MS-030|TODO|029,027|1m真实城市验收与上限报告|同规模工具、CityCoreSave、全部业务链|量化剩余复杂度，禁止静态人头填充|真实微观因果；必要已批准分层；完整规模矩阵|不能只加载；持续1×、交互、实际生产消费运输、冷热与局部编辑；未达标如实给上限|—|
