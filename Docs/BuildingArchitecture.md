# 建筑实体架构（存档格式 8）

本文描述当前实现。后续一代复刻规划见 [开发路线](ProductRoadmap.md)；独立建筑实体和稳定 ID 继续保留，分区是新增规划数据，不恢复旧 tile 建筑数据库。

当前运行时只有一份建筑实体数据库：`CityModel.buildings`。一栋建筑只有一种用途，其具体 C# 类型和只读 `Use` 是用途的权威来源。住房、岗位、生产、交通继续由各自系统推进，建筑类没有模拟 `Tick()` 虚函数。

## 实体与状态

| 类型 | 保存的数据 |
|---|---|
| `CityBuilding`（抽象基类） | `id`、`x/z/yaw`、`width/depth`、`entranceX/entranceZ`、`level`；提供 `Point/Contains/Overlaps/HitsRoad` 空间能力 |
| `ResidentialBuilding` | 公寓/别墅类型、住房容量、挂牌租金、空置天数、申请与意向家庭、实际重型车流暴露 |
| `CommercialBuilding` | 商业商品库存 `stock` |
| `IndustrialBuilding` | 成品库存 `goodsStock`，以及 `FactoryState` 原料、在制批次、劳动、产销、现金、收支、噪声与污染 |
| `PowerBuilding / WaterBuilding / ParkBuilding` | 使用共有空间数据和各自固定用途，目前没有额外业务字段 |

`level` 保留在建筑上，岗位和公共服务容量仍使用它；当前建造默认一级，没有旧版自动升级逻辑。`IGoodsBuilding.Stock` 只是对商业/工业库存的统一访问接口，不保存第二份库存。

## 身份与关系

`nextBuildingId` 单调递增；建设通过 `PlaceBuilding()` 分配 ID。拆除会从 List 真正移除对象，ID 不回收。`GetBuilding(id)` 使用运行时字典查询；该缓存不进入存档。修改建筑集合的正常入口使缓存失效，List 顺序不构成实体身份。回归夹具故意从 101 开始使用稀疏 ID，并重排集合来检查关系。

```text
Household.home ───────────────→ ResidentialBuilding.id
Household.people → CityResident.jobId → CityJob.id
                                      CityJob.buildingId → 工作建筑.id
TrafficTrip.origin/destination/home ──→ CityBuilding.id（-1 表示城外）
IndustrialBuilding.factory ───────────→ FactoryState
```

关系保存整数 ID，使用时再查询实体。住房容量和岗位生成直接判断实际子类。家庭候选评分、历史日报和事件里的旧 ID 是历史证据，实体拆除后仍可保留；活动住所、岗位、居民位置、行程端点不会继续指向被拆除实体。

## 建造、道路与拆除

道路仍由原有 `RoadNode/RoadEdge` 表示，保留直线、二次贝塞尔采样、交叉切分及车辆进度转换。鼠标投影到最近 edge，`RoadsidePreview()` 创建所选子类；中心偏移为道路半宽 + 建筑半深度 + 0.25，局部 `-Z` 正面朝路，入口位于该正面中央。最大吸附距离 10。`CanBuild()` 继续判断边界、水面、坡度、道路及建筑重叠，`PlaceBuilding()` 提交时再次检查。`AccessBuilding()` 继续使用真实 frontage 接入路网。

拆除时先处理相关运输，再移除建筑：未交付货物退回存在的供应方或记为损失；工厂原料、成品和在制品计入损失；失去住房的家庭明确离城待评估并释放岗位，取消其行程；失去雇主的居民释放岗位并尝试沿路返家。已从工厂付给居民的工资余额保留。

工厂对象移除后，只保留 `retiredFactoryWages` 工资核对总量，以及有位置的残留环境强度；后者随模拟衰减。原工厂完整经营记录保存在审计事件和当日日报中，不留空壳建筑或旧 ID 洞位。

## 新存档

保存路径仍为 Unity 持久化目录下的 `harbor-city.json`，格式号为 8；旧格式直接拒绝，失败时保留当前城市。运行时抽象 List 不直接交给 `JsonUtility`。

`CityModel.ToSaveData()` 生成 `CitySaveData`。每个 `BuildingSaveData` 保存类型标签、共有 pose、商品库存，以及明确的住宅/工业 payload。专属 payload 使用空或单元素数组：这是为了避开 Unity 把 `null` 嵌套对象序列化为默认对象的行为；零个表示不适用，一个表示存在。加载先检查标签与 payload 一致，再按标签创建具体子类、恢复数据、校验唯一 ID 和活动关系，最后重建派生缓存。不会自动补工厂现金。

城市快照、日志 baseline/latest、F3 导出和游戏保存统一使用这个 DTO。家庭、居民、岗位、路网、在途货物、加工进度及日内时间一起保存。

## 删除的历史结构

已删除 `tiles[]`、`levels[]`、`TrafficState.stock[]`、`nextCommute[]`、`nextShopping[]`、旧生产定时器、grid 建造/碰撞/道路导入、`RoadsideLots()`、旧城市样例和各阶段 `Enable*` 迁移入口。删除了旧家庭成员/工作兼容字段、worker 与 transport 启用标志，以及代表性住宅通勤/购物调度。当前每个客运行程对应真实居民。

## 主要文件与验证

- `CityBuildings.cs`：实体继承、ID 查询、库存入口、连续建造及拆除。
- `CityModel.cs / CitySaveData.cs`：城市根数据与显式存档边界。
- `CityHouseholds.cs / CityResidents.cs / CityJobs.cs / CityResidentTransport.cs`：类型化住房及稳定 ID 关系。
- `CityIndustry.cs / CityTraffic.cs / CityRoads.cs`：实体库存、真实经营与运输，移除旧索引结构。
- `CityDailyAnalysis.cs / CitySimulationLog.cs`：类型识别、拆除当日日报和 DTO 快照。
- `HarborCityGame.cs` 及建筑、道路、居民、通勤、观察台分文件：建造、视觉和 UI 查询。
- `Tools/TestCity.cs / EntityChecks.cs` 和各回归套件、`Tools/Verify.ps1`：当前世界夹具、稀疏 ID、全年度模拟、现金守恒及存档。
- `Assets/Editor/City*Checks.cs`：Unity 原生 DTO 往返、确定性续算、曲路与 typed preview；不再执行旧档迁移测试。

命令行验证：`./Tools/Verify.ps1`。Unity 菜单：`Harbor → Validate entity architecture and native saves`。原生检查使用 Unity 自身的 `JsonUtility`，独立于命令行测试的序列化器。Play 模式中的建筑视觉和地形检查仍需要实际运行城市。

原生往返及续算逐字段比较，整数、字符串、枚举与 float 要求相等；JsonUtility 的 double 解析可能改变最后一个二进制位，double 比较仅允许小于 `1e-8` 的绝对误差，资金守恒校验仍独立执行。
