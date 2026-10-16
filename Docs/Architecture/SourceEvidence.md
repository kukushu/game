# 固定源码定位证据

审查commit：`156737e`。以下行号由固定源码生成，而非当前工作树。

## R1 已改：正常城市不再调用此独立Step

[Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs:464](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs#L464)

方法/语句：`public void Step()`

## R2-B 物理位置已独立

[Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs:32](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs#L32)

方法/语句：`public long Current=>Position.Drivable`

## R1 已改：城市结构索引，旧入口Count缓存后备

[Assets/Scripts/Runtime/Simulation/Core/CityBuildings.cs:98](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Core/CityBuildings.cs#L98)

方法/语句：`if(buildingLookup==null`

## R1 已改：城市岗位字典，旧入口Find后备

[Assets/Scripts/Runtime/Simulation/Residents/CityJobs.cs:14](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Residents/CityJobs.cs#L14)

方法/语句：`public CityJob Job(int id)`

## P1 新货车环境暴露缺失：读旧任务

[Assets/Scripts/Runtime/Simulation/Industry/CityIndustry.cs:154](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Industry/CityIndustry.cs#L154)

方法/语句：`foreach(var t in traffic.trips)`

## R2-B 真实端口与共享曲线

[Assets/Scripts/Runtime/Infrastructure/Roads/CityLaneConnection.cs:28](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Infrastructure/Roads/CityLaneConnection.cs#L28)

方法/语句：`static Point[] Curve`

## P2 全量施工候选与提交

[Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs:214](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs#L214)

方法/语句：`internal bool TryCommit`

## P2 局部连接冲突两两构建

[Assets/Scripts/Runtime/Traffic/CityRoadCoreReservations.cs:45](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Traffic/CityRoadCoreReservations.cs#L45)

方法/语句：`foreach(var bucket in local.Values)`

## P2 保存协议依赖public字段布局

[Assets/Scripts/Runtime/Simulation/Core/CityCoreSave.cs:18](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Core/CityCoreSave.cs#L18)

方法/语句：`static FieldInfo[] Fields`

## 模块：旧分区规划世界坐标键

[Assets/Scripts/Runtime/Infrastructure/Zoning/CityZoning.cs:14](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Infrastructure/Zoning/CityZoning.cs#L14)

方法/语句：`public sealed class CityZoningState`

## 模块：旧地图参数而非渲染事实

[Assets/Scripts/Runtime/Infrastructure/Map/CityMap.cs:7](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Infrastructure/Map/CityMap.cs#L7)

方法/语句：`public sealed class CityMapState`

## 模块：旧供给有建筑桶，但接线遍历管网边

[Assets/Scripts/Runtime/Infrastructure/Utilities/CityUtilities.cs:89](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Infrastructure/Utilities/CityUtilities.cs#L89)

方法/语句：`public void RecalculateUtilities()`

## 模块：焚烧处理和垃圾守恒，读取旧trips

[Assets/Scripts/Runtime/Simulation/Services/CityWaste.cs:29](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Services/CityWaste.cs#L29)

方法/语句：`public long WasteBalanceError`

## 模块：消防交通业务在旧CityTraffic partial

[Assets/Scripts/Runtime/Simulation/Services/CityFire.cs:83](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Services/CityFire.cs#L83)

方法/语句：`void ScheduleFire()`

## 模块：警务交通业务在旧CityTraffic partial

[Assets/Scripts/Runtime/Simulation/Services/CityPolice.cs:75](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Services/CityPolice.cs#L75)

方法/语句：`void SchedulePolice()`

## 模块：医疗到达在旧TrafficTrip协议

[Assets/Scripts/Runtime/Simulation/Services/CityHealthcare.cs:69](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Services/CityHealthcare.cs#L69)

方法/语句：`void ArriveHealthcare`

## 模块：旧城市贷款日结

[Assets/Scripts/Runtime/Simulation/Economy/CityLoans.cs:62](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Economy/CityLoans.cs#L62)

方法/语句：`public int SettleLoans()`

## 模块：观测器不应写事实，却仍读旧交通

[Assets/Scripts/Runtime/Observability/CityDailyAnalysis.cs:135](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Observability/CityDailyAnalysis.cs#L135)

方法/语句：`public void ObserveTraffic()`

## 模块：生长与日结依赖旧clock/roads

[Assets/Scripts/Runtime/Simulation/Growth/CityDevelopment.cs:181](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Growth/CityDevelopment.cs#L181)

方法/语句：`void BaselineTick()`

## R1 已实现：成员生命周期

[Assets/Scripts/Runtime/Simulation/Core/CityEntities.cs:81](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Core/CityEntities.cs#L81)

方法/语句：`public void AddResident`

## R1 剩余风险：物流F*S

[Assets/Scripts/Runtime/Simulation/Industry/CityLogisticsSystem.cs:27](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Industry/CityLogisticsSystem.cs#L27)

方法/语句：`foreach(var factory in entities.Factories)`

## R1 已实现：物理后结果结算

[Assets/Scripts/Runtime/Simulation/Core/CitySimulation.cs:146](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Core/CitySimulation.cs#L146)

方法/语句：`foreach(var arrival in transport.Arrivals)`

## R1 已实现：唯一tick/欠步

[Assets/Scripts/Runtime/Simulation/Core/CitySimulationClock.cs:4](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Core/CitySimulationClock.cs#L4)

方法/语句：`public sealed class SimulationClock`

## CityCoreZoning

[Assets/Scripts/Runtime/Infrastructure/Zoning/CityCoreZoning.cs:9](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Infrastructure/Zoning/CityCoreZoning.cs#L9)

方法/语句：`public sealed class CityCoreZoning`

## CityCoreGrowth

[Assets/Scripts/Runtime/Simulation/Growth/CityCoreGrowth.cs:6](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Growth/CityCoreGrowth.cs#L6)

方法/语句：`public sealed class CityCoreGrowth`

## CityCoreConstruction

[Assets/Scripts/Runtime/Simulation/Core/CityCoreConstruction.cs:5](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Core/CityCoreConstruction.cs#L5)

方法/语句：`public sealed class CityCoreConstruction`

## CityCoreUtilities

[Assets/Scripts/Runtime/Simulation/Services/CityCoreUtilities.cs:7](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Services/CityCoreUtilities.cs#L7)

方法/语句：`public sealed class CityCoreUtilities`

## CityCorePopulation

[Assets/Scripts/Runtime/Simulation/Residents/CityCorePopulation.cs:9](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Residents/CityCorePopulation.cs#L9)

方法/语句：`public sealed class CityCorePopulation`

## CityCoreTrade

[Assets/Scripts/Runtime/Simulation/Economy/CityCoreTrade.cs:57](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Economy/CityCoreTrade.cs#L57)

方法/语句：`public sealed class CityCoreTrade`

## CityTradeState

[Assets/Scripts/Runtime/Simulation/Economy/CityCoreTrade.cs:27](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Economy/CityCoreTrade.cs#L27)

方法/语句：`public sealed class CityTradeState`

## CityOutsideConnection

[Assets/Scripts/Runtime/Simulation/Economy/CityCoreTrade.cs:8](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Economy/CityCoreTrade.cs#L8)

方法/语句：`public sealed class CityOutsideConnection`

## CityGameDesign / EconomyRules

[Assets/Scripts/Runtime/GameData/CityGameDesign.cs:52](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/GameData/CityGameDesign.cs#L52)

方法/语句：`public sealed class CityGameDesign`

## CityLocalization

[Assets/Scripts/Runtime/GameData/CityLocalization.cs:9](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/GameData/CityLocalization.cs#L9)

方法/语句：`public sealed class CityLocalization`

## HarborCityRoadCoreGame

[Assets/Scripts/Runtime/Presentation/Game/HarborCityRoadCoreGame.cs:13](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Presentation/Game/HarborCityRoadCoreGame.cs#L13)

方法/语句：`public sealed class HarborCityRoadCoreGame`

## CityCorePayroll

[Assets/Scripts/Runtime/Simulation/Economy/CityCorePayroll.cs:7](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Economy/CityCorePayroll.cs#L7)

方法/语句：`public sealed class CityCorePayroll`

## CityCoreConsumption

[Assets/Scripts/Runtime/Simulation/Economy/CityCoreConsumption.cs:17](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Economy/CityCoreConsumption.cs#L17)

方法/语句：`public sealed class CityCoreConsumption`

## CityConsumptionState

[Assets/Scripts/Runtime/Simulation/Economy/CityCoreConsumption.cs:8](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Economy/CityCoreConsumption.cs#L8)

方法/语句：`public sealed class CityConsumptionState`

## CityShopAccount

[Assets/Scripts/Runtime/Simulation/Economy/CityShopAccount.cs:8](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Economy/CityShopAccount.cs#L8)

方法/语句：`public sealed class CityShopAccount`

## CityRetailIndex

[Assets/Scripts/Runtime/Simulation/Economy/CityRetailIndex.cs:7](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Economy/CityRetailIndex.cs#L7)

方法/语句：`internal sealed class CityRetailIndex`

## 增量建筑视图

[Assets/Scripts/Runtime/Presentation/World/HarborCityBuildingViews.cs:32](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Presentation/World/HarborCityBuildingViews.cs#L32)

方法/语句：`public void Sync`

## 生长尖峰与分配采样

[Assets/Scripts/Runtime/Infrastructure/Zoning/CityCoreZoning.cs:101](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Infrastructure/Zoning/CityCoreZoning.cs#L101)

方法/语句：`void RebuildGeometry()`

## 占用不再重建几何

[Assets/Scripts/Runtime/Infrastructure/Zoning/CityCoreZoning.cs:147](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Infrastructure/Zoning/CityCoreZoning.cs#L147)

方法/语句：`void RefreshOccupancy()`

## CityCoreFinance

[Assets/Scripts/Runtime/Simulation/Economy/CityCoreFinance.cs:15](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Economy/CityCoreFinance.cs#L15)

方法/语句：`public sealed class CityCoreFinance`

## TreasuryData

[Assets/Scripts/Runtime/Simulation/Economy/CityCoreFinance.cs:8](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Economy/CityCoreFinance.cs#L8)

方法/语句：`public sealed class TreasuryData`

## CityLoan / CoreLoanData

[Assets/Scripts/Runtime/Simulation/Economy/CityLoans.cs:16](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Economy/CityLoans.cs#L16)

方法/语句：`[Serializable] public sealed class CityLoan`

## FinanceRules / LoanTerms

[Assets/Scripts/Runtime/GameData/CityFinanceDesign.cs:25](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/GameData/CityFinanceDesign.cs#L25)

方法/语句：`public sealed class FinanceRules`

## HarborCityFinancePanel

[Assets/Scripts/Runtime/Presentation/UI/HarborCityFinancePanel.cs:6](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Presentation/UI/HarborCityFinancePanel.cs#L6)

方法/语句：`public sealed class HarborCityFinancePanel`

## CityCoreWaste

[Assets/Scripts/Runtime/Simulation/Services/CityCoreWaste.cs:22](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Services/CityCoreWaste.cs#L22)

方法/语句：`public sealed class CityCoreWaste`

## WasteServiceData

[Assets/Scripts/Runtime/Simulation/Services/CityCoreWaste.cs:13](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Services/CityCoreWaste.cs#L13)

方法/语句：`public sealed class WasteServiceData`

## WasteMission

[Assets/Scripts/Runtime/Simulation/Services/CityCoreWaste.cs:7](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Services/CityCoreWaste.cs#L7)

方法/语句：`public sealed class WasteMission`

## WasteRules

[Assets/Scripts/Runtime/GameData/CityWasteDesign.cs:10](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/GameData/CityWasteDesign.cs#L10)

方法/语句：`public sealed class WasteRules`

## ServiceContinuation

[Assets/Scripts/Runtime/Traffic/CityTransportBoundary.cs:13](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Traffic/CityTransportBoundary.cs#L13)

方法/语句：`internal sealed class ServiceContinuation`

## CityCoreObservation

[Assets/Scripts/Runtime/Observability/CityCoreObservation.cs:30](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Observability/CityCoreObservation.cs#L30)

方法/语句：`public sealed class CityCoreObservation`

## BuildingInspection / FamilyInspection / ResidentInspection

[Assets/Scripts/Runtime/Observability/CityCoreObservation.cs:17](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Observability/CityCoreObservation.cs#L17)

方法/语句：`public sealed class BuildingInspection`

## HarborCityObservationPanel

[Assets/Scripts/Runtime/Presentation/UI/HarborCityObservationPanel.cs:7](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Presentation/UI/HarborCityObservationPanel.cs#L7)

方法/语句：`public sealed class HarborCityObservationPanel`

## HarborCityRoadEditor

[Assets/Scripts/Runtime/Presentation/UI/HarborCityRoadEditor.cs:8](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Presentation/UI/HarborCityRoadEditor.cs#L8)

方法/语句：`public sealed class HarborCityRoadEditor`

## CityRoadClearance

[Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadClearance.cs:8](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadClearance.cs#L8)

方法/语句：`internal static class CityRoadClearance`

## 已实现：临时水电覆盖索引

[Assets/Scripts/Runtime/Infrastructure/Utilities/CityUtilityCoverage.cs:8](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Infrastructure/Utilities/CityUtilityCoverage.cs#L8)

方法/语句：`internal sealed class CityUtilityCoverage`

## 已实现：居民ETA生命周期

[Assets/Scripts/Runtime/Simulation/Residents/CityCoreResidents.cs:27](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Residents/CityCoreResidents.cs#L27)

方法/语句：`double EstimateCommute`

## 已实现：实时入口与可用性守卫

[Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs:391](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs#L391)

方法/语句：`public bool HasResidentAccess`

## Actual: SCC lane reachability and bounded DAG cache

[Assets/Scripts/Runtime/Infrastructure/Roads/CityLaneReachability.cs:7](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Infrastructure/Roads/CityLaneReachability.cs#L7)

方法/语句：`internal sealed class LaneReachability`

## 实际表现：HarborCityEmploymentReadout

[Assets/Scripts/Runtime/Presentation/UI/HarborCityEmploymentReadout.cs:7](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Presentation/UI/HarborCityEmploymentReadout.cs#L7)

方法/语句：`sealed class HarborCityEmploymentReadout`

## 实际表现：HarborCityOutsideViews

[Assets/Scripts/Runtime/Presentation/World/HarborCityOutsideViews.cs:8](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Presentation/World/HarborCityOutsideViews.cs#L8)

方法/语句：`sealed class HarborCityOutsideViews`

## 实际性能修正：按真实家庭和岗位线性汇总发展需求

[Assets/Scripts/Runtime/Simulation/Growth/CityDevelopment.cs:41](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Simulation/Growth/CityDevelopment.cs#L41)

方法/语句：`public void DevelopmentDemand`

## 实际：查询结果独立的寻路工作空间

[Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreNetwork.cs:233](https://github.com/kukushu/game/blob/156737e/Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreNetwork.cs#L233)

方法/语句：`sealed class RouteScratch`
