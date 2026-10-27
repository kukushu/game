"""Recreate the archived intermediation study (run with the project venv)."""
import subprocess
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT))
from commerce.experiments import compare


if __name__=='__main__':
    compare(ROOT/'outputs/commerce-main',list(range(1,9)),plot=True)
    compare(ROOT/'outputs/commerce-holdout',[91,92,93,94],
            contexts=['separated','cheap_access','sparse','equal_assortment','freight_road'],plot=True)
    subprocess.run([sys.executable,str(ROOT/'tools/validate_commerce.py'),str(ROOT/'outputs/commerce-main'),
                    str(ROOT/'outputs/commerce-holdout')],check=True)
