# 三万人完整窗口：剩余长帧与分区显示归因（2026-10-17）

本批继承14650d8的默认通勤标量查询，不新增玩法、不重写模拟、不改0.05秒或经济。正常窗口与Profiler窗口分开；串行运行，无其他自己的Player/导出/大型模型任务同时计时。

## 已修正的测量隔离

旧自动窗口仍允许GUI改速度/相机。window30k-cost-final的0×标签实际推进1208步、请求约60秒，不是暂停；原始记录保留，新的独立校验明确拒绝。现在ScenarioMeasuring仅在测试flag启动：忽略输入事件，保持正常Layout/Repaint，逐帧写actual_rate并拒绝改变。普通游戏和manual inspect不锁。校验进一步核对暂停时完整存档SHA与输入相同，不能只看tick。

增加渲染计数；本场景Batches/Draw Calls计数器返回0，不能用于归因，新采样标为-1（未知/不可用）。主线程/渲染线程Recorder同样无有效值，不假造0；使用实际raw层级。Unity非分代GC的gen0/1/2值不相加，GC标记也不是完整暂停时长。

## 有界关键帧采样

整段30秒raw分别约2.03GB、1.83GB，首次导出超过6GB内存上限被监督器终止（峰7.19GB，1秒监测存在超调）；原件、失败资源记录不删除。随后只在暂停稳定区和tick4798起采60个观察帧，实际raw含61帧边界，约234MB；3×覆盖4798–4858，包含4801。两次导出约11.5秒、峰2.47/2.38GB、退出0。不是重复整段大文件导出。

命令：RunCityScenarioWindow -RawProfile -RawProfileSeconds 30 -RawFrameLimit 60 -RawStartTick 4798 -Rates @(0,3)。0×忽略tick触发，以免永远等不到。计时窗口部分帧启用Profiler，整段FPS不得当普通构建成绩。原始Profiler启用/刷计数本身改变耗时。

## 实际CPU层级

|范围|测量|结论与限制|
|---|---:|---|
|暂停 Render Thread / DrawBuffersBatchMode|平均Self 18.30ms|独立显示成本，非模拟；不能与其他线程等待相加|
|暂停 Main / City.RoadCore.Views|平均Self 4.66ms|保留10887辆真实车显示；另一次正常60秒Stopwatch约7.47ms|
|暂停 Profiler.FlushMemoryCounters|平均13.76ms|诊断开销，不能说正常游戏有同样开销|
|3×关键帧 Main frame3|350.23ms|该帧只执行一个完整步，不是数百步追赶|
|该帧 City.Simulation|288.76ms / 4.52MB|不是整个帧或整个引擎GC暂停|
|居民实际蓝图 PrepareRoute|149.43ms / 70次|实际选路仍原算法，剩余重要热点|
|通勤标量 PrepareCost|21.49ms / 3475次|并未神奇消失；与实际路线分开计数|
|GrowthBoundary|69.46ms（Self64.83）|需求汇总子项与候选遍历需进一步分开|
|Waste / PrepareRoute|20.08ms / 3次|不是此帧唯一原因；别把整个城市物流都归垃圾|
|UI EmploymentReadout|12.97ms Self|有节流但冷刷新仍扫描人口；只读，不可借UI改事实|

原始层级可展开：PlayerLoop → Update.ScriptRunBehaviourUpdate → BehaviourUpdate → HarborCityRoadCoreGame.Update → City.Simulation → City.Simulation.Step → City.Simulation.ResidentRequests → PrepareCost / PrepareRoute；并列看GrowthBoundary、Waste、City.RoadCore.Step/Occupancy、IntentAndReservations。表现看同一Update下City.RoadCore.Views，以及PostLateUpdate.FinishFrameRendering的Cull/RenderQueue；Render Thread单独看DrawSRPBatcher/DrawBuffersBatchMode。

更长后台2400步还发现tick6751：4866次实际准入查询、2226蓝图准备、8904底层搜索，居民阶段1263.67ms；通勤缓存未命中为0。它是另一批真实出行路线，不能误称11524次估计缓存问题复发。SourceBlocked计数没有覆盖所有居民准入拒绝，不能把计数0当作所有人都成功派车；需要进一步分类而非盲复活已拒绝的双向预检。

## 分区显示因果实验

同一e78c47da…暂停保存、1440×900、同样GameObjects及模拟，仅Temp副本在zones.Add后令Renderer.enabled=false。所有90733地块对象仍创建，未关模拟/居民/车辆，完整保存仍与输入相同。源码覆盖及Hash记录在environment；不是产品开关，也不是可接受的画面优化。初次临时注入把单行注释放在闭括号前，编译失败；日志保留，改为块注释后v2通过，失败记录不参与性能。

|场景|FPS|P99|最大帧|实际模拟倍率|对象与退出|
|---|---:|---:|---:|---:|---|
|全部正常显示|23.30|48.94ms|51.30ms|0|90733分区Renderer，22572道路，16441管线，10887车，7806建筑；退出0|
|只关闭分区绘制（诊断）|59.96|17.63ms|21.82ms|0|保留同样对象/同一完整保存；退出0|

这证明分区绘制是暂停瓶颈的重要来源，但还没证明合批收益，也没证明GPU算力是唯一瓶颈。下一实验保留所有顶点/三角形和每格可见性，按用途与空间块合并Mesh，避免90733个独立绘制对象；不改变地块规划、模拟几何或游戏规则。需对照正常全部显示、图像、动态更新及1×/3×，不可把关闭绘制的60FPS冒充游戏成果。

## 数据所有权与未完成项

本批仅诊断宿主、读数探针、监督/验证/导出工具；模拟和存档格式未改。新增对象计数只读视图集合，不派生人口或库存。临时HostSource只能构建隔离副本，元数据明确标注；最终要重建正常Player，不能把消失的分区留给用户。HTML已更新真实标量/蓝图所有权和流程，Source固定到14650d8。

三万人3×仍约0.995×、欠步136.96秒，未通过。五万人/十万人不以此视图实验推断达标；优先完成三万人当前独立瓶颈验证。原生退出崩溃本批未再现，但没有证据宣称根因已修复。

轻量证据位于Docs/Benchmarks/2026-10-17-Commute：keyframes-mean-self.csv、keyframes-worst-main.csv、窗口/时钟摘要。完整raw、所有帧、保存、截图及资源监督在Temp/CityScenarios/window30k-{locked,keyframes,view-baseline,zone-ablation-v2}，不提交大文件。
