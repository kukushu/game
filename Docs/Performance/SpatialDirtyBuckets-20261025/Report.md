# 空间索引：直接定位脏桶（2026-10-25）

结论：此前约28.4ms索引更新的主要浪费确实是全局寻找旧桶。本轮取消这段扫描，**按所有变化Drivable的旧几何精确找桶**。同一真实城市五组配对，索引更新中位数32.25→15.02ms，约减少53.4%；原全桶扫描中位19.19ms，替换后的定位及过滤合计中位约0.62ms。

本次不增加常驻的“道路→桶”表。旧Shape已经保存不可变Geometry，回放少量旧几何即可得到相同覆盖桶；此场景63720个旧Shape中52个Drivable变化，实际只需过滤10个旧桶，旧算法会检查全部12870桶。

- 独立分支：`codex/experiment-spatial-dirty-buckets-20261025`。
- 基线：分区显示实验最新HEAD `286124c0faed3e9dc1e0a0418f77764493ad3aa7`；保留其显示优化及此前拆路恢复逻辑。
- 运行时实现提交：`7259c85`。后续测试/报告提交见Git历史。
- 生产代码只修改`CityRoadCoreSpatial.cs`，不修改Network.Rebuild、RemoveRoad、车辆恢复、Dijkstra、建筑业务、模拟步、经济、道路/分区显示或保存格式。未合并其他分支。

## 实现与正确性边界

仍保留原变化识别：当前全部Drivable与旧Shape比较Geometry引用，以及StartDistance/EndDistance的double位模式；旧ID消失也加入变化集合。因此处理的不只是道路87，也包含真实变化的车道、连接、方向及裁剪端点。

原来随后遍历全部`prior.cells`，检查桶中的Segment是否属于变化ID。现在：

1. 对每个变化ID，如果存在旧Shape，回放其旧Geometry的原始采样段，收集旧覆盖桶并去重。新增ID没有旧桶。
2. 只过滤这些桶中的变化Segment，保留未变化Segment原顺序。新索引先复制桶字典；共享旧桶只读，必要时复制列表。
3. 按原Network.Drivables顺序插入变化对象的新Segment。新几何进入其他共享桶时仍走原Copy-on-Write保护。
4. 未变化桶、Segment和几何继续共享，不保留另一个Network或索引实例的引用。

插入与回放使用**同一个提取出来的supercover网格遍历函数**。保留原浮点运算、负坐标、轴向/垂直段，以及经过角点时两个侧桶的顺序。必须回放完整原始Geometry，而非只看裁剪后的有效区间：原索引也在原始段经过的桶中保存了裁剪无效Segment，删除/裁剪变化时不能遗漏这些旧记录。

该遍历用值类型visitor和泛型约束，不为每个段创建委托或常驻反向列表。`Project`本身未改；半径、距离、去重、排序、连接过滤和命中选择全部保持原逻辑。

复杂度只在本轮目标上变化：旧定位最坏检查全城桶内引用O(E)，新定位取决于变化旧几何经过的格子和脏桶中的引用。**仍有O(N)Shape建立/比较、O(B)桶字典复制，以及原有变化段生成阶段的全网筛选**。不宣称整个索引更新已完全局部，也不宣称Network已经局部重建。

## Unity Player实际测量

输入与前两轮相同：`Temp/BatchRecovery-20261025/case-old-live/before.hcity`，SHA256 `0110347D2BC4E40E8CD98E841EE92F62D675935D076A21AE58D7F7A5A9ADB6CD`，tick642633、536车辆、删除道路87。是真实玩家城检查点，不是新造的三万/五万人城市。

Unity6000.6.3f1 Development插桩Player，Windows11/i5-14600KF/RTX3090/Direct3D11、1440×900、VSync0/目标60FPS。加载后8秒显示预热；不提前建立删路后的索引。暂停在相同存档时刻，保留真实在途车辆，调用生产施工/入口重绑/恢复/显示路径。不是运行中50×人工点击或Release FPS。

旧、新各5个完整计时窗口，顺序old1→new1→old2→new2→new3→old3→old4→new4→new5→old5，无重叠测试Player。单独raw窗口作层级和分配归因，保存、截图、断言及80步续跑在采样之后。

| 指标 | 原版，中位数 | 新版，中位数 |
|---|---:|---:|
| Spatial.Build | 32.247ms | 15.023ms |
| Spatial.Build范围 | 26.87–39.30ms | 11.65–19.73ms |
| 全旧桶定位/过滤，或新局部定位/过滤 | 19.192ms | 0.617ms |
| 原索引桶数 | 12870 | 12870 |
| 检查的旧桶数 | 12870，全桶foreach | 10，直接计数 |
| 变化Drivable数 | 相同输入/相同变化判定 | 52，直接计数 |
| 施工调用整体 | 192.459ms | 164.361ms |
| 施工所在完整Main Thread帧 | 198.282ms | 168.722ms |
| 完整施工帧范围 | 165.41–310.31ms | 146.82–240.18ms |
| 下一帧完整Main Thread | 41.290ms | 39.508ms |
| 施工帧分配 | 约56.022MB | 约56.024MB |

本轮未修改阶段也有明显耗时波动，原因未进一步确定。上轮28.44ms不是固定基准值；本轮旧中位数32.25ms与新中位数15.02ms使用同一组配对条件。五对的索引耗时均下降，但**不能把完整施工帧中位数约30ms的变化全部归因于这项约17ms的局部改动，更不能宣称解决全部200ms停顿**。仍有超过200ms的样本，全部保留。

索引构建内的全局Shape表、比较、字典复制和新段生成基本保持原工作量。首个新样本细分为：复制0.574ms、Shape建立3.775ms、比较5.020ms、找脏桶0.358ms、过滤0.262ms、新段阶段1.479ms，合计11.651ms。其余逐次数据见scopes.csv。下一层仍主要是全局Shape工作，本轮未继续修改。

### 分配与原始Profiler

旧raw复用前轮同基线空间代码、同存档的`Temp/ZoningViewDelta-20261025/new-raw/window/window.raw`，新raw见本轮new-raw。只作归因，录制CPU成绩不与普通采样混合。

原生Mono线程分配API不可用，scope中保留-1；原生Profiler的Spatial.Build分配旧6,836,054、新6,837,796 bytes，基本不变。没有新增全城反向表，少量临时HashSet/埋点约KB级；**这次主要减少CPU扫描，不是降低内存分配**。新raw绝对CPU也受波动影响，不用它挑选最好成绩。

### 退出异常的处理

全部11个窗口（旧5/新5/raw1）均完成计时、完整存档和80步冷热断言。其中old4和new1在已有图形/输入退出清理阶段等待，分别在确认业务校验完成后终止；其余9个正常退出。没有把两个异常写成正常退出PASS，也没有删除其有效计时窗口来筛选成绩，resources.csv明确记录。

另列两边都正常退出的配对old2/new2、old3/new3、old5/new5：索引中位33.29→15.02ms，方向一致。完整数据和所有终止状态均在summary/runs/resources中。未修此前ZoneBoxTool.Dispose等退出问题，也未为本次测试改游戏退出流程。

## 正确性验证

- 冻结基线`286124c`空间源码，旧、新分别编译同一测试，**13680次查询、523695条有序命中完全一致**，比较包括ID、distance、separation和三维坐标的原始double字节。
- 不只比查询：每阶段序列化全部桶的键、桶内Segment顺序、ID/连接/有效标记、裁剪投影缓存、端点与参数。合并查询/桶数据/真实城市施工和80步终态的**254076253 bytes文件逐字节相同**，SHA256 `E047AB67DEE196551650737375197329C55E2608514B9368F4795280EFAAC332`。
- 保留各旧Network及索引，后续编辑/构建后重新序列化，确认旧桶没有被修改。覆盖连续多代未查询的索引继承；新增、拆分、升级、单行/反向、删除、速度变化；曲线、高程、高ID、网格角点侧桶、负坐标、轴向/垂直段、零/微小/64半径及连接查询。
- 用例观察到11个共享Geometry但裁剪端点变化的样本、6个实际编辑系列中的连接形状变化样本；独立冷夹具复用ID不算编辑变化。测试要求这两类覆盖非零，不以放宽比较通过。
- 真实Player **33份完整存档检查点**与冻结基线全部相同，11次80步冷热续跑一致，五组截图均0像素差。格子数256787→256700、分区593块重建2块、恢复搜索17次等不变。
- 本轮实际通过：150937道路核心检查、23614城市业务、10000就业、1522方格道路、83分区、460474道路水电。核心测试中的并行运行计时只作测试输出，不作为Player性能成绩。
- 没有新测正常1×/50×FPS、长期模拟倍率、所有广域编辑的性能或更多城市规模，不声称这些已经验收。没有更换存档格式或旧档适配。

## 资料与复现

实现：[CityRoadCoreSpatial.cs](../../../Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreSpatial.cs#L65)。只有这一个生产文件变化；测量工具复用原RunRecoveryWindow、AddDemolitionFrameInstrumentation及CityWindowProfilerExport，专项测试扩展原VerifySpatialProjection。

```powershell
./Tools/VerifySpatialProjection.ps1 -Results Temp/SpatialDirtyBuckets-Reproduce -Reference 286124c -Save Temp/BatchRecovery-20261025/case-old-live/before.hcity -Road 87
./Tools/RunRecoveryWindow.ps1 -Save Temp/BatchRecovery-20261025/case-old-live/before.hcity -Results Temp/SpatialDirtyBuckets-Old-Reproduce -Scenario frame-breakdown -SpatialReference 286124c
./Tools/RunRecoveryWindow.ps1 -Save Temp/BatchRecovery-20261025/case-old-live/before.hcity -Results Temp/SpatialDirtyBuckets-New-Reproduce -Scenario frame-breakdown
```

原始数据本地：`C:/Users/16934/Documents/unity_game/road-core-integration-20261024/Temp/SpatialDirtyBuckets-20261025/`，old1–old5/new1–new5/new-raw；最终冻结测试projection-coverage，核心core.csv及城市city.csv。输入、程序集、代码版本和raw哈希见environment.json。提交轻量CSV/Profiler选定两帧层级和测试比较摘要；不上传原始raw、254MB比对文件、Player或存档。

最坏情况下，很多道路都变化时仍会访问很多桶；本轮只证明该真实局部删路场景的收益。后续若继续压缩全局Shape阶段，需要另行证明失效集合完整和旧索引不可变，不能直接只检查道路87。到此停止，不自动合并或扩展优化。
