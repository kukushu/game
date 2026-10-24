# 车道级寻路与道路合批合并（2026-10-24）

用户授权两个实验分支合入 `codex/road-core-20261014`，并明确选择正常游戏默认使用车道级算法。原开发基点 `4ac850c377daca75df942a1f6ac9491df2e3b214`，保留于本地 `backup/pre-lane-road-batching-20261024`。

- 车道树源：`codex/experiment-depot-lane-routing-20261021` / `6afc4335f1929ee1a27317bc42e4088c3dd0d4cf`，第一笔合并 `2913078`。
- 道路合批源：`codex/experiment-road-view-batching-20261022` / `7d65478f540286d0e8ee8a0bafd19dee422fc4d5`。该分支的路口寻路实验祖先及历史资料保留，路口算法不作为正常Play选择。
- 第二笔合并及最终提交见本文Git历史；两个源分支保持不动，未修改main、未改写历史或强推。

## 当前游戏行为

正常Play与玩家城市inspect入口默认启用 `DepotLaneRoutingEnabled`，道路显示默认64单位分块合批。正常Save/Load保留当前运行时寻路选择，开关不写入城市事实/存档，不新增旧格式兼容层。

`World.ServiceBlueprint` / `ServiceReturn`统一服务车调用入口：车道树、显式诊断路口树、原始搜索分别独立选择；两个实验开关不能同时开启。树不可用时继续使用原始路径搜索，原发车占用、路口预约、提交和车辆安全检查全部保留。这里的回退是重新计算路线，不会恢复或删除地图道路。

解决Network partial、两个缓存的垃圾场生命周期、去返程入口、Profiler观察和测试工具参数冲突；使用合批分支的UTF-8源文件保留正常中文施工提示。更新RoadCore测试依赖列表，避免只因漏列新类型而编译失败。未调整缓存容量、路线选择、垃圾优先级、模拟规则或施工算法。

原车道实验的等成本不同路线、后续状态可能不同及多基地超64MiB预算重复建树等限制仍存在。本次是合并和启用，**不是新的算法优化或50倍性能承诺**。

## 实际验证

| 检查 | 结果 |
|---|---|
| RoadCore拓扑/方向/安全/施工/货运资金/二进制继续 | PASS 150,936 |
| 城市工业、居民、经济、保存继续 | PASS 23,469 |
| Job索引无分配 | PASS 10,000调用 |
| 方格建路 / 框选分区 / 道路水电规则 | PASS 1,522 / 83 / 73,637 |
| 固定步、欠步、有限批次和50倍容量规则 | PASS 101 |
| 场景构建 / 单一运行系统与脚本解析 | PASS 18 / 126脚本meta齐全 |
| 车道树及新增互斥/切换缓存/不改城市事实测试 | PASS 9,707 |
| 合并后保留的路口实验回归 | PASS 23,287 |
| Unity原生冻结道路几何/局部重建/资源释放 | PASS 27,244,038元素断言 |
| Unity Release Player构建 | PASS |
| 原生正常启动、正常读档、实际垃圾查询选择车道树 | PASS |
| 原生Save/Load事实一致、保持算法选择、20步冷热完整状态对照 | PASS，含车辆安全与垃圾守恒 |

原生检查使用当前玩家城市副本，SHA256 `388a7ab4039586428ecde106f13c5918f7ab574fcfb9f2b30620fc3db913845e`，63,604逻辑Drivable / 415道路Renderer，实际车道树构建4棵、路口树0棵。不是车道树与旧算法的全状态等价声明。

`Tools/Diagnostics/HarborCityRoutingIntegrationCheck.cs` 与 `HarborCityRoutingIntegrationBuild.cs` 仅复制入临时项目。测试Player使用独立公司/产品存档目录 `HarborCityIntegration/LaneRoadBatching20261024`，不调用用户实际存档的写入路径。第一轮隐藏窗口未推进而中止，不计PASS；探针启用后台运行后的独立Player重跑通过。没有本轮FPS配对成绩，也不宣称物理鼠标体验验收。

使用现有 `Tools/Verify.ps1`、`Tools/VerifyRoadViewBatches.ps1` 和两个现有寻路检查；原始日志位于隔离工作区 `Temp/Integration-20261024/`：`core-tests.log`、`lane-checks.txt`、`junction-checks.txt`、`geometry/Result.txt`、`native-running/Result.txt`、`native-running/views.csv`。测试入口额外放入临时Unity项目，正常项目不装载测试探针。

合并在隔离工作区完成；原项目三个用户修改cmd及未跟踪`.editorconfig`保留。曾有一次未提交合并误落原目录，已立即abort，原HEAD及用户文件校验保留，再于隔离目录执行正式合并。
