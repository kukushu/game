# 三万人4441步冷分区尖峰细分

基线0444e58，使用同一threshold城市从4406执行40个完整固定步。Normal控制与新细分采样最终完整保存同SHA c30c0334956f5b7b147d9f05dab666966b06192c1be75237d04fa03c2aec88fa。两次独立串行Mono进程绑定CPU0，无视图，不是Unity帧率。6000步采样开关完整保存及23461城市回归通过。

新增PlacementTerrain/PlacementBuildings/ClearanceQuery/ClearanceOverlap，均静态枚举及可复用Unity标记，调用处仅CITY_CORE_ATTRIBUTION存在。没有修改算法/实体/存档。层级为Step→Growth→ZoningRebuild→ZoningGeometry→Placement→各检查；ClearanceQuery和ClearanceOverlap为Placement下连续范围，不能把父子时间相加。

4441步2002.1ms/204.13MB；Placement 168080次1240.6ms/68.67MB，其中地形79.0ms/43.03MB，建筑7.03ms/0B，道路空间查询1060.1ms/10.85MB，道路带重叠45.92ms/14.79MB。ZoningGeometry自身616.36ms/130.90MB，Occupancy50.46ms/2.91MB，GrowthSearch28.11ms/2768B。诊断本身有开销，不能把2002ms当普通运行退化；普通ABBA中同一步1858–1926ms。

结论：主要CPU在168080次RoadSpatialIndex.Project，逐个候选反复从不可变Geometry中按距离二分采样同一段端点。优先研究把静态裁剪段坐标/投影分母在每个Network的惰性空间索引构建时计算一次；必须保留原候选顺序、最近点、同分选择和完整输出浮点位。索引跟随Network替换自然失效，不能以缓存成为另一份可修改道路。

分配仍有独立来源：地形检查为每候选生成四个RoadNode和数组；大量矩形SAT也生成轴数组。不能把全部204MB归为路由或GC暂停。暂未修改这些算法，本批只观察，后续各自成批测量。原始CSV/保存/环境留Temp/CityScenarios/placement30k-{normal-control,fine}，轻量数据见Benchmarks/2026-10-17-Placement。

## 进一步排除与标识格式化归因

空间缓存之后，SAT轴数组移除实验只让该步少5.18MB，Geometry Self仅少0.32MB；没有整日CPU收益，所以产品已回退，详见SATAllocationExperiment20261017.md。不能把此前“可能大量SAT数组”当成已证实根因。

增加仅诊断构建的ZoningCellKey范围后，4441步158070次地块Key格式化约288.78ms、36.10MB；ZoningGeometry剩余Self约394.42ms、94.80MB。完整40步保存仍应与原c30c033...一致（原文件留Temp/zoning-key30k-fine）。这是归因数据，不是优化收益；采样增加计时开销。地块标识本身是稳定数据，任何后续优化都必须保留全部字符串/坐标舍入/文化不变式，不能为省时间改变规划身份。未实施新Key规则，也没有恢复大型架构重写。
