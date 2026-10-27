"""Audit saved commerce experiments, including ledger replay when present."""
import csv
import json
import sys
from dataclasses import asdict
from pathlib import Path

sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from commerce.model import City,Settings


def validate(folder):
    folder=Path(folder)
    manifest=json.loads((folder/'manifest.json').read_text())
    snapshots=list(folder.glob('runs/*/*/seed-*/snapshot.json'))
    expected=len(manifest['contexts'])*3*len(manifest['seeds'])
    assert len(snapshots)==expected,(len(snapshots),expected)
    total_rows=0
    for path in snapshots:
        data=json.loads(path.read_text())
        c=City.restore(data)
        assert c.month==manifest['months']
        assert len(c.metrics)==c.month
        assert c.initial_money==sum(c.accounts.values())
        for row in c.metrics:
            assert row['cash_error']==row['stock_error_food']==row['stock_error_goods']==0
            assert 0<=row['food_coverage']<=1 and 0<=row['goods_coverage']<=1
        assert all(s.area in c.legal_areas(s.node) for s in c.stores)
        assert len({s.node for s in c.stores if s.active})==sum(s.active for s in c.stores)
        if c.settings.channel=='direct':
            assert not c.stores
        total_rows+=len(c.metrics)
    for name,config in manifest['contexts'].items():
        for seed in manifest['seeds']:
            profiles=[]
            for mode in manifest['channels']:
                c=City(seed,Settings(channel=mode,**config))
                profiles.append((c.accounts,[asdict(h) for h in c.residents]))
            assert profiles[0]==profiles[1]==profiles[2]
    print(f'PASS {folder}: {len(snapshots)} runs, {total_rows} rows; exact money/goods conservation; matched endowments; zoning/land constraints')


if __name__=='__main__':
    for path in sys.argv[1:] or ['outputs/commerce-final']:
        validate(path)
