# MS-014：生产阶段分项调查（进行中）

沿原30项清单，基于MS013，不重做已集成的稀疏在岗循环，也不修改工资、生产、污染或库存规则。

## 已确认调用链

`CitySimulation.Step → CityProductionSystem.Step`：

1. `CityCorePayroll.BeginStep()`。
2. 计算原居民分钟/固定步分钟。
3. 正常模式枚举 `entities.WorkingMembers()`，逐成员 `Produce`；有诊断 `logSink` 时原动态 Members 枚举仍保留。
4. `Produce` 重新检查原岗位/状态/班次，按原顺序更新工作分钟、工资、商店出勤或工厂 `ProcessFactoryWork`。
5. `CityModel.AdvanceIndustry(minutes)` 无条件进行企业及住宅环境状态处理。

MS013真实Player的生产大作用域均值约0.780ms/步，包含以上全部内容。它只能作为后续分项的上限背景，不能把0.780ms全部算成AdvanceIndustry，也不能将稀疏在岗的既有收益重复计入。

## AdvanceIndustry 的不同工作

| 部分 | 当前行为 | 正确性约束 |
|---|---|---|
| 三个衰减系数 | 每步原顺序Math.Exp→float | 不改积分步长/浮点运算顺序 |
| residualIndustry | 噪声/污染逐项衰减，按既有阈值RemoveAll | 不是只看当前建筑；拆掉企业的残留仍有实际状态 |
| 全建筑遍历 | 提取工业factory，另检查住宅用途 | 暂未测出其独立成本，不凭O(B)就重构 |
| 工厂环境 | stepProductive推导activity，更新noise/pollution，等级/开发模式影响表达式 | 即使无生产，非零历史值仍需每步衰减 |
| 工厂状态 | 废弃/供给/现金/加工/出勤/库存/原料共同决定status；变化时Trace | 没有在岗员工也可能被供水、电力、到货或资金改变状态 |
| 步出勤清零 | stepAttendance/stepProductive清零 | 不遗留上一步真实劳动，也不能在清零前跳过依赖 |
| 住宅交通暴露 | 原 `heavyTraffic * trafficKeep + 0f` | 既有非零值仍需衰减；不能因旧交通引擎已删就删除事实/运算 |

## 接下来测什么

在可丢弃诊断构建中分别测BeginStep、真实成员Produce、AdvanceIndustry；再拆AdvanceIndustry的残留、建筑分类、工厂环境/状态及住宅衰减。复用同城预热与完整固定步，记录实际工厂/住宅数量、恒零与持续变化比例，计入观察本身的开销。必要时用普通粗计时确认细埋点没有改变结论。

现阶段没有声称任何子阶段耗时或新增收益，没有实现MS015活动集合。若恒零比例不高、维护比扫描更贵，保留现行实现；如果有可靠候选，再按原MS015冻结完整状态、冷热、资金/原料/产量/环境逐项对照。
