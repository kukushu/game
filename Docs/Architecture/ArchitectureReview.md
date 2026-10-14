# HarborCity 全工程架构审查

审查日期：2026-10-14（Asia/Shanghai）。当前已推送源码：`4a4796825f43462f65ee99eb2531656bd1a2678b`，分支 `codex/road-core-20261014`；完整游戏保留基线：`110c4eaa24fa69d55b08a5db2a6d19ec6da5dd7f`。这是一份源码审查，不是完整城市性能认证，也不是复刻完成声明。

## 范围与工作状态

实际读取了当前入口、CityModel 各领域 partial、道路/交通内核、居民/岗位/家庭、生产/货运/环境、市政/管网/生长、施工/渲染/存档以及已有基准。基线中 GetBuilding 的字典缓存与当前一致；旧完整入口通过 CityTraffic 驱动城市，而当前入口禁用它。

本地尚有未提交的 RoadEditHistory、World 撤销候选构造、RoadCoreChecks 和 VerifyRoadCore 修改。没有丢弃或纳入本次架构文档提交。重新运行核心回归通过 **2700 项断言**，撤销仅回退道路事实、不倒转时间/库存/资金；尚未接正常 GUI 或完成此批 Unity 窗口验收，不称产品功能完成。图表以已推送代码为准；撤销单列为本地探索。

## 判断与推荐

推荐 **B：保留经过验证的算法和领域规则，重新划分统一 CitySimulation、道路事实/派生网络、交通动力学与城市业务的边界**。不接受 RoadCore 因为新就成为最终答案；也没有证据说明所有算法必须重写。

最严重的结构问题是：完整游戏与新内核之间还没有统一的城市事实/生命周期/时间契约，迁移桥接器正在承担实际业务；Vehicle 把出行任务、货物、居民关联、路线与物理车混成一个生命周期；World 仍拥有道路编辑、时钟、派车和同步业务回调。只加几个接口不能解决这些事实和生命周期的混合。

保留共享几何、拓扑与采样分离、混合道路键哈希、车道局部占用、双占用换道、安全区与冲突图、候选事务、真实到岗劳动及资金核对。调整路线/物理位置、业务任务与车的身份、实体修改入口、时钟和存档边界。完整城市不能无限推迟：推荐路线 R3 必须恢复可从空地建设、划区、迁入、就业、供电供水排污、垃圾处理与财政循环的小城市。

## 当前真实运行链与完整游戏差距

HarborCityRoadCoreGame.Launch 禁用旧 HarborCityGame；Start 创建 CityCoreIndustrySession.Create。Session 拥有原 CityModel、World、CityCoreResidents、CityCoreRoadBindings 和 CityCoreIndustryBusiness。World.externalBusiness 为真，禁止合成 Depot 成为第二份城市库存；正常运行不调用旧 CityTraffic.Move。

CityModel 中道路/旧交通字段仍存在，当前迁移场景为空；原建筑、家庭、居民、岗位和工厂账户被实际使用。原市政、管网、分区、建筑生长、土地、贷款和原 UI 代码仍在仓库，但不等于已接当前默认入口。CityCoreSave 只保存这个业务切片，不能称完整城市存档。新场景原料有限，消费/进口/出口尚未接，不是可持续经营城市。

## 数据事实、派生状态与写入边界

事实是会影响未来的状态，不是所有可重新计算的东西。污染历史虽来自生产，具有滞后，必须保存；工资小数信用必须保存，否则产生/丢失资金。车道占用与路线蓝图可以重建，不应保存为第二份真相。路线是可重算计划，但在途既选计划属于可保存运行状态；不能随时静默换路线。

| 对象 | 当前所有者与可写方 | 事实 / 派生 | 生命周期与问题 |
|---|---|---|---|
| CityModel | Session 持有；多个 partial、桥接器可写 public 字段 | buildings/society/账户为事实；宏观 population 等为缓存 | 不是仅数据容器；Valid 包含旧路网/旧交通验证，不能用于新完整城；需要按领域拆写入职责 |
| WorldData | World 构造复制输入，Snapshot 输出复制 | clock/accumulator/ID/network/vehicles/accesses 是保存事实 | 合成 depots 仅隔离测试；把道路与交通保存根绑在一起不是长期城根 |
| NetworkData / PhysicalRoad / Junction | World 的私有副本经 RoadEditPlan 更换 | IDs、物理方向、中心点/截面/端切线为道路事实 | Network 接受 DTO 并持有引用；公共构造边界仍需不可变快照约束 |
| Network / Drivable / Geometry | Network.Rebuild 派生有向车道/连接 | Geometry 拷贝采样点；累计距离/邻接/冲突/空间网格派生 | Drivable 同时代表 Lane 和 Connection，当前无独立 LaneConnection 类；连接形状不够准确 |
| Vehicle | World 创建/更新/退休；internal setter，业务回调拿到同一对象 | route/cursor/distance/change phase/货物/目的/居民关联 | Current=route[cursor]，物理位置仍依附路线；到达立即退休，任务与车 1:1 限制公交/服务往返 |
| RoadAccess | World 注册/迁移；建筑保存真实门口 | endpoint/lane/distance/use/valid 是关系事实 | 建筑门口 XYZ 不属于它；当前门口 Y=0、偏好车道简化，接地形前要修契约 |
| CityBuilding | CityModel.buildings | id/坐标/入口/状态/库存事实；报告不是事实 | public List 和 public 字段让删建绕过索引与关系维护 |
| Household / CityResident | society.families → people 是当前存储所有权 | 家庭共同住房/储蓄，个人工作/技能/实际出勤 | householdId 反向关系需要校验；members 字典仅构造时建立，增长/死亡/迁移未接生命周期 |
| CityJob | society.jobEntities；SyncJobs/AssignJob/ReleaseJob | buildingId/wage/requiredSkill/occupiedCitizenId | Citizen.jobId 与占用者双向关系需原子更新；Job(id) 仍 Find |
| FactoryState / 商业库存 | IndustrialBuilding.factory、商业 Stock；原业务函数写 | 原料/加工进度/库存/财务/环境历史事实 | Stock 在建筑、raw 在 Factory；可以保留但明确 Inventory 领域写入者；没有独立完整企业模型 |
| TransportTask | 当前没有独立类，任务内嵌 Vehicle | cargo/purpose/origin/destination/citizenId | 建议拆 Shipment/TripTask 与物理车，先覆盖现有三种任务，不预建复杂供应链 |
| 占用 / 预约 / DispatchRoutes | World 内部派生模块 | 可复用工作桶、当前步预约、4096 条有界缓存 | 读档重建；换道影子没有独立可写位置；全路网替换会全清蓝图 |
| CityCoreIndustrySession | 当前切片组装、每步前后回调 | calendarOffset/dispatchAt/payrollDay 需保存 | 正在变成第二个总控类；不应继续在此堆市政和新玩法 |
| Presentation / Save | Unity Host / CityCoreSave+SaveCodec | Transform/Mesh 是视图；DTO 是冷快照 | UI 当前直接调用 World；存档反射绑定 public 字段，领域重排就改变协议 |

## 已确认问题与分级

### P0：全城时间和写入事务没有统一边界（代码证据）

位置：World.Advance/Step，Session.SyncCalendar/BeforeStep，Residents.Arrive。World.clock 是当前事实，City.day/dayElapsed 按 calendarOffset 换算；到达回调另加 StepSeconds 才得到步末。World.Step 在提交车辆时同步调用 business.Deliver 改原城。正确性测试覆盖当前链，却不是跨领域异常恢复协议。直接 Step 与 Session.Advance 的业务前置步骤不同。World.Advance 无工作预算上限，不丢步是优点，但极端长帧会一次追赶全部债务并锁住 UI。建议 CitySimulation 持有唯一整数 tick/欠步量，交通接收明确步区间，保存所有 dueTime/结算游标。按安全边界处理命令和到达；不要以丢时间解决追赶。

### P0：任务与物理车辆身份/位置耦合（已确认扩展限制）

位置：Vehicle.Current、World.DispatchResident/Dispatch/Step、LaneChanges.Route。路线必须 Lane/Connection 交替，换道要遍历剩余链并最终回到原目标 Lane；一条终点车道限制会影响合法换道。服务车、公交乘客、多站卸货不适合把每个业务任务等同一个车。建议物理 Position=(DrivableId,Distance,Reference=车身中心)，独立 RoutePlan 与游标；TripTask/Shipment 保存业务生命周期，Vehicle 仅关联当前执行任务。先不实现公交功能，但先解除模型障碍。必须重做拆分/反向/保存及换道目标位置测试。

### P1：实体注册与索引失效不统一（代码证据）

位置：CityBuildings.GetBuilding/BuildingsChanged，CityJobs.Job/SyncJobs，CityCoreResidents 构造。GetBuilding 已用 Dictionary，常规查找平均 O(1)，不能误报成线性。它仅比较 Count：直接 public List 同数量替换可使旧索引仍返回旧对象。members 仅一次构建；新增居民无映射，移除/家庭迁移可保留旧 Member。岗位 Find 为 O(J)，出勤与工资多次调用。建议领域实体表维护唯一对象、增删 revision、原子成员和岗位关系；外部只读，系统持有写权限。先用 Dictionary+紧凑遍历表，不需要全量 ECS。

### P1：工业运输匹配与环境仍绑定旧数据（代码证据）

位置：Session.BeforeStep；CityIndustry.AdvanceIndustry/EnvironmentAt。每 .25 模拟秒建 incoming 字典扫描 V，再 F×所有建筑过滤寻找商店，复杂度包含 O(FB+FS)，不是仅 FS。每 .05 秒遍历成员与建筑；AdvanceIndustry 对每个住宅扫描旧 trips，当前为空，**新货车重型交通影响不生效**。建议独立 Logistics 持有未满足需求/已预留货量的派生索引，按新增需求/交付/取消更新；局部道路暴露/区域环境积累接新位置，低频积分保留守恒与衰减。需要大量真实工厂/商店/居民综合基准，不能用 Depot 夹具替代。

### P1：货物所有权与结算跨系统隐含（代码证据）

位置：ITransportBusiness、IndustryBusiness.Depart/Deliver、RecordFactorySale。发货扣工厂 Stock，Vehicle.cargo 为在途；到达商店 Stock 增加、wholesaleCosts 记录、工厂销售入账。商店没有真实现金付款账户，这是已声明外部经营资金近似，不是全城资金闭环。receipts HashSet 随每笔运输长期增长，retired 建筑也保留；10000 活动车测试不能证明历史账本有界。建议业务拥有 Shipment 与载货账，交通只报告到达；结算状态与库存/账户同一提交，完成后退休或压缩审计。保留明确外部资金规则直到玩法确认，不能偷偷创造商店资金制度。

### P1：路口端口/连接几何尚未完成（代码证据）

位置：Network.Rebuild/AddLane、ConnectionConflicts。Lane 到路口中心偏移，Connection 用 p,c,q 二次曲线，端部未截到真实端口；静态冲突依赖这份几何，不能只把模型画漂亮。连接组合随进口/出口车道数增长，冲突构建是本地连接两两路径段比对。建议真实 JunctionPort、切齐有向车道的 LaneConnection、明确允许转向/合流；几何和冲突统一重建。此处值得现在修模型；但精度/限速/安全间距要测试，不直接承诺性能提升。

### P2：施工冷路径全量复制和重建（已测与复杂度证据）

位置：World.EditDraft/Snapshot/TryCommit、RoadConstruction.Connect、Network.Rebuild。多个候选副本、全网车道/冲突重建、所有车辆/入口校验；冷路径安全性好，应保留事务语义。当前 3000 车 split 约6.708ms、3.897MB/提交，12次样本P99无稳定统计意义；还没测巨型路网连续预览。建议先建立受影响 Road/Junction/Route/Access 反向索引，再局部版本和变更集；不能先删全量校验而没有替代证明。全网缓存失效暂可接受。

### P2：确定性承诺需要界定（未全面证明）

位置：World.Step intents.Sort，LaneChanges.Choose 与 residents.members.Values。车辆意图按 ID 仲裁，但换道先顺序修改影子桶、居民字典遍历决定派车先后；这不是全阶段不可变快照。当前同进程保存继续一致不等于不同平台位级一致。建议阶段清晰、显式稳定顺序和确定随机种子；目标先同构建可重复，不承诺跨硬件浮点锁步。不能盲目并行路口共享占用。

### P2：存档协议绑类布局（代码证据）

位置：CityCoreSave.Fields/Put/Get/Read。城市6/核心9，public字段反射+名称/类型强校验，SHA256 校验损坏，旧格式明确拒绝；原字节/用户文件未删。冷路径可用，但类拆分即改变格式、反射多次序列化+Snapshot造成大副本。建议新的显式 CitySnapshot DTO 保存事实、tick、任务、账户、建设事件/排程和随机状态；加载候选→索引→跨域校验→整体替换。无需历史迁移器。完整保存目前缺管网/分区/市政/预算/地图，是接回全城前的硬门槛。

### P3：暂时保留、暂不优化

几何二分 O(log S)、混合哈希字典、4096蓝图缓存、每步本地排序和 ID 意图排序已有实测支持，不因理论更优立刻重写。结构体不必然更快：之前24字节占用结构约15%退化，复用引用恢复。固定 .05 秒先保留；居民决策/环境降频是设计变化，不是等价移动优化。Jobs/Burst/ECS 尚无当前全城测量支持。UI primitives/即时GUI分配存在，但本轮不改画风。

## 模拟顺序：当前不能画错的细节

当前 Update 用 Stopwatch 真实秒×倍速 → Session.Advance → World.accumulator 逐 .05 步。BeforeStep：步首日历同步→道路版本变化后的入口/中断任务修复→居民请求/跨日工资→工业在岗劳动与工资/生产→环境衰减→到 .25 秒时扫描在途货量并匹配发货。

World.Step：Reindex 清/建本地占用与预约→若多车道则 Choose 顺序修改影子占用→读取占用计算跟车意图→按 vehicle.id 排序→串行路口仲裁/跨结构距离推进→提交各车位置与换道完成路线→同步 Deliver 业务回调→压缩退休→clock += .05→索引标脏。afterStep 再同步日历。**当前不是先换道判断再建立占用，也不是完成全交通提交后才批量结算。**

旧完整入口 AdvanceSociety→CityTraffic.Advance；其 Simulate 先移动（ReleaseJunctions→快照逐车换道/等待/Move），再服务，再居民/工业，最后每 .6 秒货运派发。AdvanceResidents 更新日内时间，日边界调用 CityModel.Tick。development.enabled 时 BaselineTick 做老化/健康/垃圾/火灾/治安、建筑状态/增长、迁入/岗位选择、税收贷款和统计；否则走早期租金家庭选择。这些未接新默认入口，不能把两套调度流程叠加。

各问题的固定提交文件、语句和行号见 SourceEvidence.md，HTML同时内嵌源码摘录。市政库存/垃圾处理本身仍有真实守恒规则可复用，但GarbageInTransit/WasteBalanceError读旧TrafficTrip；消防、警务、医疗到达在旧CityTraffic partial中。贷款按City.day日结，地图参数在CityMapState，分区用途为世界坐标键而非旧tile建筑数据库。这些不应仅改接口名后接入；需要共同任务/时钟/实体生命周期，保证缺服务与日结后果能实际发生。分析器只读观测有价值，但ObserveTraffic与旧路线依赖需要改查询契约，不将日志变成业务真相。

## 性能证据与复杂度

符号：V车辆、P居民、B建筑、J岗位、F工厂、S商店、L车道、C连接、G几何段、k车道占用、q本路口预约、r剩余路线、c_j路口连接数、g_j保护路径段数、E_u管网边。

| 操作 | 时间 / 额外空间 | 频率与边界 | 证据 |
|---|---|---|---|
| Reindex+意图排序 | O(V+Σk log k+V log V)，桶/意图 O(V+L) | 每交通步；无全车对全车 | 已测纯交通，不含全部业务 |
| 跟车/入口 | 已排序桶邻项O(1)，换道目标二分O(log k) | 每步/跨段；List.Insert 最坏O(k) | 源码；高拥堵插入规模尚缺 |
| 路口协调 | 每查询O(q)，冲突 membership 平均O(1) | 每接近/跨路口；稳定ID顺序 | 不是全城预约扫描 |
| 换道 | O(V)资格扫描；触发时O(r+log k+k) | 每步扫描，符合条件每5tick评估 | 长路线换道仍遍历；不可称纯O(1) |
| 居民请求+劳动 | O(P)遍历，叠加Job Find O(PJ)上界；岗位收入内部统计另有扫描 | 每 .05 秒；重复静态估计命中缓存 | 四居民实测不足以外推 |
| 发货需求匹配 | O(V+B+FB+FS)，incoming O(目的地数) | 每 .25 模拟秒，OfType商店枚举在F内重做 | 源码确定；多企业尚未实测 |
| 工业环境/暴露 | O(B+残留数+住宅×旧任务) | 每 .05 秒；新货车暴露缺失 | 当前旧任务空，不是优化成果 |
| 旧管网连接重算 | 建筑接线O(B E_u)，再局部连通分量/分配 | 旧 RecalculateHouseholds的development分支调用RecalculateUtilities；本函数无revision提前返回 | 已有局部建筑桶，不能称所有连接均B² |
| 原住房/就业选择 | 住宅候选/岗位搜索多重遍历及路线准备 | 旧日结/Review，未接新入口 | 需规模夹具，暂不扩原创玩法 |
| 路网/冲突重建 | O(L+C+G+Σ c_j² g_j²)近似上界，存几何/冲突 | 施工/读档冷路径 | 连接长度与路口度数决定成本 |
| 保存 | O(事实字节+校验)，多份快照/字节副本 | 玩家保存；不是每步 | 大城峰值内存未测 |
| 视图 | O(V log S_v)，每车若干Geometry.Sample | 每渲染帧，S_v单车所处几何点数 | 即时GUI/引擎每帧仍有GC |

新跑 VerifyRoadCore：2700断言；1000分散/集中平均0.318/0.301ms，3000为1.001/0.931ms，10000为3.543/3.324ms；10000 P95 3.797/3.559、P99 4.031/3.746ms，GC 2208/320B每步。夹具300测量步，不等于长期帧尾保证。此次本地含未提交撤销代码，不能伪称精确4a47968构建性能。

历史 Unity 实际窗口 TrafficScale/window.csv：cap60，3秒热身+6秒采样；10000的1×约59.99FPS/实际0.9998，3×约59.995–60FPS/实际2.9997–3.0000，P99最高26.614ms，约20–71KB/帧GC。这是合成Depot交通、真实渲染窗口，不是当前旧城市或全部工厂市政。过去完整旧城约7–8ms/步与新场景工作量/行为不同，不算直接加速比例。

## 必须新增的综合基准设计（未实施）

固定 CitySnapshot+seed+构建+步长+实体/任务分布。道路1k/5k/20k，建筑100/1000/5000，居民1k/10k/50k分梯度；这些为试验矩阵，不是产品承诺。至少两个配置：分散通勤+多个实际企业持续库存补给/消费，以及集中通勤+瓶颈货运；补给/消费必须先接真实规则，不能无限库存伪装完整城。

分别测1×/3×每步平均/P95/P99、每帧长尾、实际tick/真实秒、所有系统GC与峰值内存；纪录居民、在岗、待发/在途/中断、实际机动车数量，不仅统计路段。施工实验独立变更局部和主干，连做100次预览/提交；保存/加载大数据并在途继续；长期至少多日生产、消费、日结和退休。没有完整城市接入前标“尚不可执行”，不以空环境替代。

## 审查局限

本轮没有运行用户旧城市做新旧全城FPS配对；没有新的Unity窗口重测，引用已有CSV并标构建与场景。没有证明跨平台确定性或完整生命周期；不承诺未来永不重构。HTML 源码位置以审查commit自动提取，待提交的撤销从工作树单列。推荐是供用户确认的设计提案，本次不实施方向性重写。
