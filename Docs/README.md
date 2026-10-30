# HarborCity 工程导航

本导航核对基线为正式开发 `codex/road-core-20261014` 的 `d248cf3`（2026-10-30）。当前源码和本轮用户要求优先；带日期的报告只证明其注明版本，不等于今天的性能保证或继续开发授权。

## 先读什么

1. [长期工程约定](../AGENTS.md)：Git 保护、所有权、模拟时间、事务、存档和验证政策。
2. [单一引擎清理结果](LegacyCleanup20261020.md)：旧运行入口已删除，CityModel 等共享业务事实仍保留。
3. [统一寻路集成说明](Integrations/UnifiedRouting-20261026.md)：已有 A*、车道对共享及各历史实验的集成关系。
4. [稀疏劳动正式报告](Performance/SparseAttendance-20261030/Report.md)：截至本基线最近的代码与真实 Player 证据，不能把局部 CPU 收益称为 FPS 改善。
5. [分支分类与保留建议](Repository/BranchReview-20261030.md)：本轮不删除分支，不据此自动合并。

## 当前运行结构：从源码确认

|边界|权威入口/事实|
|---|---|
|Unity 宿主、工具和显示|[HarborCityRoadCoreGame](../Assets/Scripts/Runtime/Presentation/Game/HarborCityRoadCoreGame.cs)|
|组装、城市业务与交通交接|[CityCoreIndustrySession](../Assets/Scripts/Runtime/Simulation/Industry/CityCoreIndustrySession.cs)|
|唯一固定步与欠步|[CitySimulation](../Assets/Scripts/Runtime/Simulation/Core/CitySimulation.cs)|
|实体身份、生命周期及派生索引|[CityEntities](../Assets/Scripts/Runtime/Simulation/Core/CityEntities.cs)|
|道路有向拓扑与路由|[Network](../Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreNetwork.cs)|
|实际车辆、任务与安全事务|[World](../Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs)|
|当前城市保存协议|[CityCoreSave](../Assets/Scripts/Runtime/Simulation/Core/CityCoreSave.cs)：不要从旧 README 或磁盘文件名推断版本|

模拟与 Unity 表现分离；CityModel、居民、家庭、岗位、企业账本仍是现行核心，不是可以按历史来源删除的旧引擎。缓存和视图不提供第二套业务事实。旧引擎源码/旧测试是否可读，只说明历史保存情况。

## 当前可定位的测试与测量入口

下列文件及总入口调用关系已核对；**本次文档整理未运行游戏回归或 Unity 性能测试**。运行时先读参数，使用新输出目录，明确输入与冻结版本；入口存在不代表所有历史脚本都已按最新代码重新验收。

|用途|脚本|
|---|---|
|受支持的综合模型回归|[Verify.ps1](../Tools/Verify.ps1)：依次调度 RoadCore、CityCore、SimulationBudget、CityScenario、CodeLayout|
|交通/道路核心|[VerifyRoadCore.ps1](../Tools/VerifyRoadCore.ps1)|
|城市、经济、岗位与分区等|[VerifyCityCore.ps1](../Tools/VerifyCityCore.ps1)|
|固定步和预算|[VerifySimulationBudget.ps1](../Tools/VerifySimulationBudget.ps1)|
|场景与代码布局|[VerifyCityScenario.ps1](../Tools/VerifyCityScenario.ps1)、[VerifyCodeLayout.ps1](../Tools/VerifyCodeLayout.ps1)|
|在岗集合与冻结状态对照|[VerifyAttendanceRoster.ps1](../Tools/VerifyAttendanceRoster.ps1)|
|增量交通与统一寻路专项|[VerifyIncrementalTraffic.ps1](../Tools/VerifyIncrementalTraffic.ps1)、[VerifyUnifiedRouting.ps1](../Tools/VerifyUnifiedRouting.ps1)|
|真实普通 Player 窗口|[RunCityScenarioWindow.ps1](../Tools/RunCityScenarioWindow.ps1)：画面 FPS、实际倍率、帧时间，区分 Profile 构建|
|有界模型场景|[RunBoundedCityScenario.ps1](../Tools/RunBoundedCityScenario.ps1)：要求独立结果目录和资源上限，Mono/.NET64不能混比|
|分项诊断|[RunResidentSchedulingDiagnosis.ps1](../Tools/RunResidentSchedulingDiagnosis.ps1)：脚本固定一步/帧不是普通游戏 FPS，频繁计时有开销|
|共享模型发现与临时项目准备|[ProjectLayout.ps1](../Tools/ProjectLayout.ps1)|

旧 `RunRecoveryWindow.ps1` 的协程间隔曾跨两帧；真实单帧口径的纠正见[拆路帧诊断](Performance/DemolitionFrameDiagnosis-20261025/Report.md)，不要重新把旧列当单帧。

## 已集成改动的说明与证据

- [增长候选复用](Performance/GrowthCandidates-20261027/Report.md)、[增量车辆占用与移动](Performance/IncrementalTraffic-20261026/Report.md)、[居民请求复用](Performance/ResidentRequests-20261026/Report.md)。
- [车道寻路与道路合批集成](Integrations/LaneRoutingRoadBatching-20261024.md)及[统一 A* 集成](Integrations/UnifiedRouting-20261026.md)。后者含旧分支继承关系和后续策略变化，不能只读原实验默认开关。
- [新城设施模板](NewTownTemplate.md)、[UI 布局](CityUI20261020.md)、[道路水电](RoadUtilities.md)是对应功能的说明；具体参数仍需核对源码。

这些报告中的“未合并”“默认关闭”可能只是当时状态；Git 拓扑、后续集成报告和当前开关共同判断，不能据旧报告重复开发或开启实验。

## 仍独立推进的百万级实验

`codex/city-scale-architecture-20261030` 保留独立成果，**没有因部分在岗代码已集成而变成可删除或可整支合并的历史分支**。

- [架构与复杂度计划（固定实验快照）](https://github.com/kukushu/game/blob/53a9e8ca14299f0cbcb5009aed9c3e1e4c2399da/Docs/Architecture/MillionScalePlan.md)。
- [实验进展、失败方案及部分十万人数据](https://github.com/kukushu/game/blob/53a9e8ca14299f0cbcb5009aed9c3e1e4c2399da/Docs/Performance/ScaleArchitecture-20261030/Progress.md)。
- 时间队列仍是影子；十万人部分无画面模型数据不是十万人 Player/百万城市验收。本导航不授权继续该实验。

## 历史资料如何读

|位置|定位|
|---|---|
|[Architecture](Architecture/)|R1/R2/R3 的阶段审查、方案及 Results。多数是历史定位，不是一份持续更新的全项目现状|
|[早期 RoadCore 设计](RoadCoreArchitecture.md)、[R1 审查](Architecture/ArchitectureReview.md)、[HTML 图](Architecture/HarborCityArchitecture.html)|理解所有权和决策背景；图与文中“当前/最新”受其源码日期限制|
|[Performance](Performance/)|按日期与源码版本保存的 Player/算法实验；失败、未合并、集成后的报告均保留|
|[Benchmarks](Benchmarks/)|早期基线和原始轻量结果；[旧发布工具说明](Benchmarks/README.txt)仅在需要时用，不是每轮交付门槛|
|[Handover](Handover/)|2026-10-17 交接与本机资源索引，暂停/未来任务仅属当时；本机路径不是跨机器资源保证|
|[AGENTS 原件与阅读说明](History/README.md)|完整历史保留，临时授权不进入新的全局规则|

不搬动报告、结果 CSV、测试脚本或其相对引用。已确认的重复全文示例：Architecture/ConstructionTransactions20261018.txt 与 Benchmarks 的 RoadTransactions-Release/Report.txt；Architecture/ZoningIncremental20261018.txt 与 ZoningIncremental-Final/Report.txt。两边旧入口都保留，避免切断证据引用。
