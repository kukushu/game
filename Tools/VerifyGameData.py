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
assert used <= set(languages['zh-CN']), f'Missing translations: {used - set(languages["zh-CN"])}'
asset = (folder / 'Economy/DefaultEconomy.asset').read_text(encoding='utf-8-sig')
guid = re.search(r'guid: ([0-9a-f]{32})', asset).group(1)
meta = (root / 'Assets/Scripts/Runtime/GameData/HarborCityEconomyAsset.cs.meta').read_text(encoding='utf-8-sig')
assert 'guid: ' + guid in meta
source = (root / 'Assets/Scripts/Runtime/GameData/CityGameDesign.cs').read_text(encoding='utf-8-sig')
definition = source.split('public sealed class EconomyDefinition')[1].split('[Serializable]')[0]
for name in re.findall(r'([A-Za-z]\w*)\s*=', definition):
    assert re.search(r'^\s+' + re.escape(name) + r':', asset, re.M), f'Missing authored economy field: {name}'
print(f'PASS typed asset binding; {len(languages)} languages, {len(languages["zh-CN"])} keys, {len(used)} current key uses')
