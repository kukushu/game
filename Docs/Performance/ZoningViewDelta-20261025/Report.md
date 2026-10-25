# 分区显示增删更新优化（2026-10-25）

结论：保留原格子和全部几何，**只为变化的显示块收集替换列表和生成Mesh**。同城删路5组前后对照，显示同步中位数30.46→11.97ms（约减少60.7%），其原生Profiler托管分配16.917→0.279MB（约减少98.35%）。下一帧完整Main Thread从56.80→37.74ms。施工当帧仍约150–160ms，未解决全部删路停顿。

- 独立分支：`codex/experiment-zoning-view-delta-20261025`，未合并。
- 基线：最新诊断分支HEAD `7b285b797b457ce7d3cd3f7556de97cd62c046ac`。
- 实现/测试提交：`5aa4fbd`。最终报告提交见本文件Git历史。
- 正式开发分支、旧实验分支和用户存档均未修改。生产改动只有`HarborCityZoningViews.cs`，未改分区计算、占用、道路事务、寻路、经济、模拟步或存档格式。

## 改了什么

旧实现只在格子总数不变时使用SyncChanges。增删格子会重新生成全城分组List、比较所有组，再重建全部Visual快照；即使实际只变化2块，也分配约16.7MB临时分组数据。

新实现直接使用显示块原有的不可变、有序Plot列表作为比较快照。一次按原输入顺序扫描：

1. 每块仅建立一个临时游标。与旧Plot顺序相同的前缀直接比较，不新建列表。
2. 某块首次发现差异，才复制该块已匹配前缀，并收集剩余新Plot。匹配前缀被截短时只复制保留部分；整块消失则释放。
3. 只为变化块生成候选Mesh，全部成功后再提交。失败时释放候选，保留旧Mesh、旧比较数据和地形失效状态。
4. 输出块按输入中的首次出现顺序排列，块内Plot仍按原输入顺序。不是按字典枚举顺序，也不依赖ZoneCell.key唯一性。
5. 相邻格子常属于同一块，复用当前游标，避免每格重复字典查询。首版没有这处复用时约19ms，最终约12ms；首版结果留本地，不混入最终统计。

删除全量Visual快照与单独的同数量分支，统一处理增删、重排、占用、用途、位置变化。`occupiedBuilding>=0`不显示、负数项目预留仍显示；available等业务字段不参与显示规则，全部沿用。地形回调变更或显式InvalidateSurface仍重建所有可见块，这是必要失效。

**仍有O(C)全量比较，没有宣称完全局部计算。** 设C为格子数、K为显示块数、D为变化块中的Plot数：旧增删路径临时列表/快照工作为O(C)；新路径比较O(C)，临时记录O(K)、替换列表O(D)，Mesh工作只与变化块有关。不新增权威分区数据，也不修改原输入List、格子或几何。

## 真实Unity Player配对

同一个真实城市检查点，SHA256 `0110347D2BC4E40E8CD98E841EE92F62D675935D076A21AE58D7F7A5A9ADB6CD`，tick642633、536车辆、删除道路87。加载完成后8秒显示预热，同一相机/1440×900/VSync0/目标60FPS，Windows11/i5-14600KF、Unity6000.6.3f1 Development插桩Player。不是新造的三万/五万人城市。

保持固定存档时刻进行暂停脚本施工，包含真实在途车辆；不是运行中50×人工点击，也不是普通Release FPS成绩。前后三次后另补旧→新→新→旧交错复测，共旧5次、新5次。raw录制独立，不进入以下中位数。保存、截图、断言和80步续跑均在性能窗口之后。

| 指标 | 原版，中位数 | 新版，中位数 |
|---|---:|---:|
| 显示Sync耗时 | 30.463ms | 11.967ms |
| 显示Sync范围 | 30.00–33.25ms | 11.59–12.81ms |
| Zoning.Cells计算 | 21.801ms | 22.726ms |
| 分区刷新所在完整帧 | 56.799ms | 37.739ms |
| 该帧范围 | 54.76–59.55ms | 36.36–39.30ms |
| 该帧托管分配 | 22,580,312 bytes | 5,942,752 bytes |
| 施工当帧完整Main Thread | 160.722ms | 157.024ms |
| 施工调用本身 | 156.034ms | 152.633ms |
| 旧式跨帧协程间隔 | 214.315ms | 191.899ms |
| 最终块数/实际重建数 | 593 / 2 | 593 / 2 |

格子数都为256787→256700；道路重算仍6条、Dijkstra仍17次，World/Network逻辑没有变。施工当帧的小幅差异属于本次采样波动，**不归因于显示优化**。确定收益是下一帧显示约18.5ms和16.64MB分配减少。旧“协程间隔”跨两帧，继续单列，不冒充最长单帧；真正最长帧仍主要受原施工事务限制。

原生Mono线程分配API不支持，细分CSV保留-1。函数分配使用独立raw Profiler：旧Sync16,916,784 bytes、新279,224 bytes；新区分为StreamCompare77,916、BuildChanged200,280（含两个BuildChunk199,356）、Commit56 bytes。新raw显示约11.93ms，旧约32.99ms，CPU录制成绩不与普通采样混比。原raw复用上轮相同基线源码/存档，具体路径及哈希见environment.json。

## 正确性与回归

- 原生Unity：**544468项检查通过**。同时对照冻结原SurfaceMesh、旧版持续更新缓存，以及每次重新构建的旧全量分组结果。覆盖中间插入/删除、尾部截短、删除整块、反转/随机重排、重复输入key、用途、占用、项目预留、跨64单位边界、地形变化、清空/复用、超过65535顶点。
- 比较块顺序、块内顶点和三角形序列、法线、材质、Bounds、索引格式、可见性；未变化块GameObject/Mesh身份必须保留。验证Sync不修改任何输入格子、字段或顺序。
- 专门让第一个候选块成功、第二块地形采样失败：旧画面和比较快照完整保留，重试未变化输入不重建。不是只测试第一块就失败的简单情况。
- 已有Editor **835项检查通过**，包括精确分组几何、占用、跨块移动、地形失效、失败原子性及借用材质生命周期。
- 真实城市全部**593块、3,953,376顶点、4,447,548三角形**逐块对照冻结旧分组Mesh：顺序、顶点、索引、法线、材质、Bounds和可见性一致。该重型验证在采样后分帧执行，不把断言时间算入性能。
- 最终五组固定相机截图（1440×900 RGBA）逐像素比较，**全部0差异**。截图留本地，pixels.csv含尺寸/结果/哈希。
- 正常退出的10个普通窗口及1个raw窗口，共**33份完整存档检查点**均与冻结基线逐字节相同；11次80固定步冷热续跑通过。边界SHA `140EB4CBDECFCD0003428A4500C88DEF6E4D4FC8CEE7CC8F24A1D4CA01F50548`，终态 `9C231E1EE087885D290F63CAD605FB4EF7626286A4E2AF7C6B70DADC9B587487`。

另外用256787个小四边形的有界显示夹具检查同数量路径CPU回归，原生无图形Player三次中位数如下。它仅验证显示Sync，不是实际城市FPS或完整模拟：

| 夹具操作 | 旧ms | 新ms |
|---|---:|---:|
| 无变化 | 12.000 | 9.369 |
| 单格占用切换 | 18.254 | 10.462 |
| 格子数量变化 | 30.492 | 10.629 |

每次双方Plot/Renderer/重建块数一致，最终该原生测试退出0、峰工作集约468MB。没有发现为增删路径提速而使同数量路径明显回退的证据；没有据此宣称全游戏1×/50×FPS改善。

## 限制与剩余问题

- 仍需约11ms流式比较，Zoning.Cells约22ms及施工帧约150–160ms未动。后续空间索引/寻路/路网工作属于另一轮，本轮到此。
- 地形失效、大量真实格子变化时，必要Mesh仍会重建；不隐藏格子、不降低几何精度来取得成绩。
- 重型真实Mesh对照的旧、新两个窗口均写出全部几何/存档/续跑通过结果，但退出时在图形/输入清理阶段等待；旧窗口到240秒被终止，新窗口确认清理等待后手动终止。保留原日志，**不把它们算作正常退出通过的性能样本**；最终统计使用另外正常退出的旧5/新5及raw窗口。前置一次探针构建因方法组未显式转Func而失败，已修复诊断工具，未进入成绩。
- 未解决此前退出阶段ZoneBoxTool.Dispose等生命周期异常，未修改该模块；不声称所有日志无异常。没有新测正常运行50×、长期内存或全部其他施工操作的Player耗时。未重跑所有与显示无关的历史核心测试，不冒称本轮全部通过。
- 当前保存格式未变，无旧档适配或迁移代码。

## 资料与复现

代码：[HarborCityZoningViews.cs](../../../Assets/Scripts/Runtime/Presentation/World/HarborCityZoningViews.cs)。新增测试只放Tools并复制进Temp验证工程。复用原有VerifyZoneViewBatches/RunRecoveryWindow/CityWindowProfilerExport，不新增数据管理框架。

```powershell
./Tools/VerifyZoneViewBatches.ps1 -Results Temp/ZoningViewDelta-Checks-Reproduce
# 旧版仅替换显示源码，模拟和其他代码相同：
./Tools/RunRecoveryWindow.ps1 -Save Temp/BatchRecovery-20261025/case-old-live/before.hcity -Results Temp/ZoningViewDelta-Old-Reproduce -Scenario frame-breakdown -ViewReference 7b285b7
# 新版；重型实际Mesh对照可选，全部在采样之后：
./Tools/RunRecoveryWindow.ps1 -Save Temp/BatchRecovery-20261025/case-old-live/before.hcity -Results Temp/ZoningViewDelta-New-Reproduce -Scenario frame-breakdown -CheckZoningViews
```

同目录CSV包含每次帧/函数测量、汇总、原生Profiler两帧层级、准确计数、存档校验、像素结果与同数量路径微基准。原始本地目录 `C:/Users/16934/Documents/unity_game/road-core-integration-20261024/Temp/ZoningViewDelta-20261025/`：正式旧样本old1–old5，新样本new2–new6，raw为new-raw；重型对照baseline-final/new1、首版optimized、测试checks-stable、Editor结果editor-checks.txt。旧raw复用`Temp/DemolitionFrame-20261025/final-raw/window/window.raw`。准确源码、程序集、输入和raw哈希见environment.json。

可以独立评审这项显示改动；本轮未合并任何分支，也未开始优化其他瓶颈。
