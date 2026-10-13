# 道路拓扑与几何分离：第一阶段设计

后续状态：用户已授权并开始第二、第三阶段的分批迁移，正常游戏已接入部分新表示；本文保留第一阶段设计语境，最新调用/风险与剩余迁移见 [RoadTopologyMigration](RoadTopologyMigration.md)。

2026-10-13，基线262adb0及当前工作树。只建立只读派生表示，不迁移运行中的寻路、车辆或路口算法。道路键混合哈希继续默认启用。以下设计由实际源码推导；不移植CityFlow引擎。

## 现状与依赖

| 所有者/模块 | 当前职责与调用 |
| --- | --- |
| CityRoads / RoadNode / RoadEdge | 唯一持久道路数据。节点带xyz、控制和stopFrom；边带a/b、方向、kind、structure、stroke和稳定边ID。Cache生成邻接、可通行对、属性查表；三个long键容器使用CityRoadKeyComparer。 |
| PlanSegment / PlanCurve / PlanTerrainSpan | 直线按约3单位细分，曲线先密集采样再按约2单位分段调用PlanSegment；每段复制整个图进行施工验证。高度净空决定交叉是否形成连接，同层交叉插入节点并替换被交叉短边。桥隧structure沿采样段存储。 |
| RoadPlan / RoadSplit / CommitRoad | Plan是待提交图副本、版本、费用和旧短边拆分列表；Commit替换CityRoads实例，ApplySplits插入车辆route节点并按空间距离重定位segment/progress。不能只比较revision忽略实例更换。 |
| Run / UpgradeRoad / ChangeRoadDirection / RemoveRoads | Run按degree、stroke/直线角度取施工范围；升级/反向保持节点但使缓存失效，部分在车道上有车时拒绝；拆除留存旧节点，撤销使用施工笔画。施工范围不是路口到路口的逻辑Road。 |
| Buildings / Zoning / ResidentTransport | FrontageEdge选地面可临街短边，入口选其较近端点并按道路实例+revision缓存。居民accessNode、分区key、服务任务均依赖旧节点/边。入口可能位于逻辑道路内部，不能把它挪到最近路口。 |
| Traffic / Lanes / Junctions / HighwayTraffic / TrafficIndex | route是节点ID列表，segment/progress定位短边；RoadLane属于有向短边。Move在0.05秒内携带剩余时间跨短边、即时更新状态；跟车索引按有向节点对。IsJunction除degree>2外还把合法通行中的车道收缩识别为路口。预约/信号/stopFrom是节点/接近臂语义，不能替换为CityFlow冲突模型。 |
| VehicleGeometry / RoadView | 车辆车道偏移后在每个采样节点附近做二次曲线平滑；RoadView沿短边生成Strip、节点Disk、支撑及标线。SurfaceHeight共用旧节点纵断面并叠加地形横坡。车辆实际平滑轨迹、3D中心线长度、路面顶点并非同一条曲线，现有近似必须明确。 |
| CitySaveData / tests | 格式23–34迁移保持旧ID、路线和状态。CityRoads.Valid也会重建旧查表。新表示不能成为公共可序列化property，否则JavaScriptSerializer可能把派生缓存写入存档。原生Unity与纯模型测试分别验证。 |

没有“这是采样点”的持久标志，不能只依据ID、笔画或间距猜测。可压缩节点必须degree=2、两边道路类型/方向可连续、无控制、非城外入口；曲率或坡度自身不产生交通分支。degree≥3是真分支，degree1是尽头；degree2的类型/车道/方向变化是拓扑边界，但不等于信号灯路口。孤立历史节点不删除；立交只按实际共享节点连接，绝不按XZ接近合并。

## CityFlow源码依据与差异

沿用已经阅读的固定版本81ee0f47659ca66177a71f81676691c58ee89184，本轮重新核对所有权与生命周期：

- [roadnet.h，Intersection/Road/Drivable/RoadNet](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/roadnet/roadnet.h#L68)：RoadNet拥有Road和Intersection容器，ID映射及Lane/Drivable列表是指针视图；Road拥有有向起终路口、Lane和多点中心线，几何点不是路口。Intersection拥有RoadLink/Cross，LaneLink连接车道且也是Drivable。不能直接采用C++容器地址作为本项目动态编辑后的稳定标识。
- [roadnet.cpp，loadFromJson、initLanesPoints、reset、connectedToRoad](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/roadnet/roadnet.cpp#L42)：加载先固定容器和指针关系、再建立车道/路口几何；reset清模拟状态而非实时施工迁移。Lane中心线由道路点偏移、长度累加，Road::getLength是各Lane长度之和，averageLength才取平均。不得把getLength直接当Harbor的道路中心线长度。CityFlow点是二维，不能直接处理这里的桥隧高程。
- [vehicle.h ControllerInfo](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/vehicle/vehicle.h#L88) 与 [vehicle.cpp setDeltaDistance/update/getPoint](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/vehicle/vehicle.cpp#L49)：车辆保存当前Drivable和距离、状态Buffer，沿几何点取位置。跨采样点不是换Road。Harbor目前的即时逐车提交及拐角平滑必须先保留。
- [router.h](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/vehicle/router.h#L19) / [router.cpp getNextDrivable/update/dijkstra](https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/vehicle/router.cpp#L43)：route由Road指针构成，planned是派生的Drivable序列，前進LaneLink/Lane时更新游标；road可达性还依赖实际LaneLink。只学所有权/层级，不把本阶段端点邻接冒充已完成车道连接。此前阶段更新/线程/换道分析见[CityFlowTrafficDesign](CityFlowTrafficDesign.md)。

## 第一阶段目标结构

唯一道路真相仍为旧CityRoads。`GetLogicalTopology()`显式、惰性构建只读快照，正常游戏没有调用它。独立文件负责几何、逻辑拓扑、构建，不扩张Traffic partial。

| 新结构 | 内容 |
| --- | --- |
| CityRoadTopology | 来源实例+revision、逻辑道路列表、边ID/有向节点对到道路采样段的映射、端点和出入有向连接、节点角色。不是另一个可编辑/可存档道路数据库。 |
| LogicalRoad | 一条属性连续的物理走廊，含端点、道路类型、车道数、允许正/反方向、只读Geometry。双向路用一个物理Road加两条有向连接；不同于CityFlow两个有向Road。 |
| RoadCenterline / samples / spans | 值类型xyz采样和双精度累计3D距离；每个span保留旧edgeId、stroke、structure、原a/b方向。弯曲和高度保留全部点，不用端点直线替代。桥/隧/地面可作为同一路的逐段结构剖面，无交通属性改变则不强行拆Road。 |
| LogicalRoadPosition | 快照内Road、采样段、正反方向与累计距离；与旧from/to/progress双向无分配映射，可查回旧边。这是中心线参数适配，不修改车辆或把车道平滑位置换为中心线。 |

分段边界：入口、非degree2、显式控制/停车牌、kind/车道或双单向变化、单向在节点不能头尾相接。不同施工stroke不切交通Road，仍逐span保留。曲率/坡度/structure变化不单独切交通Road；它们在几何层。纯环路没有自然端点，确定性选择最小节点为循环锚点，仍保留全环及合法方向；锚点不是新增交通路口。

构建按节点/边ID确定性遍历最大连续链，覆盖每条旧边恰好一次；所有集合只读、复制值，不借用可变RoadNode。构建O(V+E)加确定性排序O((V+E)log(V+E))，不递归长链。查询缓存O(1)，距离定位二分O(log采样数)。有向映射沿用混合long哈希，不重新引入XOR聚集。

## ID、施工与存档

第一阶段RoadId=链内最小旧edgeId，**仅快照范围内的确定性ID，不承诺拆分后的持久身份**。不改图、乱序输入或保存/加载后分段不变，ID相同；中途交叉、删除或方向/类型编辑可能使链拆合，此时旧快照失效，必须通过旧边/拆分沿革重新定位。不得把此阶段RoadId写入车辆存档或跨revision使用。

Changed清除派生引用；Copy不复制缓存；CommitRoad换实例导致旧快照IsCurrent=false，即使revision相同也不能复用。已有快照保留复制值便于对照，但不再是当前数据。任何直接修改公开nodes/edges后仍必须调用Changed，这是既有契约，不新增每帧全图校验。控制变化统一沿用revision保守重建；未来才拆几何/控制版本。

玩家修交叉口：本阶段仍执行Plan副本、RoadSplit和ApplySplits。新图下一次查询重新归并，新分支将原走廊切成两条或更多；车辆沿旧ApplySplits保留实际距离，新表示只读取已迁移的旧边/progress，绝不自行再迁一次。第一阶段不增SaveFormat，旧存档直接派生新快照、现有城市不丢失。

未来持久Road实体需要专用ID分配、分裂/合并映射（旧RoadId+距离区间→新RoadId+距离）、建筑入口Road+offset、revision迁移以及原子提交。应保留已占用路口movement及车道索引、准确映射双车道占用。拆掉车辆当前路面仍按现规则等待道路恢复，不传送/删车。这个持久身份和新存档版本属于第二阶段，不能现在暗中实施。

## 迁移顺序与回退

1. 本批：只读表示、完整映射/长度/方向/循环/立交测试，主动查询前后序列化和模拟状态对照；正常运行不接入新表示。
2. 待确认：先迁移静态拓扑/连接查询一个模块，旧算法作oracle；随后只迁移寻路并处理内部建筑入口虚拟端点、原成本累加和等价路径tie-break。压缩后的求和顺序可能改变float与相同成本路径，不能预先保证逐步一致。
3. 待确认：持久Road ID与道路编辑事务、路线适配；之后车辆位置、车道局部索引逐项迁移，保留步长、碰撞和预约算法。中心线距离不等于当前车道/人行道距离，必须单独验证，不能直接套CityFlow时间语义。
4. 第三阶段另行确认：RoadView消费相同中心线及横坡profile，几何细分独立于拓扑；施工改为局部修改集，保持Plan碰撞/吸附/费用与Undo原子性。原Mesh在本批完全不变。

回退本阶段只需停止调用GetLogicalTopology或回退该提交，旧存档/图/交通无迁移。旧道路哈希回退独立保留，不以旧XOR比较器作为本阶段性能基线。

## 验证门槛和风险

- 旧边全覆盖/唯一映射、方向和端点可达性oracle、全曲线3D长度/高度、入口中间位置、独立上/下层交叉、环路和大稀疏ID。
- 动态分裂/拆建/升级/反向/控制/Copy/读档：快照失效和新旧映射；已有车辆route/progress/lane/wait/cargo、车辆平滑位置不因派生查询改变；沿用原ApplySplits验证实际施工。
- 340固定步完整状态主动派生/控制对照；全量模型、Unity普通Player既有建造/交通/服务/存档/长期回归。原Mesh与车辆位置仍由旧代码生成，只检验派生查询不会改变其输出，不宣称已统一几何算法。
- 同一固定存档/运行配置重新配对完整模拟步/GC与真实窗口1×/3×FPS、实际推进；另记录惰性构建、热查询、保留内存、施工Plan预览前后成本。第一阶段不用于每帧，因此不应增加交通热路径工作；不把缩短Road数量当已实现的模拟加速。
- 高风险仍在下一阶段：路口语义/预约、车道收缩、入口偏移、float/tie-break、中心线与平滑轨迹差异、持久身份、几何重采样后的投影、编辑事务及旧存档迁移。每个子系统独立配对后才继续。

结果另见[第一阶段实现与验收](RoadTopologyStage1.md)。未验证或无法逐步等价项必须在结果中标明，不能用编译成功替代实际交通验收。
