"""Validate complete experimental outputs, independent of unit tests.

Run from the project root: python tools/validate_outputs.py outputs/main ...
"""
import csv
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from econlab.model import Economy


def validate(folder):
    root = Path(folder)
    manifest = json.loads((root / "manifest.json").read_text())
    with (root / "timeseries.csv").open(encoding="utf-8-sig") as file:
        rows = list(csv.DictReader(file))
    expected = len(manifest["seeds"]) * len(manifest["scenarios"]) * manifest["months"]
    assert len(rows) == expected, (len(rows), expected)
    baseline = {(r["seed"], r["month"]): r for r in rows if r["scenario"] == "baseline"}
    for r in rows:
        assert float(r["money_error_cents"]) == 0
        for sector in ("food", "goods"):
            assert abs(float(r[f"physical_error_{sector}"])) < 1e-6
            assert 0 <= float(r[f"{sector}_coverage"]) <= 1 + 1e-9
        if int(r["month"]) < manifest["warmup"]:
            b = baseline[r["seed"], r["month"]]
            assert {k: v for k, v in r.items() if k != "scenario"} == {k: v for k, v in b.items() if k != "scenario"}
    count = 0
    for snapshot in root.glob("runs/*/seed-*/snapshot.json"):
        state = Economy.restore(json.loads(snapshot.read_text()))
        assert state.month == manifest["months"]
        assert len(state.metrics) == state.month
        count += 1
    assert count == len(manifest["seeds"]) * len(manifest["scenarios"])
    print(f"PASS {root}: {count} branches, {len(rows)} monthly rows, shared prefixes identical, cash/stock conserved")


if __name__ == "__main__":
    for folder in sys.argv[1:] or ["outputs/main"]:
        validate(folder)
