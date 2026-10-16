# 自动化真实城市测试：第一阶段

> 本文保留第一阶段设计及当时边界。最新岗位/分区冷启动、垃圾归因、3000居民十日运行见[第二阶段记录](Stage2CityTesting.md)；500/3000城市真实窗口见[窗口报告](Stage2CityWindow.md)。不能将下文历史“未验证”误认为这些后续工作仍未执行。

2026-10-15。当前实现为 **B 模式：直接构造成熟城市，交给正常 CitySimulation 运行**。不是新交通引擎，也不是空地自然发展验证。常规游戏入口、业务规则、时钟、存档格式均未修改。

## 已实现的调用链与事实所有权

`RunCityScenario.ps1 → 导出当前 DefaultEconomy.asset → CityScenarioBuilder → CityCoreSave → CitySimulation.Advance(.05) → 日常完整 Validate → 报告`。

- Builder 仅构造初始状态：正常道路施工/设施/管线命令，CityEntities 注册实际建筑、家庭、居民和岗位，真实入口与外部连接。固定种子只控制合法地块的选择顺序。
- 设置与设计资源是输入。实际设计资源导出到 design.json，经强类型规则校验；effective-design.json 记录城市真正使用的规则。加载存档时使用存档中的规则，不用最新资产重新解释交易。
- 模拟仍是唯一事实来源：个人就业、到岗劳动、工厂库存/账户、Shipment 在途、商业账户、居民购买、税款、维护和垃圾按正常规则发生。工具不决定车辆是否到达、不补发工资、不定期增资。
- Runner 只提交固定时间并观测，不删车、丢步或关闭系统。每次要求恰好推进一个 tick。保存读取后恢复平地采样适配，不依赖 GameObject。
- 报表是冷派生数据，不保存第二份业务真相。每 200 tick（10 模拟秒）及日界聚合人口/建筑/任务，并冷复制网络以读取任务分配器（O(P+B+T+G)，G 为道路几何数据量）；不宣称 O(1)。诊断网络快照也有成本；这些成本计入宿主总时长，不放进模拟步耗时。

## 当前固定场景

配置：`Tools/Scenarios/mature-grid-500.json`，seed 7919，平地 y=2，网格间距 48，85 条实际道路、50 个节点，包含十字路口和一处实际城外货物连接。282 栋实际建筑：167 住宅、42 工厂、42 商店、9 电站、9 供水、9 排污、4 垃圾处理场。

500 名实际居民、167 户、334 名劳动力、336 个实际岗位。初始全部在家、未就业、无出行；工作由正常人口调度匹配。初始原料、成品、进口和消费均为零，不直接设置在岗劳动。

成熟构造允许的初始赋值：每户 700 存款；企业按当前设计资源获得初始资本；住宅与私人企业直接注册，不声称其造价经历正常生长投资流程。政府一次性开发拨款 100 万，正式记账；道路、设施、管线走正常建设收费。之后不再补钱。额外可开发地块未划出，Growth 仍正常执行，但本批不验证建筑生长/人口增长过程。

当前网格范围受现有地图边界限制，不能假定把 population 改成 10000 就有足够合法住房/设施位置。非法配置或不足会明确失败，不降低实际人口。3000/10000、汇流拥堵布局、A 模式自然发展为后续阶段。

该固定夹具的配比按当前默认每楼4岗位、每设施160容量及每户最多3人规划，不是通用城市平衡求解器。修改设计资源后，实际规则与岗位依然来自资源，但可能需要重新调整夹具配比；供给不足必须显式失败，不能把旧基准继续称为同一个负载。

## 重复运行

在项目目录的 PowerShell 中执行。依赖当前已安装 Unity 6000.6.3f1 的 Mono/C# 编译器、Python、PyYAML；路径与现有回归工具一致。

```powershell
./Tools/VerifyCityScenario.ps1
./Tools/RunCityScenario.ps1 -Results Temp/CityScenarios/my-baseline
./Tools/RunCityScenario.ps1 -Results Temp/CityScenarios/my-profile -Load Temp/CityScenarios/my-baseline/initial.hcity -Profile
python Tools/CompareCityScenarios.py Temp/CityScenarios/my-baseline Temp/CityScenarios/my-profile
```

每次用新的结果目录，工具拒绝覆盖既有证据。不要并行运行性能组，也不要把并行回归的耗时当正式基准。固定同一 initial.hcity、配置、代码条件和运行环境再做改动前后比较。

两组计时都先从相同初始保存恢复，避免“构造后热缓存”对比“读档后冷缓存”。profile 单独编译 CITY_CORE_ATTRIBUTION，统计已有真实阶段，普通组没有该诊断开销。不同采样开关的性能差异是诊断开销/环境差异，不能宣称优化收益。

首次构造额外重复一次并比对完整存档；种子变化测试要求实际城市改变。初始/中途/最终保存留在 Temp。中途必须有活动车辆：保存后两个恢复副本及原活跃城市分别继续 2400 步，执行完整校验并要求完整存档一致，然后主测试从检查点继续计时。额外重放不计入 72000 个正式模拟步，也不计入阶段采样；其真实耗时单独记录。

## 输出和计数口径

默认 30 游戏日 = 3600 模拟秒 = 72000 固定步。报告同时保留所有步、前 2 日预热、稳定非日界、日界，以及中途恢复后的 2400 步分布。日界分布是辅助切片，不表示其他步骤不能发生工资/调度尖峰。总体最大值始终保留。

| 文件 | 用途 |
|---|---|
| environment.json | Git 基线/dirty、所有模型和宿主源码哈希、场景/设计资产哈希、CPU/RAM/OS、Mono/Unity版本 |
| settings.json / design.json / effective-design.json | 实际配置与捕获规则 |
| initial.hcity / inflight.hcity / final.hcity | 可复用的完整城市状态，恢复时需平地适配 |
| steps.csv | 每步实际 tick、耗时、线程分配、活动车、阶段、宿主累计 GC 次数 |
| timeline.jsonl | 10 秒与日界状态：人口、就业、在岗、出行用途、库存、钱、供给、垃圾等 |
| report.json | 平均/P95/P99/最大值、验证/重放成本、警告、状态哈希、边界说明 |
| attribution.csv / profile-spikes.jsonl | 采样组各阶段累计 inclusive/self/calls/alloc；≥10ms 慢步的阶段差值 |
| failure.json / failure.hcity | 失败原因；已取得 session 且可序列化时保存失败状态。构造未返回时仅保留输入/环境和错误，不虚构可用失败城市 |

任务累计发出量来自单调 nextTask-1，已确认退休量=发出量-当前任务数；当前引擎只在确认到达/服务续程后退休。没有另外建立任务历史计数器。长期未退休任务、搁浅和垃圾积压为经营/诊断警告，不自动等于守恒错误。尚不存在明确的出发队列与失败历史计数，因此填 null，不能填零。

分配计数先探测平台支持；不支持填未知。GC 次数分别记录“模拟步内部”和“包括日志/冷观察/重放的宿主总计”，并非 GC 暂停毫秒。GC 暂停、FPS 当前未知；Mono WorkingSet 返回 0 时改为 null，不当作零内存。managedMemoryBytes 是结束时、包含诊断容器的托管内存读数，不是城市净内存或峰值。

采样的嵌套阶段不能直接相加：Population/Residents 包含 RouteLookup/RouteSearch，Growth 包含 ZoningRebuild/Placement，Dispatch 包含 Admission 等。累计采样提供 self 字段，慢步文件提供 inclusive/calls/alloc。

## 正确性与验收边界

每个日界及最终执行既有 session.Validate：独立核对实体/任务关联、路线与方向、车辆安全、库存、在途货物、工资/企业/家庭/财政及垃圾守恒。报告另要求实际观察到 Work/Home/Freight/Shopping/Service 和在岗居民，进口、生产、消费、税收、垃圾处理必须真实发生，不以“没崩溃”为通过标准。

精确状态比较覆盖钱、货、人物、车辆位置、任务和唯一时钟；不能为了变绿删除守恒断言。Compare 工具遇到初始/最终/继续状态哈希不同会拒绝宣称等价加速。未来规则有意改变时需要另审业务比较标准，不能靠本工具自动认可。

本批不修改路网施工规则。动态施工/方向/换道/在途恢复沿用城市与核心回归；**新 500 人长期场景本身没有施工事件**，不能宣称已验证大城边运行边拆路。独立 1000/3000/10000 车辆核心回归也不算这些规模的完整城市。

## 分阶段后续

1. 本批：固定 500 人成熟城、30 日、配对采样与完整继续验证，结果见 [第一批证据](../Benchmarks/2026-10-15-CityScenarios/Results.md)。守恒通过，垃圾收运仍不健康，冷寻路/分区存在尖峰。
2. 下一批：依据首步路线搜索和垃圾积压证据，先独立诊断其复杂度与长期业务影响，再在相同存档配对验证。扩展生成器承载 3000/10000 真实居民，显式保留失败场景；增加汇流布局与施工事件。
3. Unity 集成：为同一平地场景接入可见 Player，分别测真实 1×/3×、FPS、GC、视图/UI与内存。当前 normal 游戏地形不是平地，不能直接用其显示假定截图正确。鼠标修路/划区另行验收，后台构造不替代交互。
4. 固化代表性场景与性能回归阈值。先取得重复测量的波动，再定退化预算，不从一次运行承诺 60 FPS。

## 变更影响检查

新增诊断 Builder、命令行宿主、设计导出与比较工具；调用既有 CitySimulation、Entities、Construction、Utilities、Trade 和 Save。领域模块、World、CityModel、正常 UI/渲染均无修改，无双重事实、循环依赖或新的业务跨域写入。初始构造直赋仅限 B 模式的合法种子状态，随后所有业务归正常领域。已有模拟/表现边界不变；报告不反向驱动游戏规则。

源码入口：[Builder](../../Assets/Scripts/Diagnostics/Runtime/CityScenarioBuilder.cs)、[Runner/观测与报告](../../Tools/Diagnostics/CityScenarioRun.cs)、[命令行启动](../../Tools/RunCityScenario.ps1)、[配对比较](../../Tools/CompareCityScenarios.py)、[构造检查](../../Tools/Diagnostics/CityScenarioChecks.cs)。这些是测试宿主，未新增核心领域所有者；HTML原核心关系图保持其固定源码版本，本批实现单独在当前测试章节说明。


## 大规模测试的宿主位数（2026-10-16）

Unity附带命令行mono.exe实际为x86，5万人恢复副本中约1.5GB工作集抛OOM，不能当作64位Unity的城市上限。默认旧命令仍为Mono以保留历史口径；大规模请显式给RunBoundedCityScenario.ps1传-Runtime DotNet64，要求本机已有.NET10 SDK/目标包，离线Release/x64构建。同一原始CityScenarioRun与模型源文件编译，不是第二模拟引擎。CITY_SCENARIO_DOTNET仅切测试JSON适配/进程位数守卫，不改领域算法；二进制CityCoreSave仍完全相同。

64位示例：`pwsh -NoProfile -File Tools/RunBoundedCityScenario.ps1 -Runtime DotNet64 -Scenario Tools/Scenarios/mature-avenues-50000-two-days.json -Load Temp/CityScenarios/challenge50000-two-days/initial.hcity -Results Temp/CityScenarios/fresh-50000-64 -TimeoutSeconds 1200 -StallSeconds 300 -MaxMemoryGB 8`。结果目录必须新建，保留失败文件。build-sources.json记录实际编译清单（包括可选参考World源码），environment.json记录宿主选择/SDK，report.host实际记录进程位数与运行时，不能仅依赖PowerShell自身是64位。

不同运行时的长期状态不保证逐步相同：相同500人输入已实际观察到Mono与.NET的30日结果差异。优化前后必须同宿主配对，并要求该宿主完整保存/恢复状态一致；不得用.NET比Mono快宣称产品优化，也不得以.NET后台成绩冒充Unity FPS。-WorldReference只用于同宿主工具正确性对照，不是产品运行回退。默认游戏仍Unity宿主。

MaximumFamilies是100000家庭技术资源边界，不是城市规模通过声明。10001/100000户容量测试是无房未成年专用单元夹具，不是真实经营城市。实际综合结果/经营风险以ScaleChallengeResults.md为准。

续跑成熟存档如人口已自然变化，可显式使用 -AllowPopulationChange（仅 -Load 可用），报告同时记录配置声明人数及实际初始人口。默认仍严格核对。真实活动未发生时，工具先保存 status=failed-required-city-activity 的报告，再保持原断言和非零退出；不可将该报告当成通过。InspectCityBusiness.ps1 仅冷只读诊断，保存前后字节核对，不推进模拟。
