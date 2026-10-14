# 固定源码定位证据

审查commit：`4a4796825f43462f65ee99eb2531656bd1a2678b`。以下行号由固定源码生成，而非当前工作树。

## P0 时钟与同步结算

[Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs:294](https://github.com/kukushu/game/blob/4a4796825f43462f65ee99eb2531656bd1a2678b/Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs#L294)

方法/语句：`public void Step()`

## P0 物理位置依附路线

[Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs:36](https://github.com/kukushu/game/blob/4a4796825f43462f65ee99eb2531656bd1a2678b/Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs#L36)

方法/语句：`public long Current=>route[cursor]`

## P1 建筑索引已有，但Count失效不充分

[Assets/Scripts/Runtime/Simulation/Core/CityBuildings.cs:95](https://github.com/kukushu/game/blob/4a4796825f43462f65ee99eb2531656bd1a2678b/Assets/Scripts/Runtime/Simulation/Core/CityBuildings.cs#L95)

方法/语句：`if(buildingLookup==null`

## P1 岗位仍是线性查找

[Assets/Scripts/Runtime/Simulation/Residents/CityJobs.cs:14](https://github.com/kukushu/game/blob/4a4796825f43462f65ee99eb2531656bd1a2678b/Assets/Scripts/Runtime/Simulation/Residents/CityJobs.cs#L14)

方法/语句：`public CityJob Job(int id)`

## P1 成员索引生命周期仅构造

[Assets/Scripts/Runtime/Simulation/Residents/CityCoreResidents.cs:20](https://github.com/kukushu/game/blob/4a4796825f43462f65ee99eb2531656bd1a2678b/Assets/Scripts/Runtime/Simulation/Residents/CityCoreResidents.cs#L20)

方法/语句：`members.Add(person.id`

## P1 商店过滤嵌在每个工厂循环

[Assets/Scripts/Runtime/Simulation/Industry/CityCoreIndustrySession.cs:98](https://github.com/kukushu/game/blob/4a4796825f43462f65ee99eb2531656bd1a2678b/Assets/Scripts/Runtime/Simulation/Industry/CityCoreIndustrySession.cs#L98)

方法/语句：`foreach(var factory in City.buildings.OfType`

## P1 新货车环境暴露缺失：读旧任务

[Assets/Scripts/Runtime/Simulation/Industry/CityIndustry.cs:149](https://github.com/kukushu/game/blob/4a4796825f43462f65ee99eb2531656bd1a2678b/Assets/Scripts/Runtime/Simulation/Industry/CityIndustry.cs#L149)

方法/语句：`foreach(var t in traffic.trips)`

## P1 完成收据无限保留

[Assets/Scripts/Runtime/Simulation/Industry/CityCoreIndustrySession.cs:25](https://github.com/kukushu/game/blob/4a4796825f43462f65ee99eb2531656bd1a2678b/Assets/Scripts/Runtime/Simulation/Industry/CityCoreIndustrySession.cs#L25)

方法/语句：`readonly HashSet<int> receipts`

## P1 中心短连接影响冲突几何

[Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreNetwork.cs:163](https://github.com/kukushu/game/blob/4a4796825f43462f65ee99eb2531656bd1a2678b/Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreNetwork.cs#L163)

方法/语句：`Point p=a.Geometry.Sample`

## P2 全量施工候选与提交

[Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs:161](https://github.com/kukushu/game/blob/4a4796825f43462f65ee99eb2531656bd1a2678b/Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs#L161)

方法/语句：`internal bool TryCommit`

## P2 局部连接冲突两两构建

[Assets/Scripts/Runtime/Traffic/CityRoadCoreReservations.cs:45](https://github.com/kukushu/game/blob/4a4796825f43462f65ee99eb2531656bd1a2678b/Assets/Scripts/Runtime/Traffic/CityRoadCoreReservations.cs#L45)

方法/语句：`foreach(var bucket in local.Values)`

## P2 保存协议依赖public字段布局

[Assets/Scripts/Runtime/Simulation/Core/CityCoreSave.cs:18](https://github.com/kukushu/game/blob/4a4796825f43462f65ee99eb2531656bd1a2678b/Assets/Scripts/Runtime/Simulation/Core/CityCoreSave.cs#L18)

方法/语句：`static FieldInfo[] Fields`

## 模块：旧分区规划世界坐标键

[Assets/Scripts/Runtime/Infrastructure/Zoning/CityZoning.cs:14](https://github.com/kukushu/game/blob/4a4796825f43462f65ee99eb2531656bd1a2678b/Assets/Scripts/Runtime/Infrastructure/Zoning/CityZoning.cs#L14)

方法/语句：`public sealed class CityZoningState`

## 模块：旧地图参数而非渲染事实

[Assets/Scripts/Runtime/Infrastructure/Map/CityMap.cs:6](https://github.com/kukushu/game/blob/4a4796825f43462f65ee99eb2531656bd1a2678b/Assets/Scripts/Runtime/Infrastructure/Map/CityMap.cs#L6)

方法/语句：`public sealed class CityMapState`

## 模块：旧供给有建筑桶，但接线遍历管网边

[Assets/Scripts/Runtime/Infrastructure/Utilities/CityUtilities.cs:88](https://github.com/kukushu/game/blob/4a4796825f43462f65ee99eb2531656bd1a2678b/Assets/Scripts/Runtime/Infrastructure/Utilities/CityUtilities.cs#L88)

方法/语句：`public void RecalculateUtilities()`

## 模块：焚烧处理和垃圾守恒，读取旧trips

[Assets/Scripts/Runtime/Simulation/Services/CityWaste.cs:29](https://github.com/kukushu/game/blob/4a4796825f43462f65ee99eb2531656bd1a2678b/Assets/Scripts/Runtime/Simulation/Services/CityWaste.cs#L29)

方法/语句：`public long WasteBalanceError`

## 模块：消防交通业务在旧CityTraffic partial

[Assets/Scripts/Runtime/Simulation/Services/CityFire.cs:83](https://github.com/kukushu/game/blob/4a4796825f43462f65ee99eb2531656bd1a2678b/Assets/Scripts/Runtime/Simulation/Services/CityFire.cs#L83)

方法/语句：`void ScheduleFire()`

## 模块：警务交通业务在旧CityTraffic partial

[Assets/Scripts/Runtime/Simulation/Services/CityPolice.cs:75](https://github.com/kukushu/game/blob/4a4796825f43462f65ee99eb2531656bd1a2678b/Assets/Scripts/Runtime/Simulation/Services/CityPolice.cs#L75)

方法/语句：`void SchedulePolice()`

## 模块：医疗到达在旧TrafficTrip协议

[Assets/Scripts/Runtime/Simulation/Services/CityHealthcare.cs:69](https://github.com/kukushu/game/blob/4a4796825f43462f65ee99eb2531656bd1a2678b/Assets/Scripts/Runtime/Simulation/Services/CityHealthcare.cs#L69)

方法/语句：`void ArriveHealthcare`

## 模块：旧城市贷款日结

[Assets/Scripts/Runtime/Simulation/Economy/CityLoans.cs:64](https://github.com/kukushu/game/blob/4a4796825f43462f65ee99eb2531656bd1a2678b/Assets/Scripts/Runtime/Simulation/Economy/CityLoans.cs#L64)

方法/语句：`public int SettleLoans()`

## 模块：观测器不应写事实，却仍读旧交通

[Assets/Scripts/Runtime/Observability/CityDailyAnalysis.cs:135](https://github.com/kukushu/game/blob/4a4796825f43462f65ee99eb2531656bd1a2678b/Assets/Scripts/Runtime/Observability/CityDailyAnalysis.cs#L135)

方法/语句：`public void ObserveTraffic()`

## 模块：生长与日结依赖旧clock/roads

[Assets/Scripts/Runtime/Simulation/Growth/CityDevelopment.cs:129](https://github.com/kukushu/game/blob/4a4796825f43462f65ee99eb2531656bd1a2678b/Assets/Scripts/Runtime/Simulation/Growth/CityDevelopment.cs#L129)

方法/语句：`void BaselineTick()`
