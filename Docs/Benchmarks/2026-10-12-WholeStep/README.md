# 整步归因与道路键受控研究

结论、实现边界及失效方案见 [TrafficWholeStepPlan.md](../../TrafficWholeStepPlan.md)。正常游戏默认没有启用哈希原型，也没有开启细分诊断。

- attribution-off/on：细分开关原生A/B，每侧9轮2700步；on的attribution.csv是逐步工作量，不提供交通决策。paired-summary记录额外测量开销，不能当算法加速。
- key-control/prototype：三张道路查询表的比较器原生A/B，原有细分关闭、已有采样一致；运行时源hash一致。Editor源只多出独立验证入口，不进Player。
- module-summary保留Calls、Total/Self与GC。帧表映射全部原生计数器/时钟；原始模块调用路径与九轮二进制位于本机Logs，不提交巨型录制。
- replay/scan-states使用原比较器，replay/index-states使用研究比较器（继承脚本旧目录命名）；两边Following局部索引与半径缓存开启，340个完整状态相同。
- 正常/原型模型回归、分布探针、配对校验日志保留。native-validation-v2为原型+细分的建造/交通/服务/存读及26日验收；ProblemGUIUnverified保留隐藏Player的GUI限制。
- InitialNativeInputFailure为首次研究构建未对齐窗口配置的匝道输入失败；只对齐既有1440×900构建设置后V2全套通过，未改变测试阈值或交通规则。

环境：Unity6000.6.3f1，Intel i5-14600KF，Windows11，固定第128日存档SHA见configuration。没有用户实城人工操作验收或发布版容量结论。
