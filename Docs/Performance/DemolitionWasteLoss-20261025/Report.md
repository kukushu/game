# 拆路垃圾车容量容错（2026-10-25）

已实现：明确失去当前路面的垃圾车不再因为垃圾场容量不足长期占用车队。去程装不下则取消；返程部分入库、余量登记lost。没有关闭垃圾守恒或吞掉程序错误。

- 独立分支：`codex/experiment-demolition-waste-loss-20261025`。
- 基线：`2df28968e865f0d9d290d5ef6b646e556fef8884`，来自用户指定的`codex/experiment-demolition-task-recovery-20261025`。
- 运行时提交：`e4055b71ef6d624883635eb0278fd28fcd910d0b`。随后提交是测试与报告，最终SHA可由本文件最近提交确定。
- 正式开发分支和原实验分支均未修改、未合并。运行时代码仅修改`CityCoreWaste.cs`；World批量恢复、普通货运、居民、Dijkstra、路网与显示代码未改。

## 规则与账目

入口仍只接受上轮明确由RemoveRoad标记的`AwaitingRestart`服务任务，不追溯猜测旧Stranded原因。

| 阶段 | 特殊结算 |
|---|---|
| Pickup | 仍取`min(载重, 建筑当前垃圾)`。完整收集量能放下才收集并入库；放不下则整次取消，建筑垃圾、collected和lost都不变。两种情况都结束任务并回收车辆。 |
| Return | `入库=min(原载货, 剩余容量)`，`损失=原载货−入库`；只增加stored和lost，不修改建筑垃圾或再次增加collected。清零Shipment并结束任务、回收车辆。 |

保留两道守恒：

```
generated = 建筑待收 + stored + inTransit + processed + lost
collected = stored + inTransit + processed + lost
```

lost必须非负且不大于collected，collected不大于generated，transferred仍必须为0；物理场内库存仍不得超过容量。没有新建第二套库存或损失账本，使用原来已保存的`CityWasteState.lost`。现有二进制字段布局和存档版本不变，不增加兼容迁移分支。

仅离路待异常结算的任务不再占用完整入库预约；它们仍在结算前占用车队名额。普通Active/其他原因Stranded的垃圾车继续保留完整预约校验和原到达流程，没有套用丢弃逻辑。

`CheckMission`复用原任务/mission合法性检查，并检查任务索引引用、mission归属及生命周期。异常恢复时同样检查：缺失车辆/任务、错误阶段或货物类型、非法库存、重复mission等抛出异常；没有catch后继续结算。重复扫描已结束的队列不会再次记账，直接重复完成旧任务的接口仍会拒绝。没有引入复杂回滚系统；这些程序错误不会被当成容量容错。

## 实际验证

原生Unity 6000.6.3f1 Development Player，Windows11/i5-14600KF，1440×900。使用从空城实际成长并产生垃圾车的测试城，执行真实RemoveRoad，再构造有限、守恒的库存边界输入：补入的测试场内存量同时记入此前generated/collected。正常派发的入库预约本来会防止满载，因此这些是明确的容量边界功能夹具，不冒充玩家真实存档的性能样本。

| 原生场景 | 原载货 | 入库 | lost增加 | collected增加 | 建筑垃圾变化 |
|---|---:|---:|---:|---:|---:|
| Pickup，剩余容量0 | 0 | 0 | 0 | 0 | 0 |
| Return，剩余容量2 | 3 | 2 | 1 | 0 | 0 |
| Return，剩余容量0 | 3 | 0 | 3 | 0 | 0 |

三场均通过生产固定步结算，原车辆、任务和mission正确清理，损失守恒成立；结算前冷读档与热状态结果一致，结算后再次保存/读取并继续80个固定步，完整状态一致。CSV见`native-cases.csv`。本轮不做新的FPS或加速倍率收益声明；原生窗口是功能验收，不是性能基准。

自动化结果：

- 新增**787项**损失专项检查：Pickup/Return各剩余容量4、3、2、0；容量足够、恰好足够、取消、部分丢弃、全部丢弃；既有lost累加；任务/货物/车辆/车队清理；重复处理与重复完成；结算前后冷恢复和80步继续模拟。
- 错误检测：未记账库存减少、负lost、lost超过collected、错误task引用、非法phase、错误货物类型、已完成任务、缺失车辆、超容量物理库存及重复mission均被拒绝。其他Stranded和正常车辆不会进入容错；其容量预约仍会拒绝超订。
- 上轮**517项**任务恢复、**840项**批量恢复及**101项**固定步/倍率预算测试通过。当前城市综合**23614**、就业索引**10000**、方格道路**1522**、分区**83**、道路水电模板**460474**检查通过。
- 不触发拆路容错的真实城市对照：同存档SHA256 `3A67918ED29AB7252DBE6D99D3FE58429A2CB2B042987499A4F05227EAACBD3E`，基线2df2896与e4055b7各6200步，所有完整检查点及终态相同；每步安全/垃圾守恒与80步冷恢复通过。见`unchanged-checkpoints.csv`。不宣称容量不足的特殊结算新旧状态相同，因为这正是本轮授权改变的规则。

## 工具与复现

复用`VerifyDemolitionRecovery.ps1`，增加一个专项文件；复用原生`RunRecoveryWindow.ps1`及现有探针，增加`waste-loss`场景。没有新建测试管理或归档框架。

```powershell
./Tools/VerifyDemolitionRecovery.ps1 -Results Temp/DemolitionWasteLoss-20261025/recheck
./Tools/RunRecoveryWindow.ps1 -Save Temp/DemolitionWasteLoss-20261025/checks-final/loss/pickup-free0.hcity -Results Temp/DemolitionWasteLoss-20261025/repeat-native -Scenario waste-loss
./Tools/CompareFreightFunding.ps1 -Save C:/Users/16934/Documents/unity_game/test/Temp/PostMergeDiagnosis-20261024/input.hcity -Baseline 2df28968e865f0d9d290d5ef6b646e556fef8884 -Results Temp/DemolitionWasteLoss-20261025/repeat-pair -Steps 6200
```

大型输入、Player、完整日志均留在实验工作区`Temp/DemolitionWasteLoss-20261025/`。最终测量目录为`checks-final`、`native-pickup-final`、`native-return-free2`、`native-return-free0`、`no-edit-final`；较早预检目录保留，但不冒充最终源码结果。必要案例/检查点CSV提交Git即可。

本轮结束于独立实验分支，不自动合并，不继续其他优化。
