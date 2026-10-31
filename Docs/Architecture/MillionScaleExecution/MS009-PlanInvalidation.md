# MS-009：日切、路网与入口失效

基线 `e7fd202066e34bcbcecf2581f8f59390caae30be`；实现 `d647607`。仅执行分支 `codex/million-scale-execution-20261031`，未合并正式开发分支。测试宿主为 .NET 10 x64 Release 模型和 Unity 随附 Mono；不是有画面的 Unity Player 性能验收。

## 实现与审计

权威事实仍属于 CitySimulation、World 和 CityEntities。新增信号只使派生等待计划失效，未实现另一套居民或路网。正式 AdvanceRequests 保留逐成员轮询、原条件、原顺序、原 RequestCommute 和 0.25 秒重试；固定步、预算、欠步、资金、车辆及存档格式均未改变。

| 来源与实际写点 | 覆盖与处理 |
|---|---|
| CitySimulation.SyncCalendar：请求阶段之前以原 double 绝对时间 / float dayElapsed 更新 city.day | 请求边界比较 day；跨日发一次全局失效，不预测无条件睡到下一班 |
| society.settings.secondsPerDay：可变规则值；CalendarOffset 为当前 Simulation 的 readonly 值 | 日长发生变化全局失效；新会话从新 offset 建基线。分钟/门槛算法仍为 MS008 原精度 |
| World 成功道路提交的 networkRevision：普通事务及快速提交两条写路 | NetworkGeneration 变化全局失效；含新增、删除、拆分、车道/方向/速度变化；失败及过期计划不发布 |
| RebindAccesses：入口重绑复用 Network，但仍经 RoadEditPlan 增加 NetworkGeneration | 同样全局失效，安全但可能比必要范围更大；没有修改事务验证 |
| RegisterAccess / RegisterAccesses | 沿用原 AccessRevision，增加提交后内部端点信号；批量登记共享一个版本，逐端点收集、去重、稳定排序 |
| SetEndpointAvailable：唯一活跃端点写入口 | 增加派生 EndpointAvailabilityRevision 与内部信号；实际 Add/Remove 才增加，重复写相同状态不通知 |
| CityEntities 构造 / 建筑增删替换 / SetAvailability / 对外连接增改 | 均经 SetEndpointAvailable；新会话、冷读不继承旧观察队列。住宅/岗位/健康变化仍用 MS005–007 信号，不靠本任务替代 |
| 普通车辆移动、发车、任务更新、RefreshEndpoints、EditEpoch | 不使静态计划失效；不得把每步车辆移动误判为路网变化 |

所有写点先写事实，监听只记录端点。原居民请求阶段开始及每次 RequestCommute 返回后冲刷；观察者只能标记派生调度状态，不能在回调中编辑世界、派车或结算。首次订阅以当前事实建基线，最后退订解除 World 回调并清理待处理集合；无观察者不积累历史通知。

道路变化不能用固定半径或旧成功路线上的车道限定影响：新增远路也可能改变最短通勤时间，旧不可达目标也可能变得可达。因此本轮不冒险给道路事务做局部失效。纯登记入口或端点启停不会改变无关端点的几何/通行成本，可以只发相关端点。

端点版本有跳跃、监听覆盖与权威版本不符时，保留带 Untracked 原因的全局回退。每批端点按 ID 排序只是通知顺序；**未来消费者必须按真实 Members.Ordinal 去重执行**，不能按端点顺序执行居民。消费者必须记录失败查询依赖，包括缺失/不可达的 home/work/current-location 端点，不能仅记成功路线。MS010 将验证完整消费者；本轮定向消费者仅在 Tools 中。

## 已运行验证

命令均在执行工作树运行，输出为新建的本地 Temp 目录：

```powershell
Tools/VerifyPlanInvalidation.ps1 -Results Temp/MillionScaleExecution/ms009-final
Tools/VerifyPlanInvalidation.ps1 -Results Temp/MillionScaleExecution/ms009-targeted-final -TargetedOnly
Tools/VerifyPlanInvalidation.ps1 -Results Temp/MillionScaleExecution/ms009-growth -Growth
Tools/VerifyCityCore.ps1 -Results Temp/RoadCore/ms009-industry.csv
Tools/VerifySimulationBudget.ps1
```

- 60 条定向断言通过：首次观察、批量共享修订号、登记失败原子性、重复启停去重、失败/过期道路事务、速度/方向/拆分/远处新增/删除、重绑复用 Network、日切合并入口变化、真实跨日、日长变化、退订/重订阅、冷读、新入口使原不可达雇主可达及再次停用、只标相关员工且保持 Members 顺序、请求回调变化在较后成员前可见、真实发车/移动不失效。
- 漏掉底层入口或可用性回调的故障注入，均由独立修订号比较检出并全局回退。故意漏记“原不可达目标”的派生消费者，被独立资格对照检出；不靠全员修复扫描掩盖错误。
- 同一真实 30k 输入：2400 步预热 + 1200 步观测，208167 次 RequestCommute 请求序列逐行相同；12 个运行中检查点 + cold80 检查点的原生格式18完整字节哈希一致。有观察者的新版本与冻结 e7fd202 两个实际变更源码文件的旧版本对比。观测版本全程 3680 步产生 1 次全局通知、0 次局部通知。
- 复用既有增长测试：6200 步 mixed-growth、4000 步重划/拆分、4000 步真实30k，每组再比较冷读续跑80步；126 个完整状态检查点一致。新版本增长过程启用内部观察者。
- Unity Mono：23627 综合模拟、10000 Job 索引、1522 路网/事务/保存、83 分区、460474 水电拓扑断言通过。101 模拟预算/欠步/在途编辑检查通过。

真实输入仍为原 `test/Temp/CityScenarios/regenerated30000-20261017/city.hcity`，SHA256 `3F49BD1A02879BB686C24D5517902CC581C657220CE2B955107D89B6FA870B4E`。该文件格式16，复用已有冻结读取器导出**测试副本**18，不重新造城，不写回玩家文件；正式实验运行时仍只支持当前18，没有新增旧档兼容层。

第一次编译发现测试 Point 少写高度参数，已补齐。初次运动断言错误要求第30步仍有活动车；检查证明车辆已正常到达，改成检查期间确有活动车及全程零失效通知，未修改移动逻辑。失败日志留在 ms009-first/second；未标为通过。现有保存反射 SYSLIB0050 警告保留；没有本次新增编译错误。

## 维护成本与边界

每种模式先预热20000次，再测10000次。两个独立运行的观察值见 [成本CSV](MS009-Cost.csv)：

| 情况 | 10000次总耗时 | 托管分配 | 通知数 |
|---|---:|---:|---:|
| 无变化的 Flush | 0.209 / 0.209 ms | 0 | 0 |
| 同一端点反复停用恢复，每轮一次 Flush（含写标记） | 2.084 / 2.078 ms | 0 | 10000 |
| 每轮 day 改变再 Flush | 1.251 / 1.259 ms | 0 | 10000 |

这是小模型内部通知维护成本，不含未来消费通知时重新规划居民的成本，不能推算 FPS 或整体收益。通知层没有居民级常驻集合；新增固定版本/委托、一个端点原因字典与排序缓冲，容量随未冲刷端点峰值保留。无变化比较 O(1)，K 个不同端点维护 O(K log K)，全局变化发一次标记 O(1)；未来全局消费者必要时仍可能 O(N)。首次扩容、精确常驻堆字节及完整队列内存尚未量化，由 MS010 随真实消费者测量，不以本表零临时分配代替。

可核对材料：[139个状态点及请求摘要](MS009-Invalidation.csv)、[定向范围及原因位掩码](MS009-Scopes.csv)。原因位 Day=1、Calendar=2、Topology=4、Access=8、Availability=16、Untracked=32。完整请求 CSV、日志、二进制留本地 ms009-*。

MS009 完成。下一项 MS010 为唯一 IN_PROGRESS：接入完整影子，检查所有失效来源、真实请求不漏不乱、负控及维护/峰值成本。MS011/012 实际稀疏执行及 MS013 普通 Player 真实性能仍未验收；本轮未运行新的画面 FPS 验收，也不声称已实现百万居民或整体加速。
