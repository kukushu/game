# 执行进度

## 恢复点（2026-10-31）

- 独立工作树：C:/Users/16934/Documents/unity_game/million-scale-execution-20261031；分支codex/million-scale-execution-20261031。
- 正式远端93d97f2、旧实验53a9e8c重新核实；首次网络读取失败，重试成功。用户正式工作树仍d248cf3与四个用户修改，实验配置原样；不移动这些HEAD。
- MS-001 DONE（48552f5）；MS-002 DONE：见ResidentWakeContracts.md；MS-003 DONE：冻结状态/保存字节及Mono核心通过；MS-004 DONE：6920帧/995814请求零遗漏，完整状态同，正负控通过；MS-005 DONE：岗位依赖索引/完整状态与维护测试通过；当前MS-006 IN_PROGRESS（资金写口审计，未开始改现金）。
- 旧失败试验不重复；在岗位图已在基线中，未重新集成。

## 执行口径

先补权威写通知，再影子覆盖、再按类别真实执行；目前正式居民循环仍全员poll。guard-only可保守覆盖其他未通知条件，但不能据此宣称班次/资金/健康全事件化。原Members顺序、float/double边界、0.25重试及完整安全和交易规则不改。

30k复用test/Temp/CityScenarios/regenerated30000-20261017/city.hcity（3f49bd1a…）；100k复用旧实验Temp/ScaleArchitecture/resume-city100k/initial.hcity，禁止重建昂贵城市来刷规模数字。所有性能都区分冷启动/稳定、模型/普通Player/详细归因。新实测未开始，旧3.4%不是本轮新结果。

## 下一项具体操作

1. MS-002：检索所有tripId/retryAt/visitUntil写口及结构/冷恢复；记录最小可靠通知契约。
2. MS-003：利用已有CitySavedFact和实体Owner封装守卫，保留保存位模式与业务顺序；冻结93d完整状态和时间边界测试。
3. MS-004：只选择性复用53a的队列/影子工具，接入真实变更；公共setter提前deadline必须被覆盖，故意绕过通知仍须被负控发现。

## 提交与结果

台账建立提交48552f5；尚未修改运行代码，未宣称新的性能收益。原始测量/二进制继续留本地Temp，简要报告放本目录/按任务编号定位，不新建管理框架。

MS-002提交f1439a6。MS-003结果见MS003-GuardNotifications.md；未开启候选执行，未测新Player收益。

MS-003提交3102e74。MS-004复用旧队列/影子，完善真实通知和同轮后续成员唤醒；先前对照控制场景受发车占用干扰，修正场景并完整重跑通过，未放宽断言。详见MS004-WakeShadow.md。

MS-004提交7047a7d。MS-005模型维护配对无稳定CPU收益，额外约2.41MB，正式轮询未变；报告/4行CSV已保存。原始三万人存档保持只读。正式分支/旧实验未集成。

## 下一恢复点：MS-006

- MS-005提交464e94e4936f55e9b594763806d0de8cd3279619，已推送；工作树干净。重新核实正式93d、旧实验53a、main/master/Python远端均未移动，用户四个修改/旧实验配置哈希及原30k存档均保持一致。
- 已查实际资金门槛：CityIndustry.JobFunded在development.enabled下按FactoryState.cash>0或CityShopAccount.Cash>0；关闭development的分支语义不同，不能统一硬套。
- 工厂账户定义在Industry/CityIndustry.cs（没有单独FactoryState.cs）；写口包括Payroll.PayAttendance、生产成本、工业销售、Trade原料付款、Finance税费、初始化。商店写口包括工资、货运预付、零售收入、税费；CityShopAccount有独立Snapshot DTO。
- 下一次先完整列出所有余额写口和账户替换/绑定生命周期，再实现仅门槛跨越的通知，通知只标脏，不能在扣款未完成时派车。利用MS005的雇主→成员索引，去重并保留Members顺序。FactoryState.cash若封装可沿用既有CitySavedFact，商店DTO保持原有事实；不加旧档适配器。
- 当前尚未修改现金，不存在自动事件执行、资金广播优化或新的Player/FPS成果；MS-006仍IN_PROGRESS，MS-007以后TODO。无测试子进程需要恢复；原始数据均在本工作树Temp/MillionScaleExecution。
