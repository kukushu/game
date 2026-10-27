"""Paired counterfactuals. Definitions are fixed before viewing their output."""
import copy
import csv
import json
import random
import statistics
from dataclasses import asdict
from pathlib import Path

from .model import Config, Economy

SCENARIOS = {
    "baseline": [],
    "bridge": [(0, "bridge_target", 30)],
    "training": [(0, "training_seats", 4)],
    "tax_cut": [(0, "income_tax", .04)],
    "safety_net": [(0, "benefit", 3.0)],
    "higher_tax": [(0, "income_tax", .25)],
    "bridge_training": [(0, "bridge_target", 30), (0, "training_seats", 4)],
    "bridge_late": [(12, "bridge_target", 30)],
    "food_shock": [(0, "food_shock", .65), (12, "food_shock", 1.0)],
}
ABLATIONS = {
    "fixed_prices": {"enable_price_adaptation": False},
    "fixed_expectations": {"enable_demand_adaptation": False},
    "no_entry": {"enable_entry": False},
    "full_information": {"search_all_sellers": True},
    "fixed_buyer_order": {"shuffle_buyers": False},
}
MEASURES = ("employment", "food_coverage", "goods_coverage", "real_consumption", "basket_price",
            "treasury", "household_cash_gini", "skilled_share", "employment_d0", "employment_d3",
            "cash_constrained_firms", "skill_blocked", "commute_blocked", "bridge_open")


def write_csv(path, rows):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    if not rows:
        return
    keys = list(dict.fromkeys(key for row in rows for key in row))
    with path.open("w", newline="", encoding="utf-8-sig") as handle:
        writer = csv.DictWriter(handle, keys)
        writer.writeheader()
        writer.writerows(rows)


def run_until(model, months, schedule=()):
    # months is the absolute end, not a count to append to a saved game.
    for event in schedule:
        if not isinstance(event.get("month"), int) or event["month"] < model.month or event["month"] >= months:
            raise ValueError("Every scheduled month must be an integer in the remaining run interval")
        probe = copy.deepcopy(model)
        probe.intervene(event["action"], event["value"])
    while model.month < months:
        for event in schedule:
            if event["month"] == model.month:
                model.intervene(event["action"], event["value"])
        model.step()
    return model


def bootstrap_interval(values):
    if len(values) == 1:
        return [values[0], values[0]]
    r = random.Random(76003)
    means = sorted(statistics.fmean(r.choices(values, k=len(values))) for _ in range(1500))
    return [means[37], means[1462]]


def export_run(model, out):
    out = Path(out)
    out.mkdir(parents=True, exist_ok=True)
    model.save(out / "snapshot.json")
    write_csv(out / "metrics.csv", model.metrics)
    write_csv(out / "households.csv", [asdict(h) for h in model.households])
    write_csv(out / "firms.csv", [asdict(f) for f in model.firms])
    if model.trace:
        write_csv(out / "transactions.csv", model.ledger)
        write_csv(out / "last_decisions.csv", model.last_decisions)
    (out / "events.json").write_text(json.dumps(model.events, indent=2), encoding="utf-8")


def compare(out, seeds, months=72, warmup=12, tail=12, config=None, ablations=False):
    if not seeds or len(set(seeds)) != len(seeds):
        raise ValueError("Use nonempty, unique seeds")
    if not 0 < warmup < months or not 0 < tail <= months - warmup:
        raise ValueError("Require 0 < warmup < months and tail <= post-intervention months")
    out = Path(out)
    out.mkdir(parents=True, exist_ok=True)
    all_rows, summaries, events = [], [], []
    scenarios = dict(SCENARIOS)
    if ablations:
        scenarios.update({name: [] for name in ABLATIONS})
    for seed in seeds:
        original = Economy(seed, copy.deepcopy(config) if config else Config())
        run_until(original, warmup)
        fork = original.snapshot()
        for name, relative_events in scenarios.items():
            model = Economy.restore(fork)
            for key, value in ABLATIONS.get(name, {}).items():
                setattr(model.config, key, value)
            schedule = [{"month": warmup + offset, "action": action, "value": value}
                        for offset, action, value in relative_events if warmup + offset < months]
            run_until(model, months, schedule)
            export_run(model, out / "runs" / name / f"seed-{seed}")
            all_rows.extend({"scenario": name, "seed": seed, **row} for row in model.metrics)
            sample = model.metrics[-tail:]
            summary = {"scenario": name, "seed": seed, **{key: statistics.fmean(r[key] for r in sample) for key in MEASURES}}
            summary.update({"closures_total": sum(r["closures"] for r in model.metrics[warmup:]),
                            "entries_total": sum(r["entries"] for r in model.metrics[warmup:]),
                            "graduates_total": sum(r["graduates"] for r in model.metrics[warmup:]),
                            "procurement_total": sum(r["flow_public_procurement"] for r in model.metrics[warmup:]),
                            "training_total": sum(r["flow_training_stipend"] for r in model.metrics[warmup:]),
                            "food_coverage_post": statistics.fmean(r["food_coverage"] for r in model.metrics[warmup:]),
                            "max_cash_error": max(abs(r["money_error_cents"]) for r in model.metrics),
                            "max_goods_error": max(abs(r["physical_error_goods"]) for r in model.metrics)})
            summaries.append(summary)
            events.extend({"scenario": name, "seed": seed, **event} for event in model.events)
    write_csv(out / "timeseries.csv", all_rows)
    write_csv(out / "per_seed.csv", summaries)
    write_csv(out / "events.csv", events)
    baseline = {row["seed"]: row for row in summaries if row["scenario"] == "baseline"}
    effects = []
    for name in scenarios:
        group = [row for row in summaries if row["scenario"] == name]
        for key in MEASURES:
            deltas = [row[key] - baseline[row["seed"]][key] for row in group]
            low, high = bootstrap_interval(deltas)
            effects.append({"scenario": name, "metric": key, "mean": statistics.fmean(row[key] for row in group),
                            "paired_delta": statistics.fmean(deltas), "paired_ci_low": low, "paired_ci_high": high,
                            "positive_seeds": sum(x > 1e-10 for x in deltas), "seeds": len(seeds)})
    write_csv(out / "effects.csv", effects)
    manifest = {"seeds": seeds, "months": months, "warmup": warmup, "tail": tail,
                "config": asdict(config or Config()), "scenarios": scenarios,
                "ablations": ABLATIONS if ablations else {}, "interval": "1500 paired seed bootstrap resamples; not model uncertainty"}
    (out / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    make_report(out, summaries, effects, manifest)
    return summaries, effects


def make_report(out, summaries, effects, manifest):
    lines = ["# 实验结果（自动生成）", "", "这是机制探索，不是现实城市预测。数值未经过现实数据校准。", "",
             f"种子：{manifest['seeds']}；总月数：{manifest['months']}；共同历史：{manifest['warmup']} 月；末尾均值窗口：{manifest['tail']} 月。",
             "同一随机种子从相同快照分叉；95% 区间仅反映这些初始化种子的配对差异。", "",
             "| 情景 | 就业率 | 食物满足 | 可选品满足 | 消费量（基期价格） | 财政余额 |",
             "|---|---:|---:|---:|---:|---:|"]
    scenarios = list(manifest["scenarios"])
    for name in scenarios:
        group = [r for r in summaries if r["scenario"] == name]
        av = lambda key: statistics.fmean(r[key] for r in group)
        lines.append(f"| {name} | {av('employment'):.1%} | {av('food_coverage'):.1%} | {av('goods_coverage'):.1%} | {av('real_consumption'):.1f} | {av('treasury'):.1f} |")
    lines += ["", "## 相对 baseline 的配对效果", "", "| 情景 | 指标 | 平均差 | 95% 配对区间 | 正方向种子 |", "|---|---|---:|---|---|"]
    for row in effects:
        if row["scenario"] != "baseline" and row["metric"] in ("employment", "food_coverage", "real_consumption", "treasury"):
            lines.append(f"| {row['scenario']} | {row['metric']} | {row['paired_delta']:+.3f} | [{row['paired_ci_low']:+.3f}, {row['paired_ci_high']:+.3f}] | {row['positive_seeds']}/{row['seeds']} |")
    lines += ["", "## 诊断与边界", "", "- 完整时间序列见 timeseries.csv；每个主体的最终状态和可恢复快照在 runs/。",
              "- 物价是活跃企业报价的固定篮子指标；不把成交额或基期消费量称为 GDP。",
              "- 建桥需要政府实际采购商品；教育占用劳动并支付学员补贴，但尚未模拟教师和校舍。",
              "- 不把低收入家庭吃不饱解释成技术上供给不足：同时查看 budget / stock / access 原因计数。",
              "- 地理分布、初始技能、决策顺序和需求预期都有结构性影响；查看消融和额外配置实验。",
              "- 没有信贷、外贸、迁入、住房、死亡、输入品或机器投资；长期衰退可能是这些缺失和规则的产物。",
              "- 固定企业槽位；只有真实退出再投资才能换产业，不能保证观察到产业转型。", "",
              f"最大现金误差（分）：{max(r['max_cash_error'] for r in summaries)}；最大商品守恒误差：{max(r['max_goods_error'] for r in summaries):.3g}。"]
    (out / "report.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def plot(out):
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    out = Path(out)
    with (out / "timeseries.csv").open(encoding="utf-8-sig") as handle:
        rows = list(csv.DictReader(handle))
    with (out / "manifest.json").open(encoding="utf-8") as handle:
        manifest = json.load(handle)
    shown = ["baseline", "bridge", "training", "higher_tax", "tax_cut", "food_shock"]
    fig, axes = plt.subplots(2, 3, figsize=(15, 8), layout="constrained")
    for ax, (key, title) in zip(axes.flat, [("employment", "Employment share"), ("food_coverage", "Food needs met"),
            ("real_consumption", "Consumption at base prices (not GDP)"), ("basket_price", "Posted fixed-basket price"),
            ("treasury", "Government cash"), ("skilled_share", "Qualified worker share")]):
        for name in shown:
            grouped = {m: [] for m in range(manifest["months"])}
            for row in rows:
                if row["scenario"] == name:
                    grouped[int(row["month"])].append(float(row[key]))
            x = [m for m in grouped if grouped[m]]
            if not x:
                continue
            y = [statistics.fmean(grouped[m]) for m in x]
            ax.plot(x, y, label=name, linewidth=2 if name == "baseline" else 1.3)
            ax.fill_between(x, [min(grouped[m]) for m in x], [max(grouped[m]) for m in x], alpha=.07)
        ax.axvline(manifest["warmup"], color="black", linestyle=":", linewidth=1)
        ax.set_title(title)
        ax.set_xlabel("Month")
        ax.grid(alpha=.2)
    axes[0, 0].legend(fontsize=8)
    fig.suptitle("City Economy Lab | lines: seed means; bands: seed ranges | uncalibrated")
    fig.savefig(out / "comparison.png", dpi=150)
    fig.savefig(out / "comparison.svg")
    plt.close(fig)
