"""Read exported CitySimulationLog JSONL with Python's standard library only."""
import argparse
import collections
import json
from pathlib import Path


def main():
    parser = argparse.ArgumentParser(description="城市模拟日志：概览或按实体追踪事件")
    parser.add_argument("folder", type=Path)
    for name in ("citizen", "household", "trip", "building", "job", "day"):
        parser.add_argument("--" + name, type=int)
    parser.add_argument("--type", default="", help="事件类型前缀，例如 trip. 或 household.decision")
    parser.add_argument("--json", action="store_true", help="输出匹配事件的完整 JSONL")
    args = parser.parse_args()
    counts = collections.Counter()
    reasons = collections.Counter()
    warnings = 0
    broken_lines = 0
    previous = 0
    selected = any(getattr(args, n) is not None for n in ("citizen", "household", "trip", "building", "job", "day")) or args.type or args.json
    files = sorted(args.folder.glob("events-*.jsonl"))
    if not files:
        parser.error("此目录没有 events-*.jsonl，请选择单个会话或导出目录")
    for file in files:
        with file.open(encoding="utf-8-sig") as source:
            for number, line in enumerate(source, 1):
                try:
                    e = json.loads(line)
                except json.JSONDecodeError:
                    broken_lines += 1
                    print(f"警告：忽略损坏或未写完的行 {file.name}:{number}")
                    continue
                seq = e["seq"]
                if seq != previous + 1:
                    print(f"警告：事件序号不连续 {previous} → {seq}")
                previous = seq
                counts[e["type"]] += 1
                warnings += e["level"] in ("warning", "error")
                if e["type"] == "household.decision":
                    for option in (e.get("data") or {}).get("options", []):
                        if option.get("home", -1) >= 0 and option.get("rejection"):
                            reasons[option["rejection"]] += 1
                match = e["type"].startswith(args.type)
                for name in ("citizen", "household", "trip", "building", "job", "day"):
                    value = getattr(args, name)
                    key = name if name == "day" else name + "Id"
                    if value is not None and e.get(key) != value:
                        match = False
                if selected and match:
                    if args.json:
                        print(json.dumps(e, ensure_ascii=False))
                    else:
                        print(f"#{seq} 第 {e['day']} 天 {e['minute']:.1f} 分 | {e['type']} | "
                              f"家庭 {e['householdId']} 居民 {e['citizenId']} 车辆 {e['tripId']} | {e['message']}")
    if not selected:
        print(f"记录 {sum(counts.values())} 条，警告/错误事件 {warnings} 条，损坏行 {broken_lines} 条")
        for kind, count in counts.most_common():
            print(f"{kind}: {count}")
        print("\n住房候选拒绝原因（候选次数，非独立家庭数）：")
        for reason, count in reasons.most_common():
            print(f"{reason}: {count}")


if __name__ == "__main__":
    main()
