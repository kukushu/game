"""Check authored resources and new localizable keys; no Unity or network needed."""
from pathlib import Path
import json, re, string

root = Path(__file__).resolve().parents[1]
folder = root / 'Assets/Resources/GameData'
languages = {}
for path in sorted((folder / 'Localization').glob('*.json')):
    data = json.loads(path.read_text(encoding='utf-8-sig'))
    entries = {item['key']: item['text'] for item in data['entries']}
    assert len(entries) == len(data['entries']), f'Duplicate key: {path}'
    languages[data['language']] = entries
assert set(languages['zh-CN']) == set(languages['en'])
for key in languages['zh-CN']:
    slots = lambda text: {field for _, field, _, _ in string.Formatter().parse(text) if field is not None}
    assert slots(languages['zh-CN'][key]) == slots(languages['en'][key]), key
used = set()
for path in (root / 'Assets/Scripts/Runtime').rglob('*.cs'):
    source = path.read_text(encoding='utf-8-sig')
    used.update(re.findall(r'new CityRuleException\("([^"]+)"', source))
    used.update(re.findall(r'localization.Get\("([^"]+)"', source))
for path in (root / 'Assets/Scripts/Runtime/Presentation').rglob('*.cs'):
    source=path.read_text(encoding='utf-8-sig')
    for key, dynamic in re.findall(r'text.Get\("([^"\n]+)"(\s*\+)?', source):
        if dynamic:
            assert any(k.startswith(key) for k in languages['zh-CN']), key
        else:
            used.add(key)
assert used <= set(languages['zh-CN']), f'Missing translations: {used - set(languages["zh-CN"])}'
asset = (folder / 'Economy/DefaultEconomy.asset').read_text(encoding='utf-8-sig')
guid = re.search(r'guid: ([0-9a-f]{32})', asset).group(1)
meta = (root / 'Assets/Scripts/Runtime/GameData/HarborCityEconomyAsset.cs.meta').read_text(encoding='utf-8-sig')
assert 'guid: ' + guid in meta
for filename, classes in [('CityGameDesign.cs', ['EconomyDefinition']), ('CityEmploymentDesign.cs', ['EmploymentDefinition']), ('CityFinanceDesign.cs', ['FinanceDefinition']), ('CityWasteDesign.cs', ['WasteDefinition']), ('CityRetailDesign.cs', ['RetailDefinition', 'ProductionDefinition'])]:
    source = (root / 'Assets/Scripts/Runtime/GameData' / filename).read_text(encoding='utf-8-sig')
    for cls in classes:
        definition = source.split('public sealed class ' + cls)[1].split('}')[0]
        for name in re.findall(r'([A-Za-z]\w*)\s*=', definition):
            assert re.search(r'^\s+' + re.escape(name) + r':', asset, re.M), f'Missing authored field: {cls}.{name}'
print(f'PASS typed asset binding; {len(languages)} languages, {len(languages["zh-CN"])} keys, {len(used)} current key uses')

for offer in range(3):
    assert 'ui.finance.loan.'+str(offer) in languages['zh-CN']
assert re.findall(r'^\s+- id: (\d+)', asset, re.M)==['0','1','2'], 'Authored loan IDs missing/duplicated'

assert not re.search(r"[^ \n] +(?:economy|employment|retail|production|finance|waste):", asset), "Merged YAML section header"
