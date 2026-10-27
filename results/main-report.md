# 实验结果（自动生成）

这是机制探索，不是现实城市预测。数值未经过现实数据校准。

种子：[1, 2, 3, 4, 5, 6, 7, 8]；总月数：72；共同历史：12 月；末尾均值窗口：12 月。
同一随机种子从相同快照分叉；95% 区间仅反映这些初始化种子的配对差异。

| 情景 | 就业率 | 食物满足 | 可选品满足 | 消费量（基期价格） | 财政余额 |
|---|---:|---:|---:|---:|---:|
| baseline | 24.2% | 50.5% | 22.7% | 664.3 | 1.0 |
| bridge | 28.4% | 53.8% | 30.9% | 778.1 | 0.6 |
| training | 23.8% | 50.1% | 22.3% | 655.8 | 0.9 |
| tax_cut | 22.8% | 46.0% | 22.9% | 627.9 | 0.9 |
| safety_net | 24.8% | 51.6% | 23.2% | 678.5 | 0.9 |
| higher_tax | 30.0% | 63.2% | 26.5% | 811.3 | 0.8 |
| bridge_training | 28.6% | 54.5% | 31.0% | 784.8 | 0.6 |
| bridge_late | 24.1% | 50.4% | 22.3% | 658.5 | 0.1 |
| food_shock | 20.2% | 40.6% | 19.3% | 545.3 | 0.9 |
| fixed_prices | 18.5% | 35.9% | 17.5% | 486.2 | 0.9 |
| fixed_expectations | 24.9% | 52.4% | 23.2% | 685.7 | 0.9 |
| no_entry | 24.2% | 50.5% | 22.7% | 664.3 | 1.0 |
| full_information | 24.4% | 51.5% | 23.0% | 675.1 | 1.0 |
| fixed_buyer_order | 24.8% | 51.3% | 23.6% | 679.5 | 0.9 |

## 相对 baseline 的配对效果

| 情景 | 指标 | 平均差 | 95% 配对区间 | 正方向种子 |
|---|---|---:|---|---|
| bridge | employment | +0.042 | [+0.022, +0.058] | 7/8 |
| bridge | food_coverage | +0.032 | [+0.011, +0.053] | 6/8 |
| bridge | real_consumption | +113.777 | [+59.446, +156.634] | 7/8 |
| bridge | treasury | -0.333 | [-0.584, -0.144] | 0/8 |
| training | employment | -0.004 | [-0.012, +0.001] | 4/8 |
| training | food_coverage | -0.004 | [-0.017, +0.005] | 5/8 |
| training | real_consumption | -8.459 | [-29.572, +6.850] | 4/8 |
| training | treasury | -0.031 | [-0.118, +0.060] | 4/8 |
| tax_cut | employment | -0.014 | [-0.020, -0.008] | 0/8 |
| tax_cut | food_coverage | -0.045 | [-0.057, -0.034] | 0/8 |
| tax_cut | real_consumption | -36.412 | [-50.257, -22.576] | 0/8 |
| tax_cut | treasury | -0.059 | [-0.152, +0.021] | 4/8 |
| safety_net | employment | +0.006 | [+0.001, +0.013] | 6/8 |
| safety_net | food_coverage | +0.011 | [+0.002, +0.021] | 6/8 |
| safety_net | real_consumption | +14.189 | [-0.910, +31.006] | 5/8 |
| safety_net | treasury | -0.098 | [-0.184, -0.010] | 2/8 |
| higher_tax | employment | +0.058 | [+0.047, +0.071] | 8/8 |
| higher_tax | food_coverage | +0.127 | [+0.106, +0.150] | 8/8 |
| higher_tax | real_consumption | +147.048 | [+122.175, +175.886] | 8/8 |
| higher_tax | treasury | -0.179 | [-0.311, -0.056] | 2/8 |
| bridge_training | employment | +0.044 | [+0.020, +0.064] | 7/8 |
| bridge_training | food_coverage | +0.040 | [+0.008, +0.066] | 7/8 |
| bridge_training | real_consumption | +120.487 | [+55.856, +176.659] | 7/8 |
| bridge_training | treasury | -0.413 | [-0.688, -0.178] | 0/8 |
| bridge_late | employment | -0.001 | [-0.010, +0.007] | 4/8 |
| bridge_late | food_coverage | -0.002 | [-0.016, +0.011] | 4/8 |
| bridge_late | real_consumption | -5.743 | [-28.687, +15.223] | 4/8 |
| bridge_late | treasury | -0.896 | [-0.994, -0.812] | 0/8 |
| food_shock | employment | -0.040 | [-0.046, -0.033] | 0/8 |
| food_shock | food_coverage | -0.099 | [-0.112, -0.086] | 0/8 |
| food_shock | real_consumption | -118.952 | [-137.240, -99.691] | 0/8 |
| food_shock | treasury | -0.088 | [-0.259, +0.087] | 2/8 |
| fixed_prices | employment | -0.058 | [-0.067, -0.049] | 0/8 |
| fixed_prices | food_coverage | -0.146 | [-0.161, -0.133] | 0/8 |
| fixed_prices | real_consumption | -178.134 | [-202.283, -154.432] | 0/8 |
| fixed_prices | treasury | -0.093 | [-0.249, +0.048] | 3/8 |
| fixed_expectations | employment | +0.007 | [-0.003, +0.017] | 6/8 |
| fixed_expectations | food_coverage | +0.019 | [+0.001, +0.034] | 6/8 |
| fixed_expectations | real_consumption | +21.394 | [-5.876, +45.952] | 6/8 |
| fixed_expectations | treasury | -0.083 | [-0.153, -0.007] | 2/8 |
| no_entry | employment | +0.000 | [+0.000, +0.000] | 0/8 |
| no_entry | food_coverage | +0.000 | [+0.000, +0.000] | 0/8 |
| no_entry | real_consumption | +0.000 | [+0.000, +0.000] | 0/8 |
| no_entry | treasury | +0.000 | [+0.000, +0.000] | 0/8 |
| full_information | employment | +0.002 | [-0.007, +0.010] | 4/8 |
| full_information | food_coverage | +0.010 | [-0.005, +0.026] | 5/8 |
| full_information | real_consumption | +10.813 | [-12.241, +34.973] | 4/8 |
| full_information | treasury | +0.023 | [-0.063, +0.097] | 6/8 |
| fixed_buyer_order | employment | +0.006 | [-0.008, +0.015] | 7/8 |
| fixed_buyer_order | food_coverage | +0.007 | [-0.017, +0.025] | 6/8 |
| fixed_buyer_order | real_consumption | +15.205 | [-20.275, +42.706] | 7/8 |
| fixed_buyer_order | treasury | -0.064 | [-0.167, +0.033] | 2/8 |

## 诊断与边界

- 完整时间序列见 timeseries.csv；每个主体的最终状态和可恢复快照在 runs/。
- 物价是活跃企业报价的固定篮子指标；不把成交额或基期消费量称为 GDP。
- 建桥需要政府实际采购商品；教育占用劳动并支付学员补贴，但尚未模拟教师和校舍。
- 不把低收入家庭吃不饱解释成技术上供给不足：同时查看 budget / stock / access 原因计数。
- 地理分布、初始技能、决策顺序和需求预期都有结构性影响；查看消融和额外配置实验。
- 没有信贷、外贸、迁入、住房、死亡、输入品或机器投资；长期衰退可能是这些缺失和规则的产物。
- 固定企业槽位；只有真实退出再投资才能换产业，不能保证观察到产业转型。

最大现金误差（分）：0；最大商品守恒误差：4.26e-14。
