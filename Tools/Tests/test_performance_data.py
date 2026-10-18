"""Integrity/publishing regression tests; no Unity, network or city simulation."""
import copy
import importlib.util
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('performance_data', Path(__file__).parents[1] / 'PerformanceData.py')
pd = importlib.util.module_from_spec(spec)
spec.loader.exec_module(pd)


class EvidenceTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        (self.root / 'Temp').mkdir()
        self.csv = self.root / 'sample.csv'
        self.csv.write_text('phase,ms\npreview,1.5\n', encoding='utf-8')
        self.data = dict(schema_version=1, id='2026-10-17-Test01',
            measurement=dict(title='Test actual sample', kind='attribution', source_commit='a' * 40,
                captured_at_utc=None, unity_version='test', runtime='test', cpu=None, os=None,
                scenario='unit fixture', method='integrity test, not a city benchmark', commands=[],
                unknown_reasons=dict(captured_at_utc='not measured', cpu='not measured', os='not measured'),
                limitations=['synthetic integrity fixture']),
            files=[dict(source=str(self.csv), path='data/sample.csv', role='samples',
                columns={'phase': dict(type='text', unit='label'), 'ms': dict(type='number', unit='ms')})],
            local_assets=[], local_inventory=dict(scope='fixture', limitations='no raw capture exists'))
        self.spec_path = self.root / 'spec.json'

    def prepare(self):
        pd.write_json(self.spec_path, self.data)
        pd.prepare(self.root, self.spec_path)
        self.folder = self.root / pd.BENCHMARKS / self.data['id']
        return self.folder

    def manifest(self, mutate):
        data = pd.read_json(self.folder / pd.MANIFEST)
        mutate(data)
        pd.write_json(self.folder / pd.MANIFEST, data)

    def test_prepare_and_cross_platform_newlines(self):
        folder = self.prepare()
        sample = folder / 'data/sample.csv'
        sample.write_bytes(pd.canonical(sample, 'utf8-lf').replace(b'\n', b'\r\n'))
        pd.validate_manifest(folder)
        pd.write_json(self.root / pd.BENCHMARKS / 'index.json', pd.catalog(self.root))
        pd.verify_all(self.root)

    def test_tamper_and_unlisted_file(self):
        folder = self.prepare()
        (folder / 'extra.txt').write_text('unlisted')
        with self.assertRaises(pd.Invalid):
            pd.validate_manifest(folder)
        (folder / 'extra.txt').unlink()
        (folder / 'data/sample.csv').write_text('phase,ms\npreview,2.5\n')
        with self.assertRaises(pd.Invalid):
            pd.validate_manifest(folder)

    def test_columns_units_and_nonfinite_rejected(self):
        self.data['files'][0]['columns']['ms']['unit'] = ''
        with self.assertRaises(pd.Invalid):
            self.prepare()
        self.data['files'][0]['columns']['ms']['unit'] = 'ms'
        self.csv.write_text('phase,ms\npreview,NaN\n')
        with self.assertRaises(pd.Invalid):
            self.prepare()
        self.assertFalse((self.root / pd.BENCHMARKS / self.data['id']).exists())

    def test_path_escape_and_reserved_files(self):
        for unsafe in ('../escape.csv', '/tmp/escape.csv', 'C:/escape.csv', 'data/../../escape.csv'):
            self.data['files'][0]['path'] = unsafe
            with self.subTest(unsafe=unsafe), self.assertRaises(pd.Invalid):
                self.prepare()

    def test_symlink_source_rejected(self):
        linked = self.root / 'link.csv'
        try:
            linked.symlink_to(self.csv)
        except OSError:
            self.skipTest('OS does not permit symlinks')
        self.data['files'][0]['source'] = str(linked)
        with self.assertRaises(pd.Invalid):
            self.prepare()

    def test_raw_export_reference_and_offline_verification(self):
        raw = self.root / 'window.raw'
        raw.write_bytes(b'raw fixture')
        self.data['local_assets'] = [dict(path=str(raw), kind='profiler_raw',
            export=dict(status='partial', artifacts=['data/sample.csv'], included='fixture only', not_included='other frames'))]
        folder = self.prepare()
        pd.validate_manifest(folder, check_local=True)
        raw.unlink()
        pd.validate_manifest(folder)  # GitHub readers do not need local raw.
        with self.assertRaises(pd.Invalid):
            pd.validate_manifest(folder, check_local=True)
        self.manifest(lambda d: d['local_assets'][0]['export'].update(artifacts=['missing.csv']))
        with self.assertRaises(pd.Invalid):
            pd.validate_manifest(folder)

    def test_missing_local_assets_and_unknown_history(self):
        self.data['local_assets'] = [dict(path=str(self.root / 'missing.hcity'), kind='input_save', missing_reason='lost during migration')]
        pd.validate_manifest(self.prepare(), check_local=True)
        self.manifest(lambda d: d['measurement']['unknown_reasons'].pop('cpu'))
        with self.assertRaises(pd.Invalid):
            pd.validate_manifest(self.folder)

    def test_immutable_package_and_ragged_csv(self):
        self.prepare()
        with self.assertRaises(pd.Invalid):
            self.prepare()
        self.data['id'] = '2026-10-17-Test02'
        self.csv.write_text('phase,ms\npreview,1,extra\n')
        with self.assertRaises(pd.Invalid):
            self.prepare()

    def test_duplicate_headers_and_json_keys(self):
        self.csv.write_text('ms,ms\n1,2\n')
        with self.assertRaises(pd.Invalid):
            self.prepare()
        self.spec_path.write_text('{"a":1,"a":2}')
        with self.assertRaises(pd.Invalid):
            pd.read_json(self.spec_path)
        self.spec_path.write_text('{"value":1e999}')
        with self.assertRaises(pd.Invalid):
            pd.read_json(self.spec_path)

    def test_raw_or_save_not_published_as_artifact(self):
        raw = self.root / 'window.raw'
        raw.write_bytes(b'raw')
        self.data['files'][0].update(source=str(raw), path='data/window.raw')
        with self.assertRaises(pd.Invalid):
            self.prepare()

    def test_export_receipt_reuses_existing_files(self):
        capture = self.root / 'capture'
        capture.mkdir()
        (capture / 'window.raw').write_bytes(b'original recording')
        (capture / 'hierarchy.csv').write_text('frame,thread,path,total_ms,self_ms,calls,gc_bytes\n0,Main Thread,City.Test,1,1,1,0\n')
        (capture / 'threads.csv').write_text('frame,thread,frame_ms,gpu_ms\n0,Main Thread,1,-1\n')
        pd.write_json(capture / 'export-resources.json', dict(exitCode=0, stopped=None))
        pd.record_export(capture)
        receipt = pd.read_json(capture / 'export-receipt.json')
        self.assertEqual(pd.hash_file(capture / 'window.raw'), receipt['raw_asset']['sha256'])
        self.assertEqual('partial', receipt['raw_asset']['export']['status'])
        self.assertEqual(2, len(receipt['artifacts']))
        with self.assertRaises(pd.Invalid):
            pd.record_export(capture)
        self.data['local_assets'] = [receipt['raw_asset']]
        for record in receipt['artifacts']:
            self.data['files'].append(dict(source=str(capture / record['path']), path=record['path'],
                role='samples', columns=record['columns']))
        self.data['files'].append(dict(source=str(capture / 'export-receipt.json'), path='export-receipt.json', role='environment'))
        self.prepare()
        self.data['id'] = '2026-10-17-ChangedExport'
        (capture / 'hierarchy.csv').write_text('frame,thread,path,total_ms,self_ms,calls,gc_bytes\n0,Main Thread,City.Test,2,2,1,0\n')
        with self.assertRaisesRegex(pd.Invalid, 'Export receipt data is missing/changed'):
            self.prepare()

    def test_failed_export_cannot_claim_receipt(self):
        capture = self.root / 'failed-capture'
        capture.mkdir()
        pd.write_json(capture / 'export-resources.json', dict(exitCode=0, stopped='memory-limit'))
        with self.assertRaises(pd.Invalid):
            pd.record_export(capture)
        self.assertFalse((capture / 'export-receipt.json').exists())

    def test_corrupt_catalog_rejected(self):
        self.prepare()
        pd.write_json(self.root / pd.BENCHMARKS / 'index.json', dict(packages=[]))
        with self.assertRaises(pd.Invalid):
            pd.verify_all(self.root)

    def init_git(self):
        def run(*args):
            return pd.git(self.root, *args)
        run('init', '-b', 'evidence-test')
        run('config', 'user.name', 'Evidence Test')
        run('config', 'user.email', 'evidence@example.invalid')
        run('remote', 'add', 'origin', 'git@github.com:example/evidence.git')
        (self.root / 'unrelated.txt').write_text('original')
        run('add', 'unrelated.txt')
        run('commit', '-m', 'initial fixture')
        self.prepare()
        pd.write_json(self.root / pd.BENCHMARKS / 'index.json', pd.catalog(self.root))
        return run

    def test_publish_dry_run_and_no_unrelated_changes(self):
        run = self.init_git()
        before = run('rev-parse', 'HEAD')
        (self.root / 'unrelated.txt').write_text('preserved local edit')
        pd.publish(self.root, [self.data['id']], 'publish fixture', dry_run=True)
        self.assertEqual(before, run('rev-parse', 'HEAD'))
        self.assertEqual('', run('diff', '--cached', '--name-only'))
        pd.publish(self.root, [self.data['id']], 'publish fixture')
        self.assertEqual('preserved local edit', (self.root / 'unrelated.txt').read_text())
        self.assertNotIn('unrelated.txt', run('show', '--pretty=format:', '--name-only', 'HEAD'))
        self.assertTrue(pd.links(self.root, [self.data['id']], 'HEAD')[0].startswith('https://github.com/example/evidence/blob/'))

    def test_original_log_whitespace_preserved_without_disabling_code_checks(self):
        captured = self.root / 'captured.log'
        captured.write_bytes(b'original trailing space \r\n\r\n')
        self.data['files'].append(dict(source=str(captured), path='data/player.log', role='environment'))
        run = self.init_git()
        run('config', 'core.whitespace', 'blank-at-eol')
        attributes = Path(__file__).parents[2] / '.gitattributes'
        (self.root / '.gitattributes').write_bytes(attributes.read_bytes())
        run('add', '.gitattributes'); run('commit', '-m', 'scope data whitespace policy')
        pd.publish(self.root, [self.data['id']], 'preserve original capture whitespace')
        self.assertEqual(b'original trailing space \n\n', pd.canonical(self.folder / 'data/player.log', 'utf8-lf'))
        (self.root / 'unrelated.txt').write_text('bad code whitespace \n')
        run('add', 'unrelated.txt')
        with self.assertRaisesRegex(pd.Invalid, 'trailing whitespace'):
            run('diff', '--cached', '--check')

    def test_publish_refuses_unrelated_staging_and_primary(self):
        run = self.init_git()
        (self.root / 'unrelated.txt').write_text('staged edit')
        run('add', 'unrelated.txt')
        with self.assertRaises(pd.Invalid):
            pd.publish(self.root, [self.data['id']], 'refuse')
        run('reset', '--', 'unrelated.txt')
        run('branch', '-m', 'main')
        with self.assertRaises(pd.Invalid):
            pd.publish(self.root, [self.data['id']], 'refuse')

    def test_index_cannot_publish_unselected_new_package(self):
        self.init_git()
        first = self.data['id']
        self.data['id'] = '2026-10-17-Unselected'
        self.prepare()
        pd.write_json(self.root / pd.BENCHMARKS / 'index.json', pd.catalog(self.root))
        with self.assertRaises(pd.Invalid):
            pd.publish(self.root, [first], 'refuse dangling index')

    def test_failed_push_keeps_local_commit(self):
        run = self.init_git()
        before = run('rev-parse', 'HEAD')
        original = pd.git
        def fail_push(root, *args):
            if args[0] == 'push':
                raise pd.Invalid('simulated disconnected remote')
            return original(root, *args)
        with patch.object(pd, 'git', fail_push), self.assertRaisesRegex(pd.Invalid, 'Local commit .* retained'):
            pd.publish(self.root, [self.data['id']], 'keep local evidence', push=True)
        self.assertNotEqual(before, run('rev-parse', 'HEAD'))
        self.assertEqual('', run('diff', '--cached', '--name-only'))


if __name__ == '__main__':
    unittest.main()
