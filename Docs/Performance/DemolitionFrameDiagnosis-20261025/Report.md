# 删路约200ms停顿：帧边界与内部阶段诊断

本轮只调查。正式运行源码、模拟规则、校验、存档格式均未修改。最重要的结论是：**此前工具的“最长帧”实际是两次协程恢复之间的间隔，跨越施工帧的后半段和下一帧的前半段。漏解释的约55ms主要是下一帧分区刷新，并非额外的车辆恢复开销。**

- 独立分支：`codex/diagnose-demolition-frame-20261025`。
- 基线：`codex/experiment-demolition-recovery-search-20261025` 最新 HEAD `0afa420759f833b4cb97966e624dd9f4917aef89`。
- 诊断工具提交：`b24c60c`；报告最终提交可由 `git log -1 -- Docs/Performance/DemolitionFrameDiagnosis-20261025` 查询。
- 没有合并、修改正式分支或覆盖其他实验。本轮没有“优化前后提速”成绩。

## 场景与计时口径

继续使用同一个真实玩家城市删前检查点：本地 `Temp/BatchRecovery-20261025/case-old-live/before.hcity`，SHA256 `0110347D2BC4E40E8CD98E841EE92F62D675935D076A21AE58D7F7A5A9ADB6CD`，tick642633、536辆车辆、删除物理道路87。来源为此前DE51实际存档正常推进20步；不是最新F52现场，不是新生成的三万/五万人城市。

Windows11/i5-14600KF、Unity6000.6.3f1 Development Player、1440×900、VSync0/目标60FPS，同一输入与相机，加载后8秒显示预热。冻结模拟时刻以便保留相同在途车辆和删除结果；编辑后新路网的索引构建是本次操作实际成本，未预先隐藏。没有同时运行其他测试Player/构建；用户原编辑器保持打开，本轮未操作它。

这是**受控暂停脚本施工**，复用上轮生产施工、入口重绑、恢复和显示调用；不是人工点击，也不是运行中50×测量。普通UI由道路编辑器触发施工/道路显示，入口恢复还受下一模拟步顺序影响，因此不能把脚本两帧分布直接当作所有真实点击的分布。没有新测1×/50×FPS或改变调度。

新增独立帧号、`Time.unscaledDeltaTime`、Main Thread Recorder和短段原始Unity Profiler。最终同一二进制三次不录raw复测，另一次录raw作调用层级与分配归因。**不录raw仍有诊断埋点，不是普通Release性能。** 保存、日志输出、完整断言、80步冷热续跑均在施工观察窗口之外。

## 约200ms究竟是什么

| 最终三次不录raw | 中位数ms | 范围ms |
|---|---:|---:|
| 旧工具式协程间隔 | 210.903 | 206.430–217.036 |
| 施工调用本身Stopwatch | 150.562 | 148.446–156.972 |
| 施工所在完整Main Thread帧 | 155.280 | 152.859–161.997 |
| 紧接着的完整Main Thread帧 | 57.925 | 56.449–58.751 |

以final1为例：施工发生在Unity帧485的常规Update之后，调用约150.56ms；帧486的Update先做约55.35ms分区刷新，协程才恢复。两次协程之间因此记录210.90ms。Recorder分别记录帧485为155.28ms、486为58.75ms；`unscaledDeltaTime`分别155.29/58.75ms。两帧总和也不应等于协程间隔，因为起止位置不同。

原始Profiler独立复核成功：final-raw记录的施工帧约178.48ms，下一帧约60.16ms，与Recorder178.47/60.15ms吻合。施工调用约173.30ms，含录制开销，不能与不录raw的150.56ms混比。其Render Thread主要在等待主线程命令，而非执行等长绘制。

因此，需要修正此前RecoverySearch报告“最长帧198.95ms”的名称：那是相同脚本的跨帧间隔，不是真正单帧CPU。**本轮153–162ms也不是新优化成果**，不能拿不同计时口径宣称200→155ms提速。玩家仍会感受到连续两帧阻塞。

## 施工帧：约150ms用在哪里

以下为三次不录raw的各范围中位数，子项包含在父项中，**不要重复相加**；各项中位数也不保证严格相加。完整逐次值、调用次数和self时间见CSV。

| 阶段/函数 | ms | 实际任务 |
|---|---:|---|
| HistoryAndConstruction | 58.16 | 撤销记录、RemoveRoad、扣款、水电更新 |
| └ World.RemoveRoad | 42.39 | 候选网络、车辆迁移规划、预约、入口、原子提交 |
| └─ CopyAndBuild | 31.44 | 复制Network DTO并构建候选Network |
| └── Network.Rebuild | 30.45 | 全路网车道、端口、连接及冲突派生数据 |
| └─ VehicleRoutes | 1.77 | 遍历536辆车、检查13603个剩余路线段，产生400个Remap、136个离路中断 |
| └─ Reservations | 2.33 | 检查迁移后的路口预约/冲突；不是Dijkstra |
| └─ Commit阶段 | 6.27 | RoadEditPlan及TryCommit、候选快照/完整验证/状态与占用索引提交 |
| └ Utilities.Refresh | 12.53 | 道路带管线，删路后重新计算供给连通和覆盖 |
| Bindings | 78.30 | 失效入口投影、RebindAccesses、批量恢复 |
| └ 入口Project合计11次 | 28.52 | 其中几乎全部是一次惰性空间索引更新 |
| └─ Spatial.Build | 28.44 | Shape表、变化比较、旧桶检查和变化段重建 |
| └ RebindAccesses | 4.42 | 复用现有Network的入口提交，保留完整安全校验 |
| └ ResumeStrandedVehicles | 45.62 | 132次规划尝试、统一候选与安全检查/提交 |
| └─ PlanStrandedResume累计 | 39.98 | 其中只有17次真正运行Dijkstra |
| └── Dijkstra累计17次 | 38.82 | 现有固定源双目标搜索，没有恢复到旧30次搜索 |
| 显式道路显示同步 | 13.63 | 比较全体Drivable、更新受影响显示块 |

全局快照3次、候选World3次、Validate4次、AssertSafety4次、Reindex7次、Network.Rebuild1次，与上轮一致。三个Snapshot各约1.4ms，完整校验/索引没有占掉几十毫秒；不能通过删除这些校验来解释或消除整段停顿。

### 路网重建内部

Network.Rebuild约30.45ms：创建Lane及基础信息7.52ms、端口配置2.14ms、LaneConnection构造12.07ms、连接冲突结构8.53ms，其余节点/路口分类很小。旧几何部分可以共享，但仍全局创建车道/连接对象及派生表。不是“所有时间都花在道路几何采样”。

### 空间索引内部

Spatial.Build约28.44ms：全局Shape建立3.66ms、Shape比较5.25ms、扫描旧桶寻找包含变化Drivable的段16.72ms、实际变化段生成1.35ms，其余主要是桶字典复制。已有桶/Segment复用，但查找脏桶本身仍扫描全局。11次入口点查询的真正局部投影仅约0.03ms，建筑/对外连接遍历等Bindings自身开销约2ms量级，不能把28ms称为逐栋碰撞查询。

### 原来施工事务之外的开销

`CityCoreConstruction.RemoveRoad`在World.RemoveRoad后，因`roadConduits`启用调用Utilities.Refresh。`SupplyLayout`以Network引用及水电内容签名复用；删路换Network后重建布局，约11.20ms，包含两次RoadConduits合计2.97ms，其余为覆盖、建筑邻接和连通分组。因此此前“RemoveRoad约55ms但Network只有30ms”的差额中，水电重算是重要一项，而非全是车辆验证。

## 下一帧：约55ms用在哪里

| 阶段 | 中位数ms | 证据 |
|---|---:|---|
| RenderZones整体 | 54.73 | Host.Update发现NetworkGeneration变化后执行 |
| Zoning.Cells | 22.31 | 含Geometry21.39ms，其余用途/占用同步 |
| └ 道路Run变化检测 | 1.24 | 遍历道路与候选Run比较 |
| └ Drivable变化检测/索引替换 | 11.18 | 遍历全网，构建now字典、比较几何并替换previousDrivables |
| └ 移除旧claims | 约1 | 更新被移除道路的占格依赖 |
| └ 局部CanPlace | 4.18 | 672次Project约2.03ms，其余几何/候选检查 |
| └ 局部Resolve | 2.39 | 重新确定接受的格子 |
| └ 全城Cells展平 | <1 | 保留原生长迭代顺序 |
| Views.Zoning | 32.09 | **不是32ms都在生成Mesh** |
| └ 全城格子重新分组 | 15.25 | 重建分块List和分组字典 |
| └ 比较块并按需Build | 8.61 | 绝大部分为所有Plot比较，真正BuildChunk两次合计约0.5ms |
| └ 提交并记住全部Visual | 8.11 | 更新块列表/字典及全量previous显示投影 |

确定的工作量：`LastGeometryFull=false`，只对6条道路重新执行局部放置和Resolve；没有重新生成全城格子。格子总数256787→256700。显示的Surface未失效，总593块，实际只重建2块。

原因在`HarborCityZoningViews.Sync`：只有`cells.Count==previous.Count`才进入SyncChanges。删路减87格后进入全城Regroup分支；该分支仍通过Same复用未变化Mesh，所以“每次都全量重建所有Mesh”不准确。**真正不局部的是全城重新分组、逐Plot比较、全量Visual重建。**

## 内存和渲染证据

MB以下按1,000,000 bytes。最终三次施工帧各约56.02MB，下一帧约22.58MB。两帧均未观测到GC.Collect标记，不能将这次停顿直接归因于GC回收，但大量临时分配仍是应减少的压力。

原生Mono的`GC.GetAllocatedBytesForCurrentThread`不提供有效计量，细分scope中明确保留-1。下面函数分配来自**独立raw Profiler**，不是冒用其他.NET宿主；含埋点开销，父子项不重复相加。

| raw归因 | 托管分配MB |
|---|---:|
| Network.Rebuild | 28.30 |
| Utilities.Refresh | 7.65 |
| Spatial.Build | 6.84 |
| 3次Snapshot合计 | 5.93 |
| Zoning.Geometry | 5.63 |
| └ DrivableChanges | 4.07 |
| ZoningView.Regroup | 16.65 |
| ZoningView.CompareAndBuild | 0.20 |
| └ 2次BuildChunk | 0.20 |

因此分区显示的分配主要来自全城临时分组List，而不是两个Mesh。索引的Shape表和全局派生Network重建也有明显分配。没有测Native Mesh内存/长期GC频率，不能将托管分配称为总内存或泄漏。

raw渲染线程主要耗在`Gfx.WaitForGfxCommandsFromMainThread/Semaphore.WaitForSignal`。这是等待，不可把其整帧长度再加到CPU施工时间；本场景未发现另一个约55ms的独立GPU/绘制任务。GPU时间不可用，Recorder的Render Thread为-1时以raw线程数据复核，不填0冒充测得。

## 候选优化与推荐顺序

主要源码定位：[World.RemoveRoad](../../../Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs#L855)、[Construction.RemoveRoad](../../../Assets/Scripts/Runtime/Simulation/Core/CityCoreConstruction.cs#L15)、[RoadBindings](../../../Assets/Scripts/Runtime/Simulation/Core/CityCoreRoadBindings.cs#L16)、[SpatialIndex](../../../Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreSpatial.cs)、[分区变化检测](../../../Assets/Scripts/Runtime/Infrastructure/Zoning/CityCoreZoningGeometry.cs#L140)、[分区显示](../../../Assets/Scripts/Runtime/Presentation/World/HarborCityZoningViews.cs#L43)、[水电布局缓存](../../../Assets/Scripts/Runtime/Infrastructure/Utilities/CityUtilityLayout.cs#L34)。

下表时间是**已测范围的优化空间上限/依据，不是保证能节省的成绩**，本轮均未实施。

| 优先级 | 方向 | 收益依据 | 复杂度/正确性风险 |
|---|---|---|---|
| 1 | 格子增删时仍局部维护显示分组，复用稳定格子身份和已有64单位块 | 针对约32ms显示整理和16.65MB分组分配；实际Mesh工作仅0.5ms | 中等；只动显示派生数据，保持格子顺序/占用真相不变。必须覆盖格子增删、跨块、用途/占用变化、地形失效和资源释放 |
| 2 | 空间索引直接定位变化Drivable所覆盖的旧桶 | 优先消除约16.7ms全局旧桶扫描，后续再处理Shape表3.7+5.3ms | 中等；利用旧/新几何桶覆盖或派生ID→桶表。删除段、连接几何/裁剪端点变化、桶边界与共享桶不可变性必须严格验证 |
| 3 | 分区变化检测复用可靠的Network变化集合 | 约11ms检测、4.07MB字典分配；真正局部处理仅6条道路 | 中高；必须包含连接/保护距离/几何变化及claims传播，保留全量回退，不能随意限定附近半径 |
| 4 | 道路显示同步减少全局比较；水电布局更精确复用 | 当前道路显示13.6ms、水电12.5ms，各自有上限 | 中高；道路分块本已局部生成，先细分其全局比较；水电删边可能影响远处连通分量，不得简单只算附近 |
| 5 | Network连接/冲突派生结构共享或局部更新 | 连接12.1ms+冲突8.5ms，Network约28.3MB分配 | 高；端口/车道裁剪/转向/路口预约耦合，必须证明不改变候选拓扑与安全，不建议先做大重构 |
| 后续研究 | 17次恢复搜索能否共享目标/前沿 | 寻路仍约38.8ms，是最大单个算法块 | 当前数据尚未证明这些查询之间存在可安全消除的重复；需单独统计源/目标重合、未达和成本。不能因耗时高就改Dijkstra或复用失效树 |
| 暂不优先 | 道路→车辆路线反向索引 | 当前全车路线检查仅1.77ms | 索引维护涉及改道/车道变更/退休/冷加载，维护风险相对这次可得收益偏高 |
| 暂不优先 | 再压缩快照或删除全量安全校验 | 3次Snapshot约4ms；校验/索引更小 | 安全和所有权证明成本不低，不应为小收益放宽验证 |

**最推荐下一轮先独立优化分区显示增删路径**：只有2块变化，却为26万格重新分组，证据最明确；影响下一帧流畅性和分配，且可隔离在表现层验证。若下一轮专门要求压低施工当帧峰值，则先做第二项空间索引脏桶定位。两者应分开实验，不承诺一次消除全部200ms体验。

本轮不建议先跨帧拆施工。当前已存在相邻两帧工作，简单yield只会转移卡顿。候选世界验证/迁移/扣款必须维持原子性；入口恢复决定下一模拟步，推迟可能改变状态。显示工作可另行研究帧预算，但先减少已证实的全量整理更直接。

## 验证、限制及交付

- Unity诊断Player编译成功；最终3次不录raw、1次raw均完成生产施工、边界Validate、80固定步冷热续跑完整状态相同。
- 四次的before/pending/final共12份完整存档与冻结基线全部逐字节相同，见verification.csv。边界SHA `140EB4CBDECFCD0003428A4500C88DEF6E4D4FC8CEE7CC8F24A1D4CA01F50548`，80步终态SHA `9C231E1EE087885D290F63CAD605FB4EF7626286A4E2AF7C6B70DADC9B587487`。
- `git diff 0afa420 -- Assets`为空；没有修改道路删除、恢复、垃圾、任务、经济、安全或模拟步。既有大回归本轮未全部重跑，不把历史通过写成本轮新通过。
- 自定义嵌套时钟self非负、帧号对应及raw实际帧时间已复核。中途fine-raw因诊断phase越过原Profiler.AutoScope边界导致导出失败，保留本地，不用于最终CSV；修正的是诊断插桩嵌套，重新构建并完成final1/2/3/final-raw全部验收。
- Player退出清理阶段日志发现`HarborCityZoneBoxTool.Dispose → OnDestroy`空引用，在采样、保存与校验之后；本轮未修复，也未宣称日志无异常。它不能解释采样中的55ms，留作独立生命周期问题。
- 只覆盖相同真实存档/道路87；没有证明其他道路或正常运行中点击恰好具有同样比例。没有测本轮50×FPS、长期内存、所有道路形态，不据此声称整体游戏已流畅。

工具复用：`Tools/RunRecoveryWindow.ps1 -Scenario frame-breakdown`，可加`-FrameRaw`；此冻结诊断案例只接受道路87。脚本只向Temp/RoadCoreUnity拷贝并注入埋点，不向正式Assets写入。raw使用现有`CityWindowProfilerExport.Run`离线导出，不新建归档框架。

同目录保留runs/frames/scopes/counts、phase-summary、选定两帧Profiler hierarchy/threads、verification及environment，供独立核对。phase-summary对每run同path求和再取中位数，因此Host.Update/OnGUI等多帧重复范围是整个观察窗口合计，不要误当单帧；施工与分区主要范围每run各一次。raw与非raw明确分列。

原始数据保留本地：`C:/Users/16934/Documents/unity_game/road-core-integration-20261024/Temp/DemolitionFrame-20261025/`。有效最终样本final1、final2、final3、final-raw；探索性detail/raw/fine/repeat2/repeat3/fine-raw也保留但不进入最终三次统计。准确输入、程序集和最终raw校验值见environment.json。未上传大型存档、Player或raw。

复现（先准备相同输入，结果目录必须新建）：

```powershell
./Tools/RunRecoveryWindow.ps1 -Save Temp/BatchRecovery-20261025/case-old-live/before.hcity -Results Temp/DemolitionFrame-Reproduce -Scenario frame-breakdown -FrameRaw
# 离线导出，指向上一步window目录，使用已有Unity Editor入口：
$env:HARBOR_WINDOW_CAPTURE=(Resolve-Path Temp/DemolitionFrame-Reproduce/window).Path
& 'C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe' -batchmode -quit -projectPath Temp/RoadCoreUnity -executeMethod HarborCity.CityWindowProfilerExport.Run -logFile Temp/DemolitionFrame-Reproduce/export.log
```
