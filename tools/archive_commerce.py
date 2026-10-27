"""Keep compact, reproducible evidence in Git; large raw outputs stay local."""
import csv
import hashlib
import json
import platform
import shutil
import statistics
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT))
from econlab.experiments import bootstrap_interval,write_csv


if __name__=='__main__':
    archive=ROOT/'results/commerce-final'
    archive.mkdir(parents=True,exist_ok=True)
    for label in ('commerce-final','commerce-final-holdout','commerce-parcel-control'):
        src=ROOT/'outputs'/label
        for name in ('manifest.json','effects.csv','per_seed.csv','sizes.csv','report.md','comparison.png','locations.png'):
            if (src/name).exists():
                shutil.copyfile(src/name,archive/f'{label}-{name}')
    src=ROOT/'outputs/commerce-final/runs/separated/mixed/seed-1/snapshot.json'
    shutil.copyfile(src,archive/'example-city.json')
    def read(path):
        with path.open(encoding='utf-8-sig') as f:
            return list(csv.DictReader(f))
    central={int(r['seed']):r for r in read(ROOT/'outputs/commerce-final/per_seed.csv')
             if r['context']=='large_central' and r['channel']=='mixed'}
    edge={int(r['seed']):r for r in read(ROOT/'outputs/commerce-parcel-control/per_seed.csv') if r['channel']=='mixed'}
    effects=[]
    for metric in ('hours_per_unit','paid_price','large','small','retail_land','retail_share'):
        deltas=[float(central[seed][metric])-float(edge[seed][metric]) for seed in sorted(central)]
        low,high=bootstrap_interval(deltas)
        effects.append({'metric':metric,'central':statistics.fmean(float(r[metric]) for r in central.values()),
            'edge':statistics.fmean(float(r[metric]) for r in edge.values()),'delta_central_minus_edge':statistics.fmean(deltas),
            'ci_low':low,'ci_high':high,'seeds':len(deltas),'large_parcels_each':6})
    write_csv(archive/'matched-parcel-effects.csv',effects)
    provenance={'rules':'service-aware v2; draft e56df93 is explicitly superseded',
        'python':sys.version,'platform':platform.platform(),
        'source_sha256':{str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted((ROOT/'commerce').glob('*.py'))},
        'raw_outputs':'outputs/commerce-final, outputs/commerce-final-holdout, outputs/commerce-parcel-control',
        'runs':372,'monthly_rows':26784,'tests':'python -m unittest discover -s tests -v',
        'regression':'Unstaffed/fully-booked shops and factories cannot be selected as feasible sellers'}
    (archive/'provenance.json').write_text(json.dumps(provenance,indent=2),encoding='utf-8')
    print('Archived',archive)
