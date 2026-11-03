# 已合入分支清理记录（2026-11-03）

用户确认后，以 `develop` 的 `3f3670df83eea1ffdad82c37db14b067808e4c13` 为保留基准，完成批准的27个历史分支名清理：删除24个远端引用、27个本地引用。远端38→14，本地37→10；同名本地/远端不重复算作独立成果。

## 删除前后检查

- 每个候选本地/远端提交均与批准清单一致，且为develop祖先；未包含任何未合入提交或本机工作树占用项。
- GitHub开放PR查询为0。远端以原子批次及精确预期tip租约保护删除，引用并发前进时会拒绝，未移动任何保留分支或重写提交。
- 本地采用git branch -d安全删除；删除前再次检查tip和工作树占用。未执行工作树删除、文件清理或历史压缩。
- 删除后重新读取远端列表及本地列表，所有非候选分支tip保持原样；9个用户修改/配置/存档文件哈希一致。
- main仍为GitHub默认分支，因此未删除；其归档标签archive/legacy-main-20261103保留。master、develop、旧主线历史别名codex/road-core-20261014及独立项目均保留。

## 可恢复的原提交

这些完整提交仍从develop历史可达。下表不是待合入名单，不要据此重复集成。

|旧分支名|完整提交SHA|已删除引用|
|---|---|---|
|`backup/pre-lane-road-batching-20261024`|`4ac850c377daca75df942a1f6ac9491df2e3b214`|本地|
|`backup/pre-legacy-cleanup-20261020`|`5c2770ed75b670d0ee679922f7fcc9ead48e278a`|本地|
|`codex/baseline-road-core-20261014`|`110c4eaa24fa69d55b08a5db2a6d19ec6da5dd7f`|本地、远端|
|`codex/diagnose-demolition-frame-20261025`|`7b285b797b457ce7d3cd3f7556de97cd62c046ac`|本地、远端|
|`codex/diagnose-resident-scheduling-20261026`|`5551c5c917980564b0676803d05fafc5641b22e5`|本地、远端|
|`codex/experiment-access-rebind-network-reuse-20261025`|`a8ee29cfb73b0b3b1eababfb4221e44111489cc9`|本地、远端|
|`codex/experiment-batch-stranded-recovery-20261025`|`ea8e1aa65ef2f0ba93161977e18d4546b9dc6df4`|本地、远端|
|`codex/experiment-demolition-recovery-search-20261025`|`0afa420759f833b4cb97966e624dd9f4917aef89`|本地、远端|
|`codex/experiment-demolition-task-recovery-20261025`|`2df28968e865f0d9d290d5ef6b646e556fef8884`|本地、远端|
|`codex/experiment-demolition-waste-loss-20261025`|`e282db2e004c79701019e9d92afa2045e7fd5161`|本地、远端|
|`codex/experiment-depot-junction-dijkstra-20261022`|`20573fec96cb5d7b6fa533c4b0686dde972150b3`|本地、远端|
|`codex/experiment-depot-junction-routing-20261021`|`21e4513ede136d7e9112f33ba55f2ce301a655c4`|本地、远端|
|`codex/experiment-depot-lane-routing-20261021`|`6afc4335f1929ee1a27317bc42e4088c3dd0d4cf`|本地、远端|
|`codex/experiment-incremental-occupancy-20261026`|`24dee5141e4dc4224e4f86be0ec585c64ebb666d`|本地、远端|
|`codex/experiment-recovery-astar`|`a703b0bf0df2ab509bfe624ae43b9ee5360489ba`|本地、远端|
|`codex/experiment-recovery-route-sharing-20261025`|`51422efaf0c7b2a6161d643b272f6e67eb338e0c`|本地、远端|
|`codex/experiment-road-view-batching-20261022`|`7d65478f540286d0e8ee8a0bafd19dee422fc4d5`|本地、远端|
|`codex/experiment-spatial-dirty-buckets-20261025`|`4ed17612c03a01876d949abcb02c5d90a19e1dba`|本地、远端|
|`codex/experiment-zoning-view-delta-20261025`|`286124c0faed3e9dc1e0a0418f77764493ad3aa7`|本地、远端|
|`codex/fix-vehicle-instancing-20261102`|`b2a333d9944b2029c8f4998276487fb8edcde4c8`|本地、远端|
|`codex/integrate-astar-lane-sharing-20261026`|`2b261f3ed9f169d6de500827ed218f5839de8d8c`|本地、远端|
|`codex/integrate-validated-20261101`|`baab4676ca23e03c9ccedd859c6e617c6c46371e`|本地|
|`codex/layered-presentation-20261102`|`4be2f37bebbf021d97b036a085da9428f63568e2`|本地、远端|
|`codex/million-scale-execution-20261031`|`e2429c526aca5fd0e05241a29730a0b40e44f427`|本地、远端|
|`codex/optimize-junction-occupancy-20261101`|`19e0c31f5462fde018194452e949d8f1a654cd9f`|本地、远端|
|`codex/optimize-resident-requests-20261026`|`0bbd20f0a255a7055a6f0ad4a971a265e18531e7`|本地、远端|
|`codex/resident-event-scheduler-20261101`|`7525d2658d478207ab3622da1c0b95733657c170`|本地、远端|

如需恢复，在develop仓库中使用 `git branch 原分支名 表中完整SHA`；需发布时再 `git push -u origin 原分支名`。恢复分支引用不会自动切换工作目录或覆盖文件。

## 仍保留的远端分支

- `codex/city-scale-architecture-20261030`：`53a9e8ca1429`
- `codex/citystate-ii-decompiled`：`cd4f7fed763a`
- `codex/diagnose-long-frames-residents-20261027`：`5a33899c8e43`
- `codex/diagnose-occupancy-movement-20261026`：`fe957accc513`
- `codex/optimize-growth-candidates-20261027`：`a3f7cea6d5de`
- `codex/optimize-matching-indexes-20261102`：`09fd9f8214ad`
- `codex/repository-hygiene-20261030`：`93d97f2004b3`
- `codex/road-core-20261014`：`09fd9f8214ad`
- `codex/road-local-delta-experiment`：`87a43af08ede`
- `develop`：`3f3670df83ee`
- `experiment/city-economy-lab`：`b8af99ba6d2a`
- `experiment/labor-market-v1`：`b6974cb8d41b`
- `main`：`110c4eaa24fa`
- `master`：`34dd238830dc`

未合入的backup-before-profiler-cleanup仅在本地，同样保留。city-scale实验的未提交场景修改、所有7个工作树及忽略的测试数据/Player未动。原始检查结果和执行日志位于本地Temp/BranchReview-20261103；此记录用于恢复引用，不新增归档框架。
