# MS-013：居民时间调度的 Unity Player 验收

状态：DONE（完成实测与集成判定，**不默认启用、不合入正式分支**）。模型收益已经由原生Player验证，但吞吐与FPS存在取舍，不能宣称全部玩家体验验收通过。

## 版本与场景

- 分支 `codex/million-scale-execution-20261031`。冻结比较起点 `da84dfe7608c28c2e6ddc1a6a1b512166d2f2bcb`，调度实现 `e7d57ba`；诊断构建及运行入口提交 `d689056`。两侧游戏源码相同，仅居民调度编译开关不同。A 为原轮询，B 同时开启 `CITY_RESIDENT_GUARD_SCHEDULE` / `CITY_RESIDENT_TIME_SCHEDULE`。这不是正式历史提交 `93d97f2` 的二进制对比；两侧都包含此前的事实通知和成员顺序保存实现。
- Unity 6000.6.3f1 / Windows / Mono / DX11 / RTX 3090 / i5-14600KF；1440×900，目标 60 FPS，VSync=0。普通结果是非 Development 的 Release Player；Profiler 构建单列。
- 原三万人场景：`test/Temp/CityScenarios/regenerated30000-20261017/city.hcity`，原始 SHA256 `3f49bd1a02879bb686c24d5517902cc581c657220ce2b955107d89b6fa870b4e`，未写入。通过既有冻结测试导出器读取旧事实、保留其真实成员顺序，生成一次 format18 测试副本，SHA256 `bfeafdb7cc0b2cf289cdd0ed8a487c3eb591c00c7eb546f58396d84607d1dad1`。未在游戏中加入迁移/兼容层。
- 每次重新读取同一副本：2400 固定步预热、既有垃圾树预热接口、8 秒画面预热。**场景有200个垃圾处理场且200个入口有效，超过车道树基地容量128，`CanServeLiveSet=False`，因此整个基地集合走原有普通点寻路回退。** 预热报告 `depots=0/new_trees=0` 表示执行预热的基地数，不能解释成城市没有垃圾场。早期工作记录曾误读这个计数，已由原生Profiler和只读设施审计纠正。未增减设施或修改缓存上限；测量窗口也没有车道树冷建成本。本轮不能证明“车道树真正启用时”的收益或多基地树内存表现。实际人口30000、建筑 View 7806、活动车辆峰值约7500；道路逻辑 View 22572 / Renderer 256，分区格子90733 / Renderer256，水电 View16441。
- 正常窗口每档 60 秒，顺序 A0 / B0 / B1 / A1，每次独立 Player，分别测 1×、50×。预算仍为 8ms / 最多32步，固定步仍0.05秒；保留全部欠步，未隐藏道路/车辆或改变业务。冷读与断言在计时窗口之外。

## 已完成的原生正确性对照

在两侧 Release Player 内执行同一预热及随后 2400 个完整固定步，每渲染帧一步（这是相同工作量测试，不是正常倍率/FPS成绩）：

| 文件 | 两侧共同 SHA256 |
|---|---|
| fixed-start.hcity | `53e81b717e047ce74e02b0cb8907b463e8ce3a55aca191e56fe312628960ab5d` |
| fixed-final.hcity | `8c0e0229a0947f053bae83b167db3067f052b56bd1ba7681a24183264908faf9` |
| fixed-states.csv（每600步，4点） | `3f8c6ff2ac71025a2596455ebdff4969d429ca85f5ed3f62fadf61d3472b6c91` |

完整城市/交通/资金/货物/垃圾保存相同；两侧均通过另20步热/冷恢复对照。旧首轮 fixed-summary 的 final_tick=9620 包含验证追加20步，真正 fixed-final 保存终点为9600；工具已修正后续输出的端点计数。没有修改旧证据。

## 指标限制与失败记录

- 本机普通 Release 的逐帧 GC 分配计数及 `GetAllocatedBytesForCurrentThread` 不可用，输出 -1/False，不能将其写成零分配；GC 代数计数与 OS 采样峰值可用。模型分配/驻留成本另见 MS012，不能冒充 Player GC。
- 首次普通 A0 的测量、保存、恢复和截图完成，但退出阶段停滞，240秒由本次进程监控终止；`Temp/MillionScaleExecution/ms013-a0/1x-resources.json` 保留失败。该轮不计入正式表。只调整诊断探针计时结束后的截图为同步读回，重建两侧后重新完整交叉测量。一次短窗正常退出不能证明原生退出停滞根因。
- 两侧固定工作量完成后，以及正式矩阵的四个1×窗口，均记录既有 `HarborCityZoneBoxTool.Dispose() → HarborCityRoadCoreGame.OnDestroy()` 退出清理 NullReferenceException；四个50×日志没有该异常。发生在测量、保存对照和截图之后，不把退出码0等同于日志无异常，也不借本轮调度验收修改表现层清理逻辑。
- 普通 Release 的 Render Thread/GPU/Draw Call 部分计数不可用，缺失值保持未知；后续归因构建不与普通帧率混算。

## 普通 Release Player 实测

两轮分别60秒的均值。P95/P99为两个窗口各自分位数的平均，并非合池分位数；最长帧为两次全部帧的最大值。每个窗口从相同 tick7200 和上表 warm-state 开始。八次退出码均0、未触发资源上限，八次窗口后的20步热/冷保存完全相同。

| 指标 | 轮询 A | 时间调度 B | 解释 |
|---|---:|---:|---|
| 1×实际倍率 | 0.9998× | 0.9997× | 都正常保持1× |
| 1× FPS | 58.18 | 58.94 | 小幅上升约1.3% |
| 1× P95 / P99 ms | 26.62 / 30.01 | 25.01 / 29.23 | 略改善 |
| 1×最长帧 ms | 67.28 | 66.23 | 峰值基本仍在 |
| 1× Advance总耗时 / 完成步数 ms | 9.127 | 7.348 | 同样1200步，约−19.5%；包含宿主调用开销 |
| 50×目标的实际倍率 | 3.103× | 3.661× | 约+18.0%，仍远不到50× |
| 50×目标 FPS | 46.86 | 45.77 | 约−2.3%，没有FPS收益 |
| 50×目标 P95 / P99 ms | 25.76 / 31.16 | 26.24 / 32.52 | 略回退，不能隐藏 |
| 50×目标最长帧 ms | 61.86 | 66.20 | 未改善 |
| 50× Advance总耗时 / 完成步数 ms | 8.104 | 6.980 | 仅辅助；两侧60秒实际完成步数不同 |
| 50×每帧车辆表现同步均值 ms | 2.710 | 2.720 | 没有减少显示内容 |

原生固定工作量单次配对：相同2400步均值8.827→7.552ms（−14.4%）；该结果包含原生Unity/Mono开销，区别于MS012的.NET模型结果。它不是50×帧率，且仅一对固定工作量，普通窗则有两对顺序复测。

50×两侧每帧最多只推进2步。原预算在**完整步骤结束后**检查8ms，不能截断一个步骤；优化后更多帧能容纳第二步，因此推进倍率增加，同时渲染帧可能略长，符合观测到的FPS/长尾取舍。60秒后尚欠约2814 / 2781模拟秒，账目误差约1e-10秒；没有丢弃欠步。目标50×需要每真实秒完成1000个0.05秒步骤，而此负载中完整步仍为数毫秒，单线程算力和每帧预算共同限制，无法靠减少居民检查直接达到50×。

OS采样峰值：1× A/B平均约1899 / 1917MiB；50×约1720 / 1800MiB。这是含加载、托管/原生资源和不同终点状态的进程峰值，不能把差值直接归因到调度索引，更不能宣称内存改善。正常Release分配量仍未知；已知派生常驻成本见MS012。

数据：[八个完整窗口](MS013-Player.csv)、[每窗五个最长帧](MS013-LongestFrames.csv)。原始存档、Player、日志及逐帧数据留本地 `Temp/MillionScaleExecution/ms013-*` / `Temp/MS013/`。实验工具没有更改正式游戏逻辑或预算。

## 原生 Profiler：稳态与峰值分开

另建 `Development + CITY_CORE_ATTRIBUTION + temporal` Player，条件同上。60秒/1200完整步的归因结果如下；包含插桩成本，只用于定位，不代替普通Release成绩。以下顶层系统可以对照总步，但 Dispatch、Admission、RouteSearch 等嵌套子项不能再与顶层相加。

| 系统 | 总ms | 每步ms | 占完整步 |
|---|---:|---:|---:|
| 完整Step | 9536.34 | 7.947 | 100% |
| 居民请求 | 4618.95 | 3.849 | 48.4% |
| 车辆移动 | 3291.85 | 2.743 | 34.5% |
| 生产（含真实在岗处理） | 935.45 | 0.780 | 9.8% |
| 垃圾（现有Part含两个位置） | 238.96 | 0.199 | 2.5% |
| 到达结算 | 285.28 | 0.238 | 3.0% |

居民虽已减少重复等待检查，仍有大量真实/失败准入工作：Dispatch共192129次，Admission共197915次；这些是不同嵌套层的计数，不是成功派车数。Job作用域10595643次，合计735.46ms；路由搜索7368次、372.37ms（约3.9%完整步），通勤估计348次、15.73ms。不能据旧百分比把高命中通勤估计继续当首要热点。

原始采样仅开60帧，从tick8190到8212，覆盖反复出现的8201。Profiler本地frame30：Main Thread62.25ms，宿主逐帧CSV同tick的帧间隔67.98ms；两者定义不同，不能混写。该帧：

| 作用域 | ms | 说明 |
|---|---:|---|
| 完整模拟Step | 51.018 | 单个完整步骤，8ms协作预算不能中途切开 |
| 垃圾Step | 40.133 | 峰值主因，约79%模拟步 |
| 垃圾内部PrepareRoute | 38.915 | 24次调用 |
| 其中RouteSearch | 38.294 | 92次搜索作用域调用，内部候选方向搜索，非92辆成功发车 |
| 居民请求 | 5.634 | 未随此次时间计划优化消失，但不是这个峰值主因 |
| 车辆移动 | 3.034 | 正常微观推进 |
| 生产 | 1.457 | 约1.23ms自身时间 |
| 车辆显示同步 | 3.291 | 在模拟Step之外 |

该帧GC.Collect计数为0；采样垃圾子树分配159600B、内部搜索87584B（嵌套不能相加）。紧邻frame31完整Step约9.19ms、垃圾Step约0.006ms，支持“周期性普通垃圾寻路突发”的归因，而不是凭8201/8281间隔推测人口或增长工作。没有在本场景复现普通游戏100ms空闲推进峰值；本轮正常窗口最大67.28ms，不代表其他场景的100ms问题已经解决。

关键原因：只读设施审计为200/200个垃圾场/有效入口，[设施CSV](MS013-Facilities.csv)。`DepotLaneRouting.Capacity=128`、`RetainDepots/CanServeLiveSet` 使整个集合停止建树，`World.ServiceBlueprint/ServiceReturn` 回退共用点路由；测量窗口 `lane-cache-window.csv` 的查询/命中/建树均0，`ServicePointQueries`仍增加269。这不是首次建树冷启动，而是现行容量策略下的常态回退。增加预热步骤无法使这个分支变成车道树命中；本轮不修改其策略，不重复之前失败的盲目缓存扩容实验。MS020已有“按站点/目标/成功及展开统计再选共享方案”的目标，可沿此证据开展。

原始Profiler：`Temp/MillionScaleExecution/ms013-profile-peak/1x/window.raw`，194900089B，SHA256 `082073f9c31161cd7710b8f7a97597ed7ba7c5e2d9a5742636fbc3d1d691b20a`。复用既有导出器得到本地hierarchy.csv/threads.csv；它们是筛选后的CPU导出，不含所有事件，GPU未知。提交[整窗归因](MS013-ProfileTotals.csv)及[峰值完整路径](MS013-PeakScopes.csv)，没有上传大文件或新建归档体系。

## 原生道路编辑回归

Temporal Release同场景预热后，在tick7300用既有脚本命令新增道路：成功，扣款320，事务113.14ms，道路显示同步4.62ms，仅1个块释放/重建，逻辑Drivable22572→22576，Renderer仍256。热/冷续跑20完整步保存一致，车辆位置显示检查通过，退出0。此为原生脚本操作，不是手动鼠标验收，也没有与轮询做施工耗时配对；不能宣称施工卡顿改善。整个含施工窗口仍有233.38ms长帧，[编辑CSV](MS013-RoadEdit.csv)。拆分/重划/增长及在途编辑完整状态回归复用MS012的152点和核心测试，不重复运行无关性能矩阵。

## 集成判定与下一项

时间调度有可靠的同输入吞吐收益，固定步和原生保存正确性通过；但50×目标下FPS约−2.3%、P99约+4.4%、峰值未改善，施工仍有233ms帧。没有将“能推进更多模拟步”包装成“画面更流畅”。因此本阶段完成测量和判定，成果留在实验分支，普通构建保持原轮询，暂不集成正式分支。另一个明确证据边界是A也有MS003–012的通知/所有权基础，不是旧正式二进制；将来整批默认集成仍需对准确正式版本确认总维护成本，不能只挑本次开关收益。

MS014按原清单继续拆解生产0.78ms/步中的真实在岗处理与AdvanceIndustry工厂/环境/状态成本，不提前重写经济规则。垃圾容量回退及周期路由峰值记录到原MS020的调查背景，不在本轮改其目标或擅自扩容。居民请求中的失败准入/保守轮询仍是大头；本次未改变0.25秒重试或资格规则。路口预约、更多时间等待类别、Unity表现层清理异常及工程外分支均未顺带修改。

原生普通运行均从同一格式18测试副本读入，初态warm hash完全相同。大存档、用户Editor、用户未提交文件和其他分支的HEAD/哈希复核保持不变；正式远端仍93d97f2，旧百万实验仍53a9e8c。没有修改Python系统、存档协议或正式游戏运行逻辑。本轮只提交既有诊断工具增强、报告和CSV。

复现入口（输出目录须全新，两侧顺序运行；不要并行占用同一个临时Unity工程）：

```powershell
$env:HARBOR_PLAYER_RELEASE='1'
# B构建加-ResidentTimeScheduling，A构建不加；分别指定不同PlayerPath。
Tools/RunCityScenarioWindow.ps1 -Save Temp/MillionScaleExecution/ms012-paired/fixture-export/same-input-order18.hcity -Results Temp/MS013/build-new -PlayerPath Temp/MS013/TemporalNew/HarborCity.exe -BuildOnly -ResidentTimeScheduling
Tools/RunCityScenarioWindow.ps1 -Save Temp/MillionScaleExecution/ms012-paired/fixture-export/same-input-order18.hcity -Results Temp/MS013/window-new -PlayerPath Temp/MS013/TemporalNew/HarborCity.exe -SkipBuild -ResidentTimeScheduling -Rates @(1,50) -MeasureSeconds 60 -TimeoutSeconds 240 -SharedWarmup -LaneWarmSteps 2200 -PrewarmLaneTrees -DepotLaneRouting -SavedMap -CheckRestore
```

固定同工作量加 `-FixedSteps 2400 -Rates @(0)`；峰值诊断另建Development/CoreProfile，再以 `-RawProfile -RawProfileSeconds 60 -RawStartTick 8190 -RawFrameLimit 60 -Rates @(1)` 采样，复用 `Tools/ExportCityWindowProfile.ps1` 导出。设施审计读取同一副本，核心表达式为 `s.City.buildings.OfType<LandfillBuilding>()` 的总数/合法入口数与 `s.Transport.DepotRoutes.CanServeLiveSet`；只读Validate不推进城市。此审计不作为Player耗时成绩。
