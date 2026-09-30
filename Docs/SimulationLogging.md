# 模拟过程日志

运行时自动开启记录；F3 模拟观察台 → **导出日志** 会保存一个独立诊断目录并打开文件夹。用 VS Code 的“打开文件夹”选择该目录即可。原“导出快照”仍保留。

自动记录：`Application.persistentDataPath/SimulationLogs/<会话>`；手动导出：`SimulationReports/<会话>-export-<唯一编号>`。当前 Windows 默认项目路径为 `C:/Users/16934/AppData/LocalLow/DefaultCompany/test/`。具体位置以游戏导出提示为准。

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
