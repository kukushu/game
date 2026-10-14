# 原工业业务接入新交通（批次B）

起点fe946d0。默认入口不再创建演示Depot，使用CityCoreIndustrySession；CityModel中的原IndustrialBuilding/CommercialBuilding、FactoryState、Household/CityResident和CityJob是业务事实。CityModel旧roads/traffic为空且不参与执行，不调用旧Move/Dispatch/CitySaveData.ToCity。

```mermaid
flowchart LR
  Residents[原居民实际出勤与岗位] --> Labour[PayAttendance / ProcessFactoryWork]
  Labour --> Factory[原工厂原料 / 成品 / 现金]
  Factory --> Request[城市产生货运请求]
  Request --> Core[RoadCore路线 / 车辆 / 在途货物]
  Core --> Delivery[ITransportBusiness.Deliver]
  Delivery --> Shop[原商店库存 / 批发支出]
  Delivery --> Sale[RecordFactorySale及回执]
  Sale --> Factory
```

## 所有权与规则

- World只执行路线/占用/移动，cargo是唯一在途货物；ITransportBusiness负责发车时扣原库存、实际到达时验收及结算，交通不再内置城市资金操作。DepotBusiness只用于孤立规模测试，生产城市明确禁止AddDepot，不复制厂/店库存。
- 原四位居民已经在厂内作为初始状态；固定步根据原岗位、实际位置、在岗/出勤、工资支付计算有效劳动，调用原60劳动分钟/原料批次生产。没有工人不会生产。还没有模拟这四个人通勤进入工厂，不能将初始在岗称为通勤迁移。
- 初始24原料预付192，工厂资本1200、余额1008，沿用原加工成本2和售价45。CityRecordFactorySale沿用原买方简化外部经营资金支付规则，新增商业wholesaleCosts记录真实交付支出，不用政府资金冒充商店现金。没有新增贷款/股东/复杂企业经济。
- 可达且入口有空间后才扣源库存；入货容量按实际在途任务汇总。到达满仓则保留车及货等待，不沿用旧溢出丢货规则；卸货、卖方收入、买方成本和保存回执一次提交。财务回调内不执行可能失败的日志/分析订阅。
- 出发地或目的地关闭会中断货运，保留在途货物和世界位置。CloseBuilding把原企业实体移至封存账目，累计原工资在retiredFactoryWages中继续对账；不是完整拆楼损失/企业清算玩法。车的返程、回收、重新寻路恢复尚未实现。
- 世界时钟唯一驱动固定步；城市日历是从该时钟派生的缓存。生产与调度在步前，交通执行及交付随后，日历步后同步。原居民/住房日结、需求增长、税收、市政等还没有运行。
- 新组合存档harbor-city-core-v1.hcity：明确魔数/版本/校验，保存具体建筑DTO、原成员/岗位/劳动/资金、回执、时钟余量和新交通。二进制保留float/double位值，不用旧存档加载器；字段schema变化明确拒绝。原harbor-city.json不修改。核心独立测试格式升级2，旧演示格式1拒绝。

## 验证和范围

- `Tools/VerifyCityCore.ps1`：241项工业集成断言；原劳动产出、真实发货和距离移动、货物/现金守恒、满仓等待/恢复、不可达不扣库存、断路/建筑关闭保货、新组合保存后220步逐步字节一致、快照隔离。
- `Tools/VerifyRoadCore.ps1`：503项内核检查及原1000/3000活动车矩阵继续通过；业务端点检查增加少量O(1)索引工作，约1ms/3000车，不出现逐车扫描所有任务。
- `Tools/Verify.ps1`：保留模块完整回归通过，不代表原市政/居民完整游戏已接回。
- `Tools/RunRoadCoreNative.ps1 -City`：正常场景默认新工业入口编译/运行，原商品生产及到达、现金/库存关系、保存继续一致；跨实际Unity帧核对渲染车辆transform与新内核位置。原生结果与截图见Benchmarks/2026-10-14-CityCoreIndustry。
- 新城市小场景（2道路/3节点/4居民，2–5活动货车）完整工业+交通模型步约0.005ms、约2.5KB/步；它不是完整旧城，不宣称和旧7–8ms配对加速。没有测全城市FPS或1000个工厂。仍缺原地形/UI/分区、居民出行、市政、原料进口与消费，原料用完后自然停产，商店不自动消耗库存。

下一批C：建筑道路中途访问点、首尾距离范围、安全插入与拆分迁移；先迁移这条真实业务，不新增每楼路口。之后再把编辑迁移收拢成统一事务、多车道和连接冲突。
