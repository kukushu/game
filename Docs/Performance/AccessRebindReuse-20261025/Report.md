# 入口重绑复用路网实验（2026-10-25）

结论：确认了一次不必要的全局路网重建，已用最小改动移除。真实删路存档三组原生配对中，RebindAccesses中位数 **34.42→3.78 ms（约89%减少）**；整个操作最长帧的中位数 **291.97→219.72 ms（约25%减少）**。仍有约220–237 ms停顿，没有解决全部删路长帧。

- 分支：`codex/experiment-access-rebind-network-reuse-20261025`。
- 基线：`e282db2e004c79701019e9d92afa2045e7fd5161`，从用户指定的最新垃圾车容错实验分支创建。
- 运行时代码：`e675004`；随后提交为测试与报告，最终SHA见本文件最近提交。
- 未修改/合并正式开发分支或任何原实验分支。运行时只改`CityRoadCoreWorld.cs`中的RebindAccesses，增加7行、替换1行；RemoveRoad、Network实现、Dijkstra、业务规则、保存格式均未改。

## 发现与实现

原链路：RemoveRoad复制道路数据并构造Network → RefreshIfChanged查询失效入口 → RebindAccesses再CopyNetwork并new Network → 原RoadEditPlan.TryCommit完整候选验证 → 批量恢复Stranded。

第二次构造并没有改变道路、LaneConnection、方向、速度或几何，只是为了提交新的RoadAccess列表。新实现把同一份不可变道路事实`data.network`与当前`Network`交给**原RoadEditPlan事务**。原Snapshot、候选World、ValidateEditReservations、Validate、AssertSafety和真实索引更新全部保留，不另建简化验证路径。

事务成功后原NetworkGeneration和EditEpoch照常推进；旧计划即使仍持有同一个Network对象，也会被epoch拒绝。AccessRevision沿用旧行为，不擅自改版号语义。普通OD缓存仍由原TryCommit显式Reset；LostOccupancy、预约及占用仍按原事务重建。

注意：垃圾车车道树、路口树以前依赖Network引用变化来懒失效。现在在成功重绑后显式`DepotRoutes.Clear()`与`JunctionRoutes.Clear()`，保持保守失效，防止旧基地入口或终端pin留存。没有顺带改缓存失效范围或寻路算法；失败/no-op重绑不会清缓存。之后的真正道路编辑仍复制几何并创建新Network，不会修改被复用的旧实例。

## 原生Unity Player配对

同一真实删前检查点：SHA256 `0110347D2BC4E40E8CD98E841EE92F62D675935D076A21AE58D7F7A5A9ADB6CD`，tick642633、536辆实际车辆，删除道路87。来源为玩家之前DE51存档正常推进20步；不是最新F52现场，也不是重建三万/五万人城。

Unity6000.6.3f1 Development插桩Player、Windows11/i5-14600KF、1440×900、相同相机/原道路显示/车道垃圾寻路、8秒显示预热。冻结相同施工时刻，采用**暂停模拟的脚本施工窗口**，保留全部真实在途车辆状态；不是人工鼠标输入延迟或运行中1×点击测试，也不以函数耗时推算FPS。

每次执行生产Construction.RemoveRoad、Bindings和显示刷新，记录12帧；完整验证/保存和80步冷热续跑都在施工采样之后。原新各3次：首对配对后，再按旧→新→新→旧交错复测。无其他测试或构建在这些采样窗口后台运行。

| 指标 | 基线 | 复用Network |
|---|---:|---:|
| RebindAccesses ms，中位数 | 34.419 | 3.780 |
| RebindAccesses ms，范围 | 34.10–38.15 | 3.74–3.99 |
| Bindings总ms，中位数 | 129.376 | 94.846 |
| RemoveRoad事务ms，中位数 | 55.657 | 56.459 |
| Network.Rebuild总次数 | 2 | 1 |
| 重绑内部Network.Rebuild次数 | 1 | 0 |
| Snapshot次数，全操作 | 3 | 3 |
| 候选World构造次数 | 3 | 3 |
| Validate / AssertSafety次数 | 4 / 4 | 4 / 4 |
| Reindex次数 | 7 | 7 |
| 最长帧ms，中位数 | 291.966 | 219.721 |
| 最长帧ms，范围 | 289.79–305.14 | 219.68–236.64 |
| 最大单帧GC分配bytes，三次相同 | 85614301 | 55981381 |

单帧分配减少约29.63 MB（34.6%）；这是Unity Recorder的整个帧分配，不能冒充RebindAccesses逐函数分配。原生线程分配API不支持，scope CSV为-1，不填0。父子scope是包含计时，不可相加。

精确逐方法分配另在.NET10隔离宿主测量：冻结e282db2 World对当前World，同输入、相同删除与Bindings，1轮预热后各3次。RebindAccesses分配中位数 **31431736→2622312 bytes**（约91.7%减少）。其ms样本为旧122.642/39.785/32.252，新2.329/5.108/1.442；旧首样本存在明显运行时波动，全部保留，**不把.NET计时或GC冒充原生函数结果**。精确调用计数也确认Bindings阶段重建1→0，完整验证次数不变。见`model-scopes.csv`和`model-summary.csv`。

## 正确性

- 六个原生窗口的初始、施工边界及80步终态存档全部逐字节相同。边界SHA256 `140EB4CBDECFCD0003428A4500C88DEF6E4D4FC8CEE7CC8F24A1D4CA01F50548`；80步终态 `9C231E1EE087885D290F63CAD605FB4EF7626286A4E2AF7C6B70DADC9B587487`。
- 三组.NET配对的重绑边界和80步全状态也完全相同。车辆、入口、任务、货物、垃圾与居民/经济事实没有变化；原生冷热保存与继续模拟验证通过。
- **597项专项**：冻结原操作对照、入口权限/方向/距离、车道树与路口终端pin、普通缓存与版本失效、重复/缺失/无效/no-op输入、换道中车辆、连接内预约、Stranded与离路待重发、保存与80步冷续跑。注入非法货物后重绑仍被原完整验证拒绝，真实状态/epoch/Network均不变；旧计划在重绑后被拒绝；后续拆分不会修改借用的旧Network。
- 既有517项任务恢复、787项垃圾损失、840项批量恢复、101项固定步预算，以及23614项城市业务、10000就业、1522方格道路、83分区和460474水电模板检查通过。本轮道路核心150937项检查通过。
- 不删路真实城市：同SHA256 `3A67918ED29AB7252DBE6D99D3FE58429A2CB2B042987499A4F05227EAACBD3E`，基线与当前各6200固定步，所有完整保存检查点和终态相同；每步安全/垃圾守恒及80步冷续跑通过。

早期共享探针的续跑列名/结果文字沿用200步，实际本场景参数是80步。原始数据不改写，报告及发布CSV按实际80步标注；交付探针已改为输出明确的continuation_steps。续跑断言时间不进入施工性能成绩。

## 剩余瓶颈

新原生样本中，批量恢复仍约63.6 ms，其中132次原地恢复规划约58.7 ms；入口查询/收集等Bindings剩余约26–28 ms。未改动的RemoveRoad仍约56 ms，其中Network重建约31 ms；道路显示同步约13 ms。完整Validate累计仅约3.6 ms，Reindex约1.5 ms，当前没有证据支持冒险删校验。

这些是已覆盖的主要阶段，整个渲染帧还包含其他表现和运行时工作；本轮没有进一步归因所有scope之外的耗时，不能把最长帧的全部变化都算作RebindAccesses的CPU节省。下一阶段应独立分析原地恢复规划及入口候选查询；本轮不继续改这些模块，也不把本结果宣传为所有道路删除场景都可达到220 ms。

## 复现与资料

```powershell
./Tools/VerifyAccessRebind.ps1
./Tools/RunRecoveryWindow.ps1 -Save Temp/BatchRecovery-20261025/case-old-live/before.hcity -Results Temp/AccessRebindReuse-20261025/repeat-before -Reference e282db2e004c79701019e9d92afa2045e7fd5161 -Scenario access-rebind
./Tools/RunRecoveryWindow.ps1 -Save Temp/BatchRecovery-20261025/case-old-live/before.hcity -Results Temp/AccessRebindReuse-20261025/repeat-after -Scenario access-rebind
./Tools/MeasureAccessRebind.ps1 -Save Temp/BatchRecovery-20261025/case-old-live/before.hcity -Results Temp/AccessRebindReuse-20261025/repeat-model -Reference e282db2e004c79701019e9d92afa2045e7fd5161
```

复用现有Player宿主/构建工具，只扩展scope采样与专项测试。原始Player、存档、完整日志留本地`Temp/AccessRebindReuse-20261025/`。配对目录为native-before/b2/b3与native-after/a2/a3；model-before/after为.NET归因；no-edit-pair为6200步对照。native-delivery-check只验证最后的探针元数据修正，不混入事先规定的三组配对成绩。大型文件不上Git，必要CSV随报告提交。不自动合并。
