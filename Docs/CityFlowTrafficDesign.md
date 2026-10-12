# CityFlow 源码对照与 HarborCity 交通改造路线

最新实施更新：共享道路查询层的哈希聚集修正已经 [正式默认启用](RoadHashProduction.md)，普通Editor/Player均生效；真实渲染3×约58.63FPS/实际2.998倍，完整步约7.69ms。没有改变CityFlow对照中指出的逐车即时提交语义；继续在新成本下研究局部查询和规模，不能直接套用多阶段并行。

2026-10-12。HarborCity审阅基线为49d936e；已阅读[交通研究](TrafficPerformanceResearch.md)、[基准协议](TrafficBenchmark.md)、[半径缓存结果](TrafficRadiusCache.md)及当前交通代码。CityFlow完整源码只读克隆于本机Temp/Research/CityFlow，不嵌入产品，不编译或移植其引擎。

CityFlow固定版本：`81ee0f47659ca66177a71f81676691c58ee89184`。以下链接固定到该版本，位置来自本地实际源码，而非README或旧网页缓存。事实与设计解释分开；没有运行CityFlow自身性能基准，也不声称已完成其全引擎竞态审计。

## 1. 数据组织：可行驶结构负责车辆集合

| 类型/函数与源码位置 | 源码事实 | 对HarborCity的启发/限制 |
| --- | --- | --- |
| [roadnet.h：Segment，27行](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/roadnet/roadnet.h#L27) | 保存Lane归属、起止距离、索引，以及车道车辆链表的迭代器列表；它不是另复制一套车辆 | 用空间/车道局部候选代替全城扫描；不必照搬C++迭代器和链表 |
| [Road / Drivable / Lane，roadnet.h 173/233/293行](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/roadnet/roadnet.h#L173) | Road包含车道、几何与待寻路车辆；Drivable统一Lane和LaneLink的长度/限速/几何/车辆链表；Lane另有分段、下游LaneLinks及waitingBuffer | Harbor已有RoadLane标识和道路索引，应复用，不新建重复Road真相；先给交通实例增加派生局部集合 |
| [RoadLink / LaneLink / Cross，roadnet.h](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/roadnet/roadnet.h) | RoadLink组织道路间转向；LaneLink是独立可行驶路口连接，具有入/出Lane、车辆集合和Cross列表；Cross保存两连接的静态交点距离/安全范围与动态通知车辆 | 可研究movement连接几何和冲突复用，但当前按节点预约的语义不能直接变成LaneLink模型 |
| [Lane::available / canEnter，roadnet.cpp 428/437行](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/roadnet/roadnet.cpp#L428) | 只读车辆链表尾部判断入口空间；可进入和可生成并非同一条件 | 不需要为入口检查整个城市；但其车长/速度阈值和Harbor现有Gap、入口保守规则不同 |
| [Lane::initSegments，863行](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/roadnet/roadnet.cpp#L863) | 沿已按距离降序排列的车辆链表，从高编号分段向低编号分段一次分配迭代器；写vehicle.segmentIndex | 单车道分段准备约O(n+s)，不是每车再扫描全城；Segment数量不等于车辆数 |
| [getVehicleBeforeDistance / AfterDistance，877/889行](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/roadnet/roadnet.cpp#L877) | 从给定分段向后/前查局部列表；Before找较小距离（后车），After找大于等于距离（前车），遍历方向不同 | 方法名“Before”不是前车；有序局部查询不等于所有场景O(1)。该实现还会复制分段的迭代器列表，不能当成完美零成本容器 |
| [Segment::insertVehicle，943行](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/roadnet/roadnet.cpp#L943) | 按距离降序插入；remove/find在该局部集合里寻找 | 局部维护可降低候选规模，但局部查找/插入仍可能线性 |

车辆顺序不是仅由类型定义推测：Engine::updateLocation按buffer目标距离降序排序跨结构车辆，再push到目标Drivable尾部；等待车从尾部进入；LaneChange::insertShadow插在目标后车之前，Segment也降序插入。正常跟车不超越的模型维持其余旧车顺序。当前源码并没有每步把每条车道全量重新排序，也没有保证重叠/异常状态下自动修复排序。

Harbor需要动态铺路/拆分/宽度升级/桥隧/反向，车辆还保留已走路线前缀；不能直接使用CityFlow加载路网后稳定的裸指针关系。局部集合要有道路版本/生命周期协议，作为可重建派生数据，不写入城市存档。

## 2. 完整模拟步与并行边界

[engine.cpp Engine::nextStep，566行](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/engine/engine.cpp#L566)的顺序如下（换道相关三项受laneChange配置控制）：

```text
Flow.nextStep
→ planRoute → handleWaiting
→ initSegments → planLaneChange → updateLeaderAndGap
→ notifyCross
→ getAction → updateLocation → updateAction
→ updateLeaderAndGap
→ signal.passTime（未由RL控制时）→ replay（可选）→ step++
```

| 阶段及源码位置 | 实际做什么 | 依赖/工程解释 |
| --- | --- | --- |
| [threadPlanRoute，272；planRoute，450](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/engine/engine.cpp#L272) | 按分配的Road处理planRouteBuffer，主协调线程将有效车加入Lane waitingBuffer，失败路由另处理 | 规划完成后才能判断合法起始车道；不是运动每步都重新Dijkstra |
| [handleWaiting，502](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/engine/engine.cpp#L502) | 每条Lane检查队首一个候选，空间允许才变running并入车道 | 不等价于Harbor每次Dispatch按现有入口规则尝试发车 |
| [threadInitSegments / threadPlanLaneChange，392/374；scheduleLaneChange，792](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/engine/engine.cpp#L374) | 车道建立分段，各车辆形成意向，局部缓冲加锁合并；主线程按urgency顺序更新目标邻居、发信号、重新验隙并插shadow | 并行意向不直接并行修改目标车道集合；优先排序会改变Harbor现有先处理者优先关系 |
| [threadNotifyCross，317](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/engine/engine.cpp#L317) | 按路口清通知；看出口尾车、LaneLink内车辆和合法入向首车，给沿路交点通知相关车 | 静态Cross+动态局部通知；不是永远缓存路口是否可通行 |
| [vehicleControl，188；threadGetAction，402](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/engine/engine.cpp#L188) | 计算速度、梯形位移、换道横移及Buffer；跨Drivable请求进入线程局部buffer并合并 | 主要读当前控制状态，写自身下一状态，避免其他车看到半提交结果 |
| [threadUpdateLocation，282；updateLocation，477](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/engine/engine.cpp#L282) | 各Drivable所属线程移除离开者；结束车的全局集合/销毁加锁；主协调线程排序pushBuffer并加入目标结构 | 避免多个车辆worker同时无序写同一车道；目标索引维护与位置提交分离 |
| [threadUpdateAction，415；Vehicle::update](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/engine/engine.cpp#L415) | 统一把Buffer写回Controller、速度等状态，并清标志；清除已删除blocker关系 | 一批车的计算依赖提交前状态，和Harbor逐车即时更新不同 |
| [threadUpdateLeaderAndGap，429](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/engine/engine.cpp#L429) | 每条Drivable按车辆顺序单次遍历，传入上一车作为leader；Lane更新流量历史 | 同结构前车O(n)整批更新，结构边界才向下游查找，不是每车O(N) |

多线程的事实：Engine.h持有按车辆/Road/Intersection/Drivable划分的工作池，start/end Barrier与mutex；loadRoadNet轮转分配道路、路口和Drivable，车辆池有自己的threadIndex。各thread函数前后等待屏障，与threadController顺序匹配。pushBuffer、laneChangeNotifyBuffer合并和全局vehiclePool/Map的修改有显式锁；Vehicle::Buffer延迟提交。[engine.h](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/engine/engine.h)、[Barrier::wait](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/utility/barrier.cpp#L4)。

工程解释：性能依赖结构所属线程、阶段只读状态、局部写入、串行归并和屏障，而不是给所有查询加锁。partner/shadow在insertShadow中分配到与原车相同的vehicle threadIndex，避免vehicleControl互写配对buffer分属不同worker。这个观察不是“所有并发都已形式证明无竞态”，共享对象寿命、路由随机数和第三方API仍要审计。

## 3. 车辆、跟车与换道

[vehicle.h ControllerInfo / Buffer / LaneChangeInfo](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/vehicle/vehicle.h#L49)分离当前位置、drivable/prevDrivable、速度/leader/gap/blocker/route，与下一状态dis/speed/drivable/end等标志；位置是沿结构的double距离，不是Harbor的segment+float progress。

- [Vehicle::setDeltaDistance，vehicle.cpp49行](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/vehicle/vehicle.cpp#L49)可一轮跨多个Drivable并将余距离传到下一结构；[update，107行](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/vehicle/vehicle.cpp#L107)才提交并更新Router。Harbor已经支持短边跨越剩余时间，不重复实现。
- [updateLeaderAndGap，157行](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/vehicle/vehicle.cpp#L157)同Drivable直接使用前一车；没有前车时向下游drivable尾部查找，遇LaneLink还检查同起始Lane的其他连接，处理路口连接重叠。局部不意味着只查当前边，短边/合流边界必须保留。
- [getCarFollowSpeed，212行](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/vehicle/vehicle.cpp#L212)考虑前车速度/长度、制动能力、minGap、headway与interval；[getNextSpeed，308行](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/vehicle/vehicle.cpp#L308)叠加加速、路段限速、路口和换道让速。Harbor没有相同加速度模型，复制公式会改变安全/通行规则，不属于保持行为的索引优化。
- [LaneChange::updateLeaderAndFollower，lanechange.cpp27行](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/vehicle/lanechange.cpp#L27)用目标分段查前后车，前车缺失时检查连接尾车的车身占用；安全间隙依据制动距离。源码还留有入口后车过近的TODO，不把实现视为无需边界测试的标准答案。
- [SimpleLaneChange::makeSignal / estimateGap，151/221行](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/vehicle/lanechange.cpp#L151)有冷却、近路口和当前gap条件，比较左右邻Lane预计间隙，要求Router后续连接可用；不是每辆车随意随机换道，也不是Harbor当前转弯提前选道策略的直接替代。
- [insertShadow，71行](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/vehicle/lanechange.cpp#L71)为目标Lane插入shadow，原车继续在来源Lane；两个表示都有纵向占用，并更新目标后车leader。Engine.vehicleControl取partner安全速度的较小者；完成换道将原ID映射给partner并清关系，旧表示结束。shadow不是新旅程，也不能作为新增人口/运输任务。Harbor已有laneChangeFrom+remaining双占用，可用一个对象的两个索引成员表达，无须复制居民/货物实体。

## 4. 路线、静态连接与路口

[router.h](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/vehicle/router.h)拥有Road序列、anchorPoints、当前Road迭代器和planned Drivable队列。[Router::getNextDrivable，router.cpp39/49行](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/vehicle/router.cpp#L39)复用planned连接，Lane→LaneLink→目标Lane，选择还能接下一Road的合法连接；[update，78行](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/vehicle/router.cpp#L78)推进当前路段并消费计划前缀。selectLaneIndex优先小车道差，初始选择可使用随机数。

[dijkstra / updateShortestPath / setRoute，160/228/245行](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/vehicle/router.cpp#L160)基于Road连接寻路；setRoute支持重新设锚点，并有失败恢复。默认RouterType是LENGTH；DURATION分支存在但DYNAMIC仍是TODO。不能据此宣称当前默认每车动态按拥堵重寻路。Harbor已有单起终点路径缓存，需解决的是重复有效性检查及目的地/入口变动失效，不是重写Dijkstra。

[Intersection::initCrosses，roadnet.cpp515行](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/roadnet/roadnet.cpp#L515)在加载时为LaneLink对求交点，建立距离与安全范围并沿连接排序；[Cross::canPass，603行](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/roadnet/roadnet.cpp#L603)再用通知车辆、预计到达、转向优先、进入时间等动态判断，含blocker环检查。它不是仅靠一张永久可通行布尔表，也不等同Harbor预约+停车牌/信号/行人的先到规则。

## 5. HarborCity对照：复用、改造和不照搬

| Harbor当前位置 | 已有机制/瓶颈 | 最适合借鉴的思想 | 本轮边界 |
| --- | --- | --- | --- |
| CityTraffic.Move.Following | 约3.16ms/步；每个近邻有向边遍历全部State.trips，约O(N²k)风险 | 按实际占用的有向结构取局部候选，保留跨边距离窗口 | 本轮只替换候选来源，保留车距/车道过滤和原Min归约 |
| CityLanes.Headway/Space/EntryLane | 有早退但调用时全局扫描；换道中需双占用，入口还查 incoming | 复用局部结构，再评估有向车道与节点入向集合 | 未来分别实施，不将仅Current/Next桶误用于所有入口规则 |
| CityJunctions.ExitClear/PendingWalker/WalkingEntryClear | 出口窗口/行人附近仍全局扫描 | 路口入/出连接与相关行人邻域，静态Cross思路 | 先保留动态预约语义，不立即替换成Cross::canPass |
| CityTrafficControls / HighwayGapAllows | 全局扫描且依赖排队先后、主线、合法出口 | 局部入向/合流候选，稳定rank/ID优先 | 不能采用桶加入顺序作为新的公平规则 |
| CityTraffic.RouteValid | 约5.51ms/步，逐步扫描剩余路线 | planned/版本复用，但完整失效事件是关键 | 审核道路、建筑、目的地及原地路线变更后独立缓存 |
| IsJunction / DistanceTo / LimitCrossing / ChooseLane Self | 上批半径后仍约11.54/9.90ms的父Self | 预先准备静态连接与路线距离，不在查询中反复派生 | 尚未全部分配到细分节点，不声称Following是最大Self根因 |
| CrossingRadius | 已按CityRoads实例/revision缓存 | 静态派生数据复用 | 不重复实现；路径/ConflictPath仍可另批研究 |
| CityRoads / RoadLane / TripLane | 图、方向、kind/速度/结构索引已存在；车道连接隐式计算 | 借鉴静态LaneLink关系，未来按revision派生 | 不新增重复建筑/道路数据库，不改独立实体ID/世界坐标 |
| 单步流程 | Release→按快照顺序逐车换道/等待/Move→服务→居民→调度 | 抽出查询数据准备与生命周期维护 | 先保留所有车辆提交顺序；二阶段计划/提交需用户确认 |

CityFlow没有本项目同样的居民/家庭/岗位、货物库存守恒、医疗消防、死亡服务、步行横道和实时道路施工机制；其影子实体统计也不等于活动机动车数。不能搬入其发车节奏、随机priority、加速度/转弯优先/死锁解脱策略或活动数量指标。源码看到了成熟组织方式，也看到了局部线性查询/容器拷贝/TODO，学习而非逐字翻译。

## 6. 分阶段实施路线与验证

最新归因修正见 [整步计划](TrafficWholeStepPlan.md)：共享打包道路键的默认哈希严重聚集，主导此前分类/车道/连通热点；隔离比较器原型fixed42.711→7.449ms、3x达到约3.00，默认尚未启用。先修复共享查询复杂度，而不是照抄CityFlow阶段化、或按旧21ms/5ms预算堆叠缓存；下文保留结构方向，实际顺序以新计划为准。

以下列出结构依赖方向；实际优先级按每批最新Profiler重排。本轮配对后先继续归因最大的ChooseLane/LimitCrossingAdvance Self及RouteValidation，不因为已有桶就立即推广所有局部查询。

1. **有向边局部跟车索引（本轮）**：固定步Movement开始时O(N+B)重建复用桶，桶引用原trip；仍逐车Move，跨边后立即刷新成员，增删任务同步；查询外层/车道/距离算法不动。先不排序或拆成Lane，避免引入其他语义。Following的Min归约对候选顺序无关，不能把这一性质推广到路口先后判定。
2. **车道邻居/入口查询**：逐个替换Headway、Space、EntryLane、ExitClear；增加节点incoming与双车道成员，明确跨短边窗口。先用扫描oracle比较每次结果再配对基准。有序车道/Segment只有在维护成本实测更划算时引入。
3. **路口局部集合与静态连接**：按节点/入向维护车/行人邻域，保留完整信号、先到、出口和物理预约。再单独缓存静态movement路径/冲突，控制配对表内存与改路冷启动。
4. **路线与静态决策复用**：事件/版本有效性缓存、节点分类与距离数据，保持时钟和重试频率；解决最大的剩余Self需进一步归因，不先将所有父项解释成全局扫描。
5. **阶段化/并行（需另行确认）**：只读快照、意向Buffer、确定性归并与串行预约，分析与当前先处理车可见状态的差异。未确认前不双缓冲重排车辆，不引入Jobs/ECS/GPU。

每阶段独立提交、原生固定存档控制开关A/B、同种预热/步数/机器与诊断配置。校验每步任务和最终JSON（累积器比较归一保持原协议）、主要Calls、服务/货物守恒、道路变更、行人/换道/预约、存档与长期运行；性能报告含完整步及目标节点/索引维护成本、P95/P99、GC和实际1x/3x推进。

扩展性验证分清：增加独立路段而保持局部密度的车流，和同一局部桶拥堵。首版候选代价约O(N+B+ΣkK)，不是所有场景O(N)，集中到同一边仍可接近原最坏情况。100/1000/3000任务的候选量与oracle测试只证明查询缩减和一致性，不等于3000辆完整城市实时模拟达标。

本轮索引不作为实体真相、不进存档，不改变渲染或经济。控制编译符号可回退全扫描；Movement外的查询/修改继续以State.trips为准，下一步重建接纳最新状态。移除同步只集中原Remove调用，不改变任务失败、工资、服务或库存逻辑。

已完成第一批及原生配对，见 [局部跟车实现与实测](TrafficFollowingIndex.md)：Following 3.166→2.441ms/步，完整步43.479→42.550ms（约2.14%），索引维护约0.064ms/步，GC基本不变。2700步任务/9个原生状态/既有Calls一致，340步完整状态重放、4112专项和全量模型及26日原生运行通过。3x实际仍仅约1.173模拟秒/真实秒；不宣称已解决卡顿或支持3000辆实时城市。
