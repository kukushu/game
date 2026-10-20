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

## 后续阶段与验证

进行中。各阶段完成后补充实际删除清单、保留原因、测试和完整状态对照结果及文件/行数变化；未运行项不记PASS。
