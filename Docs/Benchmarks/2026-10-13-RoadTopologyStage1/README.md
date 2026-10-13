# 道路表示第一阶段证据

设计见 [RoadTopologyArchitecture](../../RoadTopologyArchitecture.md)，结果/限制见 [RoadTopologyStage1](../../RoadTopologyStage1.md)。正常交通、道路施工/渲染和存档没有迁移；哈希优化两侧均默认开启。

- `traffic-before/after`：真实修改前后Development交通Player，每侧9轮2700步，相同存档/参数/诊断；Calls/任务/状态与GC配对，不拿目录名或类数量当性能收益。
- `window-before/after`：普通完整城市画面，1×/3×各3轮300步，目标60FPS/1440×900、相同视角/配置、全部帧前台；实际推进、全帧GC、源差异白名单及状态核对。最终统一鼠标位于非交互标题区域，排除原有吸附查询的临时分配。
- `construction-memory`：纯Mono转换/热映射/保留内存估算及同输入施工预览。全部预览合法、整个输出图JSON相同；不是原生游戏FPS，GC.GetTotalMemory不是完整Unity堆快照。
- `replay`：每步主动读取快照/映射车辆和不读取的两个分支，340个完整状态（含累积器）完全相同。
- `normal-player-validation`：普通原生游戏编译、原生中心线/路面高度和道路/地形Mesh哈希、车辆坐标及原生JSON、既有建造/交通/服务/存读/26日回归；未确认GUI项保留。
- 默认/回退全量模型、40604新增结构/动态/oracle检查、配对检查日志归档。

完整Profiler原始模块/9轮二进制位于本机 `Logs/TrafficBenchmark/road-topology-*`，完整窗口/Player在 `Logs/FullWindowPerformance/road-topology-*`，巨型二进制不提交。

异常记录：首次后窗口受Windows安全弹窗影响，中断文件保留在 `road-topology-after-security-interrupted`，部分构建/日志仍在原after目录，均不计性能；第二轮窗口鼠标在地图上，作为悬停开销调查记录保留在 `after-v2`，不作最终GC配对。最终使用既有构建重新运行 `before-v3` / `after-v3`（没有重新变更游戏规则或模型）。源码改变名单验证只允许本批表示/验证接入，其他文件必须保持不变。

输入是固定第128日城市SHA256 `46DF3C9F9A9800808334C30BF1A5DA1A463106DD11DA8CEA780CC78582B115BE`；Unity6000.6.3f1、i5-14600KF、Windows11/RTX3090。仅对应此城市/配置，不保证任意规模。第一阶段惰性转换未接入正常模拟，不将轻微计时波动当成性能优化。
