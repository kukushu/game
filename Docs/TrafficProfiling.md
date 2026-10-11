# 交通移动性能采样（诊断，不改变规则）

本批保留 City.Simulation / City.Traffic.Movement 及其相邻服务、居民、调度标记。固定步长、追帧次数、所有车辆、换道/跟车/预约/任务/经济和存档格式保持原样。

## 如何展开 Hierarchy

关闭 Deep Profile，CPU Usage 选 Hierarchy / Main Thread。展开 PlayerLoop → UpdateScene → Update.ScriptRunBehaviourUpdate → BehaviourUpdate → HarborCityGame.Update → City.Simulation → City.Traffic.Movement。Unity 显示的外围节点名称可能随编辑器版本略变。

下列缩进是实际调用的层级，名称保留完整前缀（这里为可读性省略 City.Traffic.）：

```text
Movement
├─ Movement.ReleaseJunctions
│  └─ Junction.Restore（仅道路 revision 变化）
├─ Movement.Snapshot（仅 trips.ToArray）
├─ Movement.AdvanceLaneChange
├─ Movement.AdvanceStopWait
└─ Move
   ├─ Move.Prepare（服务停留、居民任务验证、沿路收垃圾、实体有效性）
   ├─ Move.VisitAndReturn
   │  ├─ Route.Search
   │  └─ Lanes.Entry
   ├─ Move.RouteValidation
   ├─ Move.Replan
   │  ├─ Route.Search
   │  └─ Move.Wait
   ├─ Move.ChooseLane
   │  ├─ Junction.CrossingRadius
   │  ├─ Lanes.TurnLane
   │  ├─ Lanes.Headway（实际 LaneHeadway 调用，可能重复）
   │  ├─ Lanes.Space（实际 LaneSpace 调用）
   │  └─ Lanes.Commit（执行换道及现有 Trace）
   ├─ Move.Arrive（无剩余路段）
   └─ Move.EdgeAdvance（同一步可能跨多个短边）
      ├─ Move.Following（完整附近边 × State.trips 跟车遍历）
      ├─ Move.LimitCrossingAdvance
      │  ├─ Junction.CrossingRadius
      │  ├─ Junction.PendingWalker
      │  └─ Junction.Reserve（行人分支）
      ├─ Junction.Reserve（接近 Next 时）
      │  ├─ Junction.ControlAllows
      │  │  ├─ Junction.HighwayGap
      │  │  ├─ Junction.PendingWalker
      │  │  ├─ Junction.ConflictGeometry
      │  │  └─ Junction.ExitClear
      │  ├─ Junction.WalkingEntryClear
      │  ├─ Junction.ExitClear
      │  └─ Junction.ConflictGeometry
      │     └─ Junction.ConflictPath
      ├─ Move.ProgressAndWait（阻塞、失败或推进 progress）
      └─ Move.EdgeTransition（换边、队列释放、收垃圾）
         └─ Move.Arrive
```

相同函数出现在多个父节点下属于真实调用，并非重复执行新增逻辑。分支本帧没执行不会出现节点；不能把缺失节点读成 0 ms。ControlAllows / ConflictGeometry 等也会出现在其他真实调用分支，树不是穷举所有组合。

## 每帧计数和导出

- City.Traffic.FixedSteps：实际调用 Simulate 的累计次数，帧结束刷新并归零。一帧中 Movement Calls 应与此一致；不是按倍速推算。
- City.Traffic.ActiveTrips：最近 Advance 结束时任务数量，包含步行/服务/访问中的任务，不等于屏幕可见汽车数量。暂停时保留最后数量。
- Unity 的计数器可能需要在自定义 Profiler 模块中添加；它们不是耗时 Hierarchy 子节点。
- Play 中将游戏设为 3×，用 Harbor → Diagnostics → Capture 120 traffic frames，或 Ctrl+Shift+F9。只录制，不修改地图、倍速或暂停状态；导出在录制停止后执行。
- 导出目录 Logs/TrafficProfiler：samples.csv 含帧、完整调用路径、total_ms/self_ms/calls/gc_bytes；frames.csv 含任务数、固定步数和 Movement 汇总。缺失计数器使用 -1。只读当前 Profiler 最近 120 帧，环形缓冲丢失的帧无法补回。
- 已录制数据可用 Export recent traffic profiler frames 导出。比较同一帧的父子项，Total 包含子项，Self 才是尚未归入子采样的时间；不能将父子 Total 相加。
- 按 Movement 时间排序，普通帧选有效模拟帧中位数，尖峰选最大值，同时比较 ms/固定步和任务数。录制启动、脚本重载、后台焦点切换帧应单独注明。

## 开销边界

所有新增标记静态创建，Auto scope 是值类型；不动态命名、不在每对车辆/每次距离计算中采样，不增加 ToArray 次数。诊断通过 UNITY_5_3_OR_NEWER + ENABLE_PROFILER + 编辑器或 Development Build 编译条件开启，纯模型测试和非 Development Player 不包含新增采样/计数器。额外定义 CITY_TRAFFIC_PROFILING_DISABLED 可完整移除细分节点，保留原有大节点用于同场景 A/B。

采样仍有原生 Begin/End 成本，特别是高 Calls 的换道和冲突检查。细分采样不等于零开销；应评估空 scope 开销和实际 Calls，再在同一城市/步数的关闭细分构建中复测。不要以诊断帧直接宣称发布版性能。

## 第一次真实城市录制（细分半径/冲突路径前）

真实 Unity 6000.6.3f1，现有第 127 日城市、3×、120 帧，未重建测试城市。所有帧均执行 20 步，任务数 118–339；这批没有健康低耗时普通帧，因此以样本中位数表示常见卡顿帧。录制初期有独立验证构建的机器负载，期间用户有操作；不是严格性能 A/B，不与旧截图直接作速度优劣结论。

### 常见帧（按 Movement 排序中位数）：帧 232

任务 218；固定步数 20。以下同名节点若在不同父节点出现则合并，但父子 Total 不能相加。

| 节点 | Total ms | Self ms | Calls | GC B |
| --- | ---: | ---: | ---: | ---: |
| City.Traffic.Movement | 986.514 | 0.485 | 20 | 11997120 |
| City.Traffic.Move | 979.374 | 0.856 | 4383 | 11554024 |
| City.Traffic.Move.RouteValidation | 110.688 | 110.680 | 4347 | 8976 |
| City.Traffic.Move.ChooseLane | 337.503 | 288.679 | 4247 | 1841760 |
| City.Traffic.Lanes.TurnLane | 28.030 | 28.030 | 840 | 0 |
| City.Traffic.Lanes.Headway | 18.111 | 18.111 | 563 | 0 |
| City.Traffic.Lanes.Space | 1.216 | 1.216 | 217 | 0 |
| City.Traffic.Move.Prepare | 6.685 | 5.999 | 4383 | 1698168 |
| City.Traffic.Move.VisitAndReturn | 0.280 | 0.011 | 10 | 5944 |
| City.Traffic.Lanes.Entry | 1.437 | 1.437 | 192 | 0 |
| City.Traffic.Route.Search | 27.631 | 27.423 | 185 | 739156 |
| City.Traffic.Move.Replan | 0.016 | 0.012 | 100 | 0 |
| City.Traffic.Move.Wait | 0.003 | 0.003 | 100 | 0 |
| City.Traffic.Move.EdgeAdvance | 523.332 | 26.254 | 4389 | 7999176 |
| City.Traffic.Junction.Reserve | 64.081 | 32.083 | 3102 | 2427416 |
| City.Traffic.Junction.ExitClear | 0.399 | 0.397 | 52 | 6656 |
| City.Traffic.Junction.ControlAllows | 19.892 | 16.533 | 1021 | 759300 |
| City.Traffic.Junction.HighwayGap | 0.404 | 0.402 | 32 | 5120 |
| City.Traffic.Junction.ConflictGeometry | 2.754 | 2.725 | 37 | 43344 |
| City.Traffic.Move.LimitCrossingAdvance | 377.542 | 329.149 | 4389 | 7241780 |
| City.Traffic.Junction.WalkingEntryClear | 11.300 | 10.843 | 181 | 1000504 |
| City.Traffic.Junction.PendingWalker | 20.664 | 20.024 | 120 | 1292496 |
| City.Traffic.Move.Following | 77.948 | 77.948 | 3215 | 0 |
| City.Traffic.Move.ProgressAndWait | 0.171 | 0.171 | 4389 | 0 |
| City.Traffic.Move.EdgeTransition | 1.668 | 1.318 | 156 | 19216 |
| City.Traffic.Move.Arrive | 0.172 | 0.170 | 12 | 8136 |
| City.Traffic.Movement.AdvanceLaneChange | 0.173 | 0.173 | 4383 | 0 |
| City.Traffic.Movement.ReleaseJunctions | 4.785 | 4.581 | 20 | 353152 |
| City.Traffic.Movement.AdvanceStopWait | 1.629 | 1.610 | 4383 | 54240 |
| City.Traffic.Movement.Snapshot | 0.034 | 0.034 | 20 | 35704 |
| City.Traffic.Services | 31.534 | 24.325 | 20 | 3030766 |
| City.Traffic.ResidentsAndIndustry | 67.528 | 44.411 | 20 | 4706246 |
| City.Traffic.Dispatch | 15.090 | 7.443 | 2 | 998784 |

### Movement 最大帧：帧 271

任务 339；固定步数 20。以下同名节点若在不同父节点出现则合并，但父子 Total 不能相加。

| 节点 | Total ms | Self ms | Calls | GC B |
| --- | ---: | ---: | ---: | ---: |
| City.Traffic.Movement | 1624.236 | 0.874 | 20 | 17361820 |
| City.Traffic.Move | 1615.290 | 1.614 | 6701 | 16916420 |
| City.Traffic.Move.RouteValidation | 169.266 | 169.255 | 6555 | 12320 |
| City.Traffic.Move.ChooseLane | 472.114 | 432.034 | 6455 | 3602712 |
| City.Traffic.Lanes.TurnLane | 19.321 | 19.321 | 591 | 0 |
| City.Traffic.Lanes.Headway | 12.771 | 12.771 | 319 | 0 |
| City.Traffic.Lanes.Space | 1.872 | 1.872 | 180 | 0 |
| City.Traffic.Lanes.Commit | 0.009 | 0.009 | 2 | 288 |
| City.Traffic.Move.Prepare | 10.072 | 9.134 | 6701 | 2002792 |
| City.Traffic.Move.VisitAndReturn | 0.012 | 0.012 | 146 | 0 |
| City.Traffic.Move.Replan | 0.016 | 0.012 | 100 | 0 |
| City.Traffic.Move.Wait | 0.004 | 0.004 | 100 | 0 |
| City.Traffic.Move.EdgeAdvance | 962.180 | 36.413 | 7024 | 11298596 |
| City.Traffic.Junction.Reserve | 220.629 | 42.020 | 3323 | 4336924 |
| City.Traffic.Junction.ExitClear | 3.886 | 3.872 | 397 | 50816 |
| City.Traffic.Junction.ControlAllows | 165.821 | 30.874 | 801 | 3518832 |
| City.Traffic.Junction.HighwayGap | 2.057 | 2.043 | 250 | 40000 |
| City.Traffic.Junction.ConflictGeometry | 138.198 | 136.157 | 717 | 2884784 |
| City.Traffic.Move.LimitCrossingAdvance | 539.057 | 522.339 | 7024 | 6831664 |
| City.Traffic.Junction.PendingWalker | 4.871 | 4.613 | 62 | 579276 |
| City.Traffic.Move.Following | 161.861 | 161.861 | 5843 | 0 |
| City.Traffic.Move.ProgressAndWait | 0.337 | 0.337 | 7024 | 0 |
| City.Traffic.Move.EdgeTransition | 5.262 | 4.992 | 590 | 21512 |
| City.Traffic.Move.Arrive | 0.266 | 0.262 | 21 | 11792 |
| City.Traffic.Movement.AdvanceLaneChange | 0.336 | 0.336 | 6701 | 0 |
| City.Traffic.Movement.ReleaseJunctions | 3.030 | 2.878 | 20 | 280112 |
| City.Traffic.Movement.AdvanceStopWait | 4.541 | 4.493 | 6701 | 111040 |
| City.Traffic.Movement.Snapshot | 0.075 | 0.074 | 20 | 54248 |
| City.Traffic.Services | 38.297 | 32.813 | 20 | 3075012 |
| City.Traffic.ResidentsAndIndustry | 486.748 | 239.227 | 20 | 14109848 |
| City.Traffic.Lanes.Entry | 15.213 | 15.213 | 1464 | 0 |
| City.Traffic.Route.Search | 544.454 | 540.354 | 1441 | 13670572 |
| City.Traffic.Dispatch | 330.857 | 9.207 | 2 | 8513576 |

这批已把原来的 Movement Self 细分出去：快照创建约 0.03–0.08 ms；ChooseLane 和 LimitCrossingAdvance 的 Self 仍很高。阅读调用链发现两者均多次调用 CrossingRadius，其内部遍历道路各入口/出口并生成 Corner，故新增 Junction.CrossingRadius；冲突判断内部另加 ConflictPath。这里只定位，不缓存/删减这些计算。

## 验证范围

- Tools/Verify.ps1 全量通过，包括真实交通生命周期、路口冲突、车道/换道、货运/工业与原生结构存档的模型回归；日志 Temp/TrafficProfilerRegression.log。
- Unity 独立项目编译成功；Development Player 构建成功（TrafficProfilerBuild.log）。原有独立玩家验证 Result.txt、LongRun.txt 等通过，26 日持续运行、垃圾守恒误差 0。它验证输入/车辆/货运/存档行为，不代表真实城市帧率。
- Release / 非 Unity 模型新增 scope 和 counters 在预处理阶段移除；不要求打开 Deep Profile。新增标记和计数器不保存到城市 JSON。
- 空 scope 的本机原生编辑器测量单独记录在 overhead.txt；结果包含循环和 Stopwatch，不能等同于完整城市开关细分后的性能对比。完整 A/B 和所有偶发分支（道路重建、特殊服务停留）的真实帧覆盖尚未全部完成。

## 最终细分节点实机核验

第二次在原城市 3× 录制，包含焦点切换和暂停：120 个记录帧中只有 20 个执行交通模拟，其中 18 帧执行 20 步，另两帧分别执行 1 / 5 步。所有有效帧 FixedSteps 与 Movement Calls 一致。不能把没有模拟的帧当成健康普通帧，也不能将两次录制直接当作优化前后对比。

有效帧中位数：5876，165 个任务、20 步，Movement 817.973 ms / 20 Calls / 10142264 B；尖峰：5868，194 个任务、20 步，994.489 ms / 20 Calls / 12453776 B。下面保留最终录制的重点节点；同名节点跨父路径合并，父子 Total 不可相加。原始完整路径位于 Logs/TrafficProfilerVerified/samples.csv。

### 帧 5876

| 节点 | Total ms | Self ms | Calls | GC B |
| --- | ---: | ---: | ---: | ---: |
| City.Traffic.Junction.ConflictGeometry | 55.171 | 8.702 | 299 | 1256976 |
| City.Traffic.Junction.ConflictPath | 46.470 | 45.799 | 522 | 1256976 |
| City.Traffic.Junction.ControlAllows | 69.074 | 13.996 | 690 | 1749472 |
| City.Traffic.Junction.CrossingRadius | 130.384 | 124.778 | 4340 | 3719232 |
| City.Traffic.Junction.PendingWalker | 5.323 | 3.961 | 38 | 432448 |
| City.Traffic.Junction.Reserve | 97.585 | 21.153 | 2025 | 2623440 |
| City.Traffic.Junction.WalkingEntryClear | 3.054 | 1.563 | 47 | 330500 |
| City.Traffic.Lanes.Headway | 2.272 | 2.272 | 78 | 0 |
| City.Traffic.Lanes.Space | 0.102 | 0.102 | 12 | 0 |
| City.Traffic.Lanes.TurnLane | 8.257 | 8.257 | 269 | 0 |
| City.Traffic.Move | 808.934 | 0.637 | 3274 | 9758336 |
| City.Traffic.Move.ChooseLane | 231.344 | 167.956 | 3116 | 1955488 |
| City.Traffic.Move.EdgeAdvance | 458.730 | 22.890 | 3321 | 6682912 |
| City.Traffic.Move.EdgeTransition | 2.188 | 1.929 | 209 | 13684 |
| City.Traffic.Move.Following | 50.240 | 50.240 | 2154 | 0 |
| City.Traffic.Move.LimitCrossingAdvance | 295.201 | 211.737 | 3321 | 4603052 |
| City.Traffic.Move.Prepare | 4.282 | 3.854 | 3274 | 1045712 |
| City.Traffic.Move.RouteValidation | 111.063 | 111.062 | 3216 | 1760 |
| City.Traffic.Move.VisitAndReturn | 2.856 | 0.007 | 40 | 72464 |
| City.Traffic.Movement | 817.973 | 0.379 | 20 | 10142264 |
| City.Traffic.Movement.AdvanceStopWait | 2.036 | 2.013 | 3274 | 59200 |
| City.Traffic.Movement.ReleaseJunctions | 6.406 | 0.378 | 20 | 297896 |
| City.Traffic.Movement.Snapshot | 0.048 | 0.047 | 20 | 26832 |
| City.Traffic.Route.Search | 3.066 | 3.044 | 3 | 77480 |


### 帧 5868

| 节点 | Total ms | Self ms | Calls | GC B |
| --- | ---: | ---: | ---: | ---: |
| City.Traffic.Junction.ConflictGeometry | 85.971 | 12.947 | 496 | 2094960 |
| City.Traffic.Junction.ConflictPath | 73.024 | 71.868 | 870 | 2094960 |
| City.Traffic.Junction.ControlAllows | 103.206 | 18.207 | 751 | 2701648 |
| City.Traffic.Junction.CrossingRadius | 147.970 | 144.782 | 5159 | 4465824 |
| City.Traffic.Junction.ExitClear | 1.259 | 1.254 | 188 | 24064 |
| City.Traffic.Junction.PendingWalker | 4.859 | 3.699 | 34 | 387836 |
| City.Traffic.Junction.Reserve | 139.773 | 26.196 | 2552 | 3590924 |
| City.Traffic.Junction.WalkingEntryClear | 2.357 | 1.307 | 34 | 140828 |
| City.Traffic.Lanes.Headway | 5.675 | 5.675 | 185 | 0 |
| City.Traffic.Lanes.Space | 0.931 | 0.931 | 144 | 0 |
| City.Traffic.Lanes.TurnLane | 12.461 | 12.461 | 406 | 0 |
| City.Traffic.Move | 981.125 | 0.721 | 3928 | 11886508 |
| City.Traffic.Move.ChooseLane | 282.109 | 203.134 | 3680 | 2216408 |
| City.Traffic.Move.EdgeAdvance | 572.152 | 25.138 | 3995 | 8280636 |
| City.Traffic.Move.EdgeTransition | 3.125 | 2.659 | 325 | 18876 |
| City.Traffic.Move.Following | 68.618 | 68.618 | 2889 | 0 |
| City.Traffic.Move.LimitCrossingAdvance | 344.244 | 250.145 | 3995 | 5113596 |
| City.Traffic.Move.Prepare | 4.554 | 4.057 | 3928 | 1234032 |
| City.Traffic.Move.RouteValidation | 115.749 | 115.744 | 3780 | 5280 |
| City.Traffic.Move.VisitAndReturn | 5.127 | 0.040 | 110 | 129372 |
| City.Traffic.Movement | 994.489 | 0.424 | 20 | 12453776 |
| City.Traffic.Movement.AdvanceStopWait | 2.706 | 2.676 | 3928 | 68480 |
| City.Traffic.Movement.ReleaseJunctions | 6.851 | 0.531 | 20 | 466724 |
| City.Traffic.Movement.Snapshot | 3.153 | 0.825 | 20 | 32064 |
| City.Traffic.Route.Search | 5.608 | 5.561 | 99 | 160384 |

CrossingRadius 与 ConflictPath 已在原生 Hierarchy 导出中出现在真实父调用下，说明节点可被 Profiler 记录。仍有未细分的 Self，不能据此宣称唯一根因已确定。此次不修复、缓存或删减这些计算。初步方向是几何计算、路线检查及全任务扫描；需要下一批根据各父路径进一步确认。

本机空 Auto scope 微测为每 scope 中位数 0.00001674 ms，9 批各 5000 次的托管分配均为 0 B。这是同步编辑器方法的测量，Profiler 开关不保证此刻原生记录已完整激活，只能作为局部参考，不能证明完整城市测量开销。尚未完成同城同状态开启/关闭细分的严格 A/B。

最终 Development 构建通过（Temp/TrafficProfilerFinalBuild.log），含最终几何节点及导出工具防重复录制、暂停结束处理。工具仅由菜单主动调用，CSV 序列化在停止录制后运行。Release 通过编译条件排除细分节点，非 Unity 全量模型回归通过；尚未单独实测 Release 构建帧率。

最终构建重新运行独立玩家回归，Temp/TrafficProfilerFinalPlayerValidation 下 Result.txt / LongRun.txt 为 PASS，交通、路口、换道、货运、保存读取及 26 日持续运行通过；ProblemGUIUnverified.txt 仍明确记录旧有交互覆盖限制，不将其视为全 UI 验证。
