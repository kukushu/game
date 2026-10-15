# 500人成熟城市：真实Unity窗口采样

本批新增独立的`HarborCityScenarioWindowProbe`诊断组件，不改模拟规则。正常游戏宿主显式识别`-harbor-city-scenario-window`后读取真实完整城市保存，建立与测试平地相符的高度2地形显示，拒绝非平地道路输入；不会误入原先通用验证标志的小型演示城。道路、建筑、车辆、分区和管线都沿用正常视图。CitySimulation继续由宿主真实墙钟推进；探针不直接Advance、不跳步、不降低业务频率。

实际调用关系：保存 → 正常CityCoreIndustrySession/CitySimulation → 正常HarborCityRoadCoreGame.Update → 原道路/建筑/车辆视图；探针只读tick、lastSteps、实际活动车辆和显示对象数，拥有采样缓冲和输出文件。显示地形适配只负责画面，测试模拟继续读取原有Flat函数。正常空城仍使用原地形Build()，未替换玩家地图或存档。

复跑：`./Tools/RunCityScenarioWindow.ps1 -Save Temp/CityScenarios/grid500-normal-final/inflight.hcity -Results Temp/CityScenarios/新窗口目录`。脚本构建Development Mono独立Player并依次运行1×、3×；窗口1440×900、vSync关闭、目标60FPS。每轮重新读取相同保存，8真实秒预热后记录60真实秒。保留完整初始输入SHA、Player哈希、源码状态和逐帧CSV。Warmup不算进下面的结果；这不是空城、孤立车辆测试，也不是无上限FPS测量。

输入SHA256：28e81d20d0d809bf8d93782fba2e52cc57def8a8d669fc7e5dce8bdc5122a8d8，为此前500人城市第15日后的在途保存。两轮预热倍率不同，所以开始tick分别36161/36481，这点明确记录，不伪装为相同开始tick。实际倍率按完成的固定tick差×0.05/墙钟计算，允许测量边界一两步量化差。

| 可见窗口，正常全城 | 1× | 3× |
|---|---:|---:|
| 平均FPS（目标60） | 59.994 | 59.986 |
| P95/P99帧耗时ms | 17.177 / 17.467 | 17.207 / 18.876 |
| 最大帧ms | 26.029 | 42.713 |
| 实际模拟/真实时间 | 0.99991× | 3.00098× |
| GC Alloc平均B/帧 | 80102.94 | 106850.15 |
| 记录帧/有效分配样本 | 3600 / 3600 | 3600 / 3600 |
| 活动车辆峰值 | 113 | 179 |
| 单帧最大实际固定步数 | 1 | 2 |
| 显示建筑/真实居民 | 282 / 500 | 282 / 500 |

分配由Unity ProfilerRecorder `GC Allocated In Frame`取得，未知值报告-1，不能用线程计数不支持时的0冒称无分配。采样循环使用预分配的数值结构缓冲，字符串和CSV在计时后生成；GC.CollectionCount也在写报告前截取。当前Unity Mono各代CollectionCount都返回相同数值（1×均103，3×均126），这些不是可以相加的独立GC次数；精确GC暂停本批未测。每帧仍有明显分配，不能凭60FPS就宣称表现层已无性能问题。

通过computer-use实际观察了1×/3×可见Player：道路成网、建筑/分区/管线显示、车辆位置随时间变化，500人口与282建筑真实出现，财政、货运、垃圾数据继续变化。自动截图保存在证据目录。最终完整Validate及保存成功；测量不依赖摄像机可见性决定模拟。

**没有进行这座平地城的鼠标施工验收。** 本批验证的是同城加载、显示与持续正常运行；不能将命令行装载说成鼠标建城。原地形实际鼠标流程仍参考R3Usability历史记录。窗口原始证据`Temp/CityScenarios/stage2-window500-verified`，提交轻量summary/environment和完整截图于`Docs/Benchmarks/2026-10-15-CityScenarios/Stage2Window500`。早期两轮试采也保留在Temp，未替换原文件；上表只用最终版本。

变更影响：宿主增加显式诊断启动分支和只读访问器，CityLandscape增加诊断平地入口，独立探针负责采样，脚本负责可重复构建与启动。没有修改交通/人口/经济/服务/存档格式，没有第二时钟、第二套车辆或库存。正常UI和已有视图成本都保留在窗口数据中。原有小演示窗口基准继续存在，但不用于声称本次综合城市达标。
