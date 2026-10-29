# 城市经济实验室 City Economy Lab

一个独立的 Python 主体模拟原型，用少量可追踪规则探索城市建设游戏的经济机制。**不依赖 Unity 或 Citybound，也没有读取、修改现有游戏。** 这不是现实经济预测工具。

**想了解游戏怎么运行，先读：[游戏运行逻辑总览：实体、属性、决策与公式](doc/游戏运行逻辑总览.md)。** 从每月结算顺序开始，完整解释工作分配、购物、搬家、工厂经营、零售开关店、土地道路与税收，并区分已实现规则和未实现内容。

**当前架构审查：[自下而上经济模拟代码审查](doc/自下而上经济模拟代码审查.md)。** 区分当前网页与早期模型，逐项检查主体决策、真实交易、固定制度和统计口径；包括同初态实验与最小改进顺序。长期设计约束见 [AGENTS.md](AGENTS.md)。

全部 Python 验证成果统一维护在 **`experiment/city-economy-lab`**：`econlab`、`commerce`、`playweb`、`landlab` 都在这个分支。各阶段基线通过标签保留，参见 [分支与历史版本](GIT_BRANCHES.md)。没有合并进正式游戏主分支。

换电脑下载：

```sh
git clone --branch experiment/city-economy-lab --single-branch https://github.com/kukushu/game.git CityEconomyLab
```

家庭迁居已加入更明确的收益门槛，减少小幅波动引起的反复搬家，旧存档可继续使用。[规则、对照结果与代价](HOME_STABILITY.md)。

顶部“财税”可调整居民收入税，用实际工资和分红的代扣收入支付养护或扩建。默认 0%，可先试 1%，并同时观察居民税后收入与商品满足率。[收税规则、试玩与高低税对照](TAXATION.md)。

地图左上角“城市图层”可按颜色查看人口、住宅租金、实际工资与税后收入、通勤、购物缺口和生产干扰。[指标口径与浏览器截图](MAP_LAYERS.md)。

商业考察现已改为先筛合法候选，最多8块时全看，更多时按最久未考察顺序轮查，避免新规划长期被遗漏。考察记录保留每次真实评估的当时数值与全部方案，含亏损、缺钱、未选中和资格不足；地块面板可展开收入成本和全部历史。[考察记录与决策明细](doc/考察记录与决策明细.md)。

## 新增：可以亲手玩的网页实验

**默认开局已改为空白场地。** 双击 `run-land.cmd`，从零铺路、划住宅和工业许可，准备好后点“开放城市”。原有小城仍在研究菜单中，旧实验与存档保留。[空白开局说明](BLANK_START.md)。

第二轮空间玩法：双击 `run-land.cmd`，进入“土地与选址”。可以规划住宅、商业、工业或混合用地，修建相邻道路，观察家庭、工厂和商店的自主反应。新旧界面、存档和启动脚本分开，旧版 `run-web.cmd` 保留。

玩家界面已返工为城市主画面：在地图上预览和确认操作，再推进三个月看真实回信。研究参数收进右上角菜单。[界面与浏览器验收截图](PLAYER_UI.md)。经济规则没有随界面返工改变。

先看 [土地实验：试玩、规则、结果与设计判断](LANDLAB.md)。空间实验已包含在统一分支中。

双击 `run-web.cmd`，打开本机的“街区经济 · 规划试验桌”。Python 负责真实结算，HTML 负责地图、主体检查和规划操作；只在你点击推进时运行。支持同状态分叉对照、种子、自动保存、导出和导入。停止服务用 `stop-web.cmd`。

先看 [试玩方法、实验证据与限制](PLAYWEB.md)。旧网页入口与商业实验均保留在统一分支中。

## 新增：商业为什么存在？

`commerce/` 是独立的流通机制实验：比较工厂直销、零售中转与两者竞争，追踪真实运输、库存、用工、购物时间、资金和开关店。旧版 `econlab/` 没有改动。

先看 [商业实验结论](COMMERCE_FINDINGS.md) 和 [规则与运行方法](COMMERCE_DESIGN.md)。双击 `run-commerce.cmd` 可重跑一组正反条件；`python -m commerce --help` 查看命令。完整复现脚本为 `tools/reproduce_commerce.py`。

商业阶段标签为 `cityeconomylab/retail`；旧基线标签为 `cityeconomylab/baseline`（`628c173`）。

## 已准备好的运行方式

直接双击 `run-demo.cmd`：运行同城分叉对照并打开结果图；批次耗时取决于机器，控制台会在完成后列出结果。

直接双击 `debug-economy.cmd`：进入文字调试器。可以边走时间边改政策，观察主体状态。

在本目录打开 PowerShell：

```powershell
$labPython = '.\.venv\Scripts\python.exe'
# 跑一座城市，记录所有货币交易
& $labPython -m econlab run --seed 1 --months 72 --trace --out outputs/trace
# 同一城市第 12 月开始建桥并开培训班
& $labPython -m econlab run --seed 1 --months 72 --schedule configs/bridge_and_training.json --out outputs/custom
# 完整政策和规则消融对照，8 个初始种子
& $labPython -m econlab compare --ablations --plot --out outputs/comparison
# 从快照继续到第 100 月（不是再走 100 月）
& $labPython -m econlab run --resume outputs/custom/snapshot.json --months 100 --out outputs/continued
# 导出所有默认参数，再修改生成的 JSON
& $labPython -m econlab defaults --out my-config.json
& $labPython -m econlab run --config my-config.json --out outputs/my-config
# 测试
& $labPython -m unittest discover -s tests -v
```

换电脑：安装 Python 3.11+，`python -m venv .venv`，再 `.venv\Scripts\python -m pip install -r requirements.txt`。**模拟、快照、测试和网页玩法本身只用 Python 标准库**；Matplotlib 仅供 `--plot` 画图。命令行实验不启动服务；网页模式使用本机 18765 端口。

## 调试器

```text
step 12                 # 走 12 个月
status                  # 就业、消费、财政、桥进度
why                     # 最近一个月的求职/购买/资金受限计数
policy                  # 当前政策参数
set bridge_target 30    # 申请建桥，实际采购 30 单位商品后才通车
set training_seats 4    # 同时培训最多 4 个家庭劳动者
set income_tax 0.25     # 改变工资所得税
step 12
firm 3                  # 企业现金、库存、预期销量、工资、岗位等
household 20            # 家庭现金、工作、技能、消费、可达雇主
save my-city.json       # 快照含完整历史，可精确续跑
quit
```

`why` 是规则触发计数，不能单独证明某个宏观变化的原因；应配合相同初态的政策对照。`--trace` 中金额字段 `cents` 是分，价格工资也是分；汇总表显示货币单位。

## 做了什么

- 240 个家庭消费/劳动单位、16 个企业槽位、4 个城区、两类商品：必需食物和可选商品。
- 家庭按税后工资与通勤负担求职；在有限样本商店中按报价与路程选择，先买食物再买可选品。
- 企业按自己的销量、未满足的有支付能力订单和库存安排岗位；现金不够发工资就不能招聘。
- 用工产生商品。企业面对缺货或积压慢慢调价，难招人且盈利时加薪，积压时降薪。
- 政府收工资税、补助无业家庭；支付不能超余额。不凭空发钱，不从公式指定 GDP 或失业率。
- 建桥占用真实商品和财政资金，完成后降低过河阻力，改变可达工作和商店。
- 培训需要累计足够的已支付学习月份并暂时占用劳动供给，毕业后才获得可选品行业资格。
- 企业有现金分红、流动性枯竭退出和所有者自有资金再投资路径，但**产业结构不一定变化**。

详细机制、时间顺序与简化见 [DESIGN.md](DESIGN.md)。本次实际结果和批判性讨论见 [FINDINGS.md](FINDINGS.md)。

## 观察哪些文件

本次批次保留在 `outputs/main/`，需求规则变体在 `outputs/demand-variant/`，没有河流障碍的反事实在 `outputs/no-river/`。这些可再生成的大文件不提交。

| 输出 | 用途 |
|---|---|
| `comparison.png` / `.svg` | 六项时间序列；线为种子均值，浅色为种子最小最大范围 |
| `report.md` | 自动结果表、配对差异和局限说明 |
| `effects.csv` | 政策效果和配对种子 bootstrap 区间 |
| `per_seed.csv` | 每个种子的末尾 12 个月均值 |
| `timeseries.csv` | 所有情景、种子、月份的指标、资金流、诊断和守恒误差 |
| `manifest.json` | 参数、种子、时域、规则消融和干预定义 |
| `runs/<情景>/seed-N/` | 可恢复快照、企业和家庭状态、事件 |
| 单次 `--trace` 的 `transactions.csv` | 每笔钱的付款方、收款方、用途，可重新构造全部账户 |

末尾均值不是整个政策的福利收益。检查过渡过程、`food_coverage_post`、培训/采购累计支出和不同地区分布。图上的种子范围也不是置信区间。

## 研究依据

[英格兰银行的 ABM 介绍](https://www.bankofengland.co.uk/quarterly-bulletin/2016/q4/agent-based-models-understanding-the-economy-from-the-bottom-up)支持把主体异质性、局部互动和有限理性作为经济研究方法；它不替这个原型的具体规则背书。

[Furtado & Eberhardt（2016）的空间经济主体模型](https://www.jasss.org/19/4/12.html)提供了家庭、企业、地方政府及距离/资格匹配的参考。本原型借用了这类结构思路，没有复制其实现，也没有照搬房地产或公共支出提高生活质量的方程。

Citybound 提供“城市条件影响主体”的设计启发。这里没有使用其代码、程序或资源；此前在独立目录中运行的 Citybound 不参与本模型。
