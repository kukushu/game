性能数据共享与发布

入口
index.json 是机器可读总索引；每个日期目录的 manifest.json 列出真实数据文件、
用途、校验值、CSV列类型和单位、测试元数据，以及只在本机保存的原始资源。
其他ChatGPT会话从GitHub读取 index.json → 选择manifest → 读取artifacts中的CSV/JSON。
不要只读Findings/README，也不要把文件完整性校验当作性能或业务验收通过。

目录约定
Docs/Benchmarks/index.json
Docs/Benchmarks/YYYY-MM-DD-Topic-RunID/manifest.json
  data/          新批次逐步/逐帧/层级/线程等原始导出
  metadata/      原有采样器的environment.json、资源限制/退出状态、导出receipt
  analysis/      计算后的summary及分析口径、必要的小脚本
  source/        若使用隔离插桩或临时覆盖源码，保留可审查的脚本/覆盖文件
旧目录不搬迁，不重复复制已有数据；manifest支持原布局。新ID不能覆盖已有manifest。
本次已登记40个目录、1362个文件。39个历史目录原位保留，另加本地旧Profiler资源清单。
新旧计数、内容和状态均以index/manifest实际记录为准。

测试元数据
measurement区分归因attribution、真实窗口window、模型model、microbenchmark、
experiment和历史legacy。记录被测源码的完整SHA、原采样UTC时间、Unity版本、
实际runtime/插桩/构建配置、CPU/OS、场景、测量方法、argv命令和限制。
打包时刻packaged_at_utc与采样时刻分开。没有记录的数据写null并说明unknown_reasons；
不能拿当前机器/当前HEAD/文件修改时间冒充历史测量环境。
原有采样器的environment.json按原样共享；一个历史目录含多个运行时，以各run记录为准。
legacy清单只保证文件完整；CSV单位unknown不推断，无法解析的原文件标legacy-opaque。
新批次CSV必须在spec中逐列声明type/text/number/integer和unit，行数/列顺序自动登记。
未知数值如GPU=-1须在测量口径说明；不默认为零。父子层级时间不可相加。

现有工具保持职责
RunCityScenario.ps1、MeasureCitySteps.ps1、RunCityScenarioWindow.ps1等继续负责原采样。
VerifyCityScenarioWindow.py、VerifyCityWindowState.ps1等仍负责各自语义/时钟/状态检查。
ExportCityWindowProfile.ps1仍调用原Unity导出器，成功后自动生成export-receipt.json。
PerformanceData.py负责打包、格式/完整性/索引校验、链接和Git发布，不运行这些测试。
这套共享校验不会替代原业务断言，也不会把失败/超时/归因版当作普通性能通过。

正式批次的操作
1. 用已有工具采样/导出，保留原始文件和原退出状态。无需为了发布重复采样。
2. 复制 Tools/PerformanceData.template.json 到 Temp，填真实元数据、明确列单位、
   逐项列出需要共享的轻量文件，给每个本地raw登记export状态和关联的共享文件。
   source可为绝对路径或仓库根相对路径；path是包内相对路径。
3. 打包（不会构建、运行或重新导出Unity）：
   python Tools/PerformanceData.py prepare --spec Temp/my-performance-spec.json
   已有未登记的历史目录可以加 --adopt；它只增加manifest，不改原数据。
4. 生成总索引并离线校验：
   python Tools/PerformanceData.py index
   python Tools/PerformanceData.py verify --all
   本机还可加 --local，核对登记的raw/输入/构建是否仍存在且SHA相同。
   GitHub/其他电脑只需导出的共享文件，默认不要求本地绝对路径存在。
5. 先查看发布范围：
   ./Tools/PublishPerformanceData.ps1 -Package YYYY-MM-DD-Topic-RunID -Message "perf data: 问题和口径" -Push -DryRun
6. 发布同一范围：
   ./Tools/PublishPerformanceData.ps1 -Package YYYY-MM-DD-Topic-RunID -Message "perf data: 问题和口径" -Push
   Package支持多个ID。去掉-Push仅提交本地。脚本只暂存所选包和index，不夹带别处修改，
   有任何预先暂存内容会拒绝，禁止main/master和detached HEAD，不强推。
   若push失败，本地提交保留，重试普通git push即可，无需重建/重测数据。
7. 获取固定提交的GitHub查看链接和raw.githubusercontent.com读取链接：
   python Tools/PerformanceData.py links --package YYYY-MM-DD-Topic-RunID --ref HEAD
   发布脚本也会打印这些不可变链接。把index/manifest链接给其他会话即可开始独立分析。

原始资源与导出覆盖
local_assets中的path是绝对本地路径；bytes和sha256是原始文件精确字节摘要，流式计算。
present_at_inventory是盘点时的存在状态，不是远程可下载声明；缺失文件要说明原因，
已有历史SHA可以保留，未知SHA写null，不能生成替代数据再声称同基准。
profiler_raw的export.status为partial/complete/not_exported/unknown。
artifacts必须指向本包中真正共享的导出文件；included与not_included必须说明范围。
Unity现有层级导出器过滤线程和小Self节点，所以该receipt一律标partial，不能称全量raw。
独立Stopwatch scopes.csv不冒充raw转换；派生summary注明计算脚本和口径。
本次RoadBuildPreview已登记524MB raw和两个实际导出CSV，附加原插桩/汇总脚本与覆盖源码。
ProfilerCaptures中另一个历史.data没有可靠的导出对应关系，标unknown，不宣称已经导出或从未导出。

对于已完成、但还没有receipt的旧导出，可以只盘点而不打开Unity：
python Tools/PerformanceData.py record-export --capture Temp/path/to/existing-export
要求已有window.raw、hierarchy.csv、threads.csv和成功的export-resources.json；
只记录现有导出和原始SHA，不重新导出。receipt时间不冒充实际采样/导出发生时间。

完整性与大小
共享UTF-8文本的摘要基于去BOM并规范换行为LF的UTF-8字节，digest_mode=utf8-lf。
这样Windows CRLF、Linux LF和Git自动换行不会制造假损坏；这是传输完整性规范，
不是把raw/存档的二进制摘要也规范化。图片和本地raw使用精确字节摘要。
每个共享文件不超过8MiB，每包32MiB。raw/.data、.hcity、Player、DLL不作为artifact上传。
超限时应利用现有有界导出/明确筛选，说明漏掉什么；工具不会静默裁剪或删数据。
校验拒绝哈希变化、漏列文件、路径越界、重名、损坏JSON/CSV、非有限数值、缺单位、
缺少导出目标及过时索引。失败证据也可以发布，但必须在metadata中诚实说明状态。
未来纠正口径或新增采样用新ID并引用旧ID；不要覆盖已发布样本。

自动校验
.github/workflows/performance-data.yml在相关push/PR执行离线数据校验和工具回归测试。
只依赖Python标准库，无Unity、无本地raw依赖，不跑城市、不评价FPS达标。
本机运行：
python -m unittest discover -s Tools/Tests -p test_performance_data.py -v

本批架构影响
新增事实只是发布清单与派生校验值，由Python打包工具生成；模拟状态、路由、经济、
视图和时钟均未改。原采样器/导出器保留，PS导出脚本仅在成功后追加廉价receipt。
文本流式/有限文件校验不参与游戏运行。保留所有历史数据、原始raw和玩家存档。
