# 道路键混合哈希正式实现证据

实现、回退、协议与限制见 [RoadHashProduction.md](../../RoadHashProduction.md)。本批正常 Unity Editor 与 Player 默认启用，旧比较器只用于显式回退对照。

- `traffic-legacy/default`：同源空场景原生 Development 交通基准，每侧9轮2700步；完整平均步42.847→7.691ms。主模块、GC、真实步数/任务与最终状态配对。
- `window-release-legacy/default`：普通完整场景、真实可见窗口、相同1440×900视角和固定城市，各6轮300步。保留逐帧FPS/推进数据、配置、构建源哈希、实际运行比较器；所有帧前台、城市状态相同。Release GC计数器不可用，-1不是0。
- `window-development-legacy/default`：修复计数器Reset后重新Start的开发构建配对，同样完整画面/模拟、已知分配控制验证。不要与Release混算。
- `replay`：两侧340个完整逐步状态签名（包括原始分数累积器），全部相同。
- `normal-player-validation`：普通原生构建，无Benchmark/原型/窗口测量定义，真实建造输入、交通/换道/行人/货运/服务、存档读写及26日运行。隐藏自动验证中未检验的GUI条目仍保留。
- 两种全量回归与配对检查日志保留。道路专项含10000键与2272断言、20000稀疏大ID节点/39700边、方向/拆建升级/读取验证。

完整Player、9轮大型Profiler录制和原始模块CSV在本机 `Logs/TrafficBenchmark/road-hash-production-*`；完整窗口文件在 `Logs/FullWindowPerformance/road-hash-*`。不提交这些巨大二进制。第一次开发窗口的GC零值来自Reset停止采集，不能作为GC结论；其原始记录在 `road-hash-*-dev`，随后已修复并重新配对，本归档开发窗口使用 `dev-v2`。

固定保存文件SHA256：`46DF3C9F9A9800808334C30BF1A5DA1A463106DD11DA8CEA780CC78582B115BE`。Unity6000.6.3f1、Windows11、i5-14600KF、RTX3090；目标60FPS、VSync关。结论只对应本城市/配置，不是任意规模的性能保证或用户当前Editor城市的配对FPS。
