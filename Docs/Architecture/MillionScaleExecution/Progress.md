# 执行进度

## 恢复点（2026-10-31）

- 独立工作树：C:/Users/16934/Documents/unity_game/million-scale-execution-20261031；分支codex/million-scale-execution-20261031。
- 正式远端93d97f2、旧实验53a9e8c重新核实；首次网络读取失败，重试成功。用户正式工作树仍d248cf3与四个用户修改，实验配置原样；不移动这些HEAD。
- MS-001 DONE（48552f5）；MS-002 DONE：见ResidentWakeContracts.md；MS-003 DONE：冻结状态/保存字节及Mono核心通过；MS-004 DONE：6920帧/995814请求零遗漏，完整状态同，正负控通过；当前MS-005 IN_PROGRESS。
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
