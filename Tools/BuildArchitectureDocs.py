"""Reproducible offline architecture document. No game source mutations.
Source links/excerpts are pinned to the reviewed commit, not moving worktree lines.
Run: python Tools/BuildArchitectureDocs.py
"""
from pathlib import Path
import subprocess, json, html, re, csv

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Docs/Architecture'
COMMIT = 'c40bde5'
BASELINE = '110c4eaa24fa69d55b08a5db2a6d19ec6da5dd7f'
R = 'Assets/Scripts/Runtime/'
files = {
 'host': R+'Presentation/Game/HarborCityRoadCoreGame.cs',
 'city': R+'Simulation/Core/CityModel.cs',
 'buildings': R+'Simulation/Core/CityBuildings.cs',
 'households': R+'Simulation/Residents/CityHouseholds.cs',
 'residents': R+'Simulation/Residents/CityResidents.cs',
 'jobs': R+'Simulation/Residents/CityJobs.cs',
 'industry': R+'Simulation/Industry/CityIndustry.cs',
 'session': R+'Simulation/Industry/CityCoreIndustrySession.cs',
 'people': R+'Simulation/Residents/CityCoreResidents.cs',
 'bindings': R+'Simulation/Core/CityCoreRoadBindings.cs',
 'network': R+'Infrastructure/Roads/CityRoadCoreNetwork.cs',
 'access': R+'Infrastructure/Roads/CityRoadCoreAccess.cs',
 'world': R+'Traffic/CityRoadCoreWorld.cs',
 'business': R+'Traffic/CityRoadCoreBusiness.cs',
 'changes': R+'Traffic/CityRoadCoreLaneChanges.cs',
 'reservations': R+'Traffic/CityRoadCoreReservations.cs',
 'spatial': R+'Infrastructure/Roads/CityRoadCoreSpatial.cs',
 'dispatch': R+'Traffic/CityRoadCoreDispatchRoutes.cs',
 'edit': R+'Infrastructure/Roads/CityRoadCoreEdit.cs',
 'construction': R+'Infrastructure/Roads/CityRoadCoreConstruction.cs',
 'save': R+'Simulation/Core/CityCoreSave.cs',
 'codec': R+'Infrastructure/Roads/CityRoadCoreSave.cs',
 'legacy': R+'Traffic/CityTraffic.cs',
 'development': R+'Simulation/Growth/CityDevelopment.cs',
 'utilities': R+'Infrastructure/Utilities/CityUtilities.cs',
 'waste': R+'Simulation/Services/CityWaste.cs',
 'zoning': R+'Infrastructure/Zoning/CityZoning.cs',
 'map': R+'Infrastructure/Map/CityMap.cs',
 'analysis': R+'Observability/CityDailyAnalysis.cs',
 'loans': R+'Simulation/Economy/CityLoans.cs',
 'fire': R+'Simulation/Services/CityFire.cs',
 'police': R+'Simulation/Services/CityPolice.cs',
 'healthcare': R+'Simulation/Services/CityHealthcare.cs',
 'simulation': R+'Simulation/Core/CitySimulation.cs',
 'clock': R+'Simulation/Core/CitySimulationClock.cs',
 'entities': R+'Simulation/Core/CityEntities.cs',
 'production': R+'Simulation/Industry/CityProductionSystem.cs',
 'logistics': R+'Simulation/Industry/CityLogisticsSystem.cs',
 'boundary': R+'Traffic/CityTransportBoundary.cs',
 'trip': R+'Traffic/CityTripTask.cs',
 'position': R+'Traffic/CityVehiclePosition.cs',
 'ports': R+'Infrastructure/Roads/CityLaneConnection.cs',
 'candidates': R+'Simulation/Industry/CityFreightCandidates.cs',
 'reachability': R+'Infrastructure/Roads/CityLaneReachability.cs',
}
sources = {key: subprocess.check_output(['git','show',f'{COMMIT}:{path}'],cwd=ROOT).decode('utf-8-sig') for key,path in files.items()}

def ref(key, token, limit=14):
    lines=sources[key].splitlines()
    matches=[i for i,s in enumerate(lines) if token in s]
    if not matches: raise ValueError((key,token))
    i=matches[0]
    return {'file':files[key],'line':i+1,'token':token,'url':f'https://github.com/kukushu/game/blob/{COMMIT}/{files[key]}#L{i+1}', 'excerpt':'\n'.join(lines[i:i+limit])}

# Cards answer concept, responsibility, ownership, facts, derivations, writers,
# lifecycle, dependencies, scalability and a concrete replacement decision.
cards=[]
def card(id,name,module,kind,key,token,fields,methods,owner,facts,derived,write,life,deps,risk,decision):
    cards.append(dict(id=id,name=name,module=module,kind=kind,fields=fields,methods=methods,owner=owner,facts=facts,derived=derived,write=write,life=life,deps=deps,risk=risk,decision=decision,source=ref(key,token)))
card('session','CityCoreIndustrySession','Simulation / 当前组装器','temporary','session','public sealed class CityCoreIndustrySession','City / Transport / Residents / Business / dispatchAt / calendarOffset','Advance · BeforeStep · SyncCalendar · Create · Validate','Unity Host持有Session；Session组装原城与新交通。','dispatchAt与calendarOffset影响运行，工资日游标在Residents。','组装关系、日历投影；不是第二份工厂库存。','BeforeStep写劳动/生产/调度，SyncCalendar写City日历，CloseBuilding删建筑。','创建默认小城或读档重新组装；retired保留关闭企业。','CityModel、World、居民/入口桥接、业务回调。','继续塞服务会重现总控类；每步成员扫描、每.25秒F×B商店枚举。','拆为纯组装根+CitySimulation与领域系统，不继续扩它。')
card('city','CityModel','城市领域 / legacy partial','fact','city','public sealed partial class CityModel','buildings / society / roads / traffic / money / day / utilities / development','GetBuilding · Tick · Recalculate · Valid','Session持有原城；原完整城入口也持有同类。','建筑/居民/岗位/账户/城市设置；当前旧道路交通为空。','population/jobs/income等NonSerialized统计，建筑索引。','大量partial+公开集合+桥接器均可写，缺领域写权限。','构造初始化很多领域；旧Valid要求旧交通关系，不能验证新完整城。','所有城市业务和旧道路/交通，职责过宽。','不是单纯数据；同数量替换可漏索引失效；新存档仅保存部分领域。','变为CityState事实根+领域Store/System；复用规则，淘汰partial总控。')
card('world','World','Traffic / 当前综合内核','fact','world','public sealed class World','data / Network / occupancy / reservations / dispatchRoutes / vehiclesById','Step · Advance · Dispatch · PlanUpgradeRoad · TryCommit · Snapshot','Session.Transport唯一持有实例；构造复制WorldData。','道路事实、车辆、入口、clock/accumulator、ID分配。','车道索引、预约、意图、空间索引和蓝图。','World独占常规状态修改；业务回调同时修改城市实体。','车辆派发→活动/中断→业务成功后退休；编辑整批交换。','Network、业务接口、施工、派生占用/缓存。','承担交通之外的时钟/路网/结算；不是最终TrafficSimulation。','拆道路编辑与城市时钟，交通只推进物理并报告结果。')
card('data','WorldData','Save / 当前核心DTO','fact','world','public sealed class WorldData','coreFormat=9 / clock / accumulator / networkRevision / network / vehicles / accesses','由World.Snapshot与SaveCodec读写','World复制入参；调用者的DTO不是活事实。','交通运行状态、道路/入口、单调ID；合成depots受externalBusiness限制。','此DTO不含实时桶、意图、空间网格。','活数据仅World；外部可以编辑自己的冷快照。','创建/保存/读档候选；复制后重新Validate。','NetworkData、Vehicle、RoadAccess、测试Depot。','城市保存根分散，不含市政/地图全事实。','显式CitySnapshot包含领域DTO，WorldData作为过渡。')
card('road','PhysicalRoad','Roads / 物理道路','fact','network','public sealed class PhysicalRoad','id / from / to / direction / lanes / speed / points / rightVectors / tangents','Network.Rebuild · World.SplitRoadAt / PlanUpgradeRoad','World私有NetworkData包含道路；渲染没有副本。','物理端点和方向，采样中心线、截面/切线。lanes是每方向车道数。','Lane几何、累计长度、连接/冲突。','只经候选事务改活网络；DTO字段本身public，公开构造要控制别名。','建路分配ID；拆分保留前段ID、新后段ID；删路迁移引用。','Junction、Geometry派生规则与编辑协调。','取值lanes1..8；路型/专用车道定义不足，端口尚未完成。','保留物理路+共享几何；添加明确车道配置/端口，避免恢复短边。')
card('junction','Junction','Roads / 拓扑地点','fact','network','public sealed class Junction','id / position XYZ','AddJunction · SplitRoadAt · RoadConstruction.Connect','道路事实集合。','真实连接地点或道路端点，不是每个曲线点。','进出口列表、是否冲突、端口与允许移动。','RoadEditPlan提交候选。','吸附/同高交叉创建；上下层不连；移除要处理关联道路。','PhysicalRoad端点和派生LaneConnection。','Junction并非某辆车的转弯轨迹；端口尺度简化。','保留稳定ID；规则/端口分离成派生或显式配置。')
card('network','Network','Roads / 只读派生网络','derived','network','public sealed class Network','drivables / outgoing / conflictJunctions / connectionConflicts / spatialIndex','Rebuild · Route · Project · Get · ValidateRoute','World.Network按事务替换；Network保留输入NetworkData引用。','不新增第二套可编辑道路事实。','有向车道/连接、邻接、冲突/净距、惰性空间网格。','构造Rebuild；查询不改道路，但派生缓存可初始化。','每次网络替换全重建；读档重建。','PhysicalRoad/Junction、Geometry、哈希比较器。','构造传入可写DTO会破坏边界；全量冲突冷成本依路口度数增长。','只读RoadNetwork快照+显式派生版本；限制可写DTO所有权。')
card('geometry','Geometry','Roads / 共享曲线','derived','network','public sealed class Geometry','readonly points / cumulative / Length','Sample · MapDistance · MapReverseDistance','Network各Drivable拥有派生Geometry；物理中心线属于Road事实。','中心线和截面从PhysicalRoad派生，当前Geometry不独立编辑。','长度表与车道曲线；读档重建。','构造复制输入数组，查询返回值Point。','网络构建生成，视图直接读取；网络更换失效。','Point数组，无Unity依赖。','二分采样O(log S)，按车道复制点会增加存储；没有全城峰值内存基准。','保留共享模拟几何；端口改变必须统一模拟/渲染。')
card('lane','Drivable（Lane）','Roads → Traffic / 有向路面','derived','network','public abstract class Drivable','Id / RoadId / From / To / Reverse / LaneIndex / Geometry / Speed','IsConnection=false · Network.Get · LaneId','Network派生；占用属于交通，不属于物理道路对象。','当前没有独立可编辑Lane事实；其身份由RoadID/方向/序号派生。','有向几何/净距/速度/邻接。','Network构建；交通不改Lane静态属性。','Road编辑重建，保持仍存在车道ID，移除方向必须迁移或拒绝。','PhysicalRoad、Geometry；Vehicle用ID引用。','Lane与Connection共用类型；车道身份和位运算不应扩散业务。','分语义Lane/LaneConnection，typed ID封装；保留混合hash。')
card('connection','Drivable（LaneConnection）','Roads / 路口通行路径','derived','network','long id=ConnectionId','负ID / JunctionId / RoadId=0 / Geometry / Speed','ConnectionId · MovementsConflict · ValidateRoute','Network生成；当前无名为LaneConnection的独立类。','允许连接目前从进口/出口车道推导，未保存专用转向配置。','p→路口中心→q短曲线、保护路径、静态冲突图。','Network.Rebuild生成，JunctionReservations写动态预约。','路口/车道变化后重建；活跃连接移除需要安全拒绝或迁移。','进出Lane、Junction与共享Geometry。','中心短曲线不能当真实端口；路口c_j条路径两两静态比较。','建立真实端口和LaneConnection；规则/冲突共享同一几何。')
card('vehicle','Vehicle','Traffic / 当前车与任务','fact','world','public sealed class Vehicle','id / purpose / citizenId / origin / destination / cargo / route / cursor / distance / changeTarget','Current · World.Position · World.Step · LaneChanges.Route','World车辆集合与vehiclesById；业务有只读观察关系。','车身中心沿有向Geometry从起点的distance；换道target+elapsed；中断XYZ。','Current=route[cursor]、影子位置、Transform不是独立事实。','World移动/换道，Depart/Deliver回调更新业务事实。','成功发车创建（一次出行一车）；Deliver成功后当步压缩退休；Stranded仍在途。','Network/Lane/Connection、任务端点与CityResident.tripId。','路线改动即可能改变Current；任务=车不适合多站服务/公交。','独立VehiclePosition/RoutePlan；任务与货物转归领域Store，保持实际车数量。')
card('access','RoadAccess','Roads ↔ Building / 接入关系','fact','access','public sealed class RoadAccess','endpoint / lane / distance / use / valid / preferredIndex','RegisterAccess · RebindAccesses · AccessRouting','World保存关系，原建筑保存门口坐标。','哪栋建筑可使用哪条有向车道的哪个站位。','对向入口按同一物理路采样对应派生；未创建额外Junction。','道路事务或入口器批量重绑。','建筑注册/道路拆分迁移/删除失效/原位重建恢复。','Building ID、Lane ID和权限；非存房子坐标。','Y=0入口器不能承载原Terrain；有隔离带/对街访问规则待定义。','入口位置由建筑唯一持有，Access用明确地表/driveway关联。')
card('building','CityBuilding','Buildings / 城市实体','fact','buildings','public abstract class CityBuilding','id / x,z / entranceX,Z / yaw / level / abandoned / maintenance状态','GetBuilding · CanBuild · DemolishBuilding','CityModel.buildings；多种具体建筑派生。','稳定ID、世界位置、用途和状态；住宅/工厂/商业具体事实。','建筑查询/入口/供给/问题展示缓存。','原CityModel partial与Session.CloseBuilding。','建造/分区生长→升级/问题→拆除或retired；当前迁移场景手动初始化。','家庭/岗位/库存/市政/道路入口关系。','public集合不统一通知；同Count替换会留下buildingLookup旧实体。','BuildingStore唯一增删/改型，索引/关系同事务失效。')
card('household','Household','Population / 家庭','fact','households','public sealed class Household','id / home / unit / savings / rent / people / preferences / wagePaid','HouseholdTick · Occupancy · Review','society.families包含家庭，people嵌套存居民。','家庭成员、住房building+unit、储蓄/合同；不存单一工作。','评估options与汇总不是居民收入事实；迁移入口不跑旧Review。','原家庭/住房函数，当前居民桥接工资转储蓄。','迁入/搬家/迁出/存档；当前无增长动态接入。','CityResident、ResidentialBuilding、CityJob评估。','嵌套所有权令跨家庭迁移/人口索引维护复杂。','独立PopulationStore+家庭成员IDs原子关系；不扩原创性格。')
card('resident','CityResident','Population / 个人','fact','residents','public sealed class CityResident','id / householdId / skill / jobId / location / tripId / atWork / factoryWageCredit / workedMinutes','CityCoreResidents.AdvanceRequests · Depart · Arrive · Observe','当前家庭people包含个人，CityCoreResidents.members保存引用。','个人劳动能力/岗位/实际工资出勤与出行关联。','观察坐标来自Vehicle或Building；commute观察上下文含revision。','居民系统与原工资函数；到达通过业务回调。','初始化居民；在家→实际车→到岗→返家；生死增长逻辑原代码未迁移。','家庭、岗位、建筑、交通任务ID。','成员索引仅构造；每步遍历居民且岗位Find；服务字段集中。','实体表生命周期+活动调度；活动/健康数据按需要分组件而非马上ECS。')
card('job','CityJob','Employment / 岗位','fact','jobs','public sealed class CityJob','id / buildingId / wage / requiredSkill / occupiedCitizenId','SyncJobs · AssignJob · ReleaseJob · Job','society.jobEntities，岗位系统写。','一个可占用的真实岗位，非建筑容量宏观数字。','就业/空岗统计从占用汇总。','AssignJob/ReleaseJob维护两端；建筑容量变化触发SyncJobs。','建筑→有限岗位→居民占用→释放/删除。','Building、Resident反向引用。','Job用List.Find O(J)，每步收入/资格反复查询；双向必须原子。','JobStore ID索引+building岗位索引，岗位关系事务。')
card('factory','FactoryState / Stock','Industry / 库存与账户','fact','industry','public sealed class FactoryState','raw / progress / processing / cash / wageCosts / salesRevenue / produced / noise / pollution','ProcessFactoryWork · PayAttendance · AdvanceIndustry · RecordFactorySale','IndustrialBuilding.factory；成品Stock在IndustrialBuilding。','真实原料/加工进度/成品、财务信用、衰减环境历史。','status字符串为展示推导，stepProductive为步内工作量。','原CityModel生产/工资与IndustryBusiness销售回调。','真实到岗且有料/空间/钱才能生产；发货/交付真实转移。','个人到岗、Job、Building、Shipment（目前Vehicle货物）。','消费者/原料进口未接；旧环境重车暴露读取旧traffic。','保留实际加工和守恒；ProductionSystem/InventoryStore拥有写入。')
card('task','TransportTask（尚无独立类）','Logistics / 当前内嵌Vehicle','temporary','world','public enum TransportPurpose','Vehicle.purpose / origin / destination / cargo / citizenId','Dispatch · DispatchResident · business.Depart / Deliver','当前World生命周期；经济保留收据ID。','任务与车混合；cargo从工厂扣除后归在途。','调度incoming按活动车辆每.25秒重算。','Session发需求，World准入，业务确认发/到。','发车即任务创建；到达即车退休；中断没有独立任务状态。','Resident/Factory/Commercial与Vehicle。','没有独立等待任务/预留货量，难扩公交、服务多站或转运。','最小TripTask与Shipment，Traffic只接请求/返回ArrivalResult。')
card('people','CityCoreResidents','Residents / 当前桥接系统','temporary','people','public sealed class CityCoreResidents','members / city / transport / PayrollDay / Enabled','AdvanceRequests · Depart · Arrive · Validate','Session组装；成员索引构造一次。','工资游标需存；居民事实仍在原实体。','Member查找字典，不是第二份Resident。','请求派车、到达确认、工资转入储蓄。','每步运行；读档重建索引；动态新增尚未接。','CityModel、World、Job、家庭。','工资/日历/出行与交通回调耦合；单次字典建立漏动态生命周期。','ResidentActivitySystem+due队列，显式到达/工资阶段。')
card('business','CityCoreIndustryBusiness','Economy ↔ Traffic / 当前回调','temporary','session','public sealed class CityCoreIndustryBusiness','city / receipts / Residents','Exists · Depart · Deliver','Session建立并给World；城市库存唯一。','receipts保证重复交付不重复入账，保存；无第二库存。','Exists为端点可用性查询。','Depart扣库存或改变人状态；Deliver收货/钱或到岗。','同步被World调用；完成收据永不清理。','CityModel、FactoryState、Commercial、Residents。','业务在物理提交中写，reciepts无限增长；商店是外部资金近似。','分ArrivalResult与业务提交协议，账本退休/审计有界。')
card('save','CityCoreSave + SaveCodec','Save / 当前协议','temporary','save','public static class CityCoreSave','城市magic/version6；核心9；字段反射缓存；SHA256','Write · Read · CopyBusiness','Host写tmp/bak文件，codec只编解码候选。','城市业务切片+World事实；用户旧save不删除。','字段描述缓存、临时字节/重建索引不存。','冷序列化；加载先构造并Validate新Session。','格式不支持明确拒绝，不静默加载。','Session DTO、原BuildingSaveData与核心codec。','反射布局即协议；没完整服务/地图；多重快照内存。','显式CitySnapshot、完整跨域校验、原子活城替换。')

card('simulation','CitySimulation','Simulation / R1已实现','fact','simulation','public sealed class CitySimulation','clock / production / Logistics / residents / transport / Fault','Advance · Step(TimeContext) · RequestCommute · RequestFreight','Session组装；CitySimulation唯一推进城市时间。','唯一tick/欠步量；出错Fault禁止推进/保存半步骤。','日历按offset投影，无第二个city时钟。','按明确阶段驱动领域；交通结束后结算、确认、退休。','创建/新存档恢复；正常World.Advance/Step拒绝旁路。','居民/生产/物流/World/业务结算，尚不是全城Services总控。','仍全额追赶；不是数据库级步骤回滚。','保持阶段边界；R2先复评任务/位置/提交协议，非继续堆玩法。')
card('clock','SimulationClock / TimeContext','Simulation / R1已实现','fact','clock','public sealed class SimulationClock','tick long / pendingSeconds / .05秒 / TimeContext.Start,End','Request · Commit · Snapshot','正常城CitySimulation拥有，World只引用；隔离夹具拥有独立时钟。','整数tick和未执行时间请求；正常城只在城市DTO保存。','Seconds=tick*.05，步区间由tick派生。','只有调度器调用internal推进，正常traffic不能推进。','暂停输入0不推进；读档恢复欠步量，1×/3×请求分组一致。','无Unity依赖；交通读取TimeContext。','没有追赶工作预算器，长期跨平台浮点确定性未证明。','保持唯一时钟；以后预算器保留债务并报告实际推进，不能丢步。')
card('entities','CityEntities / CityMember','Entities / R1已实现','derived','entities','public sealed class CityEntities','building/person/family/job字典 / Members / Factories / Shops / Revision','Add/Replace/Remove · MoveResident · SetAvailability · Validate','Session拥有，索引引用原城的同一事实对象。','不复制库存/人；领域结构变更由此唯一支持入口维护。','ID索引与typed只读视图；读档从事实重建。','结构API只在健康边界；SyncJobs冷更新岗位索引。','新增/移除/换家庭立即维护；在途或有未领工资居民删除被拒。','CityModel事实、World派生端点可用性集合。','旧public列表尚未类型级封锁；Validate拒绝绕过编辑，未每步扫描校验。','后续收紧事实容器；不要再建可写复制模型。')
card('production','CityProductionSystem','Industry / R1已实现','temporary','production','public sealed class CityProductionSystem','city / entities','Step(TimeContext)','CitySimulation拥有生产系统，原FactoryState仍唯一账户/库存。','只实际在岗且有效工作、付薪、原料和空间可加工。','在岗资格/工作区间；没有新增库存副本。','原PayAttendance/ProcessFactoryWork写原事实。','每步运行，步末到岗最早下一步劳动。','CityEntities/原Job与Factory规则。','仍O(P)遍历；新货车环境暴露未接。','R2/R4复评在岗集合与环境积分，不能仅换接口名。')
card('logistics','CityLogisticsSystem','Logistics / R1已实现','temporary','logistics','public sealed class CityLogisticsSystem','NextDispatchTick / reusable incoming / typed工厂商店视图','Step · CitySimulation.RequestFreight','CitySimulation组装；业务库存不在系统内复制。','下一次调度tick保存；货物仍在Vehicle（R2待拆）。','incoming每调度扫描活车，字典复用。','生成需求并经城市准入/业务确认创建实车。','每5tick调度；准入失败不扣货。','实体视图/World只读任务/CitySimulation。','明确O(V+F*S)；规模P99/GC仍高，不是增量需求系统。','先归因集中派车与路线准备，再考虑需求/预留事件索引。')
updates={
 'session':dict(fields='City / Transport / Simulation / Entities / Residents / Business',methods='Advance → Simulation.Advance · Create · Validate · SnapshotBusiness',facts='保存/组装原业务和交通；不再持独立dispatchAt时钟。',write='已删除BeforeStep/after委托；生产/调度在独立系统。',risk='原退休企业记录仍长寿命；旧CityModel事实容器未拆。',decision='保留组装根，禁止在Session重新堆每步规则。'),
 'world':dict(fields='Network / occupancy / reservations / arrivals / timeline只读引用',methods='MoveStep · PrepareTransport · CommitAdmission · AcknowledgeArrival · TryCommit',facts='车辆/路网/入口，正常城clock由CitySimulation持有。',write='正常城无ITransportBusiness回调；只提交物理并产出Arrival。',deps='路网/几何/局部索引/TimeContext；正常运行不查Factory/Household。',risk='仍聚合道路编辑与物理算法；任务/路线/位置仍绑定。'),
 'data':dict(fields='coreFormat=10 / network / vehicles / accesses / standaloneClock仅夹具',facts='道路/车/入口事实；城市外部clock只在城市DTO；端点可用性索引不编码。',derived='端点可用性/占用/空间/冲突读档重建。'),
 'city':dict(derived='当前城GetBuilding/Job使用CityEntities字典，宏观统计仍原缓存。',write='结构修改支持CityEntities；旧public字段仍过渡风险，非已实现只读CityState。',risk='大量partial/public仍需迁出；绕过结构API会在Validate失败。'),
 'building':dict(write='新增/同ID替换/关闭/可用性经CityEntities；直接列表编辑不支持。',risk='Count失效已由当前注册索引消除；未迁移旧入口仍有旧缓存。'),
 'resident':dict(owner='原家庭people保存事实；CityEntities.Member引用同一对象。',write='Residents经城市Request准入，Arrive带stepEnd在物理提交后执行。',risk='动态成员映射已维护；出行仍每步O(P)，尚无due队列。'),
 'job':dict(methods='SyncJobs · AssignJob · ReleaseJob · CityEntities.Job',risk='当前城Job字典O(1)，结构变更冷重建；原public jobEntities尚未类型级封锁。'),
 'people':dict(fields='entities / RequestCommute委托 / PayrollDay / transport查询',owner='Session组装；使用注册的CityEntities.Members视图。',write='AdvanceRequests产生城市请求；Arrive接只读结果，工资在步末日界结算。',risk='固定构造成员缺口已解决；仍每步成员遍历。'),
 'business':dict(name='CityCoreIndustryBusiness（城市边界）',fields='city / Residents；已删除永久receipts',methods='Depart · Deliver(TransportArrival)',owner='CitySimulation调用，不再注入交通引擎。',facts='原库存/账户；成功后同边界ACK与退休，不保存半结算。',risk='无完整商店现金/消费/进口；异常Fault停止而非自动rollback。'),
 'task':dict(derived='CityLogistics复用incoming；Arrival值记录只描述已到达，不是Task事实。',write='城市发车/结算；交通仅准入、移动、结果、确认退休。'),
 'save':dict(fields='城市version7；核心10；唯一clock/tick/debt/NextDispatchTick；SHA256',facts='完整当前切片；端点可用性从Building重建，不存成第二份事实。',risk='反射布局仍临时；管网/分区/市政/地图完整save尚缺。'),
}
for c in cards:
    c.update(updates.get(c['id'],{}))
for c in cards:
    if c['id']=='vehicle':c.update(write='World提交物理位置；CitySimulation在全提交之后确认业务，再ACK退休。',deps='Lane/Connection ID与任务关联；交通不调用居民或库存接口。')
    if c['id']=='business':c.update(life='城市每步在全部物理提交之后处理Arrival；成功ACK后退休，无永久收据集合。',write='只有城市边界调用internal Depart/Deliver，不注入World。')
    if c['id']=='people':c.update(life='CityEntities新增/移除/换家庭立即更新同对象索引；每步请求、步末到达和日界工资。')

# R2-A actual implementation overrides; historical audit descriptions above are
# not the displayed current facts. Physical position/ports remain explicit risks.
for c in cards:
    if c['id']=='task':c.update(name='TripTask',module='Transport / R2-A已实现',kind='fact',source=ref('trip','public sealed class TripTask'),fields='id / endpoints / purpose / citizenId / departedAt / state / Shipment',methods='World.FindTask · FindTaskVehicle · CitySimulation请求/确认',owner='WorldData.tasks独立集合；车辆引用同一对象，反查为派生索引。',facts='任务与货物事实；Task ID独立于Vehicle ID，居民tripId引用Task。',derived='vehiclesByTask / incoming / Arrival值；不另存可写vehicleId。',write='城市Depart(TripTask)扣货/关联居民；ACK完成后同步退休。',life='准入成功发布→InTransit（车可能Stranded）→业务确认→Completed→退休。',deps='Vehicle.Task引用；Shipment拥有在途货量；业务库存仍在Building。',risk='当前一任务一车，无等待/多站；任务准入协调仍共处World。',decision='已接真实通勤与货运；继续独立物理位置，不提前扩公交。')
    if c['id']=='vehicle':c.update(name='Vehicle',module='Traffic / R2-A实际过渡',fields='id / Task引用 / Plan / cursor / distance / changeTarget / status',facts='物理状态与唯一任务关联；已删目的/居民/货量/价格复制字段。',derived='Current=Plan.segments[cursor]；影子/Transform派生。',life='准入后实际行驶/中断；业务确认完成后车与Task同边界退休。',deps='TripTask引用、独立RoutePlan、Lane/Connection及共享Geometry。',risk='物理drivable仍依附Plan/cursor；独立位置是R2-B尚未实施。',decision='下一批独立VehiclePosition与导航进度；不能以RoutePlan类存在宣称完成。')
    if c['id']=='world':c.update(fields='道路/车辆/Tasks独立集合与派生关联索引 / arrivals / occupancy',facts='当前道路/接入、任务及物理车事实；唯一时钟仍在CitySimulation。',risk='仍聚合编辑/准入协调/交通，位置仍依附路线；不是最终边界。')
    if c['id']=='data':c.update(fields='coreFormat=11 / nextTask / tasks / vehicles / network / accesses',facts='独立任务事实与车辆→任务关联；城市时钟只在城市DTO。')
    if c['id']=='logistics':c.update(fields='NextDispatchTick / incoming / CityFreightCandidates',facts='调度游标；货物归TripTask.Shipment，库存仍归Building。',derived='车道/站位商店索引与可复用候选 scratch，结构/入口版本失效。',risk='可达候选多时仍收集排序，最坏O(F*S log S)；未实现due需求。',decision='实测P99下降；随后恢复完整城市并测局部需求，不无限扩夹具。')
    if c['id']=='save':c.update(fields='城市8 / 核心11 / 独立任务与关联 / RoutePlan / SHA256',facts='唯一城市时钟及全部在途任务/货物/实车；索引重建。')
    if c['id']=='resident':c.update(fields='id / householdId / jobId / location / tripId(Task ID) / atWork / 实际工资',deps='家庭/岗位/建筑；Task ID经派生映射定位实车。')
    if c['id']=='factory':c.update(deps='个人到岗、Job、Building、TripTask.Shipment；无重复库存。')
card('routeplan','RoutePlan','Navigation / R2-A已实现','fact','trip','public sealed class RoutePlan','segments / EndDistance','Segments只读视图 · Copy','每车独立拥有Plan；蓝图为派生缓存，不共享可写List。','路径及终点站位；不存居民/货物/几何。','车道和连接Geometry从Network读取。','导航/施工事务替换路径；Vehicle仍持cursor。','准入创建、换道/施工替换、随车退休。','Vehicle、Network、RoadAccess。','Current仍通过Plan/cursor，尚未独立物理位置。','R2-B先设计并迁移位置与导航进度唯一关系。')
card('shipment','Shipment','Freight / R2-A已实现','fact','trip','public sealed class Shipment','remaining / unitPrice','TripTask.Cargo / Price派生只读查询','TripTask唯一拥有，身份由Task ID确定。','扣库存后在途的货物与价格。','incoming预留统计读取任务，不保存第二需求真相。','准入扣原库存；成功交付ACK清remaining。','Freight准入才创建；中断不丢货；交付后随任务退休。','TripTask、原Factory/Commercial库存账户。','无转运/分货/独立货单ID；未引入不必要复杂业务。','现有最小运输够用后先恢复完整经营。')

# R2-B actual facts supersede the R2-A transition notes.
for c in cards:
    if c['id']=='vehicle':c.update(fields='id / Position唯一物理组件 / Task / Plan / cursor / changeTarget',facts='物理Drivable+车身中心距离；冻结位置归同一组件。',derived='Current/distance为Position只读投影，Plan不能决定位置。',risk='World仍兼道路/准入协调；信号/行人和完整城市服务待R3接入。',decision='R2-B必要位置/连接验收已结束，优先恢复可玩R3。')
    if c['id']=='routeplan':c.update(risk='当前段仅作导航一致性锚点，实际位置来自Position；未来任务多站尚未实现。',decision='不继续扩大导航框架，随真实城市需求演进。')
    if c['id']=='lane':c.update(name='Lane',source=ref('ports','public sealed class Lane'),fields='共享Geometry / Entry,Exit端口 / StartDistance,EndDistance',facts='端口/范围由PhysicalRoad与真实路口派生。',risk='不是可单独编辑的第二道路事实；高车道静态构建尚有冷成本。')
    if c['id']=='connection':c.update(name='LaneConnection',source=ref('ports','public sealed class LaneConnection'),fields='Incoming / Outgoing / Entry / Exit / Geometry',facts='真实允许通行关系及进口出口共享曲线。',derived='三次切线曲线；同几何供移动、Mesh、保护路径/冲突表。',risk='信号/行人、重型车辆扫掠尚未迁移/验证，不称完整复杂路口。',decision='有限端口模型已接，交通控制随R3实际集成。')
    if c['id']=='data':c.update(fields='coreFormat=13 / tasks / vehicles.Position / network / accesses')
    if c['id']=='save':c.update(fields='城市10 / 核心13 / Position+Frozen / Task关联 / Plan')
    if c['id']=='world':c.update(risk='全量冷施工仍有代价；完整经营/市政待R3，不再扩交通Demo。')
card('position','VehiclePosition','Traffic / R2-B已实现','fact','position','public sealed class VehiclePosition','Drivable / Distance / Frozen模式与世界点','Commit · Freeze · Copy','每车唯一物理组件，无每步new；外部只能读取。','从有向共享几何起点量出的车身中心距离；冻结模式保留原位。','Current/distance和Transform只读派生。','移动/换道完成/施工迁移统一提交并校验导航；中断Freeze。','创建→实际运动/冻结→原位恢复；深复制保存，不共享活组件。','Vehicle、Drivable.Geometry；Plan只导航。','纯交通提交检查有测得成本，不宣称重构加速。','已完成必要验收，R3恢复可玩城市优先。')
card('port','JunctionPort','Roads / R2-B已实现','derived','ports','public sealed class JunctionPort','LaneId / JunctionId / Station / Position / Direction','Lane.Entry/Exit · LaneConnection.Entry/Exit','Network从共享车道几何派生，不单独保存/编辑。','进出路口的明确几何边界与切线。','道路变化时重建，控制可按车道/连接稳定ID引用。','仅Network构建；UI不能独立改变端口。','真实交叉/变宽退让，同宽连续切分保持连续。','Lane、LaneConnection、PhysicalRoad与共享Geometry。','高阶匝道/公交站/行人服务并未提前实现。','边界达标就接真实城市，不提前建通用道路框架。')

def md_inline(s):
    s=html.escape(s)
    s=re.sub(r'`([^`]+)`',r'<code>\1</code>',s)
    s=re.sub(r'\*\*([^*]+)\*\*',r'<strong>\1</strong>',s)
    return s

def markdown(text):
    out=[]; table=[]
    def flush():
        if table:
            rows=table[:];table.clear();out.append('<div class="table-scroll"><table>')
            for i,row in enumerate(rows):
                if all(re.fullmatch(r'[:\- ]+',c or '-') for c in row):continue
                tag='th' if i==0 else 'td';out.append('<tr>'+''.join(f'<{tag}>{md_inline(c)}</{tag}>' for c in row)+'</tr>')
            out.append('</table></div>')
    for line in text.splitlines():
        if line.startswith('|'):
            table.append([c.strip() for c in line.strip().strip('|').split('|')]);continue
        flush()
        if not line.strip():continue
        if line.startswith('#'):
            n=min(4,len(line)-len(line.lstrip('#')));out.append(f'<h{n+1}>{md_inline(line[n:].strip())}</h{n+1}>')
        else:out.append('<p>'+md_inline(line)+'</p>')
    flush();return ''.join(out)

review=(OUT/'ArchitectureReview.md').read_text(encoding='utf-8')
decisions=(OUT/'ArchitectureDecisions.md').read_text(encoding='utf-8')
roadmap=(OUT/'RefactoringRoadmap.md').read_text(encoding='utf-8')
byid={c['id']:c for c in cards}

def node(id,label,sub,kind='derived',detail=None):return dict(id=id,label=label,sub=sub,kind=kind,detail=detail or id)
def edge(a,b,label,style='solid'):return dict(a=a,b=b,label=label,style=style)

graphs={
 'global-current':dict(cols=3,nodes=[node('host','Unity Host','运行/工具/Render', 'temporary','session'),node('session','IndustrySession','当前总调度/组装','temporary'),node('save','CityCoreSave','仅业务切片+交通保存','temporary'),node('city','CityModel','原领域事实容器','fact'),node('world','World','道路+时钟+交通+回调','fact'),node('people','CityCoreResidents','实际出行/工资桥接','temporary'),node('factory','Industry / Economy','原真实加工/库存/账户','fact'),node('network','Network','几何/车道/连接/寻路','derived'),node('business','IndustryBusiness','到达同步改城','temporary'),node('old','Services / Growth / Utilities','源码保留，未接当前入口','inactive','city'),node('presentation','Presentation','读取共享几何与车位置','temporary','geometry'),node('observability','Observability','旧分析与日志部分依赖旧图','inactive','city')],edges=[edge('host','session','持有/驱动'),edge('save','session','快照/重组'),edge('session','city','持有'),edge('session','world','每步驱动'),edge('session','people','请求调度'),edge('world','network','查询'),edge('world','business','Depart/Deliver回调'),edge('business','city','写库存/钱'),edge('people','world','派实际车'),edge('factory','city','原partial写事实'),edge('presentation','world','只读位置'),edge('old','city','旧依赖未迁移','dash')]),
 'ownership':dict(cols=3,nodes=[node('session','Session','组装持有，不复制库存','temporary'),node('city','CityModel','建筑/家庭/岗位事实','fact'),node('data','WorldData','World私有事实副本','fact'),node('building','CityBuilding','位置/入口/状态/库存','fact'),node('household','Household → people','家庭持有居民对象','fact'),node('road','NetworkData → PhysicalRoad','中心几何/端点/ID','fact'),node('factory','FactoryState','原料/进度/账户/环境历史','fact'),node('resident','CityResident / CityJob','真实个人与就业关系','fact'),node('vehicle','Vehicle / RoadAccess','车/任务及接入关联','fact'),node('network','Network → Drivable','车道/连接/几何/冲突派生','derived'),node('cache','蓝图 / 空间索引','不存档，可版本失效','cache','network'),node('work','占用 / 意图 / 预约','步内重建或复用工作桶','temporary','world')],edges=[edge('session','city','持有'),edge('session','data','经World持有'),edge('city','building','存储拥有'),edge('city','household','存储拥有'),edge('data','road','存储拥有'),edge('building','factory','拥有'),edge('household','resident','people拥有；Job在society'),edge('data','vehicle','存储拥有'),edge('road','network','派生','dash'),edge('network','cache','构建','dash'),edge('vehicle','work','重建占用','dash')]),
 'global-target':dict(cols=3,nodes=[node('sim','CitySimulation','提案：唯一时钟/命令/阶段','proposal','world'),node('state','CityState + Stores','提案：唯一城市事实根','proposal','city'),node('snapshot','CitySnapshot / Save','提案：完整显式DTO','proposal','save'),node('edit','RoadEditCoordinator','提案：跨域施工事务','proposal','road'),node('traffic','TrafficSimulation','提案：位置/占用/冲突','proposal','vehicle'),node('activity','ResidentActivitySystem','提案：活动/due队列','proposal','people'),node('network','RoadState → RoadNetwork','保留几何/拓扑算法','proposal','network'),node('logistics','Logistics / ShipmentStore','提案：需求/预留/交付','proposal','task'),node('production','Production / Economy','提案：库存/工资/账户','proposal','factory'),node('services','Services / Growth / Utilities','移植已有规则，非已完成','proposal','city'),node('view','Presentation / Queries','提案：只读视图/命令','proposal','building'),node('arrival','ArrivalResult 边界缓冲','提案：业务确认后结算','proposal','business')],edges=[edge('sim','traffic','固定TimeContext'),edge('sim','activity','due活动'),edge('sim','production','积分/日结'),edge('sim','edit','安全边界'),edge('snapshot','state','事实快照'),edge('edit','network','候选/版本提交'),edge('traffic','network','只读查询'),edge('activity','logistics','请求协议'),edge('logistics','traffic','TransportRequest'),edge('traffic','arrival','到达结果'),edge('arrival','production','业务结算'),edge('services','state','领域写权限'),edge('view','state','只读查询')]),
 'classes':dict(cols=4,nodes=[node(c['id'],c['name'],c['fields'][:70],c['kind']) for c in cards],edges=[edge('session','city','持有'),edge('session','world','持有'),edge('world','data','独占'),edge('data','road','包含'),edge('road','junction','ID引用'),edge('network','lane','派生'),edge('network','connection','派生'),edge('lane','geometry','拥有'),edge('connection','geometry','拥有'),edge('world','vehicle','创建/退休'),edge('vehicle','lane','当前位置ID'),edge('vehicle','connection','路线ID'),edge('access','lane','站位引用'),edge('building','factory','拥有'),edge('household','resident','people拥有'),edge('resident','job','jobId'),edge('job','building','工作楼ID'),edge('task','vehicle','目前混在同一对象','dash'),edge('people','resident','引用+写'),edge('business','factory','结算写'),edge('save','session','快照重组')]),
}

flows={
 'current':('当前默认入口 · 精确顺序','每个交通步 .05 秒；Phase不是完全不可变快照。',[
 ('Update / Stopwatch × speed','host','真实时间输入World.Advance，累积欠步，不丢步。'),('BeforeStep / SyncCalendar','session','步首同步日历，不是推进另一个独立时钟。'),('入口重绑 / 中断恢复','bindings','只在网络revision变化后扫描建筑与中断车，原位恢复同任务。'),('居民请求 / 跨日工资','people','AdvanceRequests每步遍历members；跨日转信用到家庭。'),('在岗劳动 / 工资 / 生产','session','原家庭成员扫描，只实际到岗且有效岗位计劳动。'),('环境更新 / 到期货运需求','industry','每步AdvanceIndustry；到dispatchAt每.25秒扫描在途+工厂商店。'),('Reindex占用与已有预约','world','清桶、源/影子/中断占用、排序，重建本路口预约。'),('Choose顺序写影子占用','changes','先遍历车辆，5tick资格评估；此阶段已写换道状态。'),('跟车意图 → ID排序 → 仲裁','world','从桶邻车读限距；预约与entries串行影响后续意图。'),('逐车提交 / 换道完成 / Deliver','world','Deliver在提交循环内同步写居民/库存/财务。'),('退休 → clock+.05 → afterStep','world','删除已交付车、标索引脏；Session再次同步日历。')]),
 'target':('目标统一调度 · 设计提案','未实施；改变到达/劳动阶段，要建立新语义测试。',[
 ('安全边界：命令/施工','edit','候选路网、车辆、入口、任务一起验证并提交。'),('TimeContext(start,end,tick)','world','CitySimulation唯一推进，交通不再积累真实秒。'),('到期活动 / 出行 / 物流准入','people','due队列由关系/路线变化更新；保留等待任务。'),('在岗区间积分 / 环境','factory','生产积分真实时间，保存加工/污染历史；区域更新。'),('占用快照 / 换道意图','vehicle','相邻车/目标前后车，先算意图再按明确顺序协调。'),('本地路口/换道冲突协调','connection','静态几何冲突+动态控制；先单线程可重复。'),('统一物理提交','vehicle','只写交通事实，输出到达记录携带stepEnd。'),('业务原子确认 / 结算','business','库存与钱/居民到岗一起提交，失败仍等待交付。'),('跨日结算 / 统计 / 快照','city','每个日界都处理；发布只读UI查询，保存排程状态。')]),
 'construction':('当前道路施工','正常工具已接；道路Undo仍本地未提交。',[
 ('玩家两次点击','host','Host平面取点；复杂地形GUI未迁移。'),('候选几何/草稿','construction','Connect复制World草稿，吸附端点/道路内部。'),('同层交叉 / 拆分拓扑','construction','高度差超过阈值不连接，共点道路归同一Junction。'),('路网/入口/车路线迁移','world','共享截面保证分割不跳车；实际占用待撤车道拒绝。'),('epoch / 位置 / 安全校验','edit','新World候选验证所有引用与占用；未通过不写活状态。'),('TryCommit → 新Network版本','world','单次替换事实、重新索引和缓存失效。'),('RenderRoads','host','读取Drivable.Geometry，Mesh不是道路事实源。')]),
 'commute':('当前居民通勤','原CityResident与Job事实，不是装饰车辆。',[
 ('AdvanceRequests判断班次','people','资格/资金/观察或自由流估计；在家才能出发。'),('DispatchResident + 路线蓝图','world','有界缓存，路线按车复制；确认入口插入空间。'),('Residents.Depart','people','tripId指实际车、location=-1、atWork=false。'),('实际Lane/Connection行驶','vehicle','跟车/换道/预约真实推进；拆路可中断，不传送。'),('Residents.Arrive','people','验证同居民同trip，卸到实际建筑；岗位变更不自动计工作。'),('下一步实际劳动','factory','步末到岗后，后续步积分；工资信用有真实资金来源。'),('下班请求Home实际返家','people','返家仍新实际车，单一工作位置不是Household字段。')]),
 'freight':('当前工业货运','商店付款为外部经营资金近似，消费/补给尚未接。',[
 ('实际到岗加工','factory','原料、实付劳动、成品空间共同限制产量。'),('每.25秒补足商店需求','session','incoming预留由在途扫描，工厂×商店匹配。'),('寻路 / 入道 / Depart','world','确认可发后扣工厂成品，货物进入Vehicle.cargo。'),('真实道路运输','vehicle','不减少车、不跳交通步；堵路保留在途货物。'),('Deliver检查收货容量','business','端点仍有效；不足保持等待，不能删货。'),('商店Stock / costs + 工厂收入','business','一次性提交，receipts避免重复；不是商店现金账户。'),('cargo清零 / 实车退休','world','成功结算才压缩热集合，历史receipts仍增长。')]),
 'save':('当前保存与恢复','城市6 / 核心9；只覆盖当前切片。',[
 ('Validate + 私有事实快照','save','原业务DTO反射复制；World深复制事实。'),('binary + SHA256','save','类型/字段/版本协议；写tmp/bak由Host处理。'),('Read检查格式/摘要','save','旧格式明确拒绝，不删用户旧文件。'),('候选CityModel + World','save','恢复原建筑/家庭/岗位与完整车状态。'),('Network/索引/成员映射重建','network','占用、冲突、空间/蓝图均派生；不存Transform。'),('跨域Validate → Host替换','save','验证成功再装活城；完整服务/地图目前未包含。')]),
 'legacy':('旧完整城市流程 · 当前未运行','用于迁移规则参照，不与新Session叠加。',[
 ('HarborCityGame.AdvanceSociety','legacy','旧完整入口，当前默认Launch已禁用。'),('CityTraffic.Advance .05','legacy','clock先加，移动先执行，短边路线。'),('移动：释放路口/快照/Move','legacy','既有跟车索引/哈希收益保留为历史证据。'),('市政服务每步更新','legacy','医疗/学校/消防/警务/焚烧及分析。'),('AdvanceResidents / 行业 / 生长','residents','原行为包含通勤/实际劳动，日内时间累计。'),('日界City.Tick / BaselineTick','development','发展模式税收维护/迁入岗位/服务；否则租金家庭Review。'),('每.6秒Schedule货运','legacy','不要把顺序画成服务→移动，源码是移动优先。')]),
}
flowdata={}
flow_refs={'host':('host','void Update()'),'session':('session','void BeforeStep()'),'bindings':('bindings','public void RefreshIfChanged()'),'people':('people','public void AdvanceRequests()'),'industry':('industry','public void AdvanceIndustry'),'world':('world','public void Step()'),'changes':('changes','public static int Choose'),'edit':('edit','public bool TryCommit()'),'factory':('industry','public void ProcessFactoryWork'),'vehicle':('world','public sealed class Vehicle'),'connection':('network','long id=ConnectionId'),'construction':('construction','public static RoadEditPlan Connect'),'business':('session','public bool Deliver'),'save':('save','public static byte[] Write'),'network':('network','void Rebuild()'),'legacy':('legacy','void Simulate()'),'development':('development','void BaselineTick()'),'residents':('residents','public sealed class CityResident')}
flow_refs['city']=('city','public void Tick()')
flow_refs.update({'session':('simulation','void Step(TimeContext time)'),'business':('session','internal bool Deliver'),'production':('production','internal void Step'),'logistics':('logistics','internal void Step'),'world':('world','internal void MoveStep')})
flows['current']=('R1当前默认入口 · 已实现','CitySimulation唯一tick/欠步量；正常交通不再推进城市时间或回调城市业务。',[
 ('Update → CitySimulation.Advance','host','真实时间×倍速请求交给城市调度，不再由交通包住业务回调。'),
 ('TimeContext / 步首日历','session','唯一整数tick产生start/end；World只读取同一时钟。'),
 ('入口重绑 / 中断恢复','bindings','网络版本变化后原位恢复，不重复扣货或移动居民。'),
 ('居民活动请求','people','注册成员视图每步O(P)；经CitySimulation准入，不是独立时钟。'),
 ('CityProductionSystem','production','真实在岗工资/加工；岗位字典O(1)，仍O(P)劳动扫描。'),
 ('CityLogisticsSystem','logistics','每5tick，复用incoming+typed视图；仍O(V+F*S)。'),
 ('World.MoveStep：占用/换道','world','源、影子、中断占用；Choose仍顺序修改，未称全阶段双缓冲。'),
 ('意图 / ID仲裁 / 全部物理提交','world','交通不调用库存/居民/资金逻辑。'),
 ('TransportArrival → 业务确认','business','带stepEnd，所有车移动后确认到岗/收货/付款；失败保留货物。'),
 ('Acknowledge / 退休','world','成功才清cargo退休；删除永久receipts，禁止半步骤save。'),
 ('clock commit / 日历 / 工资','session','扣欠步、tick+1，处理跨日工资；到岗最早下步计工。')])
for k,(name,note,stages) in flows.items():
    flowdata[k]={'name':name,'note':note,'stages':[{'name':n,'detail':d,'source':ref(*flow_refs[s])} for n,s,d in stages]}

readings=[('host','void Update()','1 · 先看真正入口','理解当前默认是工业迁移小城，以及哪里调用Advance、哪里只渲染。'),('session','void BeforeStep()','2 · 查看真实总流程','理解居民/生产/物流先于交通，识别Session哪些职责需要迁出。'),('city','public sealed partial class CityModel','3 · 事实根与旧领域','结合Building/Household/Resident/Job，别把public字段都当安全API。'),('buildings','public CityBuilding GetBuilding','4 · 注册和索引','看到已有O(1)索引，以及Count失效的漏洞；再读BuildingsChanged。'),('network','public sealed class PhysicalRoad','5 · 物理路与共享几何','再读Rebuild、AddLane、Route；曲线点不等于路口。'),('world','public void Step()','6 · 车辆与阶段','理解route/cursor/distance、占用、stable-ID预约、提交中回调。'),('changes','public static bool TryStart','7 · 双占用换道与冲突','结合Reservations；知道影子派生位置、安全与剩余链约束。'),('people','public void AdvanceRequests()','8 · 实际通勤与工资','读Depart/Arrive；只有到岗才参与加工，了解索引生命周期缺口。'),('industry','public void ProcessFactoryWork','9 · 生产与财务','读PayAttendance/AdvanceIndustry，区分环境历史和旧trips暴露。'),('construction','public static RoadEditPlan Connect','10 · 施工事务','联合World.TryCommit/CityCoreRoadBindings，看候选到活状态唯一提交。'),('save','public static CityCoreIndustrySession Read','11 · 保存与恢复','验证什么真正保存、索引如何重建，哪些全城状态缺失。'),('development','void BaselineTick()','12 · 完整城市迁移参照','再看utilities/waste/zoning/map和各服务，均不是新入口已接。')]
readings=[r for r in readings if r[0]!='session']
readings.insert(1,('simulation','void Step(TimeContext time)','2 · R1城市调度','先理解唯一时钟、阶段顺序、物理结束后的业务确认与保存边界。'))
readings.insert(3,('entities','public sealed class CityEntities','3b · 实体生命周期','原对象不复制；结构API同步ID索引/成员/岗位/端点，绕过旧列表编辑会在Validate失败。'))
readings=[(k,token,t,('当前城走CityEntities索引；下方Count缓存仅旧入口后备，理解为何要维护生命周期。' if k=='buildings' else w)) for k,token,t,w in readings]
readingdata=[dict(title=t,why=w,source=ref(k,token)) for k,token,t,w in readings]
for reading in readingdata:
    if reading['title'].startswith('6 ·'):
        reading.update(why='R2-A任务/载荷已独立；MoveStep仍使用Plan/cursor位置，业务在全提交后结算。',source=ref('world','internal void MoveStep'))
evidence_specs=[
 ('P0 时钟与同步结算','world','public void Step()'),
 ('R2-B 物理位置已独立','world','public long Current=>Position.Drivable'),
 ('P1 建筑索引已有，但Count失效不充分','buildings','if(buildingLookup==null'),
 ('P1 岗位仍是线性查找','jobs','public CityJob Job(int id)'),
 ('P1 成员索引生命周期仅构造','people','members.Add(person.id'),
 ('P1 商店过滤嵌在每个工厂循环','session','foreach(var factory in City.buildings.OfType'),
 ('P1 新货车环境暴露缺失：读旧任务','industry','foreach(var t in traffic.trips)'),
 ('P1 完成收据无限保留','session','readonly HashSet<int> receipts'),
 ('R2-B 真实端口与共享曲线','ports','static Point[] Curve'),
 ('P2 全量施工候选与提交','world','internal bool TryCommit'),
 ('P2 局部连接冲突两两构建','reservations','foreach(var bucket in local.Values)'),
 ('P2 保存协议依赖public字段布局','save','static FieldInfo[] Fields'),
 ('模块：旧分区规划世界坐标键','zoning','public sealed class CityZoningState'),
 ('模块：旧地图参数而非渲染事实','map','public sealed class CityMapState'),
 ('模块：旧供给有建筑桶，但接线遍历管网边','utilities','public void RecalculateUtilities()'),
 ('模块：焚烧处理和垃圾守恒，读取旧trips','waste','public long WasteBalanceError'),
 ('模块：消防交通业务在旧CityTraffic partial','fire','void ScheduleFire()'),
 ('模块：警务交通业务在旧CityTraffic partial','police','void SchedulePolice()'),
 ('模块：医疗到达在旧TrafficTrip协议','healthcare','void ArriveHealthcare'),
 ('模块：旧城市贷款日结','loans','public int SettleLoans()'),
 ('模块：观测器不应写事实，却仍读旧交通','analysis','public void ObserveTraffic()'),
 ('模块：生长与日结依赖旧clock/roads','development','void BaselineTick()'),
]
evidence_specs=[e for e in evidence_specs if e[1:] not in [('people','members.Add(person.id'),('session','foreach(var factory in City.buildings.OfType'),('session','readonly HashSet<int> receipts')]]
evidence_specs.extend([('R1 已实现：成员生命周期','entities','public void AddResident'),('R1 剩余风险：物流F*S','logistics','foreach(var factory in entities.Factories)'),('R1 已实现：物理后结果结算','simulation','foreach(var arrival in transport.Arrivals)'),('R1 已实现：唯一tick/欠步','clock','public sealed class SimulationClock')])
evidence_specs=[(('R1 已改：正常城市不再调用此独立Step' if t.startswith('P0 时钟') else 'R1 已改：城市岗位字典，旧入口Find后备' if t.startswith('P1 岗位') else 'R1 已改：城市结构索引，旧入口Count缓存后备' if t.startswith('P1 建筑索引') else t),k,token) for t,k,token in evidence_specs]
evidence=[dict(title=t,source=ref(k,token)) for t,k,token in evidence_specs]
# The previous grouped ownership node blurred Job storage with household membership.
graphs['ownership']=dict(cols=3,nodes=[node('session','Session','组装持有，不复制库存','temporary'),node('city','CityModel','建筑/家庭/岗位事实','fact'),node('data','WorldData','World私有事实副本','fact'),node('building','CityBuilding','位置/入口/状态/库存','fact'),node('household','Household','存储people居民对象','fact'),node('road','NetworkData → PhysicalRoad','中心几何/端点/ID','fact'),node('factory','FactoryState','原料/进度/账户/环境历史','fact'),node('resident','CityResident','家庭people包含，jobId仅引用','fact'),node('vehicle','Vehicle','位置/当前任务/在途货物','fact'),node('job','CityJob','society.jobEntities包含','fact'),node('access','RoadAccess','WorldData关联表，不存门口XYZ','fact'),node('network','Network → Drivable','车道/连接/几何/冲突派生','derived'),node('cache','蓝图 / 空间索引','不存档，可版本失效','cache','network'),node('work','占用 / 意图 / 预约','步内重建或复用工作桶','temporary','world')],edges=[edge('session','city','持有'),edge('session','data','经World持有'),edge('city','building','存储拥有'),edge('city','household','存储拥有'),edge('city','job','society包含'),edge('data','road','存储拥有'),edge('data','vehicle','存储拥有'),edge('data','access','存储拥有'),edge('building','factory','拥有'),edge('household','resident','people拥有'),edge('resident','job','jobId引用','dash'),edge('road','network','派生','dash'),edge('network','cache','构建','dash'),edge('vehicle','work','重建占用','dash')])
perf=[]
for filename in ['Docs/Benchmarks/2026-10-14-RoadUpgrade/model.csv','Docs/Benchmarks/2026-10-14-TrafficScale/window.csv']:
    with (ROOT/filename).open(encoding='utf-8-sig') as f:perf.append(dict(file=filename,rows=list(csv.DictReader(f))))

graphs['global-current']=dict(cols=3,nodes=[node('session','Session','R1组装根，无BeforeStep总控','temporary'),node('simulation','CitySimulation','唯一推进/阶段/结算边界','fact'),node('clock','SimulationClock','整数tick/欠步量','fact'),node('entities','CityEntities','生命周期/ID索引/只读视图','derived'),node('people','CityCoreResidents','实际请求/到岗/返家/工资','temporary'),node('world','World','只推进物理/产生Arrival','fact'),node('production','CityProductionSystem','真实在岗工资与加工','temporary'),node('logistics','CityLogisticsSystem','5tick匹配；仍O(V+F*S)','temporary'),node('business','City业务结算','不在交通移动中回调','temporary'),node('city','CityModel','原事实唯一；public仍风险','fact'),node('network','Network','车道/连接/几何与冲突','derived'),node('save','城市7 / 核心10','保存tick/debt和全部在途状态','temporary'),node('old','Services/Growth/Utilities','旧代码未接默认新城','inactive','city')],edges=[edge('session','simulation','持有/驱动'),edge('simulation','clock','唯一写入'),edge('world','clock','只读引用'),edge('simulation','world','MoveStep'),edge('simulation','people','活动请求'),edge('simulation','production','劳动积分'),edge('simulation','logistics','到期物流'),edge('simulation','business','结果结算'),edge('entities','city','索引同一对象'),edge('world','network','查询'),edge('save','clock','保存城市时钟'),edge('old','city','尚未迁移','dash')])
graphs['ownership']['nodes'].extend([node('simulation','CitySimulation','唯一城市步骤写入者','fact'),node('clock','SimulationClock','城市tick+未消耗时间','fact'),node('entities','CityEntities','索引引用同一原事实','derived')])
graphs['ownership']['edges'].extend([edge('session','simulation','拥有'),edge('simulation','clock','拥有并推进'),edge('data','clock','正常城不保存第二时钟','dash'),edge('entities','city','索引/支持结构写入口','dash')])
graphs['classes']['edges'].extend([edge('session','simulation','持有'),edge('simulation','clock','唯一推进'),edge('simulation','production','驱动'),edge('simulation','logistics','驱动'),edge('simulation','business','物理后结算'),edge('entities','resident','索引同对象'),edge('entities','job','ID索引')])
for n in graphs['global-target']['nodes']:
    if n['id']=='sim':n.update(label='CitySimulation',sub='R1已落地：唯一时钟/阶段',kind='fact',detail='simulation')
    if n['id']=='arrival':n.update(label='TransportArrival',sub='R1已落地：只读到达结果',kind='derived',detail='business')
graphs['ownership']['nodes'].extend([node('task','TripTask','独立ID/目的/居民/生命周期','fact'),node('shipment','Shipment','任务拥有在途货量/价格','fact'),node('routeplan','RoutePlan','每车独立路径/终点站位','fact')])
graphs['ownership']['edges'].extend([edge('data','task','tasks集合拥有'),edge('vehicle','task','唯一引用；非复制','dash'),edge('task','shipment','仅货运拥有'),edge('vehicle','routeplan','导航拥有'),edge('resident','task','tripId引用','dash')])
for n in graphs['ownership']['nodes']:
    if n['id']=='vehicle':n.update(sub='物理状态/Task引用/Plan')
graphs['classes']['edges']=[e for e in graphs['classes']['edges'] if not (e['a']=='task' and e['b']=='vehicle')]
graphs['classes']['edges'].extend([edge('vehicle','task','引用同一任务'),edge('task','shipment','货运载荷拥有'),edge('vehicle','routeplan','拥有独立导航'),edge('resident','task','tripId')])
graphs['global-current']['nodes'].extend([node('task','TripTask + Shipment','R2-A实际任务/货物事实','fact')])
graphs['global-current']['edges'].extend([edge('world','task','登记/关联/退休'),edge('business','task','读取目的与货物'),edge('people','task','居民tripId引用','dash')])
for n in graphs['global-current']['nodes']:
    if n['id']=='save':n.update(label='城市8 / 核心11',sub='独立Tasks/实车/路径/唯一时钟')
    if n['id']=='logistics':n.update(sub='有向车道/站位候选索引')
flowdata['current']['name']='R1后续 / R2-A当前默认入口 · 已实现'
flowdata['current']['stages'][5]['detail']='5tick调度；按有向车道/站位候选索引筛除不可达，再按原商店顺序准入。'
flowdata['commute']['stages'][2]['detail']='Depart(TripTask)：居民tripId指独立任务，location=-1；车辆→任务索引定位实车。'
flowdata['freight']['stages'][2]['detail']='可达候选/入口间距/预约通过后扣原库存，货物进入TripTask.Shipment。'
flowdata['freight']['stages'][1]['detail']='incoming从Tasks重算，车道/站位候选按原商店优先序处理；库存仍是建筑事实。'
flowdata['freight']['stages'][5]['detail']='全物理提交后业务确认；同健康步骤ACK/退休，重复付款由生命周期拒绝，无永久receipts。'
flowdata['freight']['stages'][-1]['detail']='成功ACK清Shipment并同时退休任务/车；无永久receipts。'
flowdata['save']['note']='城市8 / 核心11：独立任务、Shipment、Vehicle关联和Plan，旧格式明确拒绝。'
graphs['ownership']['nodes'].extend([node('position','VehiclePosition','唯一物理锚点/冻结模式','fact'),node('port','JunctionPort','几何端口/站位/切线','derived')])
graphs['ownership']['edges'].extend([edge('vehicle','position','唯一拥有'),edge('network','port','派生边界','dash'),edge('position','network','Drivable引用','dash')])
graphs['classes']['edges'].extend([edge('vehicle','position','唯一物理事实'),edge('lane','port','派生端口'),edge('connection','port','连接真实进出口')])
for n in graphs['global-current']['nodes']:
    if n['id']=='save':n.update(label='城市10 / 核心13',sub='Position/任务/路径/唯一时钟')
flowdata['current']['name']='R2-B实际更新流程（基础验收完成）'
flowdata['current']['stages'][6]['detail']='从Position建立源/影子/冻结占用；端口外等待，实际连接曲线预约。'
flowdata['current']['stages'][7]['detail']='意图读取物理锚点，导航只取后续段；全部位置与导航一致提交。'
flowdata['save']['note']='城市10 / 核心13：独立位置/冻结模式、任务与导航，旧格式拒绝。'
payload=dict(commit=COMMIT,baseline=BASELINE,cards=cards,graphs=graphs,flows=flowdata,readings=readingdata,perf=perf,evidence=evidence)

page=r'''<!doctype html>
<html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>HarborCity · 全工程架构审查</title>
<style>
:root{--ink:#172b3d;--muted:#516675;--bg:#f3f6f8;--line:#d1dce3;--blue:#175985;--fact:#126451;--derived:#1b5789;--cache:#774598;--temp:#98621c;--proposal:#6145ab;--inactive:#596975}*{box-sizing:border-box}body{margin:0;font:16px/1.8 "Segoe UI","Microsoft YaHei",sans-serif;color:var(--ink);background:var(--bg)}header{padding:42px 5vw 28px;background:#122c40;color:#fff;border-bottom:5px solid #49bba4}header p{color:#d0e0e9;max-width:1000px}h1{font-size:32px;margin:0}h2{font-size:25px;border-bottom:1px solid var(--line);padding-bottom:10px}h3{font-size:20px;margin-top:28px}h4{font-size:18px}p{margin:12px 0}a{color:#125681;overflow-wrap:anywhere}code{font-family:Consolas,monospace;background:#e8eef2;border-radius:4px;padding:2px 5px;font-size:.9em}pre{white-space:pre-wrap;overflow-wrap:anywhere;font-size:13px;background:#ecf1f5;padding:15px;border:1px solid var(--line)}layout{display:grid;grid-template-columns:245px minmax(0,1fr);max-width:1720px;margin:auto}nav{position:sticky;top:0;height:100vh;overflow:auto;padding:25px 18px;border-right:1px solid var(--line);background:#fff}nav a{display:block;padding:8px 12px;text-decoration:none;border-radius:6px;font-size:14px}nav a:hover{background:#e8f1f6}main{padding:10px 32px 80px;min-width:0}section{scroll-margin-top:16px;padding-top:20px}.lead{font-size:18px}.callout{border-left:4px solid #1b806b;background:#e5f3ef;padding:15px 20px;margin:18px 0}.warning{border-color:#af7023;background:#fff2dc}.pill{display:inline-block;font-size:12px;border:1px solid currentColor;border-radius:20px;padding:2px 9px;margin:3px}button,select,input{font:inherit}button,select{background:#fff;border:1px solid #9eb3c2;border-radius:6px;color:var(--ink);padding:7px 12px;cursor:pointer}button:hover{background:#e4eef5}button:focus-visible,a:focus-visible,select:focus-visible{outline:3px solid #bd7b00;outline-offset:2px}.controls{display:flex;gap:8px;align-items:center;flex-wrap:wrap;margin:12px 0}.graph-layout{display:grid;grid-template-columns:minmax(0,1fr) 310px;gap:15px}.canvas{overflow:auto;border:1px solid #b9cbd7;border-radius:8px;background:#fff;max-height:690px;min-height:300px}.canvas svg{display:block;width:100%;min-width:650px;height:auto}.inspector{background:#fff;border:1px solid var(--line);border-radius:8px;padding:16px;max-height:690px;overflow:auto}.inspector h3{margin:0}.inspector dt{font-weight:650;font-size:14px;margin-top:12px}.inspector dd{margin:3px 0;font-size:14px}.node{cursor:pointer}.node rect{fill:#fff;stroke-width:2}.node:hover rect,.node.selected rect{fill:#eaf2f7;stroke-width:4}.node text{font-family:"Segoe UI","Microsoft YaHei",sans-serif}.edge text{font-size:11px;fill:#334f62;paint-order:stroke;stroke:#fff;stroke-width:4px;stroke-linejoin:round}.legend{font-size:13px;color:var(--muted)}.table-scroll{overflow:auto}table{border-collapse:collapse;width:100%;background:#fff;font-size:14px;margin:14px 0}th,td{border:1px solid var(--line);padding:10px 13px;vertical-align:top;text-align:left}th{background:#e4edf3}details{background:#fff;border:1px solid var(--line);border-radius:6px;padding:12px 18px;margin:12px 0}summary{cursor:pointer;font-weight:650}.flowlist{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:12px;counter-reset:flow}.stage{background:#fff;border:1px solid var(--line);border-top:4px solid #3d7ba1;border-radius:8px;padding:13px;text-align:left;min-height:140px}.stage strong{display:block;margin-bottom:7px}.stage small{color:var(--muted)}.stage.active{background:#e7f3ee;border-color:#178166}.step-detail{min-height:180px;margin-top:16px}.concepts{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px}.concepts article{background:#fff;border:1px solid var(--line);padding:16px;border-radius:8px}.concepts h3{margin-top:0}.scenario{background:#fff;border:1px solid var(--line);border-radius:8px;overflow:auto}.scenario svg{min-width:700px;width:100%;height:auto}.search-results{display:flex;gap:7px;flex-wrap:wrap;max-height:160px;overflow:auto}.evidence{font-size:13px;color:var(--muted)}.source-path{font-size:12px;word-break:break-all}.doccontent h2{font-size:23px}.doccontent h3{font-size:20px}.doccontent h4{font-size:18px}.metric{font-size:28px;font-weight:650;color:#126451}footer{padding-top:35px;color:var(--muted);font-size:14px}@media(max-width:1050px){layout{grid-template-columns:1fr}nav{position:relative;height:auto;display:flex;flex-wrap:wrap;border-bottom:1px solid var(--line)}nav a{font-size:13px}main{padding:12px 18px}.graph-layout{grid-template-columns:1fr}.inspector{max-height:500px}.flowlist{grid-template-columns:repeat(2,1fr)}}@media(max-width:600px){.concepts,.flowlist{grid-template-columns:1fr}h1{font-size:26px}}@media print{nav,.controls{display:none}layout{display:block}.canvas,.inspector{max-height:none}.graph-layout{display:block}section{break-inside:avoid}header{background:#fff;color:#000}header p{color:#333}}
</style></head><body>
<header><div class="pill">架构审查 · 2026-10-14</div><div class="pill">中文 / 离线 / 无第三方脚本</div><h1>HarborCity：从可运行切片，到长期城市架构</h1><p>先理解数据由谁拥有、谁能够修改、每一步发生什么，再决定哪些模型值得保留。当前实现与推荐方案分别展示；目标模块没有被伪装成已经实现。</p><p class="evidence" style="color:#d0e0e9">审查分支 codex/road-core-20261014 · 已推送 4a47968 · 完整游戏基线 110c4ea。未提交道路撤销通过核心检查，未接GUI，单列本地探索。</p></header>
<layout><nav aria-label="目录"><a href="#overview">01 总览与现状</a><a href="#ownership">02 全局数据所有权图 ★</a><a href="#classes">03 核心类关系图 ★</a><a href="#simulation">04 模拟更新流程图 ★</a><a href="#scenario">05 串联场景与概念</a><a href="#businessflows">06 真实业务流程</a><a href="#performance">07 性能与规模证据</a><a href="#review">08 架构问题与源码证据</a><a href="#alternatives">09 方案比较与决策</a><a href="#target">10 目标架构 · 提案</a><a href="#roadmap">11 重构与完整城市门槛</a><a href="#reading">12 怎样阅读项目</a><a href="#verification">13 文档验收与边界</a></nav><main>
<section id="overview"><h2>01 · 当前是什么，目标是什么</h2><p class="lead">推荐方案B：重划城市调度、道路、交通与业务边界，保留正确算法和领域规则。当前RoadCore也接受重新设计，不默认它是最终架构。</p><div class="callout warning">默认入口是原居民/工厂/商店接新交通的迁移小城。完整旧城市的分区、市政、管网、公交、地形工具和经营UI尚未接回。10000车交通夹具不代表10000车完整城市。</div><p>图中实线箭头在模块图表示调用/引用依赖，所有权图表示包含/拥有；虚线标派生或未接入，边上均写具体含义。点节点可查看职责和源码；先用“只看选中关系”避免跨层连线干扰。</p><div class="legend"><span class="pill" style="color:var(--fact)">绿色 权威事实</span><span class="pill" style="color:var(--derived)">蓝色 派生数据</span><span class="pill" style="color:var(--cache)">紫色 缓存</span><span class="pill" style="color:var(--temp)">橙色 临时工作/过渡模块</span><span class="pill" style="color:var(--inactive)">灰色 尚未接入</span><span class="pill" style="color:var(--proposal)">紫色虚线框 设计提案</span></div>
<div id="globalgraph"></div></section>
<section id="ownership"><h2>02 · 全局数据所有权与生命周期 ★</h2><p>“单一事实”不是禁止缓存，而是禁止两个模块独立修改同一笔库存、同一辆车的位置或同一个居民。当前城市库存只在原建筑中；在途货物当前在Vehicle上，正是建议分离的部分。</p><div id="ownergraph"></div><div class="table-scroll"><table><tr><th>生命周期</th><th>事实变化</th><th>派生数据</th></tr><tr><td>道路建造/升级/拆分</td><td>World候选事务提交NetworkData+入口+车路线/站位；失败不部分写</td><td>车道/连接/冲突/占用重建，蓝图与空间网格按版本失效</td></tr><tr><td>车辆发车→交付→退休</td><td>Depart扣货或设置居民tripId；Deliver改业务；成功清cargo并移除活车</td><td>派车路线缓存，实际车持独立路线；退休删除vehiclesById</td></tr><tr><td>居民/建筑/岗位</td><td>原实体和ID真实存在；关系需成对更新</td><td>buildingLookup已有字典但Count失效不可靠；成员字典只创建时建立</td></tr><tr><td>保存/恢复</td><td>保存加工进度、工资信用、污染历史、路线/车站位等影响未来的事实</td><td>占用、预约、查询、Transform和Mesh读档重建</td></tr></table></div></section>
<section id="classes"><h2>03 · 核心类关系图 ★</h2><p>类名与字段来自实际源码。当前“Lane”和“LaneConnection”都是Drivable的不同语义，不存在独立TransportTask类；它们不会被画成已经完成的新实体。</p><div class="controls"><label>查类 <input id="class-search" type="search" placeholder="Vehicle / 工厂 / CityModel"></label><label>模块 <select id="module-filter"><option value="">全部</option><option>Roads</option><option>Traffic</option><option>Population</option><option>Industry</option><option>Save</option></select></label></div><div class="search-results" id="class-results"></div><div id="classgraph"></div></section>
<section id="simulation"><h2>04 · 模拟更新流程图 ★</h2><div class="controls"><label>流程 <select id="simulation-mode"><option value="current">当前默认入口</option><option value="target">目标统一调度（提案）</option><option value="legacy">旧完整城市（未接入）</option></select></label></div><p id="simulation-note"></p><div id="simulation-flow"></div><div id="simulation-detail" class="inspector step-detail"></div><div class="callout warning">当前Choose会修改换道影子桶，Deliver在逐车位置提交中直接改业务，不能把它描述成完全双缓冲、全阶段无副作用。目标流程改变这两处语义，须用新的安全/到达/资金测试证明正确性。</div><table><tr><th>工作</th><th>当前频率</th><th>目标建议（未实施）</th><th>失效/触发</th></tr><tr><td>交通移动/安全</td><td>每.05秒全活动车</td><td>保持固定精度；局部查询</td><td>车道/路口/控制版本</td></tr><tr><td>居民出行</td><td>每步全成员扫描；重试.25秒</td><td>到期活动队列+关系变化重排</td><td>岗位、家、健康、路网估计变化</td></tr><tr><td>实际劳动</td><td>每步家庭成员扫描</td><td>在岗集合区间积分</td><td>到岗/下班、资金、供应/原料/仓满</td></tr><tr><td>物流需求</td><td>.25秒扫车+工厂商店</td><td>需求/在途预留增量，有限批匹配</td><td>生产、消耗、交付、取消、企业变化</td></tr><tr><td>环境</td><td>每步建筑/旧任务扫描</td><td>生产强度积分+区域更新</td><td>真实车暴露、工厂活动、时间衰减</td></tr><tr><td>日结/城市成长</td><td>旧日界；新仅工资游标</td><td>统一每个日界，保留原规则</td><td>时间越界，不能一次跳过多日</td></tr></table></section>
<section id="scenario"><h2>05 · 用一条弯路串起来</h2><p>下面是<strong>说明性场景</strong>，用现有2车道/方向API、实际居民/生产语义说明关系；不是默认直线小城截图，也不是在浏览器运行Unity。端口转弯和任务独立模型仍是提案。</p><div class="controls"><button data-scene="0">① 从家出发</button><button data-scene="1">② 到工厂工作</button><button data-scene="2">③ 生产→发货</button><button data-scene="3">④ 商店收货</button></div><div class="scenario"><svg viewBox="0 0 1000 360" aria-label="弯曲双向四车道与建筑关系示意"><path d="M75 230 C300 230 300 105 580 135 S780 250 925 215" stroke="#546573" stroke-width="72" fill="none"/><path d="M75 210 C300 210 300 85 580 115 S780 230 925 195" stroke="#69d1b8" stroke-width="3" fill="none"/><path d="M75 224 C300 224 300 99 580 129 S780 244 925 209" stroke="#69d1b8" stroke-width="3" fill="none"/><path d="M75 238 C300 238 300 113 580 143 S780 258 925 223" stroke="#79bce4" stroke-width="3" fill="none"/><path d="M75 252 C300 252 300 127 580 157 S780 272 925 237" stroke="#79bce4" stroke-width="3" fill="none"/><g data-object="junction" role="button" tabindex="0"><circle cx="75" cy="230" r="18" fill="#fff" stroke="#315477"/><text x="30" y="289">路口 A</text></g><g data-object="junction" role="button" tabindex="0"><circle cx="925" cy="215" r="18" fill="#fff" stroke="#315477"/><text x="883" y="276">路口 B</text></g><g data-object="household" role="button" tabindex="0"><rect x="210" y="37" width="140" height="60" rx="8" fill="#d6f0e5" stroke="#126451"/><text x="225" y="73">住宅 / 家庭</text></g><path d="M280 97 L280 180" stroke="#92631d" stroke-dasharray="5 4"/><g data-object="access" role="button" tabindex="0"><circle cx="280" cy="177" r="10" fill="#ffc75b"/></g><g data-object="factory" role="button" tabindex="0"><rect x="520" y="245" width="150" height="60" rx="8" fill="#fff0d8" stroke="#996116"/><text x="538" y="281">工厂 / 岗位</text></g><path d="M590 245 L590 171" stroke="#92631d" stroke-dasharray="5 4"/><g data-object="building" role="button" tabindex="0"><rect x="780" y="37" width="150" height="60" rx="8" fill="#ddeef9" stroke="#246a93"/><text x="798" y="74">商店 / 库存</text></g><path d="M840 97 L840 187" stroke="#92631d" stroke-dasharray="5 4"/><g data-object="vehicle" id="scene-car" role="button" tabindex="0"><rect x="323" y="169" width="25" height="15" rx="3" fill="#ffe070" stroke="#202f38"/></g><g data-object="road" role="button" tabindex="0"><text x="355" y="335">点击对象：Road · 2条/方向Lane · RoadAccess · Resident · Factory · Vehicle</text></g></svg></div><div id="scene-detail" class="callout"></div><div id="scene-object" class="inspector"></div>
<div class="concepts"><article><h3>Road ≠ Lane</h3><p>Road保存这条物理路的中心曲线和两端。Lane是车实际按一个方向行驶的偏移几何。lanes=2且Both会派生4条有向车道，增加曲线采样点不会增加路口。</p></article><article><h3>Junction ≠ Connection</h3><p>Junction是路与路连接地点；Connection是“这条进口Lane→那条出口Lane”的轨迹。不同轨迹可以不冲突。当前连接短曲线需要真实端口改造。</p></article><article><h3>所有权 ≠ 引用</h3><p>工厂拥有库存，车引用出发工厂ID。Depart把库存转到在途Cargo，Deliver才进商店。两个对象都存相同库存且各自加减才是双真相。</p></article><article><h3>缓存 ≠ 事实</h3><p>前车桶可从车位置重建，蓝图可从路网重算；但污染历史、加工进度和已付款信用会影响以后，不能简单清零后重算。</p></article><article><h3>局部索引为什么有效</h3><p>10000车不意味着每辆车都看9999辆：同Lane排序后前车是邻项。当前仍有O(V log V)意图排序与拥堵桶插入，不能称整步O(1)。</p></article><article><h3>事务为什么必要</h3><p>在居民车下方切路会改变Road/Lane/路线和入口。必须在候选中证明站位不跳、安全且任务完整，然后一起提交；先改路再找车会留半城坏引用。</p></article></div></section>
<section id="businessflows"><h2>06 · 真实业务执行流程</h2><div class="controls"><select id="business-mode"><option value="construction">道路施工</option><option value="commute">居民通勤</option><option value="freight">工业货运</option><option value="save">保存与恢复</option></select></div><p id="business-note"></p><div id="business-flow"></div><div id="business-detail" class="inspector step-detail"></div></section>
<section id="performance"><h2>07 · 性能：证据、复杂度、未知</h2><div class="callout">绿色数字是已保存CSV中的实测；下方规模计算是<strong>理论候选次数</strong>，不预测毫秒或FPS。RoadCore固定步不等于包括居民、服务、税收和所有UI的完整步。</div><div id="perf-tables"></div><h3>规模示意：为什么全局匹配要移出热循环</h3><div class="controls"><label>规模 N <select id="scale"><option>100</option><option selected>1000</option><option>10000</option></select></label></div><div class="table-scroll"><table><tr><th>模型（非实测）</th><th>当前规模计算</th><th>含义/限制</th></tr><tr><td>N个工厂各检查N个商店</td><td id="quadratic" class="metric"></td><td>只计配对，不含每F重复OfType扫描、寻路与资金查询</td></tr><tr><td>N个居民，J=N个岗位线性查找</td><td id="jobs-count" class="metric"></td><td>一次查询最坏N×N；真实多次调用/提前命中差异大</td></tr><tr><td>单次N个邻项查询</td><td id="local-count" class="metric"></td><td>前提桶已建且排序；建桶/排序成本不能忽略</td></tr></table></div><p>代表性综合测试尚未建立：需要多个真实企业、在岗居民、消费/原料补给、市政/日结、道路动态施工共同运行。先恢复完整经营R3，再做R4；当前只有四居民、一工厂一商店，不能得出大城结论。</p><p class="evidence">本轮核心2700与行业居民477项检查通过；详细新CSV仅作为模型结果归档。没有新录制Unity全城窗口。历史window.csv cap60、3秒热身/6秒样本、12个配置，原始大型Profiler文件未提交或删除。</p></section>
<section id="review"><h2>08 · 全工程架构问题与证据</h2><p>以下是完整审查，包含事实/派生表、P0–P3根因、复杂度与测试边界。可折叠阅读，源码导航在第12节。</p><details open><summary>展开 ArchitectureReview</summary><div class="doccontent">__REVIEW__</div></details></section>
<section id="alternatives"><h2>09 · 替代方案与架构决策</h2><div class="callout">不建议“全部从零重写”；也不建议只给现有World/Session补接口。方案B真正改变事实归属、生命周期和写入时机，同时复用已证明有价值的算法。</div><details open><summary>A / B / C 对比与 ADR 记录</summary><div class="doccontent">__DECISIONS__</div></details><p>CityFlow参考的是局部Drivable与阶段调度思路，静态路网和线程生命周期不能直接套实时建路游戏。源码参考：<a href="https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/roadnet/roadnet.h">固定版本 roadnet.h</a>、<a href="https://github.com/cityflow-project/CityFlow/blob/81ee0f47659ca66177a71f81676691c58ee89184/src/engine/engine.cpp">engine.cpp</a>；仓库既有深入对照见 <a href="../CityFlowTrafficDesign.md">CityFlowTrafficDesign.md</a>。这些链接需联网，核心文档与图表完全离线。</p></section>
<section id="target"><h2>10 · 推荐目标架构（设计提案，未实施）</h2><p>CityState只是保存根，不是新的巨型CityModel。CitySimulation协调时钟和顺序，不处理所有规则。Traffic不查家庭/工厂财务，业务不移动车；跨域关系在明确事务边界共同验证。</p><div id="targetgraph"></div><table><tr><th>保留</th><th>重划/重写</th><th>替代通过后删除</th></tr><tr><td>几何/拓扑分离、hash、局部占用、生产工资守恒、安全事务</td><td>实体Store、时钟、任务/车位置、真实路口端口、Arrival结算协议、显式保存</td><td>Session业务总控、World施工/城时钟/同步财务、已迁移CityModel partial和旧短边Move</td></tr></table><p>过渡器不能永久积累，但仍被未迁移服务使用的源文件不能提前删掉并宣称城市恢复完成。</p></section>
<section id="roadmap"><h2>11 · 路线与完整可玩城市门槛</h2><div class="doccontent">__ROADMAP__</div></section>
<section id="reading"><h2>12 · 核心代码阅读导航</h2><p>按下面顺序读。每张卡内附固定提交、准确行号与实际代码摘录；离线可看摘录，点击“本地文件”或复制路径到VS Code。浏览器是否允许打开.cs取决于本地设置，不能保证VS Code直接跳行；固定GitHub链接行号可联网定位。</p><div id="readings"></div></section>
<section id="verification"><h2>13 · 验收、状态与继续开发约定</h2><p>文档不加载CDN、字体、外部脚本或运行期数据；数据、CSS、SVG和交互都内嵌。可双击本HTML，使用目录、切换当前/提案流程、点击节点、缩放、搜索、查看源码摘录和规模示意。源码在线链接是可选补充。</p><p>图中的核心类关系以固定commit核对。所有提案节点有紫色虚线框；当前旧系统灰色未接入。撤销四个未提交文件已验证并保留，不在本次文档提交中夹带游戏修改。方向性重写等用户理解并确认总体方案后再实施。</p><p><a href="ArchitectureReview.md">审查Markdown</a> · <a href="ArchitectureDecisions.md">决策Markdown</a> · <a href="RefactoringRoadmap.md">重构路线Markdown</a> · <a href="../../Tools/BuildArchitectureDocs.py">离线文档生成工具</a> · <a href="Verification.md">本次文档实际验收记录</a></p></section><footer>HarborCity 架构审查 · 源码基准4a47968 / 完整城市基线110c4ea。未来规模属于设计方向，不是已验证产品能力；不承诺永不重构。</footer></main></layout>
<script id="architecture-data" type="application/json">__DATA__</script><script>
'use strict';
const DATA=JSON.parse(document.getElementById('architecture-data').textContent),byId=Object.fromEntries(DATA.cards.map(c=>[c.id,c]));
const esc=s=>String(s).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const colors={fact:'#126451',derived:'#1b5789',cache:'#774598',temporary:'#98621c',inactive:'#596975',proposal:'#6145ab'};
const kinds={fact:'权威事实（或持有事实的模块）',derived:'派生/可重建',cache:'缓存',temporary:'临时工作/过渡结构',inactive:'尚未接默认入口',proposal:'设计提案，尚未实现'};
function sourceHtml(s){return `<p class="source-path">${esc(s.file)}:${s.line}<br><code>${esc(s.token)}</code></p><div class="controls"><a href="${esc(s.url)}" target="_blank" rel="noopener">固定提交源码 L${s.line}</a><a href="../../${esc(s.file)}">本地文件</a><button type="button" class="copy-source" data-path="${esc(s.file+':'+s.line)}">复制定位</button></div><details><summary>离线代码摘录</summary><pre>${esc(s.excerpt)}</pre></details>`;}
function cardHtml(id){const c=byId[id];if(!c)return '<p>此节点用于模块说明，详见审查和目标架构。</p>';return `<h3>${esc(c.name)}</h3><span class="pill" style="color:${colors[c.kind]}">${esc(kinds[c.kind])}</span><p>${esc(c.module)}</p><dl>${[['主要字段',c.fields],['入口方法',c.methods],['概念 / 谁拥有',c.owner],['事实',c.facts],['派生 / 缓存',c.derived],['谁可以修改',c.write],['生命周期',c.life],['依赖',c.deps],['扩展风险',c.risk],['推荐处理',c.decision]].map(([a,b])=>`<dt>${a}</dt><dd>${esc(b)}</dd>`).join('')}</dl>${sourceHtml(c.source)}`;}
document.addEventListener('click',async e=>{const b=e.target.closest('.copy-source');if(!b)return;try{await navigator.clipboard.writeText(b.dataset.path);b.textContent='已复制';}catch(_){const t=document.createElement('textarea');t.value=b.dataset.path;b.after(t);t.select();b.textContent='路径已展开，请复制';}});
function textLines(text,n){const result=[];for(let i=0;i<text.length;i+=n)result.push(text.slice(i,i+n));return result.slice(0,3);}
function graph(container,key){const host=document.getElementById(container),g=DATA.graphs[key];let selected=null,local=true,zoom=1;
host.innerHTML=`<div class="controls"><button data-zoom="-">缩小</button><button data-zoom="+">放大</button><button data-zoom="0">复位</button><label><input class="local" type="checkbox" checked>只看选中关系（未选显示全部）</label><span class="zoomtext">100%</span></div><div class="graph-layout"><div class="canvas"><svg role="img" aria-label="${esc(key)} 架构图"></svg></div><aside class="inspector"><h3>点击节点深入查看</h3><p>查看数据、写入者、生命周期、风险及固定提交源码。节点也可用Tab和Enter操作。</p></aside></div>`;
const svg=host.querySelector('svg'),inspector=host.querySelector('aside'),cols=g.cols,nw=230,nh=124,dx=285,dy=180,margin=35,width=cols*dx+margin*2,height=Math.ceil(g.nodes.length/cols)*dy+margin*2;
const positions=Object.fromEntries(g.nodes.map((n,i)=>[n.id,{x:margin+i%cols*dx,y:margin+Math.floor(i/cols)*dy}]));
function draw(){let edges=g.edges.filter(e=>!local||!selected||e.a===selected||e.b===selected);svg.setAttribute('viewBox',`0 0 ${width} ${height}`);svg.style.width=(zoom*100)+'%';svg.style.minWidth=(650*zoom)+'px';svg.innerHTML=`<defs><marker id="arrow-${container}" markerWidth="8" markerHeight="8" refX="7" refY="4" orient="auto"><path d="M0 0 L8 4 L0 8 Z" fill="#6d8495"/></marker></defs>`+edges.map((e,i)=>{const a=positions[e.a],b=positions[e.b];if(!a||!b)return '';const same=a.y===b.y;let x1=a.x+nw/2,y1=a.y+nh,x2=b.x+nw/2,y2=b.y;if(same){x1=a.x+nw;y1=a.y+nh/2;x2=b.x;y2=b.y+nh/2;}const lane=(i%4)*8;const d=same?`M${x1} ${y1} L${x2} ${y2}`:`M${x1} ${y1} C${x1+lane} ${(y1+y2)/2} ${x2+lane} ${(y1+y2)/2} ${x2} ${y2}`;return `<g class="edge"><path d="${d}" fill="none" stroke="#8197a7" stroke-width="1.6" ${e.style==='dash'?'stroke-dasharray="6 4"':''} marker-end="url(#arrow-${container})"/><text x="${(x1+x2)/2+4}" y="${(y1+y2)/2-4}">${esc(e.label)}</text></g>`;}).join('')+g.nodes.map(n=>{const p=positions[n.id];return `<g class="node ${selected===n.id?'selected':''}" data-id="${esc(n.id)}" role="button" tabindex="0" aria-label="${esc(n.label)}" transform="translate(${p.x} ${p.y})"><rect width="${nw}" height="${nh}" rx="9" stroke="${colors[n.kind]}" ${n.kind==='proposal'?'stroke-dasharray="6 4"':''}/><text x="13" y="27" font-size="13" font-weight="700" fill="#173348">${textLines(n.label,27).map((l,i)=>`<tspan x="13" dy="${i?18:0}">${esc(l)}</tspan>`).join('')}</text><text x="13" y="${n.label.length>27?68:54}" font-size="12" fill="#475f70">${textLines(n.sub,21).map((l,i)=>`<tspan x="13" dy="${i?18:0}">${esc(l)}</tspan>`).join('')}</text></g>`;}).join('');host.querySelector('.zoomtext').textContent=Math.round(zoom*100)+'%';}
function choose(id){selected=id;const n=g.nodes.find(n=>n.id===id);inspector.innerHTML=(n.kind==='proposal'?'<div class="callout warning">设计提案：以下关联详情解释当前问题，目标模块尚未实施。</div>':'')+cardHtml(n.detail);draw();}
host.addEventListener('click',e=>{const n=e.target.closest('.node');if(n){choose(n.dataset.id);return;}const b=e.target.closest('[data-zoom]');if(b){zoom=b.dataset.zoom==='0'?1:Math.max(.5,Math.min(2.5,zoom+(b.dataset.zoom==='+'?.25:-.25)));draw();}});
host.addEventListener('keydown',e=>{if((e.key==='Enter'||e.key===' ')&&e.target.classList.contains('node')){e.preventDefault();choose(e.target.dataset.id);}});
host.querySelector('.local').onchange=e=>{local=e.target.checked;draw();};draw();return {choose};}
graph('globalgraph','global-current');graph('ownergraph','ownership');const classView=graph('classgraph','classes');graph('targetgraph','global-target');
function search(){const q=document.getElementById('class-search').value.toLowerCase(),m=document.getElementById('module-filter').value;const c=DATA.cards.filter(c=>(!m||c.module.includes(m))&&JSON.stringify(c).toLowerCase().includes(q));document.getElementById('class-results').innerHTML=c.map(c=>`<button data-class="${c.id}">${esc(c.name)}</button>`).join('');}
document.getElementById('class-search').oninput=search;document.getElementById('module-filter').onchange=search;document.getElementById('class-results').onclick=e=>{const b=e.target.closest('[data-class]');if(b)classView.choose(b.dataset.class);};search();
function flow(prefix,key){const f=DATA.flows[key];document.getElementById(prefix+'-note').textContent=f.name+' — '+f.note;const host=document.getElementById(prefix+'-flow');host.className='flowlist';host.innerHTML=f.stages.map((s,i)=>`<button class="stage" data-index="${i}"><strong>${i+1} → ${esc(s.name)}</strong><small>${esc(s.detail)}</small></button>`).join('');function select(i){host.querySelectorAll('.stage').forEach((b,n)=>b.classList.toggle('active',i===n));const s=f.stages[i];document.getElementById(prefix+'-detail').innerHTML=`<h3>${i+1} · ${esc(s.name)}</h3><p>${esc(s.detail)}</p>${key==='target'?'<span class="pill">提案未实施；源码展示当前相关边界</span>':''}${sourceHtml(s.source)}`;}host.onclick=e=>{const b=e.target.closest('[data-index]');if(b)select(+b.dataset.index);};select(0);}
document.getElementById('simulation-mode').onchange=e=>flow('simulation',e.target.value);flow('simulation','current');document.getElementById('business-mode').onchange=e=>flow('business',e.target.value);flow('business','construction');
const scenes=['居民引用家庭住房和自己的jobId。CitySimulation.RequestCommute通过准入后创建独立TripTask；tripId引用Task ID，车辆引用同一任务，RoadAccess给站位，不增加虚假人口。','Arrive验证居民与taskId，location变为工厂，符合岗位才atWork。步末到达后从后续步计算实际劳动；没有到岗的人不能提供生产。','工厂扣原料、累计劳动完成商品；按可达车道/站位候选与原商店顺序，准入后Stock→TripTask.Shipment。Vehicle只引用任务，不复制货量；物理位置仍依附Plan待R2-B。','货车实际到达商店，容量足够才确认收货/成本/工厂收入；原外部经营资金近似保留。ACK清Shipment，任务和车同边界退休，无永久收据表，不能再次付款。'];
function scene(i){document.getElementById('scene-detail').textContent=scenes[i];const g=document.getElementById('scene-car');g.setAttribute('transform',`translate(${[0,200,350,480][i]} ${[0,-35,0,35][i]})`);}
document.querySelectorAll('[data-scene]').forEach(b=>b.onclick=()=>scene(+b.dataset.scene));document.querySelectorAll('[data-object]').forEach(n=>{const select=()=>document.getElementById('scene-object').innerHTML=cardHtml(n.dataset.object);n.onclick=select;n.onkeydown=e=>{if(e.key==='Enter')select();};});scene(0);document.getElementById('scene-object').innerHTML=cardHtml('road');
function scale(){const n=+document.getElementById('scale').value;document.getElementById('quadratic').textContent=(n*n).toLocaleString()+' 配对';document.getElementById('jobs-count').textContent=(n*n).toLocaleString()+' 比较上界';document.getElementById('local-count').textContent=n.toLocaleString()+' 次';}document.getElementById('scale').onchange=scale;scale();
document.getElementById('readings').innerHTML=DATA.readings.map(r=>`<details><summary>${esc(r.title)} · ${esc(r.source.file.split('/').pop())}</summary><p>${esc(r.why)}</p>${sourceHtml(r.source)}</details>`).join('');
document.getElementById('perf-tables').innerHTML=DATA.perf.map((p,i)=>{const fields=i===0?['scenario','active','mean_ms','p95_ms','p99_ms','allocated_bytes_per_step']:['vehicles','scenario','speed','fps','p99_frame_ms','actual_sim_rate','gc_alloc_bytes_per_frame'];return `<h3>${i===0?'已归档核心模型矩阵（RoadUpgrade）':'已归档Unity窗口（合成交通）'}</h3><p class="source-path"><a href="../../${esc(p.file)}">${esc(p.file)}</a></p><div class="table-scroll"><table><tr>${fields.map(f=>`<th>${esc(f)}</th>`).join('')}</tr>${p.rows.map(r=>`<tr>${fields.map(f=>`<td>${esc(r[f]??'—')}</td>`).join('')}</tr>`).join('')}</table></div>`;}).join('');
</script></body></html>'''
page=page.replace('__REVIEW__',markdown(review)).replace('__DECISIONS__',markdown(decisions)).replace('__ROADMAP__',markdown(roadmap)).replace('__DATA__',json.dumps(payload,ensure_ascii=False).replace('</',r'<\/'))
page=page.replace('svg.style.minWidth=(650*zoom)', 'svg.style.minWidth=(Math.max(650,width*.95)*zoom)')
edge_helper=r'''function routeEdge(a,b,i,nw,nh){
let lx,ly,d;const ax=a.x+nw/2,bx=b.x+nw/2,ay=a.y+nh/2,by=b.y+nh/2;
if(a.x===b.x){const x=a.x+nw+17+i%3*5;d=`M${a.x+nw} ${ay} H${x} V${by} H${b.x+nw}`;lx=x+3;ly=(ay+by)/2;}
else if(a.y===b.y&&Math.abs(a.x-b.x)<300){const from=b.x>a.x?a.x+nw:a.x,to=b.x>a.x?b.x:b.x+nw;d=`M${from} ${ay} H${to}`;lx=(from+to)/2;ly=ay-6;}
else if(a.y===b.y){const y=a.y-17;d=`M${ax} ${a.y} V${y} H${bx} V${b.y}`;lx=(ax+bx)/2;ly=y-5;}
else{const down=b.y>a.y,sy=down?a.y+nh:a.y,y=down?a.y+nh+23:a.y-23,x=b.x-18-i%3*4;d=`M${ax} ${sy} V${y} H${x} V${by} H${b.x}`;lx=(ax+x)/2;ly=y-5;}
return {d,lx,ly};}
'''
page=page.replace('function graph(container,key){',edge_helper+'function graph(container,key){')
old="const d=same?`M${x1} ${y1} L${x2} ${y2}`:`M${x1} ${y1} C${x1+lane} ${(y1+y2)/2} ${x2+lane} ${(y1+y2)/2} ${x2} ${y2}`;"
page=page.replace(old,'const {d,lx,ly}=routeEdge(a,b,i,nw,nh);')
page=page.replace('x="${(x1+x2)/2+4}" y="${(y1+y2)/2-4}"','x="${lx}" y="${ly}"')
page=page.replace('<details open><summary>展开 ArchitectureReview</summary>','<h3>定位证据与尚未迁移模块</h3><div id="source-evidence"></div><details open><summary>展开 ArchitectureReview</summary>')
evidence_js="document.getElementById('source-evidence').innerHTML=DATA.evidence.map(e=>`<details><summary>${esc(e.title)}</summary>${sourceHtml(e.source)}</details>`).join('');"
page=page.replace('</script></body></html>',evidence_js+'</script></body></html>')
page=page.replace('审查分支 codex/road-core-20261014 · 已推送 4a47968 · 完整游戏基线 110c4ea。未提交道路撤销通过核心检查，未接GUI，单列本地探索。','R1实现源码 0c49201 · 分支 codex/road-core-20261014 · 完整游戏基线110c4ea。撤销已独立提交0729348，尚未接GUI。')
page=page.replace('13节目录、21', '13节目录、26')
page=page.replace('推荐方案B：重划城市调度、道路、交通与业务边界，保留正确算法和领域规则。当前RoadCore也接受重新设计，不默认它是最终架构。','R1已落地：CitySimulation唯一tick与阶段、CityEntities生命周期索引、交通全提交后的业务确认。方案B继续以小范围实现和实际验证修正，不将RoadCore当最终答案。')
page=page.replace('当前Choose会修改换道影子桶，Deliver在逐车位置提交中直接改业务，不能把它描述成完全双缓冲、全阶段无副作用。目标流程改变这两处语义，须用新的安全/到达/资金测试证明正确性。','R1已把业务结算移到全物理提交之后；Choose仍顺序修改换道影子，任务与车仍混合，不是全阶段纯双缓冲。目标due队列、任务/位置分离与端口尚需R2复评。')
page=page.replace('04 · 模拟更新流程图 ★','04 · 模拟更新流程图 ★（R1实际）')
page=page.replace('10 · 推荐目标架构（设计提案，未实施）','10 · 目标架构：R1已落地节点与后续提案')
page=page.replace('图中的核心类关系以固定commit核对。所有提案节点有紫色虚线框；当前旧系统灰色未接入。撤销四个未提交文件已验证并保留，不在本次文档提交中夹带游戏修改。方向性重写等用户理解并确认总体方案后再实施。','类关系/当前所有权/实际流程以R1代码0c49201核对；提案紫色虚线、未接旧系统灰色。R1已获授权并实际验证；没有继续实施R2大重写。撤销已独立提交0729348，尚未接GUI。')
page=page.replace('核心2700与行业居民477项检查通过','核心2700与行业居民/R1共493项检查通过')
page=page.replace('没有新录制Unity全城窗口。历史window.csv','R1已重测Unity隔离交通窗口；仍没有完整市政经营窗口。历史window.csv')
page=page.replace('当前Vehicle，', '当前Vehicle，')
page=page.replace('DispatchResident；RoadAccess','CitySimulation.RequestCommute；RoadAccess')
page=page.replace('HarborCity 架构审查 · 源码基准4a47968','HarborCity R1架构 · 实现源码0c49201；初始审查4a47968')
r1=(OUT/'R1Implementation.md').read_text(encoding='utf-8')
page=page.replace('<section id="review"><h2>', '<section id="review"><details open><summary>R1实际改造、验证与剩余风险</summary><div class="doccontent">'+markdown(r1)+'</div></details><h2>')
page=page.replace('<details open><summary>展开 ArchitectureReview</summary>','<details><summary>初始架构审查历史 + R1状态标注</summary>')
page=page.replace('当前默认入口 · 精确顺序','R1默认入口 · 精确顺序')
page=page.replace('世界World私有事实副本','World私有事实副本')
page=page.replace('同步 Deliver 业务回调','输出到达后城市确认')
page=page.replace('Depart扣货或设置居民tripId；Deliver改业务；成功清cargo并移除活车','城市准入扣货或设置tripId；全物理提交后确认业务，ACK清cargo并退休')
page=page.replace('buildingLookup已有字典但Count失效不可靠；成员字典只创建时建立','R1 CityEntities维护同对象生命周期与索引；旧public列表仍需后续收紧')
page=page.replace('<td>居民出行</td><td>每步全成员扫描；重试.25秒</td>','<td>居民出行</td><td>R1每步注册成员扫描；重试.25秒</td>')
page=page.replace('未来规模属于设计方向，不是已验证产品能力；不承诺永不重构。','未来规模属于设计方向，不是已验证产品能力；R1不自动授权R2全面重写。')
page=page.replace('N个居民，J=N个岗位线性查找','旧方案：N居民×J=N岗位线性查找')
page=page.replace('一次查询最坏N×N；真实多次调用/提前命中差异大','旧方案一次查询最坏N×N；R1正常城已改为字典，N次查询平均O(N)')
page=page.replace('每步家庭成员扫描</td>','R1每步注册成员扫描</td>')
page=page.replace('只计配对，不含每F重复OfType扫描、寻路与资金查询','只计F×S配对；R1已去掉F×B重复过滤，仍有寻路/资金/准入成本')
r1rows=[]
for name in ['city-before.csv','city-after.csv']:
    with (OUT/'Results/R1'/name).open(encoding='utf-8-sig') as f:
        for row in csv.DictReader(f):r1rows.append(row)
r1fields=['version','residents','factories','shops','active_max','mean_ms','p99_ms','allocated_per_step','produced','delivered']
r1table='<h3>R1真实通勤/多企业配对（不是完整城市FPS）</h3><div class="table-scroll"><table><tr>'+''.join('<th>'+html.escape(v)+'</th>' for v in r1fields)+'</tr>'+''.join('<tr>'+''.join('<td>'+html.escape(row[v])+'</td>' for v in r1fields)+'</tr>' for row in r1rows)+'</table></div><p>同源码夹具、20步热身+200测量步。3000居民峰值1500活动车，非3000辆车；产量/交付一致。after P99约131ms、GC约6.65MB/步，物流匹配/派车/寻路仍需归因。源码0c49201，原始轻量CSV见Results/R1。</p>'
page=page.replace('<div id="perf-tables"></div>',r1table+'<div id="perf-tables"></div>')
OUT.mkdir(parents=True,exist_ok=True)
page=page.replace('HarborCity R1架构 · 实现源码0c49201；初始审查4a47968','HarborCity R1后续 / R2-A架构 · 实现源码5a3dddf；初始审查4a47968')
page=page.replace('R1实现源码 0c49201 · 分支','R2-A实现源码 5a3dddf · 分支')
page=page.replace('R1实际改造、验证与剩余风险','R1历史改造与风险（后续进展见上方R2-A）')
page=page.replace('04 · 模拟更新流程图 ★（R1实际）','04 · 模拟更新流程图 ★（R1后续 / R2-A实际）')
page=page.replace('没有继续实施R2大重写','已实施有限R2-A任务分离；独立位置/端口仍待后续批次')
page=page.replace('任务与车仍混合，不是全阶段纯双缓冲','R2-A任务已独立，物理位置仍依附Plan，不是全阶段纯双缓冲')
page=page.replace('R1不自动授权R2全面重写','R2-A已授权并完成；后续只按可验证模块推进')
r2=(OUT/'R2TaskModel.md').read_text(encoding='utf-8')
attribution=(OUT/'R1PerformanceAttribution.md').read_text(encoding='utf-8')
page=page.replace('<section id="review">','<section id="review"><details open><summary>本轮实际：性能归因与R2-A任务模型（最新）</summary><div class="doccontent">'+markdown(r2)+markdown(attribution)+'</div></details>')
b2=(OUT/'R2BImplementation.md').read_text(encoding='utf-8')
page=page.replace('<section id="review">','<section id="review"><details open><summary>最新实际：R2-B验收与转入R3</summary><div class="doccontent">'+markdown(b2)+'</div></details>')
page=page.replace('本轮实际：性能归因与R2-A任务模型（最新）','R1后续 / R2-A历史模型与性能证据')
page=page.replace('R2-A实现源码 5a3dddf','R2-B实现源码 c40bde5').replace('实现源码5a3dddf','实现源码c40bde5')
page=page.replace('R2-A任务已独立，物理位置仍依附Plan','R2-B位置和端口均已接实际调用链')
page=page.replace('任务/位置分离与端口尚需R2复评','信号/行人随R3接入，结束有限交通结构阶段')
page=page.replace('R1后续 / R2-A实际','R2-B实际')
(OUT/'HarborCityArchitecture.html').write_text(page,encoding='utf-8')
(OUT/'SourceEvidence.md').write_text('# 固定源码定位证据\n\n审查commit：`'+COMMIT+'`。以下行号由固定源码生成，而非当前工作树。\n\n'+ '\n'.join('## '+e['title']+'\n\n['+e['source']['file']+':'+str(e['source']['line'])+']('+e['source']['url']+')\n\n方法/语句：`'+e['source']['token']+'`\n' for e in evidence),encoding='utf-8')
print(f'Created {OUT / "HarborCityArchitecture.html"}; {len(cards)} class cards, {len(graphs)} graphs, {len(flowdata)} flows; pinned {COMMIT}')
