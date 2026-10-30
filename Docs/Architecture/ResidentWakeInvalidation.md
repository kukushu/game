# 居民唤醒：进入实际执行前的失效契约

当前仅Tools影子，不是正式调度器。唯一状态仍是Person/Family/Job/企业账户及World Clock。原Members顺序和阶段必须保留；阶段2的请求覆盖只是必要条件，不能替代安全、业务和全状态对照。

|真实写入口|需要关联的对象|通知时机与规则|
|---|---|---|
|CityCoreResidents.Depart/Arrive|实际citizen|业务已接受出发/结算完成后；取消在途轮询，到达重新评估，不立即越阶段派车。现影子已覆盖|
|AdvanceRequests写retryAt、Consumption.Step/BeginVisit写retryAt/visitUntil|对应citizen|保留float retryAt及double visitUntil首次满足tick。现影子识别未来守卫及到期；缩短期限必须显式通知，负控已证明缺失通知会漏请求|
|CityEntities.Add/RemoveResident、MoveResident及家庭变动|对应Member/家庭成员|ordinal身份、成员有效序和住址；现原型结构变动重建，不把全部业务字段变动当结构变化|
|CityModel.AssignJob/ReleaseJob/SyncJobs|岗位对应居民、相关工作地|成功变动后维护workplace→Members反向引用，保持原job.id/技能及Members顺序；当前仍保守检查|
|CityCorePayroll.PayAttendance、业务货运Depart、消费BeginVisit及工厂销售/税费|资金跨过有钱/没钱门槛的雇主及员工|在成功交易/原子提交后发脏通知；不能在可能失败的付款中间派发。不能每笔无关现金波动广播全城|
|健康、年龄/工作能力、死亡、家庭迁移|对应居民/家庭|先审查全部写路径及初始化和冷读；没有通知保证的类别保留旧检查，不能用状态摘要每步全扫后叫事件驱动|
|NetworkGeneration/AccessRevision、道路和建筑入口变化|受影响的待出发、未到达与不可达居民|几何影响可能跨区，不能固定半径截断。真正全局路网变更可能须重估等待集合；与普通对象变更区分|
|日切/日长/班次规则|对应到期集合，真正全局规则影响的等待集合|原阶段处理；对float minute与比较边界找首次合法Tick，不粗略ceil后晚一帧；已有队列测试不是班次策略的完整证明|

当前CityResident业务计时、健康、岗位等仍是public字段。独立在岗集合实验已将唯一必要事实atWork改为受控属性，由CityEntities维护派生索引；CityCoreSave显式支持带CitySavedFact标记的当前业务属性，保留原名称、类型、顺序及Version17，冻结旧读写与完整状态已对照。没有把其他属性自动当存档，也没有新增旧格式迁移。此写入口只解决在岗集合，不代表retryAt、visitUntil、tripId等唤醒通知已完整。不能直接把其余字段换成未标记自动属性；必须逐项证明原有保存事实及模拟顺序。

尚未解决的具体阻碍：未通知的retryAt提前变更已能触发真实漏请求；未来班次缓存还依赖资金、健康、岗位和入口。负控必须继续能失败，不能删掉或把当前居民悄悄补入候选来宣称事件覆盖。

下一验收：逐类受支持写入的原/影子请求序列、完整状态及冷热回放一致；未接管类别明确保守；再测包含队列/失效维护的真实Player总CPU和普通窗口。之后单独提交实际执行开关，未经验证不改变正式轮询。
