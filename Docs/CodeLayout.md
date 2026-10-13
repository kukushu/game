# 代码目录与职责

本轮仅整理文件位置、Unity 引用和开发工具路径，不更换道路或交通算法，不实施道路迁移第二阶段。90 个已有脚本的内容与 `.meta` GUID 原样保留；映射及迁移时 SHA256 见 `Tools/CodeLayout.json`。

```text
Assets/
  Scripts/
    Runtime/
      Simulation/
        Core/          城市、建筑、存档、建筑状态
        Residents/     家庭、居民、岗位、年龄与教育
        Economy/       贷款、服务预算
        Industry/      生产与库存
        Services/      医疗、消防、治安、垃圾与死亡处理
        Growth/        里程碑与建筑生长
      Infrastructure/
        Roads/         施工网络、道路属性、几何和派生逻辑道路
        Utilities/     水电管网
        Zoning/        沿路分区
        Map/           地图与土地管理
      Traffic/
        Trips/         通勤、购物、垃圾运输任务
                       其余文件负责运动、车道、路口及局部索引
      Observability/   城市分析、统计展示数据和结构化日志
      Presentation/
        Game/          主游戏组件和系统连接
        Tools/         建造工具与交互
        World/         地形、道路、地图边界可视化
        Traffic/       路口控制可视化
        UI/            面板、详情、提示和相机定位
    Diagnostics/
      Runtime/         显式启用的基准、Profiler 归因和独立运行验收
    Editor/            编辑器验证、构建与采样导出
  Scenes/ Resources/ Shaders/ …   原位置保留
Tools/                            模型回归、基准脚本、固定城市存档
Docs/                             路线、设计及验证证据
```

## 依赖边界与后续维护

目录表示职责，不表示新增程序集。所有运行时代码仍属于 Assembly-CSharp，Editor 代码仍属于 Assembly-CSharp-Editor；命名空间、类名、序列化字段均不变。现有 CityModel、CityTraffic、HarborCityGame 的 partial 文件仍跨目录共享一个类。本轮没有加入 asmdef，否则现有交叉依赖与 partial 类需要同时调整，超出目录整理范围。

新增模拟代码优先放 Simulation，基础网络放 Infrastructure，车辆与任务放 Traffic，Unity 表现和交互放 Presentation。诊断代码独立归档，但是否执行仍由原有开关或命令行决定。之后若拆分大型 partial，应单独设计、验证和提交，不能把移动目录视为已完成架构解耦。

`Tools/ProjectLayout.ps1` 集中提供递归模型源码发现及隔离 Unity 工程同步。模型测试排除 Editor 和两个 Unity 世界表现组件，按原文件名顺序编译。同步只在当前工程 Temp 子目录内删除不再存在的旧脚本及空目录，避免复制后残留旧路径导致重复类或 GUID。不会删除玩家城市或正常工程资源。

## 验证

运行 `Tools/VerifyCodeLayout.ps1` 检查 90 个文件内容与 GUID、旧路径残留和 PowerShell 语法。迁移时清单是历史证据，后续正式修改源码后其 SHA256 自然不再匹配，不应当成永久禁止修改源码的检查。

隔离 Unity 工程运行 `HarborCity.CityCodeLayoutChecks.Run`，核对导入后 GUID→路径、MonoScript 类型及 SampleScene 的缺失组件。清单及结果路径分别通过 HARBOR_CODE_LAYOUT_MANIFEST / HARBOR_CODE_LAYOUT_RESULT 指定。不得在玩家当前场景执行。

本轮验证记录见 `Docs/Benchmarks/2026-10-13-CodeLayout/`。目录移动前后的固定城市 340 步状态逐项对照；完整模型回归与正常场景独立运行验证分别记录。目录整理没有改变运行时源码，因此不声称获得新的 FPS 或 CPU 收益，之前道路哈希收益不变。
