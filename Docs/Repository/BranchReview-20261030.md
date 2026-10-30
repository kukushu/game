# Git 分支核查与清理建议（2026-10-30）

正式开发基准 `codex/road-core-20261014` = `d248cf339f96002ba00c85c1e426191efc52adec`。本报告只分类，不执行本地/远程分支删除、合并、重置、强推或工作树清理。

## 调查范围与判定

- 已用 `ls-remote --heads` 读取 GitHub 实际分支，并 `fetch --no-tags` 补齐缺失对象；没有 prune。覆盖 33 个既有分支名，以及新建整理分支，共 34 名；建支时本地 30、远端 30 个引用（同名合并看待）。新整理分支推送后远端会多一个引用。
- 表中是建支后、整理提交前的引用快照。本整理分支的 tip 是创建点 d248cf3；交付提交会自然前进，最终 SHA 见 Git 历史和回复，不能据创建点认定整理分支已合入。
- 完整引用、完整 tip SHA、ahead/behind、共同祖先、root、分支独有路径数量、独立提交涉及文件数、保留引用覆盖和工作树路径见 [CSV](BranchReview-20261030.csv)。local/remote 各占一行，不把它们误算成两个独立实验。
- `merge-base --is-ancestor tip development` 成功才认定全部提交被正式历史包含；不是按分支名、提交日期、patch 相似或当前源码相同判断。
- `rev-list --left-right --count development...tip` 记录正式侧/分支侧独有提交；无 merge-base 列为独立历史。`diff --diff-filter=A development tip` 检查分支有而当前开发没有的路径，并另检查独立提交修改过的文件。
- 所有本地/远端同名 tip 在此次快照一致。候选还必须没有工作树占用、没有独立提交、不在显式保护名单；工作树检查包含 detached checkout。

## 分类汇总

|分类|分支名数量|处理|
|---|---:|---|
|正式开发|1|保留正式开发，不在本轮改动|
|进行中实验/维护|2|保留独立成果，禁止整支擅自合入|
|提交完整包含于开发分支|22|21 个待确认候选；1 个工作树占用先保留|
|仍有未合入提交|4|4 个均保留，后续逐项核实价值|
|独立历史/项目|3|3 个均保留，不能按 Unity 合并状态删除|
|保护基线（已包含）|2|main/master 即使被包含也保留|

## 待用户确认的删除候选：21 个

每个候选的 local/remote tip（若两者存在）相同，且 tip 都是 d248cf3 的祖先，独立提交数为 0，无工作树占用。19 个有远端引用，2 个仅本地。**本轮一个也不删除；后续确认时仍需重新检查引用是否前进。**

|分支|tip|引用位置|当前开发缺少的路径数|
|---|---|---|---:|
|`backup/pre-lane-road-batching-20261024`|`4ac850c377da`|local|0|
|`backup/pre-legacy-cleanup-20261020`|`5c2770ed75b6`|local|214|
|`codex/baseline-road-core-20261014`|`110c4eaa24fa`|local/remote|214|
|`codex/diagnose-demolition-frame-20261025`|`7b285b797b45`|local/remote|0|
|`codex/diagnose-resident-scheduling-20261026`|`5551c5c91798`|local/remote|0|
|`codex/experiment-access-rebind-network-reuse-20261025`|`a8ee29cfb73b`|local/remote|0|
|`codex/experiment-batch-stranded-recovery-20261025`|`ea8e1aa65ef2`|local/remote|0|
|`codex/experiment-demolition-recovery-search-20261025`|`0afa420759f8`|local/remote|0|
|`codex/experiment-demolition-task-recovery-20261025`|`2df28968e865`|local/remote|0|
|`codex/experiment-demolition-waste-loss-20261025`|`e282db2e004c`|local/remote|0|
|`codex/experiment-depot-junction-dijkstra-20261022`|`20573fec96cb`|local/remote|0|
|`codex/experiment-depot-junction-routing-20261021`|`21e4513ede13`|local/remote|0|
|`codex/experiment-depot-lane-routing-20261021`|`6afc4335f192`|local/remote|0|
|`codex/experiment-incremental-occupancy-20261026`|`24dee5141e4d`|local/remote|0|
|`codex/experiment-recovery-astar`|`a703b0bf0df2`|local/remote|0|
|`codex/experiment-recovery-route-sharing-20261025`|`51422efaf0c7`|local/remote|0|
|`codex/experiment-road-view-batching-20261022`|`7d65478f5402`|local/remote|0|
|`codex/experiment-spatial-dirty-buckets-20261025`|`4ed17612c03a`|local/remote|0|
|`codex/experiment-zoning-view-delta-20261025`|`286124c0faed`|local/remote|0|
|`codex/integrate-astar-lane-sharing-20261026`|`2b261f3ed9f1`|local/remote|0|
|`codex/optimize-resident-requests-20261026`|`0bbd20f0a255`|local/remote|0|

路径数不为 0 并不代表丢失成果：例如旧基线/清理前备份包含当前已移除的旧实现，但候选 tip 的整个树和所有 blob 都通过正式分支历史可达。删除候选引用后仍可从表内 SHA 读取旧文件或重建分支，前提是保留正式历史、不重写它；这不是声称旧算法仍在当前游戏启用。两个本地备份删除只减少本地名字，不减少远端分支数。

## 必须保留或进一步核实

|分支|tip|分支侧独有提交|理由|
|---|---|---:|---|
|`codex/backup-before-profiler-cleanup-20261013`|`9cff94114ea4`|7|7 个独立提交，涉及旧逻辑路网/布局与证据；不能仅凭 backup 名称删除。|
|`codex/city-scale-architecture-20261030`|`53a9e8ca1429`|10|进行中的百万级实验，有独立时间唤醒工具、失败搜索试验与规模数据；部分在岗代码通过不同提交选择性集成不等于整支已合入。|
|`codex/citystate-ii-decompiled`|`cd4f7fed763a`|不适用|与 Unity 开发线无共同祖先，独立 C# 资料树，remote-only；保留待所有者确认用途。|
|`codex/diagnose-long-frames-residents-20261027`|`5a33899c8e43`|1|1 个独立诊断提交；涉及 19 个文件，包含真实长帧/居民证据，不因后续优化存在而视为已包含。|
|`codex/diagnose-occupancy-movement-20261026`|`fe957accc513`|2|2 个独立诊断提交；涉及 13 个文件，包含占用生命周期统计与有界负面实验。|
|`codex/optimize-growth-candidates-20261027`|`a3f7cea6d5de`|0|全部提交已包含，但工作树 road-core-integration-20261024 仍占用；可能有忽略的采样/Player，先保留，不删除工作树。|
|`codex/repository-hygiene-20261030`|`d248cf339f96`|0|本次整理分支，保留供评审，不自动合入开发线。|
|`codex/road-core-20261014`|`d248cf339f96`|0|正式开发；原工作树有三个查看城市CMD及.editorconfig用户改动，保持原样。|
|`codex/road-local-delta-experiment`|`87a43af08ede`|4|4 个独立提交，包含隔离道路增量原型和未完成窗口验收；独立工作树占用，保留。|
|`experiment/city-economy-lab`|`b8af99ba6d2a`|不适用|显式保护；与 Unity 开发线无共同祖先，是独立经济模拟项目。|
|`experiment/labor-market-v1`|`b6974cb8d41b`|不适用|显式保护；与 Unity 开发线无共同祖先，且是 economy-lab 的祖先，仍必须按用户要求保留。|
|`main`|`110c4eaa24fa`|0|显式保护；当前默认分支指向旧基线，不是正式开发线。|
|`master`|`34dd238830dc`|0|显式保护；只有远端引用。与正式开发共享历史且被包含，仍不建议删除。|

“独立历史”仅指与 Unity 正式线无共同祖先；两个经济分支之间有继承，labor-market-v1 的 tip `b6974cb8d41be032890d1fa458aa126294274fef` 是二者共同祖先。不要跨项目套用 Unity 的 AGENTS 或合并策略。

## 工作树占用

|路径|HEAD/分支|处理|
|---|---|---|
|`C:/Users/16934/Documents/unity_game/test`|`d248cf339f96` / codex/road-core-20261014|保留；不清理其文件、忽略数据或进程|
|`C:/Users/16934/.codex/worktrees/city-scenarios-stage2/test`|`facea1de8883` / detached|保留；不清理其文件、忽略数据或进程|
|`C:/Users/16934/Documents/unity_game/city-scale-architecture-20261030`|`53a9e8ca1429` / codex/city-scale-architecture-20261030|保留；不清理其文件、忽略数据或进程|
|`C:/Users/16934/Documents/unity_game/repository-hygiene-20261030`|`d248cf339f96` / codex/repository-hygiene-20261030|仅本次文档工作|
|`C:/Users/16934/Documents/unity_game/road-core-integration-20261024`|`a3f7cea6d5de` / codex/optimize-growth-candidates-20261027|保留；不清理其文件、忽略数据或进程|
|`C:/Users/16934/Documents/unity_game/road-local-delta-experiment`|`87a43af08ede` / codex/road-local-delta-experiment|保留；不清理其文件、忽略数据或进程|

旧 stage2 工作树为 detached HEAD（不是分支），即使 Git 状态干净也不自动删除；忽略的存档、Player、日志不受提交包含关系保障。

## 尚未验证的使用状态

开放 PR 占用未能读取：GitHub 连接器传输失败，公开 REST 回退也遇到 TLS 错误；没有把这项失败解释为“没有 PR”。`worktree list` 仅覆盖此本机仓库登记的工作树，不能证明其他克隆、云端任务、CI 或其他人的分支使用情况。21 个是 **Git 提交可恢复性候选**，不是已经证明无人使用的删除授权。

## 后续确认后仍需做的检查

重新读取 GitHub tip、本地同名引用、工作树和未提交内容；只有仍满足包含/保护/无占用条件的明确名单才进入删除步骤。未包含分支先审查独立提交和文件，不能用 patch 等价代替原始证据保留。恢复候选可以用表内完整 SHA 新建分支；本轮没有执行任何删除命令。
