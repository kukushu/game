# 旧版代码清理报告（2026-10-20）

## 基线与边界

开发分支codex/road-core-20261014，清理前5c2770ed75b670d0ee679922f7fcc9ead48e278a。本地恢复分支backup/pre-legacy-cleanup-20261020指向该提交；三个城市CMD及.editorconfig原有未提交内容保持原样，另存于Temp/LegacyCleanup-20261020。main、Git历史、历史性能数据和交接文档不改。本次只清理实现，不改变业务与寻路规则。

## 第一阶段清单与依赖

| 类别 | 范围 | 处理依据 |
| --- | --- | --- |
| A 完全废弃 | HarborCityGame及其23个旧表现/交互partial、旧窗口性能入口、CityRoadView | 正式启动已经是HarborCityRoadCoreGame；仍存在启动时禁用旧入口的引用，需同时解除。检查序列化GUID后移除旧组件和对应meta。 |
| A 完全废弃 | CityTraffic/TrafficState、旧车道/路口/逻辑路由、旧出行/调度/车辆显示；旧专用测试、Editor和诊断入口 | 新World/Network/TripTask/Reservations和CitySimulation已有对应正式实现；以新版核心测试、完整保存对照和Unity验收替代旧引擎测试。 |
| B 新旧共用 | CityModel各partial中的建筑/居民/家庭/岗位/服务/统计和验证 | 不能整文件删除。Entities绑定后的道路入口、就业和水电是新版路径；未绑定回退、旧Tick/结算、旧交通字段和旧查询形成隐藏依赖。逐成员追踪，保留活跃规则与序列化状态，去除旧运行流程。 |
| B 新旧共用 | CityRoads文件中的RoadNode、通用几何；CitySaveData中的BuildingSaveData及专用DTO | 管线/地图/建筑姿态仍用RoadNode和几何，新.hcity仍用BuildingSaveData。拆出必要数据/纯计算，删除旧可编辑路网和JSON整城存取。 |
| B 需分离 | 日志/统计的旧交通观察、旧火警/医疗/学校等数据与执行代码 | 现有.hcity保存了居民相关字段，保留其合同及被当前规则使用的数据；不能借清理接入新玩法或伪造空适配。调查调用链后再删执行端。 |
| C 新核心依赖 | CityCoreIndustrySession、CitySimulation/Clock、Entities、CoreGrowth/Population/Residents/Production/Payroll/Finance/Trade/Consumption/Utilities/Waste、CoreSave | 正式单线程0.05秒模拟与唯一事实，保留行为。 |
| C 新核心依赖 | HarborCity.RoadCore下的World/Network/道路施工/车道与路口预约/路径/存档；新UI、模板、分区块/显示和现有Core诊断 | 不按历史来源删除。 |
| D 待核实 | Scene/Recovery Scene/Prefab脚本GUID、Editor菜单、条件编译入口、冻结历史对照工具与反射保存字段 | 先检查再迁移或记录保留原因；历史报告不作为运行根，冻结算法只可用于明确隔离的对照。 |

真实正式调用链：HarborCityRoadCoreGame → CityCoreIndustrySession → CitySimulation固定步 → 各业务模块 → RoadCore.World.MoveStep；CityModel和Buildings/Household/Resident/Job是被引用的数据和规则，并非整个旧引擎均可删。新CoreSave不调用CityModel.Valid/旧JSON入口，但多个共享方法仍带旧路网回退；这正是第二阶段需要拆分的部分。

## 完成结果

现在正式运行链仅为：`HarborCityRoadCoreGame → CityCoreIndustrySession → CitySimulation → RoadCore.World`。居民、工业、贸易、消费、财政、垃圾、分区和水电使用现有 CityCore 模块；车辆、道路、车道、路径、预约和施工事务使用 RoadCore。0.05 秒固定步与欠步规则不变。没有旧 CityModel.Tick/CityTraffic 同时推进，也没有第二份可编辑旧路网。

### 已删除及拆分

- 删除 HarborCityGame 全部 24 个 partial、旧 UI/交互/显示、CityRoadView 和旧窗口/交通基准运行入口。Launch 直接创建新宿主，不再先寻找或禁用旧游戏。
- 删除 CityTraffic、TrafficState/TrafficTrip、CityLanes/CityJunctions 及旧移动、寻路、出行调度、服务车辆执行流程；删除 CityRoads 旧可编辑路网、旧拓扑/道路施工/宽度/方向等辅助实现。
- 删除旧 CitySaveData 整城 JSON 保存、读取、格式 23–34 迁移，以及旧城市 Tick、旧人口/房租/发展循环、旧交通观测和日报分析执行代码。
- 删除上述旧引擎专用根目录测试、六个旧诊断探针、十二个旧比较/运行脚本及八个旧 Unity 测试入口。新版测试继续覆盖道路、车辆、安全、居民、资金、垃圾、分区、水电和保存；未通过删除失败断言宣称通过。
- 同步删除确认失效的脚本及目录 meta。Unity 实查 2 个 Scene、0 个 Prefab，无缺失脚本；当前宿主、地形、道路显示 MonoScript 均能解析。旧入口 GUID 未发现序列化资产引用。
- 原 CityTrafficBenchmarkBuild 改为 CityPlayerBuild，移除旧基准构建入口/旧交通编译符号，保留当前 Player 构建和历史原始 Profiler 导出能力。现有 Core 脚本引用已更新，历史 CodeLayout.json 和架构文档仍作为历史证据保留。

### 必须保留的共享数据和规则

- BuildingSaveData 及住宅/商业/垃圾场等 DTO 原字段、转换保留，移入 `Simulation/Core/CityBuildingSaveData.cs`。当前 .hcity 仍读取 16/17、写 17；RoadCore 二进制版本 15 保持。原 v16 用户保存路径不改，已有输入文件不改。
- RoadNode 迁入 `Infrastructure/Map/CityRoadNode.cs`，仅是管线等需要的节点数据；JunctionControl/stopFrom 是现有保存字段合同，不再有旧路口执行引擎。原纯几何函数原样提取到 CityPlanarGeometry，保持浮点求值顺序。
- CityModel/Buildings/Residents/Jobs/Households 等仍被正式系统使用，逐成员去掉旧运行方法、道路/交通字段和回退；岗位构建前的真实记录查询保留并明确命名。建筑入口从 CityEntities/RoadCore 查询。
- Clinic/School/FireHouse/PoliceStation、MedicalStage/CrimeWarning、尸体记录、居民健康/教育等仍被建筑目录、查询或保存使用的数据集中保留；旧医疗/消防/警务/教育/殡葬调度执行器删除。这不代表这些玩法已重新接入新版。本次不扩展玩法。
- 工业劳动、库存、工资、污染/噪声及财政规则保留。原正式路径的旧 traffic.trips 始终为空，因此去掉旧交通暴露遍历后保留原来的 heavyTraffic 衰减及零新增行为；没有借此引入新交通噪声规则。
- RoadCore 的 DepotBusiness 是同一个内核的隔离夹具业务，正常城市仍使用 CityEntities 的唯一库存和资金，不是另一套运行引擎。冻结几何/水电对照只在 Tools 使用；历史 Profiler、存档、Player、交接文档和 Git 历史保留在原处。

未留下用途不明的旧运行引擎。仍保留的历史来源代码是上述共享合同、真实规则、纯计算或明确隔离的验证资料，不再通过 CityTraffic/CityRoads 执行。未为编译添加空替代方法或固定返回适配器。

## 实际验证

生产代码基线 `5c2770ed75b670d0ee679922f7fcc9ead48e278a`；最终代码/工具提交 `a897d7e`。同一平台、输入和固定步对照，加载/保存验证均不混入游戏操作耗时；本轮不提出性能提升结论。

| 实际执行 | 结果 |
| --- | --- |
| RoadCore 核心安全/方向/拓扑/拆分反向删除/资金及二进制继续 | 150936 检查通过 |
| CityCore 综合劳动/库存/财政/通勤/卸货/中断及完整存档继续 | 23469 检查通过；岗位索引 10000 检查通过 |
| 方格道路、框选分区、道路水电、新城模板 | 分别 1518、83、3234 检查通过 |
| 固定步/欠步/批处理/20×50×/失败事务/在途恢复 | 85 检查通过；规模场景构造 18 检查通过 |
| 同输入清理前后完整城市回放 | 9 个完整二进制保存逐字节一致，含 3000 人城、240 步、冷恢复继续、新城道路/分区/增长；SHA 见旁附 CSV |
| 冻结水电分配旧算法 | 51 快照、39933 建筑实供及全部分量/总量一致，冷恢复继续通过 |
| 直接几何扫描与现有覆盖索引；动态预算/拆管 | 3001 精确覆盖检查及动态供给验证通过 |
| 冻结建筑施工冲突算法 | 360628 精确比较通过 |
| 业务只读诊断 | 商品守恒通过，完整规范保存不变且源文件字节不变 |
| Unity C# / IMGUI / 方格和框选工具 / 地形 | 隔离项目实际编译、Layout/Repaint 和现有程序化工具检查通过 |
| Unity 正常 EnterPlaymode | 真实自动启动恰好一个新宿主、80 设施、固定步推进、城市 Validate 和冷读取通过；2 Scene 无缺失脚本 |
| Windows x64 Mono Release Player | 工业/实际车辆变道拆路、建城分区增长/迁入通勤返家、贸易消费/财政/道路编辑/在途保存三组原生验收通过 |
| 基线与最终 Player 建城对照 | 采用相同修正测试窗口，最终完整保存 SHA256 均为 24bfa4b4fd9d3ad69648a0d3ac69566fe932d0fe36ad383234b26dfd26c866f4 |
| 原生 Player 3000 人成熟城市冒烟 | 原存档 3000 人/706 活动车，预热 8 秒后实际窗口运行 60 秒、1200 固定步、峰 859 车；窗口结束后 20 步暖/冷全城逐字节一致 |
| 源码/meta/工具审计 | 119 脚本 GUID 合法且无重复、旧引擎声明/启动依赖为零；活跃 PowerShell 全部解析成功；诊断单文件编译 47 项通过，另 5 项使用所需 .NET64/冻结辅助源后均编译通过，其中业务/水电/冲突实际运行通过，ImportExperiment/LogisticsTrace 仅编译 |

### 测试中发现的既有问题与边界

1. 旧建城验收固定 65 秒可能在下班返家前结束；保留旧 Player 相同失败记录。改为完整模拟日观察且要求同一居民到岗劳动后实际返家，新旧 Player 都通过并且完整存档相同；未改变业务、安全或经济断言。
2. 业务探针把 v16 原文件直接与 v17 写出格式比较会误判。改为查询前后完整规范保存对照，并额外确认输入文件完全未变；未改变保存格式或放宽数据比较。
3. Unity Editor 自身 Search.SearchDatabase 在进入 Play 附近仍报 IndexOutOfRange，另有云配置请求失败日志；没有 HarborCity 调用栈，游戏 Play 和独立 Player 检查成功。没有把 Editor 日志称为完全无异常。
4. 原生工具验收是实际渲染帧中的程序化操作，不冒充人工鼠标逐项试玩。三千人是本轮代表性规模冒烟，未重新跑三万/五万性能配对，不声称规模性能提升。Release 采样未提供逐帧 GC 字节，记为不可用，不记零。
5. 移除的旧医疗等测试针对已废弃执行器，不用空壳新版测试伪称对应玩法已移植；本轮保证的是现有正式 CityCore 行为不变。

## 规模变化与交付

同口径统计递归 .cs 文件、splitlines 行数（不含 meta/文档/Temp）：

| 范围 | 清理前文件/行 | 清理后文件/行 | 减少 |
| --- | --- | --- | --- |
| Assets/Scripts | 181 / 20890 | 119 / 9393 | 62 文件 / 11497 行 |
| Tools | 119 / 7622 | 54 / 3593 | 65 文件 / 4029 行 |
| 合计 | 300 / 28512 | 173 / 12986 | 127 文件 / 15526 行 |

阶段提交：`91b4fa5` 调查清单；`3bfdf74` 共享合同/几何分离；`a585cd3` 旧入口/表现删除；`aed3b88` 旧路网/交通/JSON/城市执行器删除及完整回放；`a897d7e` 工具清理和最终 Unity 验证。最终代码 SHA 以上述 a897d7e 为准，报告交付提交见本文件 Git 历史；最终推送 HEAD 另在交付消息列出。

所有原始日志、两个基线/最终 Player、完整状态检查点和失败记录本地保留于 `Temp/LegacyCleanup-20261020/`。正式本地启动脚本只指向已验证的 `Temp/CurrentCityPlayer`，不再回退旧构建；原 ManualCityPlayer 正在运行，保留进程及原文件，并有完整备份。构建文件不提交 Git。三个用户城市 CMD 和 .editorconfig 原有修改未纳入提交，内容与清理前保护副本相同。恢复分支仍可恢复清理前代码，main 未变。
