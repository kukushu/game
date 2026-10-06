# 城市运行观察与 Debug Trace

正常运行不持续写原始日志或 Analysis Markdown。首页使用 KPI 卡片、进度条、Findings 卡片和真实历史趋势；视觉组件与统计口径详见 [DashboardVisuals.md](DashboardVisuals.md)。按 **F3** 打开游戏内 Dashboard；每次从地图打开时首先显示城市首页。搜索框获得焦点时 F3 仍可关闭面板。

## F3 的信息结构

- **Dashboard**：先看需要关注 / Findings。证据来自真实劳动、库存、运输、付款和已经提交的家庭决策；点击进入相关实体详情。Live 显示当前人口、就业、空房、在途居民、货运和缺货情况。Today 显示本日截至当前的实际变化量。最近日结和 History 展示过去若干天的趋势。
- **经营**：工厂 / 商业列表及住宅。工厂详情包括现金、原料、成品、加工进度、岗位、实际出勤、今日产销、三类支出、经营盈余、原因、证据和已观测到的影响。可进入员工或货运详情，或定位建筑。
- **家庭**：成员、工作、最近日结工资、合同收入、住房、租金、储蓄、通勤、最近实际决策、已评估候选和拒绝原因。成员可继续进入居民详情并定位 / 跟随。
- **居民**：按姓名、编号、家庭、位置或状态搜索，分页展示；详情提供实际活动、车辆、到岗、最近通勤和工资余额。
- **History**：最多 180 个已结束日的人口、就业、实际通勤、财政、迁入迁出和工业产销记录。
- **时间线**：最近 750 条关键变化；展示工厂状态变化、家庭迁入 / 搬家 / 迁出、岗位失效、严重迟到、货运长时间阻塞和商业库存耗尽 / 恢复。重复轮询不重复记，连续同一时刻同一场所的岗位失效合并为一条。页面最多展示最近 150 条搜索匹配项。
- **Debug**：主动开关完整 Trace、查看写入路径、导出当前 Trace、导出模拟存档快照、核对日结账目。

点击地图上的住宅、工厂和商业也读取同一份分析状态，并可进入 F3 详情。

## 数据流与职责

```text
Simulation 的真实状态 / Trace / Labour / Sale / Traffic / Settlement hooks
    ↓
CityDailyAnalysis：每日基线、真实过程证据、确定性原因、有限历史
    ↓
CityAnalysisState：Live / Today / reports / history / findings / entities / timeline
    ↓
F3 Dashboard / 手动 JSON 导出
```

`CityDailyAnalysis` 不依赖 UI、Unity、磁盘或 `CitySimulationLog`。它由游戏生命周期创建，通过现有观测钩子接收数据。控制器约每 0.25 秒生成可查询的展示快照；UI 格式化快照字段，不重新计算全城指标。地图定位仅通过实体 ID 找到目标。观察层不日结、不评估住房候选、不发车、不付款、不消耗随机数。

`CitySimulationLog` 只负责可选原始磁盘 Trace。切换 Trace 不更换分析对象，不重置今日基线或历史。已删除 Markdown 渲染、日报写盘和日志 writer 创建 / 销毁分析对象的旧职责。

## 统计口径

生产、实际到货、装车、实售、出勤、加工和工厂收支均为当前累计计数减本日观测起点。库存、现金和岗位是当前状态。装车运出不是销售收入，实际交付才确认工厂收入。资本投入另计，经营盈余包含当日预付采购，不等同于按销售成本匹配的会计利润。

缺料、仓满和资金不足使用实际受阻**工人分钟**；多人同时受阻会相加，不能解释为墙钟持续时间。库存为零本身不能证明过去全天缺料。道路断开、相关商业缺货等若没有足够的时序证据，只列相关观察，不宣称唯一因果关系。

工厂工资是雇主已经支付的钱；外部岗位工资在 Today 中显示本日已挣金额，尚未日结入家庭。家庭详情展示最近日结实收工资和合同预期收入。财政与家庭守恒使用已结束日账目。

严重迟到为实际到岗比计划迟至少 15 **游戏分钟**，每天按居民去重。货运长等待为连续受阻至少 30 **游戏分钟**，每日任务数去重，时间线按独立阻塞阶段去重。普通工资结算、发车和加工步骤不进入关键时间线。

家庭解释使用实际候选评分与提交结果；未评估或延期不补造分数。实际通勤均值展示样本人数，人口样本变化不能直接当作同一群体的因果对照。Today 截至当前与昨日全天分别标注，不把不同覆盖范围的产量直接解释为下降。Dashboard 可展示带日期的昨日阻断证据。

日报 `day` 表示刚结束的劳动日，`settlementDay` 保留原系统次日结算编号。日结的迁入搬家计入该次结束的日报，次日 Today 从零开始。读档 / 脚本重载后建立新观测基线；如果已到日中，报告标为部分日。存档已有日账目可以恢复人口等历史，过去没有记录的工业数据标为“未观测”。运行时观察历史不加入 simulation save，格式仍为 8。

## 手动导出分析

F3 顶部 **导出分析 JSON**：

`Application.persistentDataPath/SimulationReports/analysis-day-<天>-<时间>.json`

导出整个结构化 `CityAnalysisState`，包括当前日、最近日、历史、实体、证据和关键时间线。不启用 Trace，不推进模拟，不生成 Markdown。分析 schema 当前为 1；`hasDecision` / `hasTodayReport` / `hasYesterdayReport` 明确标记可选记录是否真实存在，避免 JsonUtility 的默认嵌套对象被误认为实际观察。

## 可选 Debug Trace

默认关闭，每次新开局、读档或重新启用组件都保持关闭。只有 F3 → Debug → **主动开启完整 Debug Trace** 才创建：

`Application.persistentDataPath/SimulationLogs/<会话>`

| 文件 | 用途 |
|---|---|
| `events-0001.jsonl` 等 | 完整结构化原始事件，每行一个 JSON |
| `readable-0001.log` 等 | 同一事件流的中文摘要与关联编号 |
| `baseline.json` / `latest.json` | 开启时 / 最近导出或关闭时的完整格式 8 城市 DTO |
| `daily.csv` | 开启后日结账目 |
| `summary.json` | 会话计数及写盘错误 |
| `README.md` | Trace 包使用说明 |

关闭 Trace 后保留已经写出的文件，内存观察继续。开启前的原始事件不补写。手动导出当前 Trace 生成独立包，位置由界面提示；关闭时导出不会暗中开启记录。不会生成 `Analysis/*.md`。旧磁盘日志不自动删除。

保留既有事件机制：家庭候选 / 提交、岗位、居民实际到岗 / 工资、车辆完整生命周期、工厂资本 / 原料付款 / 交付销售、库存、道路、建筑、日结和引擎异常等。共同字段包括 session、seq、UTC、游戏 day/minute、simulationSeconds、type、level、message、实体 ID、roadRevision 和结构化 data。工厂与居民的历史 ID 不表示实体仍然存在。

Trace 仍按约 8 MB 分段，每 2 秒刷新；关闭时刷新。写盘错误提示但不阻断模拟。仅 Trace 开启时进行原来的额外连通性审计与每模拟秒车辆状态记录。长期 Trace 的磁盘空间需要手动管理。

可继续使用 `Tools/AnalyzeSimulationLog.py` 筛选 Trace 包中的居民、家庭、车辆或事件类型。例如：

```text
python Tools/AnalyzeSimulationLog.py "Trace 导出目录" --citizen 10
python Tools/AnalyzeSimulationLog.py "Trace 导出目录" --household 1 --type household.decision --json
python Tools/AnalyzeSimulationLog.py "Trace 导出目录" --trip 42
```

## 验证

`Tools/Verify.ps1` 包含每日差量、瓶颈证据、750 条时间线与 180 天历史边界、快照稳定性、Trace 开关隔离及完整模拟状态一致性。Unity 菜单 **Harbor → Validate runtime observability and native analysis JSON** 使用原生 JsonUtility 验证结构化导出及存档隔离。界面 Play 下的视觉与点击验收仍需在游戏中进行。
