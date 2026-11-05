# 两类商品任务：跨账号交接（2026-11-05）

这是实际未完成任务的交接，不是完成报告。接手先重新检查 Git、源码和测试文件；不能把本文的历史状态当作当前事实。用户本次要求停下开发、整理交接，等待另一账号接手。本文不授权自动合并。

## 1. 必须打开的目录与分支

- 仓库：kukushu/game。
- **实际开发工作树**：`C:/Users/16934/Documents/unity_game/million-scale-execution-20261031`。
- **实际分支**：`codex/gameplay-two-goods-20261104`。目录名称是历史遗留，不代表当前任务是百万级架构任务。
- 冻结基线：develop `0342fde1caf5991611e35798561f8b386f0bc275`。
- 当前 HEAD：`357ad6f8e3942ce2807b08b3108983fa67c8d9a2`。
- 已提交、已推送：`805265f` 商品/保存基础；`357ad6f` 消费、交易与稀疏需求索引。
- **第三阶段大量实现仍在工作区，尚未提交，也没有推送。不得重新克隆后误以为这两笔就是全部进度。**
- 当前 origin/实验分支的本地追踪引用与上述 HEAD 一致；接手可再次检查远端，不覆盖本地修改。

用户日常 Unity 目录是 `C:/Users/16934/Documents/unity_game/test`，仍在 develop/0342fde，不要切换、重置或把本实验复制覆盖过去。该目录有 AGENTS.md、Docs/README.md 修改，ProfilerCaptures 两个已删除文件，以及未跟踪 .editorconfig、dotnet-tools.json、Docs/Gameplay；全部保留。

`performance-milestone-20261103` 是已有里程碑标签，保留。不修改 main/master/develop，不自动合并实验。

## 2. 接手阅读顺序

1. 实际工作树根目录 `AGENTS.md`。
2. 本交接文件。
3. `Docs/Architecture/TwoGoods-20261104.md`：阶段记录、所有权及已知失败。
4. `Tools/Diagnostics/CityTwoGoodsChecks.cs`：新玩法轮询/按需对照与负控。
5. `CityProducts.cs`、`CityCoreConsumption.cs`、`CityHouseholdDemand.cs`、`CityBusinessInvestment.cs`，再沿调用链读物流、生产、保存和实体写入口。

`Docs/Gameplay/CoreLoop.md` 还没有更新本次已批准细则，仍有“尚未确定/未实施”的早期文字，**必须在最终交付前修正**。`CurrentGameplay-20261104.md` 是旧原型的日期快照，不要把它误当本次完成状态，也不要改写历史证据。

## 3. 用户批准的目标（不能自行改变）

完成：企业自主投资 → 食物/日用品生产 → 综合商店采购 → 家庭购买、储存、消耗 → 真实补购需要。只做两类，不做迁址/转产/股东分红/市场模式切换。

- 单线程、固定0.05秒；玩法及账目正确性 → 算法复杂度 → 数据结构/内存 → 单线程性能。
- 玩家提供用地、交通、服务；企业自主投资。工厂固定生产一种，商店两种都卖。
- 外部启动资本：工厂1200、商店2000，明确记账；建筑本体造价暂不另算。
- 每人每天食物1份，日用品每7天1份；家庭共享库存，缺货记未满足，不负库存，不事后追扣全部欠缺。
- 食物少于1天补到3天；日用品少于2天补到14天。迁入带入目标初始物资，记外部带入。
- 一户同时一个购物周期，一趟最多32份。食物优先；食物买不到，不能自动把其预算挪给日用品。
- 取消17:00和每日一次限制；每1模拟秒保留购物决策检查时刻，不偷改检查频率。
- 缺货先提供玩家反馈，不改变健康、死亡、迁出。
- 食物零售20/批发15/工人分钟15；日用品60/45/60；共用原料成本8，加工费各2。集中配置、冻结随存档，参数未完成平衡验证。
- 库存、货物各有唯一所有者；总载货量从清单派生；普通热路径不逐家庭分配字典，不维护全城家庭×商店矩阵。
- 家庭按到期与实际状态唤醒；消费到期先于同Tick购物，顺序稳定；队列支持更新取消且空间有界。UI只读，不能触发消费。
- 审计资金、人数、住址、健康/工作/任务、库存、retry、商店营业与库存、道路、保存恢复的依赖。不可靠类别保留正确回退，不能漏通知。
- 商店空间索引长期维护；库存变化只唤醒实际等待者。物流按商品维护缺口、预留、在途，保留资金和运输提交原子性。
- 保留6秒投资检查与施工周期。商业/工业由真实缺口驱动，住宅原规则不在本轮改造范围。
- 无可达店才开店，有店缺货先补货；工厂按实际采购缺口选品类，扣库存/在途/待建预留，平局食物优先。
- 有合法地块、道路及符合岗位条件的待业居民才能投资；优先靠近缺口；新厂首交付前暴露阻塞、避免同类重复建设；项目终结释放预留。
- 宏观汇总来自实际事实的增量维护，不每帧遍历经济主体。道路变化允许一次保守全量失效，单独计量。
- 城市18→19、交通15→16、设计4→5，业务内部当前10。旧存档明确拒绝，不写迁移层、不覆盖玩家旧文件。

验收：两类商品分别守恒（生产/初始/外带/库存/在途/携带/消耗/出口/明确损失）、资金与失败原子性、成员/同Tick/缺货/补货/道路/在途冷读、交通安全。以**相同新规则**的简单轮询参考与按需版完整状态/事件/保存对照，包含漏通知负控。真实三万/十万 Release Player 同城同输入对照，完整步/FPS/倍率/长尾/GC/常驻内存，并计入通知、队列、索引、失效成本。旧玩法成绩不能当等价性能对照。

## 4. 已实现的结构与入口

### 商品与权威数据

- `Assets/Scripts/Runtime/GameData/CityProducts.cs`：稳定 ProductKind Food/DailyGoods、紧凑 GoodsAmounts 值载荷、冻结规则表。未提交阶段增加累计用64位 GoodsTotals，避免城市长时间统计32位溢出。
- FactoryState.product 为显式保存事实、开业后固定；工厂/商店 Products 为权威库存，原 Stock/goodsStock/stock 是派生合计接口。正常业务用分品类接口。
- Shipment.Products 权威，Goods 总货量派生；Raw/Waste 保持标量。曾发现切换 kind 丢数量，已修复并测试。
- 居民 carriedProducts、家庭 pantry、运输货物相互转移，不能重复保存第二份总量。
- 保存 DTO 与交通编码已经升级，规则随城市冻结。现有中途生成的格式19临时档也可能因后续尚未提交字段变动过期；这些不是用户旧档，不为临时档建兼容层。

### 家庭消费与购物

- `CityCoreConsumption.cs` + `CityHouseholdDemand.cs`：绝对Tick消费纪元、人数、余量；CityWakeQueue可更新/取消。定时消费先于购物，家庭稳定序号顺序。
- `ReferencePolling` 仅测试/诊断切换，是本轮新规则的简单参考；默认按需。不使用旧的、曾无整体收益的购物调度实验。
- shoppingMember 保证单户一个周期；返家 Home 货物转入家庭库存，不是到家立即全部消耗。
- 产品等待集合与商店变更合并，持久 saleIndex[2] 只放有货/营业门店；一般索引仅用于最近可达供应者，不逐户遍历全城缺货门店。
- 已修复：只等原门店漏其他店补货、新门店改变供应选择、道路拓扑改变等待、供水恢复营业漏唤醒等。
- `SuppliesRebuilt`、相关实体通知只标脏，阶段边界读最终事实；普通余额写入不重建全城集合。
- Requested/ShopDemand/UnservedDemand增量汇总；家庭上次选择供应者 goodsSupplier 是保存事实，汇总是可重建派生数据。
- 商店总容量32，两类有需要时按真实需要比例分配目标（食物取整优先、两类至少各1），避免食物永久独占所有配送空间。应在玩法文档明确此规则。

### 物流、生产、投资

- `CityShipmentIndex.cs` 监听实际任务成功提交/退休，维护在途、入口车辆数；独立 Validate 对照实际任务，不每次交易扫全任务。
- `CityLogisticsSystem.cs`：有货工厂集合、每商品实际补货门店空间索引；复用原运输边界/安全与资金预检。
- `CityCoreTrade.cs`：原料与出口仍有真实支付/车辆；出口按生产商品记价。原料价格唯一来源 Economy.RawUnitPrice，不再在产品表复制一套 RawPrice。
- 工厂实际劳动、工资、原料、加工成本保留，产品用不同劳动分钟；没有伪造到岗、生产或额外注资。
- **未提交的新文件** `Simulation/Growth/CityBusinessInvestment.cs`：按技能维护待业者计数、有货工厂、首交付等待；真实采购缺口决定商工投资；项目完成计 externalCapital/externalInvestment，含退休企业核账。
- UI/观察显示产品库存、支撑天数、短缺原因、投入资本、累计成交与消费；本地化 en/zh-CN 已改，尚需真实窗口检查。

## 5. 已核实结果与未通过项

### 最近专项：PASS，但不是完整验收

命令 `./Tools/VerifyTwoGoods.ps1`，日志 `Temp/TwoGoods-20261104/final-two-goods.log`：**PASS 419 two-product checks**。

包括40,000步小城新规则轮询/按需完整保存对照（每100步）、每步派生商店需求对照、供水预算50/100切换、600步冷续、初始资本真实建设与首交付、绕过pantry通知的独立负控。

正常迁入物资小城结果：成交食物85/日用品0，实际消费333/96，外部资本17600；按需购物家庭访问1921 vs参考30760。这只是候选访问数，**不是 FPS 提升**。

声明为零初始物资的压力夹具实际成交食物27/日用品1；未注入额外运行资金。证明两类可成交，不代表正常经济健康。

### 最新完整核心回归：FAIL，优先排查

命令 `./Tools/VerifyCityCore.ps1 -Results Temp/TwoGoods-20261104/stage3-final.txt`，日志 `Temp/TwoGoods-20261104/final-core.log`。

失败在 `Tools/Diagnostics/CityCoreIndustryChecks.cs` 的 TradeTests，当前约348行：

`Imported vehicle failed same-task restart or charged twice`

场景：出口专用测试没有本地商店，不再满足自主开厂条件，所以测试显式放置一座合法2×2工业厂；等待真实原料车，删除所在道路，核对 AwaitingRestart/货物/预付不变，Undo道路后最多41步等待Active，然后失败。**尚未定位是restart、货物、额外正常交易导致总预付款变化，还是新回归；不能删断言、随意延长/放宽断言或改业务规则通过。**应打印各子条件、任务ID、状态、时刻、后续合法付款，针对同笔业务检查真实性。

更早阶段的 VerifyCityCore 曾整套通过：行业/居民23513、10000岗位查询零分配、1522网格、83分区、460474水电；这些是早期代码成绩，不能代替当前未提交版本的完整PASS。

旧测试已按新规则改夹具与期望（到家库存+累计消费、初始外带纳入守恒、无真实商业缺口不自动开厂、财政等待实际工资），需审查，不能用删除失败测试宣称通过。

### 经营异常：真实存在，必须报告

40,000步普通小城中三家食品厂后期cash=0、raw=24/22/24、stock=14/14/15，已首交付。日用品普通夹具成交仍0。不能宣称长期持续经营已验证，也不要为了通过测试擅自降税、加钱或改工资。初始参数用户明确是试验值，平衡问题单列。

投资实现目前每品类先选一个最大门店缺口，再扣可供工厂库存；可能错过第二个独立区域的机会。待建预留对区域仍偏保守。缺货等待者×变化门店在极端补货集中时可能较重；需要真实测量，不能宣称完全局部化。

## 6. Player 与场景现状（2026-11-05重新核实）

本地根目录：`Temp/TwoGoods-20261104/`。大型文件保留本地，不上传。

- Release Player 构建已完成，`player-build/build.log` 尾部 Unity exit0；二进制存在 `Player/HarborCity.exe`。这只是构建，不是游玩验收。接手如改源码需重建，校验实际构建源码。
- 三万人：`author-30000-final/initial.hcity`，AuthorOnly模式初始构建+冷读；非完整模拟成绩。
- 十万人：`author-100000-final/initial.hcity`，138440670 bytes，authoring.json population=100000，SHA256 `b6998fb3977f6964d8d3bf91ad8295412f2178fbd0d6bef113184091d28a9933`；同样只是初始城市。
- 输入配置 `scenario-3000.json`、`scenario-30000.json`、`scenario-100000.json`，由已有成熟路网场景复制，days1/warmupDays0/seedRoadNetworktrue，诊断工厂声明2:1食品/日用品，不预填商品/到岗/工资。
- `author-3000-v3` 是较早中间数据，后续GoodsTotals等变更可能不兼容，建议重建。`author-3000-r2` 曾运行1日但未达到既有Waste/Shopping活动门槛，**不算PASS**；保留失败。
- **三万/十万真实窗口性能、同新规则Player固定步完整状态配对均未执行。没有FPS收益数据。**
- 本次交接检查未发现命令行指向此实验工作树的 Unity/mono/dotnet/Player 测试进程；仍需重新检查。不要停止用户 test 目录的Editor。检查进程时不要输出整条用户命令行，可能包含登录令牌。

现有脚本已增 `-GoodsPollingReference`，Probe会应用并检查实际模式。普通新路径是scheduled。复用 `RunCityScenarioWindow.ps1`，不要另建庞大框架。

示例（执行前核对参数与输入有效性、使用新的结果目录）：

```powershell
Set-Location 'C:/Users/16934/Documents/unity_game/million-scale-execution-20261031'
./Tools/VerifyTwoGoods.ps1
./Tools/VerifyCityCore.ps1 -Results Temp/TwoGoods-20261104/core-next.txt

# 新源码的Release构建（BuildOnly不会测FPS）
$env:HARBOR_PLAYER_RELEASE='1'
./Tools/RunCityScenarioWindow.ps1 -Save Temp/TwoGoods-20261104/author-30000-final/initial.hcity -Results Temp/TwoGoods-20261104/build-next -PlayerPath Temp/TwoGoods-20261104/Player/HarborCity.exe -BuildOnly -TimeoutSeconds 600

# 正常可见窗口，候选与参考分别单独执行，避免CPU争用
./Tools/RunCityScenarioWindow.ps1 -Save Temp/TwoGoods-20261104/author-30000-final/initial.hcity -Results Temp/TwoGoods-20261104/player-30k-next -PlayerPath Temp/TwoGoods-20261104/Player/HarborCity.exe -SkipBuild -Rates 1,50 -SharedWarmup -BaseWarmSteps 6000 -MeasureSeconds 20 -CheckRestore -DisplayCamera middle -DepotLaneRouting -TimeoutSeconds 900
# 参考运行使用另一个Results目录，额外加 -GoodsPollingReference，其他条件相同。
```

6000步只是待实施的建议预热，不是已完成成绩；可能很昂贵，先小城有界确认。正常20秒窗口处理Tick数不同，不能用端点字节直接证明同Tick一致。

固定工作量另用 `-Rates 0 -FixedSteps 800`；其每渲染帧推进一步，**不是正常1×/50× FPS**。用共同fixed-start对齐输入，对照fixed-final与逐步状态。冷恢复检查在计时外。GC计数不支持时输出未知/-1，不填0。

重建初始城市用既有 `RunBoundedCityScenario.ps1 ... -Runtime DotNet64 -AuthorOnly`，明确只是作者模式，不能当完整一日模拟验收。

## 7. 接手后建议执行顺序

1. 保护现状，核对diff/HEAD/进程/报告，不重开分支、不覆盖已有实现。
2. 定位TradeTests当前失败，打印分解证据，按真实业务边界修复后再完整回归。
3. 审核新增索引生命周期与缺口选址限制；补必要成员/失败/道路/在途测试。`VerifyRoadCore.ps1` 是硬编码源文件列表，新增GoodsAmounts依赖后可能需要加入CityProducts.cs；如编译缺类型，仅修测试源列表，不绕过交通测试。
4. 验证通过后提交第三阶段实现及定向测试，记录源码SHA；只stage本任务文件。
5. 小城真实Player先验证显示/交易/冷读；三万、十万同新规则参考/候选配对，含集中补货、无钱/无货/道路变化和长期经营。性能期间不同时运行其他测试。
6. 补独立维护成本CPU/GC/候选/队列常驻内存。专项测试中的每步LINQ对照是测试成本，不能混入正式性能窗口。
7. 更新玩法文档、技术说明、简短真实报告和必要CSV，阶段提交并推送当前实验分支。未覆盖的验收明确未验证，不能为了交付宣称PASS。**不合入develop**。

## 8. 未提交文件、隔离内容与交接政策

交接时git status有32个已跟踪修改，涉及资源配置/本地化、诊断Builder/Probe、商品/观察/UI、实体/保存/消费/增长/行业/服务及测试工具；另有未跟踪 CityBusinessInvestment.cs 与.meta。

`Docs/Performance/Current100k-20261103/` 是之前诊断遗留的独立未跟踪报告，**不是本次成果，不要顺手stage、删除或覆盖**。本交接文档也是新建文件。

整个工作树的未提交代码是本任务重要成果，不能stash/pop错树、reset/clean、覆盖或只拉远端重做。跨账号在同一电脑打开这个工作树，文件仍在；换电脑必须另行完整转交未提交文件与必要本地测试输入，GitHub只有已提交部分。

已有SSH推送使用443端口：

```powershell
git -c 'core.sshCommand=ssh -p 443 -o HostName=ssh.github.com -o HostKeyAlias=github.com -o ConnectTimeout=15 -o BatchMode=yes' push origin codex/gameplay-two-goods-20261104
```

最终交付必须诚实区分：实现已写、专项通过、完整回归失败、Player仅构建、经济异常、真实性能尚未测。这些不能相互替代。用户接受旧档不兼容，不接受为赶进度放松守恒/交通安全，也不要求为迁移做兼容负担。
