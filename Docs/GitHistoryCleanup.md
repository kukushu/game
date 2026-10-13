# 2026-10-13 GitHub 推送历史清理

阻塞文件是 ProfilerCaptures/test_2026-10-11_23-41-42.data，571779620 字节（约545 MiB），首次加入原15632d7。操作时 origin/main 为262adb0，后续7笔提交均未推送。

仅重建这7笔本地提交，排除上述一个文件；每笔树与原提交相比只有此路径消失，原作者、提交者、时间、编码及说明字节保留，顺序不变。最终代码、Assets GUID、Packages、Tools、文档及小型采样配套文件没有改动。历史重建使提交ID及其后代ID变化；原先文档和基准记录中的历史ID保留，通过下表查对应新ID。

原采样文件仍在原路径，内容保留；.gitignore新增ProfilerCaptures下.data/.raw规则防止再次入库，截图等小型已跟踪文件保留。没有迁移到LFS，没有覆盖GitHub已经存在的提交，推送采用main正常快进。

原完整历史保存在本地分支 codex/backup-before-profiler-cleanup-20261013，以及 .git/history-backups/main-before-profiler-cleanup-20261013.bundle；bundle已校验为完整历史。备份分支仅本地保存，本次只推送main。原完整状态及新旧映射另保存在.git/history-backups/recovery-state.json、commit-map.json。

## 提交映射
| 原提交 | 清理后提交 | 说明 |
|---|---|---|
| 15632d7a762014646fba900b4a089914ec1d998b | 38cce4da85d5a4e1463c7b9361e7a2b795fa0803 | 一部分更改 |
| d151b0735ac3055bb1c3d58e2c7d133cf0a1ac48 | 5ff10d3adfa5d444cdd010b0c71bf2b2ba178101 | 补齐逻辑道路第一阶段验收：保持7–8ms模拟步及三倍速 |
| 883756f7e8e328decdd8d1b860941caa4ce4d162 | f3f1b5737c4619bb412a748c13f986872de39605 | 整理代码目录：按职责归档90个脚本并保留Unity引用 |
| 54b293f0a31f26daec2056f21bc8c29a1019428c | 6d97f46cb1dc6433dcb68cc0de40351188889cc8 | 迁移道路与车道静态查询到逻辑道路快照 |
| bb6a7f0e85feb5f40ee2bc675edbdc9c98d65644 | 230539a4600d0f86168e059ed0bff1c5c490124c | 曲线道路预览复用单笔私有草稿，保留原施工结果 |
| dcf2faed78e514a3019765b123afcc373a89fa43 | 7fb0fff2f75e4cf8683f361c85394e2dd98e660f | 道路渲染和路面高度读取共享中心线几何 |
| 9cff94114ea4364d63418d04434aa20b596b8c1d | 65dc3aa4ff74cbb82cc2bf0249dc8f532a5eb0ac | 按逻辑道路边界寻路并保留精确旧路线选择 |
