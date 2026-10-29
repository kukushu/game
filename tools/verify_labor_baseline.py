"""Compare unchanged pooled trajectories with committed pre-experiment code."""
import hashlib
import io
import json
import os
from pathlib import Path
import subprocess
import sys
import zipfile
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from landlab.model import LandCity

root=Path(__file__).resolve().parents[1]
baseline=root/'outputs'/'labor-baseline-40981b2'
baseline.mkdir(parents=True,exist_ok=True)
archive=subprocess.run(['git','archive','--format=zip','40981b2','commerce','playweb','landlab'],cwd=root,check=True,capture_output=True).stdout
with zipfile.ZipFile(io.BytesIO(archive)) as z:
    for entry in z.infolist():
        if not (baseline/entry.filename).resolve().is_relative_to(baseline.resolve()):raise ValueError('invalid archive path')
    z.extractall(baseline)
code='''import json
from landlab.model import LandCity
out=[]
for seed in (1,7,23):
 c=LandCity(seed);c.income_tax_rate=10
 for _ in range(18):c.step()
 out.append(c.snapshot())
print(json.dumps(out,sort_keys=True))
'''
old=json.loads(subprocess.run([sys.executable,'-c',code],cwd=baseline,env={**os.environ,'PYTHONPATH':str(baseline)},check=True,capture_output=True,text=True).stdout)
out=[]
for seed,reference in zip((1,7,23),old):
    c=LandCity(seed);c.income_tax_rate=10
    for _ in range(18):c.step()
    current=c.snapshot();current['_land'].pop('employment');current=json.loads(json.dumps(current))
    digest=lambda x:hashlib.sha256(json.dumps(x,sort_keys=True).encode()).hexdigest()
    out.append({'seed':seed,'months':18,'exact_old_pool_state':current==reference,'current_sha256':digest(current),'baseline_sha256':digest(reference)})
assert all(x['exact_old_pool_state'] for x in out)
(root/'outputs/labor-market-v1/baseline-check.json').write_text(json.dumps(out,indent=2),encoding='utf-8')
print(json.dumps(out))
