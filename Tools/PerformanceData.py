"""Publish existing performance evidence. Standard library only; never runs a simulation."""
import argparse
import csv
import hashlib
import io
import json
import math
import os
from pathlib import Path, PurePosixPath, PureWindowsPath
import re
import shutil
import subprocess
import sys
import tempfile
from datetime import datetime, timezone
from urllib.parse import quote

VERSION = 1
BENCHMARKS = Path('Docs/Benchmarks')
MANIFEST = 'manifest.json'
MAX_FILE = 8 * 1024 * 1024
MAX_PACKAGE = 32 * 1024 * 1024
TEXT = {'.csv', '.json', '.jsonl', '.txt', '.log', '.md', '.py', '.ps1', '.cs', '.cjs', '.html'}
BINARY = {'.png', '.jpg', '.jpeg', '.svg'}
ROLES = {'samples', 'summary', 'environment', 'validation', 'methodology', 'analysis', 'source', 'image', 'historical'}
KINDS = {'attribution', 'window', 'model', 'microbenchmark', 'experiment', 'legacy'}


class Invalid(ValueError):
    pass


def require(ok, message):
    if not ok:
        raise Invalid(message)


def utc():
    return datetime.now(timezone.utc).isoformat()


def read_json(path):
    def reject(value):
        raise Invalid(f'{path}: non-finite JSON constant {value}')
    def unique(pairs):
        result = {}
        for key, value in pairs:
            require(key not in result, f'{path}: duplicate JSON key {key}')
            result[key] = value
        return result
    def finite(value):
        number = float(value)
        require(math.isfinite(number), f'{path}: non-finite JSON number')
        return number
    return json.loads(Path(path).read_text(encoding='utf-8-sig'), parse_constant=reject, parse_float=finite,
                      object_pairs_hook=unique)


def write_json(path, data):
    path = Path(path)
    temporary = path.with_name(path.name + '.tmp')
    temporary.write_text(json.dumps(data, ensure_ascii=False, indent=2, allow_nan=False) + '\n', encoding='utf-8')
    os.replace(temporary, path)


def git(root, *args):
    result = subprocess.run(['git', '-C', str(root), *args], capture_output=True, text=True, encoding='utf-8')
    require(result.returncode == 0, f'git {args[0]} failed: {result.stderr.strip() or result.stdout.strip()}')
    return result.stdout.strip()


def relative(value):
    require(isinstance(value, str) and bool(value), 'Empty relative path')
    p = PurePosixPath(value)
    require(not p.is_absolute() and not PureWindowsPath(value).is_absolute() and '\\' not in value
            and ':' not in value and all(x not in {'..', '.'} for x in value.split('/')),
            f'Unsafe relative path: {value}')
    return p.as_posix()


def inside(folder, value):
    path = folder / relative(value)
    require(path.resolve().is_relative_to(folder.resolve()), f'Escaping path: {value}')
    require(not path.is_symlink(), f'Symlink is not evidence: {value}')
    return path


def package_path(root, package_id):
    require(re.fullmatch(r'\d{4}-\d{2}-\d{2}-[A-Za-z0-9][A-Za-z0-9._-]*', package_id or ''), 'Invalid dated package id')
    return inside(root / BENCHMARKS, package_id)


def hash_file(path):
    digest = hashlib.sha256()
    with Path(path).open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(block)
    return digest.hexdigest()


def canonical(path, mode):
    raw = path.read_bytes()
    if mode == 'utf8-lf':
        return raw.decode('utf-8-sig').replace('\r\n', '\n').replace('\r', '\n').encode('utf-8')
    require(mode == 'binary', f'Unknown digest mode: {mode}')
    return raw


def csv_rows(path):
    rows = list(csv.reader(io.StringIO(canonical(path, 'utf8-lf').decode('utf-8')), strict=True))
    require(rows and rows[0] and len(set(rows[0])) == len(rows[0]) and all(rows[0]), f'{path}: empty/duplicate CSV header')
    require(len(rows) > 1, f'{path}: no sample rows')
    require(all(len(row) == len(rows[0]) for row in rows[1:]), f'{path}: ragged CSV rows')
    return rows


def validate_format(path, record):
    fmt = record['format']
    if fmt == 'csv':
        rows = csv_rows(path)
        require(rows[0] == list(record['columns']), f'{path}: CSV column order changed')
        require(len(rows) - 1 == record['rows'], f'{path}: CSV row count changed')
        for i, name in enumerate(rows[0]):
            col = record['columns'][name]
            require(col.get('type') in {'text', 'number', 'integer'} and isinstance(col.get('unit'), str)
                    and bool(col['unit']), f'{path}: missing column type/unit: {name}')
            if col['type'] != 'text':
                for row in rows[1:]:
                    value = float(row[i])
                    require(math.isfinite(value), f'{path}: non-finite numeric value in {name}')
                    require(col['type'] != 'integer' or value.is_integer(), f'{path}: non-integer {name}')
    elif fmt == 'json':
        read_json(path)
    elif fmt == 'jsonl':
        for line in path.read_text(encoding='utf-8-sig').splitlines():
            if line.strip():
                json.loads(line, parse_constant=lambda v: (_ for _ in ()).throw(Invalid(f'Non-finite JSONL: {v}')))
    else:
        require(fmt in {'text', 'binary', 'legacy-opaque'}, f'Unknown format: {fmt}')
        if fmt == 'legacy-opaque':
            require(bool(record.get('format_limitation')), f'{path}: legacy parse failure needs explanation')


def artifact(path, target, role, columns=None, legacy=False):
    require(role in ROLES, 'Unknown evidence role')
    suffix = path.suffix.lower()
    require(suffix in TEXT | BINARY, f'File type cannot be published: {path.name}; keep raw/saves/builds local')
    require(path.stat().st_size <= MAX_FILE, f'File exceeds {MAX_FILE} bytes: {path}')
    mode = 'utf8-lf' if suffix in TEXT else 'binary'
    try:
        payload = canonical(path, mode)
    except UnicodeDecodeError:
        require(legacy, f'{path}: exported text must be UTF-8')
        mode = 'binary'
        payload = canonical(path, mode)
    record = dict(path=relative(target), role=role, digest_mode=mode, bytes=len(payload),
                  sha256=hashlib.sha256(payload).hexdigest(), format='text' if mode == 'utf8-lf' else 'binary')
    if mode == 'binary' and suffix in TEXT:
        record.update(format='legacy-opaque', format_limitation='Historical file is not UTF-8; exact binary bytes preserved')
        return record
    if suffix in {'.json', '.jsonl'}:
        record['format'] = suffix[1:]
    if suffix == '.csv':
        try:
            rows = csv_rows(path)
            record.update(format='csv', rows=len(rows) - 1,
                          columns=columns or {name: {'type': 'text', 'unit': 'unknown'} for name in rows[0]})
            require(legacy or columns is not None, f'{path}: declare CSV column types and units in spec')
        except (Invalid, csv.Error) as error:
            if not legacy:
                raise
            record.update(format='legacy-opaque', format_limitation=str(error))
    try:
        validate_format(path, record)
    except (ValueError, csv.Error) as error:
        if not legacy:
            raise
        record.update(format='legacy-opaque', format_limitation=str(error))
    return record


def validate_measurement(data):
    require(data.get('kind') in KINDS and isinstance(data.get('title'), str) and data['title'], 'Measurement kind/title required')
    for key in ('source_commit', 'captured_at_utc', 'unity_version', 'runtime', 'cpu', 'os', 'scenario', 'method'):
        require(key in data, f'Measurement field missing: {key}')
        if data[key] is None:
            require(bool(data.get('unknown_reasons', {}).get(key)), f'Unknown {key} needs reason')
        else:
            require(isinstance(data[key], str) and bool(data[key]), f'Invalid measurement {key}')
    if data['source_commit'] is not None:
        require(re.fullmatch('[0-9a-f]{40}', data['source_commit']), 'Use full measured source SHA, not packaging HEAD')
    if data['captured_at_utc'] is not None:
        stamp = datetime.fromisoformat(data['captured_at_utc'].replace('Z', '+00:00'))
        require(stamp.utcoffset() is not None and stamp.utcoffset().total_seconds() == 0, 'Capture timestamp must be UTC')
    require(isinstance(data.get('limitations'), list) and data['limitations'], 'Measurement limitations required')
    require(isinstance(data.get('commands'), list), 'commands must be argv arrays, or [] if unknown')
    require(all(isinstance(c, list) and c and all(isinstance(x, str) for x in c) for c in data['commands']), 'Invalid command argv')


def local_asset(spec):
    path = Path(spec['path'])
    require(path.is_absolute(), 'Local asset needs absolute path')
    require(spec.get('kind') in {'profiler_raw', 'input_save', 'binary', 'source', 'other'}, 'Unknown local asset kind')
    exists = path.is_file()
    result = dict(spec, path=str(path), present_at_inventory=exists, inventoried_at_utc=utc(),
                  bytes=path.stat().st_size if exists else spec.get('bytes'),
                  sha256=hash_file(path) if exists else spec.get('sha256'))
    require(exists or bool(spec.get('missing_reason')), f'Missing local file needs reason: {path}')
    return result


def validate_manifest(folder, check_local=False):
    manifest = read_json(folder / MANIFEST)
    require(manifest.get('schema_version') == VERSION and manifest.get('id') == folder.name, 'Manifest version/id mismatch')
    validate_measurement(manifest['measurement'])
    files = manifest['artifacts']
    require(files, 'Cannot publish conclusion-only or empty package')
    require(any(a['role'] in {'samples', 'historical'} for a in files), 'Package needs actual data')
    seen = set()
    for a in files:
        key = relative(a['path'])
        require(key.casefold() not in seen and key != MANIFEST, f'Duplicate/reserved artifact: {key}')
        seen.add(key.casefold())
        require(a['role'] in ROLES, f'Unknown role: {key}')
        path = inside(folder, key)
        require(path.is_file(), f'Missing exported data: {path}')
        require(path.suffix.lower() in TEXT | BINARY and path.stat().st_size <= MAX_FILE, f'Forbidden/oversize file: {path}')
        payload = canonical(path, a['digest_mode'])
        require(a['bytes'] == len(payload) and a['sha256'] == hashlib.sha256(payload).hexdigest(), f'Checksum mismatch: {path}')
        validate_format(path, a)
    require(sum(a['bytes'] for a in files) <= MAX_PACKAGE, 'Package exceeds shared data size limit')
    actual = {p.relative_to(folder).as_posix().casefold() for p in folder.rglob('*') if p.is_file() and p.name != MANIFEST}
    require(actual == seen, f'Unlisted/missing files in {folder}; update spec deliberately')
    assets = manifest['local_assets']
    require(manifest['local_inventory']['scope'] and manifest['local_inventory']['limitations'], 'Explain local inventory coverage')
    for a in assets:
        require(PureWindowsPath(a['path']).is_absolute() or Path(a['path']).is_absolute(), 'Local path must be absolute')
        require(a['kind'] in {'profiler_raw', 'input_save', 'binary', 'source', 'other'}, 'Unknown local asset kind')
        require(not a['present_at_inventory'] or (isinstance(a['bytes'], int) and re.fullmatch('[0-9a-f]{64}', a['sha256'])), 'Present local asset needs size/SHA')
        if not a['present_at_inventory']:
            require(a.get('missing_reason'), 'Missing local asset needs reason')
        if a.get('sha256') is not None:
            require(re.fullmatch('[0-9a-f]{64}', a['sha256']), 'Invalid local SHA')
        if a['kind'] == 'profiler_raw':
            export = a['export']
            require(export['status'] in {'partial', 'complete', 'not_exported', 'unknown'}, 'Invalid export status')
            require(export['included'] and export['not_included'], 'Explain exported and unexported coverage')
            targets = export['artifacts']
            require(all(relative(p).casefold() in seen for p in targets), 'Raw export refers to unshared data')
            require(export['status'] not in {'partial', 'complete'} or targets, 'Successful raw export requires actual files')
        if check_local:
            path = Path(a['path'])
            require(path.is_file() == a['present_at_inventory'], f'Local availability changed: {path}')
            if path.is_file():
                require(path.stat().st_size == a['bytes'] and hash_file(path) == a['sha256'], f'Local asset changed: {path}')
    for a in files:
        if PurePosixPath(a['path']).name == 'export-receipt.json':
            receipt = read_json(folder / a['path'])
            require(receipt.get('schema_version') == VERSION, 'Unknown export receipt version')
            original = receipt['raw_asset']
            matching = [x for x in assets if x['path'].replace('\\', '/').casefold() == original['path'].replace('\\', '/').casefold()]
            require(len(matching) == 1 and matching[0]['sha256'] == original['sha256']
                    and matching[0]['bytes'] == original['bytes'], 'Export receipt raw is absent or mismatched in local_assets')
            require(matching[0]['export']['status'] == original['export']['status'], 'Receipt export coverage differs from declared coverage')
            targets = [x for x in files if x['path'] in matching[0]['export']['artifacts']]
            for exported in receipt['artifacts']:
                require(any(all(target[key] == exported[key] for key in ('sha256', 'bytes', 'digest_mode')) for target in targets),
                        f'Export receipt data is missing/changed: {exported["path"]}')
    return manifest


def prepare(root, spec_path, adopt=False):
    spec = read_json(spec_path)
    require(spec.get('schema_version') == VERSION, 'Unsupported spec')
    validate_measurement(spec['measurement'])
    dest = package_path(root, spec['id'])
    require(not (dest / MANIFEST).exists(), 'Immutable package already registered; choose a new id')
    require(adopt == dest.is_dir(), 'New package must not exist; use --adopt for existing evidence')
    dest.parent.mkdir(parents=True, exist_ok=True)
    stage = Path(tempfile.mkdtemp(prefix='.performance-', dir=root / 'Temp'))
    try:
        records = []
        for entry in spec['files']:
            source = Path(entry['source'])
            if not source.is_absolute():
                source = root / source
            require(source.is_file() and not source.is_symlink(), f'Missing/linked source: {source}')
            target = inside(stage, entry['path'])
            require(not target.exists() and entry['path'] != MANIFEST, 'Duplicate/reserved destination')
            target.parent.mkdir(parents=True, exist_ok=True)
            record = artifact(source, entry['path'], entry['role'], entry.get('columns'), spec['measurement']['kind'] == 'legacy')
            shutil.copyfile(source, target)
            records.append(record)
        manifest = dict(schema_version=VERSION, id=spec['id'], packaged_at_utc=utc(), measurement=spec['measurement'],
                        artifacts=records, local_assets=[local_asset(a) for a in spec['local_assets']],
                        local_inventory=spec['local_inventory'])
        write_json(stage / MANIFEST, manifest)
        # Validate under the final id without touching existing package files.
        named = stage / dest.name
        named.mkdir()
        for child in list(stage.iterdir()):
            if child != named:
                shutil.move(str(child), named / child.name)
        validate_manifest(named)
        if adopt:
            listed = {a['path'] for a in records}
            require({p.relative_to(dest).as_posix() for p in dest.rglob('*') if p.is_file()} == listed,
                    'Adoption must inventory every existing file')
            for a in records:
                require(canonical(dest / a['path'], a['digest_mode']) == canonical(named / a['path'], a['digest_mode']), 'Adoption cannot change data')
            shutil.copyfile(named / MANIFEST, dest / MANIFEST)
        else:
            shutil.copytree(named, dest)
        print(f'Prepared {dest.relative_to(root)} ({len(records)} files)')
    finally:
        shutil.rmtree(stage)


def catalog(root):
    base = root / BENCHMARKS
    packages = []
    legacy = []
    for folder in sorted(p for p in base.iterdir() if p.is_dir()):
        if (folder / MANIFEST).is_file():
            data = validate_manifest(folder)
            packages.append(dict(id=data['id'], manifest=f'{data["id"]}/{MANIFEST}', title=data['measurement']['title'],
                                 kind=data['measurement']['kind'], source_commit=data['measurement']['source_commit'],
                                 artifacts=len(data['artifacts']), local_assets=len(data['local_assets'])))
        else:
            legacy.append(folder.name)
    return dict(schema_version=VERSION, packages=packages, unregistered_legacy_directories=legacy)


def verify_all(root, check_local=False):
    expected = catalog(root)
    require(not expected['unregistered_legacy_directories'], 'Every benchmark directory needs a manifest; use prepare --adopt for history')
    for path in (root / BENCHMARKS).iterdir():
        if path.is_file():
            require(path.name in {'index.json', 'README.md', 'README.txt'}, f'Unregistered root artifact: {path.name}')
    require(read_json(root / BENCHMARKS / 'index.json') == expected, 'Benchmark index stale; run index')
    for p in expected['packages']:
        validate_manifest(package_path(root, p['id']), check_local)
    print(f'PASS: {len(expected["packages"])} packages; {sum(p["artifacts"] for p in expected["packages"])} files')


def record_export(capture):
    """Add cheap provenance to the existing exporter, without opening Unity again."""
    capture = capture.resolve()
    resources = read_json(capture / 'export-resources.json')
    require(resources.get('exitCode') == 0 and resources.get('stopped') is None, 'Export did not finish normally')
    raw = capture / 'window.raw'
    require(raw.is_file(), 'Original raw recording missing')
    files = []
    for name in ('hierarchy.csv', 'threads.csv'):
        path = capture / name
        rows = csv_rows(path)
        columns = {c: dict(type='text' if c in {'thread', 'path'} else 'number',
                          unit='label' if c in {'thread', 'path'} else ('bytes' if c == 'gc_bytes' else 'count' if c in {'frame', 'calls'} else 'ms'))
                   for c in rows[0]}
        files.append(artifact(path, name, 'samples', columns))
    asset = local_asset(dict(path=str(raw), kind='profiler_raw', export=dict(status='partial',
        artifacts=[f['path'] for f in files],
        included='CPU main/render/Gfx threads; City.* / PlayerLoop / GC.Collect or Self>=0.2ms hierarchy rows; recorded frame/thread durations.',
        not_included='Other threads and small generic scopes are filtered. Full raw events are local. Unsupported GPU timings are -1; no GPU data invented.')))
    tool = Path(__file__).resolve().parent
    exporter = tool.parent / 'Assets/Scripts/Editor/CityWindowProfilerExport.cs'
    receipt = dict(schema_version=VERSION, export_recorded_at_utc=utc(), raw_asset=asset, artifacts=files,
                   exporter=dict(path=str(exporter), sha256=hash_file(exporter)),
                   resources=resources, note='Receipt creation time is not capture time. This records an existing export; no Unity run or measurement is launched.')
    receipt_path = capture / 'export-receipt.json'
    require(not receipt_path.exists(), 'Export receipt already exists; original provenance is preserved')
    write_json(receipt_path, receipt)
    print(str(receipt_path))


def links(root, ids, ref):
    revision = git(root, 'rev-parse', '--verify', ref + '^{commit}')
    remote = git(root, 'remote', 'get-url', 'origin')
    match = re.fullmatch(r'(?:https://github.com/|git@github.com:)([^/]+/[^/]+?)(?:\.git)?', remote)
    require(match, 'GitHub origin URL required for share links')
    base = 'https://github.com/' + match[1] + '/blob/' + revision + '/'
    raw_base = 'https://raw.githubusercontent.com/' + match[1] + '/' + revision + '/'
    result = [base + 'Docs/Benchmarks/index.json', raw_base + 'Docs/Benchmarks/index.json']
    for package_id in ids:
        folder = package_path(root, package_id)
        data = validate_manifest(folder)
        for file in [MANIFEST] + [a['path'] for a in data['artifacts'] if a['role'] in {'samples', 'summary', 'analysis', 'methodology'}]:
            name = (BENCHMARKS / package_id / file).as_posix()
            git(root, 'cat-file', '-e', revision + ':' + name)
            result.append(base + quote(name, safe='/'))
            result.append(raw_base + quote(name, safe='/'))
    return result


def publish(root, ids, message, push=False, dry_run=False):
    verify_all(root)
    require(ids and len(ids) == len(set(ids)), 'Choose distinct packages to publish')
    folders = [package_path(root, i) for i in ids]
    for folder in folders:
        validate_manifest(folder)
    branch = git(root, 'branch', '--show-current')
    require(branch and branch not in {'main', 'master'}, 'Use the development branch, not detached HEAD/main/master')
    require(not git(root, 'diff', '--cached', '--name-only'), 'Refuse unrelated staged changes; unstage them first')
    for item in read_json(root / BENCHMARKS / 'index.json')['packages']:
        if item['id'] not in ids:
            other = (BENCHMARKS / item['id']).as_posix()
            git(root, 'ls-files', '--error-unmatch', '--', other + '/' + MANIFEST)
            require(not git(root, 'diff', 'HEAD', '--name-only', '--', other),
                    f'Index references unpublished changes in {item["id"]}; publish it too or restore the index')
    paths = [p.relative_to(root).as_posix() for p in folders] + [(BENCHMARKS / 'index.json').as_posix()]
    print(json.dumps(dict(branch=branch, paths=paths, push=push, dry_run=dry_run), ensure_ascii=False))
    if dry_run:
        return
    git(root, 'add', '--', *paths)
    staged = git(root, 'diff', '--cached', '--name-only', '-z').split('\0')
    require(all(not p or any(p == x or p.startswith(x + '/') for x in paths) for p in staged), 'Unexpected staged path')
    if git(root, 'diff', '--cached', '--name-only'):
        git(root, 'diff', '--cached', '--check')
        git(root, 'commit', '-m', message)
    revision = git(root, 'rev-parse', 'HEAD')
    if push:
        try:
            git(root, 'push', 'origin', 'HEAD:refs/heads/' + branch)
        except Invalid as error:
            raise Invalid(f'Local commit {revision} retained; push failed; retry push, do not rebuild data. {error}') from error
    print(('Pushed ' if push else 'Local commit ') + revision)
    for url in links(root, ids, revision):
        print(url)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
    sub = parser.add_subparsers(dest='command', required=True)
    p = sub.add_parser('prepare'); p.add_argument('--spec', type=Path, required=True); p.add_argument('--adopt', action='store_true')
    p = sub.add_parser('verify'); p.add_argument('--package'); p.add_argument('--all', action='store_true'); p.add_argument('--local', action='store_true')
    sub.add_parser('index')
    p = sub.add_parser('record-export'); p.add_argument('--capture', type=Path, required=True)
    p = sub.add_parser('links'); p.add_argument('--package', action='append', required=True); p.add_argument('--ref', default='HEAD')
    p = sub.add_parser('publish'); p.add_argument('--package', action='append', required=True); p.add_argument('--message', required=True); p.add_argument('--push', action='store_true'); p.add_argument('--dry-run', action='store_true')
    args = parser.parse_args()
    root = args.root.resolve()
    if args.command == 'prepare':
        (root / 'Temp').mkdir(exist_ok=True)
        prepare(root, args.spec, args.adopt)
    elif args.command == 'index':
        write_json(root / BENCHMARKS / 'index.json', catalog(root))
    elif args.command == 'record-export':
        record_export(args.capture)
    elif args.command == 'verify':
        require(args.all != bool(args.package), 'Choose --all or --package')
        if args.all:
            verify_all(root, args.local)
        else:
            validate_manifest(package_path(root, args.package), args.local)
            print('PASS: ' + args.package)
    elif args.command == 'links':
        print('\n'.join(links(root, args.package, args.ref)))
    else:
        publish(root, args.package, args.message, args.push, args.dry_run)


if __name__ == '__main__':
    try:
        main()
    except (Invalid, OSError, ValueError, KeyError, TypeError, csv.Error) as error:
        print(f'FAIL: {error}', file=sys.stderr)
        sys.exit(1)
