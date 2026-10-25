# 删除道路后批量恢复车辆实验（2026-10-25）

结论：本次真实删路配对中，恢复入口从 **5336.49 ms 降至131.57 ms（约40.6倍）**，施工最长帧从5533.41降至299.86 ms。新旧恢复相同17辆车，仍有119辆无法恢复，未改变后者规则。仍存在约300 ms停顿，不代表删除道路问题已经全部解决。本实验未合入正式分支。

## 版本、输入和边界

- 分支：`codex/experiment-batch-stranded-recovery-20261025`。
- 基线：`48eeb325b81f0c78fd69a48824a261602cb27a3f`，标签`before-road-demolition-performance-experiments`。独立工作区保护正式分支和用户未提交文件。
- 运行时代码：`1b9d2eddd2f097040d600c86de578f49f9b17614`；原生测量工具：`1d13726fc6c2567436014df100ab2cc1dab4057e`。本报告提交仅增加交付资料，最终SHA以本文件最近提交为准。
- Windows 11 / i5-14600KF / Unity 6000.6.3f1，1440×900，原道路显示、原车道寻路设置、0.05秒固定步。未改变车辆数量、模拟预算、发车或任务规则。
- 使用玩家之前实际保存的城市`test/Temp/RouteInvalidation-20261024/current-live-city.hcity`，SHA256 `DE51EE2FB7B2B52551F1B26142D64FEA192B46273DE43095094C6693E072C6B1`。从此状态正常推进20固定步，tick642633，删除道路87；删前536辆车，删除后136辆Stranded。不是重新生成的城市，也不是三万/五万人测试。
- 配对的删前输入SHA256：`0110347D2BC4E40E8CD98E841EE92F62D675935D076A21AE58D7F7A5A9ADB6CD`；删除后`E8199E877C095AC5A21019469D71223B42D3B41D1BB767ED93314A2B55F90AC7`。
- 最新F52A46FE…存档在本次检查的繁忙道路候选上被原有安全规则拒绝，因此未把它当成成功删除案例；采用上述较早真实检查点。未覆盖玩家存档。

## 原因与最小改动

调用链：成功道路施工 → `CityCoreRoadBindings.RefreshIfChanged()` → 原入口重绑 → 逐车`TryResumeStranded()` → 路线查询 → Snapshot → new World候选冷验证 → 修改真实车辆 → Reindex。

原单车API独立保证事务安全，因此每个成功恢复都创建自己的完整候选世界。候选构造再次复制快照、重新构建Network和丢失路面占用数据；AssertSafety又建立全局占用索引。一次恢复17辆就重复17次。Snapshot本身并非最重：隔离.NET基线中，候选构造包含约1701 ms网络重建和约3373 ms安全检查/索引成本。父子计时存在包含关系，不可相加。

只改两个运行时文件：

1. `CityRoadCoreWorld.cs`：抽出原来的只读恢复路线规划和应用操作；增加`ResumeStrandedVehicles()`。原单车API保留作为原语义及对照。
2. `CityCoreRoadBindings.cs`：把逐车调用改为一次批量调用。

批量流程：按原vehicles顺序只读规划 → 有成功方案才建立一份私有Snapshot → 在同一候选中按原顺序应用 → 原Validate/AssertSafety检查 → 全部通过后更新真实对象 → 真实恢复占用索引只重建一次。任务、货物、当前位置、原寻路及失败无法恢复的规则不变。候选复用既有不可变Network，不修改Network实现，不创建第二套路网。

**顺序依赖保留**：位于连接、换道、丢失路面或路口预约范围内的恢复仍在对应前缀执行完整安全检查，最后再检查。禁止后续恢复掩盖先前不合法前缀。本例需要两个候选检查点，并非强行一次验证。所有检查先于真实世界写入；候选验证异常时整批不提交，不会部分恢复。与旧逐车可能已提交前缀的异常原子性相比，这是明确的整批回滚边界；未通过的冲突不会被跳过、重排或放宽。

原成本近似K次全世界复制/构造/校验；现在为一次复制/候选构造、一次真实索引及P次必要候选校验，另加不变的逐车寻路。K为成功恢复数，P为安全检查点数；最坏预约密集场景仍可能P≈K，不宣称完全局部更新。

## 真实Unity Player结果

独立Development插桩Player，同一输入和同一次删除，显示预热8秒后运行生产施工、显示同步和Bindings。为冻结完全相同的操作时刻，测量使用**暂停状态的脚本施工窗口**；车辆是在途真实状态。不是人工鼠标延迟验收，也不是正常游玩FPS。完整保存验证及80步续跑放在采样帧之后，不混入施工计时。每侧一次配对，不作统计置信区间声明。

| 指标 | 原版 | 批量版 |
|---|---:|---:|
| 尝试 / 成功 / 仍滞留 | 136 / 17 / 119 | 136 / 17 / 119 |
| Snapshot次数 | 35 | 2 |
| new World完整候选构造次数 | 18 | 2 |
| Validate次数（含构造） | 18 | 3 |
| AssertSafety次数 | 18 | 3 |
| Reindex次数 | 36 | 5 |
| 恢复入口总耗时ms | 5336.494 | 131.574 |
| 删除道路事务ms | 82.982 | 82.686 |
| 道路显示同步ms | 13.936 | 12.817 |
| 最长帧ms | 5533.410 | 299.864 |
| 最大单帧GC分配bytes | 4193010987 | 91080095 |

计数口径包含Bindings内原有入口重绑：它本身仍有1次Snapshot、1次候选构造、1次Validate和2次Reindex。**单独恢复循环**从34→1次Snapshot、17→1次候选构造、17→2次Validate、34→3次Reindex（候选2次、真实1次）。两种口径均提供，不能把总Reindex写成1。原生逐方法GC接口不支持（CSV为-1），帧GC来自Unity Recorder，包含施工/显示等整个帧。

隔离.NET同一删除后完整状态、同一Bindings入口，补充可精确读取的托管分配：5311.318→138.796 ms，3974000216→42196872 bytes（约98.94%减少）。这是.NET归因数据，不能冒充原生Player逐函数分配。分项调用/包含耗时/分配见`model-scopes.csv`；原生分项见`native-scopes.csv`。

## 正确性与普通游戏回归

- 原版/批量恢复17个相同车辆ID、位置、完整路线、任务及货物一致；完整保存逐字节相同。恢复后SHA256 `5508dff8bc1d046cdd7e42602ca625b717f45dec3c7cf8ed9ba3662244a29e82`。
- 原生继续80固定步，完整保存仍相同：`544089d5f169187557587823ed475dd59eb5075c9368296709b39ab00a769dd9`；各自冷读取继续模拟一致。隔离模型继续600步完整状态相同。
- 专项840检查通过：无车辆/单辆/64辆/混合可恢复和不可恢复、连接/换道/路口前缀、重复调用、80步逐步旧新冷恢复对照。故意破坏最后车辆货物触发原验证，证明候选失败时真实世界字节和EditEpoch不变，没有删除安全约束。
- 既有VerifyRoadCore 150936、VerifyCityCore 23580、就业索引10000、路网1522、分区83、道路水电/模板460474及固定步预算101检查通过。未把无法恢复的119辆说成已修复。

普通Release Player另作1×/50×回归：同存档SHA256 `3A67918ED29AB7252DBE6D99D3FE58429A2CB2B042987499A4F05227EAACBD3E`，2200正常固定步及8秒显示预热，起点tick549891、1366人416车；每档30秒，原8 ms/32步预算、60FPS目标。该存档与删路案例不同，前后各自配对一致；不把正常窗口当成删路帧成绩。

| 模式 | 原FPS → 新FPS | 实际倍率 | P95 ms | P99 ms | 最长帧ms |
|---|---|---|---|---|---|
| 1× | 59.73 → 59.75 | 0.9999 → 0.9998 | 18.47 → 18.54 | 19.83 → 19.60 | 63.97 → 60.89 |
| 50× | 57.88 → 58.16 | 21.88 → 22.67 | 21.80 → 20.77 | 44.34 → 41.32 | 76.66 → 73.07 |

未观察到明显回退；没有道路变化的窗口不应因本改动获得结构性提速，差值不归因于恢复优化。50×仍未达到。原生普通窗口的GC字节计数不支持，未填写为0；原版/新版1×分别0/1次各代GC，50×均5次。四窗口原始逐帧账时/车辆显示/正常退出校验通过。1×同tick550491完整运输及业务状态一致，仅排除已独立核验的墙钟欠步差值；50×墙钟采样推进步数不同，不声称终态相同。

## 复现与资料

使用现有ProjectLayout/CityPlayerBuild/ScenarioWindow工具，新增小型测试探针，仅在Temp隔离项目插桩。命令示例（结果目录必须不存在）：

```powershell
./Tools/VerifyBatchRecovery.ps1
./Tools/RunRecoveryWindow.ps1 -Save Temp/BatchRecovery-20261025/case-old-live/before.hcity -Results Temp/BatchRecovery-20261025/repeat-before -Reference 48eeb325b81f0c78fd69a48824a261602cb27a3f -Road 87
./Tools/RunRecoveryWindow.ps1 -Save Temp/BatchRecovery-20261025/case-old-live/before.hcity -Results Temp/BatchRecovery-20261025/repeat-after -Road 87
python Tools/VerifyCityScenarioWindow.py Temp/BatchRecovery-20261025/normal-before
python Tools/VerifyCityScenarioWindow.py Temp/BatchRecovery-20261025/normal-after
```

本地原始文件保留在实验工作区`Temp/BatchRecovery-20261025/`：`native-before`/`native-after`保存原生Player、编译日志、输入/终态、逐帧及计数；`baseline`/`after`为.NET归因；`normal-before`/`normal-after`为普通Release窗口。普通基线二进制由48的World/Bindings冻结构建；工具environment的工作区HEAD不是冻结源版本，以本报告和程序集校验值区分。原生批量Player测量时运行时代码尚未提交，其内容与1b9d2ed相同，原dirty元数据原样保留。提交必要CSV，不上传大型存档/Player/完整日志，不另建归档框架。

## 剩余问题与建议

119辆原本无法找到恢复路线的车仍Stranded；本轮只去掉重复全局事务。删除道路本身约83 ms、显示同步约13 ms、入口重绑和恢复寻路仍有成本，总最长帧约300 ms。复杂路口恢复仍可能需要多个完整安全检查点。没有修改RemoveRoad、Network构建、Dijkstra、经济、垃圾规则，也没有道路到车辆反向索引或返回基地机制。下一阶段先单独确定永久滞留车辆的业务处置规则，再决定实现；不得用本轮性能结果证明该问题已解决。可评审本最小改动，保持实验分支，未经授权不合并。
