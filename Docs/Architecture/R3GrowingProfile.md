# R3 综合城市生长尖峰归因

从空地经生产建设命令，在原Unity地形上运行实际分区、生长、管网、居民、雇主付薪、原料/商品实车及到店消费。采样12,000固定步，到第6天30居民/26建筑/最多30活动车，真实消费45件。不是万人完整城，也不是普通1x/3x窗口FPS测试。

新增静态采样阶段 Growth、Utilities、Population、Consumption、Trade、Payroll，并细分 ZoningRebuild、GrowthSearch、Placement。普通构建不定义CITY_CORE_ATTRIBUTION，不执行这些计时/计数代码。原City.Simulation及既有交通标记保留。

实际最重一步约68.09ms：Growth约66.85ms；其中分区重建含放置检查约64.85ms。该步 Placement约21.76ms，分区重建其余Self约43.18ms。不是交通移动造成的这一尖峰。12,000步中分区重建25次，放置检查42,320次。原始Profile在忽略目录Temp/RoadCore/GrowingProfile-zoning-attribution/growing-city.raw。

Unity层级导出证明：完整Step分配145,349,720B（平均12,112B/步）；ZoningRebuild包含124,900,898B，约86%；Growth包含132,476,438B，约91%。父子分配不可相加；GC.Collect也不能与父采样耗时重复相加。Boehm的gc0/gc1/gc2计数可为同一计数，不能相加成三倍回收次数。

重要测量修正：该Unity运行时GC.GetAllocatedBytesForCurrentThread返回0，即使真实发生分配。现增加实际分配探针，不支持时CSV写-1/availability=0，而不是假报零分配。原R3Views的0B来自这个失效计数，不能作为零分配证明；对象身份未变和模拟保存未变的验证仍成立。窗口ProfilerRecorder数据和独立Mono下确实支持的计数不因此失效。

关键调用关系：Growth完成/排定项目改变development.revision；Zoning.Cells因此重建全部道路旁几何、坡度/路面检查、土地用途和占用，即使道路与地形没有改变。几何采样与占用生命周期混在同一个派生缓存里，是下一批明确优先问题。随后才评估候选脚印全表扫描，不能一次重写所有规划规则。

下一步方案：按道路/地形失效重建静态候选几何；按用途修订更新用途；按建筑/项目修订更新占用。它们仍是同一Zoning所有者中的派生数据，不新增第二套建筑真相。保持道路修改、地形采样重新绑定、项目完成和存档恢复的完整失效；先配对完整消费者城市状态/性能，再报告收益。

验证：2209城市检查通过；新增6000步采样开/关对照，完整模拟保存相同、新阶段调用数正确。原地形Unity构建与12000步原始采样/层级导出成功。轻量模块/尖峰/测量说明见Results/R3GrowingProfile；原始二进制不提交。
