# 最新版本长帧与居民调度诊断（2026-10-27）

基线 **24dee5141e4dc4224e4f86be0ec585c64ebb666d**（当前实际开发codex/road-core-20261014）。独立诊断分支 `codex/diagnose-long-frames-residents-20261027`。本轮仅修改Tools与报告，正式Assets/模拟规则没有修改，不合并诊断分支。任务顺序：定位长帧→重测居民→评估后续方向；路口预约未继续增量化。

## 结论

**约100ms长帧主要来自每6模拟秒执行一次的建筑增长选址，尤其是重复全表空搜索。** 居民轮询是持续成本，值得开展按时间唤醒影子实验，但不是这批100ms周期帧的主因。路口预约暂排在两者之后。

## 条件与证据边界

同一真实30k存档SHA256 `3F49BD1A02879BB686C24D5517902CC581C657220CE2B955107D89B6FA870B4E`，路径 `C:/Users/16934/Documents/unity_game/test/Temp/CityScenarios/regenerated30000-20261017/city.hcity`，原档不覆盖。i5-14600KF、Windows11、Unity6000.6.3f1、可见1440×900 Player，正常道路/车辆显示，2400真实固定步+8秒画面预热，计时期间无其他测试/编译并行。

复用ResidentScheduling工具：粗计时600步；完整系统归因1200步（60模拟秒）；选址计数1200步；最新居民计数/1:64采样各600步。每帧一个完整固定步，**这些诊断不代表普通50× FPS**，不包括加载、存档断言或输出整理时间。所有窗口起点原生存档SHA均为a24604bb…，完整SHA见验证CSV。

完整归因会为大量Job/CommuteLookup加嵌套时间戳，成本高于普通运行，不能用其Residents平均6.49ms取代粗计时。居民逐人计数/采样亦有明显额外成本，只用于次数与分布。本轮另编译了基于24dee运行时的粗计时Player；之前同逻辑92e版两个粗计时作为辅助复核，不伪称二进制来自同一次构建。

## 1. 长帧对应的固定步

[step-systems.csv](step-systems.csv)保存1200步完整主要阶段和边界、GC计数；[long-steps.csv](long-steps.csv)保留≥30ms样本。Tick为该步提交后的tick，开始tick比它小1。

十次增长边界：**7201、7321、7441、7561、7681、7801、7921、8041、8161、8281**。相差120步×0.05秒=6模拟秒。进入这些步时time.Start正好达到nextGrowth，同时也到nextEmployment。

|代表性提交tick|完整步ms|增长ms|其中选址ms|其他情况|
|---|---:|---:|---:|---|
|7201|92.36|70.84|64.92|人口/岗位1.97ms，垃圾5.23ms|
|7681|91.06|70.65|65.03|垃圾9.00ms|
|8041|84.97|71.18|65.19|垃圾很轻，增长仍占主导|
|8281|105.01|69.76|64.06|垃圾21.50ms叠加|

增长均约68.68–71.18ms，选址约63.20–65.19ms。这10步均无GC.CollectionCount增长；整个1200步没有GC代际收集次数增长。人口/岗位周期工作也触发，但最高约1.97ms；水电刷新最大约0.005ms，分区重建0次，不能把长帧归给分区Mesh、全路网Rebuild或这次占用增量更新。

前轮同一正式逻辑的两轮普通Release 50×窗口也提供独立印证：[prior-normal-long-frames.csv](prior-normal-long-frames.csv)，明确标记为2026-10-26历史普通窗口，并非本轮新FPS成绩。两轮最慢的9帧都对应上述前9个tick。帧约92–105ms，Advance约81–93ms，结构同步约0.0003–0.0006ms。余下约10ms包含车辆显示同步和Unity帧工作，未把所有差额凭猜测归给渲染线程。

### 进一步拆解：没有候选，仍扫30轮

`CityCoreGrowth.Step`→`AdvanceGrowth`：到期完成项目、刷新统计、计算需求、取得Zoning.Cells，然后每种用途尝试10种GrowthFootprints。

[增长计数](growth-counts.csv)覆盖另一个1200步相同终态窗口；每个增长边界都是：

- 3种用途×10种尺寸=30次尝试。
- 每次都遍历158070格，共 **4,742,100次谓词检查**。
- 实际满足row0、available、对应use的起始格 **0个**。
- GrowthFootprint、OverlapsOtherPlot、CanPlace、Entrance调用全部 **0次**。

所以不是多边形计算、碰撞或入口投影拖慢，而是每种尺寸重复运行 `candidates.Where(c=>c.row==0&&c.available&&c.use==use)`，找不到候选仍反复全表过滤。这个结论只针对该真实存档/该窗口，不推广成所有城市永远没有可建格。

附加计数窗口：统计均约0.586ms、需求约3.895ms、取得已有Cells约0.00063ms；Search含新增谓词计数约66.38ms。此处计数本身有代价，**原实际选址时间使用上一归因窗口约64.51ms，不能把66.38ms当生产实现成本**。

最推荐下一项独立优化：在一次增长调用内，保持Cells原顺序，筛出各用途可用row0候选一次，再供各尺寸复用；空组直接跳过尺寸循环。先用调用内临时分组，避免贸然引入长期缓存及复杂失效。保留用途顺序、尺寸轮换、起始格顺序、项目创建顺序和全部几何检查。理论能减少30次全表遍历，**实际减少多少ms必须另做新旧Player与完整状态对照，本轮没有实施此优化**。

### 还有一种较小长尾

8201步不在增长边界，仍52.87ms：Waste39.90ms，同一步RouteSearch合计38.26ms/109次。这是垃圾派发批次伴随搜索，不是首次建树加载；嵌套指标不能相加，也不能把109次搜索未经业务分解全算成垃圾搜索。现有垃圾调度是周期批派发（默认2秒），值得后续单独记录命中/拒绝/搜索来源；本轮未改调度、目标优先级或寻路。

本60模拟秒窗口只覆盖第4天，没有跨日，因此没有验证日切薪资、垃圾生成、财政是否另有长帧。

## 2. 最新居民成本与唤醒价值

新编译当前运行时代码的两轮600步粗计时（CityCoreProfile精细归因关闭，仅居民整体和实际派车调用计时）：

|当前成本|平均ms/步|
|---|---:|
|完整模拟步|9.394|
|居民调度总计|4.837|
|其中派车调用|1.333|
|扣除派车后的循环、资格及读取|3.505|

[resident-coarse.csv](resident-coarse.csv)记录逐轮和退出状态；居民总计约占完整步51.5%，非派车约37.3%。派车调用内承担第一次共享占用刷新等成本，不能把全部1.33ms都当成可删除的业务重试成本。旧“居民6.69ms/非派车4.62ms”已不是最新数据。

600步计数（与旧实现相同终态）：

|项目|次数|
|---|---:|
|成员循环|18,000,000|
|在途直接跳过|2,392,710|
|retryAt未到|274,131|
|通过最前面基本门槛|15,333,159|
|实际读取居民岗位|9,333,159|
|进入班次/返回/出发判断|5,737,787|
|具备上班条件|5,615,563|
|其中尚未到出发时刻|5,612,425|
|上班派车尝试|3,138|
|回家派车尝试|66,157|

[resident-gates.csv](resident-gates.csv)、[resident-reads.csv](resident-reads.csv)、[resident-job-lookups.csv](resident-job-lookups.csv)、[resident-qualification-summary.csv](resident-qualification-summary.csv)可核对。最后一个文件历史字段passed_basic_gate实际在**后面的完整资格观察点**计数，不能与前置gates.passed混同。5,737,787次班次判断并不等于全部通过基本门槛的人。

岗位在每次有效迭代内已复用，同轮资格部分没有重复读同一个岗位；派车安全复核中的1952次重复Job读取仍保留。CanAttend调用15210935、JobFunded9267002、ShiftStart/ShiftMinutes各5737787，很多是跨步重复，不能再靠同轮局部变量消除。

9766名居民在窗口中处于“尚未到出发时刻”，观察到的出发阈值/日/工作地/路网版本变更次数0；对应561万次判断占全部成员遍历31.18%。这支持时间唤醒研究，**不是证明这些人未来永不发生状态变化**。逐人采样的读时间含观测开销，未把1:64采样乘64伪装成真实总CPU，也未继续把99.99%命中的通勤缓存当单独重点。

### 建议：先窄范围影子时间唤醒

不要直接全量事件驱动替换。先为已知未来时刻维护派生唤醒队列，旧轮询仍权威：等待出发、班次结束、retryAt和visitUntil。比较每步候选居民、理由、派发顺序及完整城市状态，之后才在独立实验开启跳过。

必须处理的事件及边界：

- 岗位指派/释放、雇主/建筑消失、家庭迁移、病亡/工作能力变化；entities.Revision并不覆盖所有现金、健康、岗位字段变化，不能单靠它失效。
- 企业资金由生产、货运、购物、薪资等改变；尤其缺钱→有钱的居民不能沉睡到下一天。先证明哪些状态在出发前无需观察，无法证明者仍保留原检查。
- 车辆出发/到达/拆路恢复、购物停留结束、实际通勤时间更新；道路/入口/速度及AccessRevision变化会改变出发阈值。
- 日切及旧日前往工作者的返回、模拟日长/班次规则；按原float/double比较确定首次满足条件的固定步，不能随意加epsilon或晚唤醒一帧。
- 同tick仍按原Members稳定顺序处理，不能以任意优先队列/居民ID排序改变发车竞争；到达结算及业务事件在原阶段产生，下次应处理的请求阶段才唤醒，不立即插队派车。
- 冷加载重建派生队列，不保存第二份居民状态，不为旧档添加适配。原0.25秒失败重试和所有安全复核不变。

当前非派车约3.5ms/步是这类研究的宽松理论上界，单独跳过这9766名等待者只能节约其中一部分，不能按561万次检查推算FPS或声称能达到50×。收益与队列更新、失效维护成本要实际配对测量。

## 3. 路口预约的取舍

继续暂缓。前轮物理声明/活动计数约0.64ms/步，整个增量占用约1.33ms；本轮完整归因增量Occupancy约1.55ms/步，包含观测开销。这些是共享交通成本，不应从居民移走后冒充减少。进一步区分车体声明与本步临时预约风险高，当前优先级低于增长的约65ms周期空扫描与居民约3.5ms持续非派车判断。

建议顺序：**先验证增长调用内候选复用→开展居民时间唤醒影子实验→根据新的完整模拟步、普通FPS和长尾，再决定预约增量化**。垃圾派发52ms尾帧另列调查，不顺带更改算法。

## 验证、交付与复现

[validation.csv](validation.csv)：8个Player窗口均完成全城安全/经济/垃圾校验及20步冷热完整状态一致性检查。相同600步的6个窗口终态均为19e1a1d8…；两个1200步窗口终态均为2388ff39…，完整SHA可独立核对。观测没有改变工作量或终态。

**退出状态单列**：7轮正常退出0；新编译粗计时第一轮在测量、保存与冷热校验完成后，Unity关闭阶段超时，被原脚本终止，不能算正常退出PASS。部分窗口关闭时出现已有ZoneBoxTool.Dispose空引用，记录在validation.csv；未把它隐藏为“全程无异常”。这些发生在采样结束之后，本轮未顺带修复关闭逻辑。未重新运行前轮全部业务测试，不把旧测试数冒充本轮新通过数。

```powershell
./Tools/RunResidentSchedulingDiagnosis.ps1 -Variant legacy -BuildTag fresh-tail
./Tools/RunResidentSchedulingDiagnosis.ps1 -RunOnly -BuildTag fresh-tail -Mode legacy -RunName fresh-tail-run -Steps 1200
./Tools/RunResidentSchedulingDiagnosis.ps1 -Variant growth -BuildTag fresh-growth-counts
./Tools/RunResidentSchedulingDiagnosis.ps1 -RunOnly -BuildTag fresh-growth-counts -Mode stock -RunName fresh-growth-run -Steps 1200
./Tools/RunResidentSchedulingDiagnosis.ps1 -Variant latest -BuildTag fresh-resident
./Tools/RunResidentSchedulingDiagnosis.ps1 -RunOnly -BuildTag fresh-resident -Mode counts -RunName fresh-resident-counts
```

Tools新增每固定步阶段快照、周期边界及GC收集计数、最新居民观察入口和临时选址计数，沿用既有脚本；只在Temp副本注入观测，没有新增正式管理框架。Player、日志、完整存档和完整细阶段CSV留实验工作树的Temp/ResidentScheduling；GitHub仅报告、必要汇总CSV与测量工具。正式开发HEAD、用户四个未提交文件及原始玩家存档保留。
