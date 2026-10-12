# 局部跟车索引配对证据

协议与限制见 [TrafficFollowingIndex.md](../../TrafficFollowingIndex.md)。scan/index来自相同构建源hash、相同固定输入与原生配置，只有CITY_TRAFFIC_EDGE_INDEX_DISABLED开关不同；半径缓存两侧均开启。

- steps.csv：每侧2700步；summary.csv：每侧9轮；frames/profiler-frames：完整帧映射与原生时钟。
- module-summary.csv：按模式汇总所有City.Traffic节点，父子Total不可相加；paired-summary为整步/分位数/原生GC结果。
- configuration/build-source/build-options保留环境、输入、源码与编译开关。configuration的HEAD仅作标签，工作树构建以build-source为准。
- PairVerification验证任务/状态/Calls/帧覆盖；replay的两份step-states与ReplayVerification验证340步完整模型状态。
- ModelRegression是最终4112专项及既有全量模型回归；native-validation是本批普通Unity Player真实交通、建造输入、存读和26日继续运行证据。ProblemGUIUnverified保留隐藏Player的既有GUI覆盖限制。

原始调用路径modules.csv和九轮二进制保留于本机Logs/TrafficBenchmark/cityflow-following-v2-scan与cityflow-following-v2-index，未提交巨型录制。早期cityflow-following-scan/index单文件导入缺帧，不作为收益依据。
