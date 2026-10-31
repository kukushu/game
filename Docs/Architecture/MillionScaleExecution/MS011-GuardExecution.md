# MS-011：基础守卫实际接管

基线 `a8430164a9d00d9642f2ea491018738b38393829`；初版实现 `5c5370d`，基础状态守卫实现 `43a9e10`，过渡日期修复及最终测量源码 `b23cf85`。仍在执行分支，未合并正式开发分支。

## 实际运行方式

编译开关 `CITY_RESIDENT_GUARD_SCHEDULE` 启用时，AdvanceRequests **直接枚举可检查成员**，没有先全员扫描再过滤。未定义开关的正常 Unity 构建仍使用原 foreach；本轮没有修改 PlayerSettings 或默认启用。

CityCoreResidents 唯一拥有派生 CityResidentGuardSchedule。成员数组只引用原 CityMember，槽位沿用 Members 顺序；ID字典仅供通知定位，不决定执行顺序。复用的 CityWakeQueue 已从 Tools 移入 Runtime，未复制第二份堆；旧队列测试及影子准备入口同步更新。

派生结构包括：定时堆、开放槽位位桶、按原顺序的双向链接、当天出发过的在家成员集合。正常取下一候选仅沿链接；成员从关闭变开放时才由位桶找插入点。同步回调唤醒较后成员可本轮加入，较早成员留到下一轮；取消待访问成员会修正迭代游标，不使用易失效的集合枚举器。

接管条件全部是原有规则：

| 条件 | 重新检查来源 |
|---|---|
| tripId非零 | 原Trip setter |
| retryAt / visitUntil 尚未开放 | 原setter＋同一0.05秒Tick的定时堆 |
| 无居住资格、死亡 | Residence / Death 通知 |
| 在家且atWork，或原CanAttendWork失败 | Attendance / Ability / Sickness / Location / Home / Membership 等通知 |
| 在家且departureDay等于当天 | TripDay / Location等通知；日期变化只唤醒日期阻塞集合 |

日期在开始及每次取下一成员前比较，覆盖同步回调改日。CanAttendWork复用原方法，当前只依赖canWork/sick；未来若改变其依赖，必须重新审计通知覆盖。非有限时间值分别沿用原两个 `<` 的语义，不能用Math.Max让一个NaN吞掉另一个有效门槛。

**原业务体及全部实时守卫仍保留复核**，不改变资格、班次、资金、路由、车辆准入、0.25秒重试或提交规则。班次时间计划、岗位/资金等待尚未接管，归MS012。没有监听无关路网变化：当前索引只判断基础守卫，路网与入口仍由原业务体处理；车辆被恢复时原Trip等setter使索引更新。

结构变化仅在成功MembersChanged后安全重建；冷读从事实重建，不保存索引。普通守卫变化更新有关槽位，没有全局Reindex、居民状态副本或第二套经济/交通引擎。最坏的稀疏插入可能扫描若干位桶，尚未改成多层位索引；结构/日切成本也须继续观察。

## 三次实验与真实结果

相同原始三万人输入、同宿主.NET10 x64 Release模型；2400步预热后观测3000步。自有正确性任务结束后分别做顺序A/B/B/A，测量只包Advance，不含保存、Validate、冷读。两侧保留相同请求摘要采集，所有最终配对状态/请求/原生保存逐字节相同。

| 实现 | 旧版平均固定步 ms | 实验版 ms | 结果 |
|---|---:|---:|---|
| 仅车内/重试/停留，逐候选位扫描 | 2.8618 | 2.9505 | 慢3.10%，未采用 |
| 同三个守卫，改顺序链接 | 2.8791 | 2.9505 | 慢2.48%，仍无收益 |
| 顺序链接＋已验证的基础状态/日期守卫 | 2.8258 | 2.6866 | 平均降低4.92% |
| 同步过渡日期修正后的最终源码复测 | 2.9449 | 2.7062 | 平均降低8.10% |

前两次说明这三个检查本来便宜，索引维护会抵消收益，不能只凭访问次数下降宣布成功。最终版本还排除不具备基础出行资格及当天已出发回家的成员，才出现粗粒度净收益；没有提前接管班次或修改规则。

基础状态首轮两个配对改善约6.33%与3.49%；最终源码配对约6.78%与9.39%，原版均值存在波动，因此结论保守表述为本模型两轮约5～8%的平均步收益，不保证固定百分比。最终旧/新平均P95为4.4936/4.3066ms，平均P99为7.8895/8.0380ms；最长样本26.10/26.75ms，首轮曾出现新版本42.70ms。**没有改善长尾的证据，P99反而略高**，不能据此上线或声称玩家卡顿已解决。

暖态分配约34293/34297 bytes每步，基本相同，没有GC收益；预热后总托管存活约251781792/253768224 bytes，相差约1.89MiB。这是整个模型的观测差值，不是声称已测得百万居民容量或精确峰值。

测量结果是模型完整固定步，不是Unity画面帧、Player FPS或实际倍率。MS013普通Player验收尚未运行；默认关闭实验，不强行合入正式分支。

## 实际验证

```powershell
Tools/VerifyResidentGuardScheduling.ps1 -ControlsOnly -Results Temp/MillionScaleExecution/ms011-final-controls
Tools/VerifyResidentGuardScheduling.ps1 -Results Temp/MillionScaleExecution/ms011-expanded-replay
Tools/VerifyResidentGuardScheduling.ps1 -Growth -Results Temp/MillionScaleExecution/ms011-expanded-growth
Tools/VerifyCityCore.ps1 -GuardScheduling -Results Temp/RoadCore/ms011-expanded-core.csv
Tools/VerifySimulationBudget.ps1 -GuardScheduling
Tools/VerifyCityWakeQueue.ps1 -Results Temp/MillionScaleExecution/ms011-queue
```

- 18项原轮询/实际候选配对：提前门槛、NaN/正负无穷比较、Trip关闭/恢复、同轮早晚成员、同数量增删、健康/能力/死亡/居住/在家在岗恢复、直接出发日修改及改日唤醒（包括改到下一天、写出发日、再改回的过渡状态）。非有限停留值仅测原比较语义，保存前恢复合法事实；没有放宽存档校验。
- 1000轮独立168槽多word oracle：含同步早/晚唤醒，按原全槽顺序读取原守卫比较候选序列。这是索引单元测试，不是假冒168人真实城市性能。
- 故意反射绕过retry通知后，暖索引遗漏、独立冷读原事实的oracle正常请求，明确检出差异；没有全员修复扫描遮住失效错误。
- 正常三万人跨日、生命周期、在途删路及cold80的26点；增长6200、重划拆分4000、真实30k4000及各组cold80的126点；合计152完整原生格式18状态点相同。观测段482141次请求摘要和最终保存字节一致。两轮共八次独立计时回放也逐一比较上述结果。
- 实验开关真正启用的Unity Mono：23627综合、10000岗位、1522道路/事务、83分区、460474水电及101固定步/欠步/恢复预算检查通过。迁移后的原队列1301409断言通过（百万槽位不是百万城市）。

原真实保存文件SHA256仍为 `3F49BD1A02879BB686C24D5517902CC581C657220CE2B955107D89B6FA870B4E`；复用已有冻结读取器导出同输入18测试副本，不覆盖玩家文件、不增加运行时旧档兼容或改保存格式。

测试初次因非有限停留被旧保存校验正确拒绝，后将比较位模式记录后再恢复合法状态；替换居民缺少技能及foreach可见性编译问题均已修正，失败日志保留。未将这些失败写为通过。

## MS010证据更正与交接

复核发现旧影子脚本写出了冻结CityResidents，却未应用旧侧SourceOverrides。已修正并实际冻结26d字段版重跑完整及增长两套数据，输出ms011-ms010-freeze-corrected / ms011-ms010-growth-corrected，152点及请求/保存仍相同。原报告已明确更正，旧性能表解释为同属性模型的原轮询/影子开销，未伪称字段版性能证据；旧数据未删。

本轮代码/报告见[成本CSV](MS011-Cost.csv)、[状态及更正对照](MS011-State.csv)、[18项定向结果](MS011-Controls.csv)。三次实验的原始结果分别在ms011-replay、ms011-linked-replay、ms011-expanded-replay/paired-*及final-paired-*，二进制/日志留本地。

MS011完成基础接管和粗粒度收益门槛。MS012为唯一IN_PROGRESS，按类别接入班次与状态计划，继续检验维护成本、日期集中唤醒及尾部延迟。当前P99/最长步风险未解决；MS013仍需真实性能与集成判断，不能由平均模型成绩替代。
