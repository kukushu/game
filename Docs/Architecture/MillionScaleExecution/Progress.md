# 执行进度

## 当前恢复点（2026-10-31）

- 工作树C:/Users/16934/Documents/unity_game/million-scale-execution-20261031，分支codex/million-scale-execution-20261031。
- 正式基线93d97f2004b3893d3b76bf765e972e21405d19fa；旧独立实验53a9e8c只读参考，不整支集成。用户正式工作树仍d248cf3，有三个cmd修改与.editorconfig；保护快照Temp/MillionScaleExecution/protection.json，不能覆盖这些文件。
- MS-001..006完成；MS005复查修正791942f保留。MS006实现1d14d05/0f331fe，通知与成本验证通过但CPU增加；当前唯一MS-007 IN_PROGRESS：健康/资格/住址写口审计，尚未修改相关业务。
- 正式居民请求仍全员轮询；守卫/索引与影子是前置阶段，不是已交付事件调度。完整Player收益、100k/1m长期运行未验收。没有需要恢复的本轮测试子进程。

## 已提交阶段与证据

|任务|提交|实际结果|
|---|---|---|
|MS001台账保护|48552f5|独立工作树、30项依赖、保护文件哈希|
|MS002写口审计|f1439a6|ResidentWakeContracts.md，不声称所有条件已通知|
|MS003守卫通知|3102e74|冻结93d保存字节、126状态检查点与核心测试同|
|MS004通知影子|7047a7d|6920帧、995814请求、零遗漏；正负控制通过|
|MS005雇主成员索引|464e94e|完整状态同；稳态模型无稳定CPU收益，约+2.41MB常驻|
|恢复记录|0bb83fe|上轮保护与资金门槛审计；不是新性能收益|
|MS005复查修正|791942f|无变化重绑2.112→1.244ms/次，1.84MB/次重复分配消除；不是整体FPS|

## 当前测试与输入

原30k只读存档test/Temp/CityScenarios/regenerated30000-20261017/city.hcity，SHA3F49BD1A02879BB686C24D5517902CC581C657220CE2B955107D89B6FA870B4E。100k继续复用旧实验Temp/ScaleArchitecture/resume-city100k/initial.hcity，禁止重造来刷人口数。100k既有5706模型步不是Player/长期经营验收。

本次Temp/MillionScaleExecution/review-employment、review-events：冻结0bb对照与726成员/顺序oracle、1200步冷热、岗位替换及精确通知通过；review-guards：冻结93d完整状态/字节与126检查点通过；review-core：Mono核心23627、jobs10000、grid1522、zone83、utility460474通过。详细报告与必要CSV在本目录，原始日志/二进制留Temp。

## MS-006实际交付

实现1d14d05、无观察者短路径/实际观察者测试0f331fe；最终运行代码精确恢复0f331fe，未保留内联提示实验。报告MS006-FundingNotifications.md和两份CSV记录全部数据，不只选好看的组。39定向断言、18完整状态点/请求序/保存字节、126增长/30k/冷恢复检查点通过；观察者在完整增长场景产生9108通知。726岗位索引oracle与Mono核心23627/10000/1522/83/460474通过。

真实30k .NET模型最终A/B/B/A组均值2.86537→3.03857ms/步（约+6.04%），600步分配均17,535,888B；常驻约+0.438MiB。原始数据在ms006-final、ms006-final-paired、ms006-core-final.log；初版和失败内联试验数据保留。控制余额维护循环无持续分配；不是自然游玩/FPS成绩。没有合入正式分支；未启用事件执行。必须让MS011..013证明节省的轮询工作足以抵消新增成本。

## 下一项具体开发：MS-007

仅开始写口审计，不在本阶段修改健康/家庭逻辑。CityHealth.CanAttendWork读取canWork和sick；AdvanceRequests还单独判断dead、家庭resident、home/location及岗位。下一步沿居民、医疗/死亡、家庭迁入迁出和CityEntities.MoveResident/Add/Remove查全部权威写入口及冷恢复。复用唯一Owner和既有显式保存事实，未接类别继续保守poll；不要只接sick就宣称所有工作资格已事件化。

MS008依赖005/006/007；007未通过不得推进班次实际调度。后续008/009/010完整影子、011/012真实执行及013 Player门槛均保持TODO。旧稀疏在岗已正式集成，不重复记功；旧缓存失败试验不重做。正式/main/master/Python和旧百万实验不在本轮修改范围。

## MS-007 当前恢复点

运行代码586777d：健康/工作资格/死亡/位置/在岗、家庭home/resident受控通知，住宅→家庭局部索引，搬家不瞬移，通知按原Members序。仅通知，正式轮询未替换；没有添加医疗/死亡业务。报告MS007-ResidentState.md，三份小CSV保留成本及失败证据。

88定向断言；完整增长/拆分/30k的126状态点与冻结934同，实际观察129673通知；请求序列、当前保存字节同，普通cold80同。726岗位oracle与Mono23627/10000/1522/83/460474通过。模型均值2.89934→2.92947ms（波动约+1.04%，不能宣称收益），常驻约+0.695MiB，600步分配不变。原始日志与二进制在Temp/MillionScaleExecution/ms007-full、ms007-paired、ms007-core-final.log、ms007-cost。

**未解决且必须优先：** 冻结934和候选均复现MoveResident(1,新家庭)再MoveResident(1,原家庭)，暖态Members1;2;3;4，冷态2;3;4;1；20步后完整状态不同。MS007-ExistingColdOrder.csv记录两个版本的warm/cold SHA256。不是新回归，但不能算一般读档一致性通过，MS007不得标DONE，008保持TODO。

下一子步骤先设计/实现显式保存成员执行顺序，覆盖跨家庭交错而非只同户回移；不能偷偷按居民ID排序改变原序，也不能放宽完整状态断言。若需要新保存格式，按用户原则明确报告，不为旧档设计长期适配器。此阶段尚未改协议，当前玩家存档未动；未自动合并正式分支。这个持久化缺陷的修复与已经通过的通知等价验证分开提交/验收。
