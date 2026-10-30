# 执行进度

## 当前恢复点（2026-10-31）

- 工作树C:/Users/16934/Documents/unity_game/million-scale-execution-20261031，分支codex/million-scale-execution-20261031。
- 正式基线93d97f2004b3893d3b76bf765e972e21405d19fa；旧独立实验53a9e8c只读参考，不整支集成。用户正式工作树仍d248cf3，有三个cmd修改与.editorconfig；保护快照Temp/MillionScaleExecution/protection.json，不能覆盖这些文件。
- MS-001..005完成；本次复查发现MS005的无变化RebuildJobs重建全部员工集合，已修正和补测，见MS005-Review.md/CSV。MS-006 IN_PROGRESS：资金写口与账户生命周期审计，尚未修改现金。
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
|MS005复查修正|本报告所在提交，git log定位|无变化重绑2.112→1.244ms/次，1.84MB/次重复分配消除；不是整体FPS|

## 当前测试与输入

原30k只读存档test/Temp/CityScenarios/regenerated30000-20261017/city.hcity，SHA3F49BD1A02879BB686C24D5517902CC581C657220CE2B955107D89B6FA870B4E。100k继续复用旧实验Temp/ScaleArchitecture/resume-city100k/initial.hcity，禁止重造来刷人口数。100k既有5706模型步不是Player/长期经营验收。

本次Temp/MillionScaleExecution/review-employment、review-events：冻结0bb对照与726成员/顺序oracle、1200步冷热、岗位替换及精确通知通过；review-guards：冻结93d完整状态/字节与126检查点通过；review-core：Mono核心23627、jobs10000、grid1522、zone83、utility460474通过。详细报告与必要CSV在本目录，原始日志/二进制留Temp。

## 下一项具体开发：MS-006

1. 完整审计FactoryState.cash与CityShopAccount.Cash的全部写口、账户替换、IndexType/UnindexType与冷读绑定。
2. JobFunded在development.enabled下按工厂/商店余额>0；关闭development的语义不同，不能统一硬套。规则及失败交易不改，字段通知只能标脏，不能在部分扣款状态执行业务。
3. 复用MS005工作地→成员索引。资金跨门槛才使相关成员失效，保持Members顺序；同一交易去重及同轮较后成员唤醒需证明。工资、生产、税费、零售、贸易均需覆盖，不能只测工资。
4. 工厂账户定义在Industry/CityIndustry.cs；商店在Economy/CityShopAccount.cs，有独立DTO。不存在单独FactoryState.cs。现金写口见Payroll、Industry、Trade、Finance、Consumption、Growth/Session初始化。
5. 之后MS007健康/家庭、MS008精确时间、MS009路网/日切、完整影子和实际执行与Player门槛。不得先启用不完整调度来声称提速；负控不能削弱。

旧稀疏在岗收益已正式集成，不重复记功。旧缓存扩容/工作区失败试验不重做。正式分支、main/master/Python及旧百万实验均不在本轮修改范围。
