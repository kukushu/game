# MS-010：完整居民请求影子验收

冻结基线 `26d560073660285a7ca1a257f7baaccbc49d73db`；实现 `3e86bb0`。执行分支 `codex/million-scale-execution-20261031`，未合并正式开发分支。所有新增调度均在 Tools / Temp 诊断源码，正式 AdvanceRequests 仍全员轮询、原顺序判断并派发。唯一运行时代码变动是补齐六个已有事实的写通知；不是启用事件驱动。

## 结构与数据流

复用原 CityWakeQueue、ResidentWakeShadow、PrepareResidentWakeShadow 与 VerifyResidentWakeShadow。新增 `-Full` 模式：

- 实际候选按原 Members 顺序接受一次原循环计算的有效通勤估计，调用 MS008 Departure / ShiftEnd 计算下一次检查 Tick；没有新增寻路、重新报价或另一套居民权威数据。
- trip/retry/visit、岗位、雇主资金、健康/工作资格、住址/成员关系通知只标脏。日切、日长、路网等 MS009 全局信号重新分类；端点信号通过 home/work/current-location→成员派生依赖定位，包含原来不存在/不可达的目标。
- 非候选的 oracle 访问**禁止重新学习计划或更新依赖**。正式轮询仍独立执行，每次真实 Request 在写重试时间之前检查候选资格和原顺序，因此漏通知不会被全员 oracle 悄悄修好。
- 同轮回调唤醒较后成员时允许其本轮处理；已访问成员留到下一步。同一次请求改变自身状态后丢弃旧时间计划，在原安全边界重新分类。
- 开发模式还会影响商业班次，即使资金资格不变也必须失效。影子在请求边界/访问结束检查该政策值，变化时保守标脏全员；不能只订阅资金门槛事件。
- 没有证明可休眠的早期资格失败类别继续保守轮询；本轮验证的是候选完整性，不声称所有居民都已只按事件调度。结构变化仍复用旧影子的安全全量重建，正式接管前需评估增长场景维护成本。

冷读不保存队列/端点关系；新会话依原生成员顺序冷建。固定0.05秒、全部欠步、实时车辆准入、安全、工资/库存/任务规则未动。

## 实际发现并修复的通知缺口

边界测试在居民已等待下班时直接修改合法 public `arrivedDay`。正式轮询按原规则立即要求回家，影子却没有收到失效通知：**tick1/citizen1 的 Shadow missed request**。失败日志保留于 `Temp/MillionScaleExecution/ms010-tripday-gap.log`，未降低断言。

`CityResidents.cs` 原来的 `departureDay`、`arrivedDay`、`observedHome`、`observedWork`、`observedRevision`、`lastCommute` 改用既有 CitySavedFact 属性机制，经唯一 EntityOwner 发送 TripDay / CommuteObservation 失效。整数相同值可跳通知；float 原样先存再通知，保留负零、NaN载荷与无穷位模式，不改变原计算次序。原名、类型、保存排序及格式18均保持。

生产中的 Depart/Arrive 通常同时改已有守卫和位置，会间接使计划失效；直接事实写入反例证明仅依赖这种伴随关系并不完整。当前正式轮询原本不会漏掉该请求，此修复是为未来稀疏执行补契约，不是声称当前游戏已经丢车。源码检索未发现这六个成员的 ref/out 或按名 GetField 依赖。没有新增旧档迁移、适配器或保存字段。

## 验证结果

实际命令（.NET10 x64 Release 模型）：

```powershell
Tools/VerifyResidentWakeShadow.ps1 -Full -Results Temp/MillionScaleExecution/ms010-complete
Tools/VerifyResidentWakeShadow.ps1 -Full -Growth -Results Temp/MillionScaleExecution/ms010-growth-accepted
Tools/VerifyResidentWakeShadow.ps1 -Full -ControlsOnly -Results Temp/MillionScaleExecution/ms010-fact-controls
Tools/VerifyResidentWakeShadow.ps1 -ControlsOnly -Results Temp/MillionScaleExecution/ms010-guard-controls
Tools/VerifyCityCore.ps1 -Results Temp/RoadCore/ms010-core.csv
Tools/VerifySimulationBudget.ps1
```

更正：初版脚本写出了26d5600原字段版CityResidents，但未将其传入旧侧SourceOverrides，因此原回放实际是同属性模型的原轮询与影子。MS011复核已修正，实际冻结字段版重跑于ms011-ms010-freeze-corrected及ms011-ms010-growth-corrected；152个状态点、请求摘要与原结果一致。旧日志保留。影子使用属性版与临时插桩CityCoreResidents。输入仍为原三万人格式16文件的既有冻结读取器导出的同输入18测试副本；玩家文件SHA256仍为 `3F49BD1A02879BB686C24D5517902CC581C657220CE2B955107D89B6FA870B4E`，未覆盖、重新生成城市或增加运行时旧格式兼容。

| 回放 | 请求阶段 | oracle成员检查 | 影子候选 | 真实请求 | 漏请求 |
|---|---:|---:|---:|---:|---:|
| 30k预热2400 + 跨日3000 + 两侧cold80 + 小城生命周期/在途删路 | 7181 | 166806484 | 95100750 | 995818 | 0 |
| 混合增长6200 + 重划/拆分4000 + 30k4000 + 各组cold80 | 14680 | 124847840 | 65250752 | 821794 | 0 |

两组分别26与126个完整原生18状态检查点精确相同，共152点；正常观测3000步的482141次请求（Tick、居民、起终点、返程标志、派车结果ID）稳定二进制摘要一致；最终原生存档字节一致，各会话冷热续跑一致。另有26个状态点中的实际在途删路100步及cold80检查；没有取消原恢复规则。

定向控制通过：同数量增删、解绑后写入、同步回调唤醒较后成员、公用setter提前重试并实际发车；资金耗尽、病、岗位释放、原不可达入口恢复、开发模式改变、搬家导致回家、直接到达日修改及通勤观测增大导致提前出发；六事实通知与float位模式。

负控通过：测试反射绕过 retry、sick、资金余额写通知，或切断端点消费通知，均被正式 oracle 的原 missed-request 检查检出。没有通过全员重学来修补负控。真实大城两套回放没有局部入口变更，所以计数 localChanges=0；局部依赖的有效性由明确不可达入口恢复定向案例验证，不能冒称已在大城测到局部收益。

Unity随附Mono的23627综合、10000岗位索引、1522路网/事务、83分区、460474水电断言及101预算检查通过。没有新的编译错误；已有保存反射SYSLIB0050警告保留。未运行新的Unity窗口FPS或普通Player吞吐验收，也没有百万居民游戏验收。

## 影子维护成本与边界

并行验证运行的墙钟成绩不用于对比。另在自有验证任务结束后，使用同一已编译 old / shadow 二进制顺序独立回放，均2400步预热、3000步观测。计时只包 Advance(.05)，检查点序列化、Validate、冷读与断言不计入；保留两侧相同的请求摘要采集。结果是**同属性模型的诊断开销**，不是字段版与属性版的性能对比，也不是Player成绩：

| 指标 | 原轮询（同属性模型） | 原轮询＋完整影子 |
|---|---:|---:|
| 平均固定步 ms | 2.9141 | 5.0110 |
| P95 / P99 ms | 4.5059 / 8.3605 | 6.9337 / 10.4898 |
| 最大固定步 ms | 29.5252 | 26.8882 |
| 托管分配 bytes/步 | 34295.16 | 34292.30 |
| 预热后总托管存活 bytes | 251778888 | 258466200 |

影子额外约2.097ms/步、6.38MiB总托管存活；分配基本相同，几个字节差异不视作收益，单次最大值降低也不构成改善长帧的证据。影子保留原循环并增加字典查询/逐成员断言，本来就不应被当作优化后的正式执行。

30k队列本体三数组逻辑峰值480000 bytes，端点→成员关系峰值50022条；该480000不是完整调度器内存，额外数组、成员ID映射、端点集合等计在上表总体存活差值中。增长回放重建9次，普通/删路回放6次，见[影子CSV](MS010-Shadow.csv)。这也是MS011/012不能机械照搬整个诊断实现的原因。

候选约为原访问的57%，并不是已经省掉43%的CPU。真实维护、原业务判断成本、GC、增长重建及普通Player收益必须在MS011–013实测；本轮只完成安全影子门槛，不合入正式分支、不激活资金等待或实际稀疏执行。

数据：[完整状态/请求摘要](MS010-State.csv)、[独立顺序测量](MS010-Cost.csv)、[候选与内存计数](MS010-Shadow.csv)。原始日志/存档副本/二进制留本地ms010-*。独立测量在ms010-accepted/isolated-old与isolated-shadow；后来补入的在途删路用例在计时区间之后，完整正确性以ms010-complete为准。

MS010 DONE；下一项MS011为唯一IN_PROGRESS，先让可靠守卫类别真正接管执行并与原oracle独立对照。完整时间类别接管归MS012，真实Player收益归MS013。
