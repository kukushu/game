# MS-012：班次与下班时间实际接管

执行分支 `codex/million-scale-execution-20261031`；基础守卫基线 `45877170494a5fc99b82408c920b6460605f1b69`，主要实现及性能二进制源码 `e7d57ba`。默认构建不启用实验，也未合并正式开发分支。

## 实现

`CITY_RESIDENT_TIME_SCHEDULE` 必须同时启用 `CITY_RESIDENT_GUARD_SCHEDULE`，否则编译明确报错。正常游戏仍原轮询；实验模式沿用同一个 CityCoreResidents-owned GuardSchedule、原成员顺序链接和定时堆，没有第二套居民/路网/模拟循环。

原业务体仍先读取真实资格、岗位、班次和原通勤估计，并应用原 observedHome/work/revision 与 lastCommute 最大值。只有本轮确定不发起请求时，才把这些有效输入交给已验证的 MS008：

- 在家且具备出发资格：Departure，最早从 tick+1 检查。
- 仍在原雇主工作、健康、有资金且本轮无须返程：ShiftEnd。
- 所有请求依旧走原 0.25 秒重试、实时车辆准入和提交逻辑，不用预测代替派车安全检查。
- 无岗位/无资金等尚未接管的失败资格类别继续保守检查，不宣称已实现全事件驱动。

计划仅保存派生等待状态和定时堆中的 Tick；到期先清除等待标记，再按原顺序重新执行原业务判断。它不会保存另一份路线、重新报价、调用额外寻路、跳过模拟步或改变经济规则。

## 失效、所有权和退化

| 变化 | 处理 |
|---|---|
| trip/retry/visit、健康/能力/日期/通勤观测、家庭与住址 | 现有所有者通知清除有关成员计划，重新分类基础守卫 |
| 岗位绑定、雇主资金资格 | MS005/006 通知只使有关成员重查；资金在原安全边界冲刷，不在余额 setter 内派车 |
| 新入口/端点启停 | 复用 MS009；按 home/work/current-location 反向依赖失效，包括原来缺失/不可达目标 |
| 道路拓扑/速度/方向、入口重绑、覆盖不明的版本变化 | 保守清除全部现存时间计划，不假定影响仅在附近 |
| 日切、日长或开发模式 | 清除时间计划；日期阻塞集合继续由基础索引处理。模式改变商业班次但资金资格不变的情形也覆盖 |
| 建筑/成员结构变动、冷读 | 从真实成员顺序重建派生集合；所有缓存不进保存协议 |

依赖记录只在产生计划时维护。变动后计划失效即可安全重查，旧依赖可暂时产生额外失效，不能漏掉新查询依赖；下一次计划替换关联。驻留数量受当前成员/端点关联上限约束，未为每栋建筑存独立路径。

日期、模式、日长与日历采样均在安全边界检查。日历事实与原统一时钟的 `offset + tick*0.05 → modulo → float dayElapsed` 不一致时，清除计划并保守检查，不在手动修改分钟后继续睡到旧截止时刻。普通时钟推进不会全量失效计划；日切和真正全局变化仍可能集中重查，成本必须计入后续 Player 验收。

同轮回调唤醒较后成员仍可本轮执行，已访问成员留到下一步。失效只改派生集合，原业务体保留全部实时复核。普通车辆移动不使静态时间计划失效；到达写回 trip/location/attendance/观测事实时，通过现有通知重查。

## 独立对照与结果

三种二进制使用同一原始三万人输入：A为冻结原全员循环（a843016）；B为冻结MS011的 CityCoreResidents 与 GuardSchedule（4587717）；C为当前时间计划。原存档仍只读，SHA256 `3F49BD1A02879BB686C24D5517902CC581C657220CE2B955107D89B6FA870B4E`，复用已有测试导出器生成同输入18副本；没有新增游戏旧档兼容、保存字段或协议版本。

自有正确性任务完成后，.NET10 x64 Release模型顺序 **A/B/C/C/B/A**；每次2400步预热，观测3000步。计时只包 Advance，排除加载、保存、Validate和冷读；两侧相同的请求摘要采集仍在。不是有画面的 Unity Player，也不把它换算成实际倍率或FPS。

| 指标 | A 全员 | B 基础守卫 | C 时间计划 |
|---|---:|---:|---:|
| 平均固定步 ms | 2.9249 | 2.7141 | 2.2063 |
| P95 ms（两次观测均值） | 4.4258 | 4.2950 | 3.8866 |
| P99 ms（两次观测均值） | 8.2449 | 7.9893 | 7.7529 |
| 最长固定步样本 ms | 36.7635 | 36.0470 | 25.3910 |
| 分配 bytes/步 | 34292.94 | 34295.11 | 34315.21 |
| 预热后托管存活 bytes | 251779308 | 253769976 | 256623888 |

C比B平均再降低约 **18.7%**，比A约 **24.6%**。C比B多约 **2.72MiB** 托管存活，比A多约4.62MiB；暖分配增加约20bytes/步，没有GC降低收益。这是整个模型观测差值，不是精确峰值或百万城市容量证明。

本轮模型P95/P99也改善，但两个样本的最长步不能证明玩家约100ms画面长帧已解决。日切集中唤醒、结构重建及真实Unity渲染/GC仍待MS013；因此不默认启用、不自动合入正式分支。

## 已运行验证

```powershell
Tools/VerifyResidentGuardScheduling.ps1 -Temporal -Results Temp/MillionScaleExecution/ms012-paired
Tools/VerifyResidentGuardScheduling.ps1 -Temporal -Growth -Results Temp/MillionScaleExecution/ms012-growth-final
Tools/VerifyResidentGuardScheduling.ps1 -TemporalControls -Results Temp/MillionScaleExecution/ms012-time-controls-final
Tools/VerifyResidentGuardScheduling.ps1 -Temporal -ControlsOnly -Results Temp/MillionScaleExecution/ms012-controls
Tools/VerifyCityCore.ps1 -TemporalScheduling -Results Temp/RoadCore/ms012-core.csv
Tools/VerifySimulationBudget.ps1 -TemporalScheduling
Tools/VerifyResidentWakeShadow.ps1 -Full -ControlsOnly -Results Temp/MillionScaleExecution/ms012-shadow-compat
```

- 三方26个正常/跨日/生命周期/在途删路检查点与126个增长/重划/拆分点全部相同，共152个状态点、两侧比较304行；观测段482141请求摘要和原生18最终保存字节相同。六次独立计时回放再次逐项核对，冷热续跑一致。
- 18项基础守卫、1000轮独立168槽多word动态顺序oracle继续通过。
- 13项时间类对照通过：观测时间改变、手动分钟、日长、商业模式、雇主资金、疾病、岗位、不可达入口恢复、道路降速提前出发、失败编辑、同轮较后唤醒、住址变化；并在长等待中冷读，合计6600步跨到首次应出发时刻，暖/冷完整状态一致。
- 故意反射绕过lastCommute通知后，独立冷建oracle能发起请求、暖计划遗漏，被明确检出；没有全员扫描修补计划来隐藏漏通知。
- 两个实验开关真正启用后的Unity Mono：23627综合、10000岗位、1522路网/事务、83分区、460474水电及101预算检查通过。原影子工具定向与负控保持可用。

原MS008精确时间计算未重写；当前命中率/每类唤醒数没有独立计量，不用候选估算冒充性能数据。现有保存反射SYSLIB0050警告保留，没有新增编译错误。

## 交接

原始日志、测试副本和二进制留 `Temp/MillionScaleExecution/ms012-*`；独立六次成绩在ms012-paired/isolated-*。必要材料：[成本](MS012-Cost.csv)、[三方状态](MS012-State.csv)、[时间定向](MS012-Controls.csv)。没有新建归档管理框架。

MS012完成已证明类别的实际时间接管；未覆盖资格类别保守轮询是明确边界。MS013为唯一IN_PROGRESS：真实普通Unity Player配对，区分FPS/倍率/欠步，检查约100ms长帧、日切与施工、增长、GC和内存；只有完整游戏收益可靠才判断默认启用或选择性集成。尚未进行Player验收，不宣称达到了50×或百万城市目标。
