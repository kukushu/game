# 居民、岗位与家庭关系（实体模型）

本次沿用 CityResident 表示 Citizen，不另建第二份人口模型。不新增 UI 或教育、婚姻等玩法。

## 数据与统计

- Building → CityJob：`society.jobEntities` 保存 id、buildingId、wage、requiredSkill、occupiedCitizenId。nextJobId 单调递增，空岗也是真实岗位。
- CityResident → CityJob：jobId 与 occupiedCitizenId 双向对应；一个人至多一个岗位，一个岗位至多一个人。个人保存 householdId、年龄、canWork、skill、出勤分钟、实际已挣工资、日末入账工资、实际通勤观测和行程。
- CityResident → Household：家庭 people 列表是成员集合，householdId 是反向引用；people 是成员集合的唯一来源。
- Household → Housing：保留 home（buildingId）+ unit、租约、储蓄、搬家、住房偏好和候选评分。

population = resident 家庭中真实 people 数量，城外申请家庭不算本城人口。jobs = jobEntities.Count。Employed / Unemployed 分别统计本城有劳动能力、有有效岗位 / 无岗位的个人。EmployedAt 从该建筑已占用岗位汇总。Occupancy 从实际住户与有效住房编号汇总，不按建筑等级推断入住。

AverageCommute 是当前就业人员最近一次实际到岗耗时的均值（包括入口等待），只采用与当前家和工作地点匹配的观测；尚未实际到岗者不计入分母，没有样本时为 0。不是所有历史行程的均值。住房评估把每名劳动者的预计通勤相加；已有匹配观测时使用不低于畅通估计的实际耗时，道路修改后重新估计。

家庭日收入由每位成员的实际工资入账汇总。商业 earnedWages 按原规则结算，工厂工资先从雇主现金扣除并进入个人 factoryWageCredit，日末整元转入家庭储蓄，余数留存。只有真实到达工作地点后的在岗时间才能积累工资；岗位合同日薪用于住房预算预测，不直接发钱。CityModel.income 仍表示政府上一结算日实际收租，不是居民工资。日历史是结算快照，宏观字段为派生缓存，不反向创建人口或岗位。

## 岗位生命周期与选择

商业/工业按现有每级 4 个岗位的配置生成有限实体，断路不删除岗位。SyncJobs 保留已有 ID，增加容量时补充岗位，减少容量优先删空岗，然后释放受影响员工；建筑拆除释放全部岗位，已挣工资保留。迁出和换工作释放原岗位。

求职沿用家庭评估周期；按技能优先为成员逐个比较可达空岗和自身岗位，依据工资、通勤和换工作成本选择。提交时重新检查独占关系。成员可在不同建筑工作，也可部分就业。住房评分汇总这些个人计划的工资和通勤，仍使用原租金、空间、私密性、储蓄、租约与搬家门槛。当前为确定性顺序启发式分配，不穷举全家所有工作组合，不保证全局最优。

出行或上班期间延后正常换工作和搬家。断路保留劳动关系、实际车辆与等待状态，无法到岗不产生工资；拆除雇主后原行程按既有返家逻辑处理。

## 建筑身份与保存

建筑使用稳定 `buildingId`，通过 `GetBuilding()` 查询具体子类；List 位置不代表身份。住宅容量来自 `ResidentialBuilding`，商业/工业子类提供岗位，库存属于对应实体。拆除真实移除建筑并清理活动家庭、岗位与交通端点；历史评分及日志编号仍作为过去的证据保留。

保存格式为 8，仅支持新格式，详见 [建筑架构](BuildingArchitecture.md)。旧 Household.members/skill/work/commute、CityResident.worker 以及迁移启用标志已删除。

## 仍然简化的数值

岗位数量模板、岗位初始工资/技能门槛、申请家庭生成、年龄与初始技能、成年劳动能力、八小时班次、地图时间换算仍为原型配置。供水供电是建筑容量汇总，幸福度是公式，需求指标目前按家庭计数。生活费、通勤费、维护费、租金调节幅度和住房效用权重仍按规则计算。工业工资由工厂现金承担，经营收入来自实际商品交付；商业工资及采购暂用外部经营资金。实际关系已落到实体上，不代表这些参数已形成完整经济闭环。

检查入口：`Tools/Verify.ps1`；Unity 菜单 `Harbor → Validate entity architecture and native saves`，验证真实 JsonUtility 往返及当前实体模型续算。
