# 模拟过程日志

运行时自动开启记录。理解玩法时，先看 F3 模拟观察台 → **城市分析**，切换城市日报、工厂日报、家庭决策摘要；点击工厂也可进入对应日报。界面显示最近一个已结束日的报告，首次需要推进到日结。

F3 → **导出日志** 会保存一个独立诊断目录并打开文件夹。`Analysis/` 是面向人的报告，原始事件文件仍用于追查具体事件。原“导出快照”仍保留。

自动记录：`Application.persistentDataPath/SimulationLogs/<会话>`；手动导出：同一持久化目录下的 `SimulationReports/<会话>-export-<唯一编号>`。具体位置以游戏导出提示为准。

## 文件

| 文件 | 用途 |
|---|---|
| `events-0001.jsonl` 等 | 每行一个完整 JSON 事件，数据可直接写脚本分析 |
| `readable-0001.log` 等 | 同一事件流的中文摘要与关联编号 |
| `baseline.json` | 开始记录时的完整城市，用来解释已经存在的实体 |
| `latest.json` | 最近导出或结束记录时的完整城市 |
| `daily.csv` | 每日人口、就业、通勤、收支、迁入迁出和两类资金核对差额 |
| `summary.json` | 会话编号、事件数量、按类型计数及日志写入错误 |
| `README.md` | 随导出包附带的说明 |
| `Analysis/latest-city.md` | 最近一个已结束日的城市日报 |
| `Analysis/day-000001-city.md` | 当日变化与最多五项关注点 |
| `Analysis/day-000001-factories.md` | 逐厂当日产销、出勤、库存、收支、瓶颈证据及影响 |
| `Analysis/day-000001-households.md` | 真实决策结果、评分组成、拒绝理由及居民对应岗位 |
| `Analysis/day-000001.json` | 同一日报的结构化分析快照 |
| `Analysis/in-progress-*.md`、`in-progress.json` | 导出 / 结束记录时尚未结束日的观测 |

城市 baseline/latest 和导出快照统一使用格式 8 的显式建筑 DTO，可通过类型标签定位具体子类。拆除实体后，当日工厂报告保留已发生的产销、工资与关闭结果，历史事件 ID 不作为活动对象引用。

## 如何读分析报告

分析由模拟状态、真实事件和观测到的劳动 / 运输过程确定性生成，不调用 LLM，也不改变模拟或随机数。生产、到货、装车、实售、出勤、加工与收支使用本区间结束值减开始基线；库存和现金同时保留首末状态。装车不是销售，只有真实交付才记销售收入。资本投入单列，经营收支差额包含预付采购，不等同于按销售成本匹配的会计利润。

工厂原因与状态分开。缺料、仓满、加工资金不足以实际受阻工人分钟为证据；日末原料为零不能独自证明当天缺料。报告注明首次受阻时间、到岗和迟到人数。明显迟到指超过班次开始 15 游戏分钟；货运长等待指连续受阻至少 30 游戏分钟，按任务去重，包括后来恢复或失败的任务。道路日末断开、相关商业缺货等没有足够时间重合证据的现象单独标注，不宣称某厂是唯一原因。

家庭摘要使用本次真实候选评分和实际提交结果，区别拟选方案与实际住所 / 岗位。延期或未评估时不补造分数；工资为实收，方案日薪为预期值。城市关注项按观测到的阻断分钟、受影响人数和资金变化排序，最多五项。实际通勤均值注明样本人数，样本群体变化不能视为同一群体的因果对照。

日报 Day 是刚结束的劳动日；原始 `city.day` 和家庭结算事件沿用原系统的下一日编号，报告同时保存 `settlementDay`。住房与岗位调整发生在该次日结。读档 / 重载中途开启记录时，首日报告明确标为部分日并注明观测区间，不把记录前的累计值当成今日发生量。导出未结束日不会触发日结。分析写入错误单独提示，原始日志继续保留。

## 关联与事件

共同字段：`schema`（日志格式版本）、`session`、递增 `seq`、真实 UTC `utc`、游戏 `day/minute`、模拟累计秒 `simulationSeconds`、`type/level/message`、关联实体编号、`roadRevision`、结构化 `data`。实体编号为 -1 表示不适用；车辆数据中的出发地/目的地 -1 表示城外。以会话+编号识别实体，不把不同新城市相同的居民编号混在一起。

- `household.created/decision/unchanged/deferred/arrived/moved/jobs_changed/left`：申请、全部住房候选评分、拒绝、延期、迁入、搬家、换工作、迁出。候选携带并列的 citizenIds/jobIds，说明具体哪个成员准备占用哪个岗位。
- `job.created/assigned/released/removed`：岗位及其占用关系。
- `trip.started/departure_wait/state/redirected/arrived/returning/finished/cancelled/failed/resume`：出发、发车等待、在途状态、改目的地、到达和结束；含路线、路段进度、载货和返程标志。
- `citizen.attendance/pay`：实际到岗的通勤记录，以及每日工作分钟、挣得工资、四舍五入入账值。`household.settlement` 保存家庭结算后储蓄与各类实际支出。
- `housing.rent_changed`：挂牌租金调整前后数值及入住户数。
- `building.created/demolished/rejected/access`、`road.created/demolished/undone/rejected`、`network.audit`：建设结果、拒绝原因、连通性与路网快照。
- `city.day/invariant.cash`：每日统计和资金守恒异常。日结原有代码在跨天时执行，结算事件沿用现有每日报表的 day 字段。
- `session.start/end/export`、`save.success`、`unity.warning/error/exception/assert`：记录边界、保存和引擎异常；异常数据含堆栈。

例如人口为 0：先看 baseline/latest 的住房与岗位，再搜 `household.decision` 的 rejection；搜索相同 buildingId 的 `building.access`，结合最近一次 `network.audit` 判断道路是否真的连通。白车迟到：按 citizenId 找 trip.started、trip.state 和 citizen.attendance，再看 citizen.pay。

## 可选分析工具

项目自带 `Tools/AnalyzeSimulationLog.py`，只使用 Python 标准库：

```text
python Tools/AnalyzeSimulationLog.py "导出目录"
python Tools/AnalyzeSimulationLog.py "导出目录" --citizen 10
python Tools/AnalyzeSimulationLog.py "导出目录" --household 1 --type household.decision --json
python Tools/AnalyzeSimulationLog.py "导出目录" --trip 42
python Tools/AnalyzeSimulationLog.py "导出目录" --day 5 --json
```

无筛选时给出事件统计与住房候选拒绝原因；筛选时列出时间线，加 `--json` 输出完整数据。遇到崩溃导致的未完成行会提示并跳过，同时检查序号是否有缺口。

## 边界与性能

日志从本次启用开始，不补造历史；开始记录前的实体由 baseline 保存。读取存档或脚本重载会结束旧会话并开启新会话，session.start 中包含上一会话编号。暂停不推进游戏时间，导出不推进模拟。

不逐帧写位置：车辆状态每个模拟秒观察一次，变化或持续等待 30 模拟秒时写入；真正出发/到达等事件在发生时直接记录，因此短行程也能追踪。每约 8 MB 切换文件，不自动删旧日志。运行时每 2 秒刷新，正常关闭时刷新；进程崩溃仍可能丢失最后尚未刷新的记录。磁盘不可写时在游戏提示并停止记录，不阻断模拟。需要自行清理不用的会话目录。长期高人口城市仍需要磁盘与性能预算；这不是逐帧确定性回放系统。

日志导出含完整城市状态和 Unity 异常堆栈（可能包含本机路径），分享前可自行检查。
