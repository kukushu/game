"""Recreate the archived intermediation study (run with the project venv)."""
import subprocess
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT))
from commerce.experiments import compare


if __name__=='__main__':
    compare(ROOT/'outputs/commerce-final',list(range(1,9)),plot=True)
    compare(ROOT/'outputs/commerce-final-holdout',[91,92,93,94],
            contexts=['separated','cheap_access','sparse','equal_assortment','freight_road'],plot=True)
    compare(ROOT/'outputs/commerce-parcel-control',list(range(1,9)),contexts=['matched_edge'],plot=True)
    subprocess.run([sys.executable,str(ROOT/'tools/validate_commerce.py'),str(ROOT/'outputs/commerce-final'),
                    str(ROOT/'outputs/commerce-final-holdout'),str(ROOT/'outputs/commerce-parcel-control')],check=True)
