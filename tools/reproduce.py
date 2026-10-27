"""Recreate the five reported batches; run with the project's venv Python."""
import json
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
from econlab.experiments import compare, plot
from econlab.model import Config


if __name__ == "__main__":
    batches = [
        ("main", list(range(1, 9)), 12, Config(), True),
        ("holdout", [91, 92, 93, 94], 12, Config(), False),
        ("demand-variant", [91, 92, 93, 94], 12, Config(**json.loads((ROOT / "configs/demand_variant.json").read_text())), False),
        ("no-river", [91, 92, 93, 94], 12, Config(river_penalty=0), False),
        ("early-policy", [1, 2, 3, 4], 2, Config(), False),
    ]
    for name, seeds, warmup, config, ablations in batches:
        print("Running", name, flush=True)
        out = ROOT / "outputs" / name
        compare(out, seeds, warmup=warmup, config=config, ablations=ablations)
        plot(out)
    subprocess.run([sys.executable, str(ROOT / "tools/validate_outputs.py"), *[str(ROOT / "outputs" / b[0]) for b in batches]], check=True)
