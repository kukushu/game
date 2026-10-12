# 道路键哈希正式启用与验收

2026-10-12。承接 [整步归因与原型](TrafficWholeStepPlan.md)，本批只正式化共享道路查询表的比较器，并增加可重复的完整游戏窗口验收。**普通 Unity Editor、Development 和 Release Player 默认启用，不需要 Benchmark 或原型开关。** 不进行其他交通重构。

## 实现与回退

`CityRoadKeyComparer` 是无状态、复用的比较器。`CityRoads.EnsureLookup` 默认为 `vehicleConnections`、`roadLookup`、`structureLookup` 使用它；只对打包 long 的哈希进行 64 位混合，`Equals` 仍是完整 `x == y`。键构造、正反方向、道路列表顺序、节点与边 ID 都不变。原 XOR 哈希把当前2640个键聚集到74个哈希，最大1254个键共用一个哈希；本批消除的是共享查表层的退化，多个消费者同时受益。

这些表仍是道路派生数据，沿用既有 `Changed()` 失效/重建与读档重建流程；没有增加缓存真相、改变存档格式或需要迁移。车辆依次更新、选道/换道、路口预约、行人冲突、居民/货运/服务任务、0.05秒固定步和倍速调度全部保留。没有减车、跳步、降低精度或引入 ECS/Jobs/GPU。

回退：在 Editor 的 Scripting Define Symbols 或 Player 编译参数加入 `CITY_ROAD_KEY_HASH_DISABLED` 并重新编译，即恢复原三个容器的默认比较器。`RunTrafficBenchmark.ps1` / `Verify.ps1` / `RunFullWindowPerformance.ps1` 提供 `-LegacyRoadKeyHash`。原 `CITY_ROAD_KEY_HASH_PROTOTYPE` 不再控制产品；旧 `-RoadKeyHashPrototype` 命令兼容但提示优化已经默认开启，重放脚本的旧参数保留为别名。无需为正常游戏配置任何优化符号。

## 交通步骤原生配对

Unity 6000.6.3f1，Windows / i5-14600KF，固定第128日城市 `Tools/Benchmarks/traffic-city.json`。相同源代码与固定输入，独立 Development Benchmark Player，无渲染/UI；fixed、1×、3×各3轮，每轮40步预热+300步采样，每侧2700步。没有开启本轮额外归因开关。GC取原生 Profiler 的不重叠模拟模块分配，不使用不可靠的托管分配 API。

| 模式 | 原比较器均值 ms/步 | 默认新比较器 | 降低 | P95 前→后 | P99 前→后 |
| --- | ---: | ---: | ---: | --- | --- |
| fixed | 42.847 | 7.691 | 82.05% | 54.604→10.937 | 102.842→14.338 |
| 1× | 43.246 | 8.206 | 81.02% | 55.449→11.767 | 103.638→14.891 |
| 3× | 42.593 | 7.896 | 81.46% | 54.662→10.923 | 102.147→14.859 |

两侧 GC 都约 **807955 B/步**，没有分配收益。2700步任务数量、9轮最终城市状态和既有主要模块 Calls 相同，原生帧 Calls 与实际模拟步对应。普通与回退版本均保留已有 Following 索引和半径缓存，比较只改变道路哈希。

## 完整游戏窗口实测

使用普通 `SampleScene` 的真实 `HarborCityGame.Update`、地形、建筑、车辆、相机、UI和渲染。1440×900、相同视角/缩放、VSync关、目标60 FPS，固定输入、40步预热、暂停3秒稳定画面；1×/3×交替各3轮，每轮实际运行300个0.05秒步。通过 computer-use 查看并激活真实游戏窗口，所有记录帧 `focused=1`。帧时间包含普通 Update/OnGUI/渲染/帧率等待，计时后才输出 CSV/JSON。最后测试帧仅截到300步的有限终点，不改变其他测试步骤。每轮测量任务195–255，没有人为固定任务数量。

**Release（未启用 Profiler）：**

| 模式 | FPS 原→新 | 实际模拟秒/真实秒 原→新 | P95 帧 ms 原→新 |
| --- | --- | --- | --- |
| 1× | 9.20→59.35 | 0.988→1.001 | 299.22→16.85 |
| 3× | 1.29→58.63 | 1.137→2.998 | 1046.03→19.94 |

**Development（Profiler开启，单独比较，不与Release混算）：**

| 模式 | FPS 原→新 | 实际模拟秒/真实秒 原→新 | P95 帧 ms 原→新 |
| --- | --- | --- | --- |
| 1× | 5.65→59.30 | 0.962→1.001 | 373.38→16.96 |
| 3× | 1.19→57.19 | 1.052→2.992 | 1127.29→23.37 |

每组配对源哈希相同、配置相同，六轮步数均300、帧步数合计正确，六轮完整城市 SHA256 全部相同：`F711C2C140247EE70F1695D5B1CFC343B27E64DF7E08E2C09298D3DCCA6023E1`（仅序列化比较副本的分数累积器归零，不修改真实模拟）。`active-comparer.txt` 验证运行中的正常游戏道路表确实使用新比较器，而回退构建使用旧比较器。

完整窗口 GC：Release计数器不可用，记为-1。首版Development窗口采样错误地在 `Reset()` 后未重新 `Start()`，因此返回零，属于采样工具错误，不能作为无分配结论。根据 [Unity Reset API](https://docs.unity.com/en-us/engine/6000.5/script-reference/unity/profiling/profilerrecorder/reset) 修复为重启采集，并增加测量外1 MiB已知分配控制，最终开发构建两侧控制均通过，重新完成六轮配对。旧零值原始文件保留在 `road-hash-*-dev`，不用于最终GC结论。最终数据在 `road-hash-*-dev-v2`。

完整窗口开发V2分配（十进制MB）：

| 模式 | MB/帧 原→新 | MB/模拟步 原→新（包含整个画面/UI） | MB/真实秒 原→新 |
| --- | --- | --- | --- |
| 1× | 4.482→0.554 | 1.312→1.642 | 25.258→32.868 |
| 3× | 17.704→1.168 | 1.003→1.116 | 21.099→66.812 |

这些是全游戏帧分配，不是纯交通每步分配。优化后每秒渲染/UI帧和三倍模拟推进更多，因此每帧分配下降但每真实秒总分配增加，不能用前者声称算法减少了GC。纯交通步骤原生配对仍约0.808MB/步、不变。更快的游戏仍有值得解决的分配压力。

发布版配对在GC控制/重启修复前构建，GC不可用；开发版V2配对包含控制和重启，两组各自源完全一致。之后仅为不可用Recorder增加Valid防护，已测开发路径不变。这些辅助诊断变化不会改变正式哈希实现与普通Player模拟。

## 正确性与可维护性

- 默认与回退全量模型回归均通过，覆盖交通、车道、预约、行人、货运、服务、道路编辑及存档。
- 10000键等值检查，加2272专项断言：完整符号位/整数边界/反向；20000个稀疏大ID节点、39700条边；单行/双向、车道/类型、动态修改/删除/重建与升级、大路网序列化读取，验证默认比较器及显式回退。
- 340个完整逐步城市状态（含真实 accumulator）完全一致；另外原生交通9对最终状态、Release窗口6对、Development窗口6对一致。
- 普通游戏构建不带 Benchmark、原型、窗口测量符号，编译成功；独立建造/交通/服务/存档与26日长期运行通过：第26日人口36、就业24、实际消费258件、垃圾平衡误差0，验收结果见归档。隐藏自动验证不替代人工操作验收。

测量辅助代码只有 Editor 或 `CITY_TRAFFIC_WINDOW_MEASUREMENT` 才编译；启动必须显式命令行选择。普通Editor的探针未挂接，仅空检查，普通Player连此调用也不存在。正常玩家没有新UI、自动加载、强制GC、文件写入或时间截断。

## 复测与证据

```powershell
./Tools/Verify.ps1
./Tools/Verify.ps1 -LegacyRoadKeyHash
./Tools/CompareTrafficIndexReplay.ps1 -CompareRoadKeyHash
./Tools/RunTrafficBenchmark.ps1 -OutputDirectory Logs/TrafficBenchmark/new-legacy -LegacyRoadKeyHash
./Tools/RunTrafficBenchmark.ps1 -OutputDirectory Logs/TrafficBenchmark/new-default
./Tools/CompareTrafficBenchmark.ps1 -Before Logs/TrafficBenchmark/new-legacy -After Logs/TrafficBenchmark/new-default
```

窗口测试：对不同输出目录分别 `RunFullWindowPerformance.ps1 -Stage Build`（对照加 `-LegacyRoadKeyHash`），再 `-Stage Run`；观察、激活实际返回的游戏窗口并保持前台，收到READY后创建输出目录中的START.txt。自行完成六轮后退出。`-DevelopmentBuild` 开启开发构建，不能与发布构建当同一组配对。`CompareFullWindowPerformance.ps1 -Before ... -After ...` 检查配置/源/状态/全部步数/焦点再输出配对。不要在测量时运行编译或其他重CPU工作。

轻量结果与回归日志：[本批归档](Benchmarks/2026-10-12-RoadHashProduction/README.md)。本机完整Player、原始录制/模块在 `Logs/TrafficBenchmark/road-hash-production-*`，完整窗口文件在 `Logs/FullWindowPerformance/road-hash-*`，不提交巨大二进制。

## 限制与下一步

本存档达到约7–8ms/步及正常3倍推进，不代表任意规模都达到10ms，也不是用户当前Editor城市的配对FPS测量。用户实际Unity城市保持运行并刷新过源码，未覆盖其存档。普通Editor和Player默认启用由无条件产品入口、全量检查及正常场景实际构建共同确认。

剩余约0.808MB/步分配、服务/居民/调度和更大城市的局部邻域查询仍需按新基准研究；先扩展规模基准与热点采样，不沿用旧热点时间预算相加，不在本批改变车辆更新顺序或并行语义。现有渲染管线剥离警告保留，不借本批做无关画面修改。
