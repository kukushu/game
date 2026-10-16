# 三万人真实窗口：分项归因与局部修正

2026-10-16。使用用户刚观察的同一繁忙保存：`Temp/CityScenarios/challenge30000-traffic10000/threshold.hcity`，SHA256 `e78c47daaffcb989ffc57eb0048575c3b6aa6dc66386dd6689c9c8eacd5db7d8`，30000真实居民、初始10887活动车、7806建筑。所有阶段不改入口、人口、业务、车辆和日长，不拿之前物流布局实验替代窗口测量。

## 基线：真实 Development Mono Player

同一输入分别冷读，1440×900、vSync关、目标60FPS、8秒预热/60秒采样。暂停、1×、3×顺序运行；无其他本批后台仿真或构建并行，普通编辑器/VS Code等仍打开，不声称锁频或独占机器。各倍率预热推进不同，因此测量区间不是相同tick，不能把性能差异说成同一业务区间的等价算法比较。

|倍率|FPS|P95/P99/最大帧ms|真实模拟倍率|平均分配MB/帧|最大固定步/帧|
|---|---:|---|---:|---:|---:|
|暂停|19.385|58.494 / 62.569 / 69.458|0|2.427|0|
|1×|9.063|170.744 / 942.649 / 2180.716|1.010|5.272|43|
|3×|0.0734|24180.786 / 24180.786 / 24180.786|2.077|446.357|1098|

3×只有5个测量帧，不把P95/P99当大样本稳态分位数；它们就是这组少数严重长帧。Update推进平均13.504秒/帧，车辆同步29.31ms/帧，明确是追步占据长帧。1×模拟54.44ms/帧，车辆同步3.61ms；暂停模拟0.001ms、车辆4.19ms。对应实际完成的Unity模拟步约24.43ms/步（1×）、23.86ms/步（3×），仅本测量区间，非.NET成绩，也不是整城长期每步保证。

完成帧的clock.pendingSeconds很小不代表没有时间落后：旧宿主把正在执行的长帧墙钟消耗到下一帧才提交。实际倍率才反映未能追上的情况，不能用这个小pending值宣称正常3×。

## 原始 Unity Profiler 归因

另跑同城暂停15秒采集非Deep Profile二进制，234个可读帧。原始约1.30GB留`Temp/CityScenarios/window30k-raw-before/0x/window.raw`，不提交；离线导出工作集峰5.16GB、12.35秒，受300秒/8GiB限制。Profiler开启本身增加开销，不能把该轮15.52FPS当正常FPS。导出完整hierarchy/threads留Temp，轻量模块汇总在`Docs/Benchmarks/2026-10-16-CityWindow30000/raw-paused-modules.csv`。

- `City.Views.UI`平均28.63ms/记录帧（自身25.82ms），约1.99次调用，分配2.413MB/帧，接近整帧2.427MB。界面就业表达式每次绘制统计就业两次、失业一次，遍历三万人/一万家庭；Citizens的SelectMany会反复产生家庭成员枚举器，UI下约6万次GC.Alloc/帧。不是“所有卡顿都是GC暂停”。
- `GC.Collect`平均0.479ms/记录帧，另有增量GC。它们不代表所有暂停的完整合计；不将相同各代CollectionCount相加。
- 渲染线程`DrawBuffersBatchMode`自身18.47ms/记录帧；`Semaphore.WaitForSignal`43.12ms主要是等待，不能作为忙碌CPU成本累计。主线程还有ExecuteRenderQueueJob、RenderLoop.CleanupNodeQueue等明显成本。
- 无完整Profiler的暂停窗口FrameTimingManager有效GPU样本977个，均22.28ms、最大34.19ms；异步GPU值可能跨帧。Render Thread的简易ProfilerRecorder不可用（-1），由原始线程导出补证，不填0。不把并行GPU、渲染线程和主线程时间直接相加。

因此先针对已证实的UI统计重复工作做单独修正，随后评估有预算且不丢步的交互调度；车辆/渲染批量化尚未实施。单个真实模拟步自身可能超过帧预算，不能承诺仅加预算就获得60FPS或正常3×。

## 诊断边界与重跑

正常宿主增加静态UI采样节点；只在场景探针打开时读取分项Stopwatch，观察者不Advance、不改领域事实。采样记录全步数、车辆对象数、真实tick、已请求欠步、CPU/GC/GPU及未知值；原始Profiler与正常性能分开。

`Tools/RunCityScenarioWindow.ps1`支持显式Player路径、暂停倍率和可选15秒原始采样，默认仍为1×/3×60秒。必须用新结果目录；窗口受墙钟和工作集上限约束，终止仅杀本次自有子树。`Tools/ExportCityWindowProfile.ps1`只在退出采样后离线读原始记录，不操纵主工程Editor；读取真实Main/Render线程层级。源码、Player managed DLL和输入SHA保留，历史现场不覆盖。

当前阶段的代码/数据所有权不变：CitySimulation拥有时间，World拥有交通物理，业务系统拥有钱货，Unity适配器只拥有计时和显示对象。更详细的后续修正与配对结果在下方按批次补充，不把计划写成已完成。
