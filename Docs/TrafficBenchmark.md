# 可重复交通性能基准

2026-10-12。基准是专用空场景 Unity Development Player，读取存档副本，不接管用户城市，不写回原存档。没有城市模型渲染/UI，因此测量原生交通模拟与时间推进，不宣称完整游戏 FPS。未启用 Deep Profile、ECS 或 Job System。

## 固定输入与协议

- 仓库固定存档 `Tools/Benchmarks/traffic-city.json`：第128日城市，来自本地已有保存文件，不每次从当前正在运行的城市重新取样。后续更换场景使用新文件和新基准名称，不能覆盖历史输入。
- Unity 6000.6.3f1，Windows Standalone Development；VSync关闭，目标60FPS，Time.timeScale=1，maximumDeltaTime=0.33333334，固定交通步0.05秒。细分Profiler标记开启，两侧配置相同。
- fixed / 1x / 3x 各重复3轮，每轮重新加载同一存档、进行相同40步预热、测量300步。fixed每Update请求一固定步；1x/3x使用实际Time.deltaTime乘倍速，保留原Advance调度。最后帧仅截到剩余测试步数，标记partial，不拿它评估正常帧时延。
- 每轮开始预热之前执行GC清理，不在测量步骤中强制GC。输出和JSON序列化在步骤计时外执行。专用编译符号CITY_TRAFFIC_BENCHMARK控制逐步计时探针，普通游戏中不存在该探针调用。
- 记录实际每步完整Simulate时间（含Movement、服务、居民和调度）、GC分配及步前任务数；mean/P95/P99使用全部300步，分位数为排序后的nearest-rank。Profiler原生数据提供子模块Total/Self/Calls/GC；不将父子Total相加。
- 输出configuration含存档SHA256、代码版本、CPU/OS/Unity和参数。两个模式的GC/缓存预热相同；任务数随固定模型演进，不人为冻结/删车，因此需比较相同步序列而非只比较初始数量。
- 每轮保存最终原生JSON和SHA256。跨帧分组影响小数累积器，因此仅在比较副本中将traffic.remainder归零，原模拟仍保留其真实值；其余任务、车辆、居民、货物、时间与预约依原系统重建。两边逐步任务数也必须一致。

## 运行

在仓库根目录运行：

```powershell
./Tools/RunTrafficBenchmark.ps1 -OutputDirectory Logs/TrafficBenchmark/baseline
./Tools/RunTrafficBenchmark.ps1 -OutputDirectory Logs/TrafficBenchmark/v2-radius-cache
```

脚本在Temp/TrafficBenchmarkProject复制Assets/Packages/ProjectSettings，复用独立Library，构建专用Player、隐藏运行、退出后通过隔离Unity读取原生录制并导出所有有效采样。不会关闭/替换当前Unity场景。

可用Stage=Build/Run/Export拆开执行。Run要求对应目录有构建好的Player，且不允许覆盖已有summary.csv。保留baseline的Player，未来配对可再运行相同旧二进制，不依赖当前源码。输入可通过SavePath明确选择；两侧必须相同hash。

输出：steps.csv逐步数据；frames.csv实际delta/unscaledDelta/maximumDelta/请求时间/步数/耗时/任务/partial；summary.csv每轮统计和状态hash；traffic.raw原生录制；modules.csv按Benchmark.Frame计数器映射的主线程子模块路径；state-N.json最终比较副本；build/player/export日志与PASS/FAIL。

本机Unity原生Player的GetAllocatedBytesForCurrentThread未报告分配，而Profiler明确记录有GC。工具通过已知分配探测此API，若不支持，步骤CSV与原始summary的托管GC字段记为-1，不能理解为0B。GC验收取原生Profiler中互不重叠的Movement、Services、ResidentsAndIndustry和Dispatch之和除以实际步数。初次baseline目录是采集试验，托管GC为0不可用；正式配对使用v2-radius-disabled与v2-radius-cache目录。

导入前显式扩大历史帧容量，比较脚本要求所有有步骤的Ordinal都被Profiler覆盖且Movement Calls=实际步数；任何缺帧或计数错位直接失败，不输出成功结论。build-source.txt保存构建时各C#文件SHA256，build-options.txt保存编译开关，不只依赖HEAD描述带工作树的构建。

半径缓存的同代码控制组使用：

```powershell
./Tools/RunTrafficBenchmark.ps1 -OutputDirectory Logs/TrafficBenchmark/v2-radius-disabled -DisableRadiusCache
./Tools/RunTrafficBenchmark.ps1 -OutputDirectory Logs/TrafficBenchmark/v2-radius-cache
./Tools/CompareTrafficBenchmark.ps1 -Before Logs/TrafficBenchmark/v2-radius-disabled -After Logs/TrafficBenchmark/v2-radius-cache
```

控制组只编译掉半径缓存，执行保留的原CalculateCrossingRadius算法；运动、等待、精度和其他采样均相同。配对汇总中的P95/P99为三轮各自分位数的平均，不是将900步合并后的分位数；逐步原数据可另算合并分布。

计时/分配探针和Profiler有开销，专用空场景也少于完整游戏负载。先用相同诊断配置配对；需要发布版容量结论时另测关闭细分的构建和真实游戏。启动、预热、序列化、空闲帧不纳入模块配对；Ordinal>0不代表一定有交通步，需与frames.csv连接后筛steps>0。

## 验收

先核对输入hash、环境和配置；每轮精确300步、相同步序任务数、最终状态hash一致，再比较时间、GC和子模块Calls。相同模块出现在不同父路径时合并需保留父子关系。1x/3x比较实际墙钟、帧步数和请求模拟时间，而不是把3x的单帧总耗时简单除以3。

该存档测试仅覆盖当前规模；不能据此宣称支持3000车辆。大规模、不同密度、施工与冷缓存场景仍须扩展，但不应混入本轮算法改造。

墙钟统计来自profiler-frames.csv中原生帧开始时间差（末尾无下一帧时用原生frameTime），不是把Time.unscaledDeltaTime直接当作当前帧耗时；后者对应前一帧，短样本端点会偏差。live-summary包含最后截断帧，确保每模式每侧900步相同；正常帧时延分析另排除partial。存档携带的初始remainder保留，因此请求秒数可能略少于实际300步的15秒。

CompareTrafficBenchmark默认只允许CrossingRadius.Compute调用数改变，其余主要采样节点要求Calls一致。未来其他优化可通过ExpectedChangedMarkers明确列出预期减少的计算节点，但Movement步数检查不可跳过；任务数和最终状态检查始终保留。
