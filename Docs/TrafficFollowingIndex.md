# CityFlow 借鉴第一批：有向边局部跟车查询

2026-10-12。源码对照与分阶段路线见 [CityFlowTrafficDesign.md](CityFlowTrafficDesign.md)。本批只解决 `Move.Following` 每个近邻边反复遍历全城任务的问题；已有 CrossingRadius 缓存继续使用。没有采用 CityFlow 的车速公式、shadow 实体、换道优先级、多阶段状态提交或多线程。

## 数据与生命周期

`CityTrafficIndex.cs` 的 DirectedTrafficIndex 属于一个 CityTraffic 实例，是派生查询数据：有向 `(from,to)` → 引用实际 TrafficTrip 的复用 List；另有 trip → 旧键的成员表。包括行人、停留和换道任务，查询仍使用原来的状态、车道双占用和距离过滤，不改变实体真相，不进入存档。

每个 Movement 开始从 State.trips 构建 O(N+B) 索引；重用已分配桶和成员字典。道路实例或 revision 改变时清除旧键，下次完整重建，避免长期施工积累失效键。Movement 外查询回退全表，下次构建接纳居民、服务、道路编辑、读档产生的状态变化。

车辆仍按原 ToArray 快照顺序执行 AdvanceLaneChange → AdvanceStopWait → Move，Move 返回后立即同步该车最终有向边。即使一轮跨多个短边、返程或重新寻路，后面的车辆仍观察到刚完成的真实状态；本车 Move 内原跟车查询排除自身，因此不需要在每个跨边点提前修改自身桶。End 放在 finally 中，不遗留活动索引。

原 AddTrip 加入活跃索引；原各处 Remove 调用集中到 RemoveTrip，只有实际成功移除才更新桶；Refresh 不会把已删除的快照对象重新加入。家庭迁出 RemoveAll 等发生于 Movement 外，依然使用原逻辑，在下一步构建接纳。不重新设计失败、服务调度、居民工资或货物守恒规则。

只有 Following 的候选来源改变，仍沿原剩余路线的距离窗口查多个短边，使用原 `Behind`、`TripLane`、`OccupiesLane` 和 float 运算。其结果是所有有效候选的 Min，桶加入顺序不影响归约；这一结论不适用于依赖先后顺序的路口预约或发车。本批没有把局部桶用于那些规则。

## 复杂度与边界

原查询近似 O(N²k) 候选检查；当前为 O(N+B+ΣkK)，K 是附近边内任务数量，加上迁移时局部 List.Remove 成本。没有宣称任意密度下 O(N)：所有车集中在同一边时，仍可能接近原扫描规模；桶不排序，邻居查询仍局部线性。先确认边索引收益，再逐项研究车道/节点邻域、有序集合和分段维护成本。

独立候选测试保持每边10个任务：100/1000/3000任务，原候选10000/1000000/9000000，局部1000/10000/30000。混合步行、停留和双车道状态，逐点与扫描 oracle 相等；额外集中流量检查保留全部100候选。它们是索引与复杂度检查，**不是3000辆完整城市实时性能验收**。

`City.Traffic.Movement` 下新增静态 `City.Traffic.Index.Build` 和 `City.Traffic.Index.Sync`，原 `Move.Following` 保持位置。开发环境有采样，普通发布构建没有这两个细分 scope；索引功能在普通构建仍启用。`CITY_TRAFFIC_EDGE_INDEX_DISABLED` 回退原候选全表，只有同代码开关不同，适合配对验证。

## 可重复验证

```powershell
./Tools/Verify.ps1
./Tools/CompareTrafficIndexReplay.ps1
./Tools/RunTrafficBenchmark.ps1 -OutputDirectory Logs/TrafficBenchmark/cityflow-following-v2-scan -DisableEdgeIndex
./Tools/RunTrafficBenchmark.ps1 -OutputDirectory Logs/TrafficBenchmark/cityflow-following-v2-index
./Tools/CompareTrafficBenchmark.ps1 -Before Logs/TrafficBenchmark/cityflow-following-v2-scan -After Logs/TrafficBenchmark/cityflow-following-v2-index
```

已使用的输出目录不可覆盖，重测使用新目录。基准仍是 [固定存档协议](TrafficBenchmark.md)，两侧半径缓存开启，任务不减少，精度/步长不变，每模式3轮×300步、40步预热，原生细分采样开启。输入275任务，测量期间195–255任务。空场景 Development Player 没有城市渲染/UI，不代表真实窗口 FPS 或发布版容量。

新的分轮原生录制解决单个数GB录制导入时 Unity 内存压力丢帧，完整校验前不得发布部分结果。旧 cityflow-following-scan/index 是缺帧试验，保留但不用于本批收益。正式组两个源码 hash 表相同，仅编译索引开关不同。

340步完整状态重放（40预热+300步），每步完整 JSON 的 SHA、任务数及小数累积器均与扫描组相同；此正确性重放的序列化在模拟外，不参与性能数据。原生配对另检查2700步任务数量、9轮最终完整状态（仅按既有协议归一比较副本的 remainder）、所有测量帧 Movement Calls 与实际步数、所有既有模块 Calls。

测试、正式配对结果及剩余瓶颈见下文；完整原生调用路径和录制保留于本机 Logs，轻量证据归档到 Docs/Benchmarks。

## 正式结果

Intel Core i5-14600KF、Unity6000.6.3f1、Windows11。固定输入 SHA256：46DF3C9F9A9800808334C30BF1A5DA1A463106DD11DA8CEA780CC78582B115BE。下表每模式三轮平均，P95/P99是每轮 nearest-rank 分位数的平均；原始2700步可另算合并分布。

| 模式 | 完整Simulate均值前→后 ms/步 | P95前→后 | P99前→后 | 均值下降 | 原生GC前→后 B/步 |
| --- | ---: | ---: | ---: | ---: | ---: |
| fixed | 43.479 → 42.550 | 57.543 → 54.471 | 103.893 → 103.819 | 2.14% | 807797 → 807955 |
| 1x | 43.586 → 42.562 | 56.391 → 53.133 | 104.685 → 102.725 | 2.35% | 807796 → 807955 |
| 3x | 43.511 → 42.591 | 58.558 → 54.391 | 104.671 → 103.791 | 2.11% | 807796 → 807955 |

目标节点 Following 减少22.89%，整步仅减少约2%，不是“解决三倍速卡顿”。fixed P99基本不变，尖峰仍存在。GC增加约158B/步（0.02%），主要是后来出现的新有向边桶/容量增长；已预热的稳定桶循环零分配测试通过，但不能据此宣称完整城市动态模拟零分配。

### fixed模式子模块（900步归一）

| 节点 | Total前→后 ms/步 | Self前→后 ms/步 | Calls/步前→后 | GC前→后 B/步 |
| --- | ---: | ---: | ---: | ---: |
| Movement | 37.308 → 36.389 | 0.022 → 0.029 | 1 → 1 | 417769 → 417928 |
| Move.Following | 3.166 → 2.441 | 3.166 → 2.441 | 135.087 → 135.087 | 0 → 0 |
| Index.Build（新增） | — → 0.039 | — → 0.039 | — → 1 | — → 2 |
| Index.Sync（新增） | — → 0.025 | — → 0.025 | — → 216.680 | — → 156 |
| Move.ChooseLane | 10.751 → 10.661 | 9.874 → 9.789 | 182.123 → 182.123 | 28101 → 28101 |
| Move.LimitCrossingAdvance | 12.922 → 12.762 | 11.513 → 11.354 | 190.930 → 190.930 | 250424 → 250424 |
| Move.RouteValidation | 5.474 → 5.519 | 5.473 → 5.517 | 187.130 → 187.130 | 872 → 872 |
| Junction.ConflictPath | 1.038 → 1.017 | 1.005 → 0.988 | 13.207 → 13.207 | 31802 → 31802 |
| Services | 1.268 → 1.267 | 1.100 → 1.096 | 1 → 1 | 162200 → 162200 |
| ResidentsAndIndustry | 4.273 → 4.252 | 4.068 → 4.055 | 1 → 1 | 201265 → 201265 |

维护两节点总计约0.064ms/步，父子Total不能相加。Following的剩余耗时还包含原Behind/路线车道/距离计算，并非全部候选遍历；不能从候选数量缩减推断整个模块加速相同比例。没有改变ChooseLane或LimitCrossingAdvance，其小幅变化不归因于本批算法；未做随机交替的长期统计置信分析。

### 实际推进（每模式900步）

| 模式/版本 | 帧数 | 原生墙钟秒 | 模拟秒/真实秒 | Advance均值 ms/帧 | 平均步/帧 |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1x/扫描 | 1159 | 45.190 | 0.9958 | 33.846 | 0.7765 |
| 1x/索引 | 1250 | 45.164 | 0.9964 | 30.645 | 0.7200 |
| 3x/扫描 | 54 | 39.189 | 1.1483 | 725.185 | 16.6667 |
| 3x/索引 | 54 | 38.360 | 1.1731 | 709.864 | 16.6667 |

包含末尾截断帧，依原生帧开始时钟统计，所有帧映射及步数完整。1x基本实时，3x仍远低于请求的3倍；测量含Profiler二进制记录开销，无完整渲染或UI。不用CityFlow自身性能数字作为预期。

## 正确性结果与证据

- 4112项索引/局部oracle/生命周期/稳定桶零分配专项检查通过；最终全量模型回归包含车道、行人、路口预约、信号、道路编辑、服务重接、真实货运、存读与库存守恒，日志Temp/TrafficIndexFinalRegression.log。
- `CompareTrafficIndexReplay.ps1`：两种构建连续340步逐步完整状态相等，小数累积器也相等，见归档的replay目录。
- 正式原生A/B每侧2700步，步序任务数完全相等，9轮最终原生状态同为C8B7F9541645EBB0C3D4AD9E90F152EDEB228CC3F648EC868669B7C3ED9A62C3；所有既有模块Calls一致，全部测量帧Movement Calls与实际步数一致。
- 普通Unity Development Player构建与原生运行通过：真实换道动画/双占用/换道中读档续跑、路口/行人/高速合流、建路拆路与服务重新接近、货运/返回和原生JSON；长期续跑到26日，人口36、就业24、消费258、垃圾守恒误差0。证据Temp/TrafficIndexNativeValidation，本轮没有接管用户Unity地图。ProblemGUIUnverified.txt保留既有隐藏Player的GUI按钮输入覆盖限制，不把受控原生检查声称为用户实城人工验收。
- 可追溯轻量数据归档于 [Benchmarks/2026-10-12-FollowingIndex](Benchmarks/2026-10-12-FollowingIndex)。完整模块路径、frames与九个原始录制在Logs/TrafficBenchmark/cityflow-following-v2-scan及cityflow-following-v2-index，不把数GB raw加入Git。

## 下一批优先级

数据组织方案可以逐个推广，但实测 Following 并不是剩余最大的耗时。下一批先细分 ChooseLane/LimitCrossingAdvance 中尚未归因的Self（9.8/11.4ms），核实DistanceTo、Behind/Ahead、IsJunction和静态路线/连接推导；独立研究 RouteValidation（5.5ms）的完整失效协议。每次只选其中一个明确问题实施，再用同协议配对。

车道Headway/Space和路口邻域的局部索引路线保留；Space还看入向边、换道双占用和短边，不能简单把Following桶机械代入。先确认具体子模块成本和维护方案，不同时把所有全局扫描换掉。多阶段双缓冲、重新排序与并行仍需要另行说明影响并等待确认。
