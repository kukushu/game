# 真实返家尖峰：观察后不增加派车预检

基于aa8be4e之后相同三万人输入，正常执行2344步到6750，再观测6751–6753。只在Temp的World副本加只读Prepare/Commit钩子；没有修改产品World、物理、出勤或经济。采样不重建占用、不寻路；空间检查读取原本失败后的有效占用，包括换道影子。直接服务/货运重载不在此居民范围内，不能拿居民请求数当全城。

6751居民请求4866；新蓝图2226：Home2021次新蓝图成功提交，另6次缓存蓝图成功提交；Home205次新蓝图因SourceOccupied失败，其中196只有最佳路线起侧堵，9两侧都堵。其余2554个Home和80个Work占用失败使用已有蓝图，不是重复Dijkstra。Prepared但未Commit为0。

因此两侧廉价物理预检最多省掉9/2226条新蓝图（约0.4%），不足以解决长峰。不能为了让准入计数好看而复活以前昂贵的双向可达性预检；不能把选择另一侧空车道当等价优化，因为原最优合法路线可能恰好堵住。

观察/关闭观察均运行全部固定步，完整最终保存同SHA cff112f59b7d38ba101a5fa2e847ae2235d4cd0b78624b005e8da02ad6d49958。进程均正常退出、峰不到0.4GB、有300秒/120秒无进度/8GB及低系统内存保护。此批时间随运行批次存在波动：此前整段采样该步约1.28秒，此次观察/影子约2.52/2.54秒；不能把这次变化解释为观察开销或算法回归，也不据此发布加速比。后续性能配对需继续控制宿主/CPU条件并重复，而非猜测原因。

下一试验：合法双向居民路线原来对两出发方向×两到达方向做四次搜索。研究同一出发方向一次搜索两个到达方向，优先保持完整lane/connection序列、同分选择、自由流秒数和全部城市状态。先临时原型/冻结源码对照，通过后再决定是否进入产品。单行等不足四组合的情况优先保留原开销。没有扩大LRU、降低车辆或跳过业务。

数据为派生诊断，仅Tools/CityStepProbe与MeasureCitySteps扩展，无新增运行事实或存档格式；源码覆盖明确记录并仅在Temp生成，普通构建无钩子。证据Docs/Benchmarks/2026-10-17-ReturnPeak，完整保存和临时源保留Temp/CityScenarios/return-peak-{observed,shadow}。

## 路由正式优化后的真实Unity后半程（默认空间缓存）

延长有界诊断窗口到120秒，仍只抓6748–6808步附近60个观察帧，raw边界共61帧；原始约242MB，6GiB上限离线导出11.84秒/峰2.35GB/退出0。正常宿主没有直接Advance、跳步或减少车辆，120秒4619→8014共3395步，峰10018实车，所有帧显示数等于活动数，时钟欠步与暖/冷20步完整继续通过；Player正常退出。此窗口部分带原始Profiler，约28.29FPS/真实1.414×只是诊断现场，不能替代上批普通26.82FPS/1.341×或声称3×达标。

原始帧3对应6751返家尖峰，Main Thread 314.785ms，City.Simulation 261.206ms；ResidentRequests 250.725ms中PrepareRoute 237.198ms/2226条，前述改进已进入实际游戏，但仍是主要峰值来源。车辆视图34.394ms，其中GameObject.AddComponent 22.865ms/5967次（三组件约1989辆新车）；另外两帧还有5379/4365次组件添加。下一批优先独立验证显示对象复用，不把显示物体当真实车辆或减少活动车辆。

61原始帧平均Self：ResidentRequests 6.52ms、PrepareRoute 5.20ms、视图4.42ms、Occupancy3.43ms、IntentAndReservations3.36ms、Production1.75ms；Render Thread DrawBuffers4.28ms。Render Thread Semaphore等待40.76ms不可加到主线程预算；GPU未知-1。Profiler.FlushMemoryCounters约2.03ms/帧属于测量开销。帧53/58有GC.Collect约3.01/3.61ms，不足以解释返家237ms寻路或新增对象峰，不统称GC。

离线hierarchy导出按Self过滤普通节点，因此有的帧没有单列Main Thread根行；取总帧时间必须读threads.csv，不能把缺根行误判为无CPU帧。原始证据留Temp/CityScenarios/window30k-return-peak-raw，轻量帧3/时钟/恢复/资源摘要存ReturnPeak证据目录。原生多线程渲染是Unity内部；城市模拟依然单线程。
